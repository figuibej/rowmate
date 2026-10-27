# Inventario de producto: Escena inmersiva 2.5D (área `SCN`)

> Autor: `rowmate-po` · Fecha: 2026-10-27 · Fuente: `https://github.com/figuibej/rowmate-legacy` (solo lectura)
> Estado: **contrato de paridad v1**. Lo que no figure acá no se considera parte del producto a migrar.

## 0. Alcance y cómo leer este documento

**Incluye:** cámara de persecución, estado dinámico de la escena (velocidad, distancia, fase de palada, cabeceo, charcos), secuencia biomecánica de la palada, hora del día (paleta, sol/luna, noche), los 4 escenarios (lago, río, costa, regata) con su generación de orilla y su silueta lejana, el dibujo del cielo, del agua (modo avanzado y modo degradado), de la orilla y del bote con remero y remos, la miniatura del selector, la preferencia de escenario, el selector de escenario y la hora forzada de escena del simulador.

**No incluye (otros subsistemas, solo se citan como dependencias):** tarjetas de métricas, barra de etapas, paneles de series y controles de la pantalla inmersiva (`HUD`); fases del entrenamiento (`WKT`); parser FTMS y modelo de datos del remo (`BLE`); resto del panel del simulador (`SIM`); registro global de servicios e idiomas (`APP`).

**Niveles de paridad** (cada comportamiento lleva una etiqueta):

- **[EXACTO]**: regla de producto o lógica. La app Unity tiene que reproducir el número o la regla tal cual, y se puede verificar con un test.
- **[VISUAL]**: referencia de intención visual: qué elemento existe, cuándo aparece, dónde está, su proporción, su color de referencia y cómo se mueve. En Unity se puede reemplazar por una versión realista estilo EXR **siempre que el elemento, su momento y su comportamiento se conserven**. Los tamaños en píxeles del legacy sirven como referencia de proporción, no como requisito.

**Convenciones:**

- Mundo en metros: `x` lateral (0 = eje del bote, positivo a la derecha), `y` altura sobre el agua, `z` distancia hacia adelante desde la cámara. "z de mundo" es la z absoluta desde el inicio del recorrido; `relZ = zMundo − distanciaDeEscena`.
- Fase de palada `p ∈ [0, 1)`; `p = 0` es el *catch* (ataque).
- **Popa** = lado cercano a la cámara; **proa** = lado del horizonte. El remero mira hacia la popa, o sea, hacia la cámara.
- Pantallas de referencia del legacy: 390×844 (vertical, teléfono) y 844×390 (horizontal, escritorio o tablet), en píxeles lógicos.
- `smooth(t) = t²·(3 − 2t)` (suavizado *smoothstep*); `ramp(p, a, b) = smooth(clamp((p − a)/(b − a), 0, 1))`; `lerp(a, b, t) = a + (b − a)·t`.
- Rutas legacy relativas a `rowmate-legacy/`. `scene/` = `lib/features/workout/scene/`.

---

## 1. Mapa de funcionalidades

| ID | Funcionalidad | Lógica pura | Tests legacy |
|---|---|---|---|
| F-SCN-01 | Escena viva de fondo: composición y ciclo de vida | No | scene_view_test |
| F-SCN-02 | Cámara de persecución (proyección mundo → pantalla) | Sí | scene_camera_test |
| F-SCN-03 | Velocidad y distancia de la escena | Sí | scene_state_test |
| F-SCN-04 | Avance de la fase de palada | Sí | scene_state_test |
| F-SCN-05 | Secuencia biomecánica de la palada | Sí | stroke_cycle_test, boat_painter_test |
| F-SCN-06 | Cabeceo del bote | Sí | (ninguno directo) |
| F-SCN-07 | Charcos de pala | Sí | scene_state_test, boat_painter_test |
| F-SCN-08 | Bote, remero, remos, salpicadura y sombra | No | boat_painter_test |
| F-SCN-09 | Hora efectiva y paleta por hora | Sí | time_of_day_test |
| F-SCN-10 | Sol, luna y fuerza de la luz | Sí | time_of_day_test, water_painter_test |
| F-SCN-11 | Cielo: degradé, estrellas, sol/luna y nubes | No | sky_shore_painter_test |
| F-SCN-12 | Agua avanzada: ondas, reflejo, brillo, estela y niebla | No | water_painter_test, scene_state_test |
| F-SCN-13 | Agua degradada y disponibilidad del agua avanzada | No | water_painter_test, scene_view_test |
| F-SCN-14 | Orilla: segmentos deterministas, visibilidad y tope | Sí | environment_test |
| F-SCN-15 | Escenario Lago alpino | Sí | environment_test |
| F-SCN-16 | Escenario Río urbano | Sí | environment_test |
| F-SCN-17 | Escenario Costa | Sí | environment_test |
| F-SCN-18 | Escenario Canal de regata | Sí | environment_test |
| F-SCN-19 | Siluetas lejanas del horizonte por escenario | No | sky_shore_painter_test |
| F-SCN-20 | Dibujo de objetos de orilla (catálogo, luz, niebla, noche) | No | sky_shore_painter_test |
| F-SCN-21 | Preferencia de escenario (persistida) | Sí (con almacenamiento simulado) | environment_picker_test |
| F-SCN-22 | Selector de escenario con miniaturas | No | environment_picker_test |
| F-SCN-23 | Hora de escena forzada (solo simulador) | No | simulator_overlay_test |

---

## 2. Fichas de funcionalidad

### F-SCN-01: Escena viva de fondo (composición y ciclo de vida)

**Valor para el usuario:** entrenar en el remo indoor se siente como remar al aire libre: detrás de las métricas hay un paisaje animado que avanza a la velocidad real del remero.

**Comportamientos observables**

- [EXACTO] La escena es la capa de fondo, a pantalla completa, de la pantalla de entrenamiento inmersivo (la que se abre al iniciar "Entrenamiento libre" o una rutina). Encima van, en este orden, la barra de etapas o la barra superior del libre, el HUD de métricas y los controles (`immersive_workout_screen.dart:74-117`).
- [EXACTO] Orden de capas de atrás hacia adelante: **cielo** (degradé, estrellas, sol o luna, nubes, silueta lejana) → **agua** → **objetos de orilla** → **bote** (charcos, sombra, casco, remero, remos) (`scene_view.dart:97-121`).
- [EXACTO] La escena se actualiza una vez por cuadro. Objetivo de rendimiento: 60 fps en un teléfono de gama media (spec §Rendimiento).
- [EXACTO] El paso de tiempo por cuadro se acota a [0; 0,1] s. Si un cuadro tarda más (por ejemplo, la app pasó a segundo plano o hubo un tirón), la escena avanza como máximo 0,1 s y no "salta". Consecuencia: por debajo de 10 fps la escena corre más lenta que el reloj real (`scene_view.dart:63`, `scene_state.dart:52`).
- [EXACTO] Entradas de cada cuadro: split /500 m del monitor (segundos enteros, `RowingData.pace500mSeconds`), paladas por minuto (`RowingData.strokeRate`, con decimales de 0,5), "activo" (verdadero solo con el entrenamiento en marcha; falso en pausa y en terminado), hora efectiva (F-SCN-09) y escenario elegido (F-SCN-21) (`scene_view.dart:65-70`, `immersive_workout_screen.dart:81-86`).
- [EXACTO] La escena se adapta al tamaño disponible en cada cuadro (vertical, horizontal o cambio de tamaño de ventana en escritorio) sin reiniciarse ni perder estado (`scene_view.dart:90-92`).
- [EXACTO] La escena arranca de cero cada vez que se abre la pantalla inmersiva: velocidad 0, distancia de escena 0, fase 0 (catch), tiempo 0 y sin charcos. Pausar y reanudar **no** la reinicia.
- [EXACTO] En pausa y en terminado el tiempo de escena sigue corriendo: el agua sigue ondulando y las nubes, gaviotas, banderas y estrellas siguen animadas. El bote desacelera hasta detenerse (F-SCN-03) y la palada se congela en la fase en la que estaba (F-SCN-04).
- [EXACTO] La **distancia de escena es propia**: se integra a partir de la velocidad de escena y **no** se toma de la distancia del monitor. Puede diferir de los metros que muestra el HUD por la inercia de 1,5 s, el redondeo del split a segundos y el tope de 0,1 s por cuadro.
- [EXACTO] La animación de la escena no debe degradar la fluidez del HUD: ambas se redibujan de forma independiente (`immersive_workout_screen.dart:78-80, 99-100`).
- [EXACTO] Durante el entrenamiento no se puede cambiar de escenario (no hay control para hacerlo; spec §Fuera de alcance). La hora sí puede cambiar: la real avanza minuto a minuto y, en modo simulador, se puede forzar (F-SCN-23).
- [EXACTO] Al cerrar la pantalla, la escena se libera sin errores, aunque la carga del agua avanzada siga en curso (`scene_view.dart:54-56, 75-79`).
- [VISUAL] Fondo negro detrás de la escena (`immersive_workout_screen.dart:73`).

**Casos borde y errores**

- La escena funciona en los 4 escenarios, en vertical y horizontal, de día y de noche, sin excepciones (test).
- Si el agua avanzada no está disponible, la escena usa el agua degradada y sigue siendo usable (F-SCN-13).
- Si el monitor deja de mandar paladas, el bote se detiene solo (F-SCN-03).

**UI:** no hay estados de "cargando" ni de error visibles; la escena siempre muestra algo.

**Datos persistidos:** ninguno.

**Archivos legacy:** `scene/scene_view.dart:16-125`; `lib/features/workout/immersive_workout_screen.dart:47, 77-87`; spec `docs/superpowers/specs/2026-10-02-immersive-scene-2-5d-design.md` §SceneView y §Rendimiento.

**Tests legacy:**
- `test/scene/scene_view_test.dart:11` "`$id WxH ${hour}h anima sin excepciones`", 16 casos (4 escenarios × {390×844, 844×390} × {12 h, 23 h}): con datos spm 24, split 110 s, distancia 300 m, activo y hora forzada, anima 6 cuadros de 100 ms sin excepciones; al desmontar no hay excepciones.

**Dependencias:** `WKT` (fase activa/pausa/terminado), `BLE` (split y spm), `HUD` (capas superiores; la banda de tiempo y distancia del HUD va en la esquina inferior izquierda para no tapar el bote, spec línea 7), F-SCN-02 a F-SCN-21.

**Mejora EXR:** al entrar a la pantalla, una transición cinematográfica corta (fundido o *fly-in* de la cámara hasta su posición) en lugar de aparecer de golpe. Fluidez constante sin tirones; si el dispositivo es lento, la escena baja detalle antes que fluidez. El paisaje transmite profundidad real y escala humana: el usuario siente que está *en* el agua.

---

### F-SCN-02: Cámara de persecución

**Valor para el usuario:** una vista "desde atrás y arriba" del propio bote, como en EXR, que hace que el paisaje venga hacia el remero.

**Comportamientos observables**

- [EXACTO] Una sola cámara, fija, detrás del bote, a **2,2 m** de altura sobre el agua, mirando hacia el punto de fuga. No hay otros modos de cámara (`scene_camera.dart:12`; spec §Decisiones).
- [EXACTO] El remero (centro del bote) está a **z = 9,5 m** de la cámara (`scene_camera.dart:16`).
- [EXACTO] Línea de horizonte: **40 %** de la altura de pantalla en vertical (ancho ≤ alto) y **42 %** en horizontal (ancho > alto) (`scene_camera.dart:8`).
- [EXACTO] Focal = lado más corto de la pantalla (390 en 390×844 y en 844×390) (`scene_camera.dart:9`).
- [EXACTO] Proyección de un punto (x, y, z) con z > 0,5 m:
  - `pantallaX = ancho/2 + focal · x / z`
  - `pantallaY = horizonte + focal · (2,2 − y) / z`
  - escala (píxeles por metro a esa distancia) = `focal / z` (`scene_camera.dart:28-37`).
- [EXACTO] Nada con **z ≤ 0,5 m** se dibuja: queda "detrás de la cámara" (`scene_camera.dart:19, 29`).
- [EXACTO] Inversa sobre el agua (y = 0), para una fila de pantalla por debajo del horizonte: `z = focal · 2,2 / (pantallaY − horizonte)`, `x = (pantallaX − ancho/2) · z / focal` (`scene_camera.dart:41-45`).
- [EXACTO] Valores derivados de referencia: en 390×844 el horizonte queda en y = 337,6 px y el borde inferior de la pantalla corresponde a z ≈ 1,69 m; en 844×390 el horizonte queda en y = 163,8 px y el borde inferior en z ≈ 3,79 m. Con el remero en z = 9,5 m, la popa (z = 5,3 m) queda visible en horizontal (`scene_camera.dart:14-16`).

**Casos borde:** un punto a distancia "infinita" converge al punto de fuga (centro horizontal, línea de horizonte). Un objeto muy cercano (z apenas mayor a 0,5 m) se ve gigante y puede cubrir la pantalla (caso del puente, F-SCN-20).

**UI:** no tiene controles.

**Datos persistidos:** ninguno.

**Archivos legacy:** `scene/scene_camera.dart:1-46`; spec §Cámara.

