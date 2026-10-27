# It. 2 — Notas de diseño visual y de motion

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-27 |
| Iteración | 2 · `02-dispositivo-simulador` ([ROADMAP §It. 2](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (§4, §7, §10), [02-tech-design](02-tech-design.md) (§2.5-§2.9, §3, §3.12, §4, §6.3, §7.1), [00-deuda-diseno](00-deuda-diseno.md), [ART_BIBLE](../../design/ART_BIBLE.md) §2-§5 y §10, [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §0-§2, `Assets/RowMate/Core/Design/Tokens.cs` (manda), capturas del legacy `docs/image-01.png` (Dispositivo, 1600 × 1080) y `docs/image-02.png` (Workout) |
| Entrega | Este documento. Sin código en el repo: el contenido de `Skin.uss`, `SimulatorSkin.uss`, `Motion.uss` y los tokens nuevos de `Tokens.cs` se escriben en WP-D (§10) |
| Legacy leído (solo lectura, `3ae0f3a`) | `lib/features/device/device_screen.dart` (completo), `lib/features/device/heart_rate_card.dart` (completo), `lib/core/dev/simulator_overlay.dart:1-212`, `lib/main.dart:85-181`, `lib/shared/theme.dart:1-45`, `lib/l10n/app_es.arb` y `app_en.arb` (claves de la It. 2). No se leyó `strava_secrets.dart`. `git -C rowmate-legacy status --porcelain` vacío antes y después |
| Verificado | Anchos de texto con HarfBuzz sobre las fuentes versionadas del repo; contrastes con el método de la It. 1; **prototipo descartable en Unity 6000.3.27f1** (proyecto del *spike* S-0 en el scratchpad, nunca en el repo) que renderiza la spec con `GammaRt` y mide estilos resueltos (§12) |
| Implementación | 2026-10-27, **WP-D** (pulido visual y de motion): §15. `Tokens.cs` (los 5 tokens de DN-01, que no se habían agregado en WP-D0), `ContrastTests` (N1-N13), `Skin.uss`, `Motion.uss`, `SimulatorSkin.uss` y las capturas regeneradas con el arnés |
| Revisión | 2026-10-27, por el gate de QA ([03-qa-harmony](03-qa-harmony.md)): QA-G2-08 y QA-G2-11, más la alineación con 01-po-spec §7.3, §11.1 y §16 (CAP-04a/b, DV-04, DV-05, AC-BLE-11-07). Detalle en §14 |

Convenciones: las de [04-design-notes de la It. 1](../01-fundaciones/04-design-notes.md) (hex `#RRGGBB`, transparencia `@NN %`, contraste WCAG 2.x con mezcla sRGB sin redondear). Tamaños en dp (1 px de USS = 1 dp). "TV", "TH", "L640", "P800" y "W1280" son los perfiles de la spec §7.2. "Código" = animación manejada por `MotionDriver` con el reloj inyectable (D-14); "USS" = transición de USS con el reloj real del panel. Los nombres `#…` y `.rm-…` son los del contrato de 02-tech-design §3; los que **agrego** están marcados **[nuevo]** y se piden en §9.

---

## 0. Resumen

1. **Acepto D-01 (GammaRt), D-07, D-10, D-11, D-13 y D-14** y confirmo la aclaración del corazón (§1). Todos los contrastes de este documento son con composición sRGB; si S-B1 cae a `Overlay`, hay un juego de respaldo de tres valores (§8.3).
2. **Cinco tokens nuevos** (§2.1): `bodyM` 14/400, `bodyXs` 12/400 y `button` 14/600 (los tamaños de 12 y 14 que pide la spec no existían en la escala), y `accent.tonalBg` / `accent.onTonal`, porque el botón tonal de ART_BIBLE (`accent` sobre `accent @16 %`) **da 4,25:1 y no llega a AA** (§8.1).
3. **Hallazgo de UI Toolkit 6000.3 (medido):** `letter-spacing: N px` agrega **N/100 em** por letra, no N px. El `letter-spacing: 0.96px` que el diseño técnico pone en `.rm-type-label` daría un tracking casi nulo: tiene que ser **`8px`** (= 0,08 em a cualquier tamaño) (§2.2).
4. **Tablero de instrumentos**: tarjeta con acento superior al 60 % en reposo (como el legacy, `device_screen.dart:298`), un brillo del color de la métrica que baja 24 dp desde el borde (8,4 % en reposo, 14 % al llegar un dato) y el valor centrado ópticamente sobre la banda de las mayúsculas (§4.4). Verificado en el prototipo: se ve como `image-01.png` con más profundidad.
5. **Motion** (§5): rodillo de 180 ms con curvas por celda que terminan siempre en el valor exacto; destello del acento de 300 ms solo en las tarjetas cuyo texto cambió; latido doble a 60/bpm con compresión por encima de 143 bpm; píldora con resorte crítico ω 22; "apagar" y "encender" el tablero al caer y volver el remo; panel del simulador que crece desde la llave. Ningún número cuenta ni pasa por valores intermedios.
6. **Reducir movimiento** (§5.10): fundidos en lugar de desplazamientos y escalas; el corazón queda quieto; los datos y el destello (opacidad) no cambian.
7. **25 pedidos concretos a WP-A / WP-B** (§9, DN-01 a DN-25), sin cambiar comportamiento ni arquitectura; uno al PO (CAP-04 en dos tomas, porque en 1280 × 720 la tarjeta del pulsómetro queda bajo la barra, igual que en el legacy; **adoptado**, 01-po-spec §7.3).
8. **Visto bueno de capturas** (§7): reglas de toma deterministas (latido en reposo, sin transiciones en curso) y, por captura, qué tiene que verse y qué píxeles se miden.

---

## 1. Respuestas a 02-tech-design §7.4 (Designer)

| ID | Respuesta | Por qué |
|---|---|---|
| D-01 | **De acuerdo (co-decisión): `GammaRt` por defecto** | Es la opción A que recomendé en la It. 1 (§5). En el prototipo la barra de pestañas sobre `bg.app` midió `#0A1623`, exactamente la mezcla sRGB esperada. Si S-B1 falla, aplicar el respaldo de §8.3 hasta la 4a |
| D-07 | **De acuerdo**: con las dos fuentes de íconos versionadas no hacen falta las recetas en la It. 2; alcanza el cuadrado redondeado genérico | El caso "sin fuente" ya no ocurre en un clon; las recetas de ASSET_SLOTS §4.4 quedan para quien las necesite |
| D-10 | **De acuerdo**: `FitBox` sin tope inferior | `123456m` en 2 columnas a 411 dp mide 209 dp a 52 dp y entra en 147,7: escala 0,707 (≈ 36,8 dp). ART_BIBLE §3.1 se alinea (§11) |
| D-11 | **De acuerdo**: ícono de la insignia 12 y del estado vacío 48 | Manda la spec (y es el legacy). 00-deuda-diseno §3.5 queda superado en esos dos números |
| D-13 | **Enterado.** El ritmo vertical se arma con márgenes sobre las cajas de línea que mide Unity (§2.2) | USS no tiene `line-height` |
| D-14 | **De acuerdo**, con un agregado: el fundido de color del ícono y la etiqueta de la pestaña también va por código (DN-11), para que VID-02 sea determinista. USS queda solo para *hover*, presionado, foco y el interruptor (§5.8) | Las transiciones de USS corren con reloj real y no aparecerían igual en los videos |
| Corazón | **Confirmo**: conectado con `--` = pleno y quieto; conectado con pulso = pleno y latiendo; sin conexión = quieto `@40 %` | `heart_rate_card.dart:39-40` y CAP-12a |
| Etiqueta del pulso | **`metric.hr.hud`** (`#FF5A7E`) para el ícono y la etiqueta de la tarjeta PULSO, para el título `PULSÓMETRO` y para la unidad `bpm` de la tarjeta del pulsómetro (QA-G2-08, DV-04); el **valor** en `metric.hr` (`#EF476F`) | La base da 3,81:1 (no llega a 4,5 para texto de 12 ni para la unidad de 14/400; el rosa al 70 % del legacy da 2,45); `#FF5A7E` es el mismo matiz (OKLCH) y da 4,61 (4,54 con el brillo encima, §8.1). AC-APP-06-01 permite fijar la luminancia de la etiqueta sin cambiar el matiz |

**Cambios respecto de 00-deuda-diseno §3.5** (mismos íconos, otro color): la llave del simulador pasa de `text.onAccent` sobre `accent.primary` a **ícono `accent.primary` sobre `surface.overlay`** (5,07:1), la misma superficie del panel que abre (§4.9: discreta y coherente; un cuadrado cian de 40 dp competía con el tablero). Los *chevron* de las filas pasan de `text.disabled` a **`text.tertiary`** (5,14:1): son la señal de "tocar para conectar".

---

## 2. Tokens nuevos y reglas de USS

### 2.1 Tokens nuevos (`Tokens.cs`, los agrega el Designer; ver DN-01)

| Token | Valor | Rol | Uso en la It. 2 | Por qué |
|---|---|---|---|---|
| `bodyM` | Inter 14 / 20 / 400 | tipografía | nombre conectado y unidad `bpm` (AC-HRM-13-06: 14 dp), línea del recordado, `Conectando a …`, rótulos del panel (`Watts`, `Ritmo`, `Hora escena`, `Remando`) | La escala tenía 15 y 13. El legacy usa 14 en todos esos textos; con 15, `Hora escena` no entra en el renglón del panel (84,6 dp en 88) |
| `bodyXs` | Inter 12 / 16 / 400 | tipografía | ayudas `deviceSearchHint`, `deviceNoResultsHint`, `hrmWatchHint`, error de la tarjeta, texto de la insignia `Conectado` | La spec pide 12 dp en esos textos (AC-BLE-05-01, -02, AC-HRM-13-07); `caption` es 11 y `label` lleva tracking de mayúsculas |
| `button` | Inter 14 / 20 / 600 | tipografía | etiqueta de todo botón, chip y acción de texto | ART_BIBLE decía 15/600 sin token. A 14 entra `Simular caída del pulsómetro` en el panel de 280 (196 dp de texto + ícono + relleno = 246 ≤ 254) y coincide con el tamaño del legacy (M3 `labelLarge` 14) |
| `accent.tonalBg` | `#00B4D8 @12 %` | superficie (decorativa) | fondo del botón tonal (`hrm-search` en "Sin sensor", `hrm-retry`) | ART_BIBLE §3.6 decía `@16 %` |
| `accent.onTonal` | `#38CBEF` | `UiText` | texto e ícono del botón tonal | `accent.primary` sobre el tonal da 4,25 (con `@16 %`) o 4,57 (con `@12 %`): no llega con *hover*. `#38CBEF` es el mismo matiz más claro (es el valor de `metric.spm.hud`, decisión independiente, regla de la It. 1 §1): 5,88 en reposo, 5,24 con *hover* |

Ningún token existente cambia. `ContrastTests` suma los pares de §8.1 (`DesignerPairs`).

### 2.2 Lo que hace UI Toolkit 6000.3.27f1 (medido en el prototipo)

| Tema | Resultado | Consecuencia para la spec |
|---|---|---|
| `var()` en `transition-duration` y `transition-timing-function` (pendiente NV de la It. 1 §7) | **Funciona**, también en el atajo `transition: opacity var(--rm-dur-slow) var(--rm-ease-exit)`; se resuelven a `160ms` / `EaseInOutCubic` etc. | `Motion.uss` usa las variables de `Tokens.uss`; el generador no necesita clases de transición |
| `-unity-background-image-tint-color: var(--rm-metric-hr)` | Funciona | El brillo se hace con una textura blanca teñida desde USS (DN-06) |
| `letter-spacing` | `N px` agrega **N/100 em** entre letras (Inter 600, `IIIIIIIIII`: a 12 dp, 4 px suman 0,44 dp por letra; a 48 dp, 2,0 dp). Es la unidad de TextCore, no píxeles | `.rm-type-label { letter-spacing: 8px; }` da 0,08 em, y escala solo con `.rm-text-scale-*`. DN-02 |
| Degradés y sombras | USS no tiene `linear-gradient` ni `box-shadow` | Profundidad con escalones de superficie, bordes y la textura de degradé |
| `flex-shrink` | Vale 1 por defecto: un contenedor alto fijo comprime botones e íconos | Todo control, ícono y fila de alto de contrato lleva `flex-shrink: 0` (DN-20) |
| Cajas de línea | Inter 12 → 14,9 dp (1,238 em), Inter 14 → 17,1 dp, Barlow SC 52 → 62,9 dp (1,209 em) | Las alturas de §3 y §4 usan estas cajas |
| Centrado óptico | Inter tiene las mayúsculas centradas en su caja (ascendente − altura de mayúscula = descendente = 0,241 em). Barlow SC no: la banda de mayúsculas (0,70 em) queda 0,05 em por debajo del centro (2,6 dp a 52) | `align-items: center` alcanza para Inter e íconos; `TabularNumber` centra la banda de mayúsculas (DN-08) |
| `rotate`, `scale` y `transform-origin` | Funcionan; la cinta rotada −45° alrededor de su centro queda en su lugar | §4.8 |
| Borde superior solo, con radio | Se afina en las esquinas como el legacy | Tarjeta de §4.4 |

### 2.3 Clase de texto y color por elemento (contrato que WP-B pone por código)

Regla 2 de 02-tech-design §3.12: el tamaño y la fuente salen solo de `.rm-type-*`. Esta tabla fija qué clase y qué color lleva cada texto. **[cambia]** = distinta de la que figura en 02-tech-design §3.

| Elemento | Clase | Color |
|---|---|---|
| `.rm-appbar__title` | `.rm-type-title` (20/600) | `text.primary` |
| `#device-name` | `.rm-type-title` | `text.primary` |
| `.rm-badge__label` | `.rm-type-body-xs` **[cambia]** | `state.connected` |
| `.rm-metric-tile__label` | `.rm-type-label` | base de la métrica; pulso `metric.hr.hud` |
| `.rm-metric-tile__value`, `#hrm-bpm` | `.rm-type-num-xl` | base de la métrica / `metric.hr` |
| `#hrm-title` | `.rm-type-label` | `metric.hr.hud` |
| `#hrm-nosensor-text` | `.rm-type-title-s` | `text.primary` |
| `#hrm-watch-hint`, `#device-search-hint`, `.rm-empty__hint` | `.rm-type-body-xs` **[cambia]** (era `caption`, 11) | `text.tertiary` |
| `#hrm-error` | `.rm-type-body-xs` | `state.dangerText` |
| `#hrm-connected-name`, `#hrm-remembered-text` | `.rm-type-body-m` **[cambia]** | `text.secondary` |
| `#hrm-bpm-unit` | `.rm-type-body-m` | `metric.hr.hud` (4,61:1) **[cambia]**: 02-tech-design §3.12 dice `--rm-metric-hr` (3,81, no llega); el legacy, rosa al 70 % (DV-04, QA-G2-08) |
| `#hrm-connecting-text` | `.rm-type-body-m` | `text.primary` |
| `#hrm-searching-text` | `.rm-type-body-s` (13) | `text.tertiary` |
| `.rm-hrm-device-row__title` | `.rm-type-body` (15) | `text.primary` |
| `.rm-hrm-device-row__subtitle` | `.rm-type-caption` (11) | `text.tertiary` |
| `.rm-empty__title` | `.rm-type-body` | `text.secondary` (cambio a propósito: el legacy, 54 %; DV-05, QA-G2-11) |
| `.rm-button__label`, `.rm-chip` (etiqueta) | `.rm-type-button` **[nuevo]** | según la variante (§4.7) |
| `.rm-tab__label` | `.rm-type-label` | `text.secondary` / `accent.primary` |
| `.rm-sim-panel__header` (título) | `.rm-type-title-s` | `text.primary` |
| `.rm-sim-stepper__label`, rótulo de `#sim-rowing` | `.rm-type-body-m` | `text.primary` |
| `#sim-watts-value`, `#sim-spm-value`, `#sim-hour-value` | `.rm-type-num-stat` (Barlow 16/700) | `text.primary` |
| `.rm-sim-ribbon__label` | **ninguna** (tamaño fijo, no escala con la letra del sistema) | `dev.simulatorText` |

Las etiquetas del catálogo llegan con su caja (`SPLIT 500m`, `PULSÓMETRO` en mayúsculas por `ToUpperInvariant`): **no** hay transformación a mayúsculas en USS (rompería `500m`).

---

## 3. Geometría por tamaño (verificada)

Grilla: ancho = ventana − 40; > 600 → 3 columnas y 1,6; si no, 2 y 1,3; separación 12 (AC-BLE-11-05). Interior de la tarjeta = celda − 32 de ancho y − 35 de alto (relleno 16 y borde 3). Ancho natural a 52 dp con cifras tabulares (celda de dígito Barlow 700 = 0,544 em = 28,3 dp; `:`/`.` 0,32 em; `m` 0,755 em): `02:13` 129,8 · `24.0` 101,5 · `150` 84,9 · `226m` 124,1 · `100:01` 158,1 · `1999` 113,2 · `123456m` 209,0.

| Perfil | Celda (dp) | Interior | `FitScale` de `123456m` / `100:01` | Qué entra en la primera vista (sin desplazar) |
|---|---|---|---|---|
| TV 411 × 914 | 179,7 × 138,2 | 147,7 × 103,2 | 0,707 / 0,934 | Todo: cabecera, 4 tarjetas, tarjeta del pulsómetro (termina en y ≈ 622 de 850 visibles). La llave queda en espacio libre |
| L640 640 × 360 | 294,0 × 226,2 | 262,0 × 191,2 | 1 / 1 | Cabecera y la primera fila; el resto desplazando |
| 641 dp (límite) | 192,3 × 120,2 | 160,3 × 85,2 | 0,767 / 1 | — (3 columnas) |
| P800 800 × 360 | 245,3 × 153,3 | 213,3 × 118,3 | 1 / 1 | Cabecera y la primera fila |
| TH 914 × 411 | 283,4 × 177,1 | 251,4 × 142,1 | 1 / 1 | Cabecera y la primera fila |
| W1280 1280 × 720 | 405,3 × 253,3 | 373,3 × 218,3 | 1 / 1 | Cabecera y las 2 filas; **la tarjeta del pulsómetro empieza en y = 667, debajo de la barra (656-720)**: en el legacy, con barra de 80, tampoco se ve. Ver CAP-04 (§7.2) |

Altura de la tarjeta del pulsómetro en TV por estado (aprox., para el `HeightTween`): sin sensor 186 · buscando con 1 fila 138 · conectado 142 (medido) · recordado 135 · conectando 113. Panel del simulador: 402 dp sin `Hora real`, 442 con (medido); en TV entra entero (tope 802); en P800 el tope es 248 (cabecera 48 + 200 desplazables: presets y los tres renglones).

Con letra 1,3× (CAP-24): `num.xl` pasa a 67,6 y en TV `02:13` queda con `FitScale` 0,875; la etiqueta de 12 pasa a 15,6 (entra); la cabecera `Remo simulado` + insignia mide 365 de 371 (entra; en 360 dp de ancho baja la insignia, §4.3); la barra queda con 2,7 dp de aire abajo (entra).

---

## 4. Componentes

### 4.1 Fondo y barra superior

| Elemento | Spec |
|---|---|
| `.rm-app`, `.rm-screen` | `bg.app`, sin borde [contrato] |
| `.rm-appbar` | alto 56; `padding-left` 16, `padding-right` 4 (un botón de texto con relleno 12 deja su texto a 16 del borde, como el legacy); fondo `bg.app`, sin línea ni sombra [contrato] |
| `.rm-appbar__title` | `.rm-type-title`, `text.primary`, una línea con "…", `flex-grow: 1` |
| Acciones | fila a la derecha, centradas en el alto de 56 |

La cinta del simulador tapa parte de la primera letra del título, igual que en `image-01.png` (paridad; mover el título haría que la pantalla "sepa" del simulador, R-06).

### 4.2 Barra de pestañas de vidrio

```
#tab-bar .rm-tab-bar                 absoluta abajo; alto 64 + área segura (padding-bottom por código)
├── .rm-tab-bar__sheen  [nuevo]      brillo superior, primer hijo, pickingMode Ignore
├── #tab-bar-pill .rm-tab-bar__pill  56 × 32, top 6, x por código
└── .rm-tab × 4 (5)                  flex 1 1 0, columna, align-items center, padding-top 6
    ├── .rm-tab__icon-box            56 × 32, centra el ícono
    │   ├── .rm-tab__state [nuevo]   56 × 32 r.full, capa de estado
    │   ├── .rm-tab__icon .rm-icon   24
    │   └── .rm-focus-ring           inset −4, r.full
    └── .rm-tab__label               margin-top 4, .rm-type-label
```

| Parte | Spec |
|---|---|
| Fondo | `surface.nav` (`#0A1622 @92 %`), **sin desenfoque**; borde superior `stroke.hairline` `border.subtle` [contrato]. Sobre `bg.app` se ve `#0A1623` |
| Brillo superior (vidrio) | degradé blanco de 32 dp desde el borde superior: textura de degradé (DN-06) teñida `text.primary`, `opacity: 0.05` (5 % en el borde, 0 a 32 dp). No toca la etiqueta (y ≥ 42) |
| Píldora | `nav.indicator` (`#00B4D8 @18 %`), `r.full`, 56 × 32, centrada en el ícono del destino elegido, detrás del ícono [contrato]. Sobre la barra y `bg.app`: `#083343` |
| Ícono | 24; no elegido `text.secondary`, elegido `accent.primary`; glifo según AC-APP-02-01 (Workout relleno al elegirlo) |
| Etiqueta | `.rm-type-label` (12/600, tracking 0,08 em), siempre visible, **debajo** de la píldora; no elegida `text.secondary`, elegida `accent.primary` (6,02:1 en el peor caso) |
| Ritmo vertical | 6 + 32 + 4 + 14,9 = 56,9 → 7,1 dp abajo (a 1,3×: 2,7 dp) |
| Área táctil | la columna entera (≥ 82 × 64) |
| *Hover* (escritorio, no elegido) | `.rm-tab__state` blanco `opacity 0.04` |
| Presionado | `.rm-tab__state` `opacity 0.08` y `.rm-tab__icon-box` escala 0,94 |
| Foco (teclado) | anillo alrededor de la caja del ícono (§4.7) |

### 4.3 Dispositivo, vista conectada: cabecera y grilla

| Parte | Spec |
|---|---|
| Desplazamiento | `#device-connected-scroll`: relleno 16 / 20 / 16 / 20 [contrato] + alto de la barra abajo, **dentro** del contenido desplazable (así el final sube por encima de la barra) |
| `#device-header` | `margin-top` 8; fila centrada (`justify-content: center`), `min-height` 32, `align-items: center`, **`flex-wrap: wrap`**: si no entra, la insignia baja a una segunda línea centrada en vez de desbordar (el legacy desbordaba). USS no tiene `gap`: la insignia lleva `margin` 4 arriba y abajo siempre (no cambia el alto de 32 en una línea) |
| Ícono | `rowing` 32, `accent.primary`, `margin-right` 12 [contrato] |
| Nombre | `.rm-type-title`, `text.primary`, `margin-right` 12, una línea con "…" y `flex-shrink: 1` |
| Insignia `#device-connected-badge` | relleno 4 / 10, radio 12, fondo `state.connectedBg` (sobre `bg.app` = `#1A3932`); ícono `bluetooth_connected` 12 `state.connected` + 4 + texto `.rm-type-body-xs` `state.connected` (8,81:1) [contrato]; alto 22,9 |
| `#metric-grid` | `margin-top` 24; posiciones y alto por código (2/3 columnas, 1,3/1,6, separación 12) [contrato] |
| `#hrm-card-slot-connected` | `margin-top` 12 [contrato]; debajo, **64 dp de aire** hasta el final del contenido, sin contar el alto de la barra (AC-BLE-11-07: los 32 + 16 del legacy, `device_screen.dart:123`, `:135`, más el relleno de 16; por ejemplo `margin-bottom` 48. El mecanismo lo fija el Architect, QA-G2-12) |

### 4.4 Tarjeta de métrica (`.rm-metric-tile`)

```
#metric-tile-<m> .rm-card .rm-metric-tile .rm-metric--<m>
├── .rm-metric-tile__glow .rm-card__glow [clase nueva]   absoluta: top 0, left 0, right 0, alto 24
├── .rm-metric-tile__header                               fila centrada
│   ├── .rm-metric-tile__icon .rm-icon                    14, margin-right 6
│   └── .rm-metric-tile__label .rm-type-label
└── .rm-metric-tile__value-box (FitBox)                   flex-grow 1, centra
    └── .rm-metric-tile__value (TabularNumber) .rm-type-num-xl
```

| Parte | Reposo | Dato nuevo (destello, §5.2) |
|---|---|---|
| Contenedor | `surface.card`, `r.card` 16, relleno 16, sin sombra [contrato] | igual |
| Acento superior | `stroke.accent` 3 en el color de la métrica **`@60 %`** (legacy `withOpacity(0.6)`): split `#A3722C`, SPM `#0A7E9D`, vatios `#A39322`, distancia `#388BAC`, pulso `#9A3D5E` sobre la tarjeta | `@100 %` (el hex de la métrica) |
| Brillo | textura de degradé (alfa `smoothstep`, 1 arriba → 0 a 24 dp) teñida con el color de la métrica (`-unity-background-image-tint-color: var(--rm-metric-…)`), radios superiores 14; `opacity` 0,084 (= 0,14 × 0,60) | `opacity` 0,14 |
| Ícono y etiqueta | color base `@100 %`; pulso `metric.hr.hud` | igual |
| Valor | `num.xl` 52, color base (pulso `metric.hr`); centrado en el espacio bajo el encabezado (como los `Spacer` del legacy); la **banda de mayúsculas** centrada en su caja de 1 em (DN-08) | igual |
| Marcadores (`--:--`, `0.0`, `0`, `0m`) | mismos colores y tamaño que el valor (paridad); el guion ocupa su avance (0,37 em) para que `--` se lea como dos guiones (DN-08) | — |
| No entra | `FitBox` achica sin tope (D-10); cuando el largo cambia, la escala sigue un resorte (§5.9) | — |

El prototipo comparó seis intensidades: con 18 % el brillo sobre la tarjeta azul se volvía una bruma gris en naranja y amarillo (mezcla de complementarios); 14 % con factor 0,6 en reposo da profundidad sin ensuciar. La tarjeta no tiene *hover* ni presión (no es accionable).

**Tarjeta PULSO** (solo con pulso FTMS > 0, AC-BLE-11-04): igual, con acento y valor `metric.hr`, ícono `favorite` **relleno** y etiqueta `metric.hr.hud`. Aparece y desaparece con la coreografía de §5.9.

### 4.5 Dispositivo, vista de búsqueda (CAP-08, CAP-09, CAP-22)

| Parte | Spec |
|---|---|
| `#device-scan-view` | relleno 16 + alto de la barra abajo; columna que estira [contrato] |
| `#device-search` | botón primario a todo el ancho (§4.7), `min-height` 48, ícono `bluetooth_searching` 18 + 8 + `deviceSearch`. Buscando (remo de prueba): `Spinner` 16 `text.primary` (blanco, [contrato]) en lugar del ícono y `deviceSearching`; como está deshabilitado se ve al 38 % |
| `#device-search-hint` | `margin-top` 12, relleno vertical 8, `.rm-type-body-xs` `text.tertiary` centrado (solo buscando) |
| `#device-empty-area` | `margin-top` 12, `flex: 1 1 0`, contenido centrado en los dos ejes y desplazable |
| Estado vacío | `bluetooth_disabled` 48 `text.primary` con `opacity: 0.24` (`#47525D` sobre `bg.app`) → 12 → `deviceNoResults` `.rm-type-body` `text.secondary` centrado (`#B6BBBF`, 8,97:1; **cambio a propósito**: el legacy lo pone en blanco al 54 %, `device_screen.dart:430-431`, y con D-ART-03 la ayuda de abajo también queda al 54 %: el título sube un escalón para que se lean dos niveles; DV-05, aceptado por el PO) → 8 → `deviceNoResultsHint` `.rm-type-body-xs` `text.tertiary` centrado, dos líneas (`#90969D`) |
| `#hrm-card-slot-scan` | `margin-top` 12, `flex: 1 1 0`, la tarjeta arriba de su mitad (paridad) |

### 4.6 Tarjeta del pulsómetro (`#hrm-card`)

Estructura y nombres de 02-tech-design §3.4, más `#hrm-glow .rm-card__glow` **[nuevo]** como primer hijo, igual que la tarjeta de métrica (AC-APP-08-02: la misma tarjeta). Acento `metric.hr @60 %` fijo (**sin destello**: el signo vital de esta tarjeta es el corazón) y brillo en reposo (0,084).

| Parte | Spec |
|---|---|
| Contenedor | ancho completo, `surface.card`, `r.card`, relleno 16, acento 3 [contrato] |
| Encabezado | fila alineada a la izquierda (paridad), `margin-bottom` 12: `#hrm-heart` `favorite` **relleno** 14 `metric.hr` + 6 + `#hrm-title` `.rm-type-label` `metric.hr.hud` |
| Corazón | conectado: `opacity` 1 (late solo con pulso > 0, §5.3); en cualquier otro estado: `opacity` 0,40 (`#6F3856` sobre la tarjeta) y quieto |
| `#hrm-body` | se reconstruye por estado; `overflow: hidden` mientras anima su alto (§5.6) |
| `#hrm-error` | `margin-top` 8, `.rm-type-body-xs` `state.dangerText` (5,20:1), hasta 3 líneas con "…". Texto suelto como el legacy, **sin** la caja de error de ART_BIBLE §3.10 ni el ícono `error` |

**Cuerpo por estado** (AC-HRM-13-03):

| Cuerpo | Spec |
|---|---|
| Sin sensor (CAP-10) | `#hrm-nosensor-text` `.rm-type-title-s` `text.primary` → 4 → `#hrm-watch-hint` `.rm-type-body-xs` `text.tertiary`, ajusta en varias líneas (3 en TV) → 12 → `#hrm-search` **tonal** con `bluetooth_searching` 18, ancho según el contenido, alineado a la izquierda |
| Buscando (CAP-11) | fila: `Spinner` 16 `accent.primary` + 12 + `#hrm-searching-text` `.rm-type-body-s` `text.tertiary`. Debajo (margin-top 4), cada `#hrm-device-<id>`: fila accionable `min-height` 56, relleno vertical 8, capa de estado con radio `r.m` que se extiende 8 dp a cada lado del texto; ícono `favorite` **contorno** 24 `metric.hr` + 16 + columna (título `.rm-type-body` `text.primary` con "…", subtítulo `SIM:HRM` `.rm-type-caption` `text.tertiary`) + `chevron_right` 18 `text.tertiary`. Sin botones ni texto de lista vacía (AC-HRM-13-11) |
| Conectando (CAP-14a) | fila: `Spinner` 16 `accent.primary` + 12 + `#hrm-connecting-text` `.rm-type-body-m` `text.primary` (ajusta). Sin botones |
| Conectado (CAP-12) | fila `align-items: center`: a la izquierda (crece, `min-width` 0) `#hrm-connected-name` `.rm-type-body-m` `text.secondary`, una línea con "…" → 4 → `#hrm-bpm-box` (`FitBox` alineado a la izquierda) con `#hrm-bpm` `num.xl` `metric.hr` (`142` o `--`) + 6 + `#hrm-bpm-unit` `.rm-type-body-m` **`metric.hr.hud`** (4,61:1; se ve un poco más clara que el número, como la etiqueta de PULSO; el legacy, rosa al 70 % = 2,45:1: DV-04), **misma línea de base** (DN-09). A la derecha, `#hrm-disconnect` botón de texto peligro con `bluetooth_disabled` 18 |
| Recordado (CAP-13, CAP-14b) | `#hrm-remembered-text` `.rm-type-body-m` `text.secondary` (ajusta) → 12 → acciones en fila que ajusta, separación 8 horizontal y 4 vertical: `#hrm-retry` **tonal** con `refresh` 18 (`Conectar`, al 38 % mientras reintenta), `#hrm-search` **contorno** sin ícono (`Buscar otro`), `#hrm-forget` **texto** `accent.primary` (`Olvidar`) |

### 4.7 Botones y controles comunes

Todo botón (`RmButton`) tiene, en este orden: `.rm-button__state` **[nuevo]** (capa de estado absoluta, `inset 0`, mismo radio, `pickingMode Ignore`), ícono, etiqueta y `.rm-focus-ring` (con `.rm-focus-ring__halo` **[nuevo]**). Alturas de contrato como **`min-height`**; etiqueta `.rm-type-button`, `white-space: normal`, centrada (a 1,3× los textos largos pasan a dos líneas y el botón crece) (DN-20).

| Variante | `min-height` | Relleno horizontal | Radio | Fondo | Ícono y texto | Borde |
|---|---|---|---|---|---|---|
| Primario (`#device-search`) | 48 | 16 | `r.l` 12 | `accent.primary`; presionado `accent.primaryPressed` | `text.onAccent` (7,64 / 5,40) | — |
| Tonal (`hrm-search` sin sensor, `hrm-retry`) | 44 | 16 | `r.l` | `accent.tonalBg` (`#173E57` sobre la tarjeta) | `accent.onTonal` (5,88) | — |
| Contorno (`Buscar otro`, fallas del panel) | 44 | 16 (12 en el panel) | `r.l` | transparente | `text.primary` | `stroke.hairline` `border.strong` |
| Texto (`Olvidar`, `Hora real`) | 40 | 12 | `r.l` | transparente | `accent.primary` (5,60 tarjeta · 5,07 panel) | — |
| Texto peligro (`#device-disconnect`, `#hrm-disconnect`) | 40 | 12 | `r.l` | transparente | `state.dangerText` (6,54 `bg.app` · 5,20 tarjeta) | — |
| Ícono (`sim-close`, ± del panel) `.rm-icon-button` **[nuevo]** | caja 48 × 48 | — | capa de estado círculo de 40 | transparente | ícono 24; `text.primary` (cerrar `text.secondary`) | — |

Ícono dentro del botón: 18, separación 8. `flex-shrink: 0` en botones, íconos y filas.

| Estado | Spec |
|---|---|
| *Hover* (escritorio) | `.rm-button__state` blanco (`text.primary`) `opacity 0.04` (ART_BIBLE decía 6 %: con 6 % el texto peligro sobre la tarjeta baja a 4,34) |
| Presionado | capa `opacity 0.08` y escala 0,97 (primario: fondo `accent.primaryPressed`, sin capa) |
| Deshabilitado | `opacity 0.38` en todo el botón, sin *hover* ni presión [contrato]; cambia en el acto (sin transición) |
| Foco (solo con `.rm-keyboard-nav`) | `.rm-focus-ring`: absoluto `inset −4`, borde `stroke.focus` 2 `focus.ring`, radio = radio del control + 4; `.rm-focus-ring__halo`: `inset −2` respecto del anillo, borde `stroke.focusHalo` 4 `focus.ringHalo`, radio + 4. Aparece sin animación. Va también en pestañas, filas, chips, la llave y el interruptor |

**Chip** (`#sim-preset-*`, `.rm-chip`): `min-height` 32, relleno 0 / 12, `r.m` 8, borde 1 `border.default`, etiqueta `.rm-type-button` `text.primary`, capa de estado `r.m`; separación 8 (fila que ajusta). Sin estado "elegido" (paridad: el legacy usa `ActionChip`).

**Interruptor** (`#sim-rowing`, `.rm-switch-row`): renglón `min-height` 48, rótulo `.rm-type-body-m` `text.primary` que crece; pista `.rm-switch__track` **[nuevo]** 40 × 24 `r.full` (12) a la derecha, con `.rm-switch__thumb` **[nuevo]** de 20 × 20 (radio 10) en `left 1, top 1`.

| | Pista | Perilla |
|---|---|---|
| Apagado | `bg.sunken` + borde 1 `border.strong` | `text.secondary`, `scale 0.8` (16 dp), sin desplazamiento |
| Encendido (`:checked`) | `accent.primary` (5,07 contra el panel) sin borde visible (borde del color de la pista) | `text.onAccent` (7,64), `scale 1`, `translate 15px 0` |

**Spinner**: arco de 270° (falta 90°), 16 × 16, trazo 2 con extremos redondeados, una vuelta cada 900 ms lineal. Color: `accent.primary` en la tarjeta; `text.primary` en el botón primario [contrato].

**Barras de desplazamiento** (todas las `ScrollView` de la app; superpuestas por contrato): sin flechas (`.unity-scroller__low-button` / `__high-button` ocultos), carril transparente, *dragger* de 4 dp de ancho `r.full` `border.strong`, con margen derecho 2. Visibilidad automática.

### 4.8 Cinta SIMULADOR (`#sim-ribbon`)

| Parte | Spec |
|---|---|
| Caja | 80 × 12, `left −16`, `top 18`, `rotate −45deg` alrededor de su centro → centro en (24, 24): la geometría del `Banner topStart` de Flutter. En la esquina superior izquierda de la capa del simulador, que cubre la ventana de la app, **sin sumar insets propios**: el `Banner` del legacy envuelve toda la app y va en la esquina de su vista (`main.dart:108-110`, `simulator_overlay.dart:33-36`). Si la app dibuja o no detrás de la barra de estado de Android lo fija la regla única de área segura (QA-G2-06, Architect); la cinta la sigue sin regla aparte. En Windows los insets son 0 |
| Fondo | `dev.simulator` (5,50:1 contra `bg.app`) [contrato] |
| Texto | `SIMULADOR`, Inter **700** (no hay Inter 800 en el repo), **10 dp**, `letter-spacing 0`, `dev.simulatorText` (6,11:1), centrado. Mide 60,2 dp: entra entero en la parte visible de la franja (60,7 dp a la altura de las mayúsculas). ART_BIBLE decía 11/800 con 0,1 em: mediría 76 dp y la ventana cortaría la S y la R |
| Eventos | `pickingMode Ignore`; siempre encima de todo, sin animación |

### 4.9 Llave y panel del simulador

| Parte | Spec |
|---|---|
| `#sim-open` | 40 × 40, `r.l` 12, fondo `surface.overlay`, borde 1 `border.default`, ícono `build` 24 `accent.primary` (5,07); `right 16`, `bottom` = alto real de la barra (64 + inset inferior que consume la barra, QA-G2-06) + 16 [contrato]. *Hover*/presión: capa de estado y escala 0,94. Es discreta a propósito: la misma superficie que el panel que abre |
| `#sim-panel` | 280 de ancho, alto máximo por código [contrato]; fondo `surface.overlay`, `r.card` 16, borde 1 `border.default` (sin sombra posible, el borde lo separa del tablero), `overflow: hidden` (recorta el desplazamiento a las esquinas) |
| Cabecera | fuera del desplazamiento [contrato], alto 48, relleno 0 / 12, borde inferior 1 `border.subtle`; `Simulador` `.rm-type-title-s` `text.primary` + `#sim-close` ícono 24 `text.secondary` (6,98) en caja de 48, alineada con la columna de los `+` |
| Contenido | relleno 4 / 12 / 12 / 12 (ancho útil 254) |
| Presets | fila que ajusta, `margin` 4 / 0 / 8 / 0; chips de §4.7 (3 × 68 + 16 = 220 ≤ 254) |
| Renglones ± | `min-height` 48: rótulo `.rm-type-body-m` que crece (`flex 1 1 0`, ajusta en 2 líneas a 1,3×) + `−` 48 + valor **64** (DN-19; legacy 72) `.rm-type-num-stat` centrado + `+` 48. Con 64, `Hora escena` (84,6 dp) tiene 94 dp; con 72 tenía 0,7 dp de aire |
| `#sim-hour-real` | botón de texto `accent.primary`, alineado a la derecha bajo el valor de la hora (`align-self: flex-end`); solo con hora forzada |
| `#sim-rowing` | interruptor de §4.7 |
| `#sim-drop-rower`, `#sim-drop-hrm` | contorno a todo el ancho, relleno 12, ícono 18 (`link_off`, `heart_broken`) + 8 + texto, `margin-top` 8 |

Sin estado "límite" en ± (paridad: en `500 W` el `+` sigue habilitado y no hace nada). En TV el panel (402 dp) cubre la mitad baja de la tarjeta del pulsómetro y deja libre la grilla (CAP-19).

### 4.10 Pestañas provisorias (CAP-21)

Barra superior con el título del catálogo (§4.1) y, centrado en el área entre la barra superior y la de pestañas, `.rm-placeholder__icon` 56 `text.primary` `opacity 0.24` (`#47525D`), glifo **sin elegir** de su pestaña [contrato]. Nada más.

---

## 5. Motion

### 5.0 Quién mueve qué

| Animación | Mecanismo | Duración y curva | Con reducir movimiento |
|---|---|---|---|
| Rodillo de dígitos | código (`TabularNumber` + `DigitRoll`) | 180, §5.1 | fundido de 80 lineal |
| Destello del acento y brillo | código (`AccentFlash`) | 300 `ease.pulse` | igual |
| Latido | código (`HeartbeatAnimator`) | 60/bpm, §5.3 | quieto |
| Corazón pleno ↔ 40 % | código | `dur.fast` 160 `ease.move` | igual |
| Píldora | código (`CriticalSpring` ω 22) | ≈ 260 | salta al destino |
| Ícono de la pestaña (escala) | código | 260, §5.4 | sin escala |
| Color del ícono y la etiqueta de la pestaña | código (DN-11) | 160 `ease.move` | igual |
| Contenido de la pestaña | código (`TabTransition`) | 0-280, §5.4 | fundido sin desplazamiento |
| Vista conectada ↔ búsqueda | código (DN-12) | §5.5 | fundido |
| Alto del cuerpo del pulsómetro | código (`HeightTween`) | `dur.slow` 360 `ease.move` + fundido 160 | sin animación |
| Panel y llave | código (DN-14) | §5.7 | fundido |
| Escala de `FitBox` al cambiar el largo | código (`CriticalSpring` ω 12) | ≈ 490 | en el acto |
| Tarjeta PULSO y alto de la grilla | código | §5.9 | fundido |
| Spinner | código | 900 por vuelta, lineal | sigue (indica espera) |
| *Hover*, presionado, interruptor | USS (`Motion.uss`) | §5.8 | sin escala |

Todo lo de código pide 60 fps a `FramePacing` mientras corre (el corazón, mientras late) y no anima nada antes del **primer cuadro presentado**: el estado inicial se aplica tal cual (sin "encender" el tablero al abrir la app, sin destellos de arranque, AC-APP-01-07).

### 5.1 Rodillo de dígitos — el número que se ve es el real

Por cada celda que cambia (`DigitRollPlan`, alineado a la derecha; las celdas nuevas cuentan como cambiadas). `u = t / 180 ms`, `E` = `ease.enter` (`cubic-bezier(0.33, 1, 0.68, 1)`), `dir` = +1 si el valor subió, −1 si bajó (un marcador `--` vale 0: al apagarse los dígitos bajan, al encenderse suben).

| Glifo | Desplazamiento vertical | Opacidad |
|---|---|---|
| Saliente | `−dir · 0,35 em · E(u)` | `1 − E(min(1; u / 0,6))` (desaparece a los 108 ms) |
| Entrante | `+dir · 0,35 em · (1 − E(u))` | `E(clamp((u − 0,15) / 0,85))` (empieza a los 27 ms) |

Invariantes (AC-BLE-11-11, ANIMATION_BIBLE §0.1.4): en `u ≥ 1` la celda entrante está en 0 con opacidad 1 y la saliente en 0 → se ve **exactamente** el texto de la lectura; el texto nuevo es el de la lectura desde el cuadro 0 (solo se anima su posición y opacidad); nunca se dibuja un dígito que no sea el viejo o el nuevo; si llega otra lectura antes de `u = 1`, el rodillo salta a su final y arranca el siguiente. Las celdas que no cambian no se mueven ni un subpíxel (AC-APP-05-04).

Si cambia el largo (`9m` → `13m`), el ancho del número interpola en 180 ms con la misma `E` y las celdas quedan pegadas a la derecha: el número no salta media celda al recentrarse (DN-08). Celdas que se van (largo menor): solo salida.

**Contadores**: en la It. 2 no hay ninguno. Ninguna métrica en vivo "cuenta" (no pasa de `150` a `160` por `151…159`); los únicos valores que se interpolan son visuales (desplazamiento, opacidad, escala, posición de la píldora, alto de la tarjeta). Los totales que cuentan (ANIMATION_BIBLE §2.5) llegan en la It. 4b.

Reducir movimiento: fundido cruzado en el lugar, 80 ms lineal (saliente `1 − u`, entrante `u`).

### 5.2 Destello del acento ("dato nuevo")

Solo en las tarjetas cuyo **texto** cambió con la lectura (si `24.0` sigue en `24.0`, nada se mueve ni brilla). Factor `k`: `1,0` en el cuadro de la lectura (el mismo cuadro en que arranca el rodillo) y baja a `0,60` en 300 ms con `ease.pulse`: `k(t) = 1 − 0,4 · E_pulse(t / 300)`. Acento = color de la métrica con alfa `k`; brillo = `opacity 0,14 · k`. Una lectura nueva reinicia en 1,0. La tarjeta del pulsómetro no destella. Con reducir movimiento, igual (es opacidad).

### 5.3 Latido del corazón (AC-HRM-13-12)

`T = 60 / bpm`. Cada latido arranca en `τ = 0` y la escala (pivote en el centro de la caja del glifo) es la **suma** de dos golpes:

| Golpe | Sube | Baja | Pico |
|---|---|---|---|
| Principal | 0 → 90 ms, `ease.enter` | 90 → 290 ms, `ease.move` | +0,16 |
| Segundo | 170 → 240 ms, `ease.enter` | 240 → 390 ms, `ease.move` | +0,06 |

`s(τ) = 1 + 0,16 · p₁(τ) + 0,06 · p₂(τ)`; a los 170 ms el principal todavía vale ≈ 0,74, así que el "lub-dub" se lee como un golpe y un rebote (≈ 1,07 a los 240 ms). **Compresión**: si `T < 420 ms` (bpm > 142,9) todos los tiempos se multiplican por `T / 420 ms` (a 180 bpm, 0,79); los picos no cambian. Así el latido nunca se encima con el siguiente. A 142 bpm `T = 422,5 ms`: sin compresión, reposo de 32 ms.

- Arranca: el primer latido empieza en el cuadro en que llega el primer pulso > 0 (el corazón "despierta" con el número).
- Cambio de bpm: el período nuevo vale desde el latido siguiente (sin saltos de fase).
- Para (bpm 0, desconexión o caída): sin latidos nuevos; la escala vuelve a 1 en 120 ms `ease.move` desde donde esté; y si se desconectó, la opacidad baja a 0,40 en 160 ms.
- Reducir movimiento: escala 1 siempre; opacidad 1 conectado (con o sin pulso), 0,40 si no.
- 120 bpm → 20 latidos en 10 s (19 a 21 según la fase de inicio).

### 5.4 Cambio de pestaña (VID-02)

| t (ms) | Qué |
|---|---|
| 0 | el glifo del destino nuevo cambia a su variante elegida y el anterior a la suya; arranca todo lo demás |
| 0 → ≈ 260 | píldora: `CriticalSpring` ω 22 en x hacia el centro del destino, desde su posición **y velocidad** actuales (un segundo toque a mitad de camino no la frena en seco); sin rebote |
| 0 → 90 → 260 | ícono elegido: escala 1 → 1,12 (`ease.enter`) → 1 (`ease.move`); el que se deja no se escala |
| 0 → 160 | colores: el ícono y la etiqueta del nuevo van de `text.secondary` a `accent.primary` y los del anterior al revés (`ease.move`, por código) |
| 0 → 120 | contenido saliente: opacidad 1 → 0 y x 0 → −12·dir (`shift.m`, `ease.exit`) |
| 80 → 280 | contenido entrante: opacidad 0 → 1 y x +12·dir → 0 (`ease.enter`) |

`dir` = +1 si el destino está a la derecha. Tocar el elegido no hace nada. Reducir movimiento: la píldora está en el destino en el cuadro 0, sin escala del ícono, contenido con los mismos tiempos pero solo opacidad.

### 5.5 Vista conectada ↔ búsqueda ("apagar" y "encender", VID-03)

Las dos vistas están superpuestas debajo de la barra superior (DN-12); la saliente ignora eventos mientras sale.

| Transición | t (ms) | Qué |
|---|---|---|
| Conectada → búsqueda ("apagar") | 0 → 120 | vista conectada: opacidad 1 → 0 y escala 1 → 0,985 (origen arriba al centro), `ease.exit` |
| | 120 | la tarjeta del pulsómetro pasa a `#hrm-card-slot-scan` |
| | 120 → 360 | vista de búsqueda: opacidad 0 → 1 y y +8 → 0 (`shift.s`), `ease.enter` |
| Búsqueda → conectada ("encender") | 0 → 120 | vista de búsqueda: opacidad 1 → 0, `ease.exit` |
| | 120 | desplazamiento de la vista conectada en 0; la tarjeta pasa a `#hrm-card-slot-connected` |
| | 120 → 360 | cabecera: opacidad 0 → 1 |
| | 120 + 35·i → +240 | tarjeta `i` de la grilla: opacidad 0 → 1, y +12 → 0 (`shift.m`), `ease.enter`; en su arranque, destello del acento (§5.2) como una luz que se prende |
| | 120 + 35·n → +240 | tarjeta del pulsómetro, igual, sin destello (n = cantidad de tarjetas: termina a los 500 ms con 4) |

La grilla entra en ceros (`--:--`, `0.0`, `0`, `0m`) y la primera lectura (≤ 1 s) los hace rodar hacia arriba con la distancia que siguió sumando. Si el estado cambia en medio de una transición, la actual salta a su final y arranca la otra. Reducir movimiento: salida 0-120 y entrada 120-240, solo opacidad, sin escalonado (el destello sí).

### 5.6 Cuerpo de la tarjeta del pulsómetro

En el cuadro del cambio de estado se arma el cuerpo nuevo con opacidad 0; `#hrm-body` anima su alto del valor anterior al nuevo en `dur.slow` 360 `ease.move` con `overflow: hidden` (si crece, el contenido se "despliega"; si achica, se recoge) y el contenido entra con opacidad 0 → 1 en 160 `ease.enter`. Al terminar, alto automático. La línea de error entra igual. El corazón cambia de opacidad en 160 (§5.3). Reducir movimiento: sin animación (02-tech-design §3.4).

### 5.7 Llave y panel del simulador

- **Abrir:** en el cuadro 0 la llave se oculta y el panel aparece en su lugar: opacidad 0 → 1 en 160 y escala 0,92 → 1 en 240 (`ease.enter`, origen abajo a la derecha: crece desde la llave).
- **Cerrar:** el panel se oculta **en el acto** (así LT-26 y LT-27 ven "ya no se ve `280 W`" sin esperar) y la llave vuelve con opacidad 0 → 1 y escala 0,85 → 1 en 160 `ease.enter`.
- Cambios de valores en el panel: en el acto (es un control; el efecto se ve en el tablero, que sí anima).
- Reducir movimiento: solo opacidad (panel 160, llave 120).

### 5.8 Micro-interacciones en `Motion.uss` (reloj real)

| Propiedad | Hacia el estado | Vuelta |
|---|---|---|
| `opacity` de `.rm-button__state` / `.rm-tab__state` (*hover* 0,04, presionado 0,08) | `var(--rm-dur-micro)` `var(--rm-ease-enter)` | `var(--rm-dur-fast)` `var(--rm-ease-exit)` |
| `scale` del botón presionado (0,97; pestaña y llave 0,94) | 90 `ease-out-cubic` | 240 `ease-out-cubic` (≈ `spring.snappy`) |
| `background-color` del primario presionado | 90 | 160 |
| Interruptor: `translate`, `scale` y `background-color` de la perilla, `background-color` y `border-color` de la pista | `var(--rm-dur-fast)` `var(--rm-ease-move)` | igual |

Sin transición: `:disabled`, el anillo de foco, `display`. Con `.rm-reduce-motion` no hay escala al presionar (se queda la capa de estado). La duración "hacia el estado" se declara en la regla del estado (`:hover`, `:active`, `:checked`) y la de vuelta en la regla base, como en CSS.

### 5.9 Otros reacomodos

- **`FitBox`**: si un valor nuevo cambia la escala necesaria (`9999m` → `10000m` en 2 columnas: 1 → 0,82), la escala sigue un `CriticalSpring` ω 12 (`spring.smooth`, 98 % en ≈ 0,49 s). El primer cálculo, un cambio de tamaño de ventana o de letra: en el acto. Reducir movimiento: en el acto.
- **Tarjeta PULSO** (remo de prueba): al aparecer, el alto de la grilla anima en `dur.slow` `ease.move` y la tarjeta entra como en §5.5 (240, y +12, opacidad); al irse, opacidad → 0 en 120 y después el alto se recoge en 200 (ANIMATION_BIBLE §2.3).
- **2 ↔ 3 columnas** (rotar, redimensionar): sin animación propia, como el legacy (la ventana manda; en Android el sistema anima la rotación).

### 5.10 Reducir movimiento (AC-APP-05-08)

Se activa con `.rm-reduce-motion` en `#app-root` (USS) y `MotionSettings.ReduceMotion` (código). Resumen: rodillo → fundido de 80; corazón quieto (pleno conectado, 40 % si no); pestañas, vistas, panel y PULSO → solo opacidad con los mismos tiempos; alto de la tarjeta y `FitBox` → en el acto; sin escalas al presionar. No cambian: los datos, cuándo se actualizan, el destello del acento, los fundidos de color y el spinner. Una captura fija con y sin reducir movimiento es **idéntica** (AUX de 02-tech-design §4.3).

---

## 6. Rendimiento de la UI de la It. 2

- Elementos: tarjeta de métrica 7 (contenedor, brillo, encabezado, ícono, etiqueta, caja, número); grilla de 5 tarjetas = 35; tarjeta del pulsómetro ≤ 20; barra ≤ 26; capa del simulador ≤ 40 con el panel abierto. Botón = 5 elementos (capa, ícono, etiqueta, anillo, halo).
- Una sola textura nueva (degradé 1 × 64, generada al arrancar, 256 B) compartida por los brillos y el brillo de la barra. Sin materiales ni *shaders* nuevos.
- `overflow: hidden` (máscara) solo en el panel y en `#hrm-body` mientras anima su alto.
- Por cuadro, sin asignaciones (02-tech-design §3): el rodillo y el latido cambian `translate`/`scale`/opacidad o vértices de `TabularNumber`; el ancho del número cambia solo durante los 180 ms de un cambio de largo, en un hijo absoluto del `FitBox` (no reacomoda a los vecinos).

---

## 7. Capturas: cómo se toman y qué tiene que verse

### 7.1 Reglas de toma (para el arnés, DN-21)

1. **Foto fija** = `MotionDriver` sin animaciones finitas en curso (solo latido y spinner), **corazón en reposo** (escala 1 ± 0,002) y **ninguna transición de USS en curso** (contar `TransitionRunEvent` / `TransitionEndEvent` / `TransitionCancelEvent`; tope 1 s de tiempo real). El arnés avanza de a 1/60 s hasta cumplirlo, **sin cruzar la lectura siguiente**: con una lectura en `N` s, la toma cae en `[N + 0,55; N + 1)` s (la animación finita más larga, el "encender", dura 500 ms; a 142 bpm siempre hay un inicio de latido en esa ventana de 450 ms).
2. **Videos**: reloj de prueba a 30 fps; todo lo que se ve moverse es por código (§5.0), así que salen idénticos en cada corrida.
3. Sondas de color con `GammaRt` (la tubería de la app). Con `Overlay` los colores translúcidos cambian (§8.3) y se anotan aparte.

### 7.2 Visto bueno por captura (aspecto; QA verifica además lo funcional de la spec §7.3)

Sondas: "punto" = píxel del fondo lejos de texto; "trazo" = color dominante dentro del elemento. Tolerancia ±1 por canal salvo que diga otra cosa; trazos ±2.

| ID | Qué miro para dar el visto bueno | Sondas (GammaRt) |
|---|---|---|
| CAP-01 | Jerarquía de tablero: cuatro números grandes con su luz superior; ninguno achicado; cifras sin "bailar" respecto de CAP-02; cabecera centrada con la insignia a 12 del nombre; tarjeta del pulsómetro con el corazón al 40 % y el botón tonal; llave discreta en espacio libre a 16 de la barra; píldora centrada bajo `bluetooth_connected`; la cinta solo tapa la primera letra del título; nada pegado a los bordes (20 a los costados) | fondo `#0D1B2A` · tarjeta `#1A2E45` · barra superior `#0D1B2A` · barra de pestañas (zona de etiquetas, entre destinos) `#0A1623` · píldora (5 dp sobre su borde inferior) `#083343` · acentos en reposo `#A3722C` / `#0A7E9D` / `#A39322` / `#388BAC` (±2) · trazos de valor `#FF9F1C` `#00B4D8` `#FFD60A` `#4CC9F0` · insignia `#1A3932` · tonal `#173E57` · cinta en (27, 27) dp `#FF5722` · llave (6 dp dentro de su borde izquierdo) `#1E3550` · corazón (trazo) `#6F3856` |
| CAP-02 | Mismo layout que CAP-01 con textos en inglés: `HEART RATE SENSOR` entra en una línea; `WATTS`/`DISTANCE` centrados; barra sin cortes | como CAP-01 |
| CAP-03 | 2 columnas con tarjetas más anchas (294 × 226): el número centrado bajo el encabezado, el brillo igual de alto (24); el desplazamiento corta limpio bajo la barra superior | fondo, tarjeta |
| CAP-04 | Dos tomas (DN-22, **adoptado**: 01-po-spec §7.3). En W1280 la tarjeta del pulsómetro empieza en y ≈ 666,7, bajo la barra (656-720; también en el legacy). **CAP-04a** (desplazamiento 0): cabecera, `SPLIT 500m` `SPM` `VATIOS` y `DISTANCIA` sola a la izquierda, en 3 columnas. **CAP-04b** (desplazada **hasta el final**): `DISTANCIA` entera (y ≈ 185-438) y la tarjeta del pulsómetro entera por encima de la barra (y ≈ 450-592) con corazón pleno, `142` y `bpm` rosa claro en la misma línea de base y `Desconectar` centrado en alto; debajo, 64 dp de aire hasta la barra (AC-BLE-11-07). La primera fila queda cortada por la barra superior: es lo esperado | 04a: proporción 1,6 · 04b: trazo del número `#EF476F`; trazo de `bpm` `#FF5A7E` (±3); línea de base de `bpm` = la de `142` ± 0,5 dp; borde inferior de la tarjeta a 64 ± 1 dp del borde superior de la barra |
| CAP-05 | 3 columnas (283 × 177) en horizontal; cabecera y primera fila visibles; barra completa | fondo, tarjeta |
| CAP-06 | Quinta tarjeta PULSO con ícono `favorite` **relleno**, etiqueta `#FF5A7E` y número `#EF476F` (la etiqueta se ve un poco más clara que el número: es a propósito, AA) | trazo del valor `#EF476F`; trazo de la etiqueta `#FF5A7E` (±3) |
| CAP-07 | `123456m` achicado (≈ 0,71) y centrado, sin tocar los bordes interiores; `100:01` apenas achicado (0,93); los dos con el mismo color y peso | `FitScale` < 1 donde corresponde |
| CAP-08 | Vista de búsqueda limpia: botón primario cian con texto oscuro; estado vacío centrado en su mitad; tarjeta del pulsómetro arriba de la otra mitad; sin `Desconectar` | botón `#00B4D8` · ícono vacío (trazo) `#47525D` · título vacío (trazo) `#B6BBBF` (±3, DV-05) · ayuda (trazo) `#90969D` (±3) |
| CAP-09 | `Buscar dispositivos` al 38 % (se lee deshabilitado, no roto); la fila del sensor dentro del área desplazable; barras de desplazamiento finas (4 dp), sin flechas | botón deshabilitado `#08556C` |
| CAP-10 | Corazón al 40 %, título rosa claro, `Sin sensor` en blanco 16/600, la pista en 3 líneas al 54 %, `Buscar` tonal (cian claro sobre cian oscuro) a 12 de la pista | tonal `#173E57`; texto tonal (trazo) `#38CBEF` (±3); pista (trazo) `#969FA9` (±3) |
| CAP-11 | Spinner cian alineado con el texto; fila con corazón de contorno, título blanco, `SIM:HRM` chico, *chevron* gris claro a la derecha; aire de 8 arriba y abajo de la fila | — |
| CAP-12 | a) corazón pleno quieto, `--` como dos guiones separados en rosa, `bpm` en rosa claro y alineado (nunca blanco); b) `142` en el mismo lugar que `--` (alineado a la izquierda) | trazo del número `#EF476F` · trazo de `bpm` `#FF5A7E` (±3) |
| CAP-13 | `Pulsómetro simulado · reconectando...` sereno (gris claro, sin rojo); `Conectar` tonal al 38 %, `Buscar otro` con borde, `Olvidar` cian; corazón al 40 % | — |
| CAP-14 | a) spinner + texto sin botones; b) los tres botones habilitados; c) y d) error rojo claro a 8 de `Buscar`, d) cortado en la 3.ª línea con "…" | error (trazo) `#FF7272` |
| CAP-15 | `Buscar` tonal al 38 % | — |
| CAP-16 | Panel con la superficie, el radio y el borde de la app; cabecera con línea fina; `Watts` `Ritmo` `Hora escena` en una línea cada uno; valores en Barlow centrados; contenido que sigue abajo, desplazable | panel `#1E3550` |
| CAP-17 | `14:00`, `Hora real` a la derecha bajo la hora, interruptor encendido (pista cian, perilla oscura a la derecha), botones de falla a todo el ancho con ícono | pista `#00B4D8` |
| CAP-18 | Panel entero (402 dp) anclado abajo a la derecha, 16 sobre la barra; la grilla libre arriba; nada genérico de Unity (ni flechas de desplazamiento ni casillas) | panel `#1E3550` |
| CAP-19 | Grilla con `--:--`, `0.0`, `0` en sus colores (sin achicar), la distancia congelada; interruptor apagado (pista oscura con borde, perilla gris chica a la izquierda) | pista apagada `#08131F` |
| CAP-20 | Para cada destino: píldora centrada bajo el ícono elegido, etiqueta debajo (nunca adentro), resto en gris; Workout con `timer` relleno; brillo superior apenas perceptible; e) en inglés igual de centrado | barra `#0A1623` · píldora `#083343` |
| CAP-21 | Título y un único ícono grande apagado, centrado entre las dos barras | ícono (trazo) `#47525D` |
| CAP-22 | Como CAP-08 sin cinta ni llave; en W1280 el estado vacío centrado en su mitad | fondo, botón |
| CAP-23 | (de QA) | §4.4 de 02-tech-design |
| CAP-24 | Textos más grandes sin cortes; la cabecera sigue en una línea en TV; los números achicados lo justo; la etiqueta de la barra con su aire abajo | — |
| CAP-25 | 5 destinos de 82 dp; `Dispositivo` (75 dp) entra en una línea; píldora centrada | — |
| VID-01 | Solo **ruedan las celdas que cambian**: con el simulador sin ruido, la distancia (1 o 2 últimas cifras por segundo) y el pulso si cambia; `02:13`, `24.0` y `150` quietos, sin destello. Cada rodillo se asienta antes de 180 ms y su destello antes de 300; el corazón late doble a 142 (≈ 23-24 latidos en 10 s), sin tirones en el segundo en que llega el dato | — |
| VID-02 | Píldora que se desliza y frena sin rebote (≈ 0,26 s), el ícono que "late" una vez, el color que funde, el contenido que entra 12 dp desde el lado del toque; la grilla de Dispositivo al volver, al día | — |
| VID-03 | El tablero se "apaga" en 120 ms, entra la búsqueda; a los 3 s se "enciende" con las tarjetas escalonadas (35 ms) y sus acentos que se prenden, en ceros; al segundo, los dígitos suben a los valores y la distancia sigue (no vuelve a 0) | — |
| AUX | Las de reducir movimiento: idénticas a las fijas normales | diferencia 0 |

