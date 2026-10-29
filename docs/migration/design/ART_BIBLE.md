# Biblia de arte — RowMate (Unity 6 LTS + URP)

| Campo | Valor |
|---|---|
| Autor | `rowmate-designer` |
| Fecha | 2026-10-27 |
| Base | [LEGACY_VISUAL_AUDIT.md](LEGACY_VISUAL_AUDIT.md) (contrato visual del legacy), [ROADMAP](../ROADMAP.md) (iteraciones y gates de decisión, §6), [ARCHITECTURE](../architecture/ARCHITECTURE.md) (versiones, árbol, UI y escena: manda en el CÓMO), [FEATURE_INVENTORY](../product/FEATURE_INVENTORY.md), inventarios `immersive-scene.md` y `workout-engine-hud.md` |
| Complementos | [ANIMATION_BIBLE.md](ANIMATION_BIBLE.md) (todo lo que se mueve) · [ASSET_SLOTS.md](ASSET_SLOTS.md) (todo asset externo y su reemplazo procedural) |
| Alcance | Dirección de arte, tokens, tipografía, componentes de UI, layout, luz por hora, los 4 entornos, agua, bote, remero, cámaras, accesibilidad y presupuesto de rendimiento |

**Regla de lectura.** Lo marcado **[CONTRATO]** viene del legacy y no cambia de significado (colores semánticos, layout del HUD, reglas de escena). Lo marcado **[EXR]** es la mejora realista; no cambia datos ni reglas. Lo marcado **[DECIDIR]** es una propuesta que necesita OK del PO o del Architect (lista en §0.2). Los hex son `#RRGGBB` opacos; la transparencia se indica `@NN %`.

---

## 0. Resumen

### 0.1 Dirección en una frase

**"Amanecer en el agua, instrumento de regata encima."** La escena es fotográfica y tranquila (agua protagonista, luz de hora real, paisaje con escala humana); el HUD es un tablero de regata: vidrio oscuro esmerilado, números enormes de ancho fijo y el color semántico del legacy como único código de color.

| Pilar | Qué significa en concreto |
|---|---|
| Agua primero | El agua ocupa ~55 % de la pantalla; recibe el mayor presupuesto de GPU (§9) y siempre refleja cielo y orilla. |
| Luz que cuenta la hora | Cada hora tiene temperatura, dirección y contraste propios (§6). Nada se ve "plano de mediodía" salvo el mediodía. |
| Movimiento real | Remero, bote, agua y cámara responden a SPM, split, watts y fase (ver ANIMATION_BIBLE). Nada en loop desconectado. |
| Lectura a 1,5 m | Primario ≥ 7 mm de altura de mayúscula, contraste verificado contra el peor fondo posible (§10). |
| Semántica intocable | 7 colores de métrica, 4 de tipo de paso, 3 de fase, `#FF4444` fuera de objetivo, `#FC4C02` Strava (R-10). Solo se permiten variantes de luminancia del mismo matiz. |
| Presupuesto móvil | 60 fps en gama media con tres niveles de calidad; el nivel Bajo sigue siendo digno (R-04, R-12). |

### 0.2 Propuestas que necesitan decisión

El gate, el valor por defecto y si se le confirma al usuario están en [ROADMAP §6](../ROADMAP.md) (QA-P-13).

| ID | Para | Propuesta | Motivo |
|---|---|---|---|
| D-ART-01 | PO + QA | R-10 dice "valor ≥ 7:1". Se cumple para blanco, SPM, split, watts, distancia, tiempo y kcal con las variantes `*.hud` (§2.3). Para la familia roja (`hr`, `offTarget`) **es físicamente imposible** llegar a 7:1 sin volverla rosa pálida (el rojo puro tiene luminancia ≤ 0,21). Propuesta: valores de HUD = texto grande, umbral WCAG AAA de texto grande **4,5:1** para la familia roja, más redundancia de forma (flecha ▲/▼ y borde) en fuera de objetivo. | Accesibilidad real sin perder el significado del rojo |
| D-ART-02 | PO | Escala del HUD +15–25 % (SPM 64, Split/Watts 44, kcal/BPM 32, banda 34) contra 52/36/28 del legacy. Mismas zonas y mismo orden. | Cálculo de legibilidad a 1,5 m (§10.2) |
| D-ART-03 | PO | Texto terciario mínimo `@54 %` (legacy `@38 %` en fechas, ejes y etiquetas: 3,3:1, no pasa AA) y ejes de gráfico 11 px (legacy 9). | Contraste AA |
| D-ART-04 | Architect + PO | Bote y remos con **medidas reales** (manga 0,32 m, remo 2,88 m, envergadura 1,60 m) en lugar de las exageradas del legacy (manga 0,56 m, remo 3,8 m). Las funciones de geometría legacy quedan en Core para sus tests; los charcos nacen donde sale la pala visible. | En 3D realista el exagerado se ve de juguete (ver ANIMATION_BIBLE §4.6) |
| D-ART-05 | Architect | Sol "de pantalla" (disco, brillo en el agua) separado del sol "de luz" (sombras), con el mismo acimut (§6.2). | Legacy ubica el sol en coordenadas de pantalla; en 3D el sol de mediodía real quedaría fuera de cuadro |
| D-ART-06 | PO | Las 7 nubes conservan su deriva por tiempo (viento) pero pierden la deriva lateral por distancia, igual que Q-65 para el horizonte. | Un movimiento lateral por avanzar no es físico |
| D-ART-07 | PO | Cámaras extra (lateral, dron, popa) **especificadas** en §8.3 pero fuera de la paridad (BKL-02 / Q-66). En paridad: persecución legacy + "respiración" + entrada y salida cinematográficas. | Q-66 |
| D-ART-08 | Usuario | Texturas CC0 opcionales (Poly Haven, ambientCG) y textura de luna de NASA (dominio público). Sin OK se usan los procedurales y el Terrain Sample Asset Pack de Unity. | La decisión del usuario enumera Mixamo, paquetes de Unity y procedural |
| D-ART-09 | PO | Debajo del valor con objetivo, el objetivo en chico ("obj. 26"), como propone la ficha F-WKT-10. | Agrega texto al HUD |
| D-ART-10 | PO (gate de la 4a) | Llave del simulador **abajo a la izquierda** fuera del entrenamiento (16 dp del borde; 16 sobre la barra o, en una pantalla apilada, sobre el borde) y **abajo a la derecha** en el entrenamiento; el panel se abre desde la llave (§3.11). Reemplaza DV-13 y cambia AC-BLE-16-02 y -03 | En el carril derecho la llave tapa en reposo las acciones de fila (`⋮`, lápiz, cruz, tacho, *chevron*), que Material pone al final (QA-V3-09; [04a/00-deuda-diseno §1](../iterations/04a-motor-hud-funcional/00-deuda-diseno.md)) |

---

## 1. Referencia y tono

- **EXR como vara:** agua con reflejos del cielo y de la orilla, entornos naturales densos, atleta con volumen y ropa técnica, cámara que respira, HUD de bloques translúcidos. **No copiamos** su marca, sus íconos ni la paleta de intensidad de su timeline (la nuestra es por tipo de paso y es contrato).
- **Fotografía de referencia (para el moodboard del equipo, buscar imágenes propias o de stock libre):** lago Bled al amanecer con bruma; Henley-on-Thames con clubes de remo y puentes de piedra; costa atlántica con acantilados de capas y faro rojo y blanco; pista Bosbaan con boyas Albano y tableros de distancia.
- **Tono de color de la escena:** saturación natural (no "videojuego"), negros con azul (`#06122A`, nunca `#000000` en la escena), altas luces cálidas al amanecer y atardecer, frías de noche.
- **Tono de la UI:** "agua profunda": azules marinos de fondo, cian como acento, superficies con borde fino en lugar de sombras.

---

## 2. Tokens de diseño

Los tokens viven **como datos** en una sola fuente: `Assets/RowMate/Core/Design/Tokens.cs` (`RowMate.Core.Design.Tokens`, ADR 016): 122 tokens en la It. 1, 127 desde la It. 2 (`bodyM`, `bodyXs`, `button`, `accent.tonalBg`, `accent.onTonal`), 133 desde la It. 3 (`overlay.scrim`, `border.input`, `text.selection`, `titleXs`, `headerS`, `buttonS`) y **135** desde la It. 4a (`backdrop.sky`, `backdrop.water`: el fondo provisorio del inmersivo, §2.1). De ahí `tools/tokens` genera las variables USS (`--rm-*`, `Tokens.uss`) y las clases de tipo (`Typography.uss`) para UI Toolkit, y los tests de contraste de dotnet leen la misma clase. **Si esta biblia y `Tokens.cs` no coinciden, manda `Tokens.cs`.** Los valores exactos, el rol de contraste de cada token y los contrastes verificados están en [04-design-notes de la It. 1 §2 y §4](../iterations/01-fundaciones/04-design-notes.md), en [04-design-notes de la It. 2 §2.1 y §8](../iterations/02-dispositivo-simulador/04-design-notes.md) y, para las variantes del HUD, en [03-rutinas/00-deuda-diseno §2](../iterations/03-rutinas/00-deuda-diseno.md); los de Rutinas y el editor, en [03-rutinas/04-design-notes §2 y §8](../iterations/03-rutinas/04-design-notes.md). Cada token tiene un solo valor: el texto y su fondo, el anillo y su halo, y cada tamaño tipográfico son tokens separados (`state.connected` / `state.connectedBg`, `focus.ring` / `focus.ringHalo`, `accent.onTonal` / `accent.tonalBg`, `num.m` / `num.band`).

### 2.1 Fondos, superficies, texto y bordes de la app

| Token | Hex | Uso | Origen |
|---|---|---|---|
| `bg.app` | `#0D1B2A` | fondo de toda pantalla no inmersiva | [CONTRATO] legacy |
| `bg.sunken` | `#08131F` | áreas hundidas: campos, pistas de sliders, fondo del gráfico | nuevo |
| `surface.card` | `#1A2E45` | tarjetas (dispositivo, rutinas, historial, editor) | [CONTRATO] |
| `surface.raised` | `#23364C` | filas internas, tarjeta presionada, elemento seleccionado de lista | legacy medido |
| `surface.overlay` | `#1E3550` | diálogos, menús, hojas inferiores | nuevo |
| `overlay.scrim` | `#000000` | velo de los diálogos (opacidad 0,6), oscurecido de lo de atrás en la pila (0,08) y capa de estado del ítem de peligro del menú (12 / 24 %); la opacidad la pone el código | It. 3 |
| `surface.nav` | `#0A1622 @92 %`, **sin desenfoque**, + borde superior 1 px `border.subtle` | barra de pestañas translúcida (§3.11). UI Toolkit no desenfoca lo que queda debajo y el desenfoque de la escena no está detrás de la barra (QA-P-22). Peor caso, con texto blanco debajo: `accent.primary` 6,0:1, `text.secondary` 8,0:1 | nuevo (reemplaza `#1C2024` de M3) |
| `nav.indicator` | `#00B4D8 @18 %` | pastilla del destino seleccionado | nuevo |
| `border.subtle` | `#FFFFFF @8 %` | borde de vidrio, separadores de barra | legacy |
| `border.default` | `#FFFFFF @12 %` | separadores, miniatura no elegida | legacy |
| `border.strong` | `#FFFFFF @24 %` | botón contorno neutro, foco inactivo, segmentado | legacy |
| `border.input` | `#FFFFFF @40 %` | borde de la caja de los campos en reposo: 3,807:1 contra `bg.sunken` (con `border.strong` daba 2,132) | It. 3 |
| `focus.ring` | `#00B4D8` | anillo de foco de teclado (desktop), grosor `stroke.focus` 2. 5,6:1 sobre card | nuevo |
| `focus.ringHalo` | `#00B4D8 @30 %` | halo del foco, grosor `stroke.focusHalo` 4 (decorativo: el anillo cumple solo) | nuevo |
| `text.primary` | `#FFFFFF` | 13,8:1 sobre `surface.card` | legacy |
| `text.secondary` | `#FFFFFF @70 %` (≈ `#BAC0C7`) | subtítulos, etiquetas | 7,6:1 sobre card |
| `text.tertiary` | `#FFFFFF @54 %` (≈ `#969FA9`) | fechas, ejes, ayudas | 5,1:1 [DECIDIR D-ART-03] |
| `text.disabled` | `#FFFFFF @38 %` | solo deshabilitado o decorativo | 3,3:1 (permitido para deshabilitado) |
| `text.onAccent` | `#04131C` | texto sobre cian, menta, amarillo | 7,6:1 sobre `#00B4D8` |
| `text.selection` | `#00B4D8 @35 %` | selección de texto en los campos (blanco encima 9,586:1) | It. 3 |
| `brand.seed` | `#0077B6` | segmento seleccionado (texto blanco 4,868:1), riel de la serie, encabezado de serie (`@12 %`), cuerpo y pie (`@6 %`), hojas (`@20 %` / `@12 %`) | [CONTRATO] |
| `accent.primary` | `#00B4D8` | acento general (íconos de remo/rutina, borde de miniatura elegida, `Serie` y el botón del pie de la serie, cápsula del chip ×N a `@20 %`, índices) | [CONTRATO] |
| `accent.primaryPressed` | `#0096B4` | botón primario presionado | variante |
| `accent.tonalBg` | `#00B4D8 @12 %` | fondo del botón tonal (decorativo) | It. 2 (con `@16 %` el acento encima no llegaba a AA) |
| `accent.onTonal` | `#38CBEF` | texto e ícono del botón tonal: 5,9:1 sobre el tonal en la tarjeta, 5,2:1 con *hover* | It. 2. Mismo matiz que `accent.primary`, más claro; coincide con el `metric.spm.hud` de la It. 1, pero es un token independiente (no sigue sus cambios, §2.3). Desde la It. 3, también el número del chip `×N`: 5,620:1 sobre su cápsula (`accent.primary` daba 4,370) |
| `backdrop.sky` | `#0B4FA8` | fondo provisorio del inmersivo, del borde de arriba al horizonte de la cámara (0,40 del alto en vertical, 0,42 en horizontal). Es el `skyTop` de las 12 h del legacy; el vidrio encima da `#08162A` y el blanco directo 7,783:1 | It. 4a (DN4a-03); se va, o queda como fondo de carga, con la escena de la 6a |
| `backdrop.water` | `#0E4F8F` | el mismo fondo, del horizonte al borde de abajo (`waterNear` de las 12 h del legacy); el vidrio encima da `#081627`. Los dos tonos están a 0,008 de luminancia: se lee parejo, con el cielo apenas más vivo | It. 4a (DN4a-03) |

### 2.2 Semántica — métricas, pasos, fases y estados [CONTRATO]

| Familia | Token | Hex base | Uso |
|---|---|---|---|
| Métrica | `metric.split` | `#FF9F1C` | split /500 m, parciales |
| | `metric.spm` | `#00B4D8` | SPM |
| | `metric.watts` | `#FFD60A` | vatios |
| | `metric.distance` | `#4CC9F0` | distancia, reps y metros en paneles |
| | `metric.hr` | `#EF476F` | pulso |
| | `metric.time` | `#E0E0E0` | tiempo |
| | `metric.kcal` | `#06D6A0` | calorías |
| Paso | `step.work` | `#EF476F` | trabajo |
| | `step.rest` | `#06D6A0` | descanso |
| | `step.warmup` | `#FFD166` | calentamiento |
| | `step.cooldown` | `#118AB2` | enfriamiento |
| | `step.unknown` | `#607D8B` | otro |
| Fase | `phase.running` | `#06D6A0` | EN MARCHA (bg `@15 %`, borde `@50 %`) |
| | `phase.paused` | `#FFD166` | PAUSADO |
| | `phase.finished` | `#EF476F` | FINALIZADO |
| Estado | `state.offTarget` | `#FF4444` | valor fuera de objetivo |
| | `state.success` | `#06D6A0` | completado, reanudar (texto `text.onAccent`) |
| | `state.connected` | `#69F0AE` | texto e ícono del chip "Conectado", RECEIVING (9,7:1 sobre card; 7,0:1 sobre su chip en la tarjeta; 8,8:1 sobre la insignia de Dispositivo, que va sobre `bg.app`) |
| | `state.connectedBg` | `#4CAF50 @20 %` | fondo del chip "Conectado" |
| | `state.warning` | `#FF9800` | texto del aviso "remo no conectado" (6,4:1 sobre card; 5,0:1 sobre su aviso) |
| | `state.warningBg` | `#FF9800 @15 %` | fondo del aviso |
| | `state.danger` | `#FF5252` | terminar, desconectar (contorno; no es texto) |
| | `state.dangerText` | `#FF7272` | texto de error, de Terminar y de Desconectar: 5,2:1 sobre card, 4,6:1 sobre `surface.raised`, 4,7:1 sobre `surface.overlay` y 5,1:1 en el peor vidrio (`#FF5252` da 4,3:1 sobre card). Era `#FF6B6B`, que daba 4,4:1 sobre `surface.raised` |
| | `state.dangerFill` | `#D32F2F` | borrar rutina (texto blanco 5,0:1) |
| Marca | `brand.strava` | `#FC4C02` | todo lo de Strava (botón oficial de Strava según su guía de marca) |
| Dev | `dev.simulator` | `#FF5722` | cinta SIMULADOR (5,5:1 contra `bg.app`) |
| | `dev.simulatorText` | `#1A0A00` | texto de la cinta (6,1:1 sobre `dev.simulator`) |
| | `dev.bleDebug` | `#18FFFF` | texto mono del diagnóstico BLE (15,2:1 sobre su fondo) |
| | `dev.bleDebugBg` | `#000000 @38 %` | fondo del diagnóstico BLE |

Íconos por tipo de paso [CONTRATO]: calentamiento `whatshot`, trabajo `fitness_center`, descanso `pause_circle`, enfriamiento `ac_unit` (Material Symbols Rounded, ver ASSET_SLOTS `ui.icons`). El descanso usa `pause_circle` (`E1A2`) de la fuente **sin relleno**, que es el `pause_circle_outline` del legacy (P-07).

**Regla de color de tipo (It. 3; también fuera del HUD):** el **texto y los íconos** del tipo usan la variante `step.*.hud` (título de la fila del editor, ícono de su insignia, mosaico del diálogo, vista previa); los **gráficos** usan el color base (borde izquierdo de la fila, fondo de la insignia y del mosaico elegido `@20 %`, borde del mosaico elegido, tramos de la mini-barra). Con la base, el título del trabajo daba 3,815:1 sobre la tarjeta y el ícono del enfriamiento 2,744 sobre su insignia; con `.hud`, 4,685 y 3,723 ([03-rutinas/04-design-notes §2.3](../iterations/03-rutinas/04-design-notes.md); DV-12 ampliada).

### 2.3 Variantes de luminancia para el HUD (`*.hud`)

Se calculan con OKLCH (mismo matiz que la base, solo sube la claridad y baja el croma si sale de gamut) contra el **peor fondo posible del vidrio**: vidrio `#070D16 @86 %` (token `glass.fallback`, que es también el alfa máximo del vidrio adaptativo, §3.2) sobre una nube blanca = `(41,72; 46,88; 54,62)`, que en pantalla es `#2A2F37` (luminancia 0,028).

