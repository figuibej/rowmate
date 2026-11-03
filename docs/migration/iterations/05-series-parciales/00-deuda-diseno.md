# It. 5 — Deuda de diseño de la 4b y base de diseño de los paneles de series

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-30 |
| Iteración | 5 · `05-series-parciales` ([ROADMAP §It. 5](../../ROADMAP.md)). Se escribió al empezar, con la [spec del PO](01-po-spec.md) ya escrita. Lo que este documento cambia de la spec va pedido al PO en §8, con su número |
| Entrada | [REPORT de la 4b](../04b-hud-exr/REPORT.md) (§7.1 QA-V4b-R2-01, §7.2 y §9.2); [05-qa-verification de la 4b](../04b-hud-exr/05-qa-verification.md) (Ronda 2: R2.2 y R2.8); [04-design-notes de la 4b](../04b-hud-exr/04-design-notes.md) (§3.4 a §3.6, §3.9, §4 y §16); [02-tech-design de la 4b](../04b-hud-exr/02-tech-design.md) §3.11; [01-po-spec de la 5](01-po-spec.md) (AC-WKT-16-07, AC-WKT-18-01 a -22, §5.2, §7, §10, §14 y §15); [ART_BIBLE](../../design/ART_BIBLE.md) (§3.2 a §3.4, §5.2, §7.8, §8.1 y §10); [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) (§0.1, §1, §2.3, §2.10 y §2.11); el inventario [workout-engine-hud](../../product/inventory/workout-engine-hud.md) (F-WKT-07, -08 y -18). Del código, sin tocarlo: `Core/Design/{Tokens,HudGeometry,InfoRowFit}.cs`, `Core/Workout/PhaseChip.cs`, `Presentation/Workout/{FreeTopBar,PhaseChipView,WallClockView}.cs`, `Presentation/Controls/TabularNumber.cs` y `Tests/PlayMode/HudBudgetTests.cs`. Capturas de la 4b: CAP-01, CAP-13 a CAP-15, CAP-18 y CAP-19 |
| Entrega | Este documento: QA-V4b-R2-01 decidido con números (§1), la base de diseño de los paneles de series (§2 a §6), los pedidos al Architect **DD5-01 a DD5-12** (§7) y lo que va al PO y a QA (§8). **No cambia código, tokens, capturas ni biblias**: los tokens que propone (§3.4) los integra el Architect, y las biblias se alinean después del gate (§9) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/series_panels.dart` y `series_tracker.dart` (enteros), `lib/features/workout/immersive_workout_screen.dart:60-140`, `:488-560` y `:590-760`, `test/series_panels_test.dart`, `docs/superpowers/specs/2026-10-02-series-splits-design.md` y `docs/image-03.png`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificado | Programas propios, sin red, en `<scratch>/it5/designer-debt/`:<br>- `ink.py`: corridas de tinta por columna de píxel en CAP-13 a CAP-15 y CAP-18 de la 4b (Pillow);<br>- `measure4b.py` (de la 4b), `m5.py`, `m5b.py` y `slargo5.py`: anchos de texto con las **tablas K2 versionadas** y el *tracking* de UI Toolkit, con fontTools 4.68.0 por `uv run --offline`;<br>- `icons5.py`: la tinta de `timer`, `history`, `expand_more` y `schedule` en `RowMateIcons-Outline.ttf`;<br>- `geo5.py`: la barra libre y la columna de los paneles en los cinco perfiles, con las fórmulas de `HudGeometry`;<br>- `contrast5.py`: los pares nuevos con el método de `Contrast.cs`, contra `#2A2F37` y `#2B3038`;<br>- `motion5.py` y `motion5b.py`: las tablas por cuadro, con las curvas de `Tokens.cs` y la solución exacta de `CriticalSpring` (`rmcore.py` de la 4b).<br>**Unity no se abrió** y dotnet no se corrió |

Convenciones: las de la 4b (hex `#RRGGBB`, transparencia `@NN %`, WCAG 2.x sin redondear, 1 px de USS = 1 dp). Perfiles: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720, con los márgenes seguros en 0. `s` = `hudScale`: 1,054 en TV y TH, 0,923 en P360 y L640, 1,6 en W1280. **`sp`** = la escala de los paneles (§2.4): igual a `s` salvo en W1280. "Base" = antes de multiplicar por la escala. Los rectángulos van en dp de ventana, con x desde la izquierda e y desde arriba. Las capturas de TV y TH tienen 2,628 px por dp (1 base = 2,770 px en TV). "Código" = animación por `MotionDriver`, con el reloj inyectable. "Segundo" = el paso de 1 s del motor.

---

## 0. Resumen

1. **QA-V4b-R2-01, la barra libre en un teléfono en español** (§1). Hoy la `e` de `Libre` queda a **4,19 dp** del ícono del reloj (11 px en CAP-13 a CAP-15), y el ícono queda a **7,99 dp** de sus cifras: el ícono se lee con el título y no con la hora.
   - **La decisión:** un **modo compacto** de la barra libre, que se usa solo cuando el título no entra a su tamaño (hoy, TV y P360 en español). Pasa el relleno del chip de 8 a 6, el aire del ícono del reloj a sus cifras de 5 a 3 y el del reloj al chip de 10 a 8. Los 8 base que se liberan van al aire entre el título y el reloj, que pasa de 2 a 10.
   - **Lo que da:** título → reloj **12,62 dp**, ícono → cifras **5,88** y reloj → chip **9,31**. El espacio entre las palabras del título es 4,95. Ahora el reloj se agrupa con el chip, a la derecha.
   - El título **sigue en 0,805**, entero: lo demás ocupa lo mismo (210,8 base). En inglés y en horizontal, nada cambia.
2. **El ancho de los paneles: 137 base, fijo, cerrados y abiertos** (§2.2). Es lo que entra entre el margen de SPM (14·s) y la columna del centro (el disco de la cuenta grande menos 8·s). Así los paneles nunca llegan al lugar de la cuenta grande y de `PAUSA`, ni al sello, ni al bote.
   - **La cuenta grande y `PAUSA` se ven siempre en su lugar de la 4b**, con los paneles abiertos o cerrados y en los cinco perfiles. P-04 se resuelve sin esconderlas ni moverlas.
   - En vertical, abiertos, entran solo en la franja de afuera de la zona del bote: de 115,1 a 159,1 dp en TV, el 24 % de su ancho. Con los 200 del legacy entraban hasta 225,6 dp (el 61 %), pasando el centro del remero.
   - **Por qué no 200 ni 87.** 200 tapa el disco y el remero. 87 (lo que deja la zona del bote) no alcanza ni para el chip con su flecha (§2.2).
3. **La columna** (§2.3): debajo de SPM, **8·s** (no 10) y con 8·s entre los dos paneles. Termina 8·s arriba de la banda. Con 8, los dos chips entran sin desplazar en los cinco perfiles, también en TH y L640 con SPM con objetivo (76 de 77,1 base). La columna sigue el alto de SPM con el tiempo del cambio de paso de la 4a.
4. **Lo que no entra** (§2.4):
   - en TV y P360 entra todo, con los dos abiertos (292 de 430 base);
   - en W1280 los paneles se dibujan a **sp = 1,20** (no a 1,6), y la parte del medio de cada panel (las filas) se desplaza adentro. La vuelta en curso, la más reciente, la línea `Rep` y la repetición más reciente quedan siempre a la vista (AC-WKT-18-13);
   - en TH y L640 se desplaza la columna entera, como el legacy.
5. **Anatomía** (§3):
   - el chip, de 34 de alto;
   - la vuelta en curso en `num.lap` **28**, con los metros en `num.xs` 13;
   - las filas en `num.row` **20**, con el rótulo 12/600 en una columna del ancho del texto;
   - `Rep r/R · m` en `num.stat` 16;
   - las repeticiones con los metros a la derecha.

   Todo entra en 117 base de contenido. Las filas de repeticiones con 4 cifras de metros se achican parejas a 0,949, y `S-LARGO` entra achicado, sin cortar nada.
6. **Tokens** (§3.4): `num.lap` 34/36 → **28/28** y `num.row` 24/28 → **20/22**, que no usa ningún código todavía. Uno nuevo, **`num.xs` 13/16/700**. Son 136 → 137.
7. **Contraste** (§3.7): los valores ámbar y celeste dan 7,127 y 7,163 sobre el peor vidrio, justo arriba de 7. **Cualquier tinte debajo de un valor lo baja de 7**: el fondo ámbar al 20 % de la vuelta nueva lo deja en 4,857 (AC-WKT-18-19). Lo reemplazo por una **marca** ámbar de 3·s a la izquierda de la fila, que no pasa debajo del texto. La capa de *hover* y de presionado va solo en la cabecera.
8. **Motion** (§4), todo por código y con la regla de oro:
   - abrir y cerrar con `spring.smooth`: solo el alto, porque el ancho no cambia;
   - el contenido entra en 80-240 y sale en 0-120; la flecha gira en 240;
   - la vuelta nueva empuja la lista hacia abajo (0-160), entra desde −8·s (60-240) y se lee entera a los 200 ms. La marca se apaga a los 1400;
   - el cambio de contenido sale en 0-120 y entra en 120-280;
   - la serie que empieza o termina, el panel que sigue a SPM, la cascada con SPM y reducir movimiento.
9. **Presupuesto** (§5): **un elemento por panel**, en cualquier estado, dibujado por código como la fila de píldoras: +2. Con el peor caso de la 4b (58 o 59), quedan 60 o 61. Pido juntar 2 más (M3 y M5 de la 4b) para dejar margen.
10. **Capturas** (§6): qué cambia en lo que esperan CAP-03, CAP-16 a CAP-20, CAP-22, VID-01, VID-02, VID-08 y VID-09 de la spec. Las tomas con un panel recién abierto esperan **0,82 s**, no 0,6: a los 600 ms al resorte le falta 0,9 base.

