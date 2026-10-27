# It. 2 — Deuda de diseño de la It. 1 y decisión de íconos

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-27 |
| Iteración | 2 · `02-dispositivo-simulador` ([ROADMAP §It. 2](../../ROADMAP.md)), antes de la spec del PO |
| Entrada | QA-V-09 ([05-qa-verification §6](../01-fundaciones/05-qa-verification.md)), [04-design-notes §6 y §7](../01-fundaciones/04-design-notes.md), [REPORT §7.2](../01-fundaciones/REPORT.md), `Assets/RowMate/Core/Design/Tokens.cs` (manda), [ASSET_SLOTS §4](../../design/ASSET_SLOTS.md), ARCHITECTURE §1.2, ADR 001 |
| Entrega | Este documento + secciones "Cambios al iniciar la It. 2" en [ART_BIBLE](../../design/ART_BIBLE.md), [ASSET_SLOTS](../../design/ASSET_SLOTS.md) y [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md). Sin código: `Tokens.cs` no cambia |
| Legacy leído (solo lectura) | `lib/main.dart:150-175`, `lib/features/device/device_screen.dart:20-30, 90-170, 244-330, 365-470`, `lib/features/device/heart_rate_card.dart:25-60, 100-225`, `lib/core/dev/simulator_overlay.dart:35-212`, `test/simulator_overlay_test.dart:40, 113`. No se leyó `strava_secrets.dart`. `git status --porcelain` vacío antes y después |
| Verificado | Codepoints contra el archivo fijado; receta de fuentes ejecutada; Font Asset y atlas SDF en Unity 6000.3.27f1; importador SVG integrado; contrastes con el método de 02-tech-design §4.4 (§7) |

Convenciones: las de 04-design-notes (hex `#RRGGBB`, transparencia `@NN %`, contraste WCAG 2.x con mezcla sRGB sin redondear). "Peor vidrio" = `glass.fallback` sobre blanco = `#2A2F37`.

---

## 0. Resumen

1. **QA-V-09 cerrado.** ART_BIBLE §9 ya no tiene la "diferencia abierta" del vidrio: nombra el token `glass.fallback` (`#070D16 @86 %`), igual que ARCHITECTURE.
2. **ART_BIBLE alineada con `Tokens.cs`.** Entraron todos los cambios de 04-design-notes §6 (`state.dangerText` `#FF7272`, `num.band`, `focus.ringHalo`, `state.connectedBg` / `warningBg`, `dev.simulatorText` / `bleDebugBg`, escalas `space` / `stroke` / `shift`) y un número viejo que encontré al revisar (`text.hudPrimary`, §2.1). Ningún valor de `Tokens.cs` cambia.
3. **Ajustes para las pantallas de la It. 2** (de 04-design-notes §7 y de la lectura del legacy): etiquetas del tablero de Dispositivo y del pulsómetro con contraste AA, corazón relleno como el legacy, etiqueta de pestaña fuera de la píldora (§2.2).
4. **Íconos: decisión final = glifos de fuente** ("RowMate Icons", Material Symbols Rounded recortada). No se usa SVG. Ojo con la premisa: el *paquete* de Vector Graphics no es *released*, pero el Editor 6000.3.27f1 **sí** trae un *módulo* integrado que importa SVG como `VectorImage` (lo verifiqué). La decisión se apoya en otros motivos (§3.2). ARCHITECTURE §1.2 y ADR 001 ya decían glifos: el punto "abierto" solo seguía abierto en ASSET_SLOTS, STATUS y el REPORT.
5. **La It. 2 usa 22 glifos** (20 de contorno y 2 rellenos), todos dentro del recorte que ya fija ASSET_SLOTS §4.1: el comando no cambia (§3.5).
6. **Respaldo sin fuentes:** íconos provisorios dibujados por código con la misma caja, color y animación, y nunca un "□"; el texto usa la fuente por defecto de Unity con los tamaños de `Tokens.cs` (§4).

---

## 1. QA-V-09 (ART_BIBLE §9)