**Criterio con margen de render (It. 3, QA-V2-12).** Cada variante llega a su umbral contra **`#2B3038`**: el peor vidrio ya renderizado en 8 bits (`#2A2F37`, el que midió R-T1 en la It. 2: la mezcla se cuantiza hacia arriba) más 1 por canal (GPU, dithering, sonda). Equivale a ≈ 1,7 % de margen sobre el cálculo sin redondear. Con el criterio anterior (umbral contra el cálculo sin redondear, sin margen) el pulso daba 4,508 en el cálculo y **4,497 en el píxel real**, y el Split 7,004 y 6,987: los dos por debajo del umbral en pantalla. Detalle y números en [03-rutinas/00-deuda-diseno §2](../iterations/03-rutinas/00-deuda-diseno.md); lo prueba `ContrastTests.HudKeepsItsThresholdOnTheRenderedWorstGlass` (§11).

| Token | Hex | Contraste (:1): cálculo sin redondear · contra `#2B3038` | Regla |
|---|---|---|---|
| `metric.split.hud` | `#FFAA47` (era `#FEA845`) | 7,127 · 7,009 | valor de Split en HUD |
| `metric.spm.hud` | `#3ACCF1` (era `#38CBEF`) | 7,124 · 7,006 | valor de SPM en HUD |
| `metric.watts.hud` | `#FFD60A` (= base) | 9,557 · 9,399 | |
| `metric.distance.hud` | `#4ECBF2` (era `#4CC9F0`, la base) | 7,163 · 7,044 | |
| `metric.time.hud` | `#E0E0E0` (= base) | 10,221 · 10,052 | |
| `metric.kcal.hud` | `#06D6A0` (= base) | 7,154 · 7,035 | |
| `metric.hr.hud` | `#FF5D7F` (era `#FF5A7E`) | 4,577 · 4,502 | D-ART-01; a 7:1 sería `#FFA1AD` (rosa pálido). También es la etiqueta, el título y la unidad del pulso en Dispositivo (4,685:1 sobre card) |
| `state.offTarget.hud` | `#FF625B` (era `#FF5F59`) | 4,594 · 4,518 | + flecha ▲/▼ + borde `@70 %` (D-ART-01); a 7:1 sería `#FFA29A` |
| `step.*.hud` (texto de tipo y chips) | work `#FF5D7F` (= `metric.hr.hud`), rest `#06D6A0`, warmup `#FFD166`, cooldown `#37A2CB` (era `#35A0C9`), unknown `#7D9BA9` (era `#7C9AA8`) | ≥ 4,5:1 contra `#2B3038` | texto chico de la barra de pasos y, desde la It. 3, título e ícono de tipo en el editor, los mosaicos y la vista previa (§2.2) |
| `text.hudPrimary` | `#FFFFFF` | 13,493 · 13,269 | valor con objetivo cumplido (Q-60) |

Los cambios de la It. 3 suben la claridad OKLCH entre 0,003 y 0,006 (ΔE OK ≤ 0,0062, por debajo de lo que se distingue a simple vista) y no tocan ningún color de contrato.

Las **etiquetas** del HUD ("SPM", "Split /500m"…) usan el color base de la métrica (≥ 4,5:1 salvo hr, que usa `metric.hr.hud`). Los **rellenos gráficos** (píldoras, bordes, glow, líneas del gráfico) usan siempre el **hex base**: son elementos gráficos, no texto. Ningún texto del HUD se apoya sobre el reflejo interior del vidrio ni sobre el glow, ni baja su opacidad en pausa (§3.2): los tres se comen el margen.

### 2.4 Espaciado, radios, bordes y profundidad

- **Unidad:** 1 px de USS = 1 dp. `PanelSettings` en `ConstantPixelSize` con `scale` = densidad del sistema (Android `DisplayMetrics.density`, Windows `dpi / 96`): "1 dp = 1 px × densidad", igual que el legacy (D-03 de la It. 2; *Constant Physical Size* a 160 dpi usaba los dpi físicos y podía dar un 2-5 % distinto).
- **Espaciado** (`space.dp<N>`; un segmento de nombre no puede empezar con un dígito): `space.dp2` 2 · `space.dp4` 4 · `space.dp6` 6 · `space.dp8` 8 · `space.dp12` 12 · `space.dp16` 16 · `space.dp20` 20 · `space.dp24` 24 · `space.dp32` 32 · `space.dp40` 40 · `space.dp48` 48.
- **Radios:** `r.xs 4` (píldora de paso), `r.s 6` (chips chicos), `r.m 8` (chips, cajas internas), `r.l 12` (botones, avisos, miniaturas), `r.glass 14` (vidrio HUD) [CONTRATO], `r.card 16` (tarjetas), `r.full 999` (banda tiempo/distancia, píldoras de nav).
- **Grosores** (`stroke.*`): `stroke.hairline` 1 (vidrio, separadores) · `stroke.target` 1,5 (tarjeta con objetivo activo, contornos de botón) · `stroke.focus` 2 (anillo de foco, campo con foco) · `stroke.selected` 2,5 (miniatura de escenario elegida [CONTRATO]) · `stroke.accent` 3 (acento superior del tablero y borde izquierdo de paso [CONTRATO]) · `stroke.focusHalo` 4 (halo del foco).
- **Bordes:** `stroke.hairline` `border.subtle` en vidrio; `stroke.target` del color con objetivo activo; borde superior `stroke.accent` del color de la métrica en las tarjetas del tablero [CONTRATO].
- **Desplazamientos** (`shift.*`, ANIMATION_BIBLE §1.3): `shift.s` 8 · `shift.m` 12 · `shift.l` 16 · `shift.xl` 24, que es el tope para cualquier elemento del HUD.
- **Profundidad sin sombras:** UI Toolkit no tiene `box-shadow`. La jerarquía se arma con escalones de superficie (`bg.app` → `surface.card` → `surface.raised` → `surface.overlay`) y bordes. Los "glow" (objetivo activo, pulso del paso) se dibujan en el material del vidrio por SDF (§3.2).

---

## 3. Componentes de UI

Tecnología (ARCHITECTURE §7): **UI Toolkit (UXML + USS)** con **UI Shader Graph** para el vidrio (Unity `6000.3.27f1` + URP 17.3, que lo soportan). Los íconos son glifos de la fuente "RowMate Icons" (decisión cerrada al iniciar la It. 2, ASSET_SLOTS §4); mientras la fuente no esté descargada se dibujan los íconos provisorios de ASSET_SLOTS §4.4, del mismo tamaño y color. Todos los tamaños en dp a escala 1; el HUD escala con `hudScale` (§5.3).

### 3.1 Tarjeta de métrica (pestaña Dispositivo, "tablero")

| Parte | Spec |
|---|---|
| Contenedor | `surface.card`, `r.card` 16, padding 16, sin sombra ni borde salvo el superior [contrato]. No es accionable: sin *hover* ni presión |
| Acento superior | `stroke.accent` 3 dp del color base de la métrica **`@60 %` en reposo** (legacy `withOpacity(0.6)`): split `#A3722C`, SPM `#0A7E9D`, vatios `#A39322`, distancia `#388BAC`, pulso `#9A3D5E` sobre la tarjeta. Es decorativo (no es texto) |
| Brillo (luz de tablero) | degradé vertical del color de la métrica que baja **24 dp** desde el borde superior: una textura blanca de 1 × 64 con alfa `smoothstep` (1 arriba → 0 abajo), generada al arrancar y teñida desde USS (`-unity-background-image-tint-color`), radios superiores 14, estirada (`stretch-to-fill`). Opacidad **0,084 en reposo** (= pico 0,14 × 0,60) y **0,14** al llegar un dato. Con 18 % (el valor anterior) el brillo se volvía una bruma gris sobre la tarjeta azul en naranja y amarillo |
| Encabezado | fila centrada: ícono 14 + 6 + etiqueta `label` 12/600, tracking 0,08 em. Ícono y etiqueta en el **color base `@100 %`** (sobre card: split 6,7:1, SPM 5,6, watts 9,8, distancia 7,2; con el brillo a la altura de la etiqueta, ≥ 5,4). El **pulso** usa `metric.hr.hud` (4,685:1; 4,58 con el brillo), porque la base da 3,8 (DV-03). Las etiquetas llegan del catálogo con su caja (`SPLIT 500m`): no hay mayúsculas por USS |
| Valor | `num.xl` (52, Barlow Semi Condensed 700, cifras tabulares con `TabularNumber`), color base de la métrica (texto grande, ≥ 3:1: el mínimo es el pulso con 3,8:1). Centrado en el espacio bajo el encabezado, con la **banda de las mayúsculas** centrada en su caja (centrado óptico: en Barlow la banda queda 0,05 em por debajo del centro de la caja de línea). Si no entra, `FitBox` lo achica **sin tope inferior** (D-10; `123456m` en 2 columnas a 411 dp: escala 0,707); cuando el largo cambia, la escala sigue un resorte (ANIMATION_BIBLE §2.13) |
| Marcadores (`--:--`, `0.0`, `0`, `0m`) | mismos colores y tamaño que el valor (paridad); el guion ocupa su avance real (0,37 em), así `--` se lee como dos guiones |
| Unidad | en el tablero no hay unidad aparte: `226m` es parte del número (paridad). La única es `bpm` en la tarjeta del pulsómetro (fila Pulsómetro) |
| Estado "dato nuevo" | solo en las tarjetas cuyo **texto** cambió: el acento sube a `@100 %` y el brillo a 0,14, y vuelven a `@60 %` y 0,084 en 300 ms (`ease.pulse`, ANIMATION_BIBLE §2.4) |
| Grilla | ancho = ventana − 40; > 600 → 3 columnas y aspecto 1,6; si no, 2 y 1,3; separación 12 [CONTRATO]. 4 columnas en tablet: propuesta fuera de paridad, no implementada (P-02 de la It. 2) |
| Pulsómetro | misma tarjeta a ancho completo (`margin-top` 12 bajo la grilla y 64 dp de aire hasta el final del contenido), con acento `metric.hr @60 %` **fijo** y brillo en reposo (sin destello: su signo vital es el corazón). Encabezado alineado a la izquierda: corazón `favorite` **relleno** 14 `metric.hr` (como el legacy, `heart_rate_card.dart:39`; el contorno queda para las filas de pulsómetros encontrados, `favorite_border`, `:138`) + 6 + título `label` en `metric.hr.hud` (el `@70 %` del legacy da 2,5:1). Corazón pleno conectado (late solo con pulso > 0, ANIMATION_BIBLE §2.9) y `@40 %` en cualquier otro estado [CONTRATO]. Conectado: nombre `bodyM` 14 `text.secondary` + valor `num.xl` `metric.hr` + 6 + unidad `bpm` en `bodyM` 14/400 **`metric.hr.hud`** en la misma línea de base que el número (DV-04, QA-G2-08: con `text.secondary` era blanca y con `metric.hr` daba 3,8:1) + `Desconectar` (texto peligro). Error: texto suelto `bodyXs` `state.dangerText`, hasta 3 líneas, **sin** la caja de §3.10 (paridad). Cuerpo por estado (sin sensor, buscando, conectando, conectado, recordado): [04-design-notes de la It. 2 §4.6](../iterations/02-dispositivo-simulador/04-design-notes.md) |
| Cabecera de Dispositivo | fila centrada que puede ajustar (`flex-wrap`): ícono `rowing` 32 `accent.primary` + 12 + nombre `title` con "…" + 12 + insignia `Conectado` (relleno 4 / 10, radio 12, `state.connectedBg`, ícono 12 + 4 + `bodyXs`, `state.connected`). Si no entra (360 dp con letra 1,3×), la insignia baja a una segunda línea centrada |

### 3.2 HUD glass (tarjetas del entrenamiento)

**Material del vidrio** (`RowMate/UI/Glass`, UI Shader Graph):

1. Fondo = textura global `_RmHudBlur`, muestreada en coordenadas de pantalla. La genera `HudBlurFeature` (ARCHITECTURE §7.4): copia la escena a 1/4 de resolución y la desenfoca con 3 pasadas Kawase. El resultado va a un `RTHandle` **persistente** que se publica como global al final de la pasada, porque la UI en *Screen Space Overlay* se dibuja fuera del Render Graph de la cámara y una textura transitoria ya no existiría (QA-P-16). Llega en la It. 6b (P-04); antes, y en el nivel Bajo, se usa el respaldo de abajo.
2. Tinte `#070D16` con **alfa adaptativo**: `a = lerp(0,72; 0,86; smoothstep(0,10; 0,70; L(fondo)))`, donde `L(fondo)` es la luminancia de `_RmHudBlur` bajo el píxel. Con el borde inferior en 0,10 el peor caso es el blanco (el de §2.3) en todo el cubo RGB. Con el 0,15 de la versión anterior no: sobre un celeste de cielo (`(120; 160; 220)`, alfa 0,760) el compuesto quedaba **más claro** que el vidrio sobre blanco (luminancia 0,0292 contra 0,0278), y el pulso nuevo bajaba a 4,496 (It. 3, QA-V2-12; [03-rutinas/00-deuda-diseno §2.6](../iterations/03-rutinas/00-deuda-diseno.md)). El test de la 6b: el compuesto nunca supera la luminancia del vidrio sobre blanco.
3. Brillo de borde: 1 px `#FFFFFF @10 %` [CONTRATO] + reflejo interior superior `#FFFFFF @6 % → 0` en los **8 dp superiores** (sensación de vidrio), dentro del relleno superior de la pieza: **nunca debajo de un texto**. Con el reflejo en el 40 % superior (versión anterior), a la altura de la etiqueta vale ≈ 3 % y baja el contraste un 9 %: el pulso caía a 4,1:1 y el SPM base a 5,0:1 (QA-V2-12).
4. Glow por SDF del rectángulo redondeado: con objetivo activo, color de la métrica `@20 %`, radio 12 [CONTRATO]; en el pulso de fin de paso, 0,3→0,7 (ANIMATION_BIBLE §2.6). **Solo por fuera del borde**: no tiñe el interior del vidrio. Si lo tiñera (como pasa con un `BoxShadow` debajo de una tarjeta translúcida), el valor fuera de objetivo en una tarjeta de SPM caería a 3,3:1 (QA-V2-12).

**Respaldo sin desenfoque** (QA-P-17). Se usa en las It. 4a a 6a (el desenfoque llega en la 6b), en el nivel Bajo y en todo equipo donde `_RmHudBlur` no esté disponible:

- Tinte `#070D16` **fijo `@86 %`** en todas las piezas de vidrio del HUD: tarjetas, banda tiempo/distancia, barra de pasos, paneles de series y controles.
- Borde y radio del legacy: 1 px `#FFFFFF @10 %`, `r.glass` 14. Se conserva el reflejo interior superior.
- Glow con un 9-slice suave generado por código al iniciar (sin asset).

Por qué `@86 %`: sin desenfoque no hay luminancia de fondo para medir, así que se usa el alfa máximo del vidrio adaptativo, que es el que garantiza el peor caso de §2.3 (vidrio sobre una nube blanca = `#2A2F37`):

- Con `@86 %`: blanco 13,5:1, métricas `*.hud` ≥ 7,0:1 y familia roja ≥ 4,5:1 (D-ART-01).
- Con `@80 %` no alcanza: el peor caso es `#393D45` y da split y SPM 5,6:1, pulso y fuera de objetivo 3,6:1.
- El `#000000 @55 %` del legacy al que reemplaza da 4,8:1 en blanco en el peor caso.

En la It. 4a el fondo es provisorio: dos tonos planos del mediodía del legacy, `backdrop.sky` arriba del horizonte y `backdrop.water` abajo, en **un solo elemento** con una textura de 1 × 100 (filtro *point*, sRGB, sin *mipmaps*: el horizonte queda recto y los tonos con su hex exacto). El vidrio se ve como un panel oscuro sobre el cielo (`#08162A`) y el agua (`#081627`); sobre `bg.app` daba `#080F19` y las tarjetas casi desaparecían. El número de `@86 %` importa desde la It. 6a, con el cielo real detrás (el contraste ya está calculado contra una nube blanca).

**Scrims de pantalla** (detrás del HUD, delante de la escena): degradé superior `#06122A @38 % → 0` en el 24 % alto de la pantalla y degradé inferior `@32 % → 0` en el 18 % bajo. Nunca oscurecen la franja central donde va el bote.

| Variante | Valor | Etiqueta | Unidad | Ancho mín. | Padding |
|---|---|---|---|---|---|
| Grande (SPM) | `num.hero` 64/800 | 13/600 tracking 0,08 em, color base | 14/500, `.hud` de su métrica `@80 %` | 140 | 16×10 |
| Mediana (Split, Watts) | `num.l` 44/700 | 12/600 | 13/500, `.hud @80 %` (la `W` de Watts) | 118 | 14×10 |
| Chica (kcal, BPM) | `num.m` 32/700 | 11/600 | 12/500, `.hud @80 %` | 96 | 12×8 |

**Unidades (It. 4a, DV-31):** en la variante `.hud` de su métrica al 80 % (por código: USS no aplica alfa a un `var()`), en la línea de base de las cifras y sin cambiar con el semáforo. El color base al 55 % del legacy y de la versión anterior de esta biblia daba 3,967:1 (`W`) y 3,188 (`m`) en el peor vidrio; al 80 %, 6,678 y 5,157.

| Estado | Valor | Etiqueta y borde | Extra |
|---|---|---|---|
| Normal | `metric.*.hud` | etiqueta color base; borde `#FFFFFF @10 %` 1 px | — |
| Con objetivo, cumple | `text.hudPrimary` (blanco) | etiqueta color base; borde color base `@70 %` 1,5 px + glow `@20 %` solo por fuera (Q-60) | el **hueco de la flecha** vacío; `obj. 26` 12/600 `text.secondary` debajo (D-ART-09) |
| Con objetivo, fuera | `state.offTarget.hud` | igual que "cumple" | ▲ (subir SPM/W) o ▼ (bajar split) en el hueco (D-ART-01) |
| Pausa | valores siguen actualizándose (contrato) y **al 100 %**: ni la tarjeta ni su texto bajan de opacidad (el legacy tampoco lo hace) | igual | la pausa la dicen el velo de la escena (ANIMATION_BIBLE §2.11), el botón Reanudar y el chip de fase. Con la tarjeta al 85 % (versión anterior), sobre una nube blanca el pulso daba 3,2:1 y el Split 4,8:1 (QA-V2-12) |

**Anatomía con objetivo (It. 4a, DN4a-07 y DN4a-15; [04a/04-design-notes §3.2](../iterations/04a-motor-hud-funcional/04-design-notes.md)):**

