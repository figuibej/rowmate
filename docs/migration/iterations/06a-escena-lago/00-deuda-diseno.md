# It. 6a — Deuda de diseño de la 5 y base de arte de la escena del Lago alpino

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-02 |
| Iteración | 6a · `06a-escena-lago` ([ROADMAP §It. 6a](../../ROADMAP.md)). Primera iteración 3D: el fondo provisorio del inmersivo pasa a ser una escena URP. Se escribe al empezar, **antes** de la spec del PO: lo que toca producto va al PO en §15 con su número |
| Entrada | [REPORT de la 5](../05-series-parciales/REPORT.md) (§7 y §9); [05-qa-verification de la 5](../05-series-parciales/05-qa-verification.md) (ronda 2, QA-V5-R2-01); [04-design-notes de la 5](../05-series-parciales/04-design-notes.md) §5.2; [02-tech-design de la 5](../05-series-parciales/02-tech-design.md) (V5-04); [04-design-notes de la 4b](../04b-hud-exr/04-design-notes.md) §8.2; [ROADMAP](../../ROADMAP.md) (§0 regla 9, §It. 6a "Mejoras EXR", §6: D-ART-05, -06, -08 y P-09); [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) (Q-61 a Q-69, R-04, R-12); inventario [immersive-scene](../../product/inventory/immersive-scene.md) (F-SCN-01 a -23); [ART_BIBLE](../../design/ART_BIBLE.md) (§3.2, §5.2, §6 a §11); [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) (§0.3, §2.11, §5, §6, §7); [ASSET_SLOTS](../../design/ASSET_SLOTS.md); [LEGACY_VISUAL_AUDIT](../../design/LEGACY_VISUAL_AUDIT.md) §6.2 y §7; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §2, §7.4, §14 y §20 |
| Entrega | Este documento: QA-V5-R2-01 decidido (§1); la base de arte de la escena con números (§2 a §11); la lista de Mixamo para la It. 7 (§12, P-09); D-ART-08 (§13); los pedidos al Architect **DD6a-01 a DD6a-26** (§14) y lo que va al PO y a QA (§15). Además se alinearon, por QA-V5-R2-01, [04-design-notes de la 5](../05-series-parciales/04-design-notes.md) (§5.2, §10 y "Cambios al iniciar la It. 6a") y [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §2.10. **No cambia código, tokens, capturas ni el resto de las biblias**: se alinean después del gate (§16) |
| Legacy leído (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/scene/` entero (`scene_camera.dart`, `time_of_day.dart`, `scene_state.dart`, `scene_settings.dart`, `scene_view.dart`, `environment.dart`, `environments/lake_environment.dart`, `painters/sky_painter.dart`, `painters/water_painter.dart`, `painters/shore_painter.dart`), `test/scene/time_of_day_test.dart` y `sky_shore_painter_test.dart`, `docs/superpowers/specs/2026-10-02-immersive-scene-2-5d-design.md`, `docs/image-03.png` y la búsqueda del desplazamiento en `immersive_workout_screen.dart` (`:632`). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificado | Programas propios, sin red, en `<scratch>/it6a/designer-debt/` (Python 3.14, sin dependencias):<br>- `dartrandom.py`: el generador de la VM de Dart; da los cuatro vectores dorados del oráculo (`Random(42)` → `0.15092545597797424`, `0.6041479725361217`, `nextInt(1000) = 77`; semilla del lago con k = 7 → `0.854835531354132`);<br>- `lake.py`, `cabins.py`, `cabins2.py`, `lodcount.py`: el paisaje del lago del legacy, sus cantidades en 100 y 200 km, los choques de las cabañas con los pinos y los pinos por banda de distancia;<br>- `hours.py`: paleta, astros, luz y estrellas por hora (§4);<br>- `horizon.py`: las siluetas por acimut, la nieve y el acercamiento (§5);<br>- `proj.py`: la cámara del legacy en los cinco perfiles (§3);<br>- `pills.py`, `pills2.py`: el contraste de las píldoras con el método de `Contrast.cs` (§9.3);<br>- `moon.py`: la fase lunar contra fases reales (§4.3).<br>Licencias de D-ART-08 leídas en sus páginas (§13). **Unity no se abrió** y dotnet no se corrió |

Convenciones: las de la 5 (hex `#RRGGBB`, transparencia `@NN %`, 1 px de USS = 1 dp). Perfiles: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720; `s` = `hudScale`. Mundo como el legacy: `x` lateral (+ derecha), `y` altura sobre el agua, `z` hacia adelante desde la cámara (m). **Elevación** = ángulo sobre el horizonte; **acimut** = ángulo respecto del sentido de avance, positivo a la derecha. "Distancia de escena" `d` = la de `SceneState` (F-SCN-03), no la del monitor. Prioridades de los pedidos: **P0** (sin esto la 6a no cumple), **P1** (lo que pide la Mejora EXR del ROADMAP), **P2** (deseable; puede pasar a la 6b).

---

## 0. Resumen

1. **QA-V5-R2-01** (§1): opción **(a)**. DD5-27 vale solo para `RowsScroll` (W1280). En la columna desplazable de TH y L640 el desplazamiento conserva su valor, como el `SingleChildScrollView` del legacy, y lo de abajo baja con el empuje, sin saltos. No cambian código, pruebas ni tomas. Las notas de la 5 y ANIMATION_BIBLE §2.10 ya lo dicen.
2. **Cámara** (§3): la del legacy, exacta. 2,2 m de alto, el remero a 9,5 m, el horizonte al 40 % o al 42 %, focal = lado corto, nivelada con `lensShift.y` −0,10 / −0,08. La **respiración** es la de ANIMATION_BIBLE §5.2 sin los términos del bote, que llegan en la 7, con el rolido en 0,15° (no 0,18°): así el horizonte no se aparta más de 3 dp en ninguno de los cinco perfiles (W1280 daba 3,27). La **entrada** con el vuelo de cámara (220-1820 ms) y la **salida** en los 640 ms de la 4b.
3. **Cielo por hora** (§4): la paleta del legacy tal cual, interpolada en sRGB. El degradé del cielo sigue la elevación y llega a `skyTop` a los 24°. Hay una banda de bruma en el horizonte que cose el cielo con el agua.
   - **Sol**: máximo **12°** sobre el horizonte, no 17°. Con 17° el sol del mediodía queda detrás de la barra de pasos en TH, L640 y W1280; con 12° se ve debajo de ella en los cinco perfiles.
   - **Luna**: sale y se pone detrás del paisaje (Q-63). Esto corrige la biblia, que la hacía aparecer de golpe a 3,7°. En el cambio de las 19:34 y las 4:26 los dos astros están ocultos.
   - **Estrellas** (Q-62): con ambiente ≤ 0,85, con un fundido de 0,85 a 0,80. Se apagan entre las 5:48 y las 6:12 y se encienden entre las 18:00 y las 18:17.
   - **Nubes** (D-ART-06): las 7 del legacy, que derivan con el viento y no con la distancia.
   - **Luz**: se apaga en la puesta y en el cambio de astro vale 0, así el cambio de dirección no se ve. La luna tiene fase real.
   - **Tonemapping**: *Neutral* en lugar de ACES, para no cambiar el matiz de la paleta.
4. **Horizonte** (§5, Q-65): las dos cordilleras del legacy, con su misma fórmula, pasan a ser un perfil fijo **por acimut** (70° = un ancho de pantalla del legacy).
   - **Se acercan** al 4 % de la velocidad del bote, con un tope: al final de una maratón la lejana mide ×1,30 y la cercana ×1,89. No se corren de costado.
   - La nieve cubre todo lo que pasa el 55 % del alto.
   - El sol se pone en el collado que el perfil del legacy ya tiene a la derecha (acimut +12° a +22°).
5. **Lago alpino** (§6, Q-61): pinos, muelles y cabañas en **las mismas posiciones, escalas y variantes** que el legacy, con su generador.
   - A distancia 0 se ven 92 objetos, y como mucho 102 en 100 km: el tope de 120 nunca actúa.
   - **El terreno es nuevo**: playa de canto rodado y talud de 9 a 16 m. Detrás, una **ladera boscosa a la izquierda** (por donde sale el sol) y una **pradera abierta a la derecha** (la de las cabañas y la puesta).
   - **Nada flota**: cada pie se hunde en el terreno. La cabaña elige hacia dónde crece para no tragarse un pino: en 333 cabañas (200 km), ninguna se corre más de 0,5 m. El muelle llega a la playa.
   - Lo que se suma (juncos, rocas, bosque, matas) usa semillas propias y nunca toca la secuencia del legacy.
6. **Agua degradada** (§7, R-04): la de la 6a es digna, no un parche.
   - Color de la paleta por distancia, reflejo del cielo por Fresnel y un camino de brillo del astro con destellos que pasan con el agua.
   - Franja somera en la orilla y niebla del legacy.
   - Cuesta ≤ 24 ALU y 0 texturas; sale el "≤ 12 ALU" de la biblia, que no alcanzaba para esto.
7. **Bote provisorio** (§8): un single procedural con las medidas reales (D-ART-04), los remos en reposo con las palas planas sobre el agua y la sombra ovalada del legacy. Sin remero (It. 7).
8. **El HUD sobre la escena** (§9):
   - **Velo de pausa**: un *Volume* de URP que desatura un 30 % y oscurece un 15 % en pantalla, en 300 ms, y vuelve en 240.
   - **El vidrio** sigue en `@86 %` hasta la 6b.
   - **Los degradés** de arriba y de abajo se dibujan en la escena: 0 elementos de UI.
   - **Fondo de carga**: si la escena no llegó, se ve el fondo provisorio con los colores de la hora.
   - **Pedido al PO**: las píldoras del HUD con su variante `.hud`. En horizontal y en escritorio, las nubes pasan detrás de la barra, y la píldora hecha de enfriamiento da 2,850 (la de trabajo, 2,995 en el renderizado).
9. **Rendimiento** (§10): el reparto para 60 fps en un Galaxy A54 / Pixel 6a en el nivel Medio:
   - ≈ 60 *draw calls* de escena, ≈ 71 con la UI (tope 150), y ≤ 160 k triángulos (tope 350 k);
   - GPU estimada ≈ 10,8 ms (tope 12);
   - 0 B por cuadro.
   Sin el teléfono queda NO VERIFICABLE (regla 9).
10. **Mixamo para la 7** (§12, P-09): **Y Bot**, obligatorio, como FBX for Unity en T-pose. Opcionales: **X Bot**, un personaje realista que pase la lista de control y el clip **Sitting Idle** sin piel, a 30 fps, solo como referencia. **Ninguna animación hace falta para remar**: la remada es IK procedural.
11. **D-ART-08** (§13): por defecto, **no**. Las tres fuentes tienen licencias que sirven (ambientCG y Poly Haven son CC0; el CGI Moon Kit de la NASA pide crédito y no implica respaldo). Recomiendo volver a ofrecerlo en el gate de la It. 8, para las fachadas del río.

---

## 1. QA-V5-R2-01: DD5-27 en `ColumnScroll`

### 1.1 Lo que pasa hoy

- 04-design-notes §5.2 de la 5 pedía, para todo desplazamiento: "con la vista desplazada, la fila nueva entra fuera de la vista y el desplazamiento suma 22·sp·k, así lo que se ve no se mueve" (DD5-27).
- El código lo hace en `RowsScroll` (W1280): ahí la fila nueva entra **arriba de la ventana de filas** de su panel, fuera de la vista.
- En `ColumnScroll` (TH, L640) no lo hace. La fila nueva entra **debajo de la cabecera y de la vuelta en curso de parciales**, que se desplazan con la columna, así que sumar 22·sp las subiría y dejaría quieto solo lo de abajo (02-tech-design V5-04).
- Hoy, con la columna desplazada, una vuelta nueva baja lo que está debajo con el empuje (0-160, `ease.move`), sin saltos: AC-WKT-18-13 se cumple.

### 1.2 Las dos opciones

| | (a) DD5-27 solo en `RowsScroll` | (b) Compensar en la columna solo si la fila entra arriba del techo |
|---|---|---|
| Qué se ve en TH con la columna desplazada | Lo de abajo de la fila nueva baja 22·sp con el empuje, el mismo movimiento que con el desplazamiento en 0 | Quieto si el lugar de la fila ya está arriba del techo; si no, el empuje |
| Cuántas veces | Como mucho 4 por paso de trabajo: después de la cuarta vuelta el panel no crece (la más vieja sale por abajo) | Igual, pero con dos comportamientos para el mismo dato según 1 dp de desplazamiento |
| Legacy | Igual: `SingleChildScrollView` (`immersive_workout_screen.dart:632`) conserva el desplazamiento en píxeles, y lo de abajo baja (en el legacy, de golpe) | Distinto |
| Código, pruebas, tomas | Sin cambios | Una rama nueva en `SeriesColumn` con su prueba en SCT |

### 1.3 La decisión: (a)

- **Lo que se ve no salta**, que es el criterio de producto: el empuje es la curva de siempre.
- **Es el comportamiento del legacy**, con animación.
- En la columna, "lo que se ve" incluye la cabecera y la vuelta en curso. Ningún número compensado los deja quietos a todos.
- (b) agrega un umbral invisible: el mismo dato movería o no la columna según dónde la dejó el dedo.

Las notas quedaron alineadas: [04-design-notes de la 5](../05-series-parciales/04-design-notes.md) §5.2, la fila DD5-27 de §10 y "Cambios al iniciar la It. 6a"; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §2.10 y su "Cambios al iniciar la It. 6a". Para QA, nada nuevo: `SCT.TheColumnScrollFollowsAColumnThatShrinks` y la prueba de W1280 siguen siendo las que valen.

---

## 2. Lo que manda y lo que cambia de la base

### 2.1 Contrato y mejora

| Pieza | Contrato del legacy (no cambia) | Mejora EXR de esta base |
|---|---|---|
| Cámara | 2,2 m; remero a 9,5 m; horizonte 40 % / 42 %; focal = lado corto; nivelada; recorte 0,5 m (F-SCN-02, `scene_camera_test`) | Respiración sutil (§3.2); vuelo de entrada (§3.3) |
| Velocidad y distancia | `500/split`, τ 1,5 s, corte bajo 0,01 m/s, `dt` ≤ 0,1 s (F-SCN-03) | — |
| Hora y paleta | 6 keyframes y 8 campos, interpolación lineal, noche cerrada 22-4 h, noche si `sin((h−6)/12·π) ≤ −0,4`, luces con ambiente < 0,6, hora forzada del simulador (F-SCN-09, -23) | Transiciones de hora graduales (§4.7) |
| Astros | Acimut por `xFrac` del legacy, luna con `h + 12`, fuerza del brillo (día `clamp(1,2e; 0,2; 1)`, noche 0,45) (F-SCN-10) | Salida y puesta graduales (Q-63), luna con fase, sol "de pantalla" y "de luz" (D-ART-05) |
| Cielo | 80 estrellas con `Random(42)` y su titileo; 7 nubes con sus datos; opacidades por ambiente (F-SCN-11) | Estrellas solo con ambiente ≤ 0,85 (Q-62); nubes sin deriva por distancia (D-ART-06); degradé, bruma y discos en 3D |
| Siluetas | Perfil de dos cordones, sus tonos, su color `lerp(tono; fog; 0,35 + 0,4·prof.)`, nieve (F-SCN-19) | Perfil fijo por acimut que se acerca (Q-65), relieve y nieve por altura |
| Orilla | Generador `Random(id·1000003 + k·7919 + 17)`, segmentos de 50 m, de 1 a 400 m, tope 120, tinte de niebla 80-400 m al 80 % (F-SCN-14, -15, -20; Q-61) | Terreno, nada flota, relleno con semillas propias |
| Agua degradada | Disponible siempre; la app sigue usable (R-04, F-SCN-13) | Agua digna por hora (§7) |

### 2.2 Lo que esta base cambia de ART_BIBLE y ANIMATION_BIBLE (se alinea después del gate, §16)

| ID | Dónde | Antes | Ahora | Por qué |
|---|---|---|---|---|
| R6a-01 | ART §6.2 | Sol "de pantalla" hasta 17° (`1,5° + 15,5°·e`) | Hasta **12°** (`1,5° + 10,5°·e`) | Con 17° el sol del mediodía queda detrás de la barra de pasos en los tres perfiles horizontales (§3.4) |
| R6a-02 | ART §6.2 | La luna "con las mismas fórmulas": a las 19:34 aparecía a 3,7° | La luna sale desde −2,5° hasta su recorrido en 19:34-20:36 y se pone en 3:24-4:26 | Q-63: el astro entra y sale detrás del paisaje |
| R6a-03 | ART §6.2 | Intensidad mínima 0,05 en el cambio | La luz del sol se apaga con la puesta y la de la luna sube con la salida: en el cambio vale 0 | Una luz a 6° de un sol ya puesto daba sombras de un astro que no está |
| R6a-04 | ART §6.3 | Tonemapping ACES | **Neutral** | ACES corre el matiz de los azules y naranjas de la paleta del contrato |
| R6a-05 | ANIM §5.2 | Rolido 0,18° | **0,15°** | En W1280 el horizonte se apartaba 3,27 dp en las esquinas |
| R6a-06 | ANIM §2.11 y §5.3 | Salida de 1,2 s con la cámara | La salida de la 4b (640 ms) con un retroceso corto de la cámara | AC-WKT-13-08 y -09 de la 4b fijan los 640 |
| R6a-07 | ART §7.2 | "Bosque denso en ambas orillas" y "ladera hasta 25 m a x = 60" | Ladera boscosa a la izquierda con la copa bajo `2,2 + 0,25·|x|`; pradera a la derecha bajo `2,2 + 0,06·x` | Con la ladera de la biblia, el bosque tapaba el único pico nevado y el sol de la tarde (§6.2) |
| R6a-08 | ART §7.1 | Nieve en picos > 75 % | Nieve sobre el 55 % del alto de la cordillera lejana | Con el perfil fijo, la regla del legacy deja un solo pico nevado en cuadro |
| R6a-09 | ART §7.7 | Agua degradada ≤ 12 ALU, "port directo" | ≤ **24** ALU, 0 texturas, con la receta de §7 | El port directo pinta líneas en coordenadas de pantalla que no acompañan al agua |
| R6a-10 | ART §6.2 | (no estaba) | Las nubes de 10,5° a 18° de elevación y 8,4° a 15,3° de ancho | Entre las tarjetas de arriba y las montañas en vertical |

---

## 3. Cámara y composición del cuadro

### 3.1 La pose del contrato, por perfil (`proj.py`)

| Perfil | Horizonte (dp) | Focal | FOV vertical × horizontal | `lensShift.y` | Borde de arriba / de abajo | La orilla (|x| = 9) sale del cuadro a | Borde inferior = agua a |
|---|---|---|---|---|---|---|---|
| TV | 365,6 | 411 | 96,1° × 53,1° | −0,10 | 41,7° / −53,2° | z 18,0 m | z 1,65 m |
| P360 | 320,0 | 360 | 96,0° × 53,1° | −0,10 | 41,6° / −53,1° | z 18,0 m | z 1,65 m |
| TH | 172,6 | 411 | 53,1° × 96,1° | −0,08 | 22,8° / −30,1° | z 8,1 m | z 3,79 m |
| L640 | 151,2 | 360 | 53,1° × 83,3° | −0,08 | 22,8° / −30,1° | z 10,1 m | z 3,79 m |
| W1280 | 302,4 | 720 | 53,1° × 83,3° | −0,08 | 22,8° / −30,1° | z 10,1 m | z 3,79 m |

- Cámara física nivelada, FOV vertical = `2·atan((alto/2)/lado corto)`.
- El signo de `lensShift.y` es negativo: el encuadre baja y el horizonte sube en pantalla. Lo valida `scene_camera_test` portado: la proyección de Unity tiene que dar los píxeles de `SceneCamera.Project` con un error ≤ 0,5 dp en los cinco perfiles.

### 3.2 Respiración (la 6a no tiene bote que empuje)

Sumada a la pose del contrato, determinista, con el tiempo de escena:

| Componente | Fórmula |
|---|---|
| Lateral x | `0,035·sin(2π·0,071t) + 0,015·sin(2π·0,173t + 1,7)` m |
| Vertical y | `0,025·sin(2π·0,093t + 0,4)` m |
| Rolido | **`0,15°`**`·sin(2π·0,061t + 2,1)` |
| Cabeceo | `0,10°·sin(2π·0,083t + 0,9)` |
| Multiplicador | lago ×1,0 · pausa ×0,6 (con el velo: 300 ms `ease.move` al pausar, 240 al reanudar) · reducir movimiento ×0 |

- Los términos del bote (25 % de la arfada, 30 % del *surge* y 15 % del cabeceo) llegan con el bote de la 7.
- **Horizonte**: lo mueven solo el cabeceo (`f·tan 0,10°`) y el rolido (`(ancho/2)·tan 0,15°`). En el peor caso, 1,26 dp en TV, 1,92 en TH y **2,94 en W1280**. Con 0,18° daba 3,27 en W1280.
- **La zona del bote**: el remero queda adentro en todo momento. Los 5 cm laterales (como máximo) mueven el bote 2,2 dp a 9,5 m en TV.

### 3.3 Entrada y salida con la escena

La cascada del HUD no cambia (barra a 400, SPM 520, Split y Watts 600, banda 680, kcal y BPM 760, controles 840, cada pieza en 280).

- **Entrada**: Workout funde a negro en 0-220 como en la 4b. A los 220 empieza el vuelo y la escena sale del negro en 220-400 (`ease.enter`, como el fondo de la 4b). El vuelo dura **1,6 s (220-1820)**:
  - posición: Bézier cúbica `P0 = (3; 9; −14)`, `P1 = (2,4; 7,2; −8,5)`, `P2 = (0,6; 3,4; −2,5)`, `P3 = (0; 2,2; 0)` (m, respecto de la pose del contrato), con `ease.move`;
  - rotación: `slerp` desde mirar al bote (z 9,5, y 0,4) hasta quedar nivelada;
  - FOV de 50° al del contrato;
  - `lensShift` de 0 al del contrato entre 0,6 y 1 del recorrido.

  El motor y el reloj corren desde el toque.
- **Si la escena no llegó a los 220**: queda el fondo de carga (§9.4) y la escena entra cuando esté, con un fundido de 240. Si llega después de los 400, entra en la pose del contrato, sin vuelo.
- **Salida**: los 640 ms de la 4b.
  - El HUD se va en 0-200.
  - La cámara **se retira 0,4 m hacia arriba y 1,2 m hacia atrás en 0-400** (`ease.exit`).
  - La escena funde a negro en 200-400 y la página negra se va en 400-640.
  - A los 640 todo se detiene (AC-WKT-13-09). Sale la salida de 1,2 s de ANIMATION_BIBLE §5.3.
- **Reducir movimiento**: la escena en la pose del contrato desde el cuadro 0, con el fundido de 120 de la 4b; sin vuelo, sin retroceso y sin respiración.
- **La toma aérea del completado** (ANIMATION_BIBLE §5.3) queda para la 6b. Mientras el diálogo sea opaco y con el velo al 60 %, no se ve.

### 3.4 Qué se ve dónde (dp de ventana, a distancia 0)

| Perfil | Barra de pasos (52·s / 74·s con el sello) | Sol de mediodía, 12° (y, radio) | Con 17°, como la biblia | Nubes (10,5°-18°) | Cumbres (9°) | Zona del bote |
|---|---|---|---|---|---|---|
| TV | 0-54,8 / 78,0 | 278,2 · 11,5 | 239,9 | 232-289 | 300,5 | 115-296 × 324-598 |
| P360 | 0-48,0 / 68,3 | 243,5 · 10,1 | 209,9 | 203-253 | 263,0 | 101-259 × 283-523 |
| TH | 0-54,8 / 78,0 | **85,3** · 11,5 | 47,0 (detrás de la barra) | 39-96 | 107,5 | 256-658 × 206-329 |
| L640 | 0-48,0 / 68,3 | **74,7** · 10,1 | 41,1 (detrás) | 34-85 | 94,2 | 179-461 × 181-289 |
| W1280 | 0-83,2 / 118,4 | **149,4** · 20,1 | 82,3 (detrás) | 69-169 | 188,4 | 358-922 × 361-577 |

- **Con 12°, el disco del mediodía queda debajo de la barra en los cinco perfiles.**
  - En TH toca 4 dp de la barra solo mientras se ve el sello.
  - En TV queda en la columna del centro, donde va el disco de la cuenta grande y de `PAUSA` (234-310): cuando el disco aparece, lo tapa. No es un problema de contraste: el disco es vidrio `@86 %`, calculado sobre blanco.
- **Las nubes** pasan detrás de la barra en TH, L640 y W1280. En vertical van entre las tarjetas y las montañas.
- **El bote provisorio** (§8): en TV, la popa en y 528 y la proa en 430; las puntas de los remos en x 84 y 327. Los remos salen de la zona del bote a los costados, como en el legacy.

### 3.5 Los degradés de pantalla (ART_BIBLE §3.2)

- **Arriba**: `#06122A` del 38 % a 0 en el 24 % alto de la pantalla.
- **Abajo**: del 32 % a 0 en el 18 % bajo.
- Los dos **se dibujan en la escena** (un *quad* de la cámara después de los transparentes y antes del post), no en UI Toolkit: 0 elementos del HUD. El velo de pausa los alcanza.
- No tocan el bote: el de abajo empieza en 0,82 del alto, y la zona del bote termina en 0,654 en vertical y en 0,802 en horizontal.
- No tocan las cumbres: el de arriba termina en 0,24 del alto, y las cumbres quedan en 0,33 (TV) y 0,26 (TH).

---

## 4. Cielo, astros y luz por hora

### 4.1 La paleta (contrato) y su interpolación

- `SceneTimeOfDay` porta `time_of_day.dart` tal cual: los 6 keyframes, el cierre `22 → 28 (noche) → 29 (amanecer)` y la interpolación **en sRGB** (`Color.lerp` del legacy), no en lineal. El shader recibe el color ya pasado a lineal.
- **Los umbrales de noche**, como constantes exactas:
  - `hNocheIni = 6 + 12·(1 + asin 0,4/π) = 19,5719 h` (19:34:18);
  - `hNocheFin = 6 − 12·asin 0,4/π = 4,4281 h` (4:25:41).
- **La hora** es continua, con segundos: `h + m/60 + s/3600`. El legacy la tomaba de a minuto; un minuto mueve el sol 0,05°.
- **Valores para QA** (`hours.py`; la intensidad de la luz con luna llena):

| Hora | Ambiente | Estrellas (legacy → Q-62) | Astro: acimut, elevación | Luz | `skyTop` | `skyHorizon` | `waterNear` | `waterFar` | `fog` | `sunColor` |
|---|---|---|---|---|---|---|---|---|---|---|
| 0:00 | 0,350 | 1,00 → 1,00 | luna 0,0°, 12,0° | 0,160 | `#050A1E` | `#121C3A` | `#06122A` | `#0E1E3C` | `#0F1730` | `#E6EEFF` |
| 4:00 | 0,350 | 1,00 → 1,00 | luna +11,7°, +1,0° (se pone) | 0,060 | ídem | ídem | ídem | ídem | ídem | ídem |
| 5:00 | 0,700 | 0,46 → 0,46 | sol −20,4°, −0,4° (sale) | 0,295 | `#2A1B5E` | `#FF9A5C` | `#1A2B4A` | `#3A2F55` | `#C98A7A` | `#FFB070` |
| **6:00** | 0,825 | 0,27 → **0,14** | sol −17,5°, +1,5° | 0,900 | `#324B9A` | `#FFBA7E` | `#1C4274` | `#4B5F8F` | `#D2B9B8` | `#FFCA88` |
| 7:00 | 0,950 | 0,08 → **0** | sol −14,6°, +4,2° | 1,250 | `#3A7BD5` | `#FFD9A0` | `#1E5A9E` | `#5C8FC9` | `#DCE8F5` | `#FFE4A0` |
| **12:00** | 1,000 | 0 → 0 | sol 0,0°, +12,0° | 1,750 | `#0B4FA8` | `#8FD3FF` | `#0E4F8F` | `#2F8FD6` | `#CFE9FF` | `#FFF6D5` |
| 15:00 | 0,925 | 0,12 → **0** | sol +8,8°, +8,9° | 1,300 | `#1C469A` | `#C7B0AC` | `#12447A` | `#5676A8` | `#D8C5C4` | `#FFD4A0` |
| **18:00** | 0,850 | 0,23 → **0** | sol +17,5°, +1,5° | 0,850 | `#2E3C8C` | `#FF8C5A` | `#163A66` | `#7C5C7A` | `#E0A28A` | `#FFB36B` |
| 19:00 | 0,675 | 0,50 → 0,50 | sol +20,4°, −0,4° (se pone) | 0,455 | `#1E2B6B` | `#A4636A` | `#102C50` | `#534368` | `#886A72` | `#EECCB5` |
| 19:34 | 0,575 | 0,66 → 0,66 | cambio: los dos en −2,5° | **0** | `#142158` | `#714B73` | `#0D2443` | `#3B355D` | `#554B64` | `#E4DBDF` |
| 20:00 | 0,500 | 0,77 → 0,77 | luna −11,7°, +1,0° (sale) | 0,060 | `#0D1A4A` | `#4A3A7A` | `#0B1E3A` | `#2A2A55` | `#30335A` | `#DDE6FF` |
| **22:00** | 0,350 | 1,00 → 1,00 | luna −5,8°, +10,6° | 0,160 | `#050A1E` | `#121C3A` | `#06122A` | `#0E1E3C` | `#0F1730` | `#E6EEFF` |

En negrita, las horas de la demo. Las luces nocturnas se encienden con ambiente < 0,6: de 19:26 a 4:43.

### 4.2 El degradé del cielo y la bruma del horizonte

- **Cielo** a la elevación θ: `lerp(skyHorizon; skyTop; t)` con `t = 1 − (1 − min(θ; 24°)/24°)^2,2`.
  - A 6° da 0,47; a 12°, 0,78; desde 24°, `skyTop` pleno.
  - En TH el borde de arriba (22,8°) queda en 0,999, como el legacy, que llegaba a `skyTop` en el borde.
  - En TV la mitad de arriba del cielo es `skyTop` plano: el fondo más oscuro va detrás de las tarjetas.
- **La banda de bruma**: `bruma = lerp(skyHorizon; fog; 0,5)`.
  - Cielo de 0° a 1,5°: `lerp(bruma; cielo; smoothstep(0°; 1,5°; θ))`.
  - Agua de 600 a 1500 m: `lerp(agua; bruma; smoothstep(600; 1500; z))`.
  - Así el cielo y el agua llegan al mismo color en la línea del horizonte. En el legacy hay costura: `fog` contra `skyHorizon`.
- **El resplandor de la hora dorada**, en el cielo: `+ sunGlow·0,35·g·exp(−Δ/14°)·(1 − smoothstep(0°; 20°; θ))`.
  - Δ es la distancia angular al sol, aunque esté detrás del paisaje.
  - `g = (1 − smoothstep(1,5°; 9°; elevSol))·visSol`: vale 1 a las 6 y a las 18 y 0 desde las ≈ 9 y hasta las ≈ 15.
  - De noche, 0.

### 4.3 Sol y luna (D-ART-05, Q-63)

Con `e(h) = clamp(sin((h − 6)/12·π); 0; 1)`, `xFrac(h) = clamp(0,25 + 0,5·(h − 6)/12; 0,15; 0,85)` y `h' = (h + 12) mod 24` del legacy, y `S(a; b; h)` = `smoothstep`:

| Magnitud | Sol (de día) | Luna (de noche) |
|---|---|---|
| Acimut (los dos soles) | `(xFrac(h) − 0,5)·70°` | `(xFrac(h') − 0,5)·70°` |
| Recorrido de pantalla | `1,5° + 10,5°·e(h)` | `1,5° + 10,5°·e(h')` |
| Visibilidad `vis` | `S(4,45; 5,50; h)·(1 − S(18,50; 19,55; h))` | `S(19,5719; 20,6; h)` a la tarde; `S(4,4281; 3,40; h)` a la mañana |
| Elevación de pantalla | `−2,5° + (recorrido + 2,5°)·vis` | ídem |
| Elevación de la luz | `6° + 49°·e(h)` | `10° + 40°·e(h')` |
| Intensidad de la luz | `K(h)·vis`, con `K` = 0,55 (≤ 5 h), 1,25 (7 h), 1,75 (12 h) y 0,85 (≥ 18 h), lineal entre ellos | `(0,04 + 0,12·iluminación)·vis` |
| Color de la luz | `sunColor` de la paleta | ídem (la paleta nocturna ya es la de la luna) |

- **El cambio** (19:34:18 y 4:25:41): el sol se ocultó 1,4 min antes y la luna sale 0 a partir de ahí, y al revés a la mañana. La luz vale 0 cuando cambia de dirección.
- **El ritmo**: el astro nunca se mueve más de **0,25°/min** (lo más rápido, la luna que se pone, a las 3:51). Pasa la prueba de "≤ 0,5° por minuto" de ART_BIBLE §11.
- **Detrás del paisaje**: el cielo (con el disco y el halo) se dibuja primero y las cordilleras, el terreno y los objetos lo tapan.
  - A las 18 h el sol está en el collado de la derecha (acimut +17,5°, cordilleras de 0° a 1,6°, §5): se ve.
  - A las 6 h está detrás del macizo nevado de la izquierda (6,2°): se ve el resplandor y el disco aparece hacia las 7:20.
  - Los pinos de la orilla pasan por delante: el sol entre los árboles.
- **Disco del sol**: radio **1,6°**, borde suave de 0,12°.
  - Color `sunColor × (3 + 5·e)·vis` en HDR: el *bloom* de §4.6 lo agranda.
  - En dp: 11,5 (TV y TH), 10,1 (P360 y L640) y 20,1 (W1280). El legacy medía 22 dp, pero no tenía *bloom* y era plano.
- **Halo del sol**: `sunGlow` radial hasta 6,4° con los alfas del legacy (0,7 en el centro, 0,25 a la mitad, 0 en el borde), multiplicados por `vis`.
- **Disco de la luna**: radio **1,28°** (0,8× del sol, como el legacy), `sunColor × 1,6·vis`.
  - **Fase real**: con la normal del disco `n = (u; v; √(1 − u² − v²))` y la luz `L = (sin φ; 0; −cos φ)`, `φ = 2π·edad/29,530588853`, lo iluminado es `smoothstep(−0,06; 0,06; n·L)`.
  - Lo oscuro, con luz de Tierra: `0,06·sunColor` sobre el cielo.
  - Textura del slot `sky.moon`.
  - Halo: `sunGlow` hasta 5,1° con los alfas del legacy × `(0,4 + 0,6·iluminación)·vis`.
- **La fase**:
  - Luna nueva de referencia 2000-01-06 18:14 UTC; `edad = (tUTC − ref) mod 29,530588853 d`; `iluminación = (1 − cos φ)/2`; fecha del reloj del equipo (con hora forzada, la fecha de hoy).
  - Vectores (`moon.py`): llena real del 2024-01-25 17:54 → edad 14,401 d e iluminación 0,9985; nueva real del 2024-04-08 18:21 → 29,359 d y 0,0003; llena real del 2025-01-13 22:27 → 14,224 d y 0,9967. El modelo se aparta de la fase real ≤ 0,6 d en los tres; tolerancia de prueba, ± 1 d.
- **Hemisferio** (P2): el lado iluminado se espeja (`u → −u`) si el país de la región del sistema está en el hemisferio sur. La lista va en Core: AR, AU, BO, BR, CL, LS, MG, MZ, NA, NZ, PE, PY, SZ, UY, ZA, ZW. Sin región, el del norte.

### 4.4 Estrellas (Q-62)

- **Las 80 del legacy**: `DartRandom(42)` con las tres tiradas por estrella en el orden del legacy (x, y, radio; `sky_painter.dart:59-63`). Se mapean a direcciones del mundo:
  - acimut `(r₁ − 0,5)·100°`;
  - elevación `4° + 44°·(1 − r₂)`;
  - radio `0,6 + r₃` dp;
  - con la respiración se comportan como puntos en el infinito.
- **Brillo**: alfa `(0,4 + 0,5·(0,5 + 0,5·sin(2t + i)))·visEstrellas`, con `visEstrellas = clamp((1 − a)/0,65)·S(0,85; 0,80; a)`. Es la oscuridad del legacy, cortada por Q-62 con un fundido.
  - Valores: 0 con ambiente 0,85, 0,179 con 0,82 y 0,308 con 0,80, que ya es lo del legacy.
  - Se apagan de 5:48 a 6:12 y se encienden de 18:00 a 18:17.
  - Las nubes, la luna y el paisaje las tapan; cerca del horizonte se apagan con `S(2°; 8°; elevación)`.
- **Las pruebas del legacy siguen pasando**: a las 19 h se ven las 80 (`sky_shore_painter_test`) y a las 12 h ninguna.
- **El fondo de estrellas** (Medio y Alto; P1): hash por dirección en el shader del cielo, 1 cada 9 grados cuadrados, de 0,5 a 0,9 dp, alfa de 0,12 a 0,30 × `visEstrellas`, titileo con fase propia.

### 4.5 Nubes (D-ART-06)

- **Las 7 del legacy** (`_cloudData`: `fx`, `fy`, tamaño `s`, velocidad `v`):
  - acimut `envolver(−60°; +60°; (fx − 0,5)·70° + 0,28°·v·t)`: la deriva del legacy en grados, solo con el tiempo de escena, nunca con la distancia;
  - elevación `18° − 44°·(fy − 0,05)`: de 18° a 10,5°;
  - ancho `2,6·s/390` rad: de 8,4° a 15,3°; alto, la mitad.
  - Se envuelven a ±60°, fuera de los ±48° de TH más su medio ancho.
  - Siguen en pausa: el tiempo de escena corre.
- **Forma**: 4 cúmulos procedurales en un atlas de 512 × 256 (slot `sky.clouds`, horneado al arrancar con el reparto de las 4 manchas del legacy y bordes de fBm); la nube `i` usa la variante `i mod 4`.
- **Color**:
  - cuerpo `lerp(blanco; skyHorizon; 0,25)` (legacy), con el núcleo a `min(1; 1,5·(0,08 + 0,5·a²))` (0,87 al mediodía, 0,21 de noche) y el borde al alfa del legacy;
  - el 40 % de abajo `lerp(cuerpo; sunGlow; 0,45·g)`: se encienden por abajo en la hora dorada;
  - la sombra inferior del legacy, `skyTop` al `12 %·a`;
  - el lado del sol `+0,12·sunColor·max(0; cos(Δacimut))` de día.
- **Capa lejana** (P2): un fBm de 1024 × 512 con cobertura 0,35 y alfa ≤ 0,35, con la misma deriva. Puede pasar a la 6b.

### 4.6 Luz ambiente y post por hora (ART_BIBLE §6.3, con R6a-03 y R6a-04)

- **Luz clave**: la de §4.3.
- **Ambiente**: modo *Gradient* (cielo `skyTop`, ecuador `skyHorizon`, suelo `waterNear`) × `ambient·0,9`.
- **Sombras**: 0,55 a las 5 h, 0,75 a las 7, 0,85 a las 12, 0,65 a las 18, 0,35 a las 20 (solo Alto) y 0 / 0,25 de noche. Lineal entre keyframes.
- **Exposición, balance de blancos, saturación, contraste y *bloom***: los de la tabla de ART_BIBLE §6.3, lineales entre keyframes.
- **Tonemapping *Neutral*** (no ACES). Viñeta 0,18 y suavidad 0,4.
- **Control para QA**:
  - con la exposición y el color del mediodía, el cielo a 30° de elevación en TV (x 205, y 128) queda a ΔE OK ≤ 0,05 de `#0B4FA8`;
  - a las 6, 18 y 22 h el matiz OKLCH de `skyTop` y `skyHorizon` no se corre más de 8° de la paleta.
  - Los valores medidos los anota WP-D.

### 4.7 Las transiciones de hora

- **La hora real** avanza sola, con segundos.
- **La hora forzada** ("Hora escena"; F-SCN-23): el legacy salta en el acto. Acá la hora visible va hacia la nueva por el camino más corto del reloj, en `clamp(0,25 s × |Δh|; 0,6 s; 2,0 s)` con `ease.move`. Ejemplo: de 12 a 18 h, 1,5 s. Así se ve correr el sol, salir las estrellas y cambiar el agua.
  - El valor que muestra el panel cambia en el acto; solo la escena hace el recorrido.
  - Con reducir movimiento, 0,6 s lineal.
  - Es una diferencia con el legacy: va al PO (P6a-03).

---

## 5. Horizonte y montañas (Q-65)

- **El perfil del legacy, por acimut**, con 70° de acimut = un ancho de pantalla del legacy (la misma escala que el acimut del sol):
  - lejano `p_F = 0,45 + 0,35·sin(6,3u) + 0,2·sin(15,1u)`, con `u = acimut/70° + 0,5`;
  - cercano `p_N = 0,45 + 0,35·sin(6,3u + 3,1) + 0,2·sin(15,1u + 6,2)`, con `u = 1,6·(acimut/70° + 0,5)`;
  - de −65° a +65° en tramos de 0,5°.
- **Altura angular**: `atan(tan θ·max(0; p)·k(d))`, con θ = **9°** (lejano) y **6,3°** (cercano: el 0,7 del legacy).
  - 9° está entre lo que mide el legacy en horizontal (6,8°) y en vertical (14,5°).
  - Más alto, en vertical las cumbres se meten entre las tarjetas; más bajo, el bosque las tapa.
- **Se acercan sin correrse**:
  - `k(d) = D₀/D(d)`, con `D(d) = D_mín + (D₀ − D_mín)·exp(−0,04·d/(D₀ − D_mín))`;
  - lejano D₀ 3500 y D_mín 2500; cercano D₀ 1200 y D_mín 600 (m virtuales);
  - al empezar, se acercan al 4 % de la velocidad del bote, y nunca pasan su mínimo;
  - la forma no cambia con `d`: solo la escala.

| d | 2 km | 5 km | 10 km | 21,1 km | 42,2 km | 100 km |
|---|---|---|---|---|---|---|
| Lejano | ×1,022 | ×1,055 | ×1,104 | ×1,195 | ×1,304 | ×1,390 |
| Cercano | ×1,067 | ×1,165 | ×1,322 | ×1,606 | ×1,887 | ×1,997 |

- **Lo que deja ver** (`horizon.py`; grados sobre el horizonte):
  - en el centro, la lejana a 5,8° y la cercana a 4,3°;
  - a la izquierda, el macizo de la lejana de 5° a 8° (pico de 7,95° a −24°);
  - a la derecha, un **collado** de +12° a +22°, donde las dos cordilleras quedan entre 0° y 1,6°: ahí se pone el sol a las 18 h.
- **Color**: el del contrato, `lerp(tono; fog; 0,35 + 0,4·prof.)` (lejano `#5C6E8A`, prof. 1; cercano `#2F4A3A`, prof. 0,3). Encima:
  - el flanco que mira al sol +12 % y el otro −10 %, según la pendiente del perfil y el lado del sol, de día;
  - el 25 % de abajo de cada cordón, 0,15 más hacia `fog`.
- **Nieve** (lejano):
  - donde `p ≥ 0,55`, con borde suave (0,51-0,59) y ruido de 1,7°, en `lerp(#F2F6FA; fog; 0,3)` (legacy);
  - en la hora dorada `+ lerp(·; sunGlow; 0,35·g)` (alpenglow);
  - de noche × `(0,55 + 0,45·iluminación·visLuna)`.
  - Con la regla del legacy (> 75 %) en cuadro quedaba un solo pico.
- **Relieve fino** (P2): `+0,05·tan 9°·ridged(acimut/1,7°)` donde `p > 0,3`.
- **Malla**: dos tiras de 260 columnas (1 *draw call* cada una, la nieve en la del lejano por color de vértice). Van con el cielo (cola de fondo, sin escribir profundidad), centradas en la cámara: no tienen paralaje lateral y las tapa todo lo demás. Presupuesto: 3 *draw calls* y ≤ 2 k triángulos.

---

## 6. Lago alpino: orilla, terreno y vegetación

### 6.1 Lo que es contrato: el generador del legacy (Q-61)

- **Cada segmento** `k`: `DartRandom(0·1000003 + k·7919 + 17)`. Por orilla, de izquierda a derecha:
  - `z = z₀ + r·6`;
  - mientras `z < z₀ + 50`: pino en `x = lado·(10 + r·6)`, escala `0,7 + r·0,6`, semilla `nextInt(1000)`, y `z += 6 + r·6`.
  - Muelle si `k mod 8 = 3`, en `(−9; z₀ + 25)`. Cabaña si `k mod 12 = 7`, en `(13; z₀ + 10)`, con semilla `k`.
  - Nada más consume esa secuencia.
- **Vectores para QA**:
  - segmento 0: izquierda `(−15,899; 0,565; 0,760; 992)`, `(−12,258; 9,748; 1,265; 197)`, `(−11,767; 19,441; 0,784; 234)`…; derecha `(12,552; 0,720; 1,001; 329)`, `(13,236; 12,068; 0,706; 340)`…, como `(x; z; escala; semilla)`;
  - primer muelle en z 175 (k = 3), primera cabaña en z 360 (k = 7);
  - de 9 a 14 pinos por segmento (11,5 en promedio);
  - visibles a d = 0: **92** (90 pinos, el muelle y la cabaña); a 1234 m, 95; el máximo en 100 km, **102**: el tope de 120 del lago nunca actúa.
- **Variante del pino**: la paleta de verdes es `semilla mod 3` (legacy). La forma 3D también sale de la semilla: lóbulos, ruido y la inclinación ≤ 2°.

### 6.2 El corte transversal

Medidas en metros. `x` es la distancia a la línea del bote; la orilla, `|x| = 9`, como el contrato.

| Tramo (|x|) | Altura `y` | Qué es |
|---|---|---|
| 7,0 → 9,0 | −0,55 → 0 | fondo bajo el agua (15°); no se ve con el agua opaca, pero deja una intersección limpia |
| 9,0 → 10,0 | 0 → 0,27 | playa de canto rodado (15°); de 0 a 0,35 m sobre el agua, franja húmeda más oscura |
| 10,0 → 16,0 | 0,27 → 2,50 | talud de pasto y roca (20°), con ruido de ±0,25 m en 6 m; aquí van los pinos del legacy |
| **Izquierda** > 16 | `2,5 + 0,12·(|x| − 16)` | ladera boscosa: 5,4 m a 40 y 36,6 m a 300; más allá, hasta 1,5 km, se funde con la cordillera cercana en la niebla |
| **Derecha** > 16 | `2,5 + 0,02·(x − 16)` | pradera alta: 4,2 m a 100 y 8,2 m a 300 |

- **La orilla serpentea solo hacia afuera**: `|x_orilla(z)| = 9,0 + 0,5·n(z_mundo/41 m)`, con `n ∈ [0; 1]` suave y determinista por lado. Todo el corte se corre con ella y nunca entra al canal de 9 m.
- **Las envolventes** (lo nuevo, sin los objetos del legacy, nunca las pasa):
  - izquierda: copa a ≤ `2,2 + 0,25·|x|` desde |x| = 18 (7,2 m a 20, 12,2 a 40, 22,2 a 80, 39,7 a 150);
  - derecha: ≤ `2,2 + 0,06·x` desde x = 30, y antes solo matas de ≤ 1,2 m.
- **Por qué**: con estas envolventes la silueta del bosque mide `atan(0,25·tan|acimut|)`: 3,0° a −12°, 5,2° a −20° y 6,4° a −24°. Queda debajo del macizo nevado hasta −28° (7,7°) y debajo del sol de la tarde en el collado.
  - Con la ladera de la biblia, leída como un bosque denso de 16 m desde x = 22, el bosque medía 7,7° a −10° y tapaba todo lo que no fuera el centro.
  - El sol sale por la izquierda, detrás de la ladera, y se pone a la derecha, sobre la pradera.
  - Las cabañas (derecha) quedan en el lado abierto y los muelles (izquierda), al pie del bosque.

### 6.3 Nada flota

| Objeto | Regla |
|---|---|
| Pino (legacy) | Pie en `y` = la altura más baja del terreno bajo el tronco (radio `0,15·escala`) menos 0,20 m: el tronco entra 0,20 m en el suelo |
| Cabaña (legacy) | **El ancla `(13; z₀ + 10)` es el centro del frente o del fondo**: 4 × 4 m (x 11-15). Se prueba en orden: crece 4 m hacia +z; si no, hacia −z; si no, 3 m hacia +z y después hacia −z; si tampoco, se corre en z lo mínimo. Para que una opción sirva, ningún tronco tiene que quedar a menos de `0,15·escala + 0,30` m de la huella. En 333 cabañas (200 km): 162 hacia +z, 136 hacia −z, 10 de 3 m y 25 corridas, **la que más, 0,50 m**. El frente con la puerta y la ventana mira a la cámara, como el legacy. **Zócalo** de piedra `#6B6A66`: el piso va 0,15 m sobre la esquina más alta de la huella y el zócalo llega hasta el terreno (≈ 1,5 m del lado del agua: un chalet alpino). **Poda**: los pinos cuya copa (radio `1,3·escala`) toca la huella + 0,2 m arrancan su grada de abajo a 3,0 m, sobre el alero de 2,6 m; la posición, la escala y la variante no cambian |
| Muelle (legacy) | Tablero a 0,40 m de x −6 hasta donde la playa llega a 0,30 m (≈ −10,1), de 1,6 m de ancho (z ± 0,8). Pilotes en pares en x −8,7, −7,5 y −6,3 (los postes del legacy) hasta el fondo. El arranque en tierra se corta 0,3 m antes de un tronco |
| Juncos (nuevo) | Raíz en `y ≤ 0,05` (agua somera o playa húmeda), en |x| de 8,6 a 9,6. Ninguno a menos de 3 m de un muelle ni frente a una cabaña (± 4 m en z) |
| Rocas (nuevo) | Hundidas el 30 % de su alto |
| Bosque, matas, pasto (nuevo) | Pie a la altura mínima bajo su huella − 0,20 m |

- **La altura del terreno es una función pura de Core**, `TerrainHeight(entorno; x; z_mundo)` en `double`. La usan la malla y la colocación de cada objeto, así no hay dos verdades.
- **Prueba de dotnet en 100 km**: ningún pie queda sobre el terreno; ningún tronco dentro de una cabaña; ninguna cabaña corrida más de 0,5 m.
- **Prueba de PlayMode**: rayos hacia abajo desde cada objeto visible en las tomas de la 6a.

### 6.4 Lo que se suma al legacy

Todo es determinista por segmento, con su propia semilla `id·1000003 + k·7919 + 17 + sal·104729`. La sal va de 1 a 6: juncos, rocas, bosque cercano, matas de la pradera, pasto y helechos, y serpenteo. Nunca toma números del generador del legacy, así que el legacy da el mismo paisaje con o sin lo nuevo.

| Capa | Dónde | Densidad | Alto |
|---|---|---|---|
| Juncos (`vegetation.reeds`) | |x| 8,6-9,6 | 1 cada 0,8 m, con huecos de 2 a 6 m cada 10 a 25 m | 0,8-1,6 m |
| Rocas (`prop.rock`) | playa y pie del talud | 1 cada 7 m por orilla | 0,2-0,7 m |
| Bosque cercano (`prop.pine`, otros parámetros) | izquierda, |x| 18-60 | disco de Poisson de 4,5 m | `min(envolvente − terreno; 6 + 0,12·(|x| − 18))` |
| Copa lejana (`vegetation.canopyFar`, nuevo) | izquierda, |x| > 60 | malla: el terreno levantado por la copa, con material de bosque | hasta la envolvente |
| Matas de la pradera | derecha, x 30-120 | un grupo cada 90-150 m en z, de 3 a 9 coníferas chicas y arbustos | bajo la envolvente |
| Pasto, helechos y arbustos (`vegetation.grass`, `.fern`, `.bush`) | talud y pradera hasta 60 m de la cámara; helechos al pie del bosque | 6 matas/m² cerca, que bajan a 0 a los 60 m | 0,2-0,6 m |

### 6.5 Materiales, niebla y noche

- **Pino**: corteza `#5D4037` (*smoothness* 0,15).
  - Copa de 3 gradas en las paletas del legacy (`#1B5E20`… `#4F9A48`), mallas opacas con 9 a 12 lóbulos y ruido (sin *alpha test* salvo en LOD0), luz envolvente 0,35 y oclusión por color de vértice.
  - Viento: `0,03·sin(1,1t + x·0,13)` rad en la punta, con peso altura² (ANIMATION_BIBLE §6).
  - LOD: 1200 / 400 / 120 triángulos e impostor.
- **Cabaña**:
  - madera `#6D4C41` (tablas de 0,18 m por shader), techo `#4E342E` y puerta `#3E2723`;
  - ventana `#90CAF9` de día (refleja el cielo) y `#FFE082 × 1,6` en HDR de noche, que enciende con ambiente < 0,6 con un desfase fijo de ± 10 min por cabaña y un fundido de 0,4 s;
  - zócalo `#6B6A66`.
- **Muelle**: tablero `#8D6E63` y pilotes `#5D4037`.
- **Terreno** (procedural): canto rodado `#7A746B` (húmedo `#4E4A45`), pasto `#4F7A3A` ↔ `#6B8A3E`, suelo de bosque `#4A3B2C`, roca `#6B6A66` con pendiente > 35° (triplanar solo la roca). El ruido es una textura de 256² compartida, no un hash iterado.
- **Niebla** (terreno, objetos y vegetación): `f(z) = 0,8·clamp((z − 80)/320) + 0,2·clamp((z − 400)/800)`. Hasta 400 m es el `_c` del legacy; después sigue hasta 1 en 1200 m, para que la ladera lejana se funda.
- **Aparecer y desaparecer**: los objetos del legacy entran con *dither* de 400 a 360 m, como el rango del legacy. El terreno y el bosque lejano no tienen fundido: son continuos.
- **Noche**: el tinte azul del legacy (`blue + 0,12`) sale del ambiente y de la luz de luna.
- **Bruma baja** (P2, F-SCN-15): en la niebla de todos los shaders de la escena, `f = max(f; m(h)·exp(−max(y; 0)/1,2)·S(10; 150; z))`, con `m(h)` de 0 a las 5 h a 0,55 de 6 a 7 h y de vuelta a 0 a las 9:30.

### 6.6 Procedural por defecto, y qué mejora con el Terrain Sample Asset Pack

| Slot | Procedural (siempre disponible) | Con el pack (ASSET_SLOTS §5.2, sin verificar acá: el pack no está instalado) |
|---|---|---|
| `terrain.layer.pebbles` / `.grass` / `.soil` / `.rock` | color y normal por ruido de los tonos de §6.5 | las capas de canto rodado, pasto, tierra y roca del pack (albedo, normal y máscara de 1024; ASTC 6×6 en móvil) |
| `vegetation.grass` / `.fern` / `.bush` | tarjetas cruzadas con textura procedural | los *prefabs* de pasto, helecho y arbusto del pack con su Shader Graph de URP |
| `prop.pine`, `vegetation.reeds`, `vegetation.canopyFar`, `prop.cabin`, `prop.pier`, `prop.rock` | procedurales | **no cambian** (el pack no trae coníferas ni juncos) |
| Cielo, astros, nubes, cordilleras, agua, bote | procedurales | **no cambian** |

- **Lo que se nota**: la playa, el talud y el pie del bosque de cerca (0-40 m), que dejan de leerse como "ruido de color". El resto del cuadro no cambia.
- **Costo**: 4 capas × 3 mapas a 1024 en ASTC 6×6 ≈ 1,9 MB con *mipmaps*; el *shader* del terreno no cambia.
- **Si falta**: lo procedural, sin errores y con un solo aviso `[SLOT]` por slot.

---

## 7. Agua degradada (R-04; F-SCN-13)

En la 6a es el agua de la escena (la avanzada llega en la 6b). Va en un plano centrado en la cámara de 1,5 km. Con `z` = la distancia hacia adelante, `x` lateral, `t` = el tiempo de escena y los colores de la paleta (el lago no tiñe el agua):

1. **Base** (contrato): `lerp(waterNear; waterFar; smoothstep(2; 120; z))`.
2. **Cielo** (Fresnel del legacy): `fres = (1 − 2,2/√(z² + 2,2²))³`; `col = lerp(col; skyHorizon; 0,6·fres)`.
3. **Franja somera**: `lerp(col; húmedo·ambient; 0,35·smoothstep(|x_orilla| − 0,6; |x_orilla|; |x|))`. Es el fondo que se ve cerca de la orilla y marca el contacto con la playa.
4. **Camino de brillo del astro**:
   - columna `exp(−(Δacimut/6°)²)·S(3; 30; z)·(1 − S(250; 400; z))`;
   - por destellos `0,35 + 0,65·b·(1 − S(25; 90; z))`, con `b = S(0,62; 0,95; 0,5 + 0,5·sin(2π·z/1,6 + φ_d + 0,9x + 1,3t))`;
   - color `sunColor × 0,45 × uLight × vis × (1 + 0,8·g)`, sumado.
   - `uLight` es la fuerza del legacy (día `clamp(1,2e; 0,2; 1)`, noche 0,45). La luna, además, × `(0,4 + 0,6·iluminación)`.
   - `φ_d = 2π·frac(d/1,6)` lo calcula Core en `double` (Q-68): los destellos pasan con el agua al remar, sin saltos a ninguna distancia, y siguen con el tiempo en la pausa.
   - Pasados los 90 m los destellos se promedian en 0,35, así no hay moiré.
5. **Niebla** (contrato): `lerp(col; fog; smoothstep(80; 400; z))`.
6. **Bruma del horizonte** (§4.2): `lerp(col; bruma; smoothstep(600; 1500; z))`.
7. **Sombra del bote** (contrato del nivel Bajo, en todos los niveles en la 6a): óvalo suave de 0,5 m bajo el casco, al lado opuesto del astro (x ± 0,35), con opacidad `0,08·ambient`.

| Hora | Cerca | Lejos | Niebla | Fresnel hacia | Brillo (color · fuerza) |
|---|---|---|---|---|---|
| 6 h | `#1C4274` | `#4B5F8F` | `#D2B9B8` | `#FFBA7E` | `#FFCA88` · 0,2 × 1,8 (hora dorada) = 0,36, bajo el sol a −17,5° |
| 12 h | `#0E4F8F` | `#2F8FD6` | `#CFE9FF` | `#8FD3FF` | `#FFF6D5` · 1,0, al centro |
| 18 h | `#163A66` | `#7C5C7A` | `#E0A28A` | `#FF8C5A` | `#FFB36B` · 0,36, a +17,5° (camino dorado largo) |
| 22 h | `#06122A` | `#0E1E3C` | `#0F1730` | `#121C3A` | `#E6EEFF` · 0,45 × (0,4 + 0,6·iluminación), bajo la luna a −5,8° |

- **Costo**: ≤ **24 ALU** y 0 texturas.
  - Con el agua en ≈ 55 % de 1,83 Mpx (918 × 1989, nivel Medio), son ≈ 24 M de operaciones por cuadro: menos de 0,6 ms en una Mali-G68 MC4.
  - Es la R-04 obligatoria: el *SubShader* de respaldo de la 6b es este.
- **El legacy** (`water_painter_test:11`, sin shader) se porta como "la escena pinta con el agua degradada sin excepciones". La prueba no fija las 10 líneas de pantalla del legacy, que acá se reemplazan por el punto 4.

---

## 8. Bote provisorio

Slots `boat.*` (ASSET_SLOTS §1.1), procedural y con las medidas reales de ART_BIBLE §7.8 (D-ART-04, que se decide en la 7; por defecto se adopta). En la 6a no se mueve respecto de la cámara: el cabeceo del contrato (F-SCN-06) llega en la 7, con la palada.

| Pieza | Spec |
|---|---|
| Casco | Loft de 24 secciones de z 5,3 (popa) a 13,5 (proa), manga 0,29 m en la flotación y 0,33 m en la borda, francobordo 0,10 m, bola de proa blanca. `#F5F5F5` → `#B0BEC5` en la obra viva, línea de borda `#00B4D8` y cockpit `#263238`; gelcoat con *smoothness* 0,85 |
| Riggers | Alas negras satinadas, pernos a ±0,80 m en z 9,5, eje a 0,31 m; chumaceras blancas |
| Carro y pedalera | Asiento negro a 0,14 m con rieles; zapatillas blancas a 1,15 m del perno |
| Remos en reposo | Perpendiculares al casco. Caña de carbono de 2,88 m (interior 0,88), con la pala tipo hacha (0,47 × 0,22 m, cara `#00B4D8`, punta blanca) **plana sobre el agua** (y 0,02), como el remero que descansa |
| Sombra | §7, punto 7 |
| Presupuesto | Casco ≤ 6 k triángulos, riggers y carro ≤ 2 k, remos ≤ 3 k; 2 materiales y 3 *draw calls* (con la sombra) |

En pantalla, en TV: popa en y 528, proa en 430 y puntas de los remos en x 84 y 327. Se mira con el contorno de la zona del bote (CAP-16 de la 4a).

---

## 9. El HUD sobre la escena

### 9.1 El velo de pausa

- **Qué es**: un *Volume* de URP de peso 0 → 1 en 300 ms (`ease.move`) al pausar y 1 → 0 en 240 al reanudar (ANIMATION_BIBLE §2.11).
- **Qué hace en pantalla**: desatura un 30 % y oscurece un 15 %, medido después del tonemapping (el cielo del mediodía en TV pasa de ≈ `#0B4FA8` a ≈ `#0E428A`).
- **Cómo**: *Color Adjustments* con saturación −30 y `postExposure` ≈ −0,47 EV. El valor exacto lo ajusta WP-D con una sonda: la luminancia del cielo a 30° baja a 0,85 ± 0,03 de la de antes.
- **Qué no toca**: la UI, que se compone después del post. Las tarjetas no bajan de opacidad (como la 4b).
- **Qué sigue igual**: `PAUSA` va en el disco del centro alto y la respiración pasa a ×0,6.
- **Qué sigue en pausa**: el tiempo de escena, así que las nubes, las estrellas y los destellos siguen. El bote desacelera con su inercia (contrato).
- **Si se ve el fondo de carga** (§9.4), el velo es el de la 4b: el tinte del fondo, 15 % más oscuro.

### 9.2 El vidrio

- **Hasta la 6b**, el respaldo `glass.fallback` (`#070D16 @86 %`) en todas las piezas, sin reflejo, como la 5.
- **El contraste no cambia**: está calculado contra una nube blanca, y el sol en HDR, después del tonemapping, nunca pasa del blanco.
- **Con la escena**, el vidrio va de `#08162A` (cielo del mediodía) a ≈ `#060B14` (noche), con el mismo contraste para el texto.

### 9.3 Las píldoras con una nube detrás (el riesgo de la 4b, ART_BIBLE §10.1)

En TH, L640 y W1280 las nubes pasan detrás de la barra de pasos (§3.4); con el sol a 17° de la biblia, también el sol de 8:40 a 15:20. Con el peor vidrio (`pills.py` y `pills2.py`, método de `Contrast.cs`; umbral 3 para gráficos):

| Tipo | Hecha / vidrio, base: calculado · renderizado | Con `.hud` | Borde de la hueca / vidrio, base → `.hud` | Borde / su interior, base → `.hud` |
|---|---|---|---|---|
| Trabajo | 3,038 · **2,995** | 3,678 · 3,625 | 3,154 → 3,829 | 2,624 → 3,018 |
| Descanso, calentamiento | ≥ 5,5 | = base | ≥ 5,8 | ≥ 4,0 |
| Enfriamiento | **2,850 · 2,809** | 3,759 · 3,705 | 2,944 → 3,905 | 2,407 → 2,973 |
| Otro | **2,629 · 2,592** | 3,745 · 3,691 | 2,707 → 3,887 | 2,220 → 2,934 |

- **Propuesta al PO (P6a-01)**: en el HUD, el relleno, la hecha, el borde o los rieles de la hueca y el pulgar se dibujan con `step.*.hud`.
  - Cambian a la vista tres colores: trabajo `#EF476F → #FF5D7F` (el mismo del texto del tipo), enfriamiento `#118AB2 → #37A2CB` y otro `#607D8B → #7D9BA9`.
  - Descanso y calentamiento no cambian.
  - Fuera del HUD (la mini-barra del editor) siguen la base. El halo de los últimos 5 s sigue en la base, porque es decorativo.
- **Lo que no llega a 3** ni con `.hud` es el borde de la hueca contra su interior (2,93 a 3,02). El interior al 18 % es un tenue de contrato y la hueca se lee además por la forma (§10.3). Va como excepción contada, como la pista al 30 %.

### 9.4 El fondo de carga

- **El elemento de la 4a se reutiliza**: el de los dos tonos, que es parte del presupuesto del HUD. Se tiñe por código con `skyTop` y `waterNear` **de la hora**: de noche ya no sale el azul del mediodía.
- **Cuándo se ve**: solo mientras la escena no está lista. Cuando entra, se funde en 240 y pasa a `display: none`. Si `HudBudgetTests` lo cuenta, hay un elemento visible menos (56-57 → 55-56 de 60); si no, el presupuesto queda igual.
- **Los tokens** `backdrop.sky` y `backdrop.water` quedan como los valores de las 12 h, para sus pruebas.
- **Tokens nuevos**: ninguno. Los degradés de §3.5 van en la escena.

---

## 10. Presupuesto de rendimiento (regla 9; ARCHITECTURE §20)

Nivel **Medio** (Galaxy A54 / Pixel 6a; `renderScale` 0,85: 918 × 1989 en un 1080 × 2340), con la escena de la 6a a distancia cualquiera. Los pinos visibles por banda (`lodcount.py`, 20 km cada 37 m) son: 1-40 m, 9 en promedio (12 como máximo); 40-120 m, 18 (22); 120-250 m, 30 (34); 250-400 m, 35 (39).

| Pieza | *Draw calls* | Triángulos | Texturas | Fragmento | GPU (estimada) |
|---|---|---|---|---|---|
| Cielo (degradé, bruma, resplandor, fondo de estrellas) | 1 | 2 k | — | ≤ 35 ALU | 0,4 ms |
| Estrellas del legacy, sol, luna, nubes (instanciados) | 3 | < 1 k | luna 256², nubes 512 × 256 | ≤ 20 | 0,2 |
| Cordilleras | 3 | 2 k | — | ≤ 15 | 0,2 |
| Agua degradada | 1 | 2 k | — | ≤ 24 | 0,6 |
| Terreno (9 segmentos de 50 m + lejano) | 10 | 40 k | ruido 256² (+ pack: 4 × 3 de 1024) | ≤ 35 / 3 tex | 1,6 |
| Pinos del legacy (LOD0-2 + impostor, instanciados) | 4 | ≤ 27,4 k (peor caso) | — | ≤ 30 | 1,0 |
| Cabañas y muelles | 2 | 3 k | — | ≤ 35 | 0,2 |
| Bosque cercano, copa lejana, matas | 4 | 25 k | — | ≤ 30 | 1,2 |
| Pasto, helechos, juncos, rocas | 5 | 35 k | atlas 512² | ≤ 30 | 1,4 |
| Bote provisorio y sombra | 3 | 11 k | — | ≤ 35 | 0,3 |
| Sombras (1 cascada de 1024, 40 m: bote y objetos cercanos) | ≤ 15 | — | — | — | 1,2 |
| Post (*bloom* a media resolución, color por hora, *Neutral*, viñeta, velo) | ≤ 8 | — | — | — | 1,5 |
| UI (HUD, 55-57 elementos) | ≤ 12 | — | — | — | 1,0 |
| **Total** | **≈ 71** (tope 150) | **≤ 160 k** (tope 350 k) | **≤ 12 MB** de escena (tope 180) | | **≈ 10,8 ms** (tope 12) |

- **CPU de escena** ≤ 1,5 ms: Core del cielo, astros, colocación y *streaming*; sin asignaciones.
- **GC**: 0 B por cuadro en régimen, medido con `FrameAllocations` (cuadros reales, con su control positivo), nunca con `GC.GetAllocatedBytesForCurrentThread`.
- **Sobre-dibujo** ≤ 2,5× en la franja de vegetación: el pasto solo hasta 60 m y las copas opacas.
- **El nivel Bajo** saca:
  - el fondo de estrellas y la capa lejana de nubes;
  - el bosque cercano, que pasa a la copa lejana desde x = 18;
  - el pasto, que queda en 30 m con la mitad de la densidad, y los juncos, de 1 cada 1,6 m;
  - las sombras (queda el óvalo);
  - la distancia de LOD, ×0,7.
- **El nivel Alto** suma la bruma baja, la capa lejana, el relieve fino de las cordilleras y la sombra a 80 m con 2 cascadas.
- **La adaptación térmica** (ARCHITECTURE §20.3): resolución → (sin reflejo en la 6a) → distancia de sombras → densidad de vegetación → nivel.
- **Lo que queda para medir con el teléfono**: los 60 fps durante 30 min con el agua degradada, la fila de 200 píldoras (R4b-02) y los 60 fps con el diálogo de completado abierto. Sin teléfono, NO VERIFICABLE hasta la It. 14 (regla 9). En Windows se mide con el mismo arnés `[PERF]`.

---

## 11. Asset slots de la 6a

| Slot | Tipo (ARCHITECTURE §14.4) | Placeholder | Nuevo |
|---|---|---|---|
| `sky.profile` | `DataSlot` | la paleta del legacy y los derivados de §4 | — |
| `sky.clouds` | `TextureSlot` | atlas de 4 cúmulos horneado al arrancar (§4.5) | — |
| `sky.moon` | `TextureSlot` | albedo de 256² con 4 "mares" y cráteres por ruido | — |
| `sky.stars` | (en el shader) | `DartRandom(42)` y el hash | — |
| `horizon.lake` | `DataSlot` | perfiles, θ, `D₀`, `D_mín`, nieve (§5) | — |
| `water.profile.lake` | `DataSlot` | los números de §7 (y los de la 6b) | — |
| `terrain.layer.pebbles` / `.grass` / `.soil` / `.rock` | `TerrainLayerSlot` | §6.6 | — |
| `vegetation.grass` / `.fern` / `.bush` / `.reeds` | `VegetationSlot` | §6.6 | — |
| `vegetation.canopyFar` | `MaterialSlot` | el material de bosque lejano (§6.4) | **sí** |
| `prop.pine`, `prop.cabin`, `prop.pier`, `prop.rock` | `PropSlot` (3 LOD + impostor) | §6.5 | — |
| `boat.hull`, `boat.rigger`, `boat.seat`, `boat.stretcher`, `boat.oar`, `boat.livery` | `BoatPartSlot`, `OarSlot`, `DataSlot` | §8 | — |

Ningún slot de la 6a pide algo del usuario para verse completo. El Terrain Sample Asset Pack entra solo por los slots de terreno y vegetación (§6.6). Todo lo de terceros va a `Assets/ThirdParty/`, que el repositorio ignora salvo los `.meta`, y el CI compila sin eso.

---

## 12. Mixamo para la It. 7 (P-09)

**No hace falta ninguna animación de Mixamo para remar.** La remada es IK procedural sobre el rig Humanoid (ANIMATION_BIBLE §0.3):

- un clip tiene un ritmo fijo de drive y recuperación (Q-70 lo pide variable con el SPM);
- mezclar clips despega las manos del puño;
- un clip no se prueba con dotnet;
- Mixamo no tiene *sculling* con carro.

De Mixamo se usa **el personaje**. La pose de agarre de los dedos la genera un script del Editor (`RowerPoseClipBuilder`).

Todo se descarga desde [mixamo.com](https://www.mixamo.com) con una cuenta de Adobe (gratis) y se copia a `Assets/ThirdParty/Mixamo/<Carpeta>/`. No se versiona: la licencia de Mixamo permite usarlo en la app, no redistribuirlo suelto (ASSET_SLOTS §2). La importación es la de ASSET_SLOTS §2.5: Humanoid, *Create From This Model* y **Optimize Game Objects apagado**.

| # | Archivo | En Mixamo | Opciones de descarga | Por qué | ¿Obligatorio? |
|---|---|---|---|---|---|
| 1 | `YBot/YBot.fbx` | **Characters** → `Y Bot` | **Format** `FBX for Unity(.fbx)` · **Pose** `T-pose` (un personaje no tiene opción de piel ni de fps) | Calibra el IK y `RiggingFit` (ANIMATION_BIBLE §7.4): rig limpio, proporciones neutras y pesado parejo. Con él se fijan los números de la pose de agarre | **Sí**, antes de la 7 |
| 2 | `XBot/XBot.fbx` | **Characters** → `X Bot` | ídem | Un segundo rig con otras proporciones, para probar que `RiggingFit` ajusta solo la pedalera y el carro | Recomendado |
| 3 | `RowerRealistic/RowerRealistic.fbx` | **Characters** → el primero que cumpla la lista de control de ASSET_SLOTS §2.3 (ropa deportiva ajustada, sin accesorios, pelo corto o recogido, 5 dedos con rig, proporciones adultas) | ídem | El aspecto final del remero. No pude ver el vestuario de cada personaje: Mixamo pide sesión. Si ninguno cumple, queda Y Bot con el mono pintado | Opcional (7 o 14) |
| 4 | `YBot/YBot@SittingIdle.fbx` | **Animations** → `Sitting Idle`, con Y Bot elegido (si no aparece con ese nombre, buscar "sitting idle" y elegir uno con el torso quieto y las manos sobre los muslos) | **Format** `FBX for Unity(.fbx)` · **Skin** `Without Skin` · **Frames per Second** `30` · **Keyframe Reduction** `none` · **In Place**: no aparece en un clip sin desplazamiento (si aparece, destildado) | **Solo de referencia**: para comparar la respiración procedural y la pose de los dedos con un movimiento capturado. Nunca se reproduce en la app (`rower.clip.idleRef`). Sin piel: pesa menos y no duplica la malla ni los materiales. A 30 fps alcanza para mirarlo; sin reducción de cuadros clave, las curvas quedan enteras | Opcional |

En la 7, el slot `rower.character` acepta el 1 o el 3 y vuelve al maniquí procedural si falta o no valida (Humanoid, dedos, alto de 1,55 a 2,05 m, ≤ 30 k triángulos). La lista reemplaza a la "preliminar" de ASSET_SLOTS §2 cuando se alinee (§16).

---

## 13. D-ART-08: texturas CC0 opcionales y la luna de la NASA

| Opción | Qué aporta en el lago | Licencia (leída en su página) | Costo |
|---|---|---|---|
| **(a) No** (por defecto) | Lo procedural, más el Terrain Sample Asset Pack si el usuario lo instala | — | — |
| (b) [ambientCG](https://ambientcg.com) | Madera de tablas (muelle, cabaña), corteza, grava y roca, en 1K | **CC0 1.0** ([licencia](https://docs.ambientcg.com/license/)): sin crédito obligatorio; uso comercial y redistribución permitidos | 1K-JPG, ≈ 0,5-1 MB por material; a `Assets/ThirdParty/CC0/` |
| (c) [Poly Haven](https://polyhaven.com/license) | Lo mismo (alternativa) | **CC0** ([licencia](https://polyhaven.com/license)): igual que (b); el logo y los textos del sitio no son CC0 | ídem |
| (d) [NASA SVS — CGI Moon Kit](https://svs.gsfc.nasa.gov/4720) | El mapa de color real de la luna (`lroc_color_poles_1k.jpg`, 1024 × 512, 136 KB), reducido a 512 para `sky.moon` | La página pide el crédito "NASA's Scientific Visualization Studio". Por las [pautas de imágenes de la NASA](https://www.nasa.gov/nasa-brand-center/images-and-media/), el material en general no tiene copyright en EE. UU., hay que reconocer a la NASA como fuente y no se puede sugerir que respalda la app; sus logos están protegidos | Un archivo; el crédito va en la pantalla de licencias de la It. 14 |

- **Recomendación**: **no** para la 6a.
  - Por la luna no vale la pena: el disco mide de 8 a 16 dp de radio y el procedural se ve bien a ese tamaño.
  - En el lago, las texturas de cerca las mejora más el pack (terreno y matas) que el CC0.
  - Sumar fuentes suma pasos para el usuario y créditos que mantener.
- **Dónde sí rinde CC0**: las fachadas de ladrillo y piedra del río y los clubes de la It. 8. Propongo volver a ofrecerlo en ese gate. Si el usuario dice que sí, entra por los slots (`MaterialSlot` y `TextureSlot`) sin tocar lógica.
- **Si en algún momento se acepta**: aunque CC0 permitiría versionarlos, van a `Assets/ThirdParty/CC0/`, fuera del repositorio, por peso y por la regla única de terceros.

---

## 14. Pedidos al Architect

| ID | Prioridad | Pedido | Verificación |
|---|---|---|---|
| **DD6a-01** | P0 | `SceneTimeOfDay`: el port de `time_of_day.dart` con interpolación **en sRGB**, los umbrales exactos 19,5719 y 4,4281 como constantes y la hora continua con segundos | dotnet: `time_of_day_test` (6) portado; la tabla de §4.1 a ± 1/255 por canal y ± 1e-9 en el ambiente |
| **DD6a-02** | P0 | `CelestialPlacement` con las fórmulas de §4.3: acimut del legacy, recorrido de 1,5° a 12°, oculto a −2,5°, ventanas de salida y puesta del sol y de la luna, elevación y fuerza de la luz | dotnet: la columna "astro" de §4.1 a ± 0,01°; los dos astros ocultos en el cambio; ≤ 0,3°/min; acimut en ±24,5°; continuidad a medianoche; `water_painter_test` sigue pasando (acimut 0 al mediodía) |
| **DD6a-03** | P0 | `HourLighting`: la luz (`K(h)·vis` y la de la luna), el ambiente × `ambient·0,9`, sombras, post por hora (ART_BIBLE §6.3), el factor dorado `g`, el tonemapping *Neutral* y el perfil de *Volume* que interpola Core | dotnet: luz 0 en el cambio; los valores de §4.1; `g`(6 h) = `g`(18 h) = 1 y `g`(12 h) = 0 |
| **DD6a-04** | P1 | `MoonPhase`: el modelo sinódico, la iluminación y la lista de países del hemisferio sur (P2) | dotnet: los tres vectores de §4.3 (± 1 d) y el espejo con `AR` |
| **DD6a-05** | P0 | `SkyModel`: las 80 estrellas con `DartRandom(42)` (orden x, y, radio) y su mapeo, `visEstrellas` con Q-62, las 7 nubes con D-ART-06 (acimut, elevación, ancho, envolver a ±60°). La función de deriva del legacy queda para su prueba (LEGACY_MAPPING) | dotnet: 80 visibles a las 19 h y 0 a las 12 h y a las 6:12; las tres tiradas de la estrella 0 son `0.15092545597797424`, `0.6041479725361217` y la tercera de `Random(42)`; ninguna nube cambia con `d` |
| **DD6a-06** | P0 | Cielo (`SkyController`): el degradé de §4.2, la banda de bruma, el resplandor dorado, los discos y halos del sol y la luna con fase, las estrellas y las nubes instanciadas; todo en la cola de fondo | Tomas de §15.2; ≤ 35 ALU; 4 *draw calls* |
| **DD6a-07** | P0 | `HorizonSilhouette`: los perfiles por acimut, θ 9° y 6,3°, `k(d)` con `D₀` y `D_mín`, la nieve sobre 0,55 con el alpenglow, el sombreado por flanco y la bruma de la base; dos tiras de 260 columnas | dotnet: `D'(0) = −0,04`, monótona y acotada; la tabla de §5 a ± 0,001; la forma no depende de `d`. Toma: el collado a la derecha (+12° a +22°) |
| **DD6a-08** | P0 | `DartRandom` y `LakeEnvironment`: el generador del legacy idéntico, con la caché por instancia | dotnet: los cuatro vectores dorados del oráculo; `environment_test` (genéricos y lago); los vectores de §6.1 (las 12 primeras posiciones, 92 visibles a d = 0 y 95 a 1234 m) |
| **DD6a-09** | P0 | `TerrainProfile` (Core): `TerrainHeight(entorno; x; z_mundo)` en `double` con el corte de §6.2, el serpenteo hacia afuera y las envolventes | dotnet: `y = 0` en `|x_orilla|`, nunca adentro de 9 m; continuidad entre segmentos (costura < 1 mm); las envolventes |
| **DD6a-10** | P0 | `PropPlacement` (Core): pies, la regla de la cabaña (§6.3), el zócalo, la poda y el muelle hasta la playa | dotnet en 100 km: nada flota; ningún tronco en una cabaña; cabaña corrida ≤ 0,5 m; los totales de §6.3 en 200 km (162 / 136 / 10 / 25) |
| **DD6a-11** | P1 | `ShoreFill` (Core): lo que se suma (§6.4) con sus sales, las zonas de exclusión y las envolventes | dotnet: el paisaje del legacy es el mismo con y sin `ShoreFill`; nada fuera de su envolvente; juncos fuera de las zonas de muelles y cabañas |
| **DD6a-12** | P0 | `ShoreStreamer` y mallas: *pools* por tipo, LOD con *cross-fade*, aparecer con *dither* de 400 a 360 m, terreno por segmento hasta 450 m y el lejano hasta 1,5 km, sin asignaciones por cuadro | PlayMode: 0 B por cuadro con `FrameAllocations` mientras se rema 2 km; ningún objeto aparece de golpe (sonda de alfa en 360-400 m) |
| **DD6a-13** | P0 | Agua degradada (§7) con `φ_d` en `double` (Q-68) y ≤ 24 ALU, 0 texturas; es también el *SubShader* de respaldo de la 6b | dotnet: `φ_d` en 4096 m y en 10 km, sin salto; PlayMode: el equivalente de `water_painter_test:11` |
| **DD6a-14** | P0 | `ChaseCameraRig` y `ChaseCameraMotion`: la pose de §3.1 (con el signo de `lensShift` validado), la respiración de §3.2 con el rolido de 0,15° y los multiplicadores | dotnet: `scene_camera_test` (4); horizonte ≤ 3 dp en los cinco perfiles; ×0 con reducir movimiento. PlayMode: la proyección de Unity contra `SceneCamera.Project` (≤ 0,5 dp) |
| **DD6a-15** | P0 | Entrada y salida con la escena (§3.3): el vuelo con sus puntos de control, el retroceso en la salida de 640, la escena tarde (fondo de carga y fundido de 240) y reducir movimiento | PlayMode: AC de la 4b (13-06, -08 y -09) sin cambios; la pose del contrato a los 1820 ms; nada se mueve después de los 640 |
| **DD6a-16** | P0 | El velo de pausa como *Volume* (§9.1), sin tocar la UI | Sonda: la luminancia del cielo a 30° a 0,85 ± 0,03 en pausa; la saturación OKLCH × 0,7 ± 0,05 |
| **DD6a-17** | P1 | Los degradés de pantalla en la escena (§3.5) | Sondas en TV y TH; 0 elementos de UI nuevos (`HudBudgetTests`) |
| **DD6a-18** | P0 si el PO adopta P6a-01 | Las píldoras del HUD con `step.*.hud` (§9.3) | `ContrastTests`: hecha y borde de la hueca ≥ 3 en el peor vidrio renderizado; el borde contra su interior como excepción contada |
| **DD6a-19** | P1 | La hora visible que sigue a la forzada (§4.7): camino corto del reloj, `clamp(0,25 s/h; 0,6; 2,0)` con `ease.move` y 0,6 s lineal con reducir movimiento | dotnet: de 12 a 18 h en 1,5 s pasando por 15 h a los 0,75 s; de 23 a 1 h por medianoche |
| **DD6a-20** | P0 | El bote provisorio (§8) y su sombra | Toma: el bote en la zona del bote con su contorno |
| **DD6a-21** | P1 | Lo de cada nivel de calidad (§10) en `QualityTierProfile` | PlayMode: el nivel Bajo sin estrellas de fondo, sin bosque cercano y sin sombras |
| **DD6a-22** | P0 | `SlotIds` con `vegetation.canopyFar` y el `.asset` de cada slot de §11 | dotnet: ids sin repetir; los 15 `PropKind` con su slot; PlayMode: cada id con su `.asset` |
| **DD6a-23** | P0 | El fondo de carga teñido por la hora y oculto cuando la escena entra (§9.4) | PlayMode: a las 22 h, `#050A1E` / `#06122A`; `display: none` con la escena |
| **DD6a-24** | P0 | El conjunto de tomas `it6a` (§15.2), con `ROWMATE_DATA_DIR` temporal y la hora forzada | Dos corridas iguales byte a byte |
| **DD6a-25** | P1 | La niebla común de §6.5 en los shaders de terreno, objetos y vegetación | Sonda a 200 m y 400 m contra la fórmula (± 2/255) |
| **DD6a-26** | P2 | Bruma baja (§6.5), capa lejana de nubes (§4.5) y relieve fino (§5). Pueden pasar a la 6b | Tomas a las 6 h |

---

## 15. Para el PO y para QA

### 15.1 Lo que toca producto (al PO, para la spec y el gate)

| ID | Qué | Por defecto | Tipo |
|---|---|---|---|
| **P6a-01** | Las píldoras del HUD con su variante `.hud` (§9.3): cambian a la vista trabajo, enfriamiento y otro | Se adopta | DV (diferencia visible) |
| **P6a-02** | El sol del mediodía a 12° y no arriba de todo como en el legacy (en vertical el legacy lo pone a 38°) | Se adopta | DV |
| **P6a-03** | "Hora escena" lleva la escena a la hora nueva en 0,6-2 s (§4.7); el legacy salta en el acto. El valor del panel cambia en el acto | Se adopta | DV (solo simulador) |
| **P6a-04** | La luna con fase real, espejada en el hemisferio sur (§4.3); el legacy pinta un disco lleno | Se adopta (es la Mejora EXR de F-SCN-10) | Agregado |
| **P6a-05** | La salida del inmersivo sigue en 640 ms con un retroceso corto de la cámara (§3.3); la toma aérea del completado, en la 6b | Se adopta | Aclaración |
| **P6a-06** | Q-61 con terreno: las posiciones, escalas y variantes del legacy no cambian. La cabaña elige hacia dónde crece y se corre como mucho 0,5 m (25 de 333); los pinos que tocan una cabaña se podan desde 3 m. Lo nuevo (juncos, rocas, bosque, matas) usa sus propias semillas | Se adopta | Precisión de Q-61 |
| **P6a-07** | Q-62 con fundido: las estrellas se apagan entre ambiente 0,85 y 0,80 (5:48-6:12 y 18:00-18:17), no de golpe | Se adopta | Precisión de Q-62 |
| **P6a-08** | Q-63: el sol se pone entre las 18:30 y las 19:33 y sale entre las 4:27 y las 5:30; la luna sale entre las 19:34 y las 20:36 y se pone entre las 3:24 y las 4:26 | Se adopta | Precisión de Q-63 |
| **P6a-09** | D-ART-08 | **No** (§13); volver a ofrecerlo en el gate de la 8 | Decisión del usuario |
| **P6a-10** | P-09: la lista de §12 | Se entrega; el usuario descarga antes de la 7 | Prerrequisito |

Para la demo de la spec: "Hora escena" a las 6, 12, 18 y 22 h, con lo que se ve en §15.2.

### 15.2 Qué tiene que verse (tomas `it6a`, a distancia 0 salvo lo dicho)

| Toma | Qué se ve |
|---|---|
| TV y TH, 12 h | **Sol** a (205,5; 278,2) en TV y (457; 85,3) en TH, radio 11,5, con *bloom*, debajo de la barra. Cielo `skyTop` plano arriba de los 24°. **Nubes**: 7, sin cambios con `d`. **Cordilleras**: la lejana con nieve a la izquierda y en el centro, el collado a la derecha. **Ladera** boscosa a la izquierda y pradera a la derecha. **Primer muelle**: a la izquierda, en (184,4; 369,8) en TV. **Cabaña**: el techo en (220,3; 361,7) en TV. **Camino de brillo** al centro. **Bote** provisorio en su zona |
| TV y TH, 18 h | Sol en el collado de la derecha, acimut +17,5° y elevación 1,5° (TV ≈ (335; 354)). Nubes encendidas por abajo y alpenglow en la nieve. Camino de brillo dorado a la derecha. Sin estrellas |
| TV y TH, 6 h | Resplandor detrás del macizo de la izquierda, sin disco. Estrellas tenues (0,14). Sin luces en las cabañas |
| TV y TH, 22 h | Luna con su fase en acimut −5,8° y 10,6° de elevación (TV ≈ (163; 288)). Las 80 estrellas con titileo. Ventanas encendidas. Brillo de luna en el agua |
| TV, 12 h con `d` = 150 | El muelle cerca, a la izquierda (57,5; 395,2): apoya en la playa, con los pilotes en el agua; nada flota |
| TV y TH, pausa a las 12 h | El velo de §9.1: el HUD igual, el disco de `PAUSA` |
| W1280, 12 h | El sol en (640; 149,4), debajo de la barra de 83,2 |
| `scene_view_test` (los 4 del lago) | 390 × 844 y 844 × 390 a las 12 y a las 23 h, sin errores (también sin shader avanzado) |
| Video de la hora | "Hora escena" de 12 a 18 h: el recorrido de 1,5 s, sin saltos de color ni de astro |
| Video de la entrada | El vuelo de 1,6 s y la cascada encima; la salida de 640 ms |

### 15.3 Para QA

- **Verificación en el build de la 5**: es lo primero, con el `RowMate.exe` del usuario cerrado (STATUS, "Al empezar la It. 6a"). No es del Designer.
- **Los AC visuales de la 6a** se miden en las tomas `it6a` con sondas:
  - el sol, la luna y el muelle a ± 2 dp;
  - colores a ± 3 por canal donde no hay degradé;
  - el horizonte de la respiración ≤ 3 dp en un video de 60 s en los cinco perfiles.
- **El presupuesto del HUD** no sube: el fondo de carga se oculta y los degradés van en la escena.
- **Los 60 fps** (regla 9): NO VERIFICABLE sin el teléfono; en Windows, el arnés `[PERF]` de la 5 con la escena.
- **La excepción contada de P6a-01**: el borde de la hueca contra su interior (2,93 a 3,02), como la pista al 30 %.

---

## 16. Biblias: qué se alinea después del gate

| Documento | Dónde | Qué |
|---|---|---|
| ART_BIBLE | §0.2 | D-ART-05, -06 y -08 resueltos |
| | §2.1 | `backdrop.*`: fondo de carga teñido por la hora |
| | §3.2 | Los degradés de pantalla en la escena |
| | §3.3 | Las píldoras del HUD con `.hud` (si P6a-01) |
| | §6.1 a §6.3 | §4 de este documento: R6a-01 a -04 y R6a-10 |
| | §7.1 y §7.2 | La niebla común y la composición del lago (R6a-07, -08) |
| | §7.7 | El agua degradada (R6a-09) |
| | §8.1 y §8.2 | La cámara y su respiración |
| | §9 | El reparto de la 6a |
| | §10.1 | El riesgo de las píldoras, cerrado |
| ANIMATION_BIBLE | §2.11 | La entrada y la salida con la escena y el velo |
| | §5.2 y §5.3 | El rolido de 0,15°, los puntos del vuelo y la salida de 640 (R6a-05, -06) |
| | §6 | Las nubes y las estrellas con sus ventanas |
| ASSET_SLOTS | §0.1, §1.2 a §1.4 | `vegetation.canopyFar` y lo del pack |
| | §2 | La lista definitiva de §12 (sale "preliminar") |
| | §7 | Las licencias leídas y la recomendación de §13 |

---

## 17. Verificación hecha

1. **Legacy, solo lectura**: lo de la cabecera. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó `strava_secrets.dart`.
2. **El generador**: `dartrandom.py` reproduce los cuatro vectores dorados del oráculo en la primera corrida. El paisaje del lago sale de él (§6.1).
3. **Los números** de §3 a §10 salen de los programas de la cabecera, en `<scratch>/it6a/designer-debt/`. Las fórmulas del legacy se copiaron de su código (`time_of_day.dart`, `sky_painter.dart`, `lake_environment.dart`, `water_painter.dart`, `scene_camera.dart`).
4. **El contraste de las píldoras**: con el método de `Contrast.cs` (composición en sRGB sin redondear), contra `glass.fallback` sobre blanco y contra `#2B3038`. Los valores de la base coinciden con los de 04-design-notes de la 4b §8.2 (2,850 · 2,629 · 2,624 · 2,407 · 3,154 · 2,944).
5. **Licencias** (§13): leídas en las páginas de ambientCG, Poly Haven, NASA SVS 4720 y las pautas de imágenes de la NASA.
6. **El repositorio**: se escribió este documento y se alinearon, por QA-V5-R2-01, [04-design-notes de la 5](../05-series-parciales/04-design-notes.md) (§5.2, la fila DD5-27 y "Cambios al iniciar la It. 6a") y [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) (§2.10 y "Cambios al iniciar la It. 6a"). No se tocó código, tokens, capturas, preferencias ni bases del usuario. **Unity no se abrió y dotnet no se corrió.** Rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) o `../rowmate-legacy` y el scratch como `<scratch>`. Sin *commits*.