---

## 1. QA-V4b-R2-01: la barra libre en un teléfono en español

### 1.1 Lo que se ve hoy (medido)

Medí la tinta, columna por columna, en la fila de la barra (y 55-105 px) de las tomas de la 4b. CAP-13, CAP-14 y CAP-15 dan exactamente lo mismo:

| Hueco, de tinta a tinta (TV) | px | dp |
|---|---|---|
| Entre `Entrenamiento` y `Libre` | 13 | 4,95 |
| De la `e` de `Libre` al ícono del reloj | **11** | **4,19** |
| Del ícono a las cifras `10:00` | 21 | 7,99 |
| De las cifras al borde del chip | 30 | 11,42 |

En inglés (CAP-18), de `Free Workout` al ícono hay 146 px (55,6 dp). Con lo demás igual, el problema es solo del modo en que el título se achica.

- **Por qué se lee mal.** Por cercanía, el ícono va con lo que tiene más cerca. Hoy tiene más cerca la `e` (4,19) que sus cifras (7,99). Se lee `Libre ⏱` y después `10:00`, y el hueco más grande de la barra (11,42) queda entre el reloj y el chip, que tendrían que ir juntos a la derecha.
- **04-design-notes §16 de la 4b** decía "se ven ≈ 5". Lo medido es **≈ 4,2**; este documento lo corrige.

### 1.2 Opciones medidas

El título no puede bajar de 0,8 ni recortarse (AC-WKT-16-06 y -07). Con los anchos que midió Unity (título 155,4, reloj 69,5 y chip 101,3 base), sobran 0,78 base entre el 0,805 de hoy y el 0,8. Lo que haga falta sale de los aires de la barra:

| Opción (TV, es) | Título → ícono | Ícono → cifras | Cifras → chip | Cómo se lee |
|---|---|---|---|---|
| Hoy | 4,19 | 7,99 | 11,42 | el ícono va con el título |
| A · relleno del chip 8 → 6 (el aire mínimo, 2 → 6) | 8,40 | 7,99 | 11,42 | el ícono queda a la misma distancia de los dos |
| B · A + ícono → cifras 5 → 3 (aire 8) | 10,50 | 5,88 | 11,42 | el reloj se arma, pero queda flotando en el medio |
| **C · B + reloj → chip 10 → 8 (aire 10)** | **12,62** | **5,88** | **9,31** | **título │ reloj + chip** |

Con C, cada hueco es de 1,4 a 1,6 veces el anterior: 5,88 dentro del reloj, 9,31 entre el reloj y el chip, y 12,62 entre el título y el reloj. El título queda a 2,5 veces el espacio entre sus palabras.

### 1.3 La decisión: el modo compacto de la barra libre

| Medida (base) | Normal | **Compacto** |
|---|---|---|
| Aire mínimo entre el título y el reloj (`InfoRowFit.FreeTitleClockGap`) | 2 | **10** |
| Del ícono `schedule` a sus cifras (`WallClockIconGap`, **solo en la barra libre**) | 5 | **3** |
| Del reloj al chip (`PhaseChipGap`) | 10 | **8** |
| Relleno del chip a cada lado (`PhaseChipPadH`; el vertical sigue en 3) | 8 | **6** |
| Ancho fijo del chip, es · en (`PhaseChip.FixedWidthBase`) | 101,3 · 73,5 | **97,3** · 69,5 |
| Lo que ocupa todo menos el título: 20 + 8 + aire + reloj + aire + chip | 210,8 | **210,8** |
| Escala del título en TV y P360, en español | 0,805 | **0,805** |

- **Cuándo.** Compacto si y solo si, con las medidas normales, el título no entra a 1 (`FreeTitleScale < 1`). Se decide en el mismo momento que esa escala: en el acto, con la ventana y con el idioma.
  - Hoy es compacto en **TV y P360 en español**.
  - En inglés (`Free Workout`, al que le sobran 51) y en TH, L640 y W1280, en los dos idiomas, es normal: **nada cambia**.
- **El título no se mueve**, porque lo demás ocupa lo mismo. Sigue entero a 0,805, con su borde izquierdo en el mismo lugar.
- **Solo en la barra libre.** El reloj de la barra de pasos sigue con su aire de 5 (AC-WKT-19-01 no cambia ahí) y la fila de información, con sus modos de la 4b.
- **El chip a 6.** Con su alto de 19,6 y radio 8, a la altura de la etiqueta el borde está a 0,3 base del lado. A 6, el punto en vivo (a la izquierda) y la última `A` de `EN MARCHA` (a la derecha) quedan a 5 base del borde interior. `PAUSADO` y `FINALIZADO` van centrados en el ancho fijo, con 18,6 y 10,6 de aire. El *tracking* (0,08 em) y los colores no cambian.
- **Descarté** bajar el título hasta 0,8 (da 0,78 base) y recortar (lo prohíbe AC-WKT-16-06).

### 1.4 Lo que tiene que dar en CAP-13, CAP-14 y CAP-15 regeneradas (TV, px de la captura, ± 1)

| Pieza | Hoy | Compacto |
|---|---|---|
| `Entrenamiento Libre` (tinta) | termina en 497; escala 0,805 (131,98 dp en el *manifest*) | **igual** |
| Ícono del reloj (tinta) | 509-545 | **531-567** (+22,2: +8 base) |
| Cifras `10:00` | 567-693 | **584-710** (+16,6: +6 base) |
| Borde izquierdo del chip | 724 | **735** (+11,1: +4 base); el derecho, igual (1004) |
| Ancho del chip | 281 (106,9 dp, 101,3 base) | **270** (102,5 dp, **97,3 base**) |
| Punto en vivo (CAP-13) | 745-761 | **750-767** |
| Hueco título → ícono · ícono → cifras · cifras → chip | 11 · 21 · 30 | **33 · 15,5 · 24,5** |
| Entre `Entrenamiento` y `Libre` | 13 | **13** |

CAP-16 (TH), CAP-17 (W1280), CAP-18 (en), DEMO-02 y DEMO-03 (W1280) **no cambian**. En los videos de la 4b con la barra libre en TV en español (VID-01, VID-02, VID-06, VID-07 y VID-08), la barra cambia igual que en CAP-13, y lo que se mueve en el tiempo no cambia.

### 1.5 Lo que cambia en la spec y en las notas de la 4b

- **AC-WKT-16-07** se cumple con el aire de 10 base. En TV, 12,62 dp de tinta a tinta, 2,5 veces el espacio entre las palabras.
  - Lo único que choca es su "el resto de la barra no cambia". Propongo: "salvo, en el modo compacto, los aires del reloj y del chip (00-deuda-diseno de la 5 §1.3)".
- **AC-WKT-16-02** (punto 5): "10 de separación" pasa a "10 (8 en el modo compacto)".
- **AC-WKT-16-03**: "relleno 8 × 3" pasa a "8 × 3 (6 × 3 en el modo compacto)".
- **AC-WKT-19-01**: "5 de separación" pasa a "5 (3 en la barra libre en modo compacto)".

Todo esto va pedido al PO en §8. En la 4b, 04-design-notes §16 queda corregido acá: ≈ 4,2 dp, no ≈ 5. La regla del modo compacto pasa a ART_BIBLE §3.3 después del gate (§9).

---

## 2. Los paneles de series: dónde van y qué ancho

### 2.1 Lo que manda

- **El legacy** (`series_panels.dart`, `immersive_workout_screen.dart:611-633`):
  - los dos paneles van en la columna de SPM, a 10 debajo, con 8 entre ellos;
  - la columna termina 8 arriba de la banda y desplaza lo que no entra;
  - el chip mide al menos 130 y el panel abierto, 200 fijos;
  - un toque en cualquier parte lo abre o lo cierra.
- **El ROADMAP (mejoras EXR)**: paneles de vidrio que se despliegan con suavidad, "ancho acotado para no tapar el bote", y cada vuelta nueva entra con una animación breve.
- **El pedido de esta tarea**: que no tapen el bote ni el lugar centro alto de la cuenta grande y del sello, en vertical y en horizontal, con el presupuesto en su tope.
- **La spec** (01-po-spec §4.3) deja al Designer:
  - P-03, la zona del bote en vertical;
  - P-04, el lugar de la cuenta grande con un panel abierto;
  - P-05, achicar los paneles para que entren;
  - las discrepancias de §15: el aire de CAP-19, el lugar debajo de SPM y el "tablet" de ART_BIBLE §3.4.

  Este documento responde las tres y las tres discrepancias.

### 2.2 El ancho: 137 base, fijo

`ancho = min(137; ancho base de la ventana / 2 − 58)`, en base (× `sp`), **igual cerrado y abierto**. 137 = 390/2 − 36 (el radio del disco) − 8 (su aire) − 14 (el margen de SPM). En todo teléfono de 351 dp o más de lado corto, el panel termina exactamente 8·s antes del disco. En uno más angosto, donde `s` queda en 0,9, se angosta solo.

| Perfil | El panel (x, dp) | El disco empieza en | Aire | La zona del bote (x) |
|---|---|---|---|---|
| TV | 14,8-159,1 | 167,6 | 8,4 | 115,1-295,9 |
| P360 | 12,9-139,4 | 146,8 | 7,4 | 100,8-259,2 |
| TH | 14,8-159,1 | 419,1 | 259,9 | 255,9-658,1 |
| L640 | 12,9-139,4 | 286,8 | 147,4 | 179,2-460,8 |
| W1280 (sp 1,20) | 22,4-186,8 | 582,4 | 395,6 | 358,4-921,6 |

**Por qué no los 200 del legacy.**

- En TV, 200 base llegan a 225,6 dp. Pasan el borde del disco (167,6) y el centro del remero (205,5).
- Con 200, la cuenta grande y `PAUSA` tendrían que esconderse o moverse en vertical (P-04), y el panel taparía al remero.
- En L640, 200 base entran 18 dp en la zona del bote (spec §15).

