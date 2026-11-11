# It. 15 — Deuda resoluble en la PC (spec de producto)

| Campo | Valor |
|---|---|
| Iteración | 15 (`15-deuda-pc`), [ROADMAP §It. 15](../../ROADMAP.md). **Fuera del plan de paridad** (pedida por el usuario después de la It. 14) |
| Alcance | Issues de deuda abiertos que se resuelven **y se verifican** en la PC de desarrollo (Windows, sin teléfono, sin Mac, sin remo ni pulsómetro): Historial (#32 a #39, #67), Exportar TCX (#66, #70 a #73) y pulido de la It. 14 (#74 a #82, y el resto del #78) |
| Fuentes | Los issues (API pública de GitHub), los REPORT y las specs de las It. [9](../09-historial/REPORT.md), [12](../12-exportar-tcx/REPORT.md) y [14](../14-pulido-plataformas/REPORT.md), y [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura; **no** se leyó `lib/core/strava/strava_secrets.dart`) |
| Ya en el árbol | El arreglo del coordinador del intermitente del #78 (`UiTestBase.Settle(maxSeconds)` y la guarda de `BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame`): no se revierte; AC-D15-39 lo verifica |
| Modo | Entrega rápida, sin debates. Ante una duda, el legacy o un valor por defecto, anotado en una línea (§6) |
| Criterios | **40** (§3). 10 capturas nuevas y 1 auxiliar (set `it15`, §5) |
| Gate | Corregida según [03-qa-gate](03-qa-gate.md): AC-D15-22, -25, -30, -34 (QA-G15-01 a -04), AC-D15-03 y §4 (QA-G15-05), m-04, m-06, m-07. Cada cambio lleva su motivo en el texto |

## 0. Cómo leer esta spec

- Tamaños (TV 411 × 914, TH 914 × 411, L640 640 × 360, P360 360 × 800, W1280 1280 × 720, en dp), reloj de prueba y `ROWMATE_DATA_DIR` temporal: los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md). Textos entre comillas invertidas: exactos al carácter.
- Marcas: `[dotnet]` Core y herramientas con `dotnet test`; `[Editor]` EditMode/PlayMode, capturas y lectura de documentos; `[Device-Win]` build de desarrollo con `ROWMATE_DATA_DIR` temporal (QA) o build normal (demo del usuario). Lo que pide Android, Apple, hardware o una decisión del usuario queda **fuera** (§4) o **NO VERIFICABLE**.
- Ninguna prueba, captura ni demo toca `prefs.json`, las bases ni las carpetas reales del usuario. Nada de Bluetooth real: `-BleSmoke` tal como está.
- IDs: `AC-D15-nn`. Decisiones nuevas `DV-140` a `DV-148` (siguen a DV-139).

## 1. Objetivo y demo

**Objetivo.** Cerrar la deuda chica que dejó el plan, sin cambiar lo que el usuario ya aceptó: el Historial más pulido y medido, exportar también desde la tarjeta, un TCX más robusto, la ventana que recuerda dónde estaba, una sola forma de nombrar cada métrica, y las pruebas y capturas que faltaban. Todos los tests de las It. 1 a 14 siguen en verde.

**Qué cambia a la vista** (en `Builds/Windows/simulator`, con datos sembrados):

1. **Historial.** Cada tarjeta exportable tiene un ícono de exportar (18 dp) a la izquierda del chevron: un toque exporta sin abrir el Detalle, y el aviso aparece sobre la barra de pestañas. La columna de potencia dice `Vatios` y el split se lee `2:13` (antes `Watts` y `02:13`).
2. **Detalle.** El ícono de exportar es el glifo de la fuente (no el dibujado). Tocar una fila de `Rendimiento por paso` marca la fila y resalta su franja en el gráfico. La curva ya no hace un piquito en cada escalón. El *tooltip* tiene sombra. El resumen usa `Vatios` y `2:13`.
3. **Exportar.** Si `Abrir carpeta` no puede abrir el Explorador, avisa `No se pudo abrir la carpeta`. El TCX lleva `<Creator>` `RowMate 1.0` y nunca una vuelta con distancia negativa.
4. **HUD.** `Vatios` / `Split 500m` en es (`Watts` / `Split 500m` en en), con el *tracking* de 0,04 em en los rótulos con minúsculas. Al cambiar el tamaño de la ventana, el HUD entra en cascada corta con 8 dp de traslado.
5. **App.** La ventana vuelve a abrir donde y del tamaño en que se cerró (no en pantalla completa). La cinta `SIMULADOR` ya no tapa el título. (Estela: no se agrega; ver AC-D15-34, gate QA-G15-01.)

**Demo (≈ 10 min, `Builds/Windows/simulator`).** Remá dos sesiones cortas (una libre y `Escalera`) → Historial: exportá desde la tarjeta y mirá el aviso → abrí `Escalera`: tocá la fila 2 y mirá la franja; pasá a `Split 500m` y mirá los escalones → exportá desde el Detalle → movés y achicás la ventana, cerrás y reabrís: vuelve igual.

## 2. Historias