---

## 8. Contraste verificado

### 8.1 Pares nuevos de la It. 2 (para `DesignerPairs`)

| # | Frente | Fondo | Umbral | Valor |
|---|---|---|---|---|
| N1 | etiquetas del tablero (split · SPM · vatios · distancia) | tarjeta + brillo en el borde superior de la mayúscula, en el destello | 4,5 | 6,51 · 5,42 · 9,36 · 6,89 (calculado con un pico de 18 %, peor que el 14 % elegido) |
| N2 | `metric.hr.hud` (etiqueta PULSO y título) | tarjeta + brillo rosa en el destello (pico de 18 %) | 4,5 | 4,54 (sin brillo 4,61) |
| N3 | `accent.onTonal` | `accent.tonalBg` sobre tarjeta · ídem + *hover* 4 % | 4,5 | 5,88 · 5,24 |
| N4 | `accent.primary` (botón de texto) | tarjeta + *hover* · panel + *hover* | 4,5 | 4,98 · 4,50 |
| N5 | `state.dangerText` (botón peligro) | tarjeta + *hover* · `bg.app` + *hover* | 4,5 | 4,62 · 5,91 |
| N6 | `state.connected` | `state.connectedBg` sobre **`bg.app`** (la insignia está sobre el fondo, no sobre la tarjeta) | 4,5 | 8,81 |
| N7 | `text.tertiary` · `text.secondary` (ayudas · `deviceNoResults`, DV-05) | `bg.app` | 4,5 | 5,83 · 8,97 |
| N8 | `accent.primary` (llave, interruptor, `Hora real`) | `surface.overlay` | 3 (gráfico) / 4,5 (texto) | 5,07 |
| N9 | `text.secondary` (perilla apagada) | `bg.sunken` | 3 | 9,40 |
| N10 | `accent.primary` (ícono elegido) | píldora sobre barra sobre blanco (peor) · + brillo de la barra | 3 | 4,41 · 3,95 |
| N11 | etiqueta elegida / no elegida | barra sobre `bg.app` | 4,5 | 7,38 · 9,24 (peor caso blanco: 6,02 · 8,00, T7 de la It. 1) |
| N12 | `focus.ring` | `bg.app` · tarjeta · panel | 3 | 7,06 · 5,60 · 5,07 |
| N13 | `metric.hr.hud` (unidad `bpm`, 14/400) | tarjeta (sin brillo: la unidad queda lejos de los 24 dp del borde superior) | 4,5 | 4,61 (`metric.hr` daría 3,81; el rosa al 70 % del legacy, 2,45). QA-G2-08, DV-04 |