| | Texto |
|---|---|
| Antes | "Diferencia abierta: para el vidrio del nivel Bajo, ARCHITECTURE §20.1 dice `#070D16 @80 %`, y tiene que ser `@86 %` (§3.2, QA-P-17)." |
| Ahora | "El vidrio del nivel Bajo es el respaldo plano, token `glass.fallback` (`#070D16 @86 %`, §3.2, QA-P-17), el mismo valor que usan ARCHITECTURE §7.4, §20.1 y RA-07 (P-08, cerrada en la It. 1)." |

Comprobado: ARCHITECTURE ya no dice `@80 %` en ningún lado (§1.1, §7.4, §7.7, §20.1, RA-07, P-04 y P-08 dicen `@86 %` o nombran el token) y `Tokens.cs:90` declara `glass.fallback` = `#070D16`, 86, `Surface`.

---

## 2. ART_BIBLE alineada con `Tokens.cs`

### 2.1 Cambios de 04-design-notes §6 (y un número viejo)

| # | ART_BIBLE | Antes | Ahora | `Tokens.cs` |
|---|---|---|---|---|
| 1 | §2 | "dos lugares que se generan de una sola fuente… Propuesta para el Architect en §11" | `Tokens.cs` es la fuente única (122 tokens, ADR 016) y **manda** si algo no coincide; un valor por token | todo el archivo |
| 2 | §2.1 | `focus.ring` = "`#00B4D8` 2 px + `#00B4D8 @30 %` 4 px" | `focus.ring` `#00B4D8` (`stroke.focus` 2) + **`focus.ringHalo`** `#00B4D8 @30 %` (`stroke.focusHalo` 4) | :25-26, :117, :120 |
| 3 | §2.2 | `state.connected` "`#69F0AE` sobre `#4CAF50 @20 %`" | `state.connected` `#69F0AE` (9,65:1 card; 7,03:1 sobre su chip) + **`state.connectedBg`** `#4CAF50 @20 %` | :60-61 |
| 4 | §2.2 | `state.warning` "`#FF9800` sobre `@15 %`" | `state.warning` `#FF9800` (6,41:1 card; 5,01:1 sobre su aviso) + **`state.warningBg`** `#FF9800 @15 %` | :62-63 |
| 5 | §2.2 | `state.dangerText` `#FF6B6B` (5,0:1) | **`#FF7272`**, mismo matiz: 5,20 card · 4,64 `surface.raised` (antes 4,44) · 4,70 `surface.overlay` (antes 4,50) · 5,08 peor vidrio | :65 |
| 6 | §2.2 | `dev.simulator` "(texto `#1A0A00`, 6,1:1)" | `dev.simulator` `#FF5722` + **`dev.simulatorText`** `#1A0A00` (6,11:1) | :68-69 |
| 7 | §2.2 | `dev.bleDebug` "`#18FFFF` mono sobre `#000000 @38 %`" | `dev.bleDebug` `#18FFFF` (15,21:1) + **`dev.bleDebugBg`** `#000000 @38 %` | :70-71 |
| 8 | §2.2 | íconos de paso: "descanso `pause_circle`" | + `pause_circle` `E1A2` **sin relleno** = `pause_circle_outline` del legacy (P-07) | (dato de `StepTypeInfo.IconName`) |
| 9 | §2.3 | `text.hudPrimary` "10,1:1 (vidrio 78 %)" | **13,49:1** en el peor vidrio. Número viejo: no seguía el método de P-05 (el peor caso es con `@86 %`) | :87 |
| 10 | §2.3 | "vidrio `#070D16 @86 %`" | + nombre del token `glass.fallback` | :90 |
| 11 | §2.4 | "Escala de espaciado: `2, 4, … 48`"; bordes sin nombre | `space.dp2` … `space.dp48`; `stroke.hairline` 1 · `target` 1,5 · `focus` 2 · `selected` 2,5 · `accent` 3 · `focusHalo` 4; `shift.s` 8 · `m` 12 · `l` 16 · `xl` 24 | :93-126 |
| 12 | §3.2 | banda: "valor `num.m` 34" | **`num.band`** 34 | :160 |
| 13 | §3.5 | Terminar: `#FF6B6B`, "4,9:1 en el peor caso" | `#FF7272`, **5,08:1** en el peor vidrio | :65 |
| 14 | §3.11 | cinta: "texto … `#1A0A00`" | `dev.simulatorText` | :69 |
| 15 | §4.3 | `num.m` "32–34 / 34 … kcal, BPM, banda" | `num.m` 32/34 (kcal, BPM, resumen) + **`num.band`** 34/34 (banda) | :159-160 |
| 16 | §9 | diferencia abierta `@80 %` | §1 de este documento (QA-V-09) | :90 |
| 17 | §10.2 | "32–34 dp" | "32 (`num.m`) – 34 (`num.band`) dp" | :159-160 |