- **El hueco de la flecha** mide 0,48 em del valor (30,7 dp a 64; 21,1 a 44) y existe **toda la duración de un paso con objetivo**, aunque se cumpla: al cruzar el umbral cambian solo el color y la flecha, y el número no se corre ("no baila"). Aparece y se va solo al cambiar de paso, con la coreografía de ANIMATION_BIBLE §2.7.
- **La flecha**, Inter 700 a 0,4 em del valor, pegada a la izquierda del hueco, con su tinta centrada en la banda de las mayúsculas de las cifras: su línea de base va 0,196 em del valor (▲) o 0,214 em (▼) por encima de la de las cifras. Entre su tinta y la primera cifra quedan 0,084 em más el lado de la cifra.
- **`obj.`** (`hudTarget`: `obj. 22`, `target 22`) 2 dp debajo del valor, con el `margin-left` del hueco: empieza donde empiezan las cifras, así el objetivo queda justo bajo el valor real.
- Medidas en `em` y líneas de base las calcula el código en px (USS no tiene `em` ni `align-items: baseline`).

```
SPM            SPM
   24.0     ▲  23.5
   obj. 22     obj. 22
```

**Banda tiempo │ distancia** [CONTRATO]: cápsula `r.full`, mismo vidrio, padding 20×10; ícono cronómetro 16 `metric.time @70 %` centrado sobre el valor + valor `num.band` 34 `metric.time.hud`; divisor 1×32 `@12 %` con 16 a cada lado; ícono regla 16 `metric.distance @70 %` + valor `num.band` `metric.distance.hud` + 3 + `m` 13/500 en `metric.distance.hud @80 %` (DV-31), en la línea de base. **En horizontal, compacta** (It. 4a, DV-33): relleno 14 y 10 a cada lado del divisor, porque comparte la fila con los controles (`00:42 │ 1048 m` mide 216,1 en lugar de 240,1). Las dos cifras comparten **una sola escala** cuando no entran (la banda se lee pareja); el relleno, el divisor, los íconos y la unidad no se achican.

### 3.3 Barra de pasos y píldoras

- **Contenedor** [CONTRATO]: margen horizontal 12, vidrio (alfa base 0,72), `r.glass`, borde `@8 %`. Padding píldoras 10/8/10/4; fila de info 12/0/12/8.
- **Píldora** [CONTRATO]: alto 8, radio 4, separación 3, mismo ancho para todas. Estados: pendiente `@18 %`; en curso pista `@30 %` + relleno `@100 %` proporcional; hecha `@85 %`; hecha sin cumplir = interior `@18 %` + borde 1,5 px `@85 %`. [EXR] el relleno avanza continuo (ANIMATION_BIBLE §2.5) y la píldora en curso tiene un brillo especular de 1 px `#FFFFFF @25 %` en el borde superior del relleno.
- **Fila de info:** tipo de paso 13/700 tracking 0,04 em en `step.*.hud`; pastilla de objetivo bg color base `@20 %`, `r.s`, 6×2, 11/600 **`text.primary`** (blanco) con `trending_up` a 1,17 em en `metric.distance`, recorte con "…" (nunca empuja la cuenta); `Próximo:` / `Up next:` localizado (DV-24) en `text.secondary` 10/400 + el tipo siguiente **`text.primary`** 10/700, sobre un fondo del color base del paso siguiente `@20 %` con borde `@50 %`, `r.m`, 7×3, alto 20,4; cuenta regresiva `num.s` 20/700 blanca (escala 1,1 por terminar, con resorte, no salto, y el color blanco ↔ el del paso con el reloj común); reloj de pared 18/700 tabular con ícono 16 `@70 %` (4b).
  - **Por qué blanco (It. 4a, DV-38, aceptada por el PO):** el texto en el color del tipo sobre su tinte daba 3,719:1 (trabajo) y 3,688 (enfriamiento) en el peor fondo, y `Up next:` al 60 % sobre el 25 % del legacy, 3,758. En blanco: 8,134 a 10,962; `Próximo:` al 70 % sobre el 20 %, 4,977 a 6,301 (04a/04-design-notes §4.5, §9.1).
  - **Lo que late en `Próximo`** es solo su fondo y su borde (0,6 ↔ 1 en 600 + 600, ANIMATION_BIBLE §2.6); el texto queda siempre entero. Con el latido del legacy en toda la pastilla el texto bajaba a ≈ 3:1 en el punto más tenue.
- **La barra de la 4a (sin píldoras, DN4a-04):** una sola fila de 22 de alto; borde 1, relleno 8, fila, relleno 8, borde 1: **40** de alto, igual que la barra libre, así SPM queda a la misma distancia en libre y con rutina. Relleno a los lados 12 (rutina) y 14 (libre). En la 4b, la fila de píldoras se suma arriba con las medidas de esta sección y la de información queda igual.
- **Barra de libre:** ícono remo `accent.primary` 20, 8, título 16/600 blanco; en la 4b, el reloj y el chip de fase (`phase.*` bg `@15 %`, borde `@50 %`, `r.m`, 8×3, 11/800 tracking 0,08 em) con punto "en vivo" de 6 px que late en EN MARCHA.

### 3.4 Paneles de series (parciales y repeticiones)

Mismo vidrio; ancho colapsado mín. 140, **expandido fijo 200** [CONTRATO] (en tablet 240). Colapsado: chip con ícono 14 `@80 %`, texto 13/600 en `metric.split` o `metric.distance`, flecha 18 `@54 %`. Expandido: vuelta en curso `num.lap` 34/700 `metric.split.hud` + metros 14 `@55 %`; filas `#n` 15/600 `@60 %` en columna de 52 + tiempo `num.row` 24/700 ámbar; "Rep r/R · N m" 16/700 `metric.distance.hud`; textos vacíos 15 `@54 %`. Separación entre filas 2; entre bloques 6.

### 3.5 Controles del inmersivo y diálogos

| Botón | Spec |
|---|---|
| Terminar | contorno `state.dangerText` (`#FF7272`) 1,5 px, texto `state.dangerText` 16/600, **mismo vidrio del HUD** (adaptativo; respaldo `glass.fallback` `@86 %`): 5,1:1 en el peor caso. Antes era vidrio `@45 %` con `#FF5252`, que daba 1,0:1 sobre un cielo blanco. Mín. **128×56** (legacy 120×48; más grande para manos transpiradas), `r.l` |
| Pausa | mismo vidrio del HUD, contorno `border.strong` 1,5 px, ícono pausa 22 + texto 16/600 blanco, mín. 148×56 |
| Reanudar | relleno `state.success`, texto `text.onAccent` 16/700 (10,0:1), ícono play, mín. 148×56. `Pausa` y `Reanudar` miden lo mismo: el cambio no mueve nada |
| Estados de los controles del HUD (It. 4a) | *Hover* (escritorio) y presionado (`:hover:active`) con la capa de la app, salvo `Terminar`, que lleva la capa **oscura** (`overlay.scrim` al 12 % y al 24 %: 5,483 y 5,892:1; con la blanca del 4 % daba 4,494), como el ítem de peligro de los menús; escala 0,97 al presionar (sin escala con reducir movimiento). **Anillo de foco del HUD**: blanco (`text.primary`) de 2 dp a −4 del borde de afuera (radio 16) con un halo `overlay.scrim` al 50 % de 4 dp pegado por fuera (radio 20): 3,977:1 sobre cualquier fondo de la escena (el cian de la app daba 2,465 sobre una nube). Tab: `Terminar` → `Pausa` / `Reanudar` → la llave (simulador). Al confirmar `Terminar`, los dos al 0,38 en el acto; si el guardado tarda más de 300 ms, `Terminar` muestra el *spinner* de 16 en `state.dangerText` a la izquierda de su texto, sin cambiar de ancho |
| Diálogo de terminar (It. 4a) | **el diálogo de la app, opaco** (fila siguiente), de 331 en un teléfono de 411: título `title` 20/600 (`Terminar entrenamiento` entra en una línea), cuerpo `body` 15 `text.secondary`, `Cancelar` de texto y `Terminar` primario relleno. Sin desenfoque (6b), el "vidrio `@92 %`" de la versión anterior solo dejaba ver el HUD fantasma detrás del velo; es además la paridad (el legacy usa un `AlertDialog` estándar). La clase `.rm-dialog--hud` queda reservada, sin reglas, para el diálogo de completado de la 4b |
| Diálogo de completado (4b) | `surface.overlay` `@92 %` + vidrio (cuando haya desenfoque), `r.card`, padding 24, título 20/700, cuerpo 15 `text.secondary`, botones a la derecha (texto + relleno); check `state.success` 32, caja de resumen `#FFFFFF @4 %` `r.m` con Tiempo/Distancia/Calorías en `num.m` |
| Diálogo de la app (It. 3: eliminar, paso, repeticiones) | `surface.overlay` opaco (sin vidrio), `r.card`, borde 1 `border.default`, sin sombra; ancho `min(máx; ventana − 80)` con mínimo 280 y máximo 400 (eliminar, repeticiones) o 560 (paso); alto máximo ventana − 48; relleno 24 (16 con la ventana de menos de 480 dp de alto); título `title`; contenido desplazable con líneas `border.subtle` arriba y abajo y 8 dp de aire mientras se desplaza; acciones abajo a la derecha (texto + relleno, o peligro relleno); velo `overlay.scrim` al 60 %. Escala 0,96 → 1 al entrar y 0,98 al salir (ANIMATION_BIBLE §2.2) |
| Menú emergente (It. 3: `⋮` de la tarjeta y menú de texto) | `surface.overlay`, `r.m` 8, borde 1 `border.default`, relleno 8 / 0, ancho 140-280. Ítems de `min-height` 48 con relleno 0 / 16, `bodyM` `text.primary` en una línea con "…"; en el menú de texto (`.rm-menu--dense`, solo escritorio), 36 y 0 / 12. Ítem de peligro en `state.dangerText` con capa de estado **oscura** (`overlay.scrim` al 12 % con *hover* y al 24 % presionado: con la capa blanca daba 4,174:1). Anillo de foco adentro, sin halo. Se abre debajo del ancla con 4 dp de aire y el borde derecho alineado (arriba si no entra), a ≥ 8 dp de los bordes; el de texto, en el puntero + (2; 2). Desde la It. 4a esquiva la llave del simulador como a un borde (04a/00-deuda-diseno, R6) |
| Ayuda emergente (*tooltip* de los controles; It. 3) | `surface.overlay`, borde 1 `border.default`, `r.s` 6, relleno 4 / 8, `min-height` 24, `bodyXs` `text.primary`, ancho máximo 240 (ajusta); centrada 8 dp debajo del ancla (arriba si no entra) y a ≥ 8 dp de los bordes; nunca recibe toques. No es el *tooltip* del gráfico (§3.9) |

### 3.6 Botones de la app

Etiqueta de todo botón, chip y acción de texto: **`button`** 14/600 (It. 2; M3 `labelLarge` del legacy). Ícono dentro del botón 18, separación 8. Alturas como **`min-height`** y `flex-shrink: 0` en botones, íconos y filas (en UI Toolkit `flex-shrink` vale 1 por defecto y un contenedor alto fijo los comprime). La etiqueta va en **una línea con "…"** (`nowrap`): a 1,3× un texto largo se corta con "…" en lugar de pasar a dos líneas, porque en filas de ancho automático UI Toolkit mide la etiqueta en dos líneas dentro de una caja de una (medido en la It. 2, 04-design-notes §15.6).

| Tipo | Fondo | Texto e ícono | Borde | `min-height` · relleno horizontal | Uso |
|---|---|---|---|---|---|
| Primario | `accent.primary`; presionado `accent.primaryPressed` | `text.onAccent` (7,6 / 5,4:1) | — | 48 (56 en Workout) · 16 | "Buscar dispositivos", "Entrenamiento libre", "Iniciar", "Guardar" |
| Tonal | `accent.tonalBg` (`#00B4D8 @12 %`; sobre la tarjeta `#173E57`) | **`accent.onTonal`** `#38CBEF` (5,9:1; 5,2 con *hover*) | — | 44 · 16 | "Buscar" y "Conectar" del pulsómetro, acciones secundarias. Con `@16 %` y `accent.primary` daba 4,25:1 |
| Contorno | transparente | `text.primary` | `stroke.hairline` `border.strong` | 44 · 16 (12 en el panel del simulador) | "Buscar otro", "Cancelar" en contexto denso, fallas del simulador |
| Texto | transparente | `accent.primary` (5,6 sobre card · 5,1 sobre `surface.overlay`) | — | 40 · 12 | acciones terciarias: "Olvidar", "Hora real" |
| Texto peligro | transparente | `state.dangerText` (6,5 sobre `bg.app` · 5,2 sobre card) | — | 40 · 12 | "Desconectar" (remo y pulsómetro) |
| Peligro relleno | `state.dangerFill` | `text.primary` (5,0:1) | — | 44 · 16 | confirmar "Borrar" |
| Ícono | transparente; capa de estado circular de 40 | ícono 24 `text.primary` (cerrar: `text.secondary`) | — | caja 48 × 48 | cerrar, ± del panel del simulador |
| Strava | **asset oficial** "Connect with Strava" (slot `brand.strava`) | — | — | 48 | conexión; botones de subida en `brand.strava` con texto blanco 15/700 (3,4:1: excepción de marca documentada) |
| FAB | `accent.primary`, círculo de 56; presionado `accent.primaryPressed` y escala 0,94 | ícono `add` 24 `text.onAccent` (7,641:1) | — | 56 · — | "+" en Rutinas, a 16 de la derecha y 16 sobre la barra (It. 3); anillo 32 / halo 36 |

**Estados** (todo control accionable lleva una capa de estado absoluta, `inset 0`, con su mismo radio, que no recibe eventos; no hay colores nuevos: es blanco con opacidad):

| Estado | Spec |
|---|---|
| *Hover* (solo escritorio, nunca en el destino elegido) | capa `text.primary` `opacity 0,04`. Con 6 % (la versión anterior), el texto peligro sobre la tarjeta bajaba a 4,34:1 |
| Presionado | **`:hover:active`** (puntero o dedo apoyado sobre el control): capa 0,08 y escala 0,97 (botones de ícono, pestañas y llave del simulador: 0,94). El primario cambia el fondo a `accent.primaryPressed`, sin capa. Con teclado (Enter, Espacio) el control responde y lo marca el anillo de foco, sin "presionado". Se usa `:hover:active` y no `:active` solo porque el clic simulado de UI Toolkit deja `:active` 100 ms de reloj real, que en capturas y videos es "para siempre" (04-design-notes de la It. 2 §15.3). En Android falta verificar en equipo que el dedo apoyado marque `:hover` |
| Deshabilitado | `opacity 0,38` en todo el control, sin *hover* ni presión, sin transición |
| Foco (solo navegando con teclado) | anillo `stroke.focus` 2 `focus.ring`, por fuera del control (`inset −4` medido desde su borde de afuera: con un borde de 2 dp, como el mosaico de tipo, queda a −6 desde adentro; radio del control + 4), con halo `stroke.focusHalo` 4 `focus.ringHalo` (radio + 4 más). Aparece sin animación. Va también en pestañas, filas, chips, la llave y el interruptor. Excepción (It. 3): dentro de un contenedor que recorta (el ítem del menú, la opción de tiempo / distancia del segmentado) el anillo va **adentro**, a 2 dp, sin halo; sobre la opción elegida (`brand.seed`) en blanco (4,868:1). Los campos de texto no llevan anillo: su borde de 2 dp en `accent.primary` es el indicador |

Área táctil mínima 48 × 48 aunque el dibujo sea menor. Excepciones de contraste (no se prueban como umbral): presionado (dura lo que el toque: `accent.primary` sobre el panel 3,98) y `state.dangerText` sobre el panel con *hover* (4,17; no hay botones de peligro en el panel).

**Chip** (`ActionChip` del legacy): `min-height` 32, relleno 0 / 12, `r.m` 8, borde 1 `border.default`, etiqueta `button` `text.primary`, capa de estado `r.m`; separación 8 en una fila que ajusta. Sin estado "elegido" (paridad).

**Interruptor** (`SwitchListTile`): renglón `min-height` 48 con el rótulo `bodyM` que crece; pista 40 × 24 `r.full` a la derecha, centrada en el renglón; perilla 20 × 20. Apagado: pista `bg.sunken` + borde 1 `border.strong`, perilla `text.secondary` a escala 0,8. Encendido: pista `accent.primary` (5,07:1 sobre el panel), perilla `text.onAccent` a escala 1, desplazada 15 dp.

**Spinner**: arco de 270° (falta 90°), 16 × 16, trazo 2 con extremos redondeados; `accent.primary` en tarjetas y `text.primary` dentro del botón primario. Movimiento en ANIMATION_BIBLE §2.12.

### 3.7 Listas y tarjetas de lista

- **Rutina** (Rutinas desde la It. 3; Workout desde la 4a): `surface.card` `r.card`, relleno 16 / 4 / 16 / 16, **sin** borde superior; ícono `fitness_center` 22 `accent.primary` en una caja 36 `accent.tonalBg` (`#173E57` sobre la tarjeta, 4,572:1) `r.m`; nombre `titleS` 16/600 `text.primary`; descripción, si hay, `bodyXs` 12 **`text.secondary`**; resumen `bodyS` 13 **`text.tertiary`** (el orden y los colores del legacy; la mini-barra ya cuenta la estructura); en Rutinas, **`⋮`** 24 `text.secondary` con `margin-top` −14, en la columna de 28 dp del borde derecho (la misma de la cruz y el tacho del editor); en Workout (It. 4a, DV-21), en esa misma columna, el ***chevron*** `chevron_right` 18 `text.tertiary` (5,140:1; 4,785 con el *hover*) en una caja de 48 **centrada en el alto de la tarjeta**, como el `ListTile` del legacy, y sin toques: la tarjeta entera es la acción, y la columna de texto mide igual que en Rutinas. Nombre y descripción ajustan y se muestran tal cual (sin marcas ni escapes). *Hover* 4 %, presionado 8 %, sin escala; lo editado se ilumina (capa blanca 0,08 → 0). [EXR] Debajo del resumen, la **mini-barra de estructura**: un renglón de 4 dp con tramos de radio 2 y separación 2 en el color base de su tipo, alineados a la izquierda, de ancho `min(24; (W − 2·(n − 1)) / n)` con mínimo 2 (el largo de la franja también cuenta cuántos tramos tiene); si no entran (`n` > `⌊(W + 2) / 4⌋`), los que caben a 2 dp y los últimos 6 que se apagan; con 0 tramos, una pista `border.default` de todo el ancho. Deshabilitada sin remo: `@50 %` [CONTRATO].
- **Sesión** (Historial): padding 16; ícono remo 18 `accent.primary`; nombre 15/600; ícono Strava 14 `brand.strava`; fecha `dd/MM/yyyy  HH:mm` 12 `text.tertiary`; fila de 6 stats: valor `num.stat` 16/700 en color de métrica + etiqueta 11/500 `text.tertiary`. Split vacío `--:--`.
- **Dispositivo encontrado** (búsqueda; medido en la It. 2 con los pulsómetros): fila accionable `min-height` 56, relleno vertical 8, capa de estado `r.m` que se extiende 8 dp a cada lado del texto; ícono 24 (pulsómetro: `favorite` **contorno** `metric.hr`; remo: `rowing` `accent.primary`) + 16 + columna con el nombre `body` 15 `text.primary` con "…" y el identificador (`SIM:HRM`, MAC) `caption` 11 `text.tertiary`; a la derecha `chevron_right` 18 **`text.tertiary`** (5,1:1: es la señal de "tocar para conectar"; antes `@38 %`) o spinner 16.
- Separación vertical entre tarjetas 10; lista con margen 16.