- Como remero quiero exportar una sesión desde la lista para no tener que abrir cada Detalle.
- Como remero quiero ver en el gráfico dónde cae cada paso de la tabla, y que la curva no muestre picos que no remé.
- Como usuario de escritorio quiero que la ventana recuerde su lugar y tamaño.
- Como usuario quiero que cada métrica se llame igual en todas las pantallas.
- Como mantenedor quiero las pruebas, sondas y capturas que faltaban, y ningún test inestable.

## 3. Criterios de aceptación

### 3.1 Historial y Detalle (#32, #33, #34, #35, #37, #38, #39, #67)

- **AC-D15-01 · #32 Curva sin picos (DV-142).** *Dado* cualquiera de las 5 métricas, *entonces* entre dos puntos consecutivos la curva dibujada no sale del rango `[mín, máx]` de esos dos valores (tolerancia 0,5 dp por el borde suavizado); con dos valores iguales seguidos el tramo es recto. Con un escalón de 100 → 200 W ningún vértice de la línea central teselada ni del área pasa de 200 ni baja de 100 (el borde suavizado del trazo de 2 dp puede salir ±1 dp: no cuenta; gate m-06); con el split invertido, lo mismo en su eje. Se conservan el umbral de 3 dp del arte de la 9 y el resto de la forma de la cúbica (legacy `isCurved: true`, `curveSmoothness: 0.2`, `session_detail_screen.dart:472-473`, sin `preventCurveOverShooting`: cambio frente a `fl_chart`). [dotnet] · CAP-04
- **AC-D15-02 · #33 Fila ↔ franja (DV-146).** *Dado* el Detalle de una rutina, *cuando* se toca una fila de `Rendimiento por paso` (o se la enfoca con Tab y se aprieta Enter o Espacio), *entonces* la fila queda marcada (borde de 1 dp alrededor, del color de su tipo) y **todas** las franjas de ese índice de paso pasan del 10 % al **22 %** de opacidad, en 160 ms (`ease.enter`; al instante con "reducir movimiento"). Tocar la misma fila, `Esc` o tocar otra fila desmarca o cambia (una sola marcada). La marca sigue al cambiar de métrica y se pierde al salir del Detalle. El lector anuncia la fila con el estado seleccionado (el mismo que el selector de métricas). Una libre (sin filas) no cambia. [Editor] · CAP-03
- **AC-D15-03 · #34 Sonda del Historial en el build.** *Dado* el build de desarrollo con `ROWMATE_DATA_DIR` temporal y `F-9P` (200 sesiones) sembrada, *cuando* corre con `-rowmate-perf-history`, *entonces* el registro tiene una línea `[PERF] history` con: el peor cuadro de una recarga de la lista (**≤ 50 ms**), el del Detalle de 30 min (≤ 50 ms) y el desplazamiento de la lista con la rueda durante 10 s (p95 **≤ 16,7 ms** y ≤ 1 % de cuadros > 33 ms). El argumento existe solo en desarrollo (`BuildOutputInspector` lo revisa en el normal). Cierra la parte NO VERIFICABLE de AC-HIS-19-05. Si la recarga pasa de 50 ms, se aplica el plan B del diseño (DT15-19) y se vuelve a medir; si aun así no llega, este AC queda **FAIL con la causa medida** y esa parte vuelve a #34 (gate QA-G15-05). [Device-Win]
- **AC-D15-04 · #34 Basura por cuadro.** *Dado* el Detalle de `F-9P` n.º 1 en el Editor, *entonces*, medido con FrameAllocations del Profiler: el cursor moviéndose 60 cuadros asigna en **0** cuadros; la entrada del gráfico (48 cuadros) y el cambio de métrica (24 cuadros) asignan como mucho en **1** cuadro cada uno (el de armado). Antes: 3/60, 15/48 y 10/24 (REPORT de la 9 §3). [Editor] *(Cierre del PO, QA-V15-R2-02: **FAIL aceptado con la causa medida**, como el plan B de D15-03. Quedó entrada 2/48, cambio 10/24, cursor 2/60 en frío y 0/60 en caliente; `chart-mesh` asigna 0. Lo que falta (páginas nuevas de UIR en la entrada y el cruce de colores del selector de métrica) vuelve a #34.)*
- **AC-D15-05 · #35 Nombres únicos y sonda de aspecto.** *Entonces* las columnas de la tarjeta *i* se llaman `history-card-{i}-stat-{j}` y las del resumen `session-summary-stat-{j}`: ningún nombre de columna se repite en la lista. Re-tomado el set `it9`, la sonda de aspecto de CAP-01 da **2/2**; una sonda de aspecto que falla marca la toma como fallida en el *manifest*. [Editor]
- **AC-D15-06 · #37 Sombra del *tooltip*.** *Entonces* el *tooltip* del cursor tiene una sombra negra al **35 %**, con 8 dp de desenfoque y desplazada **2 dp** hacia abajo, sin cambiar su tamaño, su posición ni el contraste del texto (≥ 6,46:1, arte de la 9). Con "reducir movimiento" se ve igual. [Editor] · CAP-04
- **AC-D15-07 · #38 Casos borde.** *Entonces*: (a) una recarga con los mismos datos no emite cambio (igualdad por valor de la lista, `HistoryState`; ya cumple, QA lo confirma con su prueba); (b) con un solo punto, el gráfico lo centra sin dividir por 0 (`ChartSeriesTests.OnePointDoesNotFail`; ya cumple); (c) los mensajes de error de capacidad de la malla del gráfico escriben los números con el formateador de enteros de la app; (d) una rutina agregada con la pestaña Rutinas oculta está a la vista con su alto final a los **≤ 400 ms** de mostrar la pestaña (crece o aparece directo; nunca queda en alto 0). [dotnet] · [Editor]
- **AC-D15-08 · #39 y #34 Erratas de la It. 9.** *Entonces* en `09-historial/`: la spec dice `F-9P` = **72 400** puntos (200 × 362); AC-HIS-15-03 y CAP-09 dicen "a la altura de su ritmo" en lugar de "marcas" 2:08 y 2:06; el §5.5 del diseño deja solo la salida de 160 ms del cursor; el §13 del diseño dice lo que mide AC-D15-04; el arte §7 aclara que el presupuesto ≤ 4 500 / ≤ 9 500 vértices vale para un cuadro quieto (en el fundido cruzado hay dos curvas). Cada cambio con una línea "Corregido en la It. 15". [Editor] (lectura)
- **AC-D15-09 · #67 Exportar desde la tarjeta (DV-143).** *Dado* el Historial, *entonces* cada tarjeta de una sesión exportable (AC-STR-09-10) tiene, a la izquierda del chevron, un botón de ícono con el glifo de exportar de la plataforma a **18 dp** en `text.secondary` y área de toque de **40 dp**, con la ayuda `exportTcx` y el nombre accesible `exportTcxFor` (`Exportar TCX: {session}`, con `{session}` = nombre de la tarjeta y fecha como las lee el lector). Las no exportables (`F-9` n.º 4) no lo tienen. Tocarlo **no** abre el Detalle; mientras exporta muestra el indicador de 18 dp (mínimo 300 ms, AC-STR-09-18) y otro toque no hace nada. Se llega con Tab después de la tarjeta. El Detalle conserva su botón (DV-124 sigue). [Editor] · CAP-01
- **AC-D15-10 · #67 Aviso y archivo desde la tarjeta.** *Cuando* termina, *entonces* aparece el mismo aviso que en el Detalle (AC-STR-09-19 éxito con `Abrir carpeta`, AC-STR-09-21 error con `Reintentar`, 6 s), **sobre la barra de pestañas**, y el archivo es idéntico byte a byte al que exporta el Detalle para esa sesión (con `_SIM` / `[SIM]` en el simulador). La base no cambia (sin sello de "exportada", §4). Volver a tocar la pestaña Historial durante la exportación no la cancela. [Editor] · CAP-02