**Tests legacy** (`test/scene/scene_camera_test.dart`):
- `:9` "horizonte y focal según orientación": horizonte = 844 × 0,40 en vertical y 390 × 0,42 en horizontal; focal 390 en ambos.
- `:16` "lo lejano queda más cerca del horizonte, centrado y más chico": (3, 0, 200) queda más arriba que (3, 0, 10) pero debajo del horizonte, más cerca del centro horizontal y con menor escala; (3, 0, 1e9) converge a (centro, horizonte) ±0,01 px.
- `:30` "detrás de la cámara no se proyecta": z = 0,2 no devuelve posición.
- `:34` "unprojectWater invierte project para y = 0": en horizontal, (−4; 6), (0; 30) y (7,5; 120) ida y vuelta con error < 1e-6.

**Dependencias:** base de F-SCN-07, 08, 10, 11, 12, 13, 19 y 20.

**Mejora EXR:** cámara con leve inercia y vida (micro-balanceo coherente con el oleaje y con el empuje de cada palada, sin marear), sensación de altura y escala reales. Como mejora futura, y fuera de paridad, vistas adicionales como las de EXR (primera persona, lateral, cinemática); ver Preguntas abiertas.

---

### F-SCN-03: Velocidad y distancia de la escena

**Valor para el usuario:** el paisaje pasa exactamente a la velocidad que indica el monitor, así que un split más rápido "se ve" más rápido. Al dejar de remar, el bote se va frenando como uno real.

**Comportamientos observables**

- [EXACTO] Velocidad objetivo (m/s) = **500 / split** (split en segundos /500 m) **solo si** el entrenamiento está activo **y** spm > 0 **y** split > 0; en cualquier otro caso es 0 (`scene_state.dart:38-43`).
  - Ejemplos: 1:47 (107 s) → 4,67 m/s; 2:00 → 4,17 m/s; 2:05 → 4,00 m/s.
  - Motivo de producto: el parser del monitor reemplaza el split instantáneo por el split medio cuando el instantáneo es 0 (`lib/core/bluetooth/ftms_parser.dart:68-80`), así que el split nunca vuelve a 0 una vez que se remó. La señal de "está remando" es spm > 0 (`scene_state.dart:36-37`).
- [EXACTO] La velocidad de escena sigue a la objetivo con inercia de constante de tiempo **1,5 s**: en cada cuadro, `velocidad += (objetivo − velocidad) · min(1; dt / 1,5)` (`scene_state.dart:16, 55`).
- [EXACTO] Si el objetivo es 0 y la velocidad baja de **0,01 m/s**, la velocidad pasa a ser exactamente 0: el bote se detiene del todo (`scene_state.dart:56`).
- [EXACTO] `distancia de escena += velocidad · dt` en cada cuadro (`scene_state.dart:57`). Esta distancia mueve la orilla, el agua, los charcos, las nubes y las siluetas lejanas.
- [EXACTO] En pausa o terminado el objetivo es 0: el bote desacelera con la misma inercia hasta detenerse.

**Casos borde**

- Split 0 (todavía no hay dato) → no hay avance.
- spm 0 con split > 0 (el remero paró y el monitor sigue mandando el split medio) → no hay avance y el bote frena.
- Cuadros lentos: tope de 0,1 s por cuadro (F-SCN-01).
- La distancia de escena no se sincroniza con la del monitor (ver F-SCN-01 y Preguntas abiertas).

**UI:** no tiene controles; se percibe por el paso del paisaje y por la estela (F-SCN-12).

**Datos persistidos:** ninguno.

**Archivos legacy:** `scene/scene_state.dart:14-57`; spec §Estado de la escena.

**Tests legacy** (`test/scene/scene_state_test.dart`, pasos de 0,05 s):
- `:14` "la velocidad sale del split y se alcanza en unos segundos": objetivo(107 s, 24 spm, activo) = 4,67 ±0,01; inactivo → 0; split 0 → 0; spm 0 → 0. Tras 8 s la velocidad es 4,67 ±0,05 y la distancia supera 25 m.
- `:26` "al dejar de remar desacelera hasta detenerse": tras 8 s remando, 6 s con split 0 dejan la velocidad < 0,1 m/s y la distancia siguió creciendo; 10 s más la dejan exactamente en 0.
- `:60` "en pausa no avanza la palada ni la velocidad": tras 0,5 s activo, 2 s inactivo dejan la velocidad < 0,5 m/s (y la fase sin cambios).

**Dependencias:** `BLE` (split, spm), `WKT` (activo), F-SCN-01.

**Mejora EXR:** el bote "respira" con la palada: acelera en el drive y se desliza perdiendo algo de velocidad en la recuperación, con una velocidad media que sigue coincidiendo con 500/split. La parada, al dejar de remar, se siente como un deslizamiento real sobre el agua.

---

### F-SCN-04: Avance de la fase de palada

**Valor para el usuario:** el remero virtual rema al mismo ritmo que el usuario.

**Comportamientos observables**

- [EXACTO] Activo con spm > 0: la fase avanza a **spm / 60 ciclos por segundo** y vuelve a 0 al completar el ciclo (`scene_state.dart:59-60`). Ejemplo: 24 spm = 0,4 ciclos/s, es decir, una palada cada 2,5 s.
- [EXACTO] Activo con spm = 0 y una palada a medio camino: la palada en curso **termina su ciclo despacio**, a un ritmo fijo de **12 spm** (0,2 ciclos/s), y queda detenida en el catch (fase 0). No se congela a mitad de camino (`scene_state.dart:21-23, 61-65`).
- [EXACTO] Activo con spm = 0 y fase ya en 0: queda quieta en el catch.
- [EXACTO] No activo (pausa o terminado): la fase **se congela** donde estaba (`scene_state.dart:59-65`).

**Casos borde**

- Al completar exactamente un ciclo, la fase puede quedar en 0,0 o en 0,999… por redondeo; ambas son válidas (test).
- Desde cualquier fase, la vuelta al catch en reposo tarda como máximo 5 s.

**UI:** se ve en la animación del remero (F-SCN-05 y F-SCN-08).

**Datos persistidos:** ninguno.

**Archivos legacy:** `scene/scene_state.dart:21-23, 59-65`; spec §Estado de la escena.

**Tests legacy** (`test/scene/scene_state_test.dart`):
- `:37` "la fase de palada avanza a spm/60 ciclos por segundo": 1 s a 24 spm → 0,4 ±1e-6; 1,5 s más → 0,0 o 1,0 ±1e-6.
- `:46` "sin paladas la fase termina el ciclo y queda en el catch": 0,5 s a 24 spm → 0,2; luego 6 s con spm 0 (split 107) → fase exactamente 0,0 y velocidad < 0,1.
- `:60` "en pausa no avanza la palada ni la velocidad": la fase no cambia en 2 s inactivo.

**Dependencias:** `BLE` (spm), `WKT` (activo), F-SCN-05, F-SCN-06, F-SCN-07.

**Mejora EXR:** sincronía perfecta con la palada real: cuando el monitor informa el momento de la palada, el catch del remero virtual coincide con el del usuario y no solo con su ritmo promedio. Las transiciones de ritmo son suaves, sin aceleraciones bruscas de la animación.

---

### F-SCN-05: Secuencia biomecánica de la palada

**Valor para el usuario:** el remero hace la técnica real (piernas → espalda → brazos en el drive y al revés en la recuperación), lo que refuerza la técnica del usuario.

**Comportamientos observables**

- [EXACTO] Drive en `p` 0,00 a 0,40; recuperación en 0,40 a 1,00 (`stroke_cycle.dart:3-4`). La relación real es **1 : 1,5** (ver Diferencias).
- [EXACTO] Piernas (0 = plegadas en el catch, 1 = extendidas): `ramp(p; 0,00; 0,18)` si p < 0,60; si no, `1 − ramp(p; 0,60; 1,00)` (`stroke_cycle.dart:15-18`).
- [EXACTO] Espalda (0 = tronco adelante, 1 = tronco atrás): `ramp(p; 0,10; 0,30)` si p < 0,50; si no, `1 − ramp(p; 0,50; 0,75)` (`stroke_cycle.dart:21-24`).
- [EXACTO] Brazos (0 = estirados, 1 = flexionados al pecho): `ramp(p; 0,22; 0,36)` si p < 0,40; si no, `1 − ramp(p; 0,40; 0,60)` (`stroke_cycle.dart:27-30`).
- [EXACTO] Inclinación del tronco = `lerp(+25°; −20°; espalda)`. El signo positivo es hacia la popa (hacia la cámara): +25° en el catch y −20° en el finish (`stroke_cycle.dart:33-34`).
- [EXACTO] Ángulo del remo respecto de la perpendicular al bote (negativo = pala hacia la proa) (`stroke_cycle.dart:9-12, 41-45`):
  - p < 0,36: `lerp(−55°; +35°; smooth(p / 0,36))`.
  - 0,36 ≤ p < 0,40: +35° fijo (*finish*).
  - p ≥ 0,40: `lerp(+35°; −55°; smooth((p − 0,40) / 0,60))`.
- [EXACTO] Pala en el agua mientras p < **0,38**; fuera del agua (pala "plumeada", plana) desde 0,38 hasta el fin del ciclo (`stroke_cycle.dart:36-37`).
- [EXACTO] Resumen de tramos: catch en 0,00; piernas 0,00–0,18; espalda 0,10–0,30; brazos 0,22–0,36; finish 0,36–0,40, donde la pala sale en 0,38; recuperación de brazos 0,40–0,60, de tronco 0,50–0,75 y de piernas 0,60–1,00.
- [EXACTO] Geometría derivada del remo y del carro (`painters/boat_painter.dart:35-56`):
  - Carro: `z = 9,5 − 0,6 · (1 − piernas)`, o sea, z = 8,9 en el catch (hacia la popa, cerca de los pies) y z = 9,5 en el finish.
  - Mango (manos), por lado `s = ±1`: `(s · (0,85 − cos(ángulo) · 0,9); 0,45; 9,5 + sin(ángulo) · 0,9)`. En el catch queda hacia la cámara y en el finish hacia la proa.
  - Punta de la pala: `(s · (0,85 + cos(ángulo) · 2,9); y; 9,5 − sin(ángulo) · 2,9)`, con y = −0,05 dentro del agua y y = +0,35 fuera. En el catch queda hacia la proa (z ≈ 11,9) y en el finish hacia la popa (z ≈ 7,8).
  - Remo de 2,9 m desde el pivote, 0,9 m de mango interior y pivote en x = ±0,85 m.
  - Ambos lados son exactamente simétricos.

**Casos borde:** en la transición de 0,999 a 0,0 todo vuelve al catch sin salto visible (piernas ≈ 0, brazos y espalda 0 y remo ≈ −55°). El cambio pala dentro/fuera del agua es instantáneo en 0,38 y en 0,0.

**UI:** se ve en el remero (F-SCN-08).

**Datos persistidos:** ninguno.

**Archivos legacy:** `scene/stroke_cycle.dart:1-51`; `scene/painters/boat_painter.dart:35-56`; spec §Palada.

**Tests legacy:**
- `test/scene/stroke_cycle_test.dart:5` "catch: piernas plegadas, brazos estirados, pala en el agua": piernas, brazos y espalda en 0 con p = 0; pala en el agua; ángulo < 0; inclinación > 0.
- `:14` "finish: piernas extendidas, brazos flexionados, pala sale": piernas, brazos y espalda en 1 con p = 0,36; ángulo > 0; pala fuera en 0,39; inclinación < 0.
- `:23` "en el drive: piernas antes que espalda, espalda antes que brazos": en 0,12, piernas > espalda > brazos y brazos = 0.
- `:29` "la recuperación vuelve todo al catch": en 0,999 piernas ≈ 0 (±1e-3), brazos 0, espalda 0 y ángulo ≈ −55° (±1e-3); en 0,7 la pala está plumeada.
- `test/scene/boat_painter_test.dart:10` "geometría de la palada: catch hacia la cámara, pala hacia la proa": carro(0) < carro(0,36); mango(0).z < 9,5 < mango(0,36).z; pala(0).z > 11,5; pala(0,36).z < 9,5; los lados son espejados en x.

**Dependencias:** F-SCN-04 (fase), F-SCN-07 (charcos en el punto de salida de la pala), F-SCN-08 (dibujo).

**Mejora EXR:** animación de atleta realista y fluida, con continuidad entre poses: cabeza estable, hombros relajados, muñecas planas en la recuperación, la pala que gira visiblemente (*feather*) al salir y se cuadra antes de entrar. Entrada y salida de la pala progresivas, no instantáneas. El cuerpo transmite esfuerzo en el drive y relajación en la recuperación.

---

### F-SCN-06: Cabeceo del bote

**Valor para el usuario:** el bote se siente vivo sobre el agua: la proa sube y baja con cada palada y con un oleaje lento.

**Comportamientos observables**

- [EXACTO] Cabeceo (radianes, positivo = proa arriba) = `0,02 · sin(2π · fase) + 0,006 · sin(0,8 · tiempo de escena)` (`scene_state.dart:85`). Son unos 1,15° por palada (máximo de proa arriba en p = 0,25) más un balanceo de unos 0,34° con un período aproximado de 7,9 s.
- [EXACTO] El cabeceo inclina solo el casco: la altura del casco en cada punto es `0,08 + (z − 9,5) · sin(cabeceo)`. El remero y los remos no se inclinan con él (`boat_painter.dart:66`).
- [EXACTO] En pausa la componente de palada queda fija (fase congelada) y el balanceo lento sigue.

