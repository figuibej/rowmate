# It. 4a — Deuda de diseño de la It. 3 y base visual del HUD

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-28 |
| Iteración | 4a · `04a-motor-hud-funcional` ([ROADMAP §It. 4a](../../ROADMAP.md)), al empezar, en paralelo con la spec del PO y la deuda del Architect |
| Entrada | [REPORT de la It. 3 §7-§9](../03-rutinas/REPORT.md); [05-qa-verification de la It. 3](../03-rutinas/05-qa-verification.md) (QA-V3-02, QA-V3-09); [04-design-notes de la It. 3](../03-rutinas/04-design-notes.md) (§4.13, §4.16, §5, §12, §13 y §16); [STATUS](../../STATUS.md); capturas de las It. 2 y 3 (`02-dispositivo-simulador/snapshots/`, `03-rutinas/snapshots/`); código: `SimulatorPresentation/SimulatorLayerView.cs`, `Simulator/Panel/SimulatorPanelLayout.cs`, `Core/Design/BottomObstacles.cs`, `Presentation/Shell/ShellLayout.cs`, `Presentation/Controls/TabularNumber.cs`, `Presentation/Motion/*` (también el trabajo en curso del Architect sobre QA-V3-02, leído sin tocarlo), `Presentation/Styles/Typography.uss`, `UI/Fonts/**` y `UI/Fonts/Generated/*.asset` |
| Entrega | Este documento, con los pedidos **DD4a-01** a **DD4a-07**; ART_BIBLE y ANIMATION_BIBLE alineadas (secciones "Cambios al iniciar la It. 4a"). Sin tokens, código ni assets: ningún valor de `Tokens.cs` cambia |
| Legacy leído (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/core/dev/simulator_overlay.dart:1-140`, `lib/features/workout/immersive_workout_screen.dart:44-140` y `:586-760`, `lib/features/workout/workout_screen.dart:1-200`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío antes y después |
| Verificado | Kerning y cifras: programas propios con fontTools 4.68.0 (`uvx --offline`) sobre las fuentes y los `FontAsset` versionados, en `<scratch>/it4a/designer-debt/kerning/` (§3.1). Llave y HUD: la regla de §1.3 y el alto de la columna del HUD en `<scratch>/it4a/designer-debt/` (`key_rule.py`, `hud_column.py`), contrastados con las capturas. Curvas de §2: las de `Tokens.cs`, con el método de `Easing.cs` (`choreo.py`). **Unity no se abrió** y dotnet no se corrió: no cambió ningún token ni código |

Convenciones: todo en dp. Perfiles: **TV** 411 × 914, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720 y, para el entrenamiento, **P360** 360 × 800 (el teléfono más angosto en que se rema). Rectángulos en coordenadas de ventana: x desde la izquierda, y desde arriba; "abajo" o "desde abajo" es la distancia al borde inferior. Barra de pestañas de 64 y margen seguro 0, salvo que se diga otra cosa. `s` = `hudScale` (ART_BIBLE §5.3). La llave mide 40 × 40.

---

## 0. Resumen

1. **QA-V3-09, la causa.** No depende del tamaño de la ventana. La llave (`right 16`) cae sobre la columna donde Material pone las acciones de cada fila (`⋮`, lápiz, cruz, tacho, *chevron*) y donde van `+ Paso`, `Serie` y el `+`. En una lista que se desplaza no hay altura que la salve: las cruces se repiten cada 62 dp, con 14 dp entre cajas. Además de los tres casos de QA, hoy tapa en reposo el `⋮` de la segunda rutina en L640 y el *chevron* del pulsómetro encontrado en L640 (CAP-09 de la It. 2).
2. **QA-V3-09, la regla nueva (D-ART-10, la decide el PO).**
   - Fuera del entrenamiento, la llave va **abajo a la izquierda**: a 16 dp del borde y 16 dp sobre la barra de pestañas o, en una pantalla apilada, sobre el borde. Ahí las filas ponen lo que las identifica, no lo que se toca.
   - En el entrenamiento va **abajo a la derecha**: sobre los controles si se cruzan con ella, y debajo de kcal y BPM.
   - En ninguna posición de desplazamiento tapa una acción que se repite, ni tapa nada fijo. El panel se abre desde la llave.
   - Cambian AC-BLE-16-02 y AC-BLE-16-03, y la regla reemplaza a DV-13 (§1, §5).
3. **QA-V3-02, visto bueno con la coreografía exacta (§2).**
   - Al borrar lo último, la lista queda a la vista hasta que lo borrado termina de colapsar (280 ms) y recién ahí entra el vacío (240 ms, y +8).
   - Con lo primero que entra, el vacío funde en su lugar en 120 ms mientras la fila crece.
   - Hay valores por cuadro para VID-01. Lo que está implementando el Architect coincide, salvo dos ajustes (DD4a-05).
4. **Kerning de títulos (D-ART-02).** La tabla versionada de **Inter Bold**, la de `display`, `headline` y todo texto 700, tiene solo 11 "primeros". Le faltan pares que se ven:
   - `Te` (−0,078 em: −2,50 dp a 32 en `Terminar entrenamiento`);
   - `Tr`, `Wo`, `Wa`, `TA`, `VA` y `Yo`.

   SemiBold, Medium y Regular están casi completas, y los rótulos y botones del HUD no pierden nada visible.

   **Regla nueva**: la tabla de cada cara que dibuja texto tiene **todos** los pares GPOS entre los caracteres de un juego fijo (catálogo es/en, ASCII y español). Son unos 1 300 a 1 700 pares por cara: **≈ 3,4 MB** en total, contra los ≈ 20,5 MB de tablas parciales de hoy. El método reproduce sin una diferencia los 49 049 pares que ya escribió TextCore.
5. **Cifras del HUD.** `TabularNumber` dibuja cada carácter en su celda, así que entre cifras nunca hay kerning. Es correcto: Barlow no define pares para sus cifras tabulares.
   - El problema es otro: centra las cifras **proporcionales** en celdas del avance máximo, y el `1` queda con 0,12 em de aire de más por lado (7,7 dp a 64 dp).
   - Barlow trae cifras tabulares **dibujadas** (`tnum`). Con ellas, entre dos `1` hay 6,6 dp menos de aire a 64 dp: 2,4′ a 1,5 m, más que la agudeza visual. Además, los números quedan entre un 3,7 y un 5,5 % más angostos.
   - Pedido: una derivada "RowMate Numeric SC", con la receta ya verificada (DD4a-07).
6. **Base del HUD.** Con D-ART-02 y `hudScale`, la columna derecha no entra en **ninguna ventana horizontal**: necesita 535·s dp, y en W1280 son 856 sobre 720. La 4a tiene que resolver el horizontal (§4.1).
7. **Biblias.** Quedaron alineadas con la It. 3 y con este documento. Faltaban el menú y la ayuda emergentes, Noto Emoji, el kerning y el "reducir movimiento" de las hojas (§6).

---

## 1. QA-V3-09: dónde va la llave (pedido al Architect; decide el PO)

### 1.1 Qué pasa hoy

Regla de la It. 3 (DV-13, `SimulatorPanelLayout`):

- carril derecho (`right 16`) y `bottom` = barra + 16;
- en Rutinas, barra + 88 (16 dp sobre el `+`);
- con el editor encima, la barra conservada + 16.

| Caso | Dónde | Qué tapa en reposo | Fuente |
|---|---|---|---|
| Editor, `Escalera SPM` | W1280 (CAP-11, CAP-26, DEMO-06, DEMO-08) | La cruz de la 4.ª fila: la llave está en x 1224-1264, y 600-640; la caja de la cruz, en x 1216-1264, y 586-634 | QA |
| Editor, `Casos borde` | TV (CAP-12a) | La cruz de la fila 8 (dentro de la serie ×1) | QA |
| Editor | L640 (CAP-22) | `Serie`: la llave en y 240-280, la fila de pasos en y 225-281 | QA |
| Rutinas con las 4 rutinas de prueba, sin desplazar | L640 (no hay captura: CAP-06 es el final del desplazamiento) | El `⋮` de `Pirámide 500`, entero: la llave en x 584-624, y 168-208; la caja del `⋮`, en x 572-620, y 162-210 | Nuevo, calculado |
| Rutinas con 8 rutinas o más, sin desplazar | TV | El `⋮` de la octava: su caja en y 737-785 y la llave en y 722-762 | Nuevo, calculado |
| Dispositivo, buscando, con el pulsómetro simulado encontrado | L640 (CAP-09 de la It. 2) | El *chevron* de la fila del pulsómetro | Nuevo, en la captura |

**Causa.** La llave ocupa de 16 a 56 dp desde el borde derecho, y ahí Material pone las acciones de cada fila:

| Acción | Centro (desde el borde derecho) | Columna que ocupa |
|---|---|---|
| `⋮` | 44 dp | 20-68 |
| Cruz | 40 dp | 16-64 |
| Lápiz | 88 dp | 64-112 |
| Tacho | 40 dp | 16-64 |

También van a la derecha `+ Paso`, `Serie` y el `+`. Al principio de cada fila (a la izquierda) va lo que la identifica, no lo que se toca: el ícono de la tarjeta, la insignia del paso, el corazón del pulsómetro.

### 1.2 Criterio: qué es "tapar una acción en reposo"

- **Acción** es el área táctil de un control:
  - botones: de texto, tonales, rellenos, de contorno y de peligro;
  - botones de ícono: lápiz, cruz, tacho, `⋮`, flecha y ±;
  - chips y cápsulas (`×N`), el `+`, pestañas, opciones de un segmentado y mosaicos, interruptores e ítems de menú;
  - campos de texto y los controles del entrenamiento (Terminar, Pausa y Reanudar).

  Las **superficies grandes** que se tocan enteras (la tarjeta de rutina o de sesión, la fila de un dispositivo encontrado) cuentan como acción solo en sus controles propios (`⋮`) y en su *chevron*. La llave puede quedar sobre el resto (el ícono del principio, el fondo), como pasa con el `+`.
- **Tapar** es que la caja de la llave, agrandada 8 dp, se cruce con el área táctil de la acción. Con un campo de texto o una superficie grande alcanza con que no se superpongan, sin aire: en el diálogo compacto, un campo empieza a 56 dp del borde, justo donde termina la llave.
- **En reposo** es con la pantalla quieta (sin desplazamiento, transición ni resorte en curso), en cualquier posición de desplazamiento.
  - Lo fijo y las acciones que se repiten en cada fila: en todas las posiciones.
  - Una acción suelta dentro de un contenido que se desplaza (los botones de la tarjeta del pulsómetro en Dispositivo): en la posición inicial y al final del desplazamiento. Entre las dos, se destapa con un desplazamiento corto (como mucho, el alto de la acción + 56 dp), como con el `+`.
- **No cuenta**:
  - lo que no se toca: insignias, títulos, `Pasos (N)`, el ícono de una tarjeta, las tarjetas del tablero;
  - el panel abierto, que lo abrió el usuario;
  - los avisos y las ayudas, que no reciben toques.

### 1.3 La regla (D-ART-10; la llave se ubica por código)

| # | Regla | Números |
|---|---|---|
| R1 · Carril | Fuera del entrenamiento (pestañas, pantallas apiladas, diálogos, menús), **carril izquierdo**. En el entrenamiento a pantalla completa de la 4a, **carril derecho** | Izquierdo: `left = 16 + margen seguro izquierdo`. Derecho: `right = 16 + margen seguro derecho`. La llave sigue de 40 × 40 y no escala con `hudScale` |
| R2 · Altura base | 16 dp sobre lo que hay abajo | Pestaña: barra visible + 16 (80 con la barra de 64; el margen seguro ya está en la barra). Pantalla apilada que tapa la barra (el editor; después, el detalle y Perfil): 16 + margen seguro inferior. Entrenamiento: 16·s + margen seguro inferior, la misma línea de abajo que los controles |
| R3 · Obstáculos fijos | Sube hasta 8 dp por encima de cada obstáculo fijo que se cruce con su caja agrandada 8 dp, y repite hasta no cruzar ninguno (`BottomObstacles.Lift` con aire 8, que es el del aviso) | En la app no hay ninguno en el carril izquierdo. El `+` deja de ser obstáculo de la llave, pero lo sigue siendo del aviso y del relleno. En el entrenamiento, los controles |
| R4 · Techo (entrenamiento) | La llave queda al menos 8 dp debajo de la tarjeta más baja de la columna derecha (kcal, o BPM si se ve), y el HUD de la 4a deja ese hueco (§4.2) | Con las zonas del legacy (`bottom 140·s`), el margen más chico es de 6,8 dp, a 360 × 800 (§1.5) |
| R5 · Contenido que se desplaza | Su relleno de abajo suma el tope de la llave + 16, como hoy (`RegisterClearance`) | Rutinas en TV: 152, igual que sin simulador (antes, 208). Editor: 72 (antes, 136) |
| R6 · Menús emergentes | El menú `⋮` y el menú de texto esquivan la llave como a un borde. Si se cruzarían con ella agrandada 8 dp, se abren del otro lado del ancla, y si tampoco entran, se corren en x | — |
| R7 · Diálogos | Sin cambios: con un diálogo, la llave no se mueve y sigue encima del velo (AC-APP-03-07). Con el ancho `min(máx; ventana − 80)` y el relleno de 24 (16 en el compacto), el contenido y los botones empiezan a 56 dp o más de cada borde: los botones nunca entran en el carril de la llave | — |
| R8 · Teclado del sistema | La llave no sube con el teclado: queda debajo. Si subiera, taparía el campo con foco, que va 16 dp sobre el teclado | — |
| R9 · Panel | Se abre donde está la llave y crece desde ella (§1.7) | — |

### 1.4 Resultado en la app (carril izquierdo)

| Dónde está la llave (x 16-56) | TV | TH | L640 | W1280 |
|---|---|---|---|---|
| En una pestaña (Dispositivo, Workout, Rutinas, Historial) | y 794-834 | y 291-331 | y 240-280 | y 600-640 |
| En una pantalla apilada (el editor) | y 858-898 | y 355-395 | y 304-344 | y 664-704 |

Qué queda debajo en reposo, con los datos de prueba de las It. 2 y 3:

| Caso | Hoy | Con la regla |
|---|---|---|
| Editor W1280, `Escalera SPM` (DEMO-06) | La cruz de la 4.ª fila | Nada: queda debajo de la última fila |
| Editor TV, `Casos borde` (CAP-12a) | La cruz de la fila 8 | El fondo del pie de la serie, a la izquierda de su botón centrado (x 122-288) |
| Editor L640 (CAP-22) | `Serie` | La insignia de `Calentamiento`, que no es una acción |
| Rutinas L640, las 4 rutinas, sin desplazar | El `⋮` de `Pirámide 500` | El fondo de esa tarjeta, abajo a la izquierda (su texto empieza en x 84) |
| Rutinas TV, 8 rutinas o más | El `⋮` de la octava | El principio de una tarjeta (fondo o ícono), nunca su texto ni su `⋮` |
| Rutinas, cualquier perfil | La llave apilada sobre el `+` (DV-13) | El `+` solo en su rincón y la llave del otro lado. El aviso de borrado vuelve a 144-192 en TV, como sin simulador |
| Dispositivo L640 buscando (CAP-09 de la It. 2) | El *chevron* del pulsómetro | El corazón con que empieza la fila |
| Dispositivo TV y L640 conectado (CAP-01, CAP-03 de la It. 2) | Nada (TV); la tarjeta de SPM (L640) | Nada (TV); la tarjeta de SPLIT (L640), que no se toca |
| Workout de la 4a (con el orden del legacy, `workout_screen.dart:52-129`) | — | El principio de una tarjeta, o nada. `Entrenamiento libre` queda más arriba en los 4 perfiles (en L640, en y 72-128 conectado y 132-188 sin remo) |
| Editor con un aviso (CAP-18, -19 y -28) | El aviso abajo (16-64) y la llave sobre su extremo derecho | El aviso 8 dp sobre la llave: 64-112 desde abajo |

### 1.5 Entrenamiento (HUD de la 4a, carril derecho)

**Por qué la derecha.**

- En horizontal, el rincón de abajo a la derecha está libre, porque los controles van centrados. El de la izquierda está ocupado: abajo, la banda tiempo │ distancia; arriba, la columna de SPM con los paneles de series de la It. 5.
- En vertical queda un hueco entre los controles y kcal y BPM.
- Es el lado del legacy: `right 16`, `bottom 96`.

La regla R3 con los controles de ART_BIBLE §3.5 (Terminar 128 × 56, 16 de separación, Pausa 148 × 56) y las zonas del legacy × `s` (ART_BIBLE §5.2):

| Perfil | s | Controles: x · desde abajo | Llave | Techo (kcal/BPM, desde abajo) · margen |
|---|---|---|---|---|
| TV | 1,054 | 51,6-359,4 · 16,9-75,9 | x 355-395, y 790,1-830,1 (se cruza con Pausa y sube a 83,9 desde abajo) | 147,5 · 15,7 |
| P360 | 0,923 | 45,2-314,8 · 14,8-66,5 | x 304-344, y 685,5-725,5 (sube a 74,5) | 129,2 · **6,8** |
| TH | 1,054 | 303,1-610,9 · 16,9-75,9 | x 858-898, y 354,1-394,1 (en el rincón, a la altura de los controles) | 147,5 · 82,7 |
| L640 | 0,923 | 185,2-454,8 · 14,8-66,5 | x 584-624, y 305,2-345,2 | 129,2 · 66,5 |
| W1280 | 1,6 | 406,4-873,6 · 25,6-115,2 | x 1224-1264, y 654,4-694,4 | 224 · 150,4 |

- Con `s` = 0,9, el piso de `hudScale` (un teléfono de 320 dp), también entra: el margen es de 5,2 dp.
- La llave nunca llega a la zona protegida del bote (ART_BIBLE §5.2), que termina en 0,72 del ancho: el carril derecho empieza en el ancho − 56.
- El HUD registra como obstáculos sus controles y lo que la 4a ponga abajo a la derecha. El techo lo da la tarjeta más baja de la columna derecha.

### 1.6 Movimiento (ANIMATION_BIBLE §2.14)

| Cambio | Cómo | Con reducir movimiento |
|---|---|---|
| Entre pestañas | No se mueve | — |
| Pestaña ↔ pantalla apilada (la altura de R2) | `CriticalSpring` ω 22 en y, desde su posición y su velocidad actuales. Baja cuando termina la entrada de la página (240 ms, `CoversShellChanged(true)`) y sube cuando empieza su salida, como en la It. 3 | En el acto |
| Entrar o salir del entrenamiento (cambio de carril) | No viaja de un lado al otro. En el cuadro 0 de la transición sale (opacidad 1 → 0 en 90, `ease.exit`) y, cuando la transición termina, entra en el otro carril (opacidad 0 → 1 y escala 0,85 → 1 en 160, `ease.enter`, como la vuelta de la llave de la It. 2). Mientras sale no recibe toques | Solo opacidad: 90 y 120 |
| Dentro del entrenamiento (cambian los obstáculos: aparece BPM, otro layout) | Va a su lugar nuevo con ω 22. Al rotar, en el acto, junto con el fundido cruzado del HUD (ANIMATION_BIBLE §2.12) | En el acto |

Se puede tocar mientras se mueve, como hoy.

### 1.7 Panel (consecuencia de R9; lo confirma el PO en AC-BLE-16-03)

- **Dónde.** Se abre donde está la llave y crece desde su esquina: en la app, abajo a la izquierda (`left 16`, a la altura de la llave); en el entrenamiento, abajo a la derecha.
  - La transición de la It. 2 no cambia (opacidad en 160, escala 0,92 → 1 en 240); solo cambia el origen.
  - En Rutinas deja el `+` libre desde 368 dp de ancho: el panel termina en x 296 y el `+` empieza en el ancho − 72. En un teléfono de 360 dp le tapa los 8 dp de la izquierda.
- **Alto máximo.** Se mide desde el borde de abajo del panel: alto de la ventana − borde de abajo − margen seguro superior − 16, con mínimo 120.
  - En las pestañas da la fórmula de siempre (360 → 248; 914 → 802).
  - En el editor, 64 dp más.
- **En el entrenamiento** (propuesta), el panel además termina 8 dp debajo de la tarjeta de Vatios, para que se vean Split y Vatios mientras se los corrige (demo b de la 4a).
  - En W1280 quedan unos 265 dp: se ven la cabecera, los perfiles, Vatios, Ritmo y Hora, y el resto se desplaza adentro.
  - En TV no cambia nada: hay unos 544 dp y el panel mide unos 409.
- **La cinta.** En una ventana baja, el panel llega a la esquina de la cinta `SIMULADOR`. La cinta tapa solo su esquina redondeada: el título empieza 12 dp adentro y queda fuera de la franja.

### 1.8 Pedidos al Architect (DD4a-01 a DD4a-04)

| ID | Qué | Cómo se verifica |
|---|---|---|
| DD4a-01 | **Regla pura en Core**, en `SimulatorPanelLayout` o en una clase nueva, con R1 a R5.<br>Entra: el tamaño de la ventana, los márgenes seguros, el contexto (pestaña con el alto de su barra, pantalla apilada, o entrenamiento con su `s`) y los obstáculos fijos con el techo.<br>Sale: el carril, la x y el `bottom`.<br>`SimulatorLayerView` la usa, y la pantalla de arriba de la pila declara su contexto (el entrenamiento, también sus obstáculos y su techo). El `+` deja de ser obstáculo de la llave. Con el movimiento de §1.6 | dotnet: los rectángulos de §1.4 y §1.5 (± 0,1); con margen seguro (24 / 48, como D-26); nunca cruza un obstáculo; techo. Reemplaza `FabBottomAvoidsTheFab` |
| DD4a-02 | Los menús emergentes esquivan la llave (R6) | PlayMode: el menú de texto abierto en la esquina de abajo a la izquierda, en L640 |
| DD4a-03 | El panel, anclado en la llave: lado, origen de la transición y alto máximo medido desde su borde de abajo. En el entrenamiento, el tope de Vatios si el PO lo acepta | dotnet: el alto máximo. PlayMode: se abre a la izquierda en una pestaña y en el editor, y a la derecha en el entrenamiento |
| DD4a-04 | **Auditoría en el arnés**: en toda toma con simulador, ningún control (§1.2) se cruza con `#sim-open` agrandado 8 dp; los campos y las superficies grandes, que no se superpongan. Si se cruza, la toma falla: no es una sonda de aspecto.<br>Controles: `.rm-button`, `.rm-icon-button`, `.rm-chip__pill`, `.rm-fab`, las pestañas, `.rm-segmented__option`, `.rm-switch`, `.rm-menu__item`, `.rm-field__box`, los *chevron* de tarjetas y filas, y los controles del entrenamiento | Todas las tomas con simulador de las It. 2, 3 y 4a. Con la regla de hoy, fallan CAP-11, CAP-12a, CAP-22, CAP-26, DEMO-06 y DEMO-08 de la It. 3, y CAP-09 de la It. 2 |

Pruebas y tomas que cambian:

- `SimulatorPanelLayoutTests`.
- La sonda de CAP-01 de la It. 3: `#sim-open.yMax + 16 = #routines-add.yMin` pasa a ser "`#sim-open.xMin` = 16 y su borde de abajo a barra + 16".
- CAP-06: la última tarjeta queda por encima del `+`.
- CAP-18, CAP-19 y CAP-28: el aviso queda 8 dp sobre la llave.
- Las comprobaciones de AC-RUT-10-04 y AC-RUT-10-09.

### 1.9 Alternativas descartadas

| Alternativa | Por qué no |
|---|---|
| Quedarse a la derecha y buscar un hueco a otra altura | En el editor no hay hueco fijo (cruces cada 62 dp, con 14 entre cajas): la llave bailaría con el desplazamiento |
| Llevarla a la barra superior | Le saca 52 dp al título (con letra 1,3× en 360 dp no entra), cambia la barra de cada pantalla del build con simulador y obliga a abrir el panel arriba, sobre Split y Vatios |
| Reservar una franja abajo, para que el contenido termine antes de la llave | Son 72 dp menos de editor en el build con simulador, que es el de las demos: dejaría de verse como el normal |
| Una llave que se arrastra | No garantiza nada por sí sola y agrega estado |
| Un tirador angosto en el borde | Mide menos de 48 dp y, en Android, cae en la zona del gesto de "Atrás" |
| Un lado por pantalla (derecha en Dispositivo, izquierda en Rutinas) | La llave cambiaría de lado entre pestañas. Con la izquierda en toda la app no hace falta: Dispositivo también mejora (CAP-09) |

---

## 2. QA-V3-02: lo último que se va (visto bueno, con la coreografía exacta)

La regla general es la de la It. 3: lo nuevo crece y lo borrado se desvanece y colapsa. Esta sección fija el caso que faltaba, cuando lo que se va es lo **último** de la lista y entra el vacío, para el editor y para Rutinas.

### 2.1 Borrar el último paso o bloque (cruz o tacho) o la última rutina

| t (ms) | Qué pasa |
|---|---|
| 0 | El dato cambia en el acto: `Pasos (0)`, o Rutinas sin tarjetas. Lo que se va deja de recibir toques (`ExitLock`). La lista (`#editor-steps` o `#routines-list`) **sigue a la vista** |
| 0-120 | Lo que se va funde de 1 a 0 (`ease.exit`). Su alto no cambia hasta los 80 |
| 80-280 | Su slot, con los márgenes adentro, baja a 0 (`ease.move`). El contenido va en posición absoluta arriba y el slot lo recorta, así que no se aplasta. Las hojas de una serie están dentro del slot, en los 8 dp del margen de abajo: se recortan recién hacia los 121 ms, cuando ya no se ven |
| 280 | Sale del árbol y termina el último colapso en curso (`Collapsed`). En el mismo cuadro, la lista pasa a `display: none` y el vacío entra con opacidad 0 e y +8. No se mueve nada más: la lista vacía no ocupa alto y el relleno inferior está en el contenido, no en la lista |
| 280-520 | El vacío pasa de opacidad 0 a 1 y de y +8 a 0 (`ease.enter`, 240, `shift.s`), en su lugar de siempre (04-design-notes de la It. 3 §4.5, y §4.1 para Rutinas) |

Valores esperados, con las curvas de `Tokens.cs`, para PlayMode con el reloj inyectado y para VID-01 (30 cuadros por segundo). Tolerancia: ± 0,01 de opacidad y ± 0,5 dp.

| t (ms) | 0 | 66,7 | 100 | 133,3 | 200 | 266,7 | 300 | 400 | 466,7 | 520 |
|---|---|---|---|---|---|---|---|---|---|---|
| Opacidad de lo que se va | 1 | 0,826 | 0,422 | 0 | 0 | 0 | — | — | — | — |
| Alto que le queda | 100 % | 100 % | 99,1 % | 91,7 % | 25,2 % | 0,4 % | — | — | — | — |
| Opacidad del vacío | — | — | — | — | — | — | 0,231 | 0,872 | 0,988 | 1 |
| y del vacío | — | — | — | — | — | — | +6,15 | +1,02 | +0,09 | 0 |

### 2.2 Lo primero que entra (`Confirmar` en una lista vacía, o `Serie`)

| t (ms) | Qué pasa |
|---|---|
| 0 | El dato cambia. La lista vuelve a `display: flex`, con lo nuevo en alto 0. El vacío sale del flujo en su misma caja (posición absoluta): no se mueve |
| 0-120 | El vacío funde de 1 a 0 (`ease.exit`). A los 120 pasa a `display: none` |
| 0-≈490 | Lo nuevo crece con el resorte de ω 12 y funde en 60-260 (`ease.enter`). Una serie lo hace en escalera (04-design-notes de la It. 3 §5.6) |

| t (ms) | 60 | 100 | 120 | 200 | 490 |
|---|---|---|---|---|---|
| Opacidad del vacío | 0,872 | 0,422 | 0 | — | — |
| Alto de la fila nueva | 16,3 % | 33,7 % | 42,2 % | 69,2 % | 98,1 % |
| Opacidad de la fila nueva | 0 | 0,488 | 0,655 | 0,972 | 1 |

En Rutinas, la primera rutina llega con la lista tapada por el editor. El vacío se va en el acto, sin que se vea, y la tarjeta crece cuando termina el *pop* (t = 200), como hasta ahora.

### 2.3 Interrupciones

| Caso | Qué pasa |
|---|---|
| Se agrega algo mientras lo último colapsa (0-280) | El vacío no entra (lo pendiente se cancela) y lo nuevo crece. Lo que colapsaba sigue hasta terminar |
| Se agrega algo mientras el vacío entra (280-520) | El vacío sale **desde la opacidad y la y que tiene**, en 120 (`ease.exit`), sin pasar antes por 1 ni por y 0 (DD4a-05 a) |
| Se borran los dos últimos, uno detrás del otro | El vacío espera a que termine el último colapso (cuenta de colapsos en curso) |
| Se borra algo que todavía está creciendo | Colapsa desde su alto actual, como antes |
| Se cambia de pestaña o se cierra el editor en medio de la coreografía | Lo que corre termina en el acto: al volver no queda nada a medias |

### 2.4 Reducir movimiento

| Qué | Cómo | Valores |
|---|---|---|
| Lo que se va | Funde de 1 a 0 en 120 (`ease.exit`) sin cambiar de alto. A los 120 sale, y lo de abajo se acomoda en el acto | — |
| El vacío | Entra a los 120: opacidad de 0 a 1 en 160 (`ease.enter`), sin subir (DD4a-05 b) | 0,577 a los 160 ms; 0,872 a los 200; 1 a los 280 |
| Lo primero que entra | El vacío funde en su lugar en 120 (igual que sin reducir movimiento), y lo nuevo funde en 160 sin crecer | — |

### 2.5 Los otros tres puntos de QA-V3-02 (04-design-notes de la It. 3 §16.6)

| Qué | Coreografía | Con reducir movimiento |
|---|---|---|
| Hojas de la serie (`×N` → `OK`) | Alto de 0 a 3 (o de 3 a 0) y opacidad de 0 a 1 (o de 1 a 0) en 160 `ease.move`, desde el borde de arriba: cuelgan del pie. Una hoja con ×2 y dos con ×3 o más; al bajar a ×1 se van las dos a la vez. A los 40 ms: 0,21 dp y opacidad 0,071; a los 80: 1,50 y 0,5; a los 120: 2,79 y 0,929 | **Fundido de 160 sin cambio de alto.** Lo decido acá: §5.0 de la It. 3 decía "fundido" y §5.6, "en el acto". Vale el fundido, que es lo que §5.10 pide para lo que aparece y se va |
| Color del tipo (mosaico, vista previa y fila del editor) | Del color de antes al nuevo en 160 `ease.move`, mezcla en sRGB. Un cambio en medio de otro arranca desde el color que se ve | Igual: es un fundido |
| Línea de error | El alto pasa de 0 al de su texto y la opacidad de 0 a 1, en 160 `ease.move`. Al corregir el campo, al revés, con el texto viejo a la vista hasta que la línea se cierra. La caja cambia de color en el acto | En el acto |

### 2.6 Visto bueno

Leí el trabajo en curso del Architect, sin correrlo: `EditorStepsView.Collapsed`, `RoutineListView.Collapsed`, `ExitMotion`, `RevealMotion` con `ErrorLine`, `ColorFade` y las hojas de `SeriesBlockView`. Hacen lo de §2.1 a §2.5.

**Visto bueno del Designer**, con dos ajustes (**DD4a-05**):

- **a.** El vacío que estaba entrando tiene que salir desde su opacidad y su posición actuales. Hoy, `_emptyIn.Finish()` lo lleva a 1 antes de fundirlo: con `Serie` a los 300 ms de borrar, se vería un destello de 0,23 a 1.
- **b.** Con reducir movimiento, el vacío entra en 160 sin subir (§2.4), si `EnterMotion` no lo hace ya.

El visto bueno final lo da QA en VID-01, con los valores de §2.1 y dos corridas seguidas (QA-V3-06). Hoy el bloque desaparece entre 7,9 y 8,0 s. Con la corrección, contando desde el cuadro del toque:

| Cuadro | Qué se ve |
|---|---|
| + 4 | El bloque, transparente y al 91,7 % de su alto |
| + 8 | El bloque, al 0,4 % |
| + 9 | El vacío, con opacidad 0,23 y 6,15 dp más abajo |
| + 16 | El vacío, entero |

---

## 3. Kerning de los títulos y de las cifras del HUD (D-ART-02)

### 3.1 Método

- **Herramienta.** fontTools 4.68.0 (`uvx --offline`, la versión que ya estaba en caché) sobre las 7 caras de texto de `UI/Fonts/`. Es el método de la It. 3 (04-design-notes §14.7): la feature `kern` de GPOS, `PairPos` de formatos 1 y 2 (también dentro de `Extension`); dentro de cada lookup gana el primer subtable que aplica, y los lookups se suman.
- **Contra qué.** Contra `m_GlyphPairAdjustmentRecords` de cada `.asset` versionado. Los `XAdvance` están en puntos, al tamaño de muestreo del asset: 48 en Inter y 90 en Barlow.
- **Validación.** El método da exactamente los valores de los **49 049 pares** que TextCore ya escribió (Inter 400, 500, 600 y 700, y Barlow 700): 0 diferencias.
- **Textos.**
  - el catálogo es/en entero (380 textos);
  - los candidatos de la 4a y la 4b;
  - los rótulos, las unidades y los botones del HUD, con `hudScale` 1,6 (el máximo).

### 3.2 Qué falta hoy en la tabla versionada

| Cara (tabla) | Dónde se usa | Pares que faltan (los más grandes) |
|---|---|---|
| **Inter Bold** (3 402 pares, 11 "primeros") | `display` 32, `headline` 24, `headerS` 13, Reanudar 16/700, el tipo de paso en la barra de pasos 13/700 (4b) y el título del diálogo del inmersivo 20/700 | En el catálogo, 106 pares distintos, 43 de ellos de 0,02 em o más. En los textos de la 4a y la 4b:<br>- `Te` −0,078 em, en `Terminar entrenamiento`: −2,50 dp a 32, −1,88 a 24 y −1,56 a 20;<br>- `Tr` −0,057 (`Trabajo`), `Wo` −0,050 (`Workout`), `Wa` −0,058 (`Warmup`) e `Yo` −0,109 (`You have…`).<br>En mayúsculas: `TA` −0,089 (`CALENTAMIENTO`, `DISTANCIA`), `VA` −0,079 (`VATIOS`) y `WA` −0,072 (`WARMUP`, `WATTS`) |
| Inter SemiBold (16 381, 49) | `title`, `titleS`, `button`, y los rótulos y botones del HUD | En los textos de la 4a y la 4b, solo `Yo` (`You have successfully…`). En el HUD, `KC` −0,038 em si el rótulo es `KCAL` (−0,79 dp a 13 × 1,6). Los botones del HUD, nada |
| Inter Medium (11 911, 41) | Unidades del HUD, `caption` | `kc` −0,022 em en `kcal` (−0,50 dp a 14 × 1,6) |
| Inter Regular (16 356, 63) | Texto | Los 12 pares de la It. 3, de 1,15 dp como mucho |
| Barlow Bold (909, 20) | Valores del panel del simulador (etiquetas `num.stat`) | — |
| Barlow SemiBold y ExtraBold (sin tabla) | Solo `TabularNumber` | No se dibujan como texto |

**Cuánto se ve.**

- **A distancia de lectura (40 cm)**, un par de 2,5 dp que falta son 0,40 mm, unos 3,4′: más de 1′, la agudeza visual, así que se ve (`T erminar`).
- **A 1,5 m**, el mismo par mide 0,9′, en el límite.
- Los rótulos del HUD, a 1,5 m, quedan por debajo de 0,3′: no se ven.

### 3.3 Las cifras del HUD (`TabularNumber`)

- **Sin pares, por construcción**: cada carácter va en su celda. Barlow define 21 pares entre sus cifras **proporcionales**: `41` −0,075 em, `47`, `7.`, `-7` y `.0`. No sirven para cifras tabulares, y no se aplican.
- **Lo que sí importa es la celda.** Mide el avance máximo de 0-9 en la cifra proporcional, que es el del `4` (0,544 em en Bold; 0,566 en ExtraBold), y cada cifra va centrada en su celda. El `1` proporcional mide 0,319 em en Bold y 0,325 en ExtraBold: le quedan 0,113 y 0,120 em de aire de más por lado.
- **Barlow trae `tnum`**, con cifras tabulares **dibujadas para eso** (de `zero.tf` a `nine.tf`):
  - un solo avance para las diez: 0,507 em en 600, 0,521 en 700 y 0,535 en 800;
  - un `1` más ancho y con pie: su tinta mide 0,344 em en ExtraBold, contra 0,272 la del proporcional;
  - verificado: no tienen pares de kerning entre ellas ni con `:`, `.`, `-` y `m`.

Aire entre tintas, en milésimas de em, y ancho total, en Barlow ExtraBold (`num.hero`):

| Texto | `TabularNumber` hoy (aire · ancho) | Con `tnum` (aire · ancho) |
|---|---|---|
| `11:11` | 294, 228, 213, 294 · 2,584 em | 191, 192, 146, 191 · 2,460 em (−4,8 %) |
| `1:41` | 228, 94, 164 · 2,018 | 192, 84, 87 · 1,925 (−4,6 %) |
| `150` | 220, 112 · 1,698 | 163, 72 · 1,605 (−5,5 %) |
| `24.0` | 71, 96, 132 · 2,018 | 43, 86, 114 · 1,925 (−4,6 %) |

**A 1,5 m (D-ART-02).**

- En el SPM a 64 dp, el aire entre dos `1` baja de 18,8 dp (6,9′) a 12,2 dp (4,5′). Son 6,6 dp menos, 2,4′: más que la agudeza visual.
- Con `s` = 1,6 (102 dp), baja de 30,1 dp a 19,6 dp.
- En Bold los números quedan un 3,7 % (`11:11`) y un 4,2 % (`150`) más angostos, y `FitBox` los achica menos.

### 3.4 La regla

| # | Regla |
|---|---|
| K1 | El kerning es el de la tabla versionada de cada `FontAsset`, y nada más: `getFontFeatures = false`, sin cambios (04-design-notes de la It. 3 §4.16). El texto queda igual en cualquier orden y en una corrida parcial |
| K2 | La tabla de cada cara que dibuja **texto** (Inter 400, 500, 600 y 700; Barlow 700, por los valores del panel) tiene **todos** los pares GPOS `kern` distintos de 0 entre los caracteres del **juego fijo**, y solo esos.<br>El juego: el catálogo es/en, el ASCII imprimible (U+0020 a U+007E), `áéíóúüñÁÉÍÓÚÜÑ¿¡` y `×±·°`. Hoy son 116 caracteres, 115 con glifo (el `📈` del catálogo no está en Inter), y dan entre 1 301 y 1 695 pares por cara.<br>Reemplaza la tabla de hoy, que dependía de lo que se había dibujado en las It. 2 y 3 |
| K3 | El juego se recalcula cuando cambia el catálogo. Una prueba de dotnet falla si un carácter del catálogo no está en el juego, o si la tabla de un `.asset` no es exactamente la lista de pares que da la receta (DD4a-06) |
| K4 | Las caras que dibuja solo `TabularNumber` no llevan tabla: Barlow 600 y 800, y las derivadas "RowMate Numeric SC" de DD4a-07. Entre celdas no hay kerning |
| K5 | Las cifras del HUD, del tablero y de los chips son las **tabulares de la fuente** (`tnum`), en celdas de su avance. `:` y `.` siguen en celdas de 0,32 em: su tinta mide entre 0,158 y 0,173 em y les quedan entre 0,074 y 0,081 em de aire por lado |
| K6 | Un carácter fuera del juego (uno raro que escriba el usuario) va sin kerning, como hoy |

### 3.5 Costo y efecto

- **Tamaño.** Hoy hay unos 20,5 MB de tablas parciales:

  | Cara | MB |
  |---|---|
  | Inter Regular | 6,8 |
  | Inter SemiBold | 6,9 |
  | Inter Medium | 5,0 |
  | Inter Bold | 1,4 |
  | Barlow Bold | 0,4 |

  Con K2 quedan unos 3,4 MB (7 831 pares a unos 433 B cada uno): 17 MB menos en el repositorio.
- **Capturas.** Cambia todo texto en el que aparezca un par nuevo: de menos de un píxel hasta 2,5 dp.
  - Los catálogos de las It. 2 y 3 toman una base nueva. Conviene hacerlo en la misma regeneración que QA-V3-02 a -04, si llega a tiempo, y si no, antes de las capturas del HUD de la 4a.
  - Con K1, las comparaciones siguen siendo deterministas: byte a byte entre corridas completas, y ± 1 con `-Filter` (regla 5 de la It. 3).
- **Medidas de la It. 3.** Ninguna cambia: los anchos de §3 de la It. 3 se midieron sin kerning, que es la cota de arriba.

### 3.6 Pedidos al Architect (DD4a-06 y DD4a-07)

| ID | Qué | Cómo se verifica |
|---|---|---|
| DD4a-06 | **Las tablas con K2.** El camino lo elige el Architect:<br>**a.** una herramienta reproducible en `tools/` (C#, o fontTools con la versión fijada) que lee GPOS y escribe la lista de pares, y el bootstrapper la escribe en `m_FontFeatureTable.m_GlyphPairAdjustmentRecords` con `SerializedObject`, dentro de `GeneratedAssetGuard.AllowWrites()`: `XAdvance` = unidades / `unitsPerEm` × tamaño de muestreo (48 o 90), lo demás en 0, ordenado por (primero, segundo);<br>**b.** `TryAddCharacters(juego, includeFontFeatures: true)`, dejar después solo los pares de dentro del juego y vaciar glifos, caracteres y atlas (04-design-notes de la It. 3 §4.16).<br>Los programas de este documento (`kern_check.py`, `kern_set.py` y `kern_verify.py`, en `<scratch>`) son la referencia. Si conviene versionarlos, los paso a `tools/` | dotnet: K3; el conjunto de pares de cada `.asset` es igual a la lista de la receta; `m_GetFontFeatures: 0` en las 7 caras. Capturas: base nueva |
| DD4a-07 | **"RowMate Numeric SC" 600, 700 y 800**: Barlow Semi Condensed con `tnum` congelado.<br>**Receta** (verificada con fontTools 4.68.0, `freeze_tnum.py`): las cifras del `cmap` pasan a los `*.tf`, y los nombres 1, 3, 4, 6, 16 y 21 cambian de familia. La OFL 1.1 pide otro nombre; Barlow no declara un nombre reservado, pero ASSET_SLOTS §3.7 lo renombra igual. Equivale a `pyftfeatfreeze -f tnum -R "Barlow Semi Condensed/RowMate Numeric SC"`, que no está en la caché sin red.<br>**Integración:**<br>- el `.ttf` y su `OFL.txt` en `UI/Fonts/RowMateNumericSC/`;<br>- los `FontAsset` los crea el bootstrapper, con muestreo de 90 como Barlow;<br>- `FontAssetPaths`, con su cuenta;<br>- `Typography.uss` regenerado con `tools/tokens`, con el rol numérico apuntando a las derivadas.<br>`TabularNumber` no cambia: sus celdas pasan a medir todas lo mismo.<br>**ASSET_SLOTS §3.7**: Barlow sí trae `tnum` y se va la rama "si no trae" (lo actualizo cuando esté integrado) | dotnet: en cada derivada, las 10 cifras con un mismo avance (507, 521 y 535 unidades) y el `cmap` apuntando a los `*.tf`. Capturas: cambian el tablero de Dispositivo (It. 2) y todo número de `TabularNumber` (también el `×N`) |

Ninguno de los dos cambia el producto: es cómo se dibujan los números y los pares. El PO lo puede mostrar en el gate de la 4a junto con D-ART-02.

---

## 4. Base visual del HUD (para las 04-design-notes de la 4a)

### 4.1 En horizontal, la columna derecha no entra

Con las zonas del legacy × `s` (Split y Vatios desde `top 90·s`, kcal y BPM desde `bottom 140·s`) y las tarjetas de ART_BIBLE §3.2, la columna derecha necesita, como mínimo, 534,9·s dp con BPM y 465,3·s sin BPM. Es una cota de abajo: no cuenta separación entre la etiqueta y el valor.

| Perfil | s | Hace falta (con BPM · sin BPM) | Alto de la ventana | ¿Entra? |
|---|---|---|---|---|
| TV | 1,054 | 563,8 · 490,4 | 914 | Sí |
| P360 | 0,923 | 493,8 · 429,5 | 800 | Sí |
| TH | 1,054 | 563,8 · 490,4 | 411 | **No** |
| L640 | 0,923 | 493,8 · 429,5 | 360 | **No** |
| W1280 | 1,6 | 855,9 · 744,5 | 720 | **No**: kcal pisa al menos 12 dp de Vatios, y BPM queda entera encima |

En el legacy no pasa en el escritorio, porque no escala. La 4a lo tiene que resolver: su demo pide el HUD "vertical y horizontal". No lo decido acá; es la primera decisión de las 04-design-notes de la 4a. Dos caminos posibles:

- en horizontal, kcal y BPM pasan a la fila de abajo, junto a la banda;
- `hudScale` se acota también por el alto.

### 4.2 El hueco de la llave

El HUD deja libre, abajo a la derecha, un hueco de 72 dp de ancho desde el borde (16 + 40 + 16) y 56 de alto (8 + 40 + 8). Va por encima de los controles si se cruzan con él, y por debajo de la tarjeta más baja de la columna derecha (R4).

### 4.3 Números y kerning

K1 a K6 (§3.4) valen desde la primera captura del HUD. Las cifras del HUD salen con `tnum` (DD4a-07) y los títulos y rótulos, con las tablas de K2 (DD4a-06).

---

## 5. Para otros

| Para | Qué |
|---|---|
| PO | **D-ART-10** (§1): una DV nueva que reemplaza a DV-13 y se muestra en el gate de la 4a. Hay que sumarla al ROADMAP §6, en el gate de la 4a. Textos propuestos:<br>- **AC-BLE-16-02.** "*Entonces*, con el panel cerrado (como arranca siempre), hay un botón flotante chico con el ícono `build` (llave) abajo a la izquierda: a 16 dp del borde izquierdo y 16 dp por encima de la barra de pestañas, o, con una pantalla apilada que tapa la barra (el editor), a 16 dp del borde de abajo. En el entrenamiento a pantalla completa va abajo a la derecha, a 16 dp del borde derecho, alineado con el borde de abajo de los controles, y por encima de ellos si se cruzaría (8 dp de aire). En reposo no tapa ninguna acción: ni el `+`, ni el `⋮`, el lápiz, la cruz o el tacho, ni `+ Paso` o `Serie`, ni los controles del entrenamiento. Pasa de un lugar al otro sin saltos: con un desplazamiento vertical corto entre una pestaña y una pantalla apilada, y con un fundido al entrar y al salir del entrenamiento (con 'reducir movimiento', en el acto o solo con el fundido). Se puede tocar mientras se mueve. Tocarla abre el panel en su lugar, creciendo desde la llave, y mientras el panel está abierto la llave no se ve."<br>- **AC-BLE-16-03.** El alto máximo se mide desde el borde de abajo del panel (§1.7); en las pestañas da la fórmula de hoy. En el entrenamiento, además, el tope de Vatios, si se acepta.<br>- **AC-RUT-10-04.** "(el panel abierto sí lo tapa…)" pasa a "el panel abierto se abre a la izquierda y deja el `+` libre desde 368 dp de ancho".<br>- **AC-RUT-10-09.** Sale "que en Rutinas está sobre el `+`".<br>- Las descripciones de CAP-01, -06, -18, -19 y -28.<br>Además:<br>- La spec de la 4a tiene que pedir el HUD horizontal resuelto (§4.1).<br>- El ROADMAP (It. 4a, demo b) y el REPORT de la It. 3 (§9.1) todavía citan `#FF5F59`: el token es `#FF625B` desde la It. 3 (`state.offTarget.hud`, ART_BIBLE §2.3; ya lo avisaba 03-rutinas/00-deuda-diseno §4) |
| Architect | DD4a-01 a DD4a-04 (§1.8), DD4a-05 (§2.6), DD4a-06 y DD4a-07 (§3.6), con las pruebas y tomas que cambian. Prioridad: DD4a-05 y DD4a-06 con la regeneración de QA-V3-02 a -04. DD4a-01 a -04 y DD4a-07, antes de las capturas de la 4a |
| QA | La auditoría de DD4a-04 en todas las tomas con simulador. VID-01 con los valores de §2.1 y §2.6. Después de DD4a-06 y -07, una base nueva de capturas de las It. 2 y 3, y la prueba de K3 |
| Coordinador | Los programas de esta medición están en `<scratch>/it4a/designer-debt/` y no se versionan. `uvx --offline --from fonttools==4.68.0` funciona sin red con la caché de esta PC; `opentype-feature-freezer` no está en la caché, y por eso la receta de DD4a-07 usa solo fontTools |

---

## 6. Alineación de las biblias

| Biblia | Dónde | Qué cambió |
|---|---|---|
| ART_BIBLE | §0.2 | D-ART-10 (la llave) |
| ART_BIBLE | §3.5 | Filas nuevas: **menú emergente** (`⋮` y menú de texto) y **ayuda emergente**, con las medidas de 04-design-notes de la It. 3 §4.4. Faltaban desde la It. 3. Además, el menú esquiva la llave (R6) |
| ART_BIBLE | §3.6 | El anillo de foco se mide desde el borde de afuera: con un borde de 2 dp, como el mosaico, queda a −6 desde adentro (§16.2 de la It. 3) |
| ART_BIBLE | §3.11 | La llave y el panel con R1 a R9 |
| ART_BIBLE | §4.1 | Noto Emoji, como respaldo de los emojis del usuario (P-09 de la It. 3; faltaba) |
| ART_BIBLE | §4.2 | Barlow trae `tnum`: cifras con la derivada "RowMate Numeric SC" (DD4a-07) |
| ART_BIBLE | §4.4 (nueva) | Kerning: K1 a K6 |
| ART_BIBLE | §5.2 | El hueco de la llave en el HUD y el aviso del horizontal (§4.1) |
| ANIMATION_BIBLE | §2.3 | Lo último que se va y lo primero que entra (§2.1 y §2.2) |
| ANIMATION_BIBLE | §2.11 | La llave cambia de carril con un fundido al entrar y al salir del inmersivo |
| ANIMATION_BIBLE | §2.13 | El panel crece desde la esquina de la llave |
| ANIMATION_BIBLE | §2.14 | La cruz y el tacho con el vacío; las hojas con reducir movimiento como fundido de 160 sin cambio de alto (corrige "en el acto"); la llave con §1.6; el último párrafo remite a §2 de este documento |
| Las dos | Final | La sección "Cambios al iniciar la It. 4a" |

No se editaron documentos de iteraciones cerradas. La contradicción de §5.0 y §5.6 de la It. 3 sobre las hojas queda resuelta acá y en la biblia.

---

## 7. Verificación hecha

1. **Legacy**, solo lectura: los archivos del encabezado. `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar, y no se leyó `strava_secrets.dart`.
2. **Capturas miradas**:
   - de la It. 3: CAP-04, -06, -11, -12a, -22 y DEMO-06;
   - de la It. 2: CAP-01, -03, -08, -09, -12a, -16, -18 y -21a.

   Las medidas de §1.1 salen de esas capturas y del código de la llave. Las de §1.4 y §1.5, de `key_rule.py`.
3. **Kerning y cifras** (fontTools 4.68.0, sin red):
   - `kern_check.py`: los pares que faltan por texto y tamaño;
   - `kern_set.py` y `kern_final.py`: los pares dentro del juego y su tamaño;
   - `kern_verify.py`: los 49 049 pares de TextCore, iguales a GPOS, y las cifras `tnum`;
   - `freeze_tnum.py` y `tf_pairs.py`: la derivada y la ausencia de pares en sus cifras.

   Las derivadas quedaron en `<scratch>`, no en el repositorio.
4. **Curvas de §2**: `choreo.py`, con las cubic-bezier de `Tokens.cs` y el método de `Easing.cs`.
5. **HUD de §4.1**: `hud_column.py`.
6. **El repositorio**: solo cambiaron este documento y las dos biblias. Ningún token, código, asset ni captura. El trabajo en curso del Architect se leyó con `git diff`, sin tocarlo. Unity no se abrió y dotnet no se corrió.