### 3.2 Exportar TCX (#66, #70, #71, #72, #73)

- **AC-D15-11 · #66 Glifos en la fuente.** *Entonces* el recorte de íconos de contorno suma `download` (`U+F090`) y `ios_share` (`U+E6B8`) de Material Symbols Rounded (commit `10ecc14`, la receta de ASSET_SLOTS §4.1 con `uvx --from fonttools==4.67.0`, sin instalar Python), con el `NOTICE.md` al día. El botón del Detalle (Windows: `download`; Android: `ios_share`) y el de la tarjeta usan el glifo de la fuente, del mismo tamaño que la flecha de volver de la barra en el Detalle; se retiran `ExportGlyph` y su vista. Los glifos que ya estaban quedan idénticos (contornos y avances). [Editor] · [dotnet] · CAP-05
- **AC-D15-12 · #66 Limpieza del recorte.** *Entonces* salen del recorte los glifos de Strava que **ninguna** pantalla usa entre `cloud_upload` (`E2C3`), `cloud_download` (`E2C0`), `cloud_done` (`E2BF`), `person` (`F0D3`), `link` (`E250`) y `link_off` (`E16F`); una prueba confirma que cada glifo del recorte tiene al menos un uso y que cada ícono usado está en el recorte. ASSET_SLOTS §4.1 y §4.3 quedan con la lista nueva. [dotnet] · [Editor]
- **AC-D15-13 · #70 `Abrir carpeta` con respaldo y aviso.** *Cuando* se toca `Abrir carpeta` y el Explorador con el archivo seleccionado no se puede abrir, *entonces* se prueba el respaldo del sistema que abre la carpeta y selecciona el archivo; si también falla, aparece el aviso `exportTcxOpenFolderFailed` (`No se pudo abrir la carpeta`) por 4 s, sin acción, y el detalle técnico va al registro con la ruta abreviada (AC-STR-09-19). En las pruebas lo simula el destino falso y nunca se abre el Explorador. [Editor] · CAP-06. El Explorador real: demo del usuario en el build normal (NO VERIFICABLE por QA)
- **AC-D15-14 · #73 Vuelta sin distancia negativa (DV-144).** *Dado* un monitor que reinicia la distancia a mitad de una vuelta (último − primero < 0), *entonces* esa vuelta lleva `<DistanceMeters>0</DistanceMeters>`; los `Trackpoint` siguen con la distancia del monitor tal cual (DV-127). Las sesiones sin reinicio salen iguales. Cambio frente al legacy (`tcx_builder.dart:36-42`, que escribía el negativo). [dotnet]
- **AC-D15-15 · #73 `<Creator>`.** *Entonces* el `Activity` termina con, después de `Notes` (o de la última vuelta si no hay `Notes`), exactamente: `<Creator xsi:type="Device_t">` con `<Name>RowMate</Name>`, `<UnitId>0</UnitId>`, `<ProductID>0</ProductID>` y `<Version>` con `VersionMajor`, `VersionMinor`, `BuildMajor` y `BuildMinor` tomados de la versión de la app (`1.0.0` → `1`, `0`, `0`, `0`), con la sangría del golden. El golden de AC-STR-09-01 se actualiza y la paridad con el legacy queda "idéntico salvo `Notes` y `Creator`" en las 10 sesiones. [dotnet]
- **AC-D15-16 · #71 Pruebas destrabadas.** *Entonces* (a) los 4 tests ignorados de `ItTwelveToolingTests` (manifiesto de exportación, exportador de Android con la purga, `CredentialNeedles` en el inspector del build y el set `it12` en `capture-snapshots.ps1`) **corren y pasan**, sin `Ignore`; (b) `TcxExportFrameTests` siembra la sesión de 3 h (2 160 puntos) y el aviso llega en ≤ 1 s sin cuadros > 50 ms (AC-STR-09-12); (c) los 3 instantes de `T-2M` en Europe/Madrid (AC-STR-09-03) se prueban también en Unity; (d) "sin red" (`NoNetworkAccess`) es una prueba automática, no una revisión a mano. Después se corren EditMode y PlayMode, normal y `-Simulator`. [Editor] · [dotnet]
- **AC-D15-17 · #71 Inestables con causa.** *Entonces* `ThreeHoursUnder200Ms` mide sin el primer armado (calentamiento) y pasa **20 de 20** corridas seguidas de su clase; `BleAllocationTests.TheSonarDoesNotAllocatePerFrame` tiene su asignación esporádica encontrada con FrameAllocations y corregida (la causa va en una línea en `02-tech-design`), y pasa en **3 corridas completas** de PlayMode `-Simulator` y **20 de 20** de su clase. Ningún umbral sube. [dotnet] · [Editor]
- **AC-D15-18 · #72 Documentos de la 12 y el set `it9`.** *Entonces*: `12-exportar-tcx/04-design-notes` remite al §9.2 del diseño (m-01); `02-tech-design` §12 apunta a `03-qa-gate.md` (m-11); la spec dice "al pie de la pantalla, sin tapar la barra superior" donde decía "sobre la barra de pestañas" para el Detalle (QA-V12-03); la spec §6 aclara que no hay sesión `T-4`; el comentario del árbol de `SessionDetailPage.cs` queda con la sangría bien (QA-V12-05). El set `it9` se regenera entero en su carpeta (`09-historial/snapshots`) y muestra el botón de exportar. Los escapes `\uXXXX` de los archivos que lo necesitan se escriben por script y se verifican en C#. [Editor] (lectura y capturas)