**Casos borde:** ninguno.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/scene_state.dart:28, 85`; `scene/painters/boat_painter.dart:66, 139, 144`; spec §Estado de la escena.

**Tests legacy:** no hay test directo. Se ejercita en los tests de pintado del bote y de la escena.

**Dependencias:** F-SCN-04, F-SCN-08.

**Mejora EXR:** cabeceo y balanceo que responden al escenario (más movimiento en la costa con mar movido, casi nada en el lago calmo) y a la potencia de la palada. Remero, remos y bote se mueven como un solo cuerpo físico.

---

### F-SCN-07: Charcos de pala

**Valor para el usuario:** la huella clásica que deja cada palada en el agua (los "charcos" o remolinos), que queda atrás a medida que el bote avanza y da referencia de velocidad.

**Comportamientos observables**

- [EXACTO] Cuando la pala pasa de "en el agua" a "fuera del agua" (fase que cruza 0,38), se crean **dos charcos**, primero el izquierdo y después el derecho, en la posición de la punta de cada pala: `x = ±(0,85 + cos(ángulo) · 2,9)` ≈ ±3,23 m y z de mundo = distancia de escena + (9,5 − sin(ángulo) · 2,9) ≈ distancia + 7,84 m (`scene_state.dart:67-79`).
- [EXACTO] Cada charco queda fijo en el mundo: como el bote avanza, se acerca a la cámara y la pasa (`boat_painter.dart:102`).
- [EXACTO] Vida: **4 s**. La edad avanza con el tiempo de escena, también en pausa, y a los 4 s el charco desaparece (`scene_state.dart:17, 80-83`).
- [EXACTO] Se crean charcos en cualquier cruce de 0,38, incluida la palada que se termina sola a 12 spm (F-SCN-04). En pausa no se crean (la fase está congelada).
- [EXACTO] Al empezar la escena (fase 0), la pala se considera dentro del agua.
- [VISUAL] Se dibujan como un anillo ovalado blanco (alto = 0,35 × ancho) cuyo radio crece de 0,3 m a 0,9 m a lo largo de la vida, con opacidad `(1 − edad/4) · 0,3` y trazo de 0,06 m (mínimo 1 px) (`boat_painter.dart:100-116`).
- [EXACTO] Un charco que ya quedó detrás de la cámara (relZ ≤ 0,5 m) no se dibuja.

**Casos borde:** a 4,67 m/s un charco pasa detrás de la cámara unos 1,6 s después de creado, antes de agotar su vida. A baja velocidad se desvanece antes de llegar.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/scene_state.dart:5-11, 17-19, 31, 67-83`; `scene/painters/boat_painter.dart:100-116`; spec §Estado y §Bote.

**Tests legacy:**
- `test/scene/scene_state_test.dart:69` "al soltar la pala aparecen dos charcos que envejecen y desaparecen": tras 0,8 s a 24 spm (fase ≈ 0,32) no hay charcos; tras 0,3 s más (cruza 0,38) hay 2, el primero con x < 0 y el último con x > 0; tras 5 s con spm 0 no queda ninguno.
- `test/scene/boat_painter_test.dart:25` pinta sin errores con charcos a ambos lados y con uno ya detrás de la cámara (z de mundo 0,2).

**Dependencias:** F-SCN-03, F-SCN-04, F-SCN-05, F-SCN-08.

**Mejora EXR:** remolinos realistas en el agua (dos vórtices que giran y se disipan, con reflejos y espuma tenue), proporcionales a la potencia de la palada, más un hilo de gotas que cae de la pala durante la recuperación.

---

### F-SCN-08: Bote, remero, remos, salpicadura y sombra

**Valor para el usuario:** ver "su" bote, un single (*skiff*) con un atleta de frente a la cámara que rema con la técnica real.

**Comportamientos observables**

- [EXACTO] Orden de dibujo: charcos → sombra → casco → remero → remos (`boat_painter.dart:71-78`).
- [EXACTO] Tinte de luz ambiente sobre todo el bote y el remero: rojo y verde × ambiente; azul × min(1; ambiente + 0,12). De noche se tiñe más oscuro y azulado (`boat_painter.dart:58-61`).
- [VISUAL] **Casco**: centrado en x = 0, de la popa en z = 5,3 (9,5 − 4,2) a la proa en z = 13,5 (9,5 + 4,0), a 0,08 m sobre el agua, inclinado según el cabeceo. Contorno por lado (x; z): (0,14; 5,3), (0,28; 8,0), (0,26; 10,0), (0,12; 12,0), (0; 13,5), simétrico. Degradé de arriba a abajo de #F5F5F5 a #B0BEC5, borde #00B4D8 de 0,03 m y hueco del remero #263238 (x ±0,17, z 8,1 a 9,8) (`boat_painter.dart:132-170`).
- [VISUAL] **Riggers**: barra #90A4AE de 0,05 m desde x = ±0,3 hasta el pivote en x = ±0,85, a la altura del remero (z = 9,5) (`boat_painter.dart:227-230`).
- [VISUAL] **Remero** de frente a la cámara (los remeros miran a la popa), con proporciones algo exageradas para que se lea de lejos (`boat_painter.dart:172-218`):
  - Pies en z = 8,35 (9,5 − 1,15), a 0,15 m de altura: círculos #0D3B6E de 0,07 m de radio.
  - Carro #0D3B6E de 0,4 × 0,4 m bajo la cadera (cadera a 0,25 m de altura); se mueve según F-SCN-05.
  - Rodilla en z = (carro + pies)/2 − 0,1 · (1 − piernas) y altura 0,25 + 0,4 · (1 − piernas): 0,65 m en el catch y 0,25 m en el finish.
  - Muslos del color del traje (0,16 m de ancho) y pantorrillas del color de la piel (0,12 m).
  - Tronco: trapecio del color del traje, de cadera (±0,19 m) a hombros (±0,27 m), con largo 0,62 m e inclinación según F-SCN-05.
  - Cabeza: círculo de piel de 0,14 m de radio, 0,27 m arriba de los hombros, con pelo #3E2723 en la mitad superior.
  - Brazos de hombro (±0,22 m) al mango, de 0,09 m de ancho y color del traje; manos de 0,05 m de radio, color piel.
  - Colores: traje #1565C0, piel #FFCC80.
- [VISUAL] **Remos** (por lado): caña #BCAAA4 de 0,06 m de ancho, del mango al pivote y del pivote a la punta (`boat_painter.dart:220-249`).
  - Pala en el agua: rectángulo vertical #00B4D8 de 0,25 × 0,5 m con esquinas redondeadas.
  - Pala fuera del agua: óvalo plano #00B4D8 de 0,5 × 0,12 m (plumeada).
- [EXACTO] **Salpicadura en el catch**: mientras p < 0,08 (0,2 s a 24 spm; depende del ritmo) aparecen **6 gotas** blancas por pala que suben y caen en abanico. Opacidad `(1 − t) · 0,8` con t = p/0,08, radio de 0,05 m a 0,03 m, apertura de 0,15 m a 0,5 m y elevación máxima de 0,25 m a mitad de la animación (`boat_painter.dart:244, 252-264`).
- [EXACTO] **Sombra**: óvalo negro con opacidad 0,08 × ambiente, pegado al casco (de z 5,8 a z 12,5, 0,5 m de ancho) y desplazado **0,35 m hacia el lado opuesto al sol o la luna**. Si el astro está en la mitad izquierda de la pantalla, la sombra va a la derecha; si no (incluido el astro exactamente centrado), a la izquierda (`boat_painter.dart:118-130`).
- [EXACTO] Grosor mínimo de cualquier línea: 1 px.

**Casos borde:** se pinta sin errores en todas las fases de la palada (0; 0,05; 0,2; 0,36; 0,39; 0,5; 0,8; 0,99) en vertical y en horizontal (test). Si alguna parte queda detrás de la cámara, esa parte se omite.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/painters/boat_painter.dart:1-268`; `scene/scene_state.dart:18-19`; spec §Bote, remero y remos.

**Tests legacy:**
- `test/scene/boat_painter_test.dart:10`: geometría de la palada (ver F-SCN-05).
- `test/scene/boat_painter_test.dart:25` "`390x844` / `844x390` pinta todas las fases sin excepciones": 8 fases × 2 tamaños, con 3 charcos (dos delante y uno detrás de la cámara) y paleta de las 12 h.

**Dependencias:** F-SCN-02, F-SCN-04 a F-SCN-07, F-SCN-09 (ambiente), F-SCN-10 (posición del astro para la sombra).

**Mejora EXR:** skiff de competición realista (casco de fibra con brillo especular, riggers metálicos, número de proa, remos con caña de carbono y pala de color). Atleta con proporciones reales, ropa de remo, piel y pelo creíbles y animación fluida. Salpicaduras con partículas y espuma, sombra suave y coherente con la hora, reflejo del bote y del remero en el agua. Opción futura de personalizar colores del bote y del uniforme.

---

### F-SCN-09: Hora efectiva y paleta por hora del día

**Valor para el usuario:** la luz de la escena acompaña la hora real: remar a la mañana, al mediodía, al atardecer o de noche se ve distinto.

**Comportamientos observables**

- [EXACTO] Hora efectiva (decimal 0–24) = hora forzada del simulador si existe (F-SCN-23); si no, la hora local del dispositivo: `hora + minuto/60`, con resolución de minuto (se ignoran los segundos). Se reevalúa en cada cuadro, así que la escena cambia de luz durante un entrenamiento largo (`scene_view.dart:81-86`).
- [EXACTO] Paleta por keyframes, con interpolación lineal por canal de color entre horas consecutivas (`time_of_day.dart:69-116`):

| Hora | Momento | skyTop | skyHorizon | waterNear | waterFar | sunColor | sunGlow | fog | ambient |
|---|---|---|---|---|---|---|---|---|---|
| 5 | Amanecer | #2A1B5E | #FF9A5C | #1A2B4A | #3A2F55 | #FFB070 | #FF7A3D | #C98A7A | 0,70 |
| 7 | Mañana | #3A7BD5 | #FFD9A0 | #1E5A9E | #5C8FC9 | #FFE4A0 | #FFC46B | #DCE8F5 | 0,95 |
| 12 | Mediodía | #0B4FA8 | #8FD3FF | #0E4F8F | #2F8FD6 | #FFF6D5 | #FFE9A6 | #CFE9FF | 1,00 |
| 18 | Atardecer | #2E3C8C | #FF8C5A | #163A66 | #7C5C7A | #FFB36B | #FF6B3D | #E0A28A | 0,85 |
| 20 | Anochecer | #0D1A4A | #4A3A7A | #0B1E3A | #2A2A55 | #DDE6FF | #8FA3FF | #30335A | 0,50 |
| 22 a 4 | Noche | #050A1E | #121C3A | #06122A | #0E1E3C | #E6EEFF | #A6B8FF | #0F1730 | 0,35 |

- [EXACTO] La noche se mantiene **cerrada de 22 h a 4 h**; de 4 h a 5 h interpola de noche a amanecer. La medianoche no es un falso amanecer (`time_of_day.dart:100-105`).
- [EXACTO] Algoritmo: `h = hora mod 24`; si h < 5, h += 24. Keyframes en 5, 7, 12, 18, 20, 22, 28 (= 4 h, noche) y 29 (= 5 h, amanecer) (`time_of_day.dart:107-116`).
- [EXACTO] Uso de cada campo:
  - skyTop y skyHorizon: degradé del cielo.
  - waterNear y waterFar: color del agua cercana y lejana.
  - sunColor: disco del sol o la luna y brillo sobre el agua.
  - sunGlow: halo del astro.
  - fog: niebla de distancia (agua, orilla, siluetas).
  - ambient: multiplicador de luz para la orilla y el bote (1 = día, 0,35 = noche).
- [EXACTO] Valores de ambiente derivados de referencia: 0–4 h: 0,35 · 4:30: 0,525 · 6 h: 0,825 · 9 h: 0,97 · 15 h: 0,925 · 18:30: 0,7625 · 19 h: 0,675 · 21 h: 0,425.
- [EXACTO] Umbral de "luces nocturnas" (ventanas encendidas, faroles, faro, cabaña), ambiente < 0,6: aproximadamente de **19:26 a 4:43** (ver F-SCN-19 y F-SCN-20).

**Casos borde:** la paleta es continua a medianoche (23:59 ≈ 0:00) y en la transición de las 5 h (4:59 ≈ 5:00). Las horas fuera de 0–24 se normalizan con módulo 24.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/time_of_day.dart:5-39, 69-116`; `scene/scene_view.dart:81-95`; spec §Hora del día.

**Tests legacy** (`test/scene/time_of_day_test.dart`):
- `:17` "23:00 es noche con luna": ambiente(23) < 0,5 (y luna, ver F-SCN-10).
- `:25` "18:30 interpola entre atardecer y anochecer": ambiente = 0,85 + (0,5 − 0,85) · 0,25 (±1e-9); skyHorizon distinto del de las 18 h y del de las 20 h.
- `:32` "de 22 h a 4 h la paleta es noche cerrada, sin falso amanecer": ambiente(0) = 0,35 y ambiente(3) = 0,35.
- `:44` "la paleta es continua a medianoche y al amanecer": ambiente(23,99) ≈ ambiente(0) ±0,01; ambiente(4,99) ≈ ambiente(5) ±0,01.

