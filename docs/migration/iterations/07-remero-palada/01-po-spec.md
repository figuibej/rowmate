# It. 7 — Remero, bote y palada biomecánica: spec de producto

| Campo | Valor |
|---|---|
| Iteración | 7 (`07-remero-palada`), [ROADMAP §It. 7](../../ROADMAP.md) |
| Incluye | F-SCN-04, F-SCN-05, F-SCN-06, F-SCN-07, F-SCN-08 ([immersive-scene](../../product/inventory/immersive-scene.md)) |
| También toca | F-SCN-01 (rendimiento con el remero), F-SCN-02 (la respiración de la cámara suma el bote), F-WKT-07 (la boya de 500 m, P-06 de la 6a), F-WKT-17 y F-WKT-18 (la zona del bote con el remero; `Series anteriores` en vertical, P-03 de la 5) |
| Base | La escena de la 6a con el **agua degradada**. La 6b (agua avanzada y vidrio con desenfoque) quedó postergada por decisión del usuario (issue #6): nada de esta spec depende de ella |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`); `scene/` = `lib/features/workout/scene/`. Leídos enteros: `scene/stroke_cycle.dart`, `scene/scene_state.dart`, `scene/painters/boat_painter.dart`, `test/scene/stroke_cycle_test.dart`, `scene_state_test.dart`, `boat_painter_test.dart`; los presets de `lib/core/dev/simulator_overlay.dart:9-14` (`Suave` 100 W / 20 spm, `Fuerte` 280 W / 30 spm). `git -C ../rowmate-legacy status --porcelain` vacío |
| Modo | Entrega rápida, sin debates (decisión del usuario). Ante una duda, el legacy o el valor por defecto, anotado en una línea (§13). Lo menor va a "Para issues" (§15) |

---

## 0. Cómo leer esta spec

- Notación, tamaños (V390, H844, TV, P360, TH, L640, W1280), marcas de verificación (`[dotnet]`, `[Editor-EM]`, `[Editor-PM]`, `[Captura]`, `[Device-Win]`, `[Device-And]`, `[PERF]`) y estados de prueba (`R-4`, `F-5`, `E-*`, hora de prueba, carpeta de datos temporal): los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md), sin cambios.
- IDs: `AC-SCN-04-nn` a `AC-SCN-08-nn` son nuevos. `LT-88` a `LT-98` siguen a la 6a (terminó en LT-87); `DV-68` a `DV-78` siguen a DV-67.
- **`p`** = fase del contrato (la de `SceneState` del legacy, 0–1). **`φ`** = fase visual (la que se ve, D-ANI-01). **`c`** = fase canónica que sale de `φ` con el ritmo por SPM (Q-70). Los ramps del legacy se evalúan sobre `c`. `smooth(t) = t²(3 − 2t)`; `ramp(x; a; b) = smooth(clamp((x − a)/(b − a); 0; 1))`.
- **Esfuerzo** `e = clamp((W − 60)/240; 0; 1)`, suavizado τ = 1 s (con `R-4`, 150 W: e = 0,375).
- **Nuevos estados de prueba** (no existen en un build que se instala):
  - **Fase forzada**: `φ` (y `c`) fijadas en un valor, con el remero, los remos y el bote en esa pose.
  - **Personaje forzado**: `maniquí` (procedural) o `Y Bot` (si `Assets/ThirdParty/Mixamo/YBot/YBot.fbx` está y valida).
  - **Cámara lateral de prueba**: a 4 m del remero por la derecha, a 1,0 m de alto, mirando al perno; solo para capturas y pruebas, nunca en la app (Q-66: las cámaras extra son del backlog).

---

## 1. Objetivo y demo

**Objetivo.** Un remero 3D rema en un single al ritmo real del SPM, con la secuencia piernas → tronco → brazos, remos con giro de pala, salpicadura en el ataque, charcos de pala, cabeceo del bote y sombra según el astro. Funciona completo con un **maniquí procedural** y usa **Y Bot de Mixamo** si el usuario lo bajó. Nada de esto cambia la distancia, las métricas ni lo que se guarda.

**Demo** (build de simulador, W1280, español, hora real):

1. `Entrenamiento libre` → llave → `Suave` (20 spm, 100 W). El remero rema lento: drive de ≈ 1,0 s y recuperación de ≈ 2,0 s. Al salir la pala quedan dos charcos que se alejan hacia la cámara y se apagan en 4 s; en cada ataque, salpicadura.
2. `Fuerte` (30 spm, 280 W). La palada se acelera sin saltos (drive ≈ 0,82 s, recuperación ≈ 1,18 s); más salpicadura. El bote cabecea y "empuja" en cada palada.
3. `Pausa`: el remero frena en ≈ 0,25 s (no se congela de golpe) y el bote desliza hasta quedar quieto; al rato, el remero respira. `Reanudar`: retoma desde donde estaba.
4. `Hora escena` a `09:00`, `17:00` y `22:00`: la sombra del bote cambia de lado con el astro.
5. Rutina `Series 600` (`F-5`): en el trabajo, una boya pasa junto al bote cuando el panel de parciales cierra los 500 m.
6. Con `YBot.fbx` en su carpeta, el remero es Y Bot; sin él, el maniquí, con la misma palada.

**Lo que depende del usuario:** bajar Y Bot (opcional para cerrar la iteración: sin él, los criterios de Y Bot quedan NO VERIFICABLE y todo lo demás se verifica con el maniquí). Sin el teléfono Android, los 60 fps en Android quedan NO VERIFICABLE (regla 9).

---

## 2. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero**, quiero ver a un atleta que rema a mi ritmo, para sentir que el bote soy yo. |
| H-02 | Como **remero que aprende técnica**, quiero ver la secuencia piernas → tronco → brazos y el giro de la pala, para copiarla. |
| H-03 | Como **remero en una rutina**, quiero ver pasar una boya al cerrar cada 500 m, para sentir el avance en el agua. |
| H-04 | Como **usuario sin Mixamo**, quiero que la app se vea completa igual, para no depender de una descarga. |

---

## 3. Criterios de aceptación

**Conteo:** 36 nuevos (F-SCN-04: 5, F-SCN-05: 10, F-SCN-06: 3, F-SCN-07: 3, F-SCN-08: 12, también toca: 3) + 2 que cambian (§4) = **38**.

### 3.1 F-SCN-04 — Fase de palada