### 8.2 Excepciones (no se testean como umbral)

- **Presionado** (capa 8 %, dura lo que el toque): `accent.primary` sobre panel 3,98 y `state.dangerText` sobre tarjeta 4,07. Con *hover* (persistente) todos pasan (N4, N5).
- **`state.dangerText` sobre panel con *hover*** 4,17: no hay botones de peligro en el panel.
- Deshabilitado (38 %), corazón al 40 % (1,56), ícono vacío y de las pestañas provisorias (2,17), acento en reposo (split 3,29, SPM 2,96, pulso 2,10: el estado lo dicen el valor y la etiqueta), pista apagada y bordes de chip y panel (≈ 1,5): decorativos o exentos.

### 8.3 Si S-B1 cae a `Overlay` (composición lineal)

Tres pares se rompen: etiqueta elegida de la barra con texto blanco debajo 3,11; `accent.primary` sobre el tonal 3,23; ícono sobre la píldora 2,25. Respaldo (cambio de tokens, no de pantallas) hasta la 4a: `surface.nav` a `@98 %` (5,50), `accent.tonalBg` a `@8 %` (con `accent.onTonal`: 5,27 en lineal). La insignia queda en 5,38. Lo aplica WP-D si S-B1 elige `Overlay`, y se repiten CAP-01, CAP-10 y CAP-20.