**Dependencias:** F-SCN-23 (hora forzada); la consumen F-SCN-08, 10, 11, 12, 13, 19 y 20.

**Mejora EXR:** iluminación global coherente (dorada a la mañana, dura al mediodía, cálida y con sombras largas al atardecer, azul profunda de noche), cielo con dispersión atmosférica y transiciones continuas sin saltos. La escena debe sentirse fotográfica a cualquier hora.

---

### F-SCN-10: Sol, luna y fuerza de la luz

**Valor para el usuario:** el sol recorre el cielo según la hora; de noche aparece la luna y su reflejo plateado en el agua.

**Comportamientos observables**

- [EXACTO] Elevación del sol = `sin((hora − 6) / 12 · π)`: positiva entre 6 h y 18 h (`time_of_day.dart:52`).
- [EXACTO] **Noche** (se muestra la luna en lugar del sol) cuando elevación ≤ **−0,4**, es decir, aproximadamente de **19:34 a 4:26**. A las 19 h todavía es atardecer (sol) y a las 20 h ya es noche (luna) (`time_of_day.dart:53-55`).
- [EXACTO] Posición del astro (`time_of_day.dart:57-67`):
  - De noche se usa la hora espejada `h = (hora + 12) mod 24` (luna simétrica al sol); de día `h = hora`.
  - Elevación usada = `sin((h − 6)/12 · π)` acotada a [0; 1]: el astro nunca baja del horizonte.
  - x = ancho · `clamp(0,25 + 0,5 · (h − 6)/12; 0,15; 0,85)`.
  - y = horizonte − elevación · horizonte · 0,9.
- [EXACTO] Fuerza del brillo sobre el agua: de noche **0,45** fija; de día `clamp(elevación · 1,2; 0,2; 1,0)`, que llega a 1,0 entre ≈ 9:46 y ≈ 14:14 y nunca baja de 0,2.
- [EXACTO] Valores de referencia:

| Hora | Astro | x (fracción del ancho) | Elevación | Fuerza |
|---|---|---|---|---|
| 5:00 | sol | 0,208 | 0 (en el horizonte) | 0,2 |
| 9:00 | sol | 0,375 | 0,707 | 0,85 |
| 12:00 | sol | 0,500 | 1,0 (y = 0,1 · horizonte) | 1,0 |
| 18:00 | sol | 0,750 | 0 | 0,2 |
| 19:00 | sol | 0,792 | 0 | 0,2 |
| 20:00 | luna | 0,333 | 0,5 | 0,45 |
| 0:00 | luna | 0,500 | 1,0 | 0,45 |
| 4:00 | luna | 0,667 | 0,5 | 0,45 |

**Casos borde**

- A las 19:34 el astro "salta" del sol en el horizonte a la derecha (x ≈ 0,82) a la luna al 40 % de altura a la izquierda (x ≈ 0,32). A las 4:26 pasa lo inverso. Es comportamiento legacy (ver Preguntas abiertas).
- Entre 18 h y 19:34, y entre 4:26 y 6 h, el sol queda apoyado en el horizonte con brillo mínimo 0,2.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/time_of_day.dart:41-67`; spec §Hora del día.

**Tests legacy** (`test/scene/time_of_day_test.dart`):
- `:9` "12:00 es día con el sol arriba": no es noche, no es luna, y del astro < 0,2 · horizonte y fuerza = 1,0.
- `:17` "23:00 es noche con luna": es noche, es luna y queda arriba del horizonte.
- `:37` "a las 19 h sigue el atardecer y a las 20 h ya sale la luna": noche(19) falso, noche(20) verdadero, noche(5) falso, y a las 5 h no hay luna.
- `test/scene/water_painter_test.dart:27`: al mediodía la x del sol es exactamente la mitad del ancho.

**Dependencias:** F-SCN-02, F-SCN-09; la consumen F-SCN-08 (sombra), F-SCN-11 (disco), F-SCN-12 y F-SCN-13 (brillo en el agua).

**Mejora EXR:** sol y luna que salen y se ponen detrás del paisaje en lugar de aparecer de golpe, con luna con fases o al menos textura creíble, camino de luz en el agua realista y crepúsculos con cielo encendido.

---

### F-SCN-11: Cielo (degradé, estrellas, sol/luna y nubes)

**Valor para el usuario:** un cielo que acompaña la hora, con nubes que pasan y estrellas que van apareciendo al oscurecer.

**Comportamientos observables**

- [EXACTO] Orden: degradé → estrellas → sol o luna → nubes → silueta lejana del escenario (F-SCN-19) (`sky_painter.dart:36-56`).
- [VISUAL] Degradé vertical de skyTop (arriba) a skyHorizon (en la línea de horizonte) (`sky_painter.dart:39-48`).
- [EXACTO] **Estrellas**: 80, en posiciones fijas (siempre las mismas para un tamaño de pantalla), dentro del 85 % superior del cielo y con radio de 0,6 a 1,6 px. Su intensidad depende de la oscuridad: `oscuridad = clamp((1 − ambiente) / 0,65; 0; 1)`; solo se dibujan si es > 0 (`sky_painter.dart:49-52, 58-69`).
  - Opacidad = `(0,4 + 0,5 · parpadeo) · oscuridad`, con parpadeo = `0,5 + 0,5 · sin(2 · tiempo + i)` (cada estrella con su desfase).
  - Aparecen tenues desde el atardecer (a las 19 h ya están las 80) y llegan a intensidad plena con la noche cerrada (ambiente 0,35).
  - **Peculiaridad legacy:** como el ambiente es < 1 en todas las horas salvo las 12:00, quedan estrellas casi invisibles también de día (por ejemplo, a las 15 h con oscuridad ≈ 0,12). Ver Preguntas abiertas.
- [VISUAL] **Sol**: halo radial de 88 px de radio (sunGlow con opacidad 0,7 en el centro, 0,25 en el medio y 0 en el borde) y disco pleno de 22 px de radio color sunColor. **Luna**: mismo halo y disco más chico, de 17,6 px (0,8 × 22), sin sombra (`sky_painter.dart:71-86`).
- [EXACTO] **Nubes**: 7 nubes (`sky_painter.dart:29-33, 88-103`).

| # | x inicial | y relativa | tamaño (px) | velocidad propia |
|---|---|---|---|---|
| 1 | 0,08 | 0,10 | 34 | 1,0 |
| 2 | 0,26 | 0,18 | 22 | 1,4 |
| 3 | 0,42 | 0,07 | 40 | 0,8 |
| 4 | 0,58 | 0,22 | 26 | 1,2 |
| 5 | 0,72 | 0,12 | 36 | 0,9 |
| 6 | 0,88 | 0,20 | 24 | 1,3 |
| 7 | 0,97 | 0,05 | 30 | 1,1 |

  - x en pantalla = `((x0 + distancia · 0,0004 · vel + tiempo · 0,004 · vel) mod 1,2) · ancho − 0,1 · ancho`: se desplazan hacia la derecha con la distancia recorrida y con el tiempo, y reaparecen por la izquierda.
  - y = horizonte · (0,12 + yRel · 0,6).
  - Cada nube son 4 lóbulos blandos (opacos hasta el 55 % del radio y transparentes en el borde) más una sombra inferior.
  - Opacidad del cuerpo = `0,08 + 0,5 · ambiente²`, color blanco mezclado al 25 % con skyHorizon. Sombra: skyTop con opacidad 0,12 · ambiente. De noche las nubes son casi invisibles.

**Casos borde:** se pinta sin errores en los 4 escenarios × 2 orientaciones × {12 h, 23 h} × distancias {0, 1234 m}.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/painters/sky_painter.dart:1-124`; spec §Cielo.

**Tests legacy** (`test/scene/sky_shore_painter_test.dart`):
- `:16` "`$id WxH ${hour}h pinta sin excepciones`", 16 casos (4 escenarios × 2 tamaños × {12, 23} h), cada uno a distancia 0 y 1234 con tiempo 3,2 s; cielo y orilla se pintan sin errores.
- `:39` "las estrellas aparecen apenas oscurece (19 h), no solo de noche cerrada": a las 19 h se dibujan al menos 80 círculos más que a las 12 h.

**Dependencias:** F-SCN-02, F-SCN-09, F-SCN-10, F-SCN-19.

**Mejora EXR:** cielo volumétrico con nubes de formas variadas que se mueven lento con el viento, se iluminan por abajo al atardecer y se tiñen según la hora. Estrellas reales y vía láctea en noches despejadas. Sin estrellas visibles a pleno día.

---

### F-SCN-12: Agua avanzada (ondas, reflejo, brillo, estela y niebla)

**Valor para el usuario:** el agua es la protagonista: ondula, refleja el cielo, brilla bajo el sol o la luna y deja la estela del bote, como en EXR.

**Comportamientos observables** (para cada punto visible del agua; `shaders/water.frag:55-98`)

- [EXACTO] Ocupa toda la pantalla por debajo del horizonte. Las filas en la línea de horizonte o hasta medio píxel por debajo quedan transparentes (se ve el cielo) (`water.frag:57-60`).
- [EXACTO] Cada píxel se convierte a coordenadas de mundo con la inversa de la cámara (F-SCN-02): z de mundo = z + distancia de escena acotada (ver abajo) (`water.frag:61-63`).
- [EXACTO] **Altura de ola** `h(x, zMundo, z)` (`water.frag:45-53`):
  - `sin(zMundo / escala + 0,6 · t) · 0,6 + sin((0,7 · zMundo + 1,3 · x) / escala + 0,9 · t) · 0,4`;
  - más dos octavas de ruido suave: `(ruido(p · 0,8d + 0,15t) − 0,5) · 0,6 + (ruido(p · 2,6d − 0,25t) − 0,5) · 0,3`, con p = (x, zMundo), t = tiempo de escena y `d = clamp(6/z; 1; 6)` (detalle más fino cerca de la cámara);
  - escala = separación de crestas del escenario (F-SCN-15 a F-SCN-18).
- [EXACTO] **Color base**: mezcla de waterNear (cerca) a waterFar (lejos) con `smooth` entre z = 2 m y z = 120 m, multiplicada por `1 + h · amplitud · 0,25`. La amplitud es la del escenario, de 0 a 1 (`water.frag:67-68`). Los colores ya vienen teñidos por el escenario (solo la costa tiñe, F-SCN-17).
- [EXACTO] **Reflejo del cielo (Fresnel)**: `fres = (1 − clamp(2,2 / √(z² + 2,2²); 0; 1))³`; el color se mezcla con skyHorizon en proporción fres · 0,6. Cuanto más lejos, más espejo (`water.frag:71-72`).
- [EXACTO] **Brillo del sol o la luna** (`water.frag:75-82`):
  - Columna vertical bajo el astro: ancho = 25 px + 4 px por metro de z; intensidad `exp(−((px − xAstro)/ancho)²) · 0,4`.
  - Solo en las crestas: `smooth` de (h · 0,5 + 0,5) entre 0,3 y 0,9.
  - Destellos: factor `0,25 + 0,75 · destello`, con destello = `smooth` de un ruido de frecuencia (3 + 12/z) que se anima a 0,8t, entre 0,45 y 0,95.
  - Todo × fuerza de la luz (F-SCN-10); se suma sunColor · brillo · 0,9.
- [EXACTO] **Estela**: solo entre la popa (z = 5,3 m) y la cámara (`water.frag:84-94`).
  - Retroceso = 5,3 − z; semiancho de la V = 0,3 + 0,35 · retroceso.
  - Dos líneas de espuma en los bordes de la V (ancho de transición 0,35 m) más turbulencia central (`(1 − smooth(0..0,5 de |x|)) · 0,6`).
  - Se desvanece hacia la cámara: `1 − smooth(0..5,3 del retroceso)`.
  - Ruido de 0,6 a 1,0; se mezcla hacia el blanco con proporción máxima 0,35.
  - Intensidad = `clamp(velocidad / 4; 0; 1)`: plena a partir de 4 m/s (split 2:05) y nula con el bote detenido (`water_painter.dart:57`).
- [EXACTO] **Niebla**: mezcla con fog con `smooth` entre z = 80 m y z = 400 m (100 % a 400 m) (`water.frag:96`).
- [EXACTO] **Distancia acotada**: el agua usa la distancia de escena módulo **4096 m** para no perder precisión tras kilómetros (`scene_state.dart:33-34`).
- [EXACTO] Contrato de entradas del agua: 28 valores en este orden (`water_painter.dart:32-60`, `water.frag:7-23`):
  1. ancho, alto;
  2. horizonte, focal, altura de cámara (2,2), distancia acotada, tiempo;
  3. amplitud, escala de ola, x del astro, fuerza de la luz;
  4. waterNear (RGB), waterFar (RGB), skyHorizon (RGB), sunColor (RGB), fog (RGB);
  5. intensidad de estela, z de la popa (5,3).

**Casos borde**