### 3.8 Editor de rutinas

- **Paso:** `surface.card` `r.l` 12, `min-height` 56, relleno 4 / 4 / 4 / 12, **borde izquierdo 3 dp del color base del paso** [CONTRATO]; insignia 32 × 32 `r.s` con el color base `@20 %` y el ícono 16 en `step.*.hud`; título `titleXs` 14/600 en `step.*.hud` (4,685:1 el trabajo y 4,730:1 el enfriamiento sobre la tarjeta; DV-12 ampliada, §2.2); subtítulo `bodyXs` 12 `text.secondary`, que ajusta, con `trending_up` en `metric.distance` a 1,17 em dentro del renglón; acciones en cajas de 48 con ícono 20: lápiz `text.secondary` (neutro, como el legacy) y cruz `state.dangerText`; sin *hover* en la fila (no es accionable: lo son el lápiz y la cruz); handle de arrastre reservado (BKL-12).
- **Serie:** un bloque cerrado con un **riel continuo** de 3 dp `brand.seed` del encabezado al pie [CONTRATO del borde izquierdo]. Encabezado de 48 `brand.seed @12 %` con radios superiores 12: `repeat` 16 + `Serie` `headerS` 13/700 `accent.primary` + cápsula `×N` (alto 28, radio 14, `accent.primary @20 %`, número `num.stat` 16/700 en `accent.onTonal`, dentro de una caja táctil de 48) + tacho 20 `state.dangerText`. Cuerpo `@6 %` con los pasos a 16 dp del riel. Pie de 48 `@6 %` con radios inferiores 12 y `+ Agregar paso a serie` (`buttonS` 12/600; sin "la"). Debajo del pie, una o dos **hojas** apiladas (`@20 %` y `@12 %`; ×2 una, ×3 o más dos) que dejan intuir las repeticiones. 8 dp de aire arriba y abajo: 16 entre dos series contiguas.
- **Campos:** caja `bg.sunken` `r.m` 8 con borde 1 `border.input` (3,807:1) → 2 `accent.primary` con foco o 2 `state.danger` con error; alto mínimo 52 (descripción 72, dos líneas; crecen con la letra); etiqueta **adentro** de la caja, `body` 15 `text.secondary` en reposo y 12 (escala 0,8) al flotar, con foco en `accent.primary` y con error en `state.dangerText`; ayuda (`Ej: 2`) en `text.tertiary`, visible con el campo vacío; error `bodyXs` `state.dangerText` debajo; selección `text.selection` y cursor `accent.primary`; etiqueta, texto, ayuda, cursor y selección en la misma columna. Sin anillo de foco: el borde de 2 es el indicador. *Hover* en escritorio: borde `text.tertiary`.
- **Selector de tipo de paso:** **mosaicos** (ícono 20 `.hud` arriba, nombre `buttonS` abajo), 2 × 2 en el teléfono y 4 en fila desde 400 dp de contenido; `min-height` 56, `r.m`, borde 2 (`border.default`; el elegido, en el color base) y el elegido con el fondo del tipo `@20 %` y el nombre en `text.primary`. Así los 4 nombres se leen enteros a 1,3×. Tiempo / distancia: segmentado de 40 `r.full` con la opción elegida en `brand.seed` [CONTRATO de §3.9].
- **Progresiones:** el emoji 📈 del legacy se reemplaza por el ícono `trending_up` 14 en `metric.distance` (Q-10 lo permite).
- Medidas por tamaño de ventana y contrastes: [03-rutinas/04-design-notes §3, §4.5-§4.11 y §8](../iterations/03-rutinas/04-design-notes.md).

### 3.9 Gráficos (detalle de sesión)

Dibujados con `Painter2D` de UI Toolkit (vector), alto 220 (tablet 280).

| Elemento | Spec |
|---|---|
| Fondo | `bg.sunken` `r.l`, padding 12 |
| Serie | línea 2 px color base de la métrica, suavizado tipo Catmull-Rom 0,2 [CONTRATO]; área degradé vertical color `@24 % → @0 %` (legacy plano `@12 %`) |
| Franjas de paso | `step.*` `@10 %` a todo el alto [CONTRATO]; borde entre pasos `@24 %` 1 px punteado [4,4] |
| Objetivo | blanco `@78 %` 2 px punteado [8,5] + área `@7 %` [CONTRATO]; split invertido (más rápido arriba) también para el objetivo (Q-24) |
| Grilla | horizontal `#FFFFFF @8 %` 1 px (hairline) |
| Ejes | 11/500 `text.tertiary`, tabulares, intervalos X del legacy (30/60/120/300 s) |
| Tooltip | vidrio `surface.overlay`, `r.m`, valor `num.stat` 16 color de métrica + unidad 12; línea vertical guía `@40 %` 1 px; punto 8 con anillo `bg.sunken` 2 px |
| Selector de métrica | segmentado: seleccionado `brand.seed` [CONTRATO] con texto blanco 13/600; no seleccionado `bg.app` texto `text.secondary` |

### 3.10 Estados vacío, error y cargando

| Estado | Spec |
|---|---|
| Vacío de Rutinas (It. 3, paridad; base para las listas que siguen) | `fitness_center` 56 `text.primary` `opacity 0,12` (apenas visible, a propósito: AC-RUT-10-02) → 12 → título `body` `text.secondary` → 8 → ayuda `bodyS` 13 `text.tertiary`; ancho máximo 320, centrado en los dos ejes entre las dos barras |
| Vacío de Dispositivo (It. 2, paridad) | `bluetooth_disabled` **48** `text.primary` `opacity 0,24` (D-11) → 12 → `deviceNoResults` en `body` **`text.secondary`** (8,97:1; el legacy lo pone al 54 %: sube un escalón porque con D-ART-03 la ayuda también queda al 54 % y no deben verse iguales, DV-05) → 8 → ayuda `bodyXs` 12 `text.tertiary`, dos líneas; todo centrado en los dos ejes de su mitad |
| Pantalla provisoria (pestañas de iteraciones futuras) | barra superior con el título y, centrado entre las dos barras, el glifo sin elegir de su pestaña, 56 `text.primary` `opacity 0,24`. Nada más |
| Workout en reposo (It. 4a) | aviso sin remo (`state.warningBg`, `r.l`, relleno 12, ícono `warning` 18 alineado con la **primera** línea, texto 13 `state.warning`, 6,310:1; crece y colapsa con los verbos de la It. 3); `Entrenamiento libre` primario de 56 a todo el ancho (0,38 sin remo); `␣␣O elegí una rutina:` `bodyS` `text.tertiary`; cargando, error y vacío **centrados en lo que queda debajo del botón** (el mismo centro para los tres); el vacío, `workoutNoRoutines` `body` `text.tertiary`, ancho máximo 320, sin ícono (paridad) |
| "En curso" de Workout (It. 4a, paridad) | fuera de reposo la pestaña es la `_ActiveView` del legacy: **sin la barra superior**, solo `workoutInProgress` `body` 15 `text.tertiary` (5,829:1), centrado en toda la pestaña (del borde de arriba a la barra de pestañas). Se ve solo mientras entra el inmersivo; con la barra se leían dos títulos encimados durante el fundido |
| Error en línea | caja `state.danger @15 %`, `r.l`, padding 12, ícono `error` 18 + texto 13 `state.dangerText`, hasta 3 líneas, detalle técnico 11 `text.tertiary` (Q-42). Excepción: la tarjeta del pulsómetro muestra el error como texto suelto `bodyXs` sin caja ni ícono, como el legacy (§3.1) |
| Error de pantalla (Bluetooth apagado, sin permiso) | ícono 64 `state.danger`, título 20/700, ayuda 14 `text.secondary`, botón primario |
| Error de carga de Rutinas (It. 3) | `error` 48 `state.dangerText` → 12 → texto `body` `text.secondary` (ancho máximo 280) → 16 → `Reintentar` tonal con `refresh`; centrado entre las barras, con la forma del vacío |
| Cargando lista | **indicador circular de 32 dp, trazo 3, `accent.primary`**, centrado entre las barras; aparece solo si la carga sigue a los 120 ms, con fundido de 160, y no hay nada más en el cuerpo. Manda la spec (AC-RUT-10-02 pide el indicador del legacy): las tarjetas *skeleton* salieron de esta biblia en la It. 3 (DT-20). Nunca un indicador eterno (Q-13) |
| Cargando acción | spinner de §3.6 (16, trazo 2) en el lugar del ícono del botón; el texto se mantiene, salvo que el legacy lo cambie (`Buscar dispositivos` → `deviceSearching`, deshabilitado al 38 %). En el primario va en `text.primary`; en una tarjeta, `accent.primary` junto al texto de estado (`Buscando…`, `Conectando a …`) |
| Aviso (toast) | `surface.overlay` con borde 1 `border.default`, `r.l`, relleno 12 / 16, `min-height` 48, `bodyM` 14 `text.primary` que ajusta hasta 3 líneas, **sin ícono** (los avisos se explican solos); ancho de 16 a 16 con la ventana de menos de 600 dp y si no `clamp(texto + 32; 344; 560)` centrado; abajo, a 16 de la barra visible (o del borde), y sube sobre cualquier obstáculo inferior que **se cruce** con él (el `+`, la llave) con 8 dp de aire; 4 s, y el nuevo reemplaza al visible (It. 3) |

### 3.11 Navegación y extras

- **Barra de pestañas:** `surface.nav` (translúcida `@92 %`, **sin desenfoque**, con borde superior 1 px `border.subtle`; QA-P-22), alto 64 + safe area, íconos 24, etiqueta 12/600. El contenido de la pestaña corre por debajo de la barra, con padding inferior = alto de la barra + safe area: así la translucidez se nota al hacer scroll y el último elemento queda accesible. Seleccionado: ícono y texto `accent.primary` (con la variante rellena del ícono cuando existe, ASSET_SLOTS §4.3) + `nav.indicator` (píldora 56×32 `r.full`) que se desliza; no seleccionado `text.secondary`. La píldora va **detrás del ícono** y la etiqueta **debajo** de la píldora, nunca adentro: `accent.primary` sobre la píldora da 4,4:1 en el peor caso, que alcanza para el ícono (gráfico, ≥ 3:1) pero no para texto (fuera de la píldora, 6,0:1). Glifos por destino: ASSET_SLOTS §4.3 (la tabla de la It. 2 está en [00-deuda-diseno §3.5](../iterations/02-dispositivo-simulador/00-deuda-diseno.md), con dos cambios de color de [04-design-notes §1](../iterations/02-dispositivo-simulador/04-design-notes.md): la llave del simulador en `accent.primary` sobre `surface.overlay` y los *chevron* en `text.tertiary`; el ícono de la insignia mide 12 y el del vacío 48, D-11).
- **Barra de pestañas, medidas de la It. 2:** alto 64 + área segura (el legacy, 80: DV-07). Primer hijo, un **brillo superior de vidrio**: la textura de degradé del tablero (§3.1) teñida `text.primary` a `opacity 0,05`, de 5 % en el borde superior a 0 en 32 dp (no llega a la etiqueta, que empieza en y ≥ 42). Píldora 56 × 32 a 6 dp del borde superior, centrada en el ícono del destino. Cada destino es una columna `flex 1 1 0` (área táctil ≥ 82 × 64): caja del ícono 56 × 32 con su capa de estado `r.full` (*hover* 0,04; presionado 0,08 y escala 0,94 de la caja), ícono 24, y 4 dp más abajo la etiqueta `label` (12/600, tracking 0,08 em), siempre visible. Ritmo: 6 + 32 + 4 + 14,9 = 56,9, quedan 7,1 dp abajo (2,7 con letra 1,3×). Sobre `bg.app` la barra se ve `#0A1623` y la píldora `#083343`.
- **AppBar:** alto 56, fondo `bg.app`, sin línea ni sombra; relleno 16 a la izquierda y 4 a la derecha (un botón de texto con relleno 12 deja su texto a 16 del borde, como el legacy); título `title` 20/600 `text.primary` en una línea con "…"; acciones 24.
- **Barras de desplazamiento** (toda `ScrollView` de la app, superpuestas al contenido): sin flechas, carril transparente, *dragger* de 4 dp `r.full` `border.strong` con margen derecho 2. **En escritorio**, visibilidad automática (aparece si el contenido no entra). **En teléfono** (sin puntero), ocultas, como el legacy en Android, que no las dibuja: el desplazamiento se ve por el contenido que corre. Es la posición del Designer sobre QA-V2-13 (lo implementa el Architect; [03-rutinas/00-deuda-diseno §3](../iterations/03-rutinas/00-deuda-diseno.md)).
- **Padding inferior de cada pantalla**: el alto de la barra va **dentro** del contenido desplazable (no por fuera de la `ScrollView`): así el último elemento sube por encima de la barra y la translucidez se ve al desplazar.
- **Selector de escenario** (Workout): tarjetas 152×96 (legacy 120×80; [EXR] más grandes) `r.l`, borde elegido `accent.primary` 2,5 [CONTRATO] / no elegido `border.default` 1; nombre 13 (700 blanco elegido / 400 `@70 %`); miniatura = render horneado de la escena real (§7.6).
- **Cinta SIMULADOR:** caja de 80 × 12 rotada −45° alrededor de su centro, con el centro en (24; 24) dp de la esquina superior izquierda de la capa del simulador (la geometría del `Banner topStart` de Flutter), sin insets propios: sigue la regla única de área segura de la app (en Windows, 0). Fondo `dev.simulator`; texto `SIMULADOR` Inter **700, 10 dp, sin tracking**, `dev.simulatorText` (`#1A0A00`, 6,1:1; texto oscuro a propósito, DV-01), centrado: mide 60,2 dp y entra entero en la franja visible. No escala con la letra del sistema, no recibe eventos, siempre encima y sin animación. Antes decía 11/800 con 0,1 em: mediría 76 dp y la ventana cortaría la S y la R, y el repositorio no tiene Inter 800.
- **Llave y panel del simulador** (solo builds con simulador): llave 40 × 40 `r.l`, fondo `surface.overlay`, borde 1 `border.default`, ícono `build` 24 `accent.primary` (5,07:1); discreta a propósito (la misma superficie que el panel que abre). Panel de 280 de ancho, `surface.overlay`, `r.card`, borde 1 `border.default` (sin sombra: el borde lo separa del tablero), cabecera de 48 con línea inferior `border.subtle`, renglones ± de 48 con el valor en `num.stat` sobre 64 dp, chips e interruptor de §3.6. Detalle en [04-design-notes de la It. 2 §4.9](../iterations/02-dispositivo-simulador/04-design-notes.md).
  - **Dónde va [DECIDIR D-ART-10; desde la It. 4a]** ([04a/00-deuda-diseno §1](../iterations/04a-motor-hud-funcional/00-deuda-diseno.md)). Regla única: **nunca tapa una acción en reposo** (el área táctil de un control, agrandada 8 dp; de una tarjeta que se toca entera, solo su `⋮` y su *chevron*).
    - **Fuera del entrenamiento, carril izquierdo**: `left 16` (+ margen seguro). Ahí las filas ponen lo que las identifica (el ícono de la tarjeta, la insignia del paso), no lo que se toca.
    - **En el entrenamiento (D-ART-10 ajustada en la 4a, DN4a-14; el PO la integró como DV-39)**: el lugar lo calcula la geometría del HUD, por ventana, así nunca tapa una pieza ni se mueve con los valores. **En vertical**, carril derecho (`right 16` + margen seguro), con su borde de abajo 8 dp sobre el lugar que tiene la columna kcal y BPM **con BPM** (no se mueve cuando el pulso aparece o se va, y deja libre el hueco entre la banda y los controles para los avisos). **En horizontal**, 8 dp a la derecha de `Pausa`, centrada en su alto, si a su derecha entra la fila de kcal y BPM con sus mínimos (TH, W1280); si no (640 × 360), 8 dp sobre `Pausa`, alineada con su borde derecho.
    - **Altura** fuera del entrenamiento: 16 dp sobre lo que hay abajo. Barra + 16 en las pestañas; 16 + margen seguro en una pantalla apilada que tapa la barra.
    - El `+` de Rutinas deja de ser obstáculo de la llave: cada uno en su rincón.
    - El panel se abre donde está la llave y crece desde su esquina (en el entrenamiento, la de abajo a la derecha: hacia arriba y a la izquierda). Su alto máximo se mide desde su borde de abajo (en las pestañas, la fórmula de AC-BLE-16-03); en el entrenamiento vertical termina 8 dp debajo del lugar de Watts **con su objetivo** (316 dp en un teléfono de 411 × 914), así Split y Watts quedan a la vista mientras se los corrige; en horizontal quedan a su derecha. Si la llave cambia de lugar con el panel abierto, el panel la sigue. Mientras está abierto, la llave no se ve.
    - Movimiento: ANIMATION_BIBLE §2.11 y §2.14.

---

## 4. Tipografía

### 4.1 Familias (texto y números: SIL Open Font License 1.1; íconos: Apache 2.0)

