# It. 4b — Notas de diseño visual y de motion

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-29 |
| Estado | **Notas para WP-B1 a WP-B3** (van antes de WP-B1, 02-tech-design §7.0). Responden a DN4b-01 a DN4b-13 y a los desvíos DT4b que me tocan (§1). Piden DN4b-14 a DN4b-29 (§9). Lo que cambia un número o un texto de la spec va al PO (§10). **WP-D2** (el aspecto, el visto bueno de las tomas y de los videos, los pares de contraste y las biblias) queda para después de WP-B3 (§11). **WP-D2 hecho** (2026-10-29): §4.5, §4.14 y §7.1 alineados con el gate; lo implementado, lo que se midió y los hallazgos para WP-B, en §15 |
| Iteración | 4b · `04b-hud-exr` ([ROADMAP §It. 4b](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (§0.4, §1, §4, §5, §7, §10, §11, §14, §15); [02-tech-design](02-tech-design.md) (§2.1 a §2.8, §3, §5, §7.4, §7.5, §8 y el Anexo A); [00-deuda-diseno](00-deuda-diseno.md) (§4); [04-design-notes de la 4a](../04a-motor-hud-funcional/04-design-notes.md) (§2.3, §3, §5, §8, §9 y §17); [ART_BIBLE](../../design/ART_BIBLE.md) (§2.2, §2.3, §3.2, §3.3, §3.5, §4, §5.2, §10); [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) (§0.1, §1, §2.4 a §2.11, §7.1). Del código, sin tocarlo: `Core/Design/{Tokens,Icons}.cs`, `Presentation/Controls/GlowTexture.cs`, `Tests/EditMode/Design/ContrastTests.cs` y las listas `UI/Fonts/Kerning/*.pairs.json`. Capturas de la 4a: CAP-13, -14, -17, -19, -21 a -26 |
| Entrega | Este documento. No cambia código, tokens, assets, capturas ni biblias: el token nuevo (§2.1) y las biblias (§12) se escriben en WP-A y en WP-D2 |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/step_pill.dart:1-105` (los cuatro estados y el halo: `BoxShadow` con el color al `0,3 + 0,4·pulso`, `blurRadius` 8, `spreadRadius` 2 y radio 5) y `docs/image-03.png`. Lo demás, por las citas de la spec y de 02-tech-design. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificado | Programas propios en `<scratch>/it4b/designer-notes/`, sin red:<br>- `measure4b.py`: anchos de texto con las **tablas K2 versionadas** (por índice de glifo, como las aplica TextCore) y el *tracking* de UI Toolkit, con fontTools 4.68.0 por `uv run --offline`;<br>- `contrast4b2.py`: contrastes con el método de `Contrast.cs` y la búsqueda OKLCH de la variante de `FINALIZADO`;<br>- `probes4b.py` y `extra4b_*.py`: los compuestos de las sondas;<br>- `geo4b2.py` y `wrench4b.py`: la barra, las píldoras, el lugar centro alto, el diálogo y la llave por perfil;<br>- `motion4b2.py`: las tablas por cuadro, con las curvas de `Tokens.cs` (réplica de `Easing.cs`, `rmcore.py` de 04b/00-deuda-diseno).<br>**Unity no se abrió** y dotnet no se corrió |

Convenciones: las de la 4a (hex `#RRGGBB`, transparencia `@NN %`, WCAG 2.x sin redondear, 1 px de USS = 1 dp). Perfiles: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720, con los márgenes seguros en 0. `s` = `hudScale`: 1,054 en TV y TH, 0,923 en P360 y L640, 1,6 en W1280. "Base" = antes de multiplicar por `s`. Los rectángulos van en dp de ventana, con x desde la izquierda e y desde arriba. Las capturas de TV y TH tienen 2,628 px por dp y las de L640 y W1280, 1 px por dp. "Código" = animación por `MotionDriver`. Los nombres `#…` y `.rm-…` son los del contrato de 02-tech-design §7.5. **[cambia]** = distinto de ese contrato, y va pedido en §9.

---

## 0. Resumen

1. **Confirmo los números de la spec** para la entrada "sumergirse", la salida y el sello (DN4b-01, DN4b-02).
   - Retiro las alternativas de 04b/00-deuda-diseno §4.1, §4.3 y §4.9.
   - La entrada de la spec tiene el orden y los tiempos de la cascada de la 6a: en la 6a, el vuelo de cámara reemplaza solo el negro y el fondo.
   - El `#FF9800` que yo proponía para `No alcanzado` es casi el naranja de Split (`#FF9F1C`), que queda justo debajo del sello. El `#FFD166` de la spec, en cambio, no choca con nada que esté al lado.
2. **`FINALIZADO` no llega con `#FF5D7F`** (DV-44): da 3,942:1 sobre su tinte en el peor vidrio, y 3,883 renderizado.
   - La variante legible del mismo matiz es **`#FF7A92`**: 4,677 sin redondear y 4,606 renderizado. Pido el token nuevo `phase.finished.hud` (135 → 136).
   - `EN MARCHA` (5,324) y `PAUSADO` (6,432) sí llegan en su color.
3. **`PAUSA` va en un disco, el mismo de la cuenta grande.**
   - En vertical, el lugar centro alto deja como mucho 76 base de ancho, porque Watts con objetivo queda a 10,7 dp del disco. `PAUSA` en `title` 20/700 con su relleno mide 108 base, y `PAUSED`, 122.
   - El disco es de vidrio, de 72·s, con un aro de 3·s en `phase.paused`, el glifo de pausa blanco y `PAUSA` en 12/700. De lejos se reconoce por el glifo y el aro, y de cerca se lee.
   - En ese lugar hay un disco por vez.
4. **La cuenta grande**, como la propone 02-tech-design: la cifra 64/800 blanca en un disco de 72·s (DT4b-04). El borde del aro que se mueve avanza en el sentido del reloj, y el rodillo de la cifra queda dentro del aro.
5. **Las píldoras.**
   - El relleno lleva un brillo de 1·s: distingue la píldora llena en curso de la hecha.
   - El halo empieza en el borde de la píldora, al 0,65·α, que es lo que deja ahí el desenfoque del legacy. Se apaga antes del borde de su fila: 10 base a los costados, 8 arriba y 4 abajo, así que nunca se corta ni pasa debajo de un texto.
   - Con píldoras de menos de 6 base, la en curso es un **pulgar** de 4·s × 10·s y la hueca se dibuja con **dos rieles**.
6. **La fila de información en TV.**
   - CAP-01 entra por 0,2 base, y `Texto largo` no entra ni sin la pastilla.
   - Hay una regla por paso, que se decide al empezar el paso para que nada salte: primero se achican los aires (10 → 6 y 8 → 6) y después el relleno de `Próximo` (7 → 4).
   - La pastilla se corta **por fragmentos enteros**, con el "…" blanco pegado. Si no entra ni el primero, se va.
7. **La barra libre en un teléfono en español.** Al título le faltan 28 base, y recortado se lee `Entrenamient…`. Propongo achicarlo hasta 0,8 antes de recortarlo (lo decide el PO). Entero necesita 0,818.
8. **El diálogo de completado** es el diálogo de la app, opaco, con el check dibujado de 32 a la izquierda del título y los valores en `num.s` 20/700, que cuentan hasta su valor exacto. Entra en L640 a 1× y a 1,3× se desplaza 30 dp.
9. **La llave con el diálogo de completado** va al hueco libre más bajo del carril izquierdo; en TV, entre la banda y los controles. "16 + margen inferior" se encima con `Terminar` o con la banda.
   - Además, **en horizontal el panel del simulador abierto tapa el sello**. Pido bajar su techo a 8 dp debajo de la barra con el sello.
10. **Capturas y videos**: qué mirar y el valor esperado de cada sonda en CAP-01 a CAP-30, DEMO-01 a DEMO-12 y VID-01 a VID-08, con las tablas por cuadro. Las tomas en segundos enteros pasan a +0,2 s: en ese instante los rodillos arrancan y la imagen muestra el valor viejo.
11. **Contraste**: 10 grupos nuevos (B1 a B10) para `ItFourBDesignerPairs`, todos sobre su umbral. El riesgo de dos píldoras sobre una nube queda para la 6a.
12. **Presupuesto**: lo de este documento no suma elementos. `PAUSA` es uno solo, con el glifo dibujado por código.

---

## 1. Respuestas a los pedidos de 02-tech-design §8.4 y a sus desvíos

| Pedido | Respuesta | Dónde |
|---|---|---|
| **DN4b-01** · Entrada y salida | **La spec.** Entrada: negro en 0-220, fondo en 220-400, la cascada a los 400 / 520 / 600 / 680 / 760 / 840, de 280 cada pieza con +8·s, y la llave a los 1120. Salida: el HUD en 0-200, el negro en 200-400 y Workout en 400-640. Retiro las mías (C-01 y C-02 de 02-tech-design §8.2) | §4.1, §4.2 |
| **DN4b-02** · Sello | **La spec**: `No alcanzado` en `#FFD166` (`phase.paused`), el detalle en blanco y los tiempos dentro de los 2 s (la barra en 0-240, el contenido en 40-240 y el ícono en 60-300; sale en 1800-2000). El ícono acompaña a la píldora: `check_circle` relleno para cumplido, como la sólida, y `error` de contorno para no alcanzado, como la hueca | §3.7, §4.7 |
| **DN4b-03** · Cuenta grande y `PAUSA` | La cuenta: 64/800 blanca en un disco de 72·s, con aro de 3·s (DT4b-04, confirmado). **`PAUSA` [cambia]:** el mismo disco con el aro entero en `phase.paused`, el glifo de pausa de 28 y la etiqueta 12/700. Una cápsula con `title` 20/700 no entra al lado de Watts en vertical | §3.6, §4.6, §4.8 |
| **DN4b-04** · Chip | El texto en su color en `EN MARCHA` (5,324:1) y en `PAUSADO` (6,432:1). **En `FINALIZADO`, `#FF7A92`** (4,677:1): `#FF5D7F` da 3,942. El punto solo en `EN MARCHA`. Ancho fijo, con el lugar del punto solo en la etiqueta que lo lleva: 101,8 base en es y 74,1 en en | §2.1, §3.5, §8 |
| **DN4b-05** · La fila más larga | Primero los aires (10 → 6 antes del reloj y 8 → 6 antes de la cuenta) y después el relleno de `Próximo` (7 → 4). La pastilla se corta por fragmentos enteros con el "…" blanco pegado y se va si no entra ni el primero. El modo se decide al empezar el paso. Reemplaza a DT4b-12 | §3.3 |
| **DN4b-06** · Barra libre en TV | Recortado, el título se lee `Entrenamient…` (le faltan 28 base). **Propuesta al PO:** que el título se achique hasta 0,8 antes de recortarse; entero necesita 0,818. Si el PO no la acepta, vale la spec ("…") | §3.5, §10 |
| **DN4b-07** · CAP-01 en TV | Con K2 entra por 0,2 base: no alcanza. Con la regla de DN4b-05 pasa al modo compacto 1 y `18 spm` entra entero, con 6,1 base de margen | §3.3 |
| **DN4b-08** · Diálogo | Valores en `num.s` 20/700 (RowMate Numeric SC; siguen la letra del sistema). La `m` de la distancia va de sufijo, en 14/500 `text.secondary`. Rótulos en `bodyM` 14 `text.secondary`. Check de 32, dibujado, a la izquierda del título | §3.8, §4.12 |
| **DN4b-09** · "Vuelve Workout" | La página negra se va en 240 (`ease.enter`). Workout no tiene entrada propia: ya está en reposo, y volver a animar la lista en cada fin sería ruido | §4.2 |
| **DN4b-10** · La tira | La en curso como **pulgar**, de al menos 4 base de ancho y 10 de alto (1 arriba y 1 abajo de la franja), en lugar de 2 base: en TV, 2 base mide 5,5 px contra los 4,8 de un tramo y no se distingue. Rige para toda píldora de menos de 6 base. La hueca angosta, con **dos rieles**. La excepción a Q-59 la confirma el PO | §3.2 |
| **DN4b-11** · Sondas de aspecto de `it4b` | Las de §7.2 a §7.4 | §7 |
| **DN4b-12** · Pares de contraste | B1 a B10 de §8.1, todos sobre su umbral | §8 |
| **DN4b-13** · La llave con el diálogo | R10, con otro lugar: el hueco libre más bajo del carril izquierdo, a 8 dp de las piezas. En TV y en P360, entre la banda y los controles; en TH y en L640, sobre la banda. Con "16 + margen inferior" se encima con `Terminar` en TV y en P360, y con la banda en TH y en L640 | §3.9 |
| DT4b-02, -03 | De acuerdo con la spec (DN4b-01, DN4b-02) | — |
| DT4b-04 | De acuerdo: 64 en 72·s. Es el disco más grande que entra en TV y en P360 (76,2 base como máximo) | §3.6 |
| DT4b-08 | De acuerdo: un `TabularNumber` con el ícono de prefijo; el rodillo del minuto se conserva | §3.4, §4.10 |
| DT4b-10 | De acuerdo: `Pausa` y `Terminar` responden desde los 840 (P-15) y "Atrás", desde el cuadro 0. Es otra cosa que en 04b/00-deuda-diseno (desde el cuadro 0), porque la spec lo pide así | §4.1 |
| DT4b-11 | De acuerdo con el ancho fijo, con una precisión: el lugar del punto (6 + 4) cuenta solo para la etiqueta que lo lleva | §3.5 |
| DT4b-12 | Lo reemplaza DN4b-05 | §3.3 |
| DT4b-15 | Lo reemplaza DN4b-13 | §3.9 |
| DT4b-16 | De acuerdo: `TabularNumber`, en `num.s` 20/700 | §3.8 |
| DT4b-18 | De acuerdo. Con la regla de "un disco por vez" (§4.8), a los 47,6 s se ve `PAUSA` quieto | §4.8 |
| Spec P-07 | 20, como la cuenta: es la regla del legacy. ART_BIBLE §3.3 todavía dice 18/700; se corrige en WP-D2 (§12) | §3.4 |
| Spec P-08 | Opaco (`surface.overlay`) hasta la 6b, como el diálogo de terminar | §3.8 |
| Spec §15, ANIMATION_BIBLE §2.7 ("marcador") | Ya precisado al empezar la 4b: no hay marcador que viaje | — |
| Spec §15, ART_BIBLE §3.3 (`§2.5`) | Ya corregido: dice §2.6 | — |

---

## 2. Tokens, clases de texto y colores

### 2.1 Token nuevo

| Token | Valor | Rol | Uso | Por qué |
|---|---|---|---|---|
| `phase.finished.hud` **[nuevo]** | `#FF7A92` | `HudText` | el texto del chip en `FINALIZADO` (DV-44) | Es la variante OKLCH de `#EF476F` (Δh −0,43°, ΔL +0,093) que llega a 4,5 sobre su tinte al 15 % con margen de render: 4,677:1 sin redondear y 4,606 contra `#2B3038`. La primera que llega es `#FF778F` (4,517 renderizado). Elijo una con 0,09 más de margen, porque el tinte suma una mezcla más que los `*.hud` de §2.3 de la biblia. `#FF5D7F` (`step.work.hud`), la de la spec, da 3,942 / 3,883; sobre el vidrio solo daba 4,577, pero el tinte del chip aclara el fondo |

Son **135 → 136** tokens. `Tokens.cs` lo mantiene el Designer, pero `PhaseChip` (WP-A) lo necesita antes que WP-D2: lo agrega WP-A, con `tools/tokens` y los conteos (DN4b-15). `phase.running` y `phase.paused` no necesitan variante: sobre su tinte, el color base da 5,324 y 6,432.

### 2.2 Clase de texto y color por elemento nuevo (sobre 02-tech-design §7.5)

Todo color sale de `Tokens`. Los tamaños del HUD van en línea, a tamaño base, y no siguen la letra del sistema; los del diálogo sí.

| Elemento | Clase (fuente) | Tamaño | Color |
|---|---|---|---|
| Píldoras (`#hud-pill-row`) | dibujadas por código | 8·s de alto | el color **base** del tipo, con la opacidad de su estado (§3.2) |
| Brillo del relleno | dibujado | 1·s | `text.primary` al 25 % |
| Halo | dibujado | §3.2 | el color base del tipo, a 0,65·α en el borde de la píldora |
| `#hud-wall-clock` | `.rm-type-num-s` (derivada 700) | 20 | `text.primary`; el prefijo `schedule` 16 en `text.primary` al 70 % |
| `#hud-phase-label` | `.rm-type-header-s` (Inter 700) | 11, *tracking* 0,08 em | `phase.running` · `phase.paused` · **`phase.finished.hud`** [cambia] |
| `#hud-phase-dot` | dibujado | 6·s | `phase.running`, con la opacidad del latido |
| `#hud-seal-text`, título | `.rm-type-header-s` | 13 | `state.success` (cumplido) o `phase.paused` (no alcanzado) |
| `#hud-seal-text`, detalle | Inter 600 por texto enriquecido | 12 | `text.primary` |
| `#hud-seal-icon` | fuente de íconos | 14 | el del título: `check_circle` relleno o `error` de contorno |
| `#hud-big-countdown` | `.rm-type-num-hero` (derivada 800) | 64 | `text.primary`; el disco en `glass.fallback`, el aro en el color base del paso (pista al 20 %) |
| `#hud-paused-label` [cambia] | `.rm-type-header-s` (Inter 700) | 12, *tracking* 0,08 em | `text.primary`; el glifo de pausa en `text.primary`; el disco en `glass.fallback`; el aro en `phase.paused` |
| `.rm-completion__title` | `.rm-type-title` | 20/600 | `text.primary` |
| `.rm-completion__message` | `.rm-type-body` | 15 | `text.secondary` |
| `.rm-completion__row-label` | `.rm-type-body-m` [cambia] | 14/400 | `text.secondary` |
| `.rm-completion__row-value` | `TabularNumber` `.rm-type-num-s` [cambia] | 20/700 | `text.primary`; la `m`, sufijo Inter 500 de 14 en `text.secondary` |
| `.rm-completion__row-icon` | fuente de íconos | 20 | `accent.primary` |
| `#completion-check` | dibujado | 32 | el disco en `state.success`; el trazo en `surface.overlay` |
| `#completion-finish` | `.rm-type-button` | 14/600 | `text.onAccent` sobre `accent.primary` |

### 2.3 Colores con transparencia, por código (los compuestos de las sondas)

Sobre el fondo provisorio de la 4a: el cielo `#0B4FA8` y el agua `#0E4F8F`. El vidrio del HUD (`glass.fallback`) da `#08162A` sobre el cielo y `#081627` sobre el agua. La barra de pasos y la libre quedan sobre el cielo en los cinco perfiles.

| Pieza | Color | Compuesto (sobre el vidrio de la barra, sobre el cielo) |
|---|---|---|
| Píldora pendiente | base @18 % | trabajo `#311F37` · descanso `#073940` · calentamiento `#343835` · enfriamiento `#092B43` |
| Pista de la en curso | base @30 % | `#4D253F` · `#07504E` · `#524E3C` · `#0A3953` |
| Relleno de la en curso | base @100 % | `#EF476F` · `#06D6A0` · `#FFD166` · `#118AB2` |
| Brillo del relleno | blanco @25 % sobre el relleno | `#F37593` · `#44E0B8` · `#FFDC8C` · `#4CA7C5` |
| Sólida (cumplida o sin objetivo) | base @85 % | `#CC4065` · `#06B98E` · `#DAB55D` · `#10799E` |
| Hueca: interior · borde | base @18 % · base @85 % sobre el interior | interior como la pendiente; borde `#D34167` · `#06BE92` · `#E1BA5F` · `#107CA1` |
| Destello de la que se solidifica (pico, t = 120) | blanco @40 % sobre el relleno | `#F591A9` · `#6AE6C6` · `#FFE3A3` · `#70B9D1` |
| Halo con α = 0,70 (p = 1), trabajo | 0,65·α·(1 − d/E)² | en el borde `#712C4A` · a 1·s arriba `#582742` · a 1·s al costado `#5D2844` · a 1·s abajo `#43233C` |
| Chip `EN MARCHA` | tinte @15 % · borde @50 % · texto | `#07333C` · `#07856E` · `#06D6A0` |
| Chip `PAUSADO` | ídem | `#2D3233` · `#96824D` · `#FFD166` (en pausa, sobre el cielo oscurecido: `#2C3130` · `#96814B`) |
| Chip `FINALIZADO` | ídem | `#2A1E35` · `#8D3252` · `#FF7A92` |
| Punto en vivo | `phase.running` × su opacidad, sobre el chip | 0,35 `#076C5F` · 0,444 `#077B68` · 0,75 `#06AD87` · 1 `#06D6A0` |
| Ícono del reloj | blanco @70 % | `#B5B9BF` |
| Disco de la cuenta grande | `glass.fallback` sobre el cielo | `#08162A`; la pista del aro (base @20 %): `#362038` · `#073D42` · `#393C36` · `#092D46` |
| Velo de pausa | el fondo × 0,85 | cielo `#09438F` · agua `#0C437A` · el vidrio encima: `#071527` (cielo) y `#081524` (agua) |
| Disco de `PAUSA` (en pausa) | `glass.fallback` sobre el cielo oscurecido | `#071527`; aro `#FFD166`; glifo y etiqueta `#FFFFFF` |
| Velo de los diálogos | `overlay.scrim` @60 % | sobre la barra o una tarjeta `#030911` · sobre el cielo `#042043` · sobre el agua `#062039` |
| Diálogo de completado | `surface.overlay` · caja blanco @4 % | `#1E3550` · `#273D57`; rótulos y `m` (`text.secondary` sobre la caja) `#BEC5CD`; mensaje (sobre el diálogo) `#BCC2CA` |

---

## 3. Geometría

### 3.1 La barra de pasos (52 base; 74 con el sello)

```
#hud-topbar.rm-hud-topbar--steps   12·s a cada lado · margen seguro + 8·s · vidrio · borde 1 border.subtle · r.glass 14 · overflow hidden
├── #hud-pill-row          1-21   alto 20: relleno 10 / 8 / 10 / 4; la franja de píldoras de 9 a 17
├── .rm-hud-step-row      21-51   la fila de información: 22 + 8 de relleno abajo (12 a los lados)
├── #hud-seal             51-73   el sello: 17 de contenido arriba del slot + 5 de aire; 0 sin sello
└── (borde)               73-74
```

| Perfil | Barra (x) | y: 52 · 74 | La franja de píldoras (y) | La fila del sello (y) | Aire hasta las tarjetas (90·s) |
|---|---|---|---|---|---|
| TV · TH | 12,6-398,4 · 12,6-901,4 | 8,4-63,2 · 86,4 | 17,9-26,3 | 62,2-80,1 | 8,4 |
| P360 · L640 | 11,1-348,9 · 11,1-628,9 | 7,4-55,4 · 75,7 | 15,7-23,1 | 54,5-70,2 | 7,4 |
| W1280 | 19,2-1260,8 | 12,8-96,0 · 131,2 | 27,2-40,0 | 94,4-121,6 | 12,8 |

- **El contenido del sello queda arriba en su slot.** El slot recorta y crece de 0 a 22; el contenido está quieto en su lugar final y se descubre de arriba hacia abajo mientras se abre, con su fundido (§4.7). El aire es de 8 arriba (el relleno de la fila de información) y de 5 más el borde abajo.
- **La cinta `SIMULADOR`** tapa el extremo izquierdo de la primera píldora; en TV, la franja x + y < 56,5 dp. El tipo del paso baja y queda fuera de la cinta, como en `docs/image-03.png`.

### 3.2 Las píldoras

**Anatomía** (base; todo por `s`, ART_BIBLE §3.3 [CONTRATO]):

| Estado | Cómo se ve |
|---|---|
| Pendiente | el color base al 18 %, sin borde |
| En curso | la pista al 30 % y el relleno al 100 % desde la izquierda. El borde derecho del relleno es recto: es el rectángulo recortado por la cápsula, como el `ClipRRect` del legacy. **Brillo:** una línea de 1·s `#FFFFFF @25 %` en el borde de arriba del relleno, recortada por la cápsula. Es lo que distingue una en curso casi llena de una hecha, y no rodea la píldora: no se lee como marco. Sin borde ni marco. Con el relleno en 0, solo la pista, sin brillo. En un paso por distancia, sin meta o de 0 s, la pista sola |
| Sólida (cumplida o sin objetivo) | al 85 %, sin borde |
| Hueca (no cumplida) | el interior al 18 % y un borde de 1,5·s al 85 % **por dentro**: la píldora no cambia de tamaño. El borde se dibuja sobre el interior, como el `Border` del legacy sobre su color |
| El último paso | en curso hasta que sale el inmersivo: llena y con su brillo si es por tiempo, con la pista sola si es por distancia. Nunca muestra su resultado (Q-52) |

**Regímenes** (los de 02-tech-design §2.2): separación 3 mientras las píldoras miden 6 base o más, separación 1 mientras miden 2 o más, y si no, la tira.

| Perfil | Útil (base) | Corta (4) | Estados (5) | Prueba 4a (6) | Pirámide 500 (14) | 40 pasos | 100 pasos | Serie larga (200) |
|---|---|---|---|---|---|---|---|---|
| TV · P360 | 344,0 | 83,8 | 66,4 | 54,8 | 21,8 | 7,62 (sep. 1) | 2,45 (sep. 1) | tira de 1,72 |
| TH | 821,3 | 203,1 | 161,9 | 134,4 | 55,9 | 17,6 | 7,22 (sep. 1) | 3,11 (sep. 1) |
| L640 | 647,3 | 159,6 | 127,1 | 105,4 | 43,5 | 13,3 | 5,48 (sep. 1) | 2,24 (sep. 1) |
| W1280 | 754,0 | 186,2 | 148,4 | 123,2 | 51,1 | 15,9 | 6,55 (sep. 1) | 2,77 (sep. 1) |

**Píldoras angostas (de menos de 6 base, en cualquier régimen) [cambia]:**

- **La en curso es un pulgar.** Mide 10·s de alto (1·s arriba y 1·s abajo de la franja) y al menos 4·s de ancho, centrado en su tramo, con radio `min(4; ancho / 2)`·s. Se dibuja por encima de sus vecinas y adentro lleva la pista, el relleno y el brillo de siempre.
  - Es la marca de "acá estás": en una franja de 4,8 px por tramo (Serie larga en TV), un pulgar de 11,1 px y un poco más alto se ve de lejos sin ser un marco.
  - La fila tiene lugar: 8 base arriba y 4 abajo de la franja.
- **La hueca se dibuja con dos rieles**, uno arriba y otro abajo, de 1,5·s al 85 %, sin bordes a los costados y con el interior al 18 %. Con menos de 6 base, un borde alrededor deja el interior en 3 base o menos, y la hueca se confunde con la sólida. Con los rieles se sigue leyendo "intentado" (`=`) contra "cumplido" (la franja llena).
- **En la tira**, cada paso es un tramo de `útil / n` sin separación, en su color y con la opacidad de su estado. Solo los extremos de la tira van redondeados (4·s). Con el tope del editor (×999, 1998 pasos) un tramo mide menos de un píxel: la franja muestra igual cuánto se hizo, el pulgar y lo que falta (DV-40).

**El halo de los últimos 5 s [cambia]:**

- **Dónde:** alrededor de la en curso (o del pulgar), **solo por fuera**: la pista no se tiñe. Va en el color base del tipo y se dibuja debajo de las píldoras, así que las pendientes vecinas dejan pasar el 82 %.
- **Cuánto:** α = a·(0,30 + 0,40·p), con el reloj común y la rampa de 150 (AC-WKT-14-11). En el borde de la píldora se ve **0,65·α**: es lo que deja ahí el `BoxShadow` del legacy, con expansión 2 y desenfoque 8 (sigma 5,12: Φ(2 / 5,12) = 0,652).
- **Cómo se apaga:** como el halo de las tarjetas, `(1 − d / E)²`, hasta E = **10·s a los costados, 8·s arriba y 4·s abajo**. Es lo que deja la fila, así que el halo llega a 0 en su borde: no hay un corte ni pasa debajo de la fila de información (ART_BIBLE §2.3).
- **Forma:** la de adentro es la de la píldora, con radio 4·s. En W1280 se dibuja a 3×, como `GlowTexture`.

| d (base) desde el borde | 0 | 1 | 2 | 4 | 6 | 8 | 10 |
|---|---|---|---|---|---|---|---|
| Legacy, `BoxShadow` (fracción de α) | 0,652 | 0,577 | 0,500 | 0,348 | 0,217 | 0,121 | 0,059 |
| App, a los costados (E = 10) | 0,650 | 0,527 | 0,416 | 0,234 | 0,104 | 0,026 | 0 |

- **El brillo del legacy y el de la app.** En el legacy, el halo se extiende unos 14 dp y pasa por debajo de la fila de información. El de la app es un poco más ceñido y se queda en su fila, que es lo que pide la spec.
- **Contraste** sobre el fondo provisorio (gráfico ≥ 3). La hecha da 3,868 (trabajo), 7,204, 9,259 y 3,645 (enfriamiento). El relleno, de 4,569 a 12,551. El borde de la hueca contra el vidrio, 4,056 · 7,602 · 9,805 · 3,800, y contra su interior, 3,404 · 5,305 · 6,460 · 3,064. El riesgo con una nube detrás (la 6a) sigue: §8.2.

### 3.3 La fila de información: qué cede primero

Medido con las tablas K2 (`measure4b.py`). El ancho útil es el de la barra menos el borde y 12 de relleno a cada lado: 339,9 base en TV, P360 y los teléfonos de 390 dp o más; 817,2 en TH, 643,4 en L640 y 750,0 en W1280.

| Fila (TV) | Tipo | Pastilla | `Próximo` | Cuenta | Reloj | Aires | Total | Sobra |
|---|---|---|---|---|---|---|---|---|
| CAP-01 · `Estados` a los 40 s | 51,6 | 49,3 (`18 spm`) | 108,2 | 37,7 | 69,1 | 24 | 339,8 | **0,2** |
| CAP-19 · `Texto largo` a los 8 s, es | 99,5 | — | 122,3 | 37,7 | 69,1 | 18 | 346,5 | **−6,6** (ya sin la pastilla) |
| CAP-20 · ídem, en | 56,9 | — | 107,5 | 37,7 | 69,1 | 18 | 289,2 | 50,8 |
| DEMO-05 · `Prueba 4a` a los 18 s | 99,5 | — | 96,5 | 37,7 | 69,1 | 18 | 320,8 | 19,2 |
| CAP-22 · `Serie larga` | 51,6 | — | 108,2 | 37,7 | 69,1 | 18 | 284,5 | 55,4 |

En TH, L640 y W1280 sobran de 296 a 533 base en todos los casos.

**La regla [cambia]** (reemplaza a DT4b-12; AC-WKT-14-15 deja al Designer lo que se achica después de la pastilla):

1. **Cuándo se decide.** Al empezar cada paso y cuando cambian la ventana, el idioma o la letra. Entra en la cuenta lo que el paso va a mostrar en algún momento:
   - el tipo;
   - la pastilla en su mínimo: entera si tiene un solo fragmento, o el primer fragmento con "…";
   - `Próximo` con el tipo siguiente, si el paso lo va a tener;
   - la cuenta como se ve en los últimos 15 s (`M:SS`);
   - el reloj.
2. **Los modos.** Se usa el primero en el que todo eso entra, con 1 base de margen por el redondeo a píxeles:
   - **normal**: 6 · 8 · 10 y `Próximo` con 7 × 3;
   - **compacto 1**: el aire de `Próximo` a la cuenta y el de la cuenta al reloj pasan a 6;
   - **compacto 2**: además, `Próximo` con 4 × 3.

   Si en ninguno entra la pastilla en su mínimo junto con `Próximo`, el modo es el primero en el que entra el resto. La pastilla se ve mientras no hay `Próximo` y se va cuando entra (punto 3).
3. **La pastilla se corta por fragmentos.** Un fragmento es cada parte separada por ` · `; la tendencia con su texto cuenta como uno.
   - Muestra tantos fragmentos **enteros** como entran y, si quedó alguno afuera, el "…" pegado, en `text.primary` (DD4b-03): `>160W…`.
   - Nunca corta en el medio de un número: `>15…` se lee como "más de 15".
   - Si no entra ni el primero, funde a 0 en 120 (`ease.exit`), y su ancho y su aire de 6 se cierran con el crecimiento de `Próximo`. En ese paso no vuelve.
4. **Lo que nunca se corta:** el tipo, `Próximo:` y su tipo, la cuenta y el reloj. Con los textos del catálogo, en todos los casos entra.
5. **Nada salta.** El modo vale para todo el paso, así que cuando entra `Próximo` solo cambia la pastilla. Si el modo cambia de un paso al siguiente, los aires y el relleno pasan al nuevo en 0-240 (`ease.move`), con el resto del cambio de paso.

| Caso (TV) | Modo | Lo que se ve |
|---|---|---|
| CAP-01 | compacto 1 (le faltaban 0,9 base con el margen) | `Trabajo` `18 spm` entera · `Próximo: Descanso` · `0:10` · `⏱10:00`; sobran 6,1 base |
| `Prueba 4a`, un trabajo en sus últimos 15 s (la CAP-21 de la 4a) | compacto 2 | `>160W…` (48,1 + 12) en blanco. Antes de `Próximo`, más fragmentos |
| CAP-19, es | compacto 2, sin pastilla (no entra ni `>150W…`) | `Calentamiento` · `Próximo: Enfriamiento` · `0:12` · `10:00`; sobran 5,4 |
| CAP-20, en | normal, sin pastilla (`>150W…` pide 59,8 y quedan 44,7) | `Warmup` · `Up next: Cooldown` · `0:12` · `10:00` |

Con esta regla, en TV CAP-19 y CAP-20 se ven **sin pastilla**, y la spec esperaba "la pastilla recortada con un … blanco": lo resuelve el PO (§10). El "…" blanco se prueba en PlayMode (DN4b-16) y, si QA quiere una foto, en la toma auxiliar AUX-01 (§7.2).

### 3.4 El reloj de pared

`#hud-wall-clock` es un `TabularNumber` `num.s` 20/700 blanco, con el prefijo `schedule` de 16 al 70 % y 5 de aire (AC-WKT-19-01; 02-tech-design §3.7).

- **Alineación.** La tinta del ícono (0,83 em = 13,3 base) va centrada en la banda de las mayúsculas de las cifras (14 base): miden casi lo mismo y quedan a la misma altura.
- **Ancho.** 69,1 base en total (16 + 5 + 48,1), siempre igual, porque las cifras son tabulares. No cambia con la cuenta que crece a 1,1: esa escala va desde el borde derecho de la cuenta y se queda en sus 10 (o 6) de aire.
- **Lugar.** En la barra de pasos, a la derecha de la cuenta. En la libre, después del espacio flexible y a 10 del chip.

### 3.5 La barra libre y el chip de fase

```
#hud-topbar.rm-hud-topbar--free   40 base · relleno 14 × 8 · vidrio · borde 1 border.subtle · r.glass 14
[rowing 20 accent.primary] 8 [Entrenamiento Libre 16/600] ··· [⏱ 10:00] 10 [● EN MARCHA]
```

**El chip** (`#hud-phase-chip`):

- **Medidas.** Alto 19,6 base (3 + 13,6 + 3) y `r.m` 8; el texto es Inter 700 de 11 con 0,08 em de *tracking*. El repo no tiene Inter 800; con 700 y el *tracking* se ve como el del legacy.
- **Ancho fijo** [cambia] = máx(`EN MARCHA` + 10, `PAUSADO`, `FINALIZADO`) + 16. Da 101,8 base en es y **74,1** en en (02-tech-design: 102,2 y 84,1). El lugar del punto (6 + 4) cuenta solo para la etiqueta que lo lleva. El contenido va centrado.
- **Colores.** El tinte en el color base de la fase al 15 % y el borde de 1·s al 50 %. El texto, en el color de la fase en `EN MARCHA` y `PAUSADO`, y en `phase.finished.hud` en `FINALIZADO` (§2.1).
- **El punto en vivo.** 6·s, en `phase.running`, a 4 de la etiqueta y centrado en la banda de las mayúsculas. Solo en `EN MARCHA`; su latido está en §4.9.

| Etiqueta (11/700, 0,08 em) | es | en |
|---|---|---|
| en marcha | `EN MARCHA` 75,8 (+10 del punto) | `ACTIVE` 46,5 (+10) |
| en pausa | `PAUSADO` 60,1 | `PAUSED` 49,4 |
| terminada | `FINALIZADO` 76,2 | `FINISHED` 58,1 |

**El título no entra en un teléfono en español** (DN4b-06). El ancho útil es de 335,9 base. Lo demás ocupa 20 + 8 + 69,1 + 10 + 101,8, así que al título le quedan 127,0 y `Entrenamiento Libre` mide 155,4.

| Perfil | es | en |
|---|---|---|
| TV, P360 | recortado: **`Entrenamient…`**; achicado: 0,818 (13,1 base) | entra (le sobran 51) |
| TH, L640, W1280 | entra | entra |

- **Mi propuesta [cambia, PO].** Que el título se achique (escala uniforme, en el acto con la ventana o el idioma) hasta **0,8** y solo por debajo de eso se recorte con "…".
- **Por qué.** Es la regla del HUD para lo que no entra: se achica, como las cifras con `FitBox` y el sello. Además, `Entrenamient…` le corta la última letra a una palabra que todos leen entera.
- **Sin el visto bueno del PO**, vale la spec: el título recortado con "…" (DN4b-27).

### 3.6 El lugar centro alto: la cuenta grande y `PAUSA`

Es un **disco de 72·s, centrado en el ancho de la ventana**. En y va centrado entre lo que tiene arriba más 8·s y la zona del bote menos 8·s. Lo de arriba es la barra en su alto máximo (74) o SPM, Split o Watts con objetivo, si se cruzan con el disco en x.

| Perfil | Disco (dp) | x | y | Lugar en y | Aire a los costados (pide 8·s) | Disco máximo que entra |
|---|---|---|---|---|---|---|
| TV | 75,9 | 167,6-243,4 | 234,0-309,9 | 86,6 | Watts 10,7 | 76,2 base |
| P360 | 66,5 | 146,8-213,2 | 204,9-271,4 | 75,7 | Watts 9,4 | 76,3 |
| TH | 75,9 | 419,1-494,9 | 108,3-184,2 | 102,8 | SPM 208,4 | 97,6 |
| L640 | 66,5 | 286,8-353,2 | 94,9-161,4 | 90,1 | SPM 107,6 | 97,6 |
| W1280 | 115,2 | 582,4-697,6 | 188,6-303,8 | 204,3 | SPM 262,5 | 127,7 |

Las cuentas usan los rectángulos de la 4a, sin la derivada. Con ella, Watts se angosta unos 3 dp y los aires solo crecen.

**La cuenta grande** (`#hud-big-countdown`):

- **El disco.** `glass.fallback`, sin borde.
- **El aro.** 3·s en el borde del disco, con su línea central a 34,5·s del centro. La pista va en el color base del paso al 20 % y el arco al 100 %.
  - El arco ocupa la fracción que falta, f = restante visual / 3 (`PillProgress.RemainingAt / 3`).
  - **Cómo se vacía [cambia]:** el arco va del ángulo 360°·(1 − f) hasta las 12, medido en el sentido del reloj desde las 12. El borde que se mueve avanza en el sentido del reloj, como una aguja que se come el arco, y el otro extremo queda fijo en las 12. Los extremos son rectos.
  - El arco está lleno en el tick del 3, en 2/3 en el del 2, en 1/3 en el del 1 y vacío en el cambio de paso; nunca se vacía antes del tick.
- **La cifra.** `num.hero` 64/800 blanca, centrada por la banda de las mayúsculas: 44,8 base de alto y 34,2 de ancho, adentro de un interior de 66.
- **El rodillo.** La cifra nueva entra desde 0,35 em más arriba y la vieja se va hacia abajo (§4.6). Solo se asoma fuera del aro en un cuadro, por menos de 2 base y con α < 0,12: no hace falta recortarla.

**`PAUSA`** (`#hud-paused-label`) [cambia]:

- **El disco.** El mismo: 72·s, `glass.fallback`, con el aro de 3·s **entero** en `phase.paused`. El amarillo es el de `PAUSADO` y se ve también en una rutina, que no tiene chip.
- **El glifo de pausa.** El `pause` de Material, de 28 base: dos barras de 4,67 × 16,33 separadas 4,67, con radio 1,17, en blanco.
- **La etiqueta.** `hudPausedLabel` 12/700, *tracking* 0,08 em, en blanco, debajo del glifo a 4 de aire.
- **Cómo se apila.** Glifo y etiqueta van como un bloque centrado: las barras de −14,5 a +1,8 y las mayúsculas de +5,8 a +14,5, desde el centro. `PAUSED` (53,9) entra en la cuerda del aro a la altura de su base (59,3), con 2,7 de aire por lado; `PAUSA` mide 45,7.
- **Quieto.** No late: lo que corre late (el punto en vivo) y lo que está en pausa queda quieto. Los pulsos de la barra siguen, por paridad.
- **Un disco por vez** en el lugar centro alto (§4.8).
- **Por qué no la cápsula de 02-tech-design.** En TV y P360 el disco centrado no puede pasar de 76 base sin acercarse a Watts con objetivo a menos de 8·s. `PAUSA` en `title` 20/700 con 16 × 8 de relleno mide 108,2 base, y `PAUSED`, 121,8. Con el texto solo, hay que bajarlo a 12, y de lejos no se lee. El glifo de pausa sí se lee de lejos (§5).

### 3.7 El sello

- **Dónde.** En la tercera fila de la barra (§3.1), centrado, en una línea, con 12 base de relleno a cada lado: el mismo ancho útil de la fila de información.
- **Qué lleva.** El ícono de 14, 5 de aire, el título 13/700 en el color del veredicto, ` · ` y el detalle 12/600 en blanco.
- **El ícono.** `check_circle` relleno (`state.success`) para cumplido y `error` de contorno (`phase.paused`) para no alcanzado: el disco lleno y el aro hueco repiten la sólida y la hueca de la píldora que acaba de cerrarse.
- **Lo que no entra se achica**, con una escala uniforme del contenido desde su centro, en el acto. Nunca lleva "…".

| Sello | Ancho (base) | Escala en TV (339,9) | TH, L640, W1280 |
|---|---|---|---|
| `Objetivo cumplido · 24.0 / 18 spm` | 221,8 | 1 | 1 |
| `No alcanzado · 24.0 / 30 spm` | 195,8 | 1 | 1 |
| `Objetivo cumplido · 150 / 150 W · 02:05 / 02:05/500m` | 338,8 | 1 (sobran 1,1) | 1 |
| `No alcanzado · 150 / 160 W · 24.0 / 22 spm` | 272,0 | 1 | 1 |
| `Objetivo cumplido · 170 / 160 W · 26.0 / 24 spm` | 300,3 | 1 | 1 |
| `Target missed · 150 / 160 W · 24.0 / 22 spm` | 275,3 | 1 | 1 |
| Extremos de FTMS (`9999 / 9999 W · 127.5 / 99 spm · 99:59 / 99:59/500m`) | 451,7 | 0,753 | 1 |

### 3.8 El diálogo de completado

Es el diálogo de la app de la It. 3, **opaco** (P-08): `surface.overlay`, `r.card` 16, borde 1 `border.default`, velo al 60 %. El ancho es `min(400; ventana − 80)` con un mínimo de 280, y el relleno, 24 (16 con la ventana de menos de 480 dp de alto).

```
#completion-dialog
├── .rm-dialog__pinned > .rm-completion__header    fila: [check 32] 12 [título title 20/600, ajusta] — el check, centrado en el alto del título
├── 12
├── ScrollView
│   ├── .rm-completion__message                     body 15 text.secondary
│   ├── 16
│   └── .rm-completion__summary                     blanco @4 %, r.m 8, relleno 12; tres filas separadas 8
│       └── .rm-completion__row                     [ícono 20 accent.primary] 12 [rótulo bodyM 14 text.secondary] ··· [valor num.s 20/700 blanco]
├── 20
└── .rm-dialog__actions > #completion-finish        a la derecha: primario relleno (con lugar para el segundo botón de la It. 12)
```

- **El check** es un solo elemento dibujado por código, de 32 (el legacy usa 28), y quieto es el `check_circle` de Material.
  - El disco va en `state.success`.
  - El trazo, en `surface.overlay`, que es el color del agujero del glifo.
  - El trazo sigue la línea central del check de Material, en radios del disco (R = 16) y con y hacia abajo: P0 (−0,629; −0,071), P1 (−0,200; +0,359) y P2 (+0,630; −0,471). Tiene 0,20·R de grosor (3,2), unión en inglete, extremos rectos y 1,780·R de largo (28,5), en dos tramos de 0,607·R y 1,173·R (`CheckGlyph`).
  - Se dibuja de P0 a P2 en proporción al largo.
- **Los valores** son `TabularNumber` en `num.s` 20/700 (las cifras de tablero, AC-WKT-17-12) y siguen la letra del sistema.
  - La distancia lleva la `m` como sufijo, 14/500 en `text.secondary`, a 3, sobre la línea de base. Se lee `1120 m`, con el formato del legacy.
  - Las calorías van sin unidad.
  - Mientras cuentan, las cifras cambian sin rodillo.
- **Las filas.** Cada una mide max(20; 17,3; 22) = 22 base. El rótulo y el valor quedan alineados por la banda de las mayúsculas.

| Perfil | Ancho | Relleno | Título | Mensaje | Alto · alto máximo (es, ×1,0) | Rectángulo (es, ×1,0) | ×1,3 |
|---|---|---|---|---|---|---|---|
| TV | 331 | 24 | 2 líneas (`¡Entrenamiento` / `completado!`; mide 273,4 y quedan 239) | 1 línea (281,2 de 283) | 318,1 · 866 | 40,0-371,0 × 298,0-616,0 | 382,5: entra |
| P360 | 280 | 24 | 2 líneas | 2 líneas | 336,7 · 752 | 40,0-320,0 × 231,7-568,3 | 382,5: entra |
| TH | 400 | 16 | 1 línea | 1 línea | 284,6 · 363 | 257,0-657,0 × 63,2-347,8 | 342,3: entra |
| L640 | 400 | 16 | 1 línea | 1 línea | 284,6 · 312 | 120,0-520,0 × 37,7-322,3 | 342,3: se desplaza 30,3, con `Terminar` a la vista |
| W1280 | 400 | 24 | 1 línea | 1 línea | 300,6 · 672 | 440,0-840,0 × 209,7-510,3 | 382,5: entra |

- **Los valores más largos** (`Maratón` con `R-4X`) también entran, a 1,3 y en TV, la ventana más angosta.
  - `16777215` mide 108,4 en `num.s` 26, más 20,0 de la `m`, y `Distancia` a 1,3 mide 79,2.
  - La fila usa 20 + 12 + 79,2 + 12 + 128,4 = 251,6 de 259 (`9:59:59`, 84,4 y `65535`, 67,7).
- **Por qué no un check grande arriba y centrado.** Lo probé. Con 40 de check y el título debajo, el diálogo ya no entra en L640 a 1× (329,4 de 312), y además se aparta del sistema de diálogos de la app. El peso lo dan la entrada, el check que se dibuja y los totales que cuentan (§4.12).

### 3.9 La llave y el panel con las piezas nuevas

**La llave con el diálogo de completado** (DN4b-13, DN4b-22):

- **La regla.** Si su lugar del HUD, agrandado 8, se cruza con el diálogo, la llave va al **hueco libre más bajo del carril izquierdo** (x 16-56), a 8 dp de toda pieza del HUD, porque esas piezas siguen a la vista debajo del velo.
- Queda arriba del velo y se puede usar. Cambia de lugar con el fundido del cambio de carril: sale en 90 y entra en 160 con 0,85 → 1, cuando termina la entrada del diálogo.

| Perfil | Su lugar del HUD | ¿Se cruza con el diálogo? | Con el diálogo | Con "16 + margen inferior" (02-tech-design §5.7) |
|---|---|---|---|---|
| TV | 355,0-395,0 × 575,9-615,9 | sí | x 16-56 · **y 790,1-830,1**, entre la banda (766,5) y los controles (838,1) | y 858-898: se encima con `Terminar` (x 51,6) |
| P360 | 304,0-344,0 × 497,9-537,9 | sí | x 16-56 · **y 685,5-725,5** | y 744-784: se encima con `Terminar` |
| TH | 618,9-658,9 × 344,6-384,6 | sí | x 16-56 · **y 270,3-310,3**, sobre la banda | y 355-395: se encima con la banda |
| L640 | 414,8-454,8 × 245,5-285,5 | sí | x 16-56 · **y 230,8-270,8**, sobre la banda | y 304-344: se encima con la banda |
| W1280 | 881,6-921,6 × 629,6-669,6 | no | queda en su lugar | — |

Es igual a 1,3×, porque los rectángulos del diálogo a 1,3 no llegan al carril izquierdo.

**El panel del simulador abierto [cambia]** (DN4b-23):

- **En horizontal**, el panel abierto tapa el tramo del medio de la barra (04a/04-design-notes §3.9): en la 4b, eso es parte de las píldoras y **el sello**, que AC-WKT-09-08 pide que se vea igual con el panel abierto. En W1280, el panel (x 641,6-921,6) tapa la mitad derecha de un sello centrado en 640.
  - Propuesta: en el inmersivo, el techo del panel en horizontal pasa a 8 dp debajo de la barra en su alto máximo (74·s).
  - El alto máximo queda en 290,2 en TH (era 368,6), 201,8 en L640 (269,5) y 530,4 en W1280 (653,6). Lo que no entra se desplaza, como siempre.
- **En vertical**, el techo de Watts con objetivo (291,5 + 8 en TV) deja el panel sobre los 10,4 dp de abajo del disco de la cuenta grande.
  - Propuesta: el techo pasa también a 8 debajo del disco (317,9 en TV; 279,4 en P360).
  - El alto máximo queda en 298,0 en TV (era 316,4) y 258,5 en P360 (274,6).
- **Las dos cambian números de AC-BLE-16-03 de la 4a** y van al PO (§10).

### 3.10 Vertical y horizontal, en resumen

| Pieza | Vertical (TV, P360) | Horizontal (TH, L640, W1280) |
|---|---|---|
| Barra de pasos | 52·s / 74·s; píldoras de 54,8 base con `Prueba 4a` | lo mismo, más ancha: 134,4 / 105,4 / 123,2 base |
| Fila de información | los modos compactos y el corte por fragmentos (§3.3) | todo entra, en modo normal |
| Barra libre | en español, el título no entra (§3.5) | todo entra |
| Lugar centro alto | debajo de SPM con objetivo y a 10,7 / 9,4 dp de Watts | debajo de la barra, con más de 100 dp a los costados |
| Sello | una línea; los extremos de FTMS, a 0,753 | una línea, sin achicar |
| Diálogo | 331 / 280 de ancho; título en dos líneas | 400; en L640 a 1,3× se desplaza |
| Llave con el diálogo | carril izquierdo, entre la banda y los controles | carril izquierdo, sobre la banda (W1280: en su lugar) |
| kcal y BPM | kcal se desliza 71,7·s (§4.11) | kcal en el rincón: no se mueve |

---

## 4. Motion

### 4.0 Quién mueve qué

Todo va por código, con el reloj del `MotionDriver`: las líneas de tiempo son puras (`HudTimelines`, WP-A) y los `IMotion` de WP-D1 las evalúan (02-tech-design §3.1 y §7.4). La regla de oro está en 02-tech-design §3.0 y no la repito: el estado cambia en el instante del dato y se anima solo cómo se ve.

| Animación | Duración y curva | Reducir movimiento | § |
|---|---|---|---|
| Entrada "sumergirse" | 220 · 180 · cascada de 280 por pieza (`ease.enter`, +8·s) · llave 160 | fundido de 120 lineal | 4.1 |
| Salida | 200 · 200 · 240 | fundido de 120 lineal | 4.2 |
| Relleno de la píldora | continuo (`PillProgress`) | igual (es avance) | 4.3 |
| Halo | rampa de 150 (`ease.move`) × reloj común | fijo en 0,70, con la rampa | 4.4 |
| Cambio de paso | 0-120 · 120-340 · 120-440 · 0-150 | sin destello ni trazo: el borde funde en 160 | 4.5 |
| Cuenta grande | entra 200 · rueda 180 · sale 200 | entra 160 · fundido 80 · sale 120 | 4.6 |
| Sello | 0-240 · 40-240 · 60-300 · 1800-2000 | el alto en el acto; el contenido en 160 / 120 | 4.7 |
| Pausa: velo, `PAUSA` | 300 / 240 · 240 / 240 con 0,96 → 1 | fundidos sin escala | 4.8 |
| Chip y punto en vivo | colores 150 · etiqueta 0-80 / 80-200 · punto 800 + 800 | el punto quieto en 1 | 4.9 |
| Reloj de pared | rodillo de 180 al cambiar el minuto | fundido de 80 | 4.10 |
| kcal | 0-240 / 120-360 (FLIP) | en el acto | 4.11 |
| Diálogo de completado | velo 200 · diálogo 220 · check 60-300 y 180-500 · totales 700, de a 120 | solo opacidad; el check entero; los totales finales | 4.12 |

### 4.1 Entrada "sumergirse" (VID-01, VID-03)

`t = 0` en el toque en `Entrenamiento libre` o en `Iniciar`. Desde ese cuadro el motor ya corre (AC-WKT-13-01).

| t (ms) | Qué pasa |
|---|---|
| 0 | La página (`.rm-immersive`, negra) entra con opacidad 0, y se piden las barras del sistema ocultas. La llave sale (90). "Atrás" ya abre la confirmación de terminar. Si se viene del diálogo de iniciar, el diálogo sale (150) mientras Workout se va a negro |
| 0-220 | Workout funde a negro: la página va de 0 a 1 (`ease.enter`) |
| 220 | Las pestañas y la barra de Workout pasan a `display: none`, tapadas por el negro |
| 220-400 | El fondo funde de 0 a 1 sobre el negro (`ease.enter`). Debajo de la barra superior el cielo pasa por `#05244D` (253), `#083B7E` (287) y `#0A4899` (320) |
| 400-1120 | La cascada, de arriba abajo. Cada zona funde de 0 a 1 y sube de +8·s a 0 en 280 (`ease.enter`, por `translate`) |
| 840 | `Pausa` y `Terminar` responden (P-15) |
| 1000 | La banda pasa a `00:01` con su rodillo: la cascada nunca demora el reloj |
| 1120-1280 | La llave entra en su lugar del HUD (0 → 1 y 0,85 → 1, `ease.enter`). Si el panel estaba abierto, se abre ahí |

Por cuadro, a 30 cuadros por segundo (`motion4b2.py`):

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 | 333 | 367 | 400 | 433 | 467 | 500 | 533 | 567 | 600 | 633 | 667 | 700 | 733 | 767 | 800 | 833 | 867 | 900 | 933 | 967 | 1000 | 1033 | 1067 | 1100 | 1133 | 1167 | 1200 | 1233 | 1280 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Página negra | 0 | 0,389 | 0,660 | 0,835 | 0,937 | 0,985 | 0,999 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Fondo | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,207 | 0,593 | 0,826 | 0,947 | 0,993 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Barra superior | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,317 | 0,557 | 0,732 | 0,853 | 0,931 | 0,975 | 0,995 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| SPM | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,137 | 0,422 | 0,634 | 0,786 | 0,889 | 0,952 | 0,986 | 0,998 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Split y Watts | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,317 | 0,557 | 0,732 | 0,853 | 0,931 | 0,975 | 0,995 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Banda | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,200 | 0,469 | 0,669 | 0,811 | 0,905 | 0,961 | 0,989 | 0,999 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| kcal y BPM | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,071 | 0,371 | 0,597 | 0,760 | 0,872 | 0,942 | 0,981 | 0,997 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Controles | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,261 | 0,515 | 0,702 | 0,833 | 0,919 | 0,969 | 0,993 | 1 | 1 | 1 | 1 | 1 | 1 |
| Subida de la barra (base) | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 5,46 | 3,54 | 2,14 | 1,17 | 0,55 | 0,20 | 0,04 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Subida de los controles | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 8 | 5,91 | 3,88 | 2,39 | 1,34 | 0,65 | 0,25 | 0,06 | 0 | 0 | 0 | 0 | 0 | 0 |
| Llave | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,230 | 0,644 | 0,872 | 0,974 | 1 |

- **Las piezas del centro** (la cuenta grande, el sello, `PAUSA`) no existen al entrar, salvo en un primer paso de 3 s o menos. En ese caso, la cuenta grande va con el tramo de SPM (520).
- **Por qué este orden.** Es el de la lectura del remero: primero dónde está (la barra), después el ritmo (SPM) y al final los controles.
  - El negro es el "me sumerjo".
  - En Android, a los 400 las barras del sistema ya se fueron debajo del negro, así que el HUD nunca asoma debajo de la barra de estado.
- **Interrupciones** (02-tech-design §3.2). Si la sesión vuelve a reposo durante la entrada, la salida arranca desde la opacidad que tiene cada pieza. Una rotación reacomoda el HUD en el acto y la cascada sigue con sus tiempos.

### 4.2 Salida con la última foto (VID-02, VID-05)

`t = 0` en el primer estado de reposo. Desde ahí el HUD no aplica datos: queda la última foto, que es `Finished` (DD4b-05).

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 | 333 | 367 | 400 | 433 | 467 | 500 | 533 | 567 | 600 | 640 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| HUD (`ease.exit`) | 1 | 0,995 | 0,961 | 0,872 | 0,702 | 0,421 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Fondo sobre el negro (`ease.exit`) | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 0,995 | 0,961 | 0,872 | 0,702 | 0,421 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Página negra (`ease.enter`, 1 → 0) | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 0,638 | 0,378 | 0,201 | 0,090 | 0,030 | 0,005 | 0 |

- **El HUD se va sin moverse**: la salida es rápida y quieta, y el negro hace el resto.
  - Lo finito que corría termina con su reloj (un rodillo, el cruce del chip, el sello).
  - Lo que se repite (el corazón, el pulso de la fila, el halo, el punto) sigue hasta los 640 y ahí se detiene (AC-WKT-13-09).
- **Con el diálogo de completado:** `Terminar` hace que, en el cuadro 0, el diálogo salga (150, a 0,98) y empiece esta salida.
- **Los avisos del final** (`workoutSaveError`) **[cambia]** entran a los 400, cuando empieza a verse Workout, y no en el cuadro 0: sobre el negro no se ve nada más que la cinta (DN4b-24). Sus 4 s cuentan desde los 400. Con reducir movimiento, en el cuadro 0.
- **Las barras del sistema** vuelven a los 640, al terminar la salida (AC-APP-15-04). Workout tiene sus márgenes con las barras desde antes, así que no se mueve.

### 4.3 El relleno continuo

Es `PillProgress` (02-tech-design §2.3; 04b/00-deuda-diseno §4.5): nunca pasa del valor del tick siguiente, nunca baja dentro de un paso y en pausa queda quieto. Además:

- **El borde del relleno** se mueve en subpíxeles, y la fila se repinta cuando avanzó ¼ px.
  - En TV, un paso de 60 s en una píldora de 54,8 base (57,8 dp) avanza 2,5 px por segundo, así que se repinta unas 10 veces por segundo.
  - En W1280, con la misma rutina (123,2 base = 197,1 dp), 3,3 px por segundo: unas 13 veces por segundo.
- **El brillo** sigue al relleno: su largo es el del relleno.

### 4.4 El halo de los últimos 5 s (VID-04)

El reloj común `p` arranca en el primer cuadro con "a continuación" o "por terminar" después de uno sin ninguno. En `Estados`, desde el instante 0, porque los pasos de 10 s tienen `Próximo` desde que empiezan. La rampa `a` empieza en el tick que deja el restante en 5.

| t (s) | 5,0 | 5,1 | 5,2 | 5,3 | 5,4 | 5,5 | 5,6 | 5,7 | 5,8 | 5,9 | 6,0 | 6,1 | 6,2 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| p | 0,248 | 0,500 | 0,752 | 0,934 | 1 | 0,934 | 0,752 | 0,500 | 0,248 | 0,066 | 0 | 0,066 | 0,248 |
| Rampa a (`ease.move`, 150) | 0 | 0,851 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| α = a·(0,30 + 0,40·p) | 0 | 0,425 | 0,601 | 0,674 | 0,700 | 0,674 | 0,601 | 0,500 | 0,399 | 0,326 | 0,300 | 0,326 | 0,399 |
| Reducir movimiento: a·0,70 | 0 | 0,596 | 0,700 | 0,700 | 0,700 | 0,700 | 0,700 | 0,700 | 0,700 | 0,700 | 0,700 | 0,700 | 0,700 |

- **En el borde de la píldora** se ve 0,65·α (§3.2): 0,455 como mucho.
- **El reloj común** es el mismo de `Próximo` y del color de la cuenta, así que los tres laten juntos.
- **En pausa** el halo sigue latiendo, por paridad.
- **Al cambiar de paso**, el halo de la que termina se apaga con la rampa en 0-150 (§4.5).
- Las tomas caen en p = 1: a los 47,6 s (CAP-02, con origen en 35,0), a los 27,6 (DEMO-06, origen 15,0) y a los 239,2 (DEMO-10, origen 235,0).

### 4.5 El cambio de paso de las píldoras (VID-04, VID-05)

`t = 0` en el cuadro en que el estado trae el paso nuevo.

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 | 333 | 367 | 400 | 433 | 467 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| La que termina: relleno que faltaba (0-120, `ease.move`) | 0 | 0,092 | 0,652 | 0,973 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Cumplida: destello blanco, 0,40·(1 − `ease.enter`) en 120-340 | 0 | 0 | 0 | 0 | 0,331 | 0,196 | 0,104 | 0,047 | 0,015 | 0,003 | 0 | 0 | 0 | 0 | 0 |
| Cumplida: opacidad 1 → 0,85 (120-340, `ease.move`) | 1 | 1 | 1 | 1 | 1 | 0,993 | 0,971 | 0,919 | 0,872 | 0,855 | 0,850 | 0,850 | 0,850 | 0,850 | 0,850 |
| El brillo del relleno se va (120-340, `ease.move`) | 1 | 1 | 1 | 1 | 0,997 | 0,952 | 0,810 | 0,457 | 0,149 | 0,033 | 0,001 | 0 | 0 | 0 | 0 |
| No cumplida: interior 1 → 0,18 (120-440, `ease.move`) | 1 | 1 | 1 | 1 | 0,999 | 0,983 | 0,942 | 0,855 | 0,685 | 0,453 | 0,302 | 0,227 | 0,192 | 0,180 | 0,180 |
| No cumplida: el trazo del borde (120-440, `ease.move`) | 0 | 0 | 0 | 0 | 0,001 | 0,020 | 0,071 | 0,176 | 0,385 | 0,668 | 0,851 | 0,943 | 0,986 | 1 | 1 |
| La nueva: pista 0,18 → 0,30 (0-150, `ease.move`) | 0,180 | 0,186 | 0,222 | 0,282 | 0,299 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 | 0,300 |
| Halo de la que termina: la rampa a | 1 | 0,947 | 0,650 | 0,149 | 0,011 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

- **El trazo del borde** recorre el perímetro de la hueca en el sentido del reloj, desde el centro de su extremo izquierdo; en una píldora de 54,8 base, 118,7. En las angostas se dibujan los dos rieles a la vez, de izquierda a derecha.
- **El destello** es una capa blanca sobre la píldora, recortada por su cápsula.
- **El último paso** completa su relleno (0-120) y queda en curso, con su brillo: ni se solidifica ni marca su resultado.
- **El pulgar** (píldoras angostas): el de la que termina se apaga y baja a la altura de la franja en 0-150 (`ease.move`), mientras el de la nueva sube en 0-150. Nunca hay dos pulgares enteros.
- **Reducir movimiento** (AC-APP-05-08: "con un fundido, sin destello ni trazo"; QA-G4bT-03, spec §16.1): **sin destello ni trazo**. La sólida solo funde de 1 a 0,85 (120-340); la hueca baja su interior (120-440) y su borde funde de 0 a 1 en 160; el brillo se va con su fundido; la nueva sube su pista; el pulgar pasa con un fundido cruzado de 150, sin subir ni bajar. *(Alineado en WP-D2: la primera versión conservaba el destello.)*

### 4.6 La cuenta grande (VID-04, VID-05)

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 |
|---|---|---|---|---|---|---|---|
| Entra (tick del 3): opacidad, `ease.enter` | 0 | 0,421 | 0,702 | 0,872 | 0,961 | 0,995 | 1 |
| Entra: escala 0,85 → 1, `ease.enter` | 0,850 | 0,913 | 0,955 | 0,981 | 0,994 | 0,999 | 1 |
| Sale (tick del cambio o fin): opacidad, `ease.exit` | 1 | 0,995 | 0,961 | 0,872 | 0,702 | 0,421 | 0 |
| Sale: escala 1 → 1,10, `ease.enter` ("se suelta") | 1 | 1,042 | 1,070 | 1,087 | 1,096 | 1,100 | 1,100 |
| Reducir: entra en 160 | 0 | 0,503 | 0,799 | 0,945 | 0,995 | 1 | 1 |
| Reducir: sale en 120 | 1 | 0,977 | 0,826 | 0,422 | 0 | 0 | 0 |

- **El rodillo** (en los ticks del 2 y del 1; 180 ms, hacia abajo porque el valor baja), en base de la cifra de 64:
  - a los 33 ms, la vieja está a +10,3 con α 0,333 y la nueva a −12,1 con α 0,119;
  - a los 67, a +16,8 (α 0,058) y −5,6 (α 0,593);
  - a los 100, a +20,4 (0) y −2,0 (0,854);
  - a los 180, en su lugar.
- **El aro**: f vale 1 en el tick del 3, 0,917 a los 0,25 s, 0,833 a los 0,5, 0,667 en el tick del 2, 0,333 en el del 1 y 0 en el cambio. Es continuo, también con reducir movimiento: es avance.
- **El origen de la escala** es el centro del disco. "Se suelta" crece un 10 % mientras se apaga, con el aro ya vacío.

### 4.7 El sello (VID-04)

`t = 0` en `LastOutcomeAt` (el cuadro del cambio de paso). Todo pasa dentro de los 2 s (AC-WKT-09-08).

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 |
|---|---|---|---|---|---|---|---|---|---|---|
| Alto de la barra, 52 → 74 (0-240, `ease.move`) | 52,0 | 52,4 | 54,0 | 58,3 | 66,3 | 71,4 | 73,4 | 74,0 | 74,0 | 74,0 |
| Contenido (40-240, `ease.enter`) | 0 | 0 | 0,350 | 0,655 | 0,845 | 0,949 | 0,991 | 1 | 1 | 1 |
| Ícono, 0,6 → 1 (60-300, `ease.emphasis`) | 0,600 | 0,600 | 0,650 | 0,846 | 0,966 | 1,025 | 1,039 | 1,028 | 1,009 | 1 |

| t (ms) | 1800 | 1833 | 1867 | 1900 | 1933 | 1967 | 2000 |
|---|---|---|---|---|---|---|---|
| Contenido (1800-1920, `ease.exit`) | 1 | 0,977 | 0,826 | 0,422 | 0 | 0 | 0 |
| Alto de la barra, 74 → 52 (1800-2000, `ease.move`) | 74,0 | 73,4 | 70,7 | 63,0 | 55,3 | 52,6 | 52,0 |

- **La entrega de la posta.** El sello se estampa (60-300) mientras la cuenta grande "se suelta" (0-200) y la píldora se solidifica (120-340): el ojo sigue la energía del 1 que se va hacia la barra, donde aparece el veredicto.
- **Un sello que reemplaza a otro** (pasos de 2 s o menos): el viejo funde en 0-80 (1 → 0,925 → 0,421 → 0); el nuevo entra en 80-200 (0,422 a los 100, 0,826 a los 133, 1 a los 200) y su ícono se estampa otra vez en 80-320. Los 2 s vuelven a empezar y la barra sigue abierta.
- **En pausa** sigue sus 2 s. **Al volver del segundo plano**, uno con tiempo se ve en el acto, sin entrada, por lo que le queda.
- **Reducir movimiento:** el alto cambia en el acto; el contenido funde en 160 al entrar y en 120 al salir, y el ícono no escala.

### 4.8 Pausa: el velo, `PAUSA` y un disco por vez (VID-06)

| t (ms) desde `Pausa` | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 |
|---|---|---|---|---|---|---|---|---|---|---|
| Velo: oscurecido 0 → 0,15 (300, `ease.move`) | 0 | 0,0017 | 0,0080 | 0,0224 | 0,0523 | 0,0977 | 0,1276 | 0,1420 | 0,1483 | 0,1500 |
| `PAUSA`: opacidad (240, `ease.enter`) | 0 | 0,362 | 0,622 | 0,799 | 0,910 | 0,970 | 0,995 | 1 | 1 | 1 |
| `PAUSA`: escala 0,96 → 1 | 0,960 | 0,974 | 0,985 | 0,992 | 0,996 | 0,999 | 1 | 1 | 1 | 1 |

| t (ms) desde `Reanudar` | 0 | 40 | 80 | 120 | 160 | 200 | 240 |
|---|---|---|---|---|---|---|---|
| Velo (240, `ease.move`) | 0,1500 | 0,1459 | 0,1276 | 0,0750 | 0,0224 | 0,0041 | 0 |
| `PAUSA`: opacidad (240, `ease.exit`) | 1 | 0,995 | 0,961 | 0,872 | 0,702 | 0,422 | 0 |
| `PAUSA`: escala 1 → 0,96 | 1 | 1,000 | 0,998 | 0,995 | 0,988 | 0,977 | 0,960 |

- **El velo** es el tinte de la textura del fondo: el cielo va de `#0B4FA8` a `#09438F` y el agua, de `#0E4F8F` a `#0C437A`. Las tarjetas, la barra y el chip no bajan de opacidad, y su vidrio sobre el fondo oscurecido queda más oscuro: el contraste solo sube.
- **`Pausa` y `Reanudar` actúan en el acto**, y el botón cambia en el mismo cuadro.
- **Un disco por vez [cambia].** Si hay que cambiar de disco en el lugar centro alto, el que sale funde a 0 en 0-120 (`ease.exit`, sin escala) y el que entra empieza a los 120, con su entrada de siempre. Pasa al pausar con la cuenta grande a la vista, o al reanudar cuando la cuenta vuelve.

  | t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 | 333 | 360 |
  |---|---|---|---|---|---|---|---|---|---|---|---|---|
  | La cuenta que se apaga | 1 | 0,977 | 0,826 | 0,422 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
  | `PAUSA` que entra (120-360) | 0 | 0 | 0 | 0 | 0,158 | 0,478 | 0,702 | 0,850 | 0,939 | 0,983 | 0,998 | 1 |

  Al reanudar, `PAUSA` funde en 0-120 y la cuenta entra en 120-320 con 0,85 → 1. La cifra es la del momento: el dato no espera.
- **Reducir movimiento:** fundidos sin escala, con los mismos tiempos.

### 4.9 El chip de fase y el punto en vivo (VID-06)

- **Al cambiar la fase:** el tinte, el borde y el color del texto cruzan en 150 (`ease.move`). Los valores en los cuadros 1 a 4 son 0,053, 0,350, 0,851 y 0,989, como el semáforo de la 4a.
- **La etiqueta se reemplaza:** la vieja funde en 0-80 (1 → 0,925 → 0,421 → 0) y la nueva entra en 80-200 (0,422 → 0,826 → 0,977 → 1). La nueva llega ya en su color.
- **El punto va con la etiqueta:** se va con `EN MARCHA` y vuelve con ella.
- **El punto late** entre 0,35 y 1: q baja de 1 a 0 en 800 y vuelve en 800 (`ease.pulse`), y la opacidad es 0,35 + 0,65·q.
  - **Empieza en 1** (bajando) cada vez que el chip pasa a `EN MARCHA`. Así, al reanudar, se ve encendido de entrada.
  - Por cuadro de 200 ms: 1 · 0,906 · 0,675 · 0,444 · 0,350 · 0,444 · 0,675 · 0,906 · 1.
  - En las tomas, con el origen en el instante 0: a los 42,2 s, 0,444 (CAP-13); a los 3,2 s, 1 (DEMO-02).
- **Reducir movimiento:** el punto queda quieto en 1. Los colores y la etiqueta cambian igual.

### 4.10 El reloj de pared

- **El rodillo de 180** corre solo en la celda que cambia, hacia arriba (`10:00` → `10:01`) o hacia abajo a la medianoche, en base de la cifra de 20: a los 33 ms la vieja está a −3,21 (α 0,333) y la nueva a +3,79 (α 0,119); a los 100, a −6,37 (0) y +0,63 (0,854); a los 180, en su lugar.
- **Va sin rodillo** (`Instant`) en el primer estado, al volver del segundo plano, al despertar y con un cambio de zona o de hora.
- **Reducir movimiento:** fundido de 80.

### 4.11 kcal que se desliza (VID-07)

Es la tabla de 04a/04-design-notes §5.6 (FLIP), sin cambios.

| t (ms) | 0 | 60 | 120 | 180 | 240 | 300 | 360 |
|---|---|---|---|---|---|---|---|
| Aparece BPM: kcal sube (0-240) · BPM funde (120-360) | 0 · 0 | 0,071 · 0 | 0,500 · 0 | 0,929 · 0,577 | 1 · 0,872 | — · 0,983 | — · 1 |
| Se va BPM: BPM funde (0-120) · kcal baja (120-360) | 1 · 0 | 0,872 · 0 | 0 · 0 | — · 0,071 | — · 0,500 | — · 0,929 | — · 1 |

En vertical, kcal se mueve 71,7·s (75,6 dp en TV). En horizontal está en el rincón y no se mueve.

### 4.12 El diálogo de completado (VID-05)

`t = 0` en el cuadro en que la fase pasa a terminada. El diálogo entra a los 300.

| t (ms) | 300 | 333 | 367 | 400 | 433 | 467 | 500 | 533 | 567 | 600 | 633 | 667 | 700 | 733 | 767 | 800 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Velo 0 → 0,6 (200, `ease.move`) | 0 | 0,016 | 0,090 | 0,300 | 0,510 | 0,584 | 0,600 | 0,600 | 0,600 | 0,600 | 0,600 | 0,600 | 0,600 | 0,600 | 0,600 | 0,600 |
| Diálogo: opacidad (220, `ease.enter`) | 0 | 0,389 | 0,660 | 0,835 | 0,937 | 0,985 | 0,999 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Diálogo: escala 0,96 → 1 | 0,960 | 0,976 | 0,986 | 0,993 | 0,997 | 0,999 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Check: el disco 0,6 → 1 (360-600, `ease.emphasis`) | 0,600 | 0,600 | 0,650 | 0,846 | 0,966 | 1,025 | 1,039 | 1,028 | 1,009 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Check: el trazo (480-800, `ease.move`) | 0 | 0 | 0 | 0 | 0 | 0 | 0,003 | 0,027 | 0,086 | 0,208 | 0,441 | 0,715 | 0,875 | 0,954 | 0,990 | 1 |

**Los totales** (`⌊final·ease.enter(u)⌋` en 700, desde los 520, 640 y 760; con `Corta`):

| t (ms) | 520 | 600 | 667 | 700 | 767 | 833 | 900 | 967 | 1033 | 1100 | 1167 | 1220 | 1340 | 1460 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tiempo | `00:00` | `00:09` | `00:15` | `00:17` | `00:21` | `00:24` | `00:27` | `00:28` | `00:29` | `00:29` | `00:29` | **`00:30`** | `00:30` | `00:30` |
| Distancia | `0 m` | `0 m` | `124 m` | `265 m` | `505 m` | `693 m` | `839 m` | `946 m` | `1022 m` | `1072 m` | `1101 m` | `1113 m` | **`1120 m`** | `1120 m` |
| Calorías | `0` | `0` | `0` | `0` | `1` | `12` | `20` | `27` | `33` | `37` | `39` | `41` | `42` | **`43`** |

- **El orden:** primero el diálogo, después el disco del check que se estampa, el trazo que se dibuja y los tres totales en escalera.
- **Los totales terminan siempre en el valor exacto**, con el formato del legacy. Son los únicos números que cuentan (ANIMATION_BIBLE §2.5).
- **`Terminar`:** el diálogo sale (150, a 0,98) y empieza la salida (§4.2).
- **Reducir movimiento:** solo opacidad (el velo y el diálogo), con el check entero y los totales finales desde el primer cuadro.
- **Al volver del segundo plano**, el diálogo ya está, sin entrada (AC-WKT-21-09).

### 4.13 Al volver del segundo plano o al despertar el equipo

Lo de 04a/04-design-notes §5.10: el primer cuadro muestra el presente, en el acto. En las piezas nuevas:

- **Las píldoras:** cada una en su estado, con el relleno en `transcurrido / D` y el pulgar en su lugar.
- **El halo** en su α (el reloj común arranca en ese cuadro, p = 0).
- **La cuenta grande:** el número y el aro si corresponden, sin entrada.
- **El sello**, si le queda tiempo.
- **El reloj de pared:** la hora actual, sin rodillo.
- **El chip** en su fase; el punto empieza en 1.
- **El diálogo**, ya abierto.

Nunca se ven un destello, un trazo, un conteo ni una cuenta de algo que pasó mientras tanto.

### 4.14 Reducir movimiento (resumen)

| Pieza | Con reducir movimiento |
|---|---|
| Entrada y salida | un fundido lineal de 120, sin negro ni cascada; la salida, con la última foto. Los controles responden desde el cuadro 0 y la llave entra a los 120, solo con opacidad |
| Relleno de la píldora y aro de la cuenta | igual: son avance |
| Halo | fijo en 0,70 (0,455 en el borde), con la rampa de 150 |
| Cambio de paso | sin destello ni trazo: la sólida funde de 1 a 0,85, la hueca baja su interior y su borde funde en 160, el brillo se va con su fundido *(alineado en WP-D2)* |
| Pulgar | cambia de píldora con un fundido de 150, sin subir ni bajar |
| Cuenta grande | entra en 160 y sale en 120, solo opacidad; la cifra con el fundido de 80 |
| Sello | el alto en el acto; el contenido en 160 y en 120; el ícono no escala |
| Velo y `PAUSA` | fundidos sin escala |
| Chip | el punto quieto en 1; los colores y la etiqueta, iguales |
| Reloj de pared | fundido de 80 |
| kcal | en el acto; BPM funde en 240 y en 120 |
| Diálogo | solo opacidad; el check entero y los totales finales |

Nada de esto cambia los datos ni cuándo pasan. Una toma fija sale igual con y sin reducir movimiento, salvo los realces fijos: el halo en 0,70, la cuenta por terminar a 1,1 en el color del paso y `Próximo` a 1.

### 4.15 Constantes para `MotionSpecs` y `HudGeometry` (sobre 02-tech-design §3.1)

De la columna "Spec (por defecto)" de 02-tech-design §3.1 queda todo, salvo lo de esta tabla:

| Constante | Valor | Qué |
|---|---|---|
| `PillGlowEdgeFactor` **[nueva]** | 0,65 | lo que se ve en el borde de la píldora (§3.2) |
| `PillGlowExtentSideDp` / `TopDp` / `BottomDp` **[nuevas]** | 10 / 8 / 4 | el largo del halo por lado, × s. `PillGlowBlurDp`, `SpreadDp` y `RadiusDp` (8 / 2 / 5) quedan como referencia del legacy |
| `PillHighlightAlpha` / `PillHighlightDp` | 0,25 / 1 **[nueva]** | el brillo del relleno: **sí** (era opcional). Se va en 120-340 (`PillSolidify*`) |
| `PillNarrowWidth` **[nueva]** | 6 | debajo de esto, el pulgar y los rieles |
| `PillThumbMinWidth` / `PillThumbHeight` | 4 (era `StripCurrentMin` 2) / 10 **[nueva]** | el pulgar (DN4b-10) |
| `PillThumbSwapMs` **[nueva]** | 150 (`ease.move`) | el pulgar que pasa a la píldora siguiente |
| `CalloutSwapOutMs` / `CalloutSwapInStartMs` **[nuevas]** | 120 / 120 | un disco por vez (§4.8) |
| `PausedLabelOutScaleTo` **[nueva]** | 0,96 | `PAUSA` al reanudar |
| `LiveDot` | 800 / 0,35, empezando en 1 | el origen (§4.9) |
| `EndToastStartMs` **[nueva]** | 400 (= `ImmersiveRevealStartMs`) | los avisos del final (DN4b-24) |
| `BigCountRingDirection` | el borde que se mueve, en el sentido del reloj | §3.6 |
| `PausedGlyphDp` / `PausedGlyphGap` / `PausedLabelSize` / `PausedLabelTrackingEm` **[nuevas]** | 28 / 4 / 12 / 0,08 | `PAUSA` en el disco (reemplaza el `title` 20/700 de DN4b-03) |
| `InfoRowCompactGap` / `UpNextCompactPadH` / `InfoRowFitMarginDp` **[nuevas]** | 6 / 4 / 1 | la regla de §3.3 |
| `PhaseChipDotSlot` | 10 (6 + 4), solo para `EN MARCHA` | §3.5 |
| `SealRowPadH` / `SealRowContent` / `SealRowBottom` | 12 / 17 / 5 | §3.1, §3.7 |
| `CompletionCheckDp` / `CompletionValueType` / `CompletionUnitGapDp` | 32 / `num.s` / 3 | §3.8 |
| `FreeTitleMinScale` **[nueva, si el PO acepta]** | 0,8 | §3.5 |

`BigCountDiameter` 72, `BigCountDigit` 64, `BigCountRingStroke` 3 y `BigCountRingTrackAlpha` 0,20 quedan como en 02-tech-design.

---

## 5. Legibilidad a 1,5 m

TV con s = 1,054; 1 dp = 0,159 mm. Mayúscula de la derivada, 0,70 em; de Inter, 0,728 em. El diálogo va en dp de la app.

| Pieza | En pantalla | Alto | A 1,5 m | Contra 5′ |
|---|---|---|---|---|
| Cifra de la cuenta grande | mayúscula 47,2 dp | 7,51 mm | 17,2′ | 3,4× (lo que más se lee del HUD, junto con SPM) |
| Disco y aro (cuenta y `PAUSA`) | 75,9 dp | 12,1 mm | 27,6′ | 5,5× |
| Glifo de pausa | 17,2 dp | 2,74 mm | 6,3′ | 1,3× |
| Etiqueta `PAUSA` | mayúscula 9,2 dp | 1,46 mm | 3,4′ | de cerca |
| Reloj de pared | mayúscula 14,8 dp | 2,35 mm | 5,4′ | 1,1× (como la cuenta) |
| Título del sello (13/700) | mayúscula 10,0 dp | 1,59 mm | 3,6′ | de cerca; el color y el ícono se ven de lejos |
| Chip (11/700) | mayúscula 8,4 dp | 1,34 mm | 3,1′ | de cerca; el color y el punto, de lejos |
| Píldora | 8,4 dp de alto | 1,34 mm | 3,1′ | se ve la franja y dónde está el relleno, no el detalle |
| Pulgar | 10,5 × 4,2 dp | 1,67 mm | 3,8′ | se ve "dónde estás" en la tira |
| Totales del diálogo (`num.s` 20) | mayúscula 14 dp | 2,23 mm | 5,1′ | 1,0× |

- **De lejos** se leen la cuenta grande, el aro que se vacía y el amarillo de `PAUSA`, el color del paso, el halo que late y el verde o el ámbar del sello.
- **De cerca** se leen los textos del sello, del chip y de `PAUSA`. Son de la misma familia que los rótulos de la 4a, que también se leen de cerca.

---

## 6. Rendimiento y presupuesto

- **Elementos.** Lo de este documento no cambia la cuenta de 02-tech-design §3.11, que queda en 58 en el peor caso.
  - `PAUSA` es **uno**: el disco y su aro son el fondo y el borde del elemento (`border-radius` 36·s, `border-width` 3·s), y el glifo lo dibuja su `generateVisualContent` (dos rectángulos con `Painter2D`).
  - Si la sonda S-B4b no deja dibujar en el `generateVisualContent` de un `Label`, el glifo va como `IconView` hijo: +1, y la pausa del peor caso llega a 59.
  - El pulgar, los rieles y el brillo los dibuja la misma fila.
- **Texturas.** El halo de la píldora usa una textura propia, porque el radio 14 de la del halo de las tarjetas no sirve para un radio 4. Es como `GlowTexture`, con radio 4, 10 de halo y el mismo perfil, a 3×: 86 × 86 px RGBA (≈ 29,6 KB), generada al arrancar. Para la 6a no hace falta nada más.
- **Repintados.**
  - **La fila de píldoras** se repinta cada vez que el relleno avanza ¼ px, unas 10 veces por segundo en TV; cada cuadro durante los últimos 5 s, por el halo; y cada cuadro durante un cambio de paso, hasta 440 ms.
  - **El reloj de pared**, una vez por minuto.
  - **El punto en vivo** solo cambia su opacidad, sin repintar.
  - **El sello** cambia el alto de la barra durante 240 ms al entrar y 200 al salir: es *layout* en una zona, la excepción que ya admite ANIMATION_BIBLE §0.1.
  - **El diálogo** cambia el texto de sus totales en cada cuadro durante 1,16 s, y no está en el árbol del HUD.
- **0 B por cuadro en el HUD**, como pide 02-tech-design §3.11: los puntos de las polilíneas, el pulgar y los rieles son del elemento.

---

## 7. Capturas: qué tiene que verse

### 7.1 Reglas de toma (además de las de 02-tech-design §5.2)

1. **Los instantes enteros pasan a +0,2 s [cambia].** En un segundo entero vence el tick, los rodillos arrancan y, en el primer cuadro, se ve el valor viejo. A +0,2 s el rodillo (180) ya terminó (DN4b-25).

   | Toma | La spec | La toma | Lo que cambia en lo esperado |
   |---|---|---|---|
   | CAP-01 | 40,0 | 40,2 | el relleno de la 3 en 0,510 (no 0,500) |
   | CAP-13, CAP-16 a -18 | 42 | 42,2 | el punto en vivo en 0,444 |
   | CAP-19, CAP-20 | 8,0 | 8,2 | — |
   | CAP-21 | 150 | 150,2 | el relleno de la 3 en 0,503 |
   | CAP-22 | 3015,0 | **3016,6** | el relleno del pulgar en 0,553, `0:14` y `10:50` (spec §16.2; QA-G4bT-06: a los 3015,0 entraba `Próximo`) *(alineado en WP-D2)* |
   | CAP-23, CAP-24 | 50 | 50,2 | el relleno de la 1 en 0,337 |
   | DEMO-02 | 3 | 3,2 | el punto en 1 |
   | DEMO-05 | 18 | 18,2 | el relleno de la 0 en 0,607 |
   | CAP-06 | 30,2 | **30,25** | a los 30,2 justo termina la salida de la cuenta grande (200) |

   Las tomas a k,6 s, a k,2 s después de un fin y las de pausa no cambian.
2. **Las sondas.** Fondos con ± 1 por canal; trazos, bordes finos y el brillo con ± 3; el halo, **± 10** (es un degradé: en TV y TH cambia ≈ 9 por canal de un píxel al siguiente a 1·s de la píldora, y la sonda cae a ± ½ px de esa distancia; con ± 4 fallaba por el píxel de al lado con el perfil bien, §15.3). *(± 4 en la primera versión; cambiado en WP-D2.)* Los bordes de menos de 2 px (el de la hueca en L640, 1,4 px, y el del chip en W1280, 1,6 px) no tienen un píxel entero y no se sondean: se miran en la toma.
3. **"Al centro"** de una píldora es su centro en x y en y. El borde de la hueca se sondea a 0,75·s de su borde de arriba, en su centro en x. El brillo, 1 px debajo del borde de arriba del relleno.

### 7.2 Por captura

| ID | Qué miro | Sondas (`aspect: true`) |
|---|---|---|
| **CAP-01** (TV, 40,2) | Las 5 píldoras de 66,4 base, iguales ± 1 px y separadas 3·s: la 0 rosa sólida; la 1 hueca, con el borde de 1,5·s entero y el interior tenue; la 2 verde sólida; la 3 con la pista y el relleno al 51 % con su brillo, sin marco; la 4 verde tenue. La fila en modo compacto 1: `Trabajo`, `18 spm` entera, `Próximo: Descanso`, `0:10` y `⏱ 10:00`. La cinta sobre el extremo de la primera píldora. SPM con `obj. 18` | 0 `#CC4065` · 1 interior `#311F37` y borde `#D34167` · 2 `#06B98E` · 3: relleno (al 25 % de su ancho) `#EF476F`, brillo `#F37593` y pista (al 80 %) `#4D253F` · 4 `#073940` · vidrio de la fila `#08162A` · pastilla `#362038` · ícono del reloj (trazo) `#B5B9BF` |
| **CAP-02** (TV, 47,6) | El halo rosa de la 3, solo por fuera, sin cortes, hasta el borde de la fila. La cuenta grande `3` en el disco (167,6-243,4 × 234,0-309,9), a 10,7 de Watts, con el arco al 80 %: el borde que se mueve en los 72°, cerca de las 2:30, y el resto en su pista. `0:03` a 1,1 en rosa. `Próximo: Descanso` | halo a 1·s arriba de la 3 (al centro) `#582742` · disco `#08162A` · arco (a las 6, en el medio del trazo) `#EF476F` · pista del aro (a los 36°) `#362038` · cifra (trazo) `#FFFFFF` · cuenta `#FF5D7F` |
| **CAP-03** (TV, 10,6) | La barra en 74·s (hasta 86,4). El sello centrado, en una línea: el check relleno verde, `Objetivo cumplido` y ` · 24.0 / 18 spm` en blanco. La 0 sólida, ya sin destello; la 1 con la pista y apenas el relleno (0,06) | título (trazo) y disco del ícono `#06D6A0` · detalle (trazo) `#FFFFFF` · vidrio de la fila del sello (y 8,4 + 70·s) `#08162A` · el cielo entre la barra y las tarjetas (x 245, y 90,6: en x 205 cae el halo de SPM con objetivo; WP-D2) `#0B4FA8` · 0 `#CC4065` · 1 pista `#4D253F` |
| **CAP-04** (TV, 20,6) | `No alcanzado` en ámbar, con el ícono `error` de contorno, y `24.0 / 30 spm`. La 1 hueca, con el borde entero | título y aro del ícono (trazo) `#FFD166` · 1: borde `#D34167`, interior `#311F37` |
| **CAP-05** (TV, 725,6) | `Objetivo cumplido · 150 / 150 W · 02:05 / 02:05/500m` entero y sin achicar (338,8 de 339,9). 14 píldoras de 21,8: la 0 amarilla y la 1 rosa sólidas, la 2 (250 m) en curso con la pista sola (Q-51) y el resto tenues. `250m` | 0 `#DAB55D` · 1 `#CC4065` · 2 `#4D253F` · 3 (descanso pendiente) `#073940` |
| **CAP-06** (TV, 30,25) | **La última, llena y en curso**: la 3 verde al 100 % con su brillo, que no es la sólida al 85 %. La 0 y la 2 rosas sólidas, la 1 verde sólida. `Descanso`, `0:00`; sin `Próximo`, halo, cuenta grande, sello ni diálogo | 3: relleno `#06D6A0` y brillo `#44E0B8` · 0 y 2 `#CC4065` · 1 `#06B98E` |
| **CAP-07** (TV, 32,0) | El diálogo de 331 × 318 (y 298-616): el check dibujado de 32 junto a `¡Entrenamiento` / `completado!`, el mensaje en una línea, la caja con las tres filas y `Terminar` a la derecha. El velo; detrás, la barra de CAP-06. La cinta arriba de todo y la llave arriba del velo, en x 16-56 × y 790,1-830,1 | velo sobre la barra `#030911`, sobre el cielo `#042043` (x 205, y 90,6: sin objetivo, SPM no tiene halo) y sobre el agua `#062039` · diálogo `#1E3550` · caja `#273D57` · disco del check `#06D6A0` y su trazo `#1E3550` · íconos de las filas (trazo) `#00B4D8` · rótulos (trazo) `#BEC5CD` · valores (trazo) `#FFFFFF` · `Terminar` `#00B4D8` con el texto `#04131C` |
| **CAP-08** (TV, en) | `Workout Completed!` en una línea; el mensaje en dos; `Time`, `Distance`, `Calories`; `Finish` | como CAP-07 |
| **CAP-09** (TH) | El diálogo de 400 × 284,6 (y 63,2-347,8), con relleno 16 y el título en una línea; detrás, el HUD horizontal; la llave en x 16-56 × y 270,3-310,3, sobre la banda | como CAP-07 |
| **CAP-10** (L640, 1,3) | El diálogo de 400 × 312: el encabezado fijo, con el título en dos líneas; el mensaje y la caja se desplazan 30,3; `Terminar` a la vista; nada cortado | `Audit()` |
| **CAP-11** (W1280) | El diálogo de 400 × 300,6 (209,7-510,3), a escala de escritorio. La llave en su lugar del HUD, porque no se cruza con el diálogo. Detrás, las píldoras finales | como CAP-07 |
| **CAP-12** (TV, 35,2) | La 1 (100 m, la última) en curso con la pista sola, sin relleno ni brillo. `100m`. Sin sello | 1 `#4D253F` · 0 `#CC4065` |
| **CAP-13** (TV, 42,2) | La barra libre: `rowing`, el título (recortado `Entrenamient…`, o entero a 0,818 si el PO lo acepta), `⏱ 10:00` y el chip `EN MARCHA` de 101,8 base, con el punto. El divisor de la banda. El `1` de las cifras sin aire de más | vidrio `#08162A` · chip: fondo `#07333C`, borde `#07856E`, texto (trazo) `#06D6A0`, punto `#077B68` · ícono del reloj `#B5B9BF` · divisor `#263241` (DD4b-01) |
| **CAP-14** (TV, 44,0) | El velo. El disco de `PAUSA` en (205,5; 271,9), con el aro amarillo, el glifo y `PAUSA`. El chip `PAUSADO` sin punto, `Reanudar` y las tarjetas al 100 % | cielo `#09438F` · agua `#0C437A` · vidrio de SPM `#071527` · disco de `PAUSA` `#071527` · aro (a las 12) `#FFD166` · glifo (la barra izquierda, al centro) `#FFFFFF` · chip: fondo `#2C3130`, texto `#FFD166` · `Reanudar` `#06D6A0` |
| **CAP-15** (TV) | El chip `FINALIZADO` sin punto y con el texto en `#FF7A92`; los controles al 0,38 y el *spinner*; ningún diálogo | chip: fondo `#2A1E35`, borde `#8D3252`, texto (trazo) `#FF7A92` |
| **CAP-16** (TH, 42,2) | La barra libre con el título, el reloj y el chip enteros; la fila de abajo como en la 4a; nada se encima | `HudOverlap` |
| **CAP-17** (W1280, 42,2) | A escala 1,6: el chip mide 162,9 dp, y el reloj y el chip van a la derecha. Las zonas de `docs/image-03.png` | como CAP-13, sin el borde del chip (1,6 px en W1280: §7.1) |
| **CAP-18** (TV, en, 42,2) | `Free Workout` entero y `ACTIVE` con el punto, en un chip de 74,1 base | como CAP-13 |
| **CAP-19** (TV, 8,2) | La fila en compacto 2 y **sin pastilla**: `Calentamiento`, `Próximo: Enfriamiento`, `0:12` y `10:00` enteros; nada sale de la barra | `HudOverlap`; `#hud-step-target` oculto |
| **CAP-20** (TV, en, 8,2) | Modo normal, sin pastilla: `Warmup`, `Up next: Cooldown`, `0:12`, `10:00` | `HudOverlap` |
| **AUX-01** (TV, opcional) | `Prueba 4a` a los 78,2 s: `>160W…` con el "…" blanco pegado, en compacto 2, y `Próximo: Descanso`, `0:12`, `10:00` | "…" (trazo) `#FFFFFF` ± 3 |
| **CAP-21** (L640, 150,2) | 6 píldoras de 105,4 base: la 0 amarilla sólida, la 1 rosa hueca, la 2 verde sólida, la 3 en curso (0,503), la 4 verde y la 5 azul tenues. SPM y Watts fuera, con `▲`. La pastilla entera | 0 `#DAB55D` · 1 interior `#311F37` (el borde, de 1,4 px en L640, se mira en la toma: §7.1) · 2 `#06B98E` · 4 `#073940` · 5 `#092B43` |
| **CAP-22** (TV, 3016,6) | La tira de 200 tramos de 1,72 base (4,8 px): lo hecho, alternando rosa y verde al 85 %; el pulgar de 4·s × 10·s (11,1 px) en el 100, con el relleno en 0,553 y su brillo, y un poco más alto que la franja; lo que falta, alternando al 18 %. Se distinguen las tres partes, sin marco. `Trabajo`, `Próximo: Descanso` (quieto), `0:14` y `10:50` | tramo 98 `#CC4065` · 97 `#06B98E` (WP-D2: el pulgar tapa el centro de la 99) · pulgar: relleno (en su cuarto izquierdo) `#EF476F` y pista (en su cuarto derecho) `#4D253F` · tramo 150 `#311F37` · 151 `#073940` |
| **CAP-23** (TH, 50,2) | 6 píldoras de 134,4 base, la 1 en curso; la fila con el reloj; nada se encima | `HudOverlap` |
| **CAP-24** (W1280, 50,2) | Comparable con `docs/image-03.png`: las píldoras arriba (123,2 base = 197,1 dp), `Trabajo`, la pastilla entera, la cuenta y el reloj a la derecha | 0 `#DAB55D` · 1 pista `#4D253F` |
| **CAP-25** (TH, 47,6) | El halo y la cuenta `3` en (457,0; 146,3), debajo de la barra y lejos de SPM | como CAP-02 |
| **CAP-26** (TH, 20,6) | El sello `No alcanzado`, en una línea, sin tapar Split ni SPM | como CAP-04; `HudOverlap` con el alto vivo |
| **CAP-27** (TV, pausa a los 47,0; toma a los 47,6) | El velo y el disco de `PAUSA`, sin la cuenta grande. `0:03` a 1,1, teñida. El halo que sigue latiendo | halo a 1·s arriba de la 3 `#582640` · cuenta `#FF5D7F` · aro de `PAUSA` `#FFD166` · cielo `#09438F` (x 245, y 90,6; WP-D2) |
| **CAP-28** (TV) | Workout en reposo con `No se pudo guardar la sesión` entero y ningún diálogo | aviso `#1E3550` |
| **CAP-29** (L640, 47,6) | El disco de 66,5 en (320,0; 128,1), con 107,6 de aire a SPM y lejos de la zona del bote | como CAP-02 |
| **CAP-30** (L640, 20,6) | El sello en una línea, en la tercera fila (y 54,5-70,2), con las tarjetas desde 83,1 | como CAP-04, sin el borde de la hueca (1,4 px en L640: §7.1) |

### 7.3 Demo (W1280)

| ID | Qué miro |
|---|---|
| DEMO-01 | Workout en reposo, como CAP-10 de la 4a |
| DEMO-02 (3,2) | El HUD entero: la cascada terminó a los 1,12 s. La barra libre con `10:00` y `EN MARCHA`, con el punto en 1 |
| DEMO-03 | El velo, el disco de `PAUSA` (115,2 dp), `PAUSADO` y `Reanudar` |
| DEMO-04 | Workout en reposo, sin diálogo ni aviso |
| DEMO-05 (18,2) | 6 píldoras de 197,1 dp; la amarilla al 60,7 %, con su brillo; `Calentamiento`, `Próximo: Trabajo`, `0:12` y el reloj |
| DEMO-06 (27,6) | El halo de la 0 (p = 1) y la cuenta `3` en el disco de 115,2 dp, en (640; 246,2), con el arco amarillo al 80 % |
| DEMO-07 (90,6) | `No alcanzado · 150 / 160 W · 24.0 / 22 spm`; la 1 hueca y la 2 empezando |
| DEMO-08 | El panel abierto, con Split, Watts y la barra a la vista. Con DN4b-23, el panel empieza 8 debajo de la barra de 74 (y 139,2): el lugar del sello no se tapa |
| DEMO-09 (180,6) | `Objetivo cumplido · 170 / 160 W · 26.0 / 24 spm`; la 3 sólida |
| DEMO-10 (239,2) | La cuenta `1`, con el arco azul en 0,267; el halo de la 5; sin `Próximo` |
| DEMO-11 (242) | El diálogo, con `04:00`, la distancia y las kcal; detrás, la 1 hueca, la 3 sólida y la 5 llena, en curso |
| DEMO-12 | Workout en reposo, sin aviso |

### 7.4 Videos (30 cuadros por segundo; los valores por cuadro están en §4)

| ID | Qué miro |
|---|---|
| **VID-01** (TV, 2,5 s) | La entrada de §4.1, cuadro por cuadro (± 0,01 en opacidad y ± 0,1 dp en la subida). Workout llega a negro a los 7 cuadros, el fondo aparece desde el cuadro 7 y la cascada va de arriba abajo, sin que ninguna caja salte. La banda entra con `00:00` y pasa a `00:01` en el cuadro 30, con su rodillo, mientras los controles todavía suben. La llave entra en los cuadros 34 a 38 |
| **VID-02** (TV, 2,5 s) | La salida de §4.2 con la última foto: `00:12` y `EN MARCHA` o `FINALIZADO` en todos los cuadros, nunca `00:00` ni `—`. El HUD se va en 6 cuadros, el negro, y Workout aparece en los cuadros 13 a 19, sin aviso |
| **VID-03** (W1280, 2,5 s) | La entrada con la rutina: la barra de pasos llega primero, con sus 6 píldoras y la amarilla en curso, que apenas tiene relleno (1,3 % a los 400). La cuenta pasa de `0:30` a `0:29` en el cuadro 30, durante la cascada |
| **VID-04** (TV, 18,5 s) | De los 4,5 a los 23 s de `Estados`:<br>- a los 5,0, el halo entra con su rampa (§4.4), la cuenta crece a 1,1 y se tiñe;<br>- a los 7,0, la cuenta `3` entra (§4.6); a los 8,0 y 9,0 rueda (`2`, `1`) y el aro se vacía sin saltos;<br>- a los 10,0, la cuenta se suelta, la 0 completa su relleno y se solidifica con el destello (§4.5), la 1 enciende su pista y empieza a llenarse, y entra `Objetivo cumplido` (§4.7), que se va en 11,8-12,0;<br>- a los 15,0, el halo de la 1;<br>- a los 20,0, el trazo del borde de la 1 recorre el perímetro mientras el interior se vacía, y entra `No alcanzado`.<br>El reloj de la banda, la cuenta y la cuenta grande cambian en el mismo cuadro de cada segundo |
| **VID-05** (TV, 8 s) | La cuenta `3`, `2`, `1` del último paso. A los 30,0 la cuenta se suelta y la 3 completa su relleno sin solidificarse (queda llena, con su brillo). A los 30,3 entra el diálogo (§4.12): el check se estampa y se dibuja, y los totales cuentan hasta `00:30`, `1120 m` y `43`. A los 33,0, `Terminar`: el diálogo sale (150) y empieza la salida, sin aviso |
| **VID-06** (TV, 5 s) | A 1 s, `Pausa`: el velo en 300, `PAUSA` entra (240, 0,96 → 1) y el chip cruza a `PAUSADO` (150, etiqueta en 0-80 y 80-200), sin punto. El reloj de la banda queda quieto. A 3 s, `Reanudar`: todo vuelve en 240 y el punto vuelve encendido (1) |
| **VID-07** (TV, 6 s) | La tabla de §4.11: BPM se va y kcal baja 75,6 dp; a los 3 s BPM vuelve y kcal sube; nada salta |
| **VID-08** (TV, 2 s) | Reducir movimiento: la página funde lineal en 120 (0,277 · 0,556 · 0,833 · 1), sin negro ni cascada; la llave a los 120; el reloj corre igual |

---

## 8. Contraste verificado (DN4b-12)

### 8.1 Pares para `ContrastTests.ItFourBDesignerPairs`

Con la sintaxis de `ContrastTests.Stack` (la pila de abajo hacia arriba; `white` es el peor fondo del vidrio). Los valores van a tres decimales, para `ItFourBControlValuesMatchTheDesignNotes`; entre paréntesis, contra el vidrio renderizado (`#2B3038`).

| # | Frente | Fondo | Umbral | Valor |
|---|---|---|---|---|
| B1 | `phase.running` · `phase.paused` · `phase.finished.hud` (texto del chip) | `white\|glass.fallback\|phase.running@15` · `…\|phase.paused@15` · `…\|phase.finished@15` | 4,5 | 5,324 (5,244) · 6,432 (6,332) · 4,677 (4,606) |
| B2 | `phase.running` (el punto, entero) | `white\|glass.fallback\|phase.running@15` | 3 | 5,324 |
| B3 | `state.success` · `phase.paused` (título e ícono del sello) | `white\|glass.fallback` | 4,5 | 7,154 (7,035) · 9,358 (9,203) |
| B4 | `step.work` · `step.rest` · `step.warmup` · `step.cooldown` (el arco de la cuenta grande) | `white\|glass.fallback` | 3 | 3,727 (3,665) · 7,154 · 9,358 · 3,407 (3,350) |
| B5 | `phase.paused` (el aro de `PAUSA`) | `white\|glass.fallback` | 3 | 9,358 |
| B6 | `text.secondary` (el ícono del reloj de pared, gráfico) | `white\|glass.fallback` | 3 | 7,448 |
| B7 | `step.<tipo>@85` (la píldora hecha), sobre el fondo provisorio | `backdrop.sky\|glass.fallback` | 3 | 3,868 · 7,204 · 9,259 · 3,645 |
| B8 | `step.<tipo>@85` (el borde de la hueca, sobre su interior) | `backdrop.sky\|glass.fallback\|step.<tipo>@18` | 3 | 3,404 · 5,305 · 6,460 · 3,064 |
| B9 | `state.success` (disco del check) · `surface.overlay` (trazo del check) | `surface.overlay` · `state.success` | 3 | 6,626 · 6,626 |
| B10 | `accent.primary` (íconos de las filas, gráfico) · `text.secondary` (rótulos y la `m`) · `text.primary` (valores) | `surface.overlay\|text.primary@4` | 3 · 4,5 · 4,5 | 4,501 · 6,361 · 11,093 |

- **Ya los cubren** los pares de la 4a y de la It. 3:
  - el blanco sobre el vidrio (H3, 13,493): el detalle del sello, el reloj, la cifra de la cuenta grande y el glifo y la etiqueta de `PAUSA`;
  - el "…" blanco de la pastilla (H7);
  - `rowing` (H16);
  - el diálogo de la app (R26: el título sobre `surface.overlay`, 12,498; R14: el mensaje, 6,984);
  - `Terminar` (W4, 7,641).
- **B7 y B8 van sobre el fondo provisorio**, que es el de la 4b, y no sobre el peor vidrio (§8.2).

### 8.2 Excepciones y riesgo

- **Tenues de contrato**, que no se prueban como umbral: la pendiente al 18 %, la pista al 30 % y la pista del aro al 20 %.
- **Decorativos:** el borde del chip al 50 % (2,490 · 2,855 · 1,769 sobre su tinte), el halo, el destello, el brillo, el velo y el punto en vivo en lo bajo de su latido (0,35).
- **Lo que se corrige** (no es una excepción): `FINALIZADO` en `#FF5D7F` daba 3,942 y pasa a `#FF7A92`.
- **Riesgo para la 6a** (ya en ART_BIBLE §10.1). Con una nube blanca detrás de la barra, la píldora hecha de enfriamiento da 2,850 y la de otro tipo, 2,629. El borde de la hueca contra su interior da 2,624 (trabajo) y 2,407 (enfriamiento), y contra el vidrio 3,154 y 2,944. Sobre el fondo provisorio de la 4b pasan todas. En la 6a, si la escena pone nubes detrás de la barra, esas píldoras usan su variante `.hud` (3,759 y 3,745): queda como pedido de la 6a.

---

## 9. Pedidos al Architect

Ninguno cambia el comportamiento del producto. Los marcados **[contrato]** cambian 02-tech-design §7.5 o una regla de §2 o §3, y conviene resolverlos antes de WP-B1. **[PO]** = solo con el visto bueno del PO (§10).

| ID | Para | Qué | Por qué / cómo se verifica |
|---|---|---|---|
| **DN4b-14** | WP-B1 (`CenterCallouts`), WP-A (`HudGeometry`, `HudTimelines`) [contrato] | **`PAUSA` en el disco** (§3.6):<br>- `#hud-paused-label` absoluto en `HudLayout.BigCount` (no en `Callout`), con `border-radius` 36·s, `glass.fallback` y `border-width` 3·s en `phase.paused`;<br>- adentro, el glifo de pausa (G 28) dibujado en su `generateVisualContent` y la etiqueta 12/700 *tracking* 0,08 em;<br>- **un disco por vez** (§4.8).<br>Sonda S-B4b: `Painter2D` en el `generateVisualContent` de un `Label` junto con su texto; si no da, el glifo es un `IconView` hijo (+1) | La cápsula con `title` 20/700 mide 108 a 122 base y en vertical caben 76. PlayMode: el rectángulo de `PAUSA` es el del disco ± 0,5; `HudOverlap` con Watts con objetivo en TV y P360; las sondas de CAP-14 |
| **DN4b-15** | WP-A (`Tokens.cs`, `PhaseChip`, `tools/tokens`, los conteos) | `phase.finished.hud` `#FF7A92` (`HudText`; 135 → 136) y `PhaseChipSpec.Text` de `FINALIZADO` con ese token | DV-44: `#FF5D7F` da 3,942 sobre el tinte. dotnet: B1 |
| **DN4b-16** | WP-B0 / B1 (`StepInfoBar`), WP-A (`HudGeometry`) [contrato] | **La regla de la fila** (§3.3): los modos normal, compacto 1 y compacto 2 decididos al empezar el paso; la pastilla por fragmentos enteros con el "…" blanco pegado; si no entra ni el primero, funde en 120 y se cierra; el cambio de modo, en 0-240 con el cambio de paso. Reemplaza a DT4b-12 | PlayMode: CAP-01 en compacto 1 con `18 spm` entera; `Prueba 4a` en TV a los 78,2 s con `>160W…` y el "…" en `text.primary`; CAP-19 y CAP-20 sin pastilla; ninguna caja salta cuando entra `Próximo` |
| **DN4b-17** | WP-B1 (`PillRowView`), WP-A (`PillShapes`, `MotionSpecs`) [contrato] | **El halo** (§3.2): desde el borde de la píldora, sin hueco; se ve 0,65·α y se apaga como `(1 − d/E)²` hasta 10·s, 8·s y 4·s (costados, arriba, abajo); con la textura propia de radio 4. Reemplaza "agrandada 2 con 8 de desenfoque y recortada a la fila" | Con la expansión de 2 sin pintar, quedaba un aro a 2 dp de la píldora (se lee como marco), y recortado, un corte duro a 4 dp. PlayMode: la sonda del halo de CAP-02 ± 4; ningún píxel del halo fuera de la fila |
| **DN4b-18** | WP-A (`PillRowGeometry`, `PillShapes`), WP-B1 [contrato] | **Las píldoras angostas** (§3.2): con menos de 6 base, la en curso como pulgar (≥ 4·s × 10·s, centrado en su tramo, encima de las vecinas) y la hueca con dos rieles; el pulgar pasa a la siguiente en 150 (§4.5). `StripCurrentMin` 2 → `PillThumbMinWidth` 4 | DN4b-10. dotnet: el rectángulo del pulgar en la tira y en la separación 1. CAP-22 |
| **DN4b-19** | WP-B1 (`PillRowView`) | **El brillo del relleno** (§3.2): 1·s blanco al 25 % arriba del relleno, recortado por la cápsula; solo en la en curso; se va en 120-340 en el cambio de paso | Distingue "llena en curso" (CAP-06) de "hecha". Sondas de CAP-01 y CAP-06 |
| **DN4b-20** | WP-A (`HudTimelines`), WP-B1 (`CenterCallouts`) | **El aro**: el arco de 360°·(1 − f) a las 12, con el borde que se mueve en el sentido del reloj, extremos rectos y la línea central a 34,5·s | §3.6. Sondas de CAP-02 a las 6 y a los 36° |
| **DN4b-21** | WP-B2 (`CompletionDialog`, `CheckMarkView`), WP-A (`CheckGlyph`) [contrato] | **El diálogo** (§3.8): valores `num.s` 20/700 (no `num.stat`); la `m` como sufijo 14/500 `text.secondary` a 3; rótulos `bodyM` 14; el check de 32 centrado en el alto del título; los puntos del trazo (P0, P1, P2, 0,20·R, inglete); los totales sin rodillo mientras cuentan | `CompletionDialogTests.SizesAndLetter` con los rectángulos de §3.8 ± 1; las sondas de CAP-07 |
| **DN4b-22** | WP-A (`WrenchPlacement`, R10) | **La llave con el diálogo** (§3.9): el hueco libre más bajo del carril izquierdo, a 8 dp de las piezas, en lugar de "16 + margen inferior" | En TV y en P360 se encimaba con `Terminar`, y en TH y en L640, con la banda. dotnet: los cinco lugares de §3.9 |
| **DN4b-23** | WP-A (`SimulatorPanelLayout`) [contrato, PO] | **El techo del panel en el inmersivo**: en horizontal, 8 dp debajo de la barra en 74·s; en vertical, también 8 debajo del disco del lugar centro alto | AC-WKT-09-08 pide el sello visible con el panel abierto. dotnet: los altos máximos de §3.9 |
| **DN4b-24** | WP-B2 (`ImmersiveWorkoutPage`, la capa de avisos) | **Los avisos del final** entran a los 400 de la salida (`EndToastStartMs`), cuando aparece Workout; con reducir movimiento, en el cuadro 0 | Sobre el negro no tiene que haber nada. PlayMode: el aviso empieza a entrar a los 400 |
| **DN4b-25** | WP-B3 (`HudExrCatalog`) | **Los instantes de §7.1**: +0,2 s en los enteros y 30,25 en CAP-06, con lo esperado de esa tabla | Con el tick en ese mismo instante, el rodillo arranca y la imagen muestra el valor viejo |
| **DN4b-26** | WP-B1 (`PhaseChipView`), WP-A (`PhaseChip`) | **El chip** (§3.5, §4.9): el ancho fijo con el lugar del punto solo para `EN MARCHA` (101,8 / 74,1); el contenido centrado; el punto que se va y vuelve con la etiqueta y empieza en 1 | PlayMode: el reloj no se mueve al cambiar la fase; el ancho del chip ± 0,5 |
| **DN4b-27** | WP-B1 (`FreeTopBar`) [PO] | **El título libre** que se achica hasta 0,8 antes del "…" (§3.5) | Solo si el PO lo acepta. Si no, el "…" de la spec |
| **DN4b-28** | WP-B3 (`HudExrCatalog`) | **Las sondas de aspecto** de §7.2 y §7.3, con las tolerancias de §7.1 (DN4b-11); AUX-01, si QA lo pide | Las fallas se anotan para WP-D2 |
| **DN4b-29** | WP-D2 (`ContrastTests`, la excepción de 02-tech-design §7.6) | `ItFourBDesignerPairs` (B1 a B10) e `ItFourBControlValuesMatchTheDesignNotes` | dotnet |

**Prioridad:** primero DN4b-15, DN4b-16, DN4b-17, DN4b-18 y DN4b-14, que son de WP-A y de WP-B1 y cambian geometría; después DN4b-19 a DN4b-22 y DN4b-26; al final DN4b-23 a DN4b-25 y DN4b-27 a DN4b-29.

---

## 10. Para el PO y para QA

| Para | Qué | Propuesta |
|---|---|---|
| PO · confirmar | La entrada, la salida y el sello con los números de la spec | **Confirmados**; retiro las alternativas del Designer (C-01, C-02, C-04 a C-06 de 02-tech-design §8.2) |
| PO · DV-44 | `FINALIZADO` en `#FF5D7F` da 3,942:1, no llega a 4,5 | **`#FF7A92`** (4,677). CAP-15 ya lo dejaba al Designer ("o el que fije el Designer"). La DV-44 dice "`FINALIZADO` en `#FF7A92`, la variante legible del mismo matiz" |
| PO · AC-WKT-13-12 (informativo) | `PAUSA` va en un disco con el glifo de pausa y la etiqueta 12/700, en el lugar de la cuenta grande | Cumple el AC tal como está: la etiqueta aparece con un fundido y 0,96 → 1 en 240, en el lugar que fija el Designer, y con el contraste de los valores. Es una mejora EXR, no una DV |
| PO · **AC-WKT-16-06** (decidir) | En un teléfono en español, el título de la barra libre recortado se lee `Entrenamient…` | Que se achique hasta 0,8 antes de recortarse; entero necesita 0,818. El AC diría: "Si no entran, el título se achica hasta 0,8 y, si aun así no entra, se recorta con …". Por defecto, la spec |
| PO · **AC-WKT-14-15 y CAP-19, -20** (precisión) | En TV, con `Texto largo`, la pastilla no entra ni con su primer fragmento | El AC diría: "La pastilla se recorta por fragmentos enteros, con …; si no entra ni el primero, no se muestra. Antes de recortarla se achican los aires (a 6) y el relleno de `Próximo` (a 4)". CAP-19 y CAP-20 se ven sin pastilla en TV; el "…" blanco, en AUX-01 y en PlayMode |
| PO · Q-59 (DN4b-10) | La tira continua y las píldoras de menos de 6 base | La en curso, como pulgar de 4·s × 10·s: la única píldora que no mide lo mismo que las demás (DV-40) |
| PO · AC-BLE-16-03 (DN4b-23) | El panel del simulador abierto tapa el sello en horizontal y el borde del disco en vertical | El techo, 8 debajo de la barra de 74 (horizontal) y del disco (vertical): 290,2 en TH, 201,8 en L640, 530,4 en W1280, 298,0 en TV y 258,5 en P360 |
| PO · §7.3 (precisión) | Los instantes enteros | +0,2 s (CAP-01 sale con el relleno en 0,51) y CAP-06 a los 30,25 (§7.1) |
| PO · informativo | Sistema visual, no son DV | El brillo del relleno, el halo que no pasa debajo de la fila de información (el del legacy sí pasa), el aro de la cuenta grande, el check de 32 dibujado y los valores del diálogo en las cifras de tablero |
| QA | Lo que se mira | Los pares de §8.1 y las excepciones de §8.2. Las sondas y las reglas de §7. Los valores por cuadro de §4 en los videos, con ± 0,01 en opacidad y ± 0,1 dp en desplazamiento. En [Device-Win], el anillo de foco en `Terminar` del diálogo y el disco de `PAUSA` en la ventana real |
| QA | Riesgo | El de la 6a (§8.2). En L640, el diálogo a 1,3× se desplaza 30,3 (lo permite el AC) |

---

## 11. Plan de WP-D2 (después de WP-B3)

| Qué | Hecho cuando |
|---|---|
| `Skin.uss` y `Motion.uss` | Lo de la 4b es casi todo código: `#completion-finish` usa el primario de la app (*hover*, presionado y foco de las It. 2 y 3) y `PAUSA` es contrato. Solo se ajustan las opacidades finas que no sean contrato, si las tomas lo piden. `SimulatorSkin.uss`, sin cambios |
| Las tomas de `it4b` | 0 desbordes, `HudOverlap` y `WrenchAudit` en verde y todas las sondas de §7.2 y §7.3 dentro de su tolerancia. Orden: CAP-01 a -06, -22 (las píldoras), CAP-02, -25, -27, -29 (la cuenta y `PAUSA`), CAP-03 a -05, -26, -30 (el sello), CAP-13 a -18 (la barra libre), CAP-07 a -11 (el diálogo), el resto y la demo |
| Los videos | VID-01 a VID-08 cuadro por cuadro contra §4 (PyAV y Pillow sin red, como en la 4a) |
| Contraste | `ItFourBDesignerPairs` (B1 a B10) y sus valores de control (DN4b-29) |
| Las biblias | §12 y su sección "Cambios de la It. 4b" |
| Este documento | La sección de lo implementado, con lo que se encontró y se corrigió |

---

## 12. Alineación de las biblias (en WP-D2, con este documento como fuente)

| Dónde | Cambio |
|---|---|
| ART_BIBLE §2, §2.2 y §2.3 | 136 tokens: `phase.finished.hud` `#FF7A92`, el texto de `FINALIZADO` |
| ART_BIBLE §3.3 | La barra de la 4b con sus tres filas y la cinta. Las píldoras: el brillo, el halo desde el borde con sus largos, el pulgar y los rieles. La regla de la fila de información (modos y fragmentos). **El reloj de pared en `num.s` 20/700**, como la cuenta (sale "18/700", P-07). El chip en 700 con el texto en su color y `phase.finished.hud`, el ancho fijo y el punto solo en `EN MARCHA` (sale "si el PO lo acepta, el texto en blanco"). La cuenta grande en 64/800 en el disco de 72·s, con el sentido del aro (sale "`num.xl` 52 en 64·s"). El sello en `#FFD166` con el detalle blanco (sale `state.warning` y `text.secondary`). La barra libre en un teléfono en español |
| ART_BIBLE §3.5 | El diálogo de completado: opaco, el check de 32 dibujado junto al título, los valores en `num.s` 20/700 con la `m` de sufijo y los rótulos en `bodyM` (sale "`num.m`"). `PAUSA` en el disco |
| ART_BIBLE §5.2 | El lugar centro alto: el disco de 72·s, el máximo de 76 base en vertical y un disco por vez |
| ART_BIBLE §10.1 y §10.2 | Los pares B1 a B10; el riesgo de la 6a; la lectura a 1,5 m de §5 |
| ANIMATION_BIBLE §2.6 | El halo (0,65·α en el borde, el perfil de las tarjetas y los largos por lado), la cuenta grande con la cifra de 64 en el disco de 72·s y el aro en el sentido del reloj, y un disco por vez |
| ANIMATION_BIBLE §2.7 | El sello dentro de los 2 s (0-240, 40-240, 60-300, 1800-2000), en `#FFD166` y con el detalle blanco (sale la versión de 120 a 2280). El brillo que se va al cambiar de paso y el pulgar |
| ANIMATION_BIBLE §2.11 | La entrada y la salida de la 4b con los números de la spec (sale la cascada de 120 a 560 y la salida de 200). `PAUSA` (sale "queda para cuando la pida el PO"). Los avisos del final a los 400. El diálogo de completado con el check junto al título |
| ANIMATION_BIBLE §2.12 | El chip de fase y el punto en vivo |
| LEGACY_VISUAL_AUDIT §8 | Dos hallazgos del legacy: el halo de la píldora pasa por debajo de la fila de información; con más de ≈ 120 pasos en un teléfono, la fila queda vacía (DV-40) |
| Las dos biblias | La sección "Cambios de la It. 4b" |

---

## 13. Qué queda abierto

- **NO VERIFICABLE hasta WP-B0 y WP-B1** (las sondas en el proyecto real):
  - `Painter2D` en el `generateVisualContent` de un `Label` (`PAUSA`, DN4b-14);
  - la textura del halo con largos distintos por lado, por `Allocate` (DN4b-17);
  - el corte por fragmentos con el "…" en `text.primary` (DN4b-16).
- **Mis medidas son aproximadas.** Los anchos salen de fontTools con las tablas K2; WP-B1 los mide en Unity. Los márgenes más justos son CAP-01 (6,1 base en compacto 1), el sello de `Pirámide 500` (1,1) y `PAUSED` en el disco (2,7 por lado).
- **[Device-And]:** las barras del sistema y los márgenes estables (02-tech-design §4) quedan NO VERIFICABLES sin el módulo y el teléfono.
- **La It. 5:** con los paneles de series expandidos, el lugar centro alto en vertical (04b/00-deuda-diseno §4.8). La 4b no lo cambia.
- **La 6a:** el riesgo de §8.2. El velo de pausa pasa a ser el *Volume* de URP, y el negro de la entrada, el vuelo de cámara: la cascada queda igual.
- **Decisiones del PO:** las de §10 marcadas "decidir" y "precisión".

---

## 14. Verificación hecha

1. **Legacy, solo lectura.** Se leyó `lib/features/workout/step_pill.dart` (entero) y se miró `docs/image-03.png`. `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar. No se leyó `strava_secrets.dart`.
2. **Anchos de texto** (`measure4b.py`): fontTools 4.68.0 (`uv run --offline`) sobre las fuentes del repo.
   - Las cifras de `TabularNumber`, en celdas de 0,521 y 0,535 em; `:` y `.`, en 0,32 em.
   - El kerning es el de las **listas K2 versionadas** (`UI/Fonts/Kerning/*.pairs.json`), por índice de glifo, como lo aplica TextCore. El *tracking*, N/100 em por letra.
   - Con las tablas, `Texto largo` mide 346,5 base y no 347,0 como en el Anexo A, y CAP-01, 339,8 y no 340,9: las conclusiones de 02-tech-design no cambian.
3. **Contrastes** (`contrast4b2.py`): el método de `Contrast.cs` (mezcla sRGB sin redondear y WCAG sin redondear) contra el peor vidrio (`#2A2F37`) y renderizado (`#2B3038`). La variante de `FINALIZADO` es una búsqueda OKLCH con el matiz de `#EF476F` fijo.
4. **Geometría** (`geo4b2.py`, `wrench4b.py`): sobre los rectángulos de 04a/04-design-notes §3.8 (sin la derivada, que solo agranda los aires). Se calcularon las barras, las píldoras en los tres regímenes, el lugar centro alto, el disco máximo, el diálogo en 5 perfiles × es / en × 1 y 1,3, y la llave.
5. **Curvas** (`motion4b2.py`): las *cubic-bezier* de `Tokens.cs` con el método de `Easing.cs` (réplica en `rmcore.py`). El halo del legacy se modeló con el desenfoque de Flutter (sigma = 8 × 0,57735 + 0,5).
6. **Sistema construido, leído sin tocarlo:**
   - `Tokens.cs` (135 tokens);
   - `Icons.cs`: están `schedule`, `check_circle` (relleno), `error`, `timer`, `straighten`, `local_fire_department`, `rowing` y `pause`;
   - `GlowTexture.cs`: el perfil `(1 − d/E)²`, con radio 14 y 12 de halo;
   - `ContrastTests.cs`: la sintaxis de las pilas.
7. **El repositorio.** Solo se escribió este documento. Unity no se abrió, dotnet no se corrió y no se tocaron tokens, código, assets, capturas ni biblias. Los programas quedan en `<scratch>/it4b/designer-notes/`, sin versionar. Ninguna ruta de la PC en este documento.

---

## 15. Cambios por implementación de WP-D2

WP-D2 (el pulido del Designer, 02-tech-design §7.3 y §11 de este documento), 2026-10-29, sobre WP-A, WP-D1 y WP-B ya hechos. Fuente: las tomas de `it4b` del arnés real (corridas en `<scratch>/it4b/wp-d2pulido/`), los videos VID-01 a VID-08 cuadro por cuadro (PyAV y Pillow por `uv run --offline`, sin red, con programas propios en `<scratch>/it4b/wp-d2pulido/tools/`) y las pruebas de dotnet y de Unity. No cambia el QUÉ ni la lógica: lo que necesita código de WP-B va como hallazgo (§15.5).

### 15.1 Qué se escribió

| Archivo | Qué |
|---|---|
| `Presentation/Styles/Skin.uss` | La sección "It. 4b": **el aire del diálogo de completado** (§15.2, punto 1). Nada más: lo de la 4b lo dibuja y lo mueve el código o es contrato (02-tech-design §7.5), y `#completion-finish` es el primario de la app, con su capa, su presionado y su anillo de las It. 2 y 3 |
| `Presentation/Styles/Motion.uss` | La sección "It. 4b", **sin reglas nuevas**, con el porqué: ninguna pieza de la 4b tiene transición de USS (en batchmode quedaría congelada a mitad en una toma y, al salir del inmersivo, seguiría animando algo que ya no está, AC-WKT-13-09) |
| `Tests/EditMode/Design/ContrastTests.cs` | `ItFourBDesignerPairs` (B1 a B10 de §8.1: 25 casos) e `ItFourBControlValuesMatchTheDesignNotes` (los valores de §8.1 a 3 decimales, los renderizados contra `#2B3038` y los de `#FF5D7F`, el `FINALIZADO` de la spec que no llegaba: 3,942 y 3,883). Excepción de dueño de 02-tech-design §7.6 |
| `Snapshots/HudExrCatalog.cs` | **Las sondas de aspecto de §7.2** (DN4b-11, DN4b-28), en un bloque propio (`AddAspectProbes`): 148 sondas de aspecto en 26 tomas, con los colores esperados compuestos desde los tokens (sin literales, ADR 016), más las dos que 02-tech-design §5.3 pone entre las comprobaciones: el divisor de la banda en CAP-13 (DD4b-01, ± 2) y el "…" blanco de CAP-31 (DD4b-03, ± 3). El archivo es de WP-B3: el bloque no toca sus tomas ni sus comprobaciones (como las sondas del Designer de la It. 2 en `SnapshotCatalog`) |
| `Tests/UiSupport/PixelProbe.cs` | `PointIn`: un píxel en un punto de una parte del elemento (la píldora n, una franja de ella, el disco de la cuenta grande), que no son elementos. Mide como `Point`. Es una fábrica más, sin cambiar las otras |
| `docs/migration/design/{ART_BIBLE,ANIMATION_BIBLE,LEGACY_VISUAL_AUDIT}.md` | §12 de este documento y "Cambios de la It. 4b" |
| Este documento | §4.5 y §4.14 con "sin destello ni trazo" (QA-G4bT-03); §7.1 con CAP-22 a los 3016,6 y las tolerancias de las sondas; §7.2 con CAP-22 corregida; esta sección |

### 15.2 Lo que se vio en las tomas y se corrigió

1. **El diálogo de completado no tenía aire arriba** (CAP-07 a CAP-11, DEMO-11). El diálogo esconde el título de la It. 3 y pone su encabezado (el check, 12 y el título) en el bloque fijo, que en la It. 3 va *debajo* del título y por eso no tiene relleno arriba (`padding: 0 24px 16px`). Quedaba el check a 1 dp del borde de arriba del diálogo y 28 dp hasta el mensaje (el bloque suma 16 a los 12 del encabezado). En `Skin.uss`, `.rm-dialog--completion .rm-dialog__pinned` lleva el relleno del diálogo arriba (24; 16 con `--compact`) y 0 abajo, y `.rm-dialog__actions`, 20 arriba: lo de §3.8 y el contrato de 02-tech-design §7.5 ("12 hasta el contenido", "20 hasta las acciones"). Medido después, en el arnés:

   | Toma | Diálogo (dp) | Check | Título → mensaje | Caja → `Terminar` | `Terminar` → abajo |
   |---|---|---|---|---|---|
   | CAP-07 (TV) | 40,0-371,4 × 296,8-617,5 (alto 320,8) | +33,9 (centrado en las dos líneas del título) | 12,2 | 20,4 | 25,1 |
   | CAP-08 (TV, en) | 40,0-371,4 × 296,4-617,9 | +25,1 | 12,2 (desde el check) | 20,4 | 25,1 |
   | CAP-09 (TH) | 257,1-657,1 × 62,1-349,3 (alto 287,2) | +17,1 | 12,2 | 20,4 | 17,2 |
   | CAP-11 (W1280) | 440,0-840,0 × 209,0-512,0 (alto 303,0) | +25,0 | 12,0 | 20,0 | 25,0 |
   | CAP-10 (L640 × 1,3) | 120,0-520,0 × 24,0-336,0 (el máximo: se desplaza) | el encabezado fijo, con el título en dos líneas | — | — | 17,0 |

   Son los rectángulos de §3.8 con 1 a 2,7 dp más de alto: §3.8 no contaba el borde de 1 arriba y abajo, y Unity mide el título y el mensaje unas décimas distinto que fontTools. La comprobación del arnés (el diálogo entero en la ventana) y `CompletionDialogTests.SizesAndLetter` (el ancho y el borde de 40) no cambian.
2. **Puntos de sonda mal elegidos en §7.2** (como en la 4a con el cielo de W1280):
   - el cielo de CAP-03 y CAP-27 en x 205 caía en el halo de objetivo de SPM (`#0A54AA`): va en el hueco entre SPM y Split (x 245);
   - en CAP-22 el pulgar mide 11,1 px y tapa el centro de las vecinas 99 y 101 (`#EF476F` en la 99): el par hecho se mide en la 97 y la 98;
   - el borde de la hueca en L640 (CAP-21, CAP-30: 1,4 px) y el del chip en W1280 (CAP-17: 1,6 px) no tienen un píxel entero (dan el 72 % y el 60 % de su color): se miran en la toma, no con una sonda;
   - el halo con ± 4 fallaba por el píxel de al lado (§15.3): pasa a ± 10 (§7.1).

### 15.3 Las sondas

**150 sondas** en 26 tomas (148 de aspecto y 2 comprobaciones): **149 dentro de su tolerancia** en las dos corridas, casi todas a 0 o 1 por canal del valor de §2.3 (las píldoras, el vidrio, el cielo, el velo y el diálogo a ± 1; los trazos, el aro y el chip a 1 de ± 3). Fuera: la pista del pulgar de CAP-22 (`#6F344E` contra `#4D253F`), que es H-D2-01 (§15.5).

**El halo**, medido en la columna del centro de la píldora 3 de CAP-02 (TV, 2,77 px por base), desde su borde de arriba hacia afuera: 0,424·α en el primer píxel (0,455 en el borde, §3.2), 0,346 a 1·s (las notas: 0,348), 0,277 a 1,8·s, 0,117 a 4,4·s y 0 a los 8·s, donde empieza el borde de la barra; abajo, 0 a los 4·s. Es `(1 − d/E)²` con E = 8 arriba y 4 abajo, sin hueco en el borde, sin aro y sin corte: DN4b-17 tal cual. La sonda de "a 1·s" cae a ± ½ px de esa distancia, y en TV un píxel del degradé vale ≈ 9 en el rojo: midió `#612945` (el píxel de al lado, 0,385) contra `#582742`.

### 15.4 Los videos, cuadro por cuadro

Contra §4 de este documento, con los valores por cuadro que el arnés deja en el *manifest* (y que compara con las líneas de tiempo puras en VID-01, -04 y -05) y con los píxeles de cada cuadro:

| Video | Qué se verificó | Resultado |
|---|---|---|
| VID-01 | El toque en el cuadro 7,5: Workout a negro en 220 (la página en 0,39 · 0,66 · 0,835…), el fondo en 220-400, la cascada de arriba abajo (la barra a los 400, SPM a los 520, Split y Watts a los 600, la banda a los 680, kcal a los 760, los controles a los 840; +8 → 0), la banda que pasa a `00:01` en el cuadro 38 (1 s después del toque) mientras los controles terminan de subir y la llave que entra a los 1120-1280 en su lugar del HUD | Bien. Ninguna caja salta (`HudOverlap` por cuadro) |
| VID-02 | La salida con la última foto: `00:12` y `EN MARCHA` → `FINALIZADO` en todos los cuadros, nunca `00:00` ni `—`; el HUD se va en 0-200 (en el cuadro 19 al 0,44, como el 0,421 de §4.2), el fondo a negro en 200-400 (0,97 · 0,88 · 0,70 · 0,42 · 0), Workout en 400-640, sin aviso; la llave sale en el cuadro 0 y entra en el carril izquierdo al terminar | Bien. El diálogo de terminar sale (150) mientras el HUD empieza a irse debajo |
| VID-03 | La barra de pasos llega primero, con sus 6 píldoras y la amarilla en curso; el diálogo de iniciar se va (150) mientras Workout funde a negro; la cuenta pasa de `0:30` a `0:29` durante la cascada | Bien |
| VID-04 | El halo desde `0:05`, con su rampa; la cuenta grande `3` en el mismo cuadro que `0:03` de la barra (75), `2` y `1` con su rodillo y el aro que se vacía sin saltos; a los 10 s la cuenta se suelta (1 → 1,1 y se apaga en 200), la 0 completa su relleno y se solidifica con el destello (el más claro en el cuadro 168, a los 120), la 1 enciende su pista y entra `Objetivo cumplido` (la barra crece en los cuadros 167-169), que se va en 11,8-12,0; a los 15 s el halo de la 1; a los 20 s el interior de la 1 se vacía mientras su borde se dibuja (cuadros 469-478) y entra `No alcanzado` | Bien. El tipo y la pastilla cruzan con el fundido de la 4a (0-80 y 80-200) mientras `Próximo` cambia en el cuadro del dato |
| VID-05 | La cuenta `3`, `2`, `1` del último paso; a los 30,0 la 3 completa su relleno y queda llena con su brillo (no se solidifica); el diálogo entra a los 30,3 (cuadro 129), con el disco del check que se estampa (0,6 → 1,039 → 1) y el trazo que se dibuja; los totales llegan a `00:30` en el cuadro 156, a `1120 m` en el 160 y a `43` en el 164 (1220, 1340 y 1460 ms desde el fin, como §4.12); la llave cambia al carril izquierdo cuando termina la entrada del diálogo (cuadros 136-139); `Terminar` a los 33 s: el diálogo sale (150) y empieza la salida | Bien, salvo la llave en la salida: H-D2-02 |
| VID-06 | `Pausa` en el cuadro 30: el velo en 300 (0,0017 · 0,0080 · 0,0224 · 0,0523 · 0,0977 · 0,1276 · 0,1420 · 0,1483 · 0,15: la tabla de §4.8), `PAUSA` entra en su disco (240, 0,96 → 1) y el chip cruza a `PAUSADO` sin punto; el reloj de la banda, quieto; `Reanudar` en el 90: todo vuelve en 240 y el punto vuelve encendido | Bien |
| VID-07 | La caída del pulsómetro en el cuadro 30: BPM funde en 0-120 y kcal baja 71,4 dp en 120-360 (FLIP: −71,4 · −68,8 · −60,9 · −41,7 · −16,4 · −5,1 · −0,8 · 0); BPM vuelve en el cuadro 150 y kcal sube (70,3 → 0) antes de que BPM aparezca (120-360) | Bien. Nada salta |
| VID-08 | Reducir movimiento: la página funde lineal en 120 (0,278 · 0,556 · 0,833 · 1) con el fondo y el HUD ya enteros, sin negro ni cascada; la llave a los 120, solo con opacidad; la banda pasa a `00:01` 1 s después del toque | Bien. Los primeros 0,5 s el HUD muestra `0.0`, `--:--` y `0` hasta la primera lectura del remo de prueba (llega en k − 0,5): es la conducta de la app, que en VID-01 queda tapada por la cascada |

**Nada queda animando al salir del inmersivo**: en VID-02 y VID-05 los cuadros de Workout después de los 640 son iguales entre sí (salvo la llave, que entra en 160), y la regla de toma de DEMO-04 y DEMO-12 (a los 800 ms de la salida) pasa sin nada en `inFlight`.

### 15.5 Hallazgos para WP-B (no se tocó lógica)

| ID | Sev. | Dónde | Qué se ve | Propuesta |
|---|---|---|---|---|
| **H-D2-01** | menor | `Presentation/Workout/PillRowView.cs` (`DrawThumb`), WP-B1 | **El pulgar deja ver la franja de abajo.** Su pista es la cápsula al 30 % encima de la tira: lo que no está lleno muestra la vecina de la derecha (en CAP-22, la 101, un descanso pendiente) a través del 30 %: `#4E3D4E` y `#6F344E` en lugar de la pista de siempre (`#4D253F`). §3.2 pide "adentro lleva la pista, el relleno y el brillo de siempre". De lejos el pulgar se lee igual; de cerca, la parte que falta se ve verdosa | Que la tira y las píldoras angostas no se dibujen debajo del rectángulo del pulgar (cortar los tramos de `StripRuns` y las cápsulas vecinas en sus bordes), así el 30 % queda sobre el vidrio de la barra, como en las píldoras anchas. La sonda de CAP-22 ya espera eso |
| **H-D2-02** | menor | la llave en la salida desde el diálogo de completado (`SimulatorLayerView` / `ShellLayout.HoldWrench`), WP-B2 | Con el diálogo abierto, la llave está en el carril izquierdo (R10, y 790,5). Al tocar `Terminar` **salta 3,8 dp** a su lugar de Workout en reposo (y 794,3) en el cuadro 210 y **queda a la vista sobre el negro** toda la salida (VID-05, cuadros 210 a 239). Sin el diálogo (VID-02) sale en el cuadro 0 y entra al terminar, como dicen ANIMATION_BIBLE §2.11 y 02-tech-design §3.3 | Que la salida saque la llave en el cuadro 0 (90) también cuando ya está en el carril izquierdo, y la vuelva a entrar a los 640 en su lugar de Workout, como en VID-02 |
| **H-D2-03** | cosmético | el relleno de la píldora en curso (`PillShapes.FillPolygon` y `Painter2D`), WP-A y WP-B1 | **Un píxel suelto del color de la pista dentro del relleno**, en CAP-27 (TV, en (275,8; 22,1): el centro de la franja, 6 dp antes del borde del relleno), una grieta de la teselación del polígono recortado. En el barrido de las 24 tomas con barra de pasos es la única | Revisar el polígono del relleno en el centro de la franja (un vértice en T o un triángulo degenerado); si no, cerrar con un rectángulo interior sin recorte debajo del polígono |

### 15.6 Verificación

- **Contraste** (dotnet, `ContrastTests`): los 25 casos de `ItFourBDesignerPairs` sobre su umbral y los valores de §8.1 iguales a 3 decimales.
- **El arnés**: `capture-snapshots.ps1 -Set it4b`, la corrida A a `<scratch>` y la B al repo, una detrás de otra, con las sondas de §15.3; los mismos bytes en las fotos y los videos, salvo `durationMs` del *manifest*.
- **Legacy, solo lectura**: se miró `lib/features/workout/immersive_workout_screen.dart:205-250` y `:955-1019` (el diálogo de completado: un `AlertDialog` con el ícono de 28 y 12 de aire, el mensaje, 16 y la caja). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó `lib/core/strava/strava_secrets.dart`.
- **Repo público**: este documento cita el repo con rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) o `../rowmate-legacy` y el scratch como `<scratch>`; ninguna ruta de la PC.


---

## 16. Cambios por verificación (ronda 1)

Lo escribió el Architect al corregir la ronda 1 ([02-tech-design](02-tech-design.md), "Cambios por verificación (ronda 1)"). Solo cambia un valor de este documento; lo demás es que el código llegó a lo que ya decían estas notas.

| Qué | Antes | Ahora | Dónde |
|---|---|---|---|
| **El aire mínimo entre el título libre y el reloj** (§3.5, el "···") | 0 en estas notas; 6 en el código (B-14 de 02-tech-design, calibrado con el chip angosto de QA-V4b-02) | **2** (`InfoRowFit.FreeTitleClockGap`). Con el chip a su ancho y 6, el título en TV en español quedaba a 0,779 y se recortaba; con 0 quedaba a ≈ 3 dp del ícono del reloj (B-14). Con 2 se ven ≈ 5 (el lado del ícono y el de la última letra suman ≈ 3) | §3.5 |
| **La escala del título libre en TV y P360 en español** (DN4b-06, DN4b-27) | 0,818 en estas notas (fontTools, sin aire) | **0,805**, entero: 125,1 de 155,4 base, con el chip que Unity mide en 101,3 y el aire de 2. Sigue por encima del tope de 0,8 y sin "…" (AC-WKT-16-06); en inglés y en horizontal, a su tamaño | §3.5, CAP-13 |
| El *tracking* del chip y de `PAUSA` (§3.5, §3.6) | 0,0088 em en el código (QA-V4b-02) | 0,08 em, el de estas notas (8 en la unidad de TextCore): el chip mide 101,3 base en español y 73,5 en inglés (notas: 101,8 y 74,1); `PAUSED`, 53,9 en la cuerda de 59,3 | sin cambio en las notas |
| H-D2-01, la pista del pulgar (§15.5) | se veía la franja de abajo | cerrado: la tira no se pinta debajo del pulgar; la sonda de CAP-22 pasa | sin cambio |
| H-D2-02, la llave en la salida desde el diálogo (§15.5) | a la vista sobre el negro | cerrado: sale en el cuadro 0 y vuelve a los 640 (VID-05, cuadros 210-212 y 232) | sin cambio |
| H-D2-03, el píxel suelto en el relleno (§15.5) | CAP-27 | cerrado: el contorno sin vértices repetidos y, debajo del relleno opaco, un rectángulo sin suavizado (V1-04) | sin cambio |

Las tomas que cambian por esto, regeneradas con dos corridas iguales salvo `durationMs`: todas las que tienen píldoras (el contorno con los bordes rectos), CAP-13 a CAP-18, CAP-27, DEMO-02 y DEMO-03 (el *tracking* y la escala del título). 150 / 150 sondas dentro de su tolerancia.