### 3.3 Pulido de la It. 14 (#74 a #82, #78)

- **AC-D15-19 · #74 La ventana se recuerda (DV-145).** *Dado* Windows, *cuando* se cierra la app en ventana, *entonces* se guardan en `prefs.json` `window.x`, `window.y`, `window.width`, `window.height` (dp lógicos, enteros) y `window.maximized`; al reabrir, la ventana vuelve a ese lugar, tamaño y estado. Si se cerró en pantalla completa se guarda la ventana de antes (la pantalla completa **no** se recuerda, DV-129). Si el rectángulo guardado no queda visible al menos en un **50 %** en algún monitor actual, o falta, abre centrada en 1280 × 720 como hoy (DV-130); nunca por debajo de 640 × 360. Las pruebas usan un `prefs.json` temporal. [Editor] · [Device-Win] (build de desarrollo con `ROWMATE_DATA_DIR`)
- **AC-D15-20 · #74 `.ico` versionado.** *Entonces* la herramienta del ícono (`tools/icon`) genera también un `RowMate.ico` con **16, 24, 32, 48, 64 y 256** px, versionado en el repo, reproducible (dos corridas dan el mismo archivo byte a byte) y sin datos del equipo. El ícono que arma Unity no cambia. [dotnet]
- **AC-D15-21 · #74 `Alt+Enter` con un botón enfocado.** *Dado* un botón con el foco (p. ej. `Iniciar` en Workout), *cuando* se aprieta `Alt+Enter`, *entonces* solo cambia la pantalla completa: el botón **no** se activa. `Enter` solo sigue activándolo. [Editor]
- **AC-D15-22 · #75 Un nombre por métrica (DV-140).** *Entonces* cada métrica se llama igual (sin distinguir mayúsculas) en **los rótulos de métrica** de estas familias de claves: `metric*` (Dispositivo), `sessionChart*` (selector del Detalle), `historyStat*` (tarjeta y resumen) y los rótulos del HUD `hudWatts`, `hudSplit`, `hudSpm`. Nombres: potencia `Vatios` / `Watts`, split `Split 500m`, cadencia `SPM`, pulso `Pulso` / `HR`, distancia `Distancia` / `Distance`, tiempo `Tiempo` / `Time`. Dispositivo los muestra en mayúsculas (`VATIOS`, `SPLIT 500m`, sin cambios). **Excepciones cerradas**: `historyStatSplit` = `Split` (única abreviatura, columnas angostas; legacy `history_screen.dart:115`), `hudBpm` = `BPM` (el pulso del HUD como unidad, AC-D15-24), las calorías (`kcal` es unidad, no nombre) y los campos del editor (`editor*`, p. ej. `Incremento Watts (+/rep)`: va a issues). Cualquier clave nueva de esas familias tiene que estar en la tabla o en las excepciones. Cambian 3 claves (tabla de §3.5); el resto del catálogo queda igual. [dotnet] · [Editor] · CAP-07, CAP-08 *(Alcance cerrado por el gate QA-G15-04: sin familias ni excepciones, la prueba fallaba o no probaba nada.)*
- **AC-D15-23 · #75 Split `m:ss` en Historial y Detalle (DV-141).** *Entonces* en la tarjeta y en el resumen el split se escribe `m:ss` (`2:13`, `10:05`), igual que las filas por paso (`3:00/500m`), los ejes y el *tooltip*; sin dato sigue `--:--`. Dispositivo y el HUD siguen con `mm:ss` (`02:13`, legacy `rowing_data.dart:24-29`). Legacy: tarjeta y resumen `mm:ss` (`workout_session.dart:94-98`), filas `m:ss` (`session_detail_screen.dart:221-224`). [dotnet] · CAP-04
- **AC-D15-24 · #76 *Tracking* del HUD.** *Entonces* los rótulos del HUD con minúsculas (`Split 500m`, `Vatios`/`Watts`, `kcal`) llevan **0,04 em**; los que son todo mayúsculas (`SPM`, `BPM`) siguen en 0,08 em (arte de la 14 §3.2). Las sondas de la 4b siguen pasando y ningún rótulo se recorta en L640 a 1,3×. El HUD no suma elementos. [Editor] · CAP-07
- **AC-D15-25 · #76 y #37 Cifras tabulares (arte D15-01).** *Entonces* **no** se deriva `RowMate Numeric` de Inter: se queda `RowMate Numeric SC` (WD-01 de la 14). Las cifras de los ejes del gráfico del Detalle pasan a `RowMate Numeric SC` 600 a 11 dp (`caption`) en `text.tertiary`, como la fecha de la tarjeta, y alinean entre renglones (las 10 cifras con el mismo avance). `AxisCrowding` mide con los anchos de esa cara. La fecha de la tarjeta no cambia. Ese punto de #76 y #37 se cierra con esta razón. [Editor] · [dotnet] *(Cambiado por el gate QA-G15-02: las cifras tabulares de Inter son ≈ 30 % más anchas que las de la SC, medido en `hmtx`; la fecha crecía ≈ 24 dp y rompía el layout verificado en la 9 y la 14.)*
- **AC-D15-26 · #79 Auditoría de textos: prueba negativa y exentos.** *Entonces* (a) un `Label` con un texto fuera del catálogo (`zz-no-catalog`) da **1** violación en `TextAuditProbe`; (b) el panel del simulador y el panel de diagnóstico Bluetooth llevan la clase de exención y la auditoría corre también sobre los sets que se regeneran en esta iteración; (c) `Show BLE debug` sigue en inglés y exento como diagnóstico (Q-04, DV-147; legacy `device_screen.dart:128`). [Editor]
- **AC-D15-27 · #79 Escaneo estático y conteo.** *Entonces* el escaneo estático también encuentra los literales pasados como argumento de texto a los controles de la app (p. ej. el texto de un botón), con una prueba que lo demuestra; el comentario de cabecera de `L10nKeys` dice el número real de claves (**258**) y una prueba lo compara con el catálogo. [dotnet]
- **AC-D15-28 · #80 Marcos, desplazamiento y paneles.** *Entonces* el marco que el lector recibe de cada nodo coincide con el rectángulo del elemento en la pantalla (±1 px, en el sistema de ejes del lector, con una prueba que fija la convención) y `TryGetNodeAt` en el centro de un elemento devuelve su nodo; las listas (Rutinas, Historial, Dispositivo) informan el desplazamiento; los paneles de `Series anteriores` y `Parciales 500 m` informan si están expandidos. [Editor]
- **AC-D15-29 · #80 Foco del diálogo y costo.** *Entonces* al cerrar un diálogo el foco vuelve al elemento que lo abrió (prueba con `Terminar` → diálogo → `Cancelar`); `ScreenReaderAction` tiene el caso del diálogo; con el lector apagado el HUD asigna **0 B** por cuadro en 120 cuadros (FrameAllocations), y con el lector encendido la actualización de las métricas no reconstruye el árbol del lector. Los 60 fps con el Narrador real quedan para la demo del usuario (NO VERIFICABLE). [Editor]
- **AC-D15-30 · #81 y #82 Pruebas de la 14 que faltaban.** *Entonces* existen y pasan en PlayMode: `MinWindowLayout` (640 × 360 sin recortes), `WindowMode` (F11 y Alt+Enter ida y vuelta), `HapticEvents` (los 5 eventos de la 14, con el háptico falso), `DeviceLandscapeScroll` (un solo desplazamiento en TH), `SceneLoading` (con `-rowmate-scene-delay`, #21, arte 82-c, los **dos casos**: (a) la escena queda lista **antes** de que termine el negro de la entrada → el vuelo de 1 600 ms se ve completo; (b) queda lista **después** → `Preparando escena…` y, en el mismo cuadro en que queda lista, la escena aparece en su lugar con el fundido de 600 ms, **sin vuelo** (AC-SCN-01-07), y el indicador funde a 0 en 160 ms `ease.exit`; ningún cuadro con los dos opacos a la vez. *Aclarado por el gate QA-G15-03: "completa" valía solo para (a).*) y el caso de la escena demorada en `HudBudgetTests`. [Editor]
- **AC-D15-31 · #81 Capturas e índice de la 14.** *Entonces* el set `it14` tiene las tomas que faltaban (CAP-06, -08 a -11, -13, -14, -21, -24 a -26, VID-01/02) y CAP-22/23 se re-toman con el gráfico desplazado a la vista (el `0`, el eje X y los extremos redondos; CAP-23 ya no es igual a CAP-27); `14-pulido-plataformas/snapshots/INDEX.md` se titula "Capturas de la It. 14" y el *manifest* lista CAP-01/02 con su origen `tools/icon`. [Editor]
- **AC-D15-32 · #82 Entrada del HUD con cambio de clase.** *Cuando* cambia la clase de ventana con el HUD a la vista, *entonces* cada grupo del HUD entra con opacidad 0 → 1 y un traslado de **8 dp → 0** en **200 ms**, con retrasos de **0 / 40 / 80 ms** (los grupos del arte A14-05); sigue la regla de los 400 ms de la 14 (otro cambio dentro de 400 ms es al instante) y con "reducir movimiento" no anima. Ningún cuadro de la entrada pasa el tope del HUD. [Editor]
- **AC-D15-33 · #82 Ancla de desplazamiento.** *Dado* Historial, Detalle o Dispositivo desplazados, *cuando* cambia el ancho (redimensionar, rotar), *entonces* el primer elemento visible queda en su lugar, como en Rutinas (DD14-10). En el Historial el ancla de recarga sigue mandando: una recarga sin cambios no mueve la lista. [Editor]
- **AC-D15-34 · #82 Diagnóstico del "rectángulo" (V14-12).** *Entonces* **no se agrega estela**: AC-SCN-13-04 sigue ("el agua degradada no tiene estela, es de la 6b"). El set `it15` tiene CAP-10 (lago W1280, nivel Alto, como hoy) y la toma auxiliar **AUX-15-01** (el mismo cuadro con el *renderer* del casco apagado, fuera del INDEX de arte, arte D15-05/A15-12). El REPORT dice si el rectángulo desaparece en AUX-15-01 (es la popa del casco) o no; la popa va a issues. La estela del legacy (una V que se abre hacia atrás, con fuerza `speed/4`, `shaders/water.frag:84-94`) va con la 6b (#6). [Editor] · CAP-10, AUX-15-01 *(Cambiado por el gate QA-G15-01: el parche que se angostaba con alfa fijo no era el del legacy y rompía AC-SCN-13-04, ya verificado.)*
- **AC-D15-35 · #82 Cinta `SIMULADOR`.** *Dado* el build o el Editor con el simulador, *entonces* la cinta no tapa ninguna letra del título de página (`Rutinas`, `Dispositivo`, `Historial`, `Entrenamiento`) ni la flecha de volver del Detalle, en W1280, TV y L640. El build normal no cambia. [Editor] · CAP-09
- **AC-D15-36 · #78 Topes con la PC libre.** *Entonces* `SceneBudgetTests(High)` (p95 ≤ 1,5 ms) y `HudBudgetTests.SeriesWorstCaseWork` (≤ 1,0 ms) se corren **3 veces** con la PC sin otra carga y pasan; si alguno no pasa, la causa medida va en una línea y se corrige, sin subir el tope. Solo si la causa medida está **fuera de la app** (otro proceso visible en el Profiler o en el sistema), queda NO VERIFICABLE con el dato medido; el tope no sube (gate m-04). [Editor] *(Cierre: PASS con la ronda 1 de QA, 3/3 con la PC libre; en la ronda 2 fallaron por la VM de WSL encendida, sin cambios en la escena: vuelve a #78 como dato.)*
- **AC-D15-37 · #78 `[PERF]` de 120 s por escenario.** *Dado* el build de desarrollo con la pantalla en su resolución normal, *cuando* corre `-rowmate-perf-scene 120` en los 4 escenarios, *entonces* el p95 del cuadro es ≤ 16,7 ms y 0 compilaciones de *shaders* después de `Ready` (AC-POL-06). [Device-Win]. Si la pantalla sigue en modo de reserva (1024 × 768), NO VERIFICABLE, como en la 14
- **AC-D15-38 · #78 Matriz con los de equipo.** *Entonces* `run-tests.ps1 -Matrix` lee los bloques `DeviceOnlyIds` de los `It*Ids.cs` y los informa aparte (no como "sin test"), con una prueba de herramientas. [dotnet]
- **AC-D15-39 · #78 Intermitente de la It. 11.** *Dado* el arreglo del coordinador ya en el árbol, *entonces* `BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame` pasa **20 de 20** corridas de su clase y en **3 corridas completas** de PlayMode `-Simulator`; la causa (el reloj virtual del `Settle` llegaba al reintento de los 3 s) queda escrita en `02-tech-design`. [Editor] *(Cierre del PO, QA-V15-R2-01: la causa del #78 quedó corregida (la guarda nueva pasó en todas las corridas), pero apareció **otra**: 1 asignación del reloj de prueba en 1 cuadro, en 1 de 4 corridas completas. **FAIL aceptado**; vuelve a #78 sin relajar la afirmación.)*