- **AC-SCN-04-01 · Avanza a spm/60 (LT-88).** *Dado* el entrenamiento activo con spm > 0, *entonces* `p` avanza `spm/60 · dt` por cuadro (dt acotado a 0,1 s) y vuelve a 0 al completar el ciclo. Con pasos de 0,05 s: 1 s a 24 spm → `p = 0,4 ± 1e-6`; 1,5 s más → `0,0` o `1,0 ± 1e-6`. [dotnet] · `scene_state.dart:59-60`
- **AC-SCN-04-02 · Sin paladas termina el ciclo (LT-89).** *Dado* activo con spm = 0 y `p > 0`, *entonces* la palada termina a **12 spm** (0,2 ciclos/s) y queda en `p = 0` exacto; con `p = 0`, queda quieta. 0,5 s a 24 spm → 0,2; después 6 s con spm 0 (split 107) → `p = 0,0` y velocidad < 0,1 m/s. [dotnet] · `scene_state.dart:21-23, 61-65`
- **AC-SCN-04-03 · En pausa se congela (LT-90).** *Dado* no activo, *entonces* `p` no cambia: 0,5 s a 24 spm y 2 s en pausa → la misma fase y velocidad < 0,5 m/s. [dotnet] · `scene_state.dart:59-65`
- **AC-SCN-04-04 · La fase visual sigue sin saltos (D-ANI-01).** *Entonces* lo que se ve usa `φ`, con `φ' = ω_s + 4 · wrap(p − φ)`, `wrap ∈ (−0,5; 0,5]`, `φ' ≥ 0` (nunca retrocede); `ω_s` sigue a `spm/60` con τ = 0,5 s activo y cae a 0 con τ = 0,15 s en pausa. **Sin paladas** (activo con spm 0), `ω_s` se mantiene en 12/60 hasta que `φ` cruza el catch; ahí `φ = 0` y `ω_s = 0` exactos, y el remero queda en el catch (después respira, AC-SCN-05-10). *(Cambio por gate QA7-G-03: con `ω_s` → 0 la fase quedaba trabada en c ≈ 0,04.)* En régimen, `|φ − p| < 0,001` ciclos; en un escalón 24 → 30 spm el error máximo es ≤ 0,05 ciclos y se absorbe en ≤ 1 palada. `p` y sus tests no cambian. [dotnet] · ANIMATION_BIBLE §3.1
- **AC-SCN-04-05 · Ritmo según el SPM (Q-70).** *Entonces* el drive casi no se acorta y la recuperación sí: `T_d = clamp(1,05 − 0,0194·(spm − 18); 0,60; 1,15)` s; `r = clamp(T_d·spm/60; 0,22; 0,48)`; `c = 0,40·φ/r` si `φ < r`, si no `c = 0,40 + 0,60·(φ − r)/(1 − r)`. El SPM de `r` se suaviza con τ = 0,8 s y `r` se fija al cruzar el catch (nunca cambia a mitad de palada). Valores: 12 spm r = 0,230; **20 spm drive 1,011 s, recuperación 1,989 s, r = 0,337**; 24 spm r = 0,373; **30 spm drive 0,817 s, recuperación 1,183 s, r = 0,409**; 36 spm r = 0,420 (± 0,001). `c` es continua y monótona en el ciclo. [dotnet] · [Captura] VID-01, VID-02 · ANIMATION_BIBLE §3.2

### 3.2 F-SCN-05 — Secuencia biomecánica

Los ramps del legacy, sobre `c`: piernas `ramp(c; 0; 0,18)` si c < 0,60, si no `1 − ramp(c; 0,60; 1)`; espalda `ramp(c; 0,10; 0,30)` si c < 0,50, si no `1 − ramp(c; 0,50; 0,75)`; brazos `ramp(c; 0,22; 0,36)` si c < 0,40, si no `1 − ramp(c; 0,40; 0,60)`; inclinación `lerp(+25°; −20°; espalda)`; barrido `lerp(−55°; +35°; smooth(c/0,36))` hasta 0,36, +35° hasta 0,40, `lerp(+35°; −55°; smooth((c − 0,40)/0,60))` después; pala en el agua mientras c < 0,38 (`stroke_cycle.dart:9-45`).