Control cruzado: todos los nombres de token que aparecen entre comillas invertidas en ART_BIBLE, ANIMATION_BIBLE y ASSET_SLOTS existen en `Tokens.cs` (salvo los comodines `metric.*.hud`, `step.*` y los ids de slot `brand.appIcon` / `brand.splash`, que no son tokens). ANIMATION_BIBLE §1.3 nombra ahora `shift.*` y aclara que los resortes `spring.*` todavía no son tokens.

**Lo que no toqué** (es de otra iteración y ya está anotado en 04-design-notes §7): el título del paso en el editor (ART_BIBLE §3.8, It. 3: `work` 3,82, `cooldown` 3,49 y `unknown` 3,16 sobre card; propuesta `step.*.hud`), los tokens del vidrio de la 4a y la paleta por hora de la 6a.

### 2.2 Ajustes para las pantallas de la It. 2

| ART_BIBLE | Qué | Por qué (números sobre `surface.card`) |
|---|---|---|
| §3.1 encabezado del tablero | Ícono 14 y etiqueta `label` 12/600 en el **color base `@100 %`**; el pulso, en **`metric.hr.hud`** | ART_BIBLE decía `@85 %`: SPM 4,48 y pulso 3,07, no llegan a 4,5. El legacy usa `@70 %` (`device_screen.dart:310, :315`): split 3,99, SPM 3,51, distancia 4,33 y pulso 2,45. Con el cambio: split 6,73, SPM 5,60, watts 9,78, distancia 7,18 y pulso 4,61. Es el mismo criterio que D-ART-03, que el ROADMAP ya aplica en la It. 2 |
| §3.1 valor del tablero | `num.xl` en el color base, sin cambio; se aclara que es texto grande (≥ 3:1) | mínimo pulso 3,82 |
| §3.1 pulsómetro | El corazón del encabezado es `favorite` **relleno siempre**, como el legacy (`heart_rate_card.dart:39`), y baja a `@40 %` sin conexión [CONTRATO]. El contorno queda para las filas de pulsómetros encontrados (`favorite_border`, `:138`). Título "PULSÓMETRO" en `metric.hr.hud` | ASSET_SLOTS decía "contorno `@40 %` sin conexión", que no es lo que hace el legacy. Título: el `@70 %` del legacy da 2,45; `metric.hr.hud` da 4,61 |
| §3.11 barra de pestañas | La píldora `nav.indicator` (56×32) va detrás del ícono y la etiqueta **debajo**, nunca adentro | `accent.primary` sobre la píldora, en el peor caso de la barra (contenido blanco debajo), da 4,41: alcanza para el ícono (gráfico, ≥ 3) pero no para texto. Fuera de la píldora, 6,02 |

---

## 3. Íconos: decisión final

### 3.1 Qué hay en Unity 6000.3.27f1 (verificado con el Editor instalado)

| Pieza | Estado | Cómo se verificó |
|---|---|---|
| Paquete `com.unity.vectorgraphics` | Solo *preview* (`3.0.0-preview.7`); no está en el manifest | ARCHITECTURE §1.2 y `Packages/manifest.json` |
| Módulo `com.unity.modules.vectorgraphics` 1.0.0 | **Existe** como módulo integrado del Editor (`Editor/Data/Resources/PackageManager/BuiltInPackages/`, con `UnityEditor.VectorGraphicsModule.dll`). No está en el manifest del proyecto | En un proyecto de prueba **descartable** (scratchpad, fuera del repo) con el módulo agregado: el importador es `Unity.VectorGraphics.Editor.SVGImporter`, su tipo por defecto es `VectorImage` con teselado `AntialiasedArcEncodings`, y el asset principal es `UnityEngine.UIElements.VectorImage`. O sea: **sí hay camino SVG → UI Toolkit sin paquete** |
| TextCore con la fuente recortada | Funciona | Mismo proyecto: `FontAsset.CreateFontAsset(fuente, 64, 6, SDFAA, 512, 512, Dynamic)` acepta los 42 codepoints de contorno y los 7 rellenos (0 faltantes); el atlas SDF (Alpha8) muestra cada glifo bien dibujado |
| `Painter2D.SaveToVectorImage` | Existe en `UnityEngine.UIElementsModule` (sin el módulo de Vector Graphics) | documentación XML de la API instalada |