### 3.4 Transversal

- **AC-D15-40 · Paridad, catálogo y repo.** *Entonces*: ningún test de las It. 1 a 14 se rompe ni baja su cuenta (los que cambian por esta spec, p. ej. el golden del TCX o los textos de §3.5, se actualizan con su AC); el catálogo tiene **258** claves por idioma con las de §3.5; ninguna prueba ni captura toca `prefs.json`, las bases ni Documentos reales; `ProjectSettings` sin cambios (sin el define del simulador; `HorizonFar.mat`/`HorizonNear.mat` sin cambios); lo versionado sin rutas de la PC ni datos del equipo, en UTF-8 sin doble codificación. [dotnet] · [Editor]

### 3.5 Textos (F-APP-10: 256 → 258 por idioma)

| Clave | es | en | Estado |
|---|---|---|---|
| `exportTcxFor` | `Exportar TCX: {session}` | `Export TCX: {session}` | Nueva (AC-D15-09) |
| `exportTcxOpenFolderFailed` | `No se pudo abrir la carpeta` | `Couldn't open the folder` | Nueva (AC-D15-13) |
| `hudWatts` | `Watts` → `Vatios` | `Watts` (igual) | Cambia (DV-140) |
| `hudSplit` | `Split /500m` → `Split 500m` | `Split /500m` → `Split 500m` | Cambia (DV-140) |
| `historyStatPower` | `Watts` → `Vatios` | `Watts` (igual) | Cambia (DV-140) |