---

## 9. Pedidos (sin cambiar comportamiento ni arquitectura)

| ID | Para | Qué | Por qué |
|---|---|---|---|
| DN-01 | Coordinador / Designer | Agregar los 5 tokens de §2.1 a `Tokens.cs` **antes de WP-A** (paso **WP-D0**, solo datos) y regenerar `Tokens.uss`; así `Typography.uss` tiene `.rm-type-body-m`, `-body-xs` y `-button` desde WP-A y WP-B los usa. Alcance de WP-D0 (para el anexo de QA-G2-03): toca solo `Assets/RowMate/Core/Design/Tokens.cs` (3 `TypeScaleToken` y 2 `ColorToken`, ninguno existente cambia) y `Assets/RowMate/Presentation/Styles/Tokens.uss` (generado con `tools/tokens`); **hecho cuando** `tools/tokens --check` sale con 0 y `TokensUssSyncTests` y `ContrastTests` (con los pares N1-N13 de §8.1) pasan con dotnet. `Typography.uss` lo sigue generando WP-A | WP-B arma las vistas con esas clases; si llegan en WP-D, los tests de contrato de WP-B fallarían |
| DN-02 | WP-A (`TypographyUssWriter`) | `.rm-type-label { letter-spacing: 8px; }` (no `0.96px`) con el comentario "unidad de TextCore: 1/100 em"; las reglas `.rm-text-scale-*` no repiten el tracking. Un test PlayMode mide el ancho de `IIIIIIIIII` con y sin la clase (diferencia ≈ 9 × 0,08 em) | §2.2 (medido) |
| DN-03 | WP-B | Clases y colores de texto de §2.3; en la tabla de contrato de 02-tech-design §3.12, `#hrm-bpm-unit` pasa a `--rm-metric-hr-hud` (no `--rm-metric-hr`) | Tamaños de la spec; AA de la unidad (QA-G2-08, N13) |
| DN-04 | WP-B | `.rm-button__state` en `RmButton`, chips, `.rm-icon-button`, filas `hrm-device-*` y la llave; `.rm-tab__state` en `.rm-tab__icon-box` | *Hover* y presión sin colores nuevos (`opacity` sobre blanco, regla 1 de §3.12) |
| DN-05 | WP-B | `.rm-focus-ring__halo` dentro de `.rm-focus-ring`; anillo también en pestañas, filas, chips, llave e interruptor | `focus.ringHalo` |
| DN-06 | WP-B | Textura `VerticalFade` (1 × 64 RGBA, blanco con alfa `smoothstep`, arriba 1; *clamp*, bilineal; creada al arrancar, sin asset) asignada por código como `background-image` de `.rm-card__glow` (tarjetas de métrica y `#hrm-glow` **nuevo** en la del pulsómetro) y de `.rm-tab-bar__sheen` **nuevo** (primer hijo de `#tab-bar`). `background-size: 100% 100%` y el tinte van en USS | USS no tiene degradés; verificado en el prototipo |
| DN-07 | WP-A (`MotionSpecs`) + WP-B (`AccentFlash`) | `AccentGlowPeak = 0.14`; `k` de §5.2 (`ease.pulse`, 300 ms, solo si cambió el texto); acento = token × `k` por código (en reposo `@60 %`: el test de contrato del borde acepta alfa 0,6); brillo `opacity 0,14·k`; la tarjeta del pulsómetro con `k = 0,6` fijo | Legacy `withOpacity(0.6)` + ART_BIBLE §3.1 |
| DN-08 | WP-B (`TabularNumber`) + WP-A (`DigitRoll`) | (a) caja de alto 1 em con la banda de mayúsculas centrada (en Barlow SC, línea de base a 0,85 em); (b) celda de `-` = avance del guion (0,37 em); `:` y `.` 0,32; (c) al cambiar el largo, ancho interpolado 180 ms `ease.enter` con celdas a la derecha; (d) curvas de §5.1 en `DigitRoll.Evaluate` | Centrado óptico, `--` legible, sin saltos al recentrar |
| DN-09 | WP-B | `#hrm-bpm-unit` alineado por línea de base con `#hrm-bpm`, calculado con las métricas resueltas (no un margen fijo), dentro del `FitBox` | Funciona con cualquier escala de letra |
| DN-10 | WP-A (`HeartbeatClock`, `MotionSpecs`) | Suma de los dos golpes; compresión `min(1; T / 420 ms)` (`BeatFullCycleMs = 420`); parada a escala 1 en 120 ms (`BeatStopMs = 120`); primer latido en el cuadro del primer pulso > 0 | §5.3 |
| DN-11 | WP-B (`TabTransition`) | Fundido de color de ícono y etiqueta por código (160 `ease.move`), limpiando el estilo en línea al terminar; escala del ícono 0-90-260; la píldora conserva la velocidad al reinterrumpirse | VID-02 determinista (D-14) |
| DN-12 | WP-B (`DeviceScreenView`) | `#device-connected-view` y `#device-scan-view` absolutas y superpuestas bajo `#device-appbar`; transición de §5.5; la tarjeta cambia de lugar a los 120 ms; nada antes del primer cuadro presentado | Sin saltos de layout durante el fundido |
| DN-13 | WP-B | `#hrm-body` con `overflow: hidden` durante el `HeightTween`; opacidad del corazón **solo por código**: el animador escribe `style.opacity` (1 o 0,40) siempre: al armar la tarjeta, en el acto; en cada cambio, el fundido de 160 ms de §5.0, igual con reducir movimiento (es opacidad, §5.10). `.rm-hrm-card__heart--dim` no lleva `opacity` en USS (si WP-B conserva la clase, es solo un marcador de estado); el test de contrato mira `resolvedStyle.opacity` = 0,40 | §5.6; un solo mecanismo (03-qa-harmony §8) |
| DN-14 | WP-B (`SimulatorLayerView`) | Abrir/cerrar de §5.7 por código; cerrar en el acto | Paridad de LT-26 y LT-27 |
| DN-15 | WP-B (`FitBox`) | Resorte ω 12 cuando un valor cambia la escala; en el acto al primer cálculo, al redimensionar y con reducir movimiento | §5.9 |
| DN-16 | WP-B (`MetricGridView`) | Alto de la grilla y entrada/salida de PULSO de §5.9 | "Nada salta" (spec §10.4) |
| DN-17 | WP-B | `#sim-rowing`: ocultar `.unity-toggle__checkmark` y agregar `.rm-switch__track > .rm-switch__thumb` a la derecha, con el rótulo a la izquierda | Interruptor con la estética de la app (CAP-18) |
| DN-18 | WP-B | Clase `.rm-icon-button` en `#sim-close` y los ± | Capa de estado circular de 40 |
| DN-19 | WP-B | Valor de los renglones ± de **64** dp (legacy 72); rótulo `flex 1 1 0` que ajusta | 0,7 dp de aire con 72 (medido) |
| DN-20 | WP-B | `min-height` en lugar de `height` en botones; etiqueta `white-space: normal`; `flex-shrink: 0` en controles, íconos y filas | Letra 1,3×; `flex-shrink` por defecto 1 |
| DN-21 | WP-B (arnés) | Reglas de toma de §7.1; sondas de §7.2 | Capturas deterministas |
| DN-22 | PO + WP-B | CAP-04 → CAP-04a (sin desplazar) + CAP-04b (**al final del desplazamiento**, como lo adoptó el PO en 01-po-spec §7.3; no `ScrollTo("hrm-card")`) (§7.2). Falta partir la fila de 02-tech-design §4.3 (Architect) | La tarjeta no entra en 1280 × 720 |
| DN-23 | WP-B | Padding inferior por la barra **dentro** del contenido de cada `ScrollView` de pantalla (no en el `.rm-screen` por fuera) | El último elemento sube por encima de la barra y la translucidez se ve al desplazar |
| DN-24 | WP-B | `#device-header` como fila que puede ajustar (`flex-wrap`), nombre con "…" | 360 dp con letra 1,3× |
| DN-25 | QA | En los builds de desarrollo con simulador, la marca "Development Build" del motor cae abajo a la derecha, cerca de la llave y de `Historial` (P-01, aceptada): no es un defecto de diseño | Device-Win |

