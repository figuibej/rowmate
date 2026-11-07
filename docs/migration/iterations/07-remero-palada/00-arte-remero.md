# It. 7 — Base de arte y motion: remero, bote y palada

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-06 |
| Iteración | 7 · `07-remero-palada` ([ROADMAP §It. 7](../../ROADMAP.md)), sobre la escena de la 6a con el agua degradada (la 6b quedó postergada, issue #6) |
| Entrada | [01-po-spec](01-po-spec.md); [ANIMATION_BIBLE §3, §4, §5.2, §7](../../design/ANIMATION_BIBLE.md); [ART_BIBLE §7.8, §7.9, §9](../../design/ART_BIBLE.md); [ASSET_SLOTS §1.1, §1.5, §2](../../design/ASSET_SLOTS.md); [06a/04-design-notes §8.3, §9, §13, §15, §18](../06a-escena-lago/04-design-notes.md); las tomas de [06a/snapshots](../06a-escena-lago/snapshots/) (CAP-13: el casco solo en TV) |
| Legacy leído (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/scene/stroke_cycle.dart`, `scene_state.dart`, `painters/boat_painter.dart`, `docs/image-03.png` |
| Modo | Entrega rápida: cada duda se resolvió con el legacy o con el valor por defecto, en una línea (§0.2). Lo menor va a "Para issues" (§14) |

Este documento **no repite** las biblias: fija lo que faltaba (el maniquí, Y Bot, la geometría real del cockpit, la sombra, la boya, los tamaños mínimos en pantalla y el presupuesto) y resume con números lo que el Architect necesita tener a mano. Mundo como en la 6a: `x` lateral (+ derecha de la pantalla), `y` altura sobre el agua, `z` hacia adelante desde la cámara; pernos en `z` 9,5; el bote mira hacia `+z` (proa lejos) y el remero mira a la cámara (popa).

---

## 0. Resumen

### 0.1 Qué hay que lograr

1. Un **atleta** que se lea "remero de single" a 9,5 m: en TV mide ≈ 47 dp de alto y ≈ 17 dp de ancho de tronco (§1). La silueta, el color y el movimiento importan; el detalle fino, no.
2. **La misma palada** para el maniquí y para Y Bot: IK sobre `mixamorig:*` con los números de ANIMATION_BIBLE §3. Ningún clip de Mixamo.
3. **Medidas reales** (D-ART-04). La pedalera y el carro los da `RiggingFit`, porque con remos reales la pedalera del legacy (1,15 m) es imposible: DA7-01.
4. El agua responde: salpicadura, gotas, charcos y el "hervidero", todo sobre el agua degradada y con un tamaño mínimo en pantalla.
5. Con el remero, el bote y los efectos: **+12 draw calls, +12 k triángulos (con el maniquí) y +0,8 ms de GPU** sobre la 6a. Queda dentro del tope de Medio, con poco margen (§11).

### 0.2 Decisiones tomadas (una línea cada una)

- **D7-01** Pedalera y carro reales: los calcula `RiggingFit`. Para 1,85 m quedan los talones ≈ 0,17 m hacia popa del perno y la cadera de +0,18 (catch) a +0,72 m (finish) hacia proa, con ≈ 0,54 m de recorrido. Los 1,15 m y los 0,6 m del legacy quedan solo en `LegacyBoatGeometry` (LT-96).
- **D7-02** "Izquierdo" tiene dos sentidos. En los charcos es el lado `x < 0` (el `side −1` del legacy, a la izquierda de la pantalla). En las manos es la mano izquierda **del remero**, que está en `x > 0` porque el remero mira a la cámara.
- **D7-03** El maniquí usa proporciones antropométricas de 1,85 m (tabla §2.1), no las medidas de Y Bot: así no hace falta el FBX para fijarlas. Con Y Bot, `RiggingFit` mide sus huesos.
- **D7-04** Piel del maniquí `#C98E6E` (tono medio). El legacy usa `#FFCC80`, que en 3D con luz se ve como plástico.
- **D7-05** Anteojos de sol deportivos en el maniquí (una banda `#101418` brillante). Leen "atleta" y resuelven la cara sin rasgos, que mira a la cámara.
- **D7-06** Sombra: con el mapa de sombras activo (Medio y Alto de día, Alto de noche), el agua degradada lo recibe y no hay óvalo. Sin mapa (Bajo, o Medio de noche), el óvalo del legacy. Así nunca se dibujan dos sombras.
- **D7-07** Las cañas de los remos **no** proyectan sombra (3,5 cm contra un texel de 4 cm titilan). Las palas sí.
- **D7-08** Los micro-anillos de las gotas van a la 6b. El hervidero del drive entra, porque usa el mismo quad que los charcos (costo ≈ 0).
- **D7-09** Con reducir movimiento solo se detiene la cámara. El remero, el bote (*surge*, cabeceo, arfada y rolido), la respiración en reposo y el agua siguen, igual que en el PO §13.
- **D7-10** La boya sigue al PO: `x` +4,5 m y naranja `metric.split` `#FF9F1C`, el color del panel de parciales. En TV pasa por el borde derecho (≈ 400 dp de 411): queda en "Para issues".

---

## 1. Escala en pantalla (por qué el presupuesto es chico)

Con la focal igual al lado corto, 1 m a la distancia `z` mide `f / z` dp.

| Qué | Distancia `z` | TV (f 411) | W1280 (f 720) |
|---|---|---|---|
| Pernos y remero | 9,5 m | 43,3 dp/m | 75,8 dp/m |
| Palas en el finish (charcos al nacer) | 8,3 m | 49,4 | 86,7 |
| Palas en el catch (salpicadura) | 11,05 m | 37,2 | 65,2 |

Lo que resulta en TV:

- **El remero:** ≈ 47 dp de alto (cabeza a 1,10 m del agua), tronco de 17 dp y cabeza de ≈ 10 dp.
- **La caña del remo:** 1,5 dp.
- **Una gota de 1 cm:** 0,4 dp. **Sin un mínimo en pantalla no se ve** (§8.4).
- **Las palas en el finish:** llegan a ±115 dp del centro y salen de la zona del bote (±90 dp). Pasa lo mismo en el legacy (`image-03`) y lo admite AC-WKT-17-14.

---

## 2. Remero procedural (`ProceduralRower`, el valor por defecto)

### 2.1 Proporciones (H = 1,85 m, sculler de ≈ 85 kg)

| Segmento | Largo (hueso) | Sección (ancho × fondo) | Huesos `mixamorig:` |
|---|---|---|---|
| Cadera: articulaciones de las piernas | ±0,090 m de la línea media | pelvis 0,35 × 0,22 | `Hips` |
| Tronco (de la cadera a los hombros) | 0,533 (Spine 0,11 · Spine1 0,13 · Spine2 0,15 · Neck 0,10 · el resto, la clavícula) | cintura 0,29 × 0,20 · pecho 0,34 × 0,24 | `Spine`, `Spine1`, `Spine2`, `Neck` |
| Hombros: articulaciones | ±0,185 | deltoide Ø 0,12 | `LeftShoulder` / `RightShoulder` |
| Brazo | 0,344 | Ø 0,10 → 0,075 en el codo | `Arm` |
| Antebrazo | 0,270 | Ø máx. 0,085 (a 1/3) → 0,055 en la muñeca | `ForeArm` |
| Mano (puño cerrado) | 0,200 (centro del agarre a 0,08 de la muñeca) | 0,10 × 0,09 × 0,05, con un hueco de Ø 0,036 para el puño del remo | `Hand` + dedos |
| Muslo | 0,453 | Ø 0,17 arriba → 0,11 sobre la rodilla | `UpLeg` |
| Pierna | 0,455 | rodilla 0,10 · pantorrilla Ø 0,115 a 1/3 · tobillo 0,07 | `Leg` |
| Pie (zapatilla) | 0,28 de largo, 0,10 de ancho | la zapatilla del bote, blanca | `Foot`, `ToeBase` |
| Cabeza | 0,24 de alto | ovoide 0,16 × 0,20, con nariz insinuada (0,02) para que se lea hacia dónde mira | `Head` |

Esqueleto completo de Mixamo (≈ 65 huesos, con dedos y `*_End`). Los dedos existen para el avatar Humanoid y para el clip de agarre; la malla de la mano es un puño rígido atado a `Hand`.

### 2.2 Malla

- **Un solo `SkinnedMeshRenderer`** con un material.
- **Segmentos:** *loft* de superelipse (exponente 2,5) con 12 lados, un anillo cada 3–4 cm y extremos redondeados.
- **Uniones sin quiebre:** los pesos pasan de un hueso al otro linealmente en ±0,04 m (±0,05 m en rodilla y codo); máximo 2 huesos por vértice.
- **Tope:** ≤ 6 k triángulos. Sin LOD: el remero siempre está a 9,5–12 m.

### 2.3 Materiales: un shader `RowMate/Rower` con regiones por color de vértice

El shader usa `RowMateLighting.hlsl` de la 6a (ambiente 0,80, clave 0,45) y el relleno de ART_BIBLE §7.9 (`0,12 · sunColor · saturate(N·V)`), porque el frente del remero casi siempre está a la sombra: el astro queda adelante.

| Región (R del color de vértice) | Color | Suavidad | Extra |
|---|---|---|---|
| 0 mono, base | `#1565C0` [contrato] | 0,35 | brillo de borde (*sheen*) `#FFFFFF @12 %` |
| 1 franjas | `#00B4D8` | 0,35 | 3 cm de ancho, a los costados del tronco y del muslo |
| 2 panel lateral | `#0D3B6E` | 0,35 | — |
| 3 piel | `#C98E6E` (D7-04) | 0,45 | luz envolvente 0,4, terminador teñido `#E89A80` |
| 4 medias y zapatillas | `#ECEFF1` | 0,30 | — |
| 5 anteojos | `#101418` | 0,90 | refleja el cielo con el Fresnel del casco (D7-05) |
| 6 gorra (`rower.cap`, encendida en el maniquí) | `#0D3B6E` | 0,30 | visera hacia la cámara |
| 7 articulaciones (solo Y Bot) | `#263238` | 0,40 | — |

El mono (*unisuit*) cubre el tronco y el muslo hasta 6 cm sobre la rodilla y no tiene mangas: los hombros llevan tiritas de 4 cm.

**Calidad mínima** (ASSET_SLOTS §8): se lee "atleta en un single" en TV; nada de esferas ni cápsulas sueltas a la vista.

---

## 3. Con Y Bot (slot `rower.character`)

### 3.1 Para qué sirve

Es un maniquí humano de Mixamo con un rig limpio y la piel bien pesada. Sirve para tres cosas:

- **Calibrar el IK y `RiggingFit` con un cuerpo real**, no con uno generado por nosotros. Si el IK cierra con Y Bot, va a cerrar con cualquier personaje Humanoid.
- **Fijar los números del agarre** (`RowerGripPose`, §4.6).
- **Mejorar la deformación** en hombros, codos y rodillas, que en el maniquí son uniones simples.

No trae animaciones y ninguna se usa para remar.

### 3.2 Cómo se ve

- Misma pose, mismo IK y mismos números que el maniquí.
- Sus dos submallas de Mixamo (el cuerpo gris claro y las articulaciones oscuras) se cambian por `RowMate/Rower`:
  - **el cuerpo:** las regiones de §2.3 salen del hueso dominante de cada vértice, horneadas una vez al cargar en una copia de la malla (DA7-05):
    - `Hips`, `Spine*`, `UpLeg` y `Shoulder` → mono;
    - `Arm`, `ForeArm`, `Hand`, `Leg`, `Neck` y `Head` → piel;
    - `Foot` y `ToeBase` → medias;
  - **las franjas:** salen de `|normal.x|` en la pose de unión > 0,85, sobre el mono;
  - **las articulaciones:** van a la región 7 (`#263238`). Se leen como costuras del mono.
- Sin gorra ni anteojos (Y Bot no tiene cara).
- Se ve como un maniquí de pruebas vestido de remero. El personaje realista (ASSET_SLOTS §2.3) queda para cuando el usuario lo baje.

### 3.3 Validación (ASSET_SLOTS §2)

El slot valida estas condiciones:

- Humanoid, con los dedos;
- alto de 1,55 a 2,05 m;
- ≤ 60 k triángulos (Y Bot tiene 55,3 k; QA7-G-01: con 30 k el slot rechazaba a Y Bot);
- ≤ 75 huesos.

Además, el import tiene que tener **Optimize Game Objects apagado**.

Si algo falla, vuelve al maniquí con un solo aviso en el registro. **No se escala**: `RiggingFit` adapta el bote al cuerpo.

---

## 4. La palada

### 4.1 Tiempos por SPM (Q-70; ANIMATION_BIBLE §3.2)

`T_d = clamp(1,05 − 0,0194·(spm − 18); 0,60; 1,15)`; `r = clamp(T_d·spm/60; 0,22; 0,48)`. Tiempos en segundos desde el catch:

| SPM | Ciclo | Drive | Piernas terminan (c 0,18) | Tronco abre (0,10→0,30) | Brazos (0,22→0,36) | Finish (0,40) | Manos afuera hasta (0,60) | Cuerpo adelante (0,50→0,75) | Carro (0,60→1) |
|---|---|---|---|---|---|---|---|---|---|
| 20 | 3,00 | 1,01 | 0,46 | 0,25→0,76 | 0,56→0,91 | 1,01 | 1,67 | 1,34→2,17 | 1,67→3,00 |
| 24 | 2,50 | 0,93 | 0,42 | 0,23→0,70 | 0,51→0,84 | 0,93 | 1,46 | 1,20→1,85 | 1,46→2,50 |
| 30 | 2,00 | 0,82 | 0,37 | 0,20→0,61 | 0,45→0,74 | 0,82 | 1,21 | 1,01→1,51 | 1,21→2,00 |
| 36 | 1,67 | 0,70 | 0,32 | 0,18→0,53 | 0,39→0,63 | 0,70 | 1,02 | 0,86→1,27 | 1,02→1,67 |

- **Las fases:** catch (c 0,97 → 0,015) · drive con piernas (0 → 0,18) · drive con tronco (0,10 → 0,30) · drive con brazos (0,22 → 0,36) · finish (0,36 → 0,40) · recuperación con brazos, tronco y carro, en ese orden (0,40 → 1).
- **Los *ramps*:** son los del legacy sobre `c` (PO §3.2).
- **La fase visual `φ`:** seguidor con `K` = 4 s⁻¹. `r` se fija en el catch.

### 4.2 Ángulos clave (± 8°; la tabla completa de 10 poses en ANIMATION_BIBLE §3.4)

| Pose | c | Tronco | Rodilla | Cadera | Hombro | Codo | Barrido del remo | Plumeo |
|---|---|---|---|---|---|---|---|---|
| Catch | 0,000 | +28° | 50° | 32° | +80° | 178° | −62° | 0° |
| Apertura | 0,180 | +12° | 170° | 85° | +70° | 176° | −11° | 0° |
| Finish | 0,360 | −18° − 2°·e | 178° | 112° | −25° | 85° | +42° | 0° |
| Manos afuera | 0,600 | −2° | 178° | 95° | +72° | 178° | +14° | 90° |
| Antes del catch | 0,960 | +28° | 52° | 32° | +80° | 178° | −61° | 2° |

**Detalles que no se negocian** (ANIMATION_BIBLE §3.4):

- la pelvis hace el 60 % del giro del tronco y la columna el resto (15/13/12 %), con la espalda recta;
- la cabeza mira a un punto 30 m hacia popa, a la altura de los ojos, con peso 0,85;
- canillas verticales en el catch (± 8°);
- codos afuera 35–45° en el finish;
- muñecas planas (± 10°).

### 4.3 Geometría resultante con remos reales (para `RiggingFit`; DA7-01)

| Momento | Puño (`z` desde el perno, `y` sobre el agua) | Cadera (`z`) | Talones (`z`) | Control |
|---|---|---|---|---|
| Catch | −0,72 · 0,375–0,49 | ≈ +0,18 | ≈ −0,17 | brazos estirados ≥ 97 %; los brazos pasan ≥ 3 cm sobre las rodillas (rodilla a 0,52 m) |
| Finish | +0,55 · 0,515 | ≈ +0,72 | ≈ −0,17 | puño a la altura de las costillas bajas (± 3 cm); antebrazos horizontales |

Recorrido de la cadera ≈ 0,54 m (en un rig de 1,80 a 1,88 m, entre 0,52 y 0,62); rieles de 0,72 m. Con la pedalera a 1,15 m del legacy la cadera quedaría en −0,80 en el catch, más atrás que el puño (−0,72): el remero no llegaría al remo.

### 4.4 Manos sobre el mango (IK)

- **`GripTarget`:** es hijo del puño del remo. Lleva la posición y el 25 % del plumeo, así que la mano gira con el puño sin quebrar la muñeca.
- **Hint del codo:** afuera 0,35, abajo 0,25 y hacia proa 0,25 m.
- **Los remos no siguen a las manos: las manos siguen a los remos.**
- **Cruce** cuando `|α| < 25°`: la mano izquierda del remero (`x > 0`, D7-02) va 2,5 cm arriba y 1,5 cm hacia popa; la derecha, 2,5 cm abajo. El peso es `w = 1 − smooth((|α| − 20°)/10°)` y la separación vertical mínima, 4 cm.
- **Contactos** (AC-SCN-05-09): ≤ 2 cm en manos, cadera y pies.

### 4.5 Giro de pala continuo (ANIMATION_BIBLE §3.6)

| Tramo (c) | Plumeo | Centro de pala (`y`) | A 20 / 30 spm |
|---|---|---|---|
| 0,015–0,355 | 0° (cuadrada) | −0,13 (enterrada) | — |
| 0,355–0,395 | 0° | −0,13 → +0,05 (`smooth`) | 101 / 82 ms |
| 0,385–0,43 | 0° → 90° (`smooth`, cara cóncava arriba) | +0,05 → +0,09 | 137 / 90 ms |
| 0,43–0,84 | 90°, con una ondulación de ±0,01 m | +0,09; sube a +0,17 en 0,80–0,93 | — |
| 0,84–0,95 | 90° → 0° (cuadra cuando las manos pasan los tobillos) | +0,09 → +0,17 | 365 / 217 ms |
| 0,97 → 1,015 | 0° | +0,17 → −0,13 (entrada) | 137 / 90 ms |

Cada tramo es `smooth`, con velocidad 0 en los extremos (C1, sin saltos). La caña se flexiona en el drive `(0,02 + 0,04·e)·sin(π·c/0,36)` m en la punta (vertex shader, uniform `_RmOarLoad`).

### 4.6 Agarre (`RowerGripPose.anim`, un cuadro, músculos Humanoid)

| Dedo | 1 Stretched | 2 Stretched | 3 Stretched | Spread |
|---|---|---|---|---|
| Índice a meñique | −0,85 | −0,90 | −0,80 | 0 |
| Pulgar | −0,20 | −0,60 | −0,60 | 0,40 |

En el maniquí el puño es rígido y los dedos no se ven: el clip igual se aplica, por el avatar.

### 4.7 Pausa, reposo y cierre sin remar (ANIMATION_BIBLE §3.9)

- **Pausa:** `ω_s` cae con τ 0,15 s y el remero se detiene en ≈ 0,25 s.
- **Reposo:** después de 1,5 s quieto, respira 15 veces por minuto (pecho ±0,6 cm, hombros ±0,5 cm) y mira de a ratos (±2° cada 4–7 s).
- **spm = 0:** termina el ciclo a 12 spm (r 0,23) y queda en el catch.

---

## 5. Remos y single (medidas reales, D-ART-04)

### 5.1 Remo de *sculling* (par; slot `boat.oar`)

| Parte | Medida | Material |
|---|---|---|
| Largo total | 2,88 m; interior 0,88 (del extremo del puño al collarín) | — |
| Puño | 0,12 m de largo, Ø 0,036 | goma `#5F6B73`, suavidad 0,25 |
| Caña interior | Ø 0,032 | carbono `#1B1F24` satinado (suavidad 0,6), con una normal de tejido sutil |
| Manguito y collarín | manguito de 0,24 m, Ø 0,046 · collarín Ø 0,070 × 0,025 a 0,88 m | blanco `#ECEFF1` |
| Caña exterior | Ø 0,038 → 0,030 en el cuello de la pala | carbono, como la interior |
| Pala tipo "hacha" | 0,47 × 0,22 m, concavidad de 2,5 cm, borde superior recto y panza abajo; centro a 1,76 m del perno | cara y dorso `#00B4D8` [contrato]; punta blanca de 3 cm |
| Malla | ≤ 1,2 k triángulos por remo | 1 material para los dos (SRP Batcher) |

**Ancho mínimo en pantalla de la caña: 1,2 dp.** El vertex shader extruye en espacio de pantalla, como el `max(1, w·scale)` del legacy (DA7-07).

### 5.2 Single (slots `boat.hull`, `boat.rigger`, `boat.seat`, `boat.stretcher`)

| Pieza | Medida |
|---|---|
| Casco | el de la 6a sin cambios: 8,20 m (popa en `z` 5,3, proa en 13,5), manga 0,29 m en la flotación y 0,33 en la borda, francobordo 0,10 m, bola de proa, gelcoat |
| Cockpit | **se agranda y se corre**: de `z` 9,05 a 10,45 (−0,45 / +0,95 del perno; DA7-02); `#263238`, con los rieles `#9AA5AD` a ±0,09 m |
| Riggers | ala de carbono **por lado**: de la borda (`z` 9,30–9,70) al perno en `x` ±0,80, `y` 0,31; un tirante a proa; chumacera blanca de 0,06 m. No cruza el cockpit |
| Carro | asiento negro de 0,24 × 0,20 m con la cara de arriba a 0,14 m sobre el agua; las ruedas no se modelan |
| Pedalera | placa a 42° con zapatillas blancas `#ECEFF1`; talón a 0,17 m bajo el asiento. Posición de `RiggingFit` (≈ `z` 9,33) |
| Malla y materiales | riggers, carro y pedalera ≤ 2 k triángulos en un solo `MeshRenderer` (colores por vértice, shader `Hull`). Todo es hijo de `BoatPitchPivot` |

---

## 6. El bote se mueve (todo hijo de `BoatPitchPivot`, D-ANI-03)

| Movimiento | Fórmula | Pivote y amplitud |
|---|---|---|
| Cabeceo [contrato] | `0,02·sin(2π·φ) + 0,006·sin(0,8·t)` rad (+ = proa arriba) | eje `x` por el perno (`z` 9,5, `y` 0); ±1,49° en total; la proa sube ±10 cm |
| Arfada | `0,006·sin(2π(φ − 0,08))` m | ±6 mm |
| Rolido | `0,004·sin(0,9t + 1,3)·0,5` rad (lago) | ±0,11° |
| *Surge* | ANIMATION_BIBLE §4.5; `A(spm)·(0,85 + 0,30·e)` | a 24 spm: −8,6 cm en el drive y +14,5 cm en la recuperación; desfase 0 en el catch y en el finish |
| Parte de la cámara | arfada 25 % (τ 0,2 s), *surge* 30 % (τ 0,35 s), cabeceo 15 % (τ 0,5 s) | el horizonte, dentro de ±3 dp |

El remero, los remos, el carro y la sombra con mapa cabecean con el casco. El óvalo, los charcos y la boya **no** (son del agua).

---

## 7. Sombra según el astro (D7-06)

| Caso | Qué se dibuja |
|---|---|
| Con mapa de sombras (Alto; Medio de día) | el casco, el remero y las palas proyectan sobre el agua degradada, que **recibe** la sombra de la luz principal (DA7-04): oscurece `0,10·ambiente` en sombra plena y la penumbra es la del filtro suave de URP. El casco recibe la sombra del remero. Con el sol bajo (luz a 6°), la sombra del remero se estira ≈ 10 m hacia la cámara: se ve y es correcto |
| Sin mapa (Bajo; Medio de noche) | **el óvalo del legacy** (PO AC-SCN-08-06): de `z` 5,8 a 12,5, 0,5 m de ancho, borde suave `exp(−r²·4)`, alfa `0,08·ambiente`, corrido 0,35 m al lado opuesto del astro en pantalla (con el astro centrado, a la izquierda) |
| Cambio de lado del óvalo | cuando el astro cruza el centro de la pantalla, el corrimiento pasa de +0,35 a −0,35 en **1,5 s** `ease.move` (la duración de un cambio de hora); en una toma con hora fija, en el acto |

---

## 8. El agua responde (sobre el agua degradada de la 6a)

### 8.1 Salpicadura del catch (`fx.splash`; evento `BladeEntry`, c ≈ 0,99)

- **Gotas:** `8 + 14·e` por pala, mínimo 6 [contrato]. Velocidad hacia arriba `1,4 + 1,0·e` m/s, hacia proa 0,6 m/s y hacia afuera 0,3 m/s, en un cono de 35°. Tamaño 0,02–0,05 m, gravedad 9,8, vida 0,35–0,6 s y alfa de 0,8 a 0.
- **Corona:** un quad de 0,25 × `(0,12 + 0,18·e)` m que crece y se apaga en 0,18 s.
- **Color:** blanco × la luz de la hora (`ambient`); de noche apenas se ve, como el legacy.
- **Cuándo se ve:** como mínimo mientras c < 0,08. Nunca en pausa.

### 8.2 Gotas en la recuperación (`fx.drips`; de `BladeExit` a c 0,55)

- **Tasa:** `50·(1 − u)` gotas/s por pala, desde el borde inferior y la punta. Son ≈ 12 por pala y por palada a 20 spm.
- **Forma:** de 0,008–0,015 m, como trazos estirados por la velocidad (largo 0,06 m).

### 8.3 Charcos y hervidero (`fx.ripple`; D-ANI-02)

- **Cuándo nacen:** en `BladeExit` (c ≈ 0,38), dos por palada: primero `x < 0` (D7-02). Nacen en el centro de la pala visible, `x` ±2,11 m y `z` relativa −1,18 m (a 24 spm), y quedan **fijos en el mundo**.
- **Vida y forma:** viven 4 s; el radio pasa de 0,3 a 0,9 m; alfa `(1 − t/4)^1,5 · (0,30 + 0,35·e)`, con una entrada de 0,15 s.
- **Cómo se ven:** un par de remolinos separados 0,45 m a lo largo de la pala, que giran en sentidos opuestos (1,5 rad/s, decayendo), con un anillo de espuma en el borde.
- **Pausa y cámara:** en pausa no nacen, pero los que ya están siguen envejeciendo. Detrás de la cámara (relZ ≤ 0,5 m) no se dibujan.
- **Hervidero (D7-08):** durante el drive (c 0,02–0,36), una mancha de espuma de 0,4 × 0,25 m bajo cada pala, que la acompaña. Alfa `0,25 + 0,35·e`.
- **Cuántos hay:** a 36 spm, 3 paladas a la vez (6 charcos); a 60 spm, 5 (10). Se usa un *pool* de **12** quads instanciados, con un *draw call*. Ese mismo *draw call* incluye los 2 hervideros y el anillo de la boya (§9).
- **Shader:** procedural, ≤ 20 ALU y 0 texturas. Va en la cola transparente después del agua y no escribe profundidad.

### 8.4 Tamaño mínimo en pantalla (para que se vea a 1,5 m)

| Efecto | Mínimo | Por qué |
|---|---|---|
| Gota de la salpicadura | 1,5 dp | a 11 m, una de 3 cm mide 1,1 dp |
| Gota de la recuperación (trazo) | 1,0 dp de ancho | si no, a 8 m mide 0,4 dp |
| Caña del remo | 1,2 dp | §5.1 |

Se aplican en el vertex shader con el tamaño de píxel de la cámara (DA7-07). El alfa se escala con `tamaño_real / tamaño_dibujado` (como mucho × 0,5 de atenuación), así lo que se agranda no se ve más intenso.

---

## 9. La boya de 500 m (P-06; PO AC-SCN-08-10)

- **Forma:**
  - esfera de Ø 0,50 m que asoma 0,35 m sobre el agua;
  - `#FF9F1C` (`metric.split`) con una franja blanca de 0,06 m arriba del ecuador;
  - suavidad 0,7, con `RowMateLighting`;
  - ≤ 400 triángulos;
  - un anillo de espuma de 0,6 m a su alrededor (el quad de §8.3, fijo) para que no flote "pegada".
- **Movimiento propio** (ANIMATION_BIBLE §6): cabeceo `0,08·sin(1,7t + semilla)` rad y arfada 0,02 m. Sigue en pausa y con reducir movimiento (es el mundo).
- **Dónde va:** `x` +4,5 m. En `z` de mundo, el objetivo es `D_escena + 9,5 + (L_k − d_paso)`, donde `d_paso` son los metros del paso (interpolados como el panel) y `L_k` es la próxima marca de 500. Mientras está adelante, la posición sigue a ese objetivo con un resorte crítico (τ 0,6 s) y una corrección de ≤ 0,5 m/s, así nunca salta. Después de cruzar `d_paso ≥ L_k` queda fija en el mundo y se recicla a 30 m detrás de la cámara.
- **Aparición:** el tramado de los objetos de la orilla (400 → 360 m).
- **Salida:** si el paso termina sin cerrar la vuelta, se funde en 0,6 s.
- **Cuándo hay:** solo en los pasos de trabajo de una rutina.
- **HUD:** no suma ningún elemento.

---

## 10. Reducir movimiento y pausa

| Qué | Pausa | Reducir movimiento |
|---|---|---|
| Palada | frena en ≈ 0,25 s; reposo con respiración a los 1,5 s | **sigue** (es el dato) |
| *Surge*, cabeceo, arfada y rolido del bote | el *surge* se apaga con la velocidad; el balanceo lento sigue | siguen |
| Parte de la cámara (arfada, *surge* y cabeceo) | × 0,6 | **× 0** |
| Salpicadura y charcos | no nacen nuevos; los que hay se apagan | siguen |
| Gotas, hervidero y flexión de la caña | se terminan en la palada que frena | siguen |
| Boya y su vaivén | queda (el bote desliza y frena) | sigue |
| Cambio de lado del óvalo | 1,5 s | 1,5 s (es un fundido) |

---

## 11. Presupuesto para 60 fps (nivel Medio, sumado a la 6a)

| Pieza | *Draw calls* | Triángulos | GPU estimada | CPU |
|---|---|---|---|---|
| Remero (maniquí · Y Bot) | 1 (+1 con la gorra) | 6 k · 55,3 k (tope 60 k; QA7-G-01) | 0,15 ms (*skinning* por GPU) | Animation Rigging ≤ 0,30 ms; `ScullingKinematics` + `VisualStrokeClock` ≤ 0,05 ms |
| Remos (2) | 2 | 2,4 k | 0,05 | — |
| Riggers, carro y pedalera | 1 | 2 k | 0,03 | — |
| Casco (de la 6a) | 1 | 6 k | ya contado | — |
| Salpicadura y gotas (2 ParticleSystem, ≤ 120 vivas; `EmitParams`, sin asignar memoria) | 2 | < 0,5 k | 0,15 | ≤ 0,08 ms |
| Charcos, hervidero y anillo de la boya (instanciados) | 1 | < 0,1 k | 0,10 (sobre-dibujo ≤ 1,3× en ≈ 3 % de la pantalla) | — |
| Boya | 1 | 0,4 k | 0,02 | — |
| Óvalo de sombra (solo sin mapa) | 0–1 | 2 | 0,02 | — |
| Pasada de sombras (remero, palas y casco como emisores) | +4 | (≈ 10 k vueltos a dibujar) | 0,20 | — |
| Agua que recibe la sombra (+6 ALU y +1 textura: 24 → 30 ALU, tope 70) | 0 | — | 0,10 | — |
| **Suma de la 7** | **≈ +12** | **≈ +12 k con el maniquí** (+36 k con Y Bot) | **≈ +0,8 ms** | **≈ +0,45 ms** |
| **Total con la 6a** | **≈ 95** (tope 150; SetPass ≈ 38, tope 50) | **≈ 147 k** (≈ 171 k con Y Bot; tope 350 k) | **≈ 11,4 ms** (tope 12; poco margen: si la medida lo pide, lo primero que se baja es el hervidero y el mapa de sombras a 30 m) | escena ≤ 1,5 + rig 0,3 |

- **Huesos:** ≤ 75 por personaje. Pesos por vértice: 4 en Alto y Medio, 2 en Bajo. *Skinning* por GPU. El `Animator` en `Always Animate`, con 10 restricciones (pelvis, 3 de la columna, 2 hombros, 2 piernas, 2 brazos) y el *aim* de la cabeza.
- **Texturas:** ninguna nueva para el maniquí, los remos ni el bote (colores por vértice y región). Con Y Bot, las suyas a 1 024 en móvil.
- **Bajo:** partículas × 0,5, sin hervidero y el óvalo. **Alto:** sombra de 2 048 y gotas × 1.
- **0 B por cuadro en régimen.** Se mide con `FrameAllocations` (con su control positivo) y con el Profiler del build de desarrollo.

---

## 12. Lo que tiene que verse en las tomas (complementa el PO §6)

| Toma | Mira |
|---|---|
| CAP-01 / CAP-02 | la pala vertical y enterrada contra la pala plana con la cara cóncava arriba; los anteojos y la gorra se leen a 47 dp |
| CAP-03 | la salpicadura hacia proa, más alta que ancha, con ≥ 6 gotas por pala de ≥ 1,5 dp |
| CAP-04 | **Un** par de remolinos a `x` ±2,1 m, 4 m más cerca de la cámara que donde nació. Con `R-4` (≈ 4 m/s, 10 m por palada) el par anterior ya pasó la cámara: hay dos pares a la vista solo por debajo de ≈ 2,2 m/s a 24 spm (DA7-15) |
| CAP-05 a CAP-09 | la sombra con mapa estirada hacia la cámara a las 9 y a las 17 h; a las 22 h en Medio, el óvalo (sin mapa de noche); en Bajo, el óvalo |
| CAP-17 / CAP-18 | con la cámara lateral: el puño a −0,72 / +0,55 m del perno, los talones ≈ −0,17, los brazos sobre las rodillas en el catch y las canillas verticales |
| VID-06 | el plumeo empieza **después** de que la pala salió (no en el aire antes) y la pala se cuadra antes de entrar; la mano izquierda del remero arriba en el cruce |

---

## 13. Pedidos al Architect

| ID | Prioridad | Pedido | Verificación | ¿Cambia un AC? |
|---|---|---|---|---|
| **DA7-01** | P0 | `RiggingFit` (Core) fija la pedalera y el recorrido del carro a partir de los huesos (§4.3). La pedalera de 1,15 m y el carro de 0,6 m quedan solo en `LegacyBoatGeometry` | dotnet: con 1,85 m (tabla §2.1), los talones en −0,17 ± 0,05, la cadera en +0,18 / +0,72 ± 0,05, los brazos ≥ 97 % en el catch y el puño a ± 3 cm de las costillas en el finish; ídem con un rig de 1,75 m | **Sí**: AC-SCN-05-06 dice "pedalera a 1,15 m del perno" y "recorrido 0,62". Pasa a "pedalera y recorrido de `RiggingFit` (≈ 0,17 m y ≈ 0,54 m para 1,85 m)" (PO) |
| **DA7-02** | P0 | El cockpit del casco, de `z` 9,05 a 10,45, con los rieles (§5.2) | captura lateral: el carro y los pies dentro del cockpit en todo el ciclo | No |
| **DA7-03** | P0 | `ProceduralRower`: esqueleto `mixamorig:*` completo con las medidas de §2.1; malla de *loft* ≤ 6 k triángulos con los pesos de §2.2; avatar con `AvatarBuilder` (`isValid && isHuman`) | dotnet: los largos de los huesos (± 1 mm); PlayMode: el avatar es válido; ≤ 6 000 triángulos | No |
| **DA7-04** | P0 | `WaterDegraded` recibe la sombra de la luz principal cuando el nivel tiene mapa de sombras (`0,10·ambiente`); sin mapa, el óvalo de `BoatVisual`. Nunca los dos | PlayMode: Medio a las 12 h, la sombra del remero sobre el agua y 0 óvalos; Medio a las 22 h y Bajo, el óvalo; costo del *shader* ≤ 30 ALU | AC-SCN-08-06: precisa "sombra real" = el agua recibe el mapa; de noche en Medio, el óvalo |
| **DA7-05** | P1 | Un *shader* `RowMate/Rower` con 8 regiones por color de vértice (§2.3). Para Y Bot, `RowerOutfitBaker` hornea una vez, al cargar y en una copia de la malla, la región por hueso dominante y las franjas por `\|normal.x\|`; se asigna a las dos submallas | PlayMode: CAP-20 / CAP-21 con los colores del contrato; 0 B por cuadro después del horneado | No |
| **DA7-06** | P0 | Las cañas con `ShadowCastingMode.Off`; las palas, el casco y el remero proyectan sombra | captura a las 9 h: sin titileo de cañas | No |
| **DA7-07** | P1 | Tamaño mínimo en pantalla en los vertex shaders de la caña (1,2 dp), de las gotas de la salpicadura (1,5 dp) y de las de la recuperación (1,0 dp), con el alfa compensado (§8.4) | captura TV: las gotas de CAP-03 ≥ 1,5 dp | No |
| **DA7-08** | P0 | `PuddleField` + `WaterInteractionRenderer` en el modo degradado: un *pool* de 12 quads instanciados en un *draw call* (charcos, hervideros y el anillo de la boya), con un *shader* procedural de ≤ 20 ALU y 0 texturas | dotnet: reglas legacy y D-ANI-02; PlayMode: 1 *draw call* con 12 vivos | No |
| **DA7-09** | P0 | `StrokeVfx` con 2 ParticleSystem en espacio de mundo, emitidos por código (`EmitParams`) en `BladeEntry` y en el tramo `BladeExit`–0,55, con las cantidades de §8.1–§8.2 y la mitad en Bajo | `FrameAllocations`: 0 B; ≤ 120 partículas vivas a 36 spm con e = 1 | No |
| **DA7-10** | P1 | `BoatVisual.ShadowSide`: el corrimiento del óvalo con 1,5 s `ease.move` al cambiar de lado; en el acto con una hora de prueba fija | dotnet: continuo; captura: el lado correcto a las 9, 17 y 22 h | No |
| **DA7-11** | P1 | `MarkerBuoy` (Core: `BuoyPlacement`): la `z` objetivo, el resorte (τ 0,6 s, ≤ 0,5 m/s), la fijación al cruzar, el fundido al cambiar de paso; la malla y el anillo (§9) | dotnet: con `F-5` + `R-4`, la boya en `z` relativa 9,5 ± 0,5 m a los 155 ± 1 s; sin saltos de > 0,5 m/s | No |
| **DA7-12** | P1 | `RowerGripPose.anim` con los valores de §4.6 (`RowerPoseClipBuilder`) | Y Bot en la cámara lateral: dedos cerrados sobre el puño, sin atravesarlo | No |
| **DA7-13** | P2 | Cruce de manos con la mano izquierda **del remero** en `x > 0` (D7-02); los charcos "izquierdos" en `x < 0` | dotnet: en α = 0, la mano de `x > 0` está 2,5 cm arriba | No (aclara AC-SCN-05-09) |
| **DA7-15** | P2 | CAP-04 del PO pide "dos pares" de charcos, pero con `R-4` se ve uno solo (el legacy hace lo mismo: el charco nace a 7,8 m de la cámara y el bote recorre 10 m por palada). Se ajusta la toma: un par, más la cuenta de los charcos vivos en el `manifest` (2 o 4) | captura y `manifest` | Sí: el texto de CAP-04 (PO) |
| **DA7-14** | P1 | Cuentas de presupuesto en `SceneBudgetTests` con el remero: ≤ 100 *draw calls* y ≤ 40 SetPass en Medio, triángulos ≤ 160 k con el maniquí y ≤ 205 k con Y Bot (QA7-G-01: Y Bot tiene 55,3 k) | PlayMode, Medio, a las 12 y a las 22 h | No |

---

## 14. Para issues

- Boya: en TV, a `x` +4,5 m, pasa por el borde derecho (≈ 400 de 411 dp). Probar con `x` +3,5 m (queda a 0,7 m de la punta de la pala).
- Boya con número (500, 1 000…) y una boya en el entrenamiento libre cada 500 m de la sesión.
- Micro-anillos de las gotas y de la salpicadura en el agua (6b, con la textura de interacción).
- LOD1 del remero y de los remos para las cámaras extra (backlog, Q-66).
- Tono de piel elegible y personalización de colores del mono y del bote (backlog).
- Brillo de sudor que sube con el tiempo activo (ART_BIBLE §7.9).
- Pelo propio del maniquí en lugar de la gorra obligatoria.
- Reflejo del remero y del bote en el agua avanzada (6b).
- Sombra del remo entero con un mapa más fino (Alto, 2 048 a 40 m) si la caña deja de titilar.
- Respiración de remada (pecho −1 % en el drive y +1 % en la recuperación) y tensión de hombros con el esfuerzo: no se ven a 47 dp. Se hacen cuando haya una cámara cercana.