La premisa "UI Toolkit de Unity 6.3 no trae vector graphics como paquete *released*" es correcta para el **paquete**, pero no alcanza para descartar el SVG, porque el **módulo** integrado sí lo importa. Por eso la decisión se compara de verdad.

### 3.2 Comparación

| Criterio | Glifos ("RowMate Icons", `FontAsset` SDF) | SVG → `VectorImage` (módulo integrado) |
|---|---|---|
| Dependencias | Ninguna nueva (TextCore viene con UIElements, ya en el manifest) | Agregar `com.unity.modules.vectorgraphics` al manifest (decisión del Architect; ARCHITECTURE §1.2 hoy no lo agrega) |
| Archivos | 2 `.ttf` de 9 KB y 3 KB + `LICENSE`, `.codepoints` y `NOTICE.md` | 49 `.svg` (42 contorno + 7 relleno), cada uno con su `.meta` |
| Cómo los consigue el usuario | 3 descargas fijadas + 4 comandos (o 1 *script*, §5) | 49 exportaciones a mano desde fonts.google.com, cada una con 4 ajustes (Rounded, 500, Grade 0, Optical size 24); el sitio sirve la versión de hoy, no la fijada |
| Verificación sin Editor | Test de dotnet: cada nombre de `Icons.cs` con su codepoint contra el `.codepoints` versionado | Ninguna: solo que el archivo exista |
| Color por token | `color` de USS, con transición; igual que el texto | `-unity-background-image-tint-color` (se puede, pero el tinte de un `VectorImage` no está verificado en este proyecto) |
| Dibujo | Quads del mismo atlas que el texto, un `Label` por ícono | Una malla teselada por ícono, como `background-image` |
| Nitidez con `hudScale` hasta 1,6 y 64 dp | SDF muestreado a 64 pt con 10 % de *padding*: nítido hasta ≈ 2× | Vectorial: nítido a cualquier tamaño |
| Alineación con el texto | Nativa (mismo motor de texto, métricas simétricas, ASSET_SLOTS §4.2) | Caja aparte; hay que centrarla |
| Multicolor | No | Sí (en la paridad no hay ningún ícono multicolor) |
| Costo en el HUD (tope de 60 elementos) | 1 elemento por ícono, sin texturas extra | 1 elemento por ícono |

### 3.3 Decisión

**Glifos de fuente.** Material Symbols Rounded, versión fijada (commit `10ecc14`), dos instancias estáticas (sin relleno y con relleno, Weight 500, Grade 0, Optical size 24), recortadas a los glifos de la app, como `FontAsset` SDF en `Assets/RowMate/UI/Icons/`, teñidas con el color del token. Constantes en `Core/Design/Icons.cs` con test de dotnet contra el `.codepoints`.

- **SVG no se usa** y el módulo `com.unity.modules.vectorgraphics` **no se agrega** al manifest. Queda reservado para un futuro ícono multicolor, con aprobación de QA y decisión del Architect.
- Los **PNG** quedan como último recurso, solo si la captura con las fuentes muestra glifos mal dibujados (la prueba del atlas no lo anticipa).
- Strava sigue en PNG por su guía de marca (ASSET_SLOTS §6) y el ícono de la app, en PNG hecho fuera de Unity.
- **Estado entre documentos:** ARCHITECTURE §1.2 y ADR 001 ya dicen esto (sin cambios). Quedaron alineados ASSET_SLOTS §4 y §5.1 y ART_BIBLE §3, §4.1 y su tabla de cambios. Quedan con el texto viejo ("punto abierto") STATUS.md y el REPORT de la It. 1: son del PO (§6).

### 3.4 Reglas de uso en UI Toolkit (It. 2)