---

## 10. Plan de WP-D

| Archivo | Contenido |
|---|---|
| `Core/Design/Tokens.cs` | (si DN-01 no se hizo antes) los 5 tokens; regenerar los dos USS; si S-B1 → `Overlay`, el respaldo de §8.3 |
| `Presentation/Styles/Skin.uss` | §4.1-§4.7 y §4.10: superficies, brillos (tinte y tamaño), barra de vidrio, píldora, botones y sus estados, chips, filas, anillo de foco, barras de desplazamiento, separaciones internas de la tarjeta del pulsómetro, cabecera que ajusta. Solo `var(--rm-…)` y `opacity`; nunca `font-size` en un `.rm-type-*` |
| `SimulatorPresentation/Styles/SimulatorSkin.uss` | §4.8 y §4.9: cinta (10/700 Inter Bold por `-unity-font-definition`, sin clase de tipo), llave, panel, renglones, interruptor |
| `Presentation/Styles/Motion.uss` | §5.8 con `var(--rm-dur-*)` y `var(--rm-ease-*)` (verificado); reglas de `.rm-reduce-motion` |

Orden: (1) correr el arnés con lo de WP-B y mirar CAP-01, CAP-10, CAP-16 y CAP-20; (2) `Skin.uss` del tablero y la tarjeta; (3) botones y estados; (4) barra; (5) simulador; (6) `Motion.uss`; (7) todas las capturas y videos con 0 desbordes; (8) cerrar §7.2 con el visto bueno y anotar cada cambio con su captura en este documento. "Hecho" = el de 02-tech-design §6.3.

