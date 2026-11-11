# It. 15 — Notas de arte: deuda visual resoluble en la PC (Designer)

> **Reemplazado en parte por [01-po-spec](01-po-spec.md) y [02-tech-design](02-tech-design.md) §1 (gate m-05).** Donde este documento contradice la spec, manda la spec: sombra del *tooltip* 35 %/8 dp/+2 dp (no 45 %/σ6/+4), franjas al 22 % (no 20/5 %), botón de ícono junto al *chevron* (no menú ⋮ ni sello), recorte de íconos **sí** (D15-02 no rige: `uvx` ya está en la PC) y las tomas del set `it15` de la spec §5 (no las de §3). **Siguen vigentes** D15-01 (ejes en Numeric SC 600; adoptado por el gate QA-G15-02) y D15-05/A15-12 (sin estela, toma de diagnóstico AUX-15-01; adoptado por el gate QA-G15-01), y 82-c (escena demorada; adoptado por el gate QA-G15-03). WP-D no implementa los valores reemplazados.

Base: [ART_BIBLE](../../design/ART_BIBLE.md), [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md), [ASSET_SLOTS](../../design/ASSET_SLOTS.md), `Assets/RowMate/Presentation/Styles/` y las tomas de la 9, la 12 y la 14 (`09-historial/snapshots`, `12-exportar-tcx/snapshots`, `14-pulido-plataformas/snapshots`). Issues leídos: #32, #33, #35, #37, #66, #67, #76, #82, más #77 (no se toca: queda el vidrio). Ningún token, color ni fuente nueva. Todos los tiempos salen de `dur.*`, `ease.*` y `spring.*`.

## 0. Decisiones (una línea cada una)