- **AC-SCN-05-01 · Catch (LT-92).** *Entonces* con c = 0: piernas, brazos y espalda = 0; pala en el agua; barrido < 0; inclinación > 0. [dotnet] · `stroke_cycle_test.dart:5`
- **AC-SCN-05-02 · Finish (LT-93).** *Entonces* con c = 0,36: piernas, brazos y espalda = 1; barrido > 0; inclinación < 0; con c = 0,39 la pala está fuera. [dotnet] · `stroke_cycle_test.dart:14`
- **AC-SCN-05-03 · Orden del drive (LT-94).** *Entonces* con c = 0,12: piernas > espalda > brazos y brazos = 0. [dotnet] · `stroke_cycle_test.dart:23`
- **AC-SCN-05-04 · La recuperación vuelve al catch (LT-95).** *Entonces* con c = 0,999: piernas = 0 ± 1e-3, brazos 0, espalda 0, barrido = −55° ± 1e-3 rad; con c = 0,7 la pala está plumeada. [dotnet] · `stroke_cycle_test.dart:29`
- **AC-SCN-05-05 · La geometría del legacy queda como contrato (LT-96).** *Entonces* las funciones del legacy (carro `9,5 − 0,6·(1 − piernas)`; mango `(s·(0,85 − cos α·0,9); 0,45; 9,5 + sin α·0,9)`; punta `(s·(0,85 + cos α·2,9); −0,05 | 0,35; 9,5 − sin α·2,9)`) existen y cumplen: carro(0) < carro(0,36); mango(0).z < 9,5 < mango(0,36).z; punta(0).z > 11,5; punta(0,36).z < 9,5; lados espejados en x. Se usan solo para los tests y la lista de charcos del contrato; lo que se ve usa las medidas reales (AC-SCN-05-06). [dotnet] · `boat_painter.dart:35-56`; `boat_painter_test.dart:10`
- **AC-SCN-05-06 · Medidas reales (D-ART-04).** *Entonces* el bote visible mide: casco 8,20 m (popa en z 5,3, proa en 13,5, como en la 6a), manga 0,29 m en la flotación y 0,33 m en la borda; pernos a ± 0,80 m; eje del remo a 0,31 m sobre el agua; asiento a 0,14 m; pedalera y recorrido del carro los que da `RiggingFit` para el cuerpo del remero (≈ −0,17 m del perno y ≈ 0,54 m para 1,85 m; rieles de 0,72 m); remos de 2,88 m con 0,88 m de interior; pala 0,47 × 0,22 m. Remero (el maniquí) de 1,80 a 1,88 m; un personaje del slot vale con su alto dentro de 1,55–2,05 m (AC-SCN-08-08; Y Bot mide 1,786 m: *aclaración por verificación QA7-V-06*). Los 1,15 m y 0,6 m del legacy quedan solo en LT-96 (DV-77). *(Cambio por gate QA7-G-05: con remos reales la pedalera a 1,15 m deja la cadera detrás del puño; DT7-02.)* [dotnet] (las medidas) · [Captura] CAP-17 · ART_BIBLE §7.8, §7.9
- **AC-SCN-05-07 · Remo y pala, sin saltos.** *Entonces* el barrido visible es `−62° + 104°·(α_legacy + 55°)/90°` (catch −62°, finish +42°; *cambio por gate QA7-G-02: con 102° el finish daba +40°*); el centro de la pala está a −0,13 m en el drive (c 0,015–0,355), sale con `smooth` a +0,05 (c 0,355–0,395) y sube a +0,09 en la recuperación; se cuadra a +0,17 (c 0,80–0,93) y entra a −0,13 (c 0,97 → 1,015). El plumeo gira 0° → 90° en c 0,385–0,43 y 90° → 0° en c 0,84–0,95. "Pala en el agua" (centro ≤ 0) empieza en c ≈ 0,99 y termina en c ≈ 0,38 (± 0,01): coincide con el contrato. Ningún ángulo ni altura salta entre dos cuadros consecutivos a 60 fps más que la pendiente de su curva. [dotnet] · [Captura] VID-06 · ANIMATION_BIBLE §3.6
- **AC-SCN-05-08 · Poses del atleta.** *Entonces* en las 10 posiciones de ANIMATION_BIBLE §3.4 (catch c = 0 … antes del catch c = 0,96) el tronco, el carro, la rodilla, la cadera, el hombro y el codo están dentro de **± 8°** de la tabla (tronco +28° en el catch, −18° en el finish), los 60 valores sin excepciones; cabeza estable mirando hacia popa; muñecas planas ± 10°. Vale igual para el maniquí y para Y Bot, cada uno con su columna. *(Cambio por verificación QA7-V-01: la tabla es la hoja medida de A7-01, DV-78.)* [Editor-PM] · [Captura] CAP-17, CAP-18 · ANIMATION_BIBLE §3.4
- **AC-SCN-05-09 · Contactos.** *Entonces* en todo el ciclo las manos quedan sobre el puño (≤ 2 cm), la cadera sobre el asiento (≤ 2 cm) y los pies en la pedalera (≤ 2 cm). Con `|α| < 25°` los puños se cruzan: la mano **izquierda arriba**, separación vertical ≥ 4 cm, sin atravesarse. [dotnet] (la trayectoria del puño) · [Editor-PM] (los contactos medidos en el rig) · ANIMATION_BIBLE §3.5
- **AC-SCN-05-10 · Pausa, reposo y reanudar.** *Entonces* al pausar el remero se detiene con desaceleración en ≈ 0,25 s (τ 0,15 s), no de golpe; tras 1,5 s quieto (pausa o catch sin remar) respira (15/min, pecho ± 0,6 cm) y mira de a ratos (± 2° cada 4–7 s); al reanudar retoma desde donde estaba, sin salto. La respiración no cambia `p`. [dotnet] · [Captura] VID-04 · ANIMATION_BIBLE §3.9

### 3.3 F-SCN-06 — Cabeceo del bote

- **AC-SCN-06-01 · Cabeceo del legacy, con el remero (D-ANI-03).** *Entonces* el cabeceo es `0,02·sin(2π·φ) + 0,006·sin(0,8·t)` rad (+ = proa arriba; ± 1,15° por palada, máximo en φ = 0,25), con `t` el tiempo de escena. En pausa la parte de la palada queda fija y el balanceo lento sigue. **Remero, remos, carro y casco cabecean juntos** (el legacy inclina solo el casco: DV-69). [dotnet] · `scene_state.dart:85`; `boat_painter.dart:66`
- **AC-SCN-06-02 · Arfada, rolido y empuje (D-ANI-04).** *Entonces* se suman: arfada `0,006·sin(2π(φ − 0,08))` m; rolido `0,004·sin(0,9t + 1,3)·0,5` rad (lago, k = 0,5); *surge* con `A(spm) = clamp(0,075 − 0,001·(spm − 20); 0,05; 0,08) × (0,85 + 0,30·e)`, mínimo en el catch y máximo en el finish, **media exacta 0 en el ciclo**, desfase de posición 0 en el catch y en el finish (24 spm, split 2:00 con e = 0,5: ≈ 8,6 cm atrás en el drive y ≈ 14,5 cm adelante en la recuperación). La distancia de escena no cambia por el *surge* (AC-SCN-08-11). [dotnet] · ANIMATION_BIBLE §4.5, §4.6
- **AC-SCN-06-03 · La respiración de la cámara suma el bote.** *Entonces* a la respiración de la 6a (AC-SCN-02-04) se le suman: vertical + 25 % de la arfada (τ 0,2 s), adelante + 30 % del *surge* (τ 0,35 s), cabeceo + 15 % del cabeceo del casco (τ 0,5 s) con un **tope suave** para no pasar los ± 3 dp (en W1280, TH y L640 la respiración de la 6a ya casi los usa y el cabeceo entra casi en 0; *cambio por gate QA7-G-05, DT7-06*). El horizonte sigue dentro de **± 3 dp** de su línea en los siete tamaños y el remero nunca sale de la zona del bote. En pausa × 0,6; con reducir movimiento la cámara no se mueve (× 0), **pero el remero sigue remando** (es la lectura del ritmo). [dotnet] (desvíos en 10 min de escena) · [Captura] VID-01 · ANIMATION_BIBLE §5.2

### 3.4 F-SCN-07 — Charcos de pala