**Por qué no lo que deja la zona del bote (87,2 base).**

- El chip cerrado necesita 14 + 4 + `500 m` (45,3) + 4 + la flecha (13,3) + 20 de relleno, es decir 100,6.
- Abierto, la cabecera de `Series anteriores` con su ícono y la flecha pide 141,6, aun con el título en 11/600 (90,3).
- La vuelta en curso con sus metros pide 135, aun con la cifra de 28.

**Por qué fijo.** El legacy pasa de 130 a 200 al abrir: el texto se reacomoda y el panel crece hacia el remero mientras se abre. Con un solo ancho, abrir y cerrar es solo alto, como pide la mejora EXR ("crece desde el chip"). Además, la lista no cambia de columnas en medio de la animación.

**La zona del bote, en vertical (P-03).**

- **Cerrados**, nunca la tocan: en TV los dos chips van de y 228,6 a 308,7, y la zona empieza en 323,7 (15 dp de aire). En P360, de 200,3 a 270,4, contra 283,4 (13 dp).
- **Abiertos**, entran solo en su franja de afuera: x 115,1-159,1 en TV (44,1 de 180,8 dp, el **24 %**) y 100,8-139,4 en P360 (el 24 %). Nunca en la columna del centro, que tiene el disco, el casco, el remero y los *riggers* (x ≥ ancho/2 − 44·s).
- **El remo izquierdo** (aproximado, con ART_BIBLE §7.8 y §8.1: pernos a ±0,80 m, 2,0 m de remo afuera, cámara a 2,2 m y remero a 9,5 m):
  - en TV, la pala barre x 89-141 e y 444-475 dp, con el perno en (171; 447);
  - `Parciales` abierto, aun con 4 vueltas y la línea `Rep`, termina en y 418,3: **nunca tapa el remo**;
  - `Series anteriores` abierto (y 426,8-536,4) tapa la pala izquierda mientras está abierto. El legacy, con 200, tapa además el perno y el costado del remero.
  - Se revisa con el bote de verdad en la 7. Si hace falta, la alternativa medida es `Series anteriores` debajo de Watts, solo en vertical: x 251,8-396,2 desde y 299,9, con lugar hasta la llave (575,9). Ahí no tapa nada. Por ahora no la propongo, porque cambia la zona del legacy y el panel cambiaría de lado al rotar.
- **En horizontal**, ningún panel llega a la zona: el borde derecho queda a 96,8 dp de ella en TH, a 39,8 en L640 y a 171,6 en W1280.

### 2.3 La columna: techo, piso y huecos

| Medida (base, × s) | Valor | Legacy | Por qué |
|---|---|---|---|
| Lado izquierdo | 14·s (el de SPM) | 14 | alineados con SPM |
| Techo | el borde de abajo de SPM + **8·s** | 10 | con 10, en TH y L640 con SPM con objetivo, el segundo chip no entra (76 contra 75,05). Con 8 entra (76 contra 77,05), y 8 es el aire entre tarjetas apiladas del HUD (Split y Watts) |
| Entre los dos paneles | 8·sp | 8 | igual |
| Piso | 8·s arriba de la banda: en vertical, alto − 220·s; en horizontal, alto − 96·s | "margen inferior + 200" y "+ 16 + 66 + 8" | AC-WKT-18-13 |

| Perfil | Techo con objetivo en SPM · sin | Piso | Lugar (base del HUD) con · sin | ¿Los dos chips? |
|---|---|---|---|---|
| TV | 228,6 · 210,9 dp | 682,2 | 430,3 · 447,2 | sí; sobran 354 |
| P360 | 200,3 · 184,7 | 596,9 | 429,7 · 446,6 | sí |
| TH | 228,6 · 210,9 | 309,8 | **77,1** · 93,9 | **sí, sobra 1,1** |
| L640 | 200,3 · 184,7 | 271,4 | **77,1** · 93,9 | **sí, sobra 1,1** (la spec daba el segundo chip desplazado) |
| W1280 | 347,1 · 320,2 | 566,4 | 137,1 · 153,9 | sí; sobran 61 |

- **La columna sigue a SPM** (legacy): cuando SPM gana o pierde `obj.` en un cambio de paso, la columna baja o sube 16,9·s, con el tiempo de la 4a (§4.4). Las tarjetas no se mueven por los paneles (AC-WKT-18-16).
- **El área de toque** de cada chip mide **al menos 48 dp de alto** en los cinco perfiles, y 56 donde hay lugar (ART_BIBLE §10.4):
  - se extiende por el aire que lo rodea (cada uno, la mitad de los 8·sp entre los dos, así nunca se superponen: AC-WKT-18-09);
  - y, si hace falta, por el relleno de SPM o de la banda, que no responden. Nunca por su tinta ni por una pieza que responde.
  - En TV, el de `500 m` llega a 58,9 dp y el de `Reps`, a 56. En P360, 51,7 y 56. En L640, `Reps` llega a 52,7 con el relleno de la banda.
  - Abierto, el área es todo el panel (legacy).

### 2.4 Lo que entra y qué pasa cuando no entra

**Altos naturales (base, × sp):**

| Panel | Contenido | Alto |
|---|---|---|
| Cualquiera, cerrado | el chip | **34** |
| `Parciales 500 m` | sin vueltas y sin serie | 68 |
| | cada vuelta | +4 la primera, +22 cada una más (hasta 4) |
| | con serie, la línea `Rep r/R · m` | +22 |
| | 4 vueltas y la línea | **180** |
| | fuera de un trabajo (`Sin parciales en este paso`, dos líneas), con la línea · sin | 106 · 84 |
| `Series anteriores` | 1 · 2 · 3 repeticiones | 60 · 82 · **104** |
| | `Sin repeticiones completas` (dos líneas) | 84 |
| Los dos abiertos, lo más alto | 180 + 8 + 104 | **292** |
| **La vista mínima** (AC-WKT-18-13: la vuelta en curso, la más reciente, la línea `Rep` y la repetición más reciente) | `Parciales` con 1 vuelta y la línea (114) + 8 + `Series anteriores` con 1 (60) | **182** |

**La regla de lo que no entra** (P-05). Se aplica en orden:

1. **Entra todo** a la escala del HUD: `sp = s`, sin desplazar. Es el caso de TV y P360, con 292 de 430 base.
2. **Entra la vista mínima a `s`, pero no todo**: `sp = s`. La parte del medio de cada panel abierto (las filas de vueltas o de repeticiones) se desplaza **adentro del panel**. La cabecera, la vuelta en curso y la línea `Rep` quedan fijas.
   - El lugar que sobra se reparte de arriba hacia abajo: primero a `Parciales`, hasta su alto natural, y después a `Series anteriores`.
   - Así lo viejo es lo que se desplaza (spec §10.4) y la repetición más reciente nunca queda debajo de la línea de la vista.
3. **La vista mínima entra a una escala menor**: `sp = min(s; lugar en dp / 182)`, redondeado hacia abajo a 0,01. Se usa solo si da 0,9 o más, la escala más chica del HUD, que es el mínimo legible. Con esa `sp`, vale el punto 2.
   - **W1280**: 219,3 dp / 182 = 1,2048, así que **`sp` = 1,20**. Los paneles miden 164,4 dp de ancho, el 75 % de la escala de las tarjetas.
   - CAP-18 (1 vuelta y 1 repetición) entra entero, sin desplazar.
4. **Si no**, `sp = s` y se desplaza **la columna entera**, como el legacy. Es el caso de TH y L640 (harían falta 0,446 y 0,391).
   - Los dos chips entran igual.
   - Con `Parciales` abierto se ven sin desplazar su cabecera y la vuelta en curso (60 de 77,1). El resto, hacia abajo.

- **Cuándo se decide `sp`.** Por ventana, con el peor caso (SPM con objetivo, en serie, los dos abiertos). Así no cambia con los datos ni con el paso, y en el acto con una rotación.
- **El punto 2 contra el 1** se evalúa siempre: cuando llega una vuelta y el panel ya no entra, deja de crecer en su tope y su lista empieza a desplazarse, sin un salto.
- **El desplazamiento** (dedo, arrastre del mouse y rueda; AC-WKT-18-13):
  - empieza arriba y no tiene barra;
  - el borde que recorta lleva un fundido de 12·sp, abajo si hay más y arriba si se bajó;
  - un toque que se mueve menos de 8 dp abre o cierra, y uno que se mueve más, desplaza;
  - la rueda va de a una fila (22·sp), con `spring.snappy`;
  - cuando cambia el contenido, lo que se ve no salta: la lista se corre con su animación (§4.2) y el desplazamiento conserva su valor, dentro de sus topes.

---

## 3. Anatomía y aspecto

Todo va dentro de 137 base: **117 de contenido**, con 10 de relleno a cada lado y 8 arriba y abajo. Es el vidrio de las tarjetas: `glass.fallback`, `r.glass` 14 y borde de 1 al 10 %, con el reflejo interior de los 8 de arriba, que queda dentro del relleno.

### 3.1 El chip (cerrado)

```
#hud-laps-panel / #hud-reps-panel  (cerrado)   137 × 34 · vidrio de las tarjetas · relleno 10 × 8
[timer 14 · metric.split @80 %] 4 [500 m · 13/600 · 0,08 em · metric.split] ·············· [expand_more 18 · text.tertiary]
[history 14 · metric.distance @80 %] 4 [Reps · 13/600 · 0,08 em · metric.distance] ·········· [expand_more 18 · text.tertiary]
```

- **La flecha** va con su tinta (de 0,261 a 0,739 em de su caja) pegada al borde del contenido. Su caja de 18 entra 4,7 en el relleno, así que ocupa 13,3 y su lado izquierdo ya hace de aire con el texto.
- **El rótulo** tiene lugar hasta **81,7 base** a 13/600 con 0,08 em: `500 m` mide 45,3 y `Reps`, 35,4.
- **El ícono** (`timer` de contorno, como el `timer_outlined` del legacy, e `history`) va con su caja en el borde del contenido: su tinta empieza 1,6 adentro.
- **El chip mide 137 de ancho**. La spec pide un mínimo de 140 y el legacy, 130.