- `.rm-icon`: `-unity-font-definition` de contorno, `-unity-text-align: middle-center`, `white-space: nowrap`, sin `letter-spacing`; `font-size` = tamaño del ícono y caja cuadrada de ese lado. `.rm-icon--filled` cambia a la fuente rellena.
- Métricas medidas: 960 unidades por em, avance 960 en todos los glifos, ascendente 1 056 y descendente 96. La línea mide 1,2 em y es **simétrica** respecto del dibujo (0 a 960): se centra sola, y el pivote de las escalas (latido, pestaña) es el centro de la caja.
- Atlas: 512×512 el de contorno (los 42 glifos ocupan alrededor del 60 %) y 256×256 el relleno (7 glifos).
- Color: siempre un token (tabla de §3.5). Íconos decorativos y deshabilitados: `@24 %` a `@38 %` de blanco, exentos de contraste porque el estado lo dice el texto.
- Área táctil ≥ 48×48 aunque el ícono mida 18 o 24 (ART_BIBLE §3.6).
- La pestaña seleccionada cambia de glifo en el cuadro 0 y funde el color en 160 ms (ANIMATION_BIBLE §2.1). La etiqueta va debajo de la píldora (§2.2).
- Los tests portados no buscan íconos por su glifo. El legacy abre el panel con `find.byIcon(Icons.build)` (`simulator_overlay_test.dart:40, :113`); en Unity el botón se busca por su nombre de elemento estable (propuesta: `sim-open`, en la línea de las *keys* del legacy como `sim-close`). Así el test no depende de que la fuente esté descargada.

### 3.5 Íconos de la It. 2

Codepoints del `.codepoints` de `10ecc14` (verificados, §7). Tamaños en dp a escala 1. "Receta" = ícono provisorio de ASSET_SLOTS §4.4.

**Barra de pestañas** (24 dp; etiqueta 12/600 debajo). Perfil (`person` `F0D3`) no aparece en la It. 2: el legacy lo muestra solo con Strava configurado (`main.dart:171`), It. 12-13.

| Destino | No seleccionada | Seleccionada | Color no sel. / sel. | Legacy |
|---|---|---|---|---|
| Dispositivo | `bluetooth` `E1A7` contorno | `bluetooth_connected` `E1A8` contorno | `text.secondary` / `accent.primary` | `main.dart:152-153` |
| Workout | `timer` `E425` contorno | `timer` `E425` **relleno** | ídem | `:157-158` (`timer_outlined` / `timer`) |
| Rutinas | `fitness_center` `EB43` contorno | el mismo | ídem | `:162-163` |
| Historial | `history` `E8B3` contorno | el mismo | ídem | `:167-168` |

Las tres pestañas con contenido provisorio usan su mismo ícono a 56 dp `@24 %` en el estado vacío (ART_BIBLE §3.10).

**Dispositivo, conectado**

| Lugar | Ícono | Codepoint | Fuente | dp | Color | Legacy |
|---|---|---|---|---|---|---|
| Encabezado (nombre del remo) | `rowing` | `E921` | contorno | 32 | `accent.primary` (5,60) | `device_screen.dart:96` |
| Chip "Conectado" | `bluetooth_connected` | `E1A8` | contorno | 14 (legacy 12) | `state.connected` (7,03 sobre su chip) | `:110` |
| AppBar "Desconectar" | `bluetooth_disabled` | `E1A9` | contorno | 18 | `state.dangerText` (6,55 sobre `bg.app`) | `:25` |
| Tablero: Split | `timer` | `E425` | contorno | 14 | `metric.split` (6,73) | `:253` (`timer_outlined`) |
| Tablero: SPM | `speed` | `E9E4` | contorno | 14 | `metric.spm` (5,60) | `:254` |
| Tablero: Vatios | `bolt` | `EA0B` | contorno | 14 | `metric.watts` (9,78) | `:255` |
| Tablero: Distancia | `straighten` | `E41C` | contorno | 14 | `metric.distance` (7,18) | `:256` |
| Tablero: Pulso (solo con bpm > 0) | `favorite` | `E87E` | relleno | 14 | `metric.hr.hud` (4,61) | `:258` |

**Dispositivo, búsqueda** (estado vacío, F-BLE-05, y los 3 s de la caída simulada, F-BLE-08)

