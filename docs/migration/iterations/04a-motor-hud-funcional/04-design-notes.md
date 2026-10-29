# It. 4a — Notas de diseño visual y de motion

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-28 |
| Estado | **Corregido por el gate de armonía de QA** (§16: [paridad](03-qa-harmony-paridad.md) y [técnica](03-qa-harmony-tecnica.md), los dos APROBADO CON OBSERVACIONES). **WP-D0 y WP-D hechos** (§17): la derivada RowMate Numeric SC, la receta y las listas K2, los pares de contraste, `Skin.uss` y `Motion.uss`, el visto bueno de las 49 tomas y de los 3 videos, y las biblias. Lo que cambia el contrato USS de 02-tech-design §9.5 está marcado **[contrato]** y pedido en §10 |
| Iteración | 4a · `04a-motor-hud-funcional` ([ROADMAP §It. 4a](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (§1, §4.8 a §4.15, §5, §7, §10, §11, §14), [02-tech-design](02-tech-design.md) (§6, §7.3, §9.4, §9.5, §10.1, §10.3 con DN4a-01 a DN4a-12 y el Anexo B), [00-deuda-diseno](00-deuda-diseno.md) (D-ART-10, K1 a K6, §4), [04-design-notes de la It. 3](../03-rutinas/04-design-notes.md) (el sistema construido), [ART_BIBLE](../../design/ART_BIBLE.md) (§2.3, §3.2 a §3.5, §3.7, §3.11, §4, §5.2, §5.3, §8.1, §10), [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) (§1, §2), y del código: `Core/Design/{Tokens,MotionSpecs,Contrast,Easing,CriticalSpring,Icons}.cs`, `Presentation/Controls/TabularNumber.cs`, `Presentation/Styles/{Base,Skin}.uss`. Capturas: de la It. 3, CAP-04, CAP-06, CAP-08, CAP-28 y DEMO-01; de la It. 2, CAP-01 |
| Entrega | Este documento: las respuestas a DN4a-01 a DN4a-12 (§1) y los pedidos nuevos DN4a-13 a DN4a-22 (§10) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/workout_screen.dart:1-204`, `lib/features/workout/immersive_workout_screen.dart:40-150`, `:300-560` y `:560-960`, `docs/image-02.png`, `docs/image-03.png`; en el gate, otra vez `workout_screen.dart:19-35` y `:160-205`, `immersive_workout_screen.dart:40-147` y `:300-330`, `lib/main.dart:116-175` y `lib/shared/theme.dart:13`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificado | Programas propios en `<scratch>/it4a/designer-notes/`, con fontTools 4.68.0 por `uv run --offline` (sin red): anchos de texto con el kerning GPOS y el tracking de UI Toolkit (`measure.py`); la geometría del HUD en 5 perfiles × 6 casos de valores × con y sin simulador, 60 combinaciones sin ninguna superposición (`hud_layout.py`); contrastes con el método de `Contrast.cs` (`contrast.py`, `contrast2.py`); compuestos de las sondas (`probes.py`); curvas por cuadro con las cubic-bezier de `Tokens.cs` y la solución exacta de `CriticalSpring` (`motion.py`); avisos en el inmersivo (`toast.py`); en el gate, la vista "en curso" (`gate/inprogress.py`: anchos, centro por perfil y el título encimado de §4.1.5). **Unity no se abrió** y dotnet no se corrió: este documento no cambia código, tokens ni assets |

Convenciones: las de las It. 2 y 3 (hex `#RRGGBB`, transparencia `@NN %`, WCAG 2.x sin redondear, 1 px de USS = 1 dp). Perfiles: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720, con márgenes seguros en 0. Rectángulos en coordenadas de ventana: x desde la izquierda, y desde arriba. `s` = `hudScale` = `clamp(lado corto / 390; 0,9; 1,6)`: 1,054 en TV y TH, 0,923 en P360 y L640, 1,6 en W1280. "Base" = dp antes de multiplicar por `s` (02-tech-design §6.4: cada zona se arma a tamaño base y se escala con `scale`). "Caja de línea" de Inter = 1,238 em (medida en Unity, It. 2). "Código" = animación por `MotionDriver` con el reloj inyectable; "USS" = transición de `Motion.uss`, siempre bajo `:hover`. Nombres `#…` y `.rm-…` del contrato de 02-tech-design §6 y §9.5; lo que agrego va marcado **[nuevo]**.

---

## 0. Resumen

1. **El HUD horizontal se resuelve con la fila de abajo (DN4a-01).** En horizontal, kcal y BPM bajan a la altura de los controles, abajo a la derecha, en fila: kcal en el rincón y BPM a su izquierda. La banda baja como en el legacy (`immersive_workout_screen.dart:669-706`) y en horizontal es **compacta** (relleno 14 y 10 a cada lado del divisor, en lugar de 20 y 16). Queda una fila de abajo `banda │ controles │ kcal y BPM`, y Split y Vatios solos arriba a la derecha. Las zonas y el orden del legacy se conservan (DV-33). Nada se encima en las 60 combinaciones medidas. Con valores normales solo se achica algo en L640: la banda (sus cifras a 0,776) y, con objetivo, SPM (0,962).
2. **El centro de la zona del bote es el de la cámara (DN4a-02).** `horizonte + lado corto / alto × 2,2 / 9,5`, con el horizonte al 40 % en vertical y al 42 % en horizontal (ART_BIBLE §8.1). Da **0,504** del alto en vertical y **0,652** en horizontal, no 0,5. En horizontal el remero queda bajo, como en `docs/image-03.png` (el carro, en y ≈ 700 de 1080 = 0,65). Así la 6a no tiene que mover nada.
3. **La llave en el inmersivo, ajustada (D-ART-10).** En vertical va en el carril derecho, 8 dp sobre la columna de kcal y BPM, en el lugar que esa columna tiene con BPM: no se mueve cuando el pulso aparece o se va. En horizontal va 8 dp a la derecha de `Pausa`, centrada en su alto. Si a su derecha no entra la fila de kcal y BPM con sus anchos mínimos (pasa en L640), va 8 dp sobre `Pausa`. Así queda libre el hueco entre la banda y los controles para los avisos: el de CAP-29 ya no tapa el reloj. Y en L640, donde el Anexo B no encontraba lugar, ahora hay uno.
4. **Tarjetas legibles a 1,5 m y que no bailan.**
   - Las cifras son las tabulares dibujadas de RowMate Numeric SC (DD4a-07).
   - Con objetivo, la tarjeta reserva desde que empieza el paso un **hueco de 0,48 em para la flecha**, a la izquierda del número: el número no se corre al cruzar el umbral.
   - `obj.` va debajo, alineado con las cifras (el objetivo queda justo bajo el valor real).
   - Las unidades (`W`, `m`) van en la variante `.hud` al 80 %. Las del legacy, al 55 % del color base, dan 3,2-4,0:1.
5. **Semáforo con un solo empujón.**
   - El color cruza en 150 ms y la flecha entra en 120 ms, desplazándose 4 dp en su sentido (▲ sube, ▼ baja).
   - El número da un empujón de 1,04 como mucho cada 4 s, y nunca al empezar un paso: ahí ya cambian el borde, el halo y `obj.`.
6. **Fila del paso.**
   - Las dos barras miden 40 dp (DN4a-04).
   - Las pastillas llevan el texto en blanco sobre el tinte del tipo al 20 %: el texto en el color del tipo sobre su tinte da 3,5-3,7:1 en el peor fondo.
   - `Próximo` late en su fondo y en su borde, con el texto siempre entero. El PO lo aceptó en el gate como **DV-38**, junto con los textos en blanco. Con el pulso del legacy, el texto bajaba a ≈ 3:1 en el punto más tenue.
   - Un solo reloj de 600 + 600 ms mueve `Próximo` y la cuenta, como en el legacy. Al empezar "por terminar", el color entra con una rampa de 150 ms: no salta.
7. **Fondo provisorio (DN4a-03): dos tonos planos del mediodía del legacy.**
   - Arriba `skyTop` `#0B4FA8` y abajo `waterNear` `#0E4F8F`, partidos en la línea del horizonte de la cámara.
   - Son dos tokens nuevos, `backdrop.sky` y `backdrop.water` (133 → 135).
   - Así el vidrio se ve como vidrio (`#08162A` sobre el cielo, `#081627` sobre el agua) sin dibujar agua, bote ni orilla. Con `bg.app` las tarjetas casi desaparecían: el vidrio sobre `bg.app` da `#080F19`.
8. **Reposo y diálogos con el sistema de la It. 3 (DN4a-08 y DN4a-11).**
   - En Workout, la tarjeta de Rutinas lleva el *chevron* en la misma columna de 28 dp donde Rutinas tiene el `⋮`.
   - Cargando, error y vacío van centrados en lo que queda debajo de `Entrenamiento libre`.
   - "En curso" va **sin la barra superior**, como la `_ActiveView` del legacy: solo `Entrenamiento en curso...`, centrado en toda la pestaña. Se ve solo mientras entra el inmersivo (§4.1.5, gate QA).
   - El aviso naranja crece y colapsa con los tiempos de la It. 3.
   - El diálogo de terminar es el de la app (opaco), no una variante de vidrio: sin desenfoque, el "vidrio" no agrega nada.
9. **Contraste (DN4a-09).** 21 grupos de pares, todos sobre su umbral en el peor fondo. Cinco correcciones a lo que traía el legacy o la biblia:
   - las pastillas;
   - `Próximo:` al 60 %;
   - las unidades al 55 %;
   - la capa blanca del *hover* de `Terminar`, que da 4,494:1 (se pasa a la capa oscura de la It. 3);
   - el anillo de foco cian sobre una nube (2,465:1): en el HUD pasa a ser blanco, con un halo oscuro pegado.
10. **Motion (DN4a-05 y DN4a-06).** Todo por código y determinista, con valores por cuadro en §5 para VID-01 a VID-03:
    - el inmersivo entra en 240 ms y sale en 200, por fundido (120 con "reducir movimiento"; la entrada "sumergirse" es de la 4b);
    - el cambio de paso es un fundido cruzado con subida de 8 dp, y el borde, el halo, `obj.` y el hueco de la flecha cambian en 240;
    - kcal se desliza cuando BPM aparece o se va;
    - el rodillo dura 180 ms en todos los números, también en el reloj y en la cuenta.

---

## 1. Respuestas a los pedidos de 02-tech-design §10.3 y a sus desvíos

| Pedido | Respuesta | Dónde |
|---|---|---|
| **DN4a-01** · Disposición horizontal | kcal y BPM en fila a la altura de los controles, abajo a la derecha, con kcal en el rincón. Llevan ancho mínimo (96·s) si la fila entra con los dos mínimos; esto se decide por ventana, no por valor. La banda va al nivel de los controles y es compacta. Hay topes por vecina y por zona del bote, y la fila de arriba se reparte si no entra. La llave va a la derecha de `Pausa` o encima (L640) | §3.1, §3.7 a §3.9 |
| **DN4a-02** · Centro vertical de la zona del bote | El de la cámara de persecución: 0,504 en vertical y 0,652 en horizontal. Las zonas medidas quedan libres en los 5 perfiles, con los topes de §3.7 | §3.6 |
| **DN4a-03** · Fondo provisorio | Dos tonos planos del mediodía del legacy, partidos en el horizonte de la cámara. Tokens `backdrop.sky` y `backdrop.water` en WP-D0 | §2.1, §4.3 |
| **DN4a-04** · Barra de pasos sin píldoras | 40 dp de alto, igual que la barra libre: borde 1, relleno 8, fila de 22, relleno 8 y borde 1. Relleno a los lados de 12 (rutina) y 14 (libre). Las píldoras de la 4b se agregan arriba, con la medida del legacy | §3.3 |
| **DN4a-05** · Entrada y salida del inmersivo | Fundido de 240 (`ease.enter`) al entrar y de 200 (`ease.exit`) al salir; con "reducir movimiento", 120 lineal. La llave cambia de carril con su fundido (90 / 160). La barra de pestañas vuelve en el cuadro 0 de la salida, y el aviso de completado entra ahí | §5.1 |
| **DN4a-06** · Constantes del HUD | Color 150 · flecha 120 con 4 dp en su sentido · empujón 1,04 (90 / 240) como mucho cada 4 s y nunca al empezar un paso · `Próximo`: ancho con ω 12, fundido 200 y latido 0,6 ↔ 1 en 600 + 600 · por terminar: 1,1 con ω 22 y una rampa de color de 150 · rodillo **180** también en el reloj y la cuenta (no 160) · con "reducir movimiento", realce fijo | §5, §5.13 |
| **DN4a-07** · `obj.`, flecha y unidades | `obj.`: 12/600 `text.secondary`, 2 dp debajo del valor y alineado con sus cifras. Flecha: Inter 700 a 0,4 em del valor, en un hueco de 0,48 em, con la tinta centrada en la banda de las mayúsculas. Unidades: `.hud` al 80 % por código, en la línea de base | §3.2, §4.4 |
| **DN4a-08** · Workout en reposo | Aviso, botón, etiqueta, tarjetas con *chevron*, cargando, error, vacío y "en curso", con medidas, colores y estados | §4.1 |
| **DN4a-09** · Pares de contraste del HUD | H1 a H17 y W1 a W4, en el formato de `ContrastTests` | §9 |
| **DN4a-10** · Foco sobre el vidrio y orden de Tab | En el HUD, el anillo es blanco de 2 dp con un halo negro al 50 % pegado por fuera: ≥ 3,977:1 sobre cualquier fondo. Tab recorre `Terminar` → `Pausa` / `Reanudar` → la llave (simulador) | §4.6 |
| **DN4a-11** · Diálogo de terminar | El diálogo de la app de la It. 3, opaco, de 331 en TV. `.rm-dialog--hud` queda como clase sin cambios para la 4b | §4.7 |
| **DN4a-12** · `num.stat` y la derivada | **Sí**: todo `.rm-type-num-*` usa RowMate Numeric SC, también `num.stat`. Entonces la cara que dibuja texto es la derivada 700, que lleva tabla K2, y Barlow 700 sale de K2. K4 queda para la 600 y la 800 | §4.9, DN4a-22 |
| DT4a-01 · Foco al cerrar, solo con teclado | **De acuerdo** con el camino (a): con el mouse, un Enter después de `Cancelar` no tiene que volver a abrir el diálogo de terminar | §4.6 |
| DT4a-03 · Escala por zona con `scale` | **De acuerdo**: también los bordes y los radios pasan a `s` dp, que es escalar el legacy entero. En W1280 el borde de 1 dp mide 1,6 px y se ve como una línea suave al 10 %, sin problema | §3.2 |
| DT4a-04 · VID-01 de 17 s | **De acuerdo**: con 12 s no entran la entrada de `Próximo`, el "por terminar" y el cambio de paso | §8.4 |
| Spec P-12 · Lugar de la llave en el inmersivo | §3.9 | §3.9 |
| Spec §15 · ART_BIBLE §3.3 (`Up next`) | `Próximo:` / `Up next:` (DV-24), en §13 | §13 |

---

## 2. Tokens, clases de texto y colores

### 2.1 Tokens nuevos (WP-D0; `Tokens.cs` manda)

| Token | Valor | Rol | Uso | Por qué |
|---|---|---|---|---|
| `backdrop.sky` **[nuevo]** | `#0B4FA8` | superficie | fondo provisorio del inmersivo, del borde de arriba hasta el horizonte (§4.3) | Es `skyTop` de las 12 h del legacy (LEGACY_VISUAL_AUDIT §7.3), el tono oscuro de su cielo. Blanco encima, 7,783:1; el vidrio encima, `#08162A` |
| `backdrop.water` **[nuevo]** | `#0E4F8F` | superficie | fondo provisorio, del horizonte al borde de abajo | `waterNear` de las 12 h del legacy. El vidrio encima, `#081627` |

Total: **133 → 135**. Se van, o quedan como fondo de carga, cuando llegue la escena de la It. 6a. No duplican la paleta de `SceneTimeOfDay`, que llega en la 6a: son el fondo plano de la 4a.

### 2.2 Clase de texto y color por elemento (sobre 02-tech-design §9.5)

**[cambia]** = distinto del contrato. Todo color sale de `Tokens`. Los tamaños del HUD van en línea, a tamaño base.

| Elemento | Clase | Tamaño | Color |
|---|---|---|---|
| `#workout-appbar` título | `.rm-type-title` | 20/600 | `text.primary` |
| `#workout-banner` texto | `.rm-type-body-s` | 13 | `state.warning` |
| `#workout-free` | `.rm-type-button` | 14/600 | `text.onAccent` (ícono `play_arrow` 18 en el mismo color) |
| `.rm-workout__label` | `.rm-type-body-s` | 13 | `text.tertiary` |
| `#workout-empty`, `#workout-in-progress` | `.rm-type-body` | 15 | `text.tertiary` |
| `#workout-error` texto | `.rm-type-body` | 15 | `text.secondary` (como Rutinas) |
| Rótulo de tarjeta del HUD | `.rm-type-label` | 13 / 12 / 11 | color base de la métrica; pulso `metric.hr.hud` |
| Valor del HUD | `.rm-type-num-hero` / `-l` / `-m` | 64 / 44 / 32 | sin objetivo `.hud`; con objetivo `text.hudPrimary` o `state.offTarget.hud` |
| `.rm-hud-card__arrow` | Inter 700 (`.rm-type-header-s` con el tamaño en línea) | 0,4 em del valor: 25,6 / 17,6 | `state.offTarget.hud` |
| Unidad `W` | `.rm-type-caption` | 13/500 | **`metric.watts.hud` al 80 %** [cambia] (era el color base al 55 %) |
| Unidad `m` (banda) | `.rm-type-caption` | 13/500 | **`metric.distance.hud` al 80 %** [cambia] |
| `#hud-target-*` (`obj.`) | `.rm-type-button-s` | 12/600 | `text.secondary` (blanco al 70 %) |
| `#hud-step-type` | `.rm-type-header-s` | 13/700, tracking 0,04 em | `step.<tipo>.hud` |
| `#hud-step-target` (pastilla) | `.rm-type-button-s` | 11/600 | **`text.primary`** [cambia]; el ícono `trending_up` en `metric.distance` |
| `#hud-up-next` · `Próximo:` | Inter 400 (nota abajo) | 10 | **`text.secondary`** [cambia] (era blanco al 60 %) |
| `#hud-up-next` · tipo siguiente | `.rm-type-header-s` | 10/700 | **`text.primary`** [cambia] (era su color) |
| `#hud-countdown` | `.rm-type-num-s` | 20 (escala 1,1 por terminar) | blanco; por terminar, blanco ↔ `step.<tipo>.hud` |
| `#hud-time`, `#hud-distance` | `.rm-type-num-band` | 34 | `metric.time.hud`, `metric.distance.hud` |
| Íconos de la banda | `.rm-icon` | 16 | `metric.time` y `metric.distance` al 70 % |
| `#hud-free-title` | `.rm-type-title-s` | 16/600 | `text.primary`; `rowing` 20 en `accent.primary` |
| `#hud-finish` | `.rm-type-button` | 16/600 | `state.dangerText` |
| `#hud-pause` | `.rm-type-button` | 16/600 | `text.primary` (ícono `pause` 22) |
| `#hud-resume` | `.rm-type-button` con 700 | 16/700 | `text.onAccent` (ícono `play_arrow` 22) |

Nota: el contrato ponía `Próximo:` en `.rm-type-caption`, que es Inter 500, y el legacy lo dibuja en 400. Va con `.rm-type-body-s` (Inter 400) y el tamaño en línea, o con su `FontAsset` en línea, como el 700 del tipo, a elección del Architect.

### 2.3 Colores con transparencia, por código

USS no aplica transparencia a un `var()`: estos colores los pone el código desde `Tokens`. Las sondas usan los compuestos sobre el fondo provisorio (`probes.py`).

| Elemento | Color | Compuesto (sonda) |
|---|---|---|
| Vidrio de toda pieza del HUD | `glass.fallback` (`#070D16 @86 %`) | sobre el cielo `#08162A` · sobre el agua `#081627` · peor caso, sobre blanco, `#2A2F37` |
| Borde de reposo de tarjetas, banda y controles de vidrio | `#FFFFFF @10 %`, 1·s dp | sobre vidrio y cielo `#202E40` |
| Borde de la barra superior | `border.subtle` (`@8 %`) | sobre vidrio y cielo `#1B293B` |
| Borde con objetivo | color base `@70 %`, 1,5·s dp | SPM `#0285A4` · Split `#B57620` · Watts `#B59C14` (sobre vidrio y cielo) |
| Halo con objetivo (`GlowTexture`) | color base `@20 %`, solo por fuera | en el borde interior del anillo, sobre el cielo: SPM `#0963B2` · Split `#3C5F8C` · Watts `#3C6A88` |
| `obj.` | `text.secondary` | sobre vidrio y cielo `#B5B9BF` · sobre vidrio y agua `#B5B9BE` |
| Unidad `W` · `m` | `.hud @80 %` | `W` sobre vidrio y cielo `#CEB010` · `m` sobre vidrio y agua `#40A7C9` |
| Íconos de la banda | `metric.time @70 %` · `metric.distance @70 %` | `#9FA3A8` · `#3893B4` (sobre vidrio y agua) |
| Divisor de la banda | `#FFFFFF @12 %` | `#263241` |
| Pastilla de objetivo | `step.<tipo> @20 %` | sobre vidrio y cielo: trabajo `#362038` · descanso `#073D42` · calentamiento `#393C36` · enfriamiento `#092D46` |
| Pastilla `Próximo` | fondo `step.<tipo> @20 %` [cambia, era 25 %] y borde `@50 %` | descanso `#073D42` y borde `#077665`; trabajo `#362038` y borde `#7B2F4D` |
| `Próximo:` sobre su pastilla | `text.secondary` | sobre descanso `#B5C5C6` · sobre trabajo `#C3BCC3` |
| Contorno de `Pausa` | `border.strong` | `#434E5B` |
| Capa de estado de `Terminar` | `overlay.scrim` al 12 % (*hover*) y al 24 % (presionado) [cambia] | *hover* sobre vidrio y agua `#071422` |
| Capa de estado de `Pausa` y `Reanudar` | blanco al 4 % y al 8 % | `Pausa` con *hover* `#122030`; `Reanudar` `#10D8A4` (4 %) y `#1AD9A8` (8 %) |
| Halo del foco en el HUD | `overlay.scrim` al 50 % | sobre el agua `#072848` |
| Velo de los diálogos | `overlay.scrim` al 60 % | sobre vidrio y cielo `#030911` · sobre el cielo `#042043` · sobre el agua `#062039` · sobre `bg.app` `#050B11` |
| Aviso sin remo | `state.warningBg` | sobre `bg.app` `#312E24` |
| `Entrenamiento libre` deshabilitado | `accent.primary` al 38 % | `#08556C` |
| Tarjeta de rutina sin remo | la lista al 50 % | fondo de la tarjeta `#142438` |

---

## 3. Geometría del HUD

### 3.1 Zonas

Las zonas y el orden son los del legacy ([CONTRATO], ART_BIBLE §5.2). Las medidas del legacy se multiplican por `s`.

| Zona | Vertical (TV, P360) | Horizontal (TH, L640, W1280) | Fuente |
|---|---|---|---|
| Barra superior | margen seguro superior + 8·s; 12·s a cada lado; alto 40·s (§3.3) | igual | `immersive_workout_screen.dart:89-97`, `:348-354`, `:497-498` |
| SPM | arriba a la izquierda: 14·s del borde, margen superior + 90·s | igual | `:611-613` |
| Split y Watts | arriba a la derecha: 14·s del borde, margen superior + 90·s; en columna, alineadas a la derecha, 8·s entre las dos | igual | `:639-652` |
| kcal y BPM | abajo a la derecha: 14·s del borde y 140·s del borde de abajo (sin el margen seguro); en columna, kcal arriba y BPM abajo (legacy) | **abajo a la derecha, a la altura de los controles** (margen inferior + 16·s), **en fila**: kcal en el rincón, BPM a su izquierda, 8·s entre las dos [nuevo] | `:709-733`; DV-33 |
| Banda | abajo a la izquierda: 14·s del borde y 140·s del borde de abajo | abajo a la izquierda, a margen inferior + 16·s (al nivel de los controles, legacy); **compacta** [nuevo] | `:669-706` |
| Controles | abajo al centro: margen inferior + 16·s; 16·s entre los botones | igual | `:110-116`, `:901-920` |
| Zona del bote | 44 % × 30 %, centrada en x; centro en y 0,504 del alto (§3.6) | ídem; centro en y 0,652 | ART_BIBLE §5.2, §8.1 |
| Llave (simulador) | carril derecho, 8 dp sobre la columna de kcal y BPM (§3.9) | 8 dp a la derecha de `Pausa`, o sobre ella en L640 (§3.9) | D-ART-10 ajustada |

**Por qué kcal y BPM en la fila de abajo.**
- **No entran en la columna.** En horizontal, la columna derecha necesita al menos 535·s dp y no entra en ninguna ventana (00-deuda-diseno §4.1).
- **Debajo de Vatios saltarían.** Con la fila debajo de Watts (la opción por defecto de 02-tech-design §6.4), kcal y BPM quedan colgando a media altura en W1280 (482-584 de 720). Además, saltan 16,9·s dp cada vez que Watts gana o pierde `obj.` al cambiar de paso.
- **Al nivel de los controles es estable.** Su línea de abajo es la de los controles, que no se mueve. La fila de abajo queda simétrica: banda a la izquierda, controles al centro, kcal y BPM a la derecha.
- **Es la misma zona.** Sigue siendo "abajo a la derecha" (ART_BIBLE §5.2, AC-WKT-17-05).

**Por qué kcal en el rincón.** En horizontal kcal no se mueve cuando aparece o se va BPM: la tarjeta que está siempre queda quieta y BPM aparece o se va a su lado. En vertical queda el orden del legacy, kcal arriba y BPM abajo, y kcal sube con un deslizamiento (§5.6).

### 3.2 Tarjetas (base, antes de `s`)

```
#hud-<métrica> .rm-hud-card --large|--medium|--small [--target] [--off]   vidrio, r.glass 14, borde 1 @10 % (con objetivo 1,5 base @70 %)
├── .rm-hud-card__glow                 absoluto, 12 por fuera, solo con objetivo (GlowTexture, base @20 %)
├── .rm-hud-card__label                [♥ 11 + 4 en BPM] rótulo 13 / 12 / 11 /600, tracking 0,08 em
├── 2
├── .rm-hud-card__value                fila, alineada en la línea de base de las cifras
│   ├── .rm-hud-card__arrow            [cambia] hueco de 0,48 em del valor, solo con objetivo (display: flex); "▲"/"▼" adentro,
│   │                                  Inter 700 a 0,4 em, pegado a la izquierda del hueco, visible solo fuera de objetivo
│   ├── FitBox > TabularNumber         num.hero / num.l / num.m (RowMate Numeric SC)
│   └── .rm-hud-card__unit             Watts: 4 + "W" 13/500, .hud @80 %, en la línea de base de las cifras
└── #hud-target-<métrica>              2 + "obj. N" 12/600 text.secondary; margin-left = el hueco: empieza donde empiezan las cifras
```

| Medida | Grande (SPM) | Mediana (Split, Watts) | Chica (kcal, BPM) |
|---|---|---|---|
| Relleno (vertical × horizontal) | 10 × 16 | 10 × 14 | 8 × 12 |
| Ancho mínimo | 140 | 118 | 96 (en horizontal, solo si la fila entra con los dos mínimos: §3.7) |
| Rótulo (caja de línea) | 13/600 (16,1) | 12/600 (14,9) | 11/600 (13,6) |
| Valor (caja; banda de mayúsculas centrada) | 64/800 (64) | 44/700 (44) | 32/700 (32) |
| Hueco de la flecha · tamaño de la flecha | 30,7 · 25,6 | 21,1 · 17,6 | — |
| Alto sin `obj.` · con `obj.` | 102,1 · 119,0 | 80,9 · 97,8 | 63,7 |
| Ancho con los valores de la demo | `24.0`: 155,2; con objetivo, 185,9 | `02:13`: 133,8 (154,9 con objetivo); `150 W`: 113,8 → 118 (134,9 con objetivo) | `89` y `142`: 96 |

**La flecha.**
- **La forma.** ▲ y ▼ de Inter 700 (glifos `triagup` y `triagdn`). Avance 1,045 em del tamaño de la flecha, tinta de 0,934 em de ancho. La tinta de ▲ va de 0 a 0,772 em; la de ▼, de −0,046 a 0,728 em.
- **El alto.** La tinta queda **centrada en la banda de las mayúsculas de las cifras**, que es el centro vertical de la caja del valor. En números, la línea de base de ▲ va 0,196 em del valor sobre la de las cifras, y la de ▼, 0,214 em (SPM: 12,5 y 13,7 dp base).
- **El aire.** Va pegada a la izquierda del hueco. Entre su tinta y la primera cifra quedan 0,084 em más el lado de la cifra: 5,4 dp a 64.

**Por qué el hueco existe toda la duración del paso con objetivo.**
- Si apareciera solo fuera de objetivo, la tarjeta se ensancharía 30,7·s dp, o el número se correría, cada vez que se cruza el umbral: el número "bailaría".
- Con el hueco fijo, al cruzar solo cambian el color y la flecha.
- El hueco cambia de ancho únicamente al cambiar de paso, cuando el objetivo aparece o se va, y lo hace con la coreografía de §5.5.
- Con el hueco vacío (cumple), el número queda corrido: es la columna del valor y de su objetivo, con el rótulo como título de la tarjeta.

```
SPM            SPM
   24.0     ▲  23.5
   obj. 22     obj. 22
```

**Bordes y escala.** Con `scale` por zona (DT4a-03), el borde de 1 dp mide `s` dp: 1,054 en TV (2,77 px a la densidad de las capturas) y 1,6 px en W1280.

### 3.3 Barras superiores (DN4a-04)

| Parte | Con rutina (`.rm-hud-topbar --steps`) | Libre (`.rm-hud-topbar --free`) |
|---|---|---|
| Contenedor | margen 12 a cada lado; vidrio; borde 1 `border.subtle`; radio 14; **alto 40** (1 + 8 + 22 + 8 + 1) | igual |
| Relleno a los lados | 12 | 14 (AC-WKT-16-01) |
| Fila (`min-height` 22, `align-items: center`) | tipo · 6 · pastilla de objetivo (`flex-shrink` 1, "…") · espacio · `Próximo` · 8 · cuenta (`flex-shrink` 0) | `rowing` 20 · 8 · `Entrenamiento Libre` 16/600 |
| Pastilla de objetivo | radio 6, relleno 6 × 2, alto 17,6; `trending_up` a 1,17 em del texto (12,9 dp, como en el editor) | — |
| `Próximo` | borde 1, radio 8, relleno 7 × 3, alto 20,4 | — |
| Cuenta | caja de 20 (por terminar, escala 1,1 con el origen en su borde derecho: crece 1 dp arriba y abajo y hacia la izquierda, dentro de los 8 dp de aire) | — |

La fila mide 22 para que la barra no cambie de alto cuando aparece `Próximo` (20,4) ni cuando la cuenta crece. Las dos barras miden lo mismo, así la tarjeta de SPM queda a la misma distancia en libre y con rutina. En la 4b, la fila de píldoras se suma arriba con las medidas del legacy (relleno 10 / 8 / 10 / 4, píldora de 8) y la de información queda igual.

**Lo que entra en la fila** (ancho útil base: 340 en TV y P360, 817 en TH, 643 en L640, 750 en W1280):

| Toma | Contenido (base) | Resultado en TV |
|---|---|---|
| CAP-18 | `Calentamiento` 99,0 + `0:24` 37,7 | entra |
| CAP-19 | `Trabajo` 51,1 + 6 + pastilla 185,5 (`>160W · 22 spm · ` 95,0 + ícono 12,9 + ` SPM +2/rep` 65,6 + 12) + `0:40` 37,7 = 280,3 | entra, con 59,6 de aire |
| CAP-21 | lo de CAP-19 + `Próximo: Descanso` 108,6 + 8 | sobran 57,1: la pastilla de objetivo se recorta a 128,4 y se lee `>160W · 22 spm · ↗…` |
| CAP-20 | `Trabajo` + pastilla larga 293,5 + `500m` 41,3 | la pastilla se recorta a 241,5: `150-180W · 02:05/500m · ↗ W +10/rep, S…` |
| CAP-27c (inglés) | `Work` 35,4 + pastilla 185,5 + `Up next: Rest` 80,2 + `0:12` | la pastilla pierde 12,9 |
| TH, W1280 | todo | entra entero en las dos |

**La cinta.** Cruza en diagonal el rincón de arriba a la izquierda: es la franja de los puntos con x + y entre 39,5 y 56,5 dp (AC-BLE-16-01).
- **En TV**, tapa la esquina de arriba a la izquierda de la primera letra del tipo y del ícono `rowing`, como tapa la del título en las otras pantallas (CAP-04 de la It. 3: `Rutinas`).
- **En L640** tapa la parte de arriba de la primera letra y media.
- **Nunca** tapa la cuenta.
- **En la 4b**, con la fila de píldoras arriba, el tipo baja y sale de la cinta, como en `docs/image-03.png`.

### 3.4 Banda

| Parte | Vertical | Horizontal (compacta) [nuevo] |
|---|---|---|
| Cápsula | `r.full`, vidrio, borde 1 @10 %, relleno vertical 10 | igual |
| Relleno a los lados | 20 | 14 |
| Divisor | 1 × 32, blanco @12 %, 16 a cada lado | 1 × 32, 10 a cada lado |
| Columna del tiempo | `timer` 16 (`metric.time` @70 %) centrado sobre el valor · 2 · `num.band` 34 `metric.time.hud` | igual |
| Columna de la distancia | `straighten` 16 (`metric.distance` @70 %) · 2 · `num.band` `metric.distance.hud` + 3 + `m` 13/500 `.hud` @80 % en la línea de base | igual |
| Alto · ancho natural con `00:42 │ 1048 m` | 72 · 240,1 | 72 · 216,1 |

- **El achique.** Las dos cifras comparten **una sola escala** (FitBox común), así la banda se lee pareja. El relleno, el divisor y los íconos no se achican.
- **El borde redondo.** Con relleno 14, la tinta de las cifras (la banda de las mayúsculas, entre 33 y 57 dp del borde de arriba) queda dentro del semicírculo de radio 36 del extremo: ahí el borde está, como mucho, a 6,7 dp del lado.
- **Por qué compacta en horizontal.** Ahí la banda comparte la fila con los controles. Compacta, entra entera en TH y en W1280 con valores normales. En vertical sobra lugar y queda la del legacy.

### 3.5 Controles

- **La fila.** Centrada; `Terminar` (mínimo 128 × 56), 16 de separación y `Pausa` / `Reanudar` (mínimo 148 × 56): 292 base. Radio 12; relleno de 24 a cada lado de la fila, que solo cuenta en ventanas de menos de 340·s.
- **El contenido.** `Terminar`: texto 69,2 + 32 de relleno. `Pausa`: ícono 22 + 8 + texto 47,1 + 32. `Reanudar`: 22 + 8 + 75,1 + 32 = 137,1. Los tres entran en su mínimo, también en inglés.
- **El cambio.** `Pausa` y `Reanudar` miden lo mismo, así el cambio no mueve nada (AC-WKT-20-02).

### 3.6 Zona del bote (DN4a-02)

El centro es el punto donde se proyecta el remero con la cámara de persecución del legacy (ART_BIBLE §5.2 y §8.1): cámara a 2,2 m, remero a 9,5 m, focal = lado corto. Con el horizonte en `h₀` (0,40 del alto en vertical, 0,42 en horizontal):

`centro_y = h₀ + (lado corto / alto) × 2,2 / 9,5`

| Perfil | `h₀` | Centro | Zona (x · y) | Lo más cercano |
|---|---|---|---|---|
| TV | 0,40 | 0,5041 | 115,1-295,9 · 323,7-597,9 | Watts con objetivo termina en 291,5 (32,2 de aire); kcal con BPM empieza en 623,9 (26,0) |
| P360 | 0,40 | 0,5042 | 100,8-259,2 · 283,4-523,4 | Watts con objetivo 255,3 (28,1); kcal 545,9 (22,5) |
| TH | 0,42 | 0,6516 | 255,9-658,1 · 206,1-329,4 | la banda termina en x 242,5 (13,4); los controles empiezan en y 335,1 (5,7) |
| L640 | 0,42 | 0,6516 | 179,2-460,8 · 180,6-288,6 | SPM con objetivo, con su tope, termina en x 179,2 (0); la banda en 177,8; los controles empiezan en y 293,5 |
| W1280 | 0,42 | 0,6516 | 358,4-921,6 · 361,1-577,1 | la banda y los controles empiezan en y 579,2 y 604,8 (2,1 y 27,7) |

- **Contra el legacy.** En `docs/image-03.png` (1600 × 1080) el centro da 453,6 + 250,1 = 703,7, y el carro del remero está en y ≈ 700.
- **Por qué no 0,5.** En horizontal, el 0,5 de 02-tech-design dejaría la zona sobre el horizonte y no sobre el bote. La 6a tendría que moverla, y un HUD medido contra 0,5 podría quedar encima del remero.
- **La llave.** Puede quedar sobre la zona solo en el caso (b) de L640 (§3.9). La zona protege al bote de las piezas del HUD, y la llave es del build con simulador.
- **El contorno** de depuración (`#hud-boat-zone`) se dibuja en `dev.simulator`, 2 dp, sin relleno.

### 3.7 Topes y FitBox

Los anchos máximos dependen de la ventana y del ancho natural que mide cada `FitBox`. Lo que no entra se achica en sus cifras, nunca en el rótulo, el relleno ni el borde. Si un dato nuevo cambia la escala, la sigue con `spring.smooth`, como en la It. 2. El primer cálculo, una rotación o un cambio de ventana se aplican en el acto.

| # | Regla |
|---|---|
| T1 · Fila de arriba | SPM y la columna derecha comparten la fila entre los dos márgenes de 14·s, con 8·s entre las dos. Si sus anchos naturales no entran, se reparten el ancho en proporción. Pasa solo con los extremos de FTMS: ×0,959 en TV y P360 |
| T2 · Zona del bote arriba | Si SPM o la columna derecha se cruzan en alto con la zona del bote (en horizontal), SPM no pasa del borde izquierdo de la zona y la columna no empieza antes de su borde derecho |
| T3 · Banda | En vertical, hasta 8·s antes de la columna de kcal y BPM. En horizontal, hasta 8·s antes de los controles y, si se cruza en alto con la zona del bote, hasta su borde izquierdo |
| T4 · Fila de kcal y BPM (horizontal) | Empieza en el mayor de dos valores: 8·s después de los controles, u 8 dp después de la llave si está a su izquierda. Termina a 14·s del borde. Usa los anchos mínimos (96·s cada una) si los dos entran en esa franja; esto se decide **por ventana**, así la tarjeta no cambia de modo con los valores. Si no, ancho de contenido; si tampoco entra, FitBox común |
| T5 · Columna de kcal y BPM (vertical) | Ancho = el mayor de las dos (mínimo 96·s) |

**Escala de las cifras que se achican** (las demás, 1):

| Perfil | Valores normales | Con objetivo | Largos de CAP-16 | Extremos de FTMS (AC-WKT-17-10) |
|---|---|---|---|---|
| TV · P360 | — | — | banda 0,800 | SPM 0,951 · Split 0,952 · Watts 0,976 · banda 0,651 |
| TH | — | — | banda 0,778 | banda 0,675 |
| L640 | banda 0,776 | SPM 0,962 (T2) | banda 0,561 | kcal y BPM 0,915 · banda 0,486 |
| W1280 | — | — | banda 0,792 | banda 0,687 |

En L640 la banda no entra a la izquierda de los controles ni con la versión compacta. A 0,776, las cifras miden 24,3 dp y su altura de mayúscula, 17,0 dp, que es 6,2′ a 1,5 m (§6). Con los largos de CAP-16, el caso más extremo de la ventana más chica, bajan a 17,6 dp. Esa toma comprueba que nada se encime y que nada se corte. El legacy, en esa ventana, encima la banda con los controles.

### 3.8 Rectángulos verificados por perfil

Con el simulador; rutina con objetivos en SPM y Watts, con BPM y con los valores de la demo. Entre paréntesis, lo que cambia en libre (sin objetivo). En libre sin pulsómetro, kcal ocupa el lugar de BPM en vertical y queda en el rincón en horizontal. Fuente: `hud_layout.py`.

| Pieza | TV (s 1,054) | P360 (0,923) | TH (1,054) | L640 (0,923) | W1280 (1,6) |
|---|---|---|---|---|---|
| Barra superior | 12,6-398,4 · 8,4-50,6 | 11,1-348,9 · 7,4-44,3 | 12,6-901,4 · 8,4-50,6 | 11,1-628,9 · 7,4-44,3 | 19,2-1260,8 · 12,8-76,8 |
| SPM | 14,8-210,7 · 94,8-220,2 (178,3 · 202,4) | 12,9-184,5 · 83,1-192,9 (156,2 · 177,3) | 14,8-210,7 · 94,8-220,2 | 12,9-179,2 · 83,1-192,9 (0,962) | 22,4-319,9 · 144,0-334,3 (270,7 · 307,4) |
| Split | 255,3-396,2 · 94,8-180,1 | 223,6-347,1 · 83,1-157,7 | 758,3-899,2 · 94,8-180,1 | 503,6-627,1 · 83,1-157,7 | 1043,6-1257,6 · 144,0-273,4 |
| Watts | 254,1-396,2 · 188,5-291,5 (271,9 · 273,7) | 222,6-347,1 · 165,1-255,3 | 757,1-899,2 · 188,5-291,5 | 502,6-627,1 · 165,1-255,3 | 1041,8-1257,6 · 286,2-442,5 |
| kcal | 295,1-396,2 · 623,9-691,0 | 258,5-347,1 · 545,9-604,7 | 798,1-899,2 · 327,1-394,1 | 574,1-627,1 · 286,5-345,2 | 1104,0-1257,6 · 592,6-694,4 |
| BPM | 295,1-396,2 · 699,4-766,5 | 258,5-347,1 · 612,0-670,8 | 688,5-789,6 · 327,1-394,1 | 498,4-566,8 · 286,5-345,2 | 937,6-1091,2 · 592,6-694,4 |
| Banda | 14,8-267,8 · 690,6-766,5 | 12,9-234,5 · 604,3-670,8 | 14,8-242,5 · 318,3-394,1 | 12,9-177,8 · 278,8-345,2 (0,776) | 22,4-368,1 · 579,2-694,4 |
| Controles | 51,6-359,4 · 838,1-897,1 | 45,2-314,8 · 733,5-785,2 | 303,1-610,9 · 335,1-394,1 | 185,2-454,8 · 293,5-345,2 | 406,4-873,6 · 604,8-694,4 |
| Llave | 355,0-395,0 · 575,9-615,9 | 304,0-344,0 · 497,9-537,9 | 618,9-658,9 · 344,6-384,6 | 414,8-454,8 · 245,5-285,5 | 881,6-921,6 · 629,6-669,6 |
| Zona del bote | 115,1-295,9 · 323,7-597,9 | 100,8-259,2 · 283,4-523,4 | 255,9-658,1 · 206,1-329,4 | 179,2-460,8 · 180,6-288,6 | 358,4-921,6 · 361,1-577,1 |

CAP-17 lo comprueba: en W1280 la caja del valor de SPM mide 64 × 1,6 = 102,4.

### 3.9 La llave y el panel en el inmersivo (D-ART-10 ajustada; spec P-12)

En las pestañas, las pantallas apiladas, los diálogos y los menús, la regla de 00-deuda-diseno §1.3 no cambia: carril izquierdo. Para el inmersivo, esta sección reemplaza §1.5 de ese documento, el punto "En el entrenamiento" de su §1.7 y el "L640: ningún lugar" del Anexo B. El PO la integró en el gate (AC-BLE-16-02 y -03, DV-39).

| Orientación | Dónde | Por qué |
|---|---|---|
| Vertical | Carril derecho (`right` 16 + margen seguro). Su borde de abajo va 8 dp sobre el borde de arriba que tendría la columna de kcal y BPM **con BPM**, aunque BPM no esté | Así no se mueve cuando el pulso aparece o se va. Deja libre el hueco entre la banda y los controles: ahí va el aviso (CAP-29, §4.8), que antes tenía que subir sobre la llave y tapaba el reloj. Queda lejos de `Pausa`, sin riesgo de un toque de más. No llega a la zona del bote: el carril empieza en el ancho − 56 y la zona termina en 0,72 del ancho |
| Horizontal (a) | 8 dp a la derecha de `Pausa`, centrada en el alto de los controles. Se usa si a su derecha entra la fila de kcal y BPM con sus mínimos: (2 × 96 + 8)·s ≤ ancho − 14·s − (borde de `Pausa` + 56) | Pasa en TH (232,4 ≥ 210,8) y W1280 (328,0 ≥ 320,0). Es el único lugar libre de la fila de abajo |
| Horizontal (b) | 8 dp sobre `Pausa`, alineada con su borde derecho | En L640 (116,3 < 184,6). Queda dentro de la zona del bote, que en la 4a es fondo liso (§3.6) |

La decisión entre (a) y (b) es por ventana: la llave no cambia de lugar con los valores ni con BPM. Dentro del inmersivo solo se mueve si cambia la ventana o la orientación, y lo hace en el acto, junto con el HUD (§5.9).

**El panel** (DD4a-03).
- **Desde dónde.** Se abre donde está la llave y crece desde la **esquina de abajo a la derecha** de la llave: hacia arriba y hacia la izquierda. La transición es la de la It. 2 (opacidad 160, escala 0,92 → 1 en 240); solo cambia el origen.
- **Alto máximo** (AC-BLE-16-03). Va del borde de abajo de la llave hasta 16 dp debajo del margen seguro superior, con un mínimo de 120.
  - **En vertical**, además, termina 8 dp debajo del lugar de Watts **con su objetivo**, aunque el paso no lo tenga: así no cambia de alto al cambiar de paso, y Split y Watts quedan a la vista. Es el tope que 02-tech-design §6.10 dejaba apagado: el PO lo aceptó en el gate y queda siempre encendido.
  - Lo que no entra se desplaza adentro del panel, y el botón de cerrar queda siempre a la vista.

  | Perfil | Borde de abajo de la llave | Tope de arriba | Alto máximo |
  |---|---|---|---|
  | TV | 615,9 | Watts con objetivo (291,5) + 8 | 316,4 |
  | P360 | 537,9 | Watts con objetivo (255,3) + 8 | 274,6 |
  | TH | 384,6 | 16 | 368,6 |
  | L640 (b) | 285,5 | 16 | 269,5 |
  | W1280 | 669,6 | 16 | 653,6 |

- **En horizontal** nunca se cruza con Split ni con Watts, que están más a la derecha: se ven mientras se los corrige, que es lo que pide la demo (b). El panel va de x 378,9 a 658,9 en TH (Split empieza en 758,3), de 641,6 a 921,6 en W1280 (Split en 1043,6, BPM en 937,6) y de 174,8 a 454,8 en L640 (Split en 503,6).
  - **En (a), abierto, tapa la parte de arriba de `Pausa`**: 49,5 de sus 59 dp de alto en TH y 64,8 de 89,6 en W1280. En TH tapa además el 44 % derecho de `Terminar`. Es la herramienta del simulador y se cierra con su botón; la demo lo cierra antes de pausar (DEMO-04).
  - En L640 (b) termina 8 dp sobre los controles, sin tocarlos.
  - Abierto, en horizontal tapa además lo que quede en su rectángulo: el tramo del medio de la barra de arriba, sin llegar a `Próximo` ni a la cuenta, y en L640 4,4 dp del borde derecho de SPM y una esquina de 3 × 6,7 dp de la banda. AC-BLE-16-03 solo pide a la vista Split y Watts.
- **En vertical**, con el tope, el panel no tapa ninguna pieza del HUD. En TV va de x 115,0 a 395,0 y de y 299,5 a 615,9: sobre la zona del bote y el carril vacío entre Watts y kcal, con 8 dp de aire bajo Watts y otros 8 sobre kcal. En P360, igual (x 64,0-344,0, y 263,3-537,9).
- **Si la llave cambia de lugar con el panel abierto** (empieza o termina el entrenamiento, cambia la ventana), el panel sigue abierto y pasa al lugar nuevo (AC-BLE-16-02), con el movimiento de §5.9.

---

## 4. Componentes

### 4.1 Workout en reposo (DN4a-08)

```
#screen-workout .rm-screen .rm-workout
├── #workout-appbar .rm-appbar                  solo en reposo: 56; workoutTitle (title 20/600) a 16; sin acciones. La cinta tapa la esquina de la "E" (paridad)
├── #workout-rest > #workout-scroll > .rm-workout__content     relleno 16; abajo, RegisterClearance (R5 de D-ART-10)
│   ├── #workout-banner                         sin remo (§4.1.1)
│   ├── #workout-free                           primario, ancho completo, min-height 56, r.l 12: play_arrow 18 + 8 + workoutFree
│   ├── 24
│   ├── #workout-routines-label                 "␣␣O elegí una rutina:" body-s 13 text.tertiary; 8 abajo; solo con rutinas
│   ├── #workout-loading | #workout-error | #workout-empty   en lo que queda debajo del botón (§4.1.2)
│   └── #workout-routines                       tarjetas de Rutinas con chevron, 10 entre una y otra
└── #workout-in-progress                        fuera de reposo, sola: workoutInProgress body 15 text.tertiary, centrado en toda la pestaña, sin barra superior (§4.1.5)
```

**4.1.1 Aviso sin remo** (AC-BLE-10-03; contrato de §9.5):
- Fondo `state.warningBg` (`#312E24` sobre `bg.app`), radio 12, relleno 12 y 16 de margen abajo.
- Ícono `warning` 18 en `state.warning`, **alineado con la primera línea** (centro en la mitad de la caja de línea de 13), 8 y el texto 13 `state.warning`, que ajusta. Con dos líneas, el ícono queda arriba y no en el medio.
- Contraste: 6,310:1 el texto y el ícono (W1).
- En TV `Conectá el rower para iniciar un entrenamiento` (287,5) entra en una línea (329 libres) y pasa a dos con letra 1,3×.
- No se toca (spec P-07).

**4.1.2 Estados** (AC-WKT-12-06). El contenido toma de `min-height` el alto del visor (02-tech-design §6.1), y el estado crece (`flex-grow` 1) y se centra en lo que queda debajo del botón: el mismo centro para los tres.

| Estado | Spec |
|---|---|
| Cargando | `Spinner` de 32, trazo 3, `accent.primary`. Aparece si la carga sigue a los 120 ms, con un fundido de 160; una vuelta cada 900. Nunca junto con el vacío |
| Error | `error` 48 `state.dangerText` → 12 → `routinesLoadError` (body `text.secondary`, ancho máximo 280) → 16 → `Reintentar` tonal con `refresh`. Es la forma del error de Rutinas (It. 3 §4.1). `Entrenamiento libre` sigue habilitado |
| Vacío | `workoutNoRoutines` (body 15 `text.tertiary`, 5,829:1), ancho máximo 320, centrado, sin ícono (paridad: el legacy muestra solo el texto). En TV entra en una línea (277,9) y pasa a dos con letra 1,3× |
| Con rutinas | la etiqueta y la lista |
| En curso | solo `workoutInProgress`, sin la barra superior, centrado entre el borde de arriba y la barra de pestañas (§4.1.5). Se ve únicamente mientras el inmersivo entra |

**4.1.3 Tarjeta de rutina en Workout** (AC-WKT-12-04, DV-21). Es la de la It. 3 §4.2, con `RoutineCardTrailing.Chevron` en lugar del `⋮`:
- **El *chevron*.** Una caja `.rm-routine-card__chevron` **[nuevo]** de 48 × 48 en la misma columna de 28 dp del borde derecho que el `⋮` (así la columna de texto mide igual que en Rutinas y los textos parten en el mismo lugar). Lleva `chevron_right` 18 en `text.tertiary` (5,140:1; 4,785 con el *hover*), **centrado en el alto de la tarjeta** (`align-self: center`), como el `ListTile` del legacy y la fila de dispositivo de la It. 2. No recibe toques: la tarjeta entera es la acción.
- **Estados.** *Hover* 4 % y presionado 8 %, sin escala; anillo de foco de radio 20 y halo 24, como en Rutinas.
- **Sin remo.** La lista al 50 % por código (el fondo de la tarjeta compuesto, `#142438`), sin *hover* ni toques.
- **Medidas en TV, sin aviso.** Botón de 72 a 128, etiqueta de 152 a 168,1 y tarjetas desde 176,1. `Escalera SPM` mide 85,9, `Pirámide 500` (con descripción) 102,8, y las otras dos, 85,9 cada una. La llave del simulador (x 16-56, y 794-834) queda debajo de la última tarjeta.

**4.1.4 `Entrenamiento libre`**:
- Primario relleno `accent.primary`, con texto e ícono en `text.onAccent` (7,641:1). Presionado: `accent.primaryPressed`. Anillo de 16 y halo 20.
- Deshabilitado al 0,38, en el acto, sin *hover* ni toques (la regla de la It. 2): `#08556C` sobre `bg.app`.
- Es el único control grande de la pantalla: a lo ancho incluso en W1280 (1248 × 56), como en `docs/image-02.png`.

**4.1.5 "En curso"** (AC-WKT-01-09; QA-G4a-P09, QA-G4aT-17). Fuera de reposo (en marcha, en pausa o terminada), la pestaña es la `_ActiveView` del legacy: un `Scaffold` **sin** `AppBar`, con el texto en el centro (`workout_screen.dart:180-201`).

- **Qué se ve.** Solo `workoutInProgress`, en `.rm-type-body` 15 y `text.tertiary` sobre `bg.app` (5,829:1, W2; el legacy usa `white38`, DV-36).
  - `Entrenamiento en curso...` mide 181,4 dp y `Workout in progress...`, 156,2.
  - `#workout-appbar` y `#workout-rest` pasan a `display: none` en el mismo cuadro, y vuelven a `flex` juntos al volver a reposo.
- **Dónde.** `#workout-in-progress` ocupa toda la pestaña, del borde de arriba de la ventana al de arriba de la barra de pestañas.
  - Abajo lleva de `padding-bottom` el alto real de la barra (`ShellLayout.RegisterBottomPadding(…, 0)`). Ese alto se conserva mientras la barra está oculta, así que el texto no salta cuando la barra se va o vuelve.
  - Arriba no lleva nada: el legacy, sin `AppBar`, tampoco consume el margen seguro superior.
  - El texto va centrado en x y en y (`-unity-text-align: middle-center`), con 16 de relleno a los lados, y ajusta sin "…". Nunca llega a partirse: con letra 1,3× mide 235,8, y P360 tiene 328 libres.
  - Centro del texto, con márgenes seguros en 0: TV 425,0 · P360 368,0 · TH 173,5 · L640 148,0 · W1280 328,0.
- **Cuándo se ve.** Solo mientras el inmersivo **entra** (240 ms, o 120 con "reducir movimiento"), debajo de la página que funde.
  - Al salir, la sesión ya volvió a reposo y debajo está la vista de reposo, con su barra (AC-WKT-13-04).
  - Es lo que hace el legacy: cierra la página después de `reset()` (`immersive_workout_screen.dart:57-65`, `:141-147`), y debajo queda la `_IdleView` con su `AppBar`.
- **Por qué sin la barra, además de la paridad.** Con la barra, durante la entrada se leían dos títulos encimados.
  - `Entrenamiento` (x 16-156,8, centro en y 28) quedaba debajo de `Entrenamiento Libre` de la barra del HUD (x 58,0-221,8, centro en y 29,5), o de `Calentamiento` (x 26,4-130,7), en TV.
  - A los 33 ms la pestaña todavía pesa 0,638 en la mezcla, y a los 67 ms, 0,378.
  - Sin la barra, debajo de la barra del HUD hay `bg.app` liso.
  - El texto centrado cae, en los cinco perfiles, donde el HUD no tiene ninguna pieza: en vertical, dentro de la zona del bote; en horizontal, entre SPM y Split, encima de la zona. No se cruza con nada mientras funde.
- **La cinta y la llave.** La cinta queda sobre `bg.app` liso. La llave sale en el cuadro 0 de la entrada (§5.9).
- **Prueba** (DN4a-19). `WSC.InProgressTextOutsideIdle` mira además que `#workout-appbar` esté en `display: none` fuera de reposo y vuelva en reposo, y que el centro del texto quede a mitad de camino entre el borde de arriba y la barra de pestañas (± 1 dp).

### 4.2 Diálogo de iniciar (AC-WKT-12-07)

Es el diálogo de la app de la It. 3 (§4.9 y §4.10), sin cambios:
- `surface.overlay` opaco, `r.card` 16, borde 1 `border.default`, relleno 24 (16 con la ventana de menos de 480 de alto).
- Ancho `min(400; ventana − 80)`: 331 en TV.
- Velo al 60 %.

| Parte | Spec |
|---|---|
| Título | el nombre de la rutina tal cual: `title` 20/600 `text.primary`, ajusta, sin texto enriquecido ni secuencias de escape. `Escalera SPM` mide 132,1 |
| Contenido | `body` 15 `text.secondary` (6,984:1), con `white-space: pre-wrap`: `1 serie · 1 descanso`, una línea en blanco y `¿Iniciar el entrenamiento?` (181,0). Es un solo texto del catálogo y no se parte en estilos |
| Acciones | `Cancelar` de texto (`accent.primary`) e `Iniciar` primario relleno, abajo a la derecha, 8 entre los dos |

### 4.3 El inmersivo y el fondo provisorio (DN4a-03)

- **El fondo (`#hud-backdrop`) [contrato].** El elemento lleva `backdrop.water`, y **un hijo** absoluto `.rm-hud-backdrop__sky` **[nuevo]** con `backdrop.sky`, del borde de arriba hasta el horizonte: **0,40 del alto en vertical y 0,42 en horizontal**, la línea de la cámara (ART_BIBLE §8.1).
  - Quieto, sin animación ni datos, sin dibujos.
  - El borde entre los dos es recto: es un horizonte, no un dibujo de agua, bote ni orilla (AC-SCN-01-01).
  - Con la ventana o la orientación, el horizonte se recalcula en el acto.
  - Los dos tonos están a 0,008 de luminancia (0,0849 y 0,0767): se lee "parejo", con un cielo apenas más vivo que el agua.
- **La variante de un solo elemento** (§7), que el Architect toma en 02-tech-design para quedar dentro del presupuesto (`BackdropTexture`). `#hud-backdrop` lleva de fondo una textura de 1 × 100 generada por código, sin hijo. Para que se vea igual que con dos elementos:
  - **Filtro `Point`**, no bilineal. Con 100 texeles, el corte cae justo en el borde de un texel (40 o 42), y el horizonte queda como una línea neta. Con bilineal sería un degradé de un texel, 9,1 dp en TV.
  - **sRGB** (`linear: false`), sin *mipmaps* y en `Clamp`, como `VerticalFadeTexture`. Así los dos tonos salen con el hex exacto: las sondas de CAP-12 lo miden con ±1 por canal.
  - **Los colores, de `Tokens`** (`backdrop.sky` y `backdrop.water`), nunca literales (`ColorSourceTests`).
  - **Con la orientación**, el corte pasa de 40 a 42 en el acto: se rehace la textura (100 píxeles) o se tienen las dos.
- **Por qué no `bg.app`.** El vidrio sobre `bg.app` da `#080F19`, casi el mismo color que el fondo: las tarjetas se verían como recuadros sin cuerpo, y la demo no mostraría el HUD de vidrio. Sobre estos dos tonos, el vidrio se ve como un panel oscuro sobre el agua y el cielo, como en `docs/image-03.png`. El contraste está calculado para el peor fondo (una nube blanca), así que la 6a no cambia nada.
- **Sin *scrims*.** Los de ART_BIBLE §3.2 llegan con la escena.
- **Capas, de atrás hacia adelante** (AC-WKT-13-02): fondo, zona del bote (solo con el contorno de depuración), barra superior, zonas del HUD, controles; encima, la capa del simulador.

### 4.4 Tarjetas del HUD y semáforo (DN4a-07)

| Estado | Valor | Rótulo | Borde y halo | Flecha | `obj.` |
|---|---|---|---|---|---|
| Sin objetivo (libre, calentamiento, descansos, enfriamiento; kcal y BPM siempre) | `metric.*.hud` | color base (pulso `metric.hr.hud`) | 1 `#FFFFFF @10 %`, sin halo | sin hueco | — |
| Con objetivo, cumple | `text.hudPrimary` (blanco) | color base | 1,5 base `@70 %` + halo base `@20 %` | hueco vacío | `obj. N` |
| Con objetivo, fuera | `state.offTarget.hud` `#FF625B` | color base | igual | ▲ (SPM y Watts: hay que subir) o ▼ (Split: hay que bajar) | `obj. N` |
| En pausa | igual que en marcha: los valores siguen y **nada baja de opacidad** (ART_BIBLE §3.2) | igual | igual | igual | igual |

- **`obj.`** usa el texto de `hudTarget` (`obj. 22`, `obj. 160`, `obj. 02:05`; en inglés, `target 22`). Entra en todas las tarjetas: `target 02:05` mide 72,0 y la columna de Split con objetivo, 126,9. Va en una línea, sin "…": nunca es más ancho que la tarjeta.
- **La tarjeta BPM** aparece y se va con el pulso (§5.6). El corazón late con `HeartbeatAnimator` (It. 2) a la frecuencia real.
- **Los valores** cambian con `TabularNumber.SetValue` y su rodillo de 180 (§5.2). Los marcadores (`--:--`, `0.0`) van en el mismo color y tamaño.

### 4.5 Fila del paso

- **El tipo** (13/700, tracking 0,04 em, `step.<tipo>.hud`) va solo y a la izquierda: es el título de la fila.
- **La pastilla de objetivo** lleva **texto blanco** sobre el tinte del tipo al 20 % (10,962 / 9,013 / 8,134 / 10,769:1 en el peor fondo). El ícono `trending_up` va en `metric.distance`, como en el editor (DV-10, DV-35). El texto en el color del tipo del legacy da 3,719:1 (trabajo) y 3,688 (enfriamiento) sobre su tinte.
- **`Próximo`** lleva un fondo del color base del paso siguiente al **20 %** (igual que la pastilla de objetivo), un borde al 50 %, `Próximo:` en `text.secondary` 10/400 y el tipo siguiente en `text.primary` 10/700. Con el 25 % y el 60 % del legacy, `Próximo:` da 3,758 y 4,061:1 sobre calentamiento y descanso, y el tipo en su color, 3,469 a 4,948.
  - **El latido** va en un hijo `.rm-hud-up-next__bg` **[nuevo]** con el fondo y el borde, cuya opacidad va de 0,6 a 1 (§5.4). El texto queda siempre al 100 %. Con el latido del legacy, en toda la pastilla, el texto bajaba a ≈ 3,0:1 (`Próximo:`) y ≈ 4,5 (el tipo) en el punto más tenue. Es la **DV-38**, que el PO aceptó en el gate: AC-WKT-14-03 y -04 ya lo dicen.
  - **Sin duración** del paso siguiente (paridad).
- **La cuenta** (`num.s` 20, blanco) a 8 de `Próximo`.
  - **Por terminar:** escala 1,1 y color blanco ↔ `step.<tipo>.hud` con el reloj común. Con el color del paso es texto grande, ≥ 4,577:1.
  - **Por distancia:** `500m`, sin por terminar ni `Próximo` (Q-51).
  - **Sin meta:** `?` en blanco (DV-25).
  - **Con la rutina terminada:** `0:00`, sin `Próximo`, de vuelta a escala 1 y a blanco.
- **En pausa** la fila queda quieta y los pulsos siguen (paridad, AC-WKT-14-06).

### 4.6 Controles, pausa, foco y Tab (DN4a-10)

| Estado | `Terminar` | `Pausa` | `Reanudar` |
|---|---|---|---|
| Reposo | vidrio, contorno 1,5 `state.dangerText`, texto 16/600 `state.dangerText` (5,077:1) | vidrio, contorno 1,5 `border.strong`, `pause` 22 + 8 + texto 16/600 blanco (13,493) | relleno `state.success`, `play_arrow` 22 + 8 + texto 16/700 `text.onAccent` (9,985) |
| *Hover* (escritorio) | capa **oscura**: `overlay.scrim` al 12 % (5,483). La capa blanca del 4 % daba 4,494 | capa blanca al 4 % (11,944) | capa blanca al 4 % (10,172) |
| Presionado (`:hover:active`) | `overlay.scrim` al 24 % (5,892) y escala 0,97 | blanco al 8 % y escala 0,97 | blanco al 8 % (10,367) y escala 0,97 |
| Foco (teclado) | anillo del HUD (abajo) | ídem | ídem |
| Después de confirmar `Terminar` | los dos al 0,38, en el acto (`ControlsEnabled = false`); con un guardado de más de 300 ms, `Terminar` muestra el *spinner* de 16 en `state.dangerText` a la izquierda de su texto (DN4a-20, P2) | igual | — |

- **El anillo de foco del HUD.**
  - Blanco (`text.primary`), de 2 dp, a −4 del borde de afuera del control (radio 12 + 4 = 16).
  - Con un **halo `overlay.scrim` al 50 %, de 4 dp, pegado por fuera del anillo** (radio 20), sin los 2 dp de aire del de la app.
  - Blanco contra el halo da 3,977:1 sobre una nube, y el halo contra la nube, lo mismo: se ve sobre cualquier fondo. El cian de la app sobre blanco da 2,465.
  - Aparece sin animación, solo navegando con teclado.
- **Tab.** `Terminar` → `Pausa` / `Reanudar` → la llave (simulador) → `Terminar`. Shift+Tab, al revés. El inmersivo abre sin foco. Si `Pausa` tenía el foco al pausar, lo toma `Reanudar` en el mismo cuadro (02-tech-design §6.8).
- **DT4a-01.** Con el mouse, después de `Cancelar` el foco no vuelve a `Terminar`: un Enter no reabre el diálogo.

### 4.7 Diálogo de terminar (DN4a-11)

Es el diálogo de la app de §4.2, con su título (`workoutFinishTitle` `title` 20/600: `Terminar entrenamiento` mide 232,1 y entra en una línea en los 283 de TV), su texto (`body` `text.secondary`: `Se guardará la sesión. ¿Continuar?`, 245,7, una línea) y las acciones `Cancelar` (de texto) y `Terminar` (**primario relleno**, AC-WKT-20-03).

- **Por qué no "vidrio @92 %".** ART_BIBLE §3.5 lo proponía para el inmersivo, pero sin desenfoque (la It. 6b) el 8 % de transparencia solo deja ver el HUD fantasma detrás del velo. Opaco es más limpio y es el mismo diálogo que el de iniciar. `.rm-dialog--hud` queda como clase, sin reglas, para el diálogo de completado de la 4b.
- **El título** sale en 600, como en todos los diálogos de la app, no en 700. Con las tablas K2 (DD4a-06), el par `Te` de `Terminar` queda bien también en 700.
- **El velo.** Sobre el HUD da `#030911` sobre las tarjetas y `#042043` sobre el cielo. La cinta y la llave quedan encima, sin velo (AC-APP-03-10).

### 4.8 Avisos

| Dónde | Spec |
|---|---|
| Workout en reposo | Los de la It. 3: `surface.overlay`, borde 1 `border.default`, `r.l`, relleno 12 × 16, `bodyM` 14 que ajusta hasta tres líneas; de 16 a 16 con la ventana de menos de 600, y si no, `clamp(texto + 32; 344; 560)`. Con el simulador sube 8 dp sobre la llave: 128 desde abajo en TV (CAP-09, CAP-28). `Esta rutina no se puede iniciar: revisá sus pasos en Rutinas` (390,4) va en dos líneas en TV y en una en W1280 |
| Inmersivo | 8 dp sobre los controles (`BottomObstacles.Lift`, 02-tech-design §6.9). En vertical cae en el hueco entre la banda y los controles: en TV, de 782,1 a 830,1, con 15,6 de aire sobre la banda; en P360, con 6,7. La llave ya no está ahí (§3.9), así que el aviso no sube y **no tapa el reloj** (CAP-29). En horizontal va centrado sobre los controles y puede quedar sobre la zona del bote 4 s (es un mensaje, no una pieza del HUD). En L640 (b) sube sobre la llave |
| Al volver a reposo | `¡Entrenamiento completado!` o `No se pudo guardar la sesión` entran en el cuadro 0 de la salida del inmersivo, sobre la barra de pestañas que vuelve (§5.1) |
| Al volver del segundo plano o de la ventana minimizada (AC-WKT-02-06, -08, -09) | Si la sesión terminó mientras tanto, el aviso del fin entra (220) **una sola vez**, en el primer cuadro en que Workout en reposo está a la vista, con sus 4 s desde ahí. En Windows, eso es al restaurar la ventana o al volver el foco. Si al volver el cierre todavía no terminó, primero se ve la rutina terminada (§5.10), y el aviso entra con la salida del inmersivo, como en primer plano. De lo que pasó mientras tanto se muestra solo el último aviso |

### 4.9 Cifras: RowMate Numeric SC y `num.stat` (DN4a-12)

- **Todo número en la derivada.** Todo `.rm-type-num-*` usa la derivada (ART_BIBLE §4.2: cifras tabulares en todo número). Entra también `num.stat`, que en la 4a es el valor de los renglones ± del panel del simulador: así `150 W` → `160 W` no corre las letras.
- **Las tablas.** La cara que dibuja **texto** es la derivada 700: lleva la tabla K2 de su GPOS, la de Barlow sin pares entre cifras, porque las `*.tf` no tienen. Barlow 700 sale de K2.
- **Las caras sin tabla.** K4 queda para la derivada 600 y 800 (solo `TabularNumber`).
- **Barlow.** Los `.ttf` quedan como entrada de la receta (`freeze_tnum.py`). Si ya nada los referencia, sus `FontAsset` pueden salir de `FontAssetPaths.All`: lo decide el Architect (DN4a-22).
- **Las medidas** de cifras de este documento ya son las de la derivada: 0,521 em en 700 y 0,535 en 800, con `:` y `.` en 0,32 em y `-` en 0,370 / 0,376.

---

## 5. Motion

### 5.0 Quién mueve qué

| Animación | Mecanismo | Duración y curva | Reducir movimiento |
|---|---|---|---|
| Entrada y salida del inmersivo | código (`ImmersiveTransition`) | 240 `ease.enter` / 200 `ease.exit` (§5.1) | fundido 120 lineal |
| Diálogos de iniciar y de terminar | código (`DialogTransition`, It. 3) | 200 / 220 al entrar, 150 al salir | solo opacidad |
| Avisos | código (`ToastTransition`, It. 3) | 220 / 160; reemplazo con empujón | solo opacidad |
| Rodillo de valores, reloj y cuenta | código (`TabularNumber` + `DigitRoll`) | **180** `ease.enter` en todos | fundido 80 |
| Fuera de objetivo | código (`OffTargetMotion`) | color 150, flecha 120, empujón 240 (§5.3) | sin empujón ni desplazamiento: color 150 y flecha que funde 120 |
| `Próximo` y por terminar | código (`StepPulse`) | reloj común de 600 + 600 `ease.pulse` (§5.4) | realce fijo |
| Cambio de paso | código | 0-260 (§5.5) | fundidos, sin subida |
| Tarjeta BPM y kcal | código | 360 en total (§5.6) | BPM funde 240 / 120; kcal en el acto |
| `Pausa` ↔ `Reanudar` | en el acto | mismo cuadro | igual |
| Aviso sin remo | código (`GrowMotion` y `CollapseMotion`, It. 3) | crece con ω 12 y funde en 60-260; sale con 0-120 y 80-280 | fundido 160 / 120 sin cambio de alto |
| Deshabilitar sin remo | en el acto (regla de la It. 2) | — | igual |
| Escala de `FitBox` | código (resorte) | `spring.smooth` | en el acto |
| Rotar o cambiar el tamaño de la ventana | en el acto (AC-WKT-17-07) | — | igual |
| Llave | código | §5.9 | en el acto; cambio de carril solo con opacidad |
| *Hover* y presionado de los controles y de las tarjetas | USS (`Motion.uss`) | los de la It. 2 | sin escala |

Como en las It. 2 y 3:
- todo lo que va por código pide 60 fps mientras corre (en el inmersivo ya los pide la página);
- nada anima antes del primer cuadro presentado;
- ninguna propiedad animada por código tiene transición de USS;
- una captura fija con y sin "reducir movimiento" sale idéntica, salvo los realces fijos de §5.4;
- lo que crece desde 0 (el alto de `obj.`, el ancho de `Próximo` y el del hueco de la flecha) es un hueco que recorta, con el contenido ya a su tamaño final adentro (en `nowrap` o en absoluto). Así el texto nunca se aplasta ni se reacomoda mientras crece. Es el patrón de la It. 3, con la medida en el cuadro siguiente (QA-G4aT-14).

### 5.1 Entrada y salida del inmersivo (DN4a-05)

| t (ms) | Entrar (toque en `Entrenamiento libre` o en `Iniciar`) | Salir (la sesión vuelve a reposo) |
|---|---|---|
| 0 | El motor ya está en marcha y el reloj corre desde el toque (AC-WKT-13-01). La página entra con opacidad 0, entera: fondo, HUD y controles juntos. Workout pasa a `workoutInProgress` debajo, sin su barra superior (§4.1.5). La llave sale (opacidad 1 → 0 en 90, `ease.exit`) | `CoversShellChanged(false)`: pestañas y barra vuelven debajo de la página, que empieza a fundir. Workout ya está en reposo, con su barra superior. El aviso de fin, si hay, entra (220) sobre la barra. La llave sale (90) |
| 0-240 · 0-200 | opacidad 0 → 1, `ease.enter`: 0,362 / 0,622 / 0,799 / 0,910 / 0,970 / 0,995 a los 33 / 67 / 100 / 133 / 167 / 200 ms | opacidad 1 → 0, `ease.exit`: 0,995 / 0,961 / 0,872 / 0,702 / 0,421 / 0 a los 33 / 67 / 100 / 133 / 167 / 200 |
| fin | a los 240, pestañas y barra en `display: none` (It. 3). La llave entra en su lugar del inmersivo (opacidad 0 → 1 y escala 0,85 → 1 en 160, `ease.enter`) | a los 200 la página sale del árbol y se dispone. La llave entra en el carril izquierdo |

- **Desde el diálogo de iniciar.** El diálogo sale (150) mientras entra el inmersivo (240): el velo se va y el HUD aparece detrás.
- **Nada salta.** No hay desplazamientos ni escalas: el HUD se arma en su lugar y funde. La cascada "sumergirse" es de la 4b.
- **Interrupción.** Si la sesión termina antes de que acabe la entrada (una base que falla en el acto, o un `Terminar` muy rápido), la salida arranca desde la opacidad que tiene.

### 5.2 Valores en vivo

- **El rodillo.** `TabularNumber` + `DigitRoll` de la It. 2, sin cambios: solo ruedan las celdas que cambian, durante 180 ms, y desde el cuadro 0 se ve el texto real.
- **El reloj y la cuenta.** También 180, no los 160 de ANIMATION_BIBLE §2.6: así hay una sola constante (`DigitRoll.DurationMs`) y el rodillo ocupa el 18 % de cada segundo.
- **Con dos datos seguidos.** Si llega otro dato antes de terminar, el rodillo en curso salta a su final.
- **Al volver de un alcance** (segundo plano), `SetValue(…, animate: false)`: §5.10.

### 5.3 Fuera de objetivo (VID-02)

`t = 0` en la lectura que cruza el umbral. El estado cambia en el acto (AC-WKT-10-06): esto es solo cómo se ve.

| t (ms) | Color del valor (150 `ease.move`) | Flecha (120 `ease.enter`) | Empujón del valor (1 → 1,04 en 0-90 `ease.enter` → 1 en 90-240 `ease.move`) |
|---|---|---|---|
| 0 | 0,000 | opacidad 0, a 4 dp | 1,0000 |
| 33,3 | 0,053 | 0,622, a 1,51 | 1,0299 |
| 66,7 | 0,350 | 0,910, a 0,36 | 1,0393 |
| 100 | 0,851 | 0,995, a 0,02 | 1,0398 |
| 133,3 | 0,989 | 1, en su lugar | 1,0359 |
| 166,7 | 1,000 | 1 | 1,0187 |
| 200 | 1,000 | 1 | 1,0033 |
| 233,3 | 1,000 | 1 | 1,0001 |

- **Hacia dónde se mueve la flecha.** Entra desde 4 dp en el sentido contrario al que indica, y llega a su lugar: ▲ sube desde 4 dp más abajo; ▼ baja desde 4 dp más arriba. Al volver a objetivo funde 1 → 0 en 120 (`ease.exit`) y sigue 4 dp en su sentido. El color vuelve a blanco en 150.
- **El empujón** escala el valor (la flecha y las cifras, origen en su centro) y crece 2,5 dp por lado a 64: no sale del relleno.
- **Cuándo hay empujón.** Solo al pasar de cumple a fuera **dentro del mismo paso**, y como mucho uno cada 4 s por tarjeta: un vaivén alrededor del umbral cambia color y flecha cada vez, con un solo empujón. Al empezar un paso, la tarjeta llega ya con su color y su flecha, sin empujón: el borde, el halo y `obj.` que aparecen ya llaman la atención (§5.5).
- **Las mezclas de color** son en sRGB, desde el color que se ve: un cruce a mitad de otro no salta.

### 5.4 `Próximo` y por terminar: un reloj común

Un solo reloj de pulso por barra, `p(t)`: sube de 0 a 1 en 600 ms y baja de 1 a 0 en 600, con `ease.pulse` (0,066 / 0,248 / 0,500 / 0,752 / 0,934 / 1 a los 100 / 200 / 300 / 400 / 500 / 600 ms).
- **Su origen** es el primer cuadro en que hay "a continuación" o "por terminar", después de uno sin ninguno de los dos. Con rutinas de pasos de 15 s o menos (`Corta`), "a continuación" sigue de paso a paso y el reloj no se reinicia.
- **Por qué uno solo.** Es el `AnimationController(600)..repeat(reverse: true)` del legacy (`immersive_workout_screen.dart:319-322`). `Próximo` y la cuenta laten juntos; con dos orígenes quedarían desfasados un tercio de ciclo. El legacy lo arranca con la barra, al empezar la rutina, y no lo reinicia (`:317-322`). Con el origen de acá solo cambia la fase, y eso no se ve (O-01 del gate de paridad).

| Efecto | Spec |
|---|---|
| `Próximo` entra | ancho 0 → N con `spring.smooth` (ω 12) y opacidad de entrada 0 → 1 en 200 (`ease.enter`), multiplicada por el latido 0,6 + 0,4·p. Por cuadro (ms · ancho · opacidad): 0 · 0 % · 0; 100 · 33,7 % · 0,546; 200 · 69,2 % · 0,699; 300 · 87,4 % · 0,800; 490 · 98,1 % · 0,968; 600 · 99,4 % · 1. Llega tenue y se enciende: "late suave" |
| `Próximo` late | fondo y borde (`.rm-hud-up-next__bg`) a 0,6 + 0,4·p; el texto al 100 % (DV-38) |
| `Próximo` sigue de un paso al siguiente | no sale ni vuelve a entrar: el tipo viejo funde en 0-80 y el nuevo entra en 80-200, y el ancho sigue su resorte |
| `Próximo` sale (cambio de paso) | opacidad → 0 en 0-120 (`ease.exit`) y ancho → 0 en 80-280 (`ease.move`); el espacio lo toma |
| Por terminar empieza | escala 1 → 1,1 con `spring.snappy` (ω 22): 1,0167 / 1,0431 / 1,0645 / 1,0791 / 1,0881 / 1,0934 / 1,0981 / 1,0995 a los 33 / 67 / 100 / 133 / 167 / 200 / 267 / 333 ms. Color = mezcla de blanco y `step.<tipo>.hud` en `a·p`, con una **rampa** `a` de 0 a 1 en 150 (`ease.move`): si el reloj común está a mitad de ciclo, el color no salta |
| Por terminar termina (cambio de paso o fin) | escala → 1 con ω 22 y `a` → 0 en 150 desde donde esté |

Fase en las tomas (el arnés las calcula con el reloj de prueba; 02-tech-design §6.6). **Corregida en WP-D** (§17): una foto fija del arnés no cae en el segundo justo sino en la ventana de la lectura (`AtSecond`: entre 0,45 y 0,95 s después, para que la lectura del remo y sus rodillos ya estén quietos), así que la fase de las tomas con `AtSecond` no era la de esta tabla. Valen las de la columna "En la toma":

| Toma | Origen del reloj | Instante pedido (notas) | En la toma | p | Qué se ve |
|---|---|---|---|---|---|
| CAP-21 | 75,0 s ("a continuación" del primer trabajo, `0:15`) | 78,0 (p = 1) | ≈ 78,7 (`AtSecond(78)`) | ≈ 0,05 | `Próximo: Descanso` entero, con el fondo y el borde en el punto tenue del latido (0,62): el texto no late (DV-38), así que se lee igual. Con p alto no hay ningún instante del segundo 78 con los rodillos quietos (el de `0:12` a los 78,0 y el de la lectura después) |
| CAP-22 | 75,0 | **87,6** (DN4a-21) | 87,6 (sin la ventana) | 1,000 | `0:03` a 1,1 en `#FF5D7F` puro. A los 87,0, p = 0 y la cuenta se vería blanca |
| DEMO-08 | 15,0 (calentamiento, `0:15`) | 18,0 (p = 1) | ≈ 18,9 | ≈ 0,5 | `Próximo: Trabajo` entero (el fondo a 0,8) |
| DEMO-12 | 235,0 (enfriamiento: no hay "a continuación"; "por terminar" en `0:05`) | 237,0 (p = 0,752) | **237,9** (sin la ventana; WP-D) | ≈ 0,95 | `0:03` a 1,1 teñida al 95 %: `#43A7CD` (la spec pide "teñida"). Con `AtSecond(237)` caía en 237,5 (p ≈ 0,05) y salía casi blanca (`#EDF5FA`) |

### 5.5 Cambio de paso (VID-01)

`t = 0` en el segundo en que el motor cambia de paso (ANIMATION_BIBLE §2.7, sin las píldoras).

| t (ms) | Tipo y pastilla viejos (0-120 `ease.exit`, quietos y en posición absoluta) | Tipo y pastilla nuevos (60-260 `ease.enter`, y +8 → 0) | Borde, halo, hueco de la flecha y alto de `obj.` (0-240 `ease.move`) | Texto de `obj.` (60-240 `ease.enter`) |
|---|---|---|---|---|
| 0 | 1,000 | 0,000, +8,00 | 0,000 | 0,000 |
| 33,3 | 0,977 | 0,000, +8,00 | 0,018 | 0,000 |
| 66,7 | 0,826 | 0,098, +7,22 | 0,092 | 0,108 |
| 100 | 0,422 | 0,488, +4,10 | 0,286 | 0,529 |
| 133,3 | 0,000 | 0,743, +2,05 | 0,651 | 0,789 |
| 166,7 | — | 0,896, +0,83 | 0,882 | 0,930 |
| 200 | — | 0,972, +0,23 | 0,973 | 0,988 |
| 260 | — | 1,000, 0 | 1,000 | 1,000 |

- **La cuenta** rueda a la duración nueva (180; hacia arriba, porque el valor sube). Por terminar termina (§5.4). Si el paso nuevo es por distancia, el ancho del número interpola en los mismos 180.
- **Tarjetas que ganan objetivo.**
  - El borde pasa de 1 dp `@10 %` a 1,5 base `@70 %`: ancho y color interpolados.
  - El halo funde de 0 a 1.
  - El hueco de la flecha crece de 0 a 0,48 em y el número se corre con él.
  - `obj.` abre su alto (0 → 16,9 base) y funde.
  - El valor cruza en 150 del `.hud` al blanco o al rojo; la flecha entra si ya está fuera.
- **Tarjetas que pierden objetivo.** Al revés: `obj.` funde en 0-120 (`ease.exit`) y cierra su alto en 80-280 (`ease.move`). El borde, el halo y el hueco vuelven en 240, y el valor vuelve a su `.hud` en 150.
- **Sin empujón** (§5.3).
- **Lo que mueve una tarjeta.** El alto de `obj.` mueve hacia abajo la tarjeta de abajo (Watts) con el mismo tiempo: la columna se estira sin salto.
- **Como quedó construido (WP-D, §17):**
  - **El tipo y la pastilla** son dos elementos que cambian de texto (presupuesto de 60, §7): los viejos funden en 0-120 y los nuevos entran en **120-260** con la subida de 8, uno después del otro. No se cruzan (la tabla pedía 60-260); en VID-01 se lee como un relevo limpio.
  - **La tarjeta crece** con el hueco (ancho) y con `obj.` (alto) en el mismo tiempo, desde su tamaño de antes; `obj.` recorta su propio texto mientras abre su alto.
  - **La flecha de un paso que empieza fuera** entra al terminar el hueco, en **240-360** (la entrada de §5.3, desde 4 dp), y no con el valor. Mientras el hueco crece, una flecha ya visible quedaría encima de la primera cifra (el hueco no le deja lugar hasta que llega al 78 %) o fuera de la tarjeta. El valor sí cruza al rojo en 150 desde el cuadro del cambio. Si el cuadro llega tarde (o el reloj de prueba avanza segundos de una vez), todo, también la flecha, queda en su estado final en el primer cuadro.
  - **Reducir movimiento**: el hueco, el grosor del borde y los altos, en el acto; funden el color del borde, el halo y el texto de `obj.`.

### 5.6 Tarjeta BPM y kcal

| t (ms) | BPM aparece (pulso > 0): kcal a su lugar nuevo (0-240 `ease.move`) · BPM funde (120-360 `ease.enter`) | BPM se va (pulso 0): BPM funde (0-120 `ease.exit`, en su lugar, absoluta) · kcal a su lugar nuevo (120-360 `ease.move`) |
|---|---|---|
| 0 | 0,000 · 0,000 | 1,000 · 0,000 |
| 60 | 0,071 · 0,000 | 0,872 · 0,000 |
| 120 | 0,500 · 0,000 | 0,000 · 0,000 |
| 180 | 0,929 · 0,577 | — · 0,071 |
| 240 | 1,000 · 0,872 | — · 0,500 |
| 300 | — · 0,983 | — · 0,929 |
| 360 | — · 1,000 | — · 1,000 |

- **Cómo se mueve kcal.** Por `translate`, desde donde estaba (FLIP): en vertical sube 71,7·s dp y en horizontal no se mueve (está en el rincón).
- **Por qué BPM espera.** Empieza a fundir cuando kcal ya le dejó la mitad de su lugar, así no se dibujan una sobre la otra.
- **Con la caída simulada del pulsómetro** (3 s): la tarjeta se va y vuelve, con 360 + 3000 + 360.
- **El corazón** arranca con el primer pulso (ANIMATION_BIBLE §2.9).

### 5.7 Pausa, `Reanudar` y `Terminar`

- **`Pausa` ↔ `Reanudar`.** En el mismo cuadro, sin transición (AC-WKT-20-02). El reloj de la banda queda quieto. En la 4a no hay velo de pausa (es de la 4b): la pausa la dicen el botón verde y el reloj detenido.
- **`Terminar`.** El diálogo entra (velo 200, diálogo 0,96 → 1 en 220). Con `Cancelar`, la sesión sigue en su fase y el diálogo sale (150).
- **Al confirmar.** Los controles pasan al 0,38 en el acto y, cuando el guardado termina, el inmersivo sale (§5.1).
- **La rutina termina sola** (AC-WKT-03-05). Igual: la fase pasa a terminada, los controles al 0,38 en el acto y, con el guardado, la salida y `¡Entrenamiento completado!`.
- **Con el diálogo de terminar abierto** (AC-WKT-20-07). En el cuadro en que la fase pasa a terminada, el diálogo sale como con `Cancelar` (150) y, detrás, los controles ya están al 0,38.
  - Con la base al día el guardado vuelve enseguida, y las dos salidas se pisan: el diálogo se va por encima (150) mientras el inmersivo funde (200). Es como la entrada desde el diálogo de iniciar (§5.1), con el inmersivo saliendo en lugar de entrar.
  - El aviso entra en el cuadro 0 de la salida del inmersivo, como siempre. Nunca queda un diálogo sobre Workout en reposo.

### 5.8 Workout en reposo

- **El aviso sin remo** entra y sale en vivo (AC-BLE-10-05), con los verbos de la It. 3:
  - al entrar, crece (alto y margen inferior con ω 12) y funde en 60-260: 16,3 % / 33,7 % / 42,2 % / 69,2 % / 84,9 % / 98,1 % del alto a los 60 / 100 / 120 / 200 / 280 / 490 ms;
  - al irse, funde en 0-120 y colapsa en 80-280;
  - el botón y la lista bajan con él, sin salto;
  - el botón y las tarjetas cambian de opacidad en el acto (deshabilitado, regla de la It. 2).
- **La lista.** La primera carga llega con la pestaña, sin animación propia. Si la carga termina con la pestaña a la vista, el indicador funde → 0 en 120 y las tarjetas entran escalonadas de a 35 ms (hasta 8), con opacidad 0 → 1 e y +12 → 0 en 240 (It. 3 §5.5). El vacío y el error entran en 240 con y +8 → 0.
- **Los cambios hechos en Rutinas** llegan con la pestaña oculta: no se animan.

### 5.9 La llave

| Cambio | Cómo | Reducir movimiento |
|---|---|---|
| Entrar y salir del inmersivo | fundido de salida 90 en el cuadro 0 y entrada 0,85 → 1 en 160 al terminar la transición (00-deuda-diseno §1.6) | solo opacidad: 90 y 120 |
| Rotar o cambiar la ventana dentro del inmersivo | en el acto, con el HUD (en la 4a no hay fundido cruzado: abajo) | igual |
| Aparece o se va BPM, cambian los valores | no se mueve (§3.9) | — |
| Panel | crece desde la esquina de abajo a la derecha de la llave (§3.9) | solo opacidad |
| Panel abierto cuando la llave cambia de lugar (AC-BLE-16-02) | hace lo que haría la llave. Al entrar o salir del inmersivo sale en el cuadro 0 (opacidad 1 → 0 en 90, `ease.exit`) y, cuando termina la transición, se abre en el lugar nuevo con la transición de la It. 2 desde su esquina nueva (opacidad en 160, escala 0,92 → 1 en 240), con su contenido y su desplazamiento. No viaja por la pantalla ni queda en el lugar de una página que ya salió; mientras sale, no recibe toques. Al rotar o cambiar la ventana, en el acto, con el HUD. La llave sigue oculta mientras el panel está abierto | solo opacidad: 90 y 160; al rotar, en el acto |

Al rotar o cambiar el tamaño de la ventana, el HUD entero (y la llave con él) se reacomoda en el acto, sin animación, y la sesión y los valores siguen (AC-WKT-17-07). El fundido cruzado de 200 de ANIMATION_BIBLE §2.12 necesita una foto del cuadro anterior: llega con la escena de la 6a.

### 5.10 Al volver del segundo plano (AC-WKT-02-08)

En el primer cuadro todo se aplica **en el acto**, con el estado del presente:
- valores sin rodillo (`animate: false`), colores del semáforo, flecha, bordes, halo, hueco, `obj.`;
- la fila del paso (tipo, pastilla, cuenta, `Próximo`, por terminar con su escala) y la tarjeta BPM.

No arranca ningún empujón. Si el estado de los pulsos empezó en segundo plano, su origen es ese primer cuadro (p = 0). No se ve nada "poniéndose al día".

**Si la sesión terminó mientras tanto** (AC-WKT-02-06):
- **Si el cierre todavía no volvió**, el primer cuadro muestra la rutina terminada, en el acto: el último paso con `0:00` (o su largo, si es por distancia), sin `Próximo` ni por terminar (AC-WKT-03-06, AC-WKT-14-06), y los controles al 0,38. Cuando el cierre vuelve, el inmersivo sale como en primer plano (§5.1) y entra el aviso del fin (§4.8).
- **Si ya volvió**, el primer cuadro ya es Workout en reposo: el inmersivo sale **sin transición**, así no se ve fundirse el HUD de una sesión que terminó antes. El aviso del fin entra (220), con sus 4 s desde ese cuadro.
- **En Windows, minimizada o sin foco**, el *loop* sigue y la salida ocurre en su momento, como en primer plano. El aviso del fin espera a que se restaure la ventana o vuelva el foco, y entra en ese cuadro (AC-WKT-02-09).

### 5.11 Reducir movimiento (resumen)

- **Transiciones.** El inmersivo, los diálogos y los avisos, solo con opacidad.
- **Rodillo.** Fundido de 80.
- **Fuera de objetivo.** Sin empujón ni desplazamiento de la flecha: quedan el color de 150 y la flecha, que funde en 120.
- **Realces fijos.** `Próximo` a opacidad 1, sin latido. La cuenta por terminar a escala 1,1 y en el color del paso, que llega en 150 sin rampa de pulso (la escala cambia en el acto).
- **Movimientos.** El corazón, quieto. El cambio de paso, con fundidos sin subida. kcal en el acto. El aviso sin remo, con fundido sin cambio de alto. La llave en el acto.
- **Lo que no cambia:** los datos, cuándo cambian, el *spinner* y los fundidos de color.

### 5.12 `Motion.uss` (WP-D; siempre bajo `:hover`)

Con los tiempos de la It. 2 (capa hacia el estado en `dur.micro` `ease.enter` y vuelta en `dur.fast` `ease.exit`; escala hacia 90 y vuelta en 240, `ease-out-cubic`):

| Selector | Qué |
|---|---|
| `.rm-button--hud-danger`, `--hud-outline`, `--hud-success` | capa (la de `Terminar`, oscura) y escala 0,97 |
| `.rm-routine-card` en Workout | capa 4 / 8 %, sin escala (la de Rutinas) |
| `#workout-free` | el primario de la It. 2 |

Con `.rm-reduce-motion`, ninguna escala.

### 5.13 Constantes para `MotionSpecs` (WP-A)

Las duraciones y las curvas que ya son tokens se toman de `Tokens`. Nombres sugeridos:

| Constante | Valor | Constante | Valor |
|---|---|---|---|
| `HudColorMs` | 150 (`ease.move`; también la rampa de la cuenta) | `OffTargetArrowMs` / `OffTargetArrowShiftDp` | 120 / 4 |
| `OffTargetBumpScale` / `OffTargetBumpPeakMs` / `OffTargetBumpEndMs` | 1,04 / 90 / 240 | `OffTargetBumpCooldownMs` | 4000 |
| `StepPulseHalfMs` | 600 (`ease.pulse`) | `UpNextMinOpacity` / `UpNextFadeInMs` | 0,6 / 200 |
| `UpNextWidthOmega` | `CriticalSpring.SmoothOmega` (12) | `EndingSoonScale` / `EndingSoonOmega` | 1,1 / `SnappyOmega` (22) |
| `StepSwapOutMs` | 120 (`ease.exit`) | `StepSwapInStartMs` / `StepSwapInEndMs` | 60 / 260 (`ease.enter`; sube `shift.s`) |
| `TargetDecorMs` | 240 (`ease.move`: borde, halo, hueco, alto de `obj.`) | `TargetTextInStartMs` / `TargetTextInEndMs` | 60 / 240 |
| `BpmMoveMs` | 240 | `BpmFadeInStartMs` / `BpmFadeInEndMs` | 120 / 360 |
| `BpmFadeOutMs` / `BpmMoveOutStartMs` / `BpmMoveOutEndMs` | 120 / 120 / 360 | `ImmersiveInMs` / `ImmersiveOutMs` / `ReducedImmersiveFadeMs` | 240 / 200 / 120 |
| `FinishSpinnerDelayMs` (P2) | 300 (= `SaveSpinnerDelayMs`) | — | — |

Se reusan, sin cambios:
- `DigitRoll.DurationMs` (180) y `ReducedFadeMs` (80);
- `GrowOmega`, `GrowFadeStartMs` / `GrowFadeEndMs` y `CollapseFadeMs` / `CollapseStartMs` / `CollapseEndMs` (aviso sin remo, salida de `Próximo` y de `obj.`);
- `ToastTextOutMs` / `ToastTextInEndMs` (el tipo de `Próximo` que cambia);
- `FitScaleOmega`, `FabInScaleFrom` y `PanelInScaleFrom` (la llave).

De geometría, para `HudGeometry` (WP-A):
- **Barras:** `TopBarHeight` 40, `TopBarRowHeight` 22, `TopBarPadV` 8, `StepRowPadH` 12, `FreeRowPadH` 14.
- **Banda:** `BandPadH` 20 / 14 y `BandDividerGap` 16 / 10 (vertical / horizontal).
- **Flecha:** `ArrowEm` 0,4, `ArrowGutterEm` 0,48 y `ArrowUpBaselineEm` / `ArrowDownBaselineEm` 0,196 / 0,214.
- **Unidades:** `UnitAlpha` 0,8.
- **Fondo y zona del bote:** `HorizonPortrait` / `HorizonLandscape` 0,40 / 0,42, `CameraHeightM` 2,2, `RowerDistanceM` 9,5.
- **Llave:** `WrenchGap` 8.

---

## 6. Legibilidad a 1,5 m

TV con `s` = 1,054, en un teléfono (1 dp = 0,159 mm). La altura de mayúscula de Barlow es 0,70 em y la de Inter, 0,728 em. El ángulo se compara con 5′, la letra de la agudeza 20/20.

| Elemento | dp en pantalla | Altura de mayúscula | A 1,5 m | Contra 5′ |
|---|---|---|---|---|
| SPM | 67,5 | 7,50 mm | 17,2′ | 3,4× |
| Split, Watts | 46,4 | 5,16 mm | 11,8′ | 2,4× |
| Banda | 35,8 | 3,98 mm | 9,1′ | 1,8× |
| kcal, BPM | 33,7 | 3,75 mm | 8,6′ | 1,7× |
| Flecha de SPM (tinta) | 20,8 de alto | 3,31 mm | 7,6′ | 1,5× |
| Flecha de Split y Watts | 14,3 | 2,27 mm | 5,2′ | 1,0× |
| Cuenta (por terminar) | 21,1 (23,2) | 2,34 (2,58) mm | 5,4′ (5,9′) | 1,1× (1,2×) |
| Banda en L640 a 0,776 | 24,3 | 2,71 mm | 6,2′ | 1,2× |
| Tipo de paso 13, rótulos 13 / 12 / 11, `obj.` 12, pastillas 11 / 10 | 13,7 a 10,5 | 1,58 a 1,22 mm | 3,6′ a 2,8′ | de cerca |

**De lejos** se leen los valores, el rojo de fuera de objetivo y el color del paso.
- **Rojo y blanco.** El rojo (`#FF625B`, luminancia 0,32) también es más oscuro que el blanco (1,0): un daltónico ve el cambio por la claridad, además de la flecha (redundancia de D-ART-01).
- **La flecha de Watts y Split** queda justo en 5′: de lejos manda el color, y la flecha es para leerla de cerca y para el daltonismo.
- **La cuenta** queda en el límite; la cuenta grande de los últimos 3 s (D-ANI-05) es de la 4b.
- **De cerca** se leen `obj.`, los rótulos y las pastillas. Son las medidas del legacy (AC-WKT-14-02 a -05 y D-ART-09).

En W1280 (1 dp = 1 px de un monitor de 96 ppp), SPM mide 102,4 px: 43′ a 1,5 m.

---

## 7. Rendimiento

- **Elementos.** Este diseño suma dos al árbol de 02-tech-design §6.13: el cielo del fondo y el fondo de `Próximo`. El hueco de la flecha es el mismo elemento `.rm-hud-card__arrow`, con un ancho fijo, así que no suma. Con la cuenta de 58 del Architect daba 60, en el tope y sin margen, y con el *spinner* de DN4a-20 se pasaba (lo marcó el gate técnico, §9). Por eso el Architect toma la variante: el fondo es un solo elemento con una textura de 1 × 100 generada al arrancar, cielo y agua con el corte en el texel 40 o 42 (§4.3). La cuenta vuelve a 59.
- **Nada nuevo por cuadro.** Los latidos cambian la opacidad de un elemento; el empujón y la escala de la cuenta son `scale`; el color va por `color` en línea; los anchos de `Próximo`, del hueco y de `obj.` son animaciones de *layout* cortas (≤ 490 ms, una zona), la excepción que ya admite ANIMATION_BIBLE §0.1. Nada de materiales ni *shaders* nuevos. Las dos texturas se generan al arrancar y son chicas: `GlowTexture`, que ya estaba en el contrato, y la del fondo (1 × 100, 400 B).
- **0 B por cuadro entre lecturas**: los textos se arman a 1 Hz y los pulsos solo cambian estilos.

---

## 8. Capturas: qué tiene que verse

### 8.1 Reglas de toma (además de las de las It. 2 y 3 y de 02-tech-design §7.1)

1. **Sin transiciones en curso.** Ningún fundido del inmersivo, cambio de carril de la llave, cambio de paso, salida de `Próximo`, animación de BPM o kcal, empujón ni rampa de color.
2. **Los pulsos se fotografían en la fase de §5.4.** CAP-22 va a los 87,6 s y no a los 87,0, para que la cuenta salga teñida. El corazón cuenta "en reposo" con `BeatRestTolerance`.
3. **Las sondas son de la tubería `GammaRt`.** Tolerancia ±1 por canal en fondos y ±3 en trazos de texto; los valores del HUD, ±2 (AC-APP-06-01). "Punto" = un píxel de fondo, lejos de textos e íconos.
4. **El FitBox de §3.7 es parte de lo esperado.** Por ejemplo, en CAP-16 la banda sale a 0,561 a propósito.

### 8.2 Por captura (aspecto; QA verifica además lo funcional de la spec §7.3)

| ID | Qué miro | Sondas (`aspect: true`) |
|---|---|---|
| CAP-01 | `Entrenamiento libre` cian de 16 a 16 y 56 de alto, con su `play_arrow`; `␣␣O elegí una rutina:` tenue; las 4 tarjetas iguales a las de Rutinas, con el *chevron* centrado en la columna de 28 dp; la llave abajo a la izquierda, debajo de la última tarjeta; la cinta sobre la esquina de la "E" | fondo `#0D1B2A`; botón `#00B4D8`; tarjeta `#1A2E45`; *chevron* (trazo) `#969FA9`; etiqueta (trazo) `#90969D`; llave `#1E3550` |
| CAP-02 | El aviso arriba, ocre, con el ícono a la altura de la primera línea; el botón y las tarjetas apagados | aviso `#312E24`; su texto (trazo) `#FF9800`; botón `#08556C`; tarjeta `#142438` |
| CAP-03 | El texto centrado en lo que queda debajo del botón, en una línea | texto (trazo) `#90969D` |
| CAP-04 | Lo mismo en inglés; ninguna tarjeta cambia de alto, salvo por el texto | como CAP-01 |
| CAP-05 | La última tarjeta entera, 16 dp sobre la llave; nada cortado | `Audit` sin desbordes |
| CAP-06 | Solo el indicador, entero (después de la gracia y del fundido: 120 + 160 ms) | indicador (trazo) `#00B4D8` |
| CAP-07 | Ícono rojo, texto gris claro y `Reintentar` tonal, centrados debajo del botón habilitado | ícono (trazo) `#FF7272`; tonal `#0B2D3F` |
| CAP-08 | Diálogo de 331, título en una línea, una línea en blanco entre el resumen y la pregunta, `Iniciar` cian; la cinta y la llave sin velo | velo `#050B11` y sobre la tarjeta `#0A121C`; diálogo `#1E3550`; `Iniciar` `#00B4D8` |
| CAP-09 | El aviso en dos líneas, entero, 8 dp sobre la llave | aviso `#1E3550` |
| CAP-10 | Lo de CAP-01 a lo ancho: botón de 1248, tarjetas de 1248, *chevron* a 28 del borde; frente a `docs/image-02.png`, las mismas posiciones, sin el escenario | como CAP-01 |
| CAP-11 | Letra 1,3×: tarjetas más altas, aviso y botón sin cortes | `Audit` |
| CAP-12 | Cielo arriba del 40 % y agua abajo; barra libre de 40 con `rowing` y título; SPM grande cian; Split naranja; Watts amarillo `150` + `W` amarilla tenue; banda `00:42 │ … m`; kcal verde; `Terminar` (rojo claro) y `Pausa` (blanco) sobre el vidrio; la llave sobre kcal; ninguna tarjeta con borde de color | cielo `#0B4FA8` (x = w/2, y = 69·s: a mitad de camino entre la barra y las tarjetas, 72,7 en TV; con y = 70 la sonda caía sobre la barra en W1280, WP-D); agua `#0E4F8F` (x = w/2, y = 640 o 0,7 del alto); vidrio sobre el cielo `#08162A` (relleno de SPM); vidrio sobre el agua `#081627` (relleno de la banda); valores (trazo) SPM `#3ACCF1`, Split `#FFAA47`, Watts `#FFD60A`, tiempo `#E0E0E0`, distancia `#4ECBF2`, kcal `#06D6A0`; `W` `#CEB010`; `m` `#40A7C9`; rótulos `#00B4D8` · `#FF9F1C` · `#FFD60A` · `#06D6A0`; `Terminar` `#FF7272`; contorno de `Pausa` `#434E5B` |
| CAP-13 | Además, BPM rosa debajo de kcal (kcal subió); la llave en el mismo lugar que en CAP-12 | BPM valor y corazón `#FF5D7F` |
| CAP-14 | `Reanudar` verde del mismo tamaño que `Pausa`; tarjetas al 100 % | `Reanudar` `#06D6A0`; su texto `#04131C` |
| CAP-15 | Fila de abajo `banda │ Terminar Pausa │ llave │ BPM kcal`, con kcal en el rincón; banda compacta entera; nada sobre la zona del bote | como CAP-12, sobre el agua |
| CAP-16 | Contorno naranja de la zona del bote, libre de piezas del HUD; la banda achicada (0,561) sin tocar los controles ni la zona; kcal `9999` y BPM `188` enteras, a ancho de contenido; la llave sobre `Pausa`, dentro del contorno (§3.6) | contorno `#FF5722`; `HudOverlap`; `WrenchAudit` |
| CAP-17 | Escala 1,6 (caja de SPM 102,4); kcal y BPM de 153,6 cada una; la llave a la derecha de `Pausa`; la banda compacta sin achicar; frente a `docs/image-03.png`, las mismas zonas | como CAP-13 |
| CAP-18 | `Calentamiento` amarillo y `0:24`; sin pastilla ni `Próximo`; tarjetas como en libre | tipo (trazo) `#FFD166` |
| CAP-19 | `Trabajo` rosa; la pastilla con texto blanco y la tendencia celeste. Watts: `150` rojo con ▲ en su hueco, `obj. 160` debajo de las cifras, borde y halo amarillos. SPM: blanco, con el hueco vacío y `obj. 22`, borde cian. Split sin borde | pastilla `#362038`; tendencia `#4CC9F0` (un punto del trazo de la flecha de `trending_up`, en (0,5936; 0,40) de la pastilla: es un glifo dentro del texto y el color dominante es el blanco del texto, WP-D); Watts valor y flecha `#FF625B`; `obj.` `#B5B9BF`; borde de Watts `#B59C14` y de SPM `#0285A4` (a 0,008 del alto: en el medio de la línea de 1,5·s, también a 1,6); halo de Watts `#3C6A88` (±6, junto al borde) |
| CAP-20 | `500m`; la pastilla recortada con "…"; Watts fuera (▲, `obj. 150`); Split fuera (▼, `obj. 02:05`, borde naranja) | borde de Split `#B57620`; flechas `#FF625B` |
| CAP-21 | `Próximo: Descanso` entero, a 8 de `0:12`; la pastilla de objetivo recortada | `Descanso` `#FFFFFF`. El fondo y `Próximo:` dependen de la fase del latido en la toma (§5.4: p ≈ 0,05): fondo `#082E3A` y `Próximo:` `#B4C0C4` (con p = 1 serían `#073D42` y `#B5C5C6`) |
| CAP-22 | `0:03` a escala 1,1, rosa puro (p = 1); `Próximo` entero | cuenta (trazo) `#FF5D7F` |
| CAP-23 | La pastilla entera (TH: entra todo); nada se encima | `HudOverlap` |
| CAP-24 | SPM y Watts fuera, con ▲ y su `obj.`; SPM apenas achicado (0,962) por la zona del bote; la llave sobre `Pausa` | flechas `#FF625B` |
| CAP-25 | Lo de CAP-19 a 1,6 | como CAP-19 |
| CAP-26 | Velo parejo sobre el HUD; diálogo opaco de 331; `Terminar entrenamiento` en una línea; `Terminar` cian | velo sobre las tarjetas `#030911`, sobre el cielo `#042043` y sobre el agua `#062039`; diálogo `#1E3550` |
| CAP-27 | Inglés: `Free Workout`, `target 160`, `Up next: Rest` (la pastilla pierde 12,9) | como las tomas en español |
| CAP-28 | Workout en reposo con el aviso 8 dp sobre la llave | aviso `#1E3550` |
| CAP-29 | El aviso en el hueco entre la banda y los controles, entero; **el reloj a la vista** | aviso `#1E3550`; `#toast.yMax + 8 ≤ #hud-controls.yMin` y `#toast.yMin ≥ #hud-band.yMax` |
| CAP-30 | `?` blanco en la cuenta; sin `Próximo` | — |

### 8.3 Demo (W1280)

| ID | Qué miro |
|---|---|
| DEMO-01 | Lo de CAP-10 con `F-4A`: la mini-barra de `Prueba 4a` con 6 tramos de 24 dp (amarillo, rosa, verde, rosa, verde, azul) |
| DEMO-02 | Lo de CAP-17 en libre: `00:05`, `24.0`, `150 W` |
| DEMO-03 | El panel abierto desde la esquina de la llave, a la izquierda de kcal y BPM: Split y Watts a la vista. Tapa la parte de arriba de `Pausa` (§3.9); DEMO-04 lo cierra antes de pausar |
| DEMO-04 · DEMO-05 · DEMO-06 | `Reanudar` verde · el diálogo de terminar como CAP-26 · Workout en reposo sin aviso |
| DEMO-07 | El diálogo de `Prueba 4a` |
| DEMO-08 | `Calentamiento` con `0:12` y `Próximo: Trabajo` entero (p = 1) |
| DEMO-09 · DEMO-10 · DEMO-11 | Watts fuera (rojo, ▲, `obj. 160`) · Watts cumple (blanco, hueco vacío) · SPM fuera (▲, `obj. 24`) |
| DEMO-12 | `0:03` grande y teñida (a los 237,9 s, p ≈ 0,95: `#43A7CD`), sin `Próximo` |
| DEMO-13 · DEMO-14 | Workout con `¡Entrenamiento completado!` · el aviso ocre y todo apagado |

### 8.4 Videos (30 cuadros por segundo; valores por cuadro en §5)

| ID | Qué miro |
|---|---|
| VID-01 (17 s, DT4a-04) | A los 0,5 s, `Próximo: Trabajo` abre su ancho y llega tenue (0,546 al tercer cuadro) y late con el reloj común. A los 10,5 s (`0:05`), la cuenta crece con el resorte y su color entra por la rampa, sin saltar (el reloj común está en p = 0,752). A los 15,5 s, el tipo y la pastilla cruzan con la subida de 8. SPM y Watts toman el borde, el halo, el hueco y `obj.` en 240, sin empujón. Watts pasa a rojo en 150 con la flecha. `Próximo` sale en 120 y su lugar se cierra en 280. **Ninguna caja salta entre dos cuadros** |
| VID-02 | El color de SPM cruza en 150 (0,053 / 0,350 / 0,851 / 0,989 en los cuadros 1 a 4). La flecha sube desde 4 dp en 120. Un solo empujón (pico 1,0398 en el cuadro 3); el segundo cruce, antes de 4 s, sin empujón. El número es siempre el de la lectura |
| VID-03 | `Reanudar` en el cuadro del toque. El diálogo entra (0,96 → 1) y sale (0,98). El inmersivo se funde (0,995 / 0,961 / 0,872 / 0,702 / 0,421 / 0) y aparece Workout, con la llave volviendo al carril izquierdo |

---

## 9. Contraste verificado (DN4a-09)

### 9.1 Pares para `ContrastTests.ItFourADesignerPairs` (WP-D0)

Con la sintaxis de `ContrastTests.Stack`: la pila va de abajo hacia arriba y `white` es el peor fondo del vidrio. El frente con `@NN` usa el mismo parseo que las capas (`Layer`). Los valores van a tres decimales, para `ItFourAControlValuesMatchTheDesignNotes`.

| # | Frente | Fondo | Umbral | Valor |
|---|---|---|---|---|
| H1 | `state.dangerText` (texto y contorno de `Terminar`) | `white\|glass.fallback` | 4,5 | 5,077 |
| H2 | `state.dangerText` (`Terminar` con *hover* y presionado, capa oscura) | `white\|glass.fallback\|overlay.scrim@12` · `@24` | 4,5 | 5,483 · 5,892 |
| H3 | `text.primary` (`Pausa`, título libre, cuenta) | `white\|glass.fallback` | 7 | 13,493 |
| H4 | `text.primary` (`Pausa` con *hover*) | `white\|glass.fallback\|white@4` | 7 | 11,944 |
| H5 | `text.onAccent` (`Reanudar`; *hover*; presionado) | `state.success` · `\|white@4` · `\|white@8` | 4,5 | 9,985 · 10,172 · 10,367 |
| H6 | `step.work.hud` · `step.rest.hud` · `step.warmup.hud` · `step.cooldown.hud` (tipo de paso; cuenta por terminar) | `white\|glass.fallback` | 4,5 | 4,577 · 7,154 · 9,358 · 4,622 |
| H7 | `text.primary` (pastilla de objetivo; tipo de `Próximo`) | `white\|glass.fallback\|step.<tipo>@20` (trabajo, descanso, calentamiento, enfriamiento) | 4,5 | 10,962 · 9,013 · 8,134 · 10,769 |
| H8 | `metric.distance` (tendencia en la pastilla) | ídem | 3 | 5,700 · 4,687 · 4,230 · 5,600 |
| H9 | `text.secondary` (`Próximo:`) | ídem | 4,5 | 6,301 · 5,369 · 4,977 · 6,212 |
| H10 | `text.secondary` (`obj.`) | `white\|glass.fallback` | 4,5 | 7,448 |
| H11 | `metric.watts.hud@80` · `metric.distance.hud@80` (unidades) | `white\|glass.fallback` | 4,5 | 6,678 · 5,157 |
| H12 | `metric.spm` · `metric.split` · `metric.watts` · `metric.kcal` · `metric.hr.hud` (rótulos) | `white\|glass.fallback` | 4,5 | 5,474 · 6,572 · 9,557 · 7,154 · 4,577 |
| H13 | `state.offTarget.hud` (flecha) | `white\|glass.fallback` | 3 | 4,594 |
| H14 | `metric.spm@70` · `metric.split@70` · `metric.watts@70` (borde con objetivo) | `white\|glass.fallback` | 3 | 3,427 · 3,979 · 5,480 |
| H15 | `metric.time@70` · `metric.distance@70` (íconos de la banda) | `white\|glass.fallback` | 3 | 5,856 · 4,239 |
| H16 | `accent.primary` (`rowing`) | `white\|glass.fallback` | 3 | 5,474 |
| H17 | `text.primary` (anillo de foco del HUD) · `overlay.scrim@50` (su halo) | `white\|overlay.scrim@50` · `white` | 3 | 3,977 · 3,977 |
| W1 | `state.warning` (texto e ícono del aviso) | `bg.app\|state.warningBg` | 4,5 | 6,310 |
| W2 | `text.tertiary` (etiqueta, vacío, "en curso") | `bg.app` | 4,5 | 5,829 |
| W3 | `text.tertiary` (*chevron*; con el *hover* de la tarjeta) | `surface.card` · `surface.card\|white@4` | 3 | 5,140 · 4,785 |
| W4 | `text.onAccent` (`Entrenamiento libre`) | `accent.primary` | 4,5 | 7,641 |

**Con margen de render** (contra `#2B3038`, el peor vidrio renderizado + 1; el criterio de la It. 3):
- `Terminar` 4,993;
- `obj.` 7,352;
- `W` 6,584 y `m` 5,086;
- el texto blanco de la pastilla de trabajo 10,805, `Próximo:` sobre calentamiento 4,919 y la tendencia sobre calentamiento 4,168.

Los valores del HUD ya los prueba `HudKeepsItsThresholdOnTheRenderedWorstGlass` (fuera de objetivo 4,518 y blanco 13,269).

Los diálogos y la tarjeta de rutina son los de la It. 3 (R12, R14 y R26) y no se repiten.

### 9.2 Excepciones (no se prueban como umbral)

- **Lo que el legacy o la biblia traían y se corrige.** No son excepciones: se cambian. Las pastillas y `Próximo` son la DV-38 y las unidades, la DV-31, las dos aceptadas por el PO en el gate; lo demás es del sistema visual.

  | Qué | Contraste | Corrección |
  |---|---|---|
  | Texto de la pastilla en su color sobre su tinte | 3,719 (trabajo) y 3,688 (enfriamiento) | blanco |
  | `Up next:` al 60 % sobre el 25 % | 3,758 y 4,061 | 70 % sobre el 20 % |
  | Tipo siguiente en su color sobre el 25 % | 3,469 a 4,948 | blanco |
  | Unidades al 55 % del color base | 3,967 (`W`) y 3,188 (`m`) | `.hud` al 80 % |
  | `Terminar` con la capa blanca del 4 % | 4,494 | capa oscura |
  | Anillo cian sobre blanco | 2,465 | blanco con halo oscuro |

- **El contorno de `Pausa`** (`border.strong`, 2,161:1). AC-WKT-20-01 lo fija. El texto y el ícono identifican el control, como en los botones de contorno de la It. 2.
- **Decorativos.** Los bordes de reposo de 1 dp al 10 % y al 8 %, el borde de `Próximo` al 50 %, el divisor de la banda, el halo, el contorno de la zona del bote y el velo.
- **Deshabilitados** (0,38 y 0,50) y **presionados**, que duran lo que dura el toque.

Con la DV-38 aceptada, sale la excepción del punto más tenue del latido del legacy (≈ 3,0 y ≈ 4,5): en la app el texto de `Próximo` no late.

---

## 10. Pedidos al Architect

Ninguno cambia el comportamiento del producto. Los marcados **[contrato]** cambian 02-tech-design §9.5 y conviene resolverlos antes de WP-B1 (reposo, tarjetas, banda) o de WP-B2 (fila del paso, horizontal).

| ID | Para | Qué | Por qué |
|---|---|---|---|
| DN4a-13 | WP-A (`HudGeometry`) [contrato] | La disposición de §3.1 y los topes de §3.7:<br>- `HudArrangement`: en horizontal, kcal y BPM en la fila de abajo, con kcal en el rincón; ancho mínimo por ventana; la banda compacta en horizontal.<br>- `BoatCenterY` con la fórmula de la cámara (§3.6).<br>- T1 (la fila de arriba se reparte en proporción) y T2 (la zona del bote arriba).<br>- T3 y T4 con la llave.<br>`HudGeometry` recibe los anchos naturales que mide cada `FitBox`. Pruebas: los rectángulos de §3.8 (± 0,1) y las escalas de §3.7 (± 0,005) | DN4a-01 y DN4a-02. Sin T1 y T3, con los extremos de FTMS SPM pisa Split en vertical y la banda pisa kcal |
| DN4a-14 | WP-A (`HudGeometry`, `OverlaySurface`, `WrenchPlacement`) y WP-B (`SimulatorLayerView`) [contrato] | En el inmersivo, el lugar de la llave lo calcula `HudGeometry` (§3.9: vertical, sobre la columna con BPM; horizontal, (a) o (b) por ventana). La página lo declara en `OverlaySurface` (p. ej., `Slot`), y `WrenchPlacement` lo usa tal cual en `OverlayContextKind.Workout`; los obstáculos y el techo quedan para los demás contextos. El panel crece desde la esquina de abajo a la derecha de la llave. El tope de Vatios aplica en vertical y está **siempre encendido**: Watts con su objetivo, por ventana (§3.9; el PO lo aceptó, AC-BLE-16-03). Si la llave cambia de lugar con el panel abierto, el panel la sigue con el movimiento de §5.9 (AC-BLE-16-02). `WrenchSpot.Clear` deja de ser `false` en L640. Pruebas: las llaves de §3.8 y los altos máximos de §3.9 | Hace lugar en L640 y deja el hueco de los avisos. Cambia D-ART-10 para el inmersivo, que el PO ya integró (AC-BLE-16-02, -03; DV-39) |
| DN4a-15 | WP-B (`HudMetricCard`) [contrato] | `.rm-hud-card__arrow` como **hueco de 0,48 em**, presente con objetivo desde que empieza el paso, con ▲/▼ adentro (Inter 700 a 0,4 em, pegado a la izquierda, con la tinta centrada en la banda de las mayúsculas: líneas de base +0,196 / +0,214 em). `#hud-target-*` con `margin-left` igual al hueco. Unidades en `.hud @80 %` por código | El número no se corre al cruzar el umbral (§3.2). Las unidades al 55 % no llegan a 4,5 |
| DN4a-16 | WP-B (`StepInfoBar`) [contrato] | La pastilla de objetivo con el texto en `text.primary` y `trending_up` en `metric.distance`. `#hud-up-next` con el fondo `@20 %`, el borde `@50 %`, `Próximo:` en `text.secondary` 400 y el tipo siguiente en `text.primary` 700. El latido va en un hijo **`.rm-hud-up-next__bg`** con el fondo y el borde, y el texto queda al 100 %. **Aceptado por el PO** (DV-38; AC-WKT-14-03, -04) | §4.5, §9 |
| DN4a-17 | WP-B (`OffTargetMotion`, `StepPulse`, `ImmersiveTransition`, `HudView`) | La motion de §5:<br>- **el reloj común**, con la rampa de color;<br>- **la flecha**, que se desplaza en su sentido;<br>- **el empujón**, solo dentro de un paso y con espera de 4 s;<br>- **el cambio de paso** de §5.5, con el alto de `obj.`, el hueco y el borde;<br>- **BPM y kcal** (FLIP) de §5.6;<br>- **el alcance** de §5.10. **Gate:** si en el primer cuadro de vuelta la sesión ya está en reposo, el inmersivo sale sin transición (un `Pop` sin fundido) y el aviso del fin entra en ese cuadro (AC-WKT-02-06, QA-G4a-P08).<br>Las constantes de §5.13 van en `MotionSpecs` (WP-A) | DN4a-05, DN4a-06 |
| DN4a-18 | WP-B (`HudBackdrop`) [contrato]; WP-D0 (tokens) | `#hud-backdrop` con `backdrop.water` y el hijo `.rm-hud-backdrop__sky` en `backdrop.sky` hasta 0,40 / 0,42 del alto; recalculado con la ventana. `#hud-boat-zone` con un contorno de 2 dp `dev.simulator`. **Gate:** con la variante de un solo elemento que toma el Architect (`BackdropTexture`, §7), la textura de 1 × 100 va con filtro `Point`, en sRGB, sin *mipmaps* y en `Clamp`, con los colores de `Tokens`, y el corte pasa de 40 a 42 en el acto al rotar (§4.3) | DN4a-03 |
| DN4a-19 | WP-B (`WorkoutScreenView`, `RoutineCardView`) [contrato] | `.rm-routine-card__chevron` (caja de 48, ícono 18, `align-self: center`, sin toques). Los tres estados centrados en lo que queda debajo del botón. El ícono del aviso, alineado con la primera línea. El aviso crece y colapsa con `GrowMotion` y `CollapseMotion`. **Gate:** fuera de reposo, `#workout-appbar` en `display: none` junto con `#workout-rest`, y `#workout-in-progress` en toda la pestaña, con `ShellLayout.RegisterBottomPadding(…, 0)` y el texto centrado; `WSC.InProgressTextOutsideIdle` lo comprueba (§4.1.5) | §4.1; AC-WKT-01-09 |
| DN4a-20 | WP-B (P2) | Al confirmar `Terminar`, `SetEnabled(false)` en los dos controles. Si el cierre tarda más de 300 ms, el *spinner* de 16 en `state.dangerText` a la izquierda de `Terminar`, con el mismo ancho | Con una base lenta (AC-WKT-06-13), hoy no hay señal de que el toque llegó |
| DN4a-21 | WP-B (arnés) | CAP-22 a los **87,6 s**. Las sondas de §8.2 con `WithProbe(…, aspect: true)` y las reglas de §8.1. CAP-29 con `#toast.yMin ≥ #hud-band.yMax` | Las fases de §5.4; el reloj de CAP-29 a la vista |
| DN4a-22 | WP-D0 (`tools/fonts`) y WP-B0 (`FontAssetBuilder`, `FontAssetPaths`) | La tabla K2 de **RowMate Numeric SC 700** en lugar de la de Barlow 700, porque `num.stat` pasa a la derivada. `kern_pairs.py` corre después de `freeze_tnum.py`. Si nada referencia los `FontAsset` de Barlow, salen de `FontAssetPaths.All` (los `.ttf` quedan para la receta) | DN4a-12 |

---

## 11. Para el PO y para QA

| Para | Qué | Propuesta |
|---|---|---|
| PO · D-ART-10 en el inmersivo (DT4a-11) | **Hecho en el gate:** está en AC-BLE-16-02 y -03 (DV-39), con el tope de Watts encendido. La propuesta era: al integrar D-ART-10, el texto de AC-BLE-16-02 para el entrenamiento pasa a: "*En el entrenamiento a pantalla completa*, en vertical, va a 16 dp del borde derecho y 8 dp por encima del lugar de las tarjetas kcal y BPM (no se mueve si el pulso aparece o se va); en horizontal, 8 dp a la derecha de `Pausa`, centrada en su alto, o, si a la derecha no queda lugar para kcal y BPM, 8 dp por encima de `Pausa`. En ningún caso tapa una pieza del HUD ni los controles." Lo demás de la propuesta de 00-deuda-diseno §5 no cambia | §3.9 |
| PO · DV-33 | **Hecho en el gate** (DV-33, AC-WKT-17-04, -05). Era precisar: "En horizontal, kcal y BPM van en fila a la altura de los controles, abajo a la derecha (kcal en el rincón), y la banda, con sus espacios internos más cortos. Nada se encima; lo que no entra se achica (en 640 × 360, la banda)" | §3.1, §3.7 |
| PO · **DV-38** (era "DV nueva"): pastillas de la barra de pasos | **Aceptada en el gate** (AC-WKT-14-03, -04; CAP-19, CAP-21, CAP-27c). **Legacy:** el texto de la pastilla de objetivo y el tipo de `Up next` van en el color del tipo, con `Up next:` al 60 % sobre el 25 %, y la pastilla entera late de 0,6 a 1. **App:** esos textos van en blanco, `Próximo:` al 70 %, el tinte al 20 %, y laten el fondo y el borde, con el texto siempre entero. **Motivo:** contraste en el peor fondo (3,5-3,7:1 los textos de color; `Próximo:` 3,8-4,1; ≈ 3,0 en el punto tenue del latido). Cambia AC-WKT-14-03 ("texto de 11/600 **en blanco**") y AC-WKT-14-04 (tinte 20 %, `Próximo:` al 70 %, el tipo en blanco, "**el fondo y el borde** van de 0,6 a 1,0") | §4.5, §9 |
| PO · DV-31 | **Hecho en el gate** (DV-31; AC-WKT-17-03, -04). Era sumar: "las unidades `W` y `m` en la variante `.hud` de su métrica al 80 % (el legacy, el color al 55 %: 3,2-4,0:1)" | §2.2 |
| PO · AC-WKT-17-09 y AC-SCN-01-01 (informativo) | La zona del bote con su centro en la proyección del remero (0,504 / 0,652 del alto) y el fondo de dos tonos del mediodía. Las dos cosas ya las deja fijar la spec | §3.6, §4.3 |
| PO · spec §7.3 CAP-22 | "A 3 s del final" sigue: a los 87,6 s la cuenta muestra `0:03`. Es el instante del arnés, para que salga teñida | §5.4 |
| PO · DT4a-01, DT4a-03, DT4a-04 | De acuerdo con los tres | §1 |
| PO · AC-WKT-01-09 (precisión, gate) | Dice que "en curso" "se alcanza a ver solo mientras el inmersivo entra o sale". Se ve solo mientras **entra**: al salir, la sesión ya volvió a reposo y debajo está Workout en reposo, con su barra (AC-WKT-13-04). Así es también en el legacy, que cierra la página después de `reset()` (`immersive_workout_screen.dart:57-65`, `:141-147`). No cambia ningún comportamiento; evita una prueba que espere "en curso" en la salida | §4.1.5 |
| PO · informativo | No son diferencias nuevas, porque son del sistema visual (spec §11.1, párrafo inicial):<br>- el hueco fijo de la flecha y `obj.` alineado con las cifras;<br>- el anillo de foco del HUD;<br>- la capa oscura de `Terminar`;<br>- los controles apagados al confirmar;<br>- los 180 ms del rodillo en el reloj.<br>La llave arriba de kcal también estaba en esta lista: ahora la declara la DV-39 | — |
| QA | Los pares de §9.1 y las excepciones de §9.2. Las sondas y las reglas de toma de §8; las escalas de §3.7 como parte de lo esperado; `WrenchAudit` con los lugares de §3.8. En los videos, los valores por cuadro de §5. En [Device-Win], el anillo de foco del HUD con Tab sobre los dos fondos | — |

---

## 12. Plan de WP-D0 y WP-D

**WP-D0** (antes de WP-B0; datos, fuentes y pruebas):

| Archivo | Qué | Hecho cuando |
|---|---|---|
| `Assets/RowMate/Core/Design/Tokens.cs`, `Presentation/Styles/Tokens.uss` (G) | `backdrop.sky` y `backdrop.water` (133 → 135) | `tools/tokens --check` en 0; `TokensUssSyncTests` y los conteos en verde |
| `Assets/RowMate/Tests/EditMode/Design/ContrastTests.cs` | `ItFourADesignerPairs` (H1 a H17, W1 a W4) y `ItFourAControlValuesMatchTheDesignNotes` (los valores de §9.1 a 3 decimales) | dotnet en verde |
| `UI/Fonts/RowMateNumericSC/*`, `tools/fonts/*`, `UI/Fonts/Kerning/*.pairs.json` | DD4a-06 y DD4a-07 (02-tech-design §9.1), con K2 sobre la derivada 700 (DN4a-22) | la receta reproducible; las pruebas de 02-tech-design §6.14 |
| `docs/migration/design/ASSET_SLOTS.md` §3.7 | la derivada integrada; sale la rama "si no trae `tnum`" | — |

**WP-D** (después de B3, con el arnés `it4a`): `Skin.uss` y `Motion.uss` para las clases de §9.5 y lo de §4.6 y §5.12 (capas de los controles, anillo del HUD, tarjetas de Workout, aviso, estados), y `SimulatorSkin.uss` sin cambios. Además:
- el visto bueno de las sondas de §8.2;
- los videos cuadro por cuadro;
- las biblias (§13) y LEGACY_VISUAL_AUDIT §8 (DV-33).

Orden: CAP-12, CAP-19 y CAP-21 primero (son el HUD), después el horizontal (CAP-15, -16, -17, -23, -24, -25), el reposo (CAP-01, -02, -07), los diálogos y los avisos (CAP-08, -26, -29) y los videos al final. Hecho = 0 desbordes, `HudOverlap` y `WrenchAudit` en verde y todas las sondas dentro de la tolerancia.

**Estado (2026-10-28):** WP-D0 y WP-D hechos; qué se hizo, qué se midió y qué queda, en §17. La integración de la derivada y de las listas K2 en el *bootstrapper* (WP-B0, 02-tech-design §6.14) no está: las capturas de la 4a siguen dibujando las cifras de Barlow en celdas y el texto con las tablas parciales de las It. 2 y 3 (§17.6).

---

## 13. Alineación de las biblias (en WP-D, con este documento como fuente)

| Dónde | Cambio |
|---|---|
| ART_BIBLE §2.1 | 135 tokens: `backdrop.sky`, `backdrop.water` |
| ART_BIBLE §3.2 | La anatomía de la tarjeta (hueco de la flecha de 0,48 em con objetivo, la flecha centrada en la banda de las mayúsculas, `obj.` alineado con las cifras). Las unidades en `.hud @80 %` (salen del 55 %). La banda compacta en horizontal |
| ART_BIBLE §3.3 | La barra de la 4a (40 de alto, una fila). Las pastillas con texto blanco y `trending_up` en `metric.distance`. `Próximo:` / `Up next:` localizado (DV-24; spec §15), al 70 % sobre el 20 %, con el latido en el fondo y el borde (DV-38) |
| ART_BIBLE §3.5 | El diálogo de terminar es el de la app, opaco (sale "`surface.overlay @92 %` + vidrio" para el de terminar). Estados de los controles del HUD: la capa oscura de `Terminar`; el anillo de foco blanco con halo oscuro pegado; apagados al confirmar |
| ART_BIBLE §3.7 | El *chevron* de Workout centrado en la columna de 28 dp |
| ART_BIBLE §3.10 | La vista "en curso" de Workout: solo el texto, centrado en toda la pestaña, sin la barra superior (paridad; §4.1.5) |
| ART_BIBLE §3.11 y §5.2 | La llave en el inmersivo (§3.9). El "hueco de la llave": en vertical, sobre kcal y BPM; en horizontal, junto a `Pausa`. La disposición horizontal (sale "Horizontal, pendiente de la 4a"). El centro de la zona del bote con la fórmula de la cámara |
| ART_BIBLE §4.4 | K2 sobre la derivada 700 en lugar de Barlow 700; K4 para la derivada 600 y 800 |
| ART_BIBLE §10.2 | La tabla de §6, con la cuenta y las flechas |
| ANIMATION_BIBLE §2.4, §2.6 | El rodillo de 180 también en el reloj y en la cuenta. El reloj común de 600 + 600 con su origen, la rampa del color y el latido en el fondo de `Próximo` |
| ANIMATION_BIBLE §2.7, §2.8 | El cambio de paso de la 4a (sin píldoras) con sus tiempos. La flecha que se desplaza en su sentido. Sin empujón al empezar un paso |
| ANIMATION_BIBLE §2.9, §2.11 | BPM y kcal (§5.6). La entrada y la salida de la 4a por fundido (240 / 200 / 120), con "en curso" debajo en la entrada y Workout en reposo en la salida; sin transición si la sesión ya terminó al volver del segundo plano (§5.10). La llave en el inmersivo y el panel abierto que la sigue (§5.9) |
| ANIMATION_BIBLE §2.12 | La rotación, en la 4a, en el acto; el fundido cruzado de 200, desde la 6a |
| LEGACY_VISUAL_AUDIT §8 | DV-33, el defecto del legacy (kcal sobre Watts en un teléfono horizontal) |

---

## 14. Qué queda abierto

- **NO VERIFICABLE hasta WP-B (sonda S-B4a):**
  - la escala por zona: nitidez, *hit-test* y `FitBox` adentro;
  - las métricas reales de ▲ y ▼ de Inter 700 en UI Toolkit, para la línea de base de §3.2;
  - el `GlowTexture` a 1,6;
  - el alto medido de cada tarjeta contra el de §3.2 (± 1 dp).
- **Ya no quedan abiertas** las decisiones del PO, que las tomó en el gate:
  - la DV-38 (las pastillas);
  - la precisión de DV-31 y DV-33;
  - D-ART-10 en el inmersivo (DV-39), con el tope de Watts encendido.
- **Pendiente del Designer en 00-deuda-diseno** (spec §15). En el inmersivo vale §3.9 de este documento; falta precisar allá dos puntos de las pestañas:
  - §1.7, el alto máximo del panel en las pestañas. AC-BLE-16-03 lo fija en los valores de la It. 2 (360 → 248, 914 → 802), y la fórmula de §1.7 da 264 y 818;
  - §1.6, el panel abierto que sigue a la llave entre una pestaña y una pantalla apilada (AC-BLE-16-02).
- **L640.** La banda se achica con valores normales (0,776) y mucho con los largos (0,561). Es la ventana más chica, y el legacy ahí encima las piezas. Si en un teléfono real horizontal (≥ 780 dp) también se achicara, se revisa en la 10a, con el equipo.
- **[Device-And]:** las barras del sistema visibles en el inmersivo (la 4a; se ocultan en la 4b) y los márgenes seguros, que la geometría ya suma.
- **6a:** la zona del bote ya es la de la cámara. El fondo provisorio se va, o queda mientras carga la escena. Los *scrims* llegan con la escena.

---

## 15. Verificación hecha

1. **Legacy, solo lectura.** Las medidas, colores y pesos del HUD y del reposo salen de los archivos y líneas del encabezado, citados en cada sección; además, `docs/image-02.png` y `docs/image-03.png` (el horizonte y el remero de §3.6). `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar, y no se leyó `strava_secrets.dart`.
2. **Anchos de texto** (`measure.py`). fontTools 4.68.0 sobre las fuentes versionadas: avances de `hmtx`, pares GPOS `kern` (formatos 1 y 2, gana el primer subtable de cada lookup; es K2) y `letter-spacing` de UI Toolkit (N/100 em por letra). Cifras con las `*.tf` de Barlow (0,521 y 0,535 em, iguales en las diez), `:` y `.` en 0,32 em. Los glifos ▲ y ▼ de Inter 700 (`triagup` y `triagdn`), con su avance y su tinta.
3. **Geometría** (`hud_layout.py`). 5 perfiles × 6 casos (libre, libre con BPM, rutina con objetivos, `Pirámide 500`, valores largos de CAP-16 y extremos de FTMS) × con y sin simulador: **60 combinaciones**. Ninguna pieza se cruza con otra, ninguna sale de la ventana, ninguna pieza del HUD entra en la zona del bote, y la llave queda a 8 dp de todo. De ahí salen §3.7 y §3.8. Los avisos del inmersivo, con `toast.py`.
4. **Contrastes** (`contrast.py`, `contrast2.py`). El método de `Contrast.cs`: mezcla sRGB sin redondear, luminancia WCAG y relación sin redondear. Son los 21 grupos de §9.1, los descartados de §9.2 y las alternativas de capa de `Terminar`, del latido y del anillo. Los compuestos de las sondas, con `probes.py`.
5. **Curvas** (`motion.py`). Las cubic-bezier de `Tokens.cs` con el método de `Easing.cs` (Newton y bisección) y la solución exacta de `CriticalSpring` (ζ = 1): las tablas de §5.
6. **Sistema construido, leído sin tocarlo:** `Tokens.cs`, `MotionSpecs.cs`, `Contrast.cs`, `Easing.cs`, `CriticalSpring.cs`, `Icons.cs` (están `rowing`, `timer`, `straighten`, `favorite`, `warning`, `play_arrow`, `pause`, `chevron_right`, `trending_up`, `refresh`), `TabularNumber.cs`, `Base.uss` (anillo de foco, botones, diálogos, avisos) y `Skin.uss`.
7. **El repositorio.** Solo se escribió este documento: ningún token, código, asset, captura ni biblia. Unity no se abrió y dotnet no se corrió. Los programas quedaron en `<scratch>/it4a/designer-notes/`, sin versionar.

---

## 16. Cambios por gate QA

Correcciones por los dos gates de armonía de QA de la 4a, del 2026-10-28: [lente de paridad](03-qa-harmony-paridad.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 3 mayores, 7 menores) y [lente técnica](03-qa-harmony-tecnica.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 5 mayores, 13 menores). A este documento le tocaba un hallazgo, visto por las dos lentes: **QA-G4a-P09** y **QA-G4aT-17**, los dos menores. Lo demás es alineación con la spec corregida ([01-po-spec §16](01-po-spec.md)) y con [02-tech-design](02-tech-design.md), que el Architect estaba corrigiendo en paralelo. De ese documento se alineó lo que ya decía: el fondo de un solo elemento (la variante de presupuesto de DN4a-18) y el panel que sigue a la llave. Lo nuevo para el Architect va en los pedidos de §10. No cambia ningún token, color, contraste, medida del HUD ni tiempo que ya estaba: la única medida que se mueve es el centro del texto "en curso", al quitar la barra.

| Issue | Severidad | Qué cambió | Dónde |
|---|---|---|---|
| QA-G4a-P09, QA-G4aT-17 | menor | **Paridad, como ya dice la spec (AC-WKT-01-09).** Fuera de reposo, `#workout-appbar` pasa a `display: none` junto con `#workout-rest`. `#workout-in-progress` ocupa toda la pestaña, del borde de arriba a la barra de pestañas, con el texto centrado: en TV, en y 425,0 en lugar de 453,0. Es la `_ActiveView` del legacy, un `Scaffold` sin `AppBar` (`workout_screen.dart:180-201`). Precisión: se ve solo en la **entrada**; en la salida la sesión ya está en reposo, como en el legacy, que cierra la página después de `reset()` | §0, §4.1, §4.1.2, §4.1.5 (nuevo), §5.1, §10 DN4a-19, §11, §13 |

**No disputo el issue.** Las dos salidas eran la paridad o una DV más. Elijo la paridad por tres motivos:
- es lo que la spec ya pide;
- cuesta un `display: none` y el relleno de abajo que `ShellLayout` ya sabe poner;
- la barra conservada no daba continuidad: durante la entrada, `Entrenamiento` quedaba debajo del título de la barra del HUD (`Entrenamiento Libre` o `Calentamiento`, a la misma altura) y en los dos primeros cuadros se leían dos títulos encimados (§4.1.5).

**Alineación** (no son issues de este documento):

| Origen | Qué cambió | Dónde |
|---|---|---|
| QA-G4a-P03 y QA-G4aT-02: la DV-38, aceptada | Las pastillas y `Próximo` de §4.5 quedan como estaban y dejan de ser una propuesta. Sale la excepción del latido del legacy de §9.2 | §0, §4.5, §5.4, §9.2, §10 DN4a-16, §11, §13, §14 |
| QA-G4a-P04: DV-31 y DV-33, precisadas | Las dos filas de §11 quedan hechas | §11, §14 |
| QA-G4aT-02: D-ART-10 integrada (DV-39) | El tope de Watts con su objetivo, **siempre encendido** en vertical. El alto máximo del panel por perfil: TV 316,4, P360 274,6, TH 368,6, L640 269,5 y W1280 653,6. Lo que tapa el panel abierto en horizontal: en (a), la parte de arriba de `Pausa`, y DEMO-04 lo cierra antes de pausar. El panel abierto sigue a la llave (AC-BLE-16-02): sale en 90 y se vuelve a abrir en el lugar nuevo al terminar la transición | §3.9, §5.9, §8.3 DEMO-03, §10 DN4a-14, §11, §14 |
| QA-G4a-P02 y QA-G4aT-04: AC-WKT-20-07 | La rutina que termina con el diálogo de terminar abierto: el diálogo sale como con `Cancelar` y los controles quedan al 0,38. Con la base al día, las dos salidas se pisan | §5.7 |
| QA-G4a-P08: AC-WKT-02-06, -08 y -09 | El aviso del fin al volver del segundo plano o de la ventana minimizada: una sola vez, con sus 4 s desde que Workout en reposo está a la vista. Si el cierre todavía no volvió, primero se ve la rutina terminada. Si ya volvió, el inmersivo sale sin transición (pedido nuevo en DN4a-17) | §4.8, §5.10, §10 DN4a-17 |
| QA-G4aT-12 (b) | Sin cambios: VID-02 ya esperaba dos cruces en menos de 4 s, con el empujón solo en el primero, y la spec §7.5 lo adoptó | §8.4 |
| QA-G4aT-14 (b) | Lo que crece desde 0 recorta, con el contenido ya a su tamaño final: el texto no se aplasta ni se reacomoda. Es un requisito de aspecto; el patrón lo arma el Architect | §5.0 |
| O-01 del gate de paridad | El origen del reloj de pulsos queda corrido de fase frente al legacy, que lo arranca con la barra. No se ve | §5.4 |
| QA-G4a-P05 | Sin cambios: §4.7 ya era el diálogo opaco que tomó la spec (AC-WKT-20-03) | §4.7 |
| 02-tech-design: la variante de presupuesto de DN4a-18 (QA-G4aT-01, gate técnico §9) | El Architect toma el fondo de un solo elemento con la textura de 1 × 100 (`BackdropTexture`). Se suman los requisitos de aspecto para que se vea igual que con dos elementos: filtro `Point` (horizonte neto), sRGB sin *mipmaps*, colores de `Tokens` y el corte que cambia al rotar. La cuenta de elementos vuelve a 59 | §4.3, §7, §10 DN4a-18 |

**Para el Architect.**
- DN4a-19 suma la barra oculta fuera de reposo y su prueba.
- DN4a-17 suma el `Pop` sin transición al volver con la sesión ya cerrada.
- DN4a-14 suma el tope de Watts encendido y el panel que sigue a la llave.
- DN4a-18 suma los requisitos de la textura del fondo, con la variante que ya tomó.
- DN4a-16 deja de esperar al PO.
- Los demás pedidos de §10 no cambian.

**Pendiente para otro documento.** 00-deuda-diseno §1.6 y §1.7 (spec §15), en las pestañas: el alto máximo del panel y el panel abierto entre una pestaña y una pantalla apilada (§14). No se tocó en esta corrección.

**Verificación del gate.**
- **Legacy, solo lectura.** Releí:
  - `workout_screen.dart:19-35` y `:160-205`: la `_ActiveView`, sin `AppBar`;
  - `immersive_workout_screen.dart:40-147`: el `pop` después de `reset()`;
  - `immersive_workout_screen.dart:300-330`: el reloj de pulsos (O-01);
  - `lib/main.dart:116-175`: el `Scaffold` de la app, con la barra de pestañas abajo;
  - `lib/shared/theme.dart:13`: `#0D1B2A`.

  `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar, y no se leyó `lib/core/strava/strava_secrets.dart`.
- **La app, leída sin tocarla.**
  - `ShellLayout.cs`: `RegisterBottomPadding` y el alto de la barra que se conserva mientras está oculta;
  - `AppBarView.cs`, `PageStack.cs` y `PageTransition.cs`;
  - `VerticalFadeTexture.cs`, la textura por código que toma de modelo la del fondo;
  - en `Base.uss`, `.rm-tab-content`, `.rm-screen` y `.rm-tab-bar`;
  - el catálogo (`workoutInProgress`).
- **Medidas** en `<scratch>/it4a/designer-notes/gate/`:
  - `inprogress.py`, con la clase `Face` de `measure.py` (fontTools 4.68.0, `uv run --offline`): los anchos de §4.1.5, el centro por perfil, el cruce de los dos títulos y el peso de la pestaña con `ease.enter`;
  - `panel.py`: el alto máximo y lo que tapa el panel abierto, sobre los rectángulos de §3.8.
- **El repositorio.** Solo se escribió este documento. Unity no se abrió y dotnet no se corrió.

---

## 17. WP-D0 y WP-D: implementación y visto bueno

Hecho el 2026-10-28, después de WP-A y WP-B, con el arnés `it4a` (`tools/unity/capture-snapshots.ps1 -Set it4a`, `Snapshots/WorkoutCatalog.cs`) y mirando cada una de las 46 fotos y los 3 videos (cuadro por cuadro, con PyAV y Pillow sin red, en `<scratch>/it4a/wp-d/`, sin versionar). El legacy, solo lectura: `git -C ../rowmate-legacy status --porcelain` vacío antes y después; no se leyó `strava_secrets.dart`.

### 17.1 Qué se entregó

| Paquete | Archivo | Qué |
|---|---|---|
| WP-D0 | `Assets/RowMate/UI/Fonts/RowMateNumericSC/RowMateNumericSC-{SemiBold,Bold,ExtraBold}.ttf`, `OFL.txt` | La derivada de DD4a-07 (sus `.meta` los creó el Editor) |
| WP-D0 | `tools/fonts/freeze_tnum.py`, `kern_pairs.py`, `README.md` | La receta, con fontTools 4.68.0 por `uvx --offline`; primero la derivada, después las listas (DN4a-22) |
| WP-D0 | `Assets/RowMate/UI/Fonts/Kerning/{Inter-Regular,Inter-Medium,Inter-SemiBold,Inter-Bold,RowMateNumericSC-Bold}.pairs.json` | Las listas K2 (formato en `tools/fonts/README.md`) |
| WP-D0 | `Tests/EditMode/Design/ContrastTests.cs` | `ItFourADesignerPairs` (H1 a H17 y W1 a W4 de §9.1: 46 casos; el frente también puede llevar `@NN`) e `ItFourAControlValuesMatchTheDesignNotes` (los valores de §9.1 a tres decimales, los corregidos de §9.2 y los del margen de render contra `#2B3038`) |
| WP-D0 | `docs/migration/design/ASSET_SLOTS.md` | §1.6, §3 puntos 3, 5 y 7 (sale la rama "si Barlow no trae `tnum`") y "Cambios de la It. 4a" |
| WP-D0 | `Core/Design/Tokens.cs`, `Tokens.uss` | Sin cambios: `backdrop.sky` y `backdrop.water` ya estaban (135) |
| WP-D | `Presentation/Styles/Skin.uss` | La capa **oscura** de `Terminar` (`overlay.scrim` 12 / 24 %), el radio de la capa dentro del contorno de 1,5 (10,5) y el **anillo de foco del HUD** (blanco con el halo `overlay.scrim` al 50 %, a −4 del borde de afuera también con el contorno: −5,5 desde adentro) |
| WP-D | `Presentation/Styles/Motion.uss` | Sin reglas nuevas: los controles del HUD y Workout toman las de la It. 2 y la 3 (la nota lo dice) |
| WP-D | `SimulatorPresentation/Styles/SimulatorSkin.uss` | Sin cambios. La llave del inmersivo vive en la capa del simulador y conserva el anillo cian de la app: 3,363:1 sobre el agua provisoria |
| WP-D | `docs/migration/design/ART_BIBLE.md`, `ANIMATION_BIBLE.md`, `LEGACY_VISUAL_AUDIT.md` | La tabla de §13 en el cuerpo y su sección "Cambios de la It. 4a" (LEGACY_VISUAL_AUDIT §8, hallazgo 17: DV-33) |
| WP-D | este documento | §5.4, §5.5, §8.2, §12 y esta sección |

Lo que hubo que tocar en archivos de WP-B para que lo que se ve cumpla el contrato de §5 y de 02-tech-design §6.11 y §9.5 (va como desvío en el informe; §17.5): `Presentation/Workout/HudCard.cs`, `HudZones.cs`, `StepInfoBar.cs` y `Snapshots/WorkoutCatalog.cs`.

### 17.2 WP-D0, verificado

- **La derivada.** `freeze_tnum.py` da en las tres caras las diez cifras en `zero.tf` … `nine.tf` con un solo avance (507, 521 y 535 unidades) y la familia "RowMate Numeric SC" en los nombres 1, 3, 4, 6, 16 y 21. Frente a la receta verificada en 00-deuda-diseno, todas las tablas iguales salvo `head` (la fecha: ahora la de Barlow). Dos corridas seguidas: los mismos bytes.
- **Las listas K2.** Juego de 116 caracteres (115 con glifo: el `📈` no está en Inter). 1 445 pares en Inter 400, 1 695 en Inter 500, 600 y 700, y 1 272 en la derivada 700 (Barlow 700 sin los pares de cifras). Contra las tablas que TextCore había escrito en los `.asset` de las It. 2 y 3: 2 882 pares en común, **0 con otro valor**, y ningún par de esas tablas (dentro del juego) que la lista no tenga. Dos corridas: los mismos bytes.
- **Contraste.** Los 46 casos de H1 a H17 y W1 a W4 sobre su umbral; los valores de control dan los de §9.1 a tres decimales (dotnet: 2 050 tests, 2 028 pasan, 22 ignorados de equipo).

### 17.3 Las 49 tomas

- **Sin problemas**: 0 desbordes, `HudOverlap` y `WrenchAudit` en verde en todas, 82 sondas (74 de aspecto), todas dentro de su tolerancia. Dos corridas seguidas dan los mismos bytes en las 46 fotos, los 3 videos e `INDEX.md`; en `manifest.json` cambia solo `durationMs` (QA-V3-06).
- **Sondas de aspecto nuevas** (las de §8.2 que el arnés no tenía; `WithProbe(…, aspect: true)`): CAP-12, los cuatro rótulos, la `m` al 80 % y el contorno de `Pausa`; CAP-14, el texto de `Reanudar`; CAP-19 y CAP-25, la pastilla al 20 %, `obj.` y los bordes al 70 % de Watts y SPM; CAP-20, el borde de Split y su ▼; CAP-26, el velo sobre una tarjeta, sobre el cielo y sobre el agua. Con un programa propio, además, CAP-01 (fondo y etiqueta), CAP-02 (`Entrenamiento libre` al 0,38 y la tarjeta al 50 %), CAP-07 (el tonal) y CAP-08 (el velo): todos dentro de ± 1.
- **Lo que se ve, contra §4 y §8.2**: las tarjetas con el hueco de la flecha toda la duración del paso, la flecha centrada en la banda de las mayúsculas y `obj.` empezando donde empiezan las cifras (CAP-19, -20, -24); las unidades en su `.hud` al 80 %; el fondo de dos tonos con el horizonte recto a 0,40 y 0,42; la barra de 40 con las pastillas en blanco y `Próximo` con el texto entero; la fila de abajo en horizontal con kcal en el rincón y la llave junto a `Pausa` o encima en L640 (CAP-15, -16, -17, -23, -24); los diálogos opacos de 331; los avisos sobre la llave y, en el inmersivo, en el hueco entre la banda y los controles con el reloj a la vista (CAP-29).
- **Cambian frente a las tomas de WP-B** solo DEMO-12 (el instante, §5.4) y VID-01 (el cambio de paso, §17.4); las otras 47 salen con los mismos bytes (`Skin.uss` solo cambia *hover*, presionado y foco, que el arnés no toca).

### 17.4 Los videos, cuadro por cuadro (30 fps)

- **VID-01.** `Próximo: Trabajo` entra a los 0,5 s: el ancho con el resorte y las etiquetas que se revelan sin encimarse (antes de WP-D, con el ancho chico se leía "PT" y "PróxT": §17.5). A los 10,5 s la cuenta crece hasta 1,1 y se tiñe con la rampa. A los 15,5 s el tipo y la pastilla hacen el relevo (0-120 y 120-260, con la subida de 8) y las tarjetas de SPM y Watts toman borde, halo, hueco y `obj.` con `ease.move`: el borde derecho de SPM recorre 0 / 0,075 / 0,275 / 0,65 / 0,90 / 0,975 / 1 de su camino en los cuadros 0 a 6 (las curvas de §5.5: 0,018 / 0,092 / 0,286 / 0,651 / 0,882 / 0,973), su borde de abajo baja igual, `obj.` se revela desde arriba y la tarjeta de Watts crece hacia la izquierda y hacia abajo. Watts pasa a rojo en 150 y su ▲ entra en los cuadros 8 a 11, al terminar el hueco. **Ninguna caja salta entre dos cuadros.**
- **VID-02.** El color de SPM cruza en 0,063 / 0,374 / 0,862 / 0,994 de blanco a rojo en los cuadros 1 a 4 (§5.3: 0,053 / 0,350 / 0,851 / 0,989; la diferencia es la compresión del video) y vuelve igual. La ▲ entra desde 1,75 dp abajo con opacidad 0,62 en el cuadro 1 y sale subiendo 2,4 dp. Un solo empujón: 1,029 / 1,039 / 1,040 / 1,037 / 1,019 / 1,002 (§5.3: 1,0299 / 1,0393 / 1,0398 / 1,0359 / 1,0187 / 1,0033); el segundo cruce, a 2 s del primero, sin empujón.
- **VID-03.** `Reanudar` en el cuadro del toque; el diálogo de terminar entra y sale con su transición; al confirmar, los controles al 0,38 en el acto, el inmersivo funde en 200 sobre Workout en reposo con su barra, y la llave vuelve al carril izquierdo al terminar (fundido).

### 17.5 Lo que se encontró y se corrigió

| # | Qué se veía | Por qué | Qué se cambió (archivo) |
|---|---|---|---|
| 1 | En VID-01, en el cambio de paso, las tarjetas de SPM y Watts **saltaban** en un cuadro a su tamaño con objetivo: hueco, borde, halo y `obj.` enteros de golpe (la spec pide "nada salta") | `HudCard.SetTarget` aplicaba todo en el acto, y `HudZones` ponía el tamaño nuevo de la tarjeta en el mismo cuadro | `HudCard`: `TargetDecor`, la coreografía de §5.5 (borde, halo, hueco, alto y texto de `obj.`, y la tarjeta que crece con ellos), en el acto al armar, con `Instant` y sin el reloj armado; la flecha de un paso que empieza fuera entra al terminar (240-360). `HudZones`: las tarjetas toman su tamaño con `HudCard.ApplyBaseSize` |
| 2 | `Próximo` creciendo desde 0: el tipo encima de `Próximo:` ("PT", "PróxT", "PróximoT") durante 5 cuadros | UI Toolkit mide un texto "a lo sumo" el ancho que le deja el padre, aunque vaya en `nowrap` y con `flex-shrink: 0`: las etiquetas se achicaban con la pastilla | `StepInfoBar.MeasureUpNext`: las dos etiquetas con `min-width` = su ancho natural; la pastilla las recorta (es el patrón que pide QA-G4aT-14) |
| 3 | La sonda del cielo de CAP-17 fallaba (daba el vidrio) | el punto de §8.2 (y = 70) caía sobre la barra en W1280, que llega a 76,8 | `WorkoutCatalog`: y = 69·s, entre la barra y las tarjetas |
| 4 | La sonda de la tendencia (CAP-19, -23, -25) daba blanco | la tendencia es un glifo dentro del texto de la pastilla, y el color dominante es el del texto | un punto del trazo de la flecha de `trending_up`, el mismo en TV, TH y W1280 (§8.2). Hay que volver a ubicarlo si cambia el ancho del texto (por ejemplo, con las tablas K2) |
| 5 | El borde de SPM en CAP-25 daba `#028DAD` (± 9) | a 1,6 la sonda a 0,002 del alto caía en la fila del borde que se mezcla con el halo | los tres bordes a 0,008 del alto (el medio de la línea en los cuatro perfiles) |
| 6 | DEMO-12 con la cuenta casi blanca (`#EDF5FA`), y la spec pide "teñida" | la ventana de la lectura del segundo 237 cae cerca del valle del reloj común (§5.4) | `WorkoutCatalog`: DEMO-12 a los 237,9 s sin la ventana (como CAP-22) y una comprobación de p ≥ 0,5: sale `#43A7CD` (p ≈ 0,95) |
| 7 | Con el reloj de prueba que avanza segundos de una vez, la flecha de #1 entraba recién en el primer cuadro y demoraba 120 ms la toma (CAP-22 salía en p ≈ 0,88) | la animación del hueco "terminaba" en ese cuadro | si el cuadro en que termina se pasó más que la entrada de la flecha (`OverrunMs`), la flecha queda en su lugar en el acto |

CAP-21 queda como estaba (con la fase tenue: §5.4), y lo que esperan las notas se corrigió en §8.2.

### 17.6 Lo que queda (no es de WP-D)

- **La integración de la derivada y de las listas K2 en el *bootstrapper*** (WP-B0, 02-tech-design §6.14): `FontAssetBuilder` con las listas, los tres `FontAsset` de la derivada, `FontAssetPaths` con el rol numérico en la derivada y Barlow fuera de `All`, `Typography.uss` regenerado, `ItFourAFontTests` y `FontAndIconAssetsTests`, y la regeneración única de `it2` e `it3` (y la de `it4a`). Sin eso, las capturas siguen dibujando las cifras de Barlow en celdas del ancho del `4` (el `1` con 0,12 em de aire de más por lado, CAP-12 y siguientes) y el texto con las tablas parciales de antes (`Te` de `Terminar entrenamiento` sin su par en CAP-26). WP-B dejó dos comprobaciones flojas por esto (CAP-15: los rectángulos de la banda y de Split; CAP-16: la escala de la banda, que con la derivada tiene que dar 0,561): se ajustan con la integración. El punto de la sonda de la tendencia (§17.5, #4) se vuelve a ubicar en esa misma regeneración.
- **El FLIP de kcal** de §5.6 (`HudView.BpmMotion`): BPM funde en su lugar, pero kcal cambia de lugar en el acto cuando BPM aparece o se va (en vertical, 71,7·s dp). Ninguna toma ni video lo muestra; queda para WP-B.
- **La pastilla de objetivo recortada** (CAP-21, CAP-27c): el "…" sale en el cian de la tendencia y la flecha no entra (`>160W · 22 spm · …`; estas notas esperaban `↗…`). TextCore pone el "…" con el estilo del primer carácter que saca, que es el glifo. Con las tablas K2 el texto se angosta y puede que la flecha entre; si no, WP-B puede cerrar el color antes del glifo o recortar antes de él. Es menor: el recorte se ve y el texto es blanco.
- **El relevo de la pastilla en el cambio de paso**: mientras `Próximo` cierra su ancho (80-280), la pastilla nueva entra recortada y se completa en dos cuadros. Es lo que pasa cuando hay lugar recién al irse `Próximo`; se acepta.

### 17.7 DN4a-01 a DN4a-12, en las capturas

| Pedido | Respuesta (§1) | Dónde se ve |
|---|---|---|
| DN4a-01 · disposición horizontal | kcal y BPM en la fila de abajo, kcal en el rincón; banda compacta; la llave junto a `Pausa` o encima en L640 | CAP-15, -16, -17, -23, -24, -25; DEMO-02 a -12 |
| DN4a-02 · centro de la zona del bote | 0,504 / 0,652 del alto | CAP-16 (el contorno, libre de piezas) |
| DN4a-03 · fondo provisorio | dos tonos, cortados en el horizonte | CAP-12 (sondas del cielo y del agua, ± 0) |
| DN4a-04 · barra sin píldoras | 40 dp, una fila de 22 | CAP-12, -18 a -22 |
| DN4a-05 · entrada y salida | fundido 240 / 200 | VID-03 |
| DN4a-06 · constantes de motion | §5 y §5.13 | VID-01, VID-02 (§17.4) |
| DN4a-07 · `obj.`, flecha y unidades | `obj.` alineado con las cifras, la flecha en su hueco, unidades `.hud @80 %` | CAP-19, -20, -24 (sondas) |
| DN4a-08 · Workout en reposo | §4.1 | CAP-01 a -11 |
| DN4a-09 · pares de contraste | H1 a H17, W1 a W4 | `ContrastTests` (§17.2) |
| DN4a-10 · foco y Tab | anillo blanco con halo oscuro; `Terminar` → `Pausa` → la llave | `Skin.uss` (el arnés no navega con teclado: [Device-Win]) |
| DN4a-11 · diálogo del HUD | el opaco de la app | CAP-26, -27b; DEMO-05 |
| DN4a-12 · `num.stat` | en la derivada | pendiente de WP-B0 (§17.6) |