### 3.2 `Parciales 500 m` (abierto)

```
relleno 8
[timer 14] 4 [Parciales 500 m · 11/600 · metric.split] ················ [la flecha, girada 180°]               18
6
[0:55.0 · num.lap 28/700 · metric.split.hud] ······ [207 m · num.xs 13/700 · metric.split.hud @80 %]           28 (línea de base común)
4
[#4 · 12/600 · text.secondary] 4 [1:52.3 · num.row 20/700 · metric.split.hud]                                 20  ┐ hasta 4,
2                                                                                                                  │ la más reciente
[#3] 4 [1:53.0]                                                                                                    │ arriba
… (2 entre filas)                                                                                                  ┘
6
[Rep 2/3 · 1.245 m · num.stat 16/700 · metric.distance.hud]                                                     16  solo en serie
relleno 8
```

- **El título** va en 11/600, sin *tracking*. El lugar es 117 − 14 − 4 − 13,3 = 85,7: `Parciales 500 m` mide 85,2 y entra a 1. Lo que no entra se achica parejo, desde su borde izquierdo, hasta 0,8, y solo debajo de eso se recorta con `…` (la regla del título libre, `FreeTitleScale`).
  - Con el ancho de 137, a 13 no entra ningún título con su ícono y su flecha: `Series anteriores` pide 106,7.
  - El cambio del rótulo corto al título es un fundido (§4.1).
- **La vuelta en curso.** La cifra mide 76,3, más 6 de aire y los metros: con `499 m` (32,7, de tres cifras tabulares, porque la vuelta en curso nunca llega a 500), 115,0.
  - Un tiempo de 10 minutos o más (`10:02.4`, 90,9) achica solo la cifra (`FitBox`), a 0,861.
  - Los metros van pegados a la derecha, con la línea de base de la cifra.
  - Los metros van en `.hud @80 %`, la regla de las unidades del HUD (DV-31). Al 55 % de la spec dan **3,003** sobre el peor vidrio (§3.7).
- **Las filas.** La columna del rótulo mide lo de `#99` (23,1), o lo del más ancho a la vista si pasa de 99. Con la cifra, 81,6 (`#4 1:52.3`) y 92,0 con `10:02.4`.
  - La columna de 52 del legacy entraría (110,5), pero la de repeticiones no (§3.3), y las dos van iguales.
- **`Sin parciales en este paso`**, fuera de un trabajo, va en `body` 15/22/400 `text.tertiary` (legacy: 15, blanco al 54 %), en **dos líneas** dentro de 117: `Sin parciales en` (112,2) y `este paso`. En inglés, `No splits in this` (107,8) y `step`.

### 3.3 `Series anteriores` (abierto)

```
relleno 8
[history 14] 4 [Series anteriores · 11/600 · metric.distance · a 0,949 en es] ·········· [la flecha]                  18
6
[Rep 2 · 12/600 · text.secondary] 4 [2:05 · num.row 20/700 · metric.split.hud] ········· [680 m · num.xs 13/700 · metric.distance.hud]   20  hasta 3,
2 …                                                                                                                       la más reciente arriba
relleno 8
```

- **El título.** `Series anteriores` mide 90,3, así que va a **0,949**, achicado parejo. En inglés, `Previous reps` mide 72,6 y entra a 1.
- **La fila.** La columna del rótulo mide lo del `Rep n` más ancho a la vista. Después, 4; el split en `M:SS` o `—`; al menos 6 de aire; y los metros, pegados a la derecha.

| Fila | Ancho (base) | Escala de la fila |
|---|---|---|
| `Rep 1 2:13 452 m` | 110,8 | 1 |
| `Rep 3 1:54 741 m` | 113,3 | 1 |
| `Rep 3 1:54 1.245 m` (metros de 4 cifras, lo común en series de 1000 m) | 123,3 | **0,949** |
| `Rep 12 12:59 12.345 m` | 145,4 | 0,805 |
| `Rep 2 — 120 m` | 88,0 | 1 |

- **La escala de la fila** es **una sola para las filas a la vista**, así las columnas quedan alineadas. Se decide cuando cambian las filas, y el cambio de escala se anima con el empuje de la lista (§4.2). Con 12/600 y metros de 13 es lo más angosto legible. Con los 52 y los 16 de la spec, la fila común pediría 142,4 (0,82).
- **`Sin repeticiones completas`**, en dos líneas: `Sin repeticiones` (113,7) y `completas`. En inglés, `No completed` y `reps`.
- **`S-LARGO` (AC-WKT-18-17).** Todo entra achicado y nada se corta:
  - la vuelta en curso `5999:59.9` con `499 m`, a 0,652;
  - `#1999 5999:59.9`, a 0,932;
  - `Rep 999/999 · 999.999 m`, a 0,712;
  - `Rep 999 5999:59 999.999 m`, a 0,638.

### 3.4 Tipografía, color y tokens

| Pieza | Fuente y tamaño | Color | Token |
|---|---|---|---|
| Rótulo del chip | Inter 600, 13, 0,08 em (`label` de SPM) | `metric.split` / `metric.distance` | existentes |
| Título | Inter 600, 11 (el tamaño del rótulo de kcal y BPM), sin *tracking* | ídem | existentes |
| Íconos | fuente de íconos, 14 | la métrica @80 % | existentes |
| Flecha | dibujada (§4.1), 18 | `text.tertiary` | existente |
| Vuelta en curso | RowMate Numeric SC 700, 28, `TabularNumber` | `metric.split.hud` | **`num.lap` 34/36 → 28/28** |
| Metros de la vuelta en curso | RowMate Numeric SC 700, 13 | `metric.split.hud @80 %` | **`num.xs` 13/16/700, nuevo** |
| Rótulos `#n` y `Rep n` | Inter 600, 12 | `text.secondary` | existentes |
| Tiempos de las filas y splits | RowMate Numeric SC 700, 20 | `metric.split.hud` | **`num.row` 24/28 → 20/22** |
| Metros de las repeticiones | RowMate Numeric SC 700, 13 | `metric.distance.hud` | `num.xs` |
| `Rep r/R · m` | RowMate Numeric SC 700, 16 (legacy 16/800) | `metric.distance.hud` | `num.stat` (existente) |
| Textos vacíos | Inter 400, 15/22 | `text.tertiary` | `body` (existente) |
| Marca de la fila nueva | dibujada, 3·sp × 20·sp, radio 1,5·sp | `metric.split` / `metric.distance` | `stroke.accent` (3) |

- **Tokens:** son **136 → 137**.
  - `num.lap` y `num.row` solo están en `Tokens.uss` y `Typography.uss`, porque ningún código los usa todavía. Cambiarlos no mueve nada de la 4a ni de la 4b.
  - `num.row` queda con los valores de `num.s`, pero como rol propio ("filas de vueltas y repeticiones"), como lo usa ART_BIBLE §4.3.
  - Los integra el Architect, con `tools/tokens` y los conteos (DD5-08).
- **Todos los números van en cifras tabulares** (ART_BIBLE §4.2), también los metros: un `207 m` que pasa a `211 m` no baila.

### 3.5 El toque, el estado y el foco

- **El toque.** Cerrado, el chip con su área de §2.3. Abierto, todo el panel (legacy). Arrastrar para desplazar no abre ni cierra (§2.4).
- **El estado** (*hover* en la PC y presionado) es la capa de la app: blanco al 4 % y al 8 %, con 90 al entrar y 160 al volver, por código.
  - **Va solo en la cabecera** (los 34 de arriba, con el radio de arriba del panel), aunque el puntero o el dedo estén sobre el contenido.
  - Debajo de un valor, la capa del 4 % lo baja a 6,309 y la del 8 %, a 5,553 (§3.7).
  - Sin escala al presionar, como las tarjetas de rutina: un vidrio de 180 que se encoge salta.
- **El foco** (AC-WKT-18-12): el anillo del HUD (blanco de 2 dp a −4 del borde, radio 18, con el halo de `overlay.scrim` al 50 % de 4 dp). Sigue el alto visual del panel mientras se abre o se cierra.
  - El orden de Tab es el de la spec (P-06).
  - Enter y Espacio, como un toque.

### 3.6 Legibilidad a 1,5 m (TV; 1 dp = 0,159 mm; 1′ = 0,436 mm a 1,5 m)

| Pieza | Altura de mayúscula | A 1,5 m | Contra 5′ |
|---|---|---|---|
| Vuelta en curso (28) | 20,66 dp · 3,29 mm | 7,5′ | **1,5×**: se lee de lejos, como la flecha de SPM |
| Tiempos de las filas y splits (20) | 14,76 dp · 2,35 mm | 5,4′ | 1,1×: como el reloj de pared |
| `Rep r/R · m` (16) | 11,80 dp · 1,88 mm | 4,3′ | 0,9×: de cerca |
| Metros (13) | 9,59 dp · 1,52 mm | 3,5′ | de cerca |
| Rótulo del chip (13) | 9,97 dp · 1,59 mm | 3,6′ | de cerca; de lejos se ven el color y el ícono |
| Rótulos `#n` y `Rep n` (12) | 9,20 dp · 1,46 mm | 3,4′ | de cerca |
| Título (11; 0,949 el de repeticiones) | 8,43 dp · 1,34 mm | 3,1′ | de cerca |

De lejos, el remero lee la vuelta en curso, ve entrar la vuelta nueva (la lista que baja y la marca ámbar) y reconoce cada panel por su color. Los paneles son, como dice la spec, "el detalle a pedido".

### 3.7 Contraste (para `ContrastTests.ItFiveDesignerPairs`)