Siguen igual: `historyStatSplit` (`Split`, la abreviatura de columna), `metric*` (Dispositivo), `sessionChart*` (selector). Se reusan `exportTcx`, `exportTcxExporting`, `retry`.

## 4. Issues → criterios

| Issue | AC | Entra | Qué queda (o por qué no entra) |
|---|---|---|---|
| #32 | D15-01 | Entero | — (lo demás se cerró en la 14) |
| #33 | D15-02 | En parte | Cursor con todas las métricas (choca con DV-94, que aceptó el usuario), zoom y desplazamiento con gestos, tramos dentro/fuera del objetivo, marca de "sin remo", tarjeta → Detalle como elemento compartido |
| #34 | D15-03, -04, -08 | En parte (cierre) | D15-03 y -08 pasan. AC-D15-04 FAIL con la causa medida: páginas de UIR en la entrada y cruce de colores del selector (QA-V15-R2-02) |
| #35 | D15-05 | Entero | — |
| #37 | D15-06, -25 | Entero | El sello de Strava se descarta con Strava. `RowMate Numeric` de Inter se cierra sin hacer: se queda la SC (QA-G15-02) |
| #38 | D15-07 | Entero | — |
| #39 | D15-08 | Entero | — |
| #67 | D15-09, -10 | En parte | Sello de "exportada" (pide escribir en la base; la 12 la dejó de solo lectura) y exportar varias sesiones |
| #66 | D15-11, -12 | Entero | — |
| #70 | D15-13 | En parte | La prueba con el Explorador real, en la demo del usuario |
| #71 | D15-16, -17 | Entero | — |
| #72 | D15-18 | Entero | — |
| #73 | D15-14, -15 | En parte | FIT: queda evaluado y fuera (codificador binario nuevo y validación con Strava = demo del usuario) |
| #74 | D15-19, -20, -21 | En parte | Animación de marca (DV-134 aceptada: logo fijo), *launcher* nativo (DV-V14-03) y recordar la pantalla completa (DV-129) |
| #75 | D15-22, -23 | Entero | Decidido por el PO (DV-140, DV-141) |
| #76 | D15-24, -25 | Entero | — (`RowMate Numeric` de Inter: cerrado sin hacer, se queda la SC, QA-G15-02) |
| #77 | — | Fuera | El usuario aceptó el vidrio (QA-V14-11); no se cambia |
| #78 | D15-36 a -39 | En parte (cierre) | D15-36 a -38 pasan. AC-D15-39 FAIL por otra causa (1 asignación del reloj de prueba, QA-V15-R2-01); los topes fallan con la VM de WSL encendida |
| #79 | D15-26, -27 | Entero | — |
| #80 | D15-28, -29 | En parte | 60 fps con el Narrador real (demo del usuario) |
| #81 | D15-30, -31, -18 | En parte | Regenerar los sets de la 2, 3, 10a, 11 y 12 (`it12`; se hace cuando se vuelvan a tomar; gate m-07) |
| #82 | D15-30, -32 a -35 | En parte | La estela no se hace: la del legacy (V, `speed/4`) va con la 6b (#6) y la popa del casco, a issues con AUX-15-01 (QA-G15-01) |

