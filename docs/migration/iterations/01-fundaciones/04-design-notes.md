# It. 1 — Fundaciones: notas de diseño y tokens

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-27 |
| Iteración | 1 · `01-fundaciones` ([ROADMAP §2, It. 1](../../ROADMAP.md)) |
| Entrada | [01-po-spec.md](01-po-spec.md) (AC-BASE-04, AC-BASE-05, AC-RUT-01-03, P-05, P-07, P-08), [02-tech-design.md](02-tech-design.md) §4 (mecanismo, API, formato y tokens obligatorios), §5 (slots) y R-T1, [ART_BIBLE](../../design/ART_BIBLE.md) §2-§4 y §10, [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §1, [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §0 y §4.3, ADR 016 |
| Entrega | `Assets/RowMate/Core/Design/Tokens.cs` (fuente única, 122 tokens). Nada más: `Tokens.uss` lo **genera** `tools/tokens` (WP-A) |
| Legacy leído | `lib/shared/theme.dart:5-69`, `lib/features/workout/immersive_workout_screen.dart:531-533` y `:579-582`, `lib/features/history/history_screen.dart:99`, `lib/features/profile/profile_screen.dart:99` (solo lectura; `git status --porcelain` vacío antes y después). No se leyó `strava_secrets.dart` |
| Verificado | `Tokens.cs` compila tal cual (C# 9, `netstandard2.1` y `net8.0`, advertencias como errores) contra un *stub* fiel a la API de §4.2; un programa de verificación (fuera del repo) chequeó nombres, campos, la tabla obligatoria, los 21 hex de contrato y todos los contrastes de este documento: **0 fallas** (§9) |

Convenciones: hex `#RRGGBB` opacos; transparencia `@NN %`. Contrastes WCAG 2.x con el método fijado en 02-tech-design §4.4 (mezcla sRGB con alfa sin redondear, canales en `double`, umbral 0,04045, relación sin redondear). **[C]** = color de contrato del legacy. "Peor vidrio" = `glass.fallback` sobre blanco = `#2A2F37` (L = 0,02782).

---

## 0. Qué aporta diseño a la It. 1

La It. 1 no tiene pantallas. Diseño aporta **el contrato visual como datos verificables**:

1. **`Tokens.cs` completo**: los 21 colores de contrato idénticos al legacy, todo el tema de ART_BIBLE §2 (superficies, texto, bordes, estados, marca, desarrollo), las variantes `*.hud`, el respaldo del vidrio, espaciado, radios, grosores, distancias y tiempos de movimiento, curvas y escala tipográfica. Así la It. 2 arma el tema sin tocar la fuente y el generador de USS tiene todo desde el primer día.
2. **Roles de contraste** asignados a cada color que es texto o valor, para que `ContrastTests` los vigile: si alguien sube o cambia un token y rompe R-10 o AA, falla `dotnet test`.
3. **Pares de contraste que pide el Designer** (§4.3): la parte "≥ 3:1 si el Designer lo pide" del rol `Graphic` y el texto sobre fondos que no son `surface.card`.
4. **Respuesta de diseño a P-05, P-07, P-08 y R-T1** (§5 y §6).
5. **Un ajuste de token**: `state.dangerText` pasa de `#FF6B6B` a `#FF7272` (mismo matiz) porque no llegaba a 4,5:1 sobre `surface.raised` (§6).

Asset slots: la It. 1 no crea ningún slot ni carpeta de arte (02-tech-design §5, D-09). Nada que agregar de diseño.

---

## 1. Reglas del archivo `Tokens.cs`

- Formato de 02-tech-design §4.3: un campo `public static readonly` por token, nombre del campo = `TokenCatalog.FieldNameFor(nombre)`, sin lógica.
- Nombres: los de ART_BIBLE §2 y §4.3 y ANIMATION_BIBLE §1. Los nombres nuevos (§6) siguen el mismo esquema y la regex `^[a-z][a-zA-Z0-9]*(\.[a-z][a-zA-Z0-9]*)*$`. Como un segmento no puede empezar con un dígito, la escala de espaciado se llama `space.dp<N>`.
- **Contrato**: solo los 21 de 02-tech-design §4.4 usan `ColorToken.Contract`. No se cambian nunca.
- **Resto de colores**: se pueden ajustar **solo** en luminancia y con el mismo matiz (ART_BIBLE §0.1, "Semántica intocable"), y nunca bajando su rol. Cada cambio se refleja en este documento y en ART_BIBLE.
- Un mismo hex puede aparecer en varios tokens con semántica distinta (`#06D6A0` = `metric.kcal`, `step.rest`, `phase.running`, `state.success`, `*.hud`). Son decisiones independientes: si mañana cambia la variante del HUD, la base no se toca.
- Cambiar un token = editar `Tokens.cs` → `dotnet run --project tools/dotnet/RowMate.Tokens …` (02-tech-design §4.1) → `run-tests.ps1`.

---

## 2. Lista completa de tokens (122)

### 2.1 Fondos, superficies, texto y bordes (20)

| Token | Valor | Rol | Semántica | Contraste verificado |
|---|---|---|---|---|
| `bg.app` [C] | `#0D1B2A` | Surface | fondo de toda pantalla no inmersiva y AppBar (`theme.dart:13`, `:20`) | blanco 17,39 · `text.secondary` 8,97 · `text.tertiary` 5,83 |
| `bg.sunken` | `#08131F` | Surface | campos, pistas de sliders, fondo de gráficos | `text.secondary` 9,40 · `text.tertiary` 5,99 · `accent.primary` 7,59 |
| `surface.card` [C] | `#1A2E45` | Surface | tarjetas (`theme.dart:15`); fondo de referencia de `UiText` | `text.primary` 13,81 · `text.secondary` 7,56 · `text.tertiary` 5,14 |
| `surface.raised` | `#23364C` | Surface | filas internas, tarjeta presionada, seleccionado | `text.secondary` 6,92 · `text.tertiary` 4,80 · `accent.primary` 5,00 · `state.dangerText` 4,64 |
| `surface.overlay` | `#1E3550` | Surface | diálogos, menús, hojas inferiores | `text.secondary` 6,98 · `text.tertiary` 4,83 · `accent.primary` 5,07 · `state.dangerText` 4,70 |
| `surface.nav` | `#0A1622 @92 %` | Surface | barra de pestañas translúcida, sin desenfoque (QA-P-22) | peor caso (sobre blanco): `accent.primary` 6,02 · `text.secondary` 8,00 |
| `nav.indicator` | `#00B4D8 @18 %` | None | pastilla del destino seleccionado | decorativo: el estado lo dan ícono y texto en `accent.primary` |
| `border.subtle` | `#FFFFFF @8 %` | None | borde de vidrio, separadores de barra | decorativo |
| `border.default` | `#FFFFFF @12 %` | None | separadores, miniatura no elegida | decorativo |
| `border.strong` | `#FFFFFF @24 %` | None | botón contorno neutro, foco inactivo | 2,15 sobre card: no se exige (el botón se identifica por su texto) |
| `focus.ring` | `#00B4D8` | Graphic | anillo de foco 2 dp (desktop) | 5,60 card · 7,06 `bg.app` |
| `focus.ringHalo` | `#00B4D8 @30 %` | None | halo de foco 4 dp | decorativo (el anillo cumple solo) |
| `text.primary` [C] | `#FFFFFF` | UiTextPrimary | texto primario; texto de la barra superior (`theme.dart:20-21`) | 13,81 sobre card (≥ 7) |
| `text.secondary` | `#FFFFFF @70 %` | UiText | subtítulos, etiquetas (≈ `#BAC0C7` sobre card) | 7,56 sobre card |
| `text.tertiary` | `#FFFFFF @54 %` | UiText | fechas, ejes, ayudas (D-ART-03) | 5,14 sobre card |
| `text.disabled` | `#FFFFFF @38 %` | None | solo deshabilitado o decorativo | 3,31 (WCAG exime lo deshabilitado) |
| `text.onAccent` | `#04131C` | None | texto sobre cian, menta y amarillo | 7,64 sobre `accent.primary` · 5,40 sobre `accent.primaryPressed` · 9,99 sobre `state.success` · 13,34 sobre `metric.watts` |
| `brand.seed` [C] | `#0077B6` | Graphic | segmento elegido, encabezado de serie (`theme.dart:5`) | 3,57 contra `bg.app` (segmento no elegido) · blanco encima 4,87. Sobre card da 2,84: nunca se usa como borde sobre card |
| `accent.primary` | `#00B4D8` | UiText | acento general; también texto (botones de texto, "Serie") | 5,60 card · 7,06 `bg.app` · 5,00 raised · 5,07 overlay |
| `accent.primaryPressed` | `#0096B4` | Graphic | botón primario presionado | `text.onAccent` encima 5,40 |

### 2.2 Semántica [C] — métricas, pasos y fases (15)

Idénticos al legacy, campo por campo. Rol `Graphic` (rellenos, bordes, acentos, líneas). Contraste como elemento gráfico (≥ 3:1) contra card y `bg.app`.

| Token | Hex | Legacy | Semántica | Card · `bg.app` |
|---|---|---|---|---|
| `metric.split` | `#FF9F1C` | `theme.dart:33` | split /500 m, parciales | 6,73 · 8,47 |
| `metric.spm` | `#00B4D8` | `:34` | SPM | 5,60 · 7,06 |
| `metric.watts` | `#FFD60A` | `:35` | vatios | 9,78 · 12,32 |
| `metric.distance` | `#4CC9F0` | `:36` | distancia, reps y metros | 7,18 · 9,05 |
| `metric.hr` | `#EF476F` | `:37` | pulso | 3,82 · 4,81 |
| `metric.time` | `#E0E0E0` | `:38` | tiempo | 10,46 · 13,18 |
| `metric.kcal` | `#06D6A0` | `:39` | calorías | 7,32 · 9,22 |
| `step.work` | `#EF476F` | `theme.dart:59` | trabajo | 3,82 · 4,81 |
| `step.rest` | `#06D6A0` | `:61` | descanso | 7,32 · 9,22 |
| `step.warmup` | `#FFD166` | `:63` | calentamiento | 9,58 · 12,06 |
| `step.cooldown` | `#118AB2` | `:65` | enfriamiento | 3,49 · 4,39 |
| `step.unknown` | `#607D8B` | `:67` | cualquier otro nombre, vacío o sin tipo (AC-RUT-01-03) | 3,16 · 3,98 |
| `phase.running` | `#06D6A0` | `immersive_workout_screen.dart:531` | EN MARCHA | 7,32 · 9,22 |
| `phase.paused` | `#FFD166` | `:532` | PAUSADO | 9,58 · 12,06 |
| `phase.finished` | `#EF476F` | `:533` | FINALIZADO | 3,82 · 4,81 |

Íconos por tipo de paso (dato de AC-RUT-01-04, lo usa `StepTypeInfo.IconName`): `whatshot`, `fitness_center`, `pause_circle_outline`, `ac_unit`. **P-07**: el glifo de descanso es `pause_circle` (`E1A2`) de la fuente **sin relleno** (`RowMateIcons-Outline`, FILL 0), que es el `pause_circle_outline` del legacy. El nombre legacy queda como dato en la It. 1; el mapeo a codepoint llega con `Icons.cs` en la It. 2 (D-09).

### 2.3 Estados, marca y desarrollo (14)

| Token | Valor | Rol | Semántica | Contraste verificado |
|---|---|---|---|---|
| `state.offTarget` [C] | `#FF4444` | Graphic | valor fuera de objetivo (`immersive_workout_screen.dart:581-582`) | 4,05 card · 5,10 `bg.app`. En el HUD el texto usa `state.offTarget.hud` |
| `brand.strava` [C] | `#FC4C02` | Graphic | todo lo de Strava (`history_screen.dart:99`, `profile_screen.dart:99`) | 4,06 card · 5,11 `bg.app`. Blanco encima 3,40: excepción de marca documentada (ART_BIBLE §3.6) |
| `state.success` | `#06D6A0` | Graphic | completado, reanudar | 7,32 card · `text.onAccent` encima 9,99 |
| `state.connected` | `#69F0AE` | UiText | texto del chip "Conectado", RECEIVING | 9,65 card · 7,03 sobre su chip |
| `state.connectedBg` | `#4CAF50 @20 %` | None | fondo del chip "Conectado" | fondo |
| `state.warning` | `#FF9800` | UiText | texto del aviso "remo no conectado" | 6,41 card · 5,01 sobre su aviso |
| `state.warningBg` | `#FF9800 @15 %` | None | fondo del aviso | fondo |
| `state.danger` | `#FF5252` | Graphic | contorno de terminar y desconectar (no es texto) | 4,33 card · 5,45 `bg.app` |
| `state.dangerText` | **`#FF7272`** (antes `#FF6B6B`) | UiText | texto de error y de Terminar | 5,20 card · 4,64 raised · 4,70 overlay · 7,04 sunken · 5,08 peor vidrio |
| `state.dangerFill` | `#D32F2F` | Graphic | relleno de "borrar rutina" | blanco encima 4,98 (2,77 contra card: el botón se identifica por su relleno con texto, no por el borde) |
| `dev.simulator` | `#FF5722` | Graphic | cinta SIMULADOR | 5,50 `bg.app` |
| `dev.simulatorText` | `#1A0A00` | None | texto de la cinta | 6,11 sobre `dev.simulator` |
| `dev.bleDebug` | `#18FFFF` | None | texto mono del diagnóstico BLE | 15,21 sobre su fondo |
| `dev.bleDebugBg` | `#000000 @38 %` | None | fondo del diagnóstico BLE | fondo |

### 2.4 Variantes del HUD y vidrio (15)

Mismo matiz que la base, más claras (OKLCH, ART_BIBLE §2.3). Valores de la tabla obligatoria de 02-tech-design §4.4, sin cambios.

| Token | Hex | Rol (umbral) | Uso | Peor vidrio |
|---|---|---|---|---|
| `metric.split.hud` | `#FEA845` | HudValue (7) | valor de Split | **7,004** |
| `metric.spm.hud` | `#38CBEF` | HudValue (7) | valor de SPM | **7,040** |
| `metric.watts.hud` | `#FFD60A` (= base) | HudValue (7) | valor de Watts | 9,557 |
| `metric.distance.hud` | `#4CC9F0` (= base) | HudValue (7) | valor de distancia | **7,016** |
| `metric.time.hud` | `#E0E0E0` (= base) | HudValue (7) | valor de tiempo | 10,221 |
| `metric.kcal.hud` | `#06D6A0` (= base) | HudValue (7) | valor de kcal | 7,154 |
| `text.hudPrimary` | `#FFFFFF` | HudValue (7) | valor con objetivo cumplido (Q-60) | 13,493 |
| `metric.hr.hud` | `#FF5A7E` | HudValueRed (4,5) | valor de BPM (D-ART-01) | **4,508** |
| `state.offTarget.hud` | `#FF5F59` | HudValueRed (4,5) | valor fuera de objetivo + flecha ▲/▼ + borde (D-ART-01) | **4,519** |
| `step.work.hud` | `#FF5A7E` | HudText (4,5) | texto chico de la barra de pasos y chips | **4,508** |
| `step.rest.hud` | `#06D6A0` (= base) | HudText (4,5) | ídem | 7,154 |
| `step.warmup.hud` | `#FFD166` (= base) | HudText (4,5) | ídem | 9,358 |
| `step.cooldown.hud` | `#35A0C9` | HudText (4,5) | ídem | **4,512** |
| `step.unknown.hud` | `#7C9AA8` | HudText (4,5) | ídem | **4,526** |
| `glass.fallback` | `#070D16 @86 %` | Surface | respaldo plano del vidrio (It. 4a-6a, nivel Bajo, sin `_RmHudBlur`; QA-P-17) | sobre blanco = `#2A2F37` (41,72; 46,88; 54,62) |

Etiquetas del HUD en color base (texto 11-13/600, umbral 4,5): split 6,57 · SPM 5,47 · watts 9,56 · distancia 7,02 · tiempo 10,22 · kcal 7,15. El pulso usa `metric.hr.hud` (la base da 3,73). Unidades `#FFFFFF @55 %` sobre el peor vidrio: 5,23.

### 2.5 Longitudes (28; dp, 1 px de USS = 1 dp)

| Grupo | Tokens |
|---|---|
| Espaciado (ART_BIBLE §2.4) | `space.dp2` 2 · `space.dp4` 4 · `space.dp6` 6 · `space.dp8` 8 · `space.dp12` 12 · `space.dp16` 16 · `space.dp20` 20 · `space.dp24` 24 · `space.dp32` 32 · `space.dp40` 40 · `space.dp48` 48 |
| Radios | `r.xs` 4 (píldora de paso) · `r.s` 6 (chips chicos) · `r.m` 8 (chips, cajas) · `r.l` 12 (botones, avisos, miniaturas) · `r.glass` 14 (vidrio HUD [C]) · `r.card` 16 (tarjetas, `theme.dart:16`) · `r.full` 999 (cápsulas) |
| Grosores | `stroke.hairline` 1 (vidrio, separadores) · `stroke.target` 1,5 (objetivo activo, contornos de botón) · `stroke.focus` 2 · `stroke.selected` 2,5 (miniatura elegida [C]) · `stroke.accent` 3 (acento del tablero y borde izquierdo de paso [C]) · `stroke.focusHalo` 4 |
| Desplazamientos (ANIMATION_BIBLE §1.3) | `shift.s` 8 · `shift.m` 12 · `shift.l` 16 · `shift.xl` 24 (tope para todo elemento del HUD) |

### 2.6 Movimiento (13)

Duraciones (ANIMATION_BIBLE §1.1): `dur.micro` 90 · `dur.fast` 160 · `dur.base` 240 · `dur.slow` 360 · `dur.page` 420 · `dur.scene` 900 · `dur.cinematic` 1600 ms.

Curvas (ANIMATION_BIBLE §1.2). En UI Toolkit manda la palabra clave de USS (Unity evalúa la función exacta de `EasingMode`). El `cubic-bezier` es la aproximación estándar para código C# que necesite la misma curva; error máximo medido contra la función de Unity en 2001 puntos:

| Token | USS | `cubic-bezier` | Error máx. | Uso |
|---|---|---|---|---|
| `ease.enter` | `ease-out-cubic` | 0,33, 1, 0,68, 1 | 0,0028 | todo lo que entra |
| `ease.exit` | `ease-in-cubic` | 0,32, 0, 0,67, 0 | 0,0028 | todo lo que sale |
| `ease.move` | `ease-in-out-cubic` | 0,65, 0, 0,35, 1 | 0,0095 | desplazamientos y tamaños |
| `ease.pulse` | `ease-in-out-sine` | 0,37, 0, 0,63, 1 | 0,0020 | pulsos y respiraciones |
| `ease.emphasis` | `ease-out-back` | 0,34, **1,56**, 0,64, 1 | 0,0049 | solo sellos de éxito y "solidificar" la píldora |
| `ease.linear` | `linear` | 0, 0, 1, 1 | 0 | progreso, brillos en barrido |

`y1 = 1,56` de `ease.emphasis` es válido (el rebote sale de `[0; 1]` en y; x siempre queda en `[0; 1]`). Si WP-A valida coordenadas, solo x se limita.

### 2.7 Tipografía (17; tamaño / alto de línea en dp, peso)

| Token | Familia | Tamaño / línea | Peso | Uso |
|---|---|---|---|---|
| `display` | Inter (Ui) | 32 / 40 | 700 | título del resumen final |
| `headline` | Inter | 24 / 32 | 700 | títulos de vistas grandes |
| `title` | Inter | 20 / 28 | 600 | AppBar, diálogos |
| `titleS` | Inter | 16 / 22 | 600 | secciones, nombres en tarjetas |
| `body` | Inter | 15 / 22 | 400 | texto general |
| `bodyS` | Inter | 13 / 18 | 400 | subtítulos, resúmenes |
| `label` | Inter | 12 / 16 | 600 | etiquetas de métrica (mayúsculas, tracking 0,08 em) |
| `caption` | Inter | 11 / 14 | 500 | fechas, ayudas, ejes (mínimo legible) |
| `num.hero` | Barlow SC (Numeric) | 64 / 64 | 800 | SPM del HUD (D-ART-02) |
| `num.xl` | Barlow SC | 52 / 52 | 700 | tablero de Dispositivo, pulso |
| `num.l` | Barlow SC | 44 / 44 | 700 | Split y Watts del HUD |
| `num.m` | Barlow SC | 32 / 34 | 700 | kcal y BPM del HUD, resumen final |
| `num.band` | Barlow SC | 34 / 34 | 700 | banda tiempo │ distancia (nuevo, §6) |
| `num.lap` | Barlow SC | 34 / 36 | 700 | vuelta en curso |
| `num.row` | Barlow SC | 24 / 28 | 700 | filas de vueltas y repeticiones |
| `num.s` | Barlow SC | 20 / 22 | 700 | cuenta regresiva, reloj |
| `num.stat` | Barlow SC | 16 / 20 | 700 | stats de historial, chips numéricos |

El mecanismo no tiene `letter-spacing` ni mayúsculas, y el USS emite solo el tamaño (`--rm-<token>-size`). Tracking, mayúsculas y `-unity-font-definition` van en las clases USS de tipo que arma la It. 2 (§7).

---

## 3. Paridad con el legacy

- Los **21 hex de contrato** coinciden carácter por carácter con el legacy (leídos en las líneas citadas en §2.1-§2.3): base 4, métricas 7, pasos 5, fases 3, fuera de objetivo y Strava.
- **Colores de tipo de paso** (AC-RUT-01-03): `work` `#EF476F`, `rest` `#06D6A0`, `warmup` `#FFD166`, `cooldown` `#118AB2`, otro `#607D8B`, como `stepColor` de `theme.dart:56-69` (el `default` cubre vacío, `sprint` y `null`).
- **Colores de métrica**: los 7 de `MetricColors` (`theme.dart:32-40`).
- **No se migran** los colores de la barra inferior del tema (`#12243A`, `#607D8B`, `theme.dart:24-28`), que el legacy no aplica (DC-58). `#00B4D8` existe como `metric.spm` y `accent.primary` por su propia semántica, no como color de esa barra.

---

## 4. Contraste verificado

### 4.1 Método y peor fondo

El de 02-tech-design §4.4 (respuesta a P-05). Peor vidrio: `#070D16 @86 %` sobre `#FFFFFF` = (41,72; 46,88; 54,62), que redondeado es `#2A2F37`, L = 0,02782. Los números de §4.4 del Architect coinciden con los míos hasta la tercera decimal (7,004 · 7,016 · 7,040 · 13,49 · 4,508 · 4,519 · 4,512 · 7,56 · 5,14).

### 4.2 Por rol (lo que mide `ContrastTests`)

| Rol | Fondo | Umbral | Tokens (mínimo) |
|---|---|---|---|
| UiTextPrimary | `surface.card` | 7 | `text.primary` 13,81 |
| UiText | `surface.card` | 4,5 | `text.secondary` 7,56 · `text.tertiary` 5,14 · `accent.primary` 5,60 · `state.connected` 9,65 · `state.warning` 6,41 · `state.dangerText` 5,20 |
| HudValue | peor vidrio | 7 | mínimo `metric.split.hud` 7,004 |
| HudValueRed | peor vidrio | 4,5 | mínimo `metric.hr.hud` 4,508 |
| HudText | peor vidrio | 4,5 | mínimo `step.work.hud` 4,508 |

Los 21 tokens con rol de texto o valor pasan. **P-05 queda cerrada con el método**: hay 8 tokens con margen ≤ 0,04 (en negrita en §2.4). Contra el fondo cuantizado a 8 bits (`#2A2F37` exacto), cuatro quedan hasta 0,013 por debajo: split 6,987, distancia 6,999 y `#FF5A7E` 4,497 (pulso y trabajo). Es una diferencia imperceptible. No subo esos valores: AC-BASE-05 nombra `#FF5A7E` y `#FF5F59`, y el riesgo real es otro (R-T1, §5).

### 4.3 Pares que pide el Designer (para `ContrastTests`)

El rol `Graphic` dice "≥ 3:1 si el Designer lo pide", y algunos textos no van sobre `surface.card`. Pido que `ContrastTests` (WP-A) agregue esta lista como datos del test (`DesignerPairs`: frente, fondo con su composición, umbral). **No** va en `Tokens.cs`, que solo declara tokens. Todos pasan hoy:

| # | Frente | Fondo | Umbral | Valor |
|---|---|---|---|---|
| G1 | los 7 `metric.*`, los 5 `step.*`, los 3 `phase.*`, `state.offTarget`, `brand.strava`, `state.success`, `state.danger`, `focus.ring`, `accent.primary` | `surface.card` y `bg.app` | 3 | mínimo `step.unknown`/card 3,16 |
| G2 | `brand.seed` | `bg.app` | 3 | 3,57 |
| G3 | `dev.simulator` | `bg.app` | 3 | 5,50 |
| T1 | `text.onAccent` | `accent.primary` · `accent.primaryPressed` · `state.success` · `metric.watts` | 4,5 | 7,64 · 5,40 · 9,99 · 13,34 |
| T2 | `text.primary` | `state.dangerFill` · `brand.seed` | 4,5 | 4,98 · 4,87 |
| T3 | `dev.simulatorText` | `dev.simulator` | 4,5 | 6,11 |
| T4 | `dev.bleDebug` | `dev.bleDebugBg` sobre `bg.app` | 4,5 | 15,21 |
| T5 | `state.connected` | `state.connectedBg` sobre card | 4,5 | 7,03 |
| T6 | `state.warning` | `state.warningBg` sobre card | 4,5 | 5,01 |
| T7 | `accent.primary` · `text.secondary` | `surface.nav` sobre blanco (peor caso de la barra) | 4,5 | 6,02 · 8,00 |
| T8 | `text.secondary` · `text.tertiary` · `accent.primary` · `state.dangerText` | `bg.app` · `bg.sunken` · `surface.raised` · `surface.overlay` | 4,5 | mínimo `state.dangerText`/raised 4,64 |
| T9 | etiquetas HUD: `metric.split` · `metric.spm` · `metric.watts` · `metric.distance` · `metric.time` · `metric.kcal` | peor vidrio | 4,5 | mínimo `metric.spm` 5,47 |
| T10 | `state.dangerText` (botón Terminar) | peor vidrio | 4,5 | 5,08 |

### 4.4 Excepciones conocidas (no se testean como umbral)

- `text.disabled` 3,31: WCAG exime el texto deshabilitado.
- Blanco sobre `brand.strava` 3,40: lo impone la guía de marca de Strava (ART_BIBLE §3.6).
- `border.strong` 2,15 sobre card: el botón contorno se identifica por su texto.
- `brand.seed` 2,84 y `state.dangerFill` 2,77 sobre card: son rellenos con texto blanco encima (T2), no bordes contra la tarjeta.

---

## 5. R-T1 (mezcla lineal): análisis y recomendación del Designer

Si Unity mezcla la UI en espacio **lineal** (el proyecto es Linear), el peor vidrio sobre blanco ya no es `#2A2F37` sino **`#696A6B`** (L = 0,1434). Con `@86 %`:

| Token | sRGB (test) | Lineal |
|---|---|---|
| `text.hudPrimary` | 13,49 | 5,43 |
| `metric.split.hud` | 7,00 | 2,82 |
| `metric.hr.hud` | 4,51 | 1,81 |

Para cumplir en lineal haría falta un tinte de **≥ 97,6 %** en todas las métricas (90,4 % solo para el blanco). Es decir, un vidrio opaco, que pierde el carácter del HUD. Con 90 % el blanco llega a 6,84 y split a 3,55; con 95 %, a 10,12 y 5,26: tampoco alcanza.

**Recomendación: opción A, UI compuesta en espacio gamma** (`PanelSettings.forceGammaRendering` con *render texture* UNORM, como lo nombra el Architect). Por qué:

1. Es la única que cubre el **respaldo sin desenfoque** (It. 4a-6a y nivel Bajo). La opción B (mezcla propia en el material) necesita leer el fondo, y sin `_RmHudBlur` el material no tiene acceso al *framebuffer*. La opción C (alfa ≥ 97,6 %) apaga el vidrio.
2. Es como compone el **legacy** (Flutter/Skia mezcla en sRGB). Los degradés, los `@NN %` de ART_BIBLE y los bordes `@8 %` se ven como en el legacy, y el método del test describe lo que se ve.
3. Mantiene exactos los colores de los tokens (la UI se compone después del post y sin tonemapping, ART_BIBLE §6.3).

Nota: en lineal, el texto translúcido claro sobre fondo oscuro **mejora** (`text.secondary` sobre card da 9,97 y `text.tertiary` 7,92). El problema es solo vidrio oscuro sobre cielo claro, que es justamente el caso del HUD.

**Medición en la It. 2** (con Editor; si falla, se decide antes de la It. 4a): un `VisualElement` con `--rm-glass-fallback` sobre un fondo blanco, captura y lectura del píxel. Si da `#2A2F37` ± 1, la mezcla es sRGB y el test describe la realidad. Si da ≈ `#696A6B`, la mezcla es lineal y se aplica la opción A. Mismo chequeo con `text.secondary` sobre `surface.card` (esperado ≈ `#BAC0C7`).

---

## 6. Cambios respecto de ART_BIBLE (se aplican en ART_BIBLE en el cierre, 02-tech-design §10.4)

| Qué | Antes | Ahora | Por qué |
|---|---|---|---|
| `state.dangerText` | `#FF6B6B` | `#FF7272` (mismo matiz 0°) | 4,44 sobre `surface.raised` y 4,50 sobre `surface.overlay` (texto de error y Terminar en diálogos). Ahora: 4,64 y 4,70, con 5,20 sobre card y 5,08 en el botón Terminar del HUD (ART_BIBLE §3.5 decía 4,9) |
| `num.m` | "32–34 / 34" | `num.m` 32/34 y **`num.band`** 34/34 | Un token tiene un solo tamaño. La banda tiempo │ distancia usa 34 (D-ART-02); kcal y BPM, 32 |
| `focus.ring` | "`#00B4D8` 2 px + `#00B4D8 @30 %` 4 px" | `focus.ring` + **`focus.ringHalo`** + `stroke.focus` 2 / `stroke.focusHalo` 4 | Un valor por token |
| `state.connected` / `state.warning` | "`#69F0AE` sobre `#4CAF50 @20 %`", "`#FF9800` sobre `@15 %`" | + **`state.connectedBg`**, **`state.warningBg`** | Ídem |
| `dev.simulator` / `dev.bleDebug` | texto y fondo en la misma celda | + **`dev.simulatorText`**, **`dev.bleDebugBg`** | Ídem |
| Escala de espaciado y grosores | solo valores | `space.dp*`, `stroke.*`, `shift.*` | Nombres válidos para el mecanismo |
| §9 (P-08) | "Diferencia abierta: … `@80 %`" | `glass.fallback` (`#070D16 @86 %`), sin diferencia con ARCHITECTURE §20.1 | Ya resuelto (02-tech-design §10.3) |

Ningún cambio toca un color de contrato ni un valor que nombre la spec del PO.

---

## 7. Qué queda para la It. 2 (y después)

**It. 2 (shell, Dispositivo, tema):**

- **Clases USS de tipo** (`.rm-type-<token>`): `font-size: var(--rm-…-size)`, `-unity-font-definition` (Inter/Barlow con sus Font Assets, ASSET_SLOTS §3), `letter-spacing` (0,08 em en `label` y chips de fase) y mayúsculas en el texto. Hacen falta las fuentes descargadas.
- **`Icons.cs`** (D-09): codepoints de ASSET_SLOTS §4.3; `pause_circle_outline` del legacy → `pause_circle` `E1A2` de la fuente sin relleno (P-07).
- **Tablero de Dispositivo** (ajuste que propongo a ART_BIBLE §3.1, con números): las etiquetas "color base `@85 %`" no llegan a 4,5 en SPM (4,48) ni en pulso (3,07). Propuesta: etiquetas en color base `@100 %` (SPM 5,60) y la de pulso en `metric.hr.hud` (4,61). Valores `num.xl`: color base (texto grande, ≥ 3; el pulso da 3,82) o, si el PO quiere AAA, las variantes `*.hud` sobre card (mínimo 4,61).
- **Resortes** `spring.snappy/smooth/gentle` (ω 22/12/6): el mecanismo no tiene un `TokenKind` para ellos. Propuesta al Architect: `SpringToken.Omega(name, radPerSec)` cuando llegue `CriticalSpring`; mientras tanto viven en ANIMATION_BIBLE §1.2.
- **Layout** (ART_BIBLE §5 y §10.4): márgenes y gutters por clase de tamaño (16/12, 24/16, 32/24), anchos máximos (720/840/960), alto de la barra de pestañas (64), áreas táctiles (48 y 56 en el inmersivo), tamaños de ícono (14-64) y `hudScale` (390 de referencia, 0,9-1,6). Entran como `LengthToken` o constantes de layout, según decida el Architect.
- **Variantes alfa de componentes** que USS necesite como variable fija (botón tonal `#00B4D8 @16 %`, *skeleton* `#FFFFFF @6 %` / `@8 %`, caja de resumen `@4 %`, grilla del gráfico `@8 %`). Las que dependen de un color dinámico (fase, paso, métrica) las calcula el código desde el token base, sin variable USS.
- **Verificar con Editor (NV hoy):** `var(--rm-ease-*)` en `transition-timing-function` y `var(--rm-dur-*)` en `transition-duration`. Si USS no resuelve variables en esas propiedades, el generador emite clases de transición en lugar de variables. También la medición de R-T1 (§5).

**It. 3:** el título del paso en el editor (14/600 en color de paso sobre card) no llega a AA con la base: `work` 3,82, `cooldown` 3,49, `unknown` 3,16. Propuesta: texto en `step.*.hud` (work 4,61, cooldown 4,62, unknown 4,63; rest y warmup ya pasan). El borde izquierdo y el chip siguen en la base [C].

**It. 4a:** tokens del vidrio (borde `#FFFFFF @10 %` [C], reflejo interior `@6 %`, alfa base 0,68/0,72 de banda y barra), *scrims* (`#06122A @38 %` / `@32 %`) y la decisión de R-T1.

**It. 6a:** la paleta por hora (`SceneTimeOfDay`, 6 keyframes × 8 campos, ART_BIBLE §6.1) no pasa por USS. Hay que decidir con el Architect si vive en `Tokens.cs` (`scene.*`) o en `SceneTimeOfDay` con constructores numéricos, ya que `PureCodeRulesTests` prohíbe literales `"#RRGGBB"` fuera de `Tokens.cs`.

---

## 8. Notas para WP-A (sin escribir su código)

- `Tokens.cs` ya existe: WP-A **no** lo crea ni lo pisa (02-tech-design §1.1). Tiene 122 campos y los 38 obligatorios de §4.4 con nombre, valor, alfa, rol y contrato exactos.
- Longitudes fraccionarias: `stroke.target` 1,5 y `stroke.selected` 2,5 → `1.5px` y `2.5px` (con `Fmt.DartToString`). Alfas no redondas: 0,3 (`focus.ringHalo`), 0,92, 0,18, etc.
- Con el escritor descrito en §4.5, el USS tiene 122 variables, una por token (todas las curvas tienen palabra clave), sin colisiones de nombre, incluido el sufijo `-size` de tipografía.
- `ease.emphasis` tiene `Y1 = 1,56` (válido, §2.6).

---

## 9. Verificación hecha

1. Lectura del legacy (solo lectura) de cada línea de contrato citada. `git -C rowmate-legacy status --porcelain` vacío al terminar.
2. `Tokens.cs` compilado tal cual contra un *stub* de la API de 02-tech-design §4.2 (C# 9, `netstandard2.1` y `net8.0`, `TreatWarningsAsErrors`), en el directorio temporal de la sesión, fuera del repo.
3. Programa de verificación sobre ese *stub*: regex y unicidad de nombres; nombre de campo = `FieldNameFor`; los 38 obligatorios de §4.4 (nombre, hex, alfa, rol, contrato); exactamente 21 contratos; peor vidrio = `#2A2F37`; todos los roles (§4.2) y pares (§4.3) con el método de §4.4; cálculo lineal informativo (§5); error de las curvas (§2.6); vista previa del USS. Resultado: **0 fallas**.
4. Los números de este documento salen de esa corrida. Cuando WP-A implemente `Contrast` y `ContrastTests`, tienen que dar los mismos valores hasta la tercera decimal.