| Lugar | Ícono | Codepoint | Fuente | dp | Color | Legacy |
|---|---|---|---|---|---|---|
| Botón primario "Buscar dispositivos" | `bluetooth_searching` | `E60F` | contorno | 18 | `text.onAccent` (7,64 sobre el acento) | `:403` |
| Sin resultados | `bluetooth_disabled` | `E1A9` | contorno | 56 (legacy 48) | blanco `@24 %`, decorativo (lo explica el texto) | `:428` |
| Fila de remo encontrado (si el simulador la muestra) | `rowing` + `chevron_right` | `E921` + `E5CC` | contorno | 24 + 18 | `accent.primary` + `text.disabled` | `:456`, `:465` |

Bluetooth apagado o sin permiso (`bluetooth_disabled` 64 en `state.danger` + botón con `bluetooth`, `:373`, `:388`) es de la búsqueda real: It. 10a. Diagnóstico BLE (`bug_report` `E868`): también It. 10a.

**Pulsómetro**

| Lugar | Ícono | Codepoint | Fuente | dp | Color | Legacy |
|---|---|---|---|---|---|---|
| Encabezado (late a la frecuencia real) | `favorite` | `E87E` | relleno | 14 | `metric.hr`; `@40 %` sin conexión [CONTRATO] (1,56, decorativo: el estado lo dice el texto) | `heart_rate_card.dart:39` |
| Botón tonal "Buscar pulsómetro" | `bluetooth_searching` | `E60F` | contorno | 18 | `accent.primary` | `:109` |
| Filas de pulsómetros encontrados | `favorite` + `chevron_right` | `E87E` + `E5CC` | contorno | 24 + 18 | `metric.hr` (3,82) + `text.disabled` | `:138`, `:142` |
| Botón de texto "Desconectar" | `bluetooth_disabled` | `E1A9` | contorno | 18 | `state.dangerText` (5,20) | `:190` |
| Botón tonal "Conectar" (recordado, reintento) | `refresh` | `E5D5` | contorno | 18 | `accent.primary` | `:213` |

**Panel del simulador** (`surface.overlay`; ícono del color del texto de su botón)

| Lugar | Ícono | Codepoint | Fuente | dp | Color | Legacy |
|---|---|---|---|---|---|---|
| Botón flotante que abre el panel | `build` | `F8CD` | contorno | 24 (botón 40) | `text.onAccent` sobre `accent.primary` (7,64) | `simulator_overlay.dart:48` |
| Cerrar | `close` | `E5CD` | contorno | 24 | `text.secondary` (6,98) | `:107` |
| − / + de Watts, Ritmo y Hora escena | `remove` / `add` | `E15B` / `E145` | contorno | 24 | `text.primary` (12,50) | `:197`, `:206` |
| "Simular desconexión" (botón contorno) | `link_off` | `E16F` | contorno | 18 | `text.primary` | `:158` |
| "Simular caída del pulsómetro" (botón contorno) | `heart_broken` | `EAC2` | contorno | 18 | `text.primary` | `:163` |

La cinta SIMULADOR, los presets, el interruptor "Remando" y "Hora real" no llevan ícono (paridad).

**Total de la It. 2:** 20 glifos de contorno (`bluetooth`, `bluetooth_connected`, `bluetooth_disabled`, `bluetooth_searching`, `timer`, `fitness_center`, `history`, `rowing`, `speed`, `bolt`, `straighten`, `favorite`, `chevron_right`, `refresh`, `build`, `close`, `add`, `remove`, `link_off`, `heart_broken`) y 2 rellenos (`timer`, `favorite`). Todos están en el recorte de ASSET_SLOTS §4.1 (42 + 7) y todos tienen receta provisoria en ASSET_SLOTS §4.4. Opcional: `error` (`F8B6`, 18) si la tarjeta del pulsómetro usa la caja de error en línea de ART_BIBLE §3.10 en lugar del texto rojo suelto del legacy; también tiene receta.

---

## 4. Respaldo si el usuario todavía no descargó las fuentes

### 4.1 Qué se ve en cada caso

| Fuentes de texto (Inter, Barlow) | Fuente de íconos | Cómo se ve la It. 2 | Aviso |
|---|---|---|---|
| Sí | Sí | Definitivo | — |
| Sí | No | Tipografía definitiva; íconos provisorios dibujados por código | `[SLOT] ui.icons` una vez por sesión |
| No | Sí | Fuente por defecto de Unity con la escala de `Tokens.cs`; íconos definitivos | `[SLOT] ui.font.text` / `ui.font.numeric` |
| No | No | "A medio vestir" (ROADMAP §0): fuente por defecto e íconos provisorios. Mismo layout, mismos colores, mismas animaciones | los tres |

