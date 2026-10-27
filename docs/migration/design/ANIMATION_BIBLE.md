# Biblia de animación — RowMate (Unity 6 LTS + URP)

| Campo | Valor |
|---|---|
| Autor | `rowmate-designer` |
| Fecha | 2026-10-27 |
| Base | [LEGACY_VISUAL_AUDIT.md](LEGACY_VISUAL_AUDIT.md) §6, §7.7–7.9 y §12 · inventario `immersive-scene.md` (F-SCN-03 a F-SCN-08) · inventario `workout-engine-hud.md` (F-WKT-13 a F-WKT-21) · [ART_BIBLE.md](ART_BIBLE.md) · [ROADMAP](../ROADMAP.md) (iteraciones y gates, §6) · [ARCHITECTURE](../architecture/ARCHITECTURE.md) §1.2, §3 y §14 (paquetes, assemblies y escena: mandan en el CÓMO) |
| Alcance | Motion de UI, remada biomecánica, agua reactiva, bote, cámara, mundo vivo y la estrategia técnica (qué va en Core testeable con dotnet y qué en Unity) |

**Regla de lectura.** **[CONTRATO]** = regla o número del legacy que no cambia (y tiene test). **[EXR]** = mejora visual que no cambia datos, reloj ni métricas. **[DECIDIR]** = necesita OK (lista en §0.2). `smooth(t) = t²(3 − 2t)`; `ramp(x; a; b) = smooth(clamp((x − a)/(b − a); 0; 1))`; `p` = fase de palada del contrato (uniforme en el tiempo, avanza a `spm/60`); `c` = fase **canónica** (la del legacy: drive 0–0,40, recuperación 0,40–1,00).

---

## 0. Principios y decisiones

### 0.1 Principios

1. **Todo lo que se mueve en la escena está guiado por datos**: SPM → ritmo de palada; split → velocidad del bote; watts → potencia visual; fase activa/pausa/fin → inercia. No hay ningún loop de animación del remero desconectado del esfuerzo.
2. **Continuidad C1**: ninguna posición, ángulo ni velocidad salta. Los cambios de SPM, split o fase se filtran antes de llegar a la pose.
3. **La animación nunca adelanta, demora ni frena el reloj ni las métricas.** Las animaciones de UI son de presentación: el estado cambia en el instante del dato; lo que se anima es cómo se ve.
4. **El dato mostrado siempre es real**: los números en vivo no "cuentan" pasando por valores intermedios inventados (solo los totales finales pueden contar, §2.5).
5. **60 fps**: curvas evaluadas en Core sin asignaciones; nada de animación por cambio de layout de UI Toolkit en cada cuadro (se anima con `transform`/`opacity`). Fuera del inmersivo, cada animación de este documento pide 60 fps a `FramePacing` mientras corre y la libera al terminar; en reposo la app baja a 30 fps (ARCHITECTURE §13.1, QA-P-07).
6. **Reducir movimiento** del sistema (`IAccessibilitySettings.ReduceMotion`, ARCHITECTURE §13.2) apaga respiración de cámara, vuelos y pulsos (ART_BIBLE §10.4); la remada y el agua siguen (son el dato).

### 0.2 Propuestas que necesitan decisión

El gate, el valor por defecto y si se le confirma al usuario están en [ROADMAP §6](../ROADMAP.md) (QA-P-13): D-ANI-05 en la 4b; D-ANI-01 a 04 en la 7; D-ANI-06 en la 10a; D-ANI-07 en el backlog.

| ID | Para | Propuesta |
|---|---|---|
| D-ANI-01 | Architect | La animación usa una **fase visual** que sigue a la del contrato con un seguidor suave (§3.1). La fase del contrato (`SceneState`, tests legacy) no cambia. |
| D-ANI-02 | Architect + PO | Los **charcos se crean cuando sale la pala visible** (fase canónica 0,38 del reloj visual) y en la posición real de la punta de la pala (geometría realista, ART_BIBLE D-ART-04). La lista de charcos legacy de `SceneState` queda para sus tests. En 24 spm el test legacy `scene_state_test:69` pasa igual. |
| D-ANI-03 | Architect | El remero y los remos **cabecean con el casco** (el legacy no inclina al remero). Son ±1,15°: en 3D, separarlos se ve como un error. |
| D-ANI-04 | PO | Agregados visuales: arfada (*heave*) y rolido leve del bote, flexión de la caña, gotas de la pala, respiración del remero en reposo. Ninguno cambia datos. |
| D-ANI-05 | PO | Cuenta grande centrada en los últimos 3 s del paso (propuesta de F-WKT-14) y sello "Objetivo cumplido / No alcanzado" de 2 s (F-WKT-09). Agregan elementos al HUD. |
| D-ANI-06 | Architect + QA | Sincronía fina con el contador de paladas del monitor (§3.10): apagada por defecto hasta verificar con el AMS-670B en qué momento incrementa. |
| D-ANI-07 | PO | Cámaras alternativas y director automático (§5.4): especificados, fuera de paridad (BKL-02). |

### 0.3 Estrategia técnica de la animación del remero (decisión)