- **D15-01** Las cifras de los ejes del gráfico pasan a **RowMate Numeric SC 600** a 11 dp (el tamaño de `caption`), en `text.tertiary`. Con eso se cierra el pedido de "RowMate Numeric" de Inter (#37, #76): no hace falta Python ni fuente nueva, y es el mismo criterio que la fecha de la tarjeta (WD-01 de la 14).
- **D15-02** **No se agregan glifos a la fuente de íconos** (#66, sello de #67). Para eso hay que bajar la fuente y correr fontTools (`uvx` baja un Python): queda fuera por la regla de no instalar. `download_done` se dibuja con la receta de `ExportGlyph`.
- **D15-03** La línea de la serie conserva el color de la métrica [CONTRATO]. "Dentro y fuera del objetivo" y "sin remo" se marcan en el área y con un velo, nunca en el color de la línea.
- **D15-04** El zoom y el desplazamiento del gráfico con gestos, y la exportación múltiple, quedan fuera. Los gestos no se pueden verificar sin teléfono, y la exportación múltiple necesita un modo de selección que tiene que especificar el PO.
- **D15-05** El "parche de la estela" (V14-12) no aplica. En el código no hay ninguna estela: el agua de la 6a no la tiene (AC-SCN-13-04). Ese rectángulo claro detrás del bote es casi seguro la **popa del casco** (`Hull`, cubierta `#F5F5F5`), que se ve de ancho parejo porque se afina mientras la perspectiva la ensancha. Hay que confirmarlo con una toma que oculte el casco (A15-12).
- **D15-06** El sello de exportada usa `text.tertiary`. No usa color de marca: es información secundaria y no tiene que competir con el ícono de remo `accent.primary`.

## 1. Fichas por issue

### #32 — Ejes y escala (resto)

- **Hoy:** la 14 ya resolvió las etiquetas encimadas (DV-137, ninguna marca intermedia a menos de 4 dp de la de un borde) y el rango mínimo de 10 s del split (`SplitRange.Widen` cableado en `ChartSeries`). Lo que queda: la cúbica de fl_chart (suavizado 0,2, sin `preventCurveOverShooting`) se pasa de largo en cada escalón. En CAP-22 y en 09/CAP-08 se ve un pico o un pozo de 1-3 dp en cada cambio de paso.
- **Cómo debe quedar:** misma cúbica de suavizado 0,2, pero en cada tramo `[i, i+1]` la `y` de los dos puntos de control queda **recortada al rango `[min(yᵢ, yᵢ₊₁), max(yᵢ, yᵢ₊₁)]`**. En un tramo monótono la forma es idéntica, y en un escalón la curva nunca sale de los extremos. Las `x` de control no cambian. Desvío de paridad visual: va como DV del PO.
- **Verificación:** dotnet: ningún vértice teselado sale del rango de su tramo, y en una serie monótona los vértices son iguales a los de hoy, al bit. Toma: CAP-01/02 (§3).
- **Viable en la PC:** sí (Core puro).

### #33 — Interacción EXR del gráfico y del detalle

| Parte | Hoy | Cómo debe quedar | Viable |
|---|---|---|---|
| 33-a Paso ↔ franja | Las filas y las franjas no se hablan | **Elegir un paso**: con un toque en su fila de `Rendimiento por paso` o en su **tira de tipo** (la zona táctil de la tira va de y 0 a 24 del área: así no compite con el cursor, que sigue en el área). La franja elegida pasa del 10 % al **20 %** de su color base y las otras bajan al **5 %**. Su tira crece de 3 a **5 dp** hacia arriba, con radio 2,5. La fila elegida suma un borde `stroke.target` 1,5 en el color base del paso (`r.l`). Todo en 160 `ease.move` (vértices de color de la malla, sin reteselar). Si el gráfico se ve menos de la mitad, la página se desplaza para mostrarlo con 16 dp de aire en `dur.page` 420 `ease.move`. **Resumen del paso**: una caja con el aspecto del tooltip (§37-a) sobre la tira, centrada en la franja y dentro del marco a 4 dp: `Paso N · tipo` en `caption` 11 `text.secondary` y debajo la línea de la fila (`num.xs` 13/700) | sí |
| 33-a (sigue) | — | Se suelta con otro toque en la misma fila o tira, con un toque fuera del gráfico y de las filas, o con Escape. El cursor tapa al resumen: mientras se ve el cursor, el resumen funde a 0 en 90 y la franja sigue elegida. Con reducir movimiento: en el acto, sin desplazamiento animado | sí |
| 33-b Cursor con todas las métricas | El tooltip muestra solo la métrica elegida | El renglón 1 queda igual. Debajo: separador de 1 px `border.subtle` a 6 dp y una grilla de 2 × 2 con las otras 4 métricas (orden del selector), cada una con un punto de 6 dp en su color base, 4 dp de aire y el valor en `num.xs` 13/700 `metric.*.hud` con su unidad en `caption` 11 `text.secondary`. Columnas de ancho fijo por gráfico (el máximo, como TY-01). Ancho ≈ 168, alto ≈ 92. Si arriba del punto no entra, la regla de lado de 09/00-arte §5.5 se calcula con el alto nuevo. **Una sola caja**: no hay un tooltip por métrica | sí |
| 33-b Zoom y desplazamiento | — | Fuera (D15-04) | no |
| 33-c Dentro y fuera del objetivo | Solo la línea punteada | Solo en los pasos con objetivo **de la métrica elegida**: el área bajo la curva va al **24 %** (como hoy) donde el valor está dentro del objetivo, y al **10 %** fuera. El corte está en la `x` donde la curva cruza el borde del objetivo (interpolado, sin escalón), y el cambio de alfa se suaviza en 2 dp. La línea no cambia (D15-03). En el split, "dentro" respeta el eje invertido (Q-24). Contraste: decorativo (≥ 3:1 lo da la línea) | sí |
| 33-c Paradas "sin remo" | Solo el hueco del split; en las otras métricas, la línea cae a 0 (CAP-22, a los 7:00) | Un tramo sin remo es el de los huecos del split (paso ≤ 0, la misma regla en las 5 métricas). Se dibuja un **velo** `overlay.scrim` al **20 %** a todo el alto del tramo, encima de las franjas y debajo de la curva, más una línea base de 2 dp `text.tertiary` punteada [3, 3]. En ese tramo el cursor dice `Sin remo` (clave nueva: la define el PO) en lugar del valor. Los tramos de menos de 2 s no se marcan | sí |
| 33-d Tarjeta → detalle con elemento compartido | *Push* común | Ver §2.1 | sí (prioridad 2) |

### #35 — Columnas de la tarjeta

- **Hoy:** en las 6 tarjetas, las columnas se llaman `history-card-stat-<i>`, porque `SetNamePrefix` no guarda el prefijo y `Bind` crea las columnas después. Por eso la sonda de CAP-01 lee `#000000`, aunque se ve bien.
- **Cómo debe quedar:** sin cambio visual. Cada tarjeta lleva el prefijo `history-card-<n>-stat-<i>`, la sonda de aspecto vuelve a dar 2/2 y una sonda que falla marca la toma. Es trabajo de código (Architect).
- **Viable en la PC:** sí.

### #37 — Pulido de arte del Historial (resto)

La 14 ya hizo los extremos redondos, la tira en cápsulas y el ícono del error. Queda:

| Parte | Hoy | Cómo debe quedar | Viable |
|---|---|---|---|
| 37-a Sombra del tooltip | El tooltip (`glass.fallback` 86 %) se apoya plano sobre la curva | **Sombra de malla 9-slice**: textura generada por código como `GlowTexture` (48 × 48, solo alfa, borde gaussiano σ = 6 dp alrededor de un rectángulo redondeado `r.m` 8; cortes 9-slice de 16). Es un hermano **detrás** de la caja, 12 dp más grande por lado y corrido **y +4**, en `#000000` al **45 %**. Aparece y se va con la misma opacidad y escala del tooltip. Costo: 1 elemento y una textura de ≈ 2,3 KB (R8), fuera del HUD | sí |
| 37-b RowMate Numeric | Ejes en Inter con cifras proporcionales | D15-01: ejes en RowMate Numeric SC 600 a 11. Las etiquetas Y siguen alineadas a la derecha a 6 dp del área y las X centradas en su marca. Se recalcula el chequeo de "4 dp de la etiqueta de un borde" con los anchos nuevos (≈ 12 % más angostos: menos marcas ocultas) | sí |
| 37-c Sello de Strava animado | — | Pasa a ser el sello de exportada de #67 | — |

### #66 — Íconos de exportar y glifos de Strava

- **Hoy:** el botón de la AppBar dibuja `download` con `ExportGlyphView`, siguiendo la receta de ASSET_SLOTS §4.4: trazo de 2,25, esquinas de radio 2 y extremos redondos. En CAP-22 y en 12/CAP-01 está prolijo y es del mismo peso que la flecha de "Atrás".
- **Cómo debe quedar:** queda como está (D15-02). Ya están pulidas las esquinas redondeadas que pide el issue. Sumar `download` (`F090`) e `ios_share` (`E6B8`) al recorte y sacar `cloud_upload` (`E2C3`), `cloud_download` (`E2C0`), `cloud_done` (`E2BF`), `person` (`F0D3`) y `link` / `link_off` (`E250` / `E16F`) **queda fuera**: necesita fontTools y red (ASSET_SLOTS §4.1).
- **Viable en la PC:** no, salvo que el coordinador habilite `uvx` (está en la PC). Si lo habilita, la receta es A15-11.

### #67 — Exportar desde la tarjeta y sello de exportada

| Parte | Cómo debe quedar | Viable |
|---|---|---|
| 67-a Acción en la tarjeta | El `chevron_right` 18 de la tarjeta se reemplaza por `more_vert` (`E5D4`, ya está en el recorte) 24 `text.secondary`, en una columna de 28 dp con área táctil de 48. Es la misma anatomía que la tarjeta de Rutinas (ART_BIBLE §3.7). Un toque en el resto de la tarjeta sigue abriendo el detalle. El menú es el `PopupMenu` de Rutinas (120 `ease.enter`, origen en la esquina del ancla) con un ítem: `Exportar TCX` y el glifo `download` dibujado de 20 en `text.secondary`. También se abre con un clic derecho (escritorio) o con una pulsación larga de 500 ms (teléfono, no verificable). El aviso es el mismo del detalle (12/00-arte §3) | sí |
| 67-b Sello | En el renglón 1: nombre → **sello 14** → 6 dp → fecha (el lugar que la 9 reservó para `cloud_done`). El glifo es `download_done` **dibujado** (`ExportGlyph.Kind.DownloadDone`): la bandeja de `Download` y un check (7; 12) → (10,5; 15,5) → (17; 9) en la caja de 24, con el mismo trazo de 2,25 (1,31 dp a 14) en `text.tertiary` (5,15:1, D15-06). El lector lee `Exportada` (clave nueva del PO). El nombre pierde 20 dp y se corta antes con `…`. Dónde se guarda que la sesión se exportó lo deciden el PO y el Architect (propuesta: `exportedAt` UTC en la sesión) | sí |
| 67-c Aparición | La **primera vez** que la tarjeta se ve después de una exportación correcta, el sello se estampa: escala 0,6 → 1 en 240 `ease.emphasis` y opacidad 0 → 1 en 160 `ease.enter`, desde su centro. Si la exportación fue desde la tarjeta, en el cuadro en que aparece el aviso de éxito. Las demás veces, quieto. Con reducir movimiento: fundido de 120 | sí |
| 67-d Exportar varias | Fuera (D15-04) | no |

### #76 — Tracking del HUD (y RowMate Numeric)

- **Hoy:** los rótulos del HUD con minúsculas (`Split /500m`, `Watts`, `kcal`) van con el tracking de las mayúsculas, 0,08 em. Se ven separados de más al lado de `SPM` (07/CAP-11).
- **Cómo debe quedar:** la regla va **por contenido**, no por clave: un rótulo que tiene al menos una minúscula lleva 0,04 em (4 en TextCore) y uno todo en mayúsculas (`SPM`, `BPM`, `EN MARCHA`) sigue en 0,08. Así vale igual en es y en en. Como los rótulos del HUD se dibujan (`HudText.Label`), el tracking es un parámetro del dibujo y **no agrega elementos** (el HUD está cerca del tope). A 12 dp, `Split /500m` queda 4,8 dp más angosto. El borde izquierdo de cada rótulo no se mueve más de 1 dp (sondas de la 4b) y el contraste no cambia. RowMate Numeric de Inter: resuelto por D15-01 y WD-01, no se deriva.
- **Viable en la PC:** sí (PlayMode y sondas).

### #82 — Movimiento y escena (restos)

| Parte | Hoy | Cómo debe quedar | Viable |
|---|---|---|---|
| 82-a Cascada del HUD con cambio de clase | Fundido único de 200 `ease.enter`, sin traslado (WD-02) | Tres grupos que ya existen, sin elementos nuevos: **barra de pasos y barra superior** 0–200, **tarjetas de métricas** 40–240, **banda, fila de abajo y controles** 80–280. Cada uno va de opacidad 0 → 1 y de `translateY` **+8·s** → 0 (`shift.s` por la escala del HUD), con `ease.enter`. Sigue la regla de los 400 ms: otro cambio de clase antes de 400 ms la deja en el acto. Con reducir movimiento: fundido de 120 de todo el HUD | sí |
| 82-b Ancla de desplazamiento | Solo en Rutinas | `ScrollAnchor` también en la lista del Historial, en el Detalle y en Dispositivo. Ante un cambio de ancho, el primer elemento visible queda a ±1 dp de su distancia al borde de arriba. En el Detalle, si el cursor o el resumen de paso (33-a) están a la vista, se ocultan en el cuadro del cambio | sí |
| 82-c Escena demorada (#21) | `Preparando escena…` y después la escena | Manda AC-SCN-01-07: la escena demorada aparece con un fundido de 0,6 s, **sin vuelo**. La única regla visual nueva: en el mismo cuadro `t0` en que la escena queda lista, el indicador funde a 0 en 160 `ease.exit` mientras la escena empieza su fundido de 600, sin ningún cuadro con los dos opacos ni con el fondo provisorio solo. Si la escena queda lista **antes** de que termine el negro de la entrada, el vuelo de 1 600 se ve entero. Se verifica con un video (VID-01) | sí |
| 82-d Estela | Rectángulo claro detrás del bote | D15-05: no hay parche que hacer. Va a issues con el diagnóstico | — |
| 82-e Cinta `SIMULADOR` sobre el título | La cinta (80 × 12, −45°, centro en (24; 24)) cubre la franja `x + y ∈ [39,5; 56,5]` dp. A la altura del título (y 18–38) tapa hasta x ≈ 38: `utinas`, `ispositivo` | **Solo en el build con simulador**, la AppBar suma 28 dp de relleno izquierdo (16 → **44**) con una clase en la raíz (`.rm-has-sim-ribbon`). Así el título de una pestaña arranca en 44 y la flecha de "Atrás" de una pantalla apilada, en 44–68, las dos fuera de la cinta (≥ 40). La cinta no cambia (DV-01: es el `Banner topStart` del legacy). El build normal no cambia en nada: las tomas sin simulador quedan idénticas | sí (build con simulador) |

## 2. Detalle de lo que no entra en una tabla

### 2.1 Tarjeta → detalle con elemento compartido (33-d, prioridad 2)

Solo con el Historial a la vista y la tarjeta entera en pantalla. Si no, se usa el *push* común.

| t (ms) | Ida |
|---|---|
| 0–360 | Un **fantasma** de la caja de la tarjeta (`surface.card`, `r.card`) va de su rectángulo al de la tarjeta Resumen del detalle (x, y, ancho y alto) con `dur.slow` `ease.move`. Su `StatRowView` lleva cada columna a la x y la y de la columna del Resumen, que usa el mismo componente y el mismo texto: no hace falta fundido cruzado |
| 0–120 | El renglón 1 de la tarjeta (ícono, nombre, sello, fecha y ⋮) funde a 0 con `ease.exit` |
| 0–160 | El fondo del detalle funde 0 → 1 (sin traslado); lo de atrás se oscurece con `overlay.scrim` 0 → 0,08, como en la pila |
| 200–360 | `Resumen` y el botón de la AppBar funden 0 → 1 con `ease.enter` |
| 160–400 | `Rendimiento por paso`, el selector y el gráfico: y +12 → 0 y opacidad 0 → 1 con `ease.enter`. La entrada del gráfico (09/00-arte §5.1) toma `t0` = 400 |
| 360 | El fantasma se va y el Resumen real queda en su lugar, en el mismo cuadro |

- **Vuelta:** lo mismo al revés en 280 (`ease.move` para el fantasma, y 0–120 `ease.exit` para lo demás). Si la tarjeta ya no está en la lista o quedó fuera de la vista, se usa el *pop* común.
- **Reducir movimiento:** el fundido de 120 de la pila.
- **Costo:** el fantasma (≈ 20 elementos) vive solo durante la transición, y no se vuelve a teselar nada del gráfico.

## 3. Tomas del set `it15` (carpeta de la 15)

| ID | Qué tiene que verse |
|---|---|
| CAP-01 | Detalle Escalera TV, Vatios, con el gráfico desplazado al final: escalones **sin pico** (#32), ejes en Numeric SC (D15-01) y el `0` y el eje X a la vista |
| CAP-02 | Lo mismo en SPM (era 09/CAP-08): sin `25/26` ni `10:00 11:00` encimados |
| CAP-03 | Cursor con las 5 métricas (33-b) y la sombra del tooltip (37-a), en W1280 |
| CAP-04 | Paso 3 elegido: franja al 20 %, tira de 5 dp, fila con borde y resumen del paso (33-a), en TV |
| CAP-05 | Escalera con objetivo: área al 24 / 10 % y la parada de los 7:00 con velo y línea base (33-c), en TV |
| CAP-06 | Historial TV con una sesión exportada (sello) y ⋮ en todas las tarjetas (67-a/b); sonda de color 2/2 (#35) |
| CAP-07 | Menú ⋮ abierto con `Exportar TCX`, en W1280 |
| CAP-08 | HUD TV y TH con los rótulos a 0,04 em (#76), junto a la toma de la 14 |
| CAP-09 / -10 | Rutinas y Dispositivo **con simulador** en TV: el título entero a la derecha de la cinta (82-e) |
| CAP-11 | Escena con el casco oculto (solo diagnóstico, no se publica en el INDEX como arte): ¿sigue el rectángulo? (D15-05) |
| VID-01 | Escena demorada: indicador → fundido de 600 sin vuelo (82-c) |
| VID-02 | Rotación TV → TH con la cascada (82-a), y tarjeta → detalle con elemento compartido (33-d), ida y vuelta |

`capture-snapshots.ps1` con el set `it15`. Hay que revisar que escriba en `iterations/15-deuda-pc/snapshots`.

## 4. Pedidos al Architect

| ID | Prioridad | Pedido |
|---|---|---|
| **A15-01** | 1 | #32: recorte de los puntos de control por tramo en `ChartSmoothing.Tessellate`, con sus tests de dotnet (§1 #32) |
| **A15-02** | 1 | D15-01: los ejes del gráfico con RowMate Numeric SC 600 a 11 y el chequeo de 4 dp de `AxisCrowding` con los anchos medidos de esa cara |
| **A15-03** | 1 | 37-a: `ShadowTexture` (como `GlowTexture`, generada y cacheada) y el hermano 9-slice del tooltip, con su opacidad y su escala atadas al tooltip |
| **A15-04** | 1 | 33-a: selección de paso, que es un estado del Detalle (fila ↔ tira), los colores por vértice de la malla, el resumen del paso, el desplazamiento al gráfico y cómo se suelta. Tests: el color de las franjas y que el cursor tape al resumen |
| **A15-05** | 1 | 33-c: los tramos dentro y fuera del objetivo, como alfa por vértice del área con el cruce interpolado, y los tramos sin remo (velo y línea base), calculados en Core (`ChartGeometry`) y probados con dotnet. La clave `Sin remo` la pide el PO |
| **A15-06** | 1 | #67: el ⋮ de la tarjeta, el menú, el clic derecho o la pulsación larga, `ExportGlyph.Kind.DownloadDone` y el sello con su estampado de la primera vez. El dato "exportada" lo guardan el PO y el Architect |
| **A15-07** | 1 | #76: tracking según el contenido en el dibujo de `HudText` (0,04 em si hay una minúscula), sin elementos nuevos, con las sondas de la 4b (≤ 1 dp de corrimiento) |
| **A15-08** | 1 | #82-a/b/e: la cascada en `HudReflowMotion` (3 grupos, +8·s), `ScrollAnchor` en Historial, Detalle y Dispositivo, y `.rm-has-sim-ribbon` con 44 dp de relleno en la AppBar solo con el simulador |
| **A15-09** | 2 | 33-b: el tooltip con las 5 métricas (grilla de 2 × 2, columnas de ancho fijo) |
| **A15-10** | 2 | 33-d: el elemento compartido tarjeta → Resumen (§2.1), con su vuelta y el caso de respaldo |
| **A15-11** | — | Solo si el coordinador habilita `uvx`: el recorte nuevo de #66 con ASSET_SLOTS §4.1 (`uvx --from fonttools==4.67.0`), sumar `F090` y `E6B8`, sacar `E2C3`, `E2C0`, `E2BF`, `F0D3`, `E250` y `E16F` (después de confirmar con `Icons.cs` que no se usan), poner `ios_share` 16 y retirar `ExportGlyph`. Si no, nada |
| **A15-12** | 2 | D15-05: una toma de diagnóstico con el renderer del casco apagado (CAP-11), para cerrar lo de la "estela" con evidencia |
| **A15-13** | 1 | El set `it15` de §3 en `capture-snapshots.ps1`, que escriba en la carpeta de la 15 |

## 5. Para issues

- **V14-10** (ningún issue abierto lo tiene): en W1280, las filas del Detalle miden 1 232 dp y el contenido ocupa los primeros 260 (12/CAP-01, 14/CAP-23). La propuesta es un contenido de 960 dp como máximo, centrado, en el Historial, el Detalle y Rutinas.
- **Columnas desparejas entre tarjetas:** una tarjeta sin pulso tiene 5 columnas y las demás 6, así que `Split` cae en otra x (14/CAP-12). La paridad del legacy dice 5 iguales. Es mejora EXR (grilla fija de 6 con el hueco vacío) y la decide el PO.
- **Popa del casco** que se lee como una tabla blanca en TH (07/CAP-11). Si A15-12 lo confirma, la cubierta de popa va con un degradé `#F5F5F5` → `#C9D1D6` hacia la punta y se afina en los últimos 0,6 m (va con #14 y #19).
- Zoom y desplazamiento del gráfico con gestos (33-b), y exportación múltiple (67-d).
- Recorte de la fuente de íconos de #66 si esta iteración no habilita `uvx`.