## 5. Capturas (set `it15`, en `15-deuda-pc/snapshots`)

`capture-snapshots.ps1 -Set it15` escribe **solo** en esta carpeta (y `-Set it9` / `-Set it14` en las suyas). Datos sembrados (`F-9`, `F-9P`) con `ROWMATE_DATA_DIR` temporal; `TextAuditProbe` corre en cada toma.

| Toma | Tamaño | Idioma | Muestra |
|---|---|---|---|
| CAP-01 | TV | es | Historial con el botón de exportar en las tarjetas exportables, `Vatios` y `2:13` |
| CAP-02 | W1280 | en | Historial con el aviso `Saved to …` sobre la barra de pestañas |
| CAP-03 | TV | es | Detalle de `Escalera`: fila 2 marcada y sus franjas al 22 % (Vatios) |
| CAP-04 | W1280 | es | Detalle en `Split 500m`: escalones sin picos, resumen `m:ss`, cursor con la sombra del *tooltip* |
| CAP-05 | W1280 | es | Barra del Detalle con el glifo `download` de la fuente |
| CAP-06 | W1280 | es | Aviso `No se pudo abrir la carpeta` |
| CAP-07 | L640 1,3× | es | HUD: `Vatios`, `Split 500m`, `kcal` con 0,04 em |
| CAP-08 | TV | en | HUD: `Watts`, `Split 500m` |
| CAP-09 | W1280 | es | Rutinas con el simulador: la cinta no tapa el título |
| CAP-10 | W1280 | es | Lago, nivel Alto, como hoy (referencia del diagnóstico de AC-D15-34) |
| AUX-15-01 | W1280 | es | El mismo cuadro con el casco oculto (diagnóstico, fuera del INDEX de arte) |

