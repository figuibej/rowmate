# It. 9 — Base de arte: Historial, detalle y gráfico

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-07 |
| Iteración | 9 · `09-historial` ([ROADMAP §It. 9](../../ROADMAP.md)) |
| Entrada | [ART_BIBLE §2, §3.7, §3.9, §3.10, §4, §10](../../design/ART_BIBLE.md); [ANIMATION_BIBLE §1, §2.2, §2.3, §2.5, §2.12](../../design/ANIMATION_BIBLE.md); `Assets/RowMate/Presentation/Styles/` (`Tokens.uss`, `Typography.uss`, `Motion.uss`); [inventario HIS](../../product/inventory/history-persistence.md) F-HIS-05 a F-HIS-15; las tomas de [03-rutinas/snapshots](../03-rutinas/snapshots/) (lista y editor) |
| Legacy leído (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/history/history_screen.dart`, `session_detail_screen.dart`, `docs/image-05.png` (detalle), `docs/image-06.png` (lista) |
| Modo | Entrega rápida: cada duda se resolvió con el legacy o con un valor por defecto, en una línea (§0.2). Lo menor va a "Para issues" (§10) |

No repite las biblias: fija con números lo que faltaba para la lista, el detalle y el gráfico. **Cero tokens nuevos y cero fuentes nuevas**: colores, tamaños, radios y tiempos salen de `Tokens.cs` / `Tokens.uss` y de `Typography.uss`; las medidas propias del gráfico van como constantes en Core (`ChartSpecs`, A9-08). Los textos, formatos y cálculos son del PO (F-HIS-05 y siguientes); acá solo se dice **cómo se ven**. Contrastes calculados en sRGB con el alfa compuesto y cuantizado a 8 bits (WCAG 2.x).

Ventanas de referencia: **TV** 411 × 914 dp (teléfono vertical), **TH** 914 × 411 (teléfono horizontal), **W1280** 1280 × 720 (escritorio).

---

## 0. Resumen

### 0.1 Qué hay que lograr

1. **Bitácora de remo**: tarjetas con las cifras en el tablero de regata (RowMate Numeric SC, cifras tabulares, el color de cada métrica igual que en el HUD) y la fecha y las etiquetas discretas.
2. **Análisis post-regata en el detalle**: el resumen con jerarquía (tiempo y distancia arriba), cada paso como un parcial con el color de su tipo y un gráfico nítido con franjas, objetivos punteados, split invertido sin falsos picos, entrada animada y cursor fluido.
3. **Sin parpadeo (Q-22)**: nunca se ve un spinner si ya había datos, y una recarga con los mismos datos no mueve ni un píxel.
4. **60 fps con 60 min a 1 punto por segundo** (3 600 puntos): la malla se arma una vez y se recorta; el cursor no repinta el gráfico (§7).

### 0.2 Decisiones tomadas (una línea cada una)

- **D9-01** El selector tiene las **5 opciones del legacy** (Vatios, SPM, Split 500m, Distancia, Pulso; F-HIS-12). La demo del ROADMAP nombra 4, pero manda la paridad.
- **D9-02** Las cifras de texto sobre tarjetas usan la variante **`*.hud`** de su métrica (regla de color de tipo de la It. 3, ART_BIBLE §2.2). El pulso base `#EF476F` da 3,81:1 sobre `surface.card` y no llega a AA. El `.hud` da 4,68. Las líneas del gráfico usan el color base (son gráficos).
- **D9-03** La unidad va **pegada y más chica** que la cifra (`1657` + `m`), como pide ART_BIBLE §4.3. El texto completo no cambia (`1657m`). El legacy la ponía del mismo tamaño.
- **D9-04** El resumen se arma en **dos renglones con jerarquía** (tiempo y distancia en `num.m` 32; vatios, SPM, split y pulso en `num.row` 20). Es la mejora EXR de F-HIS-10. El orden, los textos y los colores no cambian. Si el PO lo rechaza, vale la fila única de la tarjeta (§2.2).
- **D9-05** Cada fila de "Rendimiento por paso" lleva el **color de su tipo**: borde izquierdo de 3 dp e insignia teñida. Una fila sin tipo usa el color de `work`, la misma regla que las franjas (F-HIS-14).
- **D9-06** La insignia del paso pone el número en **`text.primary`**. El color del tipo sobre su propio tinte daba 3,38–3,53:1 (enfriamiento, trabajo), y el blanco da ≥ 7,63:1.
- **D9-07** El tooltip va sobre **`glass.fallback`** (`#070D16 @86 %`) y no sobre `surface.overlay`, porque ahí `metric.hr.hud` daba 4,24:1. Sobre el vidrio, la peor cifra da 6,46:1 (§5.5).
- **D9-08** Los **ejes usan Inter `caption` 11/500 `text.tertiary`** (D-ART-03, sin fuente nueva). Son etiquetas quietas, así que las cifras proporcionales no bailan. Las cifras que cambian (tooltip, conteo) van en RowMate Numeric SC.
- **D9-09** Las marcas del eje Y salen de **valores redondos** (1, 2 o 5 × 10ⁿ, unas 4 divisiones). La etiqueta del máximo se omite si queda a menos de 12 dp de la marca vecina. En `image-05` el `157` se encima con el `150`.
- **D9-10** En el split, los puntos parados (≤ 0) son **huecos** en la línea y en el área (Q-24): no se dibujan como el ritmo más rápido.
- **D9-11** Una sesión **sin puntos** muestra sus ceros en `text.tertiary` en lugar de los colores de métrica (mejora EXR de F-HIS-07). Así no parece una sesión real. El texto es el mismo.
- **D9-12** El selector reparte el ancho **según el texto** de cada opción (más el aire parejo) y no en partes iguales. Con partes iguales, `Split 500m` no entra en TV (75,8 dp contra 82).
- **D9-13** Se quita el tilde de la opción elegida: el relleno `brand.seed` ya la marca, y el tilde no entra en TV.
- **D9-14** Desde 840 dp de ancho, el **detalle va en dos columnas** (§6). En TH, el gráfico de 220 dp de una sola columna dejaba 135 dp a la vista entre la barra y el borde.

---

## 1. Lista de sesiones (pestaña Historial)

### 1.1 Pantalla

- AppBar de la app (ART_BIBLE §3.11) con `historyTitle`. Fondo `bg.app`.
- Lista con margen 16 y **10 entre tarjetas**, igual que Rutinas (el legacy usa 12 y 5+5). Abajo, el padding de la barra de pestañas va dentro del contenido desplazable (§3.11).
- Desde 840 dp de ancho, la columna mide **720 como máximo** y va centrada (TH 914 y W1280). En W1280, seis columnas a todo el ancho quedaban a 260 dp unas de otras y no se leían como una fila (`image-06`).

### 1.2 Tarjeta (TV: 379 × 104 dp)

| Pieza | Spec |
|---|---|
| Caja | `surface.card`, `r.card` 16, relleno 16, sin borde ni sombra. Capa de estado `text.primary` 4 % con *hover* y 8 % al presionar, sin escala (la de la tarjeta de rutina, `Motion.uss`) |
| Renglón 1 (alto 24) | `rowing` 18 `accent.primary` (5,60:1) → 8 → nombre `titleS` 16/600 `text.primary`, una línea con `…`, ocupa lo que sobra → [It. 12: `cloud_done` 14 `brand.strava` → 6] → fecha `dd/MM/yyyy  HH:mm` `bodyXs` 12 `text.tertiary` (5,15:1) → 4 → `chevron_right` 18 `text.tertiary` |
| Aire | 12 |
| Renglón 2: 5 o 6 columnas | Columnas de igual ancho (`flex 1 1 0`), centradas. Cifra en `num.stat` 16/700 con la unidad en `caption` 11/500 `text.secondary`, apoyada en la misma línea de base. Debajo, a 2, la etiqueta `caption` 11/500 `text.tertiary` (el legacy usa 10 al 38 %: D-ART-03) |
| Colores de las cifras (D9-02) | Tiempo `text.primary` (10,46 el `.hud`; el legacy usa blanco) · Distancia `metric.distance.hud` 7,33 · Watts `metric.watts.hud` 9,78 · SPM `metric.spm.hud` 7,29 · Split `metric.split.hud` 7,29 · Pulso `metric.hr.hud` 4,68 (D-ART-01). Sin puntos (D9-11): todas en `text.tertiary` |
| Achicar para entrar | La cifra y su unidad se achican juntas hasta 0,70 si no entran en la columna menos 2 dp. Nunca se cortan ni bajan de renglón (`FittedBox` del legacy). En TV, con 6 columnas de 57,8 dp, `137/150` mide 56,4 y `12345m` mide 50,6: entran a 1,0×. Con letra 1,3× se achican a 0,77 |

Alto: 16 + 24 + 12 + 20 + 2 + 14 + 16 = **104 dp** a 1,0×, y 113 a 1,3× (el renglón 2 crece). Es un alto **fijo por escala de letra** (A9-04).

### 1.3 Estados

| Estado | Spec |
|---|---|
| Cargando (solo la primera vez, sin datos) | El indicador de ART_BIBLE §3.10: círculo de 32, trazo 3, `accent.primary`, centrado entre las barras. Aparece si la carga sigue a los 120 ms, con un fundido de 160 |
| Vacío | `history` 56 `text.primary` `opacity 0,12` → 12 → `historyEmpty` `body` 15 `text.tertiary` (5,83:1 sobre `bg.app`; el legacy usa 38 %, 3,3:1). Ancho máximo 320, centrado en los dos ejes entre las barras. Es la forma del vacío de Rutinas, sin ayuda (el legacy no la tiene) |
| Lista | Tarjetas (§1.2) |
| Error | El legacy no tiene. Si el PO agrega uno, usa la forma de "Error de carga de Rutinas" (§3.10) |

### 1.4 Movimiento (ANIMATION_BIBLE §2.3, sin tiempos nuevos)

- **Primera carga con la pestaña a la vista**: escalonado de 35 ms, 8 tarjetas como máximo. Cada una, opacidad 0 → 1 e y +12 → 0 en `dur.base` 240 `ease.enter`. Si llega antes de mostrar la pestaña, entra con la transición de la pestaña.
- **Recarga (Q-22)**, al tocar la pestaña, al volver del detalle y al terminar una sesión:
  - La lista **sigue a la vista** y no hay spinner.
  - Al llegar los datos, se comparan por id de sesión:
    - la sesión nueva **crece** arriba (`spring.smooth` ω 12; opacidad 60–260 `ease.enter`);
    - la que ya no está **colapsa** (opacidad 0–120, alto y márgenes 80–280 `ease.move`);
    - la que cambió algún texto (cifras, nombre, ícono de Strava) **se ilumina** con una capa blanca de 0,08 → 0 en 600 `ease.move`.
  - Sin cambios, **nada se repinta** y el desplazamiento no se mueve.
- **Volver del detalle**: el *pop* de §2.2. Si ese mismo cuadro trae una recarga, los verbos de arriba corren detrás del *pop*, sin esperar.
- Reducir movimiento: lo de ANIMATION_BIBLE §2.3.

---

## 2. Detalle de sesión

### 2.1 Pantalla y estados

- Entra apilada con el *push* de ANIMATION_BIBLE §2.2. La AppBar lleva la flecha y el título (`routineName` o `workoutFree`, Q-09); [It. 12: `cloud_done` 20 `brand.strava` a la derecha].
- Cargando: el indicador de §1.3. Sin puntos: `sessionNoTelemetry` `body` `text.tertiary`, centrado.
- Con puntos (una columna, TV), con relleno 16:
  1. Resumen.
  2. 20 de aire.
  3. Rendimiento por paso, si hay datos de paso, y 20 de aire.
  4. Selector.
  5. 8 de aire.
  6. Gráfico (los aires son los del legacy).
- **Entrada de las secciones**: si los datos llegan después del *push*, las 4 secciones entran escalonadas cada 35 ms (opacidad 0 → 1, y +12 → 0, 240 `ease.enter`). Si llegan durante el *push*, entran con la página, sin animación propia. Son las reglas de §2.3: dos movimientos encimados se ven como un tirón.

### 2.2 Resumen (D9-04)

| Pieza | Spec |
|---|---|
| Caja | `surface.card` `r.card` 16, relleno 16 |
| Título | `sessionSummary` `titleS` 16/600 `text.primary` → 12 |
| Renglón protagonista | 2 columnas: Tiempo y Distancia. Cifra en `num.m` 32/700 (Tiempo en `text.primary`, Distancia en `metric.distance.hud`), unidad `m` en `bodyM` 14 `text.secondary` y etiqueta `caption` 11 `text.tertiary` a 2. Entre las dos columnas, un separador vertical de 1 × 28 `border.subtle` |
| Aire | 12 y una línea de 1 `border.subtle` a todo el ancho, y 12 más |
| Renglón 2 | 3 o 4 columnas iguales: Watts, SPM, Split y Pulso. Cifra en `num.row` 20/700 con la regla de colores de §1.2 y la etiqueta como arriba |
| Achicar | La regla de §1.2. En TV, 4 columnas de 86,7: `142/143` mide 70,6 a 1,0× y 91,7 a 1,3× (se achica a 0,92) |
| Alto | ≈ 16 + 20 + 12 + 40 + 2 + 14 + 25 + 24 + 2 + 14 + 16 ≈ **185 dp** (el legacy, 101) |

**Conteo** (ANIMATION_BIBLE §2.5; solo al abrir el detalle):

- Tiempo, Distancia, Watts y SPM cuentan de 0 a su valor en 700 `ease.enter`, escalonados cada 120 (0, 120, 240 y 360), con el formato del legacy en cada cuadro. **Siempre terminan en el texto exacto**.
- Split y Pulso no cuentan: un split que sube desde `0:00` pasa por ritmos imposibles. Funden 0 → 1 en 240 desde los 480 y los 600.
- Arranca con la entrada de la sección.
- Con reducir movimiento: fundido de 160, sin conteo.

### 2.3 Rendimiento por paso (D9-05, D9-06)

| Pieza | Spec |
|---|---|
| Caja | `surface.card` `r.card` 16, relleno 16. Título `sessionStepBreakdown` `titleS` 16/600 → 12 |
| Fila | `surface.raised` (el blanco al 4 % del legacy sobre la tarjeta da `#23364C`, el mismo color), `r.m` 8, borde izquierdo de 3 dp en el **color base del tipo** (`stroke.accent`, como el editor), relleno 8 arriba y abajo, 12 a la izquierda (después del borde) y 12 a la derecha, `min-height` 52, 6 de aire debajo |
| Insignia | 28 × 28 `r.s` 6, fondo con el color base del tipo `@20 %` sobre la fila, número (índice + 1) en `num.xs` 13/700 **`text.primary`** (10,41 el trabajo, 8,26 el descanso, 7,63 el calentamiento, 9,87 el enfriamiento, 10,04 otro) → 10 |
| Línea 1 | `2:00  ·  433m` en `bodyXs` 12 `text.secondary` (6,93:1 sobre `surface.raised`; el legacy usa 11 al 54 %) |
| Línea 2 (a 2) | Tres grupos separados por 12: `150` `W` · `24.1` ` spm` · `2:13` `/500m`. Cifra en `num.stat` 16/700 en `metric.watts.hud` (8,73), `metric.spm.hud` (6,51) y `metric.split.hud` (6,51) sobre `surface.raised`. Unidad en `bodyXs` 12 `text.secondary`. Cada grupo es un solo texto (`150W`), así que los tests leen lo mismo que el legacy |
| Interacción | Ninguna (paridad). La fila no tiene capa de estado |

Alto de la fila: 8 + 15 + 2 + 20 + 8 ≈ **53 dp** (el legacy, 51).

### 2.4 Selector de métrica (D9-01, D9-12, D9-13)

- Segmentado de **40 dp**, `r.full`, borde 1 `border.strong`, separadores internos de 1 `border.strong`.
- Cada opción usa `flex-grow 1` con base en su texto y un relleno de 6 a 12 a los lados.
- Texto `buttonS` 12/600:
  - opción elegida: fondo `brand.seed` y texto `text.primary` (4,87:1);
  - las demás: sin fondo y texto `text.secondary`.
- Capa de estado de `.rm-segmented` (`Motion.uss`): *hover* 4 %, presionado 8 %. Anillo de foco de escritorio de la app.
- Medidas en TV (379): los textos suman ≈ 231 dp a 1,0× y 300 a 1,3×, más 5 × 12 de relleno. Si no entra, cada texto se achica hasta 0,70 y después lleva `…`. Con el tilde y a 13/600 (spec) no entra entero en P360 a 1,3×: necesita ≈ 0,73 (gate M-02; el tope era 0,85).
- **Cambio de elegida**: el fondo `brand.seed` funde de la vieja a la nueva en 160 `ease.move` (sin deslizar, porque las opciones no miden lo mismo). El gráfico cambia según §5.6.
- Siempre arranca en Vatios (Q-21).

---

## 3. Gráfico: anatomía (TV)

| Medida | Valor |
|---|---|
| Marco | `bg.sunken` `r.l` 12, relleno 12. Alto **220** en TV. Desde 600 dp de ancho en una columna, **280** (ART_BIBLE §3.9). En dos columnas, §6 |
| Columna del eje Y | **36 dp fijos para todas las métricas**: así el ancho del área no cambia al cambiar de métrica y la curva se puede transformar (§5.6). Las etiquetas van alineadas a la derecha, a 6 del área |
| Área de datos (TV) | x de 48 a 363 (**315 dp**), y de 18 a 190 (**172 dp**). Arriba quedan 6 para la tira de tipo y abajo, 18 para las etiquetas X (14 + 4) |
| Margen derecho | 4 dp: la última etiqueta X se alinea a la derecha del área, no al centro de su marca |

Orden de dibujo, de abajo arriba:

1. Franjas.
2. Grilla.
3. Áreas de los objetivos.
4. Área de la serie.
5. Separadores de paso.
6. Líneas objetivo.
7. Línea de la serie.
8. Tira de tipo.
9. Etiquetas.
10. Capa del cursor y del tooltip, que es otro elemento (§5.5).

---

## 4. Gráfico: elementos

| Elemento | Spec | Contraste |
|---|---|---|
| **Franja de paso** | Color base del tipo `@10 %` a todo el alto del área [CONTRATO]. Va de su inicio a su fin con la regla de F-HIS-14 | el fondo queda `#1F1827` (trabajo), `#08272C` (descanso), `#212626` (calentamiento), `#091F2E` (enfriamiento), `#111E2A` (otro) |
| **Tira de tipo** [EXR] | 3 dp (`stroke.accent`) en el color base del tipo, **pegada encima del área** (y 12–15), con 2 dp de hueco entre franjas y extremos de radio 1,5. Las franjas al 10 % se leen poco sobre el fondo oscuro, y la tira repite la mini-barra de Rutinas (§3.7) | sobre `bg.sunken`: 4,28 (otro) a 12,97 (calentamiento), todo ≥ 3:1 |
| **Separador** | Al inicio de cada franja salvo la primera: vertical de 1 dp, blanco `@24 %`, punteado [4, 4] **desde arriba del área**. La fase arranca en y = 18 y no cambia al recargar [CONTRATO] | — |
| **Grilla** | Horizontal en cada marca Y, 1 dp `#FFFFFF @8 %` (el legacy, 0,5 al 10 %). La línea del 0 (o la del ritmo más lento en el split), 1 dp `border.default` `@12 %` | decorativa |
| **Serie** | Línea de **2 dp** en el color base de la métrica [CONTRATO], extremos redondos, uniones con inglete de tope 2 | sobre `bg.sunken`: Watts 13,24 · SPM 7,59 · Split 9,11 · Distancia 9,72 · Pulso 5,16; sobre la peor franja: Pulso 4,23 y el resto ≥ 6,22 (todo ≥ 3:1) |
| **Área de la serie** | Degradé vertical del color de la métrica: **24 % arriba del área y 0 % en la base**. El alfa en cada vértice de la curva es `0,24 · (1 − y/172)`; el legacy es plano al 12 % | — |
| **Línea objetivo** | Horizontal de 2 dp, blanco `@78 %`, punteada [8, 5], de la franja de inicio a la de fin [CONTRATO]. La fase se ancla al inicio de la franja: no "camina" al revelar ni al recargar. El último trazo se corta en el fin de la franja | 9,82–10,80:1 sobre las franjas |
| **Área objetivo** | Color base del tipo `@7 %` [CONTRATO], de la línea hacia la base del área. Con mínimo y máximo de vatios se superponen, como en el legacy | — |
| **Split (Q-24, D9-10)** | Invertido: lo más rápido queda arriba, también el objetivo. Los ritmos ≤ 0 son huecos: la línea se corta con extremos redondos y el área baja vertical a la base en cada borde del hueco | — |
| **Etiquetas Y** | Inter `caption` 11/500 `text.tertiary`, alineadas a la derecha a 6 del área y centradas en su marca. Van en las marcas redondas de D9-09 (4 divisiones, 5 etiquetas) y en `m:ss` para el split. Se recortan dentro del marco (la de arriba no sube más que y 12) | 5,96:1 sobre `bg.sunken`; ≥ 5,49 sobre las franjas |
| **Etiquetas X** | Las mismas, en `m:ss` y con los intervalos del legacy (30, 60, 120 o 300 s, según el último punto), más el inicio y el fin [CONTRATO]. Una marca intermedia que queda a menos de (ancho + 8 dp) del inicio o del fin no se dibuja: en `image-05` el `3:44` casi toca el `4:00`. Van en y 194–208 | ídem |
| **Un solo punto** | Un disco de 6 dp en el color de la métrica, sin área | — |

---

## 5. Gráfico: movimiento y toque

Todo por código con el reloj del `MotionDriver` (deterministas para las tomas, regla de `Motion.uss`). Sin transiciones de USS.

### 5.1 Entrada (ANIMATION_BIBLE §2.12, con números)

`t0` = el último de dos momentos: el fin del *push* (240) y la llegada de los datos. Si la sección entra escalonada (§2.1), `t0` es su comienzo.

| t − t0 (ms) | Qué |
|---|---|
| 0–200 | Marco, grilla, etiquetas, franjas, tira y separadores: opacidad 0 → 1, `ease.enter` |
| 100–700 | **Revelado** de izquierda a derecha: el corte `xc = x0 + E(u)·315` con `ease.enter`. La línea, su área, las líneas objetivo y sus áreas se dibujan solo hasta `xc`. El último segmento se interpola hasta el corte (sin escalones) |
| 150–450 | El área de la serie funde de 0 a su alfa, encima del revelado |

- Un toque en el gráfico durante la entrada la lleva al final en ese cuadro y muestra el cursor.
- Con reducir movimiento, todo aparece en su estado final en el primer cuadro, sin fundido ni revelado (gate M-01: la spec §9 pide "al instante").

### 5.2 Recarga sin parpadeo (Q-22)

- Si el detalle se vuelve a leer (relectura de la sesión, It. 12) y los puntos son los mismos (mismo conteo y misma huella de datos), **no se rearma ni se reanima nada**.
- Si cambiaron, el gráfico pasa al dato nuevo con la transformación de §5.6, sin entrada.
- La entrada corre una vez por apertura del detalle.

### 5.3 Desplazar la página

El gráfico **no se repinta** al desplazar: UI Toolkit mueve la malla con la transformación del contenido. Nada del gráfico depende de su posición en la ventana.

### 5.4 Cursor

- **Toque** (teléfono):
  - Apoyar el dedo en el área muestra el cursor **en ese cuadro**, en el punto más cercano en x.
  - Si el dedo se mueve más de 6 dp y el movimiento es más horizontal que vertical (|dx| > |dy|), el gráfico captura el puntero: el cursor lo sigue y la página no se desplaza.
  - Si el primer movimiento es vertical, suelta el puntero a la `ScrollView` y el cursor se va.
  - Al levantar el dedo queda 1 500 ms y después se va.
- **Puntero** (escritorio): sigue al *hover* y se va al salir del área. La rueda desplaza la página.
- **Guía**: vertical de 1 dp, blanco `@40 %`, a todo el alto del área, en la x del punto.
- **Punto**: disco de 8 dp en el color base de la métrica con un anillo de 2 dp `bg.sunken` (ART_BIBLE §3.9), en la y del valor **original**. En un gráfico denso, ese valor cae a ≤ 1 dp de la envolvente dibujada (§7.2).
- **Seguimiento**: la guía y el punto van al punto elegido con `spring.snappy` (ω 22) en x y en y, desde su posición y su velocidad actuales. Así el cursor se desliza entre puntos y no salta de a uno.
- **Valor**: el texto es siempre el del punto elegido, **sin rodillo** (al recorrer rápido, el rodillo de 180 ms quedaría atrás).
- Con reducir movimiento, sin resorte: la guía y el punto van en el acto.

### 5.5 Tooltip (D9-07)

| Pieza | Spec |
|---|---|
| Caja | `glass.fallback` (`#070D16 @86 %`), borde 1 `border.default`, `r.m` 8, relleno 6 / 10, ancho mínimo 64 |
| Renglón 1 | Valor en `num.stat` 16/700 en `metric.*.hud` y la unidad en `bodyXs` 12 `text.secondary`, con el formato del PO (legacy `150.0 W`, `2:15 s`). La peor cifra, el pulso sobre la franja de trabajo, da **6,46:1** |
| Renglón 2 | Tiempo del punto `m:ss` en `caption` 11 `text.tertiary` (6,04:1). No hace falta una clave nueva |
| Lugar | Centrado en la x del punto, con su borde de abajo 10 dp sobre el punto. Se ajusta adentro del marco, a 4 de cada borde. Si arriba no entra (el punto está a menos de 56 dp del borde superior del área), va 10 dp debajo. El cambio de lado y la posición siguen al punto con el mismo `spring.snappy` |
| Aparece y se va | Aparece con opacidad 0 → 1 y escala 0,96 → 1 en 90 `ease.enter`, desde el punto. Se va con opacidad → 0 en 160 `ease.exit`. Con reducir movimiento, solo opacidad |

### 5.6 Cambio de métrica (mejora EXR de F-HIS-12; ANIMATION_BIBLE §2.12)

| t (ms) | Qué |
|---|---|
| 0–300 | **La curva se transforma**: cada vértice va de su y en la métrica vieja a su y en la nueva, con `ease.move`. El eje X es el mismo en todas, así que las dos series tienen las mismas x (§7.2). En un hueco del split, el alfa del vértice va de 0 a 1 o de 1 a 0 junto con la y |
| 0–220 | El color de la línea y del área se interpola en OKLab de la métrica vieja a la nueva. Lineal en sRGB, el amarillo y el cian pasan por un gris barroso |
| 0–120 / 100–220 | Las etiquetas Y y la grilla viejas funden a 0, y las nuevas, en su lugar final, funden a 1 |
| 0–120 / 100–300 | Las líneas objetivo viejas y sus áreas funden a 0, y las nuevas entran con opacidad 0 → 1. Cambian de lugar y de cantidad, así que no se transforman |
| — | Las franjas, la tira y los separadores no se mueven. El cursor, si está visible, se queda en su x y se acomoda a la y nueva con su resorte |

- Un segundo cambio a mitad arranca desde la y y el color actuales.
- Con reducir movimiento: la métrica nueva aparece en su estado final en el primer cuadro, sin fundido ni transformación (gate M-01: AC-HIS-12-02 pide un cambio instantáneo).

---

## 6. Vertical y horizontal

| Ventana | Lista | Detalle |
|---|---|---|
| **TV** (< 600 dp de ancho) | Una columna, tarjetas de 379 | Una columna (§2.1). Gráfico de 220 y área de 315 × 172 |
| 600–839 dp (tablet vertical) | Una columna, tarjetas a todo el ancho | Una columna. Gráfico de 280 |
| **TH** y **W1280** (≥ 840 dp de ancho, D9-14) | Columna de 720 como máximo, centrada | **Dos columnas** con relleno 16 y 16 entre ellas (detalle abajo) |

**Las dos columnas del detalle:**

- **Izquierda**: ancho `clamp(0,42 × ancho; 340; 440)`, con su propio desplazamiento. Lleva el Resumen y el Rendimiento por paso.
- **Derecha**: no se desplaza. Lleva el selector y, a 8, el gráfico, que **llena el alto que queda**, de 200 a 440 dp.
  - **TH** (914 × 355 de contenido debajo de la AppBar): izquierda de 384 y derecha de 482. Gráfico de 355 − 32 − 48 = **275 dp**, con un área de 418 × 227.
  - **W1280** (1280 × 664): izquierda de 440 y derecha de 792. Gráfico de **440**, con un área de 728 × 392.

Con dos columnas, la entrada de §2.1 escalona la izquierda (Resumen y Pasos) y la derecha (selector y gráfico) a la vez: son 2 + 2, no 4 seguidas.

---

## 7. Cómo se dibuja en UI Toolkit y presupuesto

### 7.1 Malla propia, no `Painter2D`

**Decisión.** El gráfico se dibuja con `generateVisualContent` y `MeshGenerationContext.Allocate`: una malla propia, como `StructureBarView` y `PillShapes`. Así se dibujan la línea, las áreas, las franjas, la tira, los separadores, la grilla y los objetivos.

`Painter2D` queda descartado por tres motivos:

- Vuelve a teselar todo el trazo en cada repintado, y la entrada y el cambio de métrica repintan unos 60 cuadros.
- No tiene punteado.
- Con 3 600 puntos genera basura en el teselado.

**Línea:**

- Una tira de 4 vértices por punto: borde exterior, núcleo, núcleo, borde exterior.
- El núcleo mide 2 dp de ancho con alfa 1, y los bordes exteriores agregan **1 px físico** (`1/densidad` dp) con alfa 0. Es el suavizado: el panel no tiene MSAA.
- La normal de cada vértice es el promedio de las normales de los dos segmentos, con el inglete topado en 2.
- Los extremos redondos llevan 6 triángulos en abanico.

**Área:** 2 vértices por punto (la curva con su alfa del degradé y la base con alfa 0).

**Punteados:** un cuadrilátero por trazo, con el mismo borde de 1 px.

**Revelado y transformación:**

- Los vértices quedan ordenados por x, así que **revelar es dibujar un prefijo** de los índices, más un segmento final interpolado.
- Transformar es reescribir las y y los colores de los vértices ya ubicados.
- Ninguno de los dos vuelve a calcular la geometría desde los datos.

**Capas aparte** (para que el cursor nunca repinte la malla grande):

- Las etiquetas: unos 12 `Label` o `DrawnText` con clase `.rm-type-caption`.
- El cursor y el tooltip, en un elemento hermano: la guía y el punto en su propia malla chica, y el tooltip con 2 textos.

### 7.2 Datos → vértices (Core puro, `dotnet test`)

1. **Al cargar** (una vez por sesión), para las 5 métricas: los valores originales (`float[]` por métrica) y la escala Y de F-HIS-13 con el split invertido y sus huecos. Son 3 600 × 5 × 4 B = 72 KB.
2. **Reducción por columna**, una vez por métrica y por ancho del área:
   - Por cada columna de 1 dp, el mínimo y el máximo de sus puntos, en el orden en que ocurren. Así se conservan los picos de vatios.
   - Salen como máximo 2 × 315 = **630 puntos** en TV y 1 456 en W1280.
   - Todas las métricas usan las **mismas columnas**, y eso permite la transformación de §5.6. Un hueco del split también es una columna.
3. **Suavizado** del legacy (curvatura 0,2, Catmull-Rom o Bézier como fl_chart):
   - Solo si los puntos quedan a **3 dp o más** entre sí. Ahí se subdivide cada segmento en `⌈separación / 3 dp⌉` partes, hasta 8.
   - Más denso, el suavizado no se ve, y la línea va recta entre los puntos reducidos.
   - La y suavizada se limita al área: nunca baja de la base.
4. **Cursor**: búsqueda binaria en x sobre los puntos **originales**, no los reducidos.

### 7.3 Presupuesto (60 fps en el Android de gama media, nivel de la app sin escena)

| Ítem | Tope |
|---|---|
| Vértices del gráfico (TV / W1280) | ≤ 4 500 / ≤ 9 500: línea 4·n, área 2·n, franjas, tira, objetivos y grilla. Muy por debajo de los 65 535 por malla |
| Preparar los datos (paso 1 de §7.2) | ≤ 2 ms una sola vez, antes de `t0`, fuera de la entrada |
| Repintar la malla (revelado o transformación) | ≤ **0,6 ms** de CPU por cuadro en el Android de referencia y ≤ 0,2 en escritorio. Cuadros con repintado: ≈ 42 en la entrada y ≈ 18 en un cambio de métrica |
| Cursor y tooltip | 0 repintados de la malla grande. La malla chica tiene ≤ 40 vértices |
| Desplazar la página | 0 repintados del gráfico |
| Basura por cuadro | **0 B** en la entrada, el cambio de métrica, el cursor y el desplazamiento. Se mide con FrameAllocations / Profiler (lección de la It. 7), con *buffers* reusados y sin LINQ ni cadenas nuevas por cuadro (el texto del tooltip, con un `StringBuilder` reusado o con el formateador sin asignaciones) |
| Elementos del detalle | Gráfico ≤ 20, Resumen ≈ 30, fila de paso 5 (con 64 pasos, ≈ 320). La lista es virtual (A9-04): unas 10 tarjetas vivas × 16 elementos |
| Tiempo de cuadro | p99 ≤ 16,6 ms recorriendo con el cursor una sesión de 60 min a 1 Hz, en TV y TH |

---

## 8. Tomas que tienen que verse (insumo para el PO y QA)

Todas con **datos sembrados** en un `ROWMATE_DATA_DIR` temporal: nunca sesiones reales.

| Toma | Qué muestra |
|---|---|
| CAP-H01 · lista TV | 6 tarjetas: rutina con pulso, libre sin pulso, sin puntos (D9-11), nombre largo con `…`, `12345m` y `137/150` (achicar) |
| CAP-H02 · vacío TV / CAP-H03 · cargando TV | §1.3 |
| CAP-H04 · lista W1280 | Columna de 720 centrada |
| CAP-H05 · detalle TV, rutina | Resumen en dos renglones, 4 filas de paso con los 4 tipos, gráfico de Vatios con franjas, tira, separadores y objetivos mínimo y máximo |
| CAP-H06 · detalle TV, split | Split invertido con su objetivo y un hueco por parada |
| CAP-H07 · detalle TV, libre | Sin Rendimiento por paso ni franjas |
| CAP-H08 · cursor | Tooltip arriba y, en otra toma, cerca del borde superior (va abajo) y del borde derecho (se ajusta) |
| CAP-H09 · TH y CAP-H10 · W1280 | Detalle en dos columnas |
| CAP-H11 · letra 1,3× y CAP-H12 · en | Selector entero y cifras achicadas |
| VID-H01 | Entrada del detalle, cambio Vatios → Split → Pulso y recorrido con el cursor, a 60 fps (sesión de 60 min a 1 Hz) |
| VID-H02 | Recarga del Historial al volver de una sesión nueva: solo crece la tarjeta nueva y nada más se mueve |

---

## 9. Pedidos al Architect

| ID | Pedido |
|---|---|
| **A9-01** | `SessionChartView`: un elemento con malla propia (`generateVisualContent` + `MeshGenerationContext.Allocate`) y *buffers* de vértices e índices reusados. Revelado por prefijo de índices y transformación reescribiendo y y color (§7.1). Sin `Painter2D` en la malla grande |
| **A9-02** | En Core (`RowMate.Core`, puro y con `dotnet test`): la escala Y de F-HIS-13 con el split invertido y sus huecos, D9-09 (marcas redondas y la etiqueta del máximo), la regla de las etiquetas X que chocan, la reducción por columna de 1 dp con columnas comunes a las 5 métricas, el suavizado con su umbral de 3 dp, la geometría de franjas, la tira, los separadores y los objetivos con la fase anclada, y la búsqueda del punto más cercano. La vista solo copia vértices |
| **A9-03** | Una capa del cursor y del tooltip **hermana** del gráfico: nunca marca sucia la malla grande. El reparto del gesto con la `ScrollView` (umbral de 6 dp y \|dx\| > \|dy\|) y el resorte ω 22 con el reloj del `MotionDriver` |
| **A9-04** | Historial con `ListView` virtual, de alto fijo por escala de letra (104 dp a 1,0×, §1.2) y clave estable por id de sesión. La recarga compara y aplica crecer, colapsar e iluminar (Q-22). El spinner solo aparece en la primera carga sin datos |
| **A9-05** | `TabularNumber` con **cifra y unidad en un solo elemento** (unidad en otra clase de tipo, apoyada en la línea de base) y **achicar para entrar** (mínimo 0,70) en las columnas de la tarjeta, el resumen y las filas de paso |
| **A9-06** | El corte de layout del detalle: una columna hasta 839 dp de ancho y dos desde 840 (§6). El gráfico mide 220, 280 o "llenar" según el caso. La lista va a 720 como máximo desde 840 |
| **A9-07** | El conteo del resumen (§2.2) con el contador de ANIMATION_BIBLE §2.5 y el reloj inyectable. En las tomas se ve el valor final |
| **A9-08** | Las medidas del gráfico como constantes en Core (`ChartSpecs`, como `MotionSpecs`): 36, 6, 18, 3 + 2, 2 dp de línea, [4,4], [8,5], 24 % del degradé, 1 500 ms de permanencia y 6 dp de umbral. Los colores, de los tokens que ya existen. **Ningún token ni fuente nueva** |
| **A9-09** | El color de la fila de paso y de su franja sale del tipo del primer punto del paso, y `null` vale `work` (D9-05) |
| **A9-10** | Medir el presupuesto de §7.3 con FrameAllocations y un marcador del Profiler (`RowMate.Chart.Generate`) en una sesión sembrada de 60 min a 1 Hz. Las tomas de §8 se hacen sin tocar `prefs.json` ni la base del usuario |
| **A9-11** | Con reducir movimiento, las variantes de §1.4, §2.2, §5.1, §5.4, §5.5 y §5.6 |

---

## 10. Para issues

- Tocar una fila de paso resalta su franja en el gráfico, y tocar una franja muestra el resumen del paso (mejoras EXR de F-HIS-11 y F-HIS-14).
- La tarjeta del Historial "se abre" en el detalle con un elemento compartido (mejora EXR de F-HIS-07). Hoy se usa el *push* común.
- Un cursor con todas las métricas del instante, y zoom y desplazamiento con gestos (mejora EXR de F-HIS-13).
- Distinguir los tramos dentro y fuera de objetivo sobre la curva (mejora EXR de F-HIS-15).
- Marcar las paradas como "sin remo" con una marca propia, no solo con el hueco del split, en las 5 métricas.
- "RowMate Numeric" (Inter con `tnum` congelado, ART_BIBLE §4.2 punto 2), si en algún momento la fecha o los ejes tienen que alinear las cifras entre renglones.
- El sello de Strava con microanimación al terminar la subida (It. 12, F-HIS-08).