- **AC-SCN-07-01 · La lista de charcos del contrato (LT-91).** *Entonces* cuando `p` cruza 0,38 se crean 2 charcos, primero el izquierdo (x < 0) y después el derecho (x > 0), en `x = ±(0,85 + cos α·2,9)` ≈ ± 3,23 m y z de mundo = distancia + `9,5 − sin α·2,9` ≈ distancia + 7,84; envejecen con el tiempo de escena (también en pausa) y se borran después de 4 s. Al empezar (p = 0) la pala cuenta como dentro. 0,8 s a 24 spm → ninguno; 0,3 s más → 2 (x < 0, x > 0); 5 s con spm 0 → ninguno. [dotnet] · `scene_state.dart:67-83`; `scene_state_test.dart:69`
- **AC-SCN-07-02 · Los charcos que se ven (D-ANI-02).** *Entonces* los visibles nacen en la salida de la pala visible (c ≈ 0,38 del reloj visual, una vez por palada) **donde está esa pala** (± 0,1 m), con las reglas del legacy: 2 por palada, el izquierdo primero, 4 s de vida, radio 0,3 → 0,9 m, **fijos en el mundo** (la cámara los pasa), ninguno nuevo en pausa, también en la palada que se cierra sola a 12 spm; los que quedan detrás de la cámara (relZ ≤ 0,5 m) no se dibujan. A 24 spm, LT-91 sigue pasando igual. Se ven sobre el **agua degradada** de la 6a. [dotnet] · [Captura] CAP-04, VID-04 · ANIMATION_BIBLE §4.3
- **AC-SCN-07-03 · Cómo se ven [VISUAL].** *Entonces* cada charco es un par de remolinos separados 0,45 m a lo largo de la pala, que giran en sentidos opuestos (1,5 rad/s, decayendo), con borde de espuma; alfa `(1 − edad/4)^1,5 · (0,30 + 0,35·e)` con entrada de 0,15 s. Contrato mínimo del legacy: un anillo blanco que crece y se apaga en 4 s (`boat_painter.dart:100-116`). [Captura] CAP-04

### 3.5 F-SCN-08 — Bote, remero, remos, salpicadura y sombra

- **AC-SCN-08-01 · El bote completo.** *Entonces* el bote provisorio de la 6a se reemplaza por el single completo: casco `#F5F5F5` → `#B0BEC5`, línea de borda `#00B4D8`, cockpit `#263238`, riggers de ala, carro con rieles, pedalera con zapatillas y bola de proa; centrado en x = 0, de z 5,3 a 13,5. Todo el bote y el remero reciben la luz de la hora (de noche más oscuros y azulados, como el tinte del legacy `boat_painter.dart:58-61`). [Captura] CAP-01, CAP-10, CAP-11 · ART_BIBLE §7.8
- **AC-SCN-08-02 · El remero.** *Entonces* rema **de frente a la cámara** (mira a la popa), con mono `#1565C0`, franjas `#00B4D8` y panel `#0D3B6E`, medias blancas, piel y pelo. Sus proporciones son reales (el legacy exagera tronco y cabeza: DV-74). [Captura] CAP-01, CAP-02, CAP-20, CAP-21 · ART_BIBLE §7.9
- **AC-SCN-08-03 · Remos y palas.** *Entonces* caña oscura, puño gris y pala **`#00B4D8`** con punta blanca; la pala se ve vertical en el agua y plana (plumeada) en la recuperación, con giro continuo entre las dos (el legacy cambia de golpe en 0,38: DV-75). La caña se flexiona en el drive `(0,02 + 0,04·e)·sin(π·c/0,36)` m en la punta. [Captura] CAP-01, CAP-02, VID-06
- **AC-SCN-08-04 · Salpicadura del ataque.** *Entonces* en cada entrada de la pala hay salpicadura en las dos palas, visible al menos mientras c < 0,08, con **al menos 6 gotas por pala** que suben, caen y se apagan (contrato del legacy, `boat_painter.dart:244, 252-264`); en la app, `8 + 14·e` gotas por pala, altura máx. `0,12 + 0,18·e` m, hacia proa. Nunca en pausa. [Captura] CAP-03, VID-02 · ANIMATION_BIBLE §4.1
- **AC-SCN-08-05 · Gotas en la recuperación.** *Entonces* desde la salida de la pala hasta c = 0,55 caen gotas de cada pala (tasa `50·(1 − u)`/s, u = avance del tramo). [Captura] VID-06 · ANIMATION_BIBLE §4.2
- **AC-SCN-08-06 · Sombra según el astro.** *Entonces* el bote tiene sombra del lado **opuesto al sol o a la luna**, con opacidad que sigue al ambiente. En el nivel **Bajo**, la del legacy: óvalo negro de opacidad `0,08 × ambiente`, de z 5,8 a 12,5, 0,5 m de ancho, desplazado 0,35 m; a la derecha si el astro está en la mitad izquierda de la pantalla y a la izquierda si no (también con el astro centrado). En **Medio y Alto**, sombra real del bote y del remero con la luz de la escena (DV-72). Cambia con la hora. [Editor-PM] (el lado, por hora) · [Captura] CAP-05 a CAP-09 · `boat_painter.dart:118-130`
- **AC-SCN-08-07 · Todas las fases, en vertical y en horizontal (LT-97, LT-98).** *Dado* V390 y H844, la paleta y la luz de las 12 y 3 charcos (x −3,0 / z 6,3 / edad 1; x 3,0 / z 6,3 / edad 3,5; x 3,0 / z 0,2 / edad 2, ya detrás de la cámara), *entonces* en las fases 0; 0,05; 0,2; 0,36; 0,39; 0,5; 0,8; 0,99 la escena se dibuja **sin excepciones ni avisos** en la consola, con el maniquí y con Y Bot (si está). [Editor-PM] · `boat_painter_test.dart:25`
- **AC-SCN-08-08 · El remero entra por su slot (`rower.character`).** *Entonces* por defecto el remero es el **maniquí procedural** (mismo esqueleto `mixamorig:*` y proporciones que Y Bot). Si `Assets/ThirdParty/Mixamo/YBot/YBot.fbx` (o `RowerRealistic/RowerRealistic.fbx`) está asignado y valida (Humanoid, huesos de dedos, alto 1,55–2,05 m, ≤ 60 000 triángulos; *cambio por gate QA7-G-01: Y Bot tiene 55 320*), el remero es ese personaje, con **la misma palada** (misma pose e IK, AC-SCN-05-08 y -09). Si falta o no valida, vuelve al maniquí y lo avisa **una vez** en el registro; la sesión sigue. **Ninguna animación de Mixamo** se usa para remar. [Editor-PM] (sin el FBX, con el FBX, con uno inválido de prueba) · [Captura] CAP-20, CAP-21 · ASSET_SLOTS §2
- **AC-SCN-08-09 · Nada de terceros en el repositorio.** *Entonces* `Assets/ThirdParty/` está ignorado salvo los `.meta`, y de `Assets/ThirdParty/Mixamo/` no se versiona **nada**, ni los `.meta`; ningún FBX, textura o material de Mixamo se versiona; el slot versionado queda igual en todas las máquinas (*cambio por gate QA7-G-04: un `.meta` sin su FBX queda huérfano en el CI y en otros clones*); el CI (`ci-core`) y el build compilan y pasan las pruebas **sin** la carpeta de Mixamo. [QA] (`git ls-files`, build limpio sin la carpeta)
- **AC-SCN-08-10 · La boya de 500 m (P-06 de la 6a).** *Dado* un paso de trabajo de una rutina (donde hay parciales, AC-WKT-07-01), *entonces* una **boya naranja** flota en el agua a **x = +4,5 m** (fuera del alcance de la pala) y **pasa a la altura del remero** (z relativa 9,5 m) en el momento en que el panel `Parciales 500 m` cierra la vuelta, **± 1 s** con `R-4` en régimen. Se ve llegar desde lejos, se mueve sin saltos (cualquier corrección se reparte en ≥ 1 s), después de pasar queda fija en el mundo y sale por detrás de la cámara. En libre, calentamiento, descanso y enfriamiento no hay boya. Una vuelta que cierra justo al terminar el paso también tiene su boya. No suma elementos al HUD ni texto. [dotnet] (el momento de paso, `F-5` con `R-4`: vuelta 1 de la rep 1 a los 155 s) · [Captura] CAP-15, CAP-16, VID-07 · DV-73
- **AC-SCN-08-11 · Nada cambia los datos.** *Entonces* el remero, el *surge*, el cabeceo, los charcos, la salpicadura y la boya no escriben en el motor, la sesión ni la telemetría; la distancia de escena sigue siendo la de F-SCN-03 (`500/split`, τ 1,5 s), y los metros, el tiempo, las vueltas y lo que se guarda son idénticos con y sin la escena (misma sesión de `R-4` byte a byte en la base). [dotnet] · [Editor-PM]
- **AC-SCN-08-12 · El esfuerzo se nota.** *Entonces* con más vatios hay más salpicadura, charcos más marcados, más flexión de caña, *surge* algo mayor y el tronco 2° más atrás en el finish (`−18° − 2°·e`), según `e` (ANIMATION_BIBLE §3.8). Con `Suave` (e = 0,167) y `Fuerte` (e = 0,917) la diferencia se ve. [dotnet] (las fórmulas) · [Captura] VID-01 contra VID-02