- Cada 4096 m de distancia de escena el patrón de olas puede dar un salto leve, porque la distancia acotada vuelve a 0 (peculiaridad legacy).
- De noche el brillo usa el color de la luna (sunColor nocturno #E6EEFF) con fuerza 0,45.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `shaders/water.frag:1-98`; `scene/painters/water_painter.dart:29-77`; `scene/scene_state.dart:33-34`; `pubspec.yaml:59-60`; spec §Agua.

**Tests legacy:**
- `test/scene/water_painter_test.dart:27` "WaterPainter.uniforms devuelve 28 floats en el orden del shader": en 844×390, con velocidad 2, costa y 12 h, hay 28 valores; [0] = 844, [1] = 390, [2] = horizonte, [7] = 0,8 (amplitud de la costa), [9] = ancho/2 (sol centrado al mediodía), [26] = 0,5 (estela = 2/4) y [27] = 9,5 − 4,2.
- `test/scene/scene_state_test.dart:55` "shaderDistance se acota a 4096 m": 5000 → 904.

**Dependencias:** F-SCN-02, F-SCN-03 (velocidad y distancia), F-SCN-09, F-SCN-10, F-SCN-15 a F-SCN-18 (ola y tinte), F-SCN-13 (disponibilidad).

**Mejora EXR:** agua fotorrealista con reflejos reales de orilla, edificios, árboles y del propio bote y remero; olas con carácter distinto por escenario (espejo en el lago, chop urbano en el río, oleaje con espuma en la costa, agua planchada en la regata); estela con espuma que persiste y se abre; ondas que se cruzan; brillo del sol que encandila un poco al atardecer.

---

### F-SCN-13: Agua degradada y disponibilidad del agua avanzada

**Valor para el usuario:** la app se puede usar siempre, aunque el dispositivo no soporte el agua avanzada.

**Comportamientos observables**

- [EXACTO] La carga del agua avanzada se **precarga** al mostrar la vista de reposo de Entrenamiento (antes de remar), así la escena arranca con el agua avanzada. La carga es única y queda cacheada para toda la sesión de la app (`lib/features/workout/workout_screen.dart:42-44`; `scene/water_shader.dart:12-22`).
- [EXACTO] Si al abrir la escena el agua avanzada todavía no está lista, se usa el agua degradada y se cambia sola a la avanzada cuando termina de cargar, sin reiniciar la escena (`scene_view.dart:48-57`).
- [EXACTO] Si la carga falla (equipo sin soporte o recurso ausente), se registra en el log de diagnóstico el mensaje "`[Scene] Shader de agua no disponible, usando fallback: <error>`" y se usa el agua degradada **por el resto de la sesión** (no se reintenta). No se muestra ningún error al usuario (`water_shader.dart:17-19`).
- [VISUAL] **Agua degradada** (`water_painter.dart:79-104`):
  - Degradé vertical de 3 tramos: arriba waterFar mezclado al 50 % con fog, en el medio waterFar y abajo waterNear (con tinte de escenario).
  - **10 líneas de brillo** horizontales de 1,5 px, centradas en la x del astro. Posición `t = ((i/10) + (distancia · 0,02 mod 0,1)) mod 1`, a una altura `horizonte + alto del agua · t²`, con semiancho `ancho · (0,15 + 0,35 · t)` y color sunColor con opacidad `0,05 + 0,1 · (1 − t) · fuerza de la luz`.
  - Las líneas avanzan con la distancia: un ciclo completo cada 5 m.
  - No hay estela; los charcos sí se ven (los dibuja el bote).
- [EXACTO] La miniatura del selector usa siempre el agua degradada (F-SCN-22).

**Casos borde:** la escena desmontada mientras carga el agua no produce errores. Con el agua degradada la escena sigue cumpliendo todas las demás funcionalidades.

**UI:** sin estados visibles de carga ni de error.

**Datos persistidos:** ninguno.

**Archivos legacy:** `scene/water_shader.dart:1-23`; `scene/painters/water_painter.dart:62-104`; `scene/scene_view.dart:43-59, 75-79`; `lib/features/workout/workout_screen.dart:42-44`; spec §Agua (último párrafo).

**Tests legacy:**
- `test/scene/water_painter_test.dart:11` "sin shader pinta el fallback sin excepciones" (390×844, 12 h, lago).
- `test/scene/scene_view_test.dart:11`: en el entorno de test el agua avanzada no carga, así que los 16 casos ejercitan la escena con agua degradada y el desmontaje con la carga en curso.

**Dependencias:** `WKT` (vista de reposo que precarga), F-SCN-09, F-SCN-10, F-SCN-12.

**Mejora EXR:** el modo degradado debe seguir viéndose digno: agua con reflejo del cielo y algo de movimiento, sin cortes visibles. El paso de degradado a avanzado, si ocurre durante la escena, se hace con un fundido suave.

---

### F-SCN-14: Orilla con segmentos deterministas, visibilidad y tope

**Valor para el usuario:** el paisaje es siempre el mismo a la misma distancia (el remero reconoce "su" recorrido) y nunca se satura.

**Comportamientos observables**

- [EXACTO] El recorrido se divide en **segmentos de 50 m**. Cada segmento k (z de mundo de 50k a 50(k+1)) tiene una lista de objetos de orilla con posición lateral x, z de mundo, tipo, escala (por defecto 1) y variante (*seed*, que en los carteles de distancia es el número de metros a mostrar) (`environment.dart:17-37, 43, 72-80`).
- [EXACTO] La generación es **determinista** por escenario y segmento: el mismo escenario produce los mismos objetos a la misma distancia en cada sesión, sin importar el orden de dibujo. El legacy usa como semilla `índiceEscenario · 1 000 003 + k · 7919 + 17` (`environment.dart:77`). Ver Preguntas abiertas sobre si se replican las posiciones exactas.
- [EXACTO] Todos los objetos de un segmento quedan dentro del segmento (z entre 50k y 50(k+1), inclusive).
- [EXACTO] Todos los objetos quedan en la orilla, con |x| ≥ **9 m** (el canal visible tiene 18 m de ancho), salvo puentes (x = 0) y boyas de carril (que van en el agua) (`environment.dart:45-46`).
- [EXACTO] Rango visible: objetos con z de mundo entre **distancia + 1** y **distancia + 400 m** (`environment.dart:47, 89-100`).
- [EXACTO] Se ordenan **de lejos a cerca** antes de dibujar (`environment.dart:101`).
- [EXACTO] Tope de objetos por cuadro: **120** por defecto, **60** en la costa y **200** en la regata (`environment.dart:50`, `coast_environment.dart:25`, `regatta_environment.dart:20`).
- [EXACTO] Si se supera el tope (`environment.dart:102-113`):
  1. se conservan todos los objetos que no son boyas (los hitos);
  2. si sobra lugar, se completa con **las boyas más cercanas**;
  3. si aun sin boyas se supera el tope, se descartan los hitos **más lejanos**;
  4. el resultado se vuelve a ordenar de lejos a cerca.
- [EXACTO] Los segmentos que ya quedaron atrás se olvidan; al volver a necesitarse se regeneran idénticos (`environment.dart:94, 116-120`).

**Casos borde:** no hay segmentos negativos. Un cartel de distancia en z = 0 ("0 m") nunca es visible, porque el mínimo visible es distancia + 1 ≥ 1 m.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/environment.dart:1-129`; spec §Escenarios.

**Tests legacy** (`test/scene/environment_test.dart`, por cada uno de los 4 escenarios):
- `:10` "`$id`: propsForSegment es determinista": el segmento 7 pedido dos veces da exactamente lo mismo (tipo, x, z, variante) y difiere del segmento 8.
- `:17` "`$id`: cada segmento tiene objetos y quedan dentro del segmento": en los segmentos 0 a 29 ninguno está vacío, todo z está en [50k; 50(k+1)] y todo lo que no es puente ni boya tiene |x| ≥ 9.
- `:30` "`$id`: visibleProps ordena de lejos a cerca y respeta el máximo": a distancia 1234 hay a lo sumo el tope, en orden z decreciente, con z en [1235; 1634].
- `:54` "en la regata los hitos lejanos se ven aunque haya muchas boyas": a distancia 0 aparece una tribuna, torre o cartel con z > 150.

**Dependencias:** F-SCN-03 (distancia); la usan F-SCN-15 a F-SCN-18, F-SCN-20 y F-SCN-22.

**Mejora EXR:** paisaje continuo y con densidad natural (arboledas, grupos de edificios, tramos abiertos), sin objetos que aparezcan de golpe en el horizonte: entran con la niebla de distancia. El recorrido sigue siendo reconocible sesión a sesión.

---

### F-SCN-15: Escenario Lago alpino

**Valor para el usuario:** remar en un lago de montaña calmo, rodeado de pinos y con cumbres nevadas.

**Comportamientos observables** (`environments/lake_environment.dart`)

- [EXACTO] Agua calma: amplitud **0,25** y separación de crestas **2,5 m** (`:14-16`). Sin tinte de agua.
- [EXACTO] **Pinos** en ambas orillas (primero la izquierda, después la derecha) (`:22-34`):
  - el primero en `z0 + aleatorio · 6`, los siguientes cada `6 + aleatorio · 6` m (6 a 12 m), mientras z < z0 + 50;
  - |x| entre 10 y 16 m (9 + 1 + aleatorio · 6);
  - escala de 0,7 a 1,3;
  - variante de 0 a 999, que elige una de 3 paletas de verdes.
- [EXACTO] **Muelle** en la orilla izquierda (x = −9) a z0 + 25 cuando k mod 8 = 3, es decir, cada 400 m: a 175 m, 575 m, 975 m… (`:35`).
- [EXACTO] **Cabaña** en la orilla derecha (x = +13) a z0 + 10 cuando k mod 12 = 7, es decir, cada 600 m: a 360 m, 960 m, 1560 m… (`:36-38`).
- [EXACTO] Tope de 120 objetos visibles.
- Silueta lejana: montañas nevadas en dos planos (F-SCN-19).

**Casos borde:** todos los segmentos tienen pinos, así que nunca hay segmentos vacíos.

**UI y datos persistidos:** no tiene (la elección se guarda en F-SCN-21).

**Archivos legacy:** `scene/environments/lake_environment.dart:1-86`; spec §Escenarios (tabla).

**Tests legacy:** los tres tests por escenario de `environment_test.dart:10, 17, 30` (con `EnvironmentId.lake`).

**Dependencias:** F-SCN-14, F-SCN-19, F-SCN-20, F-SCN-21.

**Mejora EXR:** lago alpino realista: agua espejada que refleja montañas y bosque, pinos 3D con variedad y movimiento suave por el viento, muelles de madera y cabañas con detalle, bruma matinal sobre el agua y nieve brillante en las cumbres.

---

### F-SCN-16: Escenario Río urbano

**Valor para el usuario:** remar por un río de ciudad con clubes de remo, edificios, faroles y puentes de piedra.

**Comportamientos observables** (`environments/river_environment.dart`)

- [EXACTO] Agua: amplitud **0,35** y separación de crestas **2,0 m** (`:14-16`). Sin tinte.
- [EXACTO] **Edificios y clubes de remo (*boathouses*)** en cada orilla (primero la izquierda) (`:22-35`):
  - el primero en `z0 + aleatorio · 8`, los siguientes cada `15 + aleatorio · 15` m (15 a 30 m);
  - 30 % de probabilidad de club de remo y 70 % de edificio;
  - |x| entre 13 y 17 m (un poco retirados de la orilla para no tapar media pantalla al pasar);
  - escala de 0,6 a 1,0;
  - variante de 0 a 999, que define pisos (3 a 6) y tono.
- [EXACTO] **Faroles** en cada orilla, x = ±9,5, a z0 + 12,5 y z0 + 37,5, o sea, cada 25 m (`:36-38`).
- [EXACTO] **Boyas de carril amarillas** en x = ±3 a z0 + 25: un par cada 50 m (`:39`).
- [EXACTO] **Puente de piedra** en x = 0 a z0 + 20 cuando k mod 16 = 5, es decir, cada 800 m: a 270 m, 1070 m, 1870 m… (`:41`).
- [EXACTO] Tope de 120 objetos visibles.
- Silueta lejana: skyline con ventanas iluminadas de noche (F-SCN-19).

**Casos borde:** el puente pasa por encima de la cámara (F-SCN-20).

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/environments/river_environment.dart:1-72`; spec §Escenarios; captura `docs/image-03.png` (río urbano con HUD).

**Tests legacy:**
- Los tres tests por escenario de `environment_test.dart:10, 17, 30`.
- `environment_test.dart:60` "el río urbano tiene un puente cada 800 m": en los segmentos 0 a 31, los que tienen puente son exactamente [5, 21].

**Dependencias:** F-SCN-14, F-SCN-19, F-SCN-20, F-SCN-21.

**Mejora EXR:** río urbano con arquitectura creíble (fachadas, ventanales, clubes con rampas y botes), faroles que se encienden al anochecer con reflejos alargados en el agua, puentes de piedra con sombra y eco visual al pasar por debajo y ciudad iluminada de noche.

---

### F-SCN-17: Escenario Costa

**Valor para el usuario:** remar en mar abierto junto a acantilados, playas y un faro, con oleaje más movido.

**Comportamientos observables** (`environments/coast_environment.dart`)

- [EXACTO] Agua movida: amplitud **0,8** y separación de crestas **4,0 m** (`:16-19`).
- [EXACTO] **Tinte del agua**: ambos colores del agua se mezclan al 30 % con #0E6B7A (verde azulado) (`:21`).
- [EXACTO] Solo hay **orilla izquierda**, con la línea de costa en x = −14. A la derecha, mar abierto sin objetos (`:11-12`).
- [EXACTO] Segmentos de **playa** cuando k mod 5 = 2 (cada 250 m: segmentos de 100–150 m, 350–400 m…): 3 **sombrillas** en x entre −13 y −10, a `z0 + 8 + i · 14 + aleatorio · 6` (i = 0, 1, 2), con una variante que define el color (`:31-40`).
- [EXACTO] Resto de los segmentos: 2 **acantilados** en x = −14, a z0 y z0 + 25, con escala de 0,8 a 1,3 y variante de forma (`:41-45`).
- [EXACTO] **Faro** en x = −14 a z0 + 30 cuando k mod 20 = 10, es decir, cada 1000 m: a 530 m, 1530 m… Siempre cae sobre un tramo de acantilado (`:46`).
- [EXACTO] Tope de **60** objetos visibles (orilla despejada) (`:23-25`).
- Silueta lejana: acantilado lejano a la izquierda y 5 gaviotas en el cielo (F-SCN-19).

**Casos borde:** cada segmento tiene acantilados o sombrillas, así que nunca hay segmentos vacíos.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/environments/coast_environment.dart:1-89`; spec §Escenarios.

**Tests legacy:** los tres tests por escenario de `environment_test.dart:10, 17, 30`. La amplitud de la costa (0,8) se verifica en `water_painter_test.dart:27`.

**Dependencias:** F-SCN-12 (tinte y oleaje), F-SCN-14, F-SCN-19, F-SCN-20, F-SCN-21.

**Mejora EXR:** costa con acantilados rocosos de escala imponente, espuma rompiendo en la base, playa con arena y sombrillas, faro con haz giratorio de noche, gaviotas 3D que planean y aletean, y mar con oleaje de fondo y destellos intensos.

---

### F-SCN-18: Escenario Canal de regata

**Valor para el usuario:** remar en una pista de regata oficial, con carriles boyados, carteles cada 250 m, tribunas y torre de llegada.

**Comportamientos observables** (`environments/regatta_environment.dart`)

- [EXACTO] Agua: amplitud **0,3** y separación de crestas **1,5 m** (`:14-16`). Sin tinte.
- [EXACTO] **Boyas de carril cada 10 m** (z0, z0 + 10, … z0 + 40) en **4 líneas** (x = −9, −3, +3, +9), que forman 3 carriles visibles. El bote rema por el carril central. Son 20 boyas por segmento (`:26-32`).
- [EXACTO] El **color de las boyas cambia cada 250 m**: el índice es `(k div 5) mod 3`, con 0 = rojo #E53935, 1 = amarillo #FDD835 y 2 = blanco #ECEFF1. De 0 a 249 m son rojas, de 250 a 499 m amarillas, de 500 a 749 m blancas, de 750 a 999 m rojas y así sucesivamente (`:27`; `shore_painter.dart:185`).
- [EXACTO] **Cartel de distancia** en x = −10,5 a z0 cuando k mod 5 = 0 (cada 250 m), con el texto "`<metros> m`", donde metros = 50k: "250 m", "500 m", "750 m"… El cartel de "0 m" existe pero nunca se ve (F-SCN-14) (`:33-35`).
- [EXACTO] **Tribuna** en la orilla derecha (x = +12) a z0 + 10 cuando k mod 20 = 4: cada 1000 m, a 210 m, 1210 m… (`:36`).
- [EXACTO] **Torre de llegada** en la orilla izquierda (x = −12) a z0 + 10 cuando k mod 20 = 12: cada 1000 m, a 610 m, 1610 m… (`:37`).
- [EXACTO] **Bandera** en cada segmento a z0 + 30, alternando lado: derecha (x = +10,5) si k es par e izquierda (x = −10,5) si es impar. Color según k mod 4: rojo #E53935, ámbar #FFB300, azul #1E88E5 o verde #43A047 (`:38`; `shore_painter.dart:254`).
- [EXACTO] Tope de **200** objetos visibles (20 boyas × 8 segmentos visibles necesitan margen); los hitos tienen prioridad sobre las boyas (F-SCN-14) (`:18-20`).
- [EXACTO] Los carteles muestran la **distancia de escena** (F-SCN-03), no la del monitor.
- Silueta lejana: tribunas, torre y banderas lejanas que se repiten (F-SCN-19).

**Casos borde:** el texto del cartel solo se dibuja cuando su tamaño en pantalla llega a 4 px, es decir, a menos de ≈ 54 m en pantallas cuyo lado corto es 390 (F-SCN-20).

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/environments/regatta_environment.dart:1-72`; spec §Escenarios.

**Tests legacy:**
- Los tres tests por escenario de `environment_test.dart:10, 17, 30`.
- `environment_test.dart:42` "el canal de regata tiene boyas cada 10 m": el segmento 3 tiene 5 posiciones z distintas de boyas, separadas 10 m.
- `environment_test.dart:54` "en la regata los hitos lejanos se ven aunque haya muchas boyas".

**Dependencias:** F-SCN-03, F-SCN-14, F-SCN-19, F-SCN-20, F-SCN-21.

**Mejora EXR:** pista de regata con boyas flotantes que se mecen, carteles de distancia grandes y legibles, tribunas con público, torre de llegada con reloj, banderas al viento y, como mejora futura, línea de llegada y botes rivales o "fantasmas" (fuera de paridad).

---

### F-SCN-19: Siluetas lejanas del horizonte por escenario

**Valor para el usuario:** cada escenario tiene su perfil reconocible en el horizonte (montañas, skyline, acantilado o tribunas), que da profundidad.

**Comportamientos observables**

- [EXACTO] La silueta se dibuja sobre el cielo, apoyada en la línea de horizonte, después de las nubes (`sky_painter.dart:55`).
- [EXACTO] Color común: `mezcla(tono propio, fog, 0,35 + 0,4 · profundidad)`, con profundidad entre 0 y 1 (lo más lejano se funde más con la niebla) (`environment.dart:127-129`).
- [VISUAL] **Lago**: dos cordones de montañas (`lake_environment.dart:43-85`).
  - Lejano: alto máximo 12 % del alto de pantalla, tono #5C6E8A, profundidad 1,0, con nieve.
  - Cercano: alto máximo 8,4 % (0,7 × 12 %), tono #2F4A3A, profundidad 0,3, sin nieve.
  - Perfil (48 tramos): `alto · (0,45 + 0,35 · sin(6,3u + fase) + 0,2 · sin(15,1u + 2 · fase))`, con fase 0 en el lejano y 3,1 en el cercano, y frecuencia 1,0 en el lejano y 1,6 en el cercano.
  - Desplazamiento lateral con la distancia: el lejano 0,24 px por metro recorrido y el cercano 0,48 px por metro (parallax).
  - Nieve: triángulos en los picos locales que superan el 75 % del alto máximo, hasta el 82 % de la altura del pico, color #F2F6FA mezclado al 30 % con fog.
- [VISUAL] **Río**: skyline de bloques de 34 px de paso (`river_environment.dart:46-71`).
  - Alto de 30 % a 100 % del 12 % del alto de pantalla y ancho de 60 % a 100 % del paso.
  - Tono #3A4660 con profundidad 0,8.
  - Se desplaza 0,24 px por metro recorrido.
  - Cada bloque mantiene siempre la misma forma.
  - De noche (ambiente < 0,6) tiene ventanas de 3×4 px cada 7 px en vertical y 8 px en horizontal, encendidas con 50 % de probabilidad de forma fija por bloque (no titilan), color #FFE08A con opacidad 0,85.
- [VISUAL] **Costa** (`coast_environment.dart:51-88`):
  - Acantilado lejano en el 35 % izquierdo del ancho, de alto máximo 8 % de la pantalla, que se desvanece hacia el centro. Tono #4A3F3A con profundidad 0,9.
  - No se desplaza: su forma se modula suavemente con la distancia (fase = distancia · 0,002), sin saltos.
  - **5 gaviotas** en forma de "M" de trazo de 1,5 px, color #2B2B2B con opacidad 0,5 + 0,4 · ambiente.
    - Cruzan el cielo de izquierda a derecha a 8, 11, 14, 17 y 20 px/s y reaparecen por la izquierda.
    - Alturas: 35 %, 45 %, 55 %, 65 % y 75 % del horizonte, con ondulación de ±4 px.
    - Aleteo de ±3 px.
    - Tamaño de 5 a 9 px.
- [VISUAL] **Regata** (`regatta_environment.dart:43-71`):
  - Franja de tierra #55704A (profundidad 0,9) de 1,5 % del alto (25 % del alto máximo de 6 %).
  - Bloque repetido cada medio ancho de pantalla, que se desplaza 0,36 px por metro y vuelve a empezar cada ancho de pantalla:
    - tribuna #6E7B8C (profundidad 0,8) de 16 % de ancho;
    - torre #B8C0CC (profundidad 0,8) de 2,5 % de ancho y alto máximo completo;
    - 4 banderas con mástil, triangulares de 6 px, en rojo, ámbar, azul y verde (profundidad 0,5), que flamean (±2 px a 3 rad/s).
- [EXACTO] Las ventanas del skyline se encienden con el mismo umbral de noche que la orilla (ambiente < 0,6, ≈ 19:26 a 4:43).

**Casos borde:** se pinta sin errores a distancia 0 y a 1234 m, de día y de noche, en ambas orientaciones.

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/environment.dart:122-129`; `scene/environments/*.dart` (métodos `paintHorizon`); `scene/painters/sky_painter.dart:55`; spec §Cielo y §Escenarios.

**Tests legacy:** `test/scene/sky_shore_painter_test.dart:16` (16 casos; ver F-SCN-11).

**Dependencias:** F-SCN-02, F-SCN-03, F-SCN-09, F-SCN-11.

**Mejora EXR:** horizontes con profundidad atmosférica real (capas que se aclaran con la distancia) y relieve creíble. El horizonte se acerca muy lentamente a medida que se rema, en lugar de deslizarse de costado (ver Preguntas abiertas). Ciudad que se enciende ventana por ventana al anochecer y gaviotas que planean con vuelo natural.

---

### F-SCN-20: Dibujo de objetos de orilla (catálogo, luz, niebla y noche)

**Valor para el usuario:** objetos reconocibles en la orilla que pasan a la velocidad real y cambian con la luz del día y de la noche.

**Comportamientos observables**

- [EXACTO] Cada objeto se dibuja en su base proyectada `(x, 0, relZ)`, escalado por `escala de cámara(relZ) · escala del objeto`. Se ve de frente a la cámara (*billboard* 2.5D). Si relZ ≤ 0,5 m se omite (`shore_painter.dart:29-42`).
- [EXACTO] Color de cada parte: primero el tinte de ambiente (rojo y verde × ambiente, azul × min(1; ambiente + 0,12)); después la niebla de distancia, que mezcla con fog en proporción `clamp((relZ − 80)/320; 0; 1) · 0,8`. Empieza a 80 m y llega al 80 % a 400 m (`shore_painter.dart:45-50`).
- [EXACTO] "Noche" para la orilla = ambiente < **0,6** (≈ 19:26 a 4:43) (`shore_painter.dart:26`).
- [EXACTO] Las luces nocturnas **no** se tiñen ni se nublan, así que brillan.
- [VISUAL] Catálogo de los 15 tipos. Medidas en metros a escala 1, con y negativa hacia arriba; colores de referencia (`shore_painter.dart:54-270`):

| Tipo | Descripción y medidas de referencia | Colores | De noche |
|---|---|---|---|
| Pino (`pine`) | Tronco de 0,3 × 1,0 y 3 pisos triangulares de 2,2 de alto con semiancho 1,3 / 1,0 / 0,7; alto total ≈ 6 | Tronco #5D4037; verdes según variante mod 3: [#1B5E20, #2E7D32, #388E3C], [#145A32, #1E8449, #27AE60] o [#2E5E2A, #3E7B38, #4F9A48] | — |
| Cabaña (`cabin`) | Cuerpo de 4 × 2,6, techo a dos aguas hasta 4,2, puerta de 0,8 × 1,4 y ventana de 0,8 × 0,7 | #6D4C41, techo #4E342E, puerta #3E2723, ventana de día #90CAF9 | Ventana encendida #FFE082 |
| Muelle (`pier`) | Tablón de 3 × 0,4 que avanza hacia el centro del agua y 3 pilotes de 0,2 × 0,7 | #8D6E63, pilotes #5D4037 | — |
| Edificio (`building`) | 6 de ancho; pisos = 3 + variante mod 4 (3 a 6), de 3 m cada uno; 3 ventanas de 1,0 × 1,2 por piso | Tono según variante mod 3: #78909C, #8D6E63 o #B0BEC5; ventanas #263238 | Cada ventana encendida (#FFE082) con 60 % de probabilidad, fija por edificio |
| Club de remo (`boathouse`) | Cuerpo de 8 × 4, techo hasta 6, portón de 3 × 2,8 y franja azul de 7,2 × 0,4 | #B0BEC5, techo #546E7A, portón #37474F, franja #1565C0 | — |
| Puente (`bridge`) | Cara de piedra de x −9,5 a +9,5 con arco (luz de 17 m, clave a ≈ 5 m), pilares de 1 × 1,5, tablero de 24 × 1,2 a 6–7,2 m y parapeto de 24 × 0,5 hasta 7,7 m | Piedra #9E9E9E, pilares #757575, tablero #7A7A7A, parapeto #616161 | — |
| Boya (`laneBuoy`) | Esfera de radio 0,25 a 0,2 sobre el agua, con reflejo blanco (opacidad 0,25) de 0,4 bajo la línea de flotación | Según variante mod 3: rojo #E53935, amarillo #FDD835 o blanco #ECEFF1 | — |
| Farol (`lamp`) | Poste de 0,12 × 4 y lámpara de radio 0,3 a 4,1 de alto | Poste #455A64, lámpara de día #90A4AE | Halo de radio 1,5 #FFE082 (opacidad 0,35) y lámpara #FFF8E1 |
| Acantilado (`cliff`) | Pared de 8 de alto con borde superior irregular (5 escalones de 1,4 con variación de hasta 1,5), 7 de profundidad hacia tierra, césped de 7 × 0,9 arriba y roca oscura de 0,6 × 1,2 en la base | #5D4A42, césped #556B2F, roca #3E2F2A | — |
| Faro (`lighthouse`) | Torre de 2,2 × 9 con 3 franjas rojas de 1,0 y linterna de 1,6 × 1,2 hasta 10,2 | #ECEFF1, franjas #D32F2F, linterna #37474F | Halo fijo de radio 3 #FFF59D (opacidad 0,35), sin haz giratorio |
| Sombrilla (`beachUmbrella`) | Palo de 0,08 × 2,2, copa semicircular de 1,8 de diámetro y toalla de 1,2 × 0,1 | Palo #5D4037; copa según variante mod 3: #F06292, #4FC3F7 o #FFD54F; toalla #FFF3E0 | — |
| Tribuna (`grandstand`) | 4 gradas de 12 × 1,2 alternadas y techo de 13 × 0,5 a 5,3 de alto | #8D9DAE / #6E7B8C, techo #37474F | — |
| Torre de llegada (`finishTower`) | 3 × 10, con banda oscura de 3 × 1,2, reloj blanco de radio 0,6 con agujas negras y antena de 1,5 | #CFD8DC, banda #263238, antena #90A4AE | — |
| Bandera (`flag`) | Mástil de 0,1 × 5 y paño triangular de 1,6 que flamea `sin(4t + variante) · 0,2` m | Mástil #9E9E9E; paño según variante mod 4: #E53935, #FFB300, #1E88E5 o #43A047 | — |
| Cartel de distancia (`distanceMarker`) | Poste de 0,1 × 1,5 y cartel blanco de 1,8 × 1,0 (de 1,5 a 2,5 de alto) | Poste #9E9E9E, cartel #FAFAFA | — |

- [EXACTO] **Texto del cartel de distancia**: "`<metros> m`" en negro al 87 % y en negrita, de tamaño 0,55 m en escala de pantalla, centrado a 2,0 m de altura sobre la base. **No se dibuja si su tamaño es menor a 4 px** (`shore_painter.dart:272-285`).
- [EXACTO] El puente cruza todo el canal (x = 0) y, al acercarse, pasa **por encima de la cámara**. Muy cerca (relZ ≈ 1 m) cubre buena parte de la pantalla; con relZ ≤ 0,5 m desaparece.
- [EXACTO] Las banderas flamean con el tiempo de escena, también en pausa.

**Casos borde:** se pinta sin errores con un puente detrás de la cámara (relZ 0,3), un puente gigante (relZ 1,0), un cartel de 750 m y los 15 tipos juntos de noche (22 h).

**UI y datos persistidos:** no tiene.

**Archivos legacy:** `scene/painters/shore_painter.dart:1-289`; `scene/environment.dart:12-37`; spec §Escenarios.

**Tests legacy** (`test/scene/sky_shore_painter_test.dart`):
- `:16`: 16 casos de pintado sin errores (ver F-SCN-11).
- `:57` "objetos detrás de la cámara se omiten y un puente encima no rompe": puente a relZ 0,3 (omitido) y 1,0 (gigante), cartel "750 m" a 30 m y los 15 tipos a x = 12, a 20 m, con variante 3 y paleta de las 22 h.

**Dependencias:** F-SCN-02, F-SCN-09, F-SCN-14 a F-SCN-18.

**Mejora EXR:** objetos 3D con volumen, materiales y sombras propias. Los árboles se mecen, el agua refleja las luces de faroles y ventanas, el faro barre el mar con su haz, la gente aparece en tribunas y playas y los carteles de distancia se leen nítidos al pasar.

---

### F-SCN-21: Preferencia de escenario

**Valor para el usuario:** la app recuerda el último escenario elegido entre sesiones.

**Comportamientos observables** (`scene/scene_settings.dart`)

- [EXACTO] Escenarios disponibles, en este orden: **lago, río, costa, regata** (`environment.dart:10`).
- [EXACTO] Por defecto: **lago** (`:13`).
- [EXACTO] Se guarda en el almacenamiento de preferencias con la clave **`scene.environment`** y el valor textual **`lake` | `river` | `coast` | `regatta`** (`:11, 41`).
- [EXACTO] Al iniciar la app se lee el valor guardado. Si es válido y distinto del actual, se aplica y la UI se actualiza. Si falta o es desconocido, queda el lago (`:26-33`).
- [EXACTO] Si el usuario elige un escenario **antes** de que termine la lectura inicial, gana la elección del usuario y lo leído se ignora (`:15, 29, 36`).
- [EXACTO] Elegir el escenario que ya está seleccionado no hace nada: no notifica ni escribe. Igual cuenta como "el usuario eligió" a efectos de la regla anterior (`:36-37`).
- [EXACTO] Elegir otro escenario lo aplica de inmediato (la UI se actualiza) y después lo persiste (`:38-41`).
- [EXACTO] La preferencia está disponible en toda la app desde el arranque (`lib/main.dart:85`).
- [EXACTO] La hora forzada del simulador vive junto a esta preferencia, pero **no** se persiste (F-SCN-23).

**Casos borde:** al arrancar se puede ver por un instante el lago antes de que se aplique el escenario guardado (la lectura es asíncrona). Tocar el lago (por defecto) antes de que termine la lectura deja el lago sin guardarlo y descarta lo guardado para esa sesión.

**UI:** ver F-SCN-22.

**Datos persistidos:** preferencia `scene.environment` (texto: `lake`, `river`, `coast` o `regatta`). No hay columnas de base de datos.

**Archivos legacy:** `scene/scene_settings.dart:1-43`; `lib/main.dart:85`; spec §Selección de escenario.

**Tests legacy** (`test/scene/environment_picker_test.dart`):
- `:11` "tocar una tarjeta cambia el escenario y lo persiste": sin preferencias, arranca en lago y se ve "Lago alpino"; tocar "Río urbano" cambia a río y guarda `scene.environment = "river"`.
- `:38` "arranca con el escenario guardado": con `scene.environment = "coast"`, a los 50 ms el escenario es costa.

**Dependencias:** `APP` (registro global y almacenamiento de preferencias); la usan F-SCN-01, F-SCN-22 y F-SCN-23.

**Mejora EXR:** sin cambios de comportamiento. La elección se aplica sin parpadeo visible al arrancar.

---

### F-SCN-22: Selector de escenario con miniaturas

**Valor para el usuario:** elegir dónde remar antes de empezar, viendo una vista previa de cada lugar.

**Comportamientos observables**

- [EXACTO] Ubicación: en la vista de reposo de **Entrenamiento** (pestaña Workout), debajo del aviso de remo no conectado (si aparece) y **arriba del botón "Entrenamiento libre"** (`l10n.workoutFree`). Arriba lleva el título **`workoutScene`** ("Escenario" / "Scenery"), en blanco al 54 % y tamaño 13, con una separación de 8; debajo del selector hay 16 de separación (`lib/features/workout/workout_screen.dart:79-83`; captura `docs/image-02.png`).
- [EXACTO] Está **disponible aunque el remo no esté conectado**: elegir escenario no depende del dispositivo.
- [EXACTO] Fila horizontal desplazable de **4 tarjetas** en el orden lago, río, costa, regata, separadas 10 px. El alto total del selector es de 112 px (`environment_picker.dart:23-29`).
- [VISUAL] Cada tarjeta tiene una miniatura de **120 × 80** con esquinas redondeadas de 12, el nombre debajo (separación 6) en tamaño 12 y un borde (`environment_picker.dart:37-58`):
  - **Seleccionada:** borde #00B4D8 de 2,5, nombre en blanco y negrita (peso 700).
  - **No seleccionada:** borde blanco al 12 % de 1, nombre en blanco al 70 % y peso normal (400).
- [EXACTO] Tocar una tarjeta (miniatura o nombre) elige ese escenario (F-SCN-21); la selección se actualiza al instante (`environment_picker.dart:33`).
- [EXACTO] Nombres, por clave `.arb` (`lib/l10n/app_es.arb:56-60`, `app_en.arb:56-60`):

| Clave | es | en |
|---|---|---|
| `workoutScene` | Escenario | Scenery |
| `sceneLake` | Lago alpino | Alpine lake |
| `sceneRiver` | Río urbano | City river |
| `sceneCoast` | Costa | Coast |
| `sceneRegatta` | Canal de regata | Regatta course |

- [EXACTO] **Miniatura** (`painters/thumbnail_painter.dart:10-43`):
  - es la escena estática del escenario a las **12:00**;
  - tiene la cámara ubicada a **20 m** de distancia de escena, sin bote ni remero;
  - muestra el agua **degradada** (F-SCN-13) y solo los objetos de orilla a menos de 150 m del bote (z de mundo < 170);
  - no tiene animación (tiempo 0);
  - se redibuja solo si cambia el escenario de la tarjeta.

**Casos borde:** no hay estados de vacío, carga ni error. Si el ancho no alcanza para las 4 tarjetas, la fila se desplaza horizontalmente.

**UI:** pantalla Entrenamiento → vista de reposo. Única interacción: tocar.

**Datos persistidos:** a través de F-SCN-21.

**Archivos legacy:** `scene/environment_picker.dart:1-66`; `scene/painters/thumbnail_painter.dart:1-44`; `lib/features/workout/workout_screen.dart:79-83`; `lib/l10n/app_es.arb:56-60`; `lib/l10n/app_en.arb:56-60`; spec §Selección de escenario.

**Tests legacy:** `test/scene/environment_picker_test.dart:11` (ver F-SCN-21), en español: se ve "Lago alpino", tocar "Río urbano" selecciona el río y lo guarda.

**Dependencias:** `WKT` (vista de reposo), `APP` (idiomas es/en), F-SCN-13, F-SCN-14 a F-SCN-18, F-SCN-20, F-SCN-21.

**Mejora EXR:** tarjetas grandes con vista previa de alta calidad del lugar (idealmente animada: agua que se mueve, luz de la hora actual), animación suave de selección (realce y escala) y, como mejora opcional, una descripción breve del lugar. Mismo orden, nombres y comportamiento.

---

### F-SCN-23: Hora de escena forzada (solo modo simulador)

**Valor para el usuario (desarrollo y demos):** probar y mostrar la escena a cualquier hora del día sin esperar al reloj real.

**Comportamientos observables**

- [EXACTO] Solo existe en **modo simulador**, dentro del panel flotante del simulador (`SIM`), que está disponible sobre todas las pantallas, incluida la inmersiva (`lib/core/dev/simulator_overlay.dart:139-150`; `lib/main.dart:108-110`).
- [EXACTO] Fila con la etiqueta **"Hora escena"** (texto fijo en español, no está en `.arb`), botones − y + y un valor central (`simulator_overlay.dart:139-145`):
  - Valor: **"real"** si no hay hora forzada; si la hay, "`H:00`", con H = hora redondeada módulo 24 (por ejemplo, "7:00" o "23:00", sin cero inicial).
  - **+**: hora forzada = (hora forzada actual, o si no hay, la **hora entera actual del reloj**) + 1, módulo 24.
  - **−**: lo mismo con −1, módulo 24 (de 0 pasa a 23).
- [EXACTO] Con una hora forzada aparece el botón de texto **"Hora real"**, que la borra y vuelve a la hora del reloj (`simulator_overlay.dart:146-150`).
- [EXACTO] La escena aplica la hora forzada de inmediato, incluso durante el entrenamiento (`immersive_workout_screen.dart:47, 85`).
- [EXACTO] La hora forzada **no se persiste**: se pierde al cerrar la app (`scene_settings.dart:14, 19-24`).
- [EXACTO] Los botones tienen identificadores de prueba `sim-hour-minus` y `sim-hour-plus`.

**Casos borde:** la hora forzada siempre es una hora entera. El primer toque parte de la hora entera actual e ignora los minutos.

**UI:** panel del simulador (ver el inventario de `SIM`).

**Datos persistidos:** ninguno.

**Archivos legacy:** `lib/core/dev/simulator_overlay.dart:74, 79-80, 139-150`; `scene/scene_settings.dart:14, 19-24`; spec §Selección de escenario (último punto).

**Tests legacy:** `test/simulator_overlay_test.dart:17` "el panel se abre fuera del Navigator y + sube los watts": entre otros pasos, tocar `sim-hour-plus` hace aparecer "Hora real".

**Dependencias:** `SIM` (panel y modo simulador), F-SCN-09, F-SCN-21.

**Mejora EXR:** en modo simulador, un control continuo (deslizador de 0 a 24 h) además de los pasos de una hora, para ver las transiciones de luz en vivo. No forma parte del producto final.

---

## 3. Constantes de referencia (resumen)

| Concepto | Valor | Fuente |
|---|---|---|
| Altura de cámara | 2,2 m | scene_camera.dart:12 |
| Distancia cámara → remero | 9,5 m | scene_camera.dart:16 |
| Distancia mínima dibujable | 0,5 m | scene_camera.dart:19 |
| Horizonte vertical / horizontal | 40 % / 42 % del alto | scene_camera.dart:8 |
| Focal | lado más corto de la pantalla | scene_camera.dart:9 |
| Popa / proa del casco | z 5,3 / 13,5 (9,5 − 4,2 / + 4,0) | boat_painter.dart:26-27 |
| Altura del casco | 0,08 m | boat_painter.dart:25 |
| Pivote del remo | x = ±0,85 m | scene_state.dart:19 |
| Largo de remo exterior / mango | 2,9 m / 0,9 m | scene_state.dart:18; boat_painter.dart:30 |
| Inercia de velocidad | constante de tiempo 1,5 s; corte a 0 bajo 0,01 m/s | scene_state.dart:16, 56 |
| Tope de paso por cuadro | 0,1 s | scene_state.dart:52; scene_view.dart:63 |
| Ritmo de cierre de palada sin remar | 12 spm | scene_state.dart:23 |
| Vida de charco | 4 s | scene_state.dart:17 |
| Pala en el agua | p < 0,38 | stroke_cycle.dart:36 |
| Ángulo de remo en catch / finish | −55° / +35° | stroke_cycle.dart:9-12 |
| Cabeceo | 0,02·sin(2πp) + 0,006·sin(0,8t) rad | scene_state.dart:85 |
| Noche (luna) | elevación ≤ −0,4 (≈ 19:34 a 4:26) | time_of_day.dart:55 |
| Luces nocturnas | ambiente < 0,6 (≈ 19:26 a 4:43) | shore_painter.dart:26; river_environment.dart:51 |
| Fuerza de luz de noche / mínima de día | 0,45 / 0,2 | time_of_day.dart:64 |
| Segmento de orilla | 50 m | environment.dart:43 |
| Orilla mínima | \|x\| ≥ 9 m | environment.dart:46 |
| Rango visible | distancia + 1 a + 400 m | environment.dart:47, 90-91 |
| Tope de objetos | 120 (lago, río) / 60 (costa) / 200 (regata) | environment.dart:50 y entornos |
| Niebla de orilla | 80 m a 400 m (máx. 80 %) | shore_painter.dart:48-49 |
| Niebla de agua | 80 m a 400 m (100 %) | water.frag:96 |
| Estela plena | velocidad ≥ 4 m/s | water_painter.dart:57 |
| Distancia acotada del agua | módulo 4096 m | scene_state.dart:34 |
| Ola: amplitud / crestas | lago 0,25/2,5 · río 0,35/2,0 · costa 0,8/4,0 · regata 0,3/1,5 | entornos |
| Preferencia | `scene.environment` = lake/river/coast/regatta (defecto lake) | scene_settings.dart:11-13 |

---

## 4. Diferencias entre la documentación (spec) y el código (manda el código)

| # | Tema | Spec dice | Código hace |
|---|---|---|---|
| 1 | Velocidad objetivo | `split > 0 && activo` | además exige **spm > 0**, porque el parser repone el split medio y nunca vuelve a 0 |
| 2 | Palada sin spm | queda en 0 (catch) | **termina el ciclo a 12 spm** y queda en 0; en pausa se congela |
| 3 | Tope de paso por cuadro | no lo menciona | 0,1 s |
| 4 | Ratio drive/recuperación | "1:2" (también lo dice el comentario del código) | 0,40 / 0,60 = **1:1,5** |
| 5 | Umbral de noche | elevación ≤ −0,1 | **≤ −0,4** (≈ 19:34 a 4:26) |
| 6 | Keyframes de la paleta | 22 h noche "y de vuelta a 5" | noche cerrada **de 22 h a 4 h**; 4→5 interpola a amanecer |
| 7 | Posición del astro | elevación sin acotar; la luna "simétrica" | elevación **acotada a [0; 1]**; luna con hora + 12; agrega fuerza de luz (día 0,2–1,0; noche 0,45) |
| 8 | Estrellas | solo de noche | aparecen con `(1 − ambiente)/0,65`: tenues desde el atardecer **y casi invisibles también de día** |
| 9 | Luna | "disco con sombra" | disco más chico (0,8×), **sin sombra** |
| 10 | Nubes | 6–8, parallax distancia·0,002 + deriva | **7**, desplazamiento distancia·0,0004·vel + tiempo·0,004·vel (en fracción de ancho), opacidad por ambiente |
| 11 | Siluetas | dos planos con parallax 0,004 y 0,008 en todos | lago 2 planos (0,004 y 0,008); río 1 plano (0,004); costa sin desplazamiento (modulación 0,002); regata 1 plano (0,006). Altos de 12 %, 12 %, 8 % y 6 % |
| 12 | Gaviotas | tipo de objeto de orilla `gull` | **no existe ese tipo**: 5 gaviotas dibujadas en la silueta de la costa |
| 13 | Tipos de objeto | `mountainCabin`, `gull` | `cabin`; no existe `gull` (15 tipos) |
| 14 | Tope de objetos | 40 por cuadro, se descartan los más lejanos | **120 / 60 (costa) / 200 (regata)**; se descartan primero las boyas más lejanas y los hitos tienen prioridad |
| 15 | Puente | x −12 a 12 a y = 6 | tablero de −12 a 12 entre 6 y 7,2 m, parapeto hasta 7,7 m, arco de 17 m de luz |
| 16 | Playa en la costa | "playa con sombrillas" | la playa **reemplaza** al acantilado cada 250 m (k mod 5 = 2) con 3 sombrillas |
| 17 | Riggers | en z = 9,5 ± 0,3, x = ±0,8 | una barra por lado de x ±0,3 a ±0,85 en z = 9,5; pivote ±0,85 |
| 18 | Remero | tronco 0,55 m, cabeza 0,24 m | tronco **0,62 m**, cabeza de **0,28 m** de diámetro (exagerado a propósito) |
| 19 | Salpicadura | 6 gotas que caen 0,3 s | 6 gotas mientras **p < 0,08** (dura 0,2 s a 24 spm; depende del ritmo) |
| 20 | Charcos | anillos de 0,3 → 1,2 m | radio de **0,3 → 0,9 m**, óvalo (alto 0,35 × ancho), opacidad máx. 0,3 |
| 21 | Entradas del agua | uSunY, uSunElevation, uBoatZ | **sin uSunY**; uLight (fuerza) en lugar de la elevación; uSternZ (popa). 28 valores |
| 22 | Ondas y brillo del agua | ruido por celda + brillo por normal^24 | ruido suave de 2 octavas con frecuencia que crece cerca; brillo = columna gaussiana × crestas × destellos × fuerza |
| 23 | Espuma de estela | mezcla al 50 % | dos líneas en V + turbulencia central, **mezcla máx. 35 %** |
| 24 | Distancia del agua | no la menciona | acotada a **módulo 4096 m** |
| 25 | Agua degradada | degradé lejos → cerca + 10 líneas con opacidad por h | degradé de 3 tramos (con niebla arriba) + 10 líneas con opacidad por posición y fuerza de la luz |
| 26 | API de SceneView | parámetros `hour` e `isPaused` | `hourOverride`; la pausa se expresa como `isActive = false` |
| 27 | Miniatura | escena a las 12:00 con 3 objetos de orilla | todos los objetos a menos de 150 m (distancia 20 m), agua degradada |
| 28 | paintHorizon | firma (canvas, size, cam, palette) | agrega distancia y tiempo (gaviotas y banderas animadas) |
| 29 | Hora escena (simulador) | − / + de a 1 h, "vacío = hora real" | muestra "real"; agrega el botón **"Hora real"**; el primer toque parte de la hora entera actual |
| 30 | Sombra del bote | elipse desplazada según el sol | óvalo plano, 0,35 m del lado **opuesto** al astro, opacidad 0,08 × ambiente |

---

## 5. Preguntas abiertas

1. **Posiciones exactas del paisaje.** El legacy es determinista con su propio generador pseudoaleatorio. ¿La app Unity tiene que reproducir *exactamente* las mismas posiciones de pinos y edificios que el legacy, o alcanza con ser determinista y cumplir las mismas reglas (frecuencias, rangos, hitos en metros exactos)? **Propuesta PO:** alcanza con determinismo propio + reglas exactas; los hitos con posición fija (muelles, cabañas, puentes, faros, carteles, tribunas, torres, banderas, boyas) sí van en los metros exactos.
2. **Estrellas de día.** El legacy dibuja estrellas casi invisibles en todas las horas salvo las 12:00. ¿Se conserva (paridad literal) o se limita al atardecer y la noche? **Propuesta PO:** limitarlas a ambiente ≤ 0,85 (desde el atardecer), como cambio de producto menor y con motivo.
3. **Salto sol ↔ luna** a las 19:34 y 4:26 (el astro cambia de lado y de altura de golpe). ¿Se suaviza en Unity? **Propuesta PO:** sí, como mejora EXR (puesta y salida graduales), manteniendo los mismos umbrales de día y noche.
4. **Carteles de distancia de la regata vs. distancia del monitor.** El legacy usa la distancia de escena, que deriva de la del monitor. ¿Deben coincidir con los metros del HUD? **Propuesta PO:** mantener la paridad (distancia de escena) y evaluar la sincronización como mejora en una iteración posterior.
5. **Desplazamiento lateral del horizonte.** En el lago, el río y la regata la silueta lejana se desliza hacia la izquierda al avanzar, y las nubes hacia la derecha, algo poco natural para una cámara que va hacia adelante. ¿Se conserva? **Propuesta PO:** en 3D realista el horizonte debe acercarse lentamente; cambio de producto aceptable, porque el valor (sensación de avance) se mantiene.
6. **Modos de cámara, rivales o fantasmas, sonido, clima y línea de llegada.** Están fuera del alcance del legacy. ¿Entran como mejoras EXR posteriores? Quedan fuera de la paridad.
7. **Agua degradada en Unity.** ¿Sigue haciendo falta un modo degradado? **Propuesta PO:** sí, como restricción de producto: la app tiene que ser usable aunque el equipo no soporte el agua avanzada. El Unity Architect define cómo.
8. **Etiqueta "Hora escena" y "Hora real".** En el legacy son textos fijos en español, fuera del `.arb`. Como es una herramienta de desarrollo, se propone mantenerlos así o moverlos al `.arb` sin cambio visible.
9. **Salto del patrón de olas cada 4096 m.** Es una peculiaridad legacy imperceptible. No se exige reproducirla; alcanza con que el agua no pierda calidad en sesiones largas (más de 10 km).
10. **Personalización del bote y del uniforme.** No existe en el legacy. Mejora opcional futura.

---

## 6. Matriz de tests legacy → funcionalidad

| Archivo de test | Casos | Funcionalidades |
|---|---|---|
| `test/scene/scene_camera_test.dart` | 4 (`:9, :16, :30, :34`) | F-SCN-02 |
| `test/scene/scene_state_test.dart` | 7 (`:14, :26, :37, :46, :55, :60, :69`) | F-SCN-03 (:14, :26, :60), F-SCN-04 (:37, :46, :60), F-SCN-12 (:55), F-SCN-07 (:69) |
| `test/scene/stroke_cycle_test.dart` | 4 (`:5, :14, :23, :29`) | F-SCN-05 |
| `test/scene/boat_painter_test.dart` | 1 + 2 tamaños (`:10, :25`) | F-SCN-05 y F-SCN-08 (:10), F-SCN-07 y F-SCN-08 (:25) |
| `test/scene/time_of_day_test.dart` | 6 (`:9, :17, :25, :32, :37, :44`) | F-SCN-09 (:17, :25, :32, :44), F-SCN-10 (:9, :17, :37) |
| `test/scene/environment_test.dart` | 3 × 4 escenarios + 3 (`:10, :17, :30, :42, :54, :60`) | F-SCN-14 a F-SCN-18 |
| `test/scene/sky_shore_painter_test.dart` | 16 + 2 (`:16, :39, :57`) | F-SCN-11, F-SCN-19, F-SCN-20 |
| `test/scene/water_painter_test.dart` | 2 (`:11, :27`) | F-SCN-13 (:11), F-SCN-12 y F-SCN-10 (:27) |
| `test/scene/scene_view_test.dart` | 16 (`:11`) | F-SCN-01, F-SCN-13 |
| `test/scene/environment_picker_test.dart` | 2 (`:11, :38`) | F-SCN-21, F-SCN-22 |
| `test/simulator_overlay_test.dart` | 1 paso (`:54-56`) | F-SCN-23 |

Prueba manual del legacy (spec §Tests): con el simulador en Windows y el preset "Fuerte", capturas de los 4 escenarios a las 6, 12, 18 y 22 h usando "Hora escena". **Se conserva como prueba de aceptación visual de la migración.**