Sobre el peor vidrio (`#2A2F37`) y el peor renderizado (`#2B3038`). Todos los pares, sobre su umbral.

| Grupo | Par | Umbral | Peor · renderizado | Sobre el cielo · el agua (fondo provisorio) |
|---|---|---|---|---|
| C1 | Rótulo y título `metric.split` | 4,5 | 6,572 · 6,463 | 8,814 · 8,847 |
| C2 | Rótulo y título `metric.distance` | 4,5 | 7,016 · 6,900 | 9,410 · 9,446 |
| C3 | Ícono `timer` `metric.split @80 %` · `history` `metric.distance @80 %` | 3 | 4,746 · 4,680 · 5,062 · 4,992 | 5,956 · 6,416 |
| C4 | Flecha `text.tertiary` | 3 | 5,099 · 5,048 | 5,908 · 5,923 |
| C5 | Valores `metric.split.hud` · `metric.distance.hud` | 7 | 7,127 · 7,009 · 7,163 · 7,044 | 9,559 · 9,607 |
| C6 | Metros de la vuelta `metric.split.hud @80 %` | 4,5 | 5,118 · 5,047 | 6,433 · 6,457 |
| C7 | Rótulos `text.secondary` · vacíos `text.tertiary` | 4,5 | 7,448 · 7,352 · 5,099 · 5,048 | 9,193 · 5,908 |
| C8 | Rótulo del chip sobre la capa al 4 % y al 8 % | 4,5 | 5,817 · 5,719 · 5,120 · 5,035 | 8,027 · 7,168 |
| C9 | Marca `metric.split` · `metric.distance` (gráfico, en su pico) | 3 | 6,572 · 7,016 | 8,814 · 9,410 |

**Lo que no se usa, porque no llega:**

- **El fondo de la vuelta nueva** (`metric.split @20 %`, AC-WKT-18-19) baja el valor ámbar a **4,857 / 4,786**. Al 12 % baja a 5,710 y al 8 %, a 6,170. No hay alfa que llegue a 7, porque el valor ya está en 7,127. Por eso, la marca (C9).
- **La capa debajo de los valores** da 6,309 al 4 % y 5,553 al 8 %. Por eso va solo en la cabecera (C8).
- **Los metros al 55 %** de la spec dan 3,003 / 2,975 (`metric.split`) y 3,201 / 3,171 (`.hud`). Por eso, C6.

**Sin umbral** (decorativos): el borde al 10 % (1,370) y el fundido de 12·sp donde se recorta un desplazamiento.

**Compuestos para las sondas** (sobre el vidrio en el cielo, `#08162A`; en el agua, `#081627`): valores `#FFAA47` y `#4ECBF2`; metros al 80 % `#CE8C41`; íconos `#CE841F` y `#3EA5C8`; flecha y vacíos `#8D949D`; rótulos `#B5B9BF`; borde `#202E40`; marca, en su pico, `#FF9F1C` y `#4CC9F0`.

---

## 4. Motion

### 4.0 La regla de oro, aplicada (ANIMATION_BIBLE §0.1)

- **Los valores cambian en el cuadro del segundo del motor**, el mismo del reloj de la banda (AC-WKT-18-08). Lo que se anima es cómo se ven.
- **La vuelta en curso no cuenta décimas en vivo**: su texto cambia una vez por segundo, con el rodillo de 180 de `TabularNumber`, solo en las celdas que cambian.
- **Una vuelta nueva se ve con su valor final** desde su primer cuadro visible (a los 67 ms está al 11 % y a los 200, al 98,8 %). Nunca pasa por valores intermedios.
- **Abrir, cerrar, desplazar o animar** no toca el motor, la sesión, la telemetría ni la preferencia, que se guarda en el cuadro del toque (AC-WKT-18-09 y -10).
- **Nada se anima por layout en cada cuadro.** El layout va a su estado final en el cuadro 0. El alto visual del vidrio, el recorte del contenido, el corrimiento de la lista y el del panel de abajo los dibuja el mismo elemento o un `translate` (FLIP, como kcal en la 4b).

### 4.1 Abrir y cerrar (VID-01)

`t = 0` en el cuadro del toque. Solo cambia el alto: el ancho es fijo (§2.2).

**Abrir**, `Parciales` de 34 a 180 base. El alto va con `spring.smooth` (ω 12), desde su posición y su velocidad. El resto, con curvas:

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 | 333 | 367 | 400 | 467 | 533 | 600 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Alto (base) | 34,0 | 42,8 | 62,1 | 83,3 | 103,2 | 120,9 | 135,0 | 146,2 | 155,1 | 161,6 | 166,6 | 170,4 | 173,0 | 176,4 | 178,2 | 179,1 |
| Contenido (80-240, `ease.enter`) | 0 | 0 | 0 | 0,331 | 0,699 | 0,902 | 0,983 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Rótulo corto sale (0-80, `ease.exit`) | 1 | 0,927 | 0,413 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Título entra (80-200, `ease.enter`) | 0 | 0 | 0 | 0,422 | 0,823 | 0,978 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Flecha (0-240, `ease.move`), grados | 0 | 3 | 17 | 51 | 117 | 159 | 175 | 180 | 180 | 180 | 180 | 180 | 180 | 180 | 180 | 180 |

**Cerrar**: el contenido sale primero y el alto empieza a los 80 (el "colapsar" de ANIMATION_BIBLE §2.3).

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 | 333 | 367 | 400 | 467 | 533 | 600 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Alto (base), resorte desde los 80 | 180,0 | 180,0 | 180,0 | 176,4 | 160,5 | 139,1 | 118,4 | 100,0 | 84,2 | 71,9 | 62,3 | 54,7 | 49,2 | 41,9 | 38,1 | 36,1 |
| Contenido (0-120, `ease.exit`) | 1 | 0,978 | 0,823 | 0,422 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Flecha, grados (240, `ease.move`) | 180 | 177 | 163 | 129 | 63 | 21 | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

- **El rótulo corto vuelve** en 80-200 y el título se va en 0-80, como el chip de fase de la 4b.
- **El contenido se recorta al alto visual** menos el relleno de abajo: se descubre de arriba hacia abajo mientras el vidrio crece ("se despliega").
- **El panel de abajo** se corre con el mismo resorte (FLIP). El aire de 8·sp vale lo mismo en cada cuadro y nada más se mueve.
- **Un toque durante la animación la invierte desde donde está**: el resorte, con su posición y su velocidad; los fundidos, desde su opacidad; la flecha, desde su ángulo, con lo que le falta de los 240.
- **La flecha** es un trazo de tres puntos dibujado por el panel, sobre la línea central de `expand_more`: (7,0; 9,3), (12; 14,3) y (17,0; 9,3) en una caja de 24, con un grosor de 2/24 de la caja y extremos redondos. Gira alrededor de su centro: en el sentido del reloj al abrir y al revés al cerrar.
- **Cuándo termina.** El resorte de 146 base queda a menos de 1 base a los 590 ms y a menos de 0,25 px de la captura a los **814** (TV). Las tomas esperan eso (§6).
- **Con `Series anteriores`** (de 34 a 104), lo mismo, con 70 base: queda a menos de 0,25 px a los 746.

### 4.2 La vuelta nueva y la repetición nueva (VID-02, VID-04)

`t = 0` en el cuadro del segundo que trae la vuelta, el mismo de la banda.

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 |
|---|---|---|---|---|---|---|---|---|
| Las filas que estaban bajan una fila: corrimiento −22 → 0 (0-160, `ease.move`), base | −22,0 | −21,0 | −15,6 | −4,6 | −0,6 | 0 | 0 | 0 |
| La fila nueva: opacidad (60-240, `ease.enter`) | 0 | 0 | 0,113 | 0,529 | 0,787 | 0,931 | **0,988** | 1 |
| La fila nueva: −8·sp → 0 (60-240, `ease.enter`), base | −8,00 | −8,00 | −7,10 | −3,77 | −1,70 | −0,55 | −0,09 | 0 |
| La quinta (o la cuarta repetición) se va (0-120, `ease.exit`) | 1 | 0,978 | 0,823 | 0,422 | 0 | 0 | 0 | 0 |

| t (ms) | 0 | 60 | 120 | 240 | 600 | 800 | 1000 | 1200 | 1400 |
|---|---|---|---|---|---|---|---|---|---|
| La marca de la fila nueva (entra con su fila y se apaga en 600-1400, `ease.move`) | 0 | 0 | 0,702 | 1 | 1 | 0,929 | 0,500 | 0,071 | 0 |

- **Por qué este orden.** La lista baja primero (a los 100 ms ya dejó libre el 79 % del lugar de arriba) y la vuelta nueva entra después, desde arriba, "desprendiéndose" de la vuelta en curso. Es lo mismo de kcal y BPM en la 4a: lo nuevo espera a que lo viejo le deje la mitad de su lugar.
  - A los 200 ms se lee entera (AC-WKT-18-19 pide 200 o menos).
- **La que se va** baja con las demás y queda recortada por el área de las filas: nunca pasa por encima de la línea `Rep`.
- **El alto.** Si había menos de 4 vueltas, el panel crece 22·sp con `spring.smooth` desde los 0 y el panel de abajo lo sigue. Si había 4, el alto no cambia.
- **La marca** (§3.4): la barra de 3·sp a la izquierda de la fila, a 4·sp del borde del vidrio, dentro del relleno. Reemplaza al fondo ámbar al 20 % de AC-WKT-18-19, que dejaba el valor en 4,857 (§3.7). Es "el realce que se apaga" de la mejora EXR del inventario, sin pasar debajo del texto.
- **La vuelta en curso** cambia con su rodillo de 180, en ese mismo cuadro: de `2:04.9` `499 m` a `0:00.1` `2 m`, por ejemplo.
- **Varias vueltas en un segundo** (AC-WKT-07-04): la lista baja 22·k en los mismos 0-160 y las k nuevas entran juntas, cada una con su marca.
- **Una repetición nueva** en `Series anteriores` (hasta 3) hace lo mismo, con la marca en `metric.distance`. Si cambia la escala común de las filas (§3.3), cambia con el corrimiento. La línea `Rep r/R · m` de `Parciales` cambia con su rodillo.
- **Con el panel cerrado** no se anima nada: el chip no cambia. Al abrirlo, la lista ya está, sin marcas.

