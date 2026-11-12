# 00 · Biblia visual del agua — It. 6b

| | |
|---|---|
| Iteración | 6b · `06b-agua-avanzada` ([ROADMAP §It. 6b](../../ROADMAP.md)), sobre los **4 escenarios** que ya existen (Lago de la 6a; Río, Costa y Regata de la 8), con el remero y el bote de la 7 |
| Rol | Designer (el QUÉ visual con números; el CÓMO lo cierra el Architect en `02-tech-design.md`) |
| Fuentes | [ART_BIBLE](../../design/ART_BIBLE.md) §3.2, §6, §7.1-7.8 y §9; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §6; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §7.4 y §20; `WaterDegraded.shader`, `WaterMarks`, `Foam`, `Glow`, `Hull`, `WaterArt`, `CoastSwell`, `ShaderArt`; `shaders/water.frag` de [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (contrato F-SCN-12); tomas de la [6a](../06a-escena-lago/snapshots/), la [7](../07-remero-palada/snapshots/), la [8](../08-entornos-selector/snapshots/), la [14](../14-pulido-plataformas/snapshots/) y AUX-15-01 de la [15](../15-deuda-pc/snapshots/) |
| Referencia | El agua de EXR: realista y **tranquila**, el bote es el protagonista. Nada de oleaje dramático: el agua se lee por el reflejo, el brillo y la estela, no por la altura de las olas |
| Convenciones | Mundo del legacy: `x` lateral (+ derecha), `y` altura, `z` hacia adelante; la cámara del contrato a 2,2 m, popa en `z` 5,3, centro del bote 9,5. `D` = distancia de escena (double). `S(a; b; v)` = smoothstep. Hex `#RRGGBB`. Costos en ALU de fragmento D3D11 con la regla de `ShaderArt` (sin sombras de URP ni lo que solo corre en saltos chicos) |

**Decisiones en una línea** (modo rápido; ante la duda, el legacy):

- **DA6b-01** Reflejo: **planar a 1/4 solo en Alto**; en Medio, **cielo analítico + perfil del horizonte + banda de orilla analítica + planar chiquito solo del bote y el remero**; en Bajo, cielo analítico y banda de orilla. **Sin SSR ni sondas** (motivo en §4).
- **DA6b-02** No hay textura de interacción en la 6b (ART_BIBLE §7.7 la pedía): los charcos, hervideros y micro-anillos siguen en los quads instanciados de `WaterMarks`, que ya funcionan y cuestan ≈ 0. La estela es una **cinta de geometría** con historia (§6).
- **DA6b-03** Bajo tiene **agua avanzada liviana** ("Lite", ≤ 35 ALU / 2 tex). La degradada de la 6a queda como respaldo: equipo sin soporte, error de carga o el último escalón térmico.
- **DA6b-04** Las ondas de color y el mar de fondo usan **la fórmula del legacy** (`water.frag:45-53` sin el ruido) con la escala de cada escenario: el casco de la costa ya flota sobre esa misma ola (`CoastSwell`) y así el agua y el bote coinciden.
- **DA6b-05** La niebla del agua es la del legacy (`lerp(col; fog; S(80; 400; z))`, al 100 % a 400 m), aplicada **después** del reflejo y del brillo, como en `water.frag:96`.

---

## 1. Qué se ve hoy (diagnóstico de las tomas)

| ID | Toma | Qué pasa | Qué lo arregla |
|---|---|---|---|
| V6b-01 | 8/CAP-05 a -08 y -22 | El agua es un piso liso de color: no hay ondas, ni reflejo de la orilla, ni del bote | §2 a §4 |
| V6b-02 | 8/CAP-06, -16 y -31 | Las 10 líneas de brillo cruzan el canal casi de borde a borde. En la Regata se confunden con los carriles | Solo en la degradada; en la avanzada el brillo es el camino físico (§5) |
| V6b-03 | 8/CAP-06 y -22 y 15/AUX-15-01 | La popa (5,3 m de cubierta vista a 22° desde la cámara) se lee como una **tabla blanca** a mediodía y gris a la noche (#85) | §9 |
| V6b-04 | 7/CAP-01 y 8/CAP-06 | El bote no "toca" el agua: no tiene línea de contacto, ni reflejo, ni estela | §4.3, §6 y §9 |
| V6b-05 | 8/CAP-16 (río, 22 h) | Los faroles tienen halo, pero el agua de abajo es negra y pareja: faltan las columnas de luz | §7.2 |
| V6b-06 | 8/CAP-22 (costa, 12 h) | Buen color, pero el mar abierto no tiene crestas ni espuma | §7.3 |
| V6b-07 | 8/CAP-07 y -31 (18 h) | El agua toma el rosa del cielo de forma plana: no hay camino dorado | §5 |
| V6b-08 | 8/CAP-03 (TV, 18 h) | Las cordilleras lavadas y el halo del sol que casi no se lee (QA-V6a-12, #3) | §10 |
| V6b-09 | 7/CAP-04 y 15/AUX-15-01 | Los charcos y la salpicadura apenas se ven a 47 dp (#12) | §6.3 |

---

## 2. Arquitectura visual del agua avanzada (orden de las capas)

Un solo shader, `RowMate/Scene/WaterAdvanced`, con tres variantes por nivel: `RM_WATER_HIGH`, la de Medio por defecto y `RM_WATER_LITE`. Va en la cola 2450, opaco, como la degradada. El orden de las capas:

1. **Normal**: B1 analítica (la ola del legacy) + B2 y B3 de los mapas de normales; en la costa, B0 del mar de fondo (§3).
2. **Color de profundidad** (§3.4): `lerp(waterNear; waterFar; S(2; 120; z))` [CONTRATO] × `(1 + h·A·0,25)` [CONTRATO, `water.frag:68`], con la absorción y la franja somera.
3. **Reflejo** según el nivel (§4) y **Fresnel** (§4.1).
4. **Especular del astro y destellos** (§5).
5. **Espuma de crestas** (solo en la costa, en un salto por uniforme, §7.3).
6. **Contacto del casco y sombra** (§9; la sombra como en la 7: × (1 − 0,10) en sombra plena).
7. **Bajo el puente** (`_RmUnderBridge`, como en la 8).
8. **Niebla** `S(80; 400; z)` hacia `fog`, después la bruma baja `m(h)` (como los agregados de la degradada) y la **costura del horizonte**: en los últimos 3 dp sobre la línea del horizonte, `lerp(col; skyHorizon; 1 − dist/3 dp)`.
   **Dónde se mide la costura** (AC-6B-15): la *última fila de agua* es la primera fila de píxeles que queda entera debajo de la línea del horizonte proyectada (`horizonY` de `SceneCamera`, redondeada hacia abajo + 1). La *primera fila de cielo* es la primera que queda entera arriba (redondeada hacia arriba − 1). Las *columnas* son solo las que `HorizonProfile` da en 0, es decir, sin cordillera, fachada, acantilado ni orilla a menos de 1 dp sobre el horizonte. Ahí el agua toca el cielo o la franja de bruma (DV-66). Nunca se mide sobre una silueta, que tiene que contrastar (AC-6B-04). Si quedan menos de 32 columnas, se usa además la pasada sin siluetas (02-tech-design §8).
   *Motivo (gate G6b-04):* la sonda anterior comparaba también contra las siluetas oscuras y no fijaba las filas. Lo escribe el Architect a pedido del gate, con los valores del contrato. El Designer puede ajustarlo en `04-design-notes`, sin medir sobre siluetas.

Lo que va aparte: la estela (una cinta, `WaterWake`, §6.1), los charcos, los hervideros y los micro-anillos (`WaterMarks`, §6.2), las columnas de luz (`Glow` tipo 3, §7.2) y la espuma de la orilla (`Foam`, #22).

---

## 3. Olas: bandas de frecuencia por escenario

### 3.1 Las bandas

| Banda | Qué es | Técnica | Dónde |
|---|---|---|---|
| **B0 · mar de fondo** | La ola del legacy con desplazamiento real: `y = 0,15·h` (la misma `HullScale` del casco) | En el vértice, con su normal analítica. Se apaga de 120 a 300 m (`CoastArt.SwellFadeNear/Far`) | Solo la Costa |
| **B1 · ondulación del legacy** | `h = sin(z_w/s + 0,6t)·0,6 + sin((0,7·z_w + 1,3x)/s + 0,9t)·0,4` (dos senos, sin el ruido) | Modula el color (contrato) y la normal por su derivada analítica, con la pendiente `k₁` del escenario | Todos los escenarios |
| **B2 · chop** | Las ondas de viento de 1 a 4 m | Mapa de normales (canales RG), tile `T₂`, deriva `v₂` en la dirección `θ₂` | Todos |
| **B3 · rizo** | El rizado fino que hace titilar el sol | El mismo mapa, en los canales BA, con tile `T₃`, deriva `v₃` y dirección `θ₃` | Todos (en Lite no) |
| **B4 · micro** (solo Alto) | El rizo en el agua cercana | B3 otra vez, a 0,4 × `T₃` y girado 37°, solo en `z < 25 m` | Alto |

El ruido de valor del legacy (dos octavas) **se reemplaza** por B2 y B3: es lo mismo a la vista y no cuesta un hash por píxel. La regla de ART_BIBLE §9 es "ruido por textura".

### 3.2 Parámetros por escenario

`θ` es el ángulo de **hacia dónde viaja** la onda, medido desde +z (el sentido de avance) y positivo hacia +x. 180° significa que la onda viene hacia la cámara. Las velocidades de B1 salen del legacy: la fase avanza `0,6·t`, así que la cresta se mueve hacia −z a `0,6·s` m/s.

| | **Lago** "Espejo" | **Río** "Corriente" | **Costa** "Mar" | **Regata** "Pista" |
|---|---|---|---|---|
| Legacy (amplitud A / escala s) | 0,25 / 2,5 | 0,35 / 2,0 | 0,8 / 4,0 | 0,3 / 1,5 |
| B0 | — | — | λ 25,1 m (a lo largo de z) y 17,0 m (a 61,7°); `y` ≤ ± 0,15 m; 1,5 m/s hacia la cámara | — |
| B1: λ₁ / λ₂, velocidad y pendiente `k₁` | 15,7 / 10,6 m · 1,5 m/s · **0,05** | 12,6 / 8,5 m · 1,2 m/s · **0,07** | la de B0 (la normal sale de la geometría) | 9,4 / 6,4 m · 0,9 m/s · **0,03** |
| B2: tile, deriva, θ y fuerza | 3,5 m · 0,25 m/s · 200° · **0,30** | 2,0 m · 0,45 m/s · 180° (con la corriente) · **0,45** | 4,0 m · 0,50 m/s · 200° · **0,55** | 1,5 m · 0,15 m/s · 190° · **0,15** |
| B3: tile, deriva, θ y fuerza | 1,3 m · 0,12 m/s · 160° · **0,20** | 0,8 m · 0,30 m/s · 135° · **0,30** | 1,5 m · 0,25 m/s · 230° · **0,35** | 0,6 m · 0,08 m/s · 165° · **0,10** |
| Parches de calma ("viento" a 40 m, del `_RmNoiseTex`) | × lerp(0,35; 1; n): calmas tipo espejo | × lerp(0,7; 1; n) | — | × lerp(0,5; 1; n) |
| Rugosidad GGX (α) | 0,05 | 0,10 | 0,14 | 0,04 |
| Fresnel máximo | 0,90 | 0,75 | 0,80 | 0,92 |
| Carácter | Calmo con calmas de espejo: refleja la orilla y las montañas | Corto y picado, corre hacia la cámara, un poco turbio | Mar de fondo largo con crestas que rompen en espuma | Planchada, casi un espejo, con los carriles reflejados |

**Desvanecido por distancia** (antialias: lo que mide menos de un píxel titila): B3 × `1 − S(25; 80; z)`, B2 × `1 − S(60; 250; z)`, B4 × `1 − S(10; 25; z)`. Más lejos quedan B1 y el Fresnel, que es el espejo. Los mapas van con *mipmaps* y filtro anisotrópico 2×.

**El mapa de normales** (slot `water.normal`, procedural): una textura de 512² RGBA8 **lineal**, sin costura. RG es el chop (suma de 12 senos de λ entre 1/8 y 1/2 del tile, con el espectro de Phillips recortado) y BA el rizo (16 senos de λ entre 1/32 y 1/8). La hornea el Editor con una semilla fija, como las miniaturas, y entra en la huella de `thumbnails.json`. Son **1 textura y 2 lecturas**.

### 3.3 Sin el salto a 4096 m (Q-68)

El legacy acota la distancia a módulo 4096 y el patrón salta. Acá nada del shader ve `z` de mundo: trabaja en **cinta** (`relZ = z − D`, con `|relZ|` ≤ 750 m, donde un float tiene precisión de 0,06 mm) y recibe lo que depende de `D` ya reducido en double por Core:

- Las fases de B0 y B1: `φ₁ = Mod(D/s + 0,6t; 2π)` y `φ₂ = Mod(0,7·D/s + 0,9t; 2π)`. Es `CoastSwell.Phases`, generalizado a los 4 escenarios.
- Los corrimientos de los mapas: `o = Mod((D·ẑ + v·t·θ̂)/T; 1)` por banda. Al dar la vuelta salta un tile entero, y como el mapa no tiene costura, no se nota.
- El ruido de los parches y de la espuma: `Mod(D/256; 1)` (el período de `_RmNoiseTex`), como ya hace `Foam`.
- La malla se ancla a la grilla del mundo con `Mod(D; 16 m)`, el paso del anillo más grueso. Así los vértices no "nadan".

Pruebas (dotnet): el alto y la normal en cinta coinciden con la fórmula de mundo (± 1e-4) en `D` = 0; 4095,9; 4096; 4096,1; 10 000 y 100 000 m. Entre dos cuadros seguidos, el corrimiento de la textura cambia menos de 0,01 tile (salvo la vuelta entera). Prueba visual: las tomas a `D` = 4095,98 y 4096,02 difieren menos que el ruido de dos tomas iguales.

### 3.4 Color de profundidad

- **Base** [CONTRATO]: `lerp(waterNear; waterFar; S(2; 120; z)) · (1 + h·A·0,25)`. El tinte de la costa (`#0E6B7A` al 30 %) ya viene en la paleta.
- **Absorción** (mirando hacia abajo, cerca del bote): `col · (1 − 0,18·(1 − F)·(1 − S(3; 25; z)))`: más oscuro y saturado bajo la cámara, donde el Fresnel es bajo.
- **Franja somera**: la de la degradada (`#5A6656` × ambiente al 25 %, de |x| 8,4 a 9,0). En el Lago se suma un turquesa `#2E8C8A` al 15 % de |x| 7,5 a 8,6. No va del lado del mar abierto (costa, `x > 0`).
- **Turbidez del Río**: `lerp(col; #3E5A3A·ambiente; 0,20)` antes del reflejo.

---

## 4. Reflejo del cielo, la orilla, el bote y el remero

### 4.1 Fresnel

Schlick con F0 = 0,02 sobre la **normal perturbada**, recortado al Fresnel máximo del escenario (§3.2). Con la normal plana, a la altura del contrato (2,2 m) da 0,05 a 2 m, 0,57 a 20 m y 0,90 a 100 m. El legacy (`(1 − cos)³·0,6`) daba 0,42 a 20 m. El nuestro refleja más lejos porque además hay orilla para reflejar. `col = lerp(base; reflejo; F)`.

### 4.2 Técnica por nivel (con su costo)

| Técnica | Qué refleja | Costo estimado | Veredicto |
|---|---|---|---|
| **Planar a 1/4** (½ × ½ de la resolución), con plano oblicuo en y = 0 | Todo: cielo, nubes, estrellas, montañas, orilla, puente, fachadas, bote, remero y luces | +12-15 SetPass, +25-40 *draw calls*, ≈ 0,8-1,2 ms de GPU en escritorio | **Alto** |
| **Cielo analítico**: el telón del legacy (`lerp(skyHorizon; skyTop; 1 − (1 − q)²)`) evaluado en la elevación del rayo reflejado | El cielo, con la paleta del cuadro (sin nubes ni estrellas) | ≈ 6 ALU, 0 texturas | **Medio y Bajo** |
| **Perfil del horizonte**: una textura de 256 × 1 (R8: la elevación del tope por acimut de −65° a +65°, con el tono en la tabla de su cordón) | Cordilleras, skyline, acantilado lejano y tribunas lejanas | ≈ 8 ALU, 1 textura | **Medio** |
| **Banda de orilla analítica**: el rayo reflejado cruza `\|x\| = x_s` a una distancia `t`; si su elevación es menor que `atan(H_s/t)`, toma el tono de la orilla. En el Río, el puente es un tramo entre 6,0 y 7,7 m de alto en la `z` del puente, con el ojo del arco abierto | Árboles, muros, fachadas y el puente | ≈ 12 ALU, 0 texturas | **Medio** y **Bajo** (en Bajo sin el puente) |
| **Planar chiquito del bote**: una cámara espejada que dibuja solo las capas Boat, Rower y Oars a 256 × 128 | Casco, remos y remero (#13) | +3-4 SetPass, +6 *draw calls*, ≈ 0,1-0,2 ms | **Medio** |
| SSR | Lo que está en pantalla | 1,5-2,5 ms en móvil; pide el color y la profundidad opacos | **No**: la orilla sale por los bordes de la pantalla (justo lo que más se ve reflejado), falla en los ángulos rasantes y el HUD tapa la mitad |
| Sondas o cubemap por hora | El cielo | Hornear 4 escenarios × N horas y mezclarlos | **No**: la hora es continua y se desincroniza de la paleta (AC-SCN-09-04: una sola paleta). El cielo analítico es exacto y gratis |

**Parámetros de la banda de orilla por escenario** (`x_s`, `H_s` y tono; el tono × la luz de la hora, que lo da Core):

| Escenario | Izquierda | Derecha |
|---|---|---|
| Lago | 14 m · 9 m · `#2F4A3A` (bosque) | 12 m · 3 m · `#4F7A3A` × 0,8 (pradera) |
| Río | 13 m · 14 m · `#78909C` × 0,85 (fachadas); muro de 9 a 1,2 m · `#9E9E9E` | igual |
| Costa | 14 m · 8 m · `#5D4A42` (acantilado) | — (mar abierto) |
| Regata | 35 m · 12 m · `#2F4A3A` (álamos); talud de 9,8 a 1,0 m · `#4F7A3A` | igual |

**La distorsión del reflejo**: `uv += N.xz · k_d · S(0,5; 3; z)` (nunca en la línea de contacto), con `k_d` = 0,035 en el Lago, 0,05 en el Río, 0,06 en la Costa y 0,025 en la Regata. El planar de Alto se lee en el mip `rugosidad × 4`: refleja nítido en la Regata y desenfocado en la Costa. En los dos métodos analíticos, el borde de la banda se ablanda con la misma distorsión: `±(N.x·k_d·8)` en la elevación.

**Qué dibuja el planar de Alto**: Sky, Horizon, Terrain (el segmento cercano), Props en LOD1, Glow y Boat/Rower/Oars. **No** dibuja el pasto, los juncos, las rocas chicas ni las partículas. Recorte lejano en 600 m, sin sombras ni post. Si el SetPass de Alto pasa de 80, lo primero que sale son los pinos a más de 150 m. **No se sube el tope.**

### 4.3 Reflejo del bote y del remero (#13)

- En Alto viene en el planar; en Medio, del planar chiquito. Se ve × 0,85 (el agua absorbe), con la distorsión de B2 y B3 (la de B1 al doble cerca del casco: el reflejo "se quiebra" con la palada).
- **Línea de contacto** (todos los niveles avanzados): donde el agua toca el casco (`|x|` < manga/2 + 0,06 m, entre la popa y la proa), `col × 0,75` con caída de 0,06 m. El bote deja de "flotar sobre" el agua (V6b-04). ≈ 4 ALU.
- En Bajo no hay reflejo del bote. Quedan la línea de contacto y la sombra del óvalo del legacy, como en la 7.

---

## 5. Brillo del astro: columna de luz y destellos

**Especular**: GGX con la α del escenario sobre la normal perturbada. Dirección = la de la luz direccional del cuadro; el disco y la luz tienen el mismo acimut (ART_BIBLE §6.2). Intensidad = `sunColor · uLight · vis` (uLight del legacy: 0,2-1,0 de día y 0,45 de noche). **Destellos**: `pow(saturate(R·L); e_g) · g_g · máscara`, con la máscara = `S(0,55; 0,95; B3)` (solo en las facetas del rizo). Tope: el especular más los destellos se recortan en 1 (sin tonemapping) y el *bloom* de la hora hace el resplandor. Así la columna aparece sola, bajo el astro, por física. Como piso de paridad, en la Lite se dibuja **la columna del legacy** (`exp(−(Δx/ancho)²)·0,4`, ancho = 25 px + 4 px·z, solo en las crestas de B1).

**Dónde cae el punto espejo** (`z = 2,2 / tan(elev)` con la elevación del disco): a las **12 h** el disco está a 12° y el punto cae en ≈ 10 m, **justo en el bote** (z 9,5). Los destellos rodean el casco y la proa los corta. Es la foto de EXR al mediodía.

| Hora | Astro (ART_BIBLE §6.2) | Punto espejo | α + | `e_g` / `g_g` | Color | Qué se ve |
|---|---|---|---|---|---|---|
| **6 h** | sol, acimut −17,5°, bajo | lejos (≥ 25 m) | +0,02 | 600 / 1,1 | `sunColor` cálido | Camino dorado largo hacia el horizonte, un poco a la izquierda. La bruma baja (0,55·m) lo apaga lejos |
| **12 h** | sol, 0°, 12° | ≈ 10 m | 0 | 900 / 1,0 | blanco cálido | Destellos alrededor del bote. El reflejo del cielo `#8FD3FF` lejos; la orilla oscura reflejada a los costados |
| **18 h** | sol, +17,5°, bajo | lejos (≥ 25 m) | +0,03 | 600 / 1,3 | naranja | Camino largo a la derecha que "encandila un poco" (*bloom* 0,75 / 1,0 de la tabla de la hora). El rosa del cielo reflejado ya no es plano: lo cortan la orilla y el rizo (V6b-07) |
| **22 h** | luna, acimut espejado, 10,6° | ≈ 12 m | −0,01 | 1 200 / 0,45 × iluminación | `#E6EEFF` | Camino plateado fino. Los faroles y las ventanas dan columnas (§7.2). Cielo casi negro reflejado: el agua se lee por el brillo, no por el color |

En el cambio de astro (19:34:19 y 4:25:41) `vis` = 0: el brillo se apaga y vuelve sin saltos, como las líneas de la degradada.

---

## 6. Estela, charcos y micro-anillos

### 6.1 Estela: una cinta con historia (`WaterWake`)

- **Historia** (Core, sin asignaciones): la posición de mundo de la popa cada 0,25 s, 48 puntos (12 s). Sale de la `D` en double y se dibuja en cinta.
- **Geometría**: 3 cintas (los dos brazos y el centro) × 48 segmentos ≈ 300 triángulos, **1 dibujo**, cola 3040 (antes de `WaterMarks`), sin profundidad.
- **Forma (contrato del legacy)**: los brazos con semiancho `0,3 + 0,35·retroceso` (pendiente 0,35 = 19,3°, el ángulo de Kelvin), espuma máxima **35 %** e intensidad `clamp(v/4; 0; 1)`. El EXR que se suma: cada punto conserva **su** intensidad al nacer (las aceleraciones dejan marcas), y la `v` incluye el *surge* de la palada (F-SCN-03, la velocidad del casco de la 7), así la estela "late" con cada drive (#13).
- **Vida** (edad `a` en s): el ancho de la línea de cada brazo va de 0,35 a 1,2 m (`0,35 + 0,07·a`); el centro turbulento, de 0,5 a 2,5 m. Alfa = `0,35 · I₀ · e^(−a/4) · n`, con `n` = 0,6-1,0 del `_RmNoiseTex` a 0,7 m, corrido por la fase de Core. A los 12 s vale < 0,02 y el punto se recicla.
- **Color**: `#F4F6F2` × la luz de la hora (la normal hacia arriba), con la niebla del contrato. De noche queda ≈ 0,25 de luminancia, plateado con la luna.
- **Desde la cámara de persecución** (z 0,5 a 5,3) se ve igual que en el legacy (nace en la popa y se apaga hacia la cámara). Los 12 s se ven en la toma aérea del final y en el vuelo de la entrada: una V de ≈ 48 m a 4 m/s.
- **Con la pausa**: el bote frena con inercia, `I₀` → 0 y lo que ya existe envejece y se apaga.
- **Costo**: ≤ 20 ALU y 1 textura.
- **En la degradada no hay estela** (AC-SCN-13-04, como el legacy). Al caer al degradado, la cinta se apaga en 0,6 s.

### 6.2 Micro-anillos (#13), en `WaterMarks` tipo 3

- **Gotas de la recuperación** (las que suelta `Spray` al tocar el agua): radio `0,05 + 0,35·a` m, ancho del anillo `0,02 + 0,03·a`, vida 1,2 s, alfa `0,5·(1 − a/1,2)²`.
- **Al ataque** (*catch*): 2 anillos por pala, de 0,15 a 0,9 m en 1,0 s.
- **Color**: no es espuma. Es la pendiente del anillo que **refleja el cielo**: `lerp(agua; skyHorizon·1,15; alfa)`. De noche, la luna.
- **Tope**: 24 vivos, en el mismo dibujo instanciado. El ancho mínimo en pantalla es de 1,0 dp, con el alfa compensado por el área (la regla de `Spray`).

### 6.3 Charcos y hervidero visibles a 47 dp (#12)

- **Charco**: el radio de Core × 1,3 en el quad (no cambia el dato) y alfa mínimo 0,35 en la media luna de espuma hasta los 2 s de edad.
- **Par de remolinos**: girados según el eje de la pala en el *finish* (≈ 40° respecto de x), con el giro del quad en `WaterInteractionRenderer`.
- **Hervidero**: + 25 % de alfa.
- **CAP-04 de la 7**: se toma en TH con la escena sola, la fase forzada y el par a 1,0 s de edad. El chequeo proyecta el charco a la pantalla.

---

## 7. Por escenario

### 7.1 Lago (inspiración Bled)

Agua calma con parches de espejo (§3.2). Refleja el bosque de la izquierda (banda de 9 m), la pradera, las cordilleras (perfil) y la nieve. A las 6 h, la bruma baja sobre el agua; a las 22 h, la ventana de la cabaña da una columna de 0,3 × 5 m al 15 %.

### 7.2 Río (Henley)

- **Agua**: chop corto, corre hacia la cámara y un poco turbia.
- **Reflejos**: las fachadas y el muro en la banda; el puente en Medio, con el ojo del arco abierto. Bajo el puente, −40 % (ya existe).
- **Columnas de luz** (#31, `Glow` tipo 3), en todos los niveles avanzados y también en Alto (el emisivo chico del planar se pierde a 1/4):
  - **Farol**: un quad emisivo aditivo sobre el agua, desde el pie del muro hacia la cámara, de 0,5 m de ancho × 8 m de largo, `#FFE082` con alfa 0,25 → 0 a lo largo. Lo cortan franjas horizontales que se mueven con B2: `0,6 + 0,4·sin(z·2π/0,9 + fase)`.
  - **Ventanas encendidas** a menos de 60 m: 0,3 × 4 m al 12 %, como mucho las **16** más cercanas.
- **Noche**: las columnas, más el skyline reflejado del perfil (sus ventanas no: el perfil es solo silueta).

### 7.3 Costa

- **Mar de fondo** B0 (el bote sube y baja con la misma ola, `CoastSwell`) y los dos tamaños de chop. Refleja el acantilado a la izquierda; a la derecha, cielo puro hasta el horizonte.
- **Espuma de crestas** (#31): `espuma = S(0,55; 0,85; h) · S(0,45; 0,75; ruido(3 m, corrido 0,3 m/s))`, `#F4F6F2` × la luz, **máximo 0,45**, × `1 − S(15; 150; z)`. Más lejos la reemplaza un aclarado de 0,05 (subpíxel). En un salto por uniforme: ≤ 8 ALU, el `_RmNoiseTex` que ya está.
- **Faro**: su haz (ya existe) suma de noche una columna de 1,2 × 25 m al 10 %, cuando apunta a ± 20° de la cámara.
- **Espuma de la orilla**: la de la 8, con `Foam` bajado a ≤ 30 ALU (#22). El latido y los extremos de la franja pasan al vértice.
- **Boyas**: la costa no tiene boyas del legacy. Las del río, las de la regata y la de 500 m (P-06) suben y bajan 0,02 m a 0,5 Hz sobre B1. Lo de #31 "que siguen la ola en la costa" se cubre con el casco; no se agregan boyas.

### 7.4 Regata (Bosbaan)

Agua planchada, casi un espejo (Fresnel máximo 0,92, α 0,04): refleja los álamos (banda a 35 m), las boyas (en Alto, el planar; en Medio, un punto de color bajo cada boya cercana con el mismo quad de `Buoy`, 0 dibujos extra) y las tribunas (perfil). A las 18 h, el camino naranja corre entre los carriles: **no** hay líneas que se confundan con los carriles (V6b-02).

---

## 8. Vidrio con desenfoque del HUD y del diálogo (P-04, F-WKT-17)

| Parámetro | Valor |
|---|---|
| Fuente | El color de la cámara de la escena **después del post** y antes de la UI. Los *scrims* entran (son de la escena, DV-67) |
| Cadena | Bajada a ½ (4 lecturas bilineales) → bajada a ¼ (4 lecturas) → **3 pasadas Kawase** a ¼, con corrimientos de 1,5, 2,5 y 3,5 texeles. Ping-pong RGBA8 |
| Resolución por densidad | Con densidad ≥ 2 (teléfonos): ¼ por eje (TV a 0,85: 230 × 510). Con densidad < 2 (escritorio W1280): ½ por eje, así el radio es parecido en dp |
| Radio efectivo | **≈ 10-12 dp** (≈ 7 texeles de ¼ más el filtro de la bajada). Las cifras de atrás no se leen y las formas grandes (bote, orilla, sol) sí |
| Costo | Estimado en Medio: **0,25-0,35 ms** (≈ 120 k píxeles × 4 lecturas × 5 pasadas). Tope **0,6 ms** (ARCHITECTURE §20.1). Memoria ≈ 2,3 MB |
| Cuándo | Solo con algún vidrio a la vista sobre la escena (todo el entrenamiento). Con el diálogo de completado, igual |
| Material | `RowMate/UI/Glass` de ART_BIBLE §3.2 sin cambios en los números: tinte `#070D16` con alfa `lerp(0,72; 0,86; S(0,10; 0,70; L))`, borde 1 px `#FFFFFF @10 %`, **reflejo interior `@6 % → 0` en los 8 dp de arriba** (llega a todas las piezas a la vez: tarjetas, banda, barra, paneles y controles) y el glow por fuera |
| Tramado | ± 0,5/255 Bayer 4 × 4 sobre la muestra (el cielo de la noche se bandea con el desenfoque) |
| Diálogo de completado (P-19) | El mismo vidrio. Detrás, la toma aérea: el bote frena **desenfocado** mientras la cámara sube. Sin velo extra |
| Elementos | **0 nuevos**: el HUD está en 56-57 de 60. El vidrio es el material de los fondos que ya existen |
| Respaldo | Bajo, degradada, o sin `_RmHudBlur`: `glass.fallback` `#070D16 @86 %` plano y **sin** el reflejo interior. Hoy es igual |
| Contraste | La prueba de la 6b: el compuesto nunca supera la luminancia del vidrio sobre blanco (§3.2), medida sobre `_RmHudBlur` real en las 32 tomas |

---

## 9. La popa del casco (#85)

- **Cubierta de popa**: degradé del gelcoat de `#F5F5F5` en el *cockpit* a `#C9D1D6` en la punta (lineal en los últimos 2,5 m), por color de vértice.
- **Forma**: los últimos **0,6 m** se afinan de la manga de borda (0,33 m) a 0,06 m en la punta, y la cubierta baja 0,03 m (la aleta de la popa).
- **Fresnel del gelcoat en la cubierta** (`N.y > 0,8`): recortado a 0,15. Hoy el ángulo rasante (≈ 22° desde la cámara) lo lleva al blanco.
- **Junto con el agua avanzada**: la línea de contacto (§4.3), el reflejo del casco y la turbulencia central de la estela, que nace en la popa. Con eso la popa se lee como casco y no como tabla.
- **Medida**: en la toma lago 12 h W1280 (la de AUX-15-01 con el casco), la luminancia media de la popa ≤ 0,82 (hoy ≈ 0,95) y el borde lateral de la popa con contraste ≥ 1,15:1 contra el agua.
- **Fuera**: #19 (la popa debajo de la zona del bote en TH) es de composición y no entra.

---

## 10. Deuda de la 6a (primero, antes de construir)

| ID | Qué | Arte |
|---|---|---|
| QA-V6a-PAR-01 (#2) | La 11.ª línea de la degradada | `n = clamp(round(...); 0; 9)` en `WaterDegraded`: `t` máximo = 0,9 + fase < 1. `WaterArt.MaxAlu` 27 → 28. Sonda en el manifest que cuente 10 líneas. AC-SCN-13-03 vuelve a PASA |
| QA-V6a-12 (#3) | Montañas lavadas y tapadas; halo de las 18 h | Sin tocar los colores del contrato ni DV-65: el contraste de los flancos `FlankToSun` 0,12 → **0,18** y `FlankAway` −0,10 → **−0,16**; `BaseFog` 0,15 → **0,10**. En TH, la envolvente izquierda `LeftEnvelopeSlope` 0,20 → **0,14** (el bosque cercano baja y deja ver el cordón). El brillo del cielo alrededor del sol `GlowStrength` 0,35 → **0,45** cuando el dorado `g` > 0,5 (las 18 h y las 6 h). Los halos del legacy no cambian |
| QA-V6a-R3-01 y R2-04 (#1) | El tirón de la entrada antes de que termine la precarga | Del Architect. Al arte le toca que el **calentamiento de las variantes de `WaterAdvanced`** (3 niveles × sombras) vaya partido en las unidades de la precarga, sin ninguna en un cuadro visible |
| — | `DeviceScreenView.Render` con el inmersivo arriba | Del Architect (no repintar lo tapado). Sin arte |

---

## 11. Niveles y caída al degradado

| | **Alto** | **Medio** (paridad) | **Bajo** ("Lite") | **Degradada** (6a) |
|---|---|---|---|---|
| Bandas | B0-B4 | B0-B3 | B0 (costa) + B1 + B2 | Ninguna (la ola de color de la costa, como en la 8) |
| Reflejo | Planar ¼, mips | Cielo analítico + perfil + banda + planar del bote | Cielo analítico + banda (sin puente) | Agregado leve de `skyHorizon` |
| Brillo | GGX + destellos + B4 | GGX + destellos | Columna del legacy en las crestas | Las 10 líneas |
| Estela / anillos / charcos | Sí / sí / sí | Sí / sí / sí | Sí / no / sí | No / no / sí (contrato) |
| Espuma de crestas (Costa) | Sí | Sí | No | No |
| Columnas de luz | Sí | Sí | Sí (las 8 más cercanas) | Sí (son `Glow`, independientes del agua) |
| Vidrio | Desenfoque | Desenfoque ≤ 0,6 ms | Plano 86 % | Plano 86 % |
| Fragmento (tope) | ≤ 110 ALU / 6 tex | **≤ 70 ALU / 5 tex** | ≤ 35 ALU / 2 tex | ≤ 28 ALU / 0 tex |
| GPU agregada estimada | +2,0-2,5 ms en escritorio | +1,3-1,8 ms en el teléfono de gama media | +0,3 ms | 0 |

**Reparto en Medio (≤ 70 ALU)**: base y B1 8 · normal B1 6 · mapas B2/B3 8 · Fresnel 4 · cielo analítico 6 · perfil y banda 12 · planar del bote 3 · GGX y destellos 12 · contacto 4 · niebla, bruma y costura 6 = **69**. En saltos aparte: la espuma de la costa (≤ 8) y bajo el puente (el `UnderBridgeBranchAlu` que ya existe). Texturas: el mapa de normales, el perfil, el RT del bote, el ruido (solo en la costa) y la sombra de URP.

**Elección y caída** (F-SCN-13):

1. **Al arrancar**, el nivel lo elige `GraphicsTier`. Si `WaterAdvanced` no está soportado o su carga falla, el agua es la **degradada por el resto de la sesión**, sin reintentar. El log dice el texto del legacy: `[Scene] Shader de agua no disponible, usando fallback: <error>`. No se muestra nada al usuario.
2. **Si el agua todavía no está lista** al abrir la escena (precarga en curso): se ve la degradada y pasa a la avanzada sola, con un **fundido de 0,6 s** (la degradada se dibuja encima con el alfa de 1 a 0; un solo cuadro paga los dos).
3. **Térmico** (`ThermalAdapter`, el orden del arte): resolución → reflejo (Alto: planar → el de Medio; Medio: sale el planar del bote; cada cambio con un fundido de 0,6 s del peso del reflejo) → distancia de sombras → vegetación → nivel → **nuevo último escalón: agua degradada**. Ahí se queda hasta la próxima sesión: fundido de 0,6 s y la estela que se apaga en 0,6 s. **La sesión nunca se corta** y las métricas no se tocan.
4. Las **miniaturas** del selector siguen con la degradada (F-SCN-22, legacy).

---

## 12. Aceptación visual (set `it6b`)

- **4 escenarios × 6, 12, 18 y 22 h × TV y TH** = 32 tomas (las mismas cámaras que el set `it8`), en Medio.
- Lago a las 12 h en Alto, Medio, Bajo y degradada forzada (4).
- Lago a `D` = 4095,98 y 4096,02 (2).
- La toma aérea con el diálogo (1).
- Popa en W1280 (1).
- Columnas del río a las 22 h (1).
- Crestas de la costa a las 12 h en TH (1).
- **Videos**: la estela con el *surge* (TV) y la caída al degradado sin cortar (TH).

**Sondas** (en el manifest):

- Camino de brillo a las 12 h: ≥ 30 píxeles con luminancia > 0,85 en ± 3 m del bote.
- Orilla reflejada en el Lago a las 12 h: la franja del agua a 20-40 m bajo el bosque, ≥ 15 % más oscura que el centro del canal.
- Costura del horizonte: `|ΔL|` ≤ 0,02 entre la fila del horizonte y la de abajo, solo en las filas y columnas de §2.8 (informativa; manda el ≤ 6/255 por canal de AC-6B-15).
- Estela a 4 m/s: hay píxeles con espuma > 0,2 entre z 1 y 5.
- Las 10 líneas de la degradada.
- Popa ≤ 0,82.
- El compuesto del vidrio ≤ la luminancia del vidrio sobre blanco.

---

## 13. Pedidos al Architect

| ID | Pedido | Issues |
|---|---|---|
| PA6b-01 | Shader `RowMate/Scene/WaterAdvanced` (Alto, Medio y Lite) con las capas de §2 y el reparto de §11. Las entradas de `ShaderArt`: Alto 110/6, Medio 70/5, Lite 35/2, y la prueba de ALU en `SceneShaderTests` | #6 |
| PA6b-02 | La malla en anillos centrada en la cámara: 40 × 40 m con paso de 0,5 m (1 m en Bajo), anillos que duplican el paso, borde a 1,5 km, ≤ 20 k triángulos, anclada con `Mod(D; 16)` en double. B0 en el vértice solo en la costa | #6, #31 |
| PA6b-03 | Core: `WaterProfile` por escenario (la tabla de §3.2 en `WaterArt`) y `WavePhases` (las fases de B0/B1 y los corrimientos de B2, B3 y el ruido en double, con las vueltas) y sus pruebas de §3.3 | #6 (Q-68), #31 |
| PA6b-04 | Mapa de normales procedural de 512² RGBA8 lineal sin costura (slot `water.normal`), horneado en el Editor con semilla fija y en la huella | #6 |
| PA6b-05 | El perfil del horizonte, 256 × 1 por escenario, desde la tira de Core; se rehace cuando las cordilleras se acercan (cada 50 m, sin asignar memoria) | #6, #31 |
| PA6b-06 | Las uniformes de la banda de orilla por escenario (§4.2) y el puente más cercano para el reflejo (de `_RmUnderBridge`) | #31 |
| PA6b-07 | El planar de Alto (½ × ½, mips, plano oblicuo, capas, 600 m, sin sombras ni post, tope de SetPass 80 con el recorte de §4.2) | #6, #13, #31 |
| PA6b-08 | El planar chiquito del bote en Medio (Boat, Rower y Oars a 256 × 128, cámara espejada), contado dentro de los 50 SetPass. Si no entra, se pide antes de cambiar el tope | #13 |
| PA6b-09 | La estela: la historia en Core (48 puntos cada 0,25 s, con `I₀` por punto que incluye el *surge*), la cinta y el shader `WaterWake` (≤ 20 ALU / 1 tex); se apaga en 0,6 s al caer al degradado | #6, #13 |
| PA6b-10 | `WaterMarks` tipo 3 (micro-anillos, ≤ 24 vivos) y los ajustes de #12: escala 1,3, alfa mínimo 0,35, el par girado al eje de la pala y la toma CAP-04 | #13, #12 |
| PA6b-11 | `Glow` tipo 3: columnas de luz de faroles, ventanas (16), cabaña, torre y faro; franjas con la fase de B2 | #31 |
| PA6b-12 | La espuma de crestas de la costa en un salto por uniforme, y `Foam` ≤ 30 ALU con el tope de nuevo en 30 | #31, #22 |
| PA6b-13 | `WaterModeSelector` en Core (puro): con el soporte, el nivel, el estado térmico y "listo", da Avanzada, Lite o Degradada; el fundido de 0,6 s; sin reintento después de un fallo; el log del legacy. El escalón térmico nuevo al final del orden | #6 (F-SCN-13) |
| PA6b-14 | `HudBlurFeature` con los números de §8 (la cadena, la resolución por densidad, el tramado, solo con vidrio a la vista) y la medición ≤ 0,6 ms en Medio con un marcador de GPU; el material de vidrio en los fondos existentes (0 elementos nuevos); el diálogo de completado | #6 (P-04, P-19) |
| PA6b-15 | Deuda: `WaterDegraded` con `n ≤ 9` y `MaxAlu` 28, y la sonda de las 10 líneas | #2 |
| PA6b-16 | Deuda: los números de las montañas y del brillo del cielo de §10 en `LakeArt` y `SkyArt`, y sus pruebas | #3 |
| PA6b-17 | Deuda: el calentamiento de las variantes de `WaterAdvanced` partido en las unidades de la precarga; el *driver* `[PERF]` que entra antes de que esté lista (AC-WKT-13-15); `DeviceScreenView.Render` con el inmersivo arriba | #1 |
| PA6b-18 | La popa: el degradé por color de vértice, el afinado de los últimos 0,6 m en `HullMesh` y el Fresnel de la cubierta en 0,15, con la sonda de §9 | #85 |
| PA6b-19 | La línea de contacto del casco en el agua avanzada (la huella del bote como uniformes) | #13 |
| PA6b-20 | Boyas (río, regata y 500 m) que suben y bajan 0,02 m a 0,5 Hz sobre B1 | #31 (en parte) |
| PA6b-21 | El set `it6b` de §12 en `capture-snapshots.ps1` (que escriba en la carpeta de la 6b) con sus sondas | — |

**Cobertura de los issues:**

- **#6**: entero, salvo los 60 fps durante 30 min en el Android de gama media ([Device-Android], NO VERIFICABLE).
- **#13, #1, #2, #3, #12, #22 y #85**: enteros.
- **#31**: entero salvo "boyas que siguen la ola en la costa" (la costa no tiene boyas; lo cubre el casco y las demás boyas se mecen), y las fachadas reflejadas con sus ventanas en Medio (allí son una banda de tono; con detalle, solo en Alto).

## Para issues

- El reflejo de las nubes y las estrellas en Medio (hoy solo en Alto): una sonda de 128² del cielo por cuadro, si el presupuesto lo deja después de medir.
- La textura de interacción de ART_BIBLE §7.7 (la "calma" que aplana las ondas en el charco) si los quads no alcanzan.
- Las ventanas de las fachadas reflejadas en Medio.
- Un mar con FFT para la costa en Alto de escritorio.
- Las miniaturas del selector con el agua avanzada (el legacy usa la degradada: queda así).
- Los impostores de los pinos para el planar de Alto si el SetPass no entra.