En ningún caso: errores que frenen la escena o el build (ASSET_SLOTS §0), cuadrados "□" de glifo faltante, ni un layout distinto cuando llegan las fuentes.

### 4.2 Íconos provisorios

Spec completa y recetas en **ASSET_SLOTS §4.4**. Lo esencial:

- Mismo elemento y misma caja cuadrada que el glifo, mismo `color` de USS (con sus `@NN %` y su fundido de 160 ms) y las mismas animaciones del elemento (latido, escala de la pestaña).
- Dibujo de líneas sobre la grilla 24 × 24 de Material Symbols, trazo 2,25 (como el peso 500), extremos redondeados; un relleno es un solo contorno cerrado (con alfa, dos formas encimadas se verían más claras).
- Receta para cada uno de los 22 de la It. 2 (más `error`); el resto usa un cuadrado redondeado neutro de 14 × 14 hasta que su iteración sume la receta.
- Implementación propuesta (Architect decide): recetas como **datos en Core** (testeables con dotnet), dibujadas una vez en Presentation con `Painter2D` y guardadas como `VectorImage` (`Painter2D.SaveToVectorImage`, sin el módulo de Vector Graphics). Menos de 200 triángulos por ícono, sin texturas.
- NV hasta el Editor: el tinte y su transición sobre un `VectorImage`, y que cada ícono se reconozca en una captura a 24 dp.

### 4.3 Texto y números sin Inter ni Barlow

- La UI usa la fuente por defecto del tema de UI Toolkit con los tamaños, interlineados y pesos de `Tokens.cs`. Las clases `.rm-type-*` de la It. 2 tienen que funcionar con o sin `-unity-font-definition` resuelto.
- Los números siguen **tabulares**: `TabularNumber` mide el ancho de celda en la fuente que haya (ARCHITECTURE §7.8), así que no "bailan" aunque falte Barlow.
- **No** usar las Inter que trae el Editor (`Editor/Data/Resources/Fonts/` de la instalación): son recursos del Editor, no entran en el build y no traen su `OFL.txt`.
- La mono del diagnóstico BLE (JetBrains Mono) no hace falta en la It. 2.

### 4.4 Qué tiene que hacer el usuario (cuando quiera)

Lo mismo de ASSET_SLOTS §3 y §4.1, con una simplificación: en esta PC no hay Python, pero sí `uv`, así que la receta de íconos corre con `uvx --from fonttools …` sin instalar nada. La probé así de punta a punta (§7).

---

## 5. Propuestas para otros roles

| Para | Propuesta |
|---|---|
| Architect | `Icons.cs` (D-09) con los 42 + 7 de ASSET_SLOTS §4.3 y test contra el `.codepoints`; recetas provisorias como datos en Core con su test (cada ícono de la It. 2 tiene receta; coordenadas en 0-24); `IconView` en Presentation que elige glifo o provisorio según el slot `ui.icons`. Nombre de elemento estable para el botón del simulador (`sim-open`) |
| Architect (opcional) | Un *script* `tools/icons` que baje los 3 archivos fijados y corra los 4 comandos con `uvx`, para que el paso del usuario sea uno solo |
| Coordinador / usuario (opcional) | Como la licencia Apache 2.0 permite redistribuir con `LICENSE` y `NOTICE`, el equipo puede generar y versionar las dos fuentes de íconos (ya las generé y verifiqué fuera del repo) y quitarle ese prerrequisito al usuario. No las copié al proyecto: es una decisión de alcance |
| PO | STATUS.md ("También queda abierto el punto de íconos…") y REPORT §7.2 / §9.2 de la It. 1 pueden marcar el punto de íconos como **cerrado** (glifos). En la spec de la It. 2: el tablero con etiquetas `@100 %` y el título del pulsómetro en `metric.hr.hud` aplican el criterio de D-ART-03 (ya decidido por defecto para la It. 2) |
| QA | Ratificar el cierre de QA-P-09. En la verificación de la It. 2: captura de los íconos definitivos y de los provisorios a 24 dp; que no haya "□"; que el layout no cambie al cargar la fuente; contraste de la etiqueta de pestaña fuera de la píldora |