### 3.6 También toca

- **AC-WKT-17-14 · La zona del bote, con el remero.** *Entonces* en las siete medidas el bote, el remero y los remos hasta los pernos quedan dentro de la zona del bote en todas las fases (las palas pueden salir por los costados); la zona sigue centrada donde la cámara proyecta el remero (AC-WKT-17-09). En **L640 con el simulador**, la llave no tapa el cuerpo del remero (cabeza, tronco y manos) en ninguna fase; si lo tapa, la llave se corre fuera de la columna del centro (pendiente de la 4a). [Captura] CAP-10, CAP-11, CAP-14
- **AC-WKT-18-23 · `Series anteriores` en vertical, con el remero a la vista (P-03 de la 5).** *Dado* TV y P360 con los dos paneles abiertos, *entonces* los paneles siguen solo en la franja de afuera de la zona (AC-WKT-18-14) y **no tapan el cuerpo del remero** en ninguna fase; los remos y las palas pueden pasar por detrás del vidrio. Se mantiene DV-50 (ancho 137). [Captura] CAP-12, CAP-13
- **AC-SCN-01-16 · Rendimiento con el remero (`[PERF]`).** *Dado* el build de simulador en la PC (W1280), *entonces* con el remero (maniquí y, si está, Y Bot), libre con `Fuerte` y `Series 600` con los dos paneles abiertos: p99 del cuadro ≤ **16,7 ms**, ningún cuadro > **33 ms**, y **0 bytes** asignados por cuadro en régimen (medido con el helper de cuadros reales `FrameAllocations`, con su control positivo, y con el Profiler del build de desarrollo). El HUD sigue en ≤ 57 de 60 elementos (el remero no suma ninguno). En el Android de gama media, 60 fps durante 30 min (AC-SCN-01-09): **NO VERIFICABLE** sin el teléfono (regla 9). [Device-Win] · [Editor-PM]

---

## 4. Criterios anteriores que cambian o se vuelven a verificar

| ID | Qué pasa |
|---|---|
| **AC-SCN-01-08** (cambia) | El bote provisorio quieto se reemplaza por el bote completo de AC-SCN-08-01; se mantiene la posición (popa 5,3, proa 13,5) y "dentro de la zona del bote" |
| **AC-SCN-02-04** (cambia) | Suma los términos del bote (AC-SCN-06-03); el límite de ± 3 dp no cambia |
| AC-SCN-03-01 a -07, AC-SCN-01-09, AC-SCN-01-10, AC-WKT-13-15, AC-WKT-17-09, AC-WKT-18-14 | Se vuelven a verificar con el remero. Si fallan, se reabren acá |
| LT-62 a LT-87 | Siguen pasando |

---

## 5. Casos de test legacy

| LT | Test legacy | Qué afirma | AC |
|---|---|---|---|
| LT-88 | `scene_state_test.dart:37` | fase a spm/60: 0,4 a 1 s; 0,0 o 1,0 a 2,5 s | AC-SCN-04-01 |
| LT-89 | `scene_state_test.dart:46` | sin paladas termina en el catch: 0,2 → 0,0; velocidad < 0,1 | AC-SCN-04-02 |
| LT-90 | `scene_state_test.dart:60` | en pausa no avanza la fase; velocidad < 0,5 | AC-SCN-04-03 |
| LT-91 | `scene_state_test.dart:69` | 2 charcos al cruzar 0,38 (x < 0 primero), se van a los 4 s | AC-SCN-07-01, AC-SCN-07-02 |
| LT-92 | `stroke_cycle_test.dart:5` | catch | AC-SCN-05-01 |
| LT-93 | `stroke_cycle_test.dart:14` | finish | AC-SCN-05-02 |
| LT-94 | `stroke_cycle_test.dart:23` | piernas > espalda > brazos en 0,12 | AC-SCN-05-03 |
| LT-95 | `stroke_cycle_test.dart:29` | la recuperación vuelve al catch; plumeada en 0,7 | AC-SCN-05-04 |
| LT-96 | `boat_painter_test.dart:10` | geometría: carro, mango, punta, espejo | AC-SCN-05-05 |
| LT-97 | `boat_painter_test.dart:25` (390 × 844) | todas las fases sin excepciones | AC-SCN-08-07 |
| LT-98 | `boat_painter_test.dart:25` (844 × 390) | ídem en horizontal | AC-SCN-08-07 |