---

## 11. Alineación de las biblias (se aplica en WP-D, con este documento como fuente)

| Dónde | Cambio |
|---|---|
| ART_BIBLE §3.1 | brillo `@14 %` con factor 0,6 en reposo (era `@18 %`); acento en reposo `@60 %` (legacy); el valor se achica **sin tope** (D-10); etiqueta del pulso `metric.hr.hud`; fila Pulsómetro: unidad `bpm` en `metric.hr.hud` (QA-G2-08) |
| ART_BIBLE §3.6 | etiqueta `button` 14/600; tonal `accent.tonalBg` + `accent.onTonal`; *hover* 4 %, presionado 8 % |
| ART_BIBLE §3.11 | brillo superior de la barra (5 %, 32 dp); cinta 10/700 sin tracking (era 11/800 con 0,1 em) |
| ART_BIBLE §4.3 | `bodyM`, `bodyXs`, `button`; nota de `letter-spacing` en USS (1/100 em) |
| ART_BIBLE §2.4 | "PanelSettings en *Constant Physical Size*" → `ConstantPixelSize` con la densidad (D-03 del Architect) |
| ANIMATION_BIBLE §2.1, §2.4, §2.9, §2.12 | color de la pestaña por código; curvas por celda del rodillo y ancho interpolado; compresión y suma del latido; presión 0,97 con vuelta de 240 |
| ART_BIBLE §3.10 | estado vacío de Dispositivo: ícono 48 (D-11), título `body` `text.secondary` (DV-05), ayuda 12 `text.tertiary` (paridad; la fila "Vacío" genérica sigue para las listas de la It. 3+) |
| 00-deuda-diseno §3.5 | llave y *chevron* (§1); insignia 12 y vacío 48 (D-11) |

