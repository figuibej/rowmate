# It. 6b — Agua avanzada, reflejos, estela, niebla, vidrio con desenfoque y rendimiento (spec de producto)

| Campo | Valor |
|---|---|
| Iteración | 6b (`06b-agua-avanzada`), [ROADMAP §It. 6b](../../ROADMAP.md). Postergada desde la 6a (issue #6) y retomada a pedido del usuario después de la It. 15 |
| Alcance | F-SCN-01, F-SCN-12 y F-SCN-13 (dueña), con F-WKT-17 (vidrio con desenfoque) y F-SCN-03 (la velocidad que alimenta la estela). **En los 4 escenarios que ya existen** (Lago alpino, Río de ciudad, Costa y Regata), no solo en el lago. **Primero la deuda de la 6a** (§3.0) |
| Fuentes | [ROADMAP §It. 6b](../../ROADMAP.md); [REPORT de la 6a §5 y §7](../06a-escena-lago/REPORT.md) y su [01-po-spec](../06a-escena-lago/01-po-spec.md); REPORT de la [7](../07-remero-palada/REPORT.md) y la [8](../08-entornos-selector/REPORT.md); [immersive-scene](../../product/inventory/immersive-scene.md) (F-SCN-01, -03, -12, -13); [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) (Q-67, Q-68, R-04, R-12); [ARCHITECTURE §20](../../architecture/ARCHITECTURE.md); [ART_BIBLE §3.2 y §7.7](../../design/ART_BIBLE.md); [ANIMATION_BIBLE §4](../../design/ANIMATION_BIBLE.md); issues #1, #2, #3, #6, #12, #13, #22, #31 y #85 (API pública). Legacy [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura): `shaders/water.frag`, `scene/water_shader.dart`, `scene/painters/water_painter.dart`, `scene/scene_view.dart`, `test/scene/scene_view_test.dart` |
| Modo | Entrega rápida, sin debates. Ante una duda, el legacy o un valor por defecto, anotado en una línea (§7) |
| Criterios | **38** (§3). Set de capturas `it6b`: 32 capturas y 5 videos (§5) |

## 0. Cómo leer esta spec

- Tamaños (TV 411 × 914, TH 914 × 411, W1280 1280 × 720, en dp), escenas forzadas (`E-0`, `E-920`, `E-250`, `E-500`, `E-180`, `E-42K`), rutinas de prueba (`Corta`, `Series 600`), `Fuerte` del simulador, reloj de prueba y `ROWMATE_DATA_DIR` temporal: los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md) y la [de la 8](../08-entornos-selector/01-po-spec.md).
- Marcas: `[dotnet]` reglas puras con `dotnet test` (y las mismas en EditMode); `[Editor]` EditMode/PlayMode, capturas con sondas y medición de contadores; `[Device-Win]` build de desarrollo o de simulador en la PC con `ROWMATE_DATA_DIR` temporal; `[Device-Android]` el Android de gama media: **NO VERIFICABLE** (no hay teléfono ni "Android Build Support").
- **Medir en esta PC** (la GPU de la PC es la referencia de escritorio): cada corrida `[PERF]` registra la resolución y la frecuencia reales del monitor y la carga de CPU de otros procesos. Si el monitor sigue en modo de respaldo, se mide igual y se anota; si la GPU no informa tiempos, esa parte es NO VERIFICABLE (sin inventar números). Un tope que falla con carga externa documentada no es FAIL del código, pero el tope no se relaja. Nunca se tocan otros procesos del usuario.
- Niveles de calidad: Alto, Medio y Bajo de [ARCHITECTURE §20.1](../../architecture/ARCHITECTURE.md); en la PC la app elige Alto, y `-rowmate-quality` fuerza otro.
- IDs: `AC-6B-nn`. La tabla de §3.6 dice qué criterio existente cierra o cambia cada uno. Decisiones nuevas `DV-149` a `DV-153` (siguen a DV-148).
- Ninguna prueba, captura ni demo toca `prefs.json`, las bases ni las carpetas reales del usuario. Nada de Bluetooth real.

## 1. Objetivo y demo

**Objetivo.** El agua pasa a ser la protagonista en los cuatro escenarios: ondas con el carácter de cada lugar, reflejo del cielo y de la orilla, el camino de brillo del sol o la luna, la estela detrás del bote y la niebla a lo lejos. Si el equipo no puede, la app usa sola el agua degradada de la 6a y la sesión sigue sin cortes. El vidrio del HUD y el del diálogo de completado dejan ver la escena desenfocada detrás. Antes, se cierra la deuda de la 6a.

**Demo (≈ 10 min, `Builds/Windows/simulator`).**
1. Entrar a `Entrenamiento libre` apenas aparece Workout (antes de que la escena esté lista): la entrada no tironea.
2. Lago a las 12 h con `Fuerte`: ondas finas, la orilla y el bote reflejados, el camino de brillo bajo el sol, la estela en V que se abre y la niebla que esfuma lo lejano. Las tarjetas dejan ver la escena desenfocada.
3. `Hora escena` a las 6, 18 y 22 h: el brillo sigue al astro y, de noche, las ventanas y los faroles dejan columnas de luz en el agua.
4. Río, Costa y Regata desde el selector: el río con oleaje corto y corriente, la costa con mar de fondo, espuma en las crestas y boyas que suben y bajan, la regata casi espejo.
5. `Corta` hasta el final: el bote frena desenfocado detrás del vidrio del diálogo mientras la cámara sube.
6. Con `-rowmate-water degraded` (o la falla simulada): el agua de la 6a aparece con un fundido y la sesión sigue.

