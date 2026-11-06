# It. 6a — Escena: Core de escena, cielo, luz, hora, Lago alpino y agua degradada: spec de producto

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-11-02 |
| Iteración | 6a · `06a-escena-lago` ([ROADMAP §2, It. 6a](../../ROADMAP.md)) |
| Estado | **Iteración cerrada por el PO el 2026-11-06, esperando el OK del usuario** (ver "Al cerrar la iteración", al final, y el [REPORT](REPORT.md)). **Spec del PO, corregida por el gate de armonía de QA** (ver "Cambios por gate QA" al final) **y por la verificación de QA, ronda 1** (ver "Cambios por verificación (ronda 1)", al final: la toma aérea detrás del diálogo opaco, DEMO-03, AC-SCN-11-02 y -11-04): un solo número por constante para WP-A. Las diferencias de arte DV-63 a DV-67 y las ampliaciones de DV-52, -53, -57 y -59 van al usuario en el gate (P-18; por defecto, se aceptan). Antes de construir encima va la deuda "al empezar la 6a" de la It. 5 (§5.1, §12 punto 1) |
| IDs dueños | **8 canónicos**: F-SCN-02, F-SCN-03, F-SCN-09, F-SCN-10, F-SCN-11, F-SCN-14, F-SCN-15, F-SCN-23 |
| También toca | F-SCN-01 (composición y ciclo de vida con el agua degradada, y el rendimiento; se cierra en la 6b); F-SCN-13 (el agua degradada; la elección del agua avanzada es de la 6b); F-SCN-19 y F-SCN-20 (la silueta y los objetos del lago; se cierran en la 8); F-SCN-21 (el lago es el escenario por defecto y el único; se cierra en la 8); F-WKT-13 (la capa de escena del inmersivo: la entrada con el vuelo de cámara, la salida, el velo de pausa); F-WKT-14 y F-WKT-17 (la fila de 200 píldoras, el HUD sobre la escena, el fundido al rotar y la zona del bote; §15); F-BLE-16 (`Hora escena`); F-APP-05 (reducir movimiento con la escena) |
| Criterios | **101 ID** = **78 nuevos** (SCN 77: F-SCN-02 8, -03 7, -09 6, -10 7, -11 7, -14 7, -15 8, -23 4, -01 14, -13 4, -19 2, -20 2, -21 1; WKT 1: F-WKT-17 1) + **23 de iteraciones anteriores** (§5.2: 11 que cambian; §5.3: 12 que se vuelven a verificar, en 11 filas porque AC-BLE-16-02 y -03 van juntos). Además: **26 casos de test legacy** (LT-62 a LT-87, §6) y **24 capturas, 7 referencias del legacy, 8 tomas de la demo y 11 videos** (§7) |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Las rutas `lib/...:línea`, `test/...` y `docs/...` de esta spec son relativas a ese repositorio, y `scene/` = `lib/features/workout/scene/`. Leídos enteros: `scene/scene_camera.dart`, `scene_state.dart`, `time_of_day.dart`, `environment.dart`, `environments/lake_environment.dart`, `scene_settings.dart`, `water_shader.dart`, `scene_view.dart`, `painters/sky_painter.dart`, `painters/water_painter.dart`, `painters/thumbnail_painter.dart`; `painters/shore_painter.dart:1-140` (luz, niebla, noche, pino, cabaña, muelle); `immersive_workout_screen.dart:40-120`; `workout_screen.dart:36-50`; `workout_provider.dart:50-90`, `:215-226`; `lib/core/dev/simulator_overlay.dart:60-160`; `test/scene/scene_camera_test.dart`, `scene_state_test.dart`, `time_of_day_test.dart`, `environment_test.dart`, `sky_shore_painter_test.dart`, `water_painter_test.dart`, `scene_view_test.dart`; `docs/superpowers/specs/2026-10-02-immersive-scene-2-5d-design.md`. Para la interpolación de colores, `Color.lerp` y `_lerpDouble` del SDK de Flutter que usa el oráculo (`<flutter-sdk>`, `sky_engine/lib/ui/painting.dart:424-454` y `lerp.dart:28-31`). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Insumos | [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) §3 (DC-39 a DC-44), §4.6 (Q-61 a Q-70) y §5 (R-04, R-12); inventario [immersive-scene](../../product/inventory/immersive-scene.md) (F-SCN-01 a -03, -09 a -15, -19 a -21, -23, §3 a §6); [ROADMAP](../../ROADMAP.md) §0 regla 9, §2 It. 6a, §5 y §6; [STATUS](../../STATUS.md); [REPORT de la It. 5](../05-series-parciales/REPORT.md) §7 y §9; [spec de la It. 4b](../04b-hud-exr/01-po-spec.md) (AC-WKT-13-06, -08, -12, AC-WKT-19-03, §13) y su [04-design-notes §8.2](../04b-hud-exr/04-design-notes.md); [spec de la It. 5](../05-series-parciales/01-po-spec.md) (§0.4, AC-WKT-13-15, AC-WKT-18-21); [ART_BIBLE](../../design/ART_BIBLE.md) §0.2, §5.2, §6, §7.1, §7.2, §7.7, §8, §9, §10.1; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §2.11, §2.12, §4.7, §5, §6; [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §0, §1.2 a §1.4, §5.2, §7, §8; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §7.4, §14, §20; [ADR 012](../../architecture/adr/012-paridad-numerica-dart.md), [ADR 015](../../architecture/adr/015-escena-3d.md); [LEGACY_MAPPING](../../architecture/LEGACY_MAPPING.md); [QA_PLAN_REVIEW](../../QA_PLAN_REVIEW.md) §4 |

---

## 0. Cómo leer esta spec

### 0.1 Identificadores

- **`AC-<ÁREA>-<NN>-<nn>`**: criterio `nn` de la ficha `F-<ÁREA>-<NN>`. Las fichas SCN empiezan en 01, salvo F-SCN-01, que sigue a AC-SCN-01-01 (el fondo provisorio de la 4a). AC-WKT-17-13 sigue a los 12 de la 4a y la 4b.
- **§5** repite IDs de iteraciones anteriores: los que **cambian** (valen los textos de §5.2) y los que se **vuelven a verificar** con la escena (§5.3). Si fallan, se reabren acá.
- **`LT-nn`**: casos de test legacy, con la numeración que viene de la It. 5 (terminó en LT-61).
- **`CAP-nn`**, **`LEG-nn`**, **`DEMO-nn`** y **`VID-nn`**: capturas de la app, imágenes de referencia dibujadas con el código del legacy, tomas de la demo y videos (§7), en `docs/migration/iterations/06a-escena-lago/snapshots/`.
- **`DV-nn`**: diferencias visibles con el legacy, a propósito (§11.1). Siguen la numeración de la It. 5 (terminó en DV-51).
- **`P-nn`**: preguntas abiertas de esta spec (§14).
- **`E-*`**: estados de prueba de la escena (§0.4).

### 0.2 Cómo se verifica cada criterio

Las marcas de la 4b ([01-po-spec §0.2](../04b-hud-exr/01-po-spec.md)): **[dotnet]**, **[Editor-EM]**, **[Editor-PM]**, **[Captura]**, **[Device-Win]**, **[Device-And]** y **[QA]**. `[PERF]` es una medida en el build de simulador en la PC, como en la It. 5. Si falta el módulo de Android o el teléfono, la parte de equipo queda **NO VERIFICABLE** con el motivo y no frena la iteración; el rendimiento en Android se arrastra a la It. 14 (ROADMAP §0, regla 9). Nunca se da por cumplido sin medirlo o verlo.

### 0.3 Notación

- Textos entre comillas invertidas: **exactos al carácter**.
- **Mundo** en metros, como el legacy: `x` lateral (0 = eje del bote, positivo a la derecha), `y` altura sobre el agua, `z` hacia adelante desde la cámara. "z de mundo" es la z absoluta desde el inicio del recorrido; `relZ = z de mundo − distancia de escena`.
- **Distancia de escena**: la que recorre el bote de la escena (F-SCN-03), propia; no es la del monitor ni la del HUD.
- **Hora** en decimales de 0 a 24 (`18,5` = 18:30). "Minuto de escena" = un minuto de esa hora.
- `smooth(t) = t²·(3 − 2t)`; `smoothstep(a; b; x) = smooth(clamp((x − a)/(b − a); 0; 1))`.
- Tamaños de pantalla en dp. Los del legacy y de sus tests: **V390** 390 × 844 y **H844** 844 × 390. Los de la app: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720.
- Los números de la paleta, de la luz y del paisaje son los del legacy y valen **[EXACTO]**. Lo marcado **[VISUAL]** es intención visual: se puede reemplazar por una versión realista si el elemento, su momento y su comportamiento se conservan (immersive-scene §0).

### 0.4 Estado de prueba

**Lo de las It. 4a a 5** ([4a §0.4](../04a-motor-hud-funcional/01-po-spec.md), [4b §0.4](../04b-hud-exr/01-po-spec.md), [5 §0.4](../05-series-parciales/01-po-spec.md)), sin cambios: reloj y planificador de prueba (la hora de pared arranca en `2026-10-28T13:00:00.000Z`, zona UTC−03:00: el reloj de pared marca `10:00`); el ciclo de vida de prueba; los remos `R-4` (split 125, 24 spm, 150 W: la escena va a 4,0 m/s) y sus variantes; las bases `F-4A` (con `Corta`, 30 s), `F-4B` (con `Serie larga`, 200 pasos) y `F-5` (con `Series 600`); el simulador sin ruido; idioma, letra y "reducir movimiento" de prueba; preferencias y bases en una carpeta de datos temporal (`ROWMATE_DATA_DIR`). **Nunca** el archivo de preferencias ni las bases del usuario (AC-HIS-20-04).

**Nuevo en la 6a** (ninguno existe en un build que se instala):

- **Hora de la escena**: la real de prueba (`10:00`) o forzada con `Hora escena` (`06:00`, `12:00`…) o con un control de prueba que la fija a cualquier minuto. Con el control de prueba la hora llega en el acto y los fundidos que dependen de la hora (estrellas, luces de la noche) quedan en su valor final, sin animar. La hora se arma como el legacy, `hora + minuto/60`.
- **Fecha de prueba**: la del reloj de prueba (28 de octubre de 2026). Con ella, la luna está gibosa menguante, ≈ 94 % iluminada (DV-57).
- **Región de prueba**: sin región (la luna como se ve desde el hemisferio norte, AC-SCN-10-07), salvo en la prueba del hemisferio sur, que fija una región del sur.
- **Tiempo de escena** fijo cuando la toma lo pide (`t = 3,2 s`, como `sky_shore_painter_test`): nubes y titileo de las estrellas quedan en ese instante.
- **Distancia de escena forzada**: **`E-0`** (el arranque), **`E-920`** (la cabaña a 40 m a la derecha y el muelle a 55 m a la izquierda: los dos hitos cerca, en los dos tamaños), **`E-1234`** (la de los tests legacy) y **`E-42K`** (42 195 m).
- **Escena sola**: la escena del inmersivo sin el HUD y sin los degradés de pantalla que van detrás del HUD (DV-67: son de la capa del HUD, ART_BIBLE §3.2), para compararla con el legacy, que se dibuja sin ellos.
- **Respiración en 0**: la cámara en la pose de contrato, quieta (lo mismo que hace "reducir movimiento" con la cámara).
- **Escena demorada**: la escena del lago tarda en estar lista lo que pida la prueba (AC-SCN-01-07).
- **Slot de prueba**: un archivo de prueba asignado a un slot del terreno, para comprobar que un asset entra por su slot sin el Terrain Sample Asset Pack (AC-SCN-15-08).

---

## 1. Objetivo y demo

**Objetivo.** El fondo provisorio del inmersivo se reemplaza por la primera escena 3D de la app: el **Lago alpino**, con el agua en su modo degradado (R-04). Tiene cielo con la hora del día (degradé, sol, luna, estrellas y nubes), luz que cambia con la hora real o con la que fuerza el simulador, y una orilla con terreno, pinos, muelles y cabañas en **las mismas posiciones que el legacy** (Q-61). Un bote provisorio avanza a la velocidad que dicta el split, con la cámara de persecución del legacy, que entra volando hasta el agua y "respira". Todo lo que es regla (cámara, velocidad, hora, paleta, astros, estrellas, nubes, orilla) es lógica pura probada fuera de Unity y comparada con el código del legacy.

**Demo (se le muestra al usuario antes de la It. 6b).** Con el build de simulador de Windows, en la ventana de 1280 × 720. Si Windows está en inglés, el HUD sale en inglés; la escena no tiene textos.

**(a) Remar en el lago** (3 minutos).

1. Workout → `Entrenamiento libre`. Workout funde a negro y la cámara baja volando desde atrás y arriba hasta quedar detrás del bote, a ras del agua (1,6 s), mientras el HUD llega en cascada. El reloj corre desde el toque.
2. El lago con la luz de la hora real de la PC: montañas al fondo (las lejanas con nieve), pinos en las dos orillas, el agua con sus líneas de brillo bajo el sol (o la luna). La cámara se mueve apenas, como si respirara.
3. Llave → `Fuerte`. El bote acelera en ≈ 1,5 s y el paisaje viene hacia la cámara: con `Fuerte` desde el arranque, el muelle (a 175 m) se ve a la izquierda desde los ≈ 34 s, sale del cuadro a los ≈ 38 s y pasa a la par del bote a los ≈ 39 s; la cabaña (a 360 m) pasa a la par por la derecha a los ≈ 79 s y se ve en los segundos de antes; con los 150 W del arranque, pasan a la par a los ≈ 48 s y ≈ 97 s. Las montañas no se deslizan de costado y las nubes solo se mueven con el viento.
4. `Pausa`: la escena se apaga un poco (menos color, un poco más oscura) y el bote se desliza ≈ 6 m hasta quedar quieto en ≈ 9 s; las nubes siguen. `Reanudar`: el bote vuelve a tomar velocidad.

**(b) La hora** (3 minutos). Llave → `Hora escena`, con `+` y `−`:

1. `18:00`: atardecer, luz cálida y baja, el sol a la derecha casi en el horizonte; aparecen las primeras estrellas, tenues, en un fundido corto. Cada cambio llega en 1,5 s, como un time-lapse.
2. `19:00` y `20:00`: el sol se esconde detrás de las montañas, sin saltar, y los brillos del agua se apagan con él; más tarde la luna sale por la izquierda, detrás del paisaje, y los brillos vuelven, plateados; las estrellas se encienden; la ventana de la cabaña se enciende.
3. `22:00`: noche con luna (con la fase del día) y 80 estrellas; las nubes casi no se ven.
4. `5:00` y `6:00`: amanecer violeta y naranja, bruma baja sobre el agua, las estrellas se van; `12:00`: mediodía azul, el sol centrado y alto.
5. `Hora real`: vuelve a la hora de la PC.

**(c) La rutina y el final** (1 minuto). Con una rutina corta (si no está, se crea en Rutinas: `Lago 1 min`, calentamiento de 20 s y trabajo de 40 s): al terminar sola, el diálogo de rutina completada aparece en el centro y, alrededor, la cámara se eleva y retrocede despacio durante 6 s: el horizonte sube, el lago se abre y el paisaje se va deteniendo mientras el bote frena. El diálogo es opaco hasta la 6b y tapa el bote (en la ventana de 1280 × 720 asoma la popa debajo); con el vidrio con desenfoque de la 6b, el bote se va a ver frenando detrás del diálogo. `Terminar`: el HUD se va, la cámara empieza a subir, funde a negro y vuelve Workout.

**(d) Las comparaciones y las pruebas.** En `snapshots/`: el lago a las 6, 12, 18 y 22 h lado a lado con la imagen dibujada por el código del legacy a la misma distancia y hora (CAP-01 a CAP-07 contra LEG-01 a LEG-07), con las marcas de las posiciones del legacy encima; el HUD y el velo de pausa sobre la escena en vertical y horizontal; los videos de la entrada, del bote avanzando, de la pausa, de la hora y del final. `dotnet test` y las pruebas de Unity en verde, con LT-62 a LT-87.

**Lo que no se ve todavía:** el agua con ondas, reflejos y estela (6b), el remero y los remos (7), los otros escenarios y el selector (8). El bote es provisorio: el casco solo.

**Lo que depende de vos:** el Terrain Sample Asset Pack es **opcional** (sin él, terreno y vegetación procedurales, AC-SCN-15-08). Sin el teléfono Android de gama media y su módulo, los 60 fps en Android quedan NO VERIFICABLE (AC-SCN-01-09).

---

## 2. Alcance

| ID | Funcionalidad | Qué se cierra en la 6a | Vuelve en |
|---|---|---|---|
| F-SCN-02 | Cámara de persecución | Completo: pose y proyección del legacy, recorte cercano, respiración, vuelo de entrada, salida y toma aérea | 7 (el cabeceo del casco que suma a la respiración); backlog (cámaras extra, BKL-02) |
| F-SCN-03 | Velocidad y distancia de la escena | Completo: velocidad por el split, inercia, frenada, paso acotado, distancia propia, `shaderDistance` como contrato | 6b (la estela usa la velocidad) |
| F-SCN-09 | Hora efectiva y paleta | Completo: hora real o forzada, paleta del legacy, luz por hora, luces nocturnas | — |
| F-SCN-10 | Sol, luna y fuerza de la luz | Completo: día y noche, astro del legacy, astro visible, puesta y salida graduales, luz y sombras, luna con fase | 6b (el brillo del astro en el agua avanzada) |
| F-SCN-11 | Cielo | Completo: degradé, 80 estrellas, sol y luna, 7 nubes | — |
| F-SCN-14 | Orilla determinista | Completo: segmentos, semilla, generador del legacy, rango visible, orden, tope y prioridad de hitos (con un escenario de prueba), olvido y regeneración | 8 (los topes 60 y 200 y la prioridad de hitos con la costa y la regata) |
| F-SCN-15 | Lago alpino | Completo: pinos, muelles, cabañas, tope, oleaje (dato), orilla con terreno, paisaje idéntico al legacy, slots | — |
| F-SCN-23 | Hora de escena forzada | Completo: efecto sobre la escena, transición, no se guarda, el reloj de pared no la sigue | — |
| F-SCN-01, -13, -19, -20, -21 (también tocan) | Composición, agua degradada, silueta, objetos, preferencia | Lo del lago: capas, ciclo de vida, rendimiento, agua degradada, cordilleras, pino, cabaña y muelle con luz, niebla y noche; el lago siempre | 6b (F-SCN-01, -13), 8 (F-SCN-19, -20, -21) |
| F-WKT-13, -14, -17 (también tocan) | Inmersivo, barra y HUD | La escena como fondo, la entrada con el vuelo, la salida, el velo sobre la escena, el fundido al rotar, el HUD legible sobre la escena, la fila de 200 píldoras con la escena | 6b (vidrio con desenfoque) |
| F-BLE-16 (también toca) | Panel del simulador | `Hora escena` mueve la luz de la escena | — |

---

## 3. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero indoor**, quiero ver un lago de montaña detrás de mis métricas, que avance a la velocidad que remo, para sentir que estoy en el agua. |
| H-02 | Como **remero que entrena seguido**, quiero que el recorrido sea siempre el mismo a la misma distancia (el muelle, la cabaña, los pinos), para reconocer "mi" lago, igual que en la app anterior. |
| H-03 | Como **remero que entrena a cualquier hora**, quiero que la luz, el cielo y el agua acompañen la hora real: mañana, mediodía, atardecer y noche con luna y estrellas. |
| H-04 | Como **remero que para**, quiero que el bote se deslice y se detenga como uno real, y que el paisaje se quede quieto cuando paro. |
| H-05 | Como **remero**, quiero que la escena nunca me tape las métricas ni las vuelva ilegibles, a ninguna hora. |
| H-06 | Como **desarrollador o en una demo**, quiero forzar la hora de la escena desde el simulador y ver cómo cambia la luz, sin esperar al reloj. |
| H-07 | Como **remero con un teléfono de gama media**, quiero que la escena vaya fluida durante todo el entrenamiento, aunque tenga que bajar detalle. |
| H-08 | Como **dueño del proyecto**, quiero que todo el arte sea gratis y se pueda cambiar por algo mejor sin tocar la lógica. |

---

## 4. Criterios de aceptación

### 4.0 Conteo

| Ficha | AC nuevos | Ficha | AC nuevos |
|---|---|---|---|
| F-SCN-02 | 8 | F-SCN-23 | 4 |
| F-SCN-03 | 7 | F-SCN-01 (también toca) | 14 |
| F-SCN-09 | 6 | F-SCN-13 (también toca) | 4 |
| F-SCN-10 | 7 | F-SCN-19 (también toca) | 2 |
| F-SCN-11 | 7 | F-SCN-20 (también toca) | 2 |
| F-SCN-14 | 7 | F-SCN-21 (también toca) | 1 |
| F-SCN-15 | 8 | F-WKT-17 (también toca) | 1 |

Total: **78 nuevos** + **23 de iteraciones anteriores** (§5.2: 11; §5.3: 12, con AC-BLE-16-02 y -03 en una fila) = **101 ID**.

### 4.1 F-SCN-02 — Cámara de persecución

- **AC-SCN-02-01 · La pose de contrato.** *Entonces* hay una sola cámara, detrás del bote, a **2,2 m** sobre el agua, **nivelada** (sin inclinación), con el remero (el centro del bote) a **9,5 m** hacia adelante. La línea del horizonte queda al **40 %** del alto si el ancho es menor o igual al alto, y al **42 %** si es mayor (un cuadrado cuenta como vertical). La focal es el **lado corto** de la pantalla. Valores: V390 horizonte 337,6 y focal 390; H844 163,8 y 390; TV 365,6 y 411; P360 320 y 360; TH 172,62 y 411; L640 151,2 y 360; W1280 302,4 y 720. El remero proyecta en el **0,504** del alto en TV y P360 (en vertical vale `0,40 + (ancho/alto)·2,2/9,5`: en V390, 0,507) y en el **0,652** en horizontal (`0,42 + 2,2/9,5`, en los cuatro tamaños): el centro de la zona del bote, ART_BIBLE §5.2. [dotnet] (LT-62) · [Editor-PM] · `scene/scene_camera.dart:7-16`
- **AC-SCN-02-02 · Lo que se ve es lo que proyecta el legacy.** *Entonces* un punto de mundo `(x, y, z)` con `z > 0,5` aparece en `x_pantalla = ancho/2 + focal·x/z` y `y_pantalla = horizonte + focal·(2,2 − y)/z`, con `focal/z` píxeles por metro; un punto sobre el agua se recupera con la inversa `z = focal·2,2/(y_pantalla − horizonte)`, `x = (x_pantalla − ancho/2)·z/focal`. Lo lejano converge al punto de fuga (centro, horizonte) y lo cercano es más grande (LT-63, LT-65). Además, **la cámara 3D de la app coincide con esas fórmulas**: con la respiración en 0, unas marcas puestas en el mundo en `(0; 0; 9,5)`, `(−4; 0; 6)`, `(0; 0; 30)`, `(7,5; 0; 120)` y `(3; 0; 200)` se ven en el píxel que dan las fórmulas, ±1 dp, en los siete tamaños (las que caen dentro de la pantalla: `(−4; 0; 6)` no entra en vertical). [dotnet] · [Editor-PM] · `scene_camera.dart:28-45`
- **AC-SCN-02-03 · Nada detrás de la cámara.** *Entonces* nada con `z ≤ 0,5 m` se proyecta ni se dibuja (LT-64), y los objetos de la orilla dejan la lista de visibles al pasar a menos de **1 m** de la cámara (`z de mundo < d + 1`, AC-SCN-14-04). Antes de eso ya salieron del cuadro por el costado: con la cámara del contrato, la línea de la orilla (`|x| = 9`) sale a `relZ` 8,1 m en TH, 8,3 en H844, 10,1 en L640 y W1280 y 18 en TV, P360 y V390, y la punta del muelle (`x = −6`) a 5,4 m en TH; durante el vuelo de entrada ningún objeto deja la lista estando a la vista. **Mejora EXR (DV-62)**: si algo de la orilla (del legacy o agregado) llegara a verse a menos de 2 m de la cámara, se desvanece con un tramado **entre 2 y 1 m** en lugar de cortarse en seco; la regla de visibles del legacy no cambia (LT-77). [dotnet] · [Editor-PM] · [Captura] VID-03 · `scene_camera.dart:19`, `:29`; `environment.dart:90`; `painters/shore_painter.dart:32-33`
- **AC-SCN-02-04 · La cámara respira (mejora EXR).** *Entonces*, sobre la pose de contrato, la cámara suma un movimiento sutil, determinista y sin azar (ANIMATION_BIBLE §5.2): lateral `0,035·sin(2π·0,071t) + 0,015·sin(2π·0,173t + 1,7)` m, vertical `0,025·sin(2π·0,093t + 0,4)` m, rolido **`0,15°`**`·sin(2π·0,061t + 2,1)` y cabeceo `0,10°·sin(2π·0,083t + 0,9)`, con `t` el tiempo de escena (la parte que viene del cabeceo, la arfada y el empuje del casco llega en la 7 y hoy vale 0). En pausa, ×0,6; con reducir movimiento, ×0. Al terminar el vuelo de entrada y cuando aparece la escena demorada, la respiración entra de a poco en **0,6 s** (sin salto de pose); durante la toma aérea se apaga (AC-SCN-02-07). **El horizonte nunca se aparta más de ±3 dp** de su línea de contrato en los siete tamaños (con 0,15°, como mucho 2,93 dp, en W1280; DT6a-01), y el remero nunca sale de la zona del bote. [dotnet] (los desvíos, barridos en 10 min de escena) · [Captura] VID-03 · ART_BIBLE §8.2
- **AC-SCN-02-05 · Entrar al agua volando (mejora EXR; AC-WKT-13-06).** *Cuando* se abre el inmersivo, *entonces* la escena aparece con la cámara ya en movimiento: arranca 23,5 m detrás del remero, 9 m arriba y 3 m al costado, mirando al bote, y llega a la pose de contrato en **1,6 s** (ANIMATION_BIBLE §5.3), siguiendo una curva suave, sin acelerones ni zoom brusco (como la bajada de un dron), sin pasar por debajo del agua ni a través de un objeto, y termina **exactamente** en la pose de contrato, sin salto en el último cuadro; desde ahí la respiración entra en 0,6 s (AC-SCN-02-04). El vuelo empieza a los 220 ms del toque (con la escena que aparece desde el negro, 220-400 ms) y termina a los 1 820 ms. Nada de la sesión espera al vuelo: el reloj y la escena (velocidad, distancia) corren desde el toque. Con reducir movimiento, sin vuelo: la cámara ya está en su lugar y todo entra con el fundido de 120 ms de la 4b. [dotnet] (el camino y su final) · [Editor-PM] · [Captura] VID-01, VID-02
- **AC-SCN-02-06 · Salir.** *Cuando* la sesión vuelve a reposo, *entonces* con la salida de la 4b (el HUD en 0-200, el negro en 200-400 y Workout en 400-640, AC-WKT-13-08), la cámara empieza a subir y a alejarse (1,5 m arriba y 4 m atrás en 1,2 s, ANIMATION_BIBLE §5.3) desde el primer cuadro, y el negro la cubre a los 400 ms: la salida sigue durando menos de 1 s. Con reducir movimiento, la cámara no se mueve. [Editor-PM] · [Captura] VID-07
- **AC-SCN-02-07 · La toma aérea del final (mejora EXR).** *Cuando* la rutina termina sola y entra el diálogo de rutina completada (a los 300 ms del fin, AC-WKT-21-01), *entonces* la cámara se eleva y retrocede despacio (1,7 m arriba y 1,7 m atrás: 0,4 m/s de promedio durante 6 s), mirando apenas hacia abajo (**4°**) para que el bote quede dentro de su zona, la respiración se apaga en esos mismos 6 s y la cámara se queda quieta; el bote frena con su inercia (AC-SCN-03-03) sin salir de su zona. En TV el horizonte termina en ≈ 337 dp (sube ≈ 29 dp) y la popa en ≈ 553, dentro de la zona. **El diálogo de completado de la 4b va encima, opaco y centrado** (el `AlertDialog` centrado y no descartable del legacy, `immersive_workout_screen.dart:207-212`; ART_BIBLE §3.5; con vidrio desde la 6b) y **en la 6a tapa el bote**: en TV, entero; en W1280 asoma la popa debajo. La toma se lee alrededor del diálogo (el horizonte que sube, el lago que se abre, el paisaje que se detiene). Que el bote quede dentro de su zona se mide con la cámara real (la pose y la popa proyectada), no a la vista; verlo frenando detrás del vidrio con desenfoque es de la 6b (§13). `Terminar` del diálogo hace salir desde donde está la cámara (AC-SCN-02-06). Solo con el diálogo de completado: al terminar a mano no hay toma aérea. Con reducir movimiento, la cámara no se mueve. [Editor-PM] · [Captura] CAP-22, VID-08
- **AC-SCN-02-08 · Una sola cámara.** *Entonces* no hay otros modos de cámara ni controles para moverla: ni toques, ni arrastre, ni rueda, ni teclado (Q-66; las cámaras extra son BKL-02). [Editor-PM] · spec legacy §Decisiones

### 4.2 F-SCN-03 — Velocidad y distancia de la escena

- **AC-SCN-03-01 · La velocidad sale del split.** *Entonces* la velocidad objetivo es **`500 / split`** m/s (split en segundos enteros por 500 m) **solo si** el entrenamiento está en marcha **y** el SPM es mayor que 0 **y** el split es mayor que 0; en cualquier otro caso es 0. Ejemplos: 1:47 (107 s) → 4,67 m/s; 2:05 (125 s, `R-4`) → 4,0 m/s; 2:00 → 4,17 m/s. Con SPM 0 y split mayor que 0 (el remero paró y el monitor repone el split medio) no hay avance (LT-66). [dotnet] · `scene/scene_state.dart:36-43`
- **AC-SCN-03-02 · La inercia.** *Entonces* la velocidad sigue a la objetivo con una constante de tiempo de **1,5 s**: en cada cuadro, `velocidad += (objetivo − velocidad)·mín(1; dt/1,5)`, y la distancia de escena suma `velocidad·dt`. Con 107 s y 24 spm, a los 8 s la velocidad es 4,67 ± 0,05 m/s y la distancia pasa los 25 m (LT-66). Con `R-4` desde el inicio, a los 60 s la escena recorrió **≈ 234 m** (± 0,5), mientras el monitor sumó 240 m (la banda dice `1240` `m`, porque el monitor ya traía 1000) (AC-SCN-03-05). [dotnet] · `scene_state.dart:16`, `:54-57`
- **AC-SCN-03-03 · Al parar, se desliza y se detiene.** *Entonces*, al pausar, al terminar (también con el diálogo de completado) o al dejar de remar, el objetivo es 0 y el bote frena con la misma inercia; cuando la velocidad baja de **0,01 m/s** pasa a ser **exactamente 0**. Con 107 s, después de 8 s remando, 6 s con split 0 dejan la velocidad por debajo de 0,1 m/s con la distancia todavía creciendo, y 10 s más la dejan en 0 (LT-67). Con `R-4`, `Pausa` a los 60,2 s: a los 2 s va a ≈ 1,05 m/s, se desliza ≈ 6 m y queda quieto a los ≈ 9 s. Tras 0,5 s remando, 2 s en pausa dejan la velocidad por debajo de 0,5 m/s (la parte de velocidad de `scene_state_test:60`, que se porta entero en la 7). Al reanudar, vuelve a acelerar con la misma inercia. [dotnet] · [Captura] CAP-20, VID-04 · `scene_state.dart:55-57`
- **AC-SCN-03-04 · Un cuadro lento no hace saltar la escena.** *Entonces* el paso de tiempo de cada cuadro se acota a **[0; 0,1] s**: después de un tirón, de volver del segundo plano o de despertar el equipo, la escena avanza como mucho 0,1 s y sigue desde donde estaba. Por debajo de 10 fps la escena corre más lenta que el reloj; el reloj y las métricas del HUD no cambian. [dotnet] · [Editor-PM] (ciclo de vida de prueba) · `scene_state.dart:52`; `scene/scene_view.dart:63`
- **AC-SCN-03-05 · Una distancia propia.** *Entonces* la distancia de escena se integra con la velocidad de escena y **no** se toma del monitor: puede diferir de los metros del HUD por la inercia, el split en segundos enteros y el paso acotado. Mueve la orilla y las líneas de brillo del agua; nunca se muestra, no se guarda y no toca la sesión, la telemetría, los parciales ni el paso por distancia. [dotnet] · [Editor-PM] · [Captura] CAP-13 · immersive-scene F-SCN-01
- **AC-SCN-03-06 · `shaderDistance`, como contrato.** *Entonces* la distancia acotada del legacy, `distancia mod 4096`, existe en la lógica con su test: 5000 → 904 (LT-68). El agua de la app no la usa (QA-P-28): con cualquier distancia, nada salta cada 4096 m (AC-SCN-01-15). [dotnet] · `scene_state.dart:33-34`
- **AC-SCN-03-07 · Las entradas.** *Entonces* la escena toma en cada cuadro el split y el SPM de **la última lectura**, los mismos que muestra el HUD, y "en marcha" es la fase del motor (en pausa, terminada y en reposo, no). Si el monitor deja de mandar lecturas (`Simular desconexión`, un corte), la escena sigue con la última lectura, como el legacy (el proveedor conserva el último dato); con `Remando` apagado (SPM 0) el bote frena. [dotnet] · [Editor-PM] (con `-Simulator`) · `scene_view.dart:65-70`; `immersive_workout_screen.dart:81-86`; `workout_provider.dart:220-224`

### 4.3 F-SCN-09 — Hora efectiva y paleta por hora

- **AC-SCN-09-01 · La hora de la escena.** *Entonces* la hora de la escena es la forzada del simulador si la hay (F-SCN-23); si no, la hora **local** del equipo con resolución de minuto, `hora + minuto/60` (los segundos se ignoran). Se vuelve a leer en todo momento: en un entrenamiento largo la luz cambia minuto a minuto, y un cambio de zona horaria o de hora del equipo se ve en el minuto siguiente. **Mejora (P2)**: cuando la hora real avanza un minuto, lo que se ve pasa del minuto viejo al nuevo en **1 s** lineal, así el astro no salta (el dato sigue siendo el del minuto); cualquier otro cambio de la hora real (zona, hora del equipo, volver del segundo plano) llega en el acto, y con reducir movimiento, todo en el acto. [dotnet] (con reloj de prueba) · [Editor-PM] · `scene_view.dart:81-86`
- **AC-SCN-09-02 · La paleta del legacy.** *Entonces* la paleta tiene los keyframes del legacy, sin cambios:

  | Hora | Momento | skyTop | skyHorizon | waterNear | waterFar | sunColor | sunGlow | fog | ambient |
  |---|---|---|---|---|---|---|---|---|---|
  | 5 | Amanecer | `#2A1B5E` | `#FF9A5C` | `#1A2B4A` | `#3A2F55` | `#FFB070` | `#FF7A3D` | `#C98A7A` | 0,70 |
  | 7 | Mañana | `#3A7BD5` | `#FFD9A0` | `#1E5A9E` | `#5C8FC9` | `#FFE4A0` | `#FFC46B` | `#DCE8F5` | 0,95 |
  | 12 | Mediodía | `#0B4FA8` | `#8FD3FF` | `#0E4F8F` | `#2F8FD6` | `#FFF6D5` | `#FFE9A6` | `#CFE9FF` | 1,00 |
  | 18 | Atardecer | `#2E3C8C` | `#FF8C5A` | `#163A66` | `#7C5C7A` | `#FFB36B` | `#FF6B3D` | `#E0A28A` | 0,85 |
  | 20 | Anochecer | `#0D1A4A` | `#4A3A7A` | `#0B1E3A` | `#2A2A55` | `#DDE6FF` | `#8FA3FF` | `#30335A` | 0,50 |
  | 22 a 4 | Noche | `#050A1E` | `#121C3A` | `#06122A` | `#0E1E3C` | `#E6EEFF` | `#A6B8FF` | `#0F1730` | 0,35 |

  - Entre dos keyframes, interpolación lineal: cada canal de color en punto flotante, sin redondear a 8 bits, con `a·(1 − t) + b·t` recortado a [0; 1] (el `Color.lerp` del Flutter del legacy; con la paleta del legacy el recorte no cambia ningún valor), y el ambiente con `a + (b − a)·t`.
  - `h = hora mod 24`; si `h < 5`, `h + 24`. Keyframes en 5, 7, 12, 18, 20, 22, 28 (= 4 h, noche) y 29 (= 5 h, amanecer): la noche queda **cerrada de 22 a 4 h** y de 4 a 5 h pasa a amanecer, así la medianoche no es un falso amanecer.
  - La paleta es continua: 23:59 ≈ 0:00 y 4:59 ≈ 5:00 (± 0,01 de ambiente).
  - Los 8 campos coinciden con el oráculo (el código del legacy) en cada minuto del día, con error ≤ 1e-9 (LT-70 a LT-72, LT-74).

  [dotnet] · `scene/time_of_day.dart:29-38`, `:69-116`
- **AC-SCN-09-03 · Ambientes de referencia.** *Entonces* el ambiente vale 0,35 de 0 a 4 h; 0,525 a las 4:30; 0,825 a las 6; 0,97 a las 9; 0,925 a las 15; 0,7625 a las 18:30 (`0,85 + (0,5 − 0,85)·0,25`); 0,675 a las 19; 0,425 a las 21. [dotnet] · immersive-scene F-SCN-09
- **AC-SCN-09-04 · Una sola paleta manda.** *Entonces* todos los colores de la escena salen de la paleta de la hora y de nada más: el degradé del cielo (skyTop, skyHorizon), el agua (waterNear, waterFar), el disco del astro (sunColor), su halo (sunGlow), la niebla (fog), la luz de la orilla y del bote (ambient), el color de las cordilleras (y su nieve) y de las nubes, y cuánto se ven las estrellas (que son **blancas**, como en el legacy: de la paleta sale su opacidad, por el ambiente). No hay una segunda paleta que pueda desincronizarse: al cambiar la hora, todo cambia en el mismo cuadro. [dotnet] · [Captura] CAP-01 a CAP-04 · ART_BIBLE §6.1
- **AC-SCN-09-05 · La luz de cada hora (mejora EXR).** *Entonces* la luz de la escena (color e intensidad de la luz principal, luz ambiente del cielo, sombras, niebla, exposición, balance de blancos, saturación y brillo) toma en los keyframes los valores de ART_BIBLE §6.3 y se interpola linealmente entre ellos, derivada de la misma paleta: dorada y con sombras muy largas al amanecer, dura y con sombras cortas al mediodía, cálida al atardecer y azul de noche. La escena se ve "fotográfica" a cualquier hora y ninguna hora se ve plana salvo el mediodía. Lo revisan QA y el Designer en las capturas, lado a lado con el legacy (LEG-01 a LEG-04: el tono dominante de cada hora es el mismo). [dotnet] (los valores en los keyframes y la interpolación) · [Captura] CAP-01 a CAP-04, VID-05, VID-06 · ART_BIBLE §6.3
- **AC-SCN-09-06 · Las luces de la noche.** *Entonces* las luces nocturnas del lago (la ventana de la cabaña) están encendidas con ambiente **menor a 0,6** (de ≈ 19:26 a ≈ 4:43) y apagadas el resto del día, como el legacy. Encendidas, no se tiñen con la luz ambiente ni se pierden en la niebla: brillan (`#FFE082`, su color antes del ajuste de color de la hora, que vale para toda la imagen, como los colores de referencia de AC-SCN-15-06). **Mejora EXR (DV-60)**: cada luz se enciende (al anochecer) o se apaga (al amanecer) en su propio minuto, fijo, dentro de los 20 min de escena alrededor del umbral (≈ 19:16 a 19:36 y ≈ 4:33 a 4:53), con un fundido de 0,4 s; fuera de esa ventana, el estado es el del legacy. [dotnet] · [Captura] CAP-04, CAP-09, VID-05 · `painters/shore_painter.dart:26`, `:121-122`; ANIMATION_BIBLE §2.12

### 4.4 F-SCN-10 — Sol, luna y fuerza de la luz

- **AC-SCN-10-01 · Día y noche.** *Entonces* la elevación del sol es `sin((hora − 6)/12·π)` y es **noche** (se ve la luna en lugar del sol) cuando es **≤ −0,4**: de 19:34:19 a 4:25:41 (≈ 19:34 a 4:26). A las 19 h todavía es día (atardecer); a las 20 h ya es noche; a las 5 h es día y no hay luna; a las 12 es día; a las 23 es noche (LT-69, LT-70, LT-73). [dotnet] · `time_of_day.dart:52-55`
- **AC-SCN-10-02 · El astro del legacy.** *Entonces* la posición de pantalla del legacy se calcula igual, como referencia y contrato: de noche se usa la hora espejada `(hora + 12) mod 24`; la elevación es `sin((h − 6)/12·π)` acotada a [0; 1]; `x = ancho·clamp(0,25 + 0,5·(h − 6)/12; 0,15; 0,85)`; `y = horizonte − elevación·horizonte·0,9`. Valores (fracción del ancho; elevación): 5:00 sol 0,208, 0; 9:00 sol 0,375, 0,707; 12:00 sol 0,500, 1,0; 18:00 sol 0,750, 0; 19:00 sol 0,792, 0; 20:00 luna 0,333, 0,5; 0:00 luna 0,500, 1,0; 4:00 luna 0,667, 0,5. A las 12, `y < 0,2·horizonte` en V390 (LT-69); a las 23, la luna queda arriba del horizonte (LT-70). Coincide con el oráculo en cada minuto y en los siete tamaños (error ≤ 1e-9). [dotnet] · `time_of_day.dart:57-67`
- **AC-SCN-10-03 · La fuerza de la luz.** *Entonces* la fuerza del brillo es **0,45** de noche y `clamp(elevación·1,2; 0,2; 1,0)` de día: 1,0 entre ≈ 9:46 y ≈ 14:14 y nunca menos de 0,2 de día (5:00 → 0,2; 9:00 → 0,85; 12:00 → 1,0; 18:00 → 0,2). Es la que usan las líneas del agua degradada (AC-SCN-13-03); la luz de luna de la escena puede variar con la fase (AC-SCN-10-07), esta no. [dotnet] · `time_of_day.dart:64`
- **AC-SCN-10-04 · El astro que se ve (D-ART-05; DV-52).** *Entonces* el disco del sol (o de la luna) va donde fija ART_BIBLE §6.2, con la misma progresión que el legacy: acimut `(x_fracción − 0,5)·70°` (positivo a la derecha) y elevación sobre el horizonte **`1,5° + 10,5°·elevación`** (antes de la puesta, AC-SCN-10-05). Ejemplos: 6:00 a la izquierda (−17,5°) a 1,5°, detrás del macizo de la izquierda (con las cordilleras de AC-SCN-19-01, el borde asoma a las ≈ 6:53 en vertical y a las ≈ 6:28 en horizontal, y el disco sale entero a las ≈ 7:59 y a las ≈ 7:31); 9:00 a −8,75° y **8,92°**; **12:00 centrado (0°) y lo más alto (12°)**; 15:00 a +8,75° y 8,92°; 18:00 a la derecha (+17,5°) en 1,5°; la luna a las 22:00 a −5,83° y **10,59°** y a las 0:00 centrada y a 12°. Siempre queda dentro del cuadro en los siete tamaños, del mismo lado y en el mismo orden que en el legacy (a la izquierda a la mañana, centrado al mediodía, a la derecha a la tarde; la luna, igual con la hora espejada), y al mediodía el sol queda debajo de la barra superior en los cinco tamaños de la app. [dotnet] · [Captura] CAP-01 a CAP-04, CAP-10, CAP-11
- **AC-SCN-10-05 · Salir y ponerse sin saltos (Q-63; DV-54).** *Entonces*:
  - el sol se pone de a poco detrás del paisaje entre ≈ 18:30 y 19:33 y la luna sale de a poco entre ≈ 19:34 y 20:36; al amanecer, al revés (la luna se pone entre ≈ 3:24 y 4:25 y el sol sale entre ≈ 4:27 y 5:30), con los umbrales de día y noche del legacy sin cambios;
  - **el cambio de sol a luna (19:34) y de luna a sol (4:26) nunca se ve**: en ese minuto los dos astros están por debajo del horizonte (≤ −2,5°, detrás del paisaje);
  - mientras un astro se ve, se mueve de forma continua: de un minuto de escena al siguiente no se corre más de **0,3°** (DT6a-02; con el arco de AC-SCN-10-04, lo más rápido es ≈ 0,26°/min, como el sol real);
  - la luz principal tampoco salta: cambia de dirección solo cuando su intensidad vale **0** (sin astro no hay luz principal ni sombras), a las 19:34 y a las 4:26.

  El legacy salta de golpe del sol en el horizonte a la derecha (x ≈ 0,82) a la luna al 40 % de altura a la izquierda (x ≈ 0,32). [dotnet] (barrido minuto a minuto) · [Captura] CAP-08, CAP-09, VID-05, VID-06 · `time_of_day.dart:53-55`; ART_BIBLE §6.2
- **AC-SCN-10-06 · Luz y sombras coherentes (D-ART-05).** *Entonces* la luz principal viene del mismo acimut que el astro que se ve, con una elevación propia (sol: `6° + 49°·elevación`, 55° al mediodía; luna: `10° + 40°·elevación`), así las sombras son cortas al mediodía y largas al amanecer y al atardecer, y caen hacia la cámara y **del lado opuesto al astro** (la regla del legacy para la sombra del bote). La luz viene de adelante: contraluz con el camino de brillo hacia la cámara. [dotnet] · [Captura] CAP-01, CAP-03 · ART_BIBLE §6.2
- **AC-SCN-10-07 · La luna (DV-57).** *Entonces* de noche la luna está siempre en el cielo, en el lugar que le da la hora espejada (AC-SCN-10-04), con un disco de **0,8 veces** el del sol (1,28° de radio) y un halo de **4 veces su disco** (0,8 del halo del sol; DV-63: el legacy le da a la luna el mismo halo que al sol). Muestra la **fase real** del día del equipo (luna nueva de referencia 2000-01-06 18:14 UTC, mes sinódico 29,530588853 días; ART_BIBLE §6.3): la parte iluminada con su terminador y la parte oscura apenas visible (luz cenicienta), así el disco se ubica aun en luna nueva; el halo es más tenue cuanto menos iluminada está (× `0,4 + 0,6·iluminación`). El lado iluminado es el que se ve desde el hemisferio de la región del equipo: en una región del hemisferio sur, espejado respecto del norte; sin región, el del norte. Con la fecha y la región de prueba (§0.4), gibosa menguante, ≈ 94 % iluminada, con la parte oscura a la derecha (con una región del sur, a la izquierda). La luz de luna de la escena crece con la parte iluminada; la fuerza del brillo en el agua sigue siendo 0,45 (AC-SCN-10-03). El legacy dibuja siempre el disco entero, sin sombra, con el halo a pleno. Si el usuario rechaza DV-57 en el gate: disco entero y halo a pleno siempre, en los dos hemisferios (P-03). [dotnet] (la fase en fechas conocidas; el espejo del sur; el factor del halo) · [Captura] CAP-04, CAP-10 · `painters/sky_painter.dart:71-86`

### 4.5 F-SCN-11 — Cielo

- **AC-SCN-11-01 · Las capas del cielo.** *Entonces*, de atrás hacia adelante: el degradé, las estrellas, el sol o la luna, las nubes y la silueta lejana del escenario (F-SCN-19). Las nubes pasan delante del astro y las montañas tapan al astro y a las nubes que quedan detrás de ellas. [Editor-PM] · [Captura] CAP-01 a CAP-04 · `sky_painter.dart:36-56`
- **AC-SCN-11-02 · El degradé (DV-66).** *Entonces* el cielo va de **skyTop en el borde de arriba** de la pantalla a **skyHorizon junto al horizonte** [VISUAL], en la fracción de cielo visible de cada ventana (como el legacy):
  - la luz se junta cerca del horizonte: a un cuarto de la altura del cielo el color ya hizo el 44 % del camino hacia skyTop (el legacy, el 25 %) y la mitad de arriba es casi skyTop;
  - en el último 1,5° sobre el horizonte, una franja de bruma (skyHorizon mezclado al 50 % con fog) cose el cielo con las montañas y el agua, sin costura;
  - en la hora dorada, un resplandor de sunGlow alrededor del sol, aunque esté detrás del paisaje.

  La curva del degradé vale skyTop en el borde de arriba y skyHorizon en el horizonte (antes de la bruma): los dos tonos de la paleta en los mismos lugares que el legacy, **antes del ajuste de color de la hora** (como `#FFE082` en AC-SCN-09-06 y AC-SCN-20-02). La dispersión lo puede enriquecer sin cambiar esos dos extremos; el ajuste de color de la hora (AC-SCN-09-05), que vale para toda la imagen, sí los corre un poco en la imagen final (en las tomas, ≤ 17/255 por canal en el borde de arriba: 17 a las 6 h, 12 a las 18, 7 a las 12 y 0 a las 22), con el mismo tono dominante. Se mide en la escena sola, sin los degradés del HUD (DV-67). [dotnet] (los dos extremos de la curva y que el tono avanza siempre en el mismo sentido) · [Captura] las sondas de contrato (sin el ajuste de la hora) en CAP-02, arriba y a media altura (04-design-notes §10.4); CAP-01 a CAP-07 contra LEG-01 a LEG-07 a la vista, con el tono dominante de AC-SCN-09-05 · `sky_painter.dart:39-48`
- **AC-SCN-11-03 · Las 80 estrellas del legacy.** *Entonces* las estrellas son **80**, las del generador del legacy con semilla **42**: para la estrella `i` (de 0 a 79), en orden, `x = u₁·ancho`, `y = u₂·horizonte·0,85` y `radio = 0,6 + u₃` dp, con `u₁`, `u₂`, `u₃` los tres valores siguientes del generador. Quedan en el 85 % de arriba del cielo, con radios de 0,6 a 1,6 dp, siempre las mismas para un tamaño de pantalla. La primera, en V390, está en (58,86; 173,37). Con la respiración en 0, cada estrella está en su lugar ± 1 dp; las 80 coinciden con el oráculo en los siete tamaños. Un campo de estrellas más tenue además de las 80 se admite como mejora EXR, solo en el nivel Alto (P-10), con la misma regla de cuándo se ven y nunca más brillante que ellas. [dotnet] · [Captura] CAP-10 · `sky_painter.dart:58-64`
- **AC-SCN-11-04 · Cuándo y cuánto brillan (Q-62; DV-53).** *Entonces* la oscuridad es `clamp((1 − ambiente)/0,65; 0; 1)` y cada estrella tiene opacidad `(0,4 + 0,5·titileo)·oscuridad`, con `titileo = 0,5 + 0,5·sin(2t + i)` (su propio desfase). **Con ambiente mayor a 0,85 no hay estrellas**: con la hora al minuto armada como el legacy (`hora + minuto/60`), son **visibles hasta las 6:12 inclusive** y **desde las 18:00** (en los dos minutos el ambiente vale 0,85 exacto, oscuridad 0,23); **a las 6:13 y a las 17:59, ninguna** (0,8521 y 0,8504). A las 19 h están las 80, tenues (LT-82); llegan a su máximo con la noche cerrada (ambiente 0,35, oscuridad 1). A las 15 h no hay ninguna; el legacy dibuja 80 casi invisibles (oscuridad 0,115) a toda hora salvo las 12. **Mejora (DV-53)**: cuando el conjunto cruza el umbral (al anochecer o al amanecer, con la hora real o con el time-lapse de `Hora escena`), entra o sale con un **fundido de 0,9 s** en el tiempo del cuadro, en lugar de aparecer en un cuadro; en régimen, la opacidad es la de la fórmula. Al abrir la escena, al volver del segundo plano y con la hora fijada por una prueba, en su valor final, sin fundido. El fundido sigue con reducir movimiento (no es movimiento). Titilan también en pausa. [dotnet] (los bordes 6:12, 6:13, 17:59 y 18:00; el fundido) · [Captura] CAP-08, CAP-11, VID-05, VID-06 · `sky_painter.dart:49-52`, `:65-66`
- **AC-SCN-11-05 · El sol y la luna en el cielo [VISUAL] (DV-63).** *Entonces* el astro es un disco de color sunColor con un halo de sunGlow **cuatro veces más ancho que el disco** (opaco al 70 % en el centro, al 25 % a mitad de camino y transparente en el borde): las proporciones y los alfas del legacy.
  - El disco del sol mide **1,6° de radio**: 21,8 dp de diámetro en V390 y H844, 23 en TV y TH, 40 en W1280; el halo llega a 6,4° (≈ 87 dp de diámetro en V390).
  - Brilla como un sol: en los niveles Alto y Medio, con su resplandor, a pleno sol se lee de ≈ 40 dp en TV (lo que pasa de 0,9 de luminancia mide ≥ 30 dp de diámetro en Alto, a las 12 h); en Bajo, plano.
  - La luna, **0,8 veces** el disco del sol (1,28°) y su halo, 4 veces su disco (AC-SCN-10-07).

  El legacy dibuja un disco plano de 44 dp de diámetro y un halo de 176 dp en cualquier tamaño, y a la luna le da el mismo halo que al sol. [dotnet] (las proporciones: halo = 4 × disco; luna = 0,8 × sol) · [Captura] CAP-02, CAP-10 (con la sonda del brillo) · `sky_painter.dart:71-86`
- **AC-SCN-11-06 · Las 7 nubes (D-ART-06; DV-56).** *Entonces* hay 7 nubes con los datos del legacy (x inicial; y relativa; tamaño en dp; velocidad propia): (0,08; 0,10; 34; 1,0), (0,26; 0,18; 22; 1,4), (0,42; 0,07; 40; 0,8), (0,58; 0,22; 26; 1,2), (0,72; 0,12; 36; 0,9), (0,88; 0,20; 24; 1,3), (0,97; 0,05; 30; 1,1).
  - Se mueven **solo con el viento**: `x = ((x₀ + t·0,004·v) mod 1,2)·ancho − 0,1·ancho`, con `t` el tiempo de escena; salen por la derecha y vuelven por la izquierda (una vuelta cada 214 a 375 s). **No se corren por avanzar**: el legacy suma `distancia·0,0004·v`, que la lógica conserva como contrato del legacy. **Mejora (DV-56)**: en la vuelta (a ± 0,1·ancho fuera de la pantalla, donde las más grandes todavía asoman) la nube se desvanece antes de saltar y entra con un fundido por el otro lado, en lugar de cortarse en un cuadro; la `x` sigue siendo la del legacy.
  - `y = horizonte·(0,12 + y_relativa·0,6)`.
  - El cuerpo es blanco mezclado al 25 % con skyHorizon, con opacidad `0,08 + 0,5·ambiente²` (0,58 al mediodía, 0,14 de noche: casi invisibles); la sombra de abajo, skyTop al `0,12·ambiente`. [VISUAL]: cúmulos de formas suaves, cada una con el tamaño del legacy.
  - Siguen en pausa. Una capa de nubes de fondo (cobertura 0,35 en el lago, ART_BIBLE §7.2) se admite como mejora EXR, con la misma opacidad por hora.

  [dotnet] · [Captura] CAP-02, VID-03 · `sky_painter.dart:29-33`, `:88-103`
- **AC-SCN-11-07 · El cielo y la orilla se dibujan sin errores.** *Entonces* el lago en V390 y H844, a las 12 y a las 23 h, a distancia 0 y 1234 m y con tiempo de escena 3,2 s, se dibuja sin excepciones ni errores en el registro (LT-78 a LT-81). [Editor-PM] · `test/scene/sky_shore_painter_test.dart:16`

### 4.6 F-SCN-14 — Orilla: segmentos deterministas, visibilidad y tope

- **AC-SCN-14-01 · Segmentos de 50 m, siempre iguales.** *Entonces* el recorrido se divide en segmentos de **50 m**: el segmento `k` cubre la z de mundo de `50k` a `50(k + 1)`. Sus objetos (tipo, x, z, escala —1 por defecto— y variante) salen del generador del legacy con la semilla **`índice del escenario·1 000 003 + k·7 919 + 17`** (el lago es el escenario 0). El mismo escenario da los mismos objetos a la misma distancia en cada sesión, se pidan en el orden que se pidan, y dos segmentos distintos no son iguales (LT-75). [dotnet] · `scene/environment.dart:43`, `:72-82`
- **AC-SCN-14-02 · El generador del legacy, con vectores dorados (Q-61).** *Entonces* el generador reproduce el `math.Random(seed)` de Dart:
  - `Random(42)`: `nextDouble()` → `0.15092545597797424`, `nextDouble()` → `0.6041479725361217` y `nextInt(1000)` → `77`;
  - semilla del segmento 7 del lago (`55 450`): el primer `nextDouble()` → `0.854835531354132`;
  - con las secuencias del oráculo para otras semillas, bit a bit.

  Con eso, los objetos de los segmentos 0 a 2 000 del lago (100 km) coinciden **bit a bit** con los que da el código del legacy en el oráculo. [dotnet] · ADR 012; QA_PLAN_REVIEW §4
- **AC-SCN-14-03 · Dentro del segmento y en la orilla.** *Entonces* en los segmentos 0 a 29 ninguno está vacío, todo objeto tiene su z dentro de `[50k; 50(k + 1)]` (inclusive) y todo lo que no es puente ni boya queda en la orilla, con `|x| ≥ 9 m`: el canal tiene 18 m de ancho (LT-76). No hay segmentos negativos. [dotnet] · `environment.dart:45-46`
- **AC-SCN-14-04 · Lo que se ve.** *Entonces* desde una distancia `d` se ven los objetos con z de mundo entre **`d + 1`** y **`d + 400`** (inclusive), ordenados **de lejos a cerca**. A 1234 m, a lo sumo el tope del escenario, en orden de z decreciente y con z entre 1235 y 1634 (LT-77). Las listas a 0, 1, 49,5, 50, 920, 1234, 5000 y 42 195 m coinciden con el oráculo (a igual z, el orden no importa). [dotnet] · `environment.dart:47`, `:89-101`
- **AC-SCN-14-05 · El tope y los hitos.** *Entonces*, si los objetos visibles superan el tope del escenario:
  1. se conservan todos los que no son boyas (los hitos);
  2. si sobra lugar, se completa con **las boyas más cercanas**;
  3. si sin boyas todavía se supera el tope, se descartan los hitos **más lejanos**;
  4. el resultado se vuelve a ordenar de lejos a cerca.

  Como el lago no tiene boyas ni llega a su tope, la regla se prueba con un escenario de prueba (con muchas boyas y con muchos hitos) y da lo mismo que la función del legacy en el oráculo. Los topes de la costa (60) y la regata (200) y su test (`environment_test:54`) son de la It. 8. [dotnet] · `environment.dart:50`, `:102-113`
- **AC-SCN-14-06 · Lo de atrás se olvida y vuelve igual.** *Entonces* los segmentos que quedaron atrás se olvidan, y si se vuelven a necesitar se regeneran idénticos. En una sesión de 42 km la memoria del paisaje no crece con la distancia. [dotnet] · `environment.dart:94`, `:116-120`
- **AC-SCN-14-07 · Nada aparece de golpe (mejora EXR; DV-62).** *Entonces* un objeto que entra en el rango visible aparece con un fundido entre los 400 y los 360 m, ya casi perdido en la niebla (AC-SCN-20-01), y uno que pasa la cámara se va con el tramado de AC-SCN-02-03. Ningún objeto aparece ni desaparece de golpe en el cuadro. [Editor-PM] · [Captura] VID-03 · ART_BIBLE §7.1

### 4.7 F-SCN-15 — Escenario Lago alpino

- **AC-SCN-15-01 · Agua calma.** *Entonces* el lago tiene amplitud de ola **0,25** y separación de crestas **2,5 m** (los usa el agua avanzada de la 6b), y su agua no se tiñe: los colores son los de la paleta. [dotnet] · `scene/environments/lake_environment.dart:14-16`; `environment.dart:63`
- **AC-SCN-15-02 · Los pinos.** *Entonces* en cada segmento hay pinos en las dos orillas, primero la izquierda y después la derecha: el primero en `z₀ + u·6` y los siguientes cada `6 + u·6` m (de 6 a 12 m) mientras `z < z₀ + 50`; `|x|` de 10 a 16 m (`9 + 1 + u·6`); escala de 0,7 a 1,3 (`0,7 + u·0,6`); variante de 0 a 999 (`nextInt(1000)`), que elige una de 3 paletas de verdes (variante mod 3). Los valores salen del generador en ese orden: z inicial; por cada pino, x, escala, variante y el paso a z siguiente. Todos los segmentos tienen pinos. [dotnet] · `lake_environment.dart:22-34`
- **AC-SCN-15-03 · Muelles y cabañas, en los metros exactos.** *Entonces* hay un **muelle** en la orilla izquierda (`x = −9`) a `z₀ + 25` cuando `k mod 8 = 3`: a **175, 575, 975, 1 375…** m (cada 400 m). Hay una **cabaña** en la orilla derecha (`x = +13`) a `z₀ + 10` cuando `k mod 12 = 7`: a **360, 960, 1 560…** m (cada 600 m), con variante `k`. [dotnet] · `lake_environment.dart:35-38`
- **AC-SCN-15-04 · El tope del lago.** *Entonces* nunca se ven más de **120** objetos del legacy a la vez (en el lago se ven ≈ 90 a 100). [dotnet] · `environment.dart:50`
- **AC-SCN-15-05 · El mismo paisaje que el legacy (Q-61).** *Dado* el mismo tamaño de pantalla y la misma distancia de escena, *entonces*:
  - están **los mismos objetos** que en el legacy, con el mismo tipo, x, z, escala y variante (AC-SCN-14-02);
  - con la respiración en 0, **el eje de cada objeto cae en la misma x de pantalla que en el legacy, ± 2 dp**, en V390, H844 y los cinco tamaños de la app (la x de pantalla no depende de la altura);
  - su base puede quedar más arriba que en el legacy **solo** por la altura del terreno donde se apoya (a lo sumo 2,5 m de mundo), nunca más abajo del agua;
  - las nubes, el astro y las montañas no coinciden a propósito (D-ART-06, D-ART-05, Q-65; y el tamaño del astro, la nieve y el alto de las montañas en vertical, DV-63 a DV-65).

  El usuario y QA lo ven en CAP-02, CAP-05 y CAP-07, cada una lado a lado con LEG-02, LEG-05 y LEG-07 y con una marca en la base de cada objeto del legacy encima: cada pino, el muelle y la cabaña sobre su marca. [dotnet] · [Editor-PM] (las bases medidas en la escena) · [Captura] · Q-61 revisada
- **AC-SCN-15-06 · Pino, cabaña y muelle [VISUAL con medidas].** *Entonces* los objetos del lago tienen el tamaño y el aspecto del catálogo del legacy, en 3D:
  - **pino**: alto total **6,0 m × escala** (± 10 %), ancho máximo ≈ 2,6 m × escala, tronco `#5D4037`, follaje en pisos con la paleta de su variante (mod 3: [`#1B5E20`, `#2E7D32`, `#388E3C`], [`#145A32`, `#1E8449`, `#27AE60`] o [`#2E5E2A`, `#3E7B38`, `#4F9A48`]) como tonos dominantes; silueta irregular, no conos perfectos;
  - **cabaña**: cuerpo de 4 × 2,6 m, techo a dos aguas hasta 4,2 m con alero, puerta de 0,8 × 1,4 y ventana de 0,8 × 0,7 (± 10 %); madera `#6D4C41`, techo `#4E342E`, puerta `#3E2723`; la ventana `#90CAF9` de día y `#FFE082` encendida de noche (AC-SCN-09-06);
  - **muelle**: tablones de 3 m que salen desde la orilla (`x = −9`) hacia el centro del lago, con la cubierta a 0,4 m sobre el agua, sobre pilotes; madera `#8D6E63`, pilotes `#5D4037`.

  Los colores son los de referencia antes de la luz de la hora. [Editor-PM] (alto y ancho medidos; la paleta por variante) · [Captura] CAP-02, CAP-04 · `painters/shore_painter.dart:89-131`
- **AC-SCN-15-07 · Una orilla de verdad (mejora EXR).** *Entonces*:
  - la línea de costa está en `|x| = 9` y **nada flota**: cada pino y la cabaña apoyan en el terreno, y el muelle en sus pilotes;
  - la orilla tiene terreno (canto rodado, talud de pasto y roca, ladera con bosque al fondo; ART_BIBLE §7.2 como referencia), con una franja húmeda entre el agua y la tierra y sin costuras entre segmentos de 50 m;
  - en la franja de los objetos del legacy (`|x| < 18 m`) solo hay esos objetos y vegetación baja (≤ 1,2 m: pasto, juncos, helechos, rocas), así el paisaje del legacy se reconoce (P-09); el bosque y lo demás que se agregue va más allá;
  - todo lo agregado es **determinista por distancia**, como los objetos del legacy: el mismo en cada sesión;
  - entre las 5 y las 9:30, una bruma baja sobre el agua (ART_BIBLE §7.2).

  [dotnet] (lo agregado, determinista) · [Editor-PM] (nada flota: la base de cada objeto contra el terreno) · [Captura] CAP-01, CAP-02, CAP-07, CAP-12
- **AC-SCN-15-08 · Arte gratis, detrás de slots.** *Entonces*:
  - todo el arte de la 6a es procedural o propio, o entra por un **slot** (ASSET_SLOTS §0): terreno, vegetación, pino, cabaña, muelle, cielo, nubes, luna, perfil del agua;
  - **sin el Terrain Sample Asset Pack** (hoy no está), el terreno y la vegetación procedurales cumplen la calidad mínima de ASSET_SLOTS §8 y la demo se ve completa y digna;
  - si el usuario instala el pack (u otro asset gratis) y lo asigna a su slot, la escena lo usa **sin tocar código ni escenas**; si un archivo asignado falta o no pasa la validación, se usa el procedural y queda **un** aviso `[SLOT]` en el registro, nunca un error. Un slot que usa el procedural a propósito (hoy, casi todos) **no avisa**: sin el pack, la app arranca y entrena sin avisos (AC-SCN-01-05);
  - nada de terceros se versiona: lo del pack va en `Assets/ThirdParty/` (ignorado por git salvo los `.meta`), y el CI compila y pasa sin él;
  - sin el OK del usuario a D-ART-08 no hay texturas CC0 ni la luna de la NASA (P-01).

  [Editor-PM] (con el slot de prueba: el asset entra y, quitado, vuelve el procedural con un solo aviso) · [QA] (`git check-ignore`, CI sin el pack) · ASSET_SLOTS §5.2, §7

### 4.8 F-SCN-23 — Hora de escena forzada (solo modo simulador)

- **AC-SCN-23-01 · La escena usa la hora forzada.** *Dado* el modo simulador, *cuando* `Hora escena` muestra `<h>:00` (AC-BLE-16-07), *entonces* la escena usa esa hora en lugar de la real, también en pleno entrenamiento: la luz, el cielo, el astro, las estrellas, el agua y las luces de la cabaña. Con `real`, la hora del equipo (AC-SCN-09-01). En el build normal no hay forma de forzarla. [Editor-PM] (con `-Simulator`) · [Device-Win] · `scene_settings.dart:14`, `:19-24`; `immersive_workout_screen.dart:85`
- **AC-SCN-23-02 · El cambio llega en 1,5 s (mejora; DV-58).** *Cuando* cambia la hora (con `−`, `+` o `Hora real`), *entonces* la escena va de la hora que muestra a la nueva en **1,5 s** (`ease.move`), por el camino más corto del reloj (de 23 a 0 hacia adelante, de 0 a 23 hacia atrás; con 12 h de diferencia, hacia adelante), como un time-lapse: todo pasa por las horas intermedias, sin saltos. Un cambio durante otro sale desde donde está. Con reducir movimiento, en el acto (como el legacy). Nada más espera: el panel muestra el valor nuevo en el acto. [dotnet] · [Captura] VID-05, VID-06 · `simulator_overlay.dart:139-150`
- **AC-SCN-23-03 · No se guarda.** *Entonces* la hora forzada dura mientras la app está abierta: al reiniciarla, la escena vuelve a la hora real (AC-BLE-16-11). [Editor-PM] · [Device-Win] · `scene_settings.dart:14`
- **AC-SCN-23-04 · El reloj de pared no la sigue.** *Entonces* el reloj de pared del HUD muestra siempre la hora real del equipo, con cualquier hora forzada y durante la transición (AC-WKT-19-03). [Editor-PM] · [Captura] DEMO-04

### 4.9 También toca

**F-SCN-01 (composición, ciclo de vida y rendimiento; se cierra en la 6b).**

- **AC-SCN-01-02 · La escena es el fondo del inmersivo.** *Entonces* el inmersivo tiene, de atrás hacia adelante: la escena (cielo, agua, orilla y bote, en ese orden de profundidad), los degradés de pantalla del HUD (arriba y abajo, DV-67), la barra superior, el HUD, los controles y, con el simulador, la cinta y la llave (AC-WKT-13-02). La escena ocupa toda la pantalla; detrás, negro. Los degradés no suman elementos al HUD, no llegan a la zona del bote y los alcanza el velo de pausa. [Editor-PM] · [Captura] CAP-13 a CAP-17 · `scene_view.dart:97-121`; `immersive_workout_screen.dart:73-117`
- **AC-SCN-01-03 · Empieza de cero y no se reinicia.** *Entonces* cada vez que se abre el inmersivo la escena arranca de cero: velocidad 0, distancia 0 y tiempo 0. Pausar y reanudar, rotar, cambiar el tamaño de la ventana, abrir el panel del simulador o los paneles de series **no** la reinician: la cámara toma el horizonte y la focal nuevos en el primer cuadro y todo sigue. En pausa y con la sesión terminada el tiempo de escena sigue: nubes, estrellas y bruma se mueven; el bote frena (AC-SCN-03-03). Una sesión nueva arranca su escena de cero, sin nada de la anterior (AC-WKT-13-10). [dotnet] · [Editor-PM] · [Captura] VID-09 · `scene_view.dart:38-41`, `:58`, `:90-92`
- **AC-SCN-01-04 · Al volver del segundo plano.** *Entonces*, al volver del segundo plano o al despertar el equipo, la escena sigue desde donde estaba (el primer cuadro avanza a lo sumo 0,1 s, AC-SCN-03-04) y con la hora actual en ese cuadro, sin transición; el HUD muestra el presente (AC-WKT-02-08). [Editor-PM] (ciclo de vida de prueba) · [Device-And]
- **AC-SCN-01-05 · Se libera sin errores.** *Entonces* la escena del lago anima 6 cuadros de 100 ms con SPM 24, split 110 s, 300 m en el monitor, en marcha y con la hora forzada, en V390 y H844, a las 12 y a las 23 h, sin excepciones, y se cierra sin excepciones ni avisos en el registro, aunque todavía esté cargando (LT-84 a LT-87). Tres entrenamientos seguidos no dejan avisos de objetos que ya no existen (AC-WKT-13-09). [Editor-PM] · [Device-Win] · `test/scene/scene_view_test.dart:11`
- **AC-SCN-01-06 · Apagada fuera del inmersivo.** *Entonces* fuera del inmersivo (Dispositivo, Workout, Rutinas, el editor) la escena no se dibuja ni gasta GPU: la app vuelve a su ritmo de reposo. Puede quedar cargada para que la entrada sea inmediata (P-14): prepararla puede incluir **un único dibujo de calentamiento fuera de pantalla**, que no se ve ni anima nada y ocurre antes de que quede lista; después, **0 dibujos** mientras se está fuera del inmersivo. Cuando el inmersivo terminó de salir, nada de la escena sigue animando. [Editor-PM] · [Device-Win] (`[PERF]`: GPU en reposo en Workout)
- **AC-SCN-01-07 · Si la escena no está lista al entrar.** *Dado* que al terminar el negro de la entrada la escena todavía no está lista (primer arranque, equipo lento: estado "escena demorada"), *entonces* la entrada sigue igual sobre el fondo provisorio de la 4a (AC-SCN-01-01), la sesión no espera, y la escena aparece con un fundido de 0,6 s en cuanto está lista, sin vuelo y ya al día: con la distancia, la velocidad y la hora que corresponden, sin reiniciar nada. Es el equivalente de la caída al agua de respaldo del legacy mientras carga el shader. [Editor-PM] · [Captura] CAP-24, VID-11 · `scene_view.dart:48-57`; `workout_screen.dart:44`
- **AC-SCN-01-08 · El bote provisorio.** *Entonces* hasta la It. 7 el bote es un **casco de single** quieto (sin remero, sin remos, sin cabeceo), centrado en `x = 0`, con la popa a 5,3 m de la cámara y la proa a 13,5 m (8,2 m de largo), casco blanco `#F5F5F5` que pasa a `#B0BEC5` abajo, línea de borda `#00B4D8` y cockpit `#263238` (ART_BIBLE §7.8). Queda dentro de la zona del bote en los cinco tamaños de la app (AC-WKT-17-09), y su punto a 9,5 m (donde irá el remero) cae en el centro de la zona ± 2 dp. Su sombra, si la hay, cae del lado opuesto al astro. El legacy tiene remero y remos desde el principio (DV-61, P-05). [Editor-PM] · [Captura] CAP-13 a CAP-17 · `painters/boat_painter.dart:25-27`
- **AC-SCN-01-09 · 60 fps en el Android de gama media (R-12; regla 9).** *Dado* el teléfono de referencia (clase Galaxy A54 / Pixel 6a, Android 8+ de 64 bits) con el nivel Medio, *entonces* durante **30 min** de entrenamiento libre sobre el lago con el agua degradada la escena va a **60 fps sostenidos**, dentro de los topes de ARCHITECTURE §20.1 (GPU ≤ 12 ms, hilo principal ≤ 8 ms, ≤ 150 draw calls, ≤ 350 k triángulos, texturas ≤ 180 MB) y §20.2 (memoria total ≤ 600 MB, UI ≤ 1 ms, escena ≤ 1,5 ms de CPU), con margen térmico. Lo mismo con `Serie larga` en los últimos 5 s de cada paso (R4b-02) y con el diálogo de completado (P-07). [Device-And] — **NO VERIFICABLE** sin el teléfono ni el módulo de Android: se arrastra a la It. 14 (ROADMAP §0, regla 9)
- **AC-SCN-01-10 · Fluida en la PC (`[PERF]`).** *Dado* el build de simulador en la PC, ventana de 1280 × 720, con el nivel que elija la app, *entonces*, en cada uno de estos momentos, el p99 del tiempo de cuadro no pasa de **16,7 ms** y ningún cuadro pasa de **33 ms** (CPU del hilo principal y, si el equipo lo informa, GPU):
  - 5 min de libre con `Fuerte`;
  - la secuencia de `Hora escena` de VID-05 (las transiciones de hora);
  - la entrada con el vuelo (AC-WKT-13-15);
  - `Serie larga`, en los últimos 5 s de un paso (AC-WKT-14-19);
  - el diálogo de completado con la toma aérea: a 60 fps mientras la cámara o el conteo se mueven; después puede bajar a 30 (P-07).

  La memoria total queda por debajo de 1,5 GB. Si el `RowMate.exe` del usuario está abierto y el build no se puede rehacer, esta parte queda NO VERIFICABLE con el motivo (§12 punto 12). [Device-Win] (`[PERF]`)
- **AC-SCN-01-11 · Dentro de los topes, por nivel.** *Entonces*, en el Editor, con el lago a las 12 y a las 22 h y el bote avanzando, en cada nivel de calidad (Alto, Medio y Bajo), los contadores de la escena entran en los topes de ARCHITECTURE §20.1 (draw calls y SetPass, triángulos visibles, memoria de texturas) y la CPU de la escena no pasa de 1,5 ms (§20.2). Es la medida que anticipa la del teléfono. [Editor-PM] (medición)
- **AC-SCN-01-12 · Sin memoria por cuadro.** *Entonces*, en régimen (remando, en pausa, durante una transición de hora, de noche), la escena y el HUD **no reservan memoria por cuadro**, medido en cuadros reales con el ayudante de cuadros de la It. 5 (repintado incluido) y su control positivo en la misma corrida. Los eventos de 1 Hz pueden reservar poco. [Editor-PM] (medición) · ARCHITECTURE §20.2
- **AC-SCN-01-13 · Si el equipo no llega, baja el detalle y no la fluidez.** *Entonces*, si los cuadros se alargan sostenidamente, la escena baja el detalle en este orden: resolución, reflejo, distancia de sombras, densidad de vegetación y, por último, el nivel; nunca baja la tasa de cuadros antes que el detalle. Un cambio de resolución se hace como mucho una vez cada 10 s y, para volver a subir, hacen falta 30 s holgados, así no se ve titilar. La sesión no se entera y el usuario no ve ningún ajuste nuevo (el legacy no tiene ajuste de calidad). [Editor-PM] (con tiempos de cuadro de prueba) · [Device-And] (NO VERIFICABLE sin teléfono) · ARCHITECTURE §20.3
- **AC-SCN-01-14 · Las reglas, fuera de Unity (Q-69).** *Entonces* todo lo marcado [dotnet] en §4 corre en `dotnet test` con .NET 8, sin Unity, y las mismas pruebas pasan dentro de Unity (EditMode) con y sin simulador, con los mismos resultados. [dotnet] · [Editor-EM]
- **AC-SCN-01-15 · Sesiones largas.** *Dado* `E-42K` (42 195 m), *entonces* la escena se ve como a 0 m: sin temblores de los objetos, de la cámara ni del agua, sin costuras, sin objetos fuera de lugar y sin saltos cada 4096 m; los objetos son los que da el oráculo para esa distancia. [dotnet] · [Captura] CAP-12 · Q-68

**F-SCN-13 (agua degradada; se cierra en la 6b).**

- **AC-SCN-13-01 · En la 6a, el agua es la degradada (R-04, Q-67).** *Entonces* el agua del lago es el agua degradada en todos los equipos y niveles: sin ondas, sin reflejos de la orilla, sin estela ni niebla propia. No se muestra ningún error ni estado de carga. En la 6b pasa a ser el respaldo del agua avanzada. [Editor-PM] · `painters/water_painter.dart:62-104`
- **AC-SCN-13-02 · El degradé de 3 tramos.** *Entonces* el agua va del horizonte al borde de abajo de la pantalla con un degradé de tres tramos parejos: arriba waterFar mezclado al 50 % con fog, en el medio waterFar y abajo waterNear (sin tinte en el lago). [dotnet] (los tres colores por hora) · [Captura] CAP-01 a CAP-07 contra LEG-01 a LEG-07 · `water_painter.dart:79-90`
- **AC-SCN-13-03 · Las 10 líneas de brillo.** *Entonces* sobre el agua hay **10 líneas** horizontales de 1,5 dp de grueso, centradas en la x de pantalla del astro que se ve (DV-59):
  - la línea `i` (0 a 9) está en `t = (i/10 + (distancia·0,02 mod 0,1)) mod 1`, a la altura `horizonte + alto del agua·t²`;
  - mide `ancho·(0,15 + 0,35·t)` a cada lado del centro;
  - su color es sunColor con opacidad `0,05 + 0,1·(1 − t)·fuerza de la luz` (AC-SCN-10-03): de día, doradas o blancas; de noche, plateadas. Esa es la opacidad del legacy y el dato que se compara con el oráculo;
  - **en pantalla (DV-59)**, esa opacidad se multiplica por la visibilidad del astro que se ve (la de la puesta y la salida, AC-SCN-10-05): las líneas se apagan mientras el sol se pone (≈ 18:30 a 19:33) y se encienden mientras sale la luna (≈ 19:34 a 20:36), y al revés al amanecer (≈ 3:24 a 4:25 y ≈ 4:27 a 5:30); en el cambio de astro (19:34 y 4:26) no hay ninguna, así no saltan de un lado al otro. Fuera de esas ventanas, la visibilidad es 1 y las líneas son las del legacy (también a las 6 y a las 18, con el sol detrás del paisaje o apoyado en el horizonte);
  - avanzan con la distancia de escena: un ciclo cada 5 m; con el bote quieto, quietas.

  Ejemplo, a 0 m al mediodía: la línea 0 en el horizonte, de 0,3 del ancho y opacidad 0,15. [dotnet] (las 10 líneas por distancia y hora, contra el oráculo; la visibilidad por minuto) · [Editor-PM] (a las 19:34, ningún píxel de línea) · [Captura] CAP-02, CAP-04, VID-03, VID-05 · `water_painter.dart:92-103`
- **AC-SCN-13-04 · Sin estela, sin errores, digno.** *Entonces* el agua degradada no tiene estela (es de la 6b) y se dibuja sin excepciones en V390 a las 12 h en el lago con la escena recién creada (LT-83). Dentro de su presupuesto (**≤ 24 ALU** y sin texturas; DT6a-07: ART_BIBLE §7.7 decía 12, que no alcanza para lo de abajo), el Designer la deja más digna con un leve reflejo del cielo hacia lo lejos, una franja más clara junto a la orilla y la bruma baja de la mañana (AC-SCN-15-07), **sin sacar ni mover lo del legacy**: los tres tramos y las 10 líneas quedan como en AC-SCN-13-02 y -03, con la única diferencia ya registrada de DV-59 (centradas bajo el astro que se ve y con su visibilidad). Si el gate no acepta DT6a-07: el agua del legacy sola, sin los agregados. [Editor-PM] · [Captura] · `test/scene/water_painter_test.dart:11`

**F-SCN-19 (silueta lejana; se cierra en la 8).**

- **AC-SCN-19-01 · Las dos cordilleras del lago.** *Entonces* sobre el horizonte, delante del astro y de las nubes, hay dos cordones de montañas con el perfil del legacy (`0,45 + 0,35·sin(6,3u + fase) + 0,2·sin(15,1u + 2·fase)`, fase 0 y frecuencia 1,0 el lejano, fase 3,1 y frecuencia 1,6 el cercano):
  - el **lejano**, más claro y alto, de tono `#5C6E8A` con profundidad 1,0, con **nieve** en todo lo que pasa el **58 %** de su alto máximo, con borde suave y lenguas irregulares (DV-64): con el perfil quieto (Q-65) cubre el macizo de la izquierda, el pico central y, en horizontal, el flanco del pico de la derecha (≈ 45 % del ancho del cordón en TV y ≈ 33 % en TH). La nieve es `#F2F6FA` mezclado al 30 % con fog (exacto en la lógica); en la hora dorada se tiñe apenas de sunGlow (rosada a las 6 y a las 18) y de noche brilla más cuanto más iluminada está la luna;
  - el **cercano**, más oscuro y boscoso, de tono `#2F4A3A` con profundidad 0,3, sin nieve, de 0,7 veces el alto del lejano, que tapa su base;
  - el color de cada uno es `mezcla(tono; fog; 0,35 + 0,4·profundidad)` con la paleta de la hora (exacto en la lógica);
  - **el alto** (P-11; DV-65): el alto máximo del perfil es el del legacy, el **12 % del alto de pantalla** (30 % del cielo visible en vertical y 28,6 % en horizontal), **con un tope de 9° sobre el horizonte**. En horizontal queda igual que el legacy (6,84° en H844, TH, L640 y W1280); en vertical, 9° (18,3 % del cielo en V390 y 17,8 % en TV y P360: ≈ 60 % del alto del legacy). El pico más alto que se ve del lejano (el macizo, a −25,4°) llega a ≈ 0,89 de ese alto: ≈ 25 % del cielo visible en horizontal y ≈ 16 % en vertical. La forma (el perfil por acimut) no depende de la ventana ni de la distancia; con la distancia solo crecen muy despacio (AC-SCN-19-02).

  [dotnet] (los colores por hora; el alto por ventana; dónde hay nieve) · [Captura] CAP-01 a CAP-07 · `environments/lake_environment.dart:43-85`; `environment.dart:127-129`
- **AC-SCN-19-02 · El horizonte se acerca, no se desliza (Q-65; DV-55).** *Entonces* al avanzar las montañas **no se corren de costado**: se acercan muy despacio, al 4 % de la velocidad del bote (ART_BIBLE §7.1), y en una sesión de 42 km siguen lejos. Con el bote quieto, quietas. El legacy las desliza a la izquierda 0,24 dp (lejanas) y 0,48 dp (cercanas) por metro. [Editor-PM] · [Captura] VID-03 · `lake_environment.dart:48-51`

**F-SCN-20 (objetos de orilla; se cierra en la 8).**

- **AC-SCN-20-01 · Luz y niebla de la orilla.** *Entonces*:
  - la luz que recibe la orilla (y el bote) escala con el ambiente de la paleta: plena de día y apagada y azulada de noche, como el tinte del legacy (rojo y verde × ambiente, azul × mín(1; ambiente + 0,12));
  - lo lejano se pierde en la niebla con fog: la mezcla es `clamp((relZ − 80)/320; 0; 1)·0,8`, desde los 80 m hasta un 80 % a los 400 m (a 240 m, 40 %);
  - lo de arriba vale para todo objeto de la orilla, también para los agregados.

  [dotnet] (los dos factores, contra el oráculo) · [Captura] CAP-02, CAP-04 · `shore_painter.dart:45-50`
- **AC-SCN-20-02 · De noche.** *Entonces* con ambiente menor a 0,6 la orilla está "de noche": la ventana de la cabaña encendida (`#FFE082` antes del ajuste de color de la hora), sin tinte ni niebla (AC-SCN-09-06); el resto de los objetos, oscuro. [Captura] CAP-04, CAP-09 · `shore_painter.dart:26`, `:121-122`

**F-SCN-21 (preferencia de escenario; se cierra en la 8).**

- **AC-SCN-21-01 · Siempre el lago.** *Entonces* en la 6a la escena es siempre el **Lago alpino**, el escenario por defecto del legacy. No hay selector, no se lee ni se escribe `scene.environment` y no se puede cambiar de escenario durante el entrenamiento (tampoco en el legacy). [Editor-PM] · [QA] · `scene_settings.dart:11-13`

**F-WKT-17 (HUD sobre la escena).**

- **AC-WKT-17-13 · El HUD se lee sobre cualquier escena.** *Entonces*, a cualquier hora y con lo más claro que la escena pueda poner detrás (una nube blanca, el sol, el cielo del mediodía, la nieve), los textos y valores del HUD cumplen ART_BIBLE §10.1: ≥ 7:1 en blanco y en las métricas no rojas y ≥ 4,5:1 en la familia roja, medidos contra el peor fondo del vidrio (`glass.fallback` `#070D16 @86 %` sobre blanco = `#2A2F37`). Las píldoras hechas y el borde de la hueca dan al menos 3:1 contra el vidrio en su peor fondo, con una nube blanca detrás de la barra (la de enfriamiento y la de otro tipo, con su variante `.hud`: el riesgo de [04b/04-design-notes §8.2](../04b-hud-exr/04-design-notes.md)). **Excepción contada**: el borde de la hueca contra su propio interior al 18 % (un tenue de contrato) da 2,93 a 3,02, como la pista al 30 % de la 4b; la hueca se distingue además por la forma. Nada del HUD baja de opacidad por la escena. [dotnet] (contraste) · [Captura] CAP-19, CAP-23

---

## 5. Criterios de iteraciones anteriores que se verifican acá

### 5.1 La deuda de la It. 5, primero (antes de construir encima)

Es la de [REPORT de la It. 5 §9.2](../05-series-parciales/REPORT.md) y del [STATUS](../../STATUS.md). No suma criterios: son los de la 5 que quedaron sin medir en el build.

1. **QA (o el Architect)**, con el `RowMate.exe` del usuario cerrado y una carpeta de datos temporal: el build de simulador; `[PERF]` de las tres entradas (AC-WKT-13-15: libre, `Prueba 4a` y `Cinco reps` con los dos paneles abiertos) y de la ventana con los paneles (AC-WKT-18-21: `Fondo 3000` y `Series 600`); la prueba a mano de [05-qa-verification §R2.6](../05-series-parciales/05-qa-verification.md) (mouse y teclado, reiniciar con los paneles recordados, `Simular desconexión`, la cuenta grande, el sello y `PAUSA`, matar el proceso a mitad). Si algo falla, se reabre en la 5 y va primero. Si el `RowMate.exe` sigue abierto, se reintenta cada 2 min hasta 20 min; si no, queda NO VERIFICABLE y se mide al cierre de la 6a, con la escena (AC-SCN-01-10).
2. **AC-WKT-02-11** (P-17): si la demo (c) del usuario mostró que el reloj salta el tiempo suspendido, se reabre y va primero.
3. **Designer**: QA-V5-R2-01 (por defecto, la regla de la fila nueva con la vista desplazada vale solo en W1280 y las notas lo dicen).

### 5.2 Cambian por esta iteración

Valen estos textos, que reemplazan a los anteriores.

- **AC-WKT-13-02 · Capas (cambia).** *Entonces* de atrás hacia adelante: **la escena** (AC-SCN-01-02; el fondo provisorio solo mientras no está lista, AC-SCN-01-07), los degradés de pantalla del HUD (DV-67), la barra superior, el HUD, los controles y, con el simulador, la cinta y la llave. [Editor-PM] · [Captura] CAP-13 a CAP-17
- **AC-WKT-13-06 · Entrada (cambia: la escena y el vuelo).** *Entonces* lo de la 4b y la 5 (Workout funde a negro en 220 ms y se ocultan las barras del sistema; la cascada a los 400, 520, 600, 680, 760 y 840 ms, con los paneles junto con SPM; la llave al terminar), con **la escena en lugar del fondo**: aparece desde el negro en 220-400 ms con la cámara ya volando, y el vuelo termina a los 1 820 ms en la pose de contrato (AC-SCN-02-05). El HUD está entero a los ≈ 1,1 s, con la cámara todavía bajando. La sesión, el reloj y la escena corren desde el toque. Con reducir movimiento, el fundido de 120 ms lineal, sin negro, sin cascada y sin vuelo. [Editor-PM] · [Captura] VID-01, VID-02
- **AC-WKT-13-08 · Salida (cambia).** *Entonces* lo de la 4b y la 5 (el HUD con la última foto en 0-200, el negro en 200-400, Workout en 400-640: menos de 1 s), con la cámara que empieza a subir y alejarse desde el primer cuadro (AC-SCN-02-06). Detrás del negro la escena deja de dibujarse (AC-SCN-01-06). [Editor-PM] · [Captura] VID-07
- **AC-WKT-13-12 · El velo de pausa (cambia: sobre la escena).** *Cuando* se pausa, *entonces* la escena **pierde un 30 % de saturación y se oscurece un 15 %** en 300 ms (`ease.move`; ANIMATION_BIBLE §2.11), y vuelve en 240 ms al reanudar. `PAUSA` en su disco, las tarjetas, la barra, el chip y los paneles siguen como en la 4b y la 5: a pleno, sin bajar de opacidad. Debajo del velo el bote se desliza y se detiene (AC-SCN-03-03) y las nubes siguen. Con reducir movimiento, el velo igual (es un fundido) y `PAUSA` sin escala. [Editor-PM] · [Captura] CAP-20, CAP-21, VID-04
- **AC-WKT-13-15 · La entrada, sin tirón (cambia).** *Entonces* lo de la 5, con la escena: mientras algo se mueve en la entrada (el fundido a negro, la aparición de la escena, el vuelo y la cascada), ningún cuadro pasa de 16,7 ms de CPU del hilo principal en el build de simulador de la PC. Cargar o preparar la escena solo puede tardar con la pantalla entera en negro y quieta, o mientras se ve Workout en reposo sin que nada se mueva. Si la escena llega tarde (AC-SCN-01-07), lo que le falte se prepara en tramos cortos, también después de la cascada, sin que ningún cuadro pase de 16,7 ms, y su primer dibujo tampoco. Tampoco la salida y la descarga de la escena hacen tironear la vuelta de Workout (medido de 0 a 1 s desde la confirmación de `Terminar`). [Device-Win] (`[PERF]`) · [Editor-PM] (el orden)
- **AC-WKT-14-19 · Fluidez con muchos pasos (cambia: con la escena; R4b-02).** *Entonces* lo de la 4b, con la escena detrás: con `Serie larga`, en el cambio de paso y en los últimos 5 s de cada paso, en la PC ningún cuadro tarda más de 33 ms y el p99 no pasa de 16,7 ms (AC-SCN-01-10); en el teléfono, 60 fps (AC-SCN-01-09, NO VERIFICABLE sin teléfono). Si hace falta, la fila se dibuja en dos elementos (el corte previsto, +1 elemento, dentro del tope de 60). [Device-Win] (`[PERF]`) · [Device-And]
- **AC-WKT-17-07 · Orientación (cambia).** *Entonces* al rotar o cambiar la clase de ventana, el HUD **entra con un fundido de 200 ms en la disposición nueva**, sin foto de la anterior (DT6a-06, registrado al cerrar; antes decía "fundido cruzado"); dentro de la misma clase (arrastrar el borde de la ventana) se reacomoda en el acto, y la escena se adapta en el acto, sin reiniciarse (AC-SCN-01-03); la sesión y los valores siguen. Con reducir movimiento, el HUD en el acto. [Editor-PM] · [Captura] VID-09
- **AC-WKT-17-09 · La zona del bote (cambia).** *Entonces* lo de la 5, y además la zona queda centrada donde la cámara real proyecta el remero (0,504 del alto en TV y P360 y 0,652 en horizontal, con la fórmula de AC-SCN-02-01 en cada ventana, ± 2 dp), con el bote provisorio adentro (AC-SCN-01-08). [Captura] CAP-13 a CAP-18 (con el contorno)
- **AC-BLE-16-07 · Hora de la escena (cambia).** *Entonces* lo de la It. 2 (`real` o `<h>:00`, ±1 h desde la hora forzada o desde la hora entera del reloj, la vuelta entre 23 y 0, `Hora real`), y ahora **la escena la usa** (AC-SCN-23-01), con la transición de AC-SCN-23-02. [dotnet] · [Editor-PM] (con `-Simulator`) · [Captura] DEMO-04 a DEMO-08 · `test/simulator_overlay_test.dart:17` (LT de la It. 2)
- **AC-SCN-01-01 · Fondo provisorio (cambia).** *Entonces* el fondo provisorio de la 4a solo se ve si la escena no está lista al entrar, hasta que aparece (AC-SCN-01-07). [Editor-PM] · [Captura] CAP-24
- **AC-APP-05-08 · Reducir movimiento (cambia).** *Entonces* lo de la 5 y, además: la entrada sin vuelo (el fundido de 120); la cámara no respira ni hace la toma aérea, y no se mueve al salir; la hora forzada cambia en el acto; el HUD se reacomoda en el acto al rotar. El velo de pausa, los fundidos de las luces y de las estrellas y el de la escena que llega tarde siguen (no son movimiento); el minuto de la hora real cambia en el acto (AC-SCN-09-01). Nada de esto cambia los datos ni cuándo pasan. [Editor-PM] · [Captura] VID-02

### 5.3 Se vuelven a verificar, sin cambios

| Criterio | Qué se mira otra vez |
|---|---|
| AC-WKT-13-09 · Nada queda animando al salir | Tampoco la cámara, la escena ni sus luces; la escena deja de dibujarse (AC-SCN-01-06) |
| AC-WKT-13-13 · Nada se encima | El HUD de la 5, igual, sobre la escena |
| AC-WKT-13-14 · La presentación no toca el dato | La secuencia del motor es idéntica con la escena, sin ella, con reducir movimiento y con cualquier hora; la distancia de escena nunca llega al motor (AC-SCN-03-05) |
| AC-WKT-19-03 · El reloj de pared, en su ritmo | Muestra la hora real, no la de la escena (AC-SCN-23-04) |
| AC-WKT-17-01 · Vidrio y tamaños | El mismo `glass.fallback` sobre la escena (el desenfoque es de la 6b) |
| AC-WKT-02-08 · Al volver, el presente | El HUD en el presente; la escena sigue desde donde estaba (AC-SCN-01-04) |
| AC-WKT-18-21 · El presupuesto del HUD | La escena no suma elementos al HUD: sigue en 56-57 de 60 (con el corte de AC-WKT-14-19, 57-58); 0 B por cuadro |
| AC-BLE-16-02, -03 · La llave y el panel | Sobre la escena, en los cinco perfiles, sin tapar Split, Watts, el sello ni el lugar de la cuenta grande |
| AC-BLE-15-09 · La física no se reinicia al reconectar | Con `Simular desconexión`, la escena sigue con la última lectura (AC-SCN-03-07) y nada salta al volver |
| AC-HIS-20-04 · Las pruebas no tocan los datos del usuario | Tampoco las de la escena ni sus capturas y videos: carpeta de datos temporal siempre |
| AC-WKT-21-01 · El diálogo de completado | Aparece a los 300 ms, una sola vez, ahora con la toma aérea detrás (AC-SCN-02-07) |

---

## 6. Casos de test legacy

Los 26 de ROADMAP §5 para la 6a. Se portan uno a uno, con el mismo nombre y las mismas afirmaciones. `$id` se escribe como lo imprime Dart (`EnvironmentId.lake`) y la hora con su decimal (`12.0h`).

| LT | Archivo:línea | Caso (nombre exacto) | Qué afirma | Dónde | AC |
|---|---|---|---|---|---|
| LT-62 | `test/scene/scene_camera_test.dart:9` | `horizonte y focal según orientación` | Horizonte = 844·0,40 en V390 y 390·0,42 en H844 (± 0,001); focal 390 en los dos | [dotnet] | AC-SCN-02-01 |
| LT-63 | `:16` | `lo lejano queda más cerca del horizonte, centrado y más chico` | En V390, (3; 0; 200) queda más arriba que (3; 0; 10) pero debajo del horizonte, más cerca del centro y con menor escala; (3; 0; 1e9) converge a (centro; horizonte) ± 0,01 | [dotnet] | AC-SCN-02-02 |
| LT-64 | `:30` | `detrás de la cámara no se proyecta` | z = 0,2 no da posición | [dotnet] | AC-SCN-02-03 |
| LT-65 | `:34` | `unprojectWater invierte project para y = 0` | En H844, (−4; 6), (0; 30) y (7,5; 120) de ida y vuelta con error < 1e-6 | [dotnet] | AC-SCN-02-02 |
| LT-66 | `test/scene/scene_state_test.dart:14` | `la velocidad sale del split y se alcanza en unos segundos` | Objetivo(107 s, 24 spm, en marcha) = 4,67 ± 0,01; detenido, split 0 o SPM 0 → 0; tras 8 s (pasos de 0,05 s) velocidad 4,67 ± 0,05 y distancia > 25 | [dotnet] | AC-SCN-03-01, -02 |
| LT-67 | `:26` | `al dejar de remar desacelera hasta detenerse` | 8 s remando y 6 s con split 0: velocidad < 0,1 y la distancia creció; 10 s más: velocidad exactamente 0 | [dotnet] | AC-SCN-03-03 |
| LT-68 | `:55` | `shaderDistance se acota a 4096 m` | 5000 → 904 | [dotnet] | AC-SCN-03-06 |
| LT-69 | `test/scene/time_of_day_test.dart:9` | `12:00 es día con el sol arriba` | No es noche ni luna; y < 0,2·horizonte (V390); fuerza 1,0 | [dotnet] | AC-SCN-10-01 a -03 |
| LT-70 | `:17` | `23:00 es noche con luna` | Es noche y luna; y < horizonte; ambiente < 0,5 | [dotnet] | AC-SCN-10-01, -02, AC-SCN-09-02 |
| LT-71 | `:25` | `18:30 interpola entre atardecer y anochecer` | Ambiente = 0,85 + (0,5 − 0,85)·0,25 (± 1e-9); skyHorizon distinto del de las 18 y del de las 20 | [dotnet] | AC-SCN-09-02, -03 |
| LT-72 | `:32` | `de 22 h a 4 h la paleta es noche cerrada, sin falso amanecer` | Ambiente(0) = 0,35 y ambiente(3) = 0,35 | [dotnet] | AC-SCN-09-02 |
| LT-73 | `:37` | `a las 19 h sigue el atardecer y a las 20 h ya sale la luna` | Noche(19) falso, noche(20) verdadero, noche(5) falso y a las 5 no hay luna | [dotnet] | AC-SCN-10-01 |
| LT-74 | `:44` | `la paleta es continua a medianoche y al amanecer` | Ambiente(23,99) ≈ ambiente(0) y ambiente(4,99) ≈ ambiente(5), ± 0,01 | [dotnet] | AC-SCN-09-02 |
| LT-75 | `test/scene/environment_test.dart:10` | `EnvironmentId.lake: propsForSegment es determinista` | El segmento 7 dos veces da lo mismo (tipo, x, z, variante) y difiere del 8 | [dotnet] | AC-SCN-14-01 |
| LT-76 | `:17` | `EnvironmentId.lake: cada segmento tiene objetos y quedan dentro del segmento` | Segmentos 0 a 29: ninguno vacío, z dentro del segmento, x a 9 m o más del eje (en valor absoluto) salvo puente y boya | [dotnet] | AC-SCN-14-03 |
| LT-77 | `:30` | `EnvironmentId.lake: visibleProps ordena de lejos a cerca y respeta el máximo` | A 1234: ≤ 120, z no creciente, z en [1235; 1634] | [dotnet] | AC-SCN-14-04, AC-SCN-15-04 |
| LT-78 | `test/scene/sky_shore_painter_test.dart:16` | `EnvironmentId.lake 390x844 12.0h pinta sin excepciones` | Cielo y orilla a 0 y 1234 m, tiempo 3,2 s, sin excepciones | [Editor-PM] | AC-SCN-11-07 |
| LT-79 | `:16` | `EnvironmentId.lake 390x844 23.0h pinta sin excepciones` | Ídem a las 23 h | [Editor-PM] | AC-SCN-11-07 |
| LT-80 | `:16` | `EnvironmentId.lake 844x390 12.0h pinta sin excepciones` | Ídem en H844 | [Editor-PM] | AC-SCN-11-07 |
| LT-81 | `:16` | `EnvironmentId.lake 844x390 23.0h pinta sin excepciones` | Ídem en H844 a las 23 h | [Editor-PM] | AC-SCN-11-07 |
| LT-82 | `:39` | `las estrellas aparecen apenas oscurece (19 h), no solo de noche cerrada` | En V390, en el lago, con tiempo 0, a las 19 h se dibujan al menos 80 estrellas más que a las 12 (el legacy cuenta los círculos del cielo; el port cuenta las estrellas visibles: 80 contra 0) | [dotnet] | AC-SCN-11-04 |
| LT-83 | `test/scene/water_painter_test.dart:11` | `sin shader pinta el fallback sin excepciones` | Agua degradada en V390, 12 h, lago, escena recién creada: sin excepciones | [Editor-PM] | AC-SCN-13-04 |
| LT-84 | `test/scene/scene_view_test.dart:11` | `EnvironmentId.lake 390x844 12.0h anima sin excepciones` | SPM 24, split 110, 300 m, en marcha, hora forzada; 6 cuadros de 100 ms y el cierre sin excepciones | [Editor-PM] | AC-SCN-01-05 |
| LT-85 | `:11` | `EnvironmentId.lake 390x844 23.0h anima sin excepciones` | Ídem a las 23 h | [Editor-PM] | AC-SCN-01-05 |
| LT-86 | `:11` | `EnvironmentId.lake 844x390 12.0h anima sin excepciones` | Ídem en H844 | [Editor-PM] | AC-SCN-01-05 |
| LT-87 | `:11` | `EnvironmentId.lake 844x390 23.0h anima sin excepciones` | Ídem en H844 a las 23 h | [Editor-PM] | AC-SCN-01-05 |

Quedan para otras iteraciones: `scene_state_test` :37, :46, :60 y :69 (It. 7; la parte de velocidad de :60 ya la cubre AC-SCN-03-03); `environment_test` de río, costa y regata y :42, :54, :60 (It. 8); `sky_shore_painter_test` :16 de los otros escenarios y :57 (It. 8: necesita el puente y el cartel); `water_painter_test:27` (It. 8, la costa); `scene_view_test` de los otros escenarios (It. 8). Los 4 de LT-84 a LT-87 se vuelven a correr en la 6b con el agua avanzada.

---

## 7. Capturas, demo y videos

### 7.1 Reglas

Las de la It. 5 ([01-po-spec §7.1](../05-series-parciales/01-po-spec.md)), y además:

- Las tomas van en `docs/migration/iterations/06a-escena-lago/snapshots/`, con `manifest.json` e `INDEX.md` **sin rutas de la PC**.
- Salvo que la toma diga otra cosa: español; simulador sin ruido; reloj, fecha y zona de prueba; carpeta de datos temporal; sin la llave; con el R-4.
- **Escena sola** (CAP-01 a CAP-12): sin HUD ni los degradés de pantalla del HUD (DV-67), con la respiración en 0, el tiempo de escena en 3,2 s, `E-920` y la región de prueba (§0.4), salvo que diga otra cosa.
- Una toma con la hora forzada se saca con la transición terminada (al menos 1,6 s después del último cambio).
- **Las referencias del legacy (`LEG-nn`)** las dibuja el propio código del legacy (su cielo, su agua de respaldo y su orilla, sin el bote) con el oráculo de Flutter, con el mismo tamaño, hora, distancia y tiempo de escena que la toma de la app, **sin tocar el legacy** (la guardia de `tools/oracle`). Las tomas con "marcas" llevan una marca en la base de cada objeto del legacy, calculada con la lista del oráculo. Si el oráculo no puede dibujar las imágenes, esas `LEG-nn` quedan NO VERIFICABLE con el motivo, y la comparación se hace con las marcas (que son obligatorias).
- Dos corridas iguales, byte a byte. Si la GPU no lo permite en alguna toma 3D, QA lo explica y acepta solo diferencias de 1 nivel por canal en menos del 0,1 % de los píxeles.
- Las tomas del inmersivo de las It. 4a, 4b y 5 quedan en sus carpetas como estaban. Si alguna prueba las compara contra la imagen guardada, se regeneran una vez con la escena (dos corridas iguales) y QA explica cada diferencia que no sea el fondo.
- El contorno de la zona del bote va en las tomas que lo piden.

### 7.2 Tamaños

**V390** 390 × 844 y **H844** 844 × 390 (los del legacy, para comparar); **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720 (los de la app).

### 7.3 Lista

**Escena sola** (comparada con el legacy).

| ID | Tamaño | Estado | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** + **LEG-01** | V390 | 06:00 | **Amanecer**: el cielo a mitad de camino entre el violeta del amanecer y el celeste de la mañana (ambiente 0,825); el sol a la izquierda (−17,5°) a 1,5°, detrás del macizo de la izquierda: se ven su resplandor dorado y el borde del halo sobre la cresta (el disco sale entero a las ≈ 7:59 en vertical); estrellas tenues (oscuridad 0,27); bruma baja sobre el agua; luz dorada, sombras muy largas hacia la cámara; la cabaña a 40 m a la derecha con la ventana apagada; el muelle a 55 m a la izquierda; las 10 líneas bajo el sol. Contra LEG-01: el mismo tono dominante |
| **CAP-02** + **LEG-02** | V390 | 12:00, con marcas | **Mediodía**: cielo de `#0B4FA8` a `#8FD3FF`; el sol centrado y alto (12°), más chico que el del legacy y brillante (DV-63); sin estrellas; las 7 nubes; las montañas más bajas que en LEG-02 (DV-65), con nieve en el macizo y en el pico central (DV-64); luz plena y sombras cortas; **cada pino, el muelle y la cabaña sobre su marca del legacy** (± 2 dp en x); nada flota |
| **CAP-03** + **LEG-03** | V390 | 18:00 | **Atardecer**: cielo de `#2E3C8C` a `#FF8C5A`; el sol a la derecha (+17,5°) en el horizonte, en el collado entre las montañas; la cabaña a 40 m queda delante del disco, en contraluz con el halo; estrellas apenas (oscuridad 0,23); luz cálida y rasante |
| **CAP-04** + **LEG-04** | V390 | 22:00 | **Noche**: la luna a la izquierda del centro (−5,8°) a ≈ 10,6°, gibosa menguante (≈ 94 %), con la parte oscura a la derecha (región de prueba); las 80 estrellas plenas; la ventana de la cabaña encendida, sin niebla; nubes casi invisibles; agua oscura con las líneas plateadas bajo la luna |
| **CAP-05** + **LEG-05** | H844 | 12:00, con marcas | CAP-02 en horizontal: más orilla a los costados; las montañas del mismo alto que en LEG-05; cada objeto sobre su marca |
| **CAP-06** + **LEG-06** | H844 | 22:00 | CAP-04 en horizontal |
| **CAP-07** + **LEG-07** | V390 | `E-0`, 12:00, con marcas | **El arranque de toda sesión**: el muelle a 175 m a la izquierda, ya algo en la niebla (≈ 24 %); pinos desde la cámara hasta los 400 m; cada objeto sobre su marca |
| **CAP-08** | TH | 19:00 | El sol detrás de las montañas, con el borde y el halo asomando (Q-63); las líneas del agua a la mitad (el sol se está poniendo, DV-59); las 80 estrellas tenues (oscuridad 0,5); la ventana todavía apagada |
| **CAP-09** | TH | 20:00 | La luna saliendo a la izquierda, detrás del paisaje (Q-63); las líneas del agua al 38 % de su opacidad (la luna sale, DV-59); estrellas (oscuridad 0,77); la ventana encendida (ambiente 0,5) |
| **CAP-10** | TV | 00:00 | La luna centrada y alta (12°) con su fase; las 80 estrellas plenas, la primera en su lugar; las líneas plateadas centradas |
| **CAP-11** | TV | 15:00 | **Sin estrellas** (Q-62: el legacy dibuja 80 casi invisibles); el sol a la derecha (+8,75°, 8,9°) |
| **CAP-12** | W1280 | `E-42K`, 12:00 | A 42 195 m, lo mismo que a 0 m: sin temblores, sin costuras, los objetos que da el oráculo |

**El HUD sobre la escena** (hora real de prueba, `10:00`, salvo que diga otra cosa).

| ID | Tamaño | Estado | Qué tiene que verse |
|---|---|---|---|
| **CAP-13** | TV | Libre a los 60,2 s, con el contorno | El HUD de la 4b sobre el lago con la luz de las 10, con los degradés de arriba y de abajo detrás (DV-67); el bote provisorio dentro de la zona, centrado (0,504 del alto); la banda dice `1240` `m` (el monitor) y la escena recorrió ≈ 234 m: los pinos que se ven son los de esa distancia |
| **CAP-14** | TH | CAP-13 en horizontal | Lo mismo, con la zona en 0,652 |
| **CAP-15** | L640 | CAP-13 en la ventana baja | Lo mismo |
| **CAP-16** | P360 | CAP-13 en un teléfono de 360 | Lo mismo |
| **CAP-17** | W1280 | CAP-13 en la ventana | Lo mismo |
| **CAP-18** | TV | `Series 600` a los 250,2 s, con los dos paneles abiertos, a las 18:00, con el contorno | Los paneles en la franja de afuera de la zona; el casco a la vista en el centro; todo legible con el atardecer detrás |
| **CAP-19** | TV | `Prueba 4a` a los 90,6 s, a las 12:00, con el tiempo de escena elegido para que la nube 3 pase detrás de la barra | Las píldoras hechas, la hueca y la fila de información legibles con la nube detrás (AC-WKT-17-13) |
| **CAP-20** | TV | Libre: `Pausa` a los 60,2 s; la toma a los 62,2 s | **El velo**: la escena con menos color y más oscura; `PAUSA` en su disco; el HUD a pleno; el bote todavía deslizándose |
| **CAP-21** | TH | CAP-20 en horizontal | Lo mismo |
| **CAP-22** | TV | `Corta`: el diálogo entra a los 30,3 s; la toma a los 36,5 s | **El final de la toma aérea**: la cámara más alta y atrás, mirando apenas hacia abajo, quieta (sin respiración); el horizonte más arriba (≈ 337 dp en lugar de 365,6); el lago alrededor del diálogo, con su velo. El diálogo opaco de la 4b, encima y centrado, **tapa el bote** (el vidrio con desenfoque es de la 6b): que el bote quede entero dentro de la zona se afirma con la cámara real en el *manifest* (la popa en ≈ 553), no a la vista |
| **CAP-23** | W1280 | Libre a los 60,2 s, a las 22:00 | El HUD legible de noche, con la luna y las estrellas |
| **CAP-24** | TV | Libre con "escena demorada", a los 0,6 s | El fondo provisorio de la 4a con el HUD llegando (AC-SCN-01-07) |

### 7.4 Secuencia de la demo

W1280, español, simulador sin ruido, `F-4A`, hora real de prueba. Cada toma sigue a la anterior.

| ID | Paso | Qué tiene que verse |
|---|---|---|
| **DEMO-01** | Workout | Reposo; la escena no se dibuja (0 dibujos después de quedar lista; el único de calentamiento, fuera de pantalla, va antes, AC-SCN-01-06) |
| **DEMO-02** | `Entrenamiento libre`; la toma a los 2,0 s | La cámara ya en su lugar detrás del bote; el HUD entero; la luz de las 10 |
| **DEMO-03** | Llave → `Fuerte` a los 3 s; la toma a los **≈ 36 s** (36,2 s, con la regla de toma de la 4b, k + 0,2) | El muelle a la izquierda, acercándose con sus pilotes (en W1280 se ve de los ≈ 34 a los ≈ 38,2 s y pasa a la par a los ≈ 39 s, ya fuera del cuadro); las montañas en el mismo lugar que en DEMO-02 |
| **DEMO-04** | `Hora escena` hasta `18:00`; la toma 1,6 s después | El atardecer; el reloj de pared sigue en la hora real |
| **DEMO-05** | `+` a `19:00` y a `20:00` | La luna saliendo detrás del paisaje; estrellas; la ventana de una cabaña, si está a la vista |
| **DEMO-06** | `+` a `22:00` | La noche con luna y 80 estrellas |
| **DEMO-07** | `Pausa` | El velo sobre la noche; el bote deslizándose |
| **DEMO-08** | `Hora real` → `Reanudar` → `Terminar` → `Terminar` | Vuelve la luz de las 10 en 1,5 s; la salida; Workout en reposo |

### 7.5 Videos

| ID | Tamaño y duración | Qué se hace | Qué tiene que verse |
|---|---|---|---|
| **VID-01** | TV, 2,5 s | `Entrenamiento libre` | **La entrada**: negro (0-220 ms); la escena aparece con la cámara volando (220-400) desde atrás y arriba; la cascada del HUD (400-1 120); la cámara llega a su lugar a los 1 820 ms, sin salto; el reloj corre desde el toque |
| **VID-02** | TV, 1 s | VID-01 con reducir movimiento | El fundido de 120 ms lineal con la cámara ya en su lugar; sin negro, sin cascada, sin vuelo |
| **VID-03** | W1280, 12 s | Libre con `Fuerte`, de los 30 a los 42 s | **El bote avanzando**: los pinos vienen hacia la cámara y salen por los costados con su tramado; el muelle pasa por la izquierda; las montañas no se corren; las nubes solo con el viento; las líneas del agua avanzan; la cámara respira apenas (el horizonte dentro de ± 3 dp); nada aparece de golpe a lo lejos |
| **VID-04** | TH, 14 s | Libre con `R-4`: `Pausa` a los 20 s y `Reanudar` a los 32 s | El velo en 300 ms; el bote se desliza ≈ 6 m y queda quieto a los ≈ 9 s; las nubes siguen; al reanudar, el velo se va en 240 ms y el bote acelera |
| **VID-05** | TH, escena sola, `E-920`, 14 s | Hora forzada `17:00` y `+` cada 2 s hasta `23:00` | **El anochecer**: cada cambio en 1,5 s; el sol se esconde detrás de las montañas sin saltar y las líneas del agua se apagan con él; las estrellas entran a las 18 con su fundido y llegan a pleno; la luna sale por la izquierda detrás del paisaje y las líneas vuelven, plateadas, bajo ella; la ventana de la cabaña se enciende con su fundido |
| **VID-06** | TH, escena sola, `E-920`, 10 s | `03:00` y `+` cada 2 s hasta `07:00` | **El amanecer**: la luna se pone detrás del paisaje y sus líneas se apagan; el sol sale y las líneas vuelven, doradas; las estrellas se van con su fundido (las últimas, visibles a las 6:12), la ventana se apaga, aparece la bruma |
| **VID-07** | TV, 1,2 s | Libre a los 20 s: `Terminar` → `Terminar` | **La salida**: el HUD con la última foto; la cámara que empieza a subir; el negro a los 400 ms; Workout a los 640 |
| **VID-08** | TV, 9 s | `Corta` de los 29 a los 38 s | **El final**: a los 30,3 s el diálogo y la toma aérea: la cámara sube y retrocede 6 s y se queda quieta, y se lee en el lago alrededor del diálogo (el horizonte sube, el paisaje se detiene); el bote frena detrás del diálogo opaco (a la vista desde la 6b, con el vidrio) |
| **VID-09** | TV → TH, 2 s | Libre a los 20 s: rotar | El HUD con el fundido cruzado de 200 ms; la escena sigue sin reiniciarse ni saltar (la distancia sigue) |
| **VID-10** | TV, 20 s, escena sola | Libre con `R-4`, sin tocar nada | **La respiración**: movimiento sutil, sin mareo; el horizonte dentro de ± 3 dp de su línea (con la línea de contrato marcada) |
| **VID-11** | TV, 3 s | Libre con "escena demorada" 1,5 s | El fondo provisorio con la entrada normal; la escena aparece con un fundido de 0,6 s, al día, sin vuelo |

---

## 8. Qué se verifica dónde

| Dónde | Qué |
|---|---|
| **[dotnet]** | LT-62 a LT-77 y LT-82. La cámara (pose, proyección, inversa, respiración, el camino del vuelo y la toma aérea). La velocidad, la inercia, la frenada, el paso acotado y la distancia propia. La hora, la paleta (cada minuto, contra el oráculo), los ambientes, la luz de cada hora, las luces nocturnas. El astro del legacy (cada minuto y siete tamaños, contra el oráculo), la fuerza, el astro visible, la puesta y la salida (barrido minuto a minuto), la luz y sus sombras, la fase de la luna, el hemisferio y el halo. Los extremos del degradé del cielo; las proporciones del disco y del halo. Las 80 estrellas (contra el oráculo), su brillo, los bordes de 6:12 / 6:13 y 17:59 / 18:00 y el fundido; las 7 nubes. El alto de las cordilleras por ventana y la nieve. El generador del legacy (vectores dorados), los segmentos del lago (0 a 2 000, bit a bit), lo visible a varias distancias, el tope con un escenario de prueba, el olvido. Los colores de las cordilleras, la luz y la niebla de la orilla, el agua degradada (los tres colores y las 10 líneas). La transición de `Hora escena`. El contraste del HUD contra el peor fondo |
| **[Editor-EM]** | Las mismas pruebas dentro de Unity, con y sin simulador |
| **[Editor-PM]** | LT-78 a LT-81, LT-83 a LT-87. La cámara 3D contra las fórmulas (marcas). La escena como fondo, el ciclo de vida (de cero, sin reinicios, pausa, segundo plano, rotación, cierre, apagada fuera del inmersivo, escena demorada). El bote provisorio y la zona. Las bases de los objetos contra el legacy y contra el terreno (nada flota). Las medidas de los objetos. Los slots con el slot de prueba. La entrada, la salida, la toma aérea, el velo y reducir movimiento. Los topes por nivel y la CPU de la escena; 0 B por cuadro con el ayudante de cuadros reales y su control positivo; la adaptación con tiempos de cuadro de prueba. `Hora escena` con `-Simulator` |
| **[Captura]** | CAP-01 a CAP-24, LEG-01 a LEG-07, DEMO-01 a DEMO-08 y VID-01 a VID-11 (§7) |
| **[Device-Win]** | Build de simulador: la demo en la ventana real; `[PERF]` de AC-SCN-01-10 (libre con `Fuerte`, la hora, la entrada, `Serie larga`, el diálogo); la escena apagada en Workout; `Hora escena` que no sobrevive a reiniciar; tres entrenamientos sin avisos nuevos en el registro. Si el usuario tiene abierto su `RowMate.exe`, se espera y se reintenta (§12 punto 12) |
| **[Device-And]** | Solo con el módulo y el teléfono: 60 fps durante 30 min (AC-SCN-01-09), la fila de 200 píldoras (R4b-02), la adaptación térmica, el primer cuadro al volver. Hoy, **NO VERIFICABLE** (se arrastra a la It. 14) |
| **[QA]** | El legacy sin cambios (`git status --porcelain` vacío antes y después, también al correr el oráculo); nada de `Assets/ThirdParty/` versionado salvo los `.meta`; el CI sin el pack; las bases y las preferencias del usuario intactas; sin rutas de la PC en lo versionado (documentos, `manifest.json`, `INDEX.md`, registros citados) |

---

## 9. Checklist de paridad

| # | Comportamiento del legacy | Legacy | Test legacy | AC |
|---|---|---|---|---|
| 1 | Cámara a 2,2 m, remero a 9,5 m, horizonte 40 % / 42 %, focal = lado corto | `scene/scene_camera.dart:7-16` | `scene_camera_test:9` | AC-SCN-02-01 |
| 2 | Proyección y escala por distancia | `scene_camera.dart:28-37` | `:16` | AC-SCN-02-02 |
| 3 | Nada con z ≤ 0,5 m se proyecta; los objetos de la orilla salen de la lista con `relZ < 1 m` | `scene_camera.dart:19`, `:29`; `environment.dart:90`; `painters/shore_painter.dart:32-33` | `:30`; `environment_test:30` | AC-SCN-02-03 (DV-62) |
| 4 | Inversa sobre el agua | `scene_camera.dart:41-45` | `:34` | AC-SCN-02-02 |
| 5 | Una sola cámara fija, sin controles | spec §Decisiones | — | AC-SCN-02-08 |
| 6 | Velocidad objetivo `500/split` solo en marcha, con SPM > 0 y split > 0 | `scene_state.dart:36-43` | `scene_state_test:14` | AC-SCN-03-01 |
| 7 | Inercia de 1,5 s y corte a 0 bajo 0,01 m/s | `scene_state.dart:16`, `:55-56` | `:14`, `:26` | AC-SCN-03-02, -03 |
| 8 | Distancia integrada con la velocidad | `scene_state.dart:57` | `:14`, `:26` | AC-SCN-03-02 |
| 9 | Paso por cuadro acotado a [0; 0,1] s | `scene_state.dart:52`; `scene_view.dart:63` | — | AC-SCN-03-04 |
| 10 | En pausa y terminado, objetivo 0: el bote frena | `immersive_workout_screen.dart:84`; `scene_state.dart:38-43` | `:60` (velocidad; entero en la 7) | AC-SCN-03-03 |
| 11 | `shaderDistance` = distancia mod 4096 | `scene_state.dart:33-34` | `:55` | AC-SCN-03-06 |
| 12 | Entradas: split en segundos enteros, SPM, en marcha; el último dato | `scene_view.dart:65-70`; `immersive_workout_screen.dart:81-86`; `workout_provider.dart:220-224` | `scene_view_test:11` | AC-SCN-03-07 |
| 13 | Distancia de escena propia, no la del monitor | `scene_state.dart:57` | — | AC-SCN-03-05 |
| 14 | Hora: forzada o `hora + minuto/60`, en cada cuadro | `scene_view.dart:81-86` | — | AC-SCN-09-01 |
| 15 | Paleta: 6 keyframes con 8 campos | `time_of_day.dart:69-98` | `time_of_day_test:25`, `:32` | AC-SCN-09-02 |
| 16 | Interpolación lineal por canal (sin redondear) y del ambiente | `time_of_day.dart:29-38`, `:107-116` | `:25`, `:44` | AC-SCN-09-02 |
| 17 | Noche cerrada de 22 a 4 h y paso a amanecer de 4 a 5 | `time_of_day.dart:100-105` | `:32`, `:44` | AC-SCN-09-02 |
| 18 | Elevación del sol y umbral de noche ≤ −0,4 | `time_of_day.dart:52-55` | `:17`, `:37` | AC-SCN-10-01 |
| 19 | Posición del astro: hora espejada, elevación acotada, x y y | `time_of_day.dart:57-67` | `:9`, `:17` | AC-SCN-10-02 (contrato), AC-SCN-10-04 (DV-52) |
| 20 | Fuerza de la luz: 0,45 de noche; 0,2 a 1,0 de día | `time_of_day.dart:64` | `:9` | AC-SCN-10-03 |
| 21 | Salto del sol a la luna a las 19:34 y a las 4:26 | `time_of_day.dart:53-55` | `:37` | AC-SCN-10-05 (Q-63, DV-54) |
| 22 | Orden del cielo: degradé, estrellas, astro, nubes, silueta | `painters/sky_painter.dart:36-56` | — | AC-SCN-11-01 |
| 23 | Degradé lineal de skyTop (arriba) a skyHorizon (horizonte) | `sky_painter.dart:39-48` | — | AC-SCN-11-02 (DV-66) |
| 24 | 80 estrellas blancas con `Random(42)`, radios de 0,6 a 1,6 | `sky_painter.dart:58-64`, `:66` | — | AC-SCN-11-03, AC-SCN-09-04 |
| 25 | Oscuridad, titileo y opacidad de las estrellas | `sky_painter.dart:49-52`, `:65-66` | `sky_shore_painter_test:39` | AC-SCN-11-04 (Q-62, DV-53) |
| 26 | Disco y halo del sol (22 y 88 dp de radio); luna 0,8× sin sombra, con el mismo halo | `sky_painter.dart:71-86` | — | AC-SCN-11-05 (DV-63), AC-SCN-10-07 (DV-57) |
| 27 | 7 nubes: datos, deriva por distancia y por tiempo, altura, vuelta en ± 0,1·ancho | `sky_painter.dart:29-33`, `:95-96` | — | AC-SCN-11-06 (D-ART-06, DV-56) |
| 28 | Opacidad y color de las nubes por ambiente | `sky_painter.dart:91-93` | — | AC-SCN-11-06 |
| 29 | Segmentos de 50 m con semilla propia y caché | `environment.dart:43`, `:72-82` | `environment_test:10` | AC-SCN-14-01, -02 |
| 30 | Orilla a 9 m o más del eje (x en valor absoluto) salvo puente y boya | `environment.dart:45-46` | `:17` | AC-SCN-14-03 |
| 31 | Rango visible de `d + 1` a `d + 400`, de lejos a cerca | `environment.dart:47`, `:89-101` | `:30` | AC-SCN-14-04 |
| 32 | Tope y prioridad de los hitos sobre las boyas | `environment.dart:50`, `:102-113` | `:30` (y `:54`, regata, It. 8) | AC-SCN-14-05 |
| 33 | Olvidar los segmentos de atrás y regenerarlos iguales | `environment.dart:94`, `:116-120` | — | AC-SCN-14-06 |
| 34 | Lago: ola 0,25 / 2,5 y agua sin tinte | `lake_environment.dart:14-16`; `environment.dart:63` | — | AC-SCN-15-01 |
| 35 | Pinos: orden, frecuencia, x, escala y variante | `lake_environment.dart:22-34` | `environment_test:10`, `:17` | AC-SCN-15-02 |
| 36 | Muelle a `x = −9` cada 400 m desde los 175 | `lake_environment.dart:35` | — | AC-SCN-15-03 |
| 37 | Cabaña a `x = +13` cada 600 m desde los 360 | `lake_environment.dart:36-38` | — | AC-SCN-15-03 |
| 38 | Tope de 120 | `environment.dart:50` | `environment_test:30` | AC-SCN-15-04 |
| 39 | Dos cordilleras: perfil, alto (12 % del alto de pantalla), tonos, profundidad, nieve en los picos sobre el 75 % | `lake_environment.dart:43-85`; `environment.dart:127-129` | `sky_shore_painter_test:16` | AC-SCN-19-01 (DV-64, DV-65) |
| 40 | Las cordilleras se corren de costado con la distancia | `lake_environment.dart:48-51` | — | AC-SCN-19-02 (Q-65, DV-55) |
| 41 | Cada objeto en su base, escalado con la distancia | `shore_painter.dart:29-42` | — | AC-SCN-15-05 |
| 42 | Tinte de ambiente y niebla de 80 a 400 m (80 %) | `shore_painter.dart:45-50` | — | AC-SCN-20-01 |
| 43 | Noche de la orilla con ambiente < 0,6; ventana encendida | `shore_painter.dart:26`, `:121-122` | — | AC-SCN-09-06, AC-SCN-20-02 (DV-60) |
| 44 | Pino, cabaña y muelle: medidas y colores | `shore_painter.dart:89-131` | — | AC-SCN-15-06 |
| 45 | Agua de respaldo: degradé de 3 tramos | `water_painter.dart:79-90` | `water_painter_test:11` | AC-SCN-13-02 |
| 46 | 10 líneas de brillo que avanzan con la distancia, centradas en el astro del legacy, con alfa ≥ 0,05 siempre | `water_painter.dart:92-103` | `water_painter_test:11` | AC-SCN-13-03 (DV-59) |
| 47 | Precarga del agua y el respaldo mientras carga, sin errores | `workout_screen.dart:44`; `water_shader.dart:12-22`; `scene_view.dart:48-57` | `scene_view_test:11` | AC-SCN-13-01, AC-SCN-01-07 |
| 48 | Capas de la escena y del inmersivo; fondo negro | `scene_view.dart:97-121`; `immersive_workout_screen.dart:73-117` | `scene_view_test:11` | AC-SCN-01-02, AC-WKT-13-02 (DV-67) |
| 49 | Arranca de cero; la pausa no reinicia; el tiempo sigue en pausa | `scene_view.dart:38-41`, `:58`; `scene_state.dart:52-53` | — | AC-SCN-01-03 |
| 50 | Se adapta al tamaño sin reiniciarse | `scene_view.dart:90-92` | `scene_view_test:11` | AC-SCN-01-03 |
| 51 | Se libera sin errores, también cargando | `scene_view.dart:54-56`, `:74-79` | `scene_view_test:11` | AC-SCN-01-05 |
| 52 | La escena y el HUD se redibujan aparte | `immersive_workout_screen.dart:78-80`, `:99-100` | — | AC-SCN-01-10, -12 |
| 53 | Lago por defecto; no se cambia de escenario entrenando | `scene_settings.dart:13`; spec §Fuera de alcance | — | AC-SCN-21-01 |
| 54 | `Hora escena`: `real` / `H:00`, ±1 h, `Hora real`, en el acto, no se guarda | `simulator_overlay.dart:74`, `:139-150`; `scene_settings.dart:14`, `:19-24` | `simulator_overlay_test:17` (It. 2) | AC-SCN-23-01 a -03, AC-BLE-16-07 (DV-58) |
| 55 | El reloj de pared, con la hora real | `series_panels.dart:57-82` | — | AC-SCN-23-04, AC-WKT-19-03 |
| 56 | 60 fps en un teléfono de gama media | spec §Rendimiento | — | AC-SCN-01-09 |

---

## 10. Mejoras EXR (cómo se tiene que sentir)

Son agregados de presentación. Ninguno cambia un dato de la sesión, y los que cambian algo visible del legacy están en §11.1.

1. **Sumergirse en el agua.** Entrar a entrenar es un vuelo corto que baja desde atrás y arriba hasta el agua, detrás del bote, mientras el tablero llega. Se siente como "entrar" al lago, no como abrir una pantalla.
2. **Una cámara viva.** La cámara nunca está clavada: se mece apenas, como sobre el agua, sin marear y sin mover el horizonte más de 3 dp.
3. **La luz cuenta la hora.** Cada hora tiene su temperatura y su dirección: dorada y rasante al amanecer, blanca y alta al mediodía, cálida al atardecer, azul y plateada de noche. El cielo y el agua acompañan. El sol brilla como un sol, no es un círculo plano, y la nieve se enciende rosada en la hora dorada.
4. **El sol se pone detrás de las montañas.** Nada salta en el cielo: el sol baja y se esconde, sus brillos en el agua se apagan con él, la luna sale detrás del paisaje, las estrellas se encienden de a poco y las luces de las cabañas se prenden una a una.
5. **El paisaje viene hacia vos.** Los pinos, el muelle y la cabaña pasan a la velocidad del split; las montañas no se deslizan de costado: están lejos y se acercan apenas; las nubes se mueven con el viento.
6. **Una orilla de verdad.** Nada flota: los pinos están en tierra, el muelle en sus pilotes; hay canto rodado, pasto, juncos y un bosque detrás; a la mañana, bruma sobre el agua.
7. **Nada aparece de golpe.** Lo lejano nace en la niebla y lo que pasa al lado se desvanece; no hay objetos que salten en el horizonte ni que se corten en el borde.
8. **La pausa se siente.** La escena pierde color y el bote se desliza hasta quedar quieto; el lago sigue vivo alrededor.
9. **Un final de película.** Al completar la rutina, la cámara se eleva despacio sobre el bote mientras aparece el resumen.
10. **Fluidez primero.** Si el equipo no llega, se baja detalle, nunca la fluidez, y sin que se note de golpe.
11. **La hora del simulador como time-lapse.** Cambiar la hora en el simulador muestra la transición entera, en 1,5 s.

Se comprueba con AC-SCN-02-03 a -07, AC-SCN-09-05, -06, AC-SCN-10-05, AC-SCN-11-06, AC-SCN-14-07, AC-SCN-15-07, AC-SCN-19-02, AC-SCN-23-02, AC-WKT-13-12, AC-SCN-01-13 y los videos VID-01 a VID-11, que revisan QA y el Designer.

---

## 11. Decisiones aplicadas y gate de la It. 6a

El gate de la It. 5 quedó aceptado con los valores por defecto: DV-48 a DV-51, P-08 y lo demás de [REPORT de la It. 5 §8.1](../05-series-parciales/REPORT.md).

| Decisión | Qué se aplica | Confirmar en el gate |
|---|---|---|
| Q-61 (revisada) | El paisaje del lago es el del legacy a la misma distancia: el mismo generador, los mismos objetos (AC-SCN-14-02, AC-SCN-15-05) | No (ya decidida en el plan) |
| Q-62 | Estrellas solo con ambiente ≤ 0,85 (DV-53) | No |
| Q-63 | Puesta y salida graduales (DV-54) | No |
| Q-65 | El horizonte se acerca en lugar de deslizarse (DV-55) | No |
| Q-66 | Una sola cámara; la respiración como mejora EXR | No |
| Q-67 / R-04 | El agua degradada existe y es la de la 6a (AC-SCN-13-01) | No |
| Q-68 | Sin saltos a ninguna distancia (AC-SCN-01-15) | No |
| Q-69 | Las reglas de la escena en la lógica pura, con `dotnet` (AC-SCN-01-14) | No |
| D-ART-05 | Dos direcciones del astro con el mismo acimut: el disco donde fija ART_BIBLE §6.2 y la luz para sombras coherentes (DV-52) | No (del Architect; se adopta) |
| D-ART-06 | Las nubes sin deriva por distancia (DV-56) | No |
| **D-ART-08** | Texturas CC0 opcionales y la luna de la NASA: **no**, hasta que el usuario diga que sí (P-01) | **Sí** |
| **Terrain Sample Asset Pack** | Opcional: sin él, terreno y vegetación procedurales; si lo instala, entra por los slots (P-02) | **Sí** (acción del usuario, opcional) |
| **DV-57** | La luna con la fase real, el halo según la parte iluminada y el lado iluminado del hemisferio de la región (P-03) | **Sí** |
| **DV-63 a DV-67 y las ampliaciones de DV-52, -53 y -59** | Las diferencias de arte que trae el Designer ([04-design-notes §21.1](04-design-notes.md)), revisadas en el gate de QA: el sol más chico y brillante (DV-63), la nieve (DV-64), las montañas más bajas en vertical (DV-65), el degradé del cielo (DV-66), los degradés de pantalla del HUD (DV-67); el arco del sol de 12° (DV-52), el fundido de las estrellas (DV-53) y las líneas del agua con la visibilidad del astro (DV-59). Por defecto, se aceptan (P-18) | **Sí** |
| Constantes del gate (DT6a-01, -02, -07; C-02) | Rolido de 0,15° (AC-SCN-02-04); el astro, como mucho 0,3°/min (AC-SCN-10-05); la luz principal en 0 en el cambio de astro (AC-SCN-10-05); el agua degradada en ≤ 24 ALU con los agregados (AC-SCN-13-04) | No (del Architect y el Designer; se adoptan) |
| P-09 del plan | El Designer entrega en el diseño de la 6a la lista exacta de archivos de Mixamo para la It. 7 (§12 punto 15) | — |
| R-12 / regla 9 | 60 fps en el Android de gama media: NO VERIFICABLE sin teléfono, a la It. 14 (AC-SCN-01-09) | Informativa |
| **La toma aérea detrás del diálogo** (QA-V6a-03) | En la 6a el diálogo de completado, opaco y centrado como el legacy, tapa el bote durante la toma aérea; el bote se ve detrás del vidrio con desenfoque en la 6b. Por defecto, se acepta (P-19) | **Sí** |
| P-01 a P-19 | Preguntas abiertas con su decisión por defecto (§14) | Las marcadas "Usuario" |

### 11.1 Diferencias visibles con el legacy (a propósito)

Siguen la numeración de la It. 5. No incluyen las mejoras de §10 que no cambian nada del legacy (el vuelo, la respiración, la toma aérea, el terreno, el velo sobre la escena, la luz de cada hora): esas se agregan.

| ID | Legacy (verificado) | App | Motivo | AC |
|---|---|---|---|---|
| DV-52 | El astro va en coordenadas de pantalla: `x = fracción·ancho` (0,15 a 0,85) y `y = horizonte − elevación·0,9·horizonte`; al mediodía, casi en el borde de arriba (y = 33,8 en V390) (`time_of_day.dart:57-67`) | El disco va en una dirección del mundo: acimut `(fracción − 0,5)·70°`, elevación **`1,5° + 10,5°·elevación`** (12° al mediodía y a medianoche; 8,92° a las 9 y a las 15; la luna de las 22, 10,59°), siempre dentro del cuadro en vertical y en horizontal, con la misma progresión (izquierda, centro, derecha) | En 3D el sol del legacy al mediodía quedaría fuera de cuadro en horizontal, y una misma dirección no puede caer en las dos posiciones de pantalla del legacy (D-ART-05). Con 17° (lo que decía esta spec antes del gate) el sol del mediodía quedaba detrás de la barra superior en TH, L640 y W1280; con 12° queda debajo en los cinco tamaños de la app y se mueve como el sol real (≤ 0,26°/min). **Si el usuario lo rechaza:** 17°, con el sol del mediodía detrás de la barra en horizontal | AC-SCN-10-04, AC-SCN-10-05 |
| DV-53 | Estrellas casi invisibles a toda hora salvo las 12 (a las 15, oscuridad 0,115) (`sky_painter.dart:51-52`) | Sin estrellas con ambiente > 0,85: de 6:13 a 17:59 no hay ninguna. Al cruzar el umbral, el conjunto entra o sale con un fundido de 0,9 s en lugar de aparecer en un cuadro (en régimen, la fórmula del legacy) | Q-62: estrellas de día no son creíbles; el fundido, "nada aparece de golpe" (§10.7). **Si el usuario rechaza el fundido:** aparecen y se van en un cuadro | AC-SCN-11-04 |
| DV-54 | A las 19:34 el astro salta del sol en el horizonte a la derecha a la luna al 40 % de altura a la izquierda; a las 4:26, al revés | El sol se pone detrás del paisaje y la luna sale detrás del paisaje; el cambio pasa con los dos escondidos | Q-63 | AC-SCN-10-05 |
| DV-55 | Las cordilleras se deslizan a la izquierda 0,24 y 0,48 dp por metro remado (`lake_environment.dart:48-51`) | Quietas de costado; se acercan al 4 % de la velocidad del bote | Q-65: una cámara que avanza no ve el horizonte correrse | AC-SCN-19-02 |
| DV-56 | Las nubes se corren a la derecha también con la distancia (`distancia·0,0004·v`) (`sky_painter.dart:95`), y en la vuelta (± 0,1·ancho) la parte de las grandes que todavía asoma desaparece en un cuadro | Solo con el viento (el tiempo); en la vuelta, se desvanecen y entran con un fundido, con la misma `x` | D-ART-06, coherente con Q-65; "nada aparece de golpe" (§10.7) | AC-SCN-11-06 |
| DV-57 | La luna es un disco entero, sin sombra, de 0,8 veces el sol, con el halo a pleno (`sky_painter.dart:71-86`) | La fase real de la fecha, con la parte oscura apenas visible y el lado iluminado como se ve desde el hemisferio de la región del equipo (el del norte sin región); el halo, × `0,4 + 0,6·iluminación`; la luz de luna crece con la parte iluminada | Realismo (hallazgo 7 del audit; la spec del legacy pedía "disco con sombra"); en el hemisferio sur la luna se ve espejada. **Si el usuario lo rechaza:** disco entero y halo a pleno siempre | AC-SCN-10-07 |
| DV-58 | La hora forzada se aplica en el acto (`immersive_workout_screen.dart:85`) | Llega en 1,5 s, por el camino más corto del reloj (en el acto con reducir movimiento) | Ver la transición de luz en las demos; es una herramienta del simulador | AC-SCN-23-02 |
| DV-59 | Las líneas del agua van centradas en la x del astro del legacy y su alfa nunca baja de 0,05: a las 19:34 saltan de la derecha (el sol) a la izquierda (la luna) (`water_painter.dart:92-103`) | Centradas bajo el astro que se ve (DV-52) y con su alfa × la visibilidad de ese astro: se apagan mientras se pone y se encienden mientras sale; en el cambio de astro, ninguna. El dato (el alfa del legacy) no cambia | Coherencia: el brillo cae bajo el astro, y sin astro a la vista no hay brillo; así tampoco saltan de un lado al otro (Q-63). **Si el usuario rechaza la parte de la visibilidad:** el alfa del legacy siempre, con el salto a las 19:34 y a las 4:26 | AC-SCN-13-03, AC-SCN-13-04 |
| DV-60 | Todas las luces nocturnas se encienden juntas al cruzar ambiente 0,6 (`shore_painter.dart:26`) | Cada una en su minuto fijo dentro de los 20 min alrededor del umbral, con un fundido de 0,4 s | Un anochecer creíble (ANIMATION_BIBLE §2.12). Fuera de esa ventana, igual que el legacy | AC-SCN-09-06 |
| DV-61 | El bote con remero y remos, que reman | Hasta la It. 7, un casco quieto, sin remero ni remos | El remero es de la 7 (P-05) | AC-SCN-01-08 |
| DV-62 | Lo de la orilla sale de la lista en seco al pasar a menos de 1 m de la cámara (`zMin = d + 1`, `environment.dart:90`), aunque antes ya salió del cuadro por el costado (la orilla a 8,1 m en TH y a 18 m en TV); lo que proyecta con `z ≤ 0,5 m` no se dibuja (`scene_camera.dart:29`); lo lejano aparece en seco a los 400 m | Si algo cercano llegara a verse, se desvanece con un tramado entre 2 y 1 m; lo lejano aparece con un fundido entre 400 y 360 m. La lista de visibles no cambia | Hallazgo 2 del audit: nada salta | AC-SCN-02-03, AC-SCN-14-07 |
| DV-63 | El sol es un disco plano de 44 dp de diámetro con un halo de sunGlow de 176 dp (4 veces el radio, alfas 0,7 / 0,25 / 0), del mismo tamaño en dp en cualquier pantalla; la luna, 0,8 del disco con **el mismo** halo que el sol (`sky_painter.dart:71-86`) | El disco del sol mide 1,6° de radio (21,8 dp de diámetro en V390, 23 en TV, 40 en W1280) y brilla: con su resplandor (Alto y Medio) se lee de ≈ 40 dp en TV; en Bajo, plano. El halo, 4 veces el disco de cada astro, con los alfas del legacy (6,4°: ≈ 87 dp en V390); la luna, 0,8 del disco del sol y su halo, 4 veces su disco (0,8 del del sol) | Un disco plano de 44 dp con resplandor encima se ve "de dibujo" en una escena 3D; con el brillo, el sol se lee del tamaño del legacy en TV, y las proporciones (halo y disco, luna y sol) se conservan. AC-SCN-11-05 es [VISUAL]. **Si el usuario lo rechaza:** disco de 44 dp y halo de 176 dp, en dp, en todos los tamaños, y la luna con el halo del sol | AC-SCN-11-05, AC-SCN-10-07 |
| DV-64 | Nieve: un capuchón triangular en cada pico local del cordón lejano que pasa el 75 % del alto máximo, hasta el 82 % de su altura (`lake_environment.dart:70-84`). Como el perfil se desliza con la distancia, los picos nevados van cambiando: a 0 m hay uno solo; en V390, de 0 a 3 000 m, uno en el 83 % de los tramos, ninguno en el 11 % y dos en el 6 % | Nieve en todo lo que pasa el 58 % del perfil lejano, con borde suave y lenguas irregulares: con el perfil quieto, el macizo de la izquierda, el pico central y, en horizontal, el flanco del pico de la derecha (≈ 45 % del ancho del cordón en TV, ≈ 33 % en TH). Mismo color que el legacy, que en la hora dorada se tiñe apenas de sunGlow y de noche brilla con la luna | Con el perfil quieto (Q-65, DV-55) la regla del legacy deja un solo pico nevado, siempre el mismo (a −25,4°), que en vertical queda en el borde izquierdo, detrás de las tarjetas: "las lejanas con nieve" no se vería. **Si el usuario lo rechaza:** la regla del legacy sobre el perfil quieto | AC-SCN-19-01 |
| DV-65 | El alto máximo de las cordilleras es el 12 % del alto de pantalla (`lake_environment.dart:46`): el 30 % del cielo visible en vertical (≈ 14,6° en V390, 14,9° en TV) y el 28,6 % en horizontal (6,84°) | El mismo alto con un tope de 9°: en horizontal, igual que el legacy; en vertical, 9° (≈ 18 % del cielo, ≈ 60 % del alto del legacy) | Con el sol a 12° (DV-52) y las montañas al 30 % en vertical, el macizo deja el sol de la mañana detrás de la cresta hasta las ≈ 10:50 en TV y la luna de las 22 h apoyada en la cresta; con 9°, el sol sale entero a las ≈ 7:59 y la luna queda con ≈ 3,7° de aire (04-design-notes §6.2). **Si el usuario lo rechaza:** el 12 % del alto en vertical también, con esas dos consecuencias | AC-SCN-19-01 |
| DV-66 | El cielo es un degradé lineal en pantalla, de skyTop en el borde de arriba a skyHorizon en el horizonte, sin nada más (`sky_painter.dart:39-48`) | Los mismos dos tonos en los mismos lugares, con la luz más junta cerca del horizonte (a un cuarto del cielo ya va al 44 % del camino; el legacy, al 25 %), una franja de bruma de 1,5° (skyHorizon mezclado al 50 % con fog) que lo une con el paisaje y el agua, y un resplandor de sunGlow alrededor del sol en la hora dorada | Un cielo real se aclara cerca del horizonte, y la franja evita la costura entre el cielo, las montañas y el agua (AC-SCN-11-02). **Si el usuario lo rechaza:** el degradé lineal, sin bruma ni resplandor | AC-SCN-11-02 |
| DV-67 | Entre la escena y las métricas no hay nada (`immersive_workout_screen.dart:73-117`) | Detrás del HUD, dos degradés de `#06122A`: arriba, del 38 % a 0 en el 24 % del alto; abajo, del 32 % a 0 en el 18 % (ART_BIBLE §3.2). Son de la capa del HUD: no llegan a la zona del bote, no suman elementos y no están en las tomas de escena sola | Le dan aire a la barra y a los controles sobre un cielo claro o la nieve (H-05); ya estaban en ART_BIBLE §3.2. **Si el usuario lo rechaza:** sin degradés | AC-SCN-01-02, AC-WKT-13-02 |

**Para mostrarle al usuario:** CAP-02 contra LEG-02 (el mismo paisaje; el sol más bajo, más chico y brillante; las montañas más bajas en vertical y con más nieve), CAP-05 contra LEG-05 (en horizontal, las montañas del alto del legacy), CAP-04 contra LEG-04 (la noche y la luna con fase), CAP-11 (sin estrellas a las 15), CAP-13 (los degradés detrás del HUD) y VID-05 (el anochecer: el sol que se esconde, los brillos del agua que se apagan y vuelven con la luna, las estrellas que entran con su fundido).

### 11.2 Claves nuevas del catálogo (Q-01)

**Ninguna.** La escena no tiene textos. `Hora escena` y `Hora real` siguen fijos en español en los dos idiomas (Q-03 de la It. 2). El catálogo sigue en 223 claves por idioma.

---

## 12. Restricciones de producto para el Architect

1. **Primero la deuda de la 5** (§5.1): la verificación en el build y la prueba a mano, antes de construir encima. Si algo falla, va primero.
2. **La lógica, aparte (Q-69).** Toda regla de la escena (cámara, velocidad, hora, paleta, luz de cada hora, astro del legacy y astro visible, fase, estrellas, nubes, orilla, tope, niebla, agua degradada, transición de hora, luces nocturnas, respiración, vuelo) es lógica pura, probada con `dotnet` sin Unity. Unity solo la aplica a lo que se ve. Las funciones del legacy se portan **como contrato** aunque la vista use otra (el astro de pantalla, la nube con la distancia, `shaderDistance`).
3. **El oráculo.** Los valores esperados salen de correr el código del legacy (que importa `dart:ui`, así que con Flutter), **sin tocar el legacy** (la guardia de `tools/oracle`): el generador, los segmentos del lago, lo visible, la paleta por minuto, el astro por minuto y tamaño, las estrellas, los colores de la silueta, la luz y la niebla de la orilla y las líneas del agua; y, si se puede, las imágenes `LEG-nn`. Los colores se interpolan como el `Color.lerp` del Flutter del legacy (en punto flotante, `a·(1 − t) + b·t`, recortado a [0; 1] por canal). Nunca se lee el módulo de credenciales del legacy.
4. **La cámara 3D es la del legacy.** Lo que proyecta coincide con las fórmulas (AC-SCN-02-02) en los siete tamaños, con la respiración en 0.
5. **Sesiones largas.** Ninguna pérdida de precisión ni salto con la distancia (AC-SCN-01-15; ADR 015).
6. **Arte gratis detrás de slots** (decisión fija del usuario): Mixamo, paquetes gratis de Unity, procedural y shaders propios. Todo asset de terceros por su slot, con su placeholder procedural; un slot que usa el procedural a propósito (sin asignar) no avisa, y un asset asignado que falta o no valida deja un solo aviso; en `Assets/ThirdParty/`, ignorado por git salvo los `.meta`; el CI compila sin él. Sin texturas CC0 ni la luna de la NASA sin el OK del usuario (D-ART-08).
7. **Rendimiento con la tabla única** (ARCHITECTURE §20): los niveles se arman en esta iteración, la app elige el nivel sola, la escena no se dibuja fuera del inmersivo y la adaptación baja el detalle antes que la fluidez. La memoria por cuadro se mide en **cuadros reales** con el ayudante de la It. 5 y su control positivo, nunca con el contador del hilo (en Mono da 0).
8. **El presupuesto del HUD**: la escena no suma elementos al HUD (56-57 de 60; el corte de la fila de 200 píldoras suma 1).
9. **La presentación no toca el dato**: la escena lee la última lectura y la fase; nunca escribe en el motor, la sesión ni la telemetría, y su distancia no se muestra ni se guarda (AC-SCN-03-05, AC-WKT-13-14).
10. **Determinismo y aislamiento**: hora, fecha, región, tiempo de escena, distancia de escena, respiración y "escena demorada" controlables en las pruebas (§0.4); ningún doble llega a un build instalable. Ninguna prueba, captura ni video toca el archivo de preferencias ni las bases del usuario (carpeta de datos temporal).
11. **Identificadores estables para las pruebas**, por ejemplo `scene-root`, `scene-camera`, `scene-boat`, `scene-sky`, `scene-water`. Los nombres finales los fija el Architect.
12. **Builds de desarrollo**: solo en `Builds/Windows/simulator` (o `Builds/Windows/normal`). Si el usuario tiene abierto su `RowMate.exe`, no se lo cierra: se reintenta cada 2 minutos hasta 20 minutos y, si sigue abierto, esa parte queda NO VERIFICABLE. Nunca se suspende ni se apaga el equipo.
13. **Unity**: una sola instancia por proyecto; nunca se cierra la del usuario ni se editan fuentes mientras corre una propia. Si Unity revienta al cerrar un `-batchmode` que cambió los *defines* (0xC0000005), los *scripts* lo absorben.
14. **Repositorio público**: sin rutas de la PC, nombres de usuario ni datos del equipo en ningún documento, `manifest.json`, `INDEX.md` o registro citado; el legacy por su URL o `../rowmate-legacy`.
15. **La lista de Mixamo para la It. 7** (P-09 del plan): el Designer la entrega en su `04-design-notes` de la 6a, con los archivos exactos y los pasos (ASSET_SLOTS §2); el usuario la descarga antes de la 7.
16. **Strava**: nada en esta iteración.

---

## 13. Fuera de alcance

- **It. 6b**: el agua avanzada (ondas, reflejo del cielo y de la orilla, brillo del astro, estela, niebla del agua), la elección del agua avanzada o la degradada y la caída a la degradada; el vidrio del HUD con desenfoque; el cierre de F-SCN-01 y F-SCN-13. **El diálogo de completado con vidrio**: durante la toma aérea (AC-SCN-02-07) el bote se ve frenando, desenfocado, detrás del diálogo (QA-V6a-03; P-19).
- **It. 7**: el remero, los remos, la palada, el cabeceo del bote (y su parte en la respiración de la cámara), los charcos, la salpicadura y la sombra del bote del legacy; las medidas reales del bote (D-ART-04); la boya o marca de 500 m en el agua (P-06); `Series anteriores` abierto en vertical con el remero a la vista (P-03 de la 5).
- **It. 8**: el Río urbano, la Costa y el Canal de regata; el selector con miniaturas y `scene.environment`; los topes 60 y 200 con sus hitos; el resto del catálogo de objetos (edificios, puentes, boyas, faroles, faros, carteles, tribunas…) y de siluetas.
- **It. 14**: la medida en el teléfono si todavía no hay teléfono (regla 9); la háptica.
- **Backlog** (fuera de la paridad): cámaras extra y director (BKL-02, D-ART-07, D-ANI-07); otros botes y fantasmas (BKL-01); sonido del agua y del ambiente (BKL-03); clima (lluvia, viento fuerte); la vía láctea; las miniaturas a la hora actual (QA-P-11, gate de la 8).

---

## 14. Preguntas abiertas

| ID | Pregunta | Por defecto | Para |
|---|---|---|---|
| P-01 | **D-ART-08**: ¿se suman texturas CC0 (ambientCG, Poly Haven) y la luna de la NASA (dominio público)? | **No**: procedurales y, si el usuario lo instala, el Terrain Sample Asset Pack. Si dice que sí, entran por sus slots sin tocar lógica (ASSET_SLOTS §7) | Usuario (gate) |
| P-02 | ¿El Terrain Sample Asset Pack es necesario? | **No**: es opcional. La 6a se ve completa con terreno y vegetación procedurales (AC-SCN-15-08). Si el usuario lo instala más adelante, entra por los slots (ASSET_SLOTS §5.2); pesa 1,6 GB y no se versiona | Usuario (opcional) |
| P-03 | ¿La luna muestra la fase real? | **Sí** (DV-57), con la parte oscura apenas visible para que siempre se ubique. En luna nueva se verá apenas el disco: si al usuario no le gusta, disco entero siempre, como el legacy | Usuario (gate, con DV-57) |
| P-04 | ¿Los cambios de `Hora escena` llegan con una transición? | **Sí**, en 1,5 s por el camino más corto (DV-58); en el acto con reducir movimiento | — (PO) |
| P-05 | ¿El bote provisorio lleva un remero quieto? | **No**: el casco solo, hasta la 7 (DV-61). Un maniquí quieto adelantaría el trabajo de la 7 y se vería peor que un casco vacío | — (PO) |
| P-06 | ¿La boya de 500 m en el agua va en la 6a? | **No: va a la It. 7**. Tiene que pasar junto al bote cuando el panel cierra la vuelta (metros del monitor), y la escena tiene su propia distancia con inercia: se resuelve con el bote, el remero y la estela a la vista | — (PO) |
| P-07 | O-2 de la 4b: ¿60 fps con el diálogo de completado abierto? | **60 mientras algo se mueve** (la toma aérea, el conteo de los totales, el bote frenando); **cuando todo quedó quieto, puede bajar a 30** (las nubes y las estrellas son lentas). Es el ritmo de reposo de ARCHITECTURE §20.2 | — (PO) |
| P-08 | ¿Las luces de la noche se encienden de a una? | **Sí** (DV-60), dentro de 20 min alrededor del umbral del legacy | — (PO) |
| P-09 | ¿Se agrega vegetación alta entre los pinos del legacy? | **No** a menos de 18 m del eje: ahí solo los objetos del legacy y vegetación baja (≤ 1,2 m), para que el paisaje del legacy se reconozca (Q-61). El bosque agregado va más allá. Si el Designer necesita otra franja, la propone en `04-design-notes` | Designer (con el PO) |
| P-10 | ¿Se suma un campo de estrellas tenue además de las 80 del legacy? | **Se admite, solo en el nivel Alto**, más tenue que las 80 y con la misma regla de cuándo se ven (AC-SCN-11-03; 04-design-notes §4.5) | Designer (decidido en el gate) |
| P-11 | ¿Qué alto tienen las cordilleras en cada orientación? | **Decidido en el gate**: el del legacy (12 % del alto de pantalla) con un tope de 9°: en horizontal, igual que el legacy; en vertical, 9° (DV-65; AC-SCN-19-01) | Designer (decidido; el usuario confirma DV-65) |
| P-12 | ¿Qué pasa si la escena no está lista al entrar? | El fondo provisorio de la 4a y la escena con un fundido de 0,6 s cuando está lista, sin que la sesión espere (AC-SCN-01-07) | — (PO) |
| P-13 | ¿Qué se compara con el legacy, si su escena es 2.5D? | Los objetos de la orilla (posición exacta, AC-SCN-15-05), los colores de cada hora (tono dominante, AC-SCN-09-05) y la composición. No se comparan las nubes, el astro ni las montañas, que cambian a propósito (DV-52, DV-55, DV-56, DV-63 a DV-66); las tomas de escena sola van sin los degradés del HUD (DV-67) | — (PO) |
| P-14 | ¿La escena puede quedar cargada en Workout? | **Sí**, sin dibujarse, para que la entrada sea inmediata, como el legacy precarga el agua en la vista de reposo (`workout_screen.dart:44`), siempre dentro de los topes de memoria | Architect |
| P-15 | Con reducir movimiento, ¿la entrada es el fundido de 120 ms de la 4b o el de 300 ms de ANIMATION_BIBLE §5.3? | **120 ms**, uno solo para todo el inmersivo (AC-WKT-13-06) | — (PO) |
| P-16 | ¿El bote sigue avanzando durante un corte de lecturas? | **Sí** (paridad): el legacy conserva el último dato; el bote frena cuando la lectura dice SPM 0 o cuando la sesión pausa (AC-SCN-03-07) | — (PO) |
| P-17 | ¿Se adoptan los valores de luz de cada hora de ART_BIBLE §6.3? | **Sí**, como contrato de la 6a; el Designer los puede ajustar con las capturas, dejando la tabla al día | Designer |
| P-18 | ¿Se aceptan las diferencias de arte del Designer revisadas en el gate de QA: el sol más chico y brillante (DV-63), la nieve sobre el 58 % (DV-64), las montañas más bajas en vertical (DV-65), el degradé del cielo con bruma (DV-66), los degradés de pantalla del HUD (DV-67), el arco del sol de 12° (DV-52), el fundido de las estrellas (DV-53) y las líneas del agua que se apagan con el astro (DV-59)? | **Sí, todas**. Cada una tiene en §11.1 qué pasa si se rechaza (vuelve lo del legacy o el valor anterior de esta spec), sin tocar lógica: son constantes de arte | Usuario (gate) |
| P-19 | En la toma aérea del final, el diálogo de completado (opaco y centrado, como el legacy) tapa el bote (QA-V6a-03). ¿Se corre o se achica el diálogo para ver el bote nítido, o alcanza con verlo desenfocado detrás del vidrio en la 6b? | **Detrás del vidrio en la 6b**: el diálogo queda centrado como el `AlertDialog` del legacy y de la 4b (sus totales son lo importante en ese momento); en la 6a la toma se lee alrededor del diálogo (el horizonte que sube, el lago que se abre) y el bote queda tapado. **Si el usuario lo quiere nítido:** el Designer propone otra ubicación del diálogo (arriba en vertical, a un costado en horizontal) para la 6b, sin cambiar su contenido ni cuándo aparece | Usuario (gate, con P-18) |

---

## 15. Discrepancias encontradas al especificar

| Dónde | Qué dice | Qué es (verificado) | Acción |
|---|---|---|---|
| ROADMAP §6, fila D-ART-05 | "Se adopta: el sol se ve donde lo ubica el legacy" | Con ART_BIBLE §6.2 el disco va a 17° al mediodía (12° desde el gate, DV-52; el Designer alinea la biblia en WP-D2) y con acimut `(x − 0,5)·70°`: no cae en la posición de pantalla del legacy (y = 33,8 en V390 al mediodía), y no puede caer en las dos orientaciones a la vez | DV-52. **Pendiente** (PO, al cerrar): "el sol se ve en la misma zona del cielo y con la misma progresión que el legacy" |
| ART_BIBLE §6.2, la luna | La luna "con las mismas fórmulas, con `e(h')` y `salidaLuna`"; "el astro entra y sale detrás del paisaje (−2,5°)" | Con la fórmula del sol (`1,5° + 15,5°·e − 4°·…`), a las 19:34 la luna tiene `e(h') = 0,4` y quedaría a ≈ +3,7°, no a −2,5°: aparecería en el cielo abierto si las montañas no la tapan en ese acimut. Lo mismo a las 4:26 | AC-SCN-10-05 pide los dos astros ≤ −2,5° en el cambio. El Designer y el Architect dejan la fórmula de la luna explícita |
| ASSET_SLOTS §1.2, `sky.stars` | "Hash por dirección en el shader del cielo" | ARCHITECTURE §14.1 y LEGACY_MAPPING piden las 80 estrellas de `Random(42)`, y el inventario las marca [EXACTO] | AC-SCN-11-03: las 80 son obligatorias; el campo por *hash* solo como agregado tenue (P-10) |
| ANIMATION_BIBLE §5.3 | Con reducir movimiento, la entrada es un fundido de 300 ms | AC-WKT-13-06 de la 4b: fundido de 120 ms lineal para todo el inmersivo | P-15: 120 ms. El Designer ajusta la biblia |
| ANIMATION_BIBLE §2.11, la salida | "Cámara se eleva y se aleja (1,2 s) → fundido a negro desde 0,8 s → Workout entra" | AC-WKT-13-08: la salida dura 640 ms (HUD 0-200, negro 200-400, Workout 400-640), menos de 1 s | AC-SCN-02-06: la cámara arranca con la salida y el negro la cubre a los 400 ms. El Designer ajusta la biblia |
| Inventario F-SCN-03 | `scene_state_test:60` como test de F-SCN-03 | ROADMAP §5 lo asigna a la It. 7 (fase y velocidad en pausa) | La parte de velocidad va en AC-SCN-03-03; el caso entero, en la 7 |
| ROADMAP §5 y LEGACY_MAPPING, `sky_shore_painter_test` | "6a (cielo y lago)"; LEGACY_MAPPING pone en la 6a `PropCullingTests (… puente encima no rompe)` | `:57` dibuja los 15 tipos a la vez, con el puente y el cartel `750 m`: necesita el catálogo de la 8 | `:57` va a la It. 8; el recorte de lo que queda detrás de la cámara se prueba en la 6a con el lago (AC-SCN-02-03) |
| ROADMAP §2 It. 6a, "También toca" | F-SCN-01, -13, -19, -20, -21, F-WKT-13 y F-BLE-16 | La 6a también cambia AC-WKT-14-19 (la fila de 200 píldoras con la escena), AC-WKT-17-07 (el fundido al rotar, de la 4b §13), AC-WKT-17-09 y suma AC-WKT-17-13, y cambia AC-APP-05-08 | **Pendiente** (PO, al cerrar): sumar F-WKT-14, F-WKT-17 y F-APP-05 a "También toca" de la 6a (§2 y la matriz §4) |
| STATUS, "Para la 6a" | "la boya de 500 m en el agua (6a o 7)" | — | P-06: It. 7. **Pendiente** (PO, al cerrar): STATUS y ROADMAP |
| Inventario F-SCN-13 | Precarga en `workout_screen.dart:42-44` | La llamada está en la línea 44; el comentario en 41-43 | Informativa |
| `scene/time_of_day.dart:53-54` (comentario) | Noche de "≈19:35 a ≈4:25" | Con `≤ −0,4`, de 19:34:19 a 4:25:41; el inventario dice ≈ 19:34 a 4:26 | Informativa: manda el código |
| ROADMAP §0, prerrequisitos de la 6a | Sin el pack, "Texturas procedurales de terreno (digno, menos realista)" | Coincide con esta spec | — |
| Esta spec antes del gate, DV-62 y AC-SCN-02-03 | "Lo cercano se corta en seco al pasar los 0,5 m"; el ejemplo de un pino que se acerca en un teléfono horizontal | En la orilla actúa `visibleProps` (`zMin = d + 1`, `environment.dart:90`): lo cercano sale de la lista a 1 m; el `minZ` de 0,5 solo actúa con objetos pasados a mano (`sky_shore_painter_test:57`, It. 8). Y la orilla sale del cuadro por el costado mucho antes (8,1 m en TH; 18 m en TV) | Corregido en el gate (QA-6a-PAR-04): DV-62 y AC-SCN-02-03 con 1 m y el tramado entre 2 y 1 m |
| Esta spec antes del gate, AC-SCN-09-04 | "El color … de las estrellas" sale de la paleta | Son blancas (`Colors.white`, `sky_painter.dart:66`); de la paleta sale su opacidad | Corregido en el gate (QA-G6aT-20) |
| Esta spec antes del gate, AC-SCN-02-01 | "El remero proyecta en el 0,504 del alto en vertical" | 0,504 en TV y P360; 0,507 en V390 (`0,40 + (ancho/alto)·2,2/9,5`); 0,652 en los cuatro horizontales | Corregido en el gate (QA-G6aT-20) |

---

## Cambios por gate QA

Gate de armonía: [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (los dos, APROBADO CON OBSERVACIONES). Fecha: 2026-11-03. Ningún issue se disputa entero; dos se resuelven distinto de una de las opciones que ofrecía QA y se explica por qué (puntos 1.3 y 2).

### 1. QA-6a-PAR-01 (mayor) y QA-G6aT-02 (mayor): las diferencias de arte y un solo número por constante

El PO revisó cada diferencia que traen [04-design-notes](04-design-notes.md) (§1.1, §3.3, §4.2, §4.3, §6.2, §6.5, §8.1 paso 2b, §12.3 y §21.1) contra el código del legacy y la decide **por defecto aceptada**, con su DV (legacy verificado, app, motivo y qué pasa si el usuario la rechaza) y con P-18 para que el usuario la confirme en el gate. Ninguna cambia un dato de la sesión; todas son constantes de arte que se cambian sin tocar lógica.

| Punto de QA | Legacy (verificado) | Decisión | Dónde |
|---|---|---|---|
| (1) Disco del sol y halo | `r = 22` dp y halo `r·4` (`sky_painter.dart:73-85`) | Se acepta: disco de 1,6° de radio con brillo, halo de 4 discos, alfas del legacy | **DV-63** (nueva); AC-SCN-11-05; CAP-02 |
| (2) Halo de la luna | el mismo `r·4` que el sol, a pleno (`:74-82`) | Se acepta: 4 veces su disco (DV-63) y × `0,4 + 0,6·iluminación` (DV-57) | DV-57 ampliada; AC-SCN-10-07 |
| (3) Alto de las cordilleras en vertical | `hMax = 0,12·alto` (`lake_environment.dart:46`) | Se acepta el tope de 9° en vertical; **en horizontal, el alto exacto del legacy** (ver 1.3) | **DV-65** (nueva); AC-SCN-19-01; P-11 cerrada |
| (4) Nieve | picos locales > 0,75·hMax, capuchón hasta 0,82 (`:70-84`) | Se acepta 0,58 con borde suave | **DV-64** (nueva); AC-SCN-19-01 |
| (5) Líneas del agua × visibilidad | alfa `0,05 + 0,1·(1 − t)·fuerza`, nunca < 0,05 (`water_painter.dart:92-103`) | Se acepta en pantalla; el dato (el alfa del legacy) no cambia | DV-59 ampliada; AC-SCN-13-03, -04; CAP-08, -09; VID-05, -06 |
| (6) Fundido de las estrellas | aparecen en un cuadro | Se acepta 0,9 s al cruzar el umbral | DV-53 ampliada; AC-SCN-11-04; AC-APP-05-08 |
| (7) Degradé del cielo | lineal de `y` 0 al horizonte (`sky_painter.dart:39-48`) | Se acepta `1 − (1 − q)²`, la franja de bruma y el resplandor dorado, con los dos tonos de la paleta en sus lugares | **DV-66** (nueva); AC-SCN-11-02 |
| (8) *Scrims* | no hay nada entre la escena y el HUD (`immersive_workout_screen.dart:73-117`) | Se aceptan como capa del HUD (ART_BIBLE §3.2): **no** van en las tomas de escena sola (QA-6a-PAR-02) | **DV-67** (nueva); AC-SCN-01-02; AC-WKT-13-02; §0.4; §7.1; CAP-13 |
| (9) Hemisferio sur | disco entero | Se acepta, con la región del sistema (sin región, el norte) | DV-57 ampliada; AC-SCN-10-07; §0.4 (región de prueba); §12 punto 10 |
| (10) DV-52 con el arco de 12° | `y = horizonte − e·0,9·horizonte` (`time_of_day.dart:57-67`) | Se acepta 12° (C-01) | DV-52 ampliada; AC-SCN-10-04, -05; CAP-02, -04, -10, -11 |

**1.1 Un solo número por constante** (lo que esperan las pruebas de dotnet de WP-A):

| Constante | Antes del gate | Ahora | AC |
|---|---|---|---|
| Rolido de la respiración | 0,18° | **0,15°** (DT6a-01; horizonte ≤ 2,93 dp) | AC-SCN-02-04 |
| Arco del astro | `1,5° + 15,5°·e` (17°) | **`1,5° + 10,5°·e`** (12°; 9 y 15 h: 8,92°; 22 h: 10,59°; 0 h: 12°) | AC-SCN-10-04 |
| Tope de velocidad del astro | 0,2°/min | **0,3°/min** (medido ≈ 0,257) | AC-SCN-10-05 |
| Luz principal en el cambio de astro | 0,05 | **0** | AC-SCN-10-05 |
| Estrellas al minuto | "aparecen a las 18:00 y se van a las 6:12" | **6:12 sí, 6:13 no, 17:59 no, 18:00 sí**; fundido de **0,9 s** | AC-SCN-11-04 |
| Disco del sol | 44 dp de diámetro en V390, halo 176 | **1,6° de radio** (21,8 dp en V390), halo **4 ×** el disco; sonda: ≥ 30 dp con luminancia > 0,9 en TV, Alto | AC-SCN-11-05 |
| Líneas del agua | alfa del legacy | dato: alfa del legacy; **en pantalla × la visibilidad del astro** | AC-SCN-13-03 |
| Costo del agua degradada | ≤ 12 ALU | **≤ 24 ALU** (DT6a-07) | AC-SCN-13-04 |
| Nieve | picos > 75 % | **≥ 58 %** del perfil, con borde suave | AC-SCN-19-01 |
| Alto de las cordilleras | "≈ 30 % del cielo en las dos orientaciones" | **`0,12·alto de pantalla` con tope de 9°**: 6,84° en horizontal, 9° en vertical; el macizo a ≈ 0,89 de ese alto | AC-SCN-19-01 |
| Toma aérea | 0,4 m/s, 6 s | lo mismo, **con cabeceo de −4°** y la respiración que se apaga (DN6a-07) | AC-SCN-02-07; CAP-22 (horizonte ≈ 337 dp en TV) |

Las tomas: CAP-01 (el sol detrás del macizo; entero a las ≈ 7:59 en vertical), CAP-02 (12°, el sol más chico, las montañas más bajas, la nieve), CAP-03 (la cabaña en contraluz), CAP-04 (10,6°; la parte oscura a la derecha), CAP-05 (las montañas del alto del legacy), CAP-08 y CAP-09 (las líneas con la visibilidad), CAP-10 (12°), CAP-11 (8,9°), CAP-13 (los degradés) y CAP-22 (cabeceo). "Para mostrarle al usuario" (§11.1) suma el sol más chico, las montañas más bajas en vertical, la nieve, los degradés y las líneas que se apagan.

**1.2 Lo que también se integró de las notas (precisiones, sin DV):** DN6a-20 (la respiración entra en 0,6 s; AC-SCN-02-04, -05), DN6a-08 (el vuelo sin acelerones ni zoom brusco; AC-SCN-02-05), DN6a-10 (P2: el minuto de la hora real en 1 s; AC-SCN-09-01 y AC-APP-05-08), DN6a-21 (la sonda del brillo; AC-SCN-11-05) y P-10 (el campo tenue de estrellas, solo en Alto).

**1.3 Donde la spec no toma el número exacto de las notas.** El alto de las cordilleras en horizontal. 04-design-notes §6.2 usa `tan Θ = mín(0,30·horizonte/focal; tan 9°)` y dice que "en horizontal es el legacy exacto". No lo es: el legacy usa `0,12·alto/focal` (28,6 % del cielo en horizontal, no 30 %), así que esa fórmula da 7,18° contra 6,84° (+5 %; QA-6a-PAR-03). Como la intención de las notas es el legacy exacto en horizontal, la spec fija el número del legacy: `tan Θ = mín(0,12·alto/focal; tan 9°)`. En vertical no cambia nada (9° con las dos fórmulas). Con ese alto, recalculado con un programa propio de .NET 8 en `<scratch>/it6a/po-gate/calc` (el perfil del legacy por acimut, las ventanas de visibilidad de 04-design-notes §3.2 y el arco de 12°), en horizontal el borde del sol asoma a las ≈ 6:28 y el disco sale entero a las ≈ 7:31 (las notas, con 7,18°: 6:32 y 7:35), y la luna de las 22 h queda con ≈ 5,0° de aire (las notas: 4,8°). En vertical, 6:53 y 7:59 y 3,7° de aire, como las notas. Ese programa reproduce los números de las notas con su fórmula (6:32, 7:35, 7:59 y 10:49-10:50 en TV sin tope), así que la diferencia es solo la fórmula. **El Designer alinea §6.2 y §4.4** (QA-6a-PAR-03 es suyo).

### 2. QA-6a-PAR-04 y QA-G6aT-20 (c) (menores): el corte cercano

Corregidos DV-62, AC-SCN-02-03 y el punto 3 del *checklist*: el legacy saca lo de la orilla de la lista a **1 m** (`zMin = d + 1`, `environment.dart:90`); `minZ` 0,5 solo actúa con objetos pasados a mano. El ejemplo del pino en un teléfono horizontal se reemplazó por las distancias reales a las que la orilla sale del cuadro (8,1 m en TH, 8,3 en H844, 10,1 en L640 y W1280, 18 en vertical; la punta del muelle a 5,4 m en TH).

**De las dos opciones que daba QA** (una banda cercana propia del *streamer* desde `d + 0,5`, o aceptar el corte a 1 m), la spec toma una tercera, más simple: el tramado de lo cercano va **entre 2 y 1 m**. Así sale de la misma lista del legacy `[d + 1; d + 400]` (LT-77 sin tocar) y no hace falta ninguna banda propia. Lo de 0,5 m deja de tener sentido de producto: con la cámara del contrato nada de la orilla se ve a menos de ≈ 5 m de la cámara (la punta del muelle, a 5,4 m en TH), en la toma aérea tampoco (la cámara está 1,7 m más atrás), y durante el vuelo ningún objeto deja la lista estando a la vista ([03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) §3, caso 24). **El Architect** cambia "de 2 a 0,5 m" por "de 2 a 1 m" en 02-tech-design §3.6, y **el Designer**, en 04-design-notes §7.6.

### 3. QA-6a-PAR-09 (menor): las estrellas al minuto

AC-SCN-11-04 dice ahora: visibles hasta las 6:12 inclusive (0,8499999999999999) y desde las 18:00 (0,85 exacto); a las 6:13 (0,8521) y a las 17:59 (0,8504), ninguna; con la hora armada como el legacy, `hora + minuto/60` (también en §0.4). DV-53 dice "de 6:13 a 17:59". VID-06 dice "las últimas, visibles a las 6:12". **El Architect** suma a STR 6:12 sí y 17:59 no.

### 4. QA-G6aT-20 (menor): precisiones

| | Antes | Ahora | Dónde |
|---|---|---|---|
| (a) | 100 criterios | **101 ID** (78 nuevos + 11 que cambian + 12 que se vuelven a verificar; AC-BLE-16-02 y -03 en una fila) | Cabecera; §4.0 |
| (b) | el borde de la hueca ≥ 3:1 | ≥ 3:1 contra el vidrio en su peor fondo; contra su propio interior al 18 % (tenue de contrato), **excepción contada** de 2,93 a 3,02, como la pista al 30 % de la 4b | AC-WKT-17-13 |
| (c) | 0,5 m | 1 m | DV-62; AC-SCN-02-03 (punto 2) |
| (d) | "el color … de las estrellas" de la paleta | las estrellas son **blancas** (`sky_painter.dart:66`); de la paleta sale su opacidad | AC-SCN-09-04; *checklist* 24 |
| (e) | "puede quedar cargada … sin dibujarse" | un **único dibujo de calentamiento fuera de pantalla** antes de quedar lista; después, 0 dibujos fuera del inmersivo | AC-SCN-01-06; DEMO-01 (cuenta después de quedar lista) |
| (f) | 0,504 en vertical | **0,504 en TV y P360**; 0,507 en V390; la fórmula por ventana; 0,652 en los cuatro horizontales | AC-SCN-02-01; AC-WKT-17-09; §15 |

### 5. Menores de otros dueños con una parte del PO (baratos, alineados)

| Issue (dueño) | Parte del PO | Dónde |
|---|---|---|
| QA-6a-PAR-02 (Designer) | Los degradés de pantalla son de la capa del HUD: la escena sola va sin ellos. AC-SCN-11-02 no cambia de sonda: se mide en la escena sola | §0.4; §7.1; AC-SCN-11-02; DV-67 |
| QA-6a-PAR-03 (Designer) | El texto de AC-SCN-19-01 con el alto del legacy y el pico del macizo a ≈ 0,89 | AC-SCN-19-01; DV-65; punto 1.3 |
| QA-6a-PAR-06 (Architect) | El recorte a [0; 1] de `Color.lerp` nombrado | AC-SCN-09-02; §12 punto 3 |
| QA-6a-PAR-08 (Designer) | Se elige la mejora (no la paridad del corte): en la vuelta, la nube se desvanece y vuelve con un fundido, con la `x` del legacy | AC-SCN-11-06; DV-56 |
| QA-6a-PAR-10 (Architect) | Las marcas [dotnet] de las partes de contrato: los extremos del degradé y las proporciones del disco y del halo | AC-SCN-11-02; AC-SCN-11-05; §8 |
| QA-G6aT-03 (Architect) | La escena demorada se prepara en tramos, también después de la cascada, sin cuadros de más de 16,7 ms (tampoco su primer dibujo) | AC-WKT-13-15 |
| QA-G6aT-04 (Architect) | Un slot con el procedural a propósito no avisa; solo avisa un asset asignado que falta o no valida (un aviso). Sin el pack, sin avisos | AC-SCN-15-08; §12 punto 6 |
| QA-G6aT-13 (Designer) | `#FFE082` es el color de la ventana antes del ajuste de color de la hora, que vale para toda la imagen (como AC-SCN-15-06) | AC-SCN-09-06; AC-SCN-20-02 |
| QA-G6aT-17 (Architect) | La salida se mide de 0 a 1 s desde la confirmación de `Terminar`; el dibujo de calentamiento y DEMO-01, como en el punto 4 (e) | AC-WKT-13-15; AC-SCN-01-06; DEMO-01 |

### 6. Lo que tienen que alinear los otros documentos

- **02-tech-design** (Architect): los valores por defecto de `CelestialSpec` (arco 10,5°, luz mínima 0, tope 0,3°/min), `BreathingSpec` (0,15°) y `HorizonSpec` (`0,12·alto/focal` con tope de 9°; nieve 0,58); el fundido de `StarField`; STR con 6:12 / 6:13 / 17:59 / 18:00; §3.6 con el tramado de 2 a 1 m; `HideForCapture` que también apaga los degradés de pantalla en la escena sola; el control de la región de prueba; `It06aIds` con 101 ID; DEMO-01 que cuenta después de `Ready`; la regla de `[SLOT]` del punto 5; §8.2 (C-01 a C-19) cerrada con estas decisiones (C-05 queda en la spec, como ya aceptó el Designer).
- **04-design-notes** (Designer): §6.2 con la fórmula del legacy y §4.4 con los minutos de horizontal (punto 1.3); §7.6 con 2 a 1 m; §12.3 sin los degradés en la escena sola; §5.1 con el fundido en la vuelta de las nubes; §10.4 con la sonda de la ventana antes del ajuste de la hora; §3.1 (b), la fuerza del legacy a las 19:34 (0,20, QA-6a-PAR-07).

### 7. Verificación hecha

1. **Legacy, solo lectura** ([rowmate-legacy](https://github.com/figuibej/rowmate-legacy), `3ae0f3a`): `scene/painters/sky_painter.dart` entero; `scene/environments/lake_environment.dart:40-86`; `scene/painters/water_painter.dart:55-108`; `scene/environment.dart:85-121`; `scene/painters/shore_painter.dart:20-55`; `scene/scene_camera.dart`; `scene/time_of_day.dart:50-68`, `:100-118`; `immersive_workout_screen.dart:70-120`. No se leyó el módulo de credenciales. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar.
2. **Programa propio** de .NET 8 en `<scratch>/it6a/po-gate/calc` (sin red, fuera del repo): el perfil del legacy por acimut, el alto por ventana con las dos fórmulas, el arco de 12° con las ventanas de visibilidad, la hora en que el sol asoma y sale entero, y el aire de la luna de las 22 h. Reproduce los números de 04-design-notes §6.2 y §4.4 con su fórmula. Las cifras del remero, la orilla fuera de cuadro, las estrellas al minuto y el disco en dp salen de las fórmulas del legacy, a mano, y coinciden con las de QA.
3. **El repositorio**: solo se escribió esta spec. Sin código, sin Unity, sin *commits*. Rutas relativas, el legacy por su URL o `../rowmate-legacy`, el *scratch* como `<scratch>`.

---

## Cambios por verificación (ronda 1)

Entrada: [05-qa-verification](05-qa-verification.md) (QA-V6a-03 mayor y QA-V6a-05 menor, del PO) y [05-qa-lens-paridad](05-qa-lens-paridad.md) (QA-V6a-PAR-03 y -04, menores, del PO). Lo de esta sección manda sobre lo anterior, también sobre "Cambios por gate QA".

### 1. QA-V6a-03 (mayor): la toma aérea pasa detrás del diálogo de completado

**Lo que se vio.** La cámara cumple AC-SCN-02-07 (modo aéreo a los 30,333 s de `Corta`, quieta a los 36,333, cabeceo −4°, horizonte +28,77 dp, popa en 553,2 dentro de la zona), pero el diálogo de completado de la 4b, opaco y centrado, ocupa justo la zona del bote: en TV lo tapa entero (CAP-22, VID-08) y en W1280 asoma solo la popa debajo (build). "El bote sigue a la vista, frenando" no se cumple.

**Las dos salidas que daba QA.** (A) Correr o achicar el diálogo fuera de la zona del bote. (B) Aceptar que en la 6a el bote quede detrás del diálogo opaco y que se vea a través del vidrio con desenfoque de la 6b.

**Decisión: (B)**, con la confirmación del usuario en el gate (P-19). Motivos:

- **Paridad.** El legacy muestra el completado con un `AlertDialog` centrado y no descartable (`immersive_workout_screen.dart:207-212`); la 4b lo portó así y verificó su geometría en los cinco tamaños (AC-WKT-21-01, `it4b`). La toma aérea es una mejora EXR que se agrega: no puede mover un diálogo del contrato.
- **Lo que importa en ese momento** son los totales del diálogo; la toma es el fondo que acompaña. Se sigue leyendo alrededor del diálogo: el horizonte sube ≈ 29 dp, el lago se abre y el paisaje se va deteniendo.
- **Ya estaba previsto por el Designer**: ART_BIBLE §3.5 deja el diálogo "opaco, con vidrio desde la 6b" y 04-design-notes §11.5 y la fila de CAP-22 ponen "el diálogo opaco encima". Con el vidrio de la 6b el bote se va a ver frenando, desenfocado, detrás del diálogo: es lo que pedía la frase y llega con la iteración que trae el desenfoque.
- **(A) cuesta mucho para la 6a**: cambiaría el diseño y las tomas verificadas de la 4b (`it4b`, `it5`), y correrlo a un costado en horizontal lo pondría sobre las piezas del HUD (en L640, con 640 dp de ancho, un diálogo de hasta 400 dp no entra al costado de la zona). Si el usuario elige ver el bote nítido, el Designer propone la ubicación nueva para la 6b (P-19), sin tocar el contenido del diálogo ni cuándo aparece.

**Qué cambia en esta spec.**

| Dónde | Antes | Ahora |
|---|---|---|
| AC-SCN-02-07 | "el bote sigue a la vista, frenando con su inercia" | El bote frena con su inercia **sin salir de su zona**; el diálogo de la 4b va encima, opaco y centrado, y **en la 6a lo tapa** (en TV entero; en W1280 asoma la popa). La toma se lee alrededor del diálogo. Que el bote quede en su zona se mide **con la cámara real** (la pose y la popa proyectada), no a la vista. Verlo detrás del vidrio es de la 6b |
| §1, demo (c) | "con el bote frenando a la vista" | El diálogo en el centro y, alrededor, la cámara que sube: el horizonte sube, el lago se abre, el paisaje se detiene; el diálogo opaco tapa el bote hasta la 6b (en 1280 × 720 asoma la popa) |
| §7.3, CAP-22 | "el bote entero dentro de la zona; el diálogo opaco" | El diálogo opaco **tapa el bote**; el bote entero en la zona se afirma con la cámara real en el *manifest* (popa ≈ 553) |
| §7.5, VID-08 | "el bote frena a la vista" | La toma se lee en el lago alrededor del diálogo; el bote frena detrás (a la vista desde la 6b) |
| §11 | — | Fila nueva "La toma aérea detrás del diálogo" para el gate (P-19); "P-01 a P-19" |
| §13, It. 6b | — | El diálogo de completado con vidrio: el bote frenando, desenfocado, detrás durante la toma aérea |
| §14 | — | **P-19** (Usuario, en el gate con P-18): por defecto, detrás del vidrio en la 6b |

**Cómo se vuelve a verificar AC-SCN-02-07.** Con lo que ya miden CAP-22 (modo `Aerial`, cabeceo −4° ± 0,05, respiración 0, horizonte en 336,9 ± 1 dp, popa en 552,9 ± 2 dp dentro de la zona, con la cámara real) y VID-08 (`Aerial` desde los 30,3 s y quieta desde los 36,3 s, la velocidad que baja), y a la vista: el horizonte que sube alrededor del diálogo y el diálogo encima. No hace falta código nuevo para este criterio. CAP-22 con la escena en su lugar real (≈ 120 m y frenando) es QA-V6a-04, del Architect.

### 2. QA-V6a-05 (menor): DEMO-03 no muestra el muelle

A los 39,2 s el muelle ya pasó: en W1280 la orilla sale del cuadro a `relZ` 10,1 m (AC-SCN-02-03) y el muelle se ve de los ≈ 34 a los ≈ 38,2 s. "A los ≈ 39 s pasa el muelle" era el momento en que pasa **a la par** del bote, no cuando se ve.

| Dónde | Antes | Ahora |
|---|---|---|
| §1, demo (a) punto 3 | "a los ≈ 39 s pasa el muelle por la izquierda (a 175 m) y a los ≈ 79 s la cabaña" | El muelle se ve a la izquierda desde los ≈ 34 s, sale del cuadro a los ≈ 38 s y pasa a la par a los ≈ 39 s; la cabaña pasa a la par a los ≈ 79 s y se ve en los segundos de antes |
| §7.4, DEMO-03 | "la toma a los ≈ 39 s" | La toma a los **≈ 36 s** (36,2 s, con la regla de toma k + 0,2 de la 4b): el muelle a la izquierda, acercándose con sus pilotes |

**El Architect** cambia el arnés para que tome DEMO-03 a los 36,2 s (`Snapshots/SceneCatalog.cs`: el texto del estado y el `AdvanceTo` de la secuencia de la demo) y regenera la toma; la comprobación del muelle a la izquierda sigue igual. El PO no toca el arnés (no escribe código).

### 3. QA-V6a-PAR-03 (menor): los extremos del degradé y el ajuste de la hora

AC-SCN-11-02 decía que el color de la hora enriquece el degradé "sin cambiar esos dos extremos"; en la toma publicada el ajuste de color de la hora (AC-SCN-09-05), que vale para toda la imagen, corre el borde de arriba hasta 17/255 (6 h), 12 (18 h), 7 (12 h) y 0 (22 h), con el mismo tono dominante. Se mide (diferencia 0) en la pasada de contrato, sin el ajuste. Ahora AC-SCN-11-02 dice que los dos extremos valen skyTop y skyHorizon **antes del ajuste de color de la hora** (como `#FFE082` en AC-SCN-09-06 y -20-02), que ese ajuste sí los corre en la imagen final (≤ 17/255) y dónde están las sondas: las de contrato en CAP-02, arriba y a media altura (04-design-notes §10.4); CAP-01 a CAP-07 contra LEG-01 a LEG-07, a la vista con el tono dominante. **Para el REPORT**: contar que en la imagen final los extremos se corren ≤ 17/255 por AC-SCN-09-05.

### 4. QA-V6a-PAR-04 (menor): el ambiente de las 6:12

El legacy da a las 6:12 un ambiente de **0,85 exacto** (`scene_palette.json`, minuto 372: `3feb333333333333`), igual que a las 18:00; no 0,8499999999999999. La regla no cambia (las dos se ven, ≤ 0,85) y la prueba ya lo afirma (`StarFieldTests.TheExactAmbientsOfTheEdges`). AC-SCN-11-04 dice ahora "en los dos minutos el ambiente vale 0,85 exacto, oscuridad 0,23". El número de "Cambios por gate QA" §3 y la tabla del punto 1 quedan como registro de entonces; manda este.

### 5. Lo que tienen que alinear los otros documentos

- **Architect**: DEMO-03 a los 36,2 s en el arnés (punto 2) y regenerar DEMO-03.
- **Designer** (04-design-notes): §11.5, "se ve el bote frenando" → "el bote frena dentro de su zona, detrás del diálogo opaco (a la vista con el vidrio de la 6b)". Si el usuario elige el bote nítido (P-19), la ubicación del diálogo para la 6b.
- **QA**: AC-SCN-02-07 con el texto nuevo (punto 1); DEMO-03 a los 36,2 s; AC-SCN-11-02 con las sondas de CAP-02.
- **ROADMAP** (PO, hecho): la 6b suma el criterio y la demo del diálogo de completado con vidrio durante la toma aérea.

### 6. Verificación hecha

1. **Legacy, solo lectura**: `lib/features/workout/immersive_workout_screen.dart:200-216` (el diálogo de completado: `showDialog` con `barrierDismissible: false` y un `AlertDialog`). No se leyó el módulo de credenciales. `git -C ../rowmate-legacy status --porcelain` vacío al terminar.
2. **El repositorio**: del código, el PO solo leyó `Tests/EditMode/Scene/StarFieldTests.cs` (el 0,85 de las 6:12) y la secuencia de la demo en `Snapshots/SceneCatalog.cs` (DEMO-03 a los 39,2 s). Se escribieron esta spec y el ROADMAP (§2, It. 6b). Sin código, sin Unity, sin *commits*. `tools/dotnet/run-tests.ps1` en verde (artefactos en `<scratch>/it6a/fix-po-r2`).

---

## Al cerrar la iteración

Fecha: 2026-11-06. Entrada: [05-qa-verification](05-qa-verification.md) (la ronda 3 manda: APROBADO CON OBSERVACIONES, 98 PASS · 2 FAIL · 1 NO VERIFICABLE), las lentes de [paridad](05-qa-lens-paridad.md) y [rendimiento](05-qa-lens-rendimiento.md), [02-tech-design](02-tech-design.md) ("Lo que queda" de las rondas 1 y 2) y las tomas de `snapshots/`, que el PO miró. Reporte para el usuario: [REPORT](REPORT.md). **Veredicto del PO: aceptada, con dos criterios en FAIL que van como deuda al empezar la 6b** (no rompen la sesión ni el dato y no se ven en el uso normal; detalle abajo y en el REPORT §5 y §7).

### 1. Decisiones del PO

| Tema | Decisión | Por qué |
|---|---|---|
| **QA-V6a-R3-01** (mayor; AC-WKT-13-15 en FAIL en la escena demorada real) | **Deuda de la 6b, primero**, con AC-WKT-13-15 en FAIL en el REPORT. Se cierra cuando, con la precarga sin terminar (arranque nuevo, Workout y `Entrenamiento libre` en pocos segundos), ningún cuadro con movimiento pase de 16,7 ms, medido con un *driver* `[PERF]` que entre antes de que termine la precarga y su prueba PlayMode | Son dos cuadros de ≈ 20 ms (bajo 33 ms) solo en el primer arranque si se entra rápido; con la escena lista, 13,6 ms. La entrada, la sesión y la escena llegan bien (AC-SCN-01-07 pasa). Corregirlo pide partir el JIT de URP y medir un camino nuevo: es el mismo trabajo que el margen de la primera entrada (R2-04), ya acordado para la 6b |
| **QA-V6a-PAR-01** (menor; AC-SCN-13-03 en FAIL en pantalla) | **Deuda de la 6b, primero**: el tope de 10 líneas en el agua degradada, una sonda que cuente las líneas de una columna y `it6a` regenerado (dos corridas) | Una línea de 1 px con alfa 0,05 en el borde de abajo. El dato (las 10 líneas del legacy) está bit a bit contra el oráculo |
| **DT6a-06** (el fundido al rotar) | **Se registra**: AC-WKT-17-07 dice ahora "al rotar o cambiar la clase de ventana, el HUD entra con un fundido de 200 ms en la disposición nueva (sin foto de la anterior); dentro de la misma clase se reacomoda en el acto". VID-09 igual | Es lo que se construyó y verificó (VID-09, `TheHudFadesInWhenTheWindowChangesClass`); el fundido cruzado necesita una foto del HUD sin la escena que con un panel no existe. No cambia un dato |
| **P-07 / QA-V6a-REN-12** (el diálogo de completado quieto sigue a 60 fps) | **Se mantiene P-07 como permiso, no como obligación**: "puede bajar a 30" cuando todo quedó quieto. En la PC queda a 60 fps (más fluido, sin costo visible). Bajar a 30 en el diálogo quieto se mide y se decide en la It. 14 con el teléfono (batería) | No es una falla: P-07 pedía 60 mientras algo se mueve, y se cumple |
| **QA-V6a-12** (aspecto: el halo de las 18 h y las montañas que en vertical se leen lavadas y en horizontal quedan casi tapadas por la ladera cercana; CAP-03, CAP-05 contra LEG-03, LEG-05) | **Al usuario en el gate**, con P-18. **Por defecto: el Designer lo ajusta al empezar la 6b** (más contraste en la silueta lejana, la ladera cercana más baja o más lejos en el tramo que tapa el horizonte en horizontal, un poco más de halo a las 18 h), sin cambiar DV-65 ni los números del legacy | H-02 pide reconocer "mi" lago, y en el legacy la silueta de las montañas es lo dominante. Son constantes de arte |
| **QA-V6a-R2-04 y `DeviceScreenView.Render`** | Deuda de la 6b (ya acordada) | — |
| **QA-V6a-R2-05, R2-02 (b), REN-13, REN-14, `B10G11R11` en Medio** | It. 14, con el teléfono | Piden el teléfono o el Memory Profiler en el equipo |
| **QA-V6a-08 = PAR-02** (sondas de la ventana y la cabaña en el *manifest*) | Las mide QA en los PNG, como en esta iteración; se suman al *manifest* cuando la 8 proyecte las mallas de los objetos | La ventana y la cabaña pasan sus sondas de aspecto en los PNG |
| Los extremos del degradé del cielo (QA-V6a-PAR-03) | Contado en el REPORT §6.1: en la imagen final se corren ≤ 17/255 por el ajuste de color de la hora (AC-SCN-09-05); antes del ajuste, diferencia 0 | Lo pedía "Cambios por verificación (ronda 1)" §3 |

### 2. Pendientes del PO de §15: hechos

- ROADMAP: "También toca" de la 6a con F-WKT-14, F-WKT-17 y F-APP-05 (§2 y la matriz §4); D-ART-05 en §6 con la redacción de DV-52; la boya de 500 m en la It. 7 (P-06), con `Series anteriores` en vertical, la sombra del bote, el cabeceo y la lista de Mixamo; la deuda de la 6a al principio de la 6b; la fila "It. 6a (cierre, PO)".
- Inventario [immersive-scene](../../product/inventory/immersive-scene.md): la precarga de `workout_screen.dart:44` (comentario en `:41-43`) y la precarga de la escena en Workout de la app (P-14).
- STATUS: al día, con la boya en la 7 y la deuda de la 6a.

### 3. Verificación hecha

Sin código, sin Unity, sin *commits*. Se leyeron los documentos de la iteración y se miraron CAP-01 a -05, -13, -17, -22, DEMO-03 y -06 contra LEG-02 a -05. Se escribieron esta sección, el ROADMAP, el inventario `immersive-scene` y el STATUS. Rutas relativas, el legacy por su URL, `<scratch>`.
