# It. 8 — Base de arte: Río urbano, Costa, Canal de regata, miniaturas y selector

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-07 |
| Iteración | 8 · `08-entornos-selector` ([ROADMAP §It. 8](../../ROADMAP.md)), sobre la escena de la 6a (agua degradada; la 6b sigue postergada, issue #6) y el remero de la 7 |
| Entrada | [ART_BIBLE §6, §7.1–§7.6, §7.10, §9, §3.11](../../design/ART_BIBLE.md); [ANIMATION_BIBLE §2.12, §6](../../design/ANIMATION_BIBLE.md); [ASSET_SLOTS §1.3, §1.4, `ui.thumbnail`](../../design/ASSET_SLOTS.md); [06a/04-design-notes §2, §6, §7, §8, §10](../06a-escena-lago/04-design-notes.md); [07/00-arte-remero §11](../07-remero-palada/00-arte-remero.md); las tomas de [06a/snapshots](../06a-escena-lago/snapshots/) y [07/snapshots](../07-remero-palada/snapshots/) |
| Legacy leído (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/scene/environment.dart`, `environments/{river,coast,regatta,lake}_environment.dart`, `painters/shore_painter.dart`, `painters/thumbnail_painter.dart`, `environment_picker.dart`, `scene_settings.dart`, `time_of_day.dart`, `workout_screen.dart` |
| Modo | Entrega rápida: cada duda se resolvió con el legacy o con el valor por defecto, en una línea (§0.2). Lo menor va a "Para issues" (§12) |

No repite las biblias: fija lo que faltaba para los 3 escenarios con números (corte de orilla, siluetas por capas, catálogo de objetos con triángulos, luz por hora, niebla, vida y presupuesto), las miniaturas y el selector. Mundo como en la 6a y la 7: `x` lateral (+ derecha de la pantalla), `y` altura sobre el agua, `z` hacia adelante; todo objeto se ubica donde lo pone el generador del legacy (Q-61) y lo de este documento es **cómo se ve**, no dónde está.

---

## 0. Resumen

### 0.1 Qué hay que lograr

1. **Tres lugares que se reconocen en un segundo**, con la misma luz, cielo, niebla y agua que el lago: un río encauzado entre muros de piedra y fachadas (inspiración Henley / Charles), una costa con acantilados, playas y faro sobre mar abierto, y un canal de regata recto, planchado, con andariveles, carteles, tribunas y álamos (inspiración Bosbaan). Sin marcas ni lugares copiados.
2. **Paridad del legacy** en lo que se genera (tipos, posiciones, frecuencias, topes, colores base, hitos en metros exactos, oleaje): lo da Core con `DartRandom`, igual que el lago.
3. **Todo procedural** detrás de *slots* (ASSET_SLOTS §1.3–§1.4): funciona sin nada descargado, y lo de terceros nunca entra en las miniaturas versionadas.
4. **Presupuesto del lago o menos** en cada escenario: Medio ≤ 95 *draw calls*, **SetPass ≤ 50**, ≤ 220 k triángulos. El más cargado es el **Río a las 22 h** (§9).
5. **Selector EXR** en la vista de reposo de Workout: 4 tarjetas con la miniatura horneada de la escena real a las 12 h (QA-P-11), selección con anillo, check y *spring*, sin relayout.

### 0.2 Decisiones tomadas (una línea cada una)

- **D8-01** Los edificios, clubes, tribunas y torre se orientan **hacia el agua** (fachada larga con ventanas mirando al río o al canal); la cara que mira a la cámara conserva el dibujo del legacy (ancho, alto, ventanas y colores). Así se ven bien de frente y en escorzo.
- **D8-02** Las siluetas del Río y de la Regata **se acercan al 4 % de la velocidad y no se corren de costado** (Q-65, como el lago). El legacy las desplazaba (`distance·0,004·60` y `·0,006·60` px). La Costa conserva su fase del legacy (`distance·0,002`), que ya no corre de costado.
- **D8-03** El faro va sobre un **promontorio bajo**: el acantilado del tramo del faro (el bloque en `z0 + 25`) se dibuja con escala 0,35 (2,8 m) y el faro apoya arriba. Solo cambia el dibujo: el generador y sus semillas quedan intactos (`environment_test` no cambia).
- **D8-04** La grada del legacy (12 m de ancho de frente) pasa a una tribuna de **24 m a lo largo del canal × 6 m**, con el techo a 5,3 m como el legacy. Una tribuna de frente a la cámara no existe en un canal real.
- **D8-05** El cartel de distancia muestra el texto del legacy (`"<metros> m"`) con **cifras que llenan el tablero** (alto ≤ 0,62 m, ancho ≤ 1,60 m): el legacy (0,55 m de fuente) se salía del tablero desde 5 cifras (`10000 m`).
- **D8-06** El oleaje visible (bandas de cresta en el agua degradada y el mar de fondo en el bote) se activa solo con `waveAmplitude ≥ 0,5`: solo la Costa (0,8). Lago (0,25), Río (0,35) y Regata (0,3) no cambian: las tomas de la 6a y la 7 siguen iguales byte a byte.
- **D8-07** El reloj de la torre de llegada marca la **hora de la escena** (la real o la forzada por el simulador): coherente con el cielo.
- **D8-08** Miniaturas de **456 × 288 px** (×3 de la tarjeta de 152 × 96 dp), no 304 × 192: en un teléfono de 2,625 px/dp la de ×2 se agrandaba y se veía blanda. Cuestan 0,13 MB cada una en ASTC 4×4.
- **D8-09** La miniatura usa todo el alcance de 400 m (el legacy cortaba en 150 m para ahorrar dibujo en 2D; horneada no cuesta). Así aparecen el puente del río (a 250 m) y la tribuna y el cartel de 250 m de la regata.
- **D8-10** Luces de noche nuevas solo donde ya hay luz en el legacy o en un edificio: la sala de control y el reloj de la torre de llegada (regata). Todas con la regla de la 6a (`ambient < 0,6`, de 19:26 a 4:43, cada una en su minuto en ± 10 min, fundido de 0,4 s; DV-60).
- **D8-11** Las tarjetas del selector no animan el ancho del borde: el anillo de selección es un elemento absoluto encima de la tarjeta y se anima con opacidad y escala (sin relayout, ANIMATION_BIBLE §0.1).

---

## 1. Lo común a los tres escenarios

### 1.1 Planos de profundidad (ART_BIBLE §7.1, ajustados por escenario)

| Plano | Río | Costa | Regata |
|---|---|---|---|
| Orilla cercana (los objetos del legacy, `z` d+1 a d+400, segmentos de 50 m) | muro, malecón, fachadas, clubes, faroles, boyas, puente | acantilados, rocas, playas, sombrillas, faro (solo a la izquierda) | boyas, carteles, banderas, tribuna, torre, talud de pasto |
| Medio (relleno determinista, sal propia por segmento, como el bosque del lago) | árboles de vereda en los huecos; manzanas de ciudad en `\|x\|` 22–120 | meseta con matas sobre el acantilado; dunas detrás de la playa | álamos en `\|x\|` 30–40 cada 12 m |
| Fondo (niebla extendida 400–1 200 m) | manzanas lejanas en bloques | la costa que sigue, baja | la línea de álamos y el césped |
| Horizonte (capa `Horizon` del cielo; `horizonColor` del contrato) | *skyline* del legacy (§2.2) | acantilado lejano del legacy + promontorio + mar abierto (§3.2) | tierra, tribunas, torre y banderas del legacy (§4.2) |

- **Alto de las siluetas**: la regla del lago, `tan Θ = mín(f·alto/focal; tan 9°)` con la fracción del legacy de cada escenario: Río **0,12**, Costa **0,08**, Regata **0,06**. En TV (411 × 914) quedan 9° (tope), 9° (tope) y 7,6°; en TH, 6,84°, 4,57° y 3,43°.
- **Color** de toda silueta: `lerp(tono; fog; 0,35 + 0,4·profundidad)` [CONTRATO]; tabla por hora en §5.
- **Aparición y LOD**: los de la 6a (tramado *Bayer* entre 400 y 360 m; LOD por tramado complementario en el 10 % del límite; salida con tramado entre 2 y 1 m). Límites de LOD por defecto **60 / 180 m** (LOD0 → LOD1 → LOD2), × 0,7 en Bajo.
- **Nada flota**: todo apoya en muro, terreno, playa o promontorio (hallazgo 1). Las boyas flotan de verdad (cabecean, §1.4).

### 1.2 Luz y materiales

- **Un solo modelo de luz** (`RowMateLighting.hlsl`): `T(a)` del legacy, `Hemi` con el 20 % del croma y la luz clave de `HourLookTable`. Ningún escenario trae su propia paleta: los colores base son los del legacy y la hora los tiñe (AC-SCN-09-04).
- **Materiales por color de vértice** (como la cabaña y el remero): `rgb` = color de referencia del legacy, `a` = material (0 rugoso, 0,5 liso, 1 emisivo de noche). Texturas solo las que ya existen (ruido 256², espuma 256²) más dos chicas nuevas (§9.3).
- **Ruido de superficie** por *shader* (± 6 % de luminancia): sillares del muro (0,90 × 0,45 m), ladrillo (0,24 × 0,07 m, solo en el tono `#8D6E63`), adoquines (0,20 m), estratos del acantilado (bandas de 0,6 a 1,2 m), arena (granos y franja húmeda), césped cortado (franjas de 3 m alternadas ± 4 %).
- **Sombras**: proyectan los edificios, el puente, los clubes, los árboles, los acantilados, el faro, las sombrillas, la tribuna y la torre dentro de la distancia de sombras del nivel. No proyectan: boyas, faroles, banderas, carteles, público, gaviotas (finos o chicos: titilan, como las cañas de la 7).

### 1.3 Niebla y atmósfera

| | Lago (6a) | Río | Costa | Regata |
|---|---|---|---|---|
| Niebla del contrato | 80–400 m, 80 %, `fog` | igual | igual | igual |
| Extendida (terreno y relleno) | 400–1 200 m | igual | igual | igual |
| Bruma baja `m(h)` (0,55 de 6 a 7 h; 0 desde 9:30) | × 1 | × 0,6 (bruma de río, hasta 3 m de alto) | × 0,4 | × 1 (canal quieto) |
| Bruma del horizonte (franja sobre el horizonte) | 1,5° | 1,5° + bruma urbana 1,2° al 35 % | **2,5°** al 45 % (salitre) | 1,5° |

### 1.4 Vida (ANIMATION_BIBLE §6; todo por GPU con el tiempo de escena; sigue en pausa)

| Qué | Movimiento | Dónde se calcula |
|---|---|---|
| Banderas de orilla | punta `sin(4t + semilla)·0,2 m` [CONTRATO] + 2.º armónico `0,06·sin(9t + 2,1·u + semilla)` (`u` 0 en el mástil, 1 en la punta), amplitud × `u²`; el paño siempre hacia +x (el viento único de la 6a) | vértice |
| Banderas del horizonte | `sin(3t + i)·2 px` [CONTRATO] → 0,28° en TV | `Horizon` |
| Boyas (río y regata) | cabeceo `0,08·sin(1,7t + semilla)` rad y arfada `0,02·sin(1,3t + semilla)` m; semilla = índice de la boya en su segmento | vértice |
| Sombrillas | borde de la tela `± 0,03 m · sin(5t + semilla)` | vértice |
| Gaviotas (costa) | 5 [CONTRATO]; recorrido del legacy pasado a ángulos (§3.4) | Core (posición) + vértice (aleteo) |
| Faro (noche) | haz que gira 360° cada 12 s | vértice |
| Público (regata) | cada impostor alterna 2–3 poses con período 1,5–4 s y desfase por semilla | vértice (cuadro del atlas) |
| Árboles de vereda y álamos | el viento de las copas de la 6a (`0,03·sin(1,1t + x·0,13)` + ráfaga) × altura² | vértice |
| Columnas de luz en el agua (río, noche) | ancho ± 15 % `sin(2,3t + z)`, alfa ± 10 % | vértice |
| Ventanas, faroles, faro, torre | encendido de la 6a (D8-10) | Core (`NightLights`) |

Con "reducir movimiento" no se detiene nada de esto (solo la cámara, como en la 7).

---

## 2. Río urbano

### 2.1 Corte transversal (desde el eje del bote; simétrico)

| `\|x\|` (m) | Qué | Alto (m) | Material |
|---|---|---|---|
| 0–9,0 | agua (18 m de canal [CONTRATO]) | 0 | agua degradada, perfil "Río" |
| 9,0 | **muro de contención** de piedra, plomo, 1,20 m sobre el agua (0,5 m bajo el agua visibles) | 0 → 1,20 | piedra `#9E9E9E`, sillares por *shader* |
| 9,0–9,35 | remate del muro (sobresale 0,08 m hacia el agua) | 1,20–1,35 | piedra clara `#B5AFA4` |
| 9,35–12,5 | **malecón** adoquinado; faroles en ±9,5 | 1,20 | adoquín `#8A8378` |
| 11,5 | árboles de vereda en los huecos ≥ 10 m entre edificios | 1,20 | §2.3 |
| 10,1–21,5 | fachadas y clubes del legacy (eje en `±(13..17)`, ancla en la cara a la cámara; volumen nunca más adentro de 10,1) | desde 1,20 | §2.3 |
| 22–120 | manzanas de relleno (medio y fondo) | 9–27 | `Facade`, sin detalle |

- **Clubes**: su rampa va **paralela al muro** en `\|x\|` 8,5–10,0 (1,5 m de ancho), 12° de 1,20 a −0,2 (6,6 m en `z`) desde el portón; corta el muro y el malecón en su tramo. *Motivo (gate QA-G8-01): perpendicular, con 6,6 m de largo, llegaba a `\|x\|` 2,55, en el carril del bote y sobre las boyas.*
- **Puente** (cada 800 m, `k mod 16 = 5`, en `z0 + 20`): sus estribos tapan el malecón y **terminan en `\|x\|` 12,5**; el malecón pasa por un arco lateral de 3 m de luz en `\|x\|` 9,5–12,5. *Motivo (gate QA-G8-01): con estribos hasta ±24, 116 de 125 puentes cruzaban una casa.*
- **Sin cruces**: el fondo de cada casa se recorta contra la casa siguiente y contra el puente (reglas R1 a R6 de [02-tech-design](02-tech-design.md) §2.4.1).

### 2.2 Siluetas lejanas (capas)

| Capa | Spec |
|---|---|
| H1 · *skyline* del legacy | Columnas de 34 px del legacy pasadas a ángulo: **4,73° por columna** (TV), ancho 0,6–1,0 de la columna, alto `hMax·(0,3 + 0,7·r)`, con `DartRandom(i·31 + 7)` en el orden del legacy (alto, ancho, ventanas). Tono `#3A4660`, profundidad 0,8. D₀ 1 500 m, se acerca al 4 % (D8-02). **Ventanas de noche** del legacy: celdas de 8 × 7 px → 1,11° × 0,98°, ventana de 3 × 4 px → 0,42° × 0,56°, encendidas con `r < 0,5` en el mismo orden de `DartRandom` (el patrón exacto del legacy), color `#FFE08A @85 %`, emisivo 1, sin niebla |
| H2 · bruma urbana | franja `lerp(fog; skyHorizon; 0,3)` de 0° a 1,2° sobre el horizonte, alfa 0,35 → 0 (*smoothstep*); de noche, `fog` con un 8 % de `#FFE08A` (el resplandor de la ciudad) |
| M · manzanas | relleno 3D en `\|x\|` 22–120, `z` hasta d+400: bloques de 12–30 m de frente, 15–30 de fondo y 3–9 pisos (9–27 m), tonos del legacy × 0,85, ventanas por `Facade` (35 % encendidas de noche), techo plano con 0–2 tanques o chimeneas. Su color lo lleva la niebla extendida: a 400 m ya casi son `fog` |

### 2.3 Catálogo de objetos (medidas en m; triángulos LOD0 / LOD1 / LOD2)

| Objeto (legacy) | Forma 3D | Colores (legacy) | Tris | Noche |
|---|---|---|---|---|
| `building` (70 %; `±(13..17)` cada 15–30 m, escala 0,6–1,0) | Planta 6 × 10 (6 de frente a la cámara, 10 al río; D8-01; ancla en la cara a la cámara, fondo recortado contra la casa siguiente y el puente, gate QA-G8-01), `pisos = 3 + semilla mod 4` de 3 m, zócalo 0,6 más oscuro (× 0,8), cornisa 0,30, techo plano con pretil 0,5 (60 %) o mansarda de pizarra `#4A5560` (40 %), por semilla. Ventanas 1,0 × 1,2: 3 por piso al frente (x = −2,4 + 1,8·i, como el legacy) y 5 al río; LOD0 hundidas 0,15 con marco y alféizar | `#78909C`, `#8D6E63` (con ladrillo), `#B0BEC5` (semilla mod 3); vidrio de día `#263238` con 30 % del cielo | 1 500 / 300 / 24 | máscara del legacy: `Random(semilla)`, piso por piso y ventana por ventana, `< 0,6` → `#FFE082`; la cara del río sigue la misma secuencia |
| `boathouse` (30 %) | Planta 8 × 12 (ancla y recortes como el edificio; el lado del río nunca más adentro de `\|x\|` 10,1, gate QA-G8-01), paredes 4, techo a dos aguas hasta 6 con el hastial hacia la cámara; portón 3 × 2,8 `#37474F` hacia el río con la rampa; franja `#1565C0` de 3,6 a 4,0 en todo el perímetro | `#B0BEC5`, techo `#546E7A` | 900 / 250 / 30 | portón apenas iluminado (farol de 0,6 m sobre el portón, halo r 0,8) |
| `lamp` (±9,5, cada 25 m) | Poste de hierro octogonal 4,0 (Ø 0,12 → 0,08) sobre el remate, base 0,3, globo r 0,30 | poste `#455A64`, globo de día `#90A4AE` | 160 / 48 / 4 | globo `#FFF8E1`, halo r 1,5 `#FFE082 @35 %` [CONTRATO] y **columna de luz** en el agua (§2.4) |
| `laneBuoy` (±3, en `z0 + 25`) | Esfera r 0,25 con franja blanca `@25 %` en la línea de flotación | amarillo `#FDD835` (semilla 1) | 80 / 32 / 8 | — |
| `bridge` (cada 800 m) | Cara de piedra con el arco del legacy: arranque en ±8,5 a 1,5 m, clave a **5,0 m** (la Bézier del legacy), pilares de ±8,5 a ±9,5, tablero 6,0–7,2, parapeto hasta 7,7; profundidad 10 m en `z`; estribos de piedra hasta ±12,5 a 7,2 m (gate QA-G8-01; antes ±24); intradós de 24 tramos con dovelas por *shader* | cara `#9E9E9E`, pilares `#757575`, tablero `#7A7A7A`, parapeto `#616161` | 3 600 / 1 000 / 200 | 4 faroles en el parapeto (los de `lamp` a escala 0,6) |
| `prop.quayWall` (nuevo) | Módulos de 50 m por lado: muro + remate con bisel, desagüe cada 10 m | §2.1 | 400 / 40 / — | — |
| `vegetation.streetTree` (nuevo) | Tronco 3 m, copa de 5 lóbulos elipsoides (r 2,2), alto 7–9; 1 por hueco ≥ 10 m | tronco `#5D4037`, copa `#4F7A3A` (follaje de la 6a) | 700 / 200 / 40 | — |
| Manzanas de relleno (nuevo) | Caja + techo | §2.2 | 24 (un solo LOD) | 35 % |

### 2.4 Detalles que venden el lugar

- **Bajo el puente**: oscurecimiento por `o` (0 afuera, 1 bajo el tablero, rampa de 1,5 m en cada borde): luz clave × (1 − 0,6·o), `Hemi` × (1 − 0,4·o), para el bote, el remero, el agua y el muro. Dura lo que tarda el bote en cruzar 10 m (≈ 2,5 s a 4 m/s).
- **Columna de luz** de cada farol encendido: un quad emisivo aditivo sobre el agua, desde el pie del muro hacia la cámara, 0,5 m de ancho × 8 m de largo, `#FFE082` alfa 0,25 → 0 a lo largo, con la ondulación de §1.4. Es el reflejo barato hasta la 6b.
- **Línea de mojado** del muro: 0,15 m sobre el agua, × 0,7 de luminancia, borde con ruido.

### 2.5 Hora (sobre §5)

- **6 h**: sol a −17,5° detrás de las fachadas de la izquierda: esa orilla en sombra, la derecha con luz rasante cálida; bruma de río 0,33 (= 0,55 × 0,6) pegada al agua.
- **12 h**: luz alta, sombras cortas bajo los aleros y cornisas; el *skyline* claro `#9EB3CB`.
- **18 h**: sol a +17,5° sobre la derecha: fachadas izquierdas encendidas en naranja, *skyline* `#A9847C`.
- **22 h**: ventanas (60 % de las cercanas, 50 % del *skyline*, 35 % de las manzanas), faroles con halo y columna, *skyline* `#1D2740`. Es el cuadro más cargado (§9).

---

## 3. Costa

### 3.1 Corte transversal

**Derecha (`x > 0`): mar abierto hasta el horizonte**, sin orilla ni objetos [legacy]. **Izquierda**:

| Tramo | `x` (m) | Spec |
|---|---|---|
| Acantilado (4 de cada 5 segmentos) | −12,5 → −14 | base de rocas sueltas (`prop.rock`, 4 por bloque, 0,4–1,4 m) con espuma |
| | −14 → −21 | pared del legacy: 8 m × escala (0,8–1,3 → 6,4–10,4 m), borde superior en 5 escalones de 1,4 m (el del legacy), estratos horizontales, grieta oscura 0,6 × 1,2 en la base [legacy] |
| | < −21 | meseta al mismo alto, pasto `#556B2F`, matas bajas (≤ 1,2 m) cada 6 m |
| Playa (`k mod 5 = 2`) | −9,5 → −20 | arena `#D8C3A0` con pendiente de 6°, franja húmeda de 1,2 m (× 0,75 de luminancia, suavidad 0,6), las 3 sombrillas del legacy en −13..−10 |
| | −20 → −30 | duna de 2 m con pasto ralo; detrás, el acantilado vuelve a −30 (una cala) |
| Transición | 10 m en `z` | la pared baja de su alto a 2 m y se mete en la duna |

### 3.2 Siluetas lejanas (capas)

| Capa | Spec |
|---|---|
| H1 · acantilado lejano del legacy | Perfil de 20 pasos pasado a ángulo: **1,0° por paso** (TV: 0,35·411 px / 20), alto pleno en ≤ −27° (borde izquierdo de TV) que baja a 0 en −7° (el 35 % del ancho), `h = hMax·fade·(0,6 + 0,4·sin(1,7·i + 0,5 + fase))`, la sinusoide sigue hasta −65°. Tono `#4A3F3A`, profundidad 0,9, fase del legacy `distance·0,002` (D8-02) |
| H2 · promontorio (nuevo) | Un cabo más cercano: de −65° a −20°, 55 % del alto de H1, tono `#3F3633`, profundidad 0,6, D₀ 1 200 m, se acerca al 4 % |
| H3 · mar abierto | "Faldón" del mar: un anillo a `y` 0 desde el borde del plano de agua (≈ 800 m) hasta el horizonte, de `waterFar` a `lerp(waterFar; fog; 0,5)` (el tramo de arriba del agua degradada) + la bruma de 2,5° (§1.3). A las 18 h el camino de brillo del sol cae sobre este mar |

### 3.3 Catálogo de objetos

| Objeto (legacy) | Forma 3D | Colores (legacy) | Tris | Noche |
|---|---|---|---|---|
| `cliff` (`z0` y `z0 + 25`, escala 0,8–1,3) | Bloque de 28 m en `z` (25 + 3 de solape con el siguiente, sin costuras) × 7 m de fondo, cara desplazada por ruido ± 0,6 m, estratos, pasto arriba 0,9 m | `#5D4A42`, pasto `#556B2F`, grieta `#3E2F2A` | 1 600 / 450 / 80 | — |
| `lighthouse` (cada 1 000 m, `k mod 20 = 10`, en `z0 + 30`) | Sobre el promontorio de 2,8 m (D8-03): torre troncocónica r 1,1 → 0,9, 9 m, 3 franjas rojas de 1 m (a 1–2, 4–5 y 7–8 m, las del legacy), balcón con baranda a 9 m, linterna 1,6 × 1,2 (9–10,2), cúpula | torre `#ECEFF1`, franjas `#D32F2F`, linterna `#37474F` | 900 / 260 / 40 | vidrio de la linterna emisivo, halo r 3 `#FFF59D @35 %` [CONTRATO] y **haz** (§3.4) |
| `beachUmbrella` (3 por playa) | Mástil 2,2, tela de 8 gajos r 0,9 (de 2,2 a 3,1), toalla 1,2 × 0,6 | `#F06292`, `#4FC3F7`, `#FFD54F`; toalla `#FFF3E0`; mástil `#5D4037` | 140 / 40 / 8 | — |
| `prop.rock` (de la 6a) | 5 variantes | `#6B6A66` → teñido 30 % hacia `#5D4A42` | 200 / 60 / — | — |
| `prop.seagull` (5) | Gaviota *low-poly*, envergadura 1,1 m | blanco `#E8E8E4`, puntas `#2B2B2B` | ≤ 300 (un LOD) | siluetas oscuras, como el legacy (alfa del legacy `0,5 + 0,4·a`) |
| Espuma de orilla (nueva) | Una tira de malla a lo largo de la base del acantilado (`x` ≈ −12,5) y de la línea de agua de la playa, 0,6–1,5 m de ancho | blanco `#F4F6F2` × la luz, con `water.foam` 256² | ≤ 2 000 por los 400 m | × `T(a)` |

### 3.4 Detalles que venden el lugar

- **Mar de fondo** (D8-06): en el agua degradada, bandas de cresta perpendiculares a `z`, separadas por `waveScale` × 4 = **16 m** (crestas de mar de fondo, no de viento), que viajan hacia la cámara a 3 m/s, `lerp(col; skyHorizon; 0,08·A·saturate(sin))` con `A` = 0,8, solo de 10 a 150 m (*smoothstep*). +6 ALU. En el bote se suma a la 7: arfada `0,15·A` m, cabeceo `1,5°·A` y rolido `1,8°·A`, período 6,5 s; la cámara no lo sigue (horizonte del contrato ± 3 px).
- **Espuma**: en la base del acantilado late con el mar de fondo (alfa 0,5 ↔ 0,9 cada 6,5 s); en la playa la línea avanza y retrocede 1,2 m sobre la arena mojada con el mismo período.
- **Haz del faro**: 2 conos opuestos aditivos de 60 m y 4° de apertura, `#FFF59D` alfa 0,18 en la linterna → 0 en la punta, con la niebla. Cuando el haz apunta a la cámara (± 6°) el halo sube × 2,2 (el destello). Solo se dibuja mientras el faro está dentro de los 400 m.
- **Gaviotas**: el recorrido del legacy en TV pasado a ángulos: velocidad `(8 + 3·i)/411` rad/s (1,1°–2,8°/s) de izquierda a derecha, elevación `0,578 − 0,089·i` rad (33° → 13°), ondulación `± 4/411` rad con `sin(2t + i)`, aleteo `sin(6t + i)` (punta ± 0,25 m), tamaño `(5 + i)/5` × la envergadura; a 60–120 m. Al salir por la derecha vuelven a entrar por la izquierda con un fundido de 1 s (no saltan).

### 3.5 Hora

- **6 h**: el sol sale detrás del acantilado lejano (como el lago detrás del macizo); los acantilados cercanos en contraluz `#4D3D3E`.
- **12 h**: mar turquesa (tinte `#0E6B7A` al 30 % [CONTRATO]), arena brillante `#D8C3A0`, sombras cortas de las sombrillas.
- **18 h**: **el cuadro de la Costa**: sol a +17,5° sobre el mar abierto con el camino de brillo; los acantilados de la izquierda encendidos de frente.
- **22 h**: acantilados casi negros `#211A1F`, luna sobre el mar, haz del faro girando, espuma apenas visible.

---

## 4. Canal de regata

### 4.1 Corte transversal

| `\|x\|` (m) | Qué | Alto (m) |
|---|---|---|
| 0–9,5 | agua planchada (perfil "Pista"); 4 líneas de boyas en ±3 y ±9 [legacy] | 0 |
| 9,5–11,6 | talud de pasto a 25° | 0 → 1,0 |
| 10,5 | carteles de distancia (izquierda) y banderas (alternando) sobre el talud | ≈ 0,45 |
| 11,6–30 | césped cortado `#5E8C3E` con franjas de corte de 3 m | 1,0 |
| ±12 | tribuna (derecha) y torre de llegada (izquierda) [legacy] | 1,0 |
| 30–40 | álamos cada 12 m, por lado | 1,0 |

### 4.2 Siluetas lejanas (capas)

| Capa | Spec |
|---|---|
| H1 · el fondo del legacy | Tira de tierra `#55704A` (prof. 0,9) de 0 a 0,25·hMax en todo el ancho; los px del legacy pasados a ángulo con 0,139°/px (TV): cada **28,6°** (0,5 del ancho) un bloque de tribuna `#6E7B8C` (prof. 0,8) que empieza a 4,6° y mide 9,2° (0,08 y 0,16 del ancho), entre 0,2 y 0,7·hMax; una torre `#B8C0CC` de 1,43° × hMax a 20,6°; 4 banderas (`#E53935`, `#FFB300`, `#1E88E5`, `#43A047`, prof. 0,5) a 24,0° + 0,86°·i, paño de 0,83°, con mástil de 0,25 a 0,6·hMax. Se acerca al 4 % (D8-02) |
| H2 · arboleda (nueva) | Lomos de copas sobre la tira de tierra, de 0,25 a 0,45·hMax, tono `#3F5A35`, profundidad 0,7, en todo el ancho salvo detrás de las tribunas |
| M · álamos | `vegetation.poplar` (§4.3) en `\|x\|` 30–40: la "pared verde" de un canal real |

### 4.3 Catálogo de objetos

| Objeto (legacy) | Forma 3D | Colores (legacy) | Tris | Noche |
|---|---|---|---|---|
| `laneBuoy` (cada 10 m en x −9, −3, 3, 9; color por tramo de 250 m) | Boya tipo Albano: esfera r 0,25 sobre un cuello de 0,1 × 0,15; suavidad 0,75 | rojo `#E53935`, amarillo `#FDD835`, blanco `#ECEFF1` (`(k div 5) mod 3`) | 96 / 40 / 8 | — |
| `distanceMarker` (cada 250 m en x −10,5) | Poste Ø 0,1 de 1,5 + tablero 1,8 × 1,0 × 0,06 (de 1,5 a 2,5 m) con borde negro de 0,04, **de frente a la cámara**; texto del legacy `"<metros> m"` con cifras de **Barlow Semi Condensed Bold** (OFL, ya en el repo) por SDF, alto ≤ 0,62 y ancho ≤ 1,60 (D8-05), "m" al 70 % del alto | tablero `#FAFAFA`, texto negro 87 % [CONTRATO], poste `#9E9E9E` | 80 + 2 por glifo / 20 + 2 por glifo / 2 (sin texto) | — |
| `grandstand` (cada 1 000 m, `k mod 20 = 4`, x +12) | 24 m en `z` × 6 m (D8-04): 4 gradas de 1,2 de alto y 1,5 de huella, colores alternados; techo 0,5 de espesor a 5,3 m sobre 5 columnas atrás, en voladizo; asientos por *shader* | `#8D9DAE` / `#6E7B8C`, techo `#37474F` | 1 200 / 300 / 40 | — |
| `prop.crowd` (nuevo) | Impostores de 0,5 × 1,0 sentados, 1 por metro de grada con 70 % de ocupación (≈ 67 por tribuna); atlas de 9 cuadros (3 ropas × 3 poses: sentado, brazos arriba, aplauso) | 6 colores de ropa por semilla (`#E53935`, `#1E88E5`, `#FFB300`, `#FAFAFA`, `#263238`, `#43A047`) | 2 por persona | — |
| `finishTower` (cada 1 000 m, `k mod 20 = 12`, x −12) | 3 × 3 × 10; banda `#263238` de 7,8 a 9,0 (la sala de control, vidrio oscuro); reloj r 0,6 a 6,5 m con dos caras (cámara y canal) y agujas reales (D8-07); mástil de 1,5 m | `#CFD8DC`, banda `#263238`, reloj blanco y agujas negras | 600 / 160 / 24 | sala de control `#FFE082 @40 %` y reloj retroiluminado (D8-10) |
| `flag` (1 por segmento, `z0 + 30`, ±10,5 alternando) | Mástil Ø 0,1 de 5 m; banderín triangular del legacy (1,6 de largo, de 4,0 a 5,0 m) en grilla de 8 × 3 | `#E53935`, `#FFB300`, `#1E88E5`, `#43A047` (`semilla mod 4`); mástil `#9E9E9E` | 90 / 30 / 6 | — |
| `vegetation.poplar` (nuevo) | Columna de 6 lóbulos elipsoides Ø 3, alto 14–18 | tronco `#5D4037`, copa `#3F6B35` | 600 / 150 / 4 (cruz) | — |

### 4.4 Detalles que venden el lugar

- **Agua planchada**: sin bandas de cresta (D8-06); el canal es el que mejor muestra las 10 líneas del sol.
- **Las 4 líneas de boyas convergen al horizonte**: con 160 boyas visibles el LOD2 de 8 triángulos alcanza; las más lejanas (≥ 250 m) miden < 1 dp y solo dan la línea de color.
- **Legibilidad del cartel**: cifra de 0,62 m → **≥ 9 dp hasta 28 m en TV y 50 m en W1280**; el número se lee al pasar, como en el legacy (que lo ocultaba bajo 4 px).

### 4.5 Hora

- **6 h**: bruma baja 0,55 sobre el canal, álamos a contraluz, boyas apagadas `#D1B232`.
- **12 h**: el cuadro de la Regata: banderas saturadas, césped con franjas, tribunas `#AFC5D9` en el horizonte.
- **18 h**: sol a +17,5° sobre los álamos de la derecha; tablero `#D5D5F3` (aún se lee).
- **22 h**: canal oscuro, sala de control y reloj encendidos; tablero `#585876` con cifras legibles a ≤ 15 m.

---

## 5. Paleta por hora (sondas de referencia del legacy para QA)

Valores del contrato (paleta del legacy interpolada; `horizonColor` y el tinte `T(a)` del legacy). En 3D la cara al astro sale ≈ 1,25 × el tinte y la de sombra ≈ 0,80 (6a §10.1); las sondas de QA se toman en la cara de frente a la cámara, a menos de 60 m, con ± 10 por canal.

| Hora | `ambient` | `fog` | Río: *skyline* | Costa: acantilado lejano | Regata: tierra / tribuna / torre | Fachada `#78909C` | Acantilado `#5D4A42` | Tablero `#FAFAFA` |
|---|---|---|---|---|---|---|---|---|
| 6 h | 0,825 | `#D3B9B8` | `#A0939B` | `#AB9693` | `#AEA498` / `#B1A5A9` / `#CABBBE` | `#637793` | `#4D3D3E` | `#CECEEC` |
| 12 h | 1,0 | `#CFE9FF` | `#9EB3CB` | `#A8B8C6` | `#ACC6CB` / `#AFC5D9` / `#C7DBEE` | `#78909C` | `#5D4A42` | `#FAFAFA` |
| 18 h | 0,85 | `#E0A28A` | `#A9847C` | `#B58573` | `#B89477` / `#BA958B` / `#D3ACA0` | `#667A97` | `#4F3F40` | `#D5D5F3` |
| 22 h | 0,35 | `#0F1730` | `#1D2740` | `#202333` | `#233138` / `#2E384E` / `#474F63` | `#2A3249` | `#211A1F` | `#585876` |

- **Emisivos** (no se tiñen ni entran en la niebla, ganancia 1): ventana `#FFE082`, ventana del *skyline* `#FFE08A @85 %`, globo del farol `#FFF8E1`, halos `#FFE082 @35 %` y `#FFF59D @35 %`.
- **Encendido**: de 19:26 a 4:43 (`ambient < 0,6`) ± 10 min por luz. A las 6, 12 y 18 h todo apagado.
- **Agua**: la de la 6a por hora (06a/04-design-notes §8.2) con el tinte de cada escenario; Costa `lerp(base; #0E6B7A; 0,3)` [CONTRATO].

---

## 6. Por escenario, de un vistazo (nivel Medio)

| | Río | Costa | Regata |
|---|---|---|---|
| Objetos del legacy visibles (tope del legacy) | ≈ 90 (120) | ≈ 20 (60) | ≈ 172 (200) |
| Instancias de relleno visibles | ≈ 20 árboles + ≈ 60 manzanas + 18 módulos de muro | ≈ 64 rocas + matas | ≈ 68 álamos + ≈ 67 público |
| Texto en 3D | — | — | 1–2 carteles (≤ 7 glifos) |
| Luces de noche | ventanas, 32 faroles, *skyline* | faro | torre |
| Lo que más cuesta | aditivos de noche, sombras de fachadas | espuma, faldón del mar | cantidad de instancias |

---

## 7. Miniaturas del selector

| Aspecto | Spec |
|---|---|
| Qué | Render horneado de la **escena real** de cada escenario (F-SCN-22), sin bote ni remero ni HUD (legacy), a la distancia de escena **20 m**, `t = 0` (nubes, banderas y boyas en su fase 0) |
| Hora | **12:00** (QA-P-11, paridad). El horneador toma la hora como parámetro: la variante 6 / 12 / 18 / 22 h con la más cercana a la hora de escena se ofrece en el gate (ART_BIBLE §7.6) y cuesta 12 texturas más |
| Cámara | la de persecución del contrato con la ventana de la tarjeta (152 × 96: apaisada, horizonte al 42 %, focal = lado corto); todo el alcance de 400 m (D8-09) |
| Tamaño | **456 × 288 px** (D8-08), renderizado a 4× (1 824 × 1 152) y reducido con filtro de caja: bordes limpios sin MSAA |
| Calidad | Alto, con `HourLookTable` de las 12 h; **siempre con los *fallbacks* procedurales** (nunca con lo de `Assets/ThirdParty`: la imagen se versiona en un repo público) |
| Formato | PNG versionado en `Assets/RowMate/Art/Textures/Thumbnails/` (≈ 200 KB cada uno); en el build, ASTC 4×4 en móvil (0,13 MB) y BC7 en escritorio, sin *mipmaps*, `Clamp`, `Bilinear` |
| Caché | Un `thumbnails.json` al lado con, por escenario: hash de los datos de arte (`SceneHash` de las constantes `*Art` + versión del horneador), tamaño y hora. Un test de EditMode falla si el hash no coincide ("miniatura vieja: volver a hornear"). Se hornean desde el menú o con un *flag* de línea de comandos. En ejecución, se cargan una vez con la vista de Workout y quedan en memoria (4 × 0,13 MB); nunca se renderizan ni se cargan escenas |
| Si falta una | Tarjeta procedural: degradé de 12 h `skyTop` → `skyHorizon` arriba del 42 % y `waterFar` → `waterNear` abajo, con un `[SLOT]` en el log |
| Lo que tiene que verse | Lago: pinos, montañas nevadas, agua. Río: muro, fachadas, faroles, el puente a 250 m. Costa: acantilados a la izquierda, mar abierto a la derecha, playa con sombrillas a 80–130 m. Regata: 4 líneas de boyas, banderas, tribuna y cartel de 250 m |

---

## 8. El selector en la vista de reposo de Workout

### 8.1 Lugar y medidas (F-WKT-12; legacy `workout_screen.dart`)

- Entre el aviso de "remo no conectado" y el botón "Entrenamiento libre": rótulo `workoutScene` ("Escenario") 13 dp `text.secondary`, 8 dp, la fila, 16 dp.
- **Fila horizontal desplazable** (como el legacy) que sangra hasta el borde de la pantalla: relleno 16 a los lados, separación **10** (legacy). En un teléfono de 411 dp se ven 2 tarjetas y un 36 % de la tercera (la invitación a deslizar); desde 680 dp entran las 4.
- **Tarjeta**: 152 × 96 dp (ART_BIBLE §3.11), radio `r.l`, miniatura recortada por el radio; debajo, a 6 dp, el nombre (13 dp, una línea, "…"): "Lago alpino", "Río urbano", "Costa", "Canal de regata" (claves `sceneLake`, `sceneRiver`, `sceneCoast`, `sceneRegatta`). Alto de la fila: 96 + 6 + 18 = 120 dp (+ 2 de aire para el anillo). Área táctil = tarjeta + nombre.
- **No elegida**: borde 1 `border.default`, velo negro 15 % sobre la miniatura, nombre 400 a `@70 %`.
- **Elegida**: anillo de 2,5 dp `accent.primary` [CONTRATO] (elemento absoluto encima, D8-11), sin velo, miniatura a escala 1,03 dentro del recorte, nombre 700 blanco y **check**: disco de 22 dp `accent.primary` a 6 dp de la esquina superior derecha con el glifo `check` de 14 dp en el color de texto del botón primario. El check, el ancho del anillo y el peso del nombre hacen que la elección no dependa solo del color (ART_BIBLE §10.3).
- Foco de teclado: el anillo de foco de la app alrededor de la tarjeta; Enter / Espacio eligen; flechas mueven el foco.
- **No es HUD**: no suma elementos a los 56–57 de 60 del inmersivo.

### 8.2 Movimiento (tokens de ANIMATION_BIBLE §1; nada cambia el layout)

| t (ms) | Qué |
|---|---|
| 0 | toque: la tarjeta a escala 0,97 en 90 y vuelve en 240 (§2.12). La preferencia se guarda en el acto (legacy) |
| 0–160 | la que se deja: anillo 1 → 0 (`ease.exit`), check 1 → 0 y escala 1 → 0,6 en 120, velo 0 → 0,15, miniatura 1,03 → 1 en 240 (`ease.move`), nombre 700 → 400 por fundido cruzado de dos etiquetas en 120 |
| 0–≈260 | la nueva: anillo 0 → 1 y escala 1,04 → 1 con `spring.snappy` (ω 22, sin rebote); velo 0,15 → 0 en 160; miniatura 1 → 1,03 en 240 (`ease.enter`); nombre 400 → 700 en 120 |
| 60–260 | check 0,6 → 1 con `ease.emphasis` y opacidad 0 → 1 en 120 |
| 0–280 | si la nueva queda cortada por el borde, la fila se desplaza hasta verla entera + 16 dp (`ease.move`) |

- *Hover* (escritorio): capa de estado 0,04 y miniatura 1,02 en `dur.fast`.
- Un segundo toque durante la animación arranca desde los valores actuales (como la pastilla de las pestañas).
- **Reducir movimiento**: sin escalas; opacidades en 120 lineal; desplazamiento en el acto.
- Fuera del inmersivo la app va a 30 fps: cada animación pide 60 a `FramePacing` mientras dura y la libera al terminar (§0.1).
- Al abrir Workout, las miniaturas ya están (texturas del build): sin fundido de carga.

---

## 9. Presupuesto para 60 fps

### 9.1 El más cargado: Río urbano a las 22 h, nivel Medio, con el maniquí

Base medida en la 7 (lago, Medio, maniquí): **82 *draw calls*, 50 SetPass, 164 k triángulos**. La orilla del lago (pinos, bosque, juncos, pasto, helechos, rocas, cabaña, muelle) se reemplaza por la del río:

| Pieza | *Draw calls* | SetPass nuevos | Triángulos | GPU (estim.) |
|---|---|---|---|---|
| Común (cielo, horizonte, agua, bote, remos, remero, VFX, post) | ≈ 38 (lo de la 7 sin la orilla) | — | ≈ 55 k | ≈ 7,6 ms |
| Terreno del malecón y fondo (`TerrainStreamer`, 9 trozos) | 9 | — (el `Terrain` de la 6a con la capa de adoquín) | ≈ 18 k | 0,8 |
| Muro (módulos instanciados, 2 LOD) | 2 | — (`Prop`) | 3,4 k | 0,1 |
| Fachadas (1 malla por tipo de techo × 3 LOD, pisos y ventanas por *shader*) | 6 | +1 `Facade` | 12,5 k | 0,6 |
| Clubes (3 LOD) | 3 | — (`Facade`) | 2,8 k | 0,1 |
| Faroles (3 LOD) | 3 | — (`Prop`) | 1,7 k | 0,05 |
| Halos, columnas de luz y globos (aditivos, instanciados) | 2 | +1 `Glow` | 0,2 k | 0,3 (sobre-dibujo ≤ 1,3× en ≈ 10 % de la pantalla) |
| Boyas | 2 | — | 0,3 k | — |
| Puente (0–1 visible) | 1 | — | 3,6 k | 0,15 |
| Árboles de vereda (3 LOD) y manzanas | 4 | — (`Foliage`, `Facade`) | 7,7 k | 0,3 |
| Sombras (de día; de noche en Medio no hay) | +8 | — | (≈ 15 k vueltos a dibujar) | 0,3 |
| **Total Río** | **≈ 78 de noche / ≈ 86 de día** | **≈ 44** (sin los *shaders* del pasto, los juncos y los helechos del lago) | **≈ 106 k** | **≈ 10,4 ms** |

- **Topes de la It. 8 (Medio)**: ≤ 95 *draw calls*, **SetPass ≤ 50**, ≤ 220 k triángulos con el maniquí (≤ 270 k con Y Bot), GPU ≤ 12 ms, 0 B por cuadro. Alto: SetPass ≤ 80. Bajo: ≤ 70 *draw calls*, sin sombras ni aditivos de columna (solo halos).
- **Costa** (≈ 74 / ≈ 44 / ≈ 95 k / ≈ 10,2 ms): espuma + faldón + haz (+1 `Foam`, +1 `Glow`); **Regata** (≈ 80 / ≈ 46 / ≈ 100 k / ≈ 10,0 ms): 3 + 3 LOD de boyas y álamos, público 1, texto 1 (+1 `Crowd`, +1 `MarkerText`, +1 variante de tela).
- **Regla de *shaders* por escenario**: a lo sumo **6 pasadas propias de la orilla** (el lago tiene más). Todo objeto nuevo usa `Prop` (color de vértice, ya existe) salvo `Facade`, `Glow`, `Foam`, `Crowd`, `MarkerText` y la variante de tela/aleteo de `Prop`.

### 9.2 Costo de fragmento

| *Shader* | ALU | Texturas |
|---|---|---|
| `Facade` (grilla de ventanas, máscara encendida, ladrillo, vidrio con 30 % del cielo) | ≤ 30 | 1 (ruido) |
| `Glow` (aditivo, caída radial o lineal, ondulación) | ≤ 8 | 0 |
| `Foam` (espuma con latido) | ≤ 25 | 1 (`water.foam`) |
| `Crowd` (atlas + color de ropa) | ≤ 10 | 1 |
| `MarkerText` (SDF) | ≤ 12 | 1 |
| `WaterDegraded` + bandas de cresta (solo Costa) + bajo el puente | 30 + 6 + 2 ≤ 38 (tope 70) | — |

### 9.3 Memoria y CPU

- Texturas nuevas: atlas del público 512 × 256 (9 cuadros de 64 × 128, procedural, horneado al precargar), atlas SDF de 12 glifos 256 × 64 R8, las 4 miniaturas. Total < 1 MB.
- Todo lo que se mueve (boyas, banderas, sombrillas, público, haz, columnas, árboles) se mueve en el vértice con el tiempo de escena: **0 trabajo por objeto y por cuadro en CPU**. Las matrices y los datos por instancia se arman solo cuando cambia el conjunto de segmentos (cada 50 m), ≤ 0,3 ms en ese cuadro y sin asignar memoria.
- Medición: `FrameAllocations` (con control positivo) y el Profiler del build de desarrollo, en el Río a las 22 h y en la Regata a las 12 h.

---

## 10. Tomas que tienen que verse (insumo para el PO y QA)

- **Aceptación visual** (la prueba manual del legacy): 4 escenarios × 6, 12, 18 y 22 h, en TV y TH, a la distancia 1 240 m (la de la 7) y además (distancia de escena; los pernos van 9,5 m adelante): Río a 1 000 m (el puente de 1 070 de frente) y a 1 060 m (el bote bajo el puente), Costa a 470 m (el faro de 530 a 60 m), Regata a 1 230 m (el cartel de 1 250 a 20 m) y a 170 m (la tribuna de 210 a 40 m).
- **Miniaturas**: las 4 y el selector con cada una elegida (TV y W1280), más el cuadro intermedio de la transición (t = 130 ms).
- **Sondas en el `manifest.json`**: las de §5, la ventana encendida (`#FFE082` ± 3) y el frente de la cabaña del lago (issue #9, QA-V6a-08).
- Lo que no puede pasar: objetos flotando, costuras entre bloques de acantilado o módulos de muro, una ventana que cambia de estado mientras se mira, el *skyline* corriéndose de costado, cifras del cartel fuera del tablero, luces encendidas a las 18 h.

---

## 11. Pedidos al Architect

| ID | Prioridad | Pedido | Verificación | ¿Cambia un AC? |
|---|---|---|---|---|
| **DA8-01** | P0 | `RiverArt`, `CoastArt`, `RegattaArt` en `Core/Scene/Art` (como `LakeArt`): todas las medidas, colores y tris de §2–§4; desde el diseño son del Designer | dotnet: constantes = este documento | No |
| **DA8-02** | P0 | Corte de orilla por escenario en `TerrainStreamer` (§2.1, §3.1, §4.1), con la capa de adoquín, arena con franja húmeda y césped con franjas | captura: nada flota; sin costuras en 0, 50 y 4 096 m | No |
| **DA8-03** | P0 | Datos por instancia en `InstanceBatch` (`RenderMeshInstanced` con un `float4` extra: tono, semilla, máscara de ventanas de hasta 24 bits, fase), armados solo al cambiar de segmento | `FrameAllocations`: 0 B en régimen; ≤ 0,3 ms al cambiar de segmento | No |
| **DA8-04** | P0 | *Shader* `Facade`: pisos por alto de objeto (3 m), ventanas de la cara de la cámara con la máscara del legacy (`Random(semilla)`, `< 0,6`, piso por piso) y de la cara del río con la misma secuencia; encendido por `NightLights` | dotnet: máscara = legacy para 20 semillas; captura 22 h | No |
| **DA8-05** | P0 | `HorizonModel` por escenario: *skyline* con `DartRandom(i·31 + 7)` y ventanas exactas (§2.2), acantilado con 20 pasos de 1° y fase (§3.2), bloques de regata cada 26,6° (§4.2); acercamiento del 4 % en Río y Regata, tope de 9° | dotnet: perfiles = legacy en TV ± 1 dp; no se corren de costado | Sí, si AC-SCN-19 pide el desplazamiento lateral del legacy: pasa a "se acercan" (Q-65, PO) |
| **DA8-06** | P0 | *Shader* `Glow` (aditivo instanciado): halos, columnas de luz en el agua, globos, linterna y haz del faro | sobre-dibujo ≤ 1,3× en ≤ 10 % de la pantalla (Río 22 h) | No |
| **DA8-07** | P1 | Oscurecimiento bajo el puente: escalar global `o` de Core según la `z` del bote y el puente (§2.4), aplicado en `RowMateLighting` y en el agua (+2 ALU) | dotnet: `o` continuo; captura a 1 060 m | No |
| **DA8-08** | P0 | Cartel de distancia con SDF de Barlow Semi Condensed Bold (12 glifos: 0–9, espacio, m), quads instanciados, ajuste de §4.3 (alto ≤ 0,62, ancho ≤ 1,60) | dotnet: `"10000 m"` entra en 1,60; captura: legible a 20 m en TV | No |
| **DA8-09** | P0 | Agua y bote de la Costa (D8-06): bandas de cresta en `WaterDegraded` con palabra clave y arfada, cabeceo y rolido de mar de fondo en `BoatMotion`, solo con `waveAmplitude ≥ 0,5`; faldón del mar abierto | Las tomas de la 6a y la 7 iguales byte a byte; dotnet: amplitud 0,8 pasa al agua (`water_painter_test` :27) | No |
| **DA8-10** | P1 | Espuma de orilla (malla por segmento + `Foam`) y gaviotas (posición en Core con las fórmulas de §3.4, aleteo en el vértice) | dotnet: posiciones deterministas; 0 B por cuadro | No |
| **DA8-11** | P1 | Faro sobre promontorio (D8-03): el bloque de acantilado del tramo del faro se dibuja a escala 0,35 sin tocar el generador; haz y destello | `environment_test` sin cambios; captura de noche a 470 m | No |
| **DA8-12** | P1 | Público: atlas procedural de 9 cuadros horneado al precargar, impostores instanciados en las gradas (70 %), poses por vértice | 1 *draw call*; 0 B por cuadro | No |
| **DA8-13** | P1 | Reloj de la torre con la hora de escena (D8-07) y la sala de control encendida de noche (D8-10) | captura 22 h con la hora forzada | No |
| **DA8-14** | P0 | `EnvironmentThumbnailBaker`: 456 × 288 con 4× de supermuestreo, fallbacks procedurales forzados, alcance de 400 m, hora por parámetro, `thumbnails.json` con hash y test de EditMode de frescura, *flag* de línea de comandos | EditMode: hash al día; 2 horneados seguidos iguales byte a byte | Sí: ART_BIBLE §7.6 y ARCHITECTURE §14.5 dicen 304 × 192 (D8-08) |
| **DA8-15** | P0 | Selector (§8): anillo y check como elementos absolutos (sin animar `border-width`), dos etiquetas para el peso, desplazamiento hasta la elegida, `FramePacing` mientras anima, `scene.environment` en las preferencias | PlayMode: 0 relayout por cuadro durante la transición; la elección sobrevive a un reinicio | No |
| **DA8-16** | P1 | Al cambiar de escenario en reposo, precargar el nuevo (mallas y *warmup* de *shaders*) en segundo plano, para que la entrada al inmersivo siga sin tirones (la lección de AC-WKT-13-15) | *driver* `[PERF]`: entrar 1 s después de cambiar de escenario, sin cuadros > 33 ms | No |
| **DA8-17** | P0 | `SceneBudgetTests` por escenario a las 12 y 22 h: Medio ≤ 95 *draw calls*, SetPass ≤ 50, ≤ 220 k triángulos (≤ 270 k con Y Bot); Alto SetPass ≤ 80; y `-rowmate-perf-scene` con el escenario como argumento (Río 22 h es el caso de rendimiento) | PlayMode y build de desarrollo | No |
| **DA8-18** | P1 | Las tomas y sondas de §10 en `capture-snapshots`, con las de la ventana y el frente de la cabaña del lago (cierra el issue #9) | `manifest.json` con las sondas | No |

---

## 12. Para issues

- D-ART-08 se vuelve a ofrecer en el gate: texturas CC0 de fachadas y de los clubes por `MaterialSlot` (nunca en las miniaturas).
- Racks con botes de colores frente a los clubes; baranda de hierro en el malecón (titila a esta escala: probar con tamaño mínimo en pantalla).
- Regata: pontones y torre de largada, números de andarivel, cables entre boyas.
- Faro lejano: el haz visible más allá de los 400 m, en el horizonte.
- Velas y barcos en el mar abierto; gaviotas posadas en el acantilado; patos en el río; tráfico sobre el puente.
- Espuma con partículas en la base del acantilado.
- Reflejos reales de faroles, ventanas y fachadas en el agua (6b, issue #6).
- Público con más variantes o con esqueleto.
- Miniaturas vivas (movimiento lento tipo *Ken Burns*) y *snap* del carrusel.
- Árboles de vereda con copa caduca por estación.