## 2. Historias

- Como remero quiero que el agua se vea y se mueva como agua de verdad en cada escenario, para sentir que estoy remando afuera.
- Como remero con un equipo modesto quiero que la app siga fluida aunque no pueda mostrar el agua avanzada.
- Como remero quiero leer las métricas sobre un vidrio que deja ver la escena detrás, sin perder contraste.
- Como mantenedor quiero cerrar la deuda de la 6a antes de construir encima, con medidas que la prueben.

## 3. Criterios de aceptación

### 3.0 Primero: la deuda de la 6a (#1, #2, #3 y Dispositivo)

- **AC-6B-01 · #1 La entrada sin tirón con la precarga sin terminar (AC-WKT-13-15 vuelve a PASA; QA-V6a-R3-01).** *Dado* el build de simulador recién arrancado (arranque en frío, W1280), *cuando* una medida `[PERF]` entra a `Entrenamiento libre` a **1,0 s** de que aparece Workout, con la precarga de la escena **sin terminar** (lo registra), *entonces* mientras algo se mueve (el fundido a negro, la aparición de la escena con su fundido de 0,6 s, el vuelo y la cascada) **ningún cuadro pasa de 16,7 ms** de CPU del hilo principal. 5 arranques con el lago y 1 con cada uno de los otros 3 escenarios elegidos en reposo. La sesión no espera a la escena (AC-SCN-01-07). Una prueba PlayMode corre la misma medida. [Device-Win] · [Editor]
- **AC-6B-02 · #1 El primer toque del proceso (QA-V6a-R2-04).** *Dado* 5 arranques en frío seguidos, *entonces* el cuadro del primer toque de la app (el de `Entrenamiento libre`) no pasa de **16,7 ms** en **5 de 5** y el registro guarda el mínimo y el máximo (en la 6a: 12,7 a 16,8 ms) junto con la carga externa. [Device-Win]
- **AC-6B-03 · #2 Las 10 líneas de brillo (AC-SCN-13-03 vuelve a PASA; QA-V6a-PAR-01).** *Entonces* el agua degradada dibuja **exactamente 10** líneas (`water_painter.dart:93`): una sonda que recorre la columna del astro cuenta 10 en las capturas del lago a las 6, 12 y 18 h (también en la fila de abajo de la pantalla, donde estaba la 11.ª). El set `it6a` se regenera y dos corridas dan lo mismo. [Editor]
- **AC-6B-04 · #3 Montañas y halo (QA-V6a-12).** *Entonces*, sin cambiar DV-65 ni los números del legacy: (a) a las 12 h la silueta lejana del lago contrasta con el cielo detrás **≥ 1,3:1** de luminancia en TV y en TH; (b) en TH la ladera cercana tapa **≤ 30 %** del ancho de la cordillera lejana; (c) a las 18 h la luminancia del cielo a 2 diámetros del disco del sol supera en **≥ 15 %** la de 6 diámetros. Sondas en CAP-03, CAP-17 (§5), y comparación a la vista con LEG-03 y LEG-05. [Editor]
- **AC-6B-05 · Dispositivo no se redibuja debajo del inmersivo.** *Dado* un entrenamiento con el simulador mandando lecturas a 1 Hz, *entonces* durante 60 s con el inmersivo arriba la pantalla de Dispositivo se redibuja **0 veces**; al salir del inmersivo muestra la última lectura en su primer cuadro visible. [Editor]

### 3.1 Qué agua usa cada equipo y la caída sin cortar (F-SCN-13; Q-67, R-04)

- **AC-6B-06 · La regla de elección.** *Entonces* el agua se elige así, sin que el usuario vea un ajuste (el legacy tampoco lo tiene):

  | Situación | Agua |
  |---|---|
  | El equipo no puede usar el agua avanzada (no carga o no es compatible) | Degradada, por el resto de la sesión de la app (sin reintentar, `water_shader.dart:12-22`) |
  | Nivel Alto / Medio / Bajo | Avanzada en su variante del nivel (ART_BIBLE §7.7: Alto con reflejo planar, Medio con reflejo analítico de orilla, Bajo con lo mínimo) |
  | La adaptación térmica ya bajó resolución, reflejo, sombras, vegetación y nivel, y los cuadros siguen largos (> 13 ms de GPU, o > 15 ms de CPU si la GPU no informa, sostenidos 3 s) | Degradada, por el resto de la sesión de la app (DV-152) |
  | Forzada en el build de desarrollo o de simulador (`-rowmate-water auto\|advanced\|degraded`) | La forzada. El build normal ignora el argumento |
  | Miniatura del selector | Siempre la degradada (F-SCN-22, paridad) |

  La regla es pura y tiene sus casos de prueba (una fila por línea de la tabla). [dotnet]
