# It. 3 — Notas de diseño visual y de motion

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-27 |
| Estado | **Final (WP-D, 2026-10-28)**: implementado sobre el contrato de WP-B y verificado con el arnés `it3` (47 tomas, 0 con problemas, 78 sondas de aspecto dentro de tolerancia) y con dotnet (R1-R28); lo que cambió al implementarlo y lo que queda para WP-B, en §16. Antes: corregido por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md), APROBADO CON OBSERVACIONES): QA-G3-18 (la receta de los glifos con `uvx` y R3-06 decidido, §4.16) y alineación con la spec corregida y con el gate (§15) |
| Iteración | 3 · `03-rutinas` ([ROADMAP §It. 3](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (§4, §7, §10, §11.1, §14 P-09, §15), [02-tech-design](02-tech-design.md) (§4, §7.5 contrato USS, §8.1 DT-01/-02/-03/-19/-20, §8.3 pedidos al Designer), [00-deuda-diseno](00-deuda-diseno.md), [04-design-notes de la It. 2](../02-dispositivo-simulador/04-design-notes.md) (sistema construido: §2-§5, §15), [ART_BIBLE](../../design/ART_BIBLE.md) §2-§4 y §10, [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §0-§2, `Assets/RowMate/Core/Design/Tokens.cs` y `MotionSpecs.cs` (mandan), `Presentation/Styles/{Base,Skin,Motion}.uss`, capturas de la It. 2 (`02-dispositivo-simulador/snapshots/`, en especial CAP-10, CAP-16 y CAP-21b) y `docs/image-04.png` del legacy |
| Entrega | Este documento. Sin código: los tokens nuevos, `Skin.uss`, `Motion.uss` y los pares de contraste se escriben en **WP-D0** y **WP-D** (§11) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/routines/routines_screen.dart` (entero), `lib/features/routines/routine_editor_screen.dart:140-739`, `lib/shared/theme.dart:1-70`, `docs/image-04.png`; en el gate, `pubspec.yaml` (sin `google_fonts`) y `lib/**` buscando `fontFamily` y `fontFeatures` (§4.16). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificado | Anchos de texto con un programa propio de .NET en `<scratch>/it3/designer/measure/` (tablas `cmap` y `hmtx` de las fuentes del repo, **sin kerning**: los anchos reales son iguales o un poco menores); contrastes con un programa propio en `<scratch>/it3/designer/contrast/` (mismo método que `Contrast.cs`: mezcla sRGB sin redondear). En el gate: la receta de glifos de WP-D0 corrida con `uvx` (`<scratch>/it3/designer/glyphs/`), el kerning de los textos de la It. 3 contra las tablas versionadas (`<scratch>/it3/designer/kerning/`, fontTools sobre las fuentes del repo) y el IL de `FontAsset` de TextCore 6000.3.27f1 (lectura de metadatos, sin abrir Unity) (§14). Unity no se abrió: nada de este documento lo necesita |

Convenciones: las de las It. 1 y 2 (hex `#RRGGBB`, transparencia `@NN %`, WCAG 2.x sin redondear, 1 px de USS = 1 dp). Perfiles de la spec §7.2: **TV** 411 × 914, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720. "Código" = animación por `MotionDriver` con el reloj inyectable; "USS" = transición de `Motion.uss` (solo bajo `:hover`). Nombres `#…` y `.rm-…` del contrato de 02-tech-design §4 y §7.5; lo que agrego va marcado **[nuevo]** y se pide en §9 (**DN3-nn**, para no chocar con los DN-nn de la It. 2). "Caja de línea" = alto que Unity da a una línea (Inter: 1,238 em → 12 dp 14,9; 13 dp 16,1; 14 dp 17,1; 15 dp 18,6; 16 dp 19,8).

---

## 0. Resumen

1. **El editor se lee como una línea de tiempo** (spec §10.1): cada paso con su borde, su insignia y su título en el color del tipo; cada serie es un **bloque cerrado** con un riel azul continuo desde el encabezado hasta el pie, y debajo del pie asoman una o dos "hojas" que dejan intuir el `×N` (§4.7). El chip `×N` es una cápsula de 28 dp dentro de un área de 48 y su número rueda al cambiar (§5.6).
2. **Tres hallazgos de contraste que el legacy trae y la spec copia** (§8): el título `Trabajo` en `#EF476F` da **3,815:1** sobre la tarjeta (texto de 14 dp: pide 4,5), el `×N` en `#00B4D8` sobre su cápsula da **4,370:1**, y el ícono del enfriamiento en su insignia da **2,744:1**. Regla única: **el texto y los íconos del tipo usan `step.*.hud`; los gráficos (borde, fondo de la insignia, mini-barra) el color base** (§2.3). Con eso el trabajo da 4,685, el enfriamiento 4,730 (resuelve DT-19 con `#37A2CB`) y el chip, con `accent.onTonal`, 5,620. Es un ajuste de DV-12 para el PO (§10), que ya lo integró en la spec.
3. **Seis tokens nuevos en WP-D0** además de `overlay.scrim`: `border.input` (el borde del campo: con `border.strong` daba 2,13:1 contra su relleno), `text.selection`, y tres tamaños con el peso que pide la spec y que la escala no tiene (`titleXs` 14/600, `headerS` 13/700, `buttonS` 12/600). El contrato de §7.5 nombraba "body-m 600" y "body-s 700", que no existen como clase (§2.1).
4. **DV-13 decidido** (DT-03): se acepta la propuesta del Architect. En Rutinas la llave **sube** 72 dp y queda 16 dp sobre el `+`, alineada a la derecha (solo cambia y, con resorte ω 22); en el resto de las pestañas y con el editor abierto, donde dice AC-BLE-16-02. El aviso evita los obstáculos **que se cruzan con su rectángulo** (con 8 dp de aire), no los que solo caen en su ancho (§4.12, §4.13).
5. **Mini-barra**: tramos iguales de 2 a 24 dp alineados a la izquierda (el largo ya cuenta cuánto dura la rutina en pasos); si no entran, se dibujan los que caben a 2 dp y los últimos 6 se desvanecen (`×2 000 000 000`); con 0 tramos (`×0`), una pista vacía (§4.3).
6. **Diálogo de paso**: los 4 tipos como **mosaicos** (ícono arriba, nombre abajo), en 2 × 2 en el teléfono y 4 en fila desde 400 dp de contenido: es lo único que deja ver los 4 nombres con letra 1,3× en 283 dp (`Calentamiento` mide 98 dp a 14/600). La **vista previa queda fija bajo el título** cuando el diálogo tiene ≥ 560 dp de alto (TV, W1280) y como primer elemento del desplazamiento si no (L640, TH) (§4.11).
7. **Motion** (§5): lo nuevo **crece** (alto y márgenes con `spring.smooth` + fundido), lo borrado **se desvanece y colapsa** (fundido 0-120, alto 80-280), lo editado **se ilumina** (capa blanca 0,08 → 0 en 600 ms). Pila, diálogo, menú, aviso y llave con los tiempos de ANIMATION_BIBLE §2.2, afinados (la página entrante llega a opacidad 1 en 160 para no superponer dos pantallas). Todo por código; `Motion.uss` solo agrega *hover* y presionado.
8. **Dos riesgos que no son de aspecto y conviene arreglar en WP-B** (§9): las etiquetas con texto del usuario (nombre, descripción, `¿Eliminar "…"?`) tienen que llevar `enableRichText = false`, o un nombre como `<b>A</b>` se dibuja en negrita (AC-RUT-08-01 pide el texto al carácter); y en el legacy entre dos series contiguas hay **16 dp** (8 + 8), no los 8 que decía CAP-13 (§10; la spec ya lo corrigió).
9. **P-09 (emojis)**: respaldo monocromo **Noto Emoji** (OFL 1.1) al final de la lista de respaldos del panel; mientras no esté, el glifo que falta se dibuja como `□` y el resto del texto no se mueve (§4.15).
10. **Kerning (R3-06, decidido en el gate)**: es el de la tabla versionada y nada más. Con `getFontFeatures = false` en las 7 caras de texto, TextCore deja de sumar pares mientras dibuja, y un texto se ve igual en cualquier orden, en una corrida parcial y en el segundo dibujo. Medido: los cinco glifos de R3-06 no cambian ningún texto de la It. 3, y solo 12 textos pierden un par (como mucho 1,15 dp). En esta iteración no se genera tabla (§4.16, DN3-24).

---

## 1. Respuestas a 02-tech-design §8.3 y decisiones pedidas

| Pedido | Respuesta | Dónde |
|---|---|---|
| §8.3.1 · DV-13 y AC-BLE-16-02 (DT-03) | **De acuerdo con la llave que se aparta del `+`**: x fija (`right 16`), `bottom` = barra + 16 + 56 + 16 = **barra + 88** mientras Rutinas es la pestaña visible y ninguna página la tapa; en el resto, barra + 16. Movimiento en y con `CriticalSpring` ω 22, en el acto con reducir movimiento. El panel sigue anclado según AC-BLE-16-03. Aviso: sube sobre todo obstáculo que **se cruce** con él (8 dp de aire) | §4.12, §4.13, DN3-13, DN3-14 |
| §8.3.2 · Mini-barra con valores imposibles y tramos que no entran | Ancho de tramo `w = min(24; (W − 2·(n − 1)) / n)`, mínimo 2, alineada a la izquierda. `n` > capacidad (`⌊(W + 2) / 4⌋`): los primeros `capacidad` tramos a 2 dp y los últimos 6 con alfa 6/7 … 1/7. `n` = 0: pista `border.default` de todo el ancho. Siempre un renglón de 4 dp | §4.3, DN3-07 |
| §8.3.3 · Título del enfriamiento (DT-19) y clases de texto | **`step.cooldown.hud` (`#37A2CB`)**, 4,730:1; y la misma regla para los cuatro tipos (el trabajo pasa a `step.work.hud` `#FF5D7F`, porque la base da 3,815). Cambios de clase y color en §2.2 | §2.2, §2.3, DN3-02, DN3-03 |
| §8.3.4 · Campos | Etiqueta **dentro** de la caja (no sobre el borde, que con relleno no admite la muesca), 15 dp en reposo y 12 dp al flotar (escala 0,8 por código); ayuda (`Ej: 2`) siempre visible con el campo vacío (de acuerdo con DT-02: esos campos tienen la etiqueta siempre flotando); error con borde `state.danger` 2 dp y texto 12 debajo; selección `text.selection`, cursor `accent.primary` | §4.8, DN3-09 |
| §8.3.5 · Indicador de carga y ART_BIBLE §3.10 (DT-20) | **Manda la spec**: indicador circular de 32 dp, trazo 3, `accent.primary`, centrado entre las dos barras; aparece con fundido 160 después de 120 ms de gracia. Los *skeleton* de ART_BIBLE §3.10 salen de la biblia | §4.1, §12 |
| Spec P-09 · emojis del usuario | Noto Emoji monocromo como último respaldo; sin él, `□` | §4.15, DN3-21 |
| Spec §15 · pendientes del Designer | ART_BIBLE §3.7 (*chevron* → `⋮` en Rutinas), §3.8 (`Agregar paso a serie`, sin "la") y LEGACY_VISUAL_AUDIT §5.4 (descripción 12 y resumen 11 al 54 % en el legacy): se corrigen en WP-D | §12 |
| DT-01 · un solo desplazamiento en el editor | **De acuerdo**: a 640 × 360 con letra 1,3× la parte fija del legacy (campos, línea y fila de pasos) ocuparía ≈ 250 de los 304 dp visibles y dejaría ≈ 50 para la lista | §4.5 |
| DT-20, DT-02, DT-19 | Ver arriba | — |
| §8.2 · R3-06 (kerning de glifos nuevos en `FontAsset` dinámicos) | **Decidido en el gate** (QA-G3-18): el kerning es el de la tabla versionada y nada más, con `getFontFeatures = false` en las 7 caras de texto. En esta iteración no se genera tabla: medido, 12 textos pierden un par de como mucho 1,15 dp y los cinco glifos de R3-06 no cambian ninguno. La generación para un juego fijo queda descrita, con su tamaño, para cuando haga falta (It. 4a). Si el ajuste no se integra, las capturas se comparan solo entre corridas completas | §4.16, DN3-24 |

---

## 2. Tokens, clases de texto y colores

### 2.1 Tokens nuevos (WP-D0; `Tokens.cs` manda)

| Token | Valor | Rol | Uso | Por qué |
|---|---|---|---|---|
| `overlay.scrim` | `#000000` | gráfico | velo del diálogo (0,6), oscurecido de la pila (0,08) y capa de *hover* del ítem de peligro del menú (§4.4) | Lo pide 02-tech-design §7.1 |
| `border.input` **[nuevo]** | `#FFFFFF @40 %` | gráfico | borde de la caja del campo en reposo | Con `border.strong` (24 %) el borde da **2,132:1** contra el relleno `bg.sunken`; con 40 %, **3,807** (y 3,355 contra `surface.overlay`, 3,784 contra `bg.app`). El legacy dibuja el borde del `OutlineInputBorder` con el `outline` de M3, bien visible en `image-04.png` |
| `text.selection` **[nuevo]** | `#00B4D8 @35 %` | superficie | `--unity-selection-color` de los campos | Blanco sobre la selección: **9,586:1**. Ningún token con transparencia servía sin cambiar de sentido (`focus.ringHalo` es el halo del foco) |
| `titleXs` **[nuevo]** | Inter 14 / 20 / 600 | tipografía | título de la fila de paso, `Pasos (N)` | El contrato pide "body-m 600": la escala no tiene 14/600 fuera de `button` (que es para etiquetas de botón) |
| `headerS` **[nuevo]** | Inter 13 / 18 / 700 | tipografía | `Serie` del encabezado | AC-RUT-14-02 dice "negrita" (700) y el legacy usa 13/700 (`routine_editor_screen.dart:388-392`) |
| `buttonS` **[nuevo]** | Inter 12 / 16 / 600 | tipografía | `Agregar paso a serie` (AC-RUT-14-02: 12 dp) y los nombres de los mosaicos de tipo | 12/600 no existe (`label` es 12/600 con *tracking* de mayúsculas) |

Ningún token existente cambia. Total: **127 → 133** (02-tech-design contaba 128 con `overlay.scrim`). `Typography.uss` suma `.rm-type-title-xs`, `.rm-type-header-s` y `.rm-type-button-s` con sus escalas de letra; los cuatro pesos de Inter ya tienen `FontAsset` (`FontAssetPaths`).

### 2.2 Clase de texto y color por elemento (sobre 02-tech-design §7.5)

**[cambia]** = distinto del contrato. Todo color sale de `Tokens`.

| Elemento | Clase | Color |
|---|---|---|
| `.rm-routine-card__name` | `.rm-type-title-s` (16/600) | `text.primary` |
| `.rm-routine-card__description` | `.rm-type-body-xs` (12) | **`text.secondary`** [cambia] |
| `.rm-routine-card__summary` | `.rm-type-body-s` (13) | **`text.tertiary`** [cambia] |
| `.rm-empty__title` / `.rm-empty__hint` (Rutinas) | `.rm-type-body` / `.rm-type-body-s` | `text.secondary` / `text.tertiary` |
| `#routines-error-text` | `.rm-type-body` | `text.secondary` |
| `#editor-steps-count` | **`.rm-type-title-xs`** [cambia] | `text.primary` |
| `.rm-editor__empty` | `.rm-type-body-m` (14, el del legacy) | `text.tertiary` |
| `.rm-step-row__title` | **`.rm-type-title-xs`** [cambia] | **`step.<tipo>.hud`** [cambia] (§2.3) |
| `.rm-step-row__subtitle` | `.rm-type-body-xs` | `text.secondary`; el glifo `trending_up` en `metric.distance` (§4.6) |
| `.rm-series__title` | **`.rm-type-header-s`** [cambia] | `accent.primary` |
| `.rm-chip--repeat` (número) | `.rm-type-num-stat` (Barlow 16/700) | **`accent.onTonal`** [cambia] (§4.7) |
| `#series-add-step-<k>` (etiqueta) | **`.rm-type-button-s`** [cambia] | `accent.primary` |
| `.rm-field__input` | `.rm-type-body` | `text.primary` |
| `.rm-field__label` | **`.rm-type-body`** (15) [cambia], escala 0,8 al flotar (= 12 dp) | `text.secondary`; con foco `accent.primary`; con error `state.dangerText` |
| `.rm-field__error` | `.rm-type-body-xs` | `state.dangerText` |
| Ayuda (`placeholder`) | la del campo | `text.tertiary` |
| Rótulos `editorOptionalTargets`, `editorProgressions` | `.rm-type-body-xs` | `text.tertiary` |
| `.rm-segmented__option` (tiempo / distancia) | `.rm-type-button` | `text.secondary`; elegida `text.primary` |
| Mosaico de tipo (etiqueta) | **`.rm-type-button-s`** [nuevo] | `text.secondary`; elegido `text.primary` |
| `.rm-dialog__title` | `.rm-type-title` | `text.primary` |
| Contenido del diálogo de eliminar | `.rm-type-body` | `text.secondary` |
| `.rm-toast__text`, `.rm-menu__item` | `.rm-type-body-m` | `text.primary`; ítem de peligro `state.dangerText` |
| `.rm-tooltip` | **`.rm-type-body-xs`** [cambia] (12, como M3) | `text.primary` |

Por qué se invierten descripción y resumen: es el orden del legacy (descripción en el color de subtítulo de `ListTile`, resumen al 54 %, `routines_screen.dart:56-61`) y la mini-barra ya cuenta la estructura. Los tamaños siguen siendo los de AC-RUT-10-03 y DV-12 (12 y 13).

### 2.3 Color por tipo de paso

Regla: **texto e íconos del tipo, variante `.hud`; gráficos, color base** (la misma regla del HUD, ART_BIBLE §2.3). Las variantes `.hud` llegan a 4,5:1 contra el peor vidrio, que es más claro que la tarjeta: sobre la tarjeta y el diálogo pasan con más margen.

| Tipo | Borde, mini-barra, borde del mosaico elegido | Fondo de insignia y de mosaico elegido (`@20 %`, por código) | Ícono y título | Título sobre tarjeta | Ícono sobre su insignia |
|---|---|---|---|---|---|
| Calentamiento | `step.warmup` `#FFD166` | sobre tarjeta `#484F4C` · sobre diálogo `#4B5454` | `step.warmup.hud` `#FFD166` | 9,577 | 5,863 |
| Trabajo | `step.work` `#EF476F` | `#45334D` · `#483956` | `step.work.hud` **`#FF5D7F`** | **4,685** (base 3,815) | 3,889 (base 3,167) |
| Descanso | `step.rest` `#06D6A0` | `#165057` · `#195560` | `step.rest.hud` `#06D6A0` | 7,322 | 4,824 |
| Enfriamiento | `step.cooldown` `#118AB2` | `#18405B` · `#1B4664` | `step.cooldown.hud` **`#37A2CB`** | **4,730** (base 3,487) | 3,723 (base **2,744**) |

Los íconos de tipo son los de AC-RUT-01-04 (`whatshot`, `fitness_center`, `pause_circle`, `ac_unit`).

### 2.4 Colores con transparencia por código

USS no aplica transparencia a un `var()`: estos colores los pone el código desde `Tokens` (como el borde del tablero en la It. 2). Lo demás va por USS.

| Elemento | Color | Compuesto (sonda) |
|---|---|---|
| `.rm-step-row__badge-bg`, mosaico de tipo elegido | `step.<tipo> @20 %` | §2.3 |
| `.rm-series__header` | `brand.seed @12 %` | sobre `bg.app` `#0B263B` |
| `.rm-series__steps` **[nuevo fondo]** y `.rm-series__footer` | `brand.seed @6 %` | `#0C2132` |
| `.rm-series__stack-layer` 1 / 2 **[nuevo]** | `brand.seed @20 %` / `@12 %` | `#0A2D46` / `#0B263B` |
| `.rm-chip__pill` **[nuevo]** | `accent.primary @20 %` | sobre el encabezado `#09425A` |
| `.rm-dialog__scrim` | `overlay.scrim`, opacidad 0 → 0,6 | sobre `bg.app` `#050B11`; sobre tarjeta `#0A121C` |
| `.rm-page-stack__dim` | `overlay.scrim`, opacidad 0 → 0,08 | — (no sale en capturas) |
| `.rm-highlight` **[nuevo]** | `text.primary`, opacidad 0,08 → 0 | — |

El chip de la tarjeta de rutina (`accent.tonalBg`, `#173E57` sobre la tarjeta) y la ayuda de selección (`text.selection`) son tokens con su alfa: van por USS.

---

## 3. Geometría por tamaño (verificada)

| Medida | TV 411 | TH 914 | L640 | W1280 | Cálculo |
|---|---|---|---|---|---|
| Tarjeta de rutina | 379 | 882 | 608 | 1 248 | ventana − 2 × 16 |
| Columna de texto de la tarjeta | **255** | 758 | 484 | 1 124 | tarjeta − 16 − 36 − 16 − 4 − 48 − 4 |
| Capacidad de la mini-barra | 64 | 190 | 121 | 281 | `⌊(columna + 2) / 4⌋` |
| Fila de paso suelta / de serie | 387 / 371 | 890 / 874 | 616 / 600 | 1 256 / 1 240 | ventana − 2 × 12 (− 16) |
| Texto de la fila suelta / de serie | **228 / 212** | 731 / 715 | 457 / 441 | 1 097 / 1 081 | fila − (3 + 12 + 32 + 12 + 96 + 4) |
| Diálogo de paso (ancho · contenido) | 331 · **283** | 560 · 512 | 560 · 512 | 560 · 512 | `min(560; ventana − 80)` − 2 × 24 |
| Diálogo de eliminar y de repeticiones | 331 | 400 | 400 | 400 | `min(400; ventana − 80)` (DN3-10) |
| Alto máximo del diálogo | 866 | 363 | 312 | 672 | ventana − 48 |
| Mosaicos de tipo | 2 × 2 de 137,5 × 56 | 4 de 122 × 56 | ídem | ídem | 4 columnas si el contenido ≥ 400 |

**Anchos medidos** (sin kerning; a 1,3× se multiplican):

| Texto | Ancho | Lugar | Entra |
|---|---|---|---|
| `Calentamiento` 14/600 · 12/600 | 98,4 · 84,4 | mosaico de 137,5 (TV) o 122 (− 12 de relleno) | a 12/600 sí, también a 1,3× (109,7 en 110). A 14/600 en horizontal haría falta 148 dp por mosaico: por eso el ícono va arriba |
| `Por distancia` 14/600 | 88,1 | segmento de 141,5 − 24 | sí; a 1,3×, 114,5 en 117 |
| `2m␣␣·␣␣120-180W · 22 spm · 02:05/500m` 12 | 224,5 | fila suelta, 228 | en una línea (justo); a 1,3× pasa a dos, como pide AC-RUT-12-06 |
| `500m␣␣·␣␣150-180W · 02:05/500m · ` + ícono + ` W +10/rep, Split -2s/rep` 12 | 197,1 + 14 + 139,5 | fila de serie, 212 | dos líneas (CAP-13) |
| `Cambio Split segundos (+/-/rep)` 12 · `Split change in seconds (+/-/rep)` 12 | 183,8 · 188,3 | etiqueta flotante, 283 − 24 | sí; a 1,3×, 244,8 en 259 |
| `Fuera de rango en la repetición 6: hasta ±999` 12 | 259,1 | error, 283 − 12 | sí; a 1,3× pasa a dos líneas (ajusta) |
| `Hay un paso con valores fuera de rango` 14 | 265,3 | aviso en TV, 379 − 32 | sí |
| `Agregar paso a serie` 12/600 | 120,2 | pie, botón de 166 | sí |
| `Pasos (11)` + `+ Paso` + `Serie` | 66,8 + 83,3 + 84,9 | fila de 411 | sí; a 1,3×, 295 |
| `×2000000000` Barlow 16/700 | 88,3 | cápsula del chip | sí (crece; nunca se corta) |

**Altos**: tarjeta de `Escalera SPM` 85,9 (16 + 19,8 + 4 + 16,1 + 10 + 4 + 16), con descripción 102,8; fila de paso 56 (56,9 con el subtítulo en dos líneas; WP-D, §16.2); encabezado y pie de serie 48; campo 52, descripción 72; mosaicos 56; segmentado 40.

**Diálogo de paso de una serie en TV**: título 65 (24 + 24,8 + 16) + vista previa fija 72 + contenido 684 + acciones 96 = 917 > 866: se desplazan 51 dp (CAP-15b). Un paso suelto (460 de contenido) entra sin desplazar.

---

## 4. Componentes

### 4.1 Rutinas: barra, cuerpo, botón `+` y estados

| Parte | Spec |
|---|---|
| `#routines-appbar` | la barra de la It. 2 (56, título a 16, sin acciones) |
| `#routines-list` | relleno 8 arriba, **16** a los costados (alineado con el título; el legacy usa 12, `routines_screen.dart:42`); separación entre tarjetas **10** (legacy 8). Abajo, dentro del contenido: el tope del obstáculo más alto que se cruza con la lista + 16 (normal: barra + 88 = 152 en TV; con la llave en Rutinas: barra + 144 = 208). Ancho completo también en escritorio (paridad con el legacy y con CAP-11; el máximo de 960 de ART_BIBLE §5.1 no se aplica en esta iteración) |
| `#routines-add` `.rm-fab` | círculo 56, `accent.primary`, `add` 24 `text.onAccent` (7,641); `right 16`, `bottom` = barra + 16 [contrato]. *Hover* capa 4 %; presionado: fondo `accent.primaryPressed` y escala 0,94; anillo de foco radio 32 / halo 36. Sin sombra (no hay en UI Toolkit): el disco cian contra la tarjeta da 5,603 |
| Centro de los tres estados | centrados en el área **entre la barra superior y la de pestañas** (relleno inferior del cuerpo = alto de la barra), como la pantalla provisoria de la It. 2 |
| Cargando (`#routines-loading`) | `Spinner` de **32 dp, trazo 3**, `accent.primary`, una vuelta en 900 ms. Aparece si la carga sigue a los **120 ms**, con fundido 0 → 1 en 160 (`ease.enter`): una carga de 20 ms no deja un parpadeo. Nada más en el cuerpo (CAP-02) |
| Vacío (`#routines-empty`) | `fitness_center` 56 `text.primary` opacidad **0,12** (trazo `#2A3644`, muy tenue a propósito: AC-RUT-10-02) → 12 → `routinesEmpty` → 8 → `routinesEmptyHint`, centrados, ancho máximo 320 |
| Error (`#routines-error`) | **[nuevo]** `#routines-error-icon`: `error` 48 `state.dangerText` (6,545) → 12 → `routinesLoadError` (`text.secondary`, 8,967; ancho máximo 280, ajusta) → 16 → `#routines-retry` tonal (`refresh` 18 + `Reintentar`; 7,468, con *hover* 6,660). El ícono da a la pantalla la misma forma que el vacío y deja claro que es un problema, no "no hay nada" |

### 4.2 Tarjeta de rutina

```
#routine-card-<n> .rm-routine-card                fila, align-items flex-start; relleno 16 / 4 / 16 / 16; r.card 16; surface.card
├── .rm-button__state                              capa común (r.card): hover 4 %, presionado 8 %; sin escala (superficie grande)
├── .rm-highlight [nuevo]                          r.card; blanco, opacidad por código (§5.5); pickingMode Ignore
├── .rm-routine-card__icon .rm-icon                caja 36 × 36, r.m 8, fondo --rm-accent-tonal-bg (#173E57); glifo 22 --rm-accent-primary (4,572); margin-right 16
├── .rm-routine-card__text                         flex 1 1 0, min-width 0
│   ├── __name                                     title-s, ajusta (nunca "…")
│   ├── __description                              solo si no está vacía; margin-top 2; ajusta y respeta los saltos de línea
│   ├── __summary                                  margin-top 4
│   └── .rm-structure-bar                          alto 4, margin-top 10, ancho de la columna
├── #routine-menu-<n> .rm-icon-button              more_vert 24 text.secondary; margin-top −14 y margin-left 4: el centro del ícono
│                                                  queda a 26 dp del borde superior (el centro del nombre, 25,9) y a 28 del derecho
└── .rm-focus-ring                                 radio 20 / halo 24
```

- El chip del ícono usa `accent.tonalBg` (12 %) y no el 14 % de ART_BIBLE §3.7: es el mismo fondo del botón tonal y no hace falta código. El legacy pone el ícono suelto (DV-12).
- El `⋮` queda en la columna de 28 dp del borde derecho, la misma de la cruz de las filas y del tacho de la serie: los tres "menús de la fila" se alinean.
- Nombre o descripción de 200 caracteres: ajustan en varias líneas y la tarjeta crece (AC-RUT-10-03). Una sola palabra de 200 caracteres sin espacios también se parte donde haga falta y nunca sale de la columna (spec AC-RUT-10-03; la prueba de RLT es de QA-G3-13 f).

### 4.3 Mini-barra de estructura (`StructureBarView`)

| Regla | Valor |
|---|---|
| Tramo | alto 4, radio 2, separación 2, color base del tipo (§2.3; todos ≥ 3,487:1 sobre la tarjeta) |
| Ancho | `w = min(24; (W − 2·(n − 1)) / n)`, **alineada a la izquierda**. TV (W = 255): `Solo calentamiento` 1 tramo de 24; `Intervalos 4×` 7 de 24 (180 dp); `Escalera SPM` ×3 8 de 24 (206); ×4 10 de 23,7; `Pirámide 500` 14 de 16,4 (todo el ancho). Así el largo de la franja también cuenta cuántos tramos tiene, como la silueta de la sesión (spec §10.5) |
| No entran (`n` > capacidad = `⌊(W + 2) / 4⌋`, 64 en TV) | se dibujan los primeros `capacidad` tramos de `Leading` a 2 dp y los **últimos 6** con alfa 6/7, 5/7 … 1/7: se lee "sigue". `×2 000 000 000` en TV: 64 tramos rosa y verde alternados que se apagan hacia la derecha |
| Cero tramos (`×0`, o una rutina que expandida no tiene pasos) | una **pista** de todo el ancho, alto 4, radio 2, `border.default` (`#FFFFFF @12 %`): la tarjeta mantiene su alto y no queda un hueco |
| Nunca | más de un renglón, ni salir de la columna, ni un bucle por tramo de la rutina expandida (usa `RoutineStructure.Segments(t, capacidad)`) |
| Para los tests | `SegmentCount` (dibujados), `SegmentTypes`, `SegmentWidth` y `Overflow` (bool) **[nuevo]** |
| Cuándo se calcula | con el ancho **resuelto** de la columna, en `GeometryChangedEvent` (antes del primer *layout* el ancho es 0). Los tests esperan el *layout* antes de leer `SegmentCount` (gate de QA §7) |

### 4.4 Menú `⋮`, menú de texto y ayudas (`#popup-layer`)

| Parte | Spec |
|---|---|
| `.rm-menu` | `surface.overlay`, `r.m` 8, borde 1 `border.default`, relleno 8 / 0; ancho mínimo 140, máximo 280 |
| `.rm-menu__item` | `min-height` **48**, relleno 0 / 16, `.rm-type-body-m` `text.primary` alineado a la izquierda, una línea con "…". *Hover* capa blanca 4 % (11,093:1), presionado 8 % (9,802) |
| `.rm-menu__item--danger` | `state.dangerText` (4,703). Su capa de estado es **oscura**: `overlay.scrim` al **12 %** con *hover* (5,147) y 24 % presionado (5,603). Con la capa blanca daría 4,174 con *hover*, debajo de AA, y no hay aclarado que llegue: el ítem de peligro "se hunde" en lugar de iluminarse |
| `.rm-menu--dense` **[nuevo]** (menú del clic derecho, solo escritorio) | ítems de `min-height` 36 y relleno 0 / 12 |
| Foco en un ítem | anillo **adentro** (`inset 2`, radio 6, sin halo), para no salir del menú |
| Posición del `⋮` | debajo del botón con 4 dp de aire, **borde derecho alineado** con el del botón; si no entra abajo, arriba; siempre a ≥ 8 dp de los bordes de la ventana. El de texto: esquina superior izquierda en el puntero + (2; 2), con las mismas vueltas |
| `.rm-tooltip` | `surface.overlay`, borde 1 `border.default`, `r.s` 6, relleno 4 / 8, `min-height` 24, `.rm-type-body-xs` `text.primary`, ancho máximo 240 (ajusta). Centrada **8 dp debajo** del ancla (arriba si no entra), a ≥ 8 dp de los bordes. **`pickingMode = Ignore`** siempre: la ayuda nunca recibe un toque ni tapa el control que está debajo (QA-G3-01) |

### 4.5 Editor: barra, campos, fila de pasos y vacío

| Parte | Spec |
|---|---|
| `#editor-appbar` | 56; `#editor-back` `.rm-icon-button` (`arrow_back` 24 `text.primary`) a 4 del borde; título con `margin-left` **20** (queda a 72, como el legacy en `image-04.png`); `#editor-save` botón de texto (su texto a 16 del borde derecho). La cinta tapa parte de la flecha (paridad) |
| `.rm-editor__fields` | relleno 16; `#editor-name` (52) → 12 → `#editor-description` (72, dos líneas) |
| `.rm-divider` | 1 dp `border.default`, de borde a borde |
| `.rm-editor__steps-header` | relleno 8 / 4 / 8 / 16 (alto 56); `#editor-steps-count` → espacio → `#editor-add-step` (texto, `add` 18) → 4 → `#editor-add-series` (texto, `repeat` 18) |
| `#editor-steps` | relleno 0 / 12 (como el legacy, `routine_editor_screen.dart:298`); abajo, el tope del obstáculo más alto que se cruza + 16 (normal 24; con la llave 136), más área segura y teclado |
| `#editor-empty` | centrado en el espacio que queda debajo de la fila de pasos (el contenido del `ScrollView` con `min-height` del 100 % del alto visible y el vacío con `flex-grow 1`); `.rm-type-body-m` `text.tertiary`, ancho máximo 280, dos líneas |

### 4.6 Fila de paso (`.rm-step-row`)

```
#step-row-<i> .rm-step-row .rm-step-row--<tipo>    fila, align-items center; min-height 56; r.l 12; surface.card;
                                                   borde izquierdo 3 (color base); margen 3 / 0 (6 entre filas); relleno 4 / 4 / 4 / 12 [WP-D, §16.2]
├── .rm-highlight [nuevo]                          r.l; resaltado al editar (§5.6)
├── .rm-step-row__badge                            32 × 32, r.s 6, margin-right 12
│   ├── .rm-step-row__badge-bg                     tipo @20 % (por código)
│   └── .rm-icon                                   16, step.<tipo>.hud
├── .rm-step-row__text                             flex 1 1 0, min-width 0, centrado vertical
│   ├── __title                                    title-xs, step.<tipo>.hud
│   └── __subtitle (StepSubtitleView)              body-xs text.secondary, margin-top 2, ajusta (nunca "…")
├── #step-edit-<i>   .rm-icon-button               edit 20, text.secondary (7,555)
└── #step-delete-<i> .rm-icon-button --danger      close 20, state.dangerText (5,196; 4,616 con hover)
```

- **Lápiz en `text.secondary`**, no en `accent.primary` como decía ART_BIBLE §3.8: es neutro en el legacy (`image-04.png`) y con 60 filas cian competiría con los colores de tipo y con el azul de la serie.
- **Íconos de acción de 20** dentro de la caja de 48 (el legacy usa 16 en 32; 24 pesaba más que la insignia). Hace falta el tamaño por parámetro en `IconButton` (DN3-17).
- **Subtítulo con `trending_up`** (DV-10): en el camino principal de 02-tech-design §4.4 (un `Label` con el glifo por fuente de respaldo), el glifo va con texto enriquecido **generado por código** desde `Tokens`: `<color=#4CC9F0><size=117%>` + U+E8E5 + `</size></color>` (`metric.distance`, 7,181:1; 117 % = 14 dp sobre 12). Si S-B3a muestra el glifo corrido de la línea de base, se agrega `<voffset>` con el valor medido. El subtítulo no lleva texto del usuario, así que el formato es seguro. En el respaldo de tres piezas: `IconView` 14 `metric.distance` con `margin` 0 / 2.
- **Sin *hover* ni presión en la fila** (no es accionable; lo son el lápiz y la cruz). El anillo de foco va en los dos botones.
- `.rm-step-row--preview` (vista previa del diálogo): la misma fila, sin los dos botones y con relleno derecho 12.

### 4.7 Bloque de serie (`.rm-series`)

```
#series-<k> .rm-series                             columna; margen 8 arriba y 8 abajo [contrato]
├── #series-header-<k> .rm-series__header          fila centrada; min-height 48; relleno 0 / 4 / 0 / 12; radios superiores 12;
│   │                                              borde izquierdo 3 brand.seed; fondo seed @12 % (por código)
│   ├── .rm-icon (repeat)                          16, accent.primary, margin-right 6
│   ├── .rm-series__title                          header-s, accent.primary (6,286), margin-right 4
│   ├── #series-repeat-<k> .rm-chip --repeat       caja táctil ≥ 48 × 48, transparente, relleno 0 / 4
│   │   └── .rm-chip__pill [nuevo]                 alto 28, min-width 40, relleno 0 / 10, radio 14; fondo accent @20 % (por código);
│   │       │                                      número num-stat en accent.onTonal (5,620; 5,030 con hover)
│   │       ├── .rm-button__state                  radio 14; hover 4 %, presionado 8 % y escala 0,97 de la cápsula
│   │       └── .rm-focus-ring                     radio 18 / halo 22
│   ├── (espacio flexible)
│   └── #series-delete-<k> .rm-icon-button --danger    delete 20, state.dangerText (5,830); ayuda editorDeleteSeries
├── .rm-series__steps                              borde izquierdo 3 brand.seed [nuevo]; fondo seed @6 % (por código) [nuevo]
│   └── #step-row-<i> … --in-series                margin-left 13 [cambia]: con el riel de 3 la sangría visible es 16 (AC-RUT-14-02)
├── #series-footer-<k> .rm-series__footer          min-height 48; radios inferiores 12; borde izquierdo 3 brand.seed; fondo seed @6 %;
│   │                                              contenido centrado (como image-04.png)
│   └── #series-add-step-<k> .rm-button --text     add 16 + 6 + button-s accent.primary (6,683; 6,003 con hover)
└── .rm-series__stack [nuevo]                      absoluta: left 0, right 0, bottom −6, alto 6; pickingMode Ignore
    ├── .rm-series__stack-layer (1)                left 8, right 8, top 0, alto 3; radios inferiores 4; seed @20 %
    └── .rm-series__stack-layer (2)                left 16, right 16, top 3, alto 3; radios inferiores 4; seed @12 %
```

- **Riel continuo**: el borde de 3 dp `brand.seed` (3,573:1 sobre `bg.app`; 3,384 sobre el cuerpo) baja sin cortes del encabezado al pie y el cuerpo lleva el mismo tinte que el pie. En el legacy el riel se corta a la altura de los pasos (`image-04.png`); con él, el bloque se lee como un corchete que agrupa sus pasos. Nada cambia de lugar: encabezado, sangría, pie, chip, tacho, lápiz y cruz quedan donde están en `image-04.png` (CAP-11).
- **Hojas apiladas** (spec §10.1, "deja intuir sus repeticiones"): `×0` y `×1`, ninguna; `×2`, la primera; `×3` o más, las dos. Asoman en los 8 dp de aire de abajo, sin cambiar la geometría del bloque.
- **Chip**: la cápsula conserva el fondo de la spec (`#00B4D8 @20 %`) y el número pasa a `accent.onTonal`: con `#00B4D8` da 4,370 (16/700 no es texto grande). Ancho según el número: `×3` 40 dp, `×999` 50, `×2000000000` 108; nunca se corta.
- Encabezado de 48 (legacy ≈ 36): lo pide el área táctil del chip y del tacho (DV-12).
- Dos series contiguas: 8 + 8 = **16 dp** entre el pie de una y el encabezado de la otra, como en el legacy (§10).

### 4.8 Campo de texto (`RmTextField`)

```
#<name> .rm-field [--focused] [--filled] [--error] [--multiline] [--floating] [nuevo]
├── .rm-field__box          min-height 52 (multilínea 72); bg.sunken; borde 1 --rm-border-input; r.m 8; relleno 0 / 12
│   ├── TextField .rm-field__input   relleno superior 22: el texto ocupa 22-40,6 (una línea) o 22-59,2 (dos)
│   └── .rm-field__label    absoluta, left 12; body 15 text.secondary; en reposo centrada en la primera línea (top 16,7);
│                           flotando: top 6 y escala 0,8 con origen arriba a la izquierda (= 12 dp, caja 14,9)
└── .rm-field__error        margin-top 4, relleno izquierdo 12, body-xs state.dangerText, ajusta; display none sin error
```

| Estado | Caja | Etiqueta |
|---|---|---|
| Reposo | borde 1 `border.input` (3,807 contra el relleno) | `text.secondary` (9,401) |
| *Hover* (escritorio) | borde `--rm-text-tertiary` (54 %) | igual |
| Foco | borde **2** `accent.primary` (7,585) y relleno 0 / 11 (no se corre el texto) | flotando, `accent.primary` (7,585) |
| Error (con o sin foco) | borde **2** `state.danger` (5,859) | `state.dangerText` (7,035) |

- **`--floating`** = con foco, con texto **o con ayuda** (`placeholder`). Los tres campos de progresión tienen la etiqueta siempre arriba y la ayuda (`Ej: 2`, `text.tertiary`, 5,992) visible con el campo vacío: es lo que piden AC-RUT-13-02 y CAP-15b (DT-02).
- La etiqueta va **dentro** de la caja: el `OutlineInputBorder` del legacy la pone sobre el borde con una muesca, que con un relleno distinto del fondo no se puede imitar en UI Toolkit (el fondo de la etiqueta tendría que ser dos colores a la vez).
- Selección `--unity-selection-color: var(--rm-text-selection)` (blanco encima 9,586); cursor `--unity-cursor-color: var(--rm-accent-primary)`.
- **Una sola columna** [WP-D, §16.2]: etiqueta, ayuda, selección, cursor y texto empiezan a 13 dp del borde de afuera de la caja. El `TextElement` interno del campo de UI Toolkit traía un margen propio de ≈ 2 dp que corría el texto a la derecha de la etiqueta; `Skin.uss` lo pone en 0, en una línea y en multilínea.
- **Sin anillo de foco** en los campos: el borde de 2 dp con el acento es el indicador (7,585:1). El anillo queda para los demás controles.
- **Letra 1,3×**: la etiqueta y el texto crecen, así que el relleno superior del texto se calcula con las cajas resueltas (6 + caja de la etiqueta flotante + 1, no un 22 fijo) y la caja crece con `min-height` (≈ 62 de una línea, ≈ 88 la descripción). Nunca un `height` fijo (DN3-09).
- **Pares** (Min/Seg, W mín/W máx, Split min/Split seg): dos campos `flex 1 1 0` con 8 dp entre ellos (legacy 8). Error de par (`#step-time-error`, `#step-split-error`): las **dos** cajas en error y un solo texto debajo del par, a 12 de su borde izquierdo.

### 4.9 Diálogos: base y velo

| Parte | Spec |
|---|---|
| `.rm-dialog__scrim` | `overlay.scrim` al 60 % (por código); sobre `bg.app` queda `#050B11` |
| `.rm-dialog` | `surface.overlay`, `r.card` 16, borde 1 `border.default` (sin sombra posible; sobre el velo se separa con 1,583:1 más el borde); ancho `min(máx; ventana − 80)` con mínimo 280 y máximo **400** (eliminar, repeticiones) o **560** (paso) (DN3-10); alto máximo `ventana − 48 − área segura` |
| `.rm-dialog__title` | relleno 24 / 24 / 16 / 24; ajusta (nunca "…") |
| `.rm-dialog__content` | relleno 0 / 24; `ScrollView` con `ScrollPolicy`. Cuando el contenido se desplaza, línea de 1 dp `border.subtle` arriba y abajo del área (`.rm-dialog__content--scrolls`, DN3-10) y **8 dp de aire** entre cada línea y lo primero o lo último del contenido [WP-D, §16.2] |
| `.rm-dialog__actions` | relleno 24; fila a la derecha, 8 entre botones: secundaria de texto (`accent.primary`, 5,071; 4,501 con *hover*), principal rellena (`accent.primary` con `text.onAccent`, 7,641) o `.rm-button--danger-fill` (4,979; 4,740 con *hover*). Los altos de contrato (40, 48, 44) quedan centrados en la fila: no hace falta igualarlos |
| `.rm-dialog--compact` **[nuevo]** | con la ventana de menos de 480 dp de alto: rellenos de 16 en lugar de 24 (título 16 / 16 / 12 / 16; acciones 16). En L640 suma 24 dp al área desplazable |

### 4.10 Diálogos de eliminar y de repeticiones

- **Eliminar rutina**: `routineDeleteTitle`; contenido `routineDeleteContent` (`.rm-type-body`, `text.secondary`, 6,984; la etiqueta **sin texto enriquecido**, porque lleva el nombre, DN3-22); acciones `Cancelar` y `Eliminar` relleno `#D32F2F`.
- **Repeticiones**: `editorRepetitions`; un campo `Cantidad` (52) con foco al abrir; `#repeat-error` es el error del campo (`Máximo 999`, `Con ×492, un objetivo queda fuera de rango`; caja en error); acciones `Cancelar` y `OK`.

### 4.11 Diálogo de paso

De arriba abajo (las separaciones son las del legacy, `routine_editor_screen.dart:574-725`, ajustadas a 4 / 8 / 12 / 16):

| # | Parte | Spec |
|---|---|---|
| 0 | `#step-preview` | `.rm-step-row--preview`. **Fija** entre el título y el contenido (`.rm-dialog__pinned` **[nuevo]**, margen inferior 16) si el alto máximo del diálogo es ≥ 560 (TV, W1280); si no (L640, TH), **primer elemento del contenido**, con margen inferior 16. Así en la demo (W1280) se ve mientras se escribe `Incremento SPM` al final del desplazamiento, y en 640 × 360 no se come el poco alto que hay. Sin rótulo (la spec no suma textos): se reconoce porque es la misma fila del editor |
| 1 | Tipo `#step-type-*` (`.rm-segmented--tiles` **[nuevo]**) | mosaicos de `min-height` 56, `r.m` 8, borde **2** (no elegido `border.default`; elegido el color base del tipo: 3,155-8,668 contra el diálogo), relleno 8 / 6; columna centrada: ícono 20 `step.<tipo>.hud` → 4 → nombre `button-s`. Elegido: fondo tipo `@20 %` y nombre `text.primary` (7,770-10,572); no elegido: nombre `text.secondary` (6,984). **2 × 2** en TV (137,5 × 56, 8 entre ellos) y **4 en fila** desde 400 dp de contenido (122 × 56). Orden `Calentamiento`, `Trabajo` / `Descanso`, `Enfriamiento` (AC-RUT-13-03) |
| 2 | `#step-mode-*` (`.rm-segmented`) | margin-top 12; alto 40, radio 20, borde 1 `border.strong`, dos opciones `flex 1 1 0` separadas por 1 dp `border.strong`; elegida fondo **`brand.seed`** [CONTRATO de ART_BIBLE §3.9] con texto blanco (4,868); no elegida transparente con `text.secondary` |
| 3 | `#step-min` + `#step-sec` o `#step-meters` | margin-top 12; par o campo entero; `#step-time-error` debajo del par |
| 4 | Separador y `editorOptionalTargets` | margin-top 16, `.rm-divider`, margin-top 12, rótulo (4,829), margin-bottom 8 |
| 5 | `#step-wmin` + `#step-wmax`, `#step-spm`, `#step-split-min` + `#step-split-sec` | 8 entre filas; `#step-split-error` debajo del par |
| 6 | Solo en serie: separador, `editorProgressions`, `#step-prog-spm`, `#step-prog-watts`, `#step-prog-split` | como 4 y 5; los tres con ayuda (etiqueta siempre flotando) |
| 7 | `#dialog-cancel`, `#dialog-confirm` | §4.9 |

- **Errores de Q-92**: al `Confirmar` con errores, el diálogo queda abierto, el foco va al **primer campo con error** y el contenido se desplaza hasta mostrarlo con su texto de error y 16 dp de aire (DN3-10). El error de una progresión por repetición (`Fuera de rango en la repetición 6: hasta ±999`) va debajo de esa progresión.
- **Cambiar de tipo** recolorea el mosaico, la vista previa (borde, insignia, ícono y título) en 160 ms (§5.7).

### 4.12 Avisos (`#toast`)

| Parte | Spec |
|---|---|
| `.rm-toast` | `surface.overlay`, borde 1 `border.default`, `r.l` 12, relleno 12 / 16, `min-height` 48; texto `body-m` `text.primary` (12,498), ajusta hasta 3 líneas. **Sin ícono** (ART_BIBLE §3.10 decía "ícono de estado"): los cinco avisos se explican solos y `Show(string)` no sabe la gravedad |
| Ancho | ventana < 600 dp: de 16 a 16 (379 en TV). Si no: según el texto, `clamp(texto + 32; 344; 560)`, centrado |
| Posición | base: alto de la barra visible (o área segura) + 16. Si su rectángulo **se cruza** con un obstáculo inferior agrandado 8 dp, sube hasta 8 dp sobre ese obstáculo; se repite hasta que no cruce ninguno (DN3-13) |

Resultados (la barra mide 64; alto del aviso, 48):

| Dónde | Sin simulador | Con simulador |
|---|---|---|
| Editor, TV (CAP-18, CAP-19, CAP-28, VID-03) | 16 → 64 desde abajo | igual: la llave (80-120) no se cruza (64 + 8 < 80) |
| Rutinas, TV (aviso de borrado) | cruza el `+` (80-136) → **144-192** | cruza además la llave (152-192) → **200-248** |
| Rutinas, W1280 | 80-128, centrado de 344 (el `+` está a la derecha y no se cruza) | igual |

Si el panel del simulador está abierto, puede tapar el aviso: es la capa de prueba que el usuario abrió, como en la It. 2 (AC-APP-03-05 habla de la llave).

### 4.13 Llave del simulador (DV-13)

| Situación | Posición |
|---|---|
| Rutinas visible, sin página encima | `right 16`, `bottom` = barra + **88** (16 dp sobre el `+`, bordes derechos alineados) |
| Otra pestaña, o el editor abierto (barra en `display: none`, alto conservado) | `right 16`, `bottom` = barra + 16 (AC-BLE-16-02) |

- El `+` se registra como obstáculo mientras Rutinas es la pestaña elegida **y** `CoversShellChanged(false)`; se quita al terminar la entrada del editor y vuelve al empezar su salida. La llave sigue al obstáculo (§5.8).
- Alternativas descartadas: a la izquierda del `+` (taparía el resumen y la mini-barra de la última tarjeta, en el medio del contenido); un lugar fijo para todas las pestañas (cambia AC-BLE-16-02 en Dispositivo, donde hoy no molesta). Apilada sobre el `+` es el patrón de dos botones flotantes de Material y no se mueve en x.
- Texto de AC-BLE-16-02: el PO lo integró en la spec §5 con la propuesta de §10 (QA-G3-05) y agregó dónde se abre el panel. Se abre **siempre en el mismo lugar** (AC-BLE-16-03, 16 dp sobre la barra), también en Rutinas, donde tapa el `+` como en el legacy. Mientras está abierto, la llave no se ve, y al cerrarlo vuelve a su lugar sobre el `+`, con la transición del panel de la It. 2. Visualmente es correcto: es la capa de prueba que el usuario abrió, y anclarla en un solo lugar evita que salte entre pestañas.

### 4.14 Teclado, foco y letra grande

- **Anillo de foco** (solo con `.rm-keyboard-nav`; la regla de la It. 2: radio del control + 4, halo + 4): tarjeta 20 / 24; `⋮`, flecha, lápiz, cruz y tacho 28 / 32; cápsula del chip 18 / 22 (alrededor de la cápsula, no de la caja de 48); botón del pie y botones de texto 16 / 20; mosaico 12 / 16 (a 4 dp de su borde de afuera, que es de 2); opción del segmentado (tiempo / distancia): **adentro**, a 2 dp del borde, esquinas de afuera 17 y del divisor 2, sin halo, en `focus.ring` sobre la no elegida y en blanco sobre la elegida (4,868:1), porque la cápsula recorta lo que sale de ella [WP-D, §16.2]; `+` 32 / 36; ítem de menú, adentro (§4.4). Los campos, sin anillo (§4.8).
- **Teléfono con teclado** (AC-RUT-11-10): el campo con foco, con su texto de error, queda **16 dp** por encima del teclado; el desplazamiento sigue un `CriticalSpring` ω 22 (≈ 260 ms, el orden del teclado del sistema). Con un diálogo, el diálogo se recentra en el área sobre el teclado (mismo resorte) y su alto máximo pasa a `área − 48`. Reducir movimiento: en el acto. [Device-And].
- **Letra 1,3×** (CAP-25): todo lo de §3 entra; los textos ajustan (nombres, descripciones, subtítulos, errores, avisos, títulos de diálogo); los botones siguen en una línea con "…" (ART_BIBLE §3.6), y ninguno de esta iteración llega a cortarse en TV. Los íconos y la mini-barra no escalan.

### 4.15 Textos del usuario: emojis y etiquetas sin formato

- **P-09.** Respaldo de fuente del panel, en este orden: Inter → RowMate Icons (contorno; solo área privada, no choca con ningún texto) → **Noto Emoji** monocromo ([Google Fonts](https://fonts.google.com/noto/specimen/Noto+Emoji), SIL OFL 1.1), como `FontAsset` dinámico (el atlas se llena solo con los emojis que aparecen). Se dibuja en el color del texto, como un glifo más: un 🚣 en el nombre queda blanco y del alto de la línea. Los emojis a color (CBDT/COLR) no los dibuja TextCore. Mientras la fuente no esté en el repo, el glifo que falta sale como `□` y el resto del texto no se corre (lo prueba S-B3a con `Remo 🚣 ñ`, DN3-21). Slot nuevo `ui.font.emoji` en ASSET_SLOTS.
- **Dónde se arma la lista de respaldos** (QA-G3-12): una sola fábrica crea la copia de `PanelTextSettings` con estos respaldos. La usan `AppRoot.Awake` (sobre la copia del `PanelSettings`, antes del `OnEnable` del `UIDocument`), `UiTestHost` y el arnés. Si a alguno le falta, PlayMode y las capturas dibujan `□` donde va el ícono de tendencia del subtítulo (§4.6).
- **Dueños de Noto Emoji** (alineado con 02-tech-design §1.1 y QA-G3-15): el `.ttf` con su `OFL.txt` y el slot en ASSET_SLOTS son de WP-D0. El `FontAsset` lo crea el bootstrapper en B2, con `FontAssetPaths.EmojiFallback` dentro de `All` (9 → 10), así `GeneratedAssetGuard` también lo protege (QA-V2-07).
- **Texto del usuario tal cual**: en todo `Label` que muestre texto del usuario (nombre y descripción de la tarjeta, contenido del diálogo de eliminar) y en el texto de los campos van `enableRichText = false` **y** `parseEscapeSequences = false`. Así `<b>A</b>` no sale en negrita y `Serie\n2` (la barra y la `n` escritas a mano) no salta de línea (spec AC-RUT-10-03, -07; DN3-22; QA-G3-10). El subtítulo del paso sí usa texto enriquecido (§4.6), porque lo arma la app.

### 4.16 Kerning: la tabla versionada y nada más (R3-06)

**Decisión.** El kerning de la app es el de la tabla que ya está versionada en cada `FontAsset` (00-deuda-it2 §3, DD-02). Las 7 caras de texto (`FontAssetPaths.TextFaces`: Inter 400 a 700 y Barlow SC 600 a 800) pasan a **`getFontFeatures = false`**, un campo público de `FontAsset` que se guarda en una línea por archivo. Así TextCore no busca pares nuevos mientras dibuja. Los íconos (recortados sin GPOS, ASSET_SLOTS §4.1) y Noto Emoji quedan como los crea Unity: no cambian ninguna captura. Lo pide DN3-24.

**Por qué el kerning dependía del orden** (medido en el gate, §14.7):

- *Cómo se llenó la tabla.* Cada glifo que entra al atlas suma **sus pares como primer glifo**, contra toda la fuente. En Inter Regular hay 16 356 pares con 63 "primeros", que son los caracteres que se dibujaron en ese peso durante la It. 2, y 1 645 "segundos". SemiBold tiene 49 primeros, Bold 11, Barlow Bold 20, y Barlow SemiBold y ExtraBold, ninguno.
- *Por qué el orden importa.* En el IL de 6000.3.27f1, `TryAddGlyphToTexture` y `TryAddGlyphs` (los caminos que usa el texto al dibujar) **encolan** esa búsqueda si `m_GetFontFeatures` está encendido (`RegisterFontAssetForFontFeatureUpdate` o `RegisterFontAssetForKerningUpdate`; las dos colas se procesan después, en `UpdateFontFeaturesForFontAssetsInQueue`). El primer dibujo de un texto con un glifo nuevo sale sin esos pares, y un dibujo posterior los tiene. Por eso AUX-02 dejó de ser igual a CAP-01 cuando se probó la tabla vacía.
- *Qué cambia con el ajuste.* Con `getFontFeatures = false` no se encola nada. Un texto se ve igual en cualquier orden, en una corrida parcial (`capture-snapshots.ps1 -Filter`, la que usa WP-D para iterar), en el primer dibujo y en los siguientes. Vale también para el texto que escribe el usuario.

**Qué cuesta en la It. 3** (los textos de la spec, en el peso con que los dibuja §2.2, contra la tabla de ese peso):

| Cara | Textos | Con un par que no está en la tabla |
|---|---|---|
| Inter SemiBold (nombres 16; `Pasos (N)`, títulos de fila y botones 14; pie y mosaicos 12; títulos 20) | 65 | `Pasos (11)` / `Steps (11)`: `1)` +0,17 dp. El nombre de prueba `<b>A</b>`: `>A` −0,41 |
| Inter Regular (subtítulos, errores, rótulos y ayudas 12; resumen 13; vacío del editor, avisos y menús 14; campos, etiquetas y diálogo de eliminar 15) | 123 | Subtítulos `SPM +2/rep` y `Split -2s/rep`: `+2` y `-2` −0,40 dp. `-2` en un campo y en `Ej: -2 (más rápido)` / `e.g: -2 (faster)`: −0,51. Vacío del editor, `"Serie".`: `".` −1,15 |
| Inter Bold (`Serie` del encabezado, 13) | 2 | `ri` +0,06 dp |
| Barlow SC Bold (`×N`, 16) | 9 | ninguno |

- Son 12 de unos 200 textos y ningún par pasa de 1,15 dp. El único que apenas se nota es el punto después de la comilla en el vacío del editor. Con el ajuste de hoy, esos mismos pares salen o no según lo que se dibujó antes.
- Los cinco glifos que nombra R3-06 (`ñ`, `¿`, `¡`, `×`, `±`) no cambian ningún texto de la It. 3: Inter no tiene par distinto de cero para `¿E`, `±9` ni `4×`; Barlow no tiene ningún par que empiece con `×`, y `ñ` y `¡` no aparecen en los textos nuevos.
- Ningún ancho de §3 cambia: se midieron **sin** kerning, que es la cota de arriba.
- El legacy no sirve de referencia par por par: no fija familia (`lib/shared/theme.dart` sin `fontFamily`, `pubspec.yaml` sin `google_fonts`), dibuja con la fuente del sistema y su motor aplica todos los pares. Inter y Barlow son del sistema visual (ART_BIBLE §4). No es una diferencia de producto.

**Completar la tabla** (no en la It. 3; primera revisión en la It. 4a, con los títulos `display` y `headline` de 24 a 32 dp, donde un par como `Te` o `Wo` llega a 1-2,5 dp). Se hace a propósito, dentro de `GeneratedAssetGuard.AllowWrites()`, para un juego fijo de caracteres: los del catálogo es/en y de las bases de prueba, más las letras del español.

1. `fontAsset.TryAddCharacters(juego, out _, includeFontFeatures: true)`, que es público. En el IL llama directo a `UpdateFontFeaturesForNewlyAddedGlyphs` sin leer el campo, así que funciona también con `getFontFeatures = false`.
2. Volver el asset al estado versionado (0 glifos, 0 caracteres, atlas de 1 × 1; lo controla `GeneratedFontAssetsAreCommittedWithoutDynamicData`) **sin vaciar** `m_FontFeatureTable`. En 6000.3.27f1, los métodos que lo hacen son internos (`ClearCharacterAndGlyphTablesInternal`, `ClearAtlasTextures`), y `ClearFontAssetData`, que es público, borra también la tabla. El camino lo elige el Architect: `SerializedObject` sobre las tablas más `Texture2D.Reinitialize(1, 1)`, o reflexión fijada a la versión, con una prueba que falle si el método desaparece.
3. Tamaño, con la forma de TextCore (cada primero contra toda la fuente, unos 433 B por par en el `.asset`). Para los primeros que hoy les faltan a los textos de la It. 3: Regular 2 793 pares (≈ 1,2 MB), SemiBold 3 074 (≈ 1,3 MB) y Bold 1 535 (≈ 0,7 MB). Solo entre los glifos de la It. 3 serían 192 pares (≈ 83 KB), pero las consultas de `FontEngine` que los devuelven son internas.

Un `FontAsset` que se vuelve a crear nace sin tabla: con el ajuste, queda sin kerning hasta que se complete con este procedimiento, antes de versionarlo.

**Si el Architect no integra el ajuste**, vale la alternativa de QA: las capturas se comparan byte a byte solo entre corridas completas del mismo catálogo. Una corrida con `-Filter` se revisa con las sondas (±1 y ±3) y a ojo, nunca byte a byte.

---

## 5. Motion

### 5.0 Quién mueve qué

| Animación | Mecanismo | Duración y curva | Reducir movimiento |
|---|---|---|---|
| Pila (editor) | código (`PageTransition`) | entra 240, sale 200 (§5.1) | fundido 120 |
| Diálogo y velo | código (`DialogTransition`) | entra 200/220, sale 150 (§5.2) | fundido sin escala |
| Menú, ayuda | código | 120 / 90 (§5.3) | fundido |
| Aviso | código (`ToastTransition`) | 220 / 160; reemplazo con empujón 240 (§5.4) | fundido; sin empujón |
| Tarjetas: primera carga, alta, baja | código (`GrowMotion`, `CollapseMotion`) | §5.5 | fundido sin cambio de alto |
| Filas y bloques del editor | código | §5.6 | ídem |
| Resaltado de lo editado | código (`.rm-highlight`) | 0,08 → 0 en 600 `ease.move` | igual (es opacidad) |
| Colores de tipo (mosaico, vista previa, fila) | código | `dur.fast` 160 `ease.move` | igual |
| `×N` | código (`TabularNumber` + `DigitRoll`) | 180 | fundido 80 |
| Hojas de la serie | código | 160 `ease.move` | fundido |
| Etiqueta del campo | código | 160 `ease.move` | en el acto |
| Línea de error | código | 160 `ease.move` (alto y opacidad) | en el acto |
| Llave | código (`CriticalSpring` ω 22 en y) | ≈ 260 | en el acto |
| Desplazar hasta lo nuevo / el error, el foco por Tab o sobre el teclado | código (`CriticalSpring` ω 12 / 22) | ≈ 490 / 260 | en el acto |
| Indicador de carga | código | gracia 120, fundido 160; vuelta 900 | igual (indica espera) |
| *Hover* y presionado | USS (`Motion.uss`, §5.9) | los de la It. 2 | sin escala |

Como en la It. 2: todo lo de código pide 60 fps mientras corre, nada anima antes del primer cuadro presentado y ninguna propiedad animada por código tiene transición de USS.

**Lo que sale no recibe toques** (QA-G3-01; spec AC-RUT-09-03 y AC-APP-03-01, -03, -04). Desde el cuadro 0 de su salida, la página, el diálogo y el menú pasan a `pickingMode = Ignore` y sueltan el foco, y la ayuda emergente no recibe toques nunca (§4.4). Un segundo toque durante los 200, 150 o 90 ms de salida ya no llega a un `Guardar` o un `Eliminar` que se está yendo. Es lo que hace Flutter con la ruta que sale (`routes.dart:1154-1156`, `:1220-1222`). El aspecto de la salida no cambia: es el de §5.1 a §5.3.

### 5.1 Pila: abrir y cerrar el editor

| t (ms) | Abrir (`Push`) | Cerrar (`Pop`) |
|---|---|---|
| 0 | la página entra con x +24, opacidad 0 | `CoversShellChanged(false)`: pestañas y barra vuelven en x −24 y oscurecido 0,08; el `+` vuelve a ser obstáculo y la llave empieza a subir (§5.8) |
| página | x +24 → 0 en 0-240 `ease.enter`; opacidad 0 → 1 en **0-160** `ease.enter` (llega a opaca antes de terminar de moverse: dos pantallas superpuestas a media opacidad se ven sucias) | x 0 → +24 en 0-200 `ease.exit`; opacidad 1 → 0 en **40-200** `ease.exit` |
| lo de atrás | x 0 → −24 y `.rm-page-stack__dim` 0 → 0,08 en 0-240 `ease.enter` (se mueve "junto" con la página) | x −24 → 0 y oscurecido 0,08 → 0 en 0-200 `ease.enter` (llega y frena) |
| fin | a los 240, `CoversShellChanged(true)`: pestañas y barra en `display: none`; el `+` deja de ser obstáculo y la llave baja | a los 200 la página sale del árbol y se dispone |

La cinta y la llave no se mueven con la pila (capa del simulador). Un "Atrás" a mitad de la entrada arranca el `Pop` desde los valores actuales. Reducir movimiento: la página funde 0 → 1 en 120 (`ease.linear`) y el oscurecido llega a 0,08 en el mismo tiempo, sin desplazamientos; al revés para cerrar.

### 5.2 Diálogos y velo

- **Entrada**: velo 0 → 0,6 en 0-200 `ease.move`; diálogo opacidad 0 → 1 y escala **0,96 → 1** (origen al centro) en 0-220 `ease.enter`.
- **Salida** (`Cancelar`, `Confirmar`, velo, Escape, "Atrás"): diálogo opacidad → 0 y escala → **0,98** en 0-150 `ease.exit`; velo → 0 en 0-150 `ease.exit`. El resultado se aplica en el cuadro 0 (principio 3): la fila nueva empieza a crecer **debajo** del diálogo que se va, y se ve la continuidad.
- **Error en un campo**: la caja cambia de color en el acto (clase); la línea de error abre su alto de 0 al de su texto y funde 0 → 1 en 160 `ease.move`; al corregir el campo, al revés. Sin sacudidas (ANIMATION_BIBLE §2.12).
- Reducir movimiento: velo igual; diálogo solo opacidad (220 / 150).

### 5.3 Menú y ayuda

- Menú: opacidad 0 → 1 y escala 0,96 → 1 en 120 `ease.enter`, con **origen en la esquina que toca el ancla** (arriba a la derecha bajo el `⋮`; arriba a la izquierda en el puntero); salida, opacidad → 0 en 90 `ease.exit`. Al elegir `Eliminar`, el menú sale mientras entra el diálogo (se superponen 90 ms).
- Ayuda: opacidad 0 → 1 en 120 `ease.enter`; salida 90 `ease.exit` (después de los 0,1 s de contrato).
- Reducir movimiento: solo opacidad.

### 5.4 Aviso

- Entra: y +16 → 0 y opacidad 0 → 1 en 220 `ease.enter`. Sale a los 4 s: opacidad → 0 e y 0 → +8 en 160 `ease.exit`.
- **Reemplazo** (DV-14): con el **mismo** texto, empujón de escala 1 → 1,02 (0-90 `ease.enter`) → 1 (90-240 `ease.move`), origen abajo al centro: cada toque en `Guardar` se nota aunque el texto no cambie (VID-03). Con **otro** texto, además, el texto viejo funde en 0-80 y el nuevo entra en 80-200.
- Si un obstáculo cambia mientras se ve (la llave que sube), el aviso sigue su nueva posición con `CriticalSpring` ω 22.
- Reducir movimiento: solo opacidad (220 / 160); sin empujón; el texto cambia en el acto.

### 5.5 Lista de rutinas

| Caso | Motion |
|---|---|
| Primera carga **con la pestaña a la vista** | el indicador funde → 0 en 120 `ease.exit`; tarjeta `i` (hasta 8; el resto con la octava) entra en `35·i` → `+240`: opacidad 0 → 1 e y +12 → 0, `ease.enter`. El vacío y el error entran igual en 240 (y +8 → 0) |
| Lista lista **antes** de mostrar la pestaña (lo normal, AC-APP-01-09) | sin animación propia: llega con la transición de la pestaña (ANIMATION_BIBLE §2.1). Dos movimientos encimados se verían como un tirón |
| Rutina nueva (al volver del editor) | al terminar el `Pop` (t = 200), la tarjeta 0 crece: alto **y margen inferior** 0 → N con `CriticalSpring` ω 12 (`spring.smooth`); opacidad 0 → 1 en 60-260 `ease.enter`. Las de abajo bajan con el alto. Si la lista está desplazada, la posición se conserva (AC-RUT-10-09) |
| Rutina editada | los textos y la mini-barra ya cambiaron (bajo el editor, con la lista en `display: none`); al terminar el `Pop`, **resaltado**: `.rm-highlight` 0,08 → 0 en 600 `ease.move`. **Deseable** (P2): la mini-barra interpola la posición y el ancho de sus tramos en 240 `ease.move` y los nuevos funden |
| Rutina borrada | tarjeta opacidad 1 → 0 en 0-120 `ease.exit`; alto y margen → 0 en **80-280** `ease.move`; las de abajo suben sin salto (VID-02). Si era la última, el vacío entra al terminar (280) |

Regla general: **lo nuevo crece, lo cambiado se ilumina, lo borrado se desvanece y colapsa**. El margen se anima junto con el alto (si no, quedan 10 dp que saltan al final). Reducir movimiento: sin escalonado (todo funde en 160 junto), sin crecer ni colapsar (fundido de 160 / 120 y el reacomodo en el acto), el resaltado igual.

### 5.6 Editor

| Acción | Motion |
|---|---|
| `+ Paso` → `Confirmar` (paso suelto al final) | en el cuadro 0 (con el diálogo saliendo), la fila nueva crece: alto y márgenes 0 → N con ω 12, opacidad 0 → 1 en 60-260 `ease.enter`. Si queda fuera de la vista, el contenido se desplaza hasta mostrarla con 16 dp de aire (ω 12, arranca con el crecimiento). Si era el primer paso, `#editor-empty` funde → 0 en 120 |
| `Serie` | el bloque entero crece con el mismo resorte y sus piezas funden en escalera: encabezado 0-200, fila 1 40-240, fila 2 80-280, pie y hojas 120-320 (`ease.enter`). El bloque se "despliega" de arriba abajo. Desplazamiento hasta mostrarlo |
| `+ Agregar paso a serie` → `Confirmar` | la fila crece dentro del bloque; el bloque se estira con ella (mismo resorte) |
| Lápiz → `Confirmar` | la fila se **resalta** (0,08 → 0 en 600 `ease.move`, desde el cuadro 0); si cambió el tipo, borde, insignia, ícono y título cruzan de color en 160 `ease.move`; el subtítulo cambia en el acto. **Deseable** (P2): si el subtítulo pasa de una a dos líneas, el alto va del viejo al nuevo con ω 12 |
| Cruz | fila opacidad → 0 en 0-120 `ease.exit`; alto y márgenes → 0 en 80-280 `ease.move`. Si era la última de su serie, colapsa **el bloque entero** con los mismos tiempos (AC-RUT-14-10). Si era el último paso, `#editor-empty` entra en 240 al terminar |
| Tacho | el bloque entero colapsa como una fila |
| `×N` → `OK` | el número rueda (`DigitRoll` 180, hacia arriba si crece); el ancho de la cápsula interpola en los mismos 180 ms; las hojas aparecen o se van (alto 0 ↔ 3 y opacidad en 160 `ease.move`) |
| `Pasos (N)` | en el acto |

Interrupciones: borrar algo que está creciendo colapsa desde su alto actual; dos altas seguidas (dos toques en `Serie`) crecen por separado, cada una con su resorte. VID-01 verifica que la caja del contenido nunca cambie más que lo que da el resorte entre dos cuadros. Reducir movimiento: como en §5.5 (fundidos sin cambio de alto, desplazamiento en el acto, `×N` con fundido de 80, hojas en el acto; el resaltado y los colores igual).

### 5.7 Diálogo de paso

- **Tipo**: el mosaico elegido cruza fondo y borde en 160 `ease.move` (y el anterior al revés); la vista previa cambia sus colores en el mismo tiempo. Al tocar, el mosaico escala 0,97 (USS, §5.9).
- **Modo**: el segmento elegido cruza su fondo en 160; el campo que se va desaparece en el acto y el que llega funde 0 → 1 en 120 `ease.enter` (tienen el mismo alto: nada se mueve).
- **Vista previa**: el texto en el acto con cada tecla (animarlo atrasaría lo que se lee); los colores en 160.
- **Etiqueta del campo**: reposo ↔ flotando con traslado y escala en 160 `ease.move`, por código.
- Reducir movimiento: colores igual (son fundidos), etiqueta y campo que llega en el acto.

### 5.8 Llave y desplazamientos

- La llave va a su nueva y con `CriticalSpring` ω 22 desde su posición y velocidad actuales: al cambiar a Rutinas o salir de ella (empieza con el cambio de pestaña), al terminar la entrada del editor (baja) y al empezar su salida (sube). Se puede tocar mientras se mueve. Reducir movimiento: en el acto.
- Desplazar hasta un elemento (lo nuevo, el primer error, el campo sobre el teclado): el desplazamiento del `ScrollView` sigue un resorte (ω 12 para lo nuevo; ω 22 para el error y el teclado, que responden a una acción inmediata). Reducir movimiento: en el acto.
- **Foco por Tab** (QA-G3-11): un `ScrollView` de UI Toolkit no lleva a la vista al hijo que recibe el foco. En `#routines-list`, `#editor-scroll` y `.rm-dialog__content`, cuando el foco cae en un elemento que no se ve entero, el contenido se desplaza hasta mostrarlo con 16 dp de aire, con ω 22, como el error (es la respuesta a una tecla). Si ya se ve entero, no se mueve nada. Reducir movimiento: en el acto. Con 60 filas en el editor, o con el diálogo de paso a 640 × 360, el foco nunca queda fuera de la pantalla (AC-APP-05-07, AC-RUT-13-13, -13-15).

### 5.9 `Motion.uss` (WP-D; siempre bajo `:hover`)

Se suman a las reglas de la It. 2, con los mismos tiempos (capa hacia el estado `dur.micro` `ease.enter`, vuelta `dur.fast` `ease.exit`; escala hacia 90, vuelta 240 `ease-out-cubic`):

| Selector | Qué |
|---|---|
| `.rm-routine-card` | capa 4 / 8 %, **sin escala** |
| `.rm-fab` | capa; presionado fondo `accent.primaryPressed` (hacia 90, vuelta 160) y escala 0,94 |
| `.rm-chip--repeat .rm-chip__pill` | capa; escala 0,97 de la cápsula |
| `.rm-segmented--tiles .rm-segmented__option` | capa; escala 0,97 (el fondo y el borde los anima el código: sin transición) |
| `.rm-segmented__option` (modo) | capa; sin escala |
| `.rm-menu__item` | capa (la del peligro, oscura); sin escala |
| `.rm-field__box` | `border-color` hacia el *hover* en 90 `ease-out-cubic` y **vuelta en el acto** [WP-D, §16.2: una transición en la regla base correría también al perder el foco, sin puntero]; el foco y el error cambian en el acto y no dependen de `:hover` |

Con `.rm-reduce-motion`, ninguna escala.

### 5.10 Reducir movimiento (resumen)

Pila, diálogos, menús, ayudas y avisos → solo opacidad; filas, bloques y tarjetas → fundido sin cambio de alto y el reacomodo en el acto; sin escalonado; llave, desplazamientos y etiqueta del campo → en el acto; `×N` → fundido de 80. No cambian: los datos y cuándo cambian, el resaltado, los fundidos de color, el indicador de carga y su gracia. Una captura fija con y sin reducir movimiento es **idéntica**.

### 5.11 Constantes para `MotionSpecs` (WP-A)

Las duraciones y curvas que ya son tokens (`dur.*`, `ease.*`, `shift.*`) se toman de `Tokens`. Nombres sugeridos:

| Constante | Valor | Constante | Valor |
|---|---|---|---|
| `PageInMs` / `PageInFadeMs` | 240 / 160 | `PageOutMs` / `PageOutFadeStartMs` | 200 / 40 |
| `PageShiftDp` | 24 (`shift.xl`) | `PageDimOpacity` / `ReducedPageFadeMs` | 0,08 / 120 |
| `DialogScrimOpacity` / `DialogScrimInMs` | 0,6 / 200 | `DialogInMs` / `DialogInScaleFrom` | 220 / 0,96 |
| `DialogOutMs` / `DialogOutScaleTo` | 150 / 0,98 | `MenuInMs` / `MenuInScaleFrom` / `MenuOutMs` | 120 / 0,96 / 90 |
| `TooltipInMs` / `TooltipOutMs` | 120 / 90 | `ToastInMs` / `ToastOutMs` / `ToastOutShiftDp` | 220 / 160 / 8 |
| `ToastBumpScale` / `ToastBumpPeakMs` / `ToastBumpEndMs` | 1,02 / 90 / 240 | `ToastTextOutMs` / `ToastTextInEndMs` / `BottomObstacleGapDp` | 80 / 200 / 8 |
| `ListStaggerMs` / `ListStaggerMax` / `ListItemInMs` | 35 / 8 / 240 | `EmptyInShiftDp` | 8 (`shift.s`) |
| `LoadingGraceMs` | 120 | `GrowOmega` | `CriticalSpring.SmoothOmega` (12) |
| `GrowFadeStartMs` / `GrowFadeEndMs` | 60 / 260 | `CollapseFadeMs` / `CollapseStartMs` / `CollapseEndMs` | 120 / 80 / 280 |
| `SeriesStaggerMs` | 40 | `HighlightPeak` / `HighlightMs` | 0,08 / 600 |
| `StackLayerMs` | 160 | `FieldLabelScale` | 0,8 |
| `KeyOmega` / `KeyboardOmega` | 22 / 22 | `ScrollToNewOmega` / `ScrollMarginDp` | 12 / 16 |
| `ModeSwapInMs` | 120 | `SaveSpinnerDelayMs` (P2) | 300 |
| `ScrollToFocusOmega` | 22 (primer error y foco por Tab, §5.8) | | |

Y de geometría (en `StructureBarView` o junto a `RoutineStructure`): `SegmentHeightDp` 4, `SegmentRadiusDp` 2, `SegmentGapDp` 2, `SegmentMaxWidthDp` 24, `SegmentMinWidthDp` 2, `OverflowFadeSegments` 6.

---

## 6. Rendimiento

- Elementos: tarjeta ≈ 15 (contenedor, capa, resaltado, ícono, columna, 3 textos, mini-barra, `⋮` de 5, anillo y halo); fila de paso ≈ 17; encabezado de serie ≈ 14, pie ≈ 7, cuerpo 1, hojas 3. 54 tarjetas ≈ 810 y un editor de 60 filas ≈ 1 100: lo mide `PRF` (R3-09).
- Mini-barra: un solo elemento; como mucho `capacidad` rectángulos redondeados (64 en TV, 281 en W1280), y la malla se rearma solo cuando cambian la rutina o el ancho.
- Ninguna textura, material ni *shader* nuevos. `overflow: hidden` solo en lo que está creciendo o colapsando, y solo mientras dura (como `#hrm-body` en la It. 2).
- Crecer y colapsar cambian el alto: es una animación de *layout*, la excepción que ANIMATION_BIBLE §0.1.5 ya admite para "nada salta" (el `HeightTween` de la It. 2). Dura ≤ 490 ms y toca una sola lista. El resto (pila, diálogo, menú, aviso, llave, resaltado) es `translate`, `scale` y opacidad.

---

## 7. Capturas: qué tiene que verse

### 7.1 Reglas de toma (además de las de la It. 2 y 02-tech-design §5.1)

1. Ningún `GrowMotion`, `CollapseMotion`, resaltado, cruce de color o resorte (llave, desplazamiento, aviso) en curso.
2. El indicador de carga cuenta como el *spinner* de la It. 2 (puede girar). CAP-02 se toma después de la gracia y del fundido: 120 + 160 ms del reloj de prueba, y lo mismo espera `RLT.LoadingShowsOnlyTheIndicator` (QA-G3-02).
3. La vista previa del diálogo se toma ya rearmada (después de `Type`).
4. Las sondas de color son de la tubería `GammaRt`; tolerancia ±1 por canal en fondos y ±3 en trazos de texto, como en la It. 2.
5. **Kerning** (§4.16): con `getFontFeatures = false` (DN3-24), una toma se puede comparar contra una corrida con `-Filter` con ±1 por canal: el kerning ya no depende del orden, pero el atlas dinámico acomoda los glifos según lo que se dibujó antes y el borde de un glifo puede variar en 1 (medido en WP-D, §16.5). Byte a byte, solo entre corridas completas del mismo catálogo.

### 7.2 Por captura (aspecto; QA verifica además lo funcional de la spec §7.3)

"Punto" = píxel de fondo lejos de textos e íconos. Las sondas se suman a `RoutinesCatalog` con `WithProbe(…, aspect: true)` (DN3-23).

| ID | Qué miro | Sondas |
|---|---|---|
| CAP-01 | Vacío centrado entre las dos barras, el ícono apenas visible, el título un escalón más claro que la ayuda; el `+` cian entero; la llave **16 dp sobre el `+`**, con los bordes derechos alineados | fondo `#0D1B2A`; ícono (trazo) `#2A3644`; título (trazo) `#B6BBBF`; ayuda (trazo) `#90969D`; `+` `#00B4D8`; llave `#1E3550`; `#sim-open.yMax + 16 = #routines-add.yMin` ± 1 y mismo `xMax` ± 1 |
| CAP-02 | Solo el indicador de 32 dp girando, centrado entre las barras, ya entero (después de la gracia y del fundido, §7.1); nada más en el cuerpo | indicador (trazo) `#00B4D8` |
| CAP-03 | Ícono rojo, texto gris claro, `Reintentar` tonal, centrados; sin indicador | ícono (trazo) `#FF7272`; tonal `#0B2D3F` |
| CAP-04 | Cuatro tarjetas con aire de 10; chip tonal del ícono; nombre blanco; en `Pirámide 500` la descripción gris claro arriba y el resumen más tenue debajo; mini-barras alineadas a la izquierda (8, 14 a todo el ancho, 1 corta, 7); sin barra de desplazamiento | tarjeta `#1A2E45`; chip del ícono `#173E57`; tramos (trazo) `#FFD166` `#EF476F` `#06D6A0` `#118AB2` (±2) en los tramos 1, 2, 3 y 8 de `Escalera SPM`; ancho del tramo 24 ± 0,5 en `Escalera SPM` y 16,4 ± 0,5 en `Pirámide 500` |
| CAP-05 | Lo mismo en inglés; ninguna tarjeta cambia de alto salvo por el texto | como CAP-04 |
| CAP-06 | Al final del desplazamiento, la última tarjeta termina 16 dp sobre la llave; la llave sobre el `+`; nada cortado | `Audit` sin desbordes |
| CAP-07 | Menú de 140 dp con borde fino, **debajo del `⋮` y con su borde derecho alineado**; `Eliminar` rojo claro; la cinta encima | menú `#1E3550`; `Eliminar` (trazo) `#FF7272` |
| CAP-08 | Velo oscuro parejo; diálogo centrado de 331 con borde fino; `Eliminar` rojo con texto blanco; la cinta y la llave **sin oscurecer** | velo sobre fondo `#050B11` y sobre tarjeta `#0A121C`; diálogo `#1E3550`; `Eliminar` `#D32F2F`; cinta en (27; 27) `#FF5722`; llave `#1E3550` |
| CAP-09 | Barra con la flecha bajo la cinta y el título a 72; campos con borde visible y la etiqueta adentro; `Pasos (0)` con los dos botones; el vacío centrado en lo que queda; sin barra de pestañas; llave a 16 sobre el alto conservado de la barra (80 desde abajo) | caja del campo `#08131F`; borde (trazo) `#6B7179` (±3); vacío (trazo) `#90969D` |
| CAP-10 | Línea de tiempo: borde, insignia y título de cada fila en su color; la serie con riel continuo, encabezado más marcado que el cuerpo y el pie; cápsula `×3` clara; **dos hojas** bajo el pie; tendencia celeste en la línea; lápiz gris y cruz roja en la columna de 28 dp; enfriamiento después del pie | insignias `#484F4C` `#45334D` `#165057` `#18405B`; títulos (trazo) `#FFD166` `#FF5D7F` `#06D6A0` `#37A2CB` (±3); encabezado `#0B263B`; cuerpo y pie `#0C2132`; riel (a 1,5 dp del borde, en el cuerpo) `#0077B6`; cápsula `#09425A`; `×3` (trazo) `#38CBEF` (±3); hoja 1 `#0A2D46`; hoja 2 `#0B263B`; tendencia (trazo) `#4CC9F0` (±3) |
| CAP-11 | Lo mismo a lo ancho; frente a `image-04.png`, todo en el mismo lugar (el pie centrado, el tacho a la derecha) | como CAP-10 |
| CAP-12a/b | Los once subtítulos legibles, los largos en dos líneas alineadas a la izquierda, el `0s` y el `?` del mismo color que el resto; serie `×1` sin hojas y `×2` con una | `×1`: sin `.rm-series__stack-layer` visible; `×2`: una |
| CAP-13 | Dos bloques separados por **16 dp** (8 + 8), cada uno con su riel, su cápsula y sus hojas (`×4` dos, `×2` una); la descripción en dos líneas | distancia pie ↔ encabezado 16 ± 1 |
| CAP-14 | Mosaicos 2 × 2 con `Trabajo` elegido (fondo rosado y borde rosa), `Por tiempo` en azul; la vista previa fija bajo el título; campos con borde visible; ningún rótulo cortado | mosaico elegido `#483956`; segmento elegido `#0077B6`; vista previa: tarjeta `#1A2E45` e insignia `#45334D` |
| CAP-15a/b | a) la vista previa con la tendencia celeste; b) desplazado, las ayudas `Ej: 5` y `Ej: -2 (más rápido)` más tenues que las etiquetas, que están arriba; el área desplazable con sus dos líneas finas | ayuda (trazo) `#8D9298` (±3) |
| CAP-16 | `Por distancia` en azul; `Metros` en el lugar de Min/Seg, con el mismo alto | — |
| CAP-17 | Diálogo de 331 con un solo campo; el borde cian de 2 dp del foco y la etiqueta cian | borde con foco (trazo) `#00B4D8` |
| CAP-18, CAP-19, CAP-28 | Aviso de borde a borde (16), abajo, entero, con la llave por encima de su extremo derecho sin tocarlo | aviso `#1E3550`; `#toast.yMin ≥ #sim-open.yMax + 8` (coordenadas de pantalla: el aviso queda debajo de la llave) |
| CAP-20a/b | La caja en rojo de 2 dp y el texto rojo claro debajo; en b) debajo de la progresión | borde (trazo) `#FF5252`; texto (trazo) `#FF7272` (±3) |
| CAP-21 | `Máximo 999` debajo del campo en rojo | como CAP-20 |
| CAP-22 | En 640 × 360 todo en un desplazamiento; la barra superior entera | `Audit` sin desbordes |
| CAP-23 | Diálogo compacto (rellenos de 16), la vista previa **dentro** del desplazamiento, mosaicos en una fila de 4; título y botones fijos | `.rm-dialog--compact` presente |
| CAP-24a/b | Inglés: mismos anchos, nada cortado | — |
| CAP-25a/b | Letra 1,3×: tarjetas más altas; subtítulos en dos líneas; mosaicos y botones sin cortar | `Audit` sin desbordes |
| CAP-26 | Menú denso (ítems de 36) en el puntero; el nombre seleccionado en cian oscuro | selección `#054B60` |
| CAP-27 | Barra de título oscura del sistema; el resto como CAP-04 sin cinta ni llave | — |

### 7.3 Demo y videos

| ID | Qué miro |
|---|---|
| DEMO-01 a DEMO-12 | Lo de CAP-01, -09, -14 y -10 a lo ancho. DEMO-03: el mosaico `Calentamiento` elegido (fondo `#4B5454`) y la vista previa amarilla; DEMO-05: la vista previa fija con la tendencia mientras el contenido está desplazado al final; DEMO-07 y DEMO-09: la mini-barra de 8 tramos (8 × 24 + 14 = 206 dp) y la de 10 (10 × 24 + 18 = 258 dp), las dos de 24 dp por tramo y alineadas a la izquierda en una columna de 1 124; DEMO-08: `×4` con dos hojas; DEMO-11: como CAP-08 |
| VID-01 | El diálogo crece 0,96 → 1 sobre el velo y sale en 150; la fila nueva se abre desde 0 con el resorte y funde; la serie se despliega de arriba abajo (encabezado, filas, pie, hojas); la cruz desvanece y cierra el hueco; el tacho colapsa el bloque. **Ningún salto de alto**, tampoco de margen al final |
| VID-02 | El editor entra 24 dp desde la derecha y ya es opaco a los 160; la lista se corre 24 a la izquierda y se oscurece; al volver, al revés en 200; el menú crece desde su esquina; la tarjeta borrada se desvanece y las de abajo suben sin salto |
| VID-03 | Un solo aviso que entra subiendo y en cada toque da un empujón de 1,02; se va a los 4 s del último, bajando 8 dp |

---

## 8. Contraste verificado

### 8.1 Pares nuevos de la It. 3 (para `ContrastTests.ItThreeDesignerPairs`)

Fondos como pila de capas (`card|work@20`, `bg.app|seed@12|accent@20|white@4`…), como en la It. 2.

| # | Frente | Fondo | Umbral | Valor |
|---|---|---|---|---|
| R1 | títulos de tipo `step.*.hud` (trabajo · descanso · calentamiento · enfriamiento) | `surface.card` | 4,5 | 4,685 · 7,322 · 9,577 · 4,730 |
| R2 | íconos de tipo `.hud` | su insignia (tarjeta + tipo @20 %) | 3 | 3,889 · 4,824 · 5,863 · 3,723 |
| R3 | tramos y bordes base (trabajo · descanso · calentamiento · enfriamiento) | `surface.card` | 3 | 3,815 · 7,322 · 9,577 · 3,487 |
| R4 | `metric.distance` (tendencia) · `text.secondary` (lápiz, con *hover*) · `state.dangerText` (cruz, con *hover*) | tarjeta | 3 | 7,181 · 6,893 · 4,616 |
| R5 | `brand.seed` (riel) | `bg.app` · cuerpo (`bg.app|seed@6`) | 3 | 3,573 · 3,384 |
| R6 | `accent.primary` (`Serie` · botón del pie con *hover*) | encabezado · pie con *hover* | 4,5 | 6,286 · 6,003 |
| R7 | `accent.onTonal` (`×N`) | cápsula · cápsula con *hover* | 4,5 | 5,620 · 5,030 |
| R8 | `state.dangerText` (tacho con *hover*) | encabezado + 4 % | 3 | 5,217 |
| R9 | `text.secondary` (descripción, con *hover* de la tarjeta) · `text.tertiary` (resumen, con *hover*) | tarjeta + 4 % | 4,5 | 6,893 · 4,785 |
| R10 | `accent.primary` (ícono) | chip tonal sobre tarjeta | 3 | 4,572 |
| R11 | `text.onAccent` (ícono `+`) · `accent.primary` (`+` contra tarjeta) | `accent.primary` · tarjeta | 4,5 · 3 | 7,641 · 5,603 |
| R12 | `text.secondary` · `text.tertiary` · `state.dangerText` (vacío y error) | `bg.app` | 4,5 · 4,5 · 3 | 8,967 · 5,829 · 6,545 |
| R13 | `accent.onTonal` (`Reintentar`, con *hover*) | `bg.app|accent@12|white@4` | 4,5 | 6,660 |
| R14 | `text.secondary` (contenido) · `text.tertiary` (rótulos) · `accent.primary` (`Cancelar` con *hover*) | `surface.overlay` (+ 4 %) | 4,5 | 6,984 · 4,829 · 4,501 |
| R15 | blanco (`Eliminar` con *hover*) | `state.dangerFill` + 4 % | 4,5 | 4,740 |
| R16 | blanco · `text.secondary` · `accent.primary` · `state.dangerText` · `text.tertiary` (texto, etiqueta, etiqueta con foco, en error, ayuda) | `bg.sunken` | 4,5 | 18,696 · 9,401 · 7,585 · 7,035 · 5,992 |
| R17 | `state.dangerText` (error del campo) | `surface.overlay` · `bg.app` | 4,5 | 4,703 · 6,545 |
| R18 | `border.input` · `accent.primary` (2 dp) · `state.danger` (2 dp) | `bg.sunken` | 3 | 3,807 · 7,585 · 5,859 |
| R19 | `border.input` | `surface.overlay` · `bg.app` | 3 | 3,355 · 3,784 |
| R20 | blanco | selección (`bg.sunken|accent@35`) | 4,5 | 9,586 |
| R21 | `text.secondary` (mosaico no elegido, con *hover*) · blanco (elegido: trabajo · descanso · calentamiento · enfriamiento) | `surface.overlay` (+ 4 %) · `overlay|tipo@20` | 4,5 | 6,361 · 10,572 · 8,345 · 7,770 · 9,970 |
| R22 | íconos `.hud` en mosaicos (no elegido; elegido) | `surface.overlay`; `overlay|tipo@20` | 3 | ≥ 4,240; ≥ 3,415 |
| R23 | borde base del mosaico elegido | `surface.overlay` | 3 | ≥ 3,155 (enfriamiento) |
| R24 | blanco (segmento elegido) · `text.secondary` (no elegido con *hover*) | `brand.seed` · overlay + 4 % | 4,5 | 4,868 · 6,361 |
| R25 | `state.dangerText` (ítem de peligro, con *hover* oscuro) | `surface.overlay|black@12` | 4,5 | 5,147 |
| R26 | blanco (ítem con *hover*; aviso; ayuda) | overlay + 4 % · overlay | 7 | 11,093 · 12,498 |
| R27 | `focus.ring` | overlay · tarjeta · `bg.app` · encabezado de serie | 3 | 5,071 · 5,603 · 7,057 · 6,286 |

### 8.2 Excepciones (no se testean como umbral)

- **Lo que el legacy trae y se corrige** (no son excepciones: se cambian, §2.3, §4.7): `step.work` como texto 3,815; `step.cooldown` como texto 3,487 y como ícono sobre su insignia 2,744; `#00B4D8` sobre la cápsula 4,370; `state.dangerText` sobre el menú con capa blanca 4,174.
- **Presionado** (dura lo que el toque): resumen sobre la tarjeta presionada 4,420; `×N` sobre la cápsula presionada 4,487; tramo del enfriamiento sobre la tarjeta presionada 2,732.
- **Decorativos**: ícono del vacío al 12 % (1,421); capas `brand.seed` al 6, 12 y 20 % y hojas; pista vacía de la mini-barra; borde del mosaico no elegido y del menú; velo y oscurecido.

---

## 9. Pedidos al Architect

Ninguno cambia el comportamiento del producto. Los que tocan el contrato de 02-tech-design §7.5 se marcan **[contrato]** y conviene resolverlos antes de B1 (los de la lista) o de B2 (los del editor).

| ID | Para | Qué | Por qué |
|---|---|---|---|
| DN3-01 | WP-D0 (Designer) + contrato | Tokens de §2.1 (`border.input`, `text.selection`, `titleXs`, `headerS`, `buttonS`) junto con `overlay.scrim`: 127 → **133**; `Typography.uss` con `.rm-type-title-xs`, `-header-s`, `-button-s`. Actualizar los conteos de los tests de tokens y la fila de WP-D0 de §7.1 [contrato] | El contrato pide pesos que la escala no tiene; el borde del campo no llega a 3:1 |
| DN3-02 | WP-B [contrato] | La tabla de clases y colores de §2.2 en lugar de la de §7.5 (descripción `text.secondary`, resumen `text.tertiary`, título de fila `title-xs` en `.hud`, `Pasos (N)` `title-xs`, `Serie` `header-s`, pie `button-s`, etiqueta del campo `body` con escala, ayuda emergente `body-xs`) | §2.2, §8 |
| DN3-03 | WP-B [contrato] | Fila `.rm-step-row__title` de §7.5: "color del tipo" → **`step.<tipo>.hud`** para los cuatro; ícono de la insignia también `.hud`; borde, fondo de insignia y mini-barra siguen en el base. Resuelve DT-19 | §2.3 (3,815 → 4,685; 2,744 → 3,723) |
| DN3-04 | WP-B [contrato] | `.rm-chip--repeat`: caja táctil transparente ≥ 48 × 48 con un hijo `.rm-chip__pill` (28, radio 14, `min-width` 40, relleno 0 / 10) que lleva el fondo `accent @20 %` por código, la capa de estado, el número en `accent.onTonal` y el anillo; sin el borde ni el `min-height` 32 de `.rm-chip` | §4.7; con el fondo en la caja de 48 el encabezado sería una franja cian |
| DN3-05 | WP-B [contrato] | `.rm-series__steps` con borde izquierdo 3 `--rm-brand-seed` y fondo `brand.seed @6 %` por código; `.rm-step-row--in-series` con `margin-left` **13** (sangría visible 16); `.rm-series__stack` con dos `.rm-series__stack-layer` (geometría y colores de §4.7), visibles según N, con la transición de §5.6 | §4.7 |
| DN3-06 | WP-B | Tarjeta: `.rm-routine-card__icon` como caja de 36 con `--rm-accent-tonal-bg` y glifo 22; `⋮` con `margin-top` −14; `.rm-highlight` (filas y tarjetas: capa absoluta blanca, `pickingMode Ignore`, opacidad por código). El resaltado no usa `.rm-button__state` porque esa capa la maneja el USS del *hover* y un estilo en línea la pisaría | §4.2, §5.5 |
| DN3-07 | WP-A / WP-B | `StructureBarView` con las reglas de §4.3 (tope 24, mínimo 2, izquierda, 6 tramos que se apagan, pista vacía) y `SegmentWidth` y `Overflow` para los tests. Las constantes, en §5.11. La capacidad se calcula con el ancho resuelto, en `GeometryChangedEvent`, y los tests esperan el *layout* antes de leer `SegmentCount` (gate de QA §7) | §8.3.2 |
| DN3-08 | WP-B | `SegmentedControl` con la variante `.rm-segmented--tiles` (ícono arriba, 2 o 4 columnas según el contenido ≥ 400, color por opción por código) para el tipo; la variante por defecto para tiempo / distancia (§4.11) | DV-09 con los cuatro nombres visibles a 1,3× |
| DN3-09 | WP-B [contrato] | `RmTextField`: `.rm-field__label` con `.rm-type-body` y escala 0,8 por código; clase `.rm-field--floating` (foco, texto **o ayuda**); animación de la etiqueta y de la línea de error por código (§5.2, §5.7); color de la ayuda en `.unity-base-text-field__input--placeholder`; sin `.rm-focus-ring`; error de par con las dos cajas en error | §4.8; DT-02 |
| DN3-10 | WP-B | `ModalDialog`: ancho máximo por diálogo (400 / 560, mínimo 280); `.rm-dialog--compact` con la ventana < 480 dp de alto; `.rm-dialog__pinned` para la vista previa si el alto máximo ≥ 560 (si no, primer hijo del contenido); `.rm-dialog__content--scrolls` cuando el contenido desborda; al `Confirmar` con errores, foco y desplazamiento al primer campo con error | §4.9, §4.11 |
| DN3-11 | WP-B | `PopupMenu`: posición de §4.4, `.rm-menu--dense` en el menú de texto, la capa de estado del ítem de peligro con `--rm-overlay-scrim` (para que WP-D le ponga 12 / 24 %), anillo interior | §4.4 (4,174 → 5,147) |
| DN3-12 | WP-B | `Tooltip`: posición de §4.4 (8 debajo, arriba si no entra, ancho máximo 240) y `pickingMode = Ignore` (QA-G3-01). Página, diálogo y menú, con `Ignore` y sin foco desde el cuadro 0 de su salida (§5.0) | §4.4, §5.0 |
| DN3-13 | WP-B | `ToastLayer`: un obstáculo cuenta si **se cruza** con el rectángulo del aviso agrandado 8 dp (no si solo cae en su ancho); ancho de §4.12; empujón y cruce de texto al reemplazar; posición con resorte si cambia un obstáculo | §4.12: en el editor el aviso queda abajo, no flotando a 128 |
| DN3-14 | WP-A / WP-B | DV-13 según §4.13: la llave en barra + 88 en Rutinas (x fija), el `+` como obstáculo solo con Rutinas elegida y sin página encima (cambios en `CoversShellChanged`); relleno inferior de cada contenido desplazable = tope del obstáculo más alto que se cruza + 16 (lista; editor: + 16, mínimo 24) | DT-03 |
| DN3-15 | WP-B | Rutinas: `Spinner` de 32 con trazo 3; gracia de 120 ms y fundido de 160; los tres estados centrados entre las dos barras (relleno inferior del cuerpo = alto de la barra); `#routines-error-icon` **[nuevo]** (`error` 48 `state.dangerText`) | §4.1; DT-20 |
| DN3-16 | WP-A / WP-B | Las constantes de §5.11 en `MotionSpecs` (Core, WP-A). `GrowMotion` y `CollapseMotion` (`Presentation/Motion`, WP-B; QA-G3-15) animan el alto **y los márgenes**; la primera carga escalona solo si la pestaña está a la vista | §5 |
| DN3-17 | WP-B | `IconButton` y `RmButton` con el tamaño del ícono por parámetro (filas y encabezado 20; pie 16); por defecto, 24 y 18 como hoy | §4.6, §4.7 |
| DN3-18 | WP-B | Desplazar hasta lo nuevo después de agregar un paso o una serie (resorte ω 12, 16 dp de aire); `KeyboardAvoider` con 16 dp sobre el teclado y el diálogo recentrado con ω 22. Además, hasta el elemento que recibe el foco por Tab en `#routines-list`, `#editor-scroll` y `.rm-dialog__content` (ω 22, 16 dp de aire; QA-G3-11) | §4.14, §5.6, §5.8 |
| DN3-19 | WP-B (P2) | `Guardar` con el *spinner* 16 en lugar del texto si el guardado tarda más de 300 ms (mismo ancho) | Sin aviso de espera con una base lenta |
| DN3-20 | WP-B (P2) | `GrowMotion` que también va del alto viejo al nuevo (subtítulo que pasa a dos líneas); mini-barra que interpola sus tramos (§5.5) | Deseables, no los pide ningún AC |
| DN3-21 | WP-D0 (`.ttf`) + WP-B (S-B3a, fábrica y `FontAsset` en B2) + WP-D | La copia de `PanelTextSettings` con los respaldos en el orden de §4.15 (Noto Emoji si está), armada por una sola fábrica que usan `AppRoot.Awake`, `UiTestHost` y el arnés (QA-G3-12). El `FontAsset` de Noto Emoji, en `FontAssetPaths.All` (9 → 10). S-B3a suma una muestra `Remo 🚣 ñ` (sin la fuente: `□` y el texto sin correrse) | P-09 |
| DN3-22 | WP-B | **`enableRichText = false`** y **`parseEscapeSequences = false`** en las etiquetas con texto del usuario (nombre y descripción de la tarjeta, contenido del diálogo de eliminar) y en el texto de los campos, con pruebas de los nombres `<b>A</b>` y `Serie\n2` (barra y `n` escritas a mano), que se ven tal cual (QA-G3-10) | AC-RUT-08-01 pide el texto al carácter, y la spec AC-RUT-10-03 dice que no se interpretan marcas; UI Toolkit interpreta las etiquetas `<…>` por defecto |
| DN3-23 | WP-B (arnés) | Las sondas de §7.2 en `RoutinesCatalog` con `WithProbe(…, aspect: true)`, y las reglas de toma de §7.1 | Deuda de la It. 2 (REPORT §7.2) |
| DN3-24 | WP-B (B2, junto con el `FontAsset` de Noto Emoji y el bootstrapper) | R3-06 (§4.16). El paso `ui.fonts` deja **`getFontFeatures = false`** en los 7 `FontAsset` de `FontAssetPaths.TextFaces`, los que ya existen y los que cree, dentro de su `AllowWrites` y solo si hace falta (sin escrituras de más). Una prueba de dotnet (en `ItThreeToolingTests`) comprueba `m_GetFontFeatures: 0` en esos 7. Al aplicarlo, dos corridas completas del catálogo de la It. 2, una justo antes y otra justo después del cambio y sin nada más en el medio, tienen que salir iguales byte a byte (así se aísla el kerning de los cambios de B1 y B2). Si alguna toma cambia, es un par que hoy se suma en runtime: se lista y el Designer decide si se acepta o se genera (§4.16). R3-06 de §8.2 pasa a "decidido" | Kerning que no depende del orden de dibujo (QA-G3-18; 00-deuda-it2 §3) |

---

## 10. Para el PO y para QA

| Para | Qué | Propuesta |
|---|---|---|
| PO · DV-12 | Ampliar: títulos de tipo en `step.*.hud` (además del enfriamiento `#37A2CB`, el trabajo `#FF5D7F` en lugar de `#EF476F`: 3,815 → 4,685); ícono de la insignia también; el número del chip `×N` en `#38CBEF` en lugar de `#00B4D8` (4,370 → 5,620), con el fondo `#00B4D8 @20 %` de AC-RUT-14-03. Mismos matices; los bordes, las insignias y la mini-barra siguen en los colores de contrato | Igual que DV-04 con el pulso en la It. 2 |
| PO · CAP-13 | "8 dp entre los dos" → **16 dp** (8 de aire de cada bloque; en el legacy, `margin top 8` del encabezado + `bottom 8` del pie, `routine_editor_screen.dart:377`, `:427`) | Paridad |
| PO · AC-BLE-16-02 (DT-03) | "La llave queda a 16 dp de la derecha y a 16 dp sobre la barra de pestañas. En Rutinas, mientras se ve su botón `+`, sube a 16 dp sobre el `+` (88 dp sobre la barra)" | §4.13 |
| PO · DT-01, DT-02 | De acuerdo con los dos (§1, §4.8); el PO los registró como DV-18 y DV-19 | — |
| PO · informativo | Sin diferencias nuevas con el legacy: los mosaicos de tipo cumplen DV-09; el ícono del error de carga, el riel continuo, las hojas de la serie, la gracia de 120 ms del indicador y el aviso sin ícono son del sistema visual (spec §11.1, párrafo inicial). El kerning congelado de §4.16 tampoco es una diferencia de producto: el legacy dibuja con la fuente del sistema | — |
| QA | Pares R1-R27 de §8.1; excepciones de §8.2; sondas de §7.2. En [Device-Win], los respaldos de §4.15 con un nombre con emoji y los nombres `<b>A</b>` y `Serie\n2`. Con DN3-24, las comparaciones byte a byte de §7.1 (regla 5) | — |
| Coordinador | WP-D0 hace los glifos `arrow_back` y `more_vert` con la receta de ASSET_SLOTS §4.1 usando **`uv`**, que está instalado: `uvx --from fonttools==4.67.0 fonttools varLib.instancer …` y `uvx --from fonttools==4.67.0 pyftsubset …`. No hace falta Python del sistema (el `python.exe` del PATH es el alias de la Microsoft Store; `uv` usa su propio Python). Corrida completa en esta PC con 44 codepoints: §14.6. Hace falta red solo para bajar la fuente variable de la versión fijada (15 MB) si no está a mano | — |

**Estado en el gate**: el PO integró las cuatro filas de arriba en la spec (01-po-spec §16). Con QA-G3-03, DV-12 ampliada y AC-RUT-12-02 y AC-RUT-14-03 corregidos; con QA-G3-04, CAP-13 con 16 dp; con QA-G3-05, AC-BLE-16-02 en §5, con el lugar del panel; con QA-G3-17, DV-18 y DV-19.

---

## 11. Plan de WP-D0 y WP-D

**WP-D0** (antes de WP-A; solo datos y fuentes):

| Archivo | Qué | Hecho cuando |
|---|---|---|
| `Assets/RowMate/Core/Design/Tokens.cs`, `Presentation/Styles/Tokens.uss`, `Typography.uss` (G) | `overlay.scrim` y los cinco de §2.1 | `tools/tokens --check` en 0 (133 tokens); `TokensUssSyncTests` en verde |
| `Tests/EditMode/Design/ContrastTests.cs` | `ItThreeDesignerPairs` (R1-R27) | dotnet en verde |
| `UI/Icons/RowMateIcons-Outline.ttf`, `Core/Design/Icons.cs`, `IconsTests` | `arrow_back`, `more_vert` (42 → 44), con la receta de ASSET_SLOTS §4.1 por `uvx` (§10) | `IconsTests` en verde; el `.ttf` con 45 glifos (44 + `.notdef`), avance 960 en todos y los 42 de antes idénticos (como en §14.6) |
| `UI/Fonts/NotoEmoji/` (`NotoEmoji-Medium.ttf` y `OFL.txt`) | el respaldo de P-09 (§4.15), de Google Fonts. Instancia estática de 500, el peso de los íconos (ASSET_SLOTS §4.1); si la descarga no la trae, sale de la variable con `uvx --from fonttools==4.67.0 fonttools varLib.instancer … wght=500` | el `FontAsset` lo crea el bootstrapper en B2 (DN3-21) |
| `docs/migration/design/ASSET_SLOTS.md` | §4.1 paso 1 con `uvx --from fonttools==4.67.0` (sin la versión, `uvx` toma la última); §4.3 (íconos); el slot `ui.font.emoji`; en §3, el kerning de §4.16 (`getFontFeatures = false` y cómo se completa la tabla) | — |

**WP-D** (después de B3, con el arnés): `Skin.uss` (§4: superficies, cápsula, riel, hojas, mosaicos, segmentado, campos, menú, ayuda, aviso, diálogo, estados y anillos), `Motion.uss` (§5.9), `SimulatorSkin.uss` si la llave lo necesita; el visto bueno de los respaldos de fuente (§4.15, con la fuente de WP-D0 y el `FontAsset` de B2) y del kerning congelado (§4.16, DN3-24); el visto bueno de §7.2 con las sondas; las biblias (§12). Orden: CAP-04 y CAP-10 primero (son el sistema), después diálogos (CAP-14, -15, -20), avisos y llave (CAP-01, -18), letra grande y ventana chica (CAP-22, -23, -25), videos al final. Hecho = el de 02-tech-design §6 con 0 desbordes y todas las sondas dentro de tolerancia.

---

## 12. Alineación de las biblias (se aplica en WP-D, con este documento como fuente)

| Dónde | Cambio |
|---|---|
| ART_BIBLE §2.1, §4.3 | Tokens `overlay.scrim`, `border.input`, `text.selection`, `titleXs`, `headerS`, `buttonS`; 133 tokens |
| ART_BIBLE §2.2-§2.3 | Regla de color de tipo: texto e íconos `.hud`, gráficos base (también fuera del HUD) |
| ART_BIBLE §3.7 | Tarjeta de rutina: chip `accent.tonalBg` (no `@14 %`); **`⋮` en Rutinas, *chevron* solo en Workout** (spec §15); descripción 12 `text.secondary`, resumen 13 `text.tertiary`; lista con márgenes 16 y separación 10; reglas de la mini-barra (§4.3) |
| ART_BIBLE §3.8 | Fila de paso (título y ícono `.hud`, lápiz `text.secondary`, acciones 20 en 48, `min-height` 56); serie (encabezado 48, `Serie` `headerS`, cápsula 28 con `accent.onTonal`, riel continuo con cuerpo al 6 %, pie de 48 con **`Agregar paso a serie`**, hojas); campos (`border.input`, etiqueta adentro 15 → 12, 52 / 72, foco y error de 2 dp, selección); selector de tipo como **mosaicos** (ícono arriba, `@20 %`, borde 2) en lugar de chips; tendencia a 1,17 em en `metric.distance` |
| ART_BIBLE §3.5, §3.6 | Diálogos de la app (no inmersivos): `surface.overlay`, `r.card`, borde, relleno 24 (16 compacto), anchos 400 / 560; `+`: presionado `accent.primaryPressed` y escala 0,94 |
| ART_BIBLE §3.10 | Vacío de Rutinas con los números de la spec (56 al 12 %, `text.secondary` / `text.tertiary`, centrado entre las barras) en lugar de "56 `@24 %`, 17/600"; error de carga (ícono 48 `state.dangerText` + texto + tonal); **cargando: indicador de 32 con gracia de 120** (salen los *skeleton*, DT-20); aviso sin ícono, con las reglas de ancho y de obstáculos |
| ART_BIBLE §3.11, §5.1 | Llave en Rutinas (§4.13); sin ancho máximo de contenido en escritorio en esta iteración (paridad) |
| ANIMATION_BIBLE §2.2 | Pila con opacidad en 160 / 40-200 y curvas; diálogo que sale a 0,98; menú y ayuda; aviso con empujón y obstáculos |
| ANIMATION_BIBLE §2.3 | "Lo nuevo crece, lo cambiado se ilumina, lo borrado se desvanece y colapsa", con los tiempos de §5.5-§5.6, márgenes incluidos, y la primera carga solo con la pestaña a la vista |
| ANIMATION_BIBLE §2.14 (nueva) | Rutinas y editor (It. 3): tabla de §5.0 y reducir movimiento de §5.10 |
| LEGACY_VISUAL_AUDIT §5.4 | Descripción 12 y resumen 11 al 54 % (spec §15; es una corrección de lectura, no un valor de su momento) |

---

## 13. Qué queda abierto

- **NO VERIFICABLE hasta WP-B**: el glifo de tendencia por respaldo de fuente (línea de base y tamaño, S-B3a), `selectAllOnFocus` con el puntero, el emoji sin la fuente (`□`), la etiqueta flotante con la escala 0,8 en un `Label` con `.rm-type-body`.
- **[Device-And]**: el campo y el diálogo sobre el teclado (§4.14), el presionado con el dedo (`:hover` en táctil, deuda de la It. 2).
- **P2**: DN3-19 y DN3-20 (alto que pasa de una a dos líneas, mini-barra que interpola sus tramos).
- **Kerning** (§4.16): que `getFontFeatures = false` no cambie ninguna toma de la It. 2 se ve recién al aplicar DN3-24, con las dos corridas que pide. En la It. 4a, antes de sus capturas, se revisan con el mismo método (§14.7) los títulos `display` y `headline` y el HUD; si falta un par que se vea, se completa la tabla para el juego fijo.
- Fuera de paridad, sin urgencia: mostrar en la tarjeta la duración total; una vista desplegada de la serie con cada repetición progresada (spec §13).

---

## 14. Verificación hecha

1. **Legacy** (solo lectura): medidas, colores y pesos de `routines_screen.dart` (tarjeta, vacío, menú, diálogo) y `routine_editor_screen.dart:202-739` (campos, fila de pasos, fila, encabezado, chip, pie, diálogo de paso, diálogo de repeticiones), citados en cada sección; `theme.dart` (tarjeta 16); `image-04.png`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó `strava_secrets.dart`.
2. **Anchos de texto**: programa propio de .NET (`<scratch>/it3/designer/measure/`) sobre `Inter-*.ttf` y `BarlowSemiCondensed-*.ttf` del repo, con `cmap` y `hmtx` y sin kerning (cota superior): los 90 textos de §3.
3. **Contrastes**: programa propio (`<scratch>/it3/designer/contrast/`), mezcla sRGB sin redondear, el método de `Contrast.cs`: los 122 pares de §2-§8, incluidos los que se descartaron (capa blanca y roja del menú, chip con `accent.primary`, `border.strong` en el campo).
4. **Sistema construido**: `Tokens.cs`, `Icons.cs`, `MotionSpecs.cs`, `FontAssetPaths.cs`, `Base.uss`, `Skin.uss`, `Motion.uss` y `Typography.uss` del repo; capturas CAP-10, CAP-16 y CAP-21b de la It. 2.
5. **Sin cambios en el repo** fuera de este archivo: no se tocó código, tokens ni otros documentos, y no se abrió Unity. En el gate, tampoco: lo de 6 y 7 corrió en el scratchpad.
6. **Receta de glifos de WP-D0** (QA-G3-18 a), en `<scratch>/it3/designer/glyphs/`, sin tocar el repo:
   - Herramientas: `uvx` 0.11.12 con `--from fonttools==4.67.0` (la versión de la It. 2; sin fijarla, `uvx` toma 4.68.0) y el Python que gestiona `uv`. La fuente variable de la versión fijada es la que quedó de la sesión de la deuda de diseño.
   - Pasos: instancia de contorno (FILL 0, GRAD 0, opsz 24, wght 500) y recorte con los 42 codepoints de ASSET_SLOTS §4.1 más `U+E5C4` y `U+E5D4`. Tardó 4,6 s.
   - Resultado: 9 304 B, 45 glifos (44 + `.notdef`), 960 unidades por em, avance 960 en todos, peso 500 y sin GPOS, como el versionado.
   - Los 42 íconos de antes tienen contornos y avances **idénticos** a los de `RowMateIcons-Outline.ttf` (comparados con un *pen* de fontTools), y `arrow_back` y `more_vert` están en el `.codepoints` versionado (`e5c4`, `e5d4`).
7. **Kerning** (QA-G3-18 b, R3-06), en `<scratch>/it3/designer/kerning/`:
   - Pares GPOS: fontTools 4.68.0 sobre las fuentes del repo (feature `kern`, formatos 1 y 2, gana el primer subtable que aplica), contra los registros `m_GlyphPairAdjustmentRecords` de cada `.asset` versionado.
   - Textos: los de la It. 3 en su peso (§2.2), con las 16 claves nuevas de la spec §11.2. Tamaños de §4.16 a 433 B por par (7 083 979 B / 16 356 pares en Inter Regular).
   - IL de `FontAsset` y `FontEngine` (`UnityEngine.TextCoreTextEngineModule.dll` y `UnityEngine.TextCoreFontEngineModule.dll` de 6000.3.27f1), leído con un programa de .NET 8 sobre los metadatos, sin cargar las DLL. Visibilidad: son públicos `getFontFeatures` y `TryAddCharacters(…, includeFontFeatures)`; son internos `ClearCharacterAndGlyphTablesInternal`, `ClearAtlasTextures`, `glyphPairAdjustmentRecords` y las consultas de pares de `FontEngine`. Qué lee y llama cada método: `TryAddGlyphToTexture` y `TryAddGlyphs` leen `m_GetFontFeatures` y encolan, y `ClearFontAssetData` llama a `ClearFontFeaturesTables`.
   - Hoy las 9 `FontAsset` tienen `m_GetFontFeatures: 1`.
   - Legacy: `pubspec.yaml` sin `google_fonts`, `theme.dart` sin `fontFamily`, y en `lib/**` el único `fontFeatures` es el de cifras tabulares de `lib/features/workout/series_panels.dart:46`.

---

## 15. Cambios por gate QA

Correcciones por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md), APROBADO CON OBSERVACIONES: 0 bloqueantes, 7 mayores, 14 menores), 2026-10-27. A este documento le tocaba un issue, **QA-G3-18** (menor). Lo demás es alineación con la spec corregida (01-po-spec §16) y con lo que el gate les pide al Architect sobre los DN3 de §9. No cambia ningún valor que ya estaba (token, color, medida, contraste ni tiempo). Lo nuevo es el ajuste de kerning, el desplazamiento hasta el foco (ω 22, el mismo del error) y el peso de Noto Emoji (500, el de los íconos).

| Issue | Severidad | Qué cambió | Dónde |
|---|---|---|---|
| QA-G3-18 (a) | menor | La fila del coordinador decía que WP-D0 necesitaba instalar Python. **Corregido**: la receta de ASSET_SLOTS §4.1 corre con `uvx --from fonttools==4.67.0`, que está instalado. Se corrió entera en el scratchpad con los 44 codepoints: 45 glifos, avance 960 y los 42 íconos de antes idénticos. En WP-D0, ASSET_SLOTS §4.1 fija esa versión | §10, §11, §14.6 |
| QA-G3-18 (b) | menor | **R3-06 decidido**: el kerning es el de la tabla versionada y nada más (`getFontFeatures = false` en las 7 caras de texto, DN3-24). En la It. 3 no se genera tabla. Medido: 12 textos con un par de como mucho 1,15 dp, y ninguno por los cinco glifos de R3-06. La generación para un juego fijo queda descrita, con su tamaño, para la It. 4a. Si el Architect no integra DN3-24, vale la segunda opción de QA: comparar solo corridas completas | §0, §1, §4.16, §7.1 regla 5, §9 DN3-24, §11, §13, §14.7 |

**Sobre las dos opciones de QA para (b).** No disputo el issue: lo decido con una variante, por los números.

- *Generar la tabla dentro de `AllowWrites` (opción 1).* Cuesta unos 3,2 MB en la forma de TextCore, para 12 textos de como mucho 1,15 dp. Además, en 6000.3.27f1, vaciar los glifos sin vaciar la tabla necesita API interna.
- *Comparar solo corridas completas (opción 2).* No cuesta nada, pero deja la dependencia del orden: una corrida con `-Filter`, un catálogo reordenado o el segundo dibujo de un mismo texto pueden salir distintos.
- *La variante.* Apagar `getFontFeatures` saca la causa con un campo público. La opción 2 queda como alternativa, y la 1, como el camino para cuando haga falta.

**Alineación** (no son issues de este documento):

| Origen | Qué cambió | Dónde |
|---|---|---|
| Spec corregida (QA-G3-03, -04, -05, -17) | Las cuatro propuestas de §10 para el PO ya están en la spec: DV-12 ampliada, CAP-13 con 16 dp, AC-BLE-16-02 en §5 y DV-18 y DV-19. §4.13 suma el lugar del panel que fijó el PO: siempre el mismo; en Rutinas tapa el `+`, y mientras está abierto la llave no se ve | §4.13, §10 |
| QA-G3-01 (spec AC-RUT-09-03, AC-APP-03-01, -03, -04) | Lo que sale no recibe toques desde el cuadro 0; la ayuda emergente, nunca | §4.4, §5.0, DN3-12 |
| QA-G3-02 (DN3-07, DN3-15) | La capacidad de la mini-barra se calcula en `GeometryChangedEvent`. CAP-02 y `RLT.LoadingShowsOnlyTheIndicator` esperan la gracia y el fundido | §4.3, §7.1 regla 2, §7.2 CAP-02, DN3-07 |
| QA-G3-10 (spec AC-RUT-10-03, -07) | `parseEscapeSequences = false` junto con `enableRichText = false`, también en los campos; prueba con `Serie\n2` | §4.15, DN3-22, §10 |
| QA-G3-11 | Desplazar hasta el foco por Tab en la lista, el editor y el diálogo (ω 22, 16 dp; en el acto con reducir movimiento); constante `ScrollToFocusOmega` | §5.0, §5.8, §5.11, DN3-18 |
| QA-G3-12 | Una sola fábrica de los respaldos de fuente para `AppRoot.Awake`, `UiTestHost` y el arnés | §4.15, DN3-21 |
| QA-G3-13 (f) | Una palabra de 200 caracteres sin espacios se parte | §4.2 |
| QA-G3-15 (y 02-tech-design §1.1) | DN3-16: las constantes en `MotionSpecs` (WP-A), y `GrowMotion` y `CollapseMotion` en WP-B. Noto Emoji: el `.ttf` en WP-D0 (Medium, el peso de los íconos) y el `FontAsset` en B2, dentro de `FontAssetPaths.All` (9 → 10), como lo asignó el Architect | §4.15, §9 DN3-16 y DN3-21, §11 |

**Para el Architect** (Anexo B de 02-tech-design): DN3-24 es nuevo. DN3-07, -12, -16, -18, -21 y -22 suman lo que pide el gate, sin cambiar lo que ya pedían. R3-06 de §8.2 pasa a "decidido" (§4.16).

Verificación del gate: §14.6 y §14.7. `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar, no se leyó `lib/core/strava/strava_secrets.dart` y Unity no se abrió.

---

## 16. WP-D: implementado y verificado (2026-10-28)

El aspecto y las micro-interacciones de la It. 3 sobre el contrato de 02-tech-design §7.5, sin tocar el contrato, la lógica ni la estructura de WP-B. Iterado con el arnés `it3` (tres corridas: dos en `<scratch>` y la final en `snapshots/`) mirando cada captura y midiendo píxeles. Unity 6000.3.27f1 en batchmode, una sola instancia por vez.

### 16.1 Archivos

| Archivo | Qué |
|---|---|
| `Assets/RowMate/Presentation/Styles/Skin.uss` | sección It. 3: capas de estado de la tarjeta, el `+`, la cápsula `×N`, los dos selectores y los menús (el ítem de peligro con su capa oscura); el `+` presionado solo con `:hover:active`; anillo **interior** de tiempo / distancia y anillo del mosaico a 4 dp; borde del campo con *hover*; texto del campo en la columna de la etiqueta; fila de paso con relleno 4; 8 dp de aire en el contenido de un diálogo que se desplaza |
| `Assets/RowMate/Presentation/Styles/Motion.uss` | transiciones de esas capas (hacia el estado 90 `ease.enter`, vuelta 160 `ease.exit`, la regla común); `+`: escala 0,94 y fondo `accent.primaryPressed` (90 / 240 / 160); mosaico: escala 0,97; borde del campo: 90; reducir movimiento sin escalas. Todo bajo `:hover`: ninguna transición puede arrancar en el arnés |
| `Assets/RowMate/SimulatorPresentation/Styles/SimulatorSkin.uss` | sin cambios: la llave se ubica y se mueve por código (DN3-14) y su aspecto es el de la It. 2 |
| `Assets/RowMate/Snapshots/RoutinesCatalog.cs` | solo sondas de aspecto (`aspect: true`, no hacen fallar la toma; DN3-23) y la comprobación de ancho de tramo de CAP-04/-05 que pide 02-tech-design §5.4 (DN3-07) |
| `Assets/RowMate/Tests/EditMode/Design/ContrastTests.cs` | `ItThreeDesignerPairs` (R1-R28, 75 casos) e `ItThreeControlValuesMatchTheDesignNotes` (12 valores a 3 decimales). Era de WP-D0 (02-tech-design §7.1) y faltaba |
| `docs/migration/design/ASSET_SLOTS.md` | también de WP-D0 y faltaba: slot `ui.font.emoji`, el kerning de §4.16, Noto Emoji, la receta con `fonttools==4.67.0` y 44 codepoints, `arrow_back` y `more_vert` |
| `docs/migration/design/ART_BIBLE.md`, `ANIMATION_BIBLE.md`, `LEGACY_VISUAL_AUDIT.md` | §12 aplicado, cada uno con su sección "Cambios de la It. 3" |
| `snapshots/*` | regeneradas con el aspecto final: 47 tomas, 0 con problemas |

Lo demás de WP-D0 ya estaba: los dos glifos e `IconsTests` (44), los seis tokens (133) con `Tokens.uss` y `Typography.uss` regenerados, y Noto Emoji con su licencia.

### 16.2 Lo que cambió al implementarlo

| Qué | Borrador | Final | Por qué (medido) |
|---|---|---|---|
| Fila de paso (§4.6) | relleno 8 / 4 / 8 / 12 | **4 / 4 / 4 / 12** | El borrador pedía 56 de alto (§3) y a la vez 8 + 48 + 8 = 64 por las cajas táctiles del lápiz y la cruz. Con 64, la vista previa del diálogo (sin botones) medía 56 y la fila del editor 64. Ahora las dos miden 56, el `min-height` de contrato, y una fila con el subtítulo en dos líneas mide 56,9. Queda más cerca del legacy (≈ 52 en `image-04.png`) y la lista de 11 pasos de CAP-12 se acorta 88 dp |
| Texto de los campos (§4.8) | — | **una columna** | El `TextElement` interno del campo de UI Toolkit trae un margen propio: en CAP-10 y CAP-13 (TV) la etiqueta empezaba en x = 80 px y el valor en 85 (≈ 2 dp), en una línea y en la descripción. Con margen 0 los dos quedan en 80, y la selección y el cursor también (CAP-16, CAP-26). El cursor en la posición 0 se sigue viendo entero |
| Diálogo que se desplaza (§4.9) | contenido pegado a las líneas | **8 dp de aire** arriba y abajo | En CAP-23 la vista previa tocaba la línea de arriba y en CAP-15b el último campo la de abajo. Solo con `--scrolls`: sin desplazamiento, el contenido sigue a 16 del título, como en M3. Es estable: el relleno solo agranda un contenido que ya desborda |
| Anillo de tiempo / distancia (§4.14) | por fuera, 24 / 28 | **adentro**, a 2 dp; esquinas de afuera 17 y del divisor 2; sin halo | La cápsula tiene `overflow: hidden` (Base.uss): un anillo por fuera queda recortado y con teclado no se veía el foco (AC-APP-05-07). Sobre la opción elegida va en blanco: `focus.ring` sobre `brand.seed` da 1,9:1 y el blanco, 4,868 (R28) |
| Anillo del mosaico (§4.14) | radio 12 a −4 | a **−6** desde adentro del borde de 2, halo 16 | Lo absoluto se mide desde adentro del borde: a −4 el anillo quedaba pegado al borde de 2 dp del mosaico. A −6 queda a 4 dp de afuera, con los 2 dp de aire de los demás controles |
| Borde del campo con *hover* (§5.9) | vuelta en 160 | **vuelta en el acto** | Una transición en la regla base también correría al perder el foco o al pasar a error, sin puntero: en batchmode quedaría congelada y la regla de toma la frena. La ida (90) queda, bajo `:hover` |
| `+` presionado (§4.1) | Base.uss lo pintaba también con `:active` solo | solo con `:hover:active` | El clic simulado deja `:active` 100 ms de reloj real (lección de la It. 2): una captura justo después de tocar `+` no puede salir "presionada" |

No cambió ningún token, color, contraste, tiempo ni texto. Los 75 pares de R1-R28 dan exactamente los valores de §8.1 (dotnet).

### 16.3 Sondas de aspecto (§7.2) en la corrida final

**78 sondas en 20 tomas, las 78 dentro de tolerancia** (antes de WP-D había 17 en 12). Las diferencias que no son 0 son de 1 por canal, de la mezcla en 8 bits: ícono del vacío `#2A3744` (esperado `#2A3644`), título del vacío `#B6BABF` (`#B6BBBF`), tonal de `Reintentar` `#0B2E3F` (`#0B2D3F`), cuerpo de la serie `#0C2032` (`#0C2132`), `×3` `#37CBEF` (`#38CBEF`) y ayuda `#8E9398` (`#8D9298`).

| Toma | Qué miden |
|---|---|
| CAP-01 | fondo, `+`, llave, ícono del vacío al 12 %, título `text.secondary`, ayuda `text.tertiary` |
| CAP-02 · CAP-03 | indicador; ícono del error y `Reintentar` tonal |
| CAP-04 · CAP-05 | tarjeta, chip del ícono, tramos 1, 2, 3 y 8 de `Escalera SPM` en su color base; además, la comprobación de ancho de tramo: 24 en `Escalera SPM` y 16,4 en `Pirámide 500` (± 0,5) |
| CAP-07 · CAP-08 | menú y `Eliminar` rojo; diálogo, velo sobre el fondo y sobre la tarjeta, `Eliminar` relleno, cinta y llave **sin oscurecer** |
| CAP-09 | caja del campo, su borde `border.input` (`#6B7179`), vacío del editor |
| CAP-10 · CAP-11 | encabezado y cuerpo de la serie, las 4 insignias, los 4 títulos en `.hud`, el riel a 1,5 dp, la cápsula, el `×3` y las dos hojas |
| CAP-14 · CAP-15b | vista previa (tarjeta e insignia), mosaico elegido `#483956`, segmento elegido `#0077B6`; ayuda `Ej: 5` en `text.tertiary` |
| CAP-17 · CAP-18 · CAP-19 · CAP-28 | borde con foco; los tres avisos en `surface.overlay` |
| CAP-20a · CAP-20b · CAP-21 | borde de error de 2 dp (`#FF5252`) y su texto (`#FF7272`) |
| CAP-26 | selección `#054B60` |

Sin sonda, porque no hay un elemento propio que medir, y medida aparte sobre la captura: la tendencia del subtítulo de CAP-10, `#4CC9F0` exacto (`metric.distance`). Lo geométrico de §7.2 lo cubren las comprobaciones de 02-tech-design §5.4 (llave 16 sobre el `+`, aviso debajo de la llave, 16 dp entre series, menú alineado, `--compact`, `Audit` sin desbordes).

### 16.4 Videos, cuadro por cuadro

Revisados con cuadros extraídos a 30 fps en `<scratch>`.

| Video | Visto |
|---|---|
| VID-01 | El diálogo entra y sale como §5.2. La fila nueva crece desde 0 debajo del diálogo que se va (a los 100 ms del `Confirmar` ya asoma; a los 300, entera) y el vacío ya se fue. La serie se despliega de arriba abajo, con sus hojas. La cruz desvanece la fila en 120 y el hueco se cierra sin salto. **Defecto** (de WP-B, §16.6): el tacho del último bloque no colapsa; el bloque desaparece en el cuadro 0 y el vacío aparece entero, sin fundido |
| VID-02 | El editor entra 24 dp desde la derecha y ya es opaco a los 160; la lista se corre y se oscurece. Al volver, el editor se va en 200 y la llave sube con su resorte. El menú crece desde su esquina. La tarjeta borrada se desvanece y las de abajo suben sin salto |
| VID-03 | Un solo aviso: entra subiendo 16 y fundiendo en 220; cada `Guardar` da un empujón de 1,02 (se ve a los 1,53 s); se va a los 6,5 s bajando 8 |

### 16.5 Visto bueno de fuentes y kerning

- **Noto Emoji** (§4.15, DN3-21): con la fuente en la lista de respaldos, `Remo 🚣 ñ` sale con el glifo **monocromo** en el color del texto, en la línea y sin correr la `ñ` (S-B3a, primera corrida: la que busca primero con Noto, que es lo que hacen todos los paneles, porque app, `UiTestHost` y arnés salen de `RuntimePanelFactory`). Sin la fuente, Windows dibuja el emoji de color del sistema. Visto bueno.
- **Kerning** (§4.16, DN3-24): las 7 caras de texto quedaron con `m_GetFontFeatures: 0`; íconos y Noto Emoji, en 1, como pedía §4.16. Comprobado con una corrida con `-Filter "CAP-04,CAP-10,CAP-26"` contra la corrida completa: CAP-04 y CAP-26 salen **idénticas byte a byte**; CAP-10 difiere en 5 píxeles en 1 unidad, en el borde de un glifo del subtítulo con la tendencia. No es kerning (un par movería el glifo entero): es el orden en que el atlas dinámico acomoda los glifos, que cambia el muestreo del SDF en los bordes. Visto bueno. Por eso la regla 5 de §7.1 queda con ±1 por canal: byte a byte solo entre corridas completas del mismo catálogo.

### 16.6 Para WP-B (motion por código y arnés; no son de USS)

Lo que §5 pide por código y no está, encontrado en los videos y en el código. Con USS no se puede hacer: una transición que no dependa del puntero corre también en el arnés, queda congelada y la toma falla.

1. **El último paso o bloque se va sin colapsar** (§5.6). `RoutineEditorPage.Render` pasa `#editor-steps` a `display: none` y `#editor-empty` a `flex` en el mismo cuadro en que `EditorStepsView` arranca el `CollapseMotion`. Propuesta: con 0 pasos, dejar `#editor-steps` visible hasta que termine el colapso y recién entonces fundir `#editor-empty` en 240; al agregar el primer paso, fundirlo → 0 en 120.
2. **Hojas de la serie** (§5.6): aparecen y se van en el acto (`SeriesBlockView.SetRepeat` cambia `display`). Piden alto 0 ↔ 3 y opacidad en 160 `ease.move`.
3. **Colores del tipo** (§5.7): mosaico elegido, vista previa y fila cambian en el acto (`SegmentedControl.Paint` y la clase de `StepRowView`). Piden el cruce en 160 `ease.move`, también con reducir movimiento.
4. **Línea de error** (§5.2): aparece en el acto (`RmTextField.SetError`). Pide alto 0 → texto y opacidad en 160.
5. **Arnés, cosmético**: `UiTestHost.Type` asigna el valor y deja el cursor en 0 (`|500` en CAP-16, `|Prueba` en CAP-19 y CAP-28, `|5` en DEMO-03). Con `SelectRange(largo, largo)` después del valor quedaría al final, como al escribir.

Ninguno cambia datos ni comprobaciones. Los cuatro primeros son de §5 y se ven en VID-01 cuando se hagan.