Los tests de `scene_state_test` de la 6a (`:14`, `:26`, `:55`) siguen como LT de la 6a. Todos se portan **sin cambiar números**.

---

## 6. Capturas y videos

**Reglas:** las de la 6a §7.1 (español, simulador sin ruido, reloj y fecha de prueba, carpeta de datos temporal, `R-4`, dos corridas iguales, `manifest.json` e `INDEX.md` sin rutas de la PC), en `docs/migration/iterations/07-remero-palada/snapshots/`. Ninguna toma toca `prefs.json` ni las bases del usuario ni deja cambiados los ProjectSettings. Personaje: **el maniquí**, salvo que diga Y Bot; las de Y Bot quedan NO VERIFICABLE si el FBX no está. Nivel de calidad: el que elige la app, salvo que diga otro. "Escena sola" = sin HUD, respiración en 0, tiempo de escena 3,2 s.

| ID | Tamaño | Estado | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | TV, escena sola | 12:00, fase forzada c = 0,18 | **Drive**: piernas extendidas, tronco abriéndose (+12°), brazos estirados, palas enterradas y verticales |
| **CAP-02** | TV, escena sola | 12:00, c = 0,60 | **Recuperación**: manos afuera, tronco casi vertical, palas planas sobre el agua (plumeadas) |
| **CAP-03** | TV, escena sola | 12:00, `R-4`, 0,1 s después de la entrada de la pala | **Ataque**: tronco adelante, piernas plegadas, salpicadura en las dos palas |
| **CAP-04** | TV, escena sola | 12:00, `R-4` a los 60,2 s, 1 s después de una salida de pala | **Charcos**: un par de remolinos por lado (con `R-4` el par anterior ya pasó la cámara); el `manifest` anota los charcos vivos. *Cambio por gate QA7-G-05 (DT7-10, DA7-15)* |
| **CAP-05** | TH, escena sola | 09:00, c = 0,5 | Sombra del lado opuesto al sol |
| **CAP-06** | TH, escena sola | 12:00 | Sombra corta bajo el casco |
| **CAP-07** | TH, escena sola | 17:00 | Sombra del otro lado que en CAP-05 |
| **CAP-08** | TH, escena sola | 22:00 | Sombra tenue de la luna; el bote y el remero oscuros y azulados |
| **CAP-09** | TH, escena sola, nivel Bajo | 09:00 | El óvalo del legacy (0,35 m al lado opuesto, opacidad 0,08 × ambiente) |
| **CAP-10** | TV | Libre a los 60,2 s, con el contorno de la zona | El HUD sobre el bote con el remero remando; todo dentro de la zona (vertical) |
| **CAP-11** | TH | CAP-10 en horizontal | Ídem (zona en 0,652) |
| **CAP-12** | TV | `Series 600` a los 250,2 s, los dos paneles abiertos, c = 0, con el contorno | `Series anteriores` no tapa el cuerpo del remero en el catch (brazos estirados) |
| **CAP-13** | TV | CAP-12 con c = 0,36 | Ídem en el finish |
| **CAP-14** | L640 | Libre a los 60,2 s con la llave, con el contorno | La llave no tapa el cuerpo del remero |
| **CAP-15** | TV | `Series 600`, `R-4`, a los 150,0 s | La boya naranja a la derecha, adelante, acercándose |
| **CAP-16** | TV | Ídem a los 155,0 s | La boya a la altura del remero; el panel recién cerró `#1` |
| **CAP-17** | Cámara lateral de prueba | Hoja con las 10 poses de ANIMATION_BIBLE §3.4, maniquí | Cada pose dentro de ± 8°; contactos de manos, cadera y pies; medidas reales (con regla de 1 m) |
| **CAP-18** | Cámara lateral de prueba | CAP-17 con Y Bot | Ídem con Y Bot |
| **CAP-19** | V390, escena sola | c = 0 y c = 0,36 (dos tomas) + **LEG-01** y **LEG-02** (el `BoatPainter` del legacy dibujado por el oráculo, si puede) | La misma composición que el legacy: el remero en el mismo lugar de la pantalla, de frente; el bote más fino y los remos más cortos (DV-68) |
| **CAP-20** | TV, escena sola | 12:00, c = 0,24, personaje `maniquí` | El maniquí procedural con el mono pintado |
| **CAP-21** | TV, escena sola | CAP-20 con `Y Bot` | Y Bot con la misma pose |

| ID | Tamaño y duración | Qué se hace | Qué tiene que verse |
|---|---|---|---|
| **VID-01** | TV, 12 s | Libre con `Suave` (20 spm) en régimen | Palada lenta: drive ≈ 1,0 s y recuperación ≈ 2,0 s (medidos en el video ± 0,05 s); charcos en pares; la cámara respira con el bote |
| **VID-02** | TV, 12 s | Libre con `Fuerte` (30 spm) en régimen | Drive ≈ 0,82 s, recuperación ≈ 1,18 s; más salpicadura que en VID-01 |
| **VID-03** | TV, 8 s | `Suave` → `Fuerte` a los 3 s | El ritmo cambia sin saltos; el cambio de ratio entra en un catch |
| **VID-04** | TH, 12 s | `R-4`: `Pausa` a los 3 s, `Reanudar` a los 8 s | El remero frena en ≈ 0,25 s; el bote desliza; los charcos siguen apagándose; respira a partir de los ≈ 4,8 s; retoma sin salto |
| **VID-05** | TV, 10 s | `R-4` y con la llave, SPM a 0 a los 2 s | La palada en curso termina despacio (12 spm) y queda en el catch; después, respira |
| **VID-06** | Cámara lateral de prueba, 6 s | `R-4` | Piernas → tronco → brazos y al revés; plumeo continuo; manos cruzadas con la izquierda arriba; gotas en la recuperación |
| **VID-07** | TV, 15 s | `Series 600` con `R-4`, de los 145 a los 160 s | La boya llega y pasa junto al remero cuando el panel cierra `#1` (155 s ± 1 s) |

**Demo** (`DEMO-01` a `DEMO-06`, W1280): los pasos 1 a 6 de §1, una toma por paso.

---

## 7. Qué se verifica dónde