---

## 12. Qué queda abierto

- **NV hasta WP-B:** cómo se ven los glifos de íconos dentro de la UI real a 12-56 dp (el prototipo los mostró bien a 12, 14, 18, 24, 32 y 48); S-B1 (tubería final); `letter-spacing` en un build de *player* (medido en el Editor).
- **Visto bueno de capturas y videos**: en WP-D, sobre lo que produzca el arnés.
- Propuesta fuera de paridad, sin urgencia: marcar el preset activo en el panel; estirar la píldora en proporción a su velocidad (efecto "líquido"); 4 columnas en tablet (P-02).

---

## 13. Verificación hecha

1. **Legacy** (solo lectura): tamaños, opacidades y rellenos de `device_screen.dart`, `heart_rate_card.dart`, `simulator_overlay.dart`, `main.dart` y `theme.dart` (citados en cada sección); textos de `app_es.arb` / `app_en.arb`. Capturas `image-01.png` e `image-02.png`. `git -C rowmate-legacy status --porcelain` vacío al empezar y al terminar.
2. **Anchos de texto** con HarfBuzz (uharfbuzz 0.57, kerning incluido) sobre `Inter-*.ttf` y `BarlowSemiCondensed-*.ttf` del repo: etiquetas, pestañas, botones, panel, cinta y valores tabulares (§3, §4).
3. **Contrastes** con un *script* propio (método de la It. 1: mezcla sRGB sin redondear; lineal informativa): todos los de §1, §4 y §8.
4. **Prototipo en Unity 6000.3.27f1** en el proyecto descartable del *spike* S-0 (scratchpad, fuera del repo; copias de las fuentes y de `Tokens.uss`): tres corridas en *batchmode* con `-executeMethod`, código 0, sin `error CS`. Renderizó con `GammaRt` (RT `R8G8B8A8_UNorm` + `forceGammaRendering`) Dispositivo en TV, el panel en TV, los estados de la tarjeta del pulsómetro y seis intensidades de brillo; midió estilos resueltos (`var()` en transiciones, tinte, `letter-spacing`, `scale`), cajas de línea y tamaños (tarjeta 179,8 × 138,3; tarjeta del pulsómetro conectada 142,1; panel 442,7 con `Hora real`; `Hora escena` 84,6 en 85,3); y probó `letter-spacing` 0 / 0,96 / 2 / 4 a 12, 24 y 48 dp (medido y en tinta). Píxel de la barra sobre `bg.app`: `#0A1623`, igual al cálculo.
5. **Sin cambios en el repo**: no se tocó código, `Tokens.cs` ni otros documentos; el único archivo escrito es este.

---

## 14. Cambios por gate QA

Correcciones por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md), APROBADO CON OBSERVACIONES), 2026-10-27. Volví a leer en el legacy (solo lectura, `3ae0f3a`): `heart_rate_card.dart:14-46` y `:150-196` (`_color` = `MetricColors.heartRate`; unidad en `_color.withValues(alpha: 0.7)`, `:178-180`), `device_screen.dart:396-442` (`deviceNoResults` en `Colors.white54` sin tamaño, `:430-431`), `simulator_overlay.dart:28-50` y `main.dart:100-112` (el `Banner` envuelve toda la app), y en el SDK local de Flutter `material/material.dart:476-477` (texto sin estilo = `bodyMedium`). Contrastes recalculados con el método de la It. 1 (mezcla sRGB sin redondear): `metric.hr.hud` sobre la tarjeta 4,614; `metric.hr` 3,815; rosa al 70 % 2,450 (`#AF4062`); `text.secondary` sobre `bg.app` 8,967 (`#B6BBBF`); `text.tertiary` 5,829 (`#90969D`). No se leyó `strava_secrets.dart`; `git -C rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se disputa ningún issue.

| Issue | Sev. | Decisión | Qué cambió | Dónde |
|---|---|---|---|---|
| QA-G2-08 | menor | **Corregido** como sugiere QA | La unidad `bpm` pasa de `text.secondary` a **`metric.hr.hud`** (`#FF5A7E`, 4,61:1, mismo matiz que el número). El PO ya lo había fijado (DV-04: "en el matiz del pulso, no en blanco"). Nuevo par N13; sondas de `bpm` en CAP-04b y CAP-12; DN-03 pide corregir la fila `#hrm-bpm-unit` de la tabla de contrato de 02-tech-design §3.12, que dice `--rm-metric-hr` (3,81, no llega a AA) | §1, §2.3, §4.6, §7.2 (CAP-04, CAP-12), §8.1 N13, §9 DN-03, §11 |
| QA-G2-11 | menor | **Anotado como cambio** (la segunda opción de QA), no `text.tertiary` | Queda `text.secondary` y se marca como diferencia a propósito: con D-ART-03 la ayuda de abajo sube del 38 % al 54 %, y con `text.tertiary` el título y la ayuda quedarían del mismo color; así se conservan los dos niveles del legacy. El PO lo aceptó (DV-05). Se suma la sonda del título en CAP-08 | §2.3, §4.5, §7.2 (CAP-08), §8.1 N7, §11 |

**Alineación con los otros documentos** (cambiaron después de la primera versión de estas notas):

| Origen | Qué cambió acá |
|---|---|
| 01-po-spec §7.3 (QA-G2-04, DN-22 adoptado) | CAP-04b es "desplazada **hasta el final**", no `ScrollTo("hrm-card")`. Recalculé la toma: contenido 880,7 dp (con la tarjeta conectada de 142 y la barra dentro del desplazamiento, DN-23), desplazamiento máximo 216,7; se ven `DISTANCIA` entera (y ≈ 185-438), la tarjeta del pulsómetro (450-592) y 64 dp de aire hasta la barra (656). Sondas nuevas: trazo de `bpm` y aire de 64 ± 1 |
| AC-BLE-11-07 (respuesta del PO a QA-G2-12) | §4.3: 64 dp de aire debajo de la tarjeta del pulsómetro (los 32 + 16 del legacy más el relleno de 16). El mecanismo es del Architect |
| QA-G2-06 (cita §4.8 de estas notas) | La cinta deja de ubicarse "en el área segura": va en la esquina de la capa del simulador sin insets propios, como el `Banner` del legacy, y sigue la regla única de área segura que fije el Architect. La llave mide `bottom` desde el alto real de la barra (64 + inset inferior) |
| QA-G2-03 (parte del Designer) y 03-qa-harmony §8 | DN-01 define **WP-D0**: archivos, que ningún token existente cambia y cuándo está hecho (`tools/tokens --check` con 0; `TokensUssSyncTests` y `ContrastTests` en verde). DN-13 fija **un solo mecanismo** para la opacidad del corazón (estilo en línea por código; `--dim` sin `opacity` en USS): es un cambio sobre la fila `.rm-hrm-card__heart` / `--dim` de 02-tech-design §3.12 que tiene que entrar en el anexo del Architect, igual que el resto de DN-01 a DN-25 |
| QA-G2-10, QA-G2-09, QA-G2-02 | Sin cambios: §5.2 y VID-01 ya decían "solo en las tarjetas cuyo texto cambió"; el acento al 60 % en reposo (DV-02), las etiquetas al 100 % o en `metric.hr.hud` (DV-03) y la cinta con texto oscuro (DV-01) ya estaban en §4.4, §1 y §4.8; DN-02 (`letter-spacing: 8px`) es lo que midió QA en P2 |

**Visto y sin cambio:** el legacy dibuja `deviceNoResults` en `bodyMedium` 14 (texto sin estilo dentro de `Material`) y la app lo pone en `body` 15, el texto general de la escala (ART_BIBLE §4.3). Es parte del sistema tipográfico que 01-po-spec §11.1 deja fuera de las diferencias, y ningún AC fija ese tamaño. Se mantiene.

**Queda para otros:** el anexo de 02-tech-design (QA-G2-03) con la fila de `#hrm-bpm-unit` y la del corazón de §3.12 (Architect); partir la fila CAP-04 de 02-tech-design §4.3 (Architect); correr WP-D0 antes de WP-A (Designer, cuando el coordinador lo habilite); alinear las biblias en WP-D (§11).

---

## 15. Implementación (WP-D)

2026-10-27. Pulido visual y de motion sobre lo que dejó WP-B (02-tech-design §6.4), iterando con el arnés (`tools/unity/capture-snapshots.ps1`, 41 tomas, `GammaRt`) y mirando cada PNG. Legacy releído solo para confirmar la variante de cada control (`heart_rate_card.dart:106`, `:187-192`, `:210-221`; `device_screen.dart:23-27`, `:386-396`; `simulator_overlay.dart:45`, `:105-162`, `:195-204`: tonal, contorno, texto, peligro, `FloatingActionButton.small`, `ActionChip`, `SwitchListTile`, `IconButton`); `git -C rowmate-legacy status --porcelain` vacío antes y después; no se leyó `strava_secrets.dart`.

### 15.1 Qué encontré al empezar

WP-B ya había puesto en `Base.uss` y `SimulatorBase.uss` la mayor parte del aspecto de §4 (superficies, radios, brillos, vidrio y píldora, cinta, panel, interruptor, barras de desplazamiento). Faltaban tres cosas, y la primera se veía en casi todas las capturas:

1. **Los 5 tokens de DN-01 no estaban en `Tokens.cs`** (WP-D0 no se había corrido): `.rm-type-body-m`, `-body-xs` y `-button` no existían en `Typography.uss`, así que los textos que WP-B marcó con esas clases (ayudas, insignia, nombre y unidad del pulsómetro, rótulos del panel, todos los botones y chips) heredaban un tamaño que no era el de la spec. El botón tonal usaba `nav.indicator` (`@18 %`) con `accent.primary`, que no llega a AA (§8.1).
2. Las capas de estado (hover y presionado), el anillo de foco por control y las transiciones de `Motion.uss`.
3. Alineaciones finas del panel del simulador.

### 15.2 Cambios, uno por uno, con la captura que lo muestra