### 4.3 El cambio de paso, la serie que empieza y la que termina (VID-03, VID-04)

Los paneles pasan al paso nuevo **un segundo después** que la barra (P-01, AC-WKT-18-08). Así su cambio no se suma al de las píldoras, la cuenta grande y el sello de la 4b, que pasaron el segundo anterior.

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 |
|---|---|---|---|---|---|---|---|---|---|---|
| El contenido viejo sale (0-120, `ease.exit`) | 1 | 0,978 | 0,823 | 0,422 | 0 | 0 | 0 | 0 | 0 | 0 |
| El nuevo entra (120-280, `ease.enter`) | 0 | 0 | 0 | 0 | 0,226 | 0,646 | 0,872 | 0,973 | 0,999 | 1 |
| El nuevo sube +4·sp → 0 (base) | 4,00 | 4,00 | 4,00 | 4,00 | 3,10 | 1,41 | 0,51 | 0,11 | 0 | 0 |

- **El alto.** Con `spring.smooth`: desde los 0 si crece y desde los 80 si se achica, así el contenido nunca se recorta mientras se ve.
- **Del trabajo al descanso.** Sale la vuelta en curso con su lista y entra `Sin parciales en este paso`. La línea `Rep` no sale: cambia con su rodillo.
- **Del descanso al trabajo nuevo.** Entra `0:00.0` `0 m` (o el primer segundo).
- **La serie que empieza.** El panel de repeticiones aparece debajo, en su estado guardado (abierto o cerrado), sin abrirse: opacidad 0 → 1 y +8·s → 0 en 120-400 (`ease.enter`), como una pieza de la cascada. Abajo no tiene nada que se mueva. Valores por cuadro: 0,134 a los 133 ms, 0,634 a los 200, 0,952 a los 300 y 1 a los 400.
- **La serie que termina.** El panel se va con opacidad 1 → 0 en 0-120 (`ease.exit`), sin moverse, y sale del árbol. Si tenía el foco, el siguiente Tab va a `Terminar` (AC-WKT-18-12).

### 4.4 SPM que cambia de alto

En el cambio de paso, la columna sigue el borde de abajo de SPM con el tiempo de 04a/04-design-notes §5.5: es el mismo movimiento que hace Watts debajo de Split.

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 240 | 280 |
|---|---|---|---|---|---|---|---|---|---|
| SPM gana `obj.`: la columna baja 16,9 (0-240, `ease.move`), base | 0 | 0,3 | 1,6 | 4,8 | 10,9 | 14,9 | 16,4 | 16,9 | 16,9 |
| SPM pierde `obj.`: la columna sube (80-280, `ease.move`) | 16,9 | 16,9 | 16,9 | 16,7 | 15,5 | 11,4 | 4,3 | 0,7 | 0 |

### 4.5 La entrada, la salida, la pausa, el segundo plano y la rotación

- **La entrada (AC-WKT-13-06).** Los paneles van con el tramo de SPM: de 520 a 800, opacidad 0 → 1 y +8·s → 0, con `ease.enter`.
  - Llegan en su estado guardado y con el primer paso (DV-48), sin abrirse ni cerrarse.
  - Responden desde que empiezan a aparecer (AC-WKT-13-07).
  - Con reducir movimiento, el fundido de 120 de la 4b, con todo el HUD.
- **La salida (AC-WKT-13-08).** Con el HUD, 0-200 (`ease.exit`), quietos y con la última foto. Lo que corría (un resorte, una marca) termina con su reloj o se detiene a los 640 (AC-WKT-13-09).
- **La pausa.** Nada cambia ni se atenúa (ART_BIBLE §3.2). Los paneles se abren y se cierran igual.
- **El segundo plano y el despertar (AC-WKT-02-08).** Se ve el presente en el primer cuadro: las filas en su lugar, sin marcas, sin empuje y sin rodillos.
- **La rotación** (AC-WKT-18-20). En el acto, con el HUD: la geometría nueva, `sp` nueva, el estado y los valores conservados y el desplazamiento en 0. Sin animación, como la 4b.
- **Con un diálogo abierto**, los paneles quedan debajo del velo y no responden.
- **El panel del simulador** abierto puede taparlos en parte (AC-BLE-16-03).

### 4.6 Reducir movimiento (AC-APP-05-08, VID-08)

| Pieza | Con reducir movimiento |
|---|---|
| Abrir | el alto en el cuadro 0; el contenido en 0-160, lineal (0,206 · 0,419 · 0,625 · 0,831 · 1 a los 33 · 67 · 100 · 133 · 167); el título, con su fundido de 0-80 / 80-200; la flecha cambia de glifo con un fundido de 80, sin girar |
| Cerrar | el contenido en 0-120, lineal (0,725 · 0,442 · 0,167 · 0); el alto a los 120, en el acto |
| El panel de abajo | en el acto, en el mismo cuadro que el alto |
| La vuelta nueva | las filas en su lugar nuevo en el cuadro 0; la nueva funde en 0-160, sin deslizarse; la que se va funde en 0-120; **la marca, igual** (es opacidad, no movimiento) |
| El cambio de contenido | los mismos fundidos (0-120 / 120-280), sin subir; el alto en el acto, a los 0 si crece y a los 120 si se achica |
| La serie que empieza y la que termina | 120-400 y 0-120, solo opacidad |
| La columna que sigue a SPM | en el acto (como `obj.` en la 4a) |
| El desplazamiento | sin la inercia del resorte de la rueda |

Nada de esto cambia los datos ni cuándo pasan.

### 4.7 Constantes (para `MotionSpecs` y `HudGeometry`)

**`HudGeometry`** (base):

- el ancho: `SeriesPanelWidth` 137 (= `ScaleShortSide` / 2 − `BigCountDiameter` / 2 − `BigCountGap` − `Side`);
- el relleno: `SeriesPanelPadH` 10 y `SeriesPanelPadV` 8;
- los aires de la columna: `SeriesColumnGap` 8 (debajo de SPM y entre los dos);
- los altos: `SeriesChipHeight` 34, `SeriesHeaderHeight` 18, `SeriesHeroHeight` 28 y `SeriesRowHeight` 20;
- los aires de adentro: `SeriesBlockGap` 6 (cabecera → contenido y lista → `Rep`), `SeriesRowsGap` 4 (vuelta en curso → lista), `SeriesRowGap` 2, `SeriesLabelGap` 4 (rótulo → valor), `SeriesMetersMinGap` 6 y `SeriesHeroMetersGap` 6;
- la cabecera: `SeriesIcon` 14, `SeriesIconGap` 4 y `SeriesChevron` 18, con la tinta de `expand_more` en 0,261-0,739 em;
- la marca: `SeriesMarkerWidth` 3 (`stroke.accent`) y `SeriesMarkerInset` 4;
- el desplazamiento y el toque: `SeriesScrollFade` 12, `SeriesDragSlopDp` 8 y `SeriesHitMinDp` 48 (56 donde hay lugar);
- la escala: `SeriesMinViewHeight` 182 y `SeriesMinScale` 0,9;
- y `PanelEmptyLines` 2.

**`MotionSpecs`** (ms):

| Constante | Valor |
|---|---|
| `SeriesHeight` | `spring.smooth` (ω 12) |
| `SeriesShrinkDelay` | 80 |
| `SeriesContentIn` | 80-240, `ease.enter` |
| `SeriesContentOut` | 0-120, `ease.exit` |
| `SeriesLabelOut` · `SeriesLabelIn` | 0-80 · 80-200 |
| `SeriesChevron` | 240, `ease.move` |
| `SeriesRowsPush` | 0-160, `ease.move` |
| `SeriesRowIn` | 60-240, `ease.enter`, con `shift.s` |
| `SeriesRowOut` | 0-120, `ease.exit` |
| `SeriesMarkerOut` | 600-1400, `ease.move` |
| `SeriesSwapOut` · `SeriesSwapIn` | 0-120 · 120-280, con +4 |
| `SeriesRepsAppear` · `SeriesRepsLeave` | 120-400 con +8 · 0-120 |
| `SeriesCascade` | el tramo de SPM: 520, 280 |
| `SeriesReducedIn` · `SeriesReducedOut` | 160 · 120, lineal |
| El rodillo del desplazamiento | `spring.snappy` |

---

## 5. Presupuesto y rendimiento (R4a-03; AC-WKT-18-21)

- **Un elemento por panel, en cualquier estado.** Cada panel dibuja todo con su `generateVisualContent`: el vidrio con su alto visual, la cabecera, la flecha, la vuelta en curso, las filas, la línea, los vacíos, la marca y los fundidos del desplazamiento.
  - El texto va con `MeshGenerationContext.DrawText` y las cifras en celdas tabulares, como `TabularNumber` (y con sus rodillos). Las píldoras de la 4b ya son un elemento.
  - La spec calcula unos 28 con un elemento por texto (§12 punto 5). Así son **2**.
- **Las pruebas** leen las filas por índice en la vista (`hud-laps-row-<n>` como datos de la vista, DT4b-01).

| Caso | Elementos a la vista |
|---|---|
| El peor caso de la 4b, en marcha (02-tech-design de la 4b §3.11: 58; `HudBudgetTests`: "59 o menos", con el sello en dos etiquetas) | 58-59 |
| + `Parciales` y `Series anteriores`, abiertos, con la vuelta nueva entrando (1 + 1) | **60-61** |
| Juntas que pido: M3 (`Próximo:` y su tipo en una etiqueta, −1) y M5 (`margin-left: auto` en lugar del espaciador, −1), opcionales en la 4b | **58-59** |