- **AC-6B-07 · Carga y cambio a la avanzada.** *Entonces* el agua avanzada se prepara en Workout junto con la escena (P-14 de la 6a, `workout_screen.dart:44`). Si al entrar todavía no está lista, se ve la degradada y la avanzada entra sola con un **fundido de 0,6 s** cuando termina (en el acto con "reducir movimiento"), sin reiniciar la escena ni cambiar su distancia, tiempo o velocidad (`scene_view.dart:48-57`). [Editor] · VID-02
- **AC-6B-08 · La caída, sin cortar la sesión.** *Cuando* el agua avanzada falla en medio de una sesión (falla simulada con un argumento del build de desarrollo a los N s) o se cumple la fila térmica de AC-6B-06, *entonces* la degradada entra con un fundido de 0,6 s; el reloj, los datos, el HUD, el guardado y la escena siguen; ningún cuadro del cambio pasa de **33 ms**; el registro tiene **una sola** línea `[Scene] Shader de agua no disponible, usando fallback: <motivo>` (`water_shader.dart:18`) y el usuario no ve ningún error. La sesión guardada es idéntica a la de una corrida sin falla (mismos datos). [Editor] · [Device-Win] · VID-02
- **AC-6B-09 · La degradada sigue igual.** *Entonces*, forzada, el agua degradada cumple todo lo de la 6a y la 8: AC-SCN-13-02 a -04 (sin estela, `≤ 28 ALU`, el tope vigente desde la It. 8; QA-V6b-16), AC-SCN-12-01 (las franjas de la costa) y AC-6B-03. Los charcos del bote se siguen viendo. [Editor] · CAP-21 a CAP-24

### 3.2 El agua avanzada en los 4 escenarios (F-SCN-12, F-SCN-03)