**Decisión: IK procedural con Animation Rigging sobre el rig Humanoid de Mixamo, con toda la lógica de ciclo, curvas y geometría en Core (C# puro, testeable con dotnet).** Los clips de Mixamo **no** se usan para la remada.

| Criterio | Clips de Mixamo + blend tree | IK procedural (elegida) |
|---|---|---|
| Existe el movimiento | Mixamo no tiene remada de *sculling* sentado con carro deslizante y remos; habría que fabricar los clips igual | Se define con los números de este documento |
| Ritmo variable por SPM (Q-70) | Un clip tiene un ratio drive/recuperación fijo; mezclar clips de distintos SPM mezcla poses, no tiempos: aparecen manos que se despegan del puño y pies que patinan | El ratio es un parámetro continuo (§3.2); la pose siempre cierra |
| Contactos (manos en el puño, pies en la pedalera, cadera en el carro) | Hace falta IK igual para corregir | Es la base del sistema |
| Fidelidad al legacy | Imposible garantizar la secuencia exacta de los ramps del contrato | Usa los mismos ramps de `StrokeCycle` (tests portados) |
| Testeable con dotnet | No (el contenido de un clip no se puede verificar fuera de Unity) | Sí: poses, ángulos, alturas de pala, eventos y continuidad |
| Intercambiar personaje (asset slot) | Requiere retarget de cada clip | Cualquier Humanoid; el ajuste de pedalera se calcula de las medidas del rig (§7.4) |
| Costo | Animator + blend | Animation Rigging corre en *Animation C# Jobs* compilados con Burst: ≤ 0,3 ms para un personaje. Burst y Mathematics entran como dependencias del paquete; el código de RowMate no los usa (§7.3) |

Uso de Mixamo: el **personaje** (malla + rig Humanoid). Opcional: un clip de reposo sentado, solo como referencia de dedos (ASSET_SLOTS `rower.clip.idleRef`). La respiración es procedural (§3.9). El `Animator` lleva un controlador de pose propio (§7.3). Detalle del rig en §7.

---

## 1. Tokens de motion

### 1.1 Duraciones

| Token | ms | Uso típico |
|---|---|---|
| `dur.micro` | 90 | presionar botón, aparición de íconos chicos |
| `dur.fast` | 160 | cambios de color, fundidos de texto, rodillo de dígitos del reloj |
| `dur.base` | 240 | entradas y salidas de elementos, indicadores |
| `dur.slow` | 360 | paneles que se expanden, reacomodos |
| `dur.page` | 420 | transiciones entre pantallas |
| `dur.scene` | 900 | cambios de escena, fundidos de luz |
| `dur.cinematic` | 1 600 | vuelo de entrada de cámara, transiciones de cámara |

### 1.2 Curvas

UI Toolkit (USS `transition-timing-function`) solo trae curvas con nombre; no hay `cubic-bezier`. Se mapean así:

| Token | USS | Uso |
|---|---|---|
| `ease.enter` | `ease-out-cubic` | todo lo que entra |
| `ease.exit` | `ease-in-cubic` | todo lo que sale |
| `ease.move` | `ease-in-out-cubic` | desplazamientos y cambios de tamaño |
| `ease.pulse` | `ease-in-out-sine` | pulsos y respiraciones (reemplaza el lineal del legacy) |
| `ease.emphasis` | `ease-out-back` | solo sellos de éxito y "solidificar" la píldora |
| `ease.linear` | `linear` | progreso, brillos en barrido |

Para lo que USS no resuelve (rodillo de dígitos, indicadores que siguen un valor, cámara) se usan **resortes críticamente amortiguados** del Core (`CriticalSpring`, ζ = 1):

| Token | ω (rad/s) | Llega al 98 % en | Uso |
|---|---|---|---|
| `spring.snappy` | 22 | ≈ 0,26 s | indicador de pestaña, píldora activa, tooltip |
| `spring.smooth` | 12 | ≈ 0,49 s | paneles de series, tamaños |
| `spring.gentle` | 6 | ≈ 0,97 s | seguimiento y respiración de cámara |

Los resortes todavía **no** son tokens de `Tokens.cs` (el mecanismo de la It. 1 no tiene un tipo para ellos). Viven en esta tabla hasta que el Architect decida el tipo (propuesta: `SpringToken.Omega(nombre, radPorSeg)` junto con `CriticalSpring`, 04-design-notes de la It. 1, §7).

### 1.3 Distancias de desplazamiento

`shift.s` 8, `shift.m` 12, `shift.l` 16 y `shift.xl` 24 dp (tokens de `Core/Design/Tokens.cs`, que es la fuente que manda; también las duraciones `dur.*` y las curvas `ease.*` de §1.1-§1.2). Nunca más de `shift.xl` en un elemento del HUD (el ojo del remero tiene que reencontrarlo en el mismo lugar).

---

## 2. Motion de UI (coreografías)

### 2.1 Cambio de pestaña

| t (ms) | Qué |
|---|---|
| 0–120 | vista saliente: opacidad 1→0, x 0→−12·dir (`ease.exit`) |
| 80–280 | vista entrante: opacidad 0→1, x +12·dir→0 (`ease.enter`) |
| 0–260 | pastilla indicadora se desliza al destino (`spring.snappy` en x y ancho); ícono escala 1→1,12→1 y cambia de color (160). El glifo (contorno → relleno en Workout, `bluetooth` → `bluetooth_connected` en Dispositivo) cambia en el cuadro 0, junto con el comienzo del fundido de color; con los íconos provisorios (ASSET_SLOTS §4.4) es igual |

`dir` = +1 si la pestaña está a la derecha. Las vistas se conservan vivas (paridad `IndexedStack`): no se recrean.

### 2.2 Pantallas apiladas y diálogos

- **Push** (detalle, editor): entra desde la derecha 24 dp + fundido (`dur.base`, `ease.enter`); la anterior baja a 92 % de brillo y x −24. **Pop**: inverso en 200 ms.
- **Diálogo:** velo `#000000` 0→60 % (200); diálogo escala 0,96→1 + fundido (220, `ease.enter`); salida 150 `ease.exit`. Tocar afuera = cancelar (paridad).
- **Aviso (toast):** sube 16 dp + fundido (220), queda 4 s, sale (160).

### 2.3 Aparición de tarjetas y listas

- Primera carga: escalonado de 35 ms por tarjeta, máximo 8 animadas (el resto aparece con la octava); cada una: opacidad 0→1, y +12→0, `dur.base` `ease.enter`.
- Recarga (Historial, Q-22 "sin parpadeo"): **no** se reanima la lista; solo los ítems nuevos entran (200) y los que se van colapsan su alto (200) con fundido (120).
- Editor de rutinas: agregar paso = alto 0→N con `spring.smooth` + fundido; borrar = fundido 120 + colapso 200; el bloque de serie se estira con el mismo resorte.

### 2.4 Valores en vivo: rodillo de dígitos

- Solo animan los **dígitos que cambian** (componente `TabularNumber`, ART_BIBLE §4.2). Dígito viejo: y 0→∓0,35 em y opacidad 1→0; nuevo: y ±0,35 em→0 y 0→1; 180 ms `ease.enter`. Sentido: hacia arriba si el valor numérico subió, hacia abajo si bajó.
- Si llega un dato nuevo antes de terminar, el rodillo en curso salta a su final y arranca el siguiente (nunca se acumulan).
- Tarjeta del tablero (Dispositivo): además, el acento superior se ilumina 100 %→60 % en 300 ms.
- Con "reducir movimiento": fundido de 80 ms en lugar de rodillo.

### 2.5 Contadores que interpolan

Solo para **totales finales** (diálogo de rutina completada, resumen del detalle la primera vez que se abre): cuentan de 0 al valor en 700 ms `ease.enter`, escalonados 120 ms, y **siempre terminan en el valor exacto** formateado igual que el legacy. Las métricas en vivo nunca cuentan (principio 4).

### 2.6 Barra de pasos, píldoras y cuenta regresiva

- **Relleno continuo de la píldora en curso** [EXR]: `progresoVisual = min(p_tick + Δt / duración; p_tick + 1 / duración)` mientras está activo, donde `p_tick` es el progreso del último tick de 1 s y `Δt` el tiempo desde ese tick. En pausa se congela. Nunca supera el valor del próximo tick: no se adelanta al reloj.
- **Últimos 5 s** [CONTRATO del momento]: un único reloj de 600 ms ida y vuelta (`ease.pulse`, el legacy usa lineal) maneja: glow de la píldora 0,3→0,7 (blur 8, spread 2); opacidad de "Up next" 0,6→1,0; color de la cuenta blanco↔color del paso. El tamaño de la cuenta pasa 20→22 con `spring.snappy` (el legacy salta 18→20).
- **Reloj y cuenta regresiva:** rodillo de dígitos de 160 ms cada segundo.
- **"Up next"** (≤ 15 s y hay paso siguiente): el chip entra con ancho 0→N (`spring.smooth`) + fundido 200, después pulsa con el reloj de 600 ms.

### 2.7 Cambio de paso ("pasar la posta")

`t = 0` en el instante en que el motor cambia de paso (el estado ya cambió; esto es solo presentación).

| t (ms) | Elemento | Animación |
|---|---|---|
| 0–120 | píldora que termina | su relleno completa a 100 % (si faltaba por redondeo) |
| 120–340 | ídem, cumplida o sin objetivo | se "solidifica": pasa a `@85 %` con un destello blanco `@40 % → 0` (`ease.emphasis`, 220) |
| 120–440 | ídem, no cumplida | el borde de 1,5 px se dibuja alrededor (trazo que recorre el perímetro, 320) y el interior baja a `@18 %` |
| 0–260 | marcador de píldora activa | se mueve a la siguiente con `spring.snappy` |
| 60–260 | tipo de paso y chip de objetivo | fundido cruzado + subida de 8 dp (200) |
| 0–160 | cuenta regresiva | rodillo a la nueva duración (o metros) |
| 0–240 | tarjetas con objetivo | borde y glow aparecen o desaparecen (240); colores 150 |
| 0–2 000 | sello de resultado [DECIDIR D-ANI-05] | "Objetivo cumplido" (`state.success`) o "No alcanzado" (`phase.paused`), con promedio vs objetivo, en vidrio bajo la barra; entra 240 `ease.emphasis`, sale 200 |

### 2.8 Alertas de objetivo (semáforo)

El umbral se evalúa **instantáneo con cada lectura** [CONTRATO]. Lo visual:

- Entrar en "fuera de objetivo": color del valor a `state.offTarget.hud` en 150 ms (`ease.move`); flecha ▲/▼ aparece (120) deslizándose 4 dp; **un** empujón de escala 1→1,04→1 (240 ms), como máximo una vez cada 4 s por tarjeta (no "molesta").
- Volver a objetivo: color a blanco 150 ms; flecha sale 120. Sin parpadeos ni loops.

### 2.9 Pulso cardíaco

El corazón de la tarjeta del pulsómetro y de la tarjeta BPM late **a la frecuencia real**: período `T = 60 / bpm`; en cada latido escala 1→1,16 (90 ms `ease.enter`) → 1 (200 ms `ease.move`) y un segundo golpe a +170 ms de 1→1,06→1 (70/150). El período nuevo se aplica al próximo latido (sin saltos). Sin pulso o bpm 0: quieto `@40 %`. El corazón es el glifo `favorite` relleno en los dos estados (como el legacy); el latido es una escala del elemento, así que el ícono provisorio late igual. El pivote de la escala es el centro del ícono (la caja del glifo es simétrica, ASSET_SLOTS §4.2). La tarjeta BPM del HUD aparece y desaparece con fundido de 240 (F-WKT-05 mejora).

### 2.10 Paneles de series

- Expandir/colapsar: alto y ancho (140↔200) con `spring.smooth`; contenido con fundido 160 demorado 80; flecha rota 180° en 240.
- Vuelta nueva: la fila entra arriba con alto 0→N (200) + deslizamiento desde −8 dp + fondo `metric.split @20 % → 0` en 1,2 s.

### 2.11 Pausa, entrada y salida del inmersivo, completado

- **Pausa:** la escena se desatura 30 % y oscurece 15 % (peso de un *Volume* de URP, 300 ms); tarjetas del HUD a 85 %; etiqueta "PAUSA" (texto del catálogo) escala 0,96→1 + fundido 240. **Reanudar:** inverso en 240. El bote sigue frenando con su inercia (§4.8); el remero se detiene suave (§3.9).
- **Entrada "sumergirse"** (al iniciar una sesión): Workout se funde a negro (220) y se ocultan las barras del sistema → la escena aparece con el **vuelo de cámara** (§5.3, 1,6 s) → HUD en cascada: barra de pasos a 400 ms, SPM 520, Split/Watts 600, banda 680, kcal/BPM 760, controles 840; cada uno 280 ms (`ease.enter`, y +10→0). La sesión y el reloj corren desde el toque: la animación no demora nada.
- **Salida:** HUD sale junto (200) → cámara se eleva y se aleja (§5.3) → fundido a negro desde 0,8 s (400) → Workout entra (240).
- **Rutina completada:** 300 ms después de `finished`, diálogo de vidrio; el check se dibuja (trazo 320) con escala `ease.emphasis`; los totales cuentan (§2.5); la cámara hace la toma aérea lenta (§5.3).

### 2.12 Otros

| Elemento | Spec |
|---|---|
| Botones | presionar: escala 0,97 (90, `ease.enter`) y capa `#FFFFFF @8 %`; soltar: `spring.snappy`. Sin *ripple*. |
| Selector de escenario | borde y ancho con `spring.snappy`; miniatura escala 1→1,03 (240); nombre: fundido de peso |
| Búsqueda BLE ("sonar", It. 10a) | 3 anillos que se expanden (radio 0→1 `ease.enter`, alfa 0,45→0), período 1,6 s, desfase 0,53 s; tarjetas encontradas con escalonado de §2.3 |
| Pasos de conexión (It. 10a) | cada paso marca su check (trazo 240); error = color + ícono, **sin sacudidas** |
| Skeleton | barrido de brillo `@8 %` de izquierda a derecha, 1,2 s lineal en loop (es "esperando", no dato) |
| Gráfico del detalle | franjas de paso aparecen (200) → la línea se dibuja de izquierda a derecha (revelado, 600 `ease.enter`) → el área se funde (300, desde 150); cambio de métrica: fundido cruzado 220 + reescalado del eje Y 300 (`ease.move`); tooltip sigue el dedo con `spring.snappy` |
| Rotación del dispositivo | el HUD se reacomoda con fundido cruzado de 200; la escena no se reinicia |
| Luces nocturnas | ventanas y faroles se encienden de a una en 20 min de escena alrededor del umbral `ambient < 0,6` (cada luz con un desfase fijo por semilla), cada encendido 0,4 s |
| Háptica (It. 14) | leve en pausa/reanudar, media en cambio de paso, "éxito" al completar la rutina |

---

## 3. La remada

### 3.1 Dos fases: contrato y visual

- **Fase del contrato `p`** [CONTRATO]: la de `SceneState`, avanza a `spm/60` ciclos/s con el entrenamiento activo y spm > 0; con spm = 0 termina el ciclo a 12 spm y queda en 0; en pausa se congela. Sus tests legacy se portan sin cambios.
- **Fase visual `φ`** [DECIDIR D-ANI-01]: la que anima remero, remos, agua y bote. Sigue a `p` sin saltos:

  `φ' = ω_s + K · wrap(p − φ)`, con `K = 4 s⁻¹`, `wrap ∈ (−0,5; 0,5]`, `φ' ≥ 0` (nunca retrocede)

  donde `ω_s` es la velocidad de fase suavizada: sigue a `spm/60` con constante de tiempo **0,5 s** mientras está activo y cae a 0 con **0,15 s** en pausa. En régimen estable `φ = p` (error < 0,001 ciclos); ante un escalón de SPM 24→30 la velocidad cambia de forma continua y el error máximo es ≈ 0,05 ciclos, absorbido en ~1 palada.

### 3.2 Ritmo según SPM (reemplaza el ratio fijo, Q-70)

La fase canónica del legacy tiene el drive en 0–0,40 siempre. El ritmo real cambia con el SPM: el drive casi no se acorta y la recuperación sí (audit §12.1). Se mapea la fase visual uniforme `φ` a la canónica `c` con un ratio de drive `r`:

- Duración del drive: `T_d(spm) = clamp(1,05 − 0,0194 · (spm − 18); 0,60; 1,15)` s
- Ratio: `r(spm) = clamp(T_d · spm / 60; 0,22; 0,48)`
- Mapeo (continuo y monótono): `c = 0,40 · φ / r` si `φ < r`; si no, `c = 0,40 + 0,60 · (φ − r) / (1 − r)`
- El SPM que alimenta `r` se suaviza (τ = 0,8 s) y `r` **se fija al pasar por el catch** (`φ` cruza 0): nunca cambia a mitad de palada, así que `c` no salta.
- Los quiebres de pendiente del mapeo caen en `c = 0` y `c = 0,40`, donde todos los ramps del legacy tienen velocidad cero: el quiebre es invisible.

| SPM | Ciclo | Drive | Recuperación | `r` | Legacy (drive = 0,40 × ciclo) |
|---|---|---|---|---|---|
| 12 (cierre sin remar) | 5,00 s | 1,15 s | 3,85 s | 0,230 | 2,00 s |
| 18 | 3,33 s | 1,05 s | 2,28 s | 0,315 | 1,33 s |
| 20 | 3,00 s | 1,01 s | 1,99 s | 0,337 | 1,20 s |
| 24 | 2,50 s | 0,93 s | 1,57 s | 0,373 | 1,00 s |
| 28 | 2,14 s | 0,86 s | 1,29 s | 0,399 | 0,86 s |
| 32 | 1,88 s | 0,78 s | 1,10 s | 0,415 | 0,75 s |
| 36 | 1,67 s | 0,70 s | 0,97 s | 0,420 | 0,67 s |

### 3.3 Fases y secuencia [CONTRATO de la secuencia]

Se usan **los mismos ramps del legacy** sobre `c` (`StrokeCycle`, audit §7.8):

| Segmento | Drive (c) | Recuperación (c) |
|---|---|---|
| Piernas `L` (0 plegadas → 1 extendidas) | `ramp(c; 0,00; 0,18)` | `1 − ramp(c; 0,60; 1,00)` |
| Espalda `B` (0 adelante → 1 atrás) | `ramp(c; 0,10; 0,30)` | `1 − ramp(c; 0,50; 0,75)` |
| Brazos `A` (0 estirados → 1 al pecho) | `ramp(c; 0,22; 0,36)` | `1 − ramp(c; 0,40; 0,60)` |
| Finish (remo quieto) | 0,36–0,40 | |

Fases con nombre (para QA y para el código de efectos):

| Fase | c | Qué pasa |
|---|---|---|
| Catch (ataque) | 0,97 → 0,015 | la pala se cuadra arriba, baja y entra; las manos suben 4 cm |
| Drive — piernas | 0,00 → 0,18 | empuje de piernas con brazos estirados; el tronco mantiene su ángulo hasta 0,10 |
| Drive — apertura de espalda | 0,10 → 0,30 | el tronco pasa de +28° a −18° |
| Drive — brazos | 0,22 → 0,36 | tirón de brazos, codos afuera y atrás |
| Finish (final) | 0,36 → 0,40 | manos bajan ("tap down"), la pala sale |
| Recuperación — manos afuera | 0,40 → 0,60 | brazos se estiran a la misma velocidad con que llegaron |
| Recuperación — cuerpo adelante | 0,50 → 0,75 | el tronco pivota desde la cadera hasta +28° |
| Recuperación — carro | 0,60 → 1,00 | las rodillas suben recién cuando las manos pasaron las rodillas |

### 3.4 Ángulos articulares aproximados por posición (sculler de 1,85 m)

Convenciones: rodilla, cadera y codo = **ángulo interior** (180° = recto); tronco desde la vertical, **+ hacia popa** (hacia los pies, como el legacy); hombro = flexión del brazo respecto del tronco (+ adelante, − atrás). Los valores de rodilla, cadera, hombro y codo los resuelve el IK; esta tabla es el **criterio de aceptación visual** (± 8°).

| Posición | c | L / B / A | Tronco | Carro (0 = adelante) | Rodilla | Cadera | Hombro | Codo | Remo (visual) | Centro de la pala | Plumeo |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Catch | 0,000 | 0 / 0 / 0 | +28° | 0,00 | 50° | 32° | +80° | 178° | −62° | 0 → −0,13 m | 0° |
| Media pierna | 0,090 | 0,50 / 0 / 0 | +28° | 0,50 | 112° | 52° | +78° | 178° | −46° | −0,13 | 0° |
| Apertura | 0,180 | 1,00 / 0,35 / 0 | +12° | 1,00 | 170° | 85° | +70° | 176° | −11° | −0,13 | 0° |
| Brazos empiezan | 0,240 | 1 / 0,78 / 0,06 | −8° | 1,00 | 178° | 105° | +40° | 165° | +14° | −0,13 | 0° |
| Finish | 0,360 | 1 / 1 / 1 | −18° | 1,00 | 178° | 112° | −25° | 85° | +42° | −0,13 | 0° |
| Extracción | 0,395 | 1 / 1 / 1 | −18° | 1,00 | 178° | 112° | −25° | 85° | +42° | +0,05 | 11° |
| Manos afuera | 0,600 | 1 / 0,65 / 0 | −2° | 1,00 | 178° | 95° | +72° | 178° | +14° | +0,09 | 90° |
| Cuerpo adelante | 0,750 | 0,68 / 0 / 0 | +28° | 0,68 | 150° | 55° | +82° | 178° | −24° | +0,09 | 90° |
| Medio carro | 0,850 | 0,35 / 0 / 0 | +28° | 0,35 | 95° | 40° | +82° | 178° | −46° | +0,10 | 88° |
| Antes del catch | 0,960 | 0,03 / 0 / 0 | +28° | 0,03 | 52° | 32° | +80° | 178° | −61° | +0,17 | 2° |

Detalles que dan realismo (todos [EXR]):

- **Pelvis:** el 60 % del giro del tronco lo hace la pelvis (rota sobre los isquiones); el 40 % restante se reparte en Spine 15 %, Spine1 13 %, Spine2 12 %. Espalda recta, sin "joroba".
- **Cabeza estable:** mira a un punto fijo a 30 m hacia popa a la altura de los ojos (compensa el balanceo del tronco); micro-movimiento ±0,5°.
- **Hombros:** protracción +8° en el catch (alcance), retracción −6° en el finish.
- **Codos:** en el finish, afuera 35–45° del plano del tronco, antebrazos horizontales (±10°).
- **Muñecas planas** todo el ciclo (±10°); el plumeo lo hacen los dedos girando el puño (la mano acompaña 25 % del giro del puño).
- **Tobillos:** canillas verticales en el catch (±8°); talones se levantan 1–3 cm en el catch (zapatillas con talonera flexible).
- **Respiración de remada:** exhala en el drive (pecho −1 %), inhala en la recuperación (+1 %).
- **Esfuerzo** (watts, §4.9): en potencia alta el tronco llega a −20° en el finish (2° más atrás) y los hombros se tensan 1,5 cm hacia arriba en el drive.

### 3.5 Trayectoria del puño (manos)

La altura del puño **no es fija** (el legacy la fija en 0,45 m, hallazgo 6): sale de la palanca del remo. Con el eje del remo en la chumacera a `y_pin = 0,31 m`, el puño a 0,82 m del perno y el centro de la pala a 1,76 m del perno:

`y_puño = y_pin + (y_pin − y_centroPala) · 0,82 / 1,76`

| Momento | Centro de pala | Puño sobre el agua | Puño sobre el asiento (0,14 m) |
|---|---|---|---|
| Drive (pala enterrada) | −0,13 | 0,515 m | 0,375 m (costillas bajas) |
| Recuperación | +0,09 | 0,413 m | 0,273 m |
| Antes del catch | +0,17 | 0,375 m | 0,235 m (las manos bajaron 4 cm: "deja lugar" para cuadrar) |

En planta, el puño recorre el arco del remo (no una recta): `x = ±(0,80 − 0,82 · cos α)`, `z = 0,82 · sin α` respecto del perno. **Cruce de manos** (*sculling*): cuando `|α| < 25°` los puños se superponen ~16 cm; la **izquierda va arriba** (+2,5 cm) y adelante (+1,5 cm), la derecha abajo (−2,5 cm), con peso `w = 1 − smooth((|α| − 20°)/10°)`. Separación vertical mínima entre manos 4 cm (test).

### 3.6 Remo y pala

| Magnitud | Curva sobre `c` |
|---|---|
| Barrido visual `α_v` | `α_v = −62° + 102° · (α_legacy + 55°) / 90°`, con `α_legacy` = barrido del contrato (−55°→+35°, smooth en drive 0–0,36, quieto 0,36–0,40, vuelta en 0,40–1,00). Catch −62°, finish +42° (arco 104°, sculler intermedio) |
| Altura del centro de la pala | drive enterrada −0,13 (c 0,015–0,355) · extracción −0,13 → +0,05 (`smooth` en c 0,355–0,395) · subida a +0,09 (c 0,395–0,43) · recuperación +0,09, con **ondulación** de ±0,01 m siguiendo el oleaje del perfil · subida para cuadrar +0,09 → +0,17 (c 0,80–0,93) · **entrada** +0,17 → −0,13 (`smooth` en c 0,97 → 1,015, cruzando el 0: se evalúa con `c' = c + 1` si `c < 0,5`) |
| Plumeo (giro sobre el eje del remo) | 0° en el drive · 0° → 90° (`smooth` en c 0,385–0,43, después de que la pala salió) · 90° en la recuperación (cara cóncava hacia arriba) · cuadrado 90° → 0° (`smooth` en c 0,84–0,95: empieza cuando las manos pasan sobre los tobillos) |
| "Pala en el agua" (para efectos) | centro de pala ≤ 0. Sale en c ≈ 0,38 y entra en c ≈ 0,99: coincide con el contrato (pala adentro mientras `p < 0,38`) |
| Flexión de la caña [EXR] | desplazamiento de la punta `(0,02 + 0,04 · e) · sin(π · c / 0,36)` m durante el drive, 0 en la recuperación (vertex shader del remo, uniform de carga) |

Duraciones resultantes (ms):

| SPM | Extracción (c 0,355–0,395) | Plumeo | Entrada | Cuadrado | Gotas (c 0,40–0,55) |
|---|---|---|---|---|---|
| 18 | 105 | 154 | 154 | 419 | 571 |
| 24 | 93 | 113 | 113 | 287 | 392 |
| 30 | 82 | 90 | 90 | 218 | 296 |
| 36 | 70 | 75 | 75 | 177 | 241 |

### 3.7 Entrada y salida del agua (eventos)

`VisualStrokeClock` emite eventos por cuadro, una sola vez por palada:

| Evento | Cuándo | Qué dispara |
|---|---|---|
| `BladeEntry` | centro de pala cruza 0 bajando (c ≈ 0,99) | salpicadura del catch (§4.1), anillo de entrada en el agua |
| `Catch` | `φ` cruza 0 | se fija el nuevo `r`; mínimo de velocidad del bote (§4.8) |
| `DriveLoad` | c ∈ [0,02; 0,36] | "hervidero" bajo la pala (espuma que se forma, §4.3) |
| `BladeExit` | centro de pala cruza 0 subiendo (c ≈ 0,38) | 2 charcos por pala (§4.3) y arranque de gotas |
| `Finish` | c cruza 0,36 | máximo de velocidad del bote |

### 3.8 Watts → potencia visual

Esfuerzo `e = clamp((W − 60) / 240; 0; 1)` (60 W → 0; 300 W → 1), suavizado τ = 1 s.

| Efecto | Fórmula |
|---|---|
| Salpicadura del catch | partículas `8 + 14·e` por pala; altura máx. `0,12 + 0,18·e` m |
| Charcos | fuerza de la normal `0,5 + 0,5·e`; espuma `0,25 + 0,35·e` |
| Flexión de la caña | ver §3.6 |
| Amplitud del *surge* | × `0,85 + 0,30·e` |
| Tronco en el finish | −18° − 2°·e |
| Tensión de hombros | +1,5 cm·e en el drive |

### 3.9 Pausa, reanudar y cierre sin remar

- **Pausa:** la fase del contrato se congela [CONTRATO]; la visual frena con `ω_s → 0` (τ 0,15 s) y converge en ≈ 0,25 s: el remero **se detiene con desaceleración**, no "se congela de golpe". El bote sigue deslizando con su inercia de 1,5 s [CONTRATO] y el *surge* se apaga con la velocidad.
- **Reposo** (pausa o catch sin remar, después de 1,5 s quieto): respiración lenta (15/min: pecho ±0,6 cm, hombros ±0,5 cm), cabeza que mira de a ratos (yaw ±2° cada 4–7 s, suavizado). Se suma a la pose; no cambia la fase.
- **Reanudar:** `ω_s` vuelve a `spm/60` con τ 0,5 s; el remero retoma desde donde estaba.
- **Sin paladas (spm = 0, activo)** [CONTRATO]: el ciclo se termina a 12 spm (`r` = 0,23) y queda en el catch, con la respiración de reposo.

### 3.10 Sincronía fina con el monitor [DECIDIR D-ANI-06]

Si el monitor informa el contador de paladas (FTMS *Stroke Count*), cada incremento es un evento real. Se mantiene un desfase `o` (|o| ≤ 0,25) que se suma a `p` antes del seguidor visual: en cada evento `o += 0,5 · wrap(φ_ref − (p + o))`, con `φ_ref` = fase en la que ese monitor incrementa (a calibrar con el AMS-670B; muchos FTMS cuentan al final del drive → `φ_ref ≈ r · 0,36/0,40`). Corrige la mitad del error por palada, sin saltos. Apagado por defecto.

---

## 4. Agua, salpicaduras y bote

### 4.1 Salpicadura del catch

Contrato legacy como mínimo: salpicadura en cada catch, visible mientras `c < 0,08`, con al menos 6 gotas por pala que suben y caen y se apagan. Versión 3D [EXR]:

- Ráfaga en `BladeEntry`: `8 + 14·e` gotas por pala (ParticleSystem, compatible con móvil); velocidad inicial: arriba `1,4 + 1,0·e` m/s, hacia **proa** 0,6 m/s (es el *backsplash* de una entrada correcta), afuera 0,3 m/s, cono 35°; tamaño 0,02–0,05 m; gravedad 9,8; vida 0,35–0,6 s; alfa 0,8→0.
- "Corona": lámina fina de 0,25 m de ancho, alto `0,12 + 0,18·e`, 0,18 s, sombreada con el mismo reflejo del agua.
- Al caer, cada gota deja un micro-anillo en la textura de interacción (radio 0,03→0,2 m en 0,5 s).

### 4.2 Gotas en la recuperación

Desde `BladeExit` hasta c = 0,55: hilo de gotas desde el borde inferior y la punta de la pala, tasa `50 · (1 − u)` gotas/s por pala (u = avance 0→1 del tramo), tamaño 0,008–0,015 m, con anillos al tocar el agua. Es el detalle que más "vende" la salida limpia de la pala.

### 4.3 Charcos que quedan atrás [CONTRATO de reglas, D-ANI-02 de posición]

- **Durante el drive** (`DriveLoad`): bajo la pala se forma un "hervidero" de espuma de 0,4 × 0,25 m que acompaña a la pala.
- **En `BladeExit`:** se liberan **2 charcos por palada, uno por pala, primero el izquierdo** [CONTRATO]. Cada charco se dibuja como lo que es en el agua real: **un par de remolinos** en los extremos de la pala, separados 0,45 m a lo largo de ella y girando en sentidos opuestos. Quedan **fijos en el mundo** y la cámara los pasa [CONTRATO].
- **Vida 4 s** [CONTRATO]; radio 0,3 → 0,9 m [CONTRATO]; alfa `(1 − t/4)^1,5 · (0,30 + 0,35·e)` con entrada de 0,15 s; giro 1,5 rad/s que decae con la vida; adentro el oleaje se aplana (canal "calma") y el borde tiene un anillo brillante de espuma.
- En pausa no se crean (la fase está congelada) [CONTRATO].
- Visual: perturbación de normal + espuma en la textura de interacción (ART_BIBLE §7.7).

### 4.4 Estela

Fórmula legacy como base [CONTRATO]: V desde la popa con semiancho `0,3 + 0,35 · retroceso` (≈ ángulo de Kelvin), bordes de espuma, turbulencia central, intensidad `clamp(velocidad/4; 0; 1)`, espuma máx. 35 %. [EXR]: textura de espuma que se abre y se deshace con ruido, ola de proa chiquita (0,05 m) visible en las cámaras alternativas.

### 4.5 Surge: el bote dentro de cada palada

La velocidad media del bote sigue siendo la del contrato (`500/split`, τ 1,5 s) y la distancia de escena **no cambia**. Lo que se dibuja es un desfase periódico del bote respecto de su posición media:

- Factor de velocidad sobre la fase visual `φ` y el ratio `r`: en el drive `g = −1 + 2·smooth(φ/r)`; en la recuperación `g = 1 − 2·smooth((φ − r)/(1 − r))`. Mínimo en el catch (el "check"), máximo en el finish. Su media temporal en el ciclo es **exactamente 0**.
- Amplitud: `A(spm) = clamp(0,075 − 0,001·(spm − 20); 0,05; 0,08)` × factor de esfuerzo (§3.8). Velocidad visual = `v̄ · (1 + A · g)`.
- Desfase de posición (integral analítica, sin acumular error): drive `Δz = v̄ · T · r · A · (−u + 2u³ − u⁴)` con `u = φ/r`; recuperación `Δz = v̄ · T · (1 − r) · A · (w − 2w³ + w⁴)` con `w = (φ − r)/(1 − r)`; `T = 60 / max(spm_s; 12)`. Vale 0 en el catch y en el finish.

| SPM / split | `A` | Bote atrás (drive) | Bote adelante (recuperación) |
|---|---|---|---|
| 18 / 2:20 | 7,7 % | 9,0 cm | 19,6 cm |
| 24 / 2:00 | 7,1 % | 8,6 cm | 14,5 cm |
| 28 / 1:50 | 6,7 % | 8,1 cm | 12,2 cm |
| 36 / 1:40 | 5,9 % | 6,5 cm | 8,9 cm |

La cámara sigue el 30 % de este desfase (§5.2), así que el remero se ve "empujar" el bote un 70 % de lo calculado: perceptible, sin marear.

### 4.6 Cabeceo, arfada y rolido

- **Cabeceo** [CONTRATO]: `0,02 · sin(2π·φ) + 0,006 · sin(0,8·t)` rad (se evalúa con la fase visual, que en régimen es igual a la del contrato). Con D-ANI-03 el remero y los remos lo heredan.
- **Arfada** [EXR, D-ANI-04]: `y = 0,006 · sin(2π(φ − 0,08))` m.
- **Rolido** [EXR]: `0,004 · sin(0,9·t + 1,3) · k_entorno` rad, con `k` lago 0,5, río 0,8, costa 2,0, regata 0,4.
- **Mar de fondo** (costa): el casco toma la altura de las olas de vértice en proa, centro y popa: arfada = promedio; cabeceo extra = `atan((h_proa − h_popa) / 8,2)`, suavizado τ 0,3 s.

### 4.7 Pausa → inercia

Al pausar o dejar de remar: velocidad con inercia de 1,5 s y corte a 0 bajo 0,01 m/s [CONTRATO]; el desfase de *surge* escala con `v̄` y se apaga; los charcos siguen envejeciendo y quedan atrás mientras el bote se desliza; la estela se apaga con la velocidad. El tiempo de escena sigue: agua, nubes y banderas se mueven [CONTRATO].

---

## 5. Cámara

### 5.1 Persecución [CONTRATO]

Posición y lente del legacy (ART_BIBLE §8.1): 2,2 m de alto, remero (pernos) a 9,5 m, horizonte al 40 % / 42 %, nivelada con desplazamiento de lente.

### 5.2 Respiración y seguimiento [EXR]

Desfases sumados a la pose contrato (deterministas, sin ruido aleatorio por cuadro):

| Componente | Fórmula | Notas |
|---|---|---|
| Lateral x | `0,035·sin(2π·0,071t) + 0,015·sin(2π·0,173t + 1,7)` m | |
| Vertical y | `0,025·sin(2π·0,093t + 0,4)` m + 25 % de la arfada (τ 0,2 s) | |
| Adelante z | 30 % del desfase de *surge* (§4.5), filtrado τ 0,35 s | el remero se ve empujar el bote |
| Rolido | `0,18°·sin(2π·0,061t + 2,1)` | |
| Cabeceo | `0,10°·sin(2π·0,083t + 0,9)` + 15 % del cabeceo del casco (τ 0,5 s) | |
| Multiplicadores | costa ×1,8 · pausa ×0,6 · reducir movimiento ×0 | |

Límite verificable: el horizonte no se aparta más de **±3 px** de su línea contrato en 390×844 y 844×390 (las traslaciones no lo mueven; el cabeceo máx. ≈ 0,3° lo mueve ≈ 2 px en horizontal y < 1 px en vertical) y el remero no sale de la zona protegida (ART_BIBLE §5.2).

### 5.3 Movimientos cinematográficos de la cámara de persecución

| Momento | Movimiento |
|---|---|
| Entrada al inmersivo | desde 23,5 m detrás del remero, 9 m de alto y 3 m al costado, mirando al bote, hasta la pose contrato en **1,6 s**: posición por curva de Bézier con `ease.move`, rotación por slerp hasta quedar nivelada, FOV de 50° al del contrato y desplazamiento de lente de 0 al del contrato en el último 40 %. Con "reducir movimiento": fundido de 300 ms |
| Salida | sube 1,5 m y retrocede 4 m en 1,2 s (`ease.move`) mientras se funde a negro desde 0,8 s |
| Rutina completada | toma aérea lenta: sube y retrocede a 0,4 m/s durante 6 s y se detiene |

### 5.4 Cámaras alternativas, transiciones y director [DECIDIR D-ANI-07 · BKL-02]

Posiciones en ART_BIBLE §8.3 (lateral, dron, popa).

- **Transición suave** (destinos a < 25 m y < 60° de diferencia): 1,6 s; posición por Hermite con `ease.move`, rotación slerp, FOV y desplazamiento de lente interpolados.
- **Transición con "corte"** (resto): fundido a 60 % de negro en 200 ms → corte → vuelta en 350 ms.
- **Director automático** (opcional, apagado por defecto): cambia cada 45–75 s (secuencia determinista por sesión); nunca a < 6 s de un cambio de paso, en los últimos 10 s de un paso, ni con una alerta visible; vuelve a persecución 15 s antes de cada paso de trabajo; si el usuario elige una cámara, el director se detiene.

---

## 6. Mundo vivo (tiempo de escena; sigue en pausa)

| Elemento | Movimiento |
|---|---|
| Banderas de orilla | `sin(4t + semilla) · 0,2 m` en la punta [CONTRATO] → tela por vertex shader con onda viajera (2 armónicos) |
| Banderas del horizonte | `sin(3t + i) · 2 px` equivalente angular [CONTRATO] |
| Nubes | deriva por viento `t · 0,004 · v` en fracción de cielo [CONTRATO]; sin deriva por distancia (D-ART-06) |
| Estrellas | titileo `0,5 + 0,5·sin(2t + i)`, alfa `(0,4 + 0,5·titileo) · oscuridad` [CONTRATO]; visibles con ambiente ≤ 0,85 (Q-62) |
| Gaviotas (costa) | 5 [CONTRATO de cantidad]; planeo con aleteo cada 2–4 s (aleteo 3,5 Hz durante 0,8 s), giro suave |
| Árboles y juncos | balanceo por viento: copa `0,03 · sin(1,1t + x·0,13)` rad + ráfagas lentas (ruido de período 9 s) |
| Boyas | cabeceo `0,08 · sin(1,7t + semilla)` rad y arfada 0,02 m; en la costa siguen la ola |
| Faro (noche) | haz que gira 360° cada 12 s |
| Público (regata) | cada impostor alterna 2–3 cuadros de pose con período 1,5–4 s y desfase por semilla |
| Luces | encendido escalonado (§2.12) |

---

## 7. Implementación técnica

### 7.1 Reparto Core / Unity y nombres únicos (QA-P-29)

Assemblies de ARCHITECTURE §3:

- Lo puro va en `RowMate.Core`.
- Todo lo de escena (remero, bote, remos, agua, efectos, cámara) va en **`RowMate.Scene`** (`Assets/RowMate/Scene/`), que es el que referencia `Unity.Animation.Rigging` (ARCHITECTURE §14.1).
- Solo la motion de UI va en `RowMate.Presentation`. Ninguna pieza de la remada vive en Presentation.

Los nombres de esta tabla son **los únicos** y coinciden con ARCHITECTURE §2 y §14 (QA-P-29). La columna "Reemplaza a" lista los nombres viejos de LEGACY_MAPPING:99 y :113, que el Architect actualiza. Los confirma el diseño de la It. 7.

| Qué hace | Nombre | Assembly (carpeta) | Reemplaza a |
|---|---|---|---|
| Fase canónica y ramps del legacy (contrato, `stroke_cycle_test`) | `StrokeCycle` | Core (`Core/Scene/`) | — |
| Ratio drive/recuperación según el SPM (Q-70, §3.2) | `StrokeRhythm` | Core (`Core/Scene/Stroke/`) | `StrokeTiming` |
| Fase visual con seguidor suave y eventos de la pala (§3.1, §3.7) | `VisualStrokeClock` | Core (`Core/Scene/Stroke/`) | — |
| Geometría 2,5D exagerada del legacy (`seatZ`, `handle`, `bladeTip`), solo para portar `boat_painter_test`; no se usa para dibujar | `LegacyBoatGeometry` | Core (`Core/Scene/Stroke/`) | `BoatKinematics` |
| Pose 3D realista del remero y los remos (D-ART-04, §3.4–§3.6) | `ScullingKinematics` + `ScullingPose`, `BoatSpec`, `RowerSpec` | Core (`Core/Scene/Stroke/`) | — |
| Ajuste del bote a las medidas del rig (§7.4) | `RiggingFit` | Core (`Core/Scene/Stroke/`) | — |
| *Surge* del bote (§4.5) | `BoatSurge` | Core (`Core/Scene/Stroke/`) | — |
| Arfada, rolido y mar de fondo (§4.6); el cabeceo del contrato sigue en `SceneState` | `BoatMotion` | Core (`Core/Scene/Stroke/`) | — |
| Esfuerzo visual según los vatios (§3.8) | `EffortModel` | Core (`Core/Scene/Stroke/`) | — |
| Charcos del contrato (lista legacy, `scene_state_test`) | `SceneState.Puddles` | Core (`Core/Scene/`) | — |
| Charcos visuales en la punta real de la pala (D-ANI-02, §4.3) | `PuddleField` | Core (`Core/Scene/Stroke/`) | — |
| Desfases y transiciones de cámara (§5) | `ChaseCameraMotion`, `CameraBlend` | Core (`Core/Scene/Stroke/`) | — |
| Curvas, resortes y relojes de la motion de UI (§1, §2) | `Easing`, `CriticalSpring`, `DigitRoll`, `PillProgress`, `HeartbeatClock` | Core (`Core/Design/`, junto a `Tokens`, ADR 016) | — |
| Arma el personaje y escribe los targets del IK desde `ScullingPose` (§7.3) | `RowerRigDriver` | Scene (`Scene/`) | — |
| Maniquí procedural con esqueleto `mixamorig:*` y avatar armado en ejecución | `ProceduralRower` | Scene (`Scene/`) | — |
| Transforms de los remos y uniform de flexión de la caña | `OarRig` | Scene (`Scene/`) | `OarController` |
| Casco: *surge*, cabeceo, arfada y rolido aplicados a transforms (`BoatPitchPivot`, ARCHITECTURE §14.3); en el nivel Bajo, además, el óvalo de sombra del legacy | `BoatVisual` | Scene (`Scene/`) | `BoatRig`, `BoatShadow` |
| Salpicadura del catch, gotas de la recuperación y anillos (ParticleSystem) | `StrokeVfx` | Scene (`Scene/`) | `SplashVfx` |
| Charcos, estela, gotas y "calma" en la textura de interacción (Medio y Alto); en el nivel Bajo, charcos como decals simples | `WaterInteractionRenderer` | Scene (`Scene/`) | `PuddleDecals` |
| Cámara de persecución | `ChaseCameraRig` | Scene (`Scene/`) | — |
| Número tabular con rodillo de dígitos (ART_BIBLE §4.2, ARCHITECTURE §7.8) | `TabularNumber` | Presentation (`Presentation/Controls/`) | — |
| Transiciones de pestaña, pila, diálogos, píldoras y paneles (§2) | componentes de motion de UI | Presentation (`Presentation/Motion/`) | — |

Conviven dos listas de charcos: la del contrato (`SceneState.Puddles`) se conserva para sus tests legacy y no se dibuja; la visual (`PuddleField`) es la que se ve, y sus tests cubren las mismas reglas legacy (§7.5).

### 7.2 Firmas propuestas para el Architect (C# 9, sin asignaciones por cuadro)

```csharp
namespace RowMate.Core.Scene.Stroke
{
    public static class StrokeRhythm
    {
        public static double DriveSeconds(double spm);      // T_d
        public static double DriveRatio(double spm);        // r, acotado a [0,22; 0,48]
        public static double ToCanonical(double phase, double ratio);
        public static double FromCanonical(double canonical, double ratio);
    }

    [System.Flags] public enum StrokeEvents { None = 0, Catch = 1, BladeEntry = 2, BladeExit = 4, Finish = 8 }

    public sealed class VisualStrokeClock
    {
        public double Phase { get; }        // φ
        public double Canonical { get; }    // c
        public double LatchedRatio { get; } // r fijado en el catch
        public StrokeEvents Step(double dt, double contractPhase, double spm, bool active);
    }

    public readonly struct BoatSpec  { /* pernos ±0,80; y_pin 0,31; remo 2,88; interior 0,88; grip 0,82; pala 1,76; asiento 0,14; recorrido 0,62 */ }
    public readonly struct RowerSpec { /* largos de muslo, pierna, tronco, brazo, antebrazo medidos del rig */ }

    public readonly struct ScullingPose
    {
        // en coordenadas del bote (origen en el punto medio entre pernos, +z proa, +y arriba)
        // asiento, inclinación de pelvis y tronco, puños L/R (posición y giro), pies,
        // barrido, ángulo vertical y plumeo de cada remo, centro de pala, carga de la caña
    }

    public static class ScullingKinematics
    {
        public static void Evaluate(double canonical, in BoatSpec boat, in RowerSpec rower, double effort, ref ScullingPose pose);
    }

    public static class BoatSurge
    {
        public static double Amplitude(double spm, double effort);
        public static double SpeedFactor(double phase, double ratio);                   // g ∈ [−1; 1]
        public static double OffsetMeters(double phase, double ratio, double amplitude, double meanSpeed, double cycleSeconds);
    }
}
```

### 7.3 Rig: Animator con controlador de pose + Animation Rigging (QA-P-29)

**Animator** (obligatorio: Animation Rigging evalúa sus restricciones sobre el grafo del `Animator`; sin un controlador, los huesos que el rig no toca, como los dedos, quedan en T-pose):

- **Avatar Humanoid.** Personaje de Mixamo: el del FBX (*Create From This Model*, ASSET_SLOTS §2.5). Maniquí `ProceduralRower`: se arma **en ejecución** con `AvatarBuilder.BuildHumanAvatar(raíz, HumanDescription)`. `human` mapea cada hueso `mixamorig:*` al nombre de `HumanTrait.BoneName` y `skeleton` toma la T-pose generada. Se arma una vez y queda en caché; se verifica `avatar.isValid && avatar.isHuman`. Si falla, se registra un aviso y el maniquí rema igual, sin la pose de dedos.
- **Controlador de pose `RowerPose`.** Es un `RuntimeAnimatorController` en `Assets/RowMate/Art/Models/Rower/`, con **un solo estado** que reproduce `RowerGripPose.anim`: un clip Humanoid **estático de 1 cuadro** con los dedos cerrados sobre el puño (índice a meñique: músculos *1/2/3 Stretched* ≈ −0,85, *Spread* 0; pulgar: *1 Stretched* −0,2, *2/3 Stretched* −0,6, *Spread* 0,4; valores finales en el diseño de la It. 7). El resto del cuerpo queda en la pose sentada neutra, porque lo sobrescribe el rig. Como es un clip de músculos Humanoid, sirve sin *retarget* para Y Bot, el personaje realista y el maniquí. Lo genera un script de Editor (`RowerPoseClipBuilder`, en `RowMate.Editor`) con `AnimationUtility.SetEditorCurve` sobre los nombres de `HumanTrait.MuscleName`, y se versiona como `.anim` de texto.
- **Ajustes:** *Apply Root Motion* apagado, *Update Mode* `Normal`, *Culling Mode* `Always Animate` (el remero está siempre en cuadro; así no se congela si algo lo tapa).
- **Orden de armado** (`RowerRigDriver.Initialize`, una vez por personaje, al entrar al inmersivo):
  1. Instanciar el personaje del slot, o crear `ProceduralRower`.
  2. Asignar avatar y `RowerPose`, y llamar a `Animator.Rebind()`.
  3. Medir los huesos para `RiggingFit`.
  4. Crear por código el `Rig` y las restricciones de la tabla, con sus targets.
  5. Llamar a `RigBuilder.Build()`.

  Cambiar de personaje = destruir y repetir; nunca a mitad de una sesión.
- **Dependencias.** Animation Rigging 1.4.1 compila sus *Animation C# Jobs* con Burst; el paquete trae `com.unity.burst` (≥ 1.4.1) y, a través de Burst, `com.unity.mathematics`. RowMate **no escribe restricciones propias**, que sí exigirían referenciar `Unity.Burst` y `Unity.Mathematics` desde `RowMate.Scene`: solo usa las de la tabla, que se configuran con `Transform` y `float`.
- **Plan B** (solo si Animation Rigging bloquea en alguna plataforma; lo decide el Architect): IK Humanoid integrado del `Animator`, sin paquete. Usa `OnAnimatorIK` con `SetIKPosition`/`SetIKHintPosition` para manos y pies, `SetLookAtPosition` para la cabeza, `bodyPosition` para la pelvis y `SetBoneLocalRotation` para la columna, sobre el mismo controlador de pose. Los números de este documento no cambian.

Encima del `Animator`, `RigBuilder` con estas restricciones, en este orden:

| Rig | Restricción (Animation Rigging) | Huesos Mixamo | Target / hint |
|---|---|---|---|
| Pelvis | `OverrideTransform` (posición y rotación en mundo) | `mixamorig:Hips` | `SeatTarget`: asiento + desplazamiento de cadera; pitch = 60 % del tronco + 6° base |
| Columna | `OverrideTransform` en espacio local: rotación de reposo del rig × delta del Core (la compone `RowerRigDriver`) | `Spine` 15 %, `Spine1` 13 %, `Spine2` 12 % del tronco | — |
| Hombros | `OverrideTransform` | `LeftShoulder`, `RightShoulder` | protracción/retracción (§3.4) |
| Piernas | `TwoBoneIKConstraint` × 2 | `UpLeg` → `Leg` → `Foot` | `FootTarget` fijo en la pedalera; hint de rodilla 0,6 m arriba y ±0,12 m afuera |
| Brazos | `TwoBoneIKConstraint` × 2 | `Arm` → `ForeArm` → `Hand` | `GripTarget` hijo del puño del remo (posición + 25 % del plumeo); hint de codo afuera 0,35, abajo 0,25, hacia proa 0,25 |
| Cabeza | `MultiAimConstraint` | `Neck` 0,4 · `Head` 0,6 | `GazeTarget` 30 m hacia popa a la altura de los ojos; peso 0,85 |

Los remos **no** se pegan a las manos: los remos siguen la cinemática del Core y las manos siguen a los remos. Requisito de import: Rig Humanoid con *Optimize Game Objects* **desactivado** (Animation Rigging necesita los transforms; ver ASSET_SLOTS).

### 7.4 Ajuste del bote al personaje (`RiggingFit`)

Al cargar el personaje se miden los largos de sus huesos y se calcula (Core, testeable): posición de la pedalera y del recorrido del carro para que en el catch las canillas queden verticales (±8°) y los brazos estirados (≥ 97 % de extensión), y en el finish el puño quede a la altura de las costillas bajas (±3 cm) con antebrazos horizontales. Así cualquier personaje Humanoid del slot rema bien sin tocar números a mano.

### 7.5 Tests en dotnet (además de los legacy portados sin cambios)

| Módulo | Casos |
|---|---|
| `StrokeRhythm` | `r(18) = 0,315`, `r(24) = 0,373`, `r(28) = 0,399`, `r(36) = 0,420` (±0,001); topes 0,22 y 0,48; `ToCanonical(0) = 0`, `(r) = 0,40`, `(1) = 1`; monótono; ida y vuelta con `FromCanonical` |
| `VisualStrokeClock` | con SPM constante sigue a la fase contrato (error < 0,001 tras 3 s); nunca retrocede; en pausa se detiene en < 0,4 s con error final < 0,005; escalón 24→30 sin salto de velocidad (< 2 % por cuadro de 16 ms); `r` solo cambia en el catch; `BladeExit` y `BladeEntry` una vez por palada; con spm 0 termina el ciclo y queda en c = 0 |
| `ScullingKinematics` | continuidad de todas las salidas en c ∈ [0; 1) incluido el paso 0,999→0 (variación máxima por Δc = 0,001 acotada); pala enterrada (≤ −0,10) en c ∈ [0,02; 0,35] y fuera (≥ +0,06) en [0,42; 0,95]; plumeo 0 en el drive, 90° en [0,43; 0,84], ≤ 2° desde 0,955; barrido −62° / +42°; manos separadas ≥ 4 cm en altura cuando se cruzan; altura del puño derivada de la pala (tabla §3.5); secuencia piernas → espalda → brazos (tests de `stroke_cycle` aplicados a la canónica) |
| `RiggingFit` | con las medidas de Y Bot y de un rig de 1,75 m: criterios de §7.4 |
| `BoatSurge` | media de `SpeedFactor` en un ciclo = 0 (±1e-6) para cualquier `r`; `OffsetMeters` = 0 en catch y finish; tabla §4.5 (±0,2 cm); la distancia de escena del contrato no cambia |
| `EffortModel` | `e(60) = 0`, `e(300) = 1`, acotado |
| `PuddleField` | reglas legacy (QA §5): 2 por `BladeExit` (uno por pala, izquierdo primero, x < 0 y x > 0); vida 4 s; radio 0,3→0,9; **fijos en el mundo** (su z de mundo no cambia mientras el bote avanza y la cámara los pasa); no se crean en pausa. Más D-ANI-02: posición = punta de pala en el mundo al salir |
| `LegacyBoatGeometry` | `boat_painter_test` portado sin cambios: catch hacia la cámara, pala hacia la proa, lados espejados (geometría legacy, no la pose 3D) |
| `ChaseCameraMotion` | desvío del horizonte ≤ 3 px en 390×844 y 844×390; desfases acotados; "reducir movimiento" → 0 |
| `CriticalSpring`, `DigitRoll`, `PillProgress`, `HeartbeatClock` | llega al 98 % en el tiempo de la tabla §1.2; dígitos que cambian (`149→155` cambia 2); el progreso visual nunca supera el del próximo tick; período = 60/bpm y el cambio se aplica en el latido siguiente |

### 7.6 Aceptación visual (Editor, capturas y video)

1. Video de 30 s a 18, 24 y 36 spm (preset del simulador): se nota la recuperación más corta a 36 spm y el drive casi igual.
2. Cuadro a cuadro en el catch y en el finish: sin saltos de pala, manos pegadas a los puños, pies en la pedalera.
3. Pausa en mitad del drive: el remero frena en ≈ 0,25 s y el bote sigue deslizando ~4 s.
4. Charcos en pares que quedan atrás y se desvanecen; salpicadura más grande con el preset Fuerte.
5. Rendimiento: animación del remero ≤ 0,3 ms y VFX ≤ 0,4 ms en el dispositivo de referencia (ART_BIBLE §9).

---

## Cambios por QA (plan)

Correcciones por el gate de armonía del plan ([QA_PLAN_REVIEW.md](../QA_PLAN_REVIEW.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-29 (parte Designer) | §7.1: todas las piezas de escena pasan a `RowMate.Scene`; solo la motion de UI queda en Presentation (`Presentation/Motion/` y `Controls/`). Tabla de **nombres únicos**, iguales a los de ARCHITECTURE §2 y §14: `StrokeRhythm`, `LegacyBoatGeometry` (geometría legacy solo para `boat_painter_test`, separada de la pose 3D `ScullingKinematics`), `OarRig`, `BoatVisual`, `StrokeVfx` y `WaterInteractionRenderer`. La tabla indica a qué nombre de LEGACY_MAPPING reemplaza cada uno (`StrokeTiming`, `BoatKinematics`, `OarController`, `BoatRig`, `BoatShadow`, `SplashVfx`, `PuddleDecals`). `PuddleField` (visual) queda separado de `SceneState.Puddles` (contrato). §7.3: `Animator` con el controlador de pose `RowerPose` (clip Humanoid estático de agarre, generado por un script de Editor) y avatar del maniquí armado en ejecución con `AvatarBuilder`, con su orden de armado. Burst y Mathematics entran como dependencias de Animation Rigging 1.4.1 y RowMate no los referencia (sin restricciones propias). Se suma un plan B con el IK Humanoid integrado. §0.3 actualizado. §7.5: los tests de `PuddleField` cubren explícitamente "fijos en el mundo" y se suma `LegacyBoatGeometry` |
| QA-P-13 (alineación con ROADMAP §6) | §0.2 remite a los gates del ROADMAP: D-ANI-05 en la 4b; D-ANI-01 a 04 en la 7; D-ANI-06 en la 10a; D-ANI-07 en el backlog |
| ROADMAP (iteraciones nuevas) | Búsqueda y pasos de conexión BLE en la It. 10a |
| QA-P-07 y QA-P-21 (alineación con ARCHITECTURE §13) | §0.1: fuera del inmersivo cada animación pide 60 fps a `FramePacing` mientras corre (en reposo, 30 fps); "reducir movimiento" se lee de `IAccessibilitySettings` |

---

## Cambios al iniciar la It. 2

Alineación con `Core/Design/Tokens.cs` (fuente única desde la It. 1) y con la decisión de íconos. Detalle en [iterations/02-dispositivo-simulador/00-deuda-diseno.md](../iterations/02-dispositivo-simulador/00-deuda-diseno.md), 2026-10-27. No cambia ningún número de motion.

| Dónde | Qué cambió |
|---|---|
| §1.2 | Los resortes `spring.*` todavía no son tokens de `Tokens.cs`; quedan acá hasta que el Architect elija el tipo (propuesta `SpringToken.Omega`, 04-design-notes §7 de la It. 1) |
| §1.3 | Las distancias se nombran como en `Tokens.cs`: `shift.s` 8, `shift.m` 12, `shift.l` 16, `shift.xl` 24 |
| §2.1 | Cambio de pestaña: el glifo (o el ícono provisorio) cambia en el cuadro 0, junto con el comienzo del fundido de color |
| §2.9 | El corazón es `favorite` relleno en los dos estados, como el legacy; el latido escala el elemento desde su centro y vale igual para el ícono provisorio |