- **El margen.** Quedan 1 o 2, que es lo que necesita la 6a si corta la fila de píldoras en dos (R4b-02, +1).
  - Si M3 o M5 no se pueden, el sello en una etiqueta (−1) o un tope nuevo medido, en ARCHITECTURE §20.2. Eso es del Architect.
- **El peor caso de la 5** para `HudBudgetTests`: el de la 4b más una serie, con los dos paneles abiertos (preferencias de prueba en verdadero), 4 vueltas, la línea, 3 repeticiones y una vuelta entrando (AC-WKT-18-21). En los cuatro perfiles de la prueba, más P360.
- **0 B por cuadro.**
  - Los textos se arman una vez por segundo, con los datos, en *buffers* de caracteres precalculados como los de `TabularNumber`. Ninguna cadena nueva por cuadro.
  - Un panel se repinta solo cuando cambia un dato (una vez por segundo) o mientras anima.
  - Se mide con `Tests/UiSupport/GcAllocations` (el registrador de `GC.Alloc`), no con `GC.GetAllocatedBytesForCurrentThread`, que en Mono da 0 (lección de la 4b).
- **QA-V4b-06.** Los paneles se arman con el HUD (en el cuadro negro de la entrada o al abrir Workout), con sus dos formas. El primer toque no arma nada ni mide texto, así no hay un tirón al abrir por primera vez.

---

## 6. Capturas y videos: qué cambia en lo que espera la spec (§7.3 y §7.5)

| ID de la spec | Lo que tiene que verse con este diseño |
|---|---|
| CAP-01 | Los dos chips de 137 × 34·s debajo de SPM, a 8·s, y 8·s entre ellos; en TV, de y 228,6 a 308,7 (SPM con objetivo) o desde 210,9 (sin) |
| CAP-03 | Con el contorno de la zona: el panel abierto entra en ella solo hasta x 159,1 (no 225,6). Nunca en la columna del disco |
| CAP-14 (P360) | El panel en x 12,9-139,4; nada cortado: las filas de repeticiones con 4 cifras de metros, a 0,949 |
| CAP-15 (TH) | Los dos chips enteros, sin desplazar (76 de 77,1 base) |
| CAP-16 (TH) · CAP-17 (L640) | La columna desplazable (regla 4 de §2.4): se ven sin desplazar la cabecera de `Parciales` y la vuelta en curso, y abajo el fundido de 12·s. En L640 **el panel no se angosta**: 137 base terminan en 139,4 dp, 39,8 antes de la zona |
| CAP-18 (W1280) | **`sp` = 1,20**: el panel de 164,4 dp desde x 22,4; sin desplazar, `0:15.0` `60 m`, `#1 2:05.0`, `Rep 2/3 · 560 m` y `Rep 1 2:05 680 m` |
| CAP-19 | El aire entre los chips y el disco es **8,4 dp** (8·s); resuelve la discrepancia de §15 de la spec |
| CAP-20 · CAP-22 | **La cuenta grande y `PAUSA` en su lugar de la 4b**, con `Parciales` abierto al lado, a 8,4 dp |
| CAP-25 | Las escalas de §3.3 (`S-LARGO`) |
| VID-01 | Solo el alto, con los valores de §4.1; el ancho no cambia |
| VID-02 | La lista que baja, la fila que entra y **la marca**, en lugar del fondo ámbar (§4.2) |
| VID-08 | Los valores de §4.6; la marca igual |
| VID-09 | **No hay nada que salga**: la cuenta grande sigue, con el panel abriéndose a 8,4 dp de ella |

**Reglas de toma**, además de las de la spec (§7.1):

1. **Un panel recién abierto o cerrado** se fotografía al menos **0,82 s** después del toque. A los 0,6 s de la spec, al resorte le falta 0,9 base (2,4 px en TV). También vale arrancar con la preferencia en verdadero.
2. **Ninguna toma fija cae en 0-1400 ms después de una vuelta o una repetición nueva**, salvo la que muestra la marca a propósito. El video la muestra entera.
3. **En las tomas con desplazamiento**, se usa el desplazamiento en 0 (arriba).
4. **Sondas sugeridas:** el vidrio y los valores con los compuestos de §3.7, la marca en su pico en VID-02 (a los 240-600 ms) y el aire de 8,4 dp al disco en CAP-19 y CAP-20.

---

## 7. Pedidos al Architect

Ninguno cambia el comportamiento del producto. Los que dependen de lo que cambia en la spec (§8) dicen "(spec)".

| ID | Qué | Cómo se verifica |
|---|---|---|
| **DD5-01** | **El modo compacto de la barra libre** (QA-V4b-R2-01, §1.3). En `InfoRowFit`: la regla (compacto si `FreeTitleScale` con las medidas normales da menos de 1) y las constantes `FreeCompactTitleClockGap` 10, `FreeCompactClockIconGap` 3, `FreeCompactChipGap` 8 y `FreeCompactChipPadH` 6. `PhaseChip.FixedWidthBase` con el relleno como parámetro. `FreeTopBar`, `WallClockView` (el aire del prefijo, solo en la barra libre) y `PhaseChipView` lo aplican en el acto, con la ventana y el idioma. El reloj de la barra de pasos no cambia | dotnet: compacto en TV y P360 en español y normal en inglés y en TH, L640 y W1280; el título a 0,805 ± 0,003 en los dos modos; lo demás, 210,8 en los dos. PlayMode: en TV, en español, del borde del título al del reloj 10·s ± 0,5 dp, el prefijo a 3·s, el chip a 8·s y su ancho 97,3 ± 1 base. En inglés, los valores de hoy |
| **DD5-02** | **Las tomas de QA-V4b-R2-01**: CAP-13, CAP-14 y CAP-15 de la 4b, y los videos de la 4b con la barra libre en TV en español, regenerados en dos corridas iguales (la regeneración única de la spec, §7.1) | Los píxeles de §1.4 (± 1 px); el *manifest* con el chip en 97,3 base y el título en 0,805; CAP-16 a CAP-18, DEMO-02 y DEMO-03, sin cambios |
| **DD5-03** | **La geometría de los paneles** en `HudGeometry` (Core, pura): el ancho (`min(137; ancho base / 2 − 58)`), la columna (techo, piso y la que sigue a SPM), los altos de §2.4, la vista mínima y `sp` (reglas 1 a 4), el área de toque y la regla de la zona del bote: cerrados nunca; abiertos en vertical, solo en `x ≤ ancho/2 − 44·s`; en horizontal, nunca (spec) | dotnet: los rectángulos de §2.2 y §2.3 (± 0,1), `sp` = 1,20 en W1280 y `s` en los demás, la regla elegida en cada perfil, los dos chips enteros en los cinco y las áreas de toque (≥ 48 dp, sin superponerse) |
| **DD5-04** | **Los paneles, un elemento cada uno** (§3, §5): el vidrio con su alto visual, el contenido recortado, la cabecera, la flecha dibujada, la vuelta en curso con su `FitBox`, las filas con la columna del rótulo y la escala común de las repeticiones, la línea, los vacíos en dos líneas, la marca y los fundidos del desplazamiento. Las cifras, tabulares y con rodillo | PlayMode: un `VisualElement` por panel; los textos y las escalas de §3.2 y §3.3 (± 0,01), también `S-LARGO`; las sondas de §3.7 en las tomas |
| **DD5-05** | **El toque, el desplazamiento, el estado y el foco** (§2.3, §2.4, §3.5): el área de toque de cada chip, todo el panel abierto, los 8 dp que separan el toque del arrastre, la rueda de a una fila, la capa solo en la cabecera y el anillo que sigue el alto visual. Tab con el orden de la spec | PlayMode: un toque a 3 dp del borde de abajo de `500 m` abre `500 m` y no `Reps`; un arrastre de 20 dp no abre; la capa no cubre el contenido; el anillo sigue al alto en cada cuadro del resorte |
| **DD5-06** | **Abrir y cerrar** (§4.1): solo el alto con `spring.smooth`; el panel de abajo por FLIP; los fundidos del contenido y del rótulo; la flecha; la inversión a mitad de camino | PlayMode con el reloj inyectado: las tablas de §4.1 (± 0,01 y ± 0,1 base), el aire de 8·sp en cada cuadro y que nada más se mueve |
| **DD5-07** | **La vuelta nueva, la repetición nueva, el cambio de contenido, la serie que empieza o termina y la columna que sigue a SPM** (§4.2 a §4.4), con varias vueltas en un segundo; la entrada, la salida, el segundo plano y la rotación (§4.5); reducir movimiento (§4.6) | PlayMode cuadro a cuadro con las tablas (± 0,01); `Instant` sin marcas ni empuje; la secuencia del motor igual con y sin paneles (AC-WKT-13-14); VID-02 a VID-04, VID-06 a VID-08 |
| **DD5-08** | **Los tokens** (§3.4): `num.lap` 28/28, `num.row` 20/22 y `num.xs` 13/16/700, nuevo (136 → 137), con `tools/tokens`, `Tokens.uss`, `Typography.uss` y los conteos | `TokenCatalog` en 137; `Typography.uss` sin diferencias con lo generado |
| **DD5-09** | **Los pares de contraste** C1 a C9 de §3.7 en `ContrastTests.ItFiveDesignerPairs`, con sus valores de control | dotnet: cada par sobre su umbral; los valores a 3 decimales |
| **DD5-10** | **`HudOverlap` y `WrenchAudit` con los paneles**: los paneles nunca se enciman con una pieza ni con la columna del disco; la zona del bote con la regla de DD5-03; la llave no los tapa (AC-BLE-16-02) | En toda toma con rutina, en los cinco perfiles, cerrados y abiertos |
| **DD5-11** | **El presupuesto** (§5): M3 y M5 (o lo que el Architect mida como equivalente), el peor caso de la 5 en `HudBudgetTests` y 0 B por cuadro con los paneles animando | `HudBudgetTests`: ≤ 60 en el peor caso de la 5, en TV, TH, L640, W1280 y P360; `GcAllocations` en 0 B durante abrir, cerrar y una vuelta nueva |
| **DD5-12** | **Las constantes** de §4.7, **el arnés** con las reglas de toma de §6 y **los paneles armados con el HUD** (QA-V4b-06) | dotnet: que las tablas salgan de las constantes. Dos corridas iguales. `[PERF]` en la entrada con los dos paneles abiertos (AC-WKT-13-15) |