| Dónde | Qué |
|---|---|
| [dotnet] | LT-88 a LT-96; fase visual, ritmo por SPM, curvas de remo y pala, trayectoria del puño, cabeceo, arfada, rolido, *surge* (media 0), charcos visuales, esfuerzo, momento de paso de la boya, que los datos no cambian |
| [Editor-PM] | LT-97, LT-98; slot del remero (sin FBX, con Y Bot, con uno inválido); contactos del rig; poses ± 8°; lado de la sombra por hora; la sesión con y sin escena |
| [Captura] | §6 |
| [Device-Win] | `[PERF]` de AC-SCN-01-16 y la demo en `Builds/Windows/simulator`. Si el `RowMate.exe` del usuario está abierto, se reintenta cada 2 min hasta 20; si no, NO VERIFICABLE |
| [Device-And] | 60 fps con remero: NO VERIFICABLE sin teléfono (regla 9) |
| [QA] | AC-SCN-08-09 (repositorio y CI sin Mixamo) |

---

## 8. Checklist de paridad

| # | Comportamiento legacy | Fuente | Test legacy | AC |
|---|---|---|---|---|
| 1 | Fase a spm/60, módulo 1 | `scene_state.dart:59-60` | `:37` | AC-SCN-04-01 |
| 2 | Cierre sin paladas a 12 spm, queda en 0 | `:21-23, 61-65` | `:46` | AC-SCN-04-02 |
| 3 | Pausa congela la fase | `:59-65` | `:60` | AC-SCN-04-03 |
| 4 | Ramps de piernas, espalda, brazos; inclinación +25° / −20° | `stroke_cycle.dart:15-34` | 4 casos | AC-SCN-05-01 a -04 |
| 5 | Barrido −55° / +35°, finish quieto 0,36–0,40 | `:9-12, 41-45` | `:5`, `:14`, `:29` | AC-SCN-05-01, -07 |
| 6 | Pala en el agua mientras p < 0,38 | `:36-37` | `:14` | AC-SCN-05-02, -07 |
| 7 | Geometría de carro, mango y punta | `boat_painter.dart:35-56` | `:10` | AC-SCN-05-05 |
| 8 | Cabeceo `0,02·sin(2πp) + 0,006·sin(0,8t)` | `scene_state.dart:85` | — | AC-SCN-06-01 |
| 9 | Charcos: 2 al cruzar 0,38, izquierdo primero, 4 s, fijos en el mundo, nada en pausa | `scene_state.dart:67-83` | `:69` | AC-SCN-07-01, -02 |
| 10 | Charco: anillo 0,3 → 0,9 m, alfa `(1 − t)·0,3`; no se dibuja detrás de la cámara | `boat_painter.dart:100-116` | `:25` | AC-SCN-07-02, -03 |
| 11 | Sombra 0,08 × ambiente, ± 0,35 m opuesta al astro | `:118-130` | — | AC-SCN-08-06 |
| 12 | Casco, colores, hueco, riggers | `:132-170, 227-230` | — | AC-SCN-08-01 |
| 13 | Remero de frente, traje `#1565C0`, piel, pelo | `:172-218` | — | AC-SCN-08-02 |
| 14 | Remos, pala `#00B4D8` vertical / plumeada | `:220-249` | — | AC-SCN-08-03 |
| 15 | Salpicadura: 6 gotas por pala mientras p < 0,08 | `:244, 252-264` | — | AC-SCN-08-04 |
| 16 | Tinte de ambiente del bote | `:58-61` | — | AC-SCN-08-01 |
| 17 | Todas las fases en 390 × 844 y 844 × 390 | — | `:25` (×2) | AC-SCN-08-07 |
| 18 | dt acotado a 0,1 s | `scene_state.dart:52` | — | AC-SCN-04-01 (ya en la 6a) |

---

## 9. Mejoras EXR (cómo se tiene que sentir)

Un atleta de verdad: técnica limpia (piernas, tronco, brazos), cabeza quieta, manos que no se despegan del puño, la pala que gira suave al salir y se cuadra antes de entrar. El ritmo se siente: a 20 spm la recuperación es larga y tranquila; a 30, enérgica. El bote vive: cabecea, "empuja" en el drive y se frena en el catch; el agua responde con salpicadura, gotas y remolinos que quedan atrás. La cámara acompaña sin marear. Todo es fluido a 60 fps y nada toca los números.

---

## 10. Decisiones aplicadas y diferencias visibles

Aplicadas con su valor por defecto (ROADMAP §6): **Q-66** (cámaras extra y personalización al backlog), **Q-70** (ritmo por SPM), **D-ART-04** (medidas reales), **D-ANI-01** (fase visual), **D-ANI-02** (charcos en la pala visible), **D-ANI-03** (remero y remos cabecean con el casco), **D-ANI-04** (arfada, rolido, flexión de caña, gotas, respiración en reposo).

| DV | Legacy | App | Si el usuario lo rechaza |
|---|---|---|---|
| DV-68 | Bote y remos exagerados (manga 0,56 m, remo 3,8 m) | Medidas reales (D-ART-04) | Escala del legacy en el bote visible |
| DV-69 | Solo el casco cabecea | Remero y remos cabecean con el casco (D-ANI-03) | Remero sin cabeceo |
| DV-70 | Drive = 40 % del ciclo siempre | Ritmo según SPM (Q-70) | `r = 0,40` fijo |
| DV-71 | Charcos en la punta de la geometría exagerada | En la pala visible (D-ANI-02); la lista del contrato no cambia | — (es consecuencia de DV-68) |
| DV-72 | Óvalo plano | Sombra real en Medio y Alto; el óvalo en Bajo | Óvalo en todos los niveles |
| DV-73 | Sin boya | Boya de 500 m en los trabajos (P-06) | Sin boya |
| DV-74 | Figura 2D con tronco y cabeza grandes; tronco +25° / −20° | Atleta 3D con proporciones reales; tronco visible +28° / −18° (ANIMATION_BIBLE §3.4; el +25° / −20° sigue en `StrokeCycle.TorsoLean`, QA7-G-10) | — |
| DV-75 | La pala cambia de vertical a plana de golpe en 0,38 | Plumeo continuo (c 0,385–0,43) | — |
| DV-76 | Sin arfada, rolido, *surge*, gotas ni respiración | Los suma (D-ANI-04), sin cambiar datos | Sin esos agregados |
| DV-77 | Pedalera a 1,15 m del perno y carro de 0,6 m | Pedalera y recorrido de `RiggingFit` (≈ −0,17 m y ≈ 0,54 m para 1,85 m); los del legacy solo en LT-96 (gate QA7-G-05, DT7-02) | — (con remos reales el legacy es imposible) |
| DV-78 | Sin ángulos articulares (figura 2D) | Tabla de 10 poses de ANIMATION_BIBLE §3.4 = hoja medida de A7-01 con las medidas reales y las manos en el puño del remo real (p. ej. rodilla 76° con medio carro, codo 128° con "manos afuera"); columna propia para Y Bot (verificación QA7-V-01) | — (la tabla anterior es imposible con D-ART-04) |