| Rol | Familia | Pesos | Link | Por qué |
|---|---|---|---|---|
| Texto de interfaz | **Inter** | 400, 500, 600, 700 | [Google Fonts](https://fonts.google.com/specimen/Inter) · [GitHub](https://github.com/rsms/inter) | Muy legible en pantallas chicas, completo para es/en, trae la función `tnum` |
| Números e indicadores | **Barlow Semi Condensed** | 600, 700, 800 | [Google Fonts](https://fonts.google.com/specimen/Barlow+Semi+Condensed) · [GitHub](https://github.com/jpt/barlow) | Estética de tablero deportivo; al ser semicondensada entra un número más grande en el mismo ancho |
| Monoespaciada (diagnóstico BLE) | **JetBrains Mono** | 400 | [Google Fonts](https://fonts.google.com/specimen/JetBrains+Mono) | Bytes crudos alineados |
| Íconos (Apache 2.0, no OFL) | **Material Symbols Rounded**, recortada como "RowMate Icons" | 500, sin relleno y con relleno | [GitHub, versión fijada](https://github.com/google/material-design-icons/tree/10ecc14790612ca8fd164e3f4ffe9c16fc50d5b7/variablefont) | Glifos SDF que se tiñen con el color del token, sin paquete ni módulo de SVG (QA-P-09; decisión cerrada al iniciar la It. 2). Pasos, lista e íconos provisorios en ASSET_SLOTS §4 |
| Emojis del usuario (respaldo, It. 3) | **Noto Emoji**, monocroma | 500 (Medium, el peso de los íconos) | [Google Fonts](https://fonts.google.com/noto/specimen/Noto+Emoji) | P-09: un emoji en un nombre se dibuja en el color del texto y del alto de la línea, sin correr lo que sigue. Último de la lista de respaldos (Inter → RowMate Icons → Noto Emoji, armada por `RuntimePanelFactory`); TextCore no dibuja emojis a color. Sin la fuente, el glifo que falta sale `□` (ASSET_SLOTS `ui.font.emoji`) |
| Cifras tabulares (derivada, It. 4a, DD4a-07) | **RowMate Numeric SC**: Barlow Semi Condensed con `tnum` congelado (otro nombre, como pide la OFL) | 600, 700, 800 | receta en [04a/00-deuda-diseno §3.6](../iterations/04a-motor-hud-funcional/00-deuda-diseno.md) | Las cifras tabulares que dibujó la propia fuente (`zero.tf` … `nine.tf`, un solo avance: 0,507 / 0,521 / 0,535 em) en lugar de las proporcionales centradas en celdas (§4.2) |

### 4.2 Cifras tabulares (obligatorio en todo número)

UI Toolkit no garantiza activar `tnum` desde USS. Se resuelve de dos formas, sin depender de la versión:

1. **HUD y tablero:** componente `TabularNumber`, **un solo `VisualElement` por número** (no un `Label` por dígito, para no pasar el tope de 60 elementos del HUD; diseño en ARCHITECTURE §7.8, QA-P-08), que dibuja cada carácter en una **celda de ancho fijo** = ancho máximo de los dígitos 0–9 de esa fuente y tamaño; `:` y `.` usan celdas angostas fijas (0,32 em). Permite además el "rodillo" por dígito (ANIMATION_BIBLE §2.4). Entre celdas **no hay kerning**, por construcción. Barlow **sí trae `tnum`** (verificado en la It. 4a): desde DD4a-07, `.rm-type-num-*` usa la derivada **RowMate Numeric SC** (§4.1).
   - Con ella las diez cifras miden lo mismo y el `1` es el tabular que dibujó la fuente.
   - Con la cifra proporcional centrada en la celda del `4`, al `1` le sobraban 0,12 em por lado: 7,7 dp a 64 dp.
   - En `11` hay 6,6 dp menos de aire a 64 dp (2,4′ a 1,5 m), y los números quedan entre un 3,7 y un 5,5 % más angostos.
   - Barlow no define pares de kerning para sus cifras tabulares, ni con `:`, `.`, `-` y `m`.
   - Números y receta: [04a/00-deuda-diseno §3.3 y §3.6](../iterations/04a-motor-hud-funcional/00-deuda-diseno.md).
   - La derivada está en el repo desde la It. 4a: `UI/Fonts/RowMateNumericSC/` (600, 700 y 800, con su `OFL.txt`), generada por `tools/fonts/freeze_tnum.py` (fontTools 4.68.0 fijado, sin red, mismos bytes en cada corrida). Sus `FontAsset` y el rol numérico de `FontAssetPaths` los integra el *bootstrapper* (02-tech-design de la 4a §6.14).
2. **Listas, tablas y ejes** (Historial, It. 9): si hacen falta cifras tabulares en Inter, un derivado con `tnum` congelado y otra familia ("RowMate Numeric": la OFL exige **renombrar** las versiones modificadas), con la misma receta de fontTools. Pasos en ASSET_SLOTS §3, punto 7.

### 4.3 Escala

| Token | Familia | Tamaño / alto de línea | Peso | Uso |
|---|---|---|---|---|
| `display` | Inter | 32 / 40 | 700 | título del resumen final |
| `headline` | Inter | 24 / 32 | 700 | títulos de vistas grandes |
| `title` | Inter | 20 / 28 | 600 | AppBar, diálogos |
| `titleS` | Inter | 16 / 22 | 600 | secciones, nombres en tarjetas |
| `body` | Inter | 15 / 22 | 400 | texto general |
| `bodyM` | Inter | 14 / 20 | 400 | nombres y estados dentro de tarjetas, unidad `bpm`, rótulos del panel del simulador (It. 2; el legacy usa 14) |
| `bodyS` | Inter | 13 / 18 | 400 | subtítulos, resúmenes |
| `bodyXs` | Inter | 12 / 16 | 400 | ayudas, errores en línea, texto de la insignia `Conectado` (It. 2) |
| `label` | Inter | 12 / 16 | 600, mayúsculas, tracking 0,08 em | etiquetas de métrica, etiquetas de la barra de pestañas |
| `button` | Inter | 14 / 20 | 600 | etiqueta de botones, chips y acciones de texto (It. 2; M3 `labelLarge` 14) |
| `titleXs` | Inter | 14 / 20 | 600 | título de la fila de paso, `Pasos (N)` (It. 3) |
| `headerS` | Inter | 13 / 18 | 700 | `Serie` del encabezado del bloque (It. 3; el legacy usa 13/700) |
| `buttonS` | Inter | 12 / 16 | 600 | `Agregar paso a serie` y nombres de los mosaicos de tipo (It. 3) |
| `caption` | Inter | 11 / 14 | 500 | fechas, identificadores, ejes (mínimo legible) |
| `num.hero` | RowMate Numeric SC | 64 / 64 | 800 | SPM del HUD |
| `num.xl` | RowMate Numeric SC | 52 / 52 | 700 | tablero de Dispositivo, pulso |
| `num.l` | RowMate Numeric SC | 44 / 44 | 700 | Split y Watts del HUD |
| `num.m` | RowMate Numeric SC | 32 / 34 | 700 | kcal y BPM del HUD, resumen final |
| `num.band` | RowMate Numeric SC | 34 / 34 | 700 | banda tiempo │ distancia (D-ART-02) |
| `num.lap` | RowMate Numeric SC | 34 / 36 | 700 | vuelta en curso |
| `num.row` | RowMate Numeric SC | 24 / 28 | 700 | filas de vueltas y reps |
| `num.s` | RowMate Numeric SC | 20 / 22 | 700 | cuenta regresiva, reloj |
| `num.stat` | RowMate Numeric SC | 16 / 20 | 700 | stats de historial, chips numéricos |

Reglas: mayúsculas solo en etiquetas cortas y chips de fase; nunca texto de párrafo bajo 13 (las ayudas de una o dos líneas usan `bodyXs` 12); los valores numéricos van con la unidad separada y más chica, como el legacy; en el HUD, en la variante `.hud` de su métrica al 80 % (It. 4a, DV-31: el `@55 %` de antes no llegaba a 4,5:1). RowMate Numeric SC es Barlow Semi Condensed con las cifras tabulares congeladas (§4.2): mismas formas y métricas, salvo las cifras.

**Cómo se aplica en UI Toolkit 6000.3** (medido en la It. 2, 04-design-notes §2.2):

- El tamaño, el peso y la fuente salen **solo** de las clases `.rm-type-*` que genera `tools/tokens` (`Typography.uss`); ningún otro USS pone `font-size`. La letra del sistema escala con `.rm-text-scale-*`, hasta 1,3× (P-03).
- **`letter-spacing: N px` agrega N/100 em por letra** (es la unidad de TextCore, no píxeles). El tracking de 0,08 em de `label` se escribe `letter-spacing: 8px` y vale igual a cualquier tamaño.
- USS no tiene `line-height`: el "alto de línea" de la tabla es el ritmo de diseño. Unity usa la caja de línea de la fuente (Inter 12 → 14,9 dp, Inter 14 → 17,1 dp; Barlow SC 52 → 62,9 dp) y el ritmo vertical se arma con márgenes sobre esas cajas.
- Centrado óptico: en Inter la banda de las mayúsculas queda centrada en la caja y alcanza con `align-items: center`; en Barlow SC queda 0,05 em por debajo del centro (2,6 dp a 52), así que `TabularNumber` centra la banda de las mayúsculas, no la caja.
- Nunca mayúsculas por USS: las etiquetas llegan del catálogo con su caja (`SPLIT 500m`, `PULSÓMETRO` con `ToUpperInvariant`), porque transformar todo rompería `500m`.

### 4.4 Kerning (It. 3 R3-06; regla completa desde la It. 4a)

Detalle, mediciones y receta en [04a/00-deuda-diseno §3](../iterations/04a-motor-hud-funcional/00-deuda-diseno.md).

1. **K1. Solo la tabla versionada.** El kerning es el de la tabla de cada `FontAsset`, y nada más: `getFontFeatures = false` en las 7 caras de texto. TextCore no suma pares mientras dibuja, así que un texto se ve igual en cualquier orden, en una corrida parcial y en el segundo dibujo.
2. **K2. Todos los pares del juego fijo.** La tabla de cada cara que dibuja **texto** (Inter 400, 500, 600 y 700; **RowMate Numeric SC 700**, por los valores `num.stat` del panel: desde la 4a usan la derivada y no Barlow 700, DN4a-22) tiene **todos** los pares GPOS `kern` distintos de 0 entre los caracteres del juego fijo, y solo esos.
   - **El juego**: el catálogo es/en, el ASCII imprimible, `áéíóúüñÁÉÍÓÚÜÑ¿¡` y `×±·°`. Son 116 caracteres (115 con glifo: el `📈` no está en Inter), y dan 1 445 (Inter 400), 1 695 (Inter 500, 600 y 700) y 1 272 (la derivada 700, que no tiene pares entre cifras) pares.
   - **Por qué**: la tabla que quedó de las It. 2 y 3 dependía de lo que se había dibujado. Inter Bold, la de `display` y `headline`, tenía 11 "primeros", y le faltaban `Te` (−0,078 em), `Tr`, `Wo`, `TA`, `VA` e `Yo`: hasta 2,5 dp a 32 dp.
   - **Las listas** las escribe `tools/fonts/kern_pairs.py` en `UI/Fonts/Kerning/<cara>.pairs.json` (por índice de glifo, en unidades de la fuente, con el SHA-256 del `.ttf`), y el *bootstrapper* las vuelca en `m_GlyphPairAdjustmentRecords` (DD4a-06, camino a). Verificado: los 2 882 pares que ya había escrito TextCore dan el mismo valor.
3. **K3. El juego sigue al catálogo.** Se recalcula cuando cambia el catálogo, y una prueba de dotnet compara la tabla de cada `.asset` con la lista que da la receta.
4. **K4. Las celdas no llevan kerning.** Las caras que dibuja solo `TabularNumber` (RowMate Numeric SC 600 y 800) van sin tabla.
5. **K5. Cifras tabulares de la fuente.** Las cifras del HUD, del tablero y de los chips son las tabulares de la fuente (§4.2). `:` y `.` van en celdas de 0,32 em.
6. **K6. Fuera del juego, sin kerning.** Un carácter fuera del juego (uno raro que escriba el usuario) se dibuja sin kerning.

El método de medición (fontTools sobre GPOS: formatos 1 y 2; dentro de cada lookup gana el primer subtable que aplica, y los lookups se suman) reproduce exactamente los 49 049 pares que escribió TextCore.

---

## 5. Layout responsive

### 5.1 Clases de tamaño

Se decide por el **lado corto** de la ventana (dp) y la orientación (horizontal si ancho > alto, como el legacy).

| Clase | Condición | Columnas / márgenes / gutter | Notas |
|---|---|---|---|
| Teléfono vertical | lado corto < 600, alto ≥ ancho | 4 / 16 / 12 | Referencia 390×844 |
| Teléfono horizontal | lado corto < 600, ancho > alto | 8 / 24 + safe area / 16 | Referencia 844×390; contenido no inmersivo con ancho máx. 720 centrado |
| Tablet | lado corto 600–899 | 8 (vertical) o 12 (horizontal) / 24 / 16 | Contenido máx. 840; tablero de 3–4 columnas |
| Desktop | lado corto ≥ 600 y puntero (Windows/macOS) o ancho ≥ 1200 | 12 / 32 / 24 | Ventana inicial 1280×720 (Windows) / 800×600 (macOS) (Q-88); hover y foco visibles; contenido máx. 960 (no se aplica todavía: en la It. 3 la lista y el editor de Rutinas van a todo el ancho, como el legacy y CAP-11) |

Navegación: **una sola columna** en todas las clases (paridad de navegación). El maestro-detalle en tablet/desktop queda como propuesta de backlog.

### 5.2 HUD inmersivo [CONTRATO de zonas]

Zonas y orden idénticos al legacy (audit §5.1): SPM arriba a la izquierda con paneles debajo; Split y Watts arriba a la derecha; banda tiempo/distancia abajo a la izquierda; kcal y BPM abajo a la derecha; Terminar/Pausa abajo al centro; barra de pasos arriba; el bote libre en el centro. Medidas legacy (`top + 90`, `bottom 140`, `bottom + 16`…) se aplican multiplicadas por `hudScale`.

**Zona protegida del bote:** rectángulo centrado en x de 44 % del ancho × 30 % del alto, con su centro en el punto donde se proyecta el remero con la cámara de persecución (§8.1: cámara a 2,2 m, remero a 9,5 m, focal = lado corto): `centro_y = horizonte + (lado corto / alto) × 2,2 / 9,5`, con el horizonte a 0,40 del alto en vertical y 0,42 en horizontal. Da **0,504** del alto en vertical y **0,652** en horizontal (It. 4a, DN4a-02; en `docs/image-03.png` del legacy, 703,7 de 1080 con el carro en ≈ 700). Con 0,5 la zona horizontal quedaba sobre el horizonte y no sobre el bote. Ningún elemento del HUD la invade; QA lo verifica en capturas (contorno de depuración de 2 dp en `dev.simulator`, activable; CAP-16).

**Horizontal (It. 4a, DN4a-01; DV-33):** la columna derecha no entra en ninguna ventana horizontal (necesita 535·s dp), así que **kcal y BPM bajan a la fila de los controles**, abajo a la derecha y en fila: kcal en el rincón (no se mueve cuando BPM aparece o se va) y BPM a su izquierda, 8·s entre las dos, con el ancho mínimo de 96·s si la fila entra con los dos, decidido por ventana. La banda baja también al nivel de los controles, como en el legacy, y es **compacta** (§3.2). Queda la fila `banda │ Terminar Pausa │ (llave) │ BPM kcal`, y arriba SPM a la izquierda y Split y Vatios a la derecha. Las zonas y el orden del legacy se conservan.

**Topes** (04a/04-design-notes §3.7): la fila de arriba se reparte en proporción si no entra (T1, solo con los extremos de FTMS); en horizontal, SPM no pasa del borde izquierdo de la zona del bote y la columna derecha no empieza antes de su borde derecho (T2); la banda termina 8·s antes de su vecina y de la zona del bote (T3); la fila de kcal y BPM empieza después de los controles y de la llave (T4); la columna vertical de kcal y BPM mide lo de la mayor (T5). Lo que no entra achica **solo sus cifras** (el rótulo, el relleno y el borde no), en el acto con la ventana y con resorte con un dato. En 640 × 360 la banda queda a 0,776 con valores normales.

**La llave del simulador** (solo builds con simulador; D-ART-10, §3.11): en vertical, en el carril derecho y 8 dp sobre el lugar de la columna kcal y BPM con BPM; en horizontal, 8 dp a la derecha de `Pausa` o, en 640 × 360, 8 dp encima. Lo calcula la misma geometría que el HUD, así que no tapa ninguna pieza en los cinco perfiles medidos (60 combinaciones de valores).

### 5.3 Escala del HUD

`hudScale = clamp(ladoCorto / 390; 0,9; 1,6)`, aplicado a tipografía, padding, radios y posiciones del HUD (no a la escena). Con D-ART-02 los tamaños base son 64/44/32. Ejemplos: teléfono 360 dp → 0,92; 390 → 1,0; tablet 800 → 1,6 (tope); desktop 1280×720 → 1,6 (lado corto 720).

### 5.4 Safe areas y orientación

Todo respeta `Screen.safeArea` (notch, isla, barra de gestos). La rotación conserva el estado de la escena y del HUD; el HUD reubica con la transición de ANIMATION_BIBLE §2.12.

---

## 6. Iluminación y paleta por hora del día

### 6.1 Paleta base [CONTRATO]

Los 6 keyframes del legacy (5 h amanecer, 7 h mañana, 12 h mediodía, 18 h atardecer, 20 h anochecer, 22–4 h noche) con sus 8 campos (`skyTop`, `skyHorizon`, `waterNear`, `waterFar`, `sunColor`, `sunGlow`, `fog`, `ambient`) se portan **tal cual** al Core (`SceneTimeOfDay`, nombre de ARCHITECTURE §2; tabla en audit §7.3) con la misma interpolación lineal y el mismo cierre 22→4→5 h. Todo lo de esta sección se **deriva** de esa paleta y de la hora: no hay una segunda paleta que pueda desincronizarse.

### 6.2 Dirección del sol y de la luna [DECIDIR D-ART-05]

El legacy da el astro en coordenadas de pantalla (`x` 0,15–0,85 del ancho; elevación `e ∈ [0; 1]`). En 3D se convierte a direcciones del mundo, independientes de la orientación:

| Magnitud | Fórmula | Mediodía | Comentario |
|---|---|---|---|
| Acimut (ambos soles) | `az = (xFrac − 0,5) · 70°` (positivo = derecha del sentido de avance) | 0° (adelante) | Rango ±24,5°: siempre dentro del cuadro en vertical (hFOV 53°) y horizontal (hFOV 73°) |
| Sol "de pantalla" (disco, halo, brillo en el agua) | `elevDisp = 1,5° + 15,5° · e − 4° · puesta` | 17° | Visible en horizontal (borde superior a 22,8°) y vertical (40,9°) |
| Sol "de luz" (luz direccional, sombras) | `elevLuz = 6° + 49° · e` | 55° | Sombras cortas al mediodía, largas al amanecer/atardecer |
| Luna (de noche, `h' = h + 12`) | mismas fórmulas con `e(h')` y `salidaLuna` | | Luz de luna: `elevLuz = 10° + 40° · e` |
| Puesta y salida graduales (Q-63) | `puesta = smoothstep(18,5 h; 19,55 h)`; `salidaLuna = smoothstep(19,57 h; 20,6 h)`; al amanecer, espejo: la luna baja entre 3,4 y 4,42 h y el sol sube entre 4,45 y 5,5 h | | El astro entra y sale detrás del paisaje (−2,5° bajo el horizonte); los umbrales de noche del legacy (`elev ≤ −0,4`, ambiente < 0,6) no cambian |
| Cambio sol↔luna de la luz | una sola luz direccional; su intensidad pasa por su mínimo (0,05) a las 19:34 y 4:26 y en ese instante cambia de dirección | | Invisible para el usuario |

Consecuencia buscada: la luz viene **de adelante** (contraluz con camino de brillo hacia la cámara, muy EXR). Las sombras caen hacia la cámara y del lado opuesto al astro, que es la regla legacy (sombra del lado contrario). El remero, que mira a la cámara, queda en semicontraluz: lo compensan la luz ambiente del cielo y el término de relleno de su material (§7.9).

### 6.3 Derivados por keyframe

Interpolación lineal entre keyframes, igual que la paleta. Valores de URP (luz direccional en unidades de intensidad de URP, no lux).

| Hora | Luz clave (color · intensidad) | Ambiente (gradiente cielo / ecuador / suelo) | Sombras | Niebla (inicio–fin, color) | Exposición | Balance de blancos (temp / tinte) | Saturación / contraste | Bloom (intensidad / umbral) |
|---|---|---|---|---|---|---|---|---|
| 5 h amanecer | `sunColor` `#FFB070` · 0,55 | `skyTop`·0,63 / `skyHorizon`·0,63 / `waterNear`·0,5 | 0,55, muy largas | 80–400 m, `fog` `#C98A7A` + bruma baja (lago) | −0,2 EV | +15 / +5 | 0 / +2 | 0,70 / 1,0 |
| 7 h mañana | `#FFE4A0` · 1,25 | ×0,85 | 0,75 | 80–400, `#DCE8F5` + bruma (lago, se disipa a las 9:30) | 0 | +8 / +2 | +2 / +4 | 0,45 / 1,1 |
| 12 h mediodía | `#FFF6D5` · 1,75 | ×0,90 | 0,85, cortas | 80–400, `#CFE9FF` | −0,15 | 0 / 0 | +4 / +8 | 0,35 / 1,2 |
| 18 h atardecer | `#FFB36B` · 0,85 | ×0,77 | 0,65, muy largas | 80–400, `#E0A28A` | +0,1 | +20 / +6 | +6 / +4 | 0,75 / 1,0 |
| 20 h anochecer (luna) | `#DDE6FF` · 0,16 × fase | ×0,45 | 0,35 (solo Alto) | 60–380, `#30335A` | +0,35 | −10 / −2 | −10 / 0 | 0,85 / 0,9 |
| 22–4 h noche (luna) | `#E6EEFF` · (0,04 + 0,10 · iluminación lunar) | ×0,32 | 0 (Medio/Bajo), 0,25 (Alto) | 50–350, `#0F1730` | +0,6 | −25 / −4 | −20 / −5 | 0,95 / 0,8 |

- **Ambiente:** modo *Gradient* de URP; multiplicador = `ambient` del legacy × 0,9 (ya incluido en "×N").
- **Tonemapping:** ACES en la escena. La UI se compone después del post (no se tonemapea): los colores de tokens se ven exactos.
- **Viñeta** 0,18 suavidad 0,4 en todas las horas; **grano** solo Alto, 0,08, de noche.
- **Luces nocturnas** (ventanas, faroles, faro, cabaña) se encienden con el umbral legacy `ambient < 0,6`, con fundido de 20 min de escena (ANIMATION_BIBLE §6).
- **Fase lunar real** por fecha (mes sinódico 29,530588853 d; luna nueva de referencia 2000-01-06 18:14 UTC): disco con terminador y textura (slot `sky.moon`). Corrige el hallazgo 7 del audit.

---

## 7. Escena: entornos, agua, bote y remero

### 7.1 Composición común (planos de profundidad)

| Plano | Distancia z (m) | Qué hay | Técnica |
|---|---|---|---|
| Agua cercana | 0–20 | estela, charcos, gotas, reflejo del bote | malla de agua densa + textura de interacción (§7.7) |
| Orilla cercana | 9–20 laterales, z 2–150 | terreno de orilla, muros, props del legacy, vegetación de borde | segmentos de 50 m deterministas (F-SCN-14), LOD0–1 |
| Medio | 150–400 | props con LOD2/impostor, arboledas, edificios en bloque | instancing, fade por dither en 360–400 m |
| Fondo | 400–1 500 | colinas, ciudad en bloques, línea de árboles | mallas de baja densidad, niebla fuerte |
| Horizonte lejano | 2 500–4 000 | cordilleras, skyline, acantilado lejano, tribunas lejanas | capa "Horizon" dibujada con el cielo, con perspectiva aérea propia `lerp(tono; fog; 0,35 + 0,4 · profundidad)` [CONTRATO de color]; **se acerca al 4 % de la velocidad del bote** (Q-65) sin desplazarse de costado |

Reglas: nada "flota" en el agua (hallazgo 1): todo prop de orilla apoya en terreno, muro o muelle. Los props aparecen con fundido entre 400 y 360 m y desaparecen con dither entre 2 y 0,5 m de la cámara (hallazgo 2). Las posiciones, frecuencias, topes y metros exactos de los hitos son los del legacy (Q-61, tabla 7.5 del audit).

Corte transversal genérico de la orilla (x desde el eje del bote): **agua 0–9 m** → **línea de costa en |x| = 9,0** (contrato `|x| ≥ 9`) → franja de transición (playa, muro o talud, 1–3 m) → orilla con props del legacy → ladera o fondo.

### 7.2 Lago alpino (por defecto) — inspiración Bled

| Aspecto | Spec |
|---|---|
| Composición | Lago calmo y espejado; dos cordones: lejano a 3,5 km con nieve (tono `#5C6E8A`, picos > 75 % con nieve `#F2F6FA`), cercano a 1,2 km boscoso (`#2F4A3A`); bosque de coníferas denso en ambas orillas |
| Orilla | Playa de canto rodado de 1,2 m (pendiente 15°) → talud de pasto y roca que sube de 0,3 a 2,5 m entre x = 10 y 16 → ladera con bosque hasta 25 m de alto a x = 60 |
| Props legacy | Pinos cada 6–12 m en x = ±10..16, escala 0,7–1,3, 3 paletas de verde (audit 7.6) → conífera 3D (malla procedural de capas + LOD + impostor); muelle cada 400 m en x = −9 (madera `#8D6E63`, pilotes `#5D4037`); cabaña cada 600 m en x = +13 (chalet de madera `#6D4C41`, techo `#4E342E`, ventana nocturna `#FFE082`) |
| Extra [EXR] | Juncos en la franja de transición (instanciados, 1 cada 0,8 m con huecos), rocas sueltas, bruma baja 5–9:30 h |
| Materiales | Corteza `#5D4037` smoothness 0,15; follaje con translucidez falsa (wrap 0,35); roca `#6B6A66` triplanar; pasto `#4F7A3A` → amarillento a la tarde por color grading |
| Cielo | Cobertura de nubes 0,35; cúmulos de buen tiempo |
| Agua | Ver §7.7, perfil "Espejo" |
| Noche | Ventanas de cabañas, reflejo largo de la luna, estrellas plenas |
| Presupuesto | ≤ 120 props legacy visibles + juncos instanciados (1 draw call) |

### 7.3 Río urbano — inspiración Henley / Charles River

| Aspecto | Spec |
|---|---|
| Composición | Río encauzado de 18 m entre muros de piedra; ciudad de 3–6 pisos detrás de un paseo; puentes de piedra; skyline lejano a 1,5 km (`#3A4660`) |
| Orilla | Muro de contención de piedra en x = ±9,0, 1,2 m sobre el agua, con remate de piedra clara; paseo adoquinado de 9 a 12,5; fachadas desde 13 |
| Props legacy | Edificios (70 %) y clubes de remo (30 %) cada 15–30 m en x = ±13..17, escala 0,6–1,0 → fachadas procedurales de ladrillo o piedra (tonos `#78909C`, `#8D6E63`, `#B0BEC5`) con grilla de ventanas por shader (encendidas 60 % de noche, fijas por edificio); clubes con portón, franja `#1565C0` y **rampa al agua** que interrumpe el muro; faroles cada 25 m en ±9,5 sobre el borde del muro; boyas amarillas en ±3 cada 50 m; puente de piedra cada 800 m (arco de 17 m de luz, tablero a 6–7,2 m, parapeto a 7,7 m) |
| Extra [EXR] | Bajo el puente: oscurecimiento 40 % por volumen (luz y reflejos); racks con botes de colores frente a los clubes (opcional) |
| Materiales | Piedra `#9E9E9E` con normal de sillares; ladrillo `#8D5A44`; vidrio de ventana: reflejo del cielo atenuado 0,3 |
| Agua | Perfil "Río": oleaje corto y corriente visible hacia la cámara |
| Noche | Faroles con halo `#FFE082 @35 %` radio 1,5 y **columnas de luz** en el agua (billboards emisivos sobre el agua con ondulación); ventanas del skyline `#FFE08A @85 %` |
| Presupuesto | ≤ 120 props visibles; fachadas en 1 material con atlas (1 draw call por tipo con instancing) |

### 7.4 Costa

| Aspecto | Spec |
|---|---|
| Composición | Mar abierto a la derecha hasta el horizonte (el horizonte es mar); costa a la izquierda con acantilados de capas, playas cada 250 m y faro cada 1 000 m; acantilado lejano en el 35 % izquierdo (`#4A3F3A`) |
| Orilla | Tramos de acantilado: base de rocas con espuma en x ≈ −12,5, pared de 8 m × escala (0,8–1,3) en x = −14, 7 m de fondo, pasto arriba (`#556B2F`); tramos de playa (k mod 5 = 2): arena desde x = −9,5 (pendiente 6°) hasta −20 con 3 sombrillas en x = −13..−10. Transición de 10 m entre tramos |
| Props legacy | Acantilados `#5D4A42`, cueva/roca oscura `#3E2F2A`; faro (torre `#ECEFF1`, 3 franjas `#D32F2F`, linterna `#37474F`) con **haz giratorio** de noche [EXR]; sombrillas `#F06292` / `#4FC3F7` / `#FFD54F` con toalla `#FFF3E0` |
| Extra [EXR] | Espuma que rompe en la base (banda animada a lo largo de la línea de costa); 5 gaviotas 3D (cantidad del legacy) que planean y aletean |
| Materiales | Roca sedimentaria triplanar con estratos horizontales; arena `#D8C3A0` con brillo húmedo cerca del agua |
| Agua | Perfil "Mar": tinte `#0E6B7A` al 30 % [CONTRATO], mar de fondo que mueve el bote |
| Presupuesto | Tope legacy 60 props; espuma de costa 1 malla |

### 7.5 Canal de regata — inspiración Bosbaan

| Aspecto | Spec |
|---|---|
| Composición | Canal recto, agua planchada, 3 carriles visibles con 4 líneas de boyas, taludes de pasto, tribunas, torre de llegada, banderas; al fondo, línea de álamos y tribunas lejanas |
| Orilla | Talud de pasto de 25° que sube 1,0 m desde x = ±9,8; césped plano; filas de álamos en x = ±30..40 cada 12 m (nuevo, plano medio) |
| Props legacy | Boyas cada 10 m en x = −9, −3, 3, 9 (Albano: esfera de 0,25 m sobre cuello), color por tramo de 250 m (rojo `#E53935`, amarillo `#FDD835`, blanco `#ECEFF1`); **tableros de distancia** cada 250 m en x = −10,5 (blanco `#FAFAFA`, número negro 87 % en Barlow SC 700, legible desde 60 m); tribuna cada 1 000 m en x = +12 (gradas `#8D9DAE`/`#6E7B8C`, techo `#37474F`) con **público** [EXR] (impostores instanciados, 3 variantes de ropa, movimiento sutil); torre de llegada cada 1 000 m en x = −12 (`#CFD8DC`, banda `#263238`, reloj real); banderas cada 50 m alternando ±10,5 (`#E53935`, `#FFB300`, `#1E88E5`, `#43A047`) con tela animada |
| Materiales | Boyas plástico brillante (smoothness 0,75); tableros mate |
| Agua | Perfil "Pista" |
| Presupuesto | Tope legacy 200 props: las boyas van en **una** llamada instanciada; hitos con prioridad (regla legacy) |

### 7.6 Miniaturas del selector

**Por defecto (paridad, QA-P-11, ROADMAP §6):**

- Render de la escena real de cada entorno a **las 12:00**, como el legacy (F-SCN-22). Lo **hornea en el Editor** `EnvironmentThumbnailBaker` (ARCHITECTURE §14): no se renderiza en ejecución ni se cargan escenas aditivas para el selector.
- 304×192 px (×2 de la tarjeta), cámara de persecución a 20 m de distancia de escena, sin bote, con calidad Alta (es una textura: no cuesta en ejecución).
- 4 texturas en `Assets/RowMate/Art/Textures/Thumbnails/`, incluidas en el build: ≈ 0,1 MB en ASTC 6×6 (≈ 0,9 MB sin comprimir).

**Agregado opcional** (se ofrece en el gate de la It. 8):

- Hornear también 6, 18 y 22 h (16 texturas: ≈ 0,4 MB en ASTC 6×6, ≈ 3,7 MB sin comprimir) y mostrar la de la hora más cercana a la hora de escena (real o forzada por el simulador).
- Fundido cruzado de 240 ms cuando cambia la miniatura elegida.
- Sigue sin cargar escenas.

### 7.7 Agua — spec para el shader URP propio

**Look objetivo:** el agua es un espejo vivo. Cerca se ve oleaje fino y la textura de la superficie; lejos es casi un espejo del cielo y la orilla (Fresnel). El sol deja un camino de brillo que titila; la estela es una V de espuma tenue con el ángulo de Kelvin (19,5°, el legacy usa pendiente 0,35 = 19,3°); los charcos de pala son dos remolinos oscuros con borde brillante que quedan atrás.

**Perfiles por entorno** (traducen `uWaveAmp`/`uWaveScale` del legacy):

| Perfil | Entorno (legacy amp/escala) | Olas de vértice (Gerstner) | Detalle de normal (2 mapas) | Rugosidad | Particularidades |
|---|---|---|---|---|---|
| Espejo | Lago (0,25 / 2,5) | 2: λ 6 m A 0,015 m, λ 3,2 m A 0,008 m | tile 3,5 m y 1,3 m, fuerza 0,35 | 0,05 | aguas someras turquesa a < 2 m de la orilla |
| Río | Río (0,35 / 2,0) | 3: λ 2,0 / 1,3 / 0,8 m, A 0,02 / 0,012 / 0,006 | fuerza 0,55, deriva −z 0,25 m/s (corriente) | 0,10 | algo turbia (absorción verdosa) |
| Mar | Costa (0,8 / 4,0) | 2 de fondo λ 18 m A 0,18 m, λ 11 m A 0,09 m + 1 corta λ 4 m A 0,05 | fuerza 0,8 | 0,14 | espuma en crestas; tinte `#0E6B7A` 30 % [CONTRATO]; el bote sube y baja con el mar de fondo |
| Pista | Regata (0,3 / 1,5) | 1: λ 1,5 m A 0,006 m | fuerza 0,25 | 0,05 | casi espejo |

**Capas del shader (en orden):**

1. **Color base** = `lerp(waterNear; waterFar; smoothstep(2; 120; z))` de la paleta [CONTRATO], más absorción por "profundidad falsa" (más oscuro y saturado mirando hacia abajo cerca del bote).
2. **Normales:** suma de las Gerstner analíticas + 2 mapas de detalle desplazándose en direcciones distintas + perturbación de la textura de interacción (charcos, estela, gotas).
3. **Reflejo:** Fresnel de Schlick (F0 = 0,02) entre el color base y el reflejo. Reflejo = sonda de reflexión del cielo (cubemap actualizado por hora) + **orilla**:
   - Alto: reflexión planar a 1/4 de resolución con máscara de capas (orilla, horizonte, bote, remero; sin sombras ni post).
   - Medio y Bajo: "reflejo analítico de orilla": banda oscura del tono medio de la orilla del entorno donde el rayo reflejado cruzaría la orilla (calculado con `x` y la altura media de la orilla), con ondulación por la normal. Barato y convincente.
4. **Especular del sol:** GGX con la rugosidad del perfil, dirección = sol "de pantalla" (§6.2), intensidad × fuerza de luz del legacy (`uLight`: 0,2–1,0 de día, 0,45 de noche). **Destellos (glitter):** `pow(saturate(R·L); 900)` sobre la normal de alta frecuencia, con máscara de titileo; el camino de brillo aparece solo, por física, bajo el astro.
5. **Estela:** desde la popa hacia atrás, dos brazos con el ángulo de Kelvin y turbulencia central; intensidad `clamp(velocidad / 4; 0; 1)` [CONTRATO]; espuma máx. 35 % [CONTRATO]; persiste 12 s y se abre (en la textura de interacción).
6. **Charcos, gotas, salpicaduras:** desde la textura de interacción (ver abajo).
7. **Espuma:** textura de espuma procedural (slot `water.foam`) modulada por la máscara de espuma.
8. **Niebla:** `lerp(col; fog; smoothstep(80; 400; z))` [CONTRATO] y mezcla final con `skyHorizon` en la línea del horizonte para que no haya costura.

**Textura de interacción** (Medio y Alto): render texture RGBA8 de 256×200 que cubre x ±8 m y z de −2 a +18 m respecto de la cámara, **sin estado** (se redibuja cada cuadro con quads instanciados en posiciones de mundo): R,G = perturbación de normal; B = espuma; A = "calma" (aplana el oleaje dentro del charco, como en la realidad). 1–2 draw calls.

**Precisión en sesiones largas (Q-68):** la fase de cada ola (`k·origen`) y el desplazamiento de los mapas de normal se calculan en `double` en la CPU y se pasan como uniforms; el shader trabaja en coordenadas locales a un origen flotante. Sin saltos a 4 096 m ni pérdida de calidad después de 10 km.

**Malla:** anillos LOD centrados en la cámara y anclados a la grilla (sin "nadar"): interior 40×40 m con paso 0,5 m, anillos que duplican el paso, borde plano a 1,5 km. ≤ 20 k triángulos en total. En Bajo, paso 1 m interior.

**Niveles:**

| Nivel | Qué tiene | Costo objetivo (fragmento) |
|---|---|---|
| Alto | todo + planar 1/4 + 2 Gerstner extra | ≤ 110 ALU, 6 texturas |
| Medio (objetivo de paridad en gama media) | Gerstner del perfil, 2 normales, cubemap, interacción, reflejo analítico de orilla, glitter | ≤ 70 ALU, 5 texturas |
| Bajo | 1 normal, cubemap, charcos como decals simples, sin interacción | ≤ 35 ALU, 2 texturas |
| Agua degradada (R-04, obligatoria) | port directo del fallback legacy: degradé de 3 tramos + 10 líneas de brillo que avanzan con la distancia; *SubShader*/`Fallback` automático si el equipo no soporta el shader | ≤ 12 ALU, 0 texturas |

El paso de degradada a avanzada durante la escena se hace con un fundido de 0,6 s (mejora de F-SCN-13).

### 7.8 Bote: single scull [DECIDIR D-ART-04]

| Pieza | Spec |
|---|---|
| Casco | largo 8,20 m [CONTRATO], manga 0,29 m en la línea de flotación y 0,33 m en la borda a la altura del cockpit, francobordo 0,10 m; proa afilada con bola de proa blanca (obligatoria en remo), popa con aleta; cubierta de fibra con brillo de *gelcoat* (smoothness 0,85, clearcoat falso) |
| Colores | casco `#F5F5F5` → `#B0BEC5` en la obra viva [CONTRATO, degradé del legacy], **línea de borda `#00B4D8`** [CONTRATO], cockpit `#263238` [CONTRATO]; número de proa opcional |
| Riggers | ala de carbono (*wing rigger*) negra satinada con dos tirantes; pernos a ±0,80 m (envergadura 1,60 m), altura del eje del remo 0,31 m sobre el agua; chumaceras (*swivels*) blancas |
| Carro y rieles | asiento ergonómico negro a 0,14 m sobre el agua, recorrido 0,62 m (legacy 0,6), rieles de aluminio |
| Pedalera | zapatillas blancas fijas a 1,15 m del perno [CONTRATO de la distancia a los pies], ángulo 42° |
| Remos (par) | largo 2,88 m, interior 0,88 m; caña de carbono negra con textura de tejido sutil; puño de goma gris; manguito y collarín blancos; **pala tipo "hacha" 0,47×0,22 m cara `#00B4D8`** [CONTRATO del color] con punta blanca; flexión visual de la caña en el drive (ANIMATION_BIBLE §4.6) |
| Sombra | sombra real (mapa de sombras) en Medio/Alto; en Bajo, óvalo suave del legacy (opacidad 0,08 × ambiente, desplazado al lado opuesto del astro) [CONTRATO] |
| Reflejo | el bote, los remos y el remero entran en la reflexión planar (Alto) o en un reflejo falso invertido de baja resolución (Medio) |
| Presupuesto | casco ≤ 6 k tris, riggers + carro ≤ 2 k, cada remo ≤ 1,5 k; 2 materiales |

### 7.9 Remero

| Aspecto | Spec |
|---|---|
| Personaje | Mixamo, rig Humanoid (slot `rower.character`; elección concreta y pasos en ASSET_SLOTS). Altura objetivo 1,80–1,88 m; proporciones reales (el legacy exagera tronco y cabeza solo por ser 2D) |
| Vestimenta | **mono de remo** (*unisuit*) ajustado: base `#1565C0` [CONTRATO del color del traje] con franjas laterales `#00B4D8` y panel lateral `#0D3B6E` (color de carro/pies del legacy); medias blancas; gorra opcional (slot). Si el personaje elegido trae ropa propia, el material "RowMate/Fabric" la tiñe con la misma paleta |
| Materiales | Piel: URP Lit + dispersión falsa (wrap lighting 0,4 y tinte `#E89A80` en el terminador), smoothness 0,45, brillo de sudor que sube con el tiempo activo (0 → +0,15 en 20 min, sutil); tela técnica: Shader Graph con brillo de borde (*sheen*) `#FFFFFF @12 %`, smoothness 0,35, normal de tejido 0,3; pelo: tarjetas con *alpha clip* + dither (nunca alpha blend en móvil) |
| Relleno de cámara | término de relleno en piel y tela `0,12 · sunColor · saturate(N·V)` para que el frente se lea en contraluz |
| Placeholder | maniquí procedural (`ProceduralRower`) con el **mismo esqueleto y nombres de huesos de Mixamo** (`mixamorig:*`), avatar Humanoid armado en ejecución con `AvatarBuilder`, segmentos en cápsulas con el mono pintado; usa exactamente el mismo controlador de pose y el mismo IK (ANIMATION_BIBLE §7.3) |
| Presupuesto | LOD0 ≤ 30 k tris, LOD1 ≤ 12 k (a > 25 m, solo cámaras extra), 3 materiales máx., texturas 1 024 (móvil) / 2 048 (desktop) |

### 7.10 Mundo vivo (todo guiado por tiempo de escena; detalle en ANIMATION_BIBLE §6)

Banderas (fórmula legacy), copas de árboles con viento, boyas que se mecen, gaviotas, nubes, estrellas que titilan (fórmula legacy), haz del faro, público con movimiento leve, ventanas que se encienden de a una al anochecer.

---

## 8. Cámaras

### 8.1 Persecución [CONTRATO]

Cámara nivelada (sin inclinación) con desplazamiento de lente, a **2,2 m** de altura, remero a **9,5 m**, horizonte al **40 %** (vertical) / **42 %** (horizontal) del alto, focal = lado corto. En Unity: cámara física; FOV vertical derivado `2·atan((alto/2)/lado corto)`; `lensShift.y` = −0,10 (vertical) / −0,08 (horizontal) (validar signo en el Editor con la prueba de proyección de `scene_camera_test`). Recorte cercano 0,5 m.

### 8.2 Vida de la cámara [EXR, sin romper el contrato]

"Respiración" y seguimiento suave especificados con números en ANIMATION_BIBLE §5. Restricciones de arte: el horizonte nunca se mueve más de ±3 px respecto de su línea contrato; el remero nunca sale de la zona protegida (§5.2); en "reducir movimiento" se apaga.

### 8.3 Cámaras alternativas estilo EXR [DECIDIR D-ART-07 · BKL-02, fuera de paridad]

| Cámara | Posición (respecto del remero) | Mira a | Lente | Movimiento | Uso |
|---|---|---|---|---|---|
| Lateral | x = +6,5 m (lado opuesto al astro para tener luz de frente), y = 1,1 m, z = −0,5 m | torso (y = 0,9) | FOV vertical 38°, sin shift | travelling suave ±1,5 m en z con período 20 s | ver la técnica completa de perfil |
| Dron | 22 m atrás, 14 m arriba, 8 m al costado | el bote + 0,3 m adelante | FOV 45° | órbita 0,8°/s acotada a ±25° | paisaje y estela |
| Popa (*stroke cam*) | en la cubierta de popa, y = 0,55 m, a 1,6 m del remero | cara y manos del remero | FOV 60° | fija al casco (cabecea con el bote) | intensidad, esfuerzo |

Transiciones y director automático en ANIMATION_BIBLE §5.4. Todas reusan los mismos materiales y LOD; la de dron exige LOD1 del entorno a mayor distancia (medir rendimiento antes de habilitarla en móvil).

---

## 9. Presupuesto de rendimiento

Objetivo (R-12): **60 fps sostenidos durante 30 min** en el dispositivo de referencia de gama media, con margen térmico. Cuadro de 16,6 ms: **GPU ≤ 12 ms**, hilo principal ≤ 8 ms, sin asignaciones de memoria por cuadro en caminos calientes.

**Niveles de calidad (Alto, Medio y Bajo):** la tabla única, con equipos de referencia, `renderScale`, antialiasing, sombras, reflejo, agua y vidrio por nivel, está en [ARCHITECTURE §20.1](../architecture/ARCHITECTURE.md) (QA-P-10). Esa tabla tomó los números de esta biblia; acá queda solo el **reparto** del arte dentro de esos topes. El nivel Bajo tiene como referencia Android 8.0+ (API 26, mínimo de Q-90; QA-P-18). El vidrio del nivel Bajo es el respaldo plano, token `glass.fallback` (`#070D16 @86 %`, §3.2, QA-P-17), el mismo valor que usan ARCHITECTURE §7.4, §20.1 y RA-07 (P-08, cerrada en la It. 1).

**Presupuesto por cuadro (nivel Medio):**

| Recurso | Tope | Reparto |
|---|---|---|
| Draw calls (batches) | ≤ 150 (SetPass ≤ 50 con SRP Batcher) | cielo 1 · horizonte 3 · agua 2 + interacción 2 · terreno de orilla 10 · props legacy (instanciados por tipo) ≤ 40 · vegetación instanciada ≤ 10 · bote + remos + remero ≤ 12 · VFX ≤ 6 · sombras ≤ 25 · UI ≤ 12 · post ≤ 8 |
| Triángulos visibles | ≤ 350 k (Alto 600 k, Bajo 180 k) | remero 30 k · bote 11 k · agua 20 k · orilla + props ≤ 220 k · cielo/horizonte ≤ 15 k |
| Texturas | máx. 1 024² en móvil (2 048² en desktop); ASTC 6×6 (Android/iOS), BC7 (desktop); memoria total ≤ 180 MB | remero 1 024 (albedo + normal + máscara) · bote 512 · normales de agua 2 × 512 · espuma 256 · capas de terreno 512–1 024 · atlas de fachadas 1 024 |
| Costo de shaders (fragmento) | agua ≤ 70 ALU / 5 tex · props ≤ 35 ALU / 3 tex · vegetación ≤ 30 ALU (sin alpha test salvo hojas cercanas) · cielo ≤ 35 ALU / 2 tex · vidrio HUD ≤ 20 ALU / 1 tex | sin lazos por píxel; ruido por textura, no por hash iterado |
| Post | ≤ 1,5 ms | bloom (mitad de resolución, sin "high quality filtering"), tonemapping, ajustes de color, viñeta; sin SSAO ni DOF en móvil |
| Desenfoque del HUD | ≤ 0,6 ms | `_RmHudBlur`: 1/4 de resolución, 3 pasadas Kawase (ARCHITECTURE §7.4), solo si hay elementos de vidrio en pantalla |
| CPU | escena ≤ 1,5 ms · Animation Rigging ≤ 0,3 ms · UI ≤ 1,0 ms (sin relayout por cuadro: los números animan con transformaciones) | |
| Sobre-dibujo | ≤ 2,5× en la zona de vegetación; partículas ≤ 400 vivas | |

**LOD:** props con 3 niveles + impostor (pino: 1 200 / 400 / 120 tris / billboard de 2 tris); transición con dither (*LOD cross-fade*); distancias de LOD multiplicadas por 0,7 en Bajo.

**Adaptación térmica:** el diseño está en [ARCHITECTURE §20.3](../architecture/ARCHITECTURE.md) (`ThermalAdapter`, ADR 018). Usa el tiempo de GPU de `FrameTimingManager` y, si el equipo no lo informa, el de CPU; la resolución se ajusta con `renderScale` y nunca con `ScalableBufferManager`, con cambios poco frecuentes y con histéresis. El orden en que se baja es el que pide el arte: resolución → reflejo → distancia de sombras → densidad de vegetación → nivel. Nunca se baja la tasa de cuadros antes que el detalle.

---

## 10. Accesibilidad y legibilidad

### 10.1 Contraste

| Elemento | Umbral | Verificación |
|---|---|---|
| Valores del HUD (texto grande) | ≥ 7:1 en blanco y métricas no rojas; ≥ 4,5:1 (AAA texto grande) en familia roja (D-ART-01). Con margen de render: también contra el peor vidrio renderizado + 1 por canal (`#2B3038`, §2.3) | test de contraste en Core contra el peor caso del vidrio, sin redondear y renderizado (§2.3, §11); el texto nunca sobre el reflejo interior ni sobre el glow, ni con opacidad de pausa (§3.2) |
| Texto de interfaz | ≥ 4,5:1 (AA), primario ≥ 7:1 | tokens de §2.1; pares de pantalla con la pila real de capas (brillo de la tarjeta a la altura de la etiqueta, *hover* al 4 %, brillo de la barra), `ContrastTests.ItTwoDesignerPairs`; desde la It. 3, `ItThreeDesignerPairs` (R1-R28: lista, editor, serie, campos, diálogos, menús y anillos, con las capas de la serie y del *hover*) |
| Componentes gráficos (bordes, píldoras hechas, íconos) | ≥ 3:1 | píldoras pendientes `@18 %` son contrato (estado "tenue" a propósito) |

### 10.2 Lectura a 1,5 m

Altura de mayúscula de Barlow SC ≈ 0,70 em; 1 dp ≈ 0,159 mm.

| Nivel | Fuente | Altura de mayúscula | Ángulo a 1,5 m | Relación con agudeza 20/20 (5′) |
|---|---|---|---|---|
| Primario (SPM) | 64 dp | 7,1 mm | 16′ | 3,3× (lectura de un vistazo) |
| Secundario (Split, Watts) | 44 dp | 4,9 mm | 11′ | 2,2× |
| Terciario (kcal, BPM, banda) | 32 (`num.m`) – 34 (`num.band`) dp | 3,6–3,8 mm | 8,5′ | 1,7× |
| Legacy SPM (52) | 52 dp | 5,8 mm | 13′ | 2,7× |

Con `hudScale` en un teléfono de 411 dp (s = 1,054; 04a/04-design-notes §6):

| Elemento (It. 4a) | dp en pantalla | Altura de mayúscula | A 1,5 m | Contra 5′ |
|---|---|---|---|---|
| SPM | 67,5 | 7,50 mm | 17,2′ | 3,4× |
| Split, Watts | 46,4 | 5,16 mm | 11,8′ | 2,4× |
| Banda | 35,8 | 3,98 mm | 9,1′ | 1,8× |
| kcal, BPM | 33,7 | 3,75 mm | 8,6′ | 1,7× |
| Flecha de SPM (tinta) | 20,8 de alto | 3,31 mm | 7,6′ | 1,5× |
| Flecha de Split y Watts | 14,3 | 2,27 mm | 5,2′ | 1,0× |
| Cuenta regresiva (por terminar) | 21,1 (23,2) | 2,34 (2,58) mm | 5,4′ (5,9′) | 1,1× (1,2×) |
| Banda en 640 × 360 a 0,776 | 24,3 | 2,71 mm | 6,2′ | 1,2× |
| Tipo de paso, rótulos, `obj.`, pastillas | 13,7 a 10,5 | 1,58 a 1,22 mm | 3,6′ a 2,8′ | de cerca |

De lejos se leen los valores, el rojo de fuera de objetivo (también más oscuro que el blanco: un daltónico lo ve por la claridad, además de por la flecha) y el color del paso; la flecha de Split y Watts queda justo en 5′, para leerla de cerca y para el daltonismo. La cuenta grande de los últimos 3 s es de la 4b (D-ANI-05).

Prueba de aceptación: teléfono de 6,1" a 1,5 m, tres personas leen SPM, Split y Watts en < 1 s cada uno, a pleno sol de pantalla (brillo máximo) y de noche.

### 10.3 Daltonismo y redundancia

- El estado de la píldora se codifica por **forma** (tenue / llenándose / sólida / hueca) [CONTRATO], no solo por color.
- El tipo de paso siempre lleva su **nombre** (barra) o **ícono** (editor).
- Fuera de objetivo: color + flecha ▲/▼ + borde (D-ART-01).
- Gráficos: la métrica elegida se nombra en el selector y en el tooltip; las líneas de objetivo son punteadas (forma) además de blancas.
- Verificación: capturas pasadas por simulación de deuteranopía y protanopía (QA).

### 10.4 Movimiento, texto y tacto

- **Reducir movimiento** (iOS *Reduce Motion*, Android escala de animación 0): sin respiración de cámara, entrada al inmersivo por fundido, rodillo de dígitos reemplazado por fundido de 80 ms, pulsos reemplazados por realce fijo. La remada y el agua **no** se apagan (son el dato).
- Escala de texto del sistema hasta 1,3× en pantallas no inmersivas; el HUD usa `hudScale`, no la escala del sistema.
- Áreas táctiles ≥ 48 dp (56 dp en el inmersivo). Háptica sutil en pausa/reanudar y cambio de paso (It. 14).
- Desktop: todo accionable por teclado, foco visible.

---

## 11. Entregables y verificación del arte

| Qué | Dónde (propuesta al Architect) | Verificación |
|---|---|---|
| Tokens como datos | `Core/Design/Tokens.cs` (colores, radios, tamaños, durations) + generador de `Tokens.uss` y `Typography.uss` (`tools/tokens`, QA-P-05; `--check` sale con 0 si están al día) | test dotnet: todo token semántico tiene el hex del legacy; contraste de §10.1 contra el peor caso calculado (luminancia WCAG, composición del respaldo `@86 %`, §3.2) y contra el peor vidrio renderizado + 1 (`HudKeepsItsThresholdOnTheRenderedWorstGlass`, It. 3). En la 6b, además: el compuesto del vidrio adaptativo nunca más claro que el vidrio sobre blanco |
| Íconos | `Core/Design/Icons.cs` (constantes de codepoints, ASSET_SLOTS §4.2) + fuente "RowMate Icons" en `Assets/RowMate/UI/Icons/` | test dotnet: cada nombre y su codepoint coinciden con el `.codepoints` versionado |
| Paleta por hora y derivados | `Core/Scene/SceneTimeOfDay.cs` (legacy) + `HourLighting.cs` (tabla §6.3) + `CelestialPlacement.cs` (§6.2) + `MoonPhase.cs` | tests: continuidad a medianoche y 5 h (legacy), acimut dentro de ±24,5°, elevación de pantalla ≤ 17°, puesta gradual sin saltos > 0,5° por minuto, fase lunar de fechas conocidas |
| Shaders | `Assets/RowMate/Art/Shaders/` (ARCHITECTURE §2): Water, Sky, Prop, Foliage, Fabric, Skin y Glass (este en `Art/Shaders/UI/`) | capturas de aceptación: 4 entornos × 6, 12, 18, 22 h (prueba manual del legacy) × 3 niveles |
| Medición de rendimiento | perfiles del dispositivo de referencia (Android de gama media del ROADMAP §0) | tabla de §9 en cada demo de las It. 6a a 8 (regla 9 del ROADMAP); sin teléfono, NO VERIFICABLE y se arrastra a la It. 14 |

Script de contraste usado para §2.3: algoritmo OKLCH de "subir claridad conservando el matiz" + contraste WCAG 2.x; debe portarse como test en Core para que cualquier cambio de token falle si rompe §10.1.

---

## Cambios por QA (plan)

Correcciones por el gate de armonía del plan ([QA_PLAN_REVIEW.md](../QA_PLAN_REVIEW.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-17 | §3.2: el respaldo sin desenfoque (It. 4a a 6a, nivel Bajo, equipos sin `_RmHudBlur`) es `#070D16` **`@86 %`** fijo en todas las piezas de vidrio, con el borde y el radio del legacy. Se corrigió el número: `@80 %` no garantiza D-ART-01 en el peor caso (cielo blanco: métricas 5,6:1, familia roja 3,6:1), y el "blanco 8,3:1" del texto anterior estaba mal calculado (es 10,9:1 con `@80 %` y 13,5:1 con `@86 %`). §3.5: Terminar y Pausa usan el mismo vidrio; Terminar pasa a `state.dangerText` (4,9:1), porque con vidrio `@45 %` daba 1,0:1 sobre un cielo blanco |
| QA-P-22 | §2.1 y §3.11: barra de pestañas translúcida `#0A1622 @92 %` + borde superior `border.subtle`, **sin desenfoque**. Contraste verificado con texto blanco debajo (acento 6,0:1, secundario 8,0:1). El contenido corre por debajo de la barra con su padding |
| QA-P-09 | §3 y §4.1: los íconos son glifos de la fuente "RowMate Icons" (Material Symbols Rounded recortada, Apache 2.0), sin `com.unity.vectorgraphics`. §11: `Core/Design/Icons.cs` con su test. Queda abierto con ARCHITECTURE §1.2, que hoy elige SVG con el módulo integrado de 6.3 (ASSET_SLOTS §4) |
| QA-P-29 (parte Designer) | §7.9: el maniquí arma su avatar con `AvatarBuilder` y usa el mismo controlador de pose que el personaje de Mixamo (detalle en ANIMATION_BIBLE §7.3) |
| QA-P-16 (alineación con ARCHITECTURE §7.4) | §3.2 y §9: un solo nombre y un solo número de pasadas, los de ARCHITECTURE §7.4: `_RmHudBlur` generado por `HudBlurFeature`, 1/4 de resolución, 3 pasadas Kawase, en un `RTHandle` persistente que llega vivo a la UI en *Screen Space Overlay*. Llega en la It. 6b |
| QA-P-11 (alineación con ROADMAP §6) | §3.11 y §7.6: miniaturas horneadas en el Editor a las 12:00 (paridad F-SCN-22). La opción de 6, 12, 18 y 22 h (la más cercana a la hora de escena, sin cargar escenas) se ofrece en el gate de la It. 8 |
| QA-P-18 | §9: el nivel Bajo tiene como referencia Android 8.0+ (API 26, mínimo de Q-90), igual que ARCHITECTURE §20.1 |
| QA-P-10 (parte Designer) | §9: la tabla de niveles se reemplaza por la referencia a la tabla única de ARCHITECTURE §20.1, y la adaptación térmica por la de ARCHITECTURE §20.3 (`renderScale`, tiempo de CPU como respaldo, histéresis). Acá queda solo el reparto del arte dentro de los topes |
| Alineación con ARCHITECTURE y ROADMAP | §0.2 remite a los gates del ROADMAP §6; §3 fija la versión de Unity de ARCHITECTURE §1.1; `SceneTimeOfDay` (nombre de ARCHITECTURE); shaders en `Assets/RowMate/Art/Shaders/`; mediciones en las It. 6a a 8 según la regla 9 del ROADMAP |
| QA-P-08 (alineación con ARCHITECTURE §7.8) | §4.2: `TabularNumber` es un solo elemento por número, así el HUD no pasa el tope de 60 elementos |

---

## Cambios al iniciar la It. 2

Deuda de diseño de la It. 1 (QA-V-09 y [04-design-notes §6](../iterations/01-fundaciones/04-design-notes.md)) y ajustes para las pantallas de la It. 2. Detalle, números y verificación en [iterations/02-dispositivo-simulador/00-deuda-diseno.md](../iterations/02-dispositivo-simulador/00-deuda-diseno.md), 2026-10-27. La fuente que manda es `Assets/RowMate/Core/Design/Tokens.cs`: ningún cambio de esta tabla toca un valor de `Tokens.cs`, solo alinea la biblia con lo que ya tiene.

| Dónde | Qué cambió |
|---|---|
| §9 (QA-V-09) | Sale la "diferencia abierta" del vidrio del nivel Bajo (`@80 %` contra `@86 %`): ya estaba resuelta (P-08, It. 1). Ahora nombra el token `glass.fallback` (`#070D16 @86 %`), igual que ARCHITECTURE §7.4, §20.1 y RA-07 |
| §2 | `Tokens.cs` es la fuente única (122 tokens) y manda si algo no coincide; un valor por token |
| §2.1 | `focus.ring` se separa en `focus.ring` (`#00B4D8`, `stroke.focus` 2) y `focus.ringHalo` (`#00B4D8 @30 %`, `stroke.focusHalo` 4) |
| §2.2 | `state.dangerText` pasa de `#FF6B6B` a **`#FF7272`** (mismo matiz): 5,2:1 sobre card, 4,6:1 sobre `surface.raised` (antes 4,4), 4,7:1 sobre `surface.overlay` (antes 4,5) y 5,1:1 en el peor vidrio. Se separan los fondos y los textos en tokens propios: `state.connectedBg` (`#4CAF50 @20 %`), `state.warningBg` (`#FF9800 @15 %`), `dev.simulatorText` (`#1A0A00`) y `dev.bleDebugBg` (`#000000 @38 %`). Íconos de paso: el descanso es `pause_circle` `E1A2` sin relleno (P-07) |
| §2.3 | El vidrio del peor caso se nombra como token (`glass.fallback`). `text.hudPrimary`: 13,5:1 en el peor caso (decía "10,1:1 (vidrio 78 %)", un número viejo que no seguía el método de P-05) |
| §2.4 | Nombres de las escalas: `space.dp2` … `space.dp48`, `stroke.hairline` / `target` / `focus` / `selected` / `accent` / `focusHalo` y `shift.s` / `m` / `l` / `xl` |
| §3.1 | Tablero de Dispositivo (04-design-notes §7): ícono y etiqueta en el color base `@100 %` (con `@85 %` SPM daba 4,48:1 y pulso 3,07:1) y el pulso en `metric.hr.hud` (4,6:1). Pulsómetro: el corazón del encabezado es `favorite` relleno siempre, como el legacy, y el título va en `metric.hr.hud` (el `@70 %` del legacy da 2,5:1) |
| §3.2, §4.3, §10.2 | `num.m` 32/34 (kcal, BPM, resumen) y **`num.band`** 34/34 (banda tiempo │ distancia), en lugar de "`num.m` 32–34" |
| §3.5 | Terminar: `state.dangerText` `#FF7272`, 5,1:1 en el peor vidrio (decía `#FF6B6B` y 4,9:1) |
| §3.11 | La etiqueta de la pestaña va debajo de la píldora `nav.indicator` (adentro, el acento da 4,4:1); la cinta SIMULADOR nombra `dev.simulatorText` |
| §3 y §4.1 (QA-P-09) | Íconos: queda **cerrado** el punto abierto con ARCHITECTURE §1.2. Son glifos de "RowMate Icons", sin paquete ni módulo de SVG; sin la fuente, íconos provisorios dibujados por código (ASSET_SLOTS §4.4) |

---

## Cambios al iniciar la It. 3

Deuda de diseño de la It. 2: alineación con lo que se decidió e implementó ([04-design-notes de la It. 2 §11 y §15.6](../iterations/02-dispositivo-simulador/04-design-notes.md)) y QA-V2-12 (margen del HUD). Detalle, números y verificación en [iterations/03-rutinas/00-deuda-diseno.md](../iterations/03-rutinas/00-deuda-diseno.md), 2026-10-27. Manda `Assets/RowMate/Core/Design/Tokens.cs`; lo único de esta tabla que cambió valores de `Tokens.cs` son las 8 variantes del HUD de §2.3 (ningún color de contrato).

| Dónde | Qué cambió |
|---|---|
| §2 | 127 tokens desde la It. 2; `Typography.uss` también sale de `tools/tokens` |
| §2.1 | `accent.tonalBg` (`#00B4D8 @12 %`) y `accent.onTonal` (`#38CBEF`), los tokens del botón tonal |
| §2.2 | `state.connected` sobre la insignia de Dispositivo (`bg.app`): 8,8:1 |
| §2.3 (QA-V2-12) | Criterio con margen de render: umbral contra `#2B3038` (peor vidrio renderizado + 1 por canal). Cambian `metric.split.hud` `#FFAA47`, `metric.spm.hud` `#3ACCF1`, `metric.distance.hud` `#4ECBF2`, `metric.hr.hud` y `step.work.hud` `#FF5D7F`, `state.offTarget.hud` `#FF625B`, `step.cooldown.hud` `#37A2CB` y `step.unknown.hud` `#7D9BA9` (ΔE OK ≤ 0,0062). Contrastes con tres decimales |
| §2.4 | `ConstantPixelSize` con la densidad (D-03), en lugar de *Constant Physical Size* |
| §3.1 | Acento `@60 %` en reposo; brillo de 24 dp con textura de degradé, 0,084 en reposo y 0,14 al llegar un dato (era `@18 %`); valor que se achica sin tope (D-10) con centrado óptico; marcadores; sin unidad aparte en el tablero; destello solo si cambió el texto; grilla > 600 → 3 columnas y 4 en tablet como propuesta (P-02); tarjeta del pulsómetro con `bpm` en `metric.hr.hud` (QA-G2-08, DV-04) y error sin caja; cabecera que ajusta |
| §3.2 (QA-V2-12) | Vidrio adaptativo con `smoothstep(0,10; 0,70)` (con 0,15 el peor caso no era el blanco); reflejo interior solo en los 8 dp superiores, nunca bajo texto; glow de objetivo solo por fuera del borde; en pausa, tarjeta y texto al 100 % |
| §3.6 | Etiqueta `button` 14/600 en una línea con "…" (DN-20 no adoptado); `min-height`; tonal con `accent.tonalBg` + `accent.onTonal`; variantes texto peligro, peligro relleno e ícono; *hover* 4 % (era 6 %), presionado `:hover:active` con capa 8 % y escala 0,97 / 0,94; foco con halo; chip, interruptor y spinner |
| §3.7 | Fila de dispositivo encontrado con las medidas de la It. 2 y *chevron* en `text.tertiary` |
| §3.10 | Vacío de Dispositivo (ícono 48, título `body` `text.secondary`, ayuda `bodyXs`; DV-05, D-11); pantalla provisoria; error del pulsómetro sin caja; spinner en botones |
| §3.11 | Barra de pestañas con brillo superior de vidrio (5 %, 32 dp) y medidas; AppBar 56; barras de desplazamiento (ocultas en teléfono: posición sobre QA-V2-13); padding inferior dentro del contenido; cinta SIMULADOR 10/700 sin tracking (era 11/800 con 0,1 em) con la geometría del `Banner`; llave y panel del simulador |
| §4.3 | `bodyM` 14/400, `bodyXs` 12/400 y `button` 14/600; reglas de UI Toolkit: `letter-spacing` en 1/100 em, cajas de línea, centrado óptico de Barlow, sin mayúsculas por USS |
| §10.1, §11 | Contraste del HUD con margen de render y el test que lo prueba; test pendiente para el vidrio adaptativo de la 6b |

---

## Cambios de la It. 3

Rutinas: lista, editor, diálogos, menús, campos y avisos. Fuente: [03-rutinas/04-design-notes](../iterations/03-rutinas/04-design-notes.md) §2-§4, §8, §12 y §16 (lo que cambió al implementarlo), 2026-10-28. Manda `Assets/RowMate/Core/Design/Tokens.cs`: ningún token existente cambió de valor.

| Dónde | Qué cambió |
|---|---|
| §2, §2.1, §4.3 | **133 tokens**: `overlay.scrim`, `border.input`, `text.selection`, `titleXs`, `headerS` y `buttonS`. Usos nuevos de `brand.seed` (riel, cuerpo y hojas de la serie), `accent.primary` (cápsula `×N` a `@20 %`) y `accent.onTonal` (número del `×N`) |
| §2.2, §2.3 | Regla de color de tipo también fuera del HUD: texto e íconos en `step.*.hud`, gráficos en el color base (resuelve DT-19: el enfriamiento en `#37A2CB`) |
| §3.5 | Diálogos de la app: `surface.overlay`, `r.card`, borde, anchos 400 / 560, relleno 24 (16 compacto), contenido desplazable con 8 dp de aire, velo `@60 %` |
| §3.6 | FAB con presionado `accent.primaryPressed` y escala 0,94; el anillo va adentro en los contenedores que recortan (ítem de menú, tiempo / distancia) |
| §3.7 | Tarjeta de rutina: caja del ícono en `accent.tonalBg` (no `@14 %`); `⋮` en Rutinas y *chevron* solo en Workout; descripción 12 `text.secondary` y resumen 13 `text.tertiary`; reglas de la mini-barra (ancho, desborde, pista vacía); lista a 16 de los bordes con 10 entre tarjetas |
| §3.8 | Fila de paso (`titleXs` e ícono en `.hud`, lápiz `text.secondary`, acciones 20 en cajas de 48, alto 56 con relleno 4); serie (riel continuo, encabezado de 48, `headerS`, cápsula 28 con `accent.onTonal`, cuerpo `@6 %`, pie de 48 con `Agregar paso a serie`, hojas); campos (`border.input`, etiqueta adentro 15 → 12, 52 / 72, foco y error de 2 dp, selección, una sola columna); selector de tipo en **mosaicos** en lugar de chips |
| §3.10 | Vacío de Rutinas (56 al 12 %, `text.secondary` / `text.tertiary`, centrado entre las barras) en lugar de "56 `@24 %`, 17/600"; error de carga (ícono 48 + texto + tonal); **cargando: indicador de 32 con gracia de 120** (salen los *skeleton*, DT-20); aviso sin ícono, con las reglas de ancho y de obstáculos |
| §3.11 | Llave en Rutinas: 16 dp sobre el `+`, solo en y (DV-13, AC-BLE-16-02); el panel se abre siempre en el mismo lugar |
| §5.1 | Sin ancho máximo de contenido en escritorio en esta iteración (paridad con el legacy y CAP-11) |
| §10.1 | Los pares de la It. 3 en `ContrastTests.ItThreeDesignerPairs` (75 casos) y sus valores a 3 decimales en `ItThreeControlValuesMatchTheDesignNotes` |

---

## Cambios al iniciar la It. 4a

Fuente: [04a-motor-hud-funcional/00-deuda-diseno](../iterations/04a-motor-hud-funcional/00-deuda-diseno.md), 2026-10-28. Son la deuda de diseño de la It. 3 (QA-V3-09 y el kerning de los títulos y del HUD), la base visual del HUD y lo que de [03-rutinas/04-design-notes](../iterations/03-rutinas/04-design-notes.md) no había llegado a esta biblia. Manda `Assets/RowMate/Core/Design/Tokens.cs`, y **ningún token cambia**. Lo marcado DD4a-nn lo implementa el Architect.

| Dónde | Qué cambió |
|---|---|
| §0.2 | **D-ART-10**: la llave del simulador abajo a la izquierda fuera del entrenamiento y abajo a la derecha en el entrenamiento. Reemplaza DV-13; la decide el PO en el gate de la 4a (QA-V3-09) |
| §3.5 | Filas nuevas del **menú emergente** (`⋮` y menú de texto: medidas, ítem de peligro con capa oscura, posición) y de la **ayuda emergente**, con los valores de 04-design-notes de la It. 3 §4.4. Faltaban desde la It. 3. El menú esquiva la llave |
| §3.6 | El anillo de foco se mide desde el borde de afuera: con un borde de 2 dp, como el mosaico, queda a −6 desde adentro (04-design-notes de la It. 3 §16.2) |
| §3.11 | La llave y el panel con la regla nueva: carril, altura, obstáculos, techo y el panel que crece desde la llave. Hasta que el PO la integre, vale DV-13 |
| §4.1 | Fila de **Noto Emoji**, el respaldo de los emojis del usuario (P-09 de la It. 3; faltaba), y de **RowMate Numeric SC**, la derivada tabular de Barlow (DD4a-07) |
| §4.2 | Barlow trae `tnum`: `.rm-type-num-*` pasa a la derivada y `TabularNumber` dibuja las cifras tabulares de la fuente, sin kerning entre celdas. Sale la rama "si Barlow no trae `tnum`" |
| §4.4 (nueva) | **Kerning**, de K1 a K6: tabla versionada y nada más (`getFontFeatures = false`), con **todos** los pares GPOS del juego fijo en cada cara que dibuja texto (DD4a-06). Inter Bold no tenía `Te`, `Tr`, `Wo`, `TA`, `VA` ni `Yo` (hasta 2,5 dp a 32) |
| §5.2 | El hueco de la llave en el HUD (72 × 56 abajo a la derecha). Aviso: con D-ART-02 y `hudScale`, la columna derecha no entra en ninguna ventana horizontal y la 4a lo resuelve |

---

## Cambios de la It. 4a

Entrenar: motor de sesión, HUD funcional y Workout en reposo (WP-D0 y WP-D). Fuente: [04a-motor-hud-funcional/04-design-notes](../iterations/04a-motor-hud-funcional/04-design-notes.md) (§13 y la sección de WP-D), con lo que el PO aceptó en el gate (DV-31, DV-33, DV-38, DV-39) y lo que se midió en las capturas y los videos del arnés `it4a`, 2026-10-28. Manda `Assets/RowMate/Core/Design/Tokens.cs`: **133 → 135** tokens.

| Dónde | Qué cambió |
|---|---|
| §2 y §2.1 | 135 tokens: `backdrop.sky` `#0B4FA8` y `backdrop.water` `#0E4F8F`, el fondo provisorio del inmersivo (DN4a-03) |
| §3.2 | El respaldo sin desenfoque sobre el fondo de dos tonos (un solo elemento con una textura de 1 × 100). Las unidades en `.hud @80 %` (DV-31; salen del 55 %). La anatomía de la tarjeta con objetivo: el hueco de 0,48 em toda la duración del paso, la flecha centrada en la banda de las mayúsculas y `obj.` alineado con las cifras. La banda compacta en horizontal (DV-33), con una sola escala para sus dos cifras |
| §3.3 | La barra de la 4a: una fila, 40 de alto, igual que la barra libre. Las pastillas con el texto en blanco y `trending_up` en `metric.distance`; `Próximo:` / `Up next:` localizado (DV-24), al 70 % sobre el 20 %, con el latido solo en el fondo y el borde (DV-38) |
| §3.5 | El diálogo de terminar es el de la app, opaco (sale "`surface.overlay @92 %` + vidrio", que pasa al de completado de la 4b). Estados de los controles del HUD: la capa oscura de `Terminar`, el anillo de foco blanco con halo oscuro, el orden de Tab, los controles al 0,38 al confirmar y el *spinner* del guardado lento |
| §3.7 | El *chevron* de Workout, centrado en la columna de 28 dp del `⋮` de Rutinas, sin toques |
| §3.10 | Workout en reposo (aviso, botón, etiqueta y los tres estados centrados debajo del botón) y la vista "en curso": solo el texto, centrado en toda la pestaña, sin la barra superior (paridad) |
| §3.11 y §5.2 | La llave en el inmersivo (D-ART-10 ajustada, DV-39): en vertical sobre el lugar de kcal y BPM con BPM; en horizontal junto a `Pausa`, o encima en 640 × 360. El panel que crece desde su esquina, con el tope de Watts en vertical y que sigue a la llave. La disposición horizontal (sale "pendiente de la 4a"), los topes T1 a T5 y el centro de la zona del bote con la fórmula de la cámara (0,504 / 0,652) |
| §4.2, §4.3 y §4.4 | RowMate Numeric SC en el repo (`tools/fonts/freeze_tnum.py`), `num.*` en la derivada; las unidades al 80 % en el HUD. K2 sobre la derivada 700 en lugar de Barlow 700, con las listas de `tools/fonts/kern_pairs.py` (1 445 a 1 695 pares por cara); K4 para la derivada 600 y 800 |
| §10.2 | La tabla de lectura a 1,5 m de la 4a, con la cuenta regresiva y las flechas |

**Verificado en el arnés (WP-D).** Las 49 tomas de `it4a` sin desbordes, con `HudOverlap` y `WrenchAudit` en verde y todas las sondas de aspecto dentro de su tolerancia (las de 04-design-notes §8.2: fondo, cielo y agua, vidrio, valores, rótulos, unidades, bordes con objetivo, pastilla, `obj.`, flechas, velo, botones), con dos correcciones de puntos de sonda que estaban mal elegidos en las notas (el cielo de W1280 caía sobre la barra; la tendencia, un glifo dentro del texto de la pastilla, no se puede medir como color dominante). Los videos, cuadro por cuadro, contra ANIMATION_BIBLE §2.4 a §2.8.

**Pendiente** (no es de esta biblia): la integración de RowMate Numeric SC y de las tablas K2 en el *bootstrapper* (WP-B0). Hasta entonces las capturas dibujan las cifras de Barlow en celdas del ancho del `4` y el texto con las tablas parciales de las It. 2 y 3.