| # | Archivo | Cambio | Dónde se ve |
|---|---|---|---|
| W1 | `Core/Design/Tokens.cs` → `Tokens.uss`, `Typography.uss` (regenerados con `tools/tokens`; `--check` en 0, 127 tokens) | `bodyM` 14/20/400, `bodyXs` 12/16/400, `button` 14/20/600, `accent.tonalBg` `#00B4D8 @12 %` (decorativo), `accent.onTonal` `#38CBEF` (`UiText`). Ningún token existente cambia | CAP-01, CAP-10: la pista pasa a 12 dp en 3 líneas y `Buscar` a 14/600; CAP-01: `Conectado` 12; CAP-12: nombre y `bpm` 14; CAP-13, CAP-14b: `Conectar`, `Buscar otro`, `Olvidar` 14/600; CAP-16 a CAP-19: rótulos 14 y chips 14/600 |
| W2 | `Tests/EditMode/Design/ContrastTests.cs` | `ItTwoDesignerPairs`: los pares N1-N13 de §8.1 (26 casos), con el fondo como pila de capas (`surface.card|metric.spm@3`, `white|surface.nav|nav.indicator|text.primary@5`…). N1 y N2 se prueban con el brillo al **3 %** a la altura de la etiqueta: es lo que vale el degradé de 24 dp en la mayúscula (≈ 19 dp bajo el borde, 0,11 del pico) con el pico de 18 %, redondeado para arriba. Aclaración de §8.1: con una capa plana del 18 % entera (sin el degradé), `SPM` daría 4,10 y `metric.hr.hud` 3,92; los valores de §8.1 ya eran a la altura de la etiqueta | dotnet: los 26 pasan; el menor, N4 `accent.primary` sobre panel + hover, 4,501 |
| W3 | `Skin.uss` | Tonal: `accent.tonalBg` con texto e ícono `accent.onTonal` (mismos selectores que `Base.uss`) | CAP-01, CAP-10 (sonda `#173E57` y trazo `#38CBEF`), CAP-13 (`Conectar` al 38 %), CAP-14b |
| W4 | `Skin.uss` | Capas de estado: hover 4 % solo en escritorio (`.rm-desktop`) y nunca en el destino elegido; **presionado = `:hover:active`** 8 %; el primario presionado cambia el fondo a `accent.primaryPressed` sin capa (`Base.uss` lo ponía también con `:active` solo: con ese estado queda `accent.primary`); deshabilitado sin capa | No se ve en las capturas (sin puntero, a propósito: §15.3). Verificado en una sonda descartable (§15.4) |
| W5 | `Skin.uss` | Anillo de foco por control (radio del control + 4, halo + 4): pestaña 20/24 alrededor de la caja del ícono, botón de ícono 28/32, chip 12/16, fila de sensor rodeando su capa extendida (−12 a los costados), interruptor −8 a los costados; botón y llave 16/20 (de `Base.uss`) | Solo con `.rm-keyboard-nav` (no está en las capturas) |
| W6 | `Skin.uss` | `-unity-background-scale-mode: stretch-to-fill` explícito en `.rm-card__glow` y `.rm-tab-bar__sheen` (DN-06) | CAP-01, CAP-03 (el brillo igual de alto a 294 dp de ancho) |
| W7 | `Motion.uss` | §5.8 con `var()`: capas de estado, vuelta `dur.fast` `ease.exit` y hacia el estado `dur.micro` `ease.enter`; escala al presionar 0,97 (botones, chips) y 0,94 (botones de ícono, caja del ícono de la pestaña, llave), hacia 90 y vuelta 240 `ease-out-cubic`; fondo del primario hacia 90 y vuelta 160; interruptor `dur.fast` `ease.move` en la perilla (desplazamiento, escala, color) y la pista (fondo, borde); `.rm-reduce-motion` sin escala | Sonda descartable (§15.4); AUX-02 (reducir movimiento) idéntica byte a byte a CAP-01 |
| W8 | `SimulatorSkin.uss` | Cabecera del panel con `padding-right: 12` (§4.9, "relleno 0 / 12"): la X queda en la columna de los `+` | CAP-16, CAP-18, CAP-19 |
| W9 | `SimulatorSkin.uss` | Pista del interruptor centrada en el renglón (`top: 50%` y `margin-top: −12`) en lugar de `top: 12`: a 1,3× el rótulo puede pasar a dos líneas | CAP-17 (encendido, `#00B4D8`), CAP-19 (apagado, `#08131F` con borde y perilla chica a la izquierda) |
| W10 | `SimulatorSkin.uss` | Anillo de foco de la llave, 16 | — |

### 15.3 Dos hallazgos del arnés que cambiaron `Motion.uss`

La primera corrida completa con `Motion.uss` según §5.8 dio fotos equivocadas que ninguna sonda marcó:

- **CAP-19**: el interruptor `Remando` salió **encendido** aunque la grilla mostraba `--:--` / `0.0` / `0`.
- **CAP-17, CAP-19, CAP-20**: los `±` tocados (y la pestaña tocada) salieron con la capa de presionado y la escala.

Causas (medidas en la sonda de §15.4): (a) en *batchmode* y modo edición, una transición de USS queda en su valor inicial hasta que el panel se vuelve a pintar por otro motivo (1 s de reloj real y 87 000 `update` después, `resolvedStyle` seguía en el valor de partida; los valores finales llegaron recién en la fase siguiente), así que la foto se toma a mitad de la transición aunque el reloj de prueba haya avanzado; (b) `Tap` del arnés manda `NavigationSubmitEvent`, y UI Toolkit lo trata como un "clic simulado" que deja `:active` durante 100 ms **de reloj real**: en el arnés, eso es "para siempre". La cuenta de `TransitionRunEvent` de la regla de toma (DN-21) no lo detectó: CAP-19 pasó con la transición congelada.

Solución en WP-D, sin tocar código: **todo lo que transiciona o se ve "presionado" depende de `:hover`** (puntero o dedo apoyado sobre el control). Presionado = `:hover:active`; las transiciones del interruptor se declaran en `.rm-switch-row:hover`. El arnés no tiene puntero, así que nada de esto se mueve en una captura y las fotos y los videos vuelven a ser deterministas. Con mouse se ve todo lo de §5.8; con teclado (Enter, Espacio) el control responde y lo marca el anillo de foco, sin "presionado" (el clic simulado de 100 ms ya no pinta nada). Corrida final: CAP-19 con el interruptor apagado (sonda de la pista `#08131F`) y ningún `±` ni pestaña con capa.

Queda para verificar en equipo **[Device-And]**: que en Android UI Toolkit marque `:hover` mientras el dedo está apoyado (es lo esperable: el puntero táctil entra al elemento al apoyarse). Si no lo hiciera, el toque sigue funcionando, pero sin la capa ni la escala de presionado; el arreglo sería un pedido al Architect (una clase de presionado puesta por `RmButton` con el `PointerDown`/`PointerUp` real).

### 15.4 Verificación

1. **Sonda descartable** de UI Toolkit (proyecto del *spike* S-0 en el scratchpad, nunca el repo; copias de `Tokens`, `Base`, `SimulatorBase`, `Skin`, `SimulatorSkin` y `Motion`), con las pseudo-clases forzadas por reflexión. Reposo: capa `opacity 0` y `transition [opacity] 160ms EaseInCubic`; botón `[scale] 240ms EaseOutCubic`. Con `:hover:active`: primario `[scale, background-color] [90ms, 90ms]` (las listas con `var()` se resuelven) y su capa en 0; ya asentado, botón de texto con hover 0,04, chip 0,97, caja del ícono de la pestaña 0,94, capa de la pestaña 0,08. Interruptor sin puntero: `transition-property all`, `0s`, cambio en el acto (perilla 0,8 a la izquierda, pista `#08131F`).
2. **Arnés**: 41 tomas, 0 con problemas, 0 desbordes; R-T1 `#2A2F37` / `#BBC1C8` (app) y `#6A6A6C` / `#DADBDC` (directa); el define restaurado al final.
3. **Sondas de aspecto de §7.2** sobre las PNG (un *script* propio en el scratchpad que lee `manifest.json` y el `worldBound` de cada elemento; no están en el catálogo del arnés, ver §15.6): **35 de 35** dentro de la tolerancia. CAP-01: fondo, tarjeta y barra superior `#0D1B2A` / `#1A2E45` / `#0D1B2A`; barra de pestañas `#0A1623`; píldora `#083344` (±1); acentos en reposo `#A3722C` `#0A7E9D` `#A39322` `#388BAC`; insignia `#1A3932`; tonal `#173E57`; llave `#1E3550`; corazón `#6F3856`; cinta `#FF5722`. CAP-08: primario `#00B4D8`, ícono vacío `#47525D`, título `#B6BABF`, ayuda `#90969D`. CAP-09: primario deshabilitado `#08556C`. CAP-10: tonal `#173E57`, texto `#38CBEF`. CAP-16 y CAP-18: panel `#1E3550`. CAP-17: pista `#00B4D8`; CAP-19: `#08131F`. CAP-20a-e: barra `#0A1623` y píldora `#083344`. Sondas del propio arnés: 24 de 25 (la que falta, en §15.5).
4. **Videos**, con cuadros extraídos con `Windows.Media.Editing`: VID-01, el corazón late con período ≈ 425 ms (picos a 0,23 / 0,66 / 1,09 / 1,49 s, 142 bpm) y doble golpe (sube, se sostiene ≈ 130 ms y baja); VID-02, píldora que se desliza en ≈ 200 ms y contenido que entra; VID-03, "apagar" en 120 ms, búsqueda, y a los 3 s "encender" con las tarjetas escalonadas en ceros y los valores al segundo, con la distancia que siguió (`229m`).
5. `run-tests.ps1` (dotnet) en verde; Unity: compilación limpia y EditMode / PlayMode con y sin simulador (resultados en el reporte de WP-D).

### 15.5 Visto bueno por captura (§7.2)

| Captura | Visto bueno | Nota |
|---|---|---|
| CAP-01, CAP-02, CAP-03, CAP-05 | Sí | 2 / 3 columnas; cabecera centrada con la insignia a 12; pista en 3 líneas a 12 dp; tonal; llave discreta; barra translúcida (en CAP-05 se ve `DISTANCIA` a través) |
| CAP-04a, CAP-04b | Sí | 3 columnas en 1,6; en 04b la tarjeta del pulsómetro entera sobre la barra, `142` y `bpm` en la misma línea de base. La sonda de aspecto de `bpm` del arnés falla **solo a densidad 1**: el color más frecuente del trazo de Inter 14/400 a 1 px por dp es un borde suavizado (`#443650`, 11 píxeles) contra el color pleno (`#FF5A7E`, 10 píxeles). El color es el correcto: la misma sonda da `#FF5A7E` exacto en CAP-12b (2,625) |
| CAP-06, CAP-07 | Sí | PULSO con etiqueta `#FF5A7E` y valor `#EF476F`; `123456m` y `100:01` achicados y centrados |
| CAP-08, CAP-09, CAP-22 | Sí | Primario cian con texto oscuro; deshabilitado al 38 %; estado vacío centrado. En L640 la llave tapa el *chevron* de la fila: es la capa del simulador sobre la app, como el `Overlay` del legacy |
| CAP-10 a CAP-15 | Sí | Corazón al 40 % / pleno; `--` como dos guiones; `142` en su lugar; error en 3 líneas con "…"; `Conectar` al 38 % |
| CAP-16, CAP-18, CAP-19 | Sí | Panel con la superficie y el borde de la app; X alineada con los `+`; interruptor apagado en CAP-19 |
| CAP-17 | Sí, con nota | Desplazado al final se ven `Hora real`, `Remando` y las dos fallas; el renglón `14:00` queda justo arriba, fuera del panel de 248 dp (desde `Hora escena` hasta el final hay 240 dp y entran 200). Es geometría del panel en 800 × 360, no aspecto: si QA lo quiere en la foto, la toma tendría que desplazar hasta `#sim-hour-row` (§15.6) |
| CAP-20a-e, CAP-21a-c, CAP-25 | Sí | Píldora centrada detrás del ícono elegido, etiqueta siempre debajo; 5 destinos de 82 dp; ícono apagado `#47525D` |
| CAP-23 | Sí | §4.4 de 02-tech-design |
| CAP-24 | Sí | Nada cortado; el nombre del pulsómetro termina en "…" (una línea, previsto); la etiqueta de la barra con su aire abajo |
| VID-01, VID-02 | Sí | §15.4 |
| VID-03 | Sí, con un hallazgo para WP-B | Durante el "apagar" (0-120 ms, 4 cuadros) la cabecera dice `Rower` en lugar de `Remo simulado`: la vista saliente se vuelve a dibujar con el estado desconectado (sin nombre → `Rower`) mientras se apaga. No es de USS (§15.6) |
| AUX-01, AUX-02 | Sí | AUX-02 (reducir movimiento) = CAP-01 byte a byte |

### 15.6 No adoptado, desvíos y pedidos

| Qué | Decisión | Por qué / para quién |
|---|---|---|
| DN-20: etiqueta de botón con `white-space: normal` | **No adoptado**; queda `nowrap` con "…" (`Base.uss`) | Medido: en filas de ancho automático UI Toolkit mide la etiqueta en dos líneas dentro de una caja de una (`LayoutAudit`: "más alto que su caja (34,3 > 17,1)" en `Conectar`, `Buscar otro`, `Olvidar`, `Buscando...`, `Simular desconexión`, `Disconnect`; CAP-13, -14b, -15, -18, -19, -20e). A 1,3× un texto largo termina en "…" en lugar de pasar a dos líneas (ninguna captura lo muestra) |
| Presionado con `:active` solo (§4.7, §5.8) | Cambiado a `:hover:active` | §15.3. Con teclado, el anillo de foco; Android, [Device-And] |
| Sondas de aspecto de §7.2 en `SnapshotCatalog` | Pedido al **Architect** (archivo de WP-B) | Hoy las mide un *script* del Designer fuera del repo (§15.4); conviene sumarlas al catálogo con los puntos de §15.4 (la píldora a 16 dp de su borde izquierdo y 5 del inferior; la pista a 6 dp del borde opuesto a la perilla) |
| Sonda de `bpm` de CAP-04b | Pedido al **Architect** | A densidad 1 el "color dominante" de un texto de 14/400 empata con el borde suavizado: pasar a "píxel más saturado" o dejarla en CAP-12b |
| Regla de toma (DN-21) | Pedido al **Architect** | No detectó la transición congelada de CAP-19 ni el `:active` de 100 ms del clic simulado (§15.3). Con este `Motion.uss` ya no ocurre, pero la regla tendría que fallar si volviera a pasar |
| VID-03: `Rower` durante el "apagar" | Hallazgo para **WP-B** | `ViewSwap` / `DeviceHeaderView`: congelar la vista saliente o no redibujar la cabecera fuera de "conectado" |
| CAP-17 sin el renglón `14:00` | Para el **Architect** / QA | Geometría (§15.5) |
| Alineación de las biblias (§11) | Pendiente (Designer, fuera de los archivos de esta entrega) | ART_BIBLE §2.4, §3.1, §3.6, §3.10, §3.11, §4.3 y ANIMATION_BIBLE §2.1, §2.4, §2.9, §2.12, más §15.3 (presionado con `:hover:active`) y §15.6 (sin `white-space: normal`) |
| Respaldo de §8.3 | No hace falta | S-B1 eligió `GammaRt` |