No hay claves nuevas del catálogo (la boya no tiene texto).

---

## 11. Restricciones de producto para el Architect

1. La fase del contrato (`p`) y los LT se portan sin cambiar un número; lo visual se apoya encima (D-ANI-01).
2. Ninguna animación de Mixamo; la palada es por código al ritmo del SPM. El maniquí procedural es el valor por defecto y la app funciona completa sin Mixamo.
3. `Assets/ThirdParty/` no se versiona (salvo `.meta`; de `Mixamo/`, nada: QA7-G-04); el CI compila sin él.
4. 0 bytes asignados por cuadro en régimen; el HUD no suma elementos (≤ 57 de 60).
5. La escena no escribe en la sesión, el motor ni la telemetría.
6. Pruebas y capturas con `ROWMATE_DATA_DIR` temporal; nunca `prefs.json` ni las bases del usuario; los ProjectSettings quedan como estaban.
7. Builds solo a `Builds/Windows/simulator`; nunca cerrar el `RowMate.exe` del usuario.
8. Repo público: ninguna ruta de la PC en lo que se versiona.

---

## 12. Fuera de alcance

- Agua avanzada, reflejo del bote y del remero, estela con *surge*, niebla avanzada, vidrio con desenfoque (6b, postergada).
- Rolido por escenario distinto del lago y mar de fondo de la costa (It. 8).
- Sincronía fina con el contador de paladas del monitor (D-ANI-06, It. 10a).
- Cámaras alternativas, director y personalización de colores (Q-66, BKL-02).
- X Bot, el personaje realista y el clip `Sitting Idle` (opcionales; si están, entran por el slot sin cambios).
- 60 fps en el Android de gama media (NO VERIFICABLE hasta tener el teléfono; It. 14).
- La deuda de los issues #1 a #10 (salvo que bloquee algo de esta spec).

---

## 13. Decisiones tomadas por el PO al especificar (sin preguntas abiertas)

- Sin Y Bot, la iteración se cierra con el maniquí; los criterios de Y Bot quedan NO VERIFICABLE y se verifican cuando el usuario lo baje.
- La boya: naranja, a x = +4,5 m, solo en trabajos de rutina, sin texto, paso ± 1 s en régimen (P-06).
- Con reducir movimiento el remero sigue remando; solo la cámara queda quieta.
- La sombra en Bajo sigue la regla del legacy (astro centrado → izquierda); en Medio y Alto, la luz real.
- Los paneles y la llave pueden dejar ver los remos por detrás del vidrio, nunca tapar el cuerpo del remero.
- Las poses se verifican con una cámara lateral solo de prueba (no es una cámara de la app).
- El hervidero bajo la pala en el drive y los microanillos de las gotas se hacen si el agua degradada lo permite sin costo; si no, van a la 6b.

---

## 14. Discrepancias encontradas

- `scene_state_test.dart:60` también afirma velocidad < 0,5: se porta entero en LT-90 (la parte de velocidad ya la cubría AC-SCN-03-03 de la 6a).
- El legacy no tiene boya de 500 m ni test del cabeceo ni de la sombra: AC-SCN-08-10, AC-SCN-06-01 y AC-SCN-08-06 salen del código (`scene_state.dart:85`, `boat_painter.dart:118-130`) y de P-06.
- Ninguna diferencia entre el inventario (F-SCN-04 a -08) y el código del legacy en los números usados acá.

---

## 15. Para issues

- X Bot para probar el ajuste del cuerpo con otras proporciones (ASSET_SLOTS §2, recomendado).
- Personaje realista (`Kaya` u otro de la lista de control) y gorra opcional (It. 14).
- Brillo de sudor que sube con el tiempo activo y LOD1 del remero (ART_BIBLE §7.9).
- Reflejo del remero y del bote en el agua avanzada, estela con *surge*, microanillos de gotas (6b).
- Rolido por escenario (`k` río 0,8, costa 2,0, regata 0,4) y mar de fondo (It. 8).
- Calibrar `φ_ref` con el AMS-670B (D-ANI-06, It. 10a).
- Número de proa opcional del bote y personalización de colores (backlog, Q-66).
- 60 fps con remero en el Android de gama media (It. 14).

---

## Al cerrar la iteración (PO)

QA: APROBADO CON OBSERVACIONES en la ronda 2 ([05-qa-verification](05-qa-verification.md)): **37 PASS · 0 FAIL · 1 NO VERIFICABLE** de 38. El PO **acepta** la It. 7.

- **QA7-V-01** (el único mayor): cerrado con DV-78 (la tabla de §3.4 es la hoja medida de A7-01, con columna para Y Bot).
- **QA7-V-06**: aclarado en AC-SCN-05-06 (el 1,80–1,88 m es del maniquí; el slot acepta 1,55–2,05 m).
- **AC-SCN-01-16** queda NO VERIFICABLE: CPU, asignaciones y HUD dan bien; los tiempos de cuadro del build se rehacen con la pantalla encendida en la próxima corrida de QA, y Android en la It. 14.
- **Antes del commit**, sin tocar la app: borrar `Assets/New Text Document.txt` y su `.meta`; volver a guardar en UTF-8 `RowerCatalog.cs` y `SceneBudgetTests.cs` (QA7-V-04); regenerar `snapshots/INDEX.md` entero desde el `manifest.json` (QA7-V-R2-01).
- **El resto** (QA7-V-02, -03, -05, -07, los "Para issues" de esta spec, del diseño, de las notas de diseño y de QA) va a issues de GitHub, agrupados. Ya resueltos y no van: QA7-G-06 a -10 (por gate), los `.meta` y `.asset` de los slots nuevos (B0), los shaders de la 7 en `ShaderArt` (D7-03) y la tabla de §3.4 (DV-78).
- Pendientes del PO: STATUS actualizado. La próxima es la **It. 8** (`08-entornos-selector`); la 6b sigue postergada (issue #6).