## 6. Decisiones (rigen por defecto; van al gate)

| Decisión | Por defecto | Si se rechaza |
|---|---|---|
| DV-140 / #75: un nombre por métrica (`Vatios`/`Watts`, `Split 500m`); `Split` solo en columnas angostas | Se acepta | Nombres del legacy por pantalla |
| DV-141 / #75: split `m:ss` en Historial y Detalle; `mm:ss` en Dispositivo y HUD | Se acepta | `mm:ss` en tarjeta y resumen, como el legacy |
| DV-142 / #32: curva sin sobrepaso entre puntos | Se acepta | La cúbica de `fl_chart` con su pico |
| DV-143 / #67: botón de exportar también en la tarjeta (además del Detalle) | Se acepta | Solo en el Detalle (DV-124) |
| DV-144 / #73: vuelta con distancia negativa → `0`; `<Creator>` RowMate | Se acepta | El negativo del legacy; sin `Creator` |
| DV-145 / #74: la ventana recuerda lugar, tamaño y maximizado (no la pantalla completa) | Se acepta | Siempre centrada en 1280 × 720 |
| DV-146 / #33: tocar una fila resalta sus franjas al 22 % | Se acepta | Sin vínculo fila ↔ franja |
| DV-147 / #79: `Show BLE debug` sigue en inglés y exento (Q-04) | Se acepta | Pasa al catálogo |
| DV-148 / #73: no se hace FIT | Se acepta | FIT en otra iteración |

## 7. Fuera de alcance

Todo lo que pide Android, Apple, el remo, el pulsómetro o un teléfono; #77 (vidrio, decisión del usuario); la animación de marca, el *launcher* nativo y la pantalla completa recordada (#74); el resto de #33, #67 y #73 (§4); los sets viejos de la 2, 3, 10a y 11; la 6b (#6); la corrida real del CI (#10); nada de `ProjectSettings`, push, workflows ni issues en GitHub.

## 8. Para issues

- Re-tomar los sets del HUD de la 4a, 4b y 5 cuando se toquen: muestran `Watts`/`Split /500m` y el *tracking* viejo (DV-140, AC-D15-24).
- Subir a Strava un TCX con `<Creator>` en la demo del usuario y confirmar que lo acepta (AC-D15-15).
- Exportar desde la tarjeta con pulsación larga o menú contextual, si el ícono resulta poco visible (DV-143).
- La popa del casco que se lee como una tabla blanca (arte D15-05 y §5), con la evidencia de AUX-15-01 (QA-G15-01).
- La estela del legacy (V que se abre hacia atrás, fuerza `speed/4`) con la 6b (#6), enmendando AC-SCN-13-04 ahí (QA-G15-01).
- El editor dice `Incremento Watts (+/rep)` en es (`editorProgressionWatts`): ¿`Incremento Vatios`? (fuera de AC-D15-22, QA-G15-04).

## 9. Cierre del PO (2026-11-10)

- **Aceptada** con 2 FAIL documentados (QA ronda 2: 38 PASS, 2 FAIL, 0 NO VERIFICABLE). Los dos tienen la causa medida y una prueba que no deja retroceder; no bloquean nada que el usuario use.
- AC-D15-04 → #34 (sigue abierto). AC-D15-39 → #78 (sigue abierto), junto con los topes que fallan con la VM de WSL encendida (m-09).
- #82 se cierra: lo que queda ya tiene dueño (la estela con la 6b, #6; la popa del casco, issue nuevo con AUX-15-01).
- #33 y #67 siguen abiertos con su resto (§4); no se abren issues nuevos para eso.
- Los menores de QA, del gate y del arte van a issues de GitHub agrupados, sin corregirlos antes del commit.
