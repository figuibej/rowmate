# It. 5 — Notas de diseño visual y de motion

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-30 |
| Estado | **Notas para WP-D1 y WP-B1 a WP-B3, corregidas por el gate de armonía** ([03-qa-harmony-paridad](03-qa-harmony-paridad.md), [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md)); lo que cambió está en "Cambios por gate QA", al final. Responden a DN5-01 a DN5-05 y a los desvíos DT5 que me tocan (§1). Corrigen la base de diseño ([00-deuda-diseno](00-deuda-diseno.md)) donde la revisé con las tablas por cuadro (§2) y piden **DD5-13 a DD5-28** (§10). Lo que cambia un texto o un número de la spec ya lo decidió el PO: SD-12 a SD-19, adoptados ([01-po-spec §11.3](01-po-spec.md); §11 de este documento). **WP-D2 hecho** (§15, "Cambios por implementación de WP-D2"): las tomas y los videos de `it5` mirados cuadro por cuadro, el vidrio contra las tarjetas, las sondas de aspecto en `SeriesCatalog` (27, todas dentro de su tolerancia) y las biblias; §9.4 y §9.6 quedan corregidas con lo medido |
| Iteración | 5 · `05-series-parciales` ([ROADMAP §It. 5](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (§0.4, §1, §4.3, §5.2, §7, §10, §11, §14, §15); [02-tech-design](02-tech-design.md) (§3, §4, §5, §7.3 a §7.6 y §8: DT5-01 a DT5-13, SD-01 a SD-11, DN5-01 a DN5-05); [00-deuda-diseno](00-deuda-diseno.md) (mi base: §2 a §7); [04-design-notes de la 4b](../04b-hud-exr/04-design-notes.md) (§3.6, §4, §7, §8); [ART_BIBLE](../../design/ART_BIBLE.md) (§2.4, §3.2, §3.4, §4.3, §5.2, §10); [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) (§0.1, §1, §2.3, §2.10, §2.11). Del código, sin tocarlo: `Presentation/Workout/HudCard.cs` (el borde y el halo de las tarjetas), `Presentation/Styles/{Base,Skin,Tokens}.uss` (el vidrio y el anillo de foco del HUD), `Core/Design/HudGeometry.cs` y `Tests/Support/RoutineSeeds.cs` (qué rutinas tienen objetivos). Capturas de la 4b: CAP-02, -21, -23, -24, -27 y DEMO-08 |
| Entrega | Este documento. No cambia código, tokens, capturas ni biblias |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/series_panels.dart` entero (el chip, la cabecera, las filas alineadas por la línea de base y el relleno de 2 arriba y abajo de cada fila), `lib/features/workout/immersive_workout_screen.dart:760-805` (el rótulo de las tarjetas: el mismo estilo que el del chip) y `docs/image-03.png`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificado | Programas propios en `<scratch>/it5/designer-notes/`, sin red:<br>- `probe_cards.py` y `probe_cards2.py` (Pillow): el vidrio y el borde de las tarjetas en las tomas de la 4b, píxel a píxel;<br>- `metrics5.py`: las métricas de las fuentes (altura de mayúscula, ascendente y descendente) y los anchos de los textos en es y en, con las tablas K2 versionadas y el *tracking* de UI Toolkit (fontTools 4.68.0 por `uv run --offline`);<br>- `geo_it5.py`: la columna y los rectángulos de cada toma, por perfil, con las fórmulas de `HudGeometry`;<br>- `motion_it5.py`, `swap_check.py` y `data_resize.py`: las tablas por cuadro y el aire mínimo entre tintas en cada coreografía, con las curvas de `Tokens.cs` y la solución exacta de `CriticalSpring`;<br>- `probes5.py`: los compuestos de las sondas;<br>- para el gate, en `<scratch>/it5/designer-gate/`: `sd15_17.py` y `sd15_19b.py` (los anchos con el estilo del chip, dónde quedan los metros y la flecha, los altos con el aire del legacy, los contrastes al 80 % y la capa de estado por cuadro).<br>Reusan `measure4b.py` y `rmcore.py` de `<scratch>/it5/designer-debt/`. **Unity no se abrió** y dotnet no se corrió |

Convenciones: las de [00-deuda-diseno](00-deuda-diseno.md) (hex `#RRGGBB`, transparencia `@NN %`, WCAG 2.x sin redondear, 1 px de USS = 1 dp). Perfiles: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720. `s` = `hudScale` (1,054 · 0,923 · 1,054 · 0,923 · 1,6); **`sp`** = la escala de los paneles (= `s`, salvo W1280: 1,20). "Base del panel" = unidades del panel antes de multiplicar por `sp`, con el origen en su esquina de arriba a la izquierda. **Renglón** = cada línea que dibuja el panel (la cabecera, la vuelta en curso, cada fila, la línea `Rep`, cada línea de un vacío). **Tinta** = la franja vertical que ocupan los glifos de un renglón (la banda de las mayúsculas más los descendentes). Las tomas de TV y TH tienen 2,628 px por dp; las de P360, L640 y W1280, 1 px por dp. **[cambia]** = distinto de 02-tech-design o de 00-deuda-diseno, y va pedido en §10.

---

## 0. Resumen

1. **La base de diseño queda** ([00-deuda-diseno](00-deuda-diseno.md) §2 a §6, construida por el Architect como DT5-01): 137 base de ancho fijo, cerrado y abierto; el chip de 34; la columna a 8·s debajo de SPM; `sp` 1,20 en W1280; la columna desplazable en TH y L640; los tamaños `num.lap` 28, `num.row` 20, `num.xs` 13 y `num.stat` 16; la marca en lugar del fondo ámbar. **La cuenta grande y `PAUSA` se ven siempre en su lugar** (8,4 dp de aire en TV, 7,4 en P360) y los paneles nunca llegan al sello, a la llave ni a la columna del bote.
2. **DN5-01, el "se descubre por renglón"** (DT5-02). La fórmula de 02-tech-design deja **el último renglón al 67 % en reposo** (el vidrio termina 8 base debajo de él y el fundido es de 12). La reemplazo por tres reglas por la **tinta** (§5.3):
   - **revelar** (abrir y crecer): un renglón aparece cuando el vidrio ya cubre toda su tinta y llega a 1 en 4 base más; nunca se dibuja tinta fuera del vidrio;
   - **la ventana de filas**: una fila que se sale de su lugar se apaga en 4 base de desborde, así nunca pasa por encima de la línea `Rep` ni de la vuelta en curso;
   - **la columna desplazable** (TH y L640): una máscara de 12 base sobre el vidrio y el texto, solo si hay algo escondido.
3. **El alto que cambia por un dato va con el empuje, no con el resorte** [cambia]. Con el resorte, la línea `Rep` bajaba más despacio que lo nuevo que entraba arriba y se le encimaba hasta 2,8 base. Ahora el alto, la lista y la línea `Rep` se mueven juntos: crece en 0-160 y se achica en 80-240, con `ease.move`. El resorte `spring.smooth` queda para lo que hace el usuario: abrir y cerrar.
4. **La fila nueva entra en 80-240** [cambia] (no en 60-240): a los 62 ms su tinta tocaba la de la fila que bajaba. A los 200 ms se lee al 98,3 %, dentro de lo que pide AC-WKT-18-19.
5. **El cambio de contenido es por pieza** [cambia]: funde solo lo que cambia de clase (la vuelta en curso ↔ `Sin parciales en este paso`). La vuelta en curso de un trabajo que sigue a otro trabajo cambia con su rodillo, el vacío que sigue a otro vacío no se mueve, y las filas que se vacían se apagan en su lugar.
6. **El vidrio es el de las tarjetas, tal como está hoy** (DN5-02, medido en las tomas de la 4b): `glass.fallback`, borde de 1·sp blanco al 10 % y radio 14·sp. **Sin reflejo**: las tarjetas no lo tienen (ART_BIBLE §3.2 lo describe; llega con el vidrio de la 6b, para todas las piezas juntas).
7. **Los textos**:
   - los vacíos van en **dos líneas equilibradas** [cambia]: `Sin parciales` / `en este paso` y `No splits` / `in this step`, en lugar de `Sin parciales en` / `este paso`;
   - los dos títulos van a **una sola escala por idioma** [cambia]: 0,949 en español y 1 en inglés.
8. **La geometría, corregida con las rutinas reales**:
   - `Series 600` no tiene objetivos, así que en TV la columna empieza en **210,9 dp** (no en 228,6);
   - en TH y L640, sin objetivo, entran 93,9 base (no 77,1);
   - en W1280, el reparto reserva el mínimo de repeticiones antes de dar el lugar a parciales (DD5-26).
9. **El anillo de foco** es el de los controles del HUD (blanco 2 dp + halo `overlay.scrim` @50 % de 4 dp), con los radios 14·sp + 4 y 14·sp + 8. Las áreas de toque llegan a 56 dp, o a 48 como mínimo en P360 y L640.
10. **VID-08 no puede mostrar lo que pide**: con los dos paneles abiertos, el toque a los 323,8 s *cierra* parciales y la vuelta nueva de los 325 no se ve. Pido que arranque con parciales cerrado, así el toque lo abre (§11).
11. **Capturas y videos**: qué tiene que verse en CAP-01 a CAP-26, DEMO-01 a DEMO-12 y VID-01 a VID-09, con los rectángulos en dp, los cuadros clave y las sondas (§9). DN5-03: acepto la subida de 9,6 dp de la cascada en W1280 (DT5-10). DN5-04: "+24 la primera" (§2).
12. **Contraste**: los pares C1 a C9 de 00-deuda-diseno §3.7, sin cambios y todos sobre su umbral. No hay pares nuevos: el anillo es el de los controles (3,977:1).
13. **Presupuesto**: nada de esto suma elementos ni memoria por cuadro. Todos los alfas salen de extremos de tinta que se calculan cuando cambia el texto (una vez por segundo como mucho).
14. **La capa de *hover* y presionado** (gate, QA-G5T-11) es una fase finita de `SeriesPanelMotion`, con los tiempos de las capas de estado de la app (entra en 90 con `ease.enter` y sale en 160 con `ease.exit`): bloquea la regla de toma mientras corre, `Stop()` la deja en 0 y con reducir movimiento es igual, porque es opacidad (§6.10, DD5-28).
15. **Lo que se ve distinto del legacy adentro del panel** (gate, QA-G5P-02): el título en 11/600, los metros de la vuelta en curso a la derecha, el rótulo del chip en el color pleno, la flecha pegada a la derecha y 2 entre filas. Son **SD-15 a SD-19**, con sus motivos medidos en §11.1. El PO los adoptó (DV-50 y DV-51) y van al usuario con las demás DV.

---

## 1. Respuestas a los pedidos de 02-tech-design §8.4 y a sus desvíos

| Pedido o desvío | Respuesta | Dónde |
|---|---|---|
| **DN5-01** · El "se descubre por renglón" (DT5-02) | **Sí al renglón con alfa, sin recorte; no a la fórmula.** `clamp((bordeVisible − abajoDelRenglón) / 12·sp)` da 8/12 = 0,667 en reposo para el último renglón de cada panel, porque el vidrio termina 8 base (el relleno) debajo de él. Además, mientras crece deja tinta fuera del vidrio, porque la tinta de una fila de vueltas termina 3 base antes del fondo de su caja. Lo reemplazan tres reglas por la tinta (revelar, la ventana de filas y la columna), con sus constantes. Las reviso en VID-01, -02, -03 y -08 en WP-D2 | §5.3, DD5-13 a DD5-15 |
| **DN5-02** · El vidrio dibujado contra el de las tarjetas | Medí las tarjetas en las tomas de la 4b: relleno `#08162B` sobre el cielo y `#081627` sobre el agua, plano desde el borde (no hay reflejo); borde de 1·s (3 px en TV, 1,6 en W1280) `#212E41` / `#212E3D`. El panel va igual, con el borde de 1·sp. Las sondas, con su tolerancia, en §9.6 | §4.1, §9.6, DD5-21 |
| **DN5-03** · La subida de 9,6 dp de la cascada en W1280 (DT5-10) | **Se acepta.** El aire entre SPM y el panel pasa de 9,6 a 12,8 dp mientras los dos aparecen: vale `12,8 − 3,2·(1 − e)`, con `e` la curva de la cascada, así que cuando el panel está al 50 % el aire ya está en 11,2 dp. No se nota y no salta | §6.7 |
| **DN5-04** · "+4 la primera" | **Corregido:** la primera vuelta suma **24** base (4 de aire y 20 de fila) y cada una más, 22. Los totales de 00-deuda-diseno (114 y 180) ya estaban bien | §2 (R-01), §4.4 |
| **DN5-05** · Las biblias después del gate | En WP-D2, con este documento como fuente | §12 |
| DT5-01 · Construir con la base del Designer | Sí, con las revisiones de §2 | — |
| DT5-03 · La marca en lugar del fondo ámbar | Confirmada: 3·sp × 20·sp, radio 1,5·sp, de x 4 a 7 de la base del panel. Entra con su fila (80-240) y se apaga en 600-1400 | §4.4, §6.2 |
| DT5-04 · CAP-11 a los 881,2 s y DEMO-11 a los 31,6 s | Sí. Las dos caían en una fase finita (la marca y la aparición del panel). Lo que muestran no cambia | §9.2, §9.3 |
| DT5-11 · El anillo lo dibuja el panel | Sí, en dp de ventana (no escalado por `sp`): blanco de −4 a −2, halo de −8 a −4 | §4.9, DD5-22 |
| DT5-12 · Los paneles con el tramo de SPM | Sí | §6.7 |
| DT5-13 · `TabularCells` aparte de `TabularNumber` | Sin objeción de diseño: los dos tienen que dar el mismo rodillo (180, ANIMATION_BIBLE §2.4) y el mismo fundido de 80 con reducir movimiento | §6.0 |

---

## 2. Lo que cambia de la base de diseño (00-deuda-diseno de la 5)

Lo que no está en esta tabla sigue valiendo tal cual.

| # | Dónde | Decía | Queda | Por qué |
|---|---|---|---|---|
| R-01 | §2.4 | cada vuelta "+4 la primera, +22 cada una más" | **+24 la primera**, +22 cada una más | DN5-04; los totales no cambian |
| R-02 | §4.1 | el contenido "se recorta al alto visual menos el relleno" | se revela **por renglón, con la tinta** (§5.3, regla 1) | sin recorte (DT5-02), sin tinta fuera del vidrio y con todo al 100 % en reposo |
| R-03 | §2.4 | "el borde que recorta lleva un fundido de 12·sp" (en las dos reglas de desplazamiento) | en W1280 (filas desplazables), **4 base de desborde por la tinta**; en TH y L640 (columna desplazable), la máscara de **12** | sin recorte, la fila que se sale no puede pasar sobre la línea `Rep` (§5.3) |
| R-04 | §4.2 y §4.3 | el alto crece con `spring.smooth` "desde los 0" cuando entra una vuelta o cambia el contenido | **con el empuje**: crece en 0-160 y se achica en 80-240 (`ease.move`), con la lista y la línea `Rep` juntas | con el resorte, la línea `Rep` (que baja con el vidrio) y lo nuevo se enciman hasta 2,8 base (`swap_check.py`) |
| R-05 | §4.2 | la fila nueva entra en 60-240 | **80-240** | a los 62 ms su tinta tocaba la de la fila de abajo (−1,22 base); con 80, el aire mínimo es 5,48 |
| R-06 | §4.3 | "sale la vuelta en curso con su lista y entra `Sin parciales en este paso`" en todo cambio de paso | **por pieza** (§6.3): solo funde lo que cambia de clase | un trabajo que sigue a otro trabajo no tiene por qué apagar y volver a prender la vuelta en curso |
| R-07 | §3.2 y §3.3 | los vacíos en dos líneas, cortados donde se llena la primera | **corte equilibrado**: el de la línea más larga más corta | `Sin parciales en` / `este paso` deja la preposición colgada |
| R-08 | §3.3 | `Series anteriores` a 0,949 y `Parciales 500 m` a 1 | **una escala de título por idioma**, la menor de los dos (es 0,949; en 1) | los dos paneles van a 8 uno del otro, con el mismo ancho: dos títulos de tamaño distinto se notan |
| R-09 | §3 | "el reflejo interior de los 8 de arriba" | **sin reflejo**, como las tarjetas de hoy | DN5-02: las tarjetas no lo dibujan |
| R-10 | §3.5 | el anillo "radio 18" | radios **14·sp + 4** (anillo) y **14·sp + 8** (halo): 18,8 y 22,8 en TV | concéntricos con el vidrio, como los controles (radio 12 → 16 y 20) |
| R-11 | §4.1 | la flecha "sobre la línea central de `expand_more` en una caja de 24" | la misma forma, dada en base del panel (§4.3) | se dibuja con `Painter2D`, no con el glifo |
| R-12 | §6 | CAP-01 "de y 228,6 a 308,7 (SPM con objetivo)" | **210,9 a 291,0**: `Series 600` no tiene objetivos | `RoutineSeeds`; el número con objetivo vale para `Prueba 4a` y `Corta` |
| R-13 | §2.3 | TH y L640: 77,1 base | **77,1 con objetivo y 93,9 sin**; las tomas de la 5 en horizontal son sin objetivo | CAP-15, -16, -17, -21 y -23 |
| R-14 | §2.3 | el área de toque de `500 m` "llega a 58,9 dp" | tope de **56** (48 como mínimo) | ART_BIBLE §10.4: 56 en el inmersivo; más no suma y sí acerca el toque a SPM |
| R-15 | §6 | VID-08 con los dos abiertos | **arranca con parciales cerrado** | el toque tiene que abrirlo para que la vuelta nueva se vea |
| R-16 | §3.2 | "a 13 no entra ningún título …: `Series anteriores` pide 106,7" | con el estilo del chip, **120,3** (el espaciado del legacy, 0,8 dp = 0,0615 em) o **124,4** (0,08 em) contra 85,7: 0,712 y 0,689, debajo del mínimo de 0,8 (SD-15) | 106,7 era a 13 sin espaciado; la conclusión no cambia (gate, QA-G5P-02) |
| R-17 | §3.5 | la capa de estado "con 90 al entrar y 160 al volver, por código" | una **fase finita de `SeriesPanelMotion`** (§6.10, DD5-28) | no tenía dueño: ni la regla de toma ni el `Stop` de la salida la veían (gate, QA-G5T-11) |

---

## 3. Tokens, clases de texto y colores

**Tokens: 136 → 137**, como pidió DD5-08 y ya está en el diseño técnico (§2.10): `num.lap` 28/28/700, `num.row` 20/22/700 y `num.xs` 13/16/700, nuevo. No pido otro.

| Pieza | Fuente · tamaño (base) | Color | Alfa por código |
|---|---|---|---|
| Ícono `timer` · `history` | RowMate Icons Outline · 14 | `metric.split` · `metric.distance` | 0,80 |
| Rótulo del chip `500 m` · `Reps` | Inter 600 · 13 · *tracking* 0,08 em (`letter-spacing` 8, unidad de TextCore): el rótulo de SPM de la app | `metric.split` · `metric.distance` | 1 (SD-17; el legacy, 0,8) |
| Título `Parciales 500 m` · `Series anteriores` | Inter 600 · 11 × la escala de título (§4.8), sin *tracking* (SD-15; el legacy, el estilo del chip) | ídem | 1 (SD-15) |
| Flecha | trazo dibujado de 1,5 base (§4.3) | `text.primary` | 0,54 (= `text.tertiary`) |
| Vuelta en curso | RowMate Numeric SC 700 · `num.lap` 28, cifras tabulares | `metric.split.hud` | 1 |
| Metros de la vuelta en curso | RowMate Numeric SC 700 · `num.xs` 13 | `metric.split.hud` | 0,80 (la regla de las unidades, DV-31) |
| Rótulos `#n` · `Rep n` | Inter 600 · 12 | `text.secondary` | — (ya es @70 %) |
| Tiempo de la vuelta · split de la repetición | RowMate Numeric SC 700 · `num.row` 20 | `metric.split.hud` | 1 |
| Metros de la repetición | RowMate Numeric SC 700 · `num.xs` 13 | `metric.distance.hud` | 1 |
| `Rep r/R · m` | RowMate Numeric SC 700 · `num.stat` 16 | `metric.distance.hud` | 1 |
| Vacíos | Inter 400 · `body` 15/22 | `text.tertiary` | — |
| Marca de la fila nueva | rectángulo de 3 × 20, radio 1,5 | `metric.split` (vueltas) · `metric.distance` (repeticiones) | la de su línea de tiempo (§6.2) |
| Vidrio | — | `glass.fallback` (`#070D16 @86 %`) | — |
| Borde | `stroke.hairline` 1 (× `sp`) | `text.primary` | 0,10 |
| Capa de *hover* · presionado | rectángulo de la cabecera (§4.9) | `text.primary` | 0,04 · 0,08, con su fase (§6.10) |
| Anillo de foco · halo | 2 dp · 4 dp (en dp de ventana) | `text.primary` · `overlay.scrim` | 1 · 0,50 |

Todos los colores salen de `Tokens.cs` (`TokenColors.ToColor`); los alfas, de constantes de `SeriesPanelGeometry` o `MotionSpecs` (§6.9). No hay ningún color literal. El contrato USS de 02-tech-design §7.5 no cambia: el panel no tiene aspecto en USS.

---

## 4. Anatomía

Todo en **base del panel** (× `sp` = dp). Ancho 137: relleno de 10 a cada lado, **contenido de x 10 a 127**. Relleno de 8 arriba y abajo.

### 4.1 El vidrio

- **Relleno** `glass.fallback` (`#070D16 @86 %`); **borde** de 1 base blanco al 10 %, por dentro del rectángulo; **radio** 14 (`r.glass`) en las cuatro esquinas. Sin halo, sin sombra y **sin reflejo**, como las tarjetas de hoy (DN5-02).
- **Su alto visual** `G` (base) sale de la motion (`SeriesPanelVisual.HeightBase`). El ancho es fijo. Las esquinas de abajo se dibujan en `G` y bajan o suben con él.
- **Lo que tiene que dar en las tomas** (sobre el fondo provisorio): `#08162B` sobre el cielo y `#081627` sobre el agua, ± 1 por canal. El borde da `#212E41` (cielo) y `#212E3D` (agua), ± 3: son los mismos que dan las tarjetas en la misma toma.
- **En pausa**, el velo oscurece el fondo y el vidrio sobre el cielo pasa a `#071527`. El panel no baja de opacidad (ART_BIBLE §3.2).

### 4.2 El chip (cerrado): 137 × 34

```
y 0 ┌───────────────────────────────────────────────────────────────────────────┐ radio 14
    │ relleno 8                                                                  │
y 8 │ [ícono 14: x 10-24, y 10-24]  4  [500 m · x 28 →]            [flecha ∨]   │ renglón de cabecera, 18 de alto,
    │                                                    centro (122,5; 17)      │ centro en y 17
y26 │ relleno 8                                                                  │
y34 └───────────────────────────────────────────────────────────────────────────┘
```

- **El ícono**: su caja de 14 (el em de la fuente de íconos) en x 10-24 e y 10-24, centrada en el renglón.
- **El rótulo** empieza en x 28, con la banda de las mayúsculas centrada en y 17 (línea de base en y 21,73). Tiene lugar hasta x 109,7 (81,7 base): `500 m` mide 45,3 y `Reps`, 35,4.
- **El rótulo va en el color pleno, no al 80 % del legacy** (SD-17). En el legacy, el chip usa el mismo estilo que el rótulo de SPM (`series_panels.dart:26-39`, `immersive_workout_screen.dart:795-801`), y la app ya pasó los rótulos de las tarjetas al color pleno en la 4a (DV-31, AC-APP-06-01): el chip sigue siendo igual al rótulo de SPM. Además, al 80 % no llega a 4,5:1 debajo de la capa de estado: `metric.split` da 4,746 sobre el peor vidrio, pero 4,285 con el *hover* y 3,847 presionado; `metric.distance`, 5,062, 4,569 y 4,099. En el color pleno, presionado, 5,120 y 5,467 (5,035 y 5,376 en el peor renderizado; C8). Lo mismo vale para el título, que está en la misma banda. El espaciado, 0,08 em, es el de los rótulos de las tarjetas (`rm-type-label`); el 0,8 dp del legacy es 0,0615 em a 13, y `500 m` mide 1,2 base más (45,3 contra 44,1). El ícono queda al 80 %, como el legacy.
- **La flecha** se describe en §4.3.
- Entre el rótulo y la flecha queda vidrio vacío, a propósito: el chip mide lo mismo que el panel abierto (abrir cambia solo el alto) y deja ver de un vistazo el color de cada panel.
- **La flecha no va a 6 del rótulo, como en el legacy** (SD-18). Ahí su tinta quedaría en x 84,0-92,6 con `500 m` y en 74,1-82,7 con `Reps`: las flechas de los dos chips, una encima de la otra, quedarían corridas 9,9 base entre sí, y al abrir la flecha tendría que viajar 34 o 44 base hasta su lugar abierto (en el legacy también va a la derecha abierto, porque el título es `Expanded`, y salta). Pegada a la derecha, gira en su lugar.

### 4.3 La cabecera (abierto) y la flecha

- **La cabecera** es el mismo renglón del chip, con el título en lugar del rótulo corto. El título empieza en x 28, en 11 × la escala de título, con la banda de las mayúsculas centrada en y 17 (línea de base en y 21,0 a escala 1). Tiene lugar hasta x 113,7 (85,7 base).
- **No lleva el estilo del chip** (13/600 con espaciado, que es lo que hace el legacy; SD-15): con él, `Parciales 500 m` pide 112,7 y `Series anteriores`, 120,3. Irían a 0,761 y 0,712, debajo del mínimo de 0,8, así que en español los dos se cortarían con `…`; y, sin el mínimo, quedarían en 9,9 y 9,3 base, más chicos que los 11 y 10,4 de ahora. A 11 sin espaciado, los dos entran enteros, en el tamaño del rótulo de kcal y BPM. Con 0,08 em a 11 tampoco alcanza: 0,871 y 0,814.
- **La flecha** es un trazo de tres puntos con extremos y unión redondos, de **1,5** de grosor, en el color de §3:
  - cerrado (∨): (118,75; 15,125) → (122,5; 18,875) → (126,25; 15,125);
  - gira 180° alrededor de su centro **(122,5; 17)**, en el sentido del reloj al abrir y al revés al cerrar. Abierto queda ∧ (la forma de `expand_less`);
  - su tinta va de x 118 a 127: pegada al borde del contenido, como la del glifo de 18 del legacy (que ocupaba 0,261-0,739 de su caja).
- **El aire** entre el título y la tinta de la flecha es de 4,3 base como mínimo.

### 4.4 `Parciales 500 m`

```
y  0  relleno 8
y  8  cabecera (18)                                                          centro 17
y 26  aire 6
y 32  vuelta en curso (28): [0:50.0 · x 10 →] ·············· [← 200 m · hasta x 127]   banda centrada en 46 · base 55,8
y 60  aire 4  (solo con vueltas)
y 64  fila 0 (20): [#3 · x 10] 4 [2:05.0]                                    banda centrada en 74 · base 81   ┐ hasta 4
y 86  fila 1 (20)                                                                                             │ la más reciente
…     (2 entre filas: la fila k empieza en 64 + 22·k)                                                         ┘ arriba
      aire 6
      línea Rep (16): [Rep 2/3 · 200 m · x 10 →]                             banda centrada a +8 · base +13,6   (solo en serie)
      relleno 8
```

- **La línea `Rep`** empieza en y **66** sin vueltas, en **68 + 22·n** con n vueltas a la vista (90 con una, 156 con cuatro) y en **82** fuera de un trabajo.
- **Alto natural** (con la serie y sin ella): sin vueltas 90 · 68; con n vueltas, **90 + 24 + 22·(n − 1)** (114, 136, 158 y 180) o 22 menos sin la serie (92, 114, 136 y 158); fuera de un trabajo, 106 · 84.
- **La vuelta en curso.** El tiempo empieza en x 10. Los metros van **pegados a la derecha** (terminan en x 127), en la misma línea de base, con al menos 6 entre los dos (SD-16; el legacy los pone a 6 del tiempo). Por qué:
  - quedan en **la misma columna que los metros de las repeticiones**, que en el legacy también van pegados a la derecha (`series_panels.dart:380-387`): en CAP-03, `200 m` y `680 m` terminan en el mismo borde;
  - su lugar **no depende del ancho del tiempo**, que cambia al pasar de `9:59.9` a `10:00.0` y con la escala de ajuste (§4.7);
  - con tres cifras (desde los 100 m de cada vuelta) quedan solo **2,0 base** más a la derecha que en el legacy; con dos cifras, 8,8, y con una, 15,5. Desde `10:00.0` la cifra llena la línea y las dos reglas coinciden.
- **Las filas.** El rótulo `#n` va en x 10, en una columna del ancho de `#99` (23,1), o del más ancho a la vista si pasa de `#99`. El tiempo va 4 después de la columna. Todo en la misma línea de base, como el legacy (`CrossAxisAlignment.baseline`).
- **El aire entre filas es 2, no el 2 + 2 del legacy** (SD-19). Entre las tintas de dos filas quedan 8 base (5,1 en repeticiones, por la `p` de `Rep`): se leen como filas separadas. Con el aire del legacy, la vista mínima de AC-WKT-18-13 pasaría de 182 a 190 base y, en W1280, `sp` bajaría de 1,20 a 1,15 (todo un 4 % más chico); lo más alto, de 292 a 310. Sin vueltas, la línea `Rep` sigue a la vuelta en curso con su aire de 6, sin el 4 de las filas (el legacy deja 4 + 6).
- **La marca de la fila nueva**: x 4-7 (dentro del relleno izquierdo, a 3 del rótulo), de alto el de la fila (20), radio 1,5. Nunca pasa debajo de un texto.
- **Fuera de un trabajo**, en lugar de la vuelta en curso y las filas va `hudLapsEmpty` en dos renglones de 22 (y 32 y 54), con el corte equilibrado de §4.8 y la línea de base en +16,46 de cada uno.

### 4.5 `Series anteriores`

```
y  0  relleno 8
y  8  cabecera (18)
y 26  aire 6
y 32  fila 0 (20): [Rep 2 · x 10] 4 [2:05] ······················· [← 680 m · hasta x 127]   base 49   ┐ hasta 3
y 54  fila 1 (20)                                                                                      │ la más reciente
y 76  fila 2 (20)                                                                                      ┘ arriba
      relleno 8                    → 60 · 82 · 104 con 1 · 2 · 3; vacío, 84 (dos renglones de 22)
```

- **La columna del rótulo** mide lo del `Rep n` más ancho a la vista. El split va 4 después y los metros, pegados a la derecha, con al menos 6 de aire. Van en columnas y sin `·`, como el legacy.
- **Sin repeticiones** va `hudRepsEmpty` en dos renglones, como el vacío de parciales.

### 4.6 La vertical de cada renglón (lo que necesitan el dibujo y las reglas de §5.3)

Medido desde el borde de arriba del renglón, con las métricas de las fuentes del repo: Inter, altura de mayúscula 0,7275 em y descendente 0,2412; RowMate Numeric SC, 0,700 y 0,200. **La banda de las mayúsculas va centrada en el renglón**: es la regla de `TabularNumber` para Barlow, y en Inter coincide con centrar la caja.

| Renglón | Alto | Centro de la banda | Línea de base | Tinta de arriba | Tinta de abajo (con descendentes) |
|---|---|---|---|---|---|
| Cabecera: rótulo 13/600 | 18 | 9 | 13,73 | 4,27 | 13,73 (`Reps` tiene `p`: **16,87**) |
| Cabecera: título 11/600 | 18 | 9 | 13,00 | 5,00 | 13,00 (`p` de `Series anteriores`, `repeticiones` en otros idiomas: **15,65**) |
| Vuelta en curso (`num.lap` 28 + `num.xs` 13) | 28 | 14 | 23,80 | 4,20 | **23,80** (cifras, `.` y `m`: sin descendentes) |
| Fila de vuelta (`#n` 12 + `num.row` 20) | 20 | 10 | 17,00 | 3,00 | **17,00** |
| Fila de repetición (`Rep n` 12 + `num.row` 20 + `num.xs` 13) | 20 | 10 | 17,00 | 3,00 | **19,89** (la `p` de `Rep`) |
| Línea `Rep` (`num.stat` 16) | 16 | 8 | 13,60 | 2,40 | **16,80** (la `p` de `Rep`, un poco fuera de su caja) |
| Renglón de un vacío (`body` 15) | 22 | 11 | 16,46 | 5,54 | **20,08** |

Con una escala de ajuste `k` (§4.7), la banda sigue centrada y la tinta se achica alrededor del centro: la tinta de abajo es `centro + (tinta − centro)·k`.

### 4.7 Lo que no entra se achica (DV-49, AC-WKT-18-17)

| Pieza | Cómo | Ancla | Mínimo |
|---|---|---|---|
| Título | escala pareja de título (§4.8) | su borde izquierdo y la banda centrada | 0,8; debajo, `…` al final |
| Rótulo del chip | no se achica (los dos idiomas entran; tope para otros, 81,7) | — | — |
| Tiempo de la vuelta en curso | solo la cifra (`FitBox`), con los metros a 13 | (x 10; línea de base) | ninguno; más allá de `S-LARGO`, `…` |
| Filas de vueltas | **una escala para todas las que están a la vista**: rótulo, columna, aire y tiempo | rótulo y tiempo desde x 10; la banda centrada | ídem |
| Filas de repeticiones | **una escala común** a las que están a la vista, para las tres piezas; los metros siguen pegados a x 127 | rótulo y split desde x 10; metros desde x 127 | ídem |
| Línea `Rep` | escala pareja | (x 10; línea de base) | ídem |
| Vacíos | no se achican: dos renglones | — | — |

- **Cuándo cambia una escala común**: cuando cambian las filas a la vista, en el cuadro del segundo. Llega a su valor nuevo con el empuje (0-160, `ease.move`), igual que la columna del rótulo cuando se ensancha (de `Rep 9` a `Rep 10`).
- **Las escalas medidas** (K2; WP-B1 las vuelve a medir en Unity, R5-02):

| Caso | Ancho | Escala |
|---|---|---|
| `10:02.4` + `499 m` | 129,6 | la cifra a 0,861 |
| `Rep 1 2:05 1.112 m` (CAP-11) | 120,7 | 0,969 |
| `Rep 3 1:54 1.245 m` | 123,3 | 0,949 |
| `Rep 2/3 · 1.245 m` | 114,3 | 1 |
| **`S-LARGO`** (CAP-25): `5999:59.9` + `499 m` | 158,8 | la cifra a **0,652** |
| `S-LARGO`: `#1999 5999:59.9` | 125,6 | **0,932** |
| `S-LARGO`: `Rep 999/999 · 999.999 m` | 164,3 | **0,712** |
| `S-LARGO`: `Rep 999 5999:59 999.999 m` | 183,5 | **0,638** |

### 4.8 Español e inglés

| Pieza | es | en | Escala |
|---|---|---|---|
| Chips | `500 m` (45,3) · `Reps` (35,4) | iguales | 1 |
| Títulos | `Parciales 500 m` (85,2) · `Series anteriores` (90,3) | `500 m splits` (65,2) · `Previous reps` (72,6) | **es 0,949 los dos · en 1 los dos** [cambia] |
| Vacío de parciales | `Sin parciales` (90,4) / `en este paso` (90,9) | `No splits` (61,6) / `in this step` (76,9) | 1 |
| Vacío de repeticiones | `Sin repeticiones` (113,7) / `completas` (73,6) | `No completed` (99,5) / `reps` (31,2) | 1 |
| Rótulos, formatos y números | `#3`, `Rep 2`, `Rep 2/3 · 1.112 m`, `2:05.0`, `—` | iguales (Q-06; P-08) | §4.7 |

- **La escala de título** es una por idioma (y por ventana, si algún día cambiara el ancho): la menor entre las que necesitan los dos títulos, aunque el de repeticiones no se vea. Así nada cambia de tamaño cuando aparece o se va un panel.
- **El corte equilibrado de los vacíos**: de los cortes entre palabras que dejan las dos líneas en 117 o menos, el de la línea más larga más corta. En un empate, el primero. Si ninguno entra, la regla del título (achicar hasta 0,8 y, debajo, `…`). Siempre se reservan dos renglones, así un idioma en el que el texto entre en una línea no cambia el alto del panel.

### 4.9 Estados

| Estado | Qué se ve |
|---|---|
| Reposo | §4.1 a §4.5 |
| *Hover* (PC, con puntero de mouse) | capa blanca al 4 % sobre **la banda de la cabecera**, de y 0 a 34 (con los radios de arriba; cerrado, todo el chip), aunque el puntero esté sobre el contenido. Entra en 90 (`ease.enter`) y sale en 160 (`ease.exit`), con la fase de la capa de estado de `SeriesPanelMotion` (§6.10) |
| Presionado | ídem al 8 %, desde el `PointerDown`, con la misma fase. Sin escala: un vidrio de 180 que se encoge salta |
| Foco con teclado | el anillo del HUD en dp de ventana: blanco de 2 dp entre −4 y −2 del borde visual y halo `overlay.scrim` @50 % de 4 dp entre −8 y −4. Radios 14·sp + 4 y 14·sp + 8 (18,8 y 22,8 en TV; 20,8 y 24,8 en W1280). Sigue el alto visual `G` en cada cuadro. Solo en el modo teclado, sin animación propia |
| En pausa | igual que en reposo, con los valores quietos. Se abre y se cierra |
| Inerte (diálogo abierto, salida, antes de los 520 ms de la cascada, rutina terminada) | sin *hover*, sin presionado y sin anillo; el aspecto no cambia. Si la capa estaba encendida, se apaga en 160 (§6.10) |

- **El orden de las capas** (de abajo arriba): el vidrio, el borde, la capa de *hover* y presionado, los textos y las cifras, la marca y el anillo.
- La capa nunca pasa debajo de un valor: la tinta de la vuelta en curso empieza en y 36,2.

---

## 5. Lugar y espacio

### 5.1 Por perfil (dp de ventana)

| Perfil | `s` · `sp` | Panel (x) · ancho | Columna: techo con objetivo · sin | Piso | Lugar (base del panel) con · sin | Regla | El disco empieza en (aire) | Zona del bote |
|---|---|---|---|---|---|---|---|---|
| TV | 1,054 · 1,054 | 14,8-159,1 · 144,4 | 228,6 · **210,9** | 682,2 | 430,3 · 447,2 | `FitAll` | 167,6 (8,4) | x 115,1-295,9 · y 323,7-597,9 |
| P360 | 0,923 · 0,923 | 12,9-139,4 · 126,5 | 200,3 · 184,7 | 596,9 | 429,7 · 446,6 | `FitAll` | 146,8 (7,4) | x 100,8-259,2 · y 283,4-523,4 |
| TH | 1,054 · 1,054 | 14,8-159,1 · 144,4 | 228,6 · 210,9 | 309,8 | 77,1 · **93,9** | `ColumnScroll` | 419,1 (259,9) | x 255,9-658,1 · y 206,1-329,4 |
| L640 | 0,923 · 0,923 | 12,9-139,4 · 126,5 | 200,3 · 184,7 | 271,4 | 77,1 · **93,9** | `ColumnScroll` | 286,8 (147,4) | x 179,2-460,8 · y 180,6-288,6 |
| W1280 | 1,6 · **1,20** | 22,4-186,8 · 164,4 | 347,1 · 320,2 | 566,4 | 182,7 · 205,2 | `RowsScroll` | 582,4 (395,6) | x 358,4-921,6 · y 361,1-577,1 |

- **Qué rutinas tienen objetivo en SPM** (`RoutineSeeds`): `Prueba 4a` (los trabajos, SPM 22), `Corta` (SPM 18) y los trabajos de 250 m de `Pirámide 500` (SPM 30). `Series 600`, `Trabajo 1000`, `Fondo 3000`, `Cinco reps` y `Dos series` no tienen. En `Prueba 4a` a los 90,6 s (CAP-24) la barra ya está en el descanso: SPM perdió el objetivo y la columna ya subió (80-280).
- **La regla de cada ventana** se decide por ventana, con el peor caso (SPM con objetivo, en serie, los dos abiertos): no cambia con los datos ni con el paso, y cambia en el acto al rotar.

### 5.2 Lo que no entra: las tres reglas

1. **`FitAll`** (TV, P360). Todo a su alto natural; lo más alto son 292 de 430 base.
2. **`RowsScroll`** (W1280, `sp` = 1,20). La cabecera, la vuelta en curso y la línea `Rep` de cada panel quedan fijas; **las filas** de cada panel abierto se desplazan dentro de su ventana. El reparto [cambia, DD5-26]:
   - **repeticiones tiene reservado su mínimo**: 34 cerrado; abierto, 60 (la cabecera y una fila, o el primer renglón de su vacío), o su natural si es menor;
   - **parciales** recibe `min(su natural; lugar − 8 − el mínimo de repeticiones)`;
   - **repeticiones** recibe lo que queda, hasta su natural.

   | Caso (W1280) | Con objetivo (182,7) | Sin objetivo (205,2) |
   |---|---|---|
   | 4 vueltas + 3 repeticiones | parciales 114,7 (se ve 1 fila) · repeticiones 60 (1 fila) | 137,2 (2 filas) · 60 (1) |
   | 1 vuelta + repeticiones vacío (la rep 1 con trabajos largos; DEMO-06) | 114 (entero) · 60,7 (la cabecera y `Sin repeticiones`) | 114 · 83,2 (los dos renglones; 0,8 base menos de relleno abajo) |
   | 4 vueltas + repeticiones cerrado | 140,7 (2 filas) · 34 | 163,2 (3 filas) · 34 |
   | CAP-18 (1 vuelta + 1 repetición, 182) | entra todo | entra todo |

   - Abrir uno puede achicar la ventana de filas del otro: los dos altos van con el mismo resorte y las filas que dejan de verse se apagan con la regla 2 de §5.3.
3. **`ColumnScroll`** (TH, L640). Cada panel a su alto natural y **la columna entera** se desplaza, como el legacy. Los dos chips entran siempre (76 base). Con parciales abierto se ven sin desplazar su cabecera y la vuelta en curso; sin objetivo, también la línea `Rep` (en el fundido, §5.3).

**El desplazamiento** (AC-WKT-18-13; 02-tech-design §3.2):

- empieza arriba y no tiene barra;
- un toque que se mueve menos de 8 dp abre o cierra, y uno que se mueve más, desplaza;
- la rueda va de a una fila (22·sp), con `spring.snappy` (en el acto con reducir movimiento);
- al soltar un arrastre no hay inercia: queda donde se soltó, dentro de sus topes;
- al rotar vuelve a 0;
- **cuando entra una fila con la vista desplazada** (desplazamiento > 0), **solo en `RowsScroll` (W1280)**: la fila entra arriba de la ventana de filas, fuera de la vista, y el desplazamiento suma 22·sp·k en el mismo cuadro, así lo que se ve no se mueve (DD5-27). Con el desplazamiento en 0, la lista baja con el empuje y lo de abajo sale por la ventana.
- **en `ColumnScroll` (TH, L640) no hay compensación** (decidido al empezar la 6a, QA-V5-R2-01, opción a: [06a-escena-lago/00-deuda-diseno §1](../06a-escena-lago/00-deuda-diseno.md)): el desplazamiento de la columna conserva su valor, como el `SingleChildScrollView` del legacy, y lo que está debajo de la fila nueva baja con el empuje (0-160, `ease.move`), sin saltos (AC-WKT-18-13). En la columna, la fila nueva entra debajo de la cabecera y de la vuelta en curso, que se desplazan con ella: sumar 22·sp las subiría y dejaría quieto solo lo de abajo.

### 5.3 Renglones con alfa (sin recorte; DT5-02 revisado, DN5-01)

Cada renglón se dibuja entero, con un alfa que es el producto de su motion (fundidos de §6) y de estas reglas. Ningún renglón se dibuja con tinta fuera del vidrio, ni sobre otro renglón, con alfa visible.

| Regla | Dónde | Alfa | Constante |
|---|---|---|---|
| **1. Revelar** | todo renglón de un panel, contra su vidrio | `clamp((G − tintaAbajo) / 4)`: 0 hasta que el vidrio cubre toda la tinta; 1 cuando la pasa por 4 | `SeriesRevealFade` = 4 base |
| **2. La ventana de filas** | las filas (de vueltas o de repeticiones), contra la ventana de filas de su panel: abajo, siempre; arriba, solo con el desplazamiento > 0 | `clamp(1 − o / 4)`, con `o` = lo que la tinta pasa el borde (abajo: `tintaAbajo − Vb`; arriba: `Va − tintaArriba`) | `SeriesRowsEdgeFade` = 4 base |
| **3. La columna** | `ColumnScroll`: el vidrio (por vértice) y todo renglón, contra el borde de la columna que tiene algo escondido detrás | vidrio `× clamp((Cb − y) / 12)`; renglón `× clamp((Cb − tintaAbajo) / 12)`; arriba, lo mismo con `Ca`. Con una fuerza `f = clamp(escondido / 12)` que mezcla con 1 | `SeriesScrollFade` = 12 base |

- **Por qué 4 en las reglas 1 y 2.** Es el mayor fundido que cumple las dos garantías:
  - en reposo, todo renglón al 100 %: el último de cada panel termina su tinta entre 7,2 (la línea `Rep`, con la `p`) y 11 base antes del final del vidrio;
  - una fila que se sale por abajo se apaga antes de que su tinta llegue a 4,4 base de la de la línea `Rep`, y una que se sale por arriba, antes de llegar a 4,2 de la de la vuelta en curso.
- **La ventana de filas sigue al vidrio**: su borde de abajo es el final de la última fila en el estado final, corrido por el mismo corrimiento que la cola (§6.2). Así, cuando la lista baja con su empuje, nada se apaga de más.
- **La fuerza de la regla 3** evita que el fundido aparezca de golpe cuando el contenido apenas pasa el borde. Si nada pasa el borde (los dos chips en 76 de 93,9), no hay fundido.
- **Por qué la regla 3 es una máscara (adentro) y la 2 un desborde (afuera).** En TH y L640, más allá del borde de la columna están SPM y la banda, y no hay vidrio: el texto tiene que apagarse antes de llegar al borde. En W1280, más allá del borde de las filas sigue el vidrio de su panel (el aire de 4 o de 6), y la fila más reciente tiene que verse entera aunque la ventana mida 20 base (el peor caso de §5.2).
- **En los cuadros de una apertura** (VID-01, de 34 a 90): la cabecera siempre se ve; la vuelta en curso aparece a los 113 ms (el vidrio pasa su tinta, 55,8) y la cubre entera a los 130; la línea `Rep` aparece a los 298 ms (82,8) y está entera a los 382. Los valores por cuadro van en §6.1.

### 5.4 Con el bote, la cuenta grande, `PAUSA`, el sello, la llave y el panel del simulador

| Pieza | Regla | Lo medido |
|---|---|---|
| **Zona del bote**, en vertical | los chips nunca la tocan; abiertos, solo en su franja de afuera (x ≤ ancho / 2 − 44·s), nunca en la columna del centro (P-03) | TV: los chips terminan en y 291,0 y la zona empieza en 323,7 (32,7 dp de aire); abiertos entran en x 115,1-159,1 (44 de 180,8 dp, el 24 %). P360: ídem, 24 % |
| Zona del bote, en horizontal | nunca | el borde derecho a 96,8 dp de ella en TH, 39,8 en L640 y 171,6 en W1280 |
| **Cuenta grande y `PAUSA`** | siempre en su lugar de la 4b; el panel termina 8·s antes del disco | TV 8,4 dp; P360 7,4. `BlocksCallout` da `false` en los cinco perfiles |
| **El sello** | la tercera fila de la barra: los paneles nunca llegan | el techo de la columna está 8·s debajo de SPM, y SPM, debajo de la barra de 74·s |
| **La llave** | no tapa ningún panel (`WrenchAudit`) | TV: x 355-395 contra 159,1; TH: a la derecha de `Pausa`; L640: encima de `Pausa` (x ≥ 415); W1280: a la derecha de `Pausa` |
| **El panel del simulador** | puede tapar los paneles en parte (AC-BLE-16-03) | W1280: el panel abierto va en x 643-921 y no los toca (DEMO-08) |
| La cinta `SIMULADOR` | no los toca | tapa x + y < 56,5 dp; la columna empieza en y ≥ 184,7 |
| **Las tarjetas** | no se mueven por los paneles | la columna solo lee SPM y la banda |

### 5.5 El área de toque (AC-WKT-18-09; ART_BIBLE §10.4)

Cerrado, el chip agrandado hacia arriba y hacia abajo **hasta 56 dp** (48 como mínimo). Cada chip toma la mitad del aire entre los dos (4·sp), así nunca se superponen. Si hace falta, toma también el aire con SPM y el relleno de abajo de SPM o de arriba de la banda, que no responden. Abierto, el área es el panel entero, en su alto visual. Esto es lo que tiene que responder; cómo llega ahí la selección de UI Toolkit (una caja de *layout* del tamaño del área, un hijo oculto o `style.translate`) es del Architect (QA-G5T-04). El aspecto no cambia con ninguna de esas formas.

| Perfil | Chip | `500 m` llega a | `Reps` llega a |
|---|---|---|---|
| TV | 35,8 | 56 | 56 |
| P360 | 31,4 | **51,7** (todo lo que hay hasta el relleno de SPM) | 56 |
| TH | 35,8 | 56 | 56 |
| L640 | 31,4 | **51,7** | **52,7** (hasta el relleno de la banda) |
| W1280 | 40,8 | 56 | 56 |

---

## 6. Motion

### 6.0 La regla de oro, en cada pieza (ANIMATION_BIBLE §0.1)

- Los valores cambian **en el cuadro del segundo del motor**, el mismo del reloj de la banda. La vuelta en curso no cuenta décimas en vivo: sus celdas cambian una vez por segundo con el rodillo de 180 (ANIMATION_BIBLE §2.4). Con reducir movimiento, en su lugar va el fundido de 80 (ART_BIBLE §10.4).
- **Lo que hace el usuario** (abrir, cerrar) va con `spring.smooth` (ω 12), desde la posición y la velocidad que tenga. **Lo que cambia el dato** (una vuelta, una repetición, un paso, la serie) va con curvas de duración fija y termina en 160-280 ms, antes del segundo siguiente.
- Nada se anima por *layout*: el *layout* va a su estado final en el cuadro del cambio y lo demás es dibujo (02-tech-design §3.1). Abrir, cerrar o desplazar no toca el motor, la sesión, la telemetría ni la preferencia (que se guarda en el cuadro del toque).
- **Con el panel cerrado no se anima nada del contenido**: el chip no cambia, y al abrirlo la lista ya está, sin marcas.

### 6.1 Abrir y cerrar (VID-01)

`t = 0` en el cuadro del toque. Solo cambia el alto.

| Pieza | Abrir | Cerrar |
|---|---|---|
| Alto `G` | `spring.smooth` desde los 0 | `spring.smooth` desde los **80** |
| Contenido (todo lo que no es la cabecera) | 0 → 1 en 80-240, `ease.enter` | 1 → 0 en 0-120, `ease.exit` |
| Rótulo corto · título | el rótulo sale en 0-80 (`ease.exit`); el título entra en 80-200 (`ease.enter`) | el título sale en 0-80; el rótulo entra en 80-200 |
| Flecha | 0 → 180° en 240, `ease.move` | 180 → 0° en 240 |
| Revelar (§5.3, regla 1) | por renglón, contra `G` | ídem (el contenido ya se fue cuando el vidrio llega a él) |
| El panel de abajo | va corrido `(G − H_final)·sp` dp (FLIP), con el aire de 8·sp en cada cuadro. Si el corrimiento se dibuja o es un `style.translate` lo decide el Architect (QA-G5T-04): los valores por cuadro son los mismos | ídem |
| La cola (línea `Rep`) | quieta en su lugar final: la revela el vidrio | quieta |

**VID-01**: parciales de 34 a 90 base (`0:50.0` `200 m`, sin vueltas, `Rep 2/3 · 200 m`) en TV, con `Series anteriores` abierto debajo.

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 | 300 | 333 | 367 | 400 | 467 | 533 | 600 | 700 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Alto (base) | 34,0 | 37,4 | 44,7 | 52,9 | 60,6 | 67,3 | 72,7 | 77,1 | 80,4 | 83,0 | 84,9 | 86,3 | 87,3 | 88,6 | 89,3 | 89,7 | 89,9 |
| Contenido | 0 | 0 | 0 | 0,331 | 0,701 | 0,901 | 0,983 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Rótulo · título | 1 · 0 | 0,925 · 0 | 0,421 · 0 | 0 · 0,422 | 0 · 0,826 | 0 · 0,977 | 0 · 1 | 0 · 1 | … | | | | | | | | |
| Flecha (°) | 0 | 3 | 17 | 51 | 117 | 159 | 175 | 180 | 180 | 180 | 180 | 180 | 180 | 180 | 180 | 180 | 180 |
| Vuelta en curso (contenido × revelar) | 0 | 0 | 0 | 0 | 0,701 | 0,901 | 0,983 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Línea `Rep` (ídem) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0,05 | 0,52 | 0,87 | 1 | 1 | 1 | 1 | 1 |
| `Series anteriores` corrido (dp) | −59,0 | −55,4 | −47,7 | −39,1 | −31,0 | −24,0 | −18,2 | −13,6 | −10,1 | −7,4 | −5,4 | −3,9 | −2,8 | −1,4 | −0,7 | −0,4 | −0,1 |

- **Cerrar** (de 90 a 34): el contenido es 0,977 · 0,826 · 0,422 · 0 a los 33 · 67 · 100 · 133 ms. El alto se queda en 90 hasta los 80 ms y después baja: 88,6 · 82,4 · 74,4 · 66,4 · 59,3 · 48,5 a los 100 · 133 · 167 · 200 · 233 · 300; 39,8 a los 400 y 34,8 a los 600. Ningún texto se ve cuando el vidrio pasa por él.
- **Un toque durante la animación la invierte desde donde está**: el resorte sigue con su posición y su velocidad; los fundidos, desde su opacidad; la flecha, desde su ángulo, con lo que le falta de los 240.
- **Cuándo termina** (para la regla de toma): al resorte le faltan menos de 0,25 px de captura a los 726 ms (34 → 90, TV), 749 (34 → 106), 814 (34 → 180) y 866 (34 → 292, el mayor posible). A los **820 ms** (`SeriesSettleTapMs`) le faltan 0,08 base en el peor caso de una toma (34 → 180): 0,23 px en TV y 0,10 en W1280.

### 6.2 La vuelta nueva y la repetición nueva (VID-02, VID-04) [cambia: R-04 y R-05]

`t = 0` en el cuadro del segundo que trae la vuelta.

| Pieza | Cómo |
|---|---|
| **Las filas que estaban** | bajan una fila: corrimiento −22·k → 0 en **0-160**, `ease.move` (k = las vueltas nuevas de ese segundo) |
| **El alto** (si había menos de 4 a la vista) | crece +24 (la primera) o +22·k en **0-160, `ease.move`**, la misma curva que el empuje |
| **La cola** (la línea `Rep`) y la ventana de filas | bajan con el alto: corrimiento `G − H_final` |
| **El panel de abajo** | FLIP, con el mismo `G` |
| **La fila nueva** | opacidad 0 → 1 y corrimiento −8·sp → 0 en **80-240**, `ease.enter` (`shift.s`) |
| **La marca** | entra con su fila (el mismo alfa hasta los 240); queda en 1 hasta los 600 y se apaga en 600-1400, `ease.move` |
| **La que se va** (si había 4 vueltas, o 3 repeticiones) | baja con las demás y se apaga 1 → 0 en 0-120 (`ease.exit`), **por la regla 2 de §5.3**: a los 67 ms queda en 0,127 y a los 100, en 0 |
| **La vuelta en curso** | cambia con su rodillo en ese mismo cuadro (de `2:04.0` `496 m` a `0:00.0` `0 m`, por ejemplo) |

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 400 | 600 | 800 | 1000 | 1200 | 1400 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Empuje (base) | −22,0 | −21,0 | −15,7 | −4,6 | −0,6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Alto +Δ (Δ = 24; base) | 0 | 1,1 | 6,9 | 19,0 | 23,3 | 24 | 24 | 24 | 24 | 24 | 24 | 24 | 24 | 24 |
| Fila nueva: opacidad | 0 | 0 | 0 | 0,331 | 0,701 | 0,901 | **0,983** | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| Fila nueva: corrimiento (base) | −8,00 | −8,00 | −8,00 | −5,35 | −2,39 | −0,79 | −0,13 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Marca | 0 | 0 | 0 | 0,331 | 0,701 | 0,901 | 0,983 | 1 | 1 | 1 | 0,929 | 0,500 | 0,071 | 0 |
| La que se va (propio × ventana) | 1 | 0,978 | 0,127 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

- **Por qué este orden.** La lista baja primero: a los 100 ms ya dejó libre el 79 % del lugar de arriba. La vuelta nueva entra después, desde arriba, "desprendiéndose" de la vuelta en curso. El aire mínimo entre su tinta y la de la fila que baja es de **5,48 base** (a los 82 ms, con opacidad 0,037); con 60-240 era −1,22. A los 200 ms se lee al 98,3 % (AC-WKT-18-19: 200 o menos).
- **La fila nueva no tiene borde de arriba** (§5.3, regla 2): su corrimiento máximo (8) deja su tinta 3,2 base debajo de la de la vuelta en curso.
- **Varias vueltas en un segundo**: el empuje y el alto con 22·k (24 + 22·(k − 1) si es la primera), y las k nuevas entran juntas, cada una con su marca.
- **Una repetición nueva** en `Series anteriores` hace lo mismo, con la marca en `metric.distance`. **La primera de la lista** reemplaza al vacío: es un cambio de contenido (§6.3), con la marca.
- **Si el alto ya estaba con el resorte** (un toque menos de 0,6 s antes): el resorte toma el alto nuevo desde su posición y su velocidad. Las filas y la cola nunca van más rápido que el vidrio (`corrimiento = min(empuje; G − H_final)`), y la regla 1 esconde lo que todavía no cubre.

### 6.3 El cambio de contenido, por pieza (VID-03, VID-04) [cambia: R-04 y R-06]

En el segundo siguiente al del cambio de paso (P-01). Cada pieza hace lo suyo:

| Pieza | Si cambia de clase (la vuelta en curso ↔ el vacío) | Si no |
|---|---|---|
| Vuelta en curso | sale 1 → 0 en 0-120 (`ease.exit`) o entra 0 → 1 en 120-280 (`ease.enter`) subiendo +4 → 0 | cambia con su rodillo (de un trabajo a otro) |
| Vacío (`Sin parciales en este paso`) | ídem | se queda, sin moverse (de un descanso a un enfriamiento) |
| Filas que se vacían (el paso nuevo empieza sin vueltas) | salen en su lugar, 0-120 | — |
| Línea `Rep` | no sale: cambia con su rodillo y se mueve con la cola; si la serie termina, sale en 0-120; si empieza, entra con lo nuevo (120-280, +4) | ídem |
| Alto | crece en 0-160 o se achica en 80-240, `ease.move` | ídem |
| Cola y ventana de filas | `G − H_final` | ídem |

| t (ms) | 0 | 33 | 67 | 100 | 133 | 167 | 200 | 233 | 267 |
|---|---|---|---|---|---|---|---|---|---|
| Lo viejo | 1 | 0,978 | 0,823 | 0,422 | 0 | 0 | 0 | 0 | 0 |
| Lo nuevo · su subida (base) | 0 · 4 | 0 · 4 | 0 · 4 | 0 · 4 | 0,226 · 3,10 | 0,646 · 1,41 | 0,872 · 0,51 | 0,973 · 0,11 | 0,999 · 0 |
| Crece (0-160): fracción | 0 | 0,045 | 0,290 | 0,792 | 0,972 | 1 | 1 | 1 | 1 |
| Se achica (80-240): fracción | 0 | 0 | 0 | 0,014 | 0,147 | 0,621 | 0,929 | 0,998 | 1 |

- **VID-03** (a los 181 s): parciales de 114 a 106 base. La vuelta en curso y `#1` salen; entra el vacío; la línea `Rep` sube 8 con el vidrio y su rodillo pasa de `600 m` a `604 m`. El alto vale 113,9 · 112,8 · 109,0 · 106,6 · 106,0 a los 100 · 133 · 167 · 200 · 233 ms. `Series anteriores` sube 8,4 dp con el FLIP.
- **VID-04** (a los 201 s):
  - parciales, de 106 a 90: sale el vacío, entra `0:01.0` `4 m`, y la línea pasa de `Rep 1/3 · 680 m` a `Rep 2/3 · 4 m` con su rodillo;
  - repeticiones, de 84 a 60: sale `Sin repeticiones completas` y entra `Rep 1 2:05 680 m` en 120-280, con su marca;
  - los dos altos van con la misma curva: 105,8 · 103,7 · 96,1 · 91,1 · 90,0 y 83,7 · 80,5 · 69,1 · 61,7 · 60,0 a los 100 · 133 · 167 · 200 · 233 ms.
- **El aire mínimo** entre lo nuevo y la cola, con el empuje, es de 5,34 base. Con el resorte de 00-deuda-diseno era −2,78 (`swap_check.py`).

### 6.4 La serie que empieza y la que termina

- **Empieza** (`SeriesStarted`): el panel de repeticiones aparece debajo, en su estado guardado (abierto o cerrado), sin abrirse: opacidad 0 → 1 y +8·s → 0 en 120-400 (`ease.enter`). Por cuadro: 0,134 · 0,424 · 0,634 · 0,785 · 0,890 · 0,952 · 1 a los 133 · 167 · 200 · 233 · 267 · 300 · 400. En el mismo segundo, parciales suma la línea `Rep` (con lo nuevo, 120-280) y su alto crece en 0-160, así el FLIP del panel de abajo ya terminó cuando él empieza a verse.
- **Termina** (`SeriesEnded`): el panel se va 1 → 0 en 0-120 (`ease.exit`), sin moverse, y sale del árbol. Parciales pierde la línea `Rep` (0-120) y su alto se achica en 80-240. Si tenía el foco, el siguiente Tab va a `Terminar`.

### 6.5 La columna que sigue a SPM

Cuando SPM gana o pierde `obj.` en un cambio de paso, la columna baja o sube 16,9·s con el tiempo de la 4a (00-deuda-diseno §4.4): gana en 0-240 y pierde en 80-280, con `ease.move`. Es el mismo movimiento que hace Watts debajo de Split. Las tarjetas no se mueven.

### 6.6 El desplazamiento

La rueda, de a una fila con `spring.snappy` (ω 22, ≈ 0,26 s). El arrastre sigue al dedo 1:1, sin inercia. Las filas que entran y salen de la ventana, con las reglas 2 y 3 de §5.3.

### 6.7 La entrada, la salida, la pausa, el segundo plano, la rotación y los diálogos

- **La entrada** (AC-WKT-13-06, VID-06): los paneles van con el tramo de SPM, de 520 a 800: opacidad 0 → 1 y `translate` +8·sp → 0 (`ease.enter`). Llegan en su estado guardado y con el primer paso (DV-48), sin abrirse ni cerrarse. Responden desde los 520.
  - En W1280 la subida es 9,6 dp, contra 12,8 de SPM (DT5-10): se acepta (§1, DN5-03).
  - Con reducir movimiento, el fundido de 120 de la 4b, con todo el HUD.
- **La salida** (AC-WKT-13-08, VID-07): con el HUD, 0-200 (`ease.exit`), quietos y con la última foto. Lo que corría (un resorte, una marca, un empuje) se detiene a los 640 (AC-WKT-13-09).
- **La pausa**: nada cambia ni se atenúa. Se abren y se cierran igual.
- **El segundo plano y el despertar** (AC-WKT-02-08): el presente en el primer cuadro (`Instant`), con las filas en su lugar, sin marcas, sin empuje y sin rodillos.
- **Un panel que vuelve a un panel de UI Toolkit** después de `DetachFromPanelEvent` (QA-G5T-12): lo mismo, todo en su estado final, sin marcas y con la capa de estado en 0 hasta el próximo evento del puntero. Medir otra vez y repintar es del Architect.
- **La rotación** (AC-WKT-18-20): en el acto, con la geometría nueva, `sp` nueva, el estado y los valores conservados y el desplazamiento en 0.
- **Un diálogo abierto**: los paneles quedan debajo del velo y no responden.

### 6.8 Reducir movimiento (AC-APP-05-08, VID-08)

| Pieza | Con reducir movimiento |
|---|---|
| Abrir | el alto en el cuadro 0; el contenido 0 → 1 en 0-160, lineal (0,208 · 0,417 · 0,625 · 0,833 · 1 a los 33 · 67 · 100 · 133 · 167); el rótulo y el título, con sus fundidos (0-80 / 80-200); la flecha no gira: el trazo ∨ funde 1 → 0 en 0-80 y el ∧ 0 → 1 en 80-160, lineales |
| Cerrar | el contenido 1 → 0 en 0-120, lineal (0,723 · 0,444 · 0,167 · 0); el alto a los 120, en el acto; la flecha al revés |
| El panel de abajo | en el acto, en el mismo cuadro que el alto |
| La vuelta nueva | las filas, el alto y la cola en su lugar nuevo en el cuadro 0; la nueva funde 0 → 1 en 0-160, sin deslizarse; la que se va funde en 0-120; **la marca, igual** (es opacidad) |
| El cambio de contenido | los mismos fundidos (0-120 / 120-280), sin subir; el alto en el acto: a los 0 si crece y a los 120 si se achica |
| La serie que empieza · termina | 120-400 · 0-120, solo opacidad |
| La columna que sigue a SPM | en el acto |
| La rueda | en el acto |
| Los rodillos | el fundido de 80 (ART_BIBLE §10.4) |
| La capa de estado (§6.10) | **igual**: 90 / 160, porque es opacidad, como la marca y como las capas de estado de los controles de la app (`Motion.uss`: con reducir movimiento se va la escala al presionar y queda la capa) |

Las reglas de §5.3 valen igual. Nada de esto cambia los datos ni cuándo pasan.

### 6.9 Constantes (para `MotionSpecs` y `SeriesPanelGeometry`)

Las de 02-tech-design §3.2 y §4.1, con estos cambios **[cambia]**:

| Constante | Valor | Antes |
|---|---|---|
| `SeriesHeight` | `spring.smooth`, **solo para abrir, cerrar y su inversión** | también el alto por un dato |
| `SeriesDataGrowMs` (nueva) | 0-160, `ease.move` (es `SeriesRowsPushMs`) | el resorte desde 0 |
| `SeriesDataShrinkStartMs` / `EndMs` (nueva) | 80 / 240, `ease.move` | el resorte desde 80 |
| `SeriesRowInStartMs` | **80** (fin 240, `ease.enter`, `shift.s`) | 60 |
| `SeriesRevealFade` (nueva) | 4 base (§5.3, regla 1) | el α_borde de 12·sp |
| `SeriesRowsEdgeFade` (nueva) | 4 base (regla 2) | ídem |
| `SeriesScrollFade` | 12 base, **solo en `ColumnScroll`** (regla 3) | 12 en las dos reglas de desplazamiento |
| `SeriesStateLayerInMs` / `OutMs` (nuevas) | `dur.micro` 90 (`ease.enter`) / `dur.fast` 160 (`ease.exit`), la fase de §6.10; con reducir movimiento, igual | "90 y 160" sin nombre ni dueño |
| `SeriesHoverAlpha` / `SeriesPressedAlpha` (nuevas) | 0,04 / 0,08 | ídem |
| `SeriesChevronStroke` (nueva) | 1,5 base; puntos (±3,75; −1,875) y (0; +1,875) desde el centro (122,5; 17) | la caja de 24 |
| `SeriesReducedChevronOutMs` / `InMs` (nuevas) | 0-80 / 80-160, lineales | "fundido de 80" |
| `HitRoomyDp` | 56 (tope) | ídem; 00-deuda-diseno daba 58,9 sin tope |

Sin cambios: `SeriesShrinkDelayMs` 80 (cerrar), `SeriesContentIn` 80-240, `SeriesContentOut` 0-120, `SeriesLabelOut` / `In` 0-80 / 80-200, `SeriesChevronMs` 240, `SeriesRowsPushMs` 160, `SeriesRowOutMs` 120, `SeriesMarkerOut` 600-1400, `SeriesSwapOut` / `In` 0-120 / 120-280 con 4, `SeriesRepsAppear` / `Leave` 120-400 con 8 / 0-120, la cascada (520, 280), `SeriesReducedIn` / `Out` 160 / 120, `SeriesWheel` (`spring.snappy`), `SeriesSettleTapMs` 820 y las de geometría de 02-tech-design §3.2.

### 6.10 La capa de estado: *hover* y presionado (gate, QA-G5T-11)

**Es una fase finita de `SeriesPanelMotion`, no un cambio instantáneo.** Los otros controles del HUD (`Terminar`, `Pausa` / `Reanudar`, la llave) funden su capa de estado con estos mismos tiempos, por USS (`Motion.uss`: hacia el estado `dur.micro` con `ease.enter`, de vuelta `dur.fast` con `ease.exit`). Si la capa del panel se prendiera de golpe, sería el único control del HUD que parpadea. El panel no puede usar la transición de USS porque no tiene aspecto en USS (02-tech-design §7.5): por eso va con su motion.

| Evento | Objetivo del alfa | Cómo |
|---|---|---|
| Entra un puntero de **mouse** al área del panel (§5.5; abierto, el panel entero) | 0,04 | 90, `ease.enter` |
| `PointerDown`, de cualquier puntero | 0,08 | 90, `ease.enter` |
| `PointerUp` de un toque (se movió menos de 8 dp): abre o cierra | 0,04 si es el mouse y sigue encima; si no, 0 | 160, `ease.exit` |
| El toque pasa a arrastre (más de 8 dp, AC-WKT-18-13): ya no va a abrir ni cerrar | 0 | 160, `ease.exit` |
| Sale el puntero, `PointerCancel`, o el panel queda inerte (§4.9) | 0 | 160, `ease.exit` |
| `Stop()` (la salida, `Dispose`, `DetachFromPanelEvent`; ROB-02) | 0, en el acto | — |
| `Instant` (el segundo plano, la rotación) | su objetivo, en el acto | — |

- **Siempre desde el alfa que tiene**, con la duración entera de su sentido (90 si sube, 160 si baja), como una transición de USS. Un `PointerDown` en medio del fundido del *hover* sigue desde ahí, sin saltos.
- **Por cuadro** (subir a 0,04 · a 0,08): 0,0187 · 0,0373 a los 17 ms; 0,0297 · 0,0595 a los 33; 0,0364 · 0,0728 a los 50; 0,0393 · 0,0786 a los 67; 0,0400 · 0,0800 a los 83 (redondeado), y exactos a los 90. Bajar desde 0,08: 0,0792 · 0,0739 · 0,0603 · 0,0341 · 0 a los 33 · 67 · 100 · 133 · 160 ms (desde 0,04, la mitad).
- **Para la regla de toma**: es **F** mientras corre (los 90 o 160 desde el último cambio). Un *hover* quieto, con el puntero apoyado, no bloquea: es un estado, no una animación. Ninguna toma tiene un puntero de mouse sobre un panel: el arnés toca con `NavigationSubmitEvent`, sin puntero (la regla 1 de `Motion.uss`). Si una prueba toca con eventos de puntero (SCT por la selección real, QA-G5T-04), la fase la bloquea mientras corre.
- **Con reducir movimiento, igual** (§6.8): es opacidad.
- **El dato**: la mueve solo el puntero. No cambia nada del motor, la sesión ni la preferencia, y no cambia cuándo pasa un valor.
- **Costo**: un alfa por panel, dibujado en la capa que ya existe (§4.9: entre el borde y los textos). Pide repintar solo mientras corre. No suma elementos ni asigna memoria por cuadro.
- **Dónde va cada parte** (los nombres los pone el Architect): la línea de tiempo pura (`SeriesTimelines.StateLayer(t, desde, hasta, reduced)`, WP-A, con dotnet), la fase en `SeriesPanelMotion` (WP-D1) y los eventos del puntero en `SeriesPanelView` (WP-B1). Es DD5-28.

---

## 7. Contraste

Los pares de 00-deuda-diseno §3.7 (`ContrastTests.ItFiveDesignerPairs`) **no cambian**: lo que reviso en este documento no pone nada nuevo debajo de un texto. Recalculados con el método de `Contrast.cs`:

| Grupo | Par | Umbral | Peor vidrio · renderizado |
|---|---|---|---|
| C1 | rótulo y título `metric.split` | 4,5 | 6,572 · 6,463 |
| C2 | rótulo y título `metric.distance` | 4,5 | 7,016 · 6,900 |
| C3 | íconos @80 % (`timer` · `history`) | 3 | 4,746 · 4,680 · 5,062 · 4,992 |
| C4 | flecha @54 % | 3 | 5,099 · 5,048 |
| C5 | valores `metric.split.hud` · `metric.distance.hud` | 7 | 7,127 · 7,009 · 7,163 · 7,044 |
| C6 | metros de la vuelta `.hud @80 %` | 4,5 | 5,118 · 5,047 |
| C7 | rótulos `text.secondary` · vacíos `text.tertiary` | 4,5 | 7,448 · 7,352 · 5,099 · 5,048 |
| C8 | rótulo del chip sobre la capa al 4 % · al 8 % | 4,5 | 5,817 · 5,719 · 5,120 · 5,035 |
| C9 | la marca en su pico (gráfico) | 3 | 6,572 · 7,016 |

- **La capa de *hover*** sigue solo en la banda de la cabecera: sobre un valor bajaría a 6,309 (4 %) y 5,553 (8 %).
- **El anillo de foco** es el de los controles del HUD: blanco sobre su halo y el halo sobre una nube, **3,977:1** (H17 de la 4a). No es un par nuevo.
- **Sin umbral** (decorativos): el borde al 10 % (1,370) y la máscara de la columna. A mitad del fundido, el ámbar al 50 % sobre el vidrio al 50 % da 2,594 sobre el cielo: es lo que se va, no lo que se lee.

---

## 8. Legibilidad a 1,5 m

La tabla de 00-deuda-diseno §3.6 (TV) vale. De lejos, el remero lee la vuelta en curso (28 base: 7,5′, 1,5×), ve bajar la lista y entrar la marca, y reconoce cada panel por su color y su ícono. Con la escala de título de §4.8, el título en español queda en 10,4 base (8,0 dp de mayúscula en TV, 2,9′): se lee de cerca, como los rótulos de kcal y BPM. En W1280 todo es 1,20 / 1,054 = 1,14 veces lo de TV.

---

## 9. Capturas, demo y videos: qué tiene que verse

### 9.1 Reglas de toma (las de 02-tech-design §5.1, más estas)

1. **Un panel recién abierto o cerrado** se toma 0,82 s después del toque, o la toma arranca con la preferencia en verdadero.
2. **Ninguna toma fija cae en 0-1400 ms después de una vuelta o una repetición nueva**, salvo las que muestran la marca a propósito (ninguna CAP de la lista). Ya están corridas CAP-11 y DEMO-11 (DT5-04).
3. **Con desplazamiento**, siempre en 0.
4. **El contorno de la zona del bote** en CAP-03, CAP-14 a CAP-18.
5. **Las sondas de aspecto** (§9.6) en las tomas que cubren cada pieza, cada fondo (cielo, agua, el velo) y cada perfil; las demás tomas con un panel abierto dan los mismos píxeles en las mismas piezas (§15.2).
6. **Sin puntero de mouse sobre un panel**: las tomas tocan con `NavigationSubmitEvent`, así la capa de estado (§6.10) nunca aparece. Si una prueba toca con eventos de puntero, la fase de la capa bloquea la toma mientras corre.

### 9.2 Por captura (dp de ventana; SPM sin objetivo salvo lo dicho)

| ID | Qué tiene que verse |
|---|---|
| **CAP-01** (TV, 60,2 s) | Los dos chips de 144,4 × 35,8 en x 14,8-159,1: `500 m` en y **210,9-246,7** y `Reps` en 255,1-291,0, con 8,4 de aire. Ícono, rótulo y flecha ∨ centrados en el renglón; el vidrio vacío entre el rótulo y la flecha. A 32,7 dp de la zona del bote. Lo de la 4b igual: 8 píldoras, `Trabajo`, `600m` |
| **CAP-02** (TV, 170,2 s, parciales abierto) | Parciales en y 210,9-331,0 (114): `Parciales 500 m` a 0,949 (Unity mide 0,941: §15.2); `0:15.0` con `60 m` pegado a la derecha; `#1 2:05.0`; `Rep 1/3 · 560 m`. El chip `Reps` en 339,4-375,3 |
| **CAP-03** (TV, 250,2 s, los dos) | Como `docs/image-03.png`, con los cambios del sistema visual. Parciales 210,9-305,7 (90): `0:50.0` `200 m`, sin filas, `Rep 2/3 · 200 m`. `Series anteriores` 314,1-377,4 (60): `Rep 1` `2:05` y `680 m` pegado a la derecha. El contorno: `Series anteriores` entra en la zona (y > 323,7) solo hasta x 159,1, nunca en la columna del centro |
| **CAP-04** (TV, 380,2 s) | Parciales 210,9-305,7: `0:10.0` `40 m`, `Rep 3/3 · 40 m`. Repeticiones 314,1-400,6: `Rep 2` arriba y `Rep 1` abajo, `2:05` `680 m`, con las columnas alineadas |
| **CAP-05** (TV, 190,2 s) | Parciales 210,9-322,6 (106): `Sin parciales` / `en este paso` [cambia: corte equilibrado] en `text.tertiary`, y `Rep 1/3 · 640 m`. Repeticiones 331,0-419,5 (84): `Sin repeticiones` / `completas`. La barra en `Descanso` |
| **CAP-06** (TV, 180,6 s) | La barra ya en `Descanso` y `0:20`; parciales todavía con el trabajo (210,9-331,0): `0:25.0` `100 m`, `#1 2:05.0`, `Rep 1/3 · 600 m`. Repeticiones vacío (339,4-428,0) |
| **CAP-07** (TV, 545,2 s) | Parciales 210,9-299,4 (84): solo el vacío, sin la línea `Rep`; sin panel de repeticiones (las preferencias en verdadero no lo muestran) |
| **CAP-08** (TV, `Fondo 3000` con `R-4A`, 600,2 s) | Parciales 210,9-377,4 (158): `0:50.0` `250 m`; `#5`, `#4`, `#3` `1:40.0` y `#2` `2:05.0`, con los rótulos en la columna de `#99` y los tiempos alineados; sin `#1`, sin línea `Rep` y sin chip `Reps` |
| **CAP-09** (TV, `Trabajo 1000` con `R-3`, 170,2 s) | Parciales 210,9-307,8 (92): `0:03.3` `10 m`; `#1 2:46.7` |
| **CAP-10** (TV, `Cinco reps`, 545,2 s) | Parciales 210,9-305,7: `0:05.0` `20 m`, `Rep 5/5 · 20 m`. Repeticiones 314,1-423,7 (104): `Rep 4`, `Rep 3`, `Rep 2`, cada una `2:05` `540 m`, a escala 1 |
| **CAP-11** (TV, `Pirámide 500`, **881,2 s**) | SPM sin objetivo (el trabajo de 500 m tiene objetivo de Split y Watts). Parciales 210,9-305,7: `0:03.0` `12 m`, `Rep 2/4 · 12 m`. Repeticiones 314,1-377,4: `Rep 1 2:05 1.112 m` a **0,969** (± 0,01 de lo que mida Unity). Sin el sello |
| **CAP-12** (TV, en, CAP-03) | `500 m splits` y `Previous reps` a escala 1 (la escala de título en inglés); lo demás como CAP-03 |
| **CAP-13** (TV, en, CAP-05) | `No splits` / `in this step` [cambia] y `No completed` / `reps`; `Rep 1/3 · 640 m`. Los rectángulos de CAP-05 |
| **CAP-14** (P360, CAP-04, contorno) | Parciales x 12,9-139,4, y 184,7-267,8; repeticiones 275,2-350,9. Nada se cruza con Split ni con Watts; todo entero |
| **CAP-15** (TH, CAP-01, contorno) | Los dos chips enteros en y 210,9-246,7 y 255,1-291,0, con 18,8 dp hasta el piso (309,8). Sin máscara (nada escondido). La zona del bote, a 96,8 dp a la derecha |
| **CAP-16** (TH, CAP-04, contorno) | `ColumnScroll`, desplazamiento 0, máscara de y 297,2 a 309,8 (el piso). Se ven la cabecera y `0:10.0` `40 m` al 100 %; `Rep 3/3 · 40 m` al ≈ 0,925 (en la máscara); el vidrio de parciales se apaga desde y 297,2 hasta su borde de abajo (305,7), donde queda al ≈ 0,33. `Series anteriores` no se ve: empieza debajo del piso. Nada sobre la banda ni sobre los controles |
| **CAP-17** (L640, CAP-04, contorno) | Lo mismo en base: la máscara de y 260,3 a 271,4 y el vidrio de parciales hasta su borde (267,8). El panel no se angosta: termina en x 139,4, a 39,8 de la zona |
| **CAP-18** (W1280, 340,2 s, contorno) | `sp` 1,20 (± 0,005). Parciales x 22,4-186,8, y 320,2-457,0: `0:15.0` `60 m`, `#1 2:05.0`, `Rep 2/3 · 560 m`. Repeticiones 466,6-538,6: `Rep 1 2:05 680 m`. Todo sin desplazar y al 100 %, sin fundidos |
| **CAP-19** (TV, 197,6 s) | La cuenta grande `3` en su disco (x 167,6-243,4, y 234,0-309,9), con **8,4 dp (± 0,5)** de aire a los chips (210,9-291,0). `Próximo: Trabajo` y el halo |
| **CAP-20** (TV, CAP-19 con parciales) | La cuenta grande **en su lugar**, a 8,4 dp de parciales abierto (210,9-322,6: el vacío y `Rep 1/3 · 668 m`); el chip `Reps` en 331,0-366,8; `0:03` de la barra agrandada y teñida |
| **CAP-21** (TH, CAP-19 con los dos) | La cuenta grande en su lugar de TH (x 419,1-…). Parciales abierto en la columna: el vacío entero; la línea `Rep` todavía no se ve (su tinta pasa el piso); la máscara abajo. Nada se encima |
| **CAP-22** (TV, pausa a los 250,2, toma a los 252,2) | El velo; `Reanudar`; parciales 210,9-305,7 con `0:50.0` `200 m` quieto; repeticiones 314,1-377,4; **`PAUSA` en su disco**, a 8,4 dp |
| **CAP-23** (TH, CAP-22) | El velo y `PAUSA` en su lugar; la columna como CAP-16, con `0:50.0` `200 m` |
| **CAP-24** (TV, `Prueba 4a`, 90,6 s) | El sello en la tercera fila de la barra; las tarjetas en su lugar de la 4b; la columna ya subió (SPM perdió el objetivo): parciales 210,9-305,7 con `1:00.0` `240 m` y `Rep 1/2 · 240 m`; repeticiones vacío, 314,1-402,7 |
| **CAP-25** (TV, `S-LARGO`) | Parciales 210,9-400,6 (180); repeticiones 409,0-518,6 (104). Las escalas de §4.7: 0,652 · 0,932 · 0,712 · 0,638 (± 0,01 de lo que mida Unity). Nada cortado, partido ni fuera del vidrio |
| **CAP-26** (W1280, CAP-18, Tab × 3) | El anillo alrededor de parciales: blanco de 2 dp de x 18,4-190,8 e y 316,2-461,0 (por fuera), radio 20,8; el halo hasta x 14,4-194,8 e y 312,2-465,0, radio 24,8. Ni el anillo ni el halo tocan SPM (que termina en 307,4) ni el panel de abajo (466,6) |

### 9.3 Demo (W1280, sin objetivos: la columna desde y 320,2)

| ID | Qué tiene que verse |
|---|---|
| DEMO-01 | reposo, `Series 600` primera |
| DEMO-02 | el chip `500 m` solo, en y 320,2-361,0 |
| DEMO-03 | parciales abierto (84): `Parciales 500 m` y `Sin parciales` / `en este paso`; sin la línea `Rep` |
| DEMO-04 | parciales (90): `0:30.0` y ≈ `113 m`, `Rep 1/3 · ≈ 113 m`; el chip `Reps` en 437,8-478,6 |
| DEMO-05 | `Series anteriores` abierto, vacío (437,8-538,6) |
| DEMO-06 | parciales (114) con `#1` ≈ `2:12.6`. Repeticiones con sus dos renglones a la vista (§5.2: 83,2 de 84; el relleno de abajo, 0,96 dp más corto) |
| DEMO-07 | `Rep 1` `2:13` ≈ `676 m`; `Rep 2/3 · …` |
| DEMO-08 | el panel del simulador abierto (x 643-921) sin tocar los paneles; los metros de la vuelta en curso quietos y el tiempo avanzando |
| DEMO-09 | los metros siguen desde donde estaban; ninguna fila nueva |
| DEMO-10 | reposo |
| DEMO-11 (31,6 s) | los dos abiertos desde que se ven; `0:01.0` ≈ `3 m`; `Rep 1/3 · ≈ 3 m`; repeticiones vacío; nada de la sesión anterior |
| DEMO-12 | reposo; las preferencias en falso |

### 9.4 Videos (30 cuadros por segundo; el cuadro `n` del archivo, contado desde 0, es `inicio + (n + 1)/30`)

**La convención del arnés** (corregida en WP-D2, §15.4): el arnés hace la acción del instante `inicio + n/30`, avanza un cuadro y recién ahí graba el cuadro `n`. Así, el cuadro del toque o del dato ya muestra **33 ms** de su animación, y el cuadro `n + k` muestra `(k + 1)·33`. Un cambio que el motor hace en un segundo entero `T` aparece en el cuadro `30·(T − inicio)`, el mismo de la banda. Los números de abajo ya están con esta convención.

| ID | Cuadros clave y qué tiene que verse |
|---|---|
| **VID-01** (TV, 250,0-253,0; toques a 250,3 = cuadro 9 y 251,8 = 54) | Cuadros 9-29 (de los 33 a los 700 ms): la tabla de §6.1 (alto, contenido, título, flecha, revelar y FLIP de `Series anteriores`, ± 0,01 y ± 0,1 base). Cuadro 30 (251,0): `0:51.0` `204 m` y `Rep 2/3 · 204 m` con el rodillo, en el mismo cuadro que la banda. Cuadros 54-75: cerrar (el contenido se va antes de que baje el vidrio). El ancho no cambia en ningún cuadro y ninguna otra pieza se mueve |
| **VID-02** (TV, 323,5-327,5; vuelta a los 325,0 = cuadro 45) | Cuadro 45: `0:00.0` `0 m` y `Rep 2/3 · 500 m` con el rodillo, en el cuadro de la banda. Cuadros 45-48: el alto de 90 a 114 (114 desde el 49) y la línea `Rep` y `Series anteriores` bajando 25,3 dp con el empuje. Cuadros 45-50: `#1 2:05.0` entra desde −8·s (opacidad 0,331 en el 47 y 0,983 en el 50). La marca en su pico de los cuadros 51 a 62 y apagándose hasta el 86 (0,5 en el 74). Cuadro 75 (326,0): `0:01.0` `4 m` |
| **VID-03** (TV, 178,5-184,5) | Cuadro 45 (180,0): la barra pasa a `Descanso` y los paneles siguen con el trabajo (`0:25.0` `100 m`, `#1`). Cuadro 75 (181,0): lo viejo sale en los cuadros 75-77 (0 en el 78) y el vacío entra en 78-82 (subiendo 4); el alto de 114 a 106 en los cuadros 77-81; la línea `Rep` sube 8 y su rodillo pasa a `604 m` |
| **VID-04** (TV, 198,5-203,5) | Cuadros 15 y 45: la cuenta grande `1` y su salida, en su disco y sin tocar los paneles. Cuadro 45 (200,0): la barra al trabajo de la rep 2; los paneles siguen en el descanso. Cuadro 75 (201,0): el cambio de §6.3 en los dos paneles; `Rep 1 2:05 680 m` entra en los cuadros 78-82 con su marca |
| **VID-05** (W1280, 238-248) | Durante el corte, los metros de la vuelta en curso iguales y el tiempo +1 por segundo, cada uno con su rodillo en el cuadro del segundo. Después siguen desde los mismos metros. Ninguna fila nueva |
| **VID-06** (TV, `Cinco reps` → `Iniciar`; toque a los 0,1 s = cuadro 3) | Cuadros 18-26 (533-800 ms desde el toque): los dos paneles, abiertos, con SPM (opacidad y +8·s), ya con `0:00.0` `0 m`, `Rep 1/5 · 0 m` y el vacío. Cuadro 33 (1 s): `0:01.0` `4 m` con el rodillo (el arnés da `0 m`: H5-D2-02, §15.5). Nada aparece de golpe |
| **VID-07** (TV, salida) | En todos los cuadros de la salida (0-200 ms): `0:52.0` `208 m`, `Rep 2/3 · 208 m`, `Rep 1 2:05 680 m`, quietos, con la opacidad del HUD |
| **VID-08** (TV, reducir movimiento; **parciales cerrado y repeticiones abierto al empezar** [cambia]; toque a 323,8 = cuadro 9) | Cuadro 9: el alto de 34 a 90 en el acto, y `Series anteriores` corrido 59 dp en el mismo cuadro; el contenido funde en los cuadros 9-13 (lineal: 0,206 · 0,419 · 0,625 · 0,831 · 1); la flecha cambia de trazo con su fundido (∨ se va en 9-10, ∧ entra en 11-13). Cuadro 45 (325,0): el alto, la lista y la línea `Rep` en su lugar nuevo en el cuadro 45; `#1` funde en los cuadros 45-49, sin deslizarse; la marca, igual que en VID-02 |
| **VID-09** (TV, 196,5-199,5; toque a 197,3 = cuadro 24) | La cuenta grande aparece a los 197,0 (cuadro 15) y **sigue en su lugar** mientras parciales se abre a 8,4 dp (de 34 a 106, desde y 210,9: el vidrio hasta y 272,3 · 299,2 · 319,0 · 322,6 a los 100 · 200 · 400 · 820 ms). Ningún cuadro con un píxel de los dos encimado. `2` en el cuadro 45 y `1` en el 75; la cuenta de la barra sigue |

### 9.5 La regeneración única de `it4b`

En las tomas con rutina, debajo de SPM aparecen los chips (los dos si la rutina está en serie y el paso también; si no, solo `500 m`), cerrados, sin preferencias. Lo demás tiene que dar los mismos píxeles.

| Perfil | Chips con objetivo en SPM | Sin objetivo |
|---|---|---|
| TV · TH | 228,6-264,5 · 272,9-308,7 | 210,9-246,7 · 255,1-291,0 |
| P360 · L640 | 200,3-231,6 · 239,0-270,4 | 184,7-216,1 · 223,5-254,9 |
| W1280 | 347,1-387,9 · 397,5-438,3 | 320,2-361,0 · 370,6-411,4 |

En TH y L640 con objetivo, los dos chips entran con 1,1 base hasta el piso, sin máscara. En CAP-02 y CAP-27 de la 4b (la cuenta grande y `PAUSA`), el disco queda a 8,4 dp a la derecha de los chips.

### 9.6 Sondas de aspecto (la tabla de `SeriesCatalog`, con lo que midió WP-D2: §15.3)

Todos los esperados salen de las fichas compuestas en sRGB (sin literales). "Dominante" = el color más frecuente que no es el fondo, en la ranura o en la parte de ella que se dice.

| Sonda | Dónde (base del panel, salvo lo dicho) | Espera | Medido (A = B) |
|---|---|---|---|
| S5-01 · vidrio sobre el cielo | parciales de CAP-03 y de CAP-14 (P360), (5; 30) | `glass.fallback` sobre `backdrop.sky` (`#08162A`) ± 2 | `#08162B` |
| S5-02 · vidrio sobre el agua | repeticiones de CAP-04, (5; 60): y 377,4 dp, debajo del horizonte (365,6); parciales de CAP-16 (TH) y de CAP-18 (W1280), (5; 30) | ídem sobre `backdrop.water` (`#081627`) ± 2 | `#081627` |
| S5-03 · sin reflejo | parciales de CAP-03, (60; 3) y (60; 30) | los dos, el vidrio del cielo ± 2 | `#08162B` los dos |
| S5-04 · borde | a 0,4 dp del lado izquierdo, a la mitad del alto: parciales y la tarjeta de SPM en CAP-03 (cielo); parciales y la tarjeta de kcal en CAP-18 (agua) | `text.primary` al 10 % sobre el vidrio (`#212D3F` · `#212D3D`) ± 3 | `#212E41` · `#212E3D`, iguales en el panel y en la tarjeta |
| S5-05 · vuelta en curso | dominante de `hud-laps-current` en CAP-02 | `metric.split.hud` (`#FFAA47`) ± 3 | `#FFA947` |
| S5-06 · metros | dominante de `60 m` en CAP-02 (`.hud` al 80 %); el cuarto derecho de `hud-reps-row-0` en CAP-04 | `#CE8C41` ± 4 · `metric.distance.hud` (`#4ECBF2`) ± 3 | `#CE8C41` · `#4DCBF1` |
| S5-07 · rótulo `#n` | dominante del 16 % izquierdo de `hud-laps-row-0` (`#1`) en CAP-02 | `text.secondary` sobre el vidrio (`#B5B9BF`) ± 4 | `#B4B9BF` |
| S5-08 · vacío | dominante de `hud-laps-empty` (cielo) y de `hud-reps-empty` (agua) en CAP-05 | `text.tertiary` sobre el vidrio (`#8D949D` · `#8D949C`) ± 4 | `#8E949E` · `#8E949C` |
| S5-09 · la marca en su pico | VID-02, cuadros 51-62, (5,5; 74). **No es una sonda del arnés** (las tomas de video no llevan sondas): se mide en el video, que es H.264 4:2:0 con 1,31 px por dp | `metric.split` (`#FF9F1C`) ± 12 en el video; la motion, `marker=1` en el *manifest* | `#F69D27` a `#F89B24` |
| S5-10 · aire al disco | CAP-19 (el chip `Reps`, a la altura del centro del disco) y CAP-20 (parciales abierto), a la mitad del alto: cielo a 0,5 y a 7,9 dp del borde derecho del panel; el aro a 9,2 dp | `backdrop.sky` ± 2 · `step.rest` (el aro del descanso) ± 3 | `#0B4FA8` · `#06D6A0`; en la fila del centro del disco, 22,1 px de cielo (8,42 dp) |
| S5-11 · el anillo y su halo | CAP-26, a 3 dp por fuera del borde (el blanco) y a 6 dp (el halo), a la mitad del lado izquierdo | `text.primary` ± 3 · `overlay.scrim` al 50 % sobre el agua (`#072848`) ± 2 | `#FFFFFF` · `#072747` |
| S5-12 · la máscara de la columna (WP-D2) | parciales de CAP-16 (TH) a 6 dp del piso (310,15 dp); parciales de CAP-21 a 1,5 dp debajo del piso | el vidrio al 6 / 12·s = 0,474 de su alfa sobre el agua (`#0B345E`) ± 5 · el agua, sin vidrio, ± 2 | `#0B345E` · `#0E4F8F` |

---

## 10. Pedidos al Architect

Ninguno cambia un dato del producto. Los que tocan un texto de la spec van también al PO (§11). Se integran al contrato de motion (02-tech-design §7.4) y a las constantes (§3.2, §4.1). **El contrato USS (§7.5) no cambia.**

| ID | Qué | Cómo se verifica |
|---|---|---|
| **DD5-13** | **Revelar por la tinta** (§5.3, regla 1), en lugar del α_borde de DT5-02: `α = clamp((G − tintaAbajo) / SeriesRevealFade)`, con la tinta de cada clase de renglón de §4.6 (y su escala de ajuste). `SeriesPanelView` guarda los extremos de tinta cuando cambia el texto, nunca por cuadro | PlayMode: en reposo, todo renglón con α 1; durante VID-01, ningún cuadro con un renglón de α > 0 cuya tinta pase `G`; los alfas de §6.1 (± 0,02) |
| **DD5-14** | **La ventana de filas** (regla 2), siempre (también en `FitAll`), con su borde de abajo corrido por la cola; el borde de arriba solo con desplazamiento > 0 | PlayMode: la que se va de §6.2 (0,127 a los 67 ms, 0 a los 100); ninguna tinta de fila con α > 0,01 a menos de 4 base de la de la línea `Rep` o de la vuelta en curso, en todos los cuadros de VID-02 y de una rueda en W1280 |
| **DD5-15** | **La máscara de la columna** (regla 3), solo en `ColumnScroll`: el vidrio por vértice (franjas con color por vértice en `Allocate`) y los renglones por su tinta, con la fuerza `clamp(escondido / 12)` | CAP-15 sin máscara; CAP-16 con la línea `Rep` a 0,925 (± 0,02) y el vidrio de parciales al 0,33 (± 0,03) en su borde de abajo; CAP-21 con el vidrio en 0 en el piso |
| **DD5-16** | **El alto por un dato con el empuje** (R-04): `SeriesTimelines.RowsChange` y `.Swap` devuelven el alto con `ease.move` (crece en 0-160, se achica en 80-240) y el resorte queda en `Open` / `Close`. `SeriesPanelVisual` suma **`TailShiftBase`** (la línea `Rep` y la ventana de filas: `G − H_final` en un cambio por dato, 0 al abrir o cerrar). Si el resorte estaba corriendo, lo retoma con el alto nuevo y `corrimiento = min(empuje; G − H_final)` | `STL`: las tablas de §6.2 y §6.3; el aire mínimo entre lo nuevo y la cola ≥ 5 base en VID-03 y VID-04, y entre la fila nueva y la que baja ≥ 5 en VID-02 |
| **DD5-17** | **`SeriesRowInStartMs` = 80** (R-05) | la opacidad de §6.2: 0,331 a los 100 ms y 0,983 a los 200 |
| **DD5-18** | **El cambio de contenido por pieza** (R-06; §6.3): `HudSeriesChange` (o la vista, comparando el antes y el después) distingue la vuelta en curso ↔ el vacío (funde), trabajo → trabajo (rodillo), vacío → vacío (nada), filas que se vacían (salen en su lugar) y la línea `Rep` que entra o sale con la serie | dotnet: los casos de 02-tech-design §2.5 con la pieza que cambia; PlayMode: en `Dos series` a los 61 s, la vuelta en curso no funde; en `Series 600` a los 541, el vacío no se mueve |
| **DD5-19** | **Los vacíos con corte equilibrado** (R-07; §4.8) | CAP-05: `Sin parciales` / `en este paso`; CAP-13: `No splits` / `in this step` y `No completed` / `reps` |
| **DD5-20** | **Una escala de título por idioma** (R-08): la menor de los dos títulos, calculada con el idioma y la ventana, aunque un panel no se vea | es: 0,949 en los dos títulos (± 0,005); en: 1 en los dos; no cambia cuando aparece o se va el panel de repeticiones |
| **DD5-21** | **El vidrio como las tarjetas de hoy** (R-09): relleno, borde de 1·sp al 10 % y radio 14·sp, **sin reflejo** (sale de la capa del vidrio de 02-tech-design §3.1) | S5-01 a S5-04 |
| **DD5-22** | **El anillo de foco** en dp de ventana (R-10): blanco de −4 a −2 y halo de −8 a −4 del borde visual, radios 14·sp + 4 y 14·sp + 8, siguiendo `G` | CAP-26 y S5-11; en VID-01 con foco, el anillo a 4 dp del vidrio en cada cuadro |
| **DD5-23** | **La anatomía como constantes**: los puntos de la flecha (§4.3, `SeriesChevronStroke`), la caja del ícono (x 10-24), el comienzo del rótulo (x 28), los metros pegados a x 127, la columna del rótulo (`#99` o el más ancho), la marca en x 4-7 y la vertical de §4.6 | PlayMode: las ranuras de CAP-02 a CAP-05 con sus rectángulos (± 0,5 dp); la línea de base del tiempo y de los metros de la vuelta en curso, iguales (± 0,25 dp) |
| **DD5-24** | **Las escalas comunes de las filas** (vueltas y repeticiones) y su cambio con el empuje (§4.7) | CAP-11 a 0,969; CAP-25 a 0,932 y 0,638; una fila nueva que cambia la escala no salta |
| **DD5-25** | **Los checks de las tomas** con la geometría real: CAP-01 y todas las de `Series 600` con la columna desde 210,9 en TV (no 228,6); TH y L640 con 93,9 base; CAP-16 y CAP-17 como en §9.2; VID-08 con parciales cerrado al empezar (R-15) | el *manifest* con los rectángulos de §9.2 y §9.3 (± 0,5 dp) |
| **DD5-26** | **El reparto de `RowsScroll`** (§5.2): repeticiones con su mínimo reservado (34; abierto, 60 o su natural); parciales `min(natural; lugar − 8 − ese mínimo)` | dotnet (`SeriesPanelGeometryTests`): los cuatro casos de la tabla de §5.2, con y sin objetivo |
| **DD5-27** | **La fila que entra con la vista desplazada**, **solo en `RowsScroll` (W1280)**: el desplazamiento suma 22·sp·k en el mismo cuadro. En `ColumnScroll` (TH, L640) no se compensa: lo de abajo baja con el empuje (QA-V5-R2-01, opción a, al empezar la 6a) | PlayMode: en W1280 con la rueda abajo, una vuelta nueva no mueve ninguna fila a la vista. En TH, la columna desplazada no salta (`SCT.TheColumnScrollFollowsAColumnThatShrinks`, sin cambios) |
| **DD5-28** | **La capa de estado como fase de `SeriesPanelMotion`** (gate, QA-G5T-11; §6.10): la línea de tiempo pura en `SeriesTimelines` (desde el alfa actual; 90 `ease.enter` al subir, 160 `ease.exit` al bajar; con reducir movimiento, igual), la fase en `SeriesPanelMotion` (**F** mientras corre; `Stop()` la deja en 0; `Instant`, en su objetivo), `StateLayerAlpha` en `SeriesPanelVisual`, y los eventos de la tabla de §6.10 en `SeriesPanelView` (el *hover* solo con un puntero de mouse). Constantes `SeriesStateLayerInMs` / `OutMs`, `SeriesHoverAlpha` y `SeriesPressedAlpha` (§6.9) | dotnet (`STL`): los valores por cuadro de §6.10 (± 0,001) y la inversión desde el alfa actual. PlayMode (`SMT`): `IsFinite` durante los 90 y los 160, y no con el *hover* quieto; `Stop()` en 0; la variante con reducir movimiento igual; 0 B por cuadro. PlayMode (vista): con un mouse, entrar, presionar, soltar y salir dan 0,04, 0,08, 0,04 y 0; con un puntero táctil no hay *hover*; un arrastre de más de 8 dp apaga la capa; `ITT.NothingAnimatesAfterTheExit` con el puntero de mouse sobre un panel al tocar `Terminar` con el teclado |

**Prioridad:** DD5-13 a DD5-17 (sin ellos hay tinta encimada o un último renglón al 67 %) y DD5-25 (las tomas fallarían por la geometría) antes de WP-B1; DD5-28 con su línea de tiempo en WP-A y su fase en WP-D1 (los dos antes de WP-B1); los demás, en WP-B1 y WP-B3.

---

## 11. Para el PO y para QA

### 11.1 Lo que cambia de la spec (se suman a SD-01 a SD-11 de 02-tech-design §8.2; **decididos por el PO en el gate**)

El PO adoptó todos ([01-po-spec §11.3](01-po-spec.md)). SD-15 a SD-17 llevan los números que propuso QA (QA-G5P-02) y SD-18 y SD-19 los numeró el PO (QA-G5T-02); acá van con esos mismos números. La columna "La spec decía" es lo de antes del gate: la spec ya está reescrita con lo propuesto. Lo que se ve distinto del legacy va al usuario con **DV-50** y **DV-51**, junto con las demás DV.

| # | Criterio | La spec decía | Propongo | Por qué | Gate |
|---|---|---|---|---|---|
| SD-12 | AC-WKT-18-19, último punto | "El alto se acomoda con el resorte" | **con el empuje de la lista** (160, `ease.move`); el resorte, al abrir y cerrar | §6.2 y §6.3: con el resorte, la línea `Rep` y lo nuevo se enciman | adoptado (informativa) |
| SD-13 | CAP-05 y CAP-13 | `Sin parciales en este paso` y `No splits in this step` (sin decir el corte) | `Sin parciales` / `en este paso`, `No splits` / `in this step` | §4.8: el texto no cambia, solo dónde se corta | adoptado (DV-51) |
| SD-14 | VID-08 (§7.5) | "VID-02 con reducir movimiento, y un toque en `500 m` a los 323,8 s" | **con parciales cerrado y repeticiones abierto al empezar**, así el toque lo abre | con los dos abiertos, el toque lo cierra y la vuelta nueva de los 325 no se ve | adoptado (de prueba) |
| SD-15 | AC-WKT-18-03 | el título "con el estilo del chip" (legacy `_labelStyle`: 13/600, 0,8 dp de espaciado, al 80 %) | **11/600, sin espaciado, en el color pleno**, a una escala por idioma (es 0,949, en 1) | **El ancho de 137**: con el estilo del chip, `Parciales 500 m` pide 112,7 y `Series anteriores` 120,3 de los 85,7 que hay. Irían a 0,761 y 0,712, debajo del mínimo de 0,8: en español los dos se cortarían con `…`, y sin el mínimo quedarían en 9,9 y 9,3 base, más chicos que los 11 y 10,4 de ahora. **El contraste**: el color, por lo mismo que SD-17 (§4.2, §4.3; `sd15_17.py`) | adoptado (DV-51) |
| SD-16 | AC-WKT-18-04 | los metros de la vuelta en curso "al lado" del tiempo (el legacy, a 6) | **pegados al borde derecho** (terminan en x 127), en la misma línea de base y con al menos 6 de aire | Una sola columna de metros a la derecha en los dos paneles: los de las repeticiones también van a la derecha en el legacy (`series_panels.dart:380-387`). Además, su lugar no depende del ancho del tiempo (`10:00.0`, la escala de ajuste). Con tres cifras, desde los 100 m de cada vuelta, quedan 2,0 base más a la derecha que en el legacy; con dos cifras, 8,8, y con una, 15,5 (§4.4; `sd15_17.py`) | adoptado (DV-51) |
| SD-17 | AC-WKT-18-02 | el rótulo del chip al 80 % del color (el legacy) | **en el color pleno**, con 0,08 em: el rótulo de SPM de la app | **La misma regla que las tarjetas**: el legacy usa para el chip el estilo del rótulo de SPM, y la app ya lo pasó al color pleno en la 4a (DV-31, AC-APP-06-01). **El contraste**: al 80 % no llega a 4,5:1 debajo de la capa de estado: `metric.split` da 4,285 con el *hover* y 3,847 presionado, y `metric.distance`, 4,569 y 4,099. En el color pleno, 5,120 y 5,467 presionado (C8). El espaciado de 0,08 em suma 1,2 base a `500 m` contra el 0,8 dp del legacy (§4.2; `sd15_17.py`, `sd15_19b.py`) | adoptado (DV-51) |
| SD-18 | AC-WKT-18-02 | la flecha "a 6" del texto (el legacy) | **pegada al borde derecho del contenido** (tinta en x 118-127), con vidrio vacío en el medio | Con un solo ancho, la flecha gira en su lugar al abrir. A 6 del texto, quedaría en x 84,0-92,6 con `500 m` y en 74,1-82,7 con `Reps`: las de los dos chips, una encima de la otra, corridas 9,9 base entre sí, y al abrir tendría que viajar 34 o 44 base hasta su lugar abierto, que también es a la derecha en el legacy (§4.2; `sd15_19b.py`) | adoptado (DV-50) |
| SD-19 | AC-WKT-18-04, -07 | "2 entre filas" (el legacy: 2 arriba y 2 abajo de cada fila, 4 entre filas, y 4 debajo de la vuelta en curso también sin vueltas) | **2 entre filas**; el 4 debajo de la vuelta en curso, solo con vueltas | Con el aire del legacy, la vista mínima de AC-WKT-18-13 pasa de 182 a 190 base y, en W1280, `sp` baja de 1,20 a 1,15 (todo un 4 % más chico); lo más alto, de 292 a 310. Con 2, entre las tintas de dos filas quedan 8 base (5,1 en repeticiones). Sin vueltas, la línea `Rep` queda a su aire de 6, como siempre (§4.4; `sd15_19b.py`) | adoptado (DV-51) |
| — | AC-WKT-18-19 | la fila "se desliza desde −8 dp" | desde −8·sp, en 80-240 (SD-09, ajustado) | §6.2 | adoptado (SD-09) |

P-03 (sí, solo abiertos y solo en la franja de afuera) y P-04 (no hace falta un segundo lugar) quedan como en 00-deuda-diseno §8.2. **Si el usuario rechaza DV-50 o DV-51**, cambian constantes (R5-07): el título con el estilo del chip se achica hasta 0,8 y se corta con `…` en español; los metros van a 6 del tiempo; el rótulo y el título, al 80 %, y entonces la capa de estado no puede pasar debajo de ellos (con cualquiera de las dos, `metric.split` baja de 4,5); la flecha, a 6 del texto y con un desplazamiento horizontal al abrir; y las filas con 2 + 2, con `sp` 1,15 en W1280.

### 11.2 Para QA

- **DT5-02 revisado** (§5.3): las tres reglas, con sus constantes y sus pruebas (DD5-13 a DD5-15).
- **Las tablas** de §6.1 a §6.3 y §6.8, y el aire mínimo entre tintas (DD5-16).
- **Los rectángulos** de §9.2 y §9.3, y las sondas de §9.6.
- **Los pares** C1-C9 (§7), sin cambios.
- **Las rutinas sin objetivo**: si una toma de `Series 600` da la columna en 228,6 en TV, está mal la toma, no la geometría.
- **La capa de estado** (§6.10, DD5-28): su tabla por cuadro, que bloquee la toma mientras corre y que `Stop()` la deje en 0.
- **SD-15 a SD-19** (§11.1): lo que se mide en las tomas. El título a 0,949 en español; los metros de la vuelta en curso y los de las repeticiones con el mismo borde derecho (± 0,5 dp en CAP-03); la flecha de los dos chips en la misma x (CAP-01); y las filas cada 22·sp (CAP-04, CAP-08).

---

## 12. Plan de WP-D2 (después de WP-B3) y biblias

1. **Las tomas**: CAP-01 a CAP-26 y DEMO-01 a DEMO-12 contra §9.2 y §9.3; las sondas de §9.6 en la tabla de `SeriesCatalog` (excepción de 02-tech-design §7.6), con los valores medidos.
2. **Los videos, cuadro por cuadro**: VID-01 a VID-09 contra §6 y §9.4; el revelar por renglón (DN5-01) en VID-01, -02, -03 y -08. Si una constante de §6.9 necesita otro valor, cambio solo esa constante y su valor en la prueba.
3. **Las biblias**: lo de 00-deuda-diseno §9, con estas revisiones:

| Biblia | Dónde | Qué |
|---|---|---|
| ART_BIBLE | §3.2 | el reflejo interior "llega con el vidrio de la 6b, para todas las piezas"; hasta entonces ninguna lo dibuja |
| ART_BIBLE | §3.3 | el modo compacto de la barra libre (00-deuda-diseno §1.3) |
| ART_BIBLE | §3.4 | se reescribe con §3 y §4 de este documento: 137 fijo, la anatomía con coordenadas, los colores, la marca, los estados y la escala de título, con lo que decidió el gate (SD-15 a SD-19, DV-50, DV-51); sale "expandido fijo 200 (en tablet 240)" |
| ART_BIBLE | §4.3 | `num.lap` 28/28, `num.row` 20/22 y `num.xs` 13/16 |
| ART_BIBLE | §5.2 | la columna de los paneles, las tres reglas de §5.2 y la zona del bote con los abiertos en vertical (P-03) |
| ART_BIBLE | §10.1 y §10.2 | el grupo C1-C9 y la legibilidad de los paneles |
| ANIMATION_BIBLE | §2.3 | "lo que cambia el dato se acomoda con el empuje; lo que abre el usuario, con el resorte" |
| ANIMATION_BIBLE | §2.10 | se reescribe con §6, con la capa de estado de §6.10: sale "alto y ancho (140↔200)" y el fondo `metric.split @20 %` |
| ANIMATION_BIBLE | §2.11 | la cascada con los paneles en el tramo de SPM |
| Las dos | al final | "Cambios al iniciar la It. 5" y "Cambios de la It. 5" |

---

## 13. Qué queda abierto

- **La pala izquierda en vertical** (P-03): `Series anteriores` abierto la tapa mientras está abierto (00-deuda-diseno §2.2). Se decide en la 7, con el bote a la vista; la alternativa medida es ponerlo debajo de Watts.
- **El vidrio con desenfoque y el reflejo** (6b): llegan a todas las piezas juntas, también a los paneles.
- **Un indicio de "hay más" en W1280**: con las filas desplazables no se ve la siguiente, porque sin recorte no puede asomarse sin encimarse con la línea `Rep`. Es como el legacy, que tampoco tiene barra. Si el usuario lo pide, el lugar es la cabecera (sin elementos nuevos).
- **El aviso de cada vuelta con la diferencia** (P-09, BKL-26): en la fila del sello, si algún día se hace.

---

## 14. Verificación hecha

1. **Legacy, solo lectura.** Se leyeron `series_panels.dart` entero y `docs/image-03.png`. `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar, y no se leyó `strava_secrets.dart`.
2. **Las tomas de la 4b**, miradas y medidas con Pillow:
   - CAP-02 (TV) y CAP-24 (W1280): el relleno de las tarjetas, plano desde el borde (`#08162B` y `#081627`: no hay reflejo), y el borde de 1·s (`#212E41`, `#212E3D`);
   - CAP-02, -21, -23, -27 y DEMO-08: dónde quedan SPM, el disco, la llave, la banda y el panel del simulador, que coinciden con la geometría de §5.1.
3. **Las fuentes del repo**: la altura de mayúscula, el ascendente y el descendente de Inter y de RowMate Numeric SC (fontTools), y los anchos con las tablas K2 versionadas (§4.7 y §4.8).
4. **La geometría** (`geo_it5.py`): las fórmulas de `HudGeometry` reproducen los números de 00-deuda-diseno §2.2 y §2.3 y dan los rectángulos de §9.2, §9.3 y §9.5. Qué rutinas tienen objetivo sale de `Tests/Support/RoutineSeeds.cs`.
5. **Las curvas**: las tablas de §6 salen de `motion_it5.py` y `data_resize.py` (las cubic-bezier de `Tokens.cs` y `CriticalSpring` exacto). Los aires entre tintas salen de `motion_it5.py` (la fila nueva: −1,22 con 60-240 y 5,48 con 80-240) y de `swap_check.py` (el cambio que crece: −2,78 con el resorte y 5,34 con el empuje).
6. **Los contrastes**, con el método de `Contrast.cs` (`contrast5.py` de la base y `probes5.py`).
7. **El repositorio.** Se escribió solo este documento, con rutas relativas y sin datos de la PC. No se tocó código, tokens, capturas ni biblias. Unity no se abrió y dotnet no se corrió. Sin *commits*.
8. **En el gate** (`<scratch>/it5/designer-gate/`): `sd15_17.py` y `sd15_19b.py`, con `measure4b.py` y `rmcore.py` de la base. Los anchos con el estilo del chip, dónde quedarían los metros y la flecha con la regla del legacy, los altos con el aire del legacy, los contrastes al 80 % con la capa de estado y la capa de estado por cuadro. Además se leyeron, sin tocarlos, `Presentation/Styles/{Motion,Base,Typography,Tokens}.uss` (las capas de estado de los controles, sus tiempos y el espaciado de `rm-type-label`), `Presentation/Workout/HudCard.cs` (el rótulo de las tarjetas en el color pleno) y `Core/Design/Tokens.cs` (`dur.micro` 90, `dur.fast` 160).

---

## Cambios por gate QA

Correcciones por [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md), alineadas con la spec ya corregida ([01-po-spec §11.3](01-po-spec.md): SD-01 a SD-19 decididos, DV-50 y DV-51). Se verificaron contra el legacy (`3ae0f3a`, solo lectura): `series_panels.dart` entero y `immersive_workout_screen.dart:760-805` (el rótulo de las tarjetas, del mismo estilo que el chip). No se leyó `lib/core/strava/strava_secrets.dart`, y `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar. **No se disputa ningún issue.** Unity no se abrió, dotnet no se corrió (no hay código) y no hay *commits*.

| Issue | Qué cambió | Dónde |
|---|---|---|
| **QA-G5P-02** (mayor) | Las tres contradicciones con un AC o con el legacy entran como **SD-15** (el título en 11/600 sin espaciado, no el estilo del chip), **SD-16** (los metros de la vuelta en curso a la derecha, no a 6 del tiempo) y **SD-17** (el rótulo del chip en el color pleno, no al 80 %), con los motivos medidos: el ancho de 137 (con el estilo del chip, los dos títulos en español quedan debajo de 0,8 y se cortarían), la columna común de metros a la derecha (2,0 base de diferencia con tres cifras) y el contraste (al 80 %, el rótulo baja de 4,5:1 debajo de la capa de estado: 3,847 presionado), además de DV-31 de la 4a. Se suman **SD-18** (la flecha pegada a la derecha) y **SD-19** (2 entre filas), con los números del PO (QA-G5T-02). El PO ya adoptó los cinco: la tabla dice "adoptado" y en qué DV van, y cómo se vuelve atrás si el usuario rechaza DV-50 o DV-51. §4.4 corrige un motivo mal dicho: los metros del legacy no "bailan" con un tiempo de cifras tabulares, solo cambian de lugar cuando el tiempo cambia de ancho | §0 (punto 15), §2 (R-16), §3, §4.2, §4.3, §4.4, §11.1, §11.2, §12 |
| **QA-G5T-11** (menor) | Los fundidos de *hover* y presionado tienen dueño: **una fase finita de `SeriesPanelMotion`** (no instantáneos, porque los demás controles del HUD funden su capa con los mismos tiempos). Su tabla de eventos y valores por cuadro; **F** mientras corre (un *hover* quieto no bloquea); `Stop()` la deja en 0; con reducir movimiento, igual, porque es opacidad; el *hover* solo con un puntero de mouse, y las tomas, sin puntero. Constantes `SeriesStateLayerInMs` / `OutMs` (90 / 160) y `SeriesHoverAlpha` / `SeriesPressedAlpha` (0,04 / 0,08). Pedido nuevo al Architect: **DD5-28** | §0 (punto 14), §2 (R-17), §3, §4.9, §6.8, §6.9, **§6.10 (nueva)**, §9.1 (regla 6), §10, §11.2 |
| QA-G5T-04 (del Architect; lo que me toca) | El área de toque y el corrimiento del panel de abajo (FLIP) dicen qué se ve y qué responde, no con qué mecanismo: dibujo, caja de *layout*, hijo oculto o `style.translate` los elige el Architect, y los valores por cuadro son los mismos | §5.5, §6.1 |
| QA-G5T-12 (del Architect; lo que me toca) | Un panel que vuelve a un panel de UI Toolkit: todo en su estado final, sin marcas y con la capa de estado en 0 | §6.7 |

**Para los otros roles** (no los hace el Designer):

- **Architect**: integrar **DD5-28** con los demás pedidos de §10 (la línea de tiempo en `SeriesTimelines`, WP-A; la fase y `StateLayerAlpha` en `SeriesPanelMotion` y `SeriesPanelVisual`, WP-D1; los eventos en `SeriesPanelView`, WP-B1), con sus pruebas en `STL`, `SMT` e `ITT`, y las constantes de §6.9 en `MotionSpecs`. No suma archivos: van en los que ya tienen esos paquetes.
- **QA**: verificar SD-15 a SD-19 en las tomas con lo que dice §11.2, y la capa de estado con DD5-28.

---

## 15. Cambios por implementación de WP-D2

WP-D2 (el pulido del Designer: 02-tech-design §7.0 punto 7 y §7.3, §12 de este documento), 2026-10-30, sobre WP-A, WP-D1 y WP-B (B0 a B3) ya hechos. Fuente: las tomas y los videos de `it5` del arnés real (dos corridas: A a `<scratch>/it5/wp-d2pulido/capA`, B al repo), mirados uno por uno y medidos píxel a píxel y cuadro por cuadro con programas propios (Pillow y PyAV por `uv run --offline`, sin red, en `<scratch>/it5/wp-d2pulido/tools/`), los valores por cuadro que el arnés deja en el *manifest* y las pruebas de dotnet y de Unity. **No cambia el QUÉ ni la lógica**: lo que necesita código de WP-B va como hallazgo (§15.5). Ninguna constante de §6.9 necesitó otro valor.

### 15.1 Qué se escribió

| Archivo | Qué |
|---|---|
| `Snapshots/SeriesCatalog.cs` | **Solo la tabla de las sondas de aspecto** (`AddAspectProbes`, la excepción de dueño de 02-tech-design §7.6) y su ayudante `SlotPart` (una parte horizontal de una ranura, para mirar el rótulo o los metros de una fila). De las 8 sondas que dejó B3 (S5-01, -02, -03 ×2, -05, -06, -08 y -11) a **27**: el borde contra la tarjeta (S5-04, cielo y agua), el rótulo `#n` (S5-07), los metros de la repetición (S5-06), el vacío sobre el agua (S5-08), el vidrio en P360, TH y W1280 (S5-01 y S5-02), el aire al disco (S5-10, seis puntos), el halo del anillo (S5-11) y la máscara de la columna (S5-12, nueva). Los esperados, compuestos desde las fichas (`Over`, `PixelProbe.Composite`; sin literales). No toca ninguna toma, comprobación ni video |
| `docs/…/05-series-parciales/snapshots/*` | La corrida B: **las 47 imágenes y videos iguales byte a byte** a los de B3 (las sondas no cambian lo que se dibuja); cambian `manifest.json` (las 27 sondas) e `INDEX.md` (la columna de sondas) |
| Este documento | §9.1 (regla 5), §9.2 (CAP-02), §9.4 (la convención de los cuadros y los números de VID-01 a -04, -06 y -08), §9.6 (la tabla de sondas con lo medido) y esta sección |
| `docs/migration/design/{ART_BIBLE,ANIMATION_BIBLE}.md` | §12 de este documento: ART_BIBLE §3.2, §3.3, §3.4, §4.3, §5.2, §10.1 y §10.2; ANIMATION_BIBLE §2.3, §2.10 y §2.11; "Cambios al iniciar la It. 5" y "Cambios de la It. 5" en las dos |

`Skin.uss`, `Motion.uss` y `Tokens.cs` **no cambian**: el panel no tiene aspecto en USS (02-tech-design §7.5) y los 137 tokens son los de WP-A.

### 15.2 Las tomas: el vidrio contra las tarjetas, la anatomía y la máscara

Las 26 CAP y las 12 DEMO, miradas enteras y en recortes de los paneles. **Todas como §9.2 y §9.3**, sin nada cortado, partido ni encimado; las comprobaciones de B3 (los rectángulos ± 1 dp, la geometría ± 0,5, los textos, las escalas) pasan en las dos corridas.

**El vidrio es el de las tarjetas (DN5-02, DD5-21).** Medido en la misma toma, en la fila de píxeles de la mitad del alto, desde afuera del borde izquierdo:

| Toma | Pieza | Afuera | Borde (px, de afuera adentro) | Relleno | Arriba (a 1,6 y a 3 dp del borde) |
|---|---|---|---|---|---|
| CAP-03 (TV, 2,625 px/dp) | parciales | `#0B4FA8` | `#212E41` · `#212E41` · `#1B283C` (2,6 px = 1·sp) | `#08162B` | `#08162B` · `#08162B` |
| CAP-03 | la tarjeta de SPM | `#0B4FA8` | `#212E41` ×3 · `#0C1A2E` (3,2 px) | `#08162B` | ídem |
| CAP-03 | la tarjeta de kcal (agua) | `#0E4F8F` | `#1E3248` · `#212E3D` ×2 · `#101D2E` | `#081627` | `#081627` |
| CAP-18 (W1280, 1 px/dp) | parciales (agua) | `#0E4F8F` | `#212E3D` · `#0D1B2B` (1,2 px = 1·sp) | `#081627` | ídem |
| CAP-18 | la tarjeta de kcal | `#0E4F8F` | `#193A5C` · `#212E3D` (1,6 px = 1·s) | `#081627` | ídem |
| CAP-14 (P360) | parciales | `#0B4FA8` | `#0B4CA2` · `#212E41` ×2 · `#132034` | `#08162B` | `#08162B` |

- **El mismo color** en el relleno y en el borde, sobre el cielo y sobre el agua; **sin reflejo** (el relleno es plano desde el borde, en el panel y en la tarjeta).
- **El grosor** difiere en menos de un píxel: el borde de USS de la tarjeta se ajusta al píxel (3 px en TV) y el del panel, dibujado con `Painter2D`, es 1·sp con antialias (2,6 px). A 1,5 m no se distingue; no pido cambiarlo.
- **En pausa**, el vidrio sobre el cielo velado da `#071527` (CAP-22), lo de §4.1.

**La anatomía (DD5-23)**, la tinta medida en base del panel (TV, sp 1,053 medido):

| Pieza | Notas (§4) | Medido |
|---|---|---|
| La flecha (los dos chips de CAP-01, y CAP-03 abierta) | x 118-127, y 15,1-18,9 ± 0,75 de trazo | x 118,29-127,33 · y 14,47-19,90, **la misma x en los dos chips** (SD-18) |
| El ícono `timer` | su caja en x 10-24, y 10-24 | x 11,58-22,43 · y 10,49-23,51 |
| El rótulo `500 m` (13/600, 0,08 em) | desde x 28, la banda de las mayúsculas centrada en y 17 | x 28,58-71,62 · y 12,30-21,70 (centro 17,0) |
| El título a la escala del español | desde x 28, centrado en 17 | x 28,94-107,44 (`Parciales 500 m`) y hasta 112,14 (`Series anteriores`, con lugar hasta 113,7) · y 13,02-20,98 |
| La vuelta en curso | la tinta de las cifras en y 36,2-55,8; los metros terminan en x 127 | y 35,81-56,43 · x hasta 126,61; el tiempo y los metros por la misma línea de base (las dos ranuras terminan en 269,914 dp) |
| Las filas de repeticiones | rótulo y valor por la línea de base 49; la `p` hasta 51,9 | y 34,73-51,73 |
| Los metros de la vuelta y los de la repetición | el mismo borde derecho (SD-16) | `200 m` y `680 m` terminan en la misma columna (CAP-03, ± 0,25 dp) |

**Lo que mide Unity (R5-02)** contra las tablas K2 de §4.7 y §4.8: la escala del título en español **0,941** (0,942 en W1280; las notas, 0,949: Unity mide `Series anteriores` en 91,1 base y no en 90,3); `Rep 1 2:05 1.112 m` **0,958** (0,969); en `S-LARGO`, la cifra 0,652 (igual), `#1999` 0,926 (0,932), la línea `Rep` 0,707 (0,712) y las repeticiones **0,631** (0,638), sobre el piso de 0,6 y sin `…`. Todas dentro de lo que piden las comprobaciones (± 0,01 a ± 0,02), y ninguna cambia lo que se ve: la escala de título sigue siendo una por idioma, la de los dos títulos.

**Los colores de los trazos** (dominantes, CAP-02 a CAP-05), a 0 o 1 por canal de la ficha compuesta sobre el vidrio: la cifra `#FFA947`, los metros `#CE8C41`, los títulos `#FF9F1B` y `#4BC9EF`, los íconos `#CE841E` y `#3EA5C8`, la flecha `#8E949E`, `#1` y `Rep n` `#B4B9BF`, los vacíos `#8E949E` (cielo) y `#8E949C` (agua), la línea `Rep` `#4DCBF1`.

**El aire al disco (S5-10).** En la fila de píxeles del centro del disco de CAP-19 y CAP-20: el borde del panel termina en el píxel 418 (30 % de borde), el cielo va del 419 al 439 y el aro empieza en el 440 (60 %): **22,1 px = 8,42 dp** (8·s). En CAP-22 (`PAUSA`), lo mismo con el cielo velado y el aro amarillo.

**La máscara de la columna (DD5-15)**, la fracción del vidrio por fila de píxeles (el azul, contra el agua de al lado y el vidrio lleno de arriba) y `clamp((piso − y) / 12·sp)`:

| Toma | y (dp) → medido · fórmula | En el borde de abajo del panel |
|---|---|---|
| CAP-16 (TH; el piso en Unity, 310,15) | 298,5 → 0,913 · 0,919; 301,9 → 0,644 · 0,652; 305,3 → 0,375 · 0,381 | 306,0 dp → 0,33 (las notas: 0,33 ± 0,03); `Rep 3/3 · 40 m` a 0,925 |
| CAP-17 (L640; piso 271,4) | 261,5 → 0,913; 264,5 → 0,644; 267,5 → 0,375 (+ 0,02 de la fórmula) | 268,1 dp → ≈ 0,32 |
| CAP-21 (TH, el vacío abierto) | 0,990 a 297,3; 0,288 a 306,5; 0,058 a 309,9; 0 desde 311 | el vidrio de parciales se apaga en el piso; la línea `Rep`, en 0 |
| CAP-15 (TH, los dos chips) | sin máscara (nada escondido) | — |

Las notas usaban el piso con s = 1,054 (309,8); Unity usa 1,0549 (310,15), y con ese piso la fórmula da lo medido a ± 0,01. El texto se apaga con la máscara por su tinta, sin recorte: no queda una franja dura en el piso.

**Un ajuste al píxel que no se ve.** UI Toolkit ajusta al píxel el borde de arriba de cada panel (en W1280, repeticiones empieza en 467 y no en 466,4; en TV el aire entre los dos da 8,29 dp y no 8,44). Son 0,6 px como mucho; las comprobaciones lo absorben (± 1 dp) y no pido cambiarlo.

### 15.3 Las sondas

**27 sondas de aspecto en 11 tomas, las 27 dentro de su tolerancia en las dos corridas, a 0, 1 o 2 por canal** (la tabla de §9.6, con lo medido). Antes de correr Unity las calculé con un espejo en Python de `PixelProbe` (el mismo redondeo de `Mathf.RoundToInt`) sobre las tomas de B3, y dieron lo mismo. Los puntos están elegidos para que no caigan en un borde de 1 px:

- **S5-04** a 0,4 dp del lado (no a 0,5: en W1280 el 22,5 redondea al par, que es el borde, pero la sonda no tiene que depender de eso);
- **S5-10** a 0,5 y 7,9 dp (cielo) y a 9,2 dp (el aro): un corrimiento de medio dp del panel o del disco hace fallar una de las tres;
- **S5-12** a 6 dp del piso, con ± 5 (el degradé vale ≈ 8 por dp en el azul y el piso se corre un tercio de dp con la escala).

**S5-09 no está en el arnés**: las tomas de video no llevan sondas. Lo medí en el video (§15.4): la marca en su pico da `#F69D27` a `#F89B24` contra `#FF9F1C`, por la compresión H.264 4:2:0 de una franja de 4 px; el valor de la motion (`marker=1`) está en el *manifest* de los cuadros 51 a 62.

### 15.4 Los videos, cuadro por cuadro (y el "se descubre por renglón", DN5-01)

**La convención de los cuadros** (corrige §9.4): el arnés graba el cuadro `n` en `inicio + (n + 1)/30`, después de avanzar el reloj (`SnapshotRunner.VideoRoutine`; es la de la 4b). El cuadro del toque o del dato ya muestra 33 ms de su animación. Las tablas de §6 no cambian; cambian los números de cuadro de §9.4 (VID-02: la fila nueva al 0,331 en el cuadro 47 y no en el 48).

**El revelar por la tinta**, en todos los cuadros de VID-01, -02, -03, -04, -08 y -09: en el aire de 8·sp que queda debajo de cada vidrio (y debajo del panel de abajo, corrido por el FLIP) no hay ningún píxel de tinta. Lo más lejos del fondo que da la compresión es 20 por canal y parejo a lo ancho del panel (los bloques del H.264 junto a un borde que se mueve); la tinta de un número daría más de 100. Además, la comprobación del arnés (`InkCheck`: ninguna ranura con alfa > 0,01 debajo del vidrio) pasa en todos los cuadros.

| Video | Qué se verificó (valores del *manifest* y píxeles) | Resultado |
|---|---|---|
| VID-01 | **Abrir** (cuadros 9-29): el alto 37,4 · 44,8 · 52,9 · 60,5 · 67,3 · 72,7 · 77,0 · 80,4 · 83,0 · 84,9 · 86,3 · 87,3 base, el contenido 0 · 0 · 0,331 · 0,699 · 0,902 · 0,983 · 1, el rótulo 0,927 · 0,413 · 0 y el título 0 · 0 · 0,422 · 0,823 · 0,978 · 1, la flecha 3 · 17 · 51 · 117 · 159 · 175 · 180° en el sentido del reloj, el FLIP de `Series anteriores` de −55,5 a −0,1 dp con el aire de 8·sp en cada cuadro: la tabla de §6.1 (± 0,15 base; el arnés lo compara además con `SeriesTimelines`). La vuelta en curso aparece en el cuadro 12 (el vidrio pasa su tinta) y la línea `Rep` en los cuadros 17 (0,04), 18 (0,51), 19 (0,88) y 20 (1). `0:51.0` `204 m` y `Rep 2/3 · 204 m` en el cuadro 30, el de la banda. **Cerrar** (54-75): el contenido se va (0,978 · 0,823 · 0,422 · 0) antes de que el vidrio baje (90 hasta el cuadro 55, después 88,6 · 82,5 · 74,3 · 66,4 · 59,3 …), el rótulo vuelve en 80-200 y la flecha gira al revés | Bien. El ancho no cambia y nada más se mueve |
| VID-02 | La vuelta de los 325 (cuadro 45): la vuelta en curso pasa a `0:00.0` `0 m` con su rodillo en el cuadro de la banda; el empuje −21,0 · −15,6 · −4,6 · −0,6 · 0 y el alto 91,1 · 97,0 · 109,0 · 113,3 · 114 con la cola y el FLIP (−24,2 · −18,0 · −5,3 · −0,7 · 0 dp); `#1 2:05.0` entra 0 · 0 · 0,331 · 0,699 · 0,902 · 0,983 desde −8 base; la marca, con su fila, en 1 de los cuadros 51 a 62 y en 0,929 · 0,5 · 0,071 · 0 en los cuadros 68, 74, 80 y 86 (la tabla de §6.2); `0:01.0` `4 m` en el 75 | Bien. Entre `#1` y la línea `Rep` que baja quedan 11 base o más |
| VID-03 | Cuadro 45 (180): la barra pasa a `Descanso` y los paneles siguen con el trabajo un segundo (P-01). Cuadro 75 (181): lo viejo (la vuelta en curso y `#1`) 0,978 · 0,823 · 0,422 · 0; el vacío entra 0,226 · 0,646 · 0,872 · 0,973 · 0,999 subiendo 3,1 → 0; el alto 113,9 · 112,8 · 109,0 · 106,6 · 106,0 (§6.3); la línea `Rep` queda en su lugar (la cola, +8) y sube con el vidrio, y su rodillo pasa a `604 m`; `Series anteriores` sube 8,44 dp | Bien |
| VID-04 | Cuadros 15 y 45: la cuenta grande `1` y su salida, sin tocar los paneles (la comprobación del disco, en todos los cuadros). Cuadro 75 (201): parciales 105,8 · 103,7 · 96,1 · 91,1 · 90,0 y repeticiones 83,7 · 80,5 · 69,1 · 61,7 · 60,0, con la misma curva; `Rep 1 2:05 680 m` entra en 78-82 con su marca (D1-05) | Bien, salvo **el rodillo de la línea `Rep`** cuando cambia el largo del texto (cuadros 75-76 y 135-136): H5-D2-01 |
| VID-05 | El corte del simulador: los metros de la vuelta en curso quietos en `117 m` de los cuadros 60 a 179 mientras el tiempo sube de `0:31.0` a `0:34.0`, un segundo cada 30 cuadros con su rodillo; después `121 m`, `125 m` …; ninguna fila nueva | Bien (AC-BLE-15-09) |
| VID-06 | La entrada de `Cinco reps`: los paneles, abiertos y ya armados (`0:00.0` `0 m`, `Rep 1/5 · 0 m`, `Sin repeticiones completas`), entran con SPM en su tramo de la cascada (cuadros 18-26), con la misma opacidad y la misma subida; nada aparece después | Bien. `0:01.0` llega con su rodillo en el cuadro 33, pero con `0 m` y no `4 m`: H5-D2-02 (del arnés). El video dura 1,5 s (la spec dice 2,5): H5-D2-03 |
| VID-07 | La salida: la opacidad del HUD y la de parciales juntas, 1 · 0,995 · 0,961 · 0,872 · 0,704 · 0,418 · 0 (cuadros 11-17), con `0:52.0`, `Rep 2/3 · 208 m` y `Rep 1 2:05 680 m` en todos los cuadros | Bien. Nunca vacíos ni en cero |
| VID-08 | Reducir movimiento: el alto de 34 a 90 en el cuadro 9, con `Series anteriores` en su lugar nuevo en el mismo cuadro (sin FLIP); el contenido lineal 0,206 · 0,419 · 0,625 · 0,831 · 1; el rótulo y el título con sus fundidos; la flecha cambia de trazo (∨ se va en 9-10, ∧ entra en 11-13) sin girar. Cuadro 45: el alto 114, la lista y la línea en su lugar en el acto; `#1` funde 0,206 … 1 en 45-49 sin deslizarse; la vuelta en curso cambia con el fundido de 80; la marca, igual que en VID-02 | Bien |
| VID-09 | La cuenta grande `3` aparece en el cuadro 15 (el de la banda) y sigue en su lugar mientras parciales se abre a 8,4 dp: el vidrio hasta y 272,5 · 299,4 · 319,2 · 322,8 dp en los cuadros 26, 29, 35 y 48 (100, 200, 400 y 833 ms; las notas, con s = 1,054: 272,3 · 299,2 · 319,0 · 322,6); `2` en el 45 y `1` en el 75 | Bien. Ningún cuadro con los dos encimados |

**Lo que pidió D1-03** (un dato con el resorte de apertura corriendo: "verlo en un video con un toque menos de 0,6 s antes de una vuelta"): el catálogo `it5` no tiene esa escena y agregarla es de WP-B3. Queda cubierto por SMT (`ADataChangeDuringTheOpeningSpringRetargetsItFromWhereItIs`, con su mutación de control) y por la regla de §6.2; si QA quiere verlo, es un video más (VID-02 con parciales cerrado y un toque en `500 m` a los 324,6 s).

**DN5-06** (el piso de 0,6 y el `…` al final de la pieza que no entra): confirmado. `S-LARGO` queda en 0,631, sin `…`; con `ExtremeSplit` (SPV) la escala común queda en 0,6 y solo el split se corta, con el rótulo y los metros enteros: es la regla del HUD (lo que no entra se achica y, en su piso, se corta la pieza, nunca lo que la nombra). **DN5-07** (la capa de estado con reducir movimiento): el mismo fundido, como dicen §6.8 y §6.10.

### 15.5 Hallazgos para WP-B (no se tocó lógica)

| ID | Sev. | Dónde | Qué se ve | Propuesta |
|---|---|---|---|---|
| **H5-D2-01** | menor | `Presentation/Controls/TabularCells.cs` (WP-B1), en la línea `Rep r/R · m` (y en la vuelta en curso de 10 minutos o más) | **Cuando el texto cambia de largo, rueda la línea entera.** Las celdas se alinean a la derecha de toda la cadena (como un número de `TabularNumber`), pero la línea `Rep` está anclada a la izquierda y empieza con un rótulo. Con `Rep 2/3 · 8 m` → `Rep 2/3 · 12 m` (VID-04, cuadros 135-136) o `Rep 1/3 · 680 m` → `Rep 2/3 · 4 m` (cuadros 75-76), todas las celdas cambian de carácter y ruedan, también `Rep 2/3 · `, y la línea se corre de costado mientras el ancho se interpola. Pasa al empezar cada repetición y en los 10, 100 y 1.000 m de cada una. ANIMATION_BIBLE §2.4 pide que solo rueden las celdas que cambian | Que `TabularCells` arme su plan con el prefijo y el sufijo comunes: lo igual a la izquierda (`Rep 2/3 · `) y a la derecha (` m`) no rueda; el tramo del medio (las cifras) rueda alineado a su derecha, y lo que va después del tramo se corre con el ancho, como el sufijo de `TabularNumber`. Para lo anclado a la izquierda, el borde fijo es el izquierdo |
| **H5-D2-02** | menor | `Snapshots/SeriesCatalog.cs` (WP-B3), VID-06 | La vuelta en curso pasa a `0:01.0` con **`0 m`**, no con los `4 m` que piden §9.4 y la spec (§7.5). Es el reloj de prueba de la 4b (B5-09): VID-06 llega a la sesión con `TapRoutine` y no pone `ReadingZeroAtStart`, así `L(1)` cuenta como reinicio. El producto no cambia | Poner `s.ReadingZeroAtStart = true` antes de `TapRoutine("Cinco reps")` en VID-06 y volver a tomar `it5` dos veces |
| **H5-D2-03** | cosmético | `Snapshots/SeriesCatalog.cs` (WP-B3), VID-06 y VID-07 | Duran 1,5 s; la spec (§7.5) dice 2,5 s. Lo que tienen que mostrar entra en 1,5 s (la cascada hasta los 840 ms y el segundo 1; la salida de 200 ms) | Llevarlos a 2,5 s junto con H5-D2-02, o que el PO ajuste la spec |

### 15.6 Verificación

- **El arnés** (`capture-snapshots.ps1 -Set it5`): A a `<scratch>/it5/wp-d2pulido/capA` y B al repo, una detrás de otra: `47 tomas, 0 con problemas, 0 sondas de aspecto que fallan` en las dos; **A = B byte a byte** en las 47 imágenes y videos y en `INDEX.md` (en `manifest.json` cambia solo `durationMs`), y las 47 iguales a las de B3. `ProjectSettings` sin `ROWMATE_SIMULATOR` al terminar. Sin rutas de la PC en `manifest.json` ni en `INDEX.md`.
- **dotnet** (`tools/dotnet/run-tests.ps1`): compilación con 0 avisos y 0 errores; simulador 192 / 192; Core 2536: 2502 pasan, 0 fallan, 34 ignoradas (las de WP-B).
- **Unity**, con `SeriesCatalog.cs` cambiado (compila sin avisos nuevos: quedan los de antes, de `Infrastructure/*Sqlite*` y dos pruebas viejas): EditMode 2595 (2557 pasan, 0 fallan, 38 ignoradas); PlayMode 420 / 420; EditMode con `-Simulator` 2787 (2749 pasan, 0 fallan, 38 ignoradas); PlayMode con `-Simulator` 489 / 489. `ProjectSettings` sin `ROWMATE_SIMULATOR` al terminar.
- **Legacy, solo lectura**: no hizo falta abrirlo (lo visual del legacy ya estaba medido en §14); `git -C ../rowmate-legacy status --porcelain` vacío al terminar; no se leyó `lib/core/strava/strava_secrets.dart`.
- **Ninguna prueba ni toma tocó** el `prefs.json` ni la base del usuario (las tomas usan una `MemoryPreferences` por toma y una base temporal). El `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator` y no se tocó; WP-D2 no hace builds.
- **Repo público**: este documento y las biblias citan el repo con rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) o `../rowmate-legacy` y el scratch como `<scratch>`; ninguna ruta de la PC. Sin *commits*.

---

## Cambios al iniciar la It. 6a

QA-V5-R2-01 (DD5-27 en `ColumnScroll`), decidido por el Designer con la opción (a) por defecto: [06a-escena-lago/00-deuda-diseno §1](../06a-escena-lago/00-deuda-diseno.md), 2026-11-02. No cambia código, constantes, pruebas ni tomas: las notas pasan a decir lo que el código ya hace.

| Dónde | Qué cambió |
|---|---|
| §5.2 (el desplazamiento) | DD5-27 vale solo para `RowsScroll` (W1280). En `ColumnScroll` (TH, L640) el desplazamiento conserva su valor, como el legacy, y lo de abajo de la fila nueva baja con el empuje, sin saltos |
| §10 (DD5-27) | La fila dice "solo en `RowsScroll`" y nombra la prueba de la columna que ya existe |