---

## 6. Qué queda abierto

- **NV hasta tener capturas en el Editor:** cómo se ven los glifos dentro de un `Label` a 14-64 dp (la prueba fue sobre el atlas), el tinte de los provisorios y la medición de R-T1 (04-design-notes §5).
- STATUS.md y el REPORT de la It. 1 todavía dicen "punto de íconos abierto" (son del PO).
- Del resto de 04-design-notes §7 para la It. 2 (clases USS de tipo, resortes como tokens, tokens de layout, variantes alfa de componentes, `var()` en transiciones): van en las notas de diseño de la It. 2, después de la spec del PO.

---

## 7. Verificación hecha

1. **Legacy** (solo lectura): los íconos de las pantallas de la It. 2 salen de un `grep` de `Icons.*` en `main.dart`, `device_screen.dart`, `heart_rate_card.dart` y `simulator_overlay.dart` (líneas citadas en §3.5), más los tests que buscan íconos (`simulator_overlay_test.dart:40, :113`). `git -C rowmate-legacy status --porcelain` vacío al empezar y al terminar.
2. **Codepoints.** Bajé el `.codepoints` del commit fijado `10ecc14` (4 304 nombres) y busqué cada nombre de ASSET_SLOTS §4.3: los 42 de contorno y los 7 rellenos coinciden todos.
3. **Receta de fuentes (ASSET_SLOTS §4.1), en el scratchpad.** Bajé la fuente variable fijada (15 235 612 bytes) y corrí los comandos tal cual, con `uvx --from fonttools` (fonttools 4.67.0, Python 3.14.4 de `uv`). `RowMateIcons-Outline.ttf`: 9 084 bytes, 43 glifos (42 + `.notdef`), cmap con los 42 codepoints. `RowMateIcons-Filled.ttf`: 2 700 bytes, 8 glifos, cmap con los 7. Los dos con `usWeightClass` 500, 960 unidades por em, avance 960 en todos los glifos y ascendente/descendente 1 056/−96. El relleno cambia de verdad el dibujo (`favorite`: mismas cantidades de contornos y puntos, otras coordenadas).
4. **Unity 6000.3.27f1**, en un proyecto de prueba descartable del scratchpad (nunca en `<repo>`), `-batchmode -executeMethod … -quit`, exit 0. `FontAsset.CreateFontAsset(…, 64, 6, SDFAA, 512, 512, Dynamic)` y `TryAddCharacters`: 42/42 y 7/7, 0 faltantes. Exporté los dos atlas a PNG y los miré: los 49 glifos están bien dibujados. Importador SVG del módulo integrado: `SVGImporter`, tipo por defecto `VectorImage`, teselado `AntialiasedArcEncodings`, asset principal `UnityEngine.UIElements.VectorImage`. (Los `DirectoryNotFoundException` del log son de paquetes de la plantilla por defecto con rutas de más de 260 caracteres en el scratchpad; no afectan la prueba.)
5. **API:** `Painter2D.SaveToVectorImage(VectorImage)`, `LineCap`, `LineJoin`, `Arc` y `IStyle.unityBackgroundImageTintColor` figuran en la documentación XML de `UnityEngine.UIElementsModule` instalada.
6. **Contrastes** de este documento y de las secciones nuevas de ART_BIBLE: script propio con el método de 02-tech-design §4.4. Reproduce los de 04-design-notes (4,475 · 3,068 · 5,603 · 4,614 · 7,028 · 5,196 · 4,639 · 4,703 · 5,077 · 6,020 · 8,004 · 7,641 · 6,105 · 13,493) y suma los nuevos: legacy `@70 %` (split 3,99, SPM 3,51, watts 5,52, distancia 4,33, pulso 2,45), acento sobre la píldora de la barra en el peor caso 4,41, `state.dangerText` sobre `bg.app` 6,55, `text.primary` sobre `surface.overlay` 12,50, `metric.hr @40 %` sobre card 1,56.
7. **Control cruzado de nombres** entre las tres biblias y `Tokens.cs` (§2.1): ningún nombre de token inexistente.
8. **Sin cambios de código** y sin tocar `Tokens.cs`. Nada se copió al proyecto Unity.