**Prioridad:**

1. DD5-01 y DD5-02, la deuda de la 4b, antes de construir encima (spec §5.1).
2. DD5-03, DD5-08 y DD5-11: la geometría, los tokens y el presupuesto, sin los cuales los paneles no entran.
3. DD5-04 a DD5-07.
4. DD5-09, DD5-10 y DD5-12.

---

## 8. Para el PO y para QA

### 8.1 Lo que este diseño cambia de la spec (para el gate de armonía)

| Criterio o pregunta | La spec dice | Propongo | Por qué |
|---|---|---|---|
| AC-WKT-16-02, -03, -07 y AC-WKT-19-01 | 10 al chip, relleno 8 × 3, 5 del ícono a sus cifras; "el resto de la barra no cambia" | el modo compacto de §1.3, solo en TV y P360 en español | §1.2: es lo único que separa el título sin bajarlo de 0,8 |
| AC-WKT-18-02 | chip con relleno 14 × 10 y un mínimo de 140 | **137 × 34**, relleno 10 × 8 | §2.2 y §2.3: entra junto al disco y los dos entran en TH y L640 |
| AC-WKT-18-03 | abierto, 200 fijo | **137 fijo** (× `sp`) | §2.2 |
| AC-WKT-18-04 | `num.lap`; metros "al 55 %"; `#n` 15/600 al 60 % en una columna de 52; `num.row` | `num.lap` **28**; metros en `.hud @80 %`; `#n` 12/600 `text.secondary` en una columna del ancho de `#99`; `num.row` **20** | §3.2 y §3.7: al 55 % los metros dan 3,003 |
| AC-WKT-18-07 | metros 16/700; columna de 52 | metros **13/700** (`num.xs`); columna del `Rep n` más ancho; una escala común | §3.3 |
| AC-WKT-18-13 | 10·s debajo de SPM; en L640, el segundo chip se desplaza | **8·s**; los dos chips entran en los cinco perfiles; las reglas de §2.4 (en W1280, `sp` 1,20 y filas desplazables adentro de cada panel) | §2.3 y §2.4: es lo que cumple las garantías de la spec en W1280 con 4 vueltas y 3 repeticiones, no solo en CAP-18 |
| AC-WKT-18-14 · P-03 | en vertical, abiertos hasta 225,6 en TV; en L640, angostarlo | en vertical, abiertos solo hasta 159,1 (el 24 % de afuera); **nunca la columna del centro**; en horizontal nunca, sin angostar nada | §2.2 |
| AC-WKT-18-15 · P-04 · AC-WKT-13-12 · AC-WKT-14-18 · demo (b) | en vertical, con un panel abierto, la cuenta grande y `PAUSA` no se muestran (o en un segundo lugar) | **se muestran siempre en su lugar de la 4b**; ya no hace falta la regla "se van antes de que el panel llegue" | §2.2: el panel nunca llega a la columna del disco |
| AC-WKT-18-18 | "alto y ancho"; el contenido "entra o se va" en 160 demorado 80 | solo el alto; entra en 80-240 y **se va en 0-120** | §4.1: si se fuera desde los 80, el alto lo recortaría mientras se ve |
| AC-WKT-18-19 · AC-APP-05-08 | la fila crece de 0 a su alto en 200, desde −8 dp, con un fondo ámbar al 20 % que se apaga en 1,2 s | la lista baja en 0-160 y la fila entra en 60-240 desde −8·sp; **una marca ámbar de 3·sp a la izquierda** que se apaga en 600-1400 | §3.7: el fondo al 20 % deja el valor en 4,857 (< 7) |
| AC-WKT-18-21 | el tope de 60 o uno nuevo | un elemento por panel; M3 y M5 | §5 |
| Spec §7.1 | "el toque al menos 0,6 s antes" | **0,82 s** | §6, regla 1 |

### 8.2 Las preguntas de la spec que me tocaban

- **P-03:** sí, solo los abiertos, y solo en la franja de afuera. Se revisa en la 7 con el bote (§2.2).
- **P-04:** no hace falta un segundo lugar.
- **P-05:** la zona del legacy, con la regla de §2.4. No propongo otra zona.
- **P-06:** el orden de la spec.
- **P-07:** los textos de la spec entran todos: los chips son iguales en inglés, `500 m splits` mide 65,2, `Previous reps` 72,6, y los vacíos van en dos líneas. Para cualquier idioma nuevo, los topes son:
  - el rótulo del chip, 81,7 base a 13/600 con 0,08 em;
  - el título, 85,7 a 11/600 (107,1 achicado a 0,8);
  - los vacíos, dos líneas de 117 a 15/400.

### 8.3 Las discrepancias de la spec (§15) que me tocaban

- **El aire de CAP-19:** 8,4 dp con el chip de 137 (era ≈ 5 con 140).
- **El lugar debajo de SPM:** §2.3 y §2.4.
- **El "tablet" de ART_BIBLE §3.4:** no hay variante de tablet. Es un solo ancho, 137 × `sp`, en toda ventana.

### 8.4 Decisiones chicas, con valor por defecto

| Decisión | Por defecto |
|---|---|
| El título abierto, en 11/600, se achica parejo hasta 0,8 (`Series anteriores` a 0,949) en lugar de recortarse con `…` | Sí. Es la regla del HUD: lo que no entra se achica |
| Las filas de repeticiones se achican parejas (0,949 con metros de 4 cifras) | Sí. Es DV-49 |
| La alternativa de `Series anteriores` debajo de Watts en vertical, para no tapar la pala izquierda | Se decide en la 7, con el bote a la vista |

### 8.5 Para QA

- **Las tomas de §1.4**, en dos corridas iguales.
- **Los rectángulos de §2.2 y §2.3** en el *manifest* de CAP-01, CAP-03, CAP-14 a CAP-20.
- **Los pares de §3.7.**
- **Las reglas de toma de §6.**
- **El peor caso del presupuesto** de §5.
- **El fundido de 12·s** del borde que se desplaza es decorativo: no tiene umbral.

### 8.6 Fuera de esta iteración

- **El aviso de cada vuelta con la diferencia contra la anterior** (P-09, BKL-26).
  - Si algún día se hace, el lugar natural es la tercera fila de la barra, la del sello: 2 s, el mismo mecanismo y 0 elementos más. El sello tiene prioridad en un cambio de paso.
- **La boya de 500 m en el agua** (6a / 7).
- **El vidrio con desenfoque** (6b).

---

## 9. Biblias: qué se alinea después del gate

| Biblia | Dónde | Qué cambia |
|---|---|---|
| ART_BIBLE | §3.3 (barra libre) | El modo compacto (§1.3). Corrige "con 6 de aire hasta el reloj" (son 2 en el modo normal y 10 en el compacto) y "cerca de 0,82" (es 0,805) |
| ART_BIBLE | §3.4 | Se reescribe con §2 y §3 de este documento: 137 fijo, la columna, `sp`, la anatomía, los colores y la marca. Sale "expandido fijo 200 (en tablet 240)" |
| ART_BIBLE | §4.3 | `num.lap` 28/28, `num.row` 20/22 y `num.xs` 13/16 |
| ART_BIBLE | §5.2 | La zona del bote: "salvo los paneles de series abiertos en vertical, en su franja de afuera, nunca en la columna del centro" (P-03). La columna de los paneles y su regla de lo que no entra |
| ART_BIBLE | §10.1 y §10.2 | El grupo C1-C9 y la tabla de §3.6 |
| ANIMATION_BIBLE | §2.10 | Se reescribe con §4.1 a §4.6: sale "alto y ancho (140↔200)" y el fondo `metric.split @20 %` |
| ANIMATION_BIBLE | §2.11 | La cascada con los paneles en el tramo de SPM |
| Las dos | Al final | "Cambios al iniciar la It. 5" |

---

## 10. Verificación hecha

1. **Legacy, solo lectura.** Se leyeron los archivos del encabezado. `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar, y no se leyó `strava_secrets.dart`.
2. **Las tomas de la 4b**, miradas y medidas:
   - CAP-13 a CAP-15 y CAP-18: las corridas de tinta de §1.1, iguales en las tres de TV;
   - CAP-13: dónde quedan SPM, Split, Watts, la banda, kcal, la llave y los controles, que coinciden con 04a/04-design-notes §3.8;
   - `docs/image-03.png` del legacy: los paneles de 200 a la izquierda, `0:55.0 207 m`, `Rep 2/3 · 207 m` y `Rep 1 2:13 452 m`.
3. **Los anchos de texto** se midieron con las tablas K2 versionadas y fontTools 4.68.0. Los anchos que Unity mide en la barra libre (155,4, 69,5 y 101,3) salen del *manifest* y de 05-qa-verification de la 4b, R2.2.
4. **La geometría** sale de las fórmulas de `HudGeometry` (escala, tarjetas, banda, zona del bote y disco) y reproduce los rectángulos de 04a/04-design-notes §3.8 y de 04b/04-design-notes §3.6.
5. **Los contrastes**, con el método de `Contrast.cs`. **Las curvas**, con las cubic-bezier de `Tokens.cs` y la solución exacta de `CriticalSpring`.
6. **El repositorio.** Se escribió solo este documento. No se tocó código, tokens, assets, capturas ni biblias. Unity no se abrió y dotnet no se corrió.