- **AC-6B-10 · El contrato del legacy, en números.** *Entonces* el modelo del agua avanzada da, contra `water.frag:55-98`: color base `lerp(waterNear; waterFar; smoothstep(2; 120; z))` (z = 61 m → 0,5); peso del reflejo `0,6·(1 − 2,2/√(z² + 2,2²))³` = **0,128** a 5 m, **0,290** a 10 m, **0,424** a 20 m y **0,561** a 100 m (± 0,005); niebla `smoothstep(80; 400; z)` = 0 a 80 m, **0,5** a 240 m, 1 a 400 m; estela con intensidad `clamp(velocidad/4; 0; 1)` (2 m/s → 0,5; 4,67 m/s → 1; 0 → 0), nacimiento en la popa a **5,3 m** de la cámara (`9,5 − 4,2`), semiancho `0,3 + 0,35·retroceso` (≈ 19,3°; 2,155 m a 5,3 m de retroceso) y espuma como mucho **35 %**; columna de brillo de `25 + 4·z` dp de ancho con peso 0,4 × fuerza de la luz (0,2-1,0 de día, 0,45 de noche); las filas en el horizonte o medio dp debajo, transparentes. El contrato de 28 entradas sigue (LT-124). [dotnet] · `water.frag:57-96`; `water_painter.dart:32-60`
- **AC-6B-11 · Cada escenario con su agua (DV-150).** *Entonces* cada escenario usa su perfil de ART_BIBLE §7.7, que sale de la amplitud y la separación de crestas del legacy: **lago** 0,25 / 2,5 (Espejo), **río** 0,35 / 2,0 (oleaje corto con corriente hacia la cámara de 0,25 m/s), **costa** 0,8 / 4,0 (mar de fondo, tinte `#0E6B7A` al 30 %), **regata** 0,3 / 1,5 (Pista, casi espejo). En las capturas de las 12 h, la variación de luminancia del agua cercana (franja de 2 a 20 m) ordena: regata ≤ lago < río < costa. El casco de la costa sigue la ola de AC-SCN-06-04. [dotnet] (la tabla) · [Editor] CAP-02, -06, -10, -14
- **AC-6B-12 · Reflejo del cielo, la orilla, el bote y el remero (DV-149; #13, #31).** *Entonces* el agua refleja el cielo de esa hora y la orilla, con el peso de AC-6B-10 (más espejo cuanto más lejos): en **Alto**, un reflejo planar a 1/4 de resolución con la orilla, el horizonte, el puente y las fachadas del río, el **bote y el remero**; en **Medio**, una banda del tono de la orilla donde el reflejo la cruzaría, ondulada por las olas; en **Bajo**, solo el cielo. Sonda: a las 12 h en Alto, el agua justo debajo de una masa oscura de la orilla (pinos del lago, fachadas del río) es **≥ 10 %** más oscura que el agua bajo cielo abierto en la misma fila; el reflejo del bote aparece debajo del casco (CAP-31). [Editor] · CAP-01 a -16, CAP-25, CAP-26
- **AC-6B-13 · El camino de brillo del astro.** *Entonces* bajo el sol o la luna que se ve hay un camino de brillo que titila sobre las crestas, × la fuerza de la luz y × la visibilidad del astro (DV-59: se apaga cuando se pone, sin saltar a las 19:34). Sonda: la columna más brillante de la franja de agua de 20 a 80 m queda a **± 40 dp** de la x del astro a las 6, 12 y 18 h; a las 22 h con la luna, más tenue que a las 12 h. [Editor] · CAP-01 a CAP-16
- **AC-6B-14 · La estela (F-SCN-03; #13).** *Entonces* detrás de la popa hay una V de espuma con turbulencia central (AC-6B-10) que **persiste 12 s y se abre** (DV-151) y nace en la popa que se dibuja (con el empuje de la palada, *surge*, ANIMATION_BIBLE §4.5). Con el bote quieto no hay estela; con `Fuerte` (≥ 4 m/s) está a pleno; al pausar o dejar de remar se apaga con la velocidad (inercia de 1,5 s) mientras el agua sigue ondulando (`scene_view.dart:63`, F-SCN-01). [dotnet] (intensidad) · [Editor] VID-03
- **AC-6B-15 · Niebla y horizonte sin costura.** *Entonces* la niebla del agua sigue el contrato `S(80; 400; z)` (`water.frag:96`): a 400 m el agua es exactamente el color `fog` de esa hora antes de la costura [dotnet]. En captura W1280, la fila de z ≈ 300 m (5,3 dp debajo del horizonte, fuera de la costura) mide un peso de niebla de **0,77 ± 0,05**, con el método de 02-tech-design §8 [Editor] · CAP-28, -30 y -31. En el horizonte no hay costura: **donde el agua toca el cielo o la franja de bruma** (nunca sobre una silueta), la diferencia entre la última fila de agua y la primera de cielo es **≤ 6/255** por canal, con las filas y columnas que define el arte §2.8, en las 16 tomas de la matriz [Editor] · CAP-01 a CAP-16.
  *Motivo del cambio (gate G6b-04):* en TV, 400 m caen a 2,26 dp del horizonte, adentro de la costura de 3 dp que lleva el agua a `skyHorizon`, así que no se podía medir ± 2/255 contra `fog`. Y medir la costura contra una silueta contradecía el contraste de las siluetas que pide AC-6B-04.
- **AC-6B-16 · Charcos, gotas y hervidero en el agua (#13).** *Entonces* en Medio y Alto los charcos del contrato (2 por palada, el izquierdo primero, 4 s, radio 0,3 → 0,9 m, fijos en el mundo, ninguno en pausa; F-SCN-07) aplanan el oleaje adentro y tienen borde de espuma; cada gota y la salpicadura dejan **micro-anillos** (radio 0,03 → 0,2 m en 0,5 s); bajo la pala, en el drive, un hervidero de 0,4 × 0,25 m. En Bajo y en la degradada, los charcos como en la It. 7. [dotnet] (reglas) · [Editor] CAP-32
- **AC-6B-17 · #12 Efectos visibles a la escala del remero.** *Entonces* en TV (remero de ≈ 47 dp) el hervidero, los charcos y la salpicadura se distinguen del agua de alrededor: contraste de luminancia **≥ 1,15:1** con la escena sola y la fase forzada (edad del par de charcos en 1,0 s); el par de remolinos se orienta con el eje de la pala en el finish (**≈ 40°** respecto de x, ± 5°). CAP-04 de la 7 se vuelve a tomar con el charco proyectado a la pantalla y lo encuentra. [Editor] · CAP-32
- **AC-6B-18 · #31 Columnas de luz de noche.** *Entonces*, con las luces de la noche encendidas (AC-SCN-20-04: ambiente < 0,6), cada farol, ventana encendida, linterna y el faro de la costa deja una columna de luz vertical en el agua debajo de su x de pantalla, que ondula con las olas y se enciende con su luz (sin saltos). De día no hay ninguna. [Editor] · CAP-04, -08, -12, -16; VID-04
- **AC-6B-19 · #31 La costa: espuma de crestas y boyas.** *Entonces* en la costa las crestas del mar de fondo tienen espuma (solo arriba de la ola, nunca más del 35 %) y las boyas suben y bajan con la misma ola que el casco (`h·0,15` m, ± 0,12 m) sin hundirse ni flotar en el aire. [Editor] · CAP-10, VID-05
- **AC-6B-20 · Olas sin salto a los 4096 m (Q-68).** *Entonces* el agua no usa la distancia acotada del legacy (que sigue como contrato, 5000 → 904, AC-SCN-03-06): (a) [dotnet] la fase de cada ola, calculada a 4095,9 y a 4096,1 m, avanza lo mismo que entre 0,0 y 0,2 m (± 1e-6 rad); (b) [Editor] en VID-01, cruzando 4096 m con `Fuerte`, la diferencia entre cuadros consecutivos no tiene un pico mayor que **1,5 ×** la mediana del video; (c) a `E-42K` el agua se ve como a 0 m (CAP-30 contra CAP-02, AC-SCN-01-15).
- **AC-6B-21 · #85 La popa no es una tabla blanca.** *Entonces* la cubierta del casco va de `#F5F5F5` a `#C9D1D6` hacia la popa y los últimos **0,6 m** se afinan; en el cuadro de CAP-10 de la It. 15 (lago, W1280, Alto, ahora CAP-31) la zona de la popa no es un rectángulo uniforme: su luminancia varía **≥ 8 %** de punta a punta. Casco, borda `#00B4D8` y cockpit sin cambios de contrato (AC-SCN-01-08). [Editor] · CAP-31
- **AC-6B-22 · Escena viva en los 4 escenarios con el agua avanzada (LT-84 a LT-87, LT-127 a LT-138).** *Entonces* los 16 casos de `scene_view_test.dart:11` (4 escenarios × V390 y H844 × 12 y 23 h; SPM 24, split 110 s, 300 m, en marcha, hora forzada) animan 6 cuadros de 100 ms **con el agua avanzada** sin excepciones y se cierran sin excepciones ni avisos, también si se cierran mientras el agua avanzada se está preparando. [Editor]

### 3.3 Vidrio con desenfoque (F-WKT-17; P-04, P-19)

- **AC-6B-23 · El vidrio deja ver la escena.** *Dado* Alto o Medio, *entonces* todas las piezas de vidrio del inmersivo (tarjetas, banda de tiempo y distancia, barra de pasos, paneles de series, controles y diálogos) muestran la escena **desenfocada** detrás (a 1/4 de resolución con 3 pasadas), con tinte `#070D16` de alfa `lerp(0,72; 0,86; smoothstep(0,10; 0,70; L))` según la luminancia L del fondo (L 0,10 → 0,72; 0,40 → **0,79**; 0,70 → 0,86), borde 1 dp `#FFFFFF @10 %` y reflejo interior `#FFFFFF @6 % → 0` en los 8 dp de arriba, nunca debajo de un texto (ART_BIBLE §3.2). Sonda: sobre la orilla del lago, la variación de luminancia dentro de una tarjeta es **≤ 25 %** de la de la misma zona sin tarjeta. [dotnet] (alfa) · [Editor] CAP-27, CAP-28
- **AC-6B-24 · Contraste en el peor caso.** *Entonces* sobre una nube blanca el vidrio da **≥ 13,5:1** al blanco, **≥ 7,0:1** a las métricas `*.hud` y **≥ 4,5:1** a la familia roja (D-ART-01), igual que el respaldo de `@86 %`; ningún texto queda sobre el reflejo interior ni sobre el *glow*. [dotnet] · [Editor]
- **AC-6B-25 · Bajo, sin soporte y cambio de nivel.** *Entonces* en Bajo, o si el desenfoque no está disponible, el vidrio es el respaldo plano `glass.fallback` (`#070D16 @86 %`, sin reflejo interior), como hoy. Si el nivel cambia en medio de la sesión, el vidrio pasa de uno a otro en **200 ms**, sin destello ni reacomodar el HUD. El desenfoque no depende del agua: con el agua degradada y Medio, el vidrio sigue desenfocado. [Editor]
- **AC-6B-26 · Costo del desenfoque.** *Dado* el build de simulador en W1280 con `-rowmate-quality medium`, *entonces* el desenfoque cuesta **≤ 0,6 ms** de GPU por cuadro (ARCHITECTURE §20.1), con la resolución y la frecuencia del monitor anotadas; si la GPU no informa tiempos, NO VERIFICABLE en esa parte. El HUD no suma elementos (sigue en ≤ 57 de 60) y no reserva memoria por cuadro. [Device-Win] · [Editor]
- **AC-6B-27 · El diálogo de completado con el bote detrás (P-19; QA-V6a-03; AC-SCN-02-07).** *Cuando* `Corta` termina y entra el diálogo de completado con la toma aérea, *entonces* el diálogo (mismo contenido, lugar y momento que la 4b) es de vidrio desenfocado y detrás se ve el bote frenando: una sonda sobre la popa proyectada, debajo del diálogo, tiene **≥ 10 %** más luminancia que una sonda de agua a la misma altura fuera del bote, y el texto del diálogo cumple AC-6B-24. En Bajo, el respaldo plano (el bote no se ve, como en la 6a). [Editor] · CAP-29

### 3.4 Rendimiento (F-SCN-01; R-12)

- **AC-6B-28 · Fluida en la PC, en los 4 escenarios (`[PERF]`; AC-SCN-01-10, -16, -19).** *Dado* el build de simulador en W1280 con el nivel que elige la app (Alto) y otra vez con `-rowmate-quality medium`, *entonces* en cada escenario, libre con `Fuerte` a las 12 y a las 22 h, en la regata en `E-180` (con público) y en el diálogo de completado: p99 del cuadro **≤ 16,7 ms**, ningún cuadro **> 33 ms** y **0 bytes** por cuadro en régimen (FrameAllocations con su control positivo y Profiler del build de desarrollo). Se anotan resolución, frecuencia y carga externa. [Device-Win]
- **AC-6B-29 · Dentro de los topes, por nivel y escenario.** *Entonces* en el Editor, con el agua avanzada, en los 4 escenarios × Alto, Medio y Bajo: draw calls **≤ 150**, SetPass **≤ 80 / 50 / 50** (Alto con el espejo, D-01 y ARCHITECTURE §20.1; G6b-06), triángulos visibles **≤ 600 k / 350 k / 180 k**, CPU de escena **≤ 1,5 ms**, malla del agua **≤ 20 k** triángulos. El agua cuesta por fragmento **≤ 110 ALU y 6 texturas** (Alto), **≤ 70 y 5** (Medio), **≤ 35 y 2** (Bajo); la degradada sigue en ≤ 28 y 0 (QA-V6b-16); y la espuma de la costa baja de 62 a **≤ 30 ALU** (#22: el tope vuelve a 30 en su prueba). `SceneBudgetTests` y `HudBudgetTests` sin relajar topes. [Editor]
- **AC-6B-30 · Memoria y cambio de escenario.** *Entonces* alternar 10 veces entre los 4 escenarios entrando y saliendo del inmersivo con el agua avanzada no deja avisos y la memoria vuelve a **± 5 %** de la del primer recorrido; la memoria total queda **≤ 1,5 GB** en escritorio (AC-SCN-01-18, ARCHITECTURE §20.2). [Editor] · [Device-Win]
- **AC-6B-31 · Si el equipo no llega, baja el detalle (AC-SCN-01-13).** *Entonces* el orden de la adaptación es resolución → reflejo → distancia de sombras → densidad de vegetación → nivel → **agua degradada** (el último paso, AC-6B-06); nunca baja la tasa de cuadros antes que el detalle; un cambio de resolución, como mucho cada 10 s. Con una carga de GPU simulada en el build de desarrollo, el registro muestra los pasos en ese orden. [dotnet] (el orden) · [Device-Win]
- **AC-6B-32 · 60 fps 30 min en el Android de gama media (R-12; AC-SCN-01-09).** *Dado* el teléfono de referencia en Medio, *entonces* 30 min de libre en cada escenario con el agua avanzada a 60 fps sostenidos, dentro de los topes. [Device-Android] **NO VERIFICABLE** (sin teléfono ni módulo; sigue en #7)

### 3.5 Capturas, regresión y repo

- **AC-6B-33 · Captura de aceptación visual por escenario y hora.** *Entonces* el set `it6b` (§5) tiene la matriz **4 escenarios × 6, 12, 18 y 22 h** en TV con el agua avanzada (16), las 4 de las 12 h en TH, las 4 degradadas, las del vidrio y el diálogo, y los videos; cada toma lleva sus sondas en el `manifest.json` y dos corridas dan las mismas sondas. El Designer las compara con las del legacy (LEG de la 6a y la 8) y las de la 6a/8 en `INDEX.md`. [Editor]
- **AC-6B-34 · `Hora escena` y la noche, sobre el agua.** *Entonces* al cambiar `Hora escena` (1,5 s por el camino más corto, DV-58) el agua cambia de color, de brillo y de reflejo junto con el cielo, sin saltos; con "reducir movimiento", en el acto. [Editor] · VID-04
- **AC-6B-35 · Nada de lo anterior se rompe.** *Entonces* todos los tests de las It. 1 a 15 siguen en verde: `dotnet test` (también `-Matrix`), EditMode y PlayMode con y sin simulador; los criterios de la escena de la 6a, la 7 y la 8 que no cambia esta spec siguen pasando en el set regenerado. Un test intermitente no se arregla subiendo su tolerancia sin explicar la causa. [dotnet] · [Editor]
- **AC-6B-36 · Las reglas fuera de Unity (Q-69; AC-SCN-01-14).** *Entonces* lo marcado [dotnet] (la regla de elección, el orden de la adaptación, los números de AC-6B-10, los perfiles de AC-6B-11, la fase de las olas, la intensidad de la estela y el alfa del vidrio) corre con .NET 8 sin Unity y las mismas pruebas pasan en EditMode. [dotnet] · [Editor]
- **AC-6B-37 · Assets propios y gratis.** *Entonces* el agua, la espuma, los mapas de normal y el desenfoque son propios (shaders URP y texturas procedurales generadas en el Editor), detrás de los *slots* existentes (`water.profile.*`, `water.normalDetail`, `water.foam`); nada pago ni de terceros sin licencia libre documentada en `THIRD_PARTY_NOTICES.md`. [Editor]
- **AC-6B-38 · Repo limpio.** *Entonces* `ProjectSettings` no cambia (y el define del simulador nunca queda ahí); `HorizonFar.mat` y `HorizonNear.mat` no cambian; el legacy sigue sin cambios (`git status --porcelain` vacío); lo versionado no tiene rutas de la PC, nombres de usuario ni datos del equipo. [Editor]

### 3.6 Criterios existentes que esta spec cierra o cambia

| Existente | Qué pasa | Por |
|---|---|---|
| AC-WKT-13-15 (FAIL desde la 6a) | Vuelve a PASA | AC-6B-01 |
| AC-SCN-13-03 (FAIL desde la 6a) | Vuelve a PASA | AC-6B-03 |
| AC-SCN-13-01 ("en la 6a, el agua es la degradada") | **Cambia**: la degradada es el respaldo; la regla es AC-6B-06 | AC-6B-06 a -09 |
| AC-SCN-13-04 ("sin estela, es de la 6b") | Sigue para la degradada; la estela es de la avanzada | AC-6B-09, -14 |
| AC-SCN-02-07 (la toma aérea, "se completa en la 6b") | Se cierra con el vidrio | AC-6B-27 |
| AC-SCN-01-05, -17 (escena viva) | Con el agua avanzada | AC-6B-22 |
| AC-SCN-01-10, -11, -13, -16, -19 | Con el agua avanzada y el vidrio | AC-6B-28, -29, -31 |
| AC-SCN-01-09 | Sigue NO VERIFICABLE | AC-6B-32 |
| AC-SCN-01-15 / AC-SCN-03-06 | Con el agua avanzada | AC-6B-20 |

## 4. Issues → criterios

| Issue | Criterios | Entra | Qué queda |
|---|---|---|---|
| #6 (la iteración) | AC-6B-06 a -38 | En parte | 60 fps en Android (AC-6B-32), a #7 |
| #1 (tirón antes de la precarga) | AC-6B-01, -02 | Entero | — |
| #2 (11 líneas de brillo) | AC-6B-03 | Entero | — |
| #3 (montañas y halo) | AC-6B-04 | Entero | — |
| #12 (efectos poco visibles a 47 dp, remolinos) | AC-6B-16, -17 | Entero | — |
| #13 (reflejo del remero y del bote, micro-anillos, estela con *surge*) | AC-6B-12, -14, -16 | Entero | El reflejo del bote y del remero es de Alto (en Medio, banda de orilla: ARCHITECTURE §20.1) |
| #22 (Foam ≤ 30 ALU) | AC-6B-29 | Entero | — |
| #31 (reflejos, columnas de luz, espuma de crestas, oleaje propio) | AC-6B-11, -12, -18, -19 | Entero | — |
| #85 (popa como tabla blanca) | AC-6B-21 | Entero | #14 y #19 (remero y composición), fuera |

## 5. Capturas (set `it6b`, carpeta `06b-agua-avanzada/snapshots`)

Escena con el HUD salvo donde dice "sola"; bote con `Fuerte` (estela a pleno) en la distancia de referencia de cada escenario (lago `E-920`, río `E-250`, costa `E-500`, regata `E-180`); nivel Alto salvo donde se indica.

| Toma | Tamaño | Qué |
|---|---|---|
| CAP-01 a CAP-04 | TV | Lago a las 6, 12, 18 y 22 h (CAP-03: halo, AC-6B-04) |
| CAP-05 a CAP-08 | TV | Río, ídem (CAP-08: columnas de luz) |
| CAP-09 a CAP-12 | TV | Costa, ídem (CAP-10: crestas y boyas) |
| CAP-13 a CAP-16 | TV | Regata, ídem |
| CAP-17 a CAP-20 | TH | Los 4 escenarios a las 12 h (CAP-17: montañas, AC-6B-04) |
| CAP-21 a CAP-24 | TV | Los 4 a las 12 h con `-rowmate-water degraded` (CAP-21: 10 líneas) |
| CAP-25 / CAP-26 | TV | Lago 12 h en Medio / en Bajo |
| CAP-27 | TV | `Series 600` con los dos paneles: vidrio sobre la orilla (AC-6B-23) |
| CAP-28 | W1280 | Río 18 h, libre: vidrio sobre el atardecer |
| CAP-29 | TV | `Corta`, diálogo de completado a los 36,3 s, toma aérea (AC-6B-27) |
| CAP-30 | W1280 | Lago `E-42K`, 12 h (AC-6B-20) |
| CAP-31 | W1280 | Lago 12 h, el cuadro de CAP-10 de la It. 15: popa y reflejo del bote (#85, #13) |
| CAP-32 | TH | Lago, escena sola, fase forzada, charcos de 1,0 s, hervidero y micro-anillos (#12) |
| VID-01 | W1280 | Lago cruzando 4096 m con `Fuerte` (AC-6B-20) |
| VID-02 | TV | Avanzada → degradada con la falla simulada, y de vuelta al arrancar sin falla (AC-6B-07, -08) |
| VID-03 | TV | Estela: arrancar, `Fuerte` y frenar en pausa (AC-6B-14) |
| VID-04 | TV | `Hora escena` 18 → 22 h en el río: columnas de luz que se encienden (AC-6B-18, -34) |
| VID-05 | TH | Costa: mar de fondo, casco y boyas (AC-6B-19) |

`tools/unity/capture-snapshots.ps1` suma el set `it6b` con su carpeta (y no escribe en la de otra iteración). El set `it6a` se regenera por AC-6B-03.

## 6. Paridad con el legacy

| Comportamiento | Legacy | Test legacy | AC |
|---|---|---|---|
| Ola, color base, Fresnel, brillo, estela, niebla | `water.frag:45-96` | `water_painter_test:27` (LT-124) | AC-6B-10, -11, -13 a -15 |
| Distancia acotada a 4096 m | `scene_state.dart:33-34` | `scene_state_test:55` | AC-6B-20 (contrato; el agua no la usa, Q-68) |
| Precarga, respaldo mientras carga, sin reintento, log | `water_shader.dart:12-22`; `scene_view.dart:48-57`; `workout_screen.dart:44` | `scene_view_test:11` | AC-6B-06 a -08 |
| Respaldo: degradé + 10 líneas | `water_painter.dart:79-104` | `water_painter_test:11` | AC-6B-03, -09 |
| La escena anima y se libera en 4 escenarios | `scene_view.dart:47-79` | `scene_view_test:11` (16) | AC-6B-22 |
| Velocidad → estela | `water_painter.dart:57` | `scene_state_test:14` | AC-6B-14 |

## 7. Decisiones (rigen si el usuario no dice otra cosa)

| ID | Legacy | App | Si se rechaza |
|---|---|---|---|
| DV-149 | El agua se mezcla con un color plano (`skyHorizon`) | Refleja el cielo real de la hora y la orilla (en Alto también el bote y el remero), con el **peso** del legacy | Color plano con el peso del legacy |
| DV-150 | Una misma fórmula de ola escalada por escenario | Carácter por escenario (ART_BIBLE §7.7) a partir de la amplitud y la escala del legacy | La fórmula del legacy en los 4 |
| DV-151 | La estela solo entre la popa y la cámara, sin memoria | Persiste 12 s y se abre detrás del bote | La V del legacy, sin persistencia |
| DV-152 | El agua degradada solo si el agua avanzada no carga | También como último paso de la adaptación térmica, sin volver hasta reiniciar la app | Solo si no carga |
| DV-153 | Los umbrales de las sondas de AC-6B-04, -12, -17, -21 y -27 (1,3:1; 30 %; 15 %; 10 %; 1,15:1; 8 %) | Valores por defecto del PO para medir "se lee" | El Designer propone otros con la medida |

Decididas por el PO al escribir: la elección de agua no se muestra al usuario (el legacy no tiene ajuste de calidad); el contrato de Fresnel es el peso del legacy, no el Schlick de ART_BIBLE §7.7 (paridad); el diálogo de completado queda donde está y se ve el bote por el vidrio (P-19 por defecto); la captura de aceptación es la matriz completa en TV más las 12 h en TH.

## 8. Fuera de alcance

- 60 fps en Android (AC-6B-32 NO VERIFICABLE, #7); Apple (10b).
- Un ajuste de calidad o de agua visible al usuario (el legacy no lo tiene).
- Cambiar de escenario durante el entrenamiento, clima, lluvia, cámaras alternativas (la ola de proa), botes rivales (Q-66).
- Texturas CC0 o fotos de terceros (D-ART-08 sigue en "no"); el Terrain Sample Asset Pack sigue opcional.
- El remero y la composición de la It. 7 (#14, #19); impostores de pinos lejanos (DT6a-14), salvo que los pida AC-6B-29.
- El diálogo quieto a 30 fps (P-07, decisión del teléfono).

## 9. Para issues

- Reflejo del bote y del remero en Medio, si el teléfono deja margen.
- Ola de proa (0,05 m) cuando haya cámaras alternativas.
- Volver al agua avanzada sin reiniciar la app cuando el equipo se enfría (hoy, DV-152).
- Fresnel de Schlick (ART_BIBLE §7.7) como opción de arte, si el Designer lo prefiere con la medida.

## 10. Al cerrar la iteración (PO, 2026-11-11)

QA: RECHAZADO en las rondas 1 y 2 ([05-qa-verification](05-qa-verification.md)). **El PO la acepta con 3 FAIL** de causa medida, como en la 15: **31 PASS · 3 FAIL · 4 NO VERIFICABLE**.

| Tema | Decisión |
|---|---|
| AC-6B-01 y -02 (FAIL, #1) | Se aceptan con su dato (toque 22,9-36,1 ms; con movimiento 14,0-20,2 ms con el monitor en su modo normal, 6/16 > 16,7) y vuelven a #1, que sigue abierto. AC-WKT-13-15 sigue en FAIL |
| AC-6B-20 b (FAIL) | Se acepta con su causa (el ancla de la malla cada 16 m) y va a un issue propio, el primero de la deuda de la 6b |
| AC-6B-15, -26, -29 (CPU de Alto) y -32 | NO VERIFICABLE: costura no concluyente sin la pasada sin siluetas; sin la medida de GPU del desenfoque; el tope de 1,5 ms de Alto falla con 25-31 % de carga externa documentada y pasa con la PC más quieta (no se relaja); Android sin teléfono (#7) |
| #85, la media de la popa (QA-V6b, 04-design-notes §8) | Rige la cubierta de la 6a (0,80-0,95): la media de 0,897 entra y AC-6B-21 (≥ 8 %, mide 13,1 %) pasa. No se agrega el tope de ≤ 0,82 |
| #13 | Se cierra entero: reflejo del bote y del remero en Alto, micro-anillos y estela con *surge* pasan (AC-6B-12, -14, -16). Lo que falta del espejo (las instancias de la orilla) es de #31 |
| Antes del commit | `INDEX.md` de la 6b repuesto desde la corrida completa de QA (38 filas, el `manifest.json` es igual) y el de la 7 desde HEAD con la fila nueva de CAP-04 (QA-V6b-R2-05). Los sets viejos con el lago (6a, 7, 8, 14, 15) no se regeneran ahora: van al issue de capturas (QA-V6b-R2-06) |
| Texto de la spec | AC-6B-09 y -29 corregidos: la degradada ≤ 28 ALU (QA-V6b-16) y SetPass 80 en Alto (G6b-06) |
| DV-149 a DV-153 | Rigen por defecto, al gate del usuario |
