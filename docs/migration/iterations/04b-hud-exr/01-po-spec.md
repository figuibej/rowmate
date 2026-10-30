# It. 4b — HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema: spec de producto

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-29 |
| Iteración | 4b · `04b-hud-exr` ([ROADMAP §2, It. 4b](../../ROADMAP.md)) |
| Estado | **Spec del PO, corregida por el gate de armonía de QA** ([lente de paridad](03-qa-harmony-paridad.md) y [lente técnica](03-qa-harmony-tecnica.md); qué cambió y por qué, en §16). Lista para que el Architect y el Designer alineen sus documentos. Antes de construir encima van las dos condiciones de la It. 4a y la deuda "al empezar la 4b" (§5.1, §12 punto 1). **Cerrada** el 2026-10-30: lo que cambió al cerrar está en §17 |
| IDs dueños | **7 canónicos**: F-WKT-13, F-WKT-14, F-WKT-15, F-WKT-16, F-WKT-17, F-WKT-19, F-WKT-21 |
| También toca | F-APP-15 (las barras del sistema; se cierra en la It. 14); F-WKT-04 (el halo de la píldora y la cuenta grande); F-WKT-09 (el sello de resultado, D-ANI-05); F-WKT-02 (P-17 de la 4a: el equipo suspendido); F-WKT-01, F-WKT-03, F-WKT-06 y F-WKT-20 (el fin de la rutina espera al diálogo: DT4a-15); F-APP-03 (el diálogo modal, la entrada y la salida); F-APP-05 (teclado, letra y movimiento en las piezas nuevas); F-APP-10 (12 claves nuevas); F-BLE-16 (la cinta, la llave y el panel con las piezas nuevas); F-HIS-20 (las bases del usuario) |
| Criterios | **101** = **61 nuevos** (WKT 56: F-WKT-13 9, -14 13, -15 6, -16 5, -17 2, -19 5, -21 13, -09 2, -02 1; APP 5: F-APP-15 4, F-APP-10 1) + **40 de iteraciones anteriores** (§5: 2 condiciones, 23 que cambian y 15 que se vuelven a verificar para cerrar las fichas). Además: **1 caso de test legacy** (LT-46, §6) y **31 capturas, 12 tomas de la demo y 8 videos** (§7) |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Las rutas `lib/...:línea`, `test/...` y `docs/...` de esta spec son relativas a ese repositorio. Cada valor se leyó en `lib/features/workout/step_pill.dart`, `immersive_workout_screen.dart`, `series_panels.dart` (reloj de pared), `workout_provider.dart`, `step_target_tracker.dart`, `workout_screen.dart`, `lib/shared/theme.dart`, `lib/l10n/app_es.arb` / `app_en.arb`, el test `test/step_pill_test.dart` y la spec `docs/superpowers/specs/2026-10-02-step-timeline-states-design.md`. El último commit del legacy (`3ae0f3a`) es justamente el que saca el marco blanco de la píldora en curso ("el relleno alcanza"). Captura de referencia: `docs/image-03.png` (HUD con rutina: la fila de píldoras, `Work`, la cuenta `1:05` y el reloj `13:54`) |
| Insumos | [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) §3 (DC-33, DC-37) y §4 (Q-01, Q-06, Q-51, Q-52, Q-53, Q-59, Q-88, Q-91); inventarios [workout-engine-hud](../../product/inventory/workout-engine-hud.md) (F-WKT-13 a -17, -19, -21; F-WKT-04 y -09 para el pulso y el sello) y [app-shell-platform](../../product/inventory/app-shell-platform.md) (F-APP-15); [spec de la It. 4a](../04a-motor-hud-funcional/01-po-spec.md) (todo lo que se cita por ID), su [REPORT](../04a-motor-hud-funcional/REPORT.md) §5.1, §6, §7, §8 y §9, su [05-qa-verification](../04a-motor-hud-funcional/05-qa-verification.md) (QA-V4a-03 a -06 y -L02), la [lente de paridad](../04a-motor-hud-funcional/05-qa-lens-paridad.md) (O-02), la [de robustez](../04a-motor-hud-funcional/05-qa-lens-robustez.md) (ROB-05) y sus [04-design-notes](../04a-motor-hud-funcional/04-design-notes.md) §17.6; [ART_BIBLE](../../design/ART_BIBLE.md) §2.2-§2.3, §3.2-§3.5, §5.2-§5.3, §10; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §0.1, §1, §2.2, §2.4-§2.9, §2.11; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) (tabla de plataforma: modo inmersivo); [ADR 017](../../architecture/adr/017-segundo-plano.md); [STATUS](../../STATUS.md) |

---

## 0. Cómo leer esta spec

### 0.1 Identificadores

- **`AC-<ÁREA>-<NN>-<nn>`**: criterio `nn` de la ficha `F-<ÁREA>-<NN>`. Los de las fichas que empezó la 4a siguen su numeración: AC-WKT-13-06 sigue a los 5 de la 4a, AC-WKT-14-07 a los 6, AC-WKT-16-02 al 1, AC-WKT-17-11 a los 10, AC-WKT-09-08 a los 7, AC-WKT-02-11 a los 10 y AC-APP-10-11 a los 10. F-WKT-15, F-WKT-19, F-WKT-21 y F-APP-15 empiezan en 01.
- **§5** repite IDs de iteraciones anteriores: las dos condiciones de la 4a, los que **cambian** por esta iteración (valen los textos de §5, que reemplazan a los de la 4a) y los que se **vuelven a verificar** porque sus fichas se cierran acá. Si fallan, se reabren acá.
- **`LT-nn`**: casos de test legacy, con la numeración de las It. 1, 2 y 4a (la 4a terminó en LT-45).
- **`CAP-nn`**, **`DEMO-nn`** y **`VID-nn`**: capturas, tomas de la demo y videos de esta iteración (§7), en `docs/migration/iterations/04b-hud-exr/snapshots/`. Las de la 4a se citan como "CAP-nn de la 4a".
- **`DV-nn`**: diferencias visibles con el legacy, a propósito (§11.1). Siguen la numeración de la 4a (terminó en DV-39).
- **`P-nn`**: preguntas abiertas de esta spec (§14). La pregunta de la suspensión del equipo de la 4a se cita siempre como **"P-17 de la 4a"**.
- **`F-*`** y **`R-4*`**: bases, rutinas y lecturas de prueba (§0.4).

### 0.2 Cómo se verifica cada criterio

Las marcas de la It. 4a:

| Marca | Qué es | Disponible |
|---|---|---|
| **[dotnet]** | `dotnet test` del Core, del motor y de los repositorios, fuera de Unity, con SQLite real, reloj, planificador y ciclo de vida de prueba | Sí |
| **[Editor-EM]** | Unity Test Framework, EditMode, Editor 6000.3.27f1 | Sí |
| **[Editor-PM]** | Unity Test Framework, PlayMode: la UI real montada, con la base, el tamaño, el idioma, los servicios, la interfaz del sistema y el ciclo de vida que pide el criterio | Sí |
| **[Captura]** | Imagen o video renderizado por Unity con la UI real (no una maqueta), revisado por QA y el Designer | Sí |
| **[Device-Win]** | Build de Windows abierto en la PC (con simulador y normal), lanzado siempre desde su carpeta de salida por defecto | Sí con Mono; IL2CPP solo con el módulo |
| **[Device-And]** | Build de Android en un teléfono 8.0+ de 64 bits | Solo con el módulo de Android y el teléfono |
| **[QA]** | Revisión estática de QA (código, base, build) | Sí |

Si falta el módulo de Android, el de IL2CPP o el teléfono, el criterio (o su parte de equipo) queda **NO VERIFICABLE** con el motivo y no frena la iteración. Nunca se da por cumplido sin verlo. Lo que pide suspender el equipo (AC-WKT-02-11) lo verifica **el usuario** en la demo: QA no suspende su equipo. Los criterios [Apple] van a la 10b (ROADMAP §4.1).

### 0.3 Notación

La de la 4a ([01-po-spec §0.3](../04a-motor-hud-funcional/01-po-spec.md)):

- Textos entre comillas invertidas: **exactos al carácter**. `·` es U+00B7 con un espacio a cada lado; `…` es U+2026; `—` es U+2014; `▲` y `▼` son U+25B2 y U+25BC; `−` en las zonas horarias es U+2212.
- Tamaños en dp. En el HUD, toda medida que viene del legacy se multiplica por la **escala del HUD** (`clamp(lado corto / 390; 0,9; 1,6)`): 1,054 en TV y TH, 0,92 en L640 y 1,6 en W1280. "Por la escala" quiere decir eso, y **"base"** es la medida antes de multiplicarla (como en las 04-design-notes): una píldora de 6 base mide 6,3 dp en TV.
- "Segundo" del motor = un paso del reloj activo de 1 s. "En marcha", "en pausa", "terminada" y "reposo" son las fases `active`, `paused`, `finished` e `idle`.
- "Aviso" = mensaje breve abajo, de 4 s, que reemplaza al visible. "Diálogo" = ventana modal centrada.
- "Reloj común" = el único reloj de pulso de la barra (600 ms de ida y 600 de vuelta, `ease.pulse`; ANIMATION_BIBLE §2.6), con su origen en el primer cuadro con "a continuación" o "por terminar" después de uno sin ninguno; su fase `p` vale 1 a los 600 ms del origen y después cada 1,2 s.
- Las claves **nuevas** del catálogo están en §11.2.

### 0.4 Estado de prueba

**Lo de la 4a** ([01-po-spec §0.4](../04a-motor-hud-funcional/01-po-spec.md)), sin cambios: cada prueba y cada captura con su base temporal (**nunca** `rower_app.db` ni `rower_app_dev.db` del usuario, AC-HIS-20-04); reloj y planificador de prueba, con la hora de pared en `2026-10-28T13:00:00.000Z` al iniciar y la sesión creada en el instante 0; el ciclo de vida de prueba (segundo plano y foco); las bases `F-VACIA`, `F-LISTA` (con `Pirámide 500`: calentamiento 600 s + serie ×4 [trabajo 500 m, vatios 150-180, split 125, progresión W +10 y split −2; trabajo 250 m, SPM 30; descanso 90 s] + enfriamiento 300 s: 14 pasos), `F-ROTA`, `F-RARA` y `F-4A` (`Prueba 4a`: calentamiento 30 s; serie ×2 [trabajo 60 s, vatios mín. 160, SPM 22, progresión SPM +2; descanso 30 s]; enfriamiento 30 s, 6 pasos y 240 s; `Corta`: serie ×2 [trabajo 10 s, SPM 18, progresión SPM +2; descanso 5 s], 30 s; `Distancia 100`; `Sin meta`); el remo `R-4` (la lectura `L(k)` en el instante `k − 0,5`: tiempo del monitor `300 + k`, distancia `1000 + 4k`, split 125, 150 W, SPM 24.0, kcal `40 + ⌊k / 10⌋`, pulso 0) y sus variantes `R-4(s)`, `R-4R`, `R-4Z` y `R-4P`; el pulsómetro de prueba; la base que falla o tarda a pedido; el simulador sin ruido (150 W y 24 spm dan split `02:13`); idioma, letra del sistema y "reducir movimiento" de prueba. Ningún doble existe en un build que se instala.

**Nuevo en la 4b:**

- **Zona horaria de prueba**: UTC−03:00, sin horario de verano. El instante `13:00:00.000Z` se ve `10:00` en el reloj de pared, así que toda toma del primer minuto muestra `10:00`. AC-WKT-19-04 la pasa a UTC−05:00 a mitad de la sesión.
- **Ciclo de vida de prueba, con suspensión del equipo**: además del segundo plano y el foco, la prueba puede avisar que el equipo se suspende en el instante `t1` y despierta en `t2`. Durante el lapso no llega ninguna lectura del remo ni del pulsómetro. El reloj monótono del equipo puede seguir contando o no durante la suspensión: se prueban **los dos casos**, porque no está medido qué hace el de Windows (QA-V4a-ROB-05).
- **Interfaz del sistema de prueba**: anota cada pedido de ocultar o mostrar las barras del sistema, con su instante.
- **Base `F-4B`** (en v8), con estas rutinas, de la más nueva a la más vieja:
  - **`Estados`**: pasos sueltos trabajo 10 s (SPM 18); trabajo 10 s (SPM 30); descanso 10 s; trabajo 20 s (SPM 18); descanso 10 s. 5 pasos, 60 s. Con `R-4`, los pasos 0 y 3 se cumplen, el 1 no, y los descansos no tienen objetivo.
  - **`Final por distancia`**: trabajo 10 s (SPM 18); trabajo 100 m (SPM 18). Con `R-4` termina a los 35 s, con el último paso por distancia y con objetivo.
  - **`Texto largo`**: calentamiento 20 s con vatios mín. 150, SPM 24 y split 125 (`>150W · 24 spm · 02:05/500m`); enfriamiento 20 s. Es la fila de información más larga: `Calentamiento` con pastilla y `Próximo: Enfriamiento`.
  - **`Serie larga`**: serie ×100 [trabajo 30 s; descanso 30 s]. 200 pasos, 6000 s.
  - **`Maratón`**: trabajo 35 999 s (`9:59:59`).
- **Remos de prueba nuevos**: **`R-4(s; w)`**, `R-4` con SPM `s` y vatios `w` (por ejemplo `R-4(23; 170)`); **`R-4X`**, los extremos FTMS de AC-WKT-17-10 de la 4a (SPM `127.5`, Watts `-32768`, Split `1092:15`, distancia `16777215`, kcal `65535`, pulso `255`).
- **Fase del pulso en las tomas**: depende solo del instante (reloj de prueba). Las tomas de los últimos segundos de un paso van en un instante con `p` = 1 (por ejemplo, `Estados` a los 47,6 s: el reloj común arranca a los 35,0, cuando el paso 3 llega a 15 s del final, y 47,6 = 35,6 + 10 × 1,2).

---

## 1. Objetivo y demo

**Objetivo.** Llevar el HUD funcional de la 4a al nivel EXR y completar la pantalla inmersiva del legacy. Llegan la barra de pasos con sus píldoras (pendiente, en curso, hecha y no cumplida) y el reloj de pared, la barra del libre con el chip de fase, y el diálogo de rutina completada, que espera al remero. También la entrada al entrenamiento como "sumergirse", la salida, el velo de pausa y las barras del sistema ocultas mientras se rema. Se suman la cuenta grande de los últimos 3 s y el sello de resultado (D-ANI-05), y se corrige lo que dejó la 4a: sus dos condiciones y su deuda.

**Demo (se le muestra al usuario antes de la It. 5).** Con el build de simulador de Windows (o el Editor con el modo simulador), con la ventana de 1280 × 720. Si Windows está en inglés, el build sale en inglés: el texto en inglés va entre paréntesis.

**(a) Entrenamiento libre: sumergirse, la hora, el chip y la pausa** (2 minutos).

1. Workout → `Entrenamiento libre` (`Free workout`). Workout se funde a negro y el HUD llega de arriba abajo en poco más de un segundo; el reloj de la banda ya corre.
2. Arriba: `Entrenamiento Libre` (`Free Workout`), la hora de la PC (`HH:mm`) con su ícono y el chip `EN MARCHA` (`ACTIVE`), verde, con un punto que late.
3. `Pausa`. El fondo se oscurece, aparece `PAUSA` (`PAUSED`), el chip pasa a `PAUSADO` (`PAUSED`), en amarillo, y el botón a `Reanudar`; los valores siguen llegando. `Reanudar`: todo vuelve.
4. *(Opcional, con el pulsómetro simulado conectado antes de empezar.)* Llave → `Simular caída del pulsómetro`: BPM se va y kcal baja deslizándose a su lugar; a los 3 s BPM vuelve y kcal sube.
5. `Terminar` → `Terminar`. El HUD se va con sus últimos valores (nunca pasa por `00:00`), la pantalla funde a negro y vuelve Workout. Ni diálogo ni aviso.

**(b) Una rutina con píldoras, sello y diálogo** (`Prueba 4a`, 4 minutos; si no está, se arma como en la demo de la 4a, [REPORT §3.1 (b)](../04a-motor-hud-funcional/REPORT.md)).

1. Con Watts en 150 (llave, `−` o `+`), `Prueba 4a` → `Iniciar`. Arriba hay 6 píldoras del mismo ancho, en amarillo, rosa, verde, rosa, verde y azul: la amarilla se va llenando de a poco. Debajo, `Calentamiento`, `0:30` y la hora.
2. En `0:15` aparece `Próximo: Trabajo` (`Up next: Work`). En `0:05` la píldora late con un halo y la cuenta crece. En `0:03` aparece en grande `3`, `2`, `1`.
3. **Trabajo 1, a 150 W**: Watts en rojo. Al terminar, por 2 s, el sello `No alcanzado` (`Target missed`) con `150 / 160 W · 24.0 / 22 spm`, y la píldora rosa queda **hueca**: tenue con un borde.
4. **En el descanso**: llave → `+` de Watts dos veces (170) y `+` de Ritmo dos veces (26).
5. **Trabajo 2**: al terminar, `Objetivo cumplido` (`Target met`) con `170 / 160 W · 26.0 / 24 spm` (con el ruido del simulador del build, valores parecidos), y la píldora queda **sólida**.
6. **Enfriamiento**: sin `Próximo`; en los últimos 3 s, la cuenta grande. Al terminar, la píldora azul queda llena, sin marcar resultado. Un instante después aparece el diálogo `¡Entrenamiento completado!` (`Workout Completed!`). El check se dibuja y los totales cuentan hasta su valor: `Tiempo` `04:00`, y la distancia y las calorías del monitor.
7. Tocar afuera o Escape no hace nada. `Terminar` (`Finish`): sale el inmersivo y vuelve Workout, sin aviso.

**(c) El equipo suspendido** (P-17 de la 4a, 3 minutos). **La verifica el usuario**: QA no suspende su equipo.

1. `Entrenamiento libre`; remar hasta que el reloj de la banda marque `01:00`.
2. Suspender Windows: Inicio → Iniciar/apagar → Suspender, o cerrar la tapa si así está configurada. Esperar 1 minuto y despertarlo.
3. El reloj sigue desde `01:00` (a lo sumo `01:01`), no desde `02:00`. El chip sigue en `EN MARCHA` y la hora del HUD es la actual.
4. Remar hasta `02:00` → `Terminar` → `Terminar`.
5. Con la herramienta de solo lectura de la 4a (`dotnet run --project tools/dotnet/RowMate.SessionList -- --variant simulator`), la sesión más nueva tiene unos 25 puntos (uno cada 5 s de los 2 minutos remados, más el final), no unos 37.

**(d) Las dos condiciones de la 4a** (1 minuto).

- En el teléfono vertical del Editor (`411 x 914`), o en CAP-13, el divisor entre el reloj y la distancia de la banda se ve.
- En la PC, después de usar el mouse, **el primer** Tab ya muestra el anillo de foco: en el HUD, en Workout y en el editor de Rutinas.

**(e) En Android** (solo con el módulo y un teléfono; si no, NO VERIFICABLE). Al entrar al entrenamiento desaparecen las barras de estado y de navegación. Si se las hace aparecer deslizando desde el borde, se vuelven a ir solas. Al salir, vuelven.

**(f) Las pruebas**: `dotnet test` y las de Unity en verde, con LT-46. La carpeta de capturas trae CAP-01 a CAP-31, la secuencia DEMO-01 a DEMO-12 y VID-01 a VID-08.

---

## 2. Alcance

| ID | Funcionalidad | Qué se cierra en la 4b | Vuelve en |
|---|---|---|---|
| F-WKT-13 | Inmersivo: estructura, capas y navegación | Completo sin la escena: entrada "sumergirse" y salida, velo de pausa, el diálogo de completado una vez por sesión, nada animando al salir, las barras del sistema (Android) | 6a (escena, vuelo de cámara, el bote que frena en pausa), 6b (vidrio con desenfoque) |
| F-WKT-14 | Barra de pasos | Completo: la fila de píldoras, el halo de los últimos 5 s, el reloj de pared, la cuenta grande de los últimos 3 s | — |
| F-WKT-15 | Píldora: estados visuales | Completo, con `step_pill_test` (LT-46) | — |
| F-WKT-16 | Barra del libre y chip de fase | Completo | — |
| F-WKT-17 | Tarjetas y banda | Completo sin el desenfoque: el divisor (QA-V4a-03), las cifras de tablero (QA-V4a-06), kcal que se desliza | 6b (vidrio con desenfoque) |
| F-WKT-19 | Reloj de pared | Completo | 6a (la hora conduce la luz de la escena: SCN) |
| F-WKT-21 | Diálogo de rutina completada | Completo sin Strava; reemplaza el aviso provisorio de la 4a | 12 (la variante con Strava: dos botones y subida) |
| F-APP-15 (también toca) | Modo inmersivo del sistema | Las barras ocultas al entrar y visibles al salir (Android); la ventana de escritorio sin cambios | 10b ([Apple]: barra de estado de iOS), 14 (cierre: orientación y ventana) |
| F-WKT-04, F-WKT-09 (también tocan) | El halo de la píldora, la cuenta grande y el sello | D-ANI-05 | — |
| F-WKT-02 (también toca) | Reloj activo | P-17 de la 4a: el tiempo con el equipo suspendido no cuenta | 10a (Android con el teléfono, RA-19), 10b ([Apple]) |

---

## 3. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero con una rutina**, quiero ver toda la rutina en una barra de píldoras que se llenan, para saber de un vistazo cuánto llevo y cuánto falta. |
| H-02 | Como **remero con objetivos**, quiero que cada paso terminado quede marcado como cumplido (sólido) o no cumplido (hueco), y ver un momento mi promedio contra el objetivo, para saber cómo me fue sin dejar de remar. |
| H-03 | Como **remero**, quiero que el final de cada paso se anuncie (la píldora late, la cuenta crece y en los últimos 3 s aparece en grande), para prepararme para el cambio sin mirar el reloj. |
| H-04 | Como **remero**, quiero ver la hora en el HUD, para no salir del entrenamiento a buscarla. |
| H-05 | Como **remero en entrenamiento libre**, quiero ver si la sesión está en marcha, en pausa o terminando, para no dudar del estado. |
| H-06 | Como **remero que terminó su rutina**, quiero un cierre con el tiempo, la distancia y las calorías que quede hasta que lo toque, para ver mi resultado aunque no estuviera mirando la pantalla. |
| H-07 | Como **remero**, quiero que entrar al entrenamiento se sienta como sumergirme (sin las barras del sistema en el teléfono) y que salir me devuelva a la app sin sobresaltos, para concentrarme en remar. |
| H-08 | Como **remero**, quiero que en pausa se note claramente que estoy en pausa, sin perder de vista las métricas. |
| H-09 | Como **usuario de la PC**, quiero que si el equipo se suspende con una sesión en marcha, ese tiempo no cuente, para que la sesión no se llene de tiempo sin remar. |
| H-10 | Como **usuario de teclado**, quiero que el primer Tab muestre dónde está el foco, para no activar algo sin verlo. |

---

## 4. Criterios de aceptación

### 4.0 Conteo

| Ficha | AC | Ficha | AC | Ficha | AC |
|---|---|---|---|---|---|
| F-WKT-13 | 9 | F-WKT-17 | 2 | F-WKT-02 | 1 |
| F-WKT-14 | 13 | F-WKT-19 | 5 | F-APP-15 | 4 |
| F-WKT-15 | 6 | F-WKT-21 | 13 | F-APP-10 | 1 |
| F-WKT-16 | 5 | F-WKT-09 | 2 | | |

Total: **61 criterios nuevos** (§4.1-§4.8) + **40 de iteraciones anteriores** (§5) = **101**.

### 4.1 F-WKT-15 — Píldora de paso: estados visuales

Los cuatro estados son la regla del legacy (`docs/superpowers/specs/2026-10-02-step-timeline-states-design.md:14-21`): **sólido = cumplido, hueco = intentado y no cumplido, tenue = falta, llenándose = en curso**. Las opacidades son sobre el color **base** del tipo: la píldora es un gráfico, no un texto (ART_BIBLE §2.3).

- **AC-WKT-15-01 · Color y forma.** *Entonces* cada píldora lleva el color base de su tipo de paso: trabajo `#EF476F`, descanso `#06D6A0`, calentamiento `#FFD166`, enfriamiento `#118AB2`; el de un tipo desconocido, `#607D8B`, no se llega a ver, porque una rutina con un tipo desconocido no carga (AC-WKT-12-06 de la 4a). Mide 8 de alto con radio 4, por la escala. Las hechas y el borde de la no cumplida llegan al contraste de los componentes gráficos (ART_BIBLE §10.1, lo mide el Designer); la pendiente es tenue a propósito. [Editor-PM] · [Captura] CAP-01 · `lib/features/workout/step_pill.dart:36`, `:57-60`; `lib/shared/theme.dart:56-69`
- **AC-WKT-15-02 · Pendiente.** *Entonces* una píldora pendiente es el color al 18 %, sin borde y sin relleno: una silueta tenue que deja ver la estructura de la rutina. [dotnet] (la regla) · [Editor-PM] · [Captura] CAP-01 · `step_pill.dart:30`, `:44-45`
- **AC-WKT-15-03 · En curso.** *Entonces* la píldora en curso es una pista del color al 30 % con un relleno sólido (al 100 %) desde la izquierda, de ancho = progreso del paso (0 a 1, acotado; AC-WKT-04-03 de la 4a). Con progreso 0 no hay relleno. No lleva borde ni **marco**: el legacy probó un marco blanco y lo sacó porque "a simple vista molesta, y el relleno avanzando ya lo identifica" (commit `3ae0f3a`). El relleno lleva el brillo fino en su borde de arriba de ART_BIBLE §3.3, que el Designer confirmó (04-design-notes §3.2): distingue una en curso llena de una hecha (CAP-06), no rodea la píldora y no se lee como un marco. Con el relleno en 0, no hay brillo. [dotnet] · [Editor-PM] · [Captura] CAP-01 · `step_pill.dart:31`, `:46-47`, `:71-76`; `docs/superpowers/specs/2026-10-02-step-timeline-states-design.md:17`
- **AC-WKT-15-04 · Hecha, cumplida o sin objetivo.** *Entonces* una píldora hecha con el objetivo cumplido, o de un paso sin objetivos, es **sólida**: el color al 85 %, sin borde. Las dos se ven idénticas. [dotnet] · [Editor-PM] · [Captura] CAP-01 · `step_pill.dart:32`, `:52-54`
- **AC-WKT-15-05 · Hecha, no cumplida.** *Entonces* una píldora hecha con el objetivo no cumplido es **hueca**: el interior al 18 % (igual que pendiente) y un borde de 1,5 (por la escala) del color al 85 %, por dentro: la píldora no cambia de tamaño. Con píldoras de menos de 6 base de ancho, el borde va como dos rieles, uno arriba y otro abajo, sin los costados, así la hueca se sigue distinguiendo de la sólida (AC-WKT-14-16, DV-40). [dotnet] · [Editor-PM] · [Captura] CAP-01, CAP-04 · `step_pill.dart:49-51`
- **AC-WKT-15-06 · Se lee sin el color.** *Entonces* los cuatro estados se distinguen por la forma (tenue, llenándose, sólida, hueca) sin depender del color: en CAP-01 pasada por la simulación de deuteranopía y de protanopía se distinguen la hecha cumplida, la no cumplida, la en curso y la pendiente (ART_BIBLE §10.3). [Captura] CAP-01 (QA)

### 4.2 F-WKT-14 — Barra de pasos de la rutina (se cierra acá)

En la 4a se hizo la fila de información (AC-WKT-14-01 a -06; los que cambian están en §5).

- **AC-WKT-14-07 · La fila de píldoras.** *Dado* una rutina, *entonces* arriba de la fila de información va la fila de píldoras, con relleno 10 / 8 / 10 / 4 (izquierda, arriba, derecha, abajo) por la escala, y píldoras de 8 de alto. La fila ocupa todo el ancho de la barra menos su relleno. [Editor-PM] · [Captura] CAP-01, CAP-24 · `immersive_workout_screen.dart:359-362`
- **AC-WKT-14-08 · Una por paso, todas iguales (Q-59).** *Entonces* hay una píldora por paso de la rutina **expandida**, en su orden, todas del mismo ancho (± 1 px por el redondeo) sin importar su duración, su distancia ni su tipo, separadas 3 dp por la escala. No se desplazan ni se recortan. La única que se dibuja distinta es la en curso cuando las píldoras miden menos de 6 base: va como un **pulgar** sobre su lugar, y su lugar mide lo mismo que los demás (AC-WKT-14-16, DV-40). Q-59 sigue igual: ninguna píldora es proporcional a su duración.
  - `Prueba 4a`: 6 píldoras (amarilla, rosa, verde, rosa, verde, azul).
  - `Pirámide 500`: 14, y la del calentamiento de 600 s mide lo mismo que la del trabajo de 250 m.

  Según el índice: antes del paso en curso, **hechas**, con su resultado (AC-WKT-09-07 de la 4a); la del paso en curso, **en curso**, con su progreso; después, **pendientes**. [dotnet] (el estado de cada píldora) · [Editor-PM] · [Captura] CAP-01, CAP-05, CAP-24 · `immersive_workout_screen.dart:338`, `:363-375`; `step_pill.dart:103`
- **AC-WKT-14-09 · Qué se llena.** *Entonces* solo se llena la píldora de un paso por tiempo con duración mayor que 0. La de un paso por distancia o sin meta, y la de uno de 0 s o de duración negativa, queda en curso con la pista sola (Q-51; AC-WKT-04-03 de la 4a). [Editor-PM] · [Captura] CAP-05, CAP-12 · `workout_provider.dart:31-36`
- **AC-WKT-14-10 · Relleno continuo, sin adelantarse (mejora EXR).** *Entonces* entre un segundo y el siguiente el relleno avanza sin saltos, de forma continua. Nunca pasa del progreso del segundo siguiente y nunca retrocede: al reanudar sigue desde donde quedó. En pausa se congela. Al volver del segundo plano, o con un cuadro tarde, el primer cuadro muestra el progreso presente, sin recorrerlo (ANIMATION_BIBLE §2.6). El legacy salta de a un segundo. [Editor-PM] · [Captura] VID-04
- **AC-WKT-14-11 · El halo de los últimos 5 s (F-WKT-04).** *Entonces*, cuando el paso está por terminar (restante de 1 a 5 s, AC-WKT-04-04 de la 4a), la píldora en curso suma un halo de su color, **solo por fuera**: la pista y el relleno no cambian. El halo va del 30 % al 70 % con el reloj común, el mismo de `Próximo` y de la cuenta. Junto al borde de la píldora se ve como el del legacy, que es una sombra de desenfoque 8, expansión 2 y radio 5 por la escala (ahí deja ≈ 0,65 de su intensidad), y se apaga antes del borde de su fila. La forma la fija el Designer (04-design-notes §3.2).
  - Ninguna otra píldora lleva halo, ni la en curso fuera del "por terminar".
  - En pausa sigue latiendo (paridad), y con reducir movimiento queda fijo.
  - El halo nunca tapa la fila de información ni pasa por debajo de ella.
  - Diferencias del sistema visual (§11.1.1): en el legacy la sombra también se pinta debajo de la píldora y se ve a través de la pista, que se aclara con el pulso; en la app la pista queda al 30 %, así el avance del relleno se sigue leyendo en los últimos 5 s. El halo del legacy, además, se extiende ≈ 14 dp y pasa por debajo de la fila de información.

  [Editor-PM] · [Captura] CAP-02, CAP-25 · `immersive_workout_screen.dart:372`; `step_pill.dart:82-101`; `workout_provider.dart:39-41`
- **AC-WKT-14-12 · El cambio de paso.** *Entonces*, en el segundo en que el motor cambia de paso, la píldora que termina pasa a hecha con su resultado y la siguiente a en curso; lo que se anima es solo el aspecto (ANIMATION_BIBLE §2.7):
  - la que termina completa su relleno;
  - si se cumplió o no tenía objetivo, se solidifica con un destello corto;
  - si no se cumplió, su borde se dibuja alrededor;
  - la siguiente empieza a llenarse.

  Nada salta ni se corre de lugar, y no aparece ningún marco. [Editor-PM] · [Captura] VID-04
- **AC-WKT-14-13 · El último paso queda en curso (Q-52).** *Entonces*, con la rutina terminada y hasta que se cierra el inmersivo (después del diálogo de completado: AC-WKT-21-08), la **última** píldora queda **en curso**: llena si el paso es por tiempo (progreso 1) y con la pista sola si es por distancia. Nunca muestra su resultado, aunque el motor lo calculó (AC-WKT-09-07 de la 4a). Las demás quedan hechas con su resultado.
  - `Corta` con `R-4`: sólidas la 0 y la 2 (rosas, cumplidas) y la 1 (verde, sin objetivo), y la 3 llena.
  - `Final por distancia` con `R-4`: la 0 sólida y la 1 con la pista sola (cumplida, pero no se muestra).

  [dotnet] (la regla) · [Editor-PM] · [Captura] CAP-06, CAP-12 · `workout_provider.dart:203-216`; `immersive_workout_screen.dart:367-371`
- **AC-WKT-14-14 · El reloj de pared en la barra (DV-46).** *Entonces* a la derecha de la cuenta regresiva, a 10 por la escala (6 cuando la fila no entra: AC-WKT-14-15), va el reloj de pared sin marco (F-WKT-19). Es el último elemento de la fila de información. La cuenta y el reloj van **pegados al borde derecho** de la fila, con pastilla o sin ella y con `Próximo` o sin él, y no se mueven cuando entra `Próximo` ni al pasar a un paso con o sin pastilla. En el legacy, con pastilla, quedan corridos hacia el centro y saltan (DV-46). [Editor-PM] · [Captura] CAP-01, CAP-24 · `immersive_workout_screen.dart:397-416`, `:472-474`
- **AC-WKT-14-15 · Cuando la fila no entra (DV-45).** *Entonces* la fila de información nunca sale de la barra ni encima sus piezas. El tipo, `Próximo:` con su tipo, la cuenta y el reloj **nunca se cortan**. Lo que cede, en este orden:
  1. la pastilla de objetivo pierde fragmentos enteros, de atrás para adelante (AC-WKT-14-03);
  2. para que entre al menos su primer fragmento, los aires de `Próximo` a la cuenta y de la cuenta al reloj bajan de 8 y 10 a 6, y después el relleno de `Próximo` baja de 7 a 4 por lado (AC-WKT-14-04, -14);
  3. si ni así entra el primer fragmento, la pastilla no se muestra. Los objetivos que cuentan siguen a la vista en sus tarjetas (`obj.`, AC-WKT-10-05 de la 4a).

  **Nada salta.** Cómo queda la fila se decide al empezar cada paso, con todo lo que ese paso va a mostrar: el tipo, la pastilla en su mínimo (el primer fragmento con `…`, o la pastilla entera si tiene uno solo), `Próximo` si lo va a tener, la cuenta y el reloj. Se vuelve a decidir si cambian la ventana o el idioma. Dentro del paso, cuando entra `Próximo`, lo único que cambia es la pastilla: pierde fragmentos o, si no entra junto a `Próximo`, se va con un fundido y no vuelve en ese paso. Si los aires cambian de un paso al siguiente, cambian con el resto del cambio de paso, sin saltos.
  - *Dado* `Texto largo` en TV y en español: a los 2,2 s (18 s del final, todavía sin `Próximo`) se ven `Calentamiento`, la pastilla `>150W · 24 spm…` con el `…` blanco, `0:18` y `10:00`, con los aires en 6 (CAP-31). A los 8,2 s se ven enteros `Calentamiento`, `Próximo: Enfriamiento`, `0:12` y `10:00`, con los aires en 6 y `Próximo` con relleno 4 × 3, y **sin la pastilla**, que se fue al entrar `Próximo` (CAP-19). Ese paso no entra ni siquiera sin pastilla con los aires normales.
  - En inglés, a los 8,2 s: `Warmup`, `Up next: Cooldown`, `0:12` y `10:00`, con los aires normales y sin la pastilla, porque ni `>150W…` entra junto a `Up next` (CAP-20).
  - `Estados` a los 40,2 s en TV: `18 spm`, de un solo fragmento, entra entera con los aires en 6 (CAP-01).
  - En TH, L640 y W1280 todo entra con los aires normales y la pastilla entera.

  El `…` va en el color del texto de la pastilla (blanco), no en el cian del ícono de tendencia (04-design-notes de la 4a §17.6). Los anchos de cada caso están en las 04-design-notes §3.3, y WP-B1 los vuelve a medir en Unity. El legacy reparte el espacio de otra forma: la pastilla recibe a lo sumo la mitad del espacio libre y se corta por carácter, sin ocultarse nunca (DV-45). [dotnet] (la regla, con los anchos) · [Editor-PM] (el corte y el `…` blanco) · [Captura] CAP-01, CAP-19, CAP-20, CAP-31 · `immersive_workout_screen.dart:394-416`
- **AC-WKT-14-16 · Muchos pasos (DV-40).** *Entonces*, con cualquier cantidad de pasos que el editor deje guardar (hasta 999 repeticiones, Q-26), la fila muestra **todos** los pasos en orden, cada uno en un lugar del mismo ancho, sin desplazarse, sin salir de la barra y sin encimarse con la fila de información. Si no entran con su separación, la separación se achica (de 3 a 1; lo fija el Designer) y, en el extremo, la fila se ve como una franja continua donde cada tramo conserva el color y el estado de su paso. Se distinguen los hechos, el en curso y los pendientes, nunca con un marco blanco (AC-WKT-15-03).

  **Píldoras angostas.** Cuando miden menos de 6 base, en cualquiera de esos casos (en TV y en un teléfono de 360, desde 50 pasos; en TH, desde 118; en L640, desde 93; en W1280, desde 108):
  - la en curso se dibuja como un **pulgar**, más ancho y más alto que las demás (al menos 4 × 10 base; 04-design-notes §3.2), centrado en su lugar y por encima de sus vecinas, con su pista y su relleno. Es la marca de "acá estás" y no es un marco: no rodea nada;
  - la hueca lleva dos rieles, arriba y abajo, en lugar del borde entero (AC-WKT-15-05);
  - al cambiar de paso, el pulgar pasa a la píldora siguiente sin saltos.

  Ejemplos:
  - *Dado* `Serie larga` (200 pasos) en el paso 100, en TV, *entonces* se ve la franja con el pulgar (CAP-22).
  - En el legacy, con más de ≈ 120 pasos en un teléfono de 411, la separación se come todo el ancho, las píldoras miden 0 y la fila queda vacía.

  [dotnet] (los lugares y el pulgar) · [Editor-PM] · [Captura] CAP-22 · `immersive_workout_screen.dart:361-375`
- **AC-WKT-14-17 · Sin interacciones.** *Entonces* la barra de pasos no responde a toques ni clics, y sus píldoras y su reloj no se enfocan con Tab: el orden de Tab del HUD es el de la 4a. [Editor-PM] · `immersive_workout_screen.dart:348-482`
- **AC-WKT-14-18 · La cuenta grande de los últimos 3 s (D-ANI-05; F-WKT-04).** *Dado* un paso por tiempo (también el último), *cuando* su restante es 3, 2 o 1 s, *entonces* aparece la cuenta grande con el restante en segundos, sin ceros ni dos puntos (`3`, `2`, `1`):
  - centrada en el ancho de la pantalla, en el lugar que fije el Designer, fuera de la zona del bote, y sin tapar la barra, ninguna tarjeta, los controles ni el sello;
  - más grande que cualquier valor del HUD (al menos el tamaño del de SPM) y con el contraste de los valores del HUD (R-10);
  - el número cambia en el mismo cuadro que la cuenta de la barra y que el reloj de la banda;
  - se va con un fundido corto al cambiar de paso o al terminar la rutina.

  No aparece:
  - en pasos por distancia o sin meta;
  - en pasos de 0 s o de duración negativa (su restante es 0);
  - en el libre;
  - en pausa: ahí va `PAUSA` (AC-WKT-13-12), y la cuenta vuelve al reanudar si corresponde.

  Al volver del segundo plano, el primer cuadro la muestra si corresponde, sin animar. Con reducir movimiento, el número cambia con un fundido. `Estados`: de los 7 a los 10 s, de los 17 a los 20, de los 27 a los 30, de los 47 a los 50 y de los 57 a los 60. `Final por distancia`: de los 7 a los 10 s, y nunca en el paso de 100 m. Si el usuario rechaza D-ANI-05, no existe (queda la cuenta de la barra). [dotnet] (cuándo y qué número) · [Editor-PM] · [Captura] CAP-02, CAP-25, CAP-29, VID-04, VID-05
- **AC-WKT-14-19 · Fluidez con muchos pasos (R4a-03).** *Dado* `Serie larga`, *entonces* el HUD sigue sin tirones en el cambio de paso, con el halo, con el sello y con la cuenta grande, y no reserva memoria por cuadro, como el de la 4a. En la PC, ningún cuadro tarda más de 33 ms durante esos momentos en el build de simulador. [Editor-PM] (medición) · [Device-Win]

### 4.3 F-WKT-19 — Reloj de pared

- **AC-WKT-19-01 · Formato y aspecto.** *Entonces* el reloj de pared muestra la hora local del equipo en `HH:mm`, siempre de 24 h, aunque el sistema use 12 h (AC-WKT-11-12 de la 4a; Q-06): 09:05 → `09:05`; 13:54 → `13:54`; 00:00 → `00:00`. Lleva el ícono `schedule` de 16 en blanco al 70 %, 5 de separación y el texto en blanco, con cifras tabulares. El texto es del mismo tamaño que la cuenta regresiva de la barra, 20 por la escala (AC-WKT-14-05 de la 4a); en el legacy los dos miden 18 (`series_panels.dart:99`). No cambia de tamaño cuando la cuenta crece por terminar. [Editor-PM] · [Captura] CAP-01, CAP-13 · `series_panels.dart:41-47`, `:92-103`
- **AC-WKT-19-02 · Cambia al minuto.** *Entonces* el minuto que se ve cambia a más tardar 1 s después del cambio de minuto real. Entre un cambio y otro, el reloj no se redibuja ni anima: no "late" cada segundo. Con la zona de prueba (UTC−03:00), a los `13:00:59.5Z` dice `10:00` y desde los `13:01:01.0Z`, `10:01`. El cambio de cifra usa el rodillo del HUD (ANIMATION_BIBLE §2.4). [dotnet] (la regla, con el reloj de prueba) · [Editor-PM] · `series_panels.dart:72-82`
- **AC-WKT-19-03 · Su propio ritmo.** *Entonces* el reloj de pared anda solo: sigue en pausa, con la sesión terminada y con el diálogo de completado abierto, y no depende del reloj de la sesión ni lo mueve. Muestra la hora real del equipo, no la de la escena: `Hora escena` del simulador no lo cambia. Al volver del segundo plano o al despertar el equipo (AC-WKT-02-11), el primer cuadro ya muestra la hora actual. [Editor-PM] · `series_panels.dart:57-82`
- **AC-WKT-19-04 · Cambio de hora del equipo.** *Cuando* cambia la hora del equipo en una sesión en marcha (cambio de horario, de zona o a mano; con la zona de prueba pasada de UTC−03:00 a UTC−05:00), *entonces* el reloj de pared muestra la hora nueva a más tardar 1 s después (`10:00` → `08:00`). El reloj de la sesión, la cuenta regresiva, los pasos y los puntos no cambian: cuentan el tiempo remado, no la hora (paridad: el legacy cuenta disparos de un temporizador). [dotnet] · [Editor-PM]
- **AC-WKT-19-05 · Dónde va.** *Entonces* va sin marco dentro de la barra de pasos (AC-WKT-14-14) y de la barra del libre (AC-WKT-16-02). La variante con marco del legacy no se usa en ninguna pantalla y no se migra. No responde al toque ni se enfoca. [Editor-PM] · `series_panels.dart:104-110`; `immersive_workout_screen.dart:474`, `:515`

### 4.4 F-WKT-16 — Barra superior del entrenamiento libre y chip de fase (se cierra acá)

- **AC-WKT-16-02 · Contenido.** *Dado* un entrenamiento libre, *entonces* la barra del libre (la de AC-WKT-16-01, §5) lleva, de izquierda a derecha y por la escala:
  1. el ícono `rowing` de 20 en `#00B4D8`;
  2. 8 de separación y `hudFreeTitle` (`Entrenamiento Libre` / `Free Workout`) de 16/600, en blanco;
  3. un espacio flexible;
  4. el reloj de pared sin marco (F-WKT-19);
  5. 10 de separación y el chip de fase.

  No responde al toque. [Editor-PM] · [Captura] CAP-13, CAP-18 · `immersive_workout_screen.dart:505-519`
- **AC-WKT-16-03 · Chip de fase (DV-44).** *Entonces* el chip dice, según la fase (el color de la fase es el del tinte y el borde):
  - en marcha, `hudPhaseActive` (`EN MARCHA` / `ACTIVE`), en `phase.running` `#06D6A0`;
  - en pausa, `hudPhasePaused` (`PAUSADO` / `PAUSED`), en `phase.paused` `#FFD166`;
  - terminada, `hudPhaseFinished` (`FINALIZADO` / `FINISHED`), en `phase.finished` `#EF476F`.

  Lleva fondo del color al 15 % y borde del color al 50 %, en el color base, radio 8, relleno 8 × 3 y texto de 11 con tracking 0,8, todo por la escala. El texto va en el peso 700 de la letra de los textos de la app, que no tiene 800 (con el tracking se ve como el 800 del legacy: sistema visual, §11.1.1), y en la variante legible del mismo matiz, que llega a 4,5:1 sobre su tinte (regla de DV-12; lo mide el Designer, 04-design-notes §2.1 y §8.1):
  - `EN MARCHA` en `#06D6A0` (5,324:1);
  - `PAUSADO` en `#FFD166` (6,432:1);
  - `FINALIZADO` en **`#FF7A92`** (`phase.finished.hud`, 4,677:1; DV-44). El legacy usa `#EF476F`, y `#FF5D7F` no llega (3,942:1).

  El chip cambia en el mismo cuadro que la fase (`Pausa` y `Reanudar` actúan en el acto, AC-WKT-20-02 de la 4a), con un cruce de color corto. Los textos salen del catálogo, con el español del legacy, que los tiene fijos (Q-01). [dotnet] (texto y color por fase) · [Editor-PM] · [Captura] CAP-13, CAP-14, CAP-15, CAP-18 · `immersive_workout_screen.dart:524-550`
- **AC-WKT-16-04 · `FINALIZADO` y el `—`.** *Dado* un entrenamiento libre con la base lenta (cada escritura tarda 2 s), *cuando* se confirma `Terminar`, *entonces* hasta que sale el inmersivo el chip dice `FINALIZADO`, porque la fase es terminada mientras se guarda. Los controles quedan al 0,38, `Terminar` muestra su indicador (AC-WKT-20-06 de la 4a) y no hay diálogo de completado (AC-WKT-21-07). El chip `—` del legacy nunca se ve: es la fase de reposo, que el legacy llega a mostrar mientras la página sale (`:534`), y en la app la salida conserva la última foto (AC-WKT-13-08; DV-41). [Editor-PM] · [Captura] CAP-15
- **AC-WKT-16-05 · El punto en vivo (mejora EXR).** *Entonces* en `EN MARCHA` el chip lleva, antes del texto, un punto de 6 dp por la escala en `#06D6A0`, que late suave. En `PAUSADO` y `FINALIZADO` no hay punto. Con reducir movimiento, el punto queda quieto (ART_BIBLE §3.3). [Editor-PM] · [Captura] CAP-13
- **AC-WKT-16-06 · Nada se encima (DV-47).** *Entonces*, en TV, TH, L640 y W1280, en es y en, el ícono, el título, el reloj y el chip entran en la barra sin encimarse. El reloj y el chip quedan siempre enteros. Si el título no entra:
  1. se achica, con una escala pareja y en el acto, hasta 0,8 de su tamaño;
  2. solo si ni así entra, se recorta con `…`.

  En TV y en un teléfono de 360, en español, `Entrenamiento Libre` entra entero, achicado a ≈ 0,8 (0,805 medido en Unity con el chip a su ancho; 04-design-notes §3.5 y §16); recortado se leería `Entrenamient…`. En inglés y en TH, L640 y W1280 entra a su tamaño. En el legacy el título no se achica ni se recorta: en 411 entra justo y en 390 y 360 se sale de la barra y empuja afuera el reloj y el chip (DV-47). La cinta `SIMULADOR` tapa solo el comienzo de la barra (AC-BLE-16-01). [Editor-PM] · [Captura] CAP-13, CAP-16, CAP-17, CAP-18 · `immersive_workout_screen.dart:505-519`

### 4.5 F-WKT-21 — Diálogo de rutina completada

- **AC-WKT-21-01 · Cuándo aparece.** *Cuando* la rutina termina sola (AC-WKT-03-05, §5), *entonces*, a los 300 ms del fin (ANIMATION_BIBLE §2.11; el legacy, en el cuadro siguiente), aparece el diálogo de rutina completada, **una sola vez** por sesión (AC-WKT-13-11). No espera al guardado, que corre en paralelo (paridad: el legacy avisa a la pantalla en el mismo segundo y guarda después, `workout_provider.dart:189`, `:210-216`). Nunca aparece al terminar a mano (AC-WKT-21-07) ni en el libre, que no termina solo. [dotnet] (la regla de "una vez") · [Editor-PM] · [Captura] CAP-07, VID-05 · `immersive_workout_screen.dart:29`, `:49-55`
- **AC-WKT-21-02 · Modal.** *Entonces* el diálogo va sobre el velo y solo lo cierra su botón: tocar afuera, Escape y "Atrás" de Android no hacen nada. Detrás queda el HUD con la rutina terminada (AC-WKT-14-13; AC-WKT-03-06 de la 4a), y sus controles no responden. La cinta y la llave del simulador quedan por encima del velo. [Editor-PM] · [Device-And] ("Atrás") · `immersive_workout_screen.dart:207-212`
- **AC-WKT-21-03 · Qué dice.** *Entonces* el diálogo tiene, de arriba abajo:
  - el ícono `check_circle` en `#06D6A0` y el título `workoutCompleted` (`¡Entrenamiento completado!` / `Workout Completed!`);
  - el texto `workoutCompletedMessage` (`Has completado tu rutina exitosamente.` / `You have successfully completed your routine.`);
  - la caja del resumen, con tres filas en este orden. Cada fila lleva su ícono en `#00B4D8`, la etiqueta en texto secundario y el valor en negrita:
    - `timer` y `workoutCompletedTime` (`Tiempo` / `Time`), con el reloj activo en `H:MM:SS` o `MM:SS` (AC-WKT-11-06 de la 4a);
    - `straighten` y `workoutCompletedDistance` (`Distancia` / `Distance`), con `{n} m`, sin separador de miles (AC-WKT-11-15);
    - `local_fire_department` y `workoutCompletedCalories` (`Calorías` / `Calories`), con las kcal, sin unidad;
  - el botón `workoutFinish` (`Terminar` / `Finish`), relleno primario.

  `Corta` con `R-4` da `00:30`, `1120 m` y `43`. Las medidas, el aspecto (opaco o de vidrio, P-08) y el tamaño del check los fija el Designer (ART_BIBLE §3.5); los textos que el legacy tiene fijos en español (`Tiempo`, `Distancia`, `Calorías`) salen del catálogo con el mismo español (Q-01). [Editor-PM] · [Captura] CAP-07, CAP-08 · `immersive_workout_screen.dart:213-229`, `:959-1019`
- **AC-WKT-21-04 · Los valores, fijos.** *Entonces* el tiempo es el reloj activo al terminar, y la distancia y las kcal son las del monitor en el instante en que terminó la rutina: las del punto final (AC-WKT-06-06 de la 4a), no los totales guardados. No cambian mientras el diálogo está abierto, aunque lleguen lecturas nuevas o se rote la pantalla. Valen también si la rutina terminó en segundo plano (AC-WKT-21-09). El legacy los lee **una sola vez**, cuando arma el diálogo, un cuadro después del fin, y después no los cambia: ni con lecturas nuevas, ni al rotar, ni al cambiar el tamaño de la ventana. Si la rutina termina con la app en segundo plano en Android, arma el diálogo recién al volver, con la lectura de ese momento (DV-43). [dotnet] · [Editor-PM] · `immersive_workout_screen.dart:965-991`
- **AC-WKT-21-05 · Entra con peso (mejora EXR).** *Entonces*, al aparecer, el check se dibuja y los tres totales cuentan de 0 a su valor en 700 ms, escalonados 120 ms, y terminan **siempre** en el valor exacto, con el formato del legacy. El tiempo cuenta en su formato (`00:00` … `00:30`). Son los únicos números que cuentan (ANIMATION_BIBLE §2.5). Con reducir movimiento, el diálogo entra con un fundido, el check ya dibujado y los valores finales desde el primer cuadro. Al volver del segundo plano el diálogo ya está, sin la entrada (AC-WKT-21-09). [Captura] VID-05 · [Editor-PM]
- **AC-WKT-21-06 · `Terminar`.** *Cuando* se toca `Terminar` en el diálogo, *entonces* el diálogo sale, la sesión vuelve a reposo (AC-WKT-01-07 de la 4a: se borran la rutina, los relojes y los resultados) y el inmersivo sale (AC-WKT-13-08): se ve Workout en reposo, sin aviso.
  - **Si el cierre de la sesión todavía no volvió**: `Terminar` queda tomado (no responde a otro toque y, si tarda más de 300 ms, muestra el indicador, como `Terminar` del HUD) y se vuelve a reposo cuando el cierre vuelve. Tocar rápido nunca pierde el cierre.
  - **Si el cierre falló**, o no volvió a los 15 s de primer plano contados desde el fin de la rutina y no desde el toque (DT4a-13, DT4b-09): se vuelve a reposo igual y Workout muestra el aviso `workoutSaveError` (`No se pudo guardar la sesión` / `Couldn't save the session`). Un cierre que tardó más de 15 s pero volvió bien antes del toque cuenta como guardado: no hay aviso.
  - **Si la app se cierra con el diálogo abierto**: la sesión queda como la dejó el cierre (cerrada, si el cierre ya había vuelto; cortada, si no: AC-WKT-06-10) y, al abrir de nuevo, no aparece nada (paridad).

  En el legacy, `Terminar` llama a `reset()` sin esperar: si el guardado no había terminado, el cierre falla en silencio y la sesión queda sin fin ni totales (`workout_provider.dart:283-299`, con `_startedAt` ya vacío; DV-42). [dotnet] · [Editor-PM] (con la base lenta y con la que falla al cerrar) · [Captura] CAP-28 · `immersive_workout_screen.dart:261-289`
- **AC-WKT-21-07 · Nunca al terminar a mano (Q-53).** *Cuando* se confirma `Terminar` en el HUD, *entonces* en ningún cuadro aparece el diálogo de completado ni nada de "completado". Vale en marcha, en pausa, con la base lenta y con lecturas del monitor que llegan durante el guardado. En el legacy, una lectura que llega mientras se guarda redibuja la pantalla con la fase terminada y el diálogo puede asomar un instante (`immersive_workout_screen.dart:49-55`, `:141-147`). [dotnet] · [Editor-PM] (con la base lenta y el remo mandando lecturas)
- **AC-WKT-21-08 · El motor espera al diálogo (DT4a-15).** *Entonces*, desde el fin de la rutina hasta `Terminar` del diálogo, la sesión sigue terminada, también después de que vuelve el cierre. El inmersivo no se cierra solo. Mientras tanto:
  - quedan las píldoras con sus resultados (AC-WKT-14-13), el último paso con `0:00` o su largo (AC-WKT-03-06 de la 4a) y el reloj de la banda en el valor final;
  - los valores en vivo del HUD siguen cambiando detrás del velo con las lecturas del monitor (AC-WKT-17-06), y el reloj de pared sigue;
  - no se toma ningún punto más.

  Recién `Terminar` borra los resultados y vuelve a reposo. En la 4a el motor volvía solo a reposo al terminar el guardado (DT4a-15, provisorio); el legacy espera al `reset()` del diálogo (`immersive_workout_screen.dart:235`, `:255`, `:287`). [dotnet] · [Editor-PM]
- **AC-WKT-21-09 · Termina en segundo plano o minimizada.** *Dado* `Corta` con `R-4`, *cuando* la app pasa a segundo plano a los 8 s y vuelve a los 40 s (la rutina terminó a los 30), *entonces*:
  - la sesión se cerró como en primer plano, con los 7 puntos y la hora de fin del segundo 30 (AC-WKT-02-06, §5);
  - al volver se ve el inmersivo con la rutina terminada y el diálogo **ya abierto**, sin su entrada, con `00:30`, `1120 m` y `43`, una sola vez y sin avisos.

  En la PC, con la ventana minimizada o sin foco, pasa lo mismo al restaurarla. El legacy también: su diálogo es modal y queda hasta que se lo toca (`immersive_workout_screen.dart:207-212`). [dotnet] (ciclo de vida de prueba) · [Editor-PM] · [Device-Win] (minimizada)
- **AC-WKT-21-10 · Strava, hasta la It. 12.** *Entonces* Strava se comporta como no configurado (un estado válido del legacy), y con el simulador nunca existe (R-01): el diálogo tiene un solo botón, `workoutFinish`, y no se sube ni se pregunta nada. Son de la It. 12 la variante de dos botones (`workoutFinish` con contorno y `profileUploadToStrava` con el ícono de nube) y la subida automática con sus avisos (F-WKT-22, F-STR-06; Q-53 y Q-54). [Editor-PM] · [QA] · `immersive_workout_screen.dart:198-205`, `:230-292`
- **AC-WKT-21-11 · Teclado.** *Entonces*, cuando el diálogo aparece, ningún control queda enfocado: el legacy tampoco enfoca nada. El primer Tab enfoca `Terminar` con su anillo, también si antes se usó el mouse (AC-APP-05-07, §5). Enter o Espacio lo activan, Tab no sale del diálogo y Escape no hace nada (AC-WKT-21-02). [Editor-PM] · [Device-Win]
- **AC-WKT-21-12 · Tamaños y letra.** *Entonces*, en TV, TH, L640 y W1280, en es y en, con la letra del sistema a 1,3× y con los valores más largos (`Maratón` con `R-4X`: `9:59:59`, `16777215 m`, `65535`), nada del diálogo se corta ni sale de la pantalla. Crece con la letra del sistema como los otros diálogos de la app (AC-APP-05-05, §5), lo que no entra se desplaza dentro del diálogo y `Terminar` queda siempre a la vista. El título, si no entra en una línea, se parte en dos junto al check (en TV, en español: `¡Entrenamiento` / `completado!`); en el legacy, en un teléfono, se sale del diálogo (DV-47). [Editor-PM] · [Captura] CAP-07, CAP-09, CAP-10, CAP-11
- **AC-WKT-21-13 · El motor, igual que el legacy (QA-V4a-L02).** *Entonces*, con la espera del diálogo, el fin automático queda como en el legacy: terminada hasta el `reset()`. Así el oráculo del motor compara también lo que pasa después del fin. Además corren contra el motor del legacy los guiones de QA-V4a-L02:
  - pasos de 0 s y de duración negativa;
  - un paso sin meta y `Terminar`;
  - terminar en pausa;
  - el último paso por distancia;
  - el pulso del sensor;
  - pausas que cruzan un cambio de paso;
  - dos sesiones seguidas.

  Dan los mismos puntos, totales, cambios de paso, resultados de objetivos e instante del fin, salvo las diferencias ya aceptadas (DV-26, DV-37). [dotnet] (oráculo, con `run-oracle.ps1 -Engine` y el `<flutter-sdk>` del legacy)

### 4.6 F-WKT-13 — Pantalla inmersiva: estructura, capas y navegación (se cierra acá)

- **AC-WKT-13-06 · Entrada: "sumergirse" (mejora EXR).** *Cuando* se toca `Entrenamiento libre` o `Iniciar`, *entonces*:
  1. Workout funde a negro (220 ms) y con él se ocultan las barras del sistema (Android, AC-APP-15-01);
  2. aparece el fondo;
  3. el HUD llega en cascada, de arriba abajo: la barra superior a los 400 ms, SPM a los 520, Split y Watts a los 600, la banda a los 680, kcal y BPM a los 760 y los controles a los 840, cada uno en 280 ms y con una subida corta (≤ 12 dp; ANIMATION_BIBLE §2.11, sin el vuelo de cámara, que es de la 6a);
  4. la llave del simulador entra al terminar.

  A los ≈ 1,1 s del toque el HUD está entero. La sesión y el reloj corren desde el toque (AC-WKT-01-02 de la 4a): la banda entra mostrando el reloj real, y nada de la entrada espera a la base. Con reducir movimiento, el fundido de 120 lineal de la 4a, sin negro ni cascada. El legacy abre la página con la transición de la plataforma (`workout_screen.dart:169-175`). [Editor-PM] · [Captura] VID-01, VID-03, VID-08
- **AC-WKT-13-07 · Los controles durante la entrada.** *Entonces* `Terminar` y `Pausa` responden desde que empiezan a aparecer. "Atrás" de Android abre la confirmación de terminar desde el primer cuadro (AC-WKT-13-03 de la 4a). Si la sesión vuelve a reposo durante la entrada, la salida arranca desde donde está la entrada, sin saltos. [Editor-PM] · [Device-And] ("Atrás")
- **AC-WKT-13-08 · Salida, con la última foto.** *Cuando* la sesión vuelve a reposo (al terminar a mano, AC-WKT-20-04 de la 4a, o con `Terminar` del diálogo de completado, AC-WKT-21-06), *entonces* el HUD se va junto, el fondo funde a negro y vuelve Workout con la barra de pestañas, y con ella las barras del sistema (AC-APP-15-01); todo en menos de 1 s. Mientras sale, el HUD conserva **la última foto de la sesión**: el reloj final, la barra de pasos con sus píldoras (o la barra del libre con su chip) y los últimos valores. Nunca se ve `00:00`, la barra del libre en una rutina ni el chip `—` (O-01 de la 4a; DV-41). Lo que sale no responde. Con reducir movimiento, un fundido de 120 lineal. [Editor-PM] · [Captura] VID-02
- **AC-WKT-13-09 · Nada queda animando al salir.** *Cuando* el inmersivo terminó de salir, *entonces* no queda nada del HUD en movimiento: ni el corazón, ni los pulsos (`Próximo`, la cuenta, el halo), ni el punto en vivo, ni rodillos de cifras, ni el sello, la cuenta grande, el relleno o el conteo del diálogo. La app vuelve a su ritmo de reposo, como en la 4a, y el registro del build no suma avisos por elementos que ya no están en pantalla (QA-V4a-02 y QA-V4a-05). [Editor-PM] · [Device-Win] (el registro del build de simulador después de tres entrenamientos)
- **AC-WKT-13-10 · Una sesión, un inmersivo.** *Dado* el inmersivo de una sesión que está saliendo, *cuando* se toca `Entrenamiento libre` en cuanto Workout responde, *entonces* la sesión nueva abre su propio inmersivo, con su HUD y sus controles (QA-V4a-01, corregido en la 4a), y no reaparece nada de la sesión anterior: ni su diálogo ni sus píldoras. [Editor-PM]
- **AC-WKT-13-11 · El diálogo de completado, a lo sumo una vez.** *Entonces* el diálogo de completado aparece a lo sumo una vez por sesión. Rotar, cambiar el tamaño de la ventana, minimizarla y volver o pasar a segundo plano no lo duplican ni lo vuelven a abrir, y una sesión nueva muestra el suyo. [dotnet] · [Editor-PM] · `immersive_workout_screen.dart:29`, `:49-55`, `:61`
- **AC-WKT-13-12 · El velo de pausa (mejora EXR).** *Cuando* se pausa, *entonces*:
  - en 300 ms el fondo se oscurece un 15 % (todavía sin escena: el fondo provisorio; la desaturación llega con la escena de la 6a);
  - aparece `hudPausedLabel` (`PAUSA` / `PAUSED`) con un fundido y una escala corta (0,96 → 1 en 240), en el lugar de la cuenta grande (centrada en el ancho, fuera de la zona del bote; lo fija el Designer) y con el contraste de los valores del HUD (R-10);
  - las tarjetas, la barra, sus textos y el chip no bajan de opacidad, y los valores siguen en vivo (ART_BIBLE §3.2); la cuenta grande no se muestra.

  Al reanudar, todo vuelve en 240. La etiqueta no responde al toque ni se enfoca. Con reducir movimiento, fundidos sin escala. El legacy no tiene velo: la pausa se ve en el botón, en el chip (libre) y en el reloj quieto. [Editor-PM] · [Captura] CAP-14, CAP-27, VID-06
- **AC-WKT-13-13 · Nada se encima, con las piezas nuevas.** *Entonces*, en TV, TH, L640 y W1280, en libre y con rutina, con pulsómetro y con los valores largos (CAP-16 de la 4a), ninguna pieza del HUD se encima con otra ni sale de la pantalla: la fila de píldoras, el reloj de pared, el chip, el sello, la cuenta grande, `PAUSA` y las de la 4a. Ninguna entra en la zona del bote (AC-WKT-17-08 y -09, §5). El velo del diálogo de completado, que tapa todo a propósito, no cuenta. [Editor-PM] · [Captura] CAP-01 a CAP-06, CAP-13 a CAP-27, CAP-29 a CAP-31
- **AC-WKT-13-14 · La presentación no toca el dato.** *Entonces*, con el reloj de prueba, la secuencia de estados del motor es idéntica con las animaciones, con reducir movimiento y sin HUD (solo el motor): reloj, paso, restante, puntos con sus valores, resultados de objetivos e instante del fin. Ninguna de estas cosas demora ni adelanta nada: la entrada, la salida, el velo, los pulsos, el halo, la cuenta grande, el sello, el relleno continuo, el diálogo ni el conteo de sus totales. En pantalla:
  - el reloj de la banda, la cuenta de la barra y la cuenta grande cambian en el mismo cuadro en que cambia el segundo del motor;
  - el relleno nunca pasa del progreso del segundo siguiente;
  - el sello y el diálogo aparecen después del cambio que anuncian, nunca antes;
  - ningún número en vivo pasa por valores inventados (solo cuentan los totales del diálogo, que terminan en el valor exacto: ANIMATION_BIBLE §0.1).

  [dotnet] · [Editor-PM] · [Captura] (videos cuadro por cuadro)

### 4.7 F-WKT-17 — HUD: tarjetas y banda (se cierra acá)

Los diez de la 4a se vuelven a verificar en §5 (AC-WKT-17-04 es una condición).

- **AC-WKT-17-11 · kcal se desliza.** *Cuando* la tarjeta BPM aparece (pulso > 0) o se va (pulso 0), *entonces*, en vertical, kcal va a su lugar nuevo sin saltar, deslizándose, y BPM funde (ANIMATION_BIBLE §2.9):
  - al aparecer BPM, kcal se mueve en 0-240 y BPM funde en 120-360;
  - al irse, BPM funde en 0-120 y kcal se mueve en 120-360.

  En horizontal kcal está en el rincón y no se mueve. Con reducir movimiento, kcal cambia de lugar en el acto y BPM funde. En la 4a kcal saltaba (04-design-notes de la 4a §17.6). [Editor-PM] · [Captura] VID-07
- **AC-WKT-17-12 · Cifras de tablero y kerning.** *Entonces* los números del HUD (y los de Dispositivo y del diálogo de completado) se dibujan con las cifras de tablero del sistema visual (RowMate Numeric SC, ART_BIBLE §4.1-§4.2), y todos los textos llevan el kerning de ART_BIBLE §4.4. El `1` no tiene aire de más (CAP-13) y el par `Te` de `Terminar entrenamiento` se ve ajustado. La prueba de las escalas del HUD corre sin ignorarse. Las capturas de las It. 2, 3 y 4a se regeneran una vez, con dos corridas iguales, y QA explica cada diferencia (QA-V4a-06). [Editor-PM] · [Captura] · [QA]

### 4.8 También toca

**F-WKT-09 (cumplimiento de objetivos; se cerró en la 4a): el sello de resultado (D-ANI-05).**

- **AC-WKT-09-08 · El sello: cuándo.** *Cuando* se completa un paso con objetivos (AC-WKT-09-07 de la 4a), *entonces*, en el segundo del cambio de paso, aparece por 2 s un sello con el veredicto:
  - `hudSealMet` (`Objetivo cumplido` / `Target met`), en `#06D6A0`, o `hudSealMissed` (`No alcanzado` / `Target missed`), en `#FFD166`;
  - el detalle de AC-WKT-09-09, en blanco;
  - en una **tercera fila de la barra de pasos**, debajo de la fila de información y en el mismo vidrio, centrado y en una línea (DT4b-02). La barra crece para el sello y vuelve a su alto cuando se va, sin mover las tarjetas (AC-WKT-14-01). Lo que no entra se achica, nunca se corta. No tapa las píldoras ni la fila de información, ninguna tarjeta, la zona del bote ni los controles; con el panel del simulador abierto, se ve igual (AC-BLE-16-03).

  Entra y sale con fundidos cortos dentro de esos 2 s (ANIMATION_BIBLE §2.7: 240 de entrada y 200 de salida), y nunca demora nada. Un sello nuevo reemplaza al visible. En pausa sigue sus 2 s. Al volver del segundo plano, un sello cuyos 2 s ya pasaron no se muestra.

  No hay sello:
  - en un paso sin objetivos;
  - en un paso cortado con `Terminar`;
  - en el libre;
  - en el **último** paso: queda en curso sin mostrar su resultado (Q-52; P-02).

  `Estados` con `R-4`: sellos a los 10 s (cumplido), 20 (no alcanzado) y 50 (cumplido); a los 30 y 60, ninguno. Si el usuario rechaza D-ANI-05, no hay sello: el resultado lo muestra la píldora. [dotnet] (cuándo y cuál) · [Editor-PM] · [Captura] CAP-03, CAP-04, CAP-05, CAP-26, CAP-30, VID-04
- **AC-WKT-09-09 · El sello: qué dice.** *Entonces*, después del veredicto, va un fragmento por cada objetivo que define el paso, en el orden de la pastilla (vatios, SPM, split), unidos por `·`:
  - **Vatios**: `hudSealWatts` (`{avg} / {target} W`).
  - **SPM**: `hudSealSpm` (`{avg} / {target} spm`).
  - **Split**: `hudSealSplit` (`{avg} / {target}/500m`).

  El promedio es el del paso, el mismo que decide el veredicto (F-WKT-09): vatios y SPM sobre las lecturas remando; split sobre las lecturas mayores que 0. Se muestra redondeado hacia el lado desfavorable, así el número nunca contradice el veredicto (P-03):
  - vatios, hacia abajo, al entero;
  - SPM, hacia abajo, a un decimal con punto;
  - split, hacia arriba, al segundo, en `MM:SS`.

  El objetivo va con el formato de la pastilla (AC-WKT-11-13 de la 4a): los vatios mín. y el SPM enteros, y el split en `MM:SS`. Sin ninguna lectura remando, el promedio se ve `--` (vatios y SPM) y `--:--` (split); con un split nunca informado, `--:--`. Los mismos textos en es y en. Casos:
  - `Estados` con `R-4`: paso 0 → `Objetivo cumplido` · `24.0 / 18 spm`; paso 1 → `No alcanzado` · `24.0 / 30 spm`.
  - `Prueba 4a` con `R-4` (150 W), trabajo 1 → `No alcanzado` · `150 / 160 W · 24.0 / 22 spm`.
  - `Pirámide 500` con `R-4`: el primer trabajo (500 m) → `Objetivo cumplido` · `150 / 150 W · 02:05 / 02:05/500m`; el de 250 m → `No alcanzado` · `24.0 / 30 spm`.
  - Redondeo: un promedio de 159,6 W contra 160 → `159 / 160 W` (no alcanzado); 21,96 spm contra 22 → `21.9 / 22 spm`; un split de 125,2 contra 125 → `02:06 / 02:05/500m`; 124,6 contra 125 → `02:05 / 02:05/500m` (cumplido).
  - Con `R-4P` (parado) todo un paso con vatios mín. 160 y SPM 22 → `No alcanzado` · `-- / 160 W · -- / 22 spm`.

  [dotnet] (los textos, con estos casos) · [Captura] CAP-03, CAP-04, CAP-05 · `lib/features/workout/step_target_tracker.dart:30-40`, `:50-61`

**F-WKT-02 (reloj activo; se cerró en la 4a): P-17 de la 4a.**

- **AC-WKT-02-11 · El equipo suspendido no cuenta (P-17 de la 4a).** *Dado* una sesión en marcha en la PC, libre o con rutina, *cuando* Windows suspende el equipo y después despierta, *entonces* el lapso suspendido no cuenta como tiempo de sesión. Vale con Suspender, con la tapa cerrada que suspende, con el modo de espera moderno y con la hibernación. Al despertar:
  - la sesión sigue en la fase en que estaba;
  - el reloj de la banda sigue desde el valor que tenía al suspenderse (a lo sumo 1 s más);
  - los pasos por tiempo no avanzaron: la cuenta regresiva sigue desde donde estaba, y ningún paso ni la rutina terminaron durante el lapso;
  - no hay puntos del lapso: el siguiente cae 5 s en marcha después del anterior;
  - los objetivos no suman lecturas del lapso;
  - no aparece ningún aviso.

  Lo demás sigue la hora real:
  - el reloj de pared muestra la hora actual (AC-WKT-19-03);
  - la distancia y las kcal son las que informe el monitor al volver (Q-15); en un paso por distancia, un salto de más de 10 m por segundo no suma (AC-WKT-03-03 de la 4a).

  Si estaba en pausa, sigue en pausa; si estaba abierto el diálogo de completado o el de terminar, sigue abierto. El tiempo suspendido no cuenta como demora del cierre: no aparece un `workoutSaveError` falso. El primer cuadro al despertar ya muestra el presente (AC-WKT-02-08). Es lo que hace el legacy, que cuenta disparos de un temporizador que no corre con el equipo suspendido (`workout_provider.dart:147`, `:247`), y lo que aceptó el usuario en el gate de la 4a.

  **No son suspensión** (el tiempo sí cuenta, Q-91):
  - la ventana minimizada o sin foco (AC-WKT-02-09);
  - la pantalla bloqueada con el equipo despierto;
  - en Android, la pantalla bloqueada o el paso a otra app (AC-WKT-02-10; la 10a mide el reloj del teléfono, RA-19). En macOS vale lo mismo que en Windows ([Apple], 10b).

  Con el ciclo de vida de prueba (§0.4), en un libre con `R-4` que se suspende a los 62,4 s y despierta 600 s después, en los dos casos del reloj monótono:
  - al despertar el reloj dice `01:02` (o `01:03` desde el primer segundo);
  - los puntos son los de los segundos en marcha 5 a 60 y, después, 65, 70…;
  - terminada a los 120 s en marcha, la sesión tiene 25 puntos (24 + el final), no 145.

  [dotnet] (ciclo de vida de prueba con suspensión) · [Device-Win] (**lo verifica el usuario** en la demo (c); para QA queda NO VERIFICABLE en equipo)

**F-APP-15 (modo inmersivo del sistema; se cierra en la It. 14).**

- **AC-APP-15-01 · Android: las barras se van al entrar y vuelven al salir.** *Entonces* al entrar al inmersivo, con el fundido de Workout (AC-WKT-13-06), se ocultan la barra de estado y la de navegación en modo inmersivo persistente: si el usuario las revela deslizando desde el borde, se vuelven a ocultar solas. Al salir, con la entrada de Workout (AC-WKT-13-08), vuelven a verse y ninguna pantalla queda tapada por ellas (se respetan los márgenes seguros). Vale también al volver de otra app con el inmersivo abierto (se vuelven a ocultar) y con un diálogo abierto. [Device-And] (NO VERIFICABLE sin módulo y teléfono) · `immersive_workout_screen.dart:32-41`
- **AC-APP-15-02 · Windows: la ventana no cambia.** *Entonces* en la PC, al entrar y al salir del inmersivo, la ventana queda como estaba: no pasa a pantalla completa, y la barra de tareas, la barra de título, el tamaño y la posición no cambian (paridad: en escritorio el modo inmersivo del legacy no tiene efecto; la pantalla completa real es Q-88, backlog). [Device-Win] · ARCHITECTURE (tabla de plataforma, modo inmersivo)
- **AC-APP-15-03 · iOS y macOS.** *Entonces* en iOS la barra de estado se oculta en el inmersivo y vuelve al salir; en macOS, la ventana no cambia, como en Windows. [Apple] (10b, ROADMAP §4.1)
- **AC-APP-15-04 · El pedido, en orden.** *Entonces*, con la interfaz del sistema de prueba, hay un pedido de ocultar las barras al empezar cada entrada y uno de mostrarlas al terminar cada salida, salvo que otro inmersivo ya esté entrando o arriba. Si una sesión nueva abre su inmersivo mientras sale el anterior (AC-WKT-13-10), la salida del anterior no pide nada y las barras siguen ocultas hasta que termine la salida del nuevo (DT4b-05). Se cumple también si la sesión termina durante la entrada, con el diálogo de completado abierto y al volver del segundo plano (otra vez "ocultar" mientras sigue el inmersivo). Nunca quedan ocultas en Workout ni en otra pantalla. En escritorio el pedido no tiene efecto (AC-APP-15-02). [dotnet] · [Editor-PM] (interfaz del sistema de prueba)

**F-APP-10 (catálogo; se cerró en la It. 1).**

- **AC-APP-10-11 · 12 claves nuevas.** *Entonces* el catálogo pasa de 202 a **214** claves en es y en, con las 12 de §11.2 (textos y parámetros exactos). El control de la cantidad pasa a 214 y ninguna clave queda en un solo idioma. Los textos que el legacy tiene fijos en español (los del chip de fase y las etiquetas del resumen) salen del catálogo con el mismo español (Q-01). [dotnet] · FEATURE_INVENTORY Q-01

---

## 5. Criterios de iteraciones anteriores que se verifican acá

Si fallan, se reabren en esta iteración. Desde esta iteración valen los textos de abajo, que reemplazan a los de la [spec de la 4a](../04a-motor-hud-funcional/01-po-spec.md) (y a los de la It. 2 o la 3 cuando se indica).

### 5.1 Las dos condiciones de la It. 4a (primero, antes de construir encima)

- **AC-WKT-17-04 · Banda (condición, QA-V4a-03).** *Entonces* lo de la 4a, con el divisor vertical **visible y entero** en todos los perfiles. Mide 1 por la escala de ancho (nunca menos de 1 px de pantalla) y 32 por la escala de alto, al 12 %, nunca se achica y lleva su margen a cada lado (16, o 10 en horizontal, por la escala: DV-33). Una prueba mide su ancho. Las tomas de la 4a que no lo mostraban (CAP-16, -18, -20, -24, -26, -27a, -27b y -30) se regeneran y lo muestran. [Editor-PM] · [Captura] (las de la 4a regeneradas; CAP-13)
- **AC-APP-05-07 · Teclado y foco (condición, QA-V4a-04; se completa).** *Entonces* vale todo lo de la 4a y, además:
  - después de usar el mouse o el dedo, **el primer** Tab enfoca el primer control **con** su anillo, en el HUD, en Workout y en el editor de Rutinas: un Enter nunca activa un control enfocado que no se ve;
  - en el diálogo de completado, Tab va a `Terminar` con el anillo (AC-WKT-21-11).

  El orden de Tab del HUD no cambia: las píldoras, el reloj, el chip, el sello y la cuenta grande no se enfocan. [Editor-PM] (una prueba "sin foco, un Tab enfoca con anillo" en el HUD, en Workout y en el editor) · [Device-Win]

### 5.2 Cambian por esta iteración

- **AC-WKT-01-06 · Terminar.** *Cuando* la sesión termina (a mano o al completar la rutina), *entonces* la fase pasa a terminada, el reloj se detiene y se guarda la sesión (AC-WKT-06-06). Si se terminó a mano, cuando el guardado termina la sesión vuelve a reposo y el inmersivo se cierra. Si la rutina terminó sola, queda terminada hasta `Terminar` del diálogo de completado (AC-WKT-21-06, -08). [dotnet] · [Editor-PM]
- **AC-WKT-02-04 · No se pierden ni se duplican segundos (DV-27), salvo con el equipo suspendido.** *Entonces* lo de la 4a: si un cuadro llega tarde (la app estuvo trabada, minimizada o en segundo plano), se procesan en orden los segundos que faltan, cada uno con su lectura. **Salvo** el tiempo con el equipo suspendido, que no se procesa (AC-WKT-02-11). [dotnet]
- **AC-WKT-02-06 · Segundo plano: la rutina termina mientras tanto.** *Dado* `Corta` con `R-4` en segundo plano desde los 8 s, *cuando* vuelve a los 40 s, *entonces* la sesión quedó cerrada como si hubiera terminado en primer plano, con los 7 puntos y la hora de fin del segundo 30. Al volver se ve el inmersivo con el diálogo de completado ya abierto, una sola vez (AC-WKT-21-09), sin avisos. `workoutSaveError` sale solo si el cierre falló, al volver a Workout después del diálogo. Lo mismo si la vuelta llega a los 330 s: el tiempo en segundo plano no cuenta como demora del guardado. Sale el aviso `workoutCompleted` de la 4a. [dotnet] (ciclo de vida de prueba) · [Editor-PM]
- **AC-WKT-02-08 · Al volver, el presente.** *Entonces* lo de la 4a: el primer cuadro muestra el estado actual, sin números que pasan por valores intermedios. Ahora vale también para las píldoras, el halo, la cuenta grande, el reloj de pared, el chip y el diálogo de completado. De los avisos producidos en segundo plano se muestra, una vez y con sus 4 s desde la vuelta, solo el último; con el diálogo de completado abierto, ninguno hasta volver a Workout. [Editor-PM] (ciclo de vida de prueba) · [Device-And]
- **AC-WKT-02-09 · Windows: ventana minimizada o sin foco.** *Entonces* lo de la 4a, con el equipo despierto (con el equipo suspendido rige AC-WKT-02-11). Si la rutina terminó mientras tanto, al restaurar la ventana se ve el diálogo de completado (AC-WKT-21-09), una sola vez, en lugar del aviso. [Device-Win]
- **AC-WKT-03-05 · Fin automático.** *Entonces*, al terminar el último paso, en ese mismo segundo la fase pasa a terminada (ningún otro segundo vuelve a evaluar), el reloj se detiene y empieza el guardado (AC-WKT-06-06). A los 300 ms aparece el diálogo de rutina completada (AC-WKT-21-01), sin esperar al guardado. La sesión queda terminada hasta `Terminar` del diálogo (AC-WKT-21-08). Si el diálogo de terminar estaba abierto, se cierra sin efecto (AC-WKT-20-07). Sale el aviso provisorio `workoutCompleted` (DV-34, cerrada para el diálogo). Al terminar a mano nunca aparece el diálogo (AC-WKT-21-07). [dotnet] · [Editor-PM] · [Captura] CAP-07, DEMO-11
- **AC-WKT-06-12 · Errores al guardar (Q-13).** *Entonces* lo de la 4a, con el final nuevo de una rutina que termina sola: el diálogo de completado aparece igual. Al tocar `Terminar`, Workout muestra `workoutSaveError` si la sesión no quedó cerrada con todos sus puntos, y nada si el reintento lo guardó todo (DT4a-17). Al terminar a mano, como en la 4a. [dotnet] · [Editor-PM] · [Captura] CAP-28
- **AC-WKT-13-01 · Se abre al iniciar.** *Cuando* se toca `Entrenamiento libre` o `Iniciar`, *entonces* el inmersivo se abre en el acto, a pantalla completa y por encima de toda la app, sin esperar a la base: la sesión y el reloj corren desde el toque y ninguna transición los demora. Entra con "sumergirse" (AC-WKT-13-06) y, en Android, oculta las barras del sistema (AC-APP-15-01). Sale DV-34 para esta parte. [Editor-PM]
- **AC-WKT-13-04 · Se cierra solo.** *Entonces*, cuando la sesión vuelve a reposo (al terminar a mano o con `Terminar` del diálogo de completado), el inmersivo sale (AC-WKT-13-08) y se ve Workout en reposo con la barra de pestañas. Nunca quedan el inmersivo abierto en reposo, Workout en reposo con una sesión en curso ni un diálogo del inmersivo sobre Workout en reposo. [Editor-PM]
- **AC-WKT-14-01 · Contenedor.** *Dado* una rutina, *entonces* la barra superior tiene 12 de margen a cada lado, el vidrio de AC-WKT-17-01 con borde `#FFFFFF @8 %` y radio 14, y **dos filas**: arriba la de píldoras (AC-WKT-14-07) y abajo la de información (relleno 12 / 0 / 12 / 8; AC-WKT-14-02 a -05 y el reloj de pared, AC-WKT-14-14). Mientras se ve el sello, la barra suma **una tercera fila**, la del sello, debajo de la de información y en el mismo vidrio (AC-WKT-09-08). No responde al toque. Las tarjetas no se mueven, tampoco cuando la barra crece con el sello: SPM, Split y Watts siguen a `margen seguro superior + 90` por la escala. Sale DV-34 para esta parte. El legacy tiene dos filas y no tiene sello. [Editor-PM] · [Captura] CAP-01, CAP-03 · `immersive_workout_screen.dart:348-357`
- **AC-WKT-14-03 · Pastilla de objetivo (cambia: DV-35, DV-38, DV-45).** *Entonces* lo de la 4a: si el paso tiene etiqueta de objetivos (AC-WKT-11-13 de la 4a), a 6 del tipo va una pastilla con fondo del color base del tipo al 20 %, radio 6, relleno 6 × 2 y texto de 11/600 en blanco (`text.primary`; el legacy, en el color del tipo: DV-38), con el `📈` como el ícono `trending_up` en `metric.distance` (DV-35). Sin objetivos, no hay pastilla. **Cambia cómo se corta** (DV-45):
  - puede usar todo el espacio libre de la fila; en el legacy recibe a lo sumo la mitad;
  - si no entra entera, muestra los **fragmentos enteros** que entran y un `…` blanco pegado al último: `>150W · 24 spm…`. Un fragmento es cada parte separada por ` · `, y la tendencia con su texto cuenta como uno. Nunca corta un número ni una palabra: `>15…` se leería "más de 15";
  - si no entra ni el primer fragmento, no se muestra (AC-WKT-14-15).

  Nunca empuja afuera la cuenta regresiva ni el reloj. [dotnet] (el corte por fragmentos) · [Editor-PM] · [Captura] CAP-01, CAP-21, CAP-24, CAP-31 · `immersive_workout_screen.dart:394-415`
- **AC-WKT-14-04 · `Próximo` (cambia: DV-24, DV-38).** *Entonces* lo de la 4a: cuando aplica "a continuación" (AC-WKT-04-05 de la 4a), del lado derecho de la fila, justo antes de la cuenta y a 8 de ella, va una pastilla con `hudUpNext` (`Próximo:` / `Up next:`) en `text.secondary`, de 10, un espacio y el tipo del paso siguiente localizado, en blanco, de 10/700; fondo del color base del paso siguiente al 20 %, borde del mismo color al 50 %, radio 8 y relleno 7 × 3. El fondo y el borde laten con el reloj común, el mismo del halo y de la cuenta, y el texto queda siempre entero. No muestra la duración del paso siguiente (paridad). **Cambia** solo cuando la fila no entra (AC-WKT-14-15): el aire a la cuenta baja a 6 y, si hace falta, el relleno a 4 × 3. `Próximo:` y su tipo nunca se cortan. Entra y sale como en la 4a, y la cuenta y el reloj no se mueven cuando entra (AC-WKT-14-14). [Editor-PM] · [Captura] CAP-01, CAP-19, CAP-20 · `immersive_workout_screen.dart:319-322`, `:418-452`
- **AC-WKT-14-06 · En pausa y al terminar.** *Entonces*, en pausa, la barra queda quieta (el relleno no avanza), pero si el paso estaba por terminar o con `Próximo` los pulsos siguen: el halo, la cuenta y `Próximo` (paridad). Con la rutina terminada, hasta que sale el inmersivo, muestra:
  - las píldoras finales (AC-WKT-14-13);
  - el último paso con `0:00` (si es por distancia, su largo), sin `Próximo`, sin halo, sin cuenta grande ni sello.

  No hay "paso N de M" ni el nombre de la rutina. [Editor-PM] · [Captura] CAP-06, CAP-27
- **AC-WKT-16-01 · Barra del libre.** *Dado* un entrenamiento libre, *entonces* la barra superior tiene 12 de margen a cada lado, relleno 14 × 8, el vidrio de AC-WKT-17-01 con borde `@8 %` y radio 14, y el contenido de AC-WKT-16-02 (ícono, título, reloj de pared y chip de fase). Sale DV-34 para esta parte. [Editor-PM] · [Captura] CAP-13 · `immersive_workout_screen.dart:497-504`
- **AC-WKT-20-07 · La rutina termina con el diálogo de terminar abierto.** *Dado* una rutina con el diálogo de terminar abierto, *cuando* el último paso se completa, *entonces*, en el cuadro en que la fase pasa a terminada, el diálogo de terminar se cierra sin efecto, como con `Cancelar`, y sigue AC-WKT-21-01: a los 300 ms, el diálogo de rutina completada. La sesión cuenta como terminada sola.
  - Un toque en `Terminar` del diálogo de terminar que llega después de ese cuadro no cambia nada.
  - Uno que llega antes (la fase todavía estaba en marcha) es un terminar a mano: no hay diálogo de completado (AC-WKT-21-07).

  Vale también en segundo plano (AC-WKT-21-09). Nunca quedan dos diálogos apilados. En el legacy el de completado se apila sobre el de terminar y, al cerrarlo, el de terminar se cierra sin efecto. [Editor-PM] · `immersive_workout_screen.dart:49-65`, `:125-147`
- **AC-APP-03-09 · El inmersivo se apila.** *Entonces* el inmersivo se apila sobre la app como el editor: la barra de pestañas no se ve ni se puede tocar, no hay flecha de volver ni lo cierra Escape, se cierra solo al volver a reposo y lo que se está cerrando no responde. Entra con "sumergirse" (AC-WKT-13-06) y sale como fija AC-WKT-13-08; con reducir movimiento, un fundido. [Editor-PM] · [Captura] VID-01, VID-02
- **AC-APP-03-10 · Los diálogos de Workout y del inmersivo.** *Entonces* el diálogo de inicio y el de terminar, como en la 4a. El de completado se comporta como ellos (centrado sobre el velo, Tab recorre solo el diálogo, el que sale no responde), salvo en que es **modal**: tocar afuera, Escape y "Atrás" no lo cierran (AC-WKT-21-02). La cinta y la llave quedan por encima del velo. [Editor-PM] · [Captura] CAP-07
- **AC-APP-03-11 · Avisos.** *Entonces* `workoutRoutineCannotStart` y `workoutSaveError` son avisos como los de la It. 3. `workoutCompleted` deja de ser un aviso: es el título del diálogo de completado (AC-WKT-21-03). Con el diálogo de completado abierto no aparece ningún aviso; los que haya se muestran una vez al volver a Workout, cuando Workout empieza a verse, y no sobre el negro de la salida (04-design-notes §4.2). En el inmersivo, los avisos no tapan los controles ni el sello. [Editor-PM] · [Captura] CAP-28
- **AC-APP-05-05 · Letra del sistema 1,3×, en las pantallas nuevas.** *Entonces* el diálogo de completado crece hasta 1,3× sin cortes, y lo que no entra se desplaza dentro del diálogo (AC-WKT-21-12). El HUD, las píldoras, el sello, la cuenta grande y `PAUSA` no siguen la letra del sistema (AC-WKT-17-01). [Editor-PM] · [Captura] CAP-10
- **AC-APP-05-08 · Reducir movimiento, en las piezas nuevas.** *Entonces* lo de la 4a y, además:
  - la entrada y la salida, un fundido de 120 lineal, sin cascada (AC-WKT-13-06, -08);
  - el velo y `PAUSA`, fundidos sin escala;
  - el halo de la píldora, fijo, y el punto en vivo, quieto;
  - la píldora cambia de estado con un fundido, sin destello ni trazo;
  - con píldoras angostas, el pulgar pasa a la siguiente con un fundido, sin subir ni bajar (AC-WKT-14-16);
  - en la fila de información, los aires cambian en el acto y la pastilla que no entra se va con un fundido (AC-WKT-14-15);
  - la cuenta grande y el sello, fundidos sin escala;
  - kcal cambia de lugar en el acto;
  - el diálogo de completado entra con un fundido, con el check dibujado y los totales finales desde el primer cuadro.

  Nada de esto cambia los datos ni cuándo pasan. [Editor-PM]
- **AC-BLE-16-01 · Cinta SIMULADOR sobre el entrenamiento.** *Entonces* lo de la 4a: la cinta queda por encima del inmersivo, de sus diálogos (también el de completado) y de sus avisos. Tapa en parte el comienzo de la barra superior, que ahora incluye el comienzo de la primera píldora, como en `docs/image-03.png`. No tapa la cuenta regresiva ni el reloj de pared y no recibe toques. [Editor-PM] · [Captura] CAP-01, CAP-07, CAP-13
- **AC-BLE-16-02 · La llave.** *Entonces* lo de la 4a (D-ART-10, DV-39). En el entrenamiento tampoco tapa la fila de píldoras, el reloj de pared, el chip, el sello, la cuenta grande, `PAUSA` ni el diálogo de completado, en TV, TH, L640 y W1280, en libre y con rutina. En la entrada "sumergirse" aparece cuando termina la cascada, y en la salida se va en el primer cuadro (ANIMATION_BIBLE §2.11). Con el diálogo de completado abierto queda por encima del velo y se puede usar. [dotnet] (la regla) · [Editor-PM] (con `-Simulator`) · [Captura] (auditoría de la llave en toda toma con simulador)
- **AC-BLE-16-03 · El panel (cambia por DN4b-23).** *Entonces* lo de la 4a: el panel mide 280 dp de ancho, se abre desde la llave, tiene su alto máximo con un mínimo de 120 dp, lo que no entra se desplaza adentro y el botón de cerrar queda siempre visible. Fuera del entrenamiento, sin cambios. **En el entrenamiento cambia el techo**: con el panel abierto siguen a la vista Split y Watts, para ver el efecto de cambiarlos, y además el sello y el lugar de la cuenta grande y de `PAUSA`. El sello se tiene que ver igual con el panel abierto (AC-WKT-09-08).
  - **En vertical**, el panel termina 8 dp debajo del lugar de la cuenta grande, que está debajo de Watts con su objetivo. En la 4a terminaba 8 dp debajo de Watts.
  - **En horizontal**, termina 8 dp debajo de la barra superior en su alto mayor, con el sello, y sigue sin llegar a Split y Watts. En la 4a subía hasta 16 dp del borde de arriba y tapaba el medio de la barra.

  Así el alto del panel no cambia con el paso, con el sello ni con la cuenta grande. Sus altos máximos en el entrenamiento pasan a ser: TV 298,0 dp (era 316,4), un teléfono de 360 258,5 (274,6), TH 290,2 (368,6), L640 201,8 (269,5) y W1280 530,4 (653,6). Son los de las 04-design-notes §3.9. [dotnet] (la regla, con los cinco altos) · [Editor-PM] (con `-Simulator`) · [Captura] DEMO-08 · `lib/core/dev/simulator_overlay.dart:40-49`, `:83-95`

### 5.3 Se vuelven a verificar, sin cambios (las fichas se cierran acá)

F-WKT-13, F-WKT-14 y F-WKT-17 se cierran en la 4b: QA vuelve a correr y a mirar estos criterios de la 4a con las piezas nuevas puestas.

| Criterio | Qué se mira otra vez |
|---|---|
| AC-WKT-13-02 · Capas | El orden de las capas con el velo de pausa, el sello, la cuenta grande y el diálogo de completado; la cinta y la llave siempre encima |
| AC-WKT-13-03 · "Atrás" y Escape | Escape sin diálogo no hace nada; con el de terminar, lo cancela; con el de completado, nada (AC-WKT-21-02) |
| AC-WKT-13-05 · Sin el nombre de la rutina | Tampoco en el diálogo de completado ni en el sello |
| AC-WKT-14-02 · Tipo del paso | Igual, con la fila de píldoras arriba |
| AC-WKT-14-05 · Cuenta regresiva | Igual; el reloj de pared, a su derecha (AC-WKT-14-14) |
| AC-WKT-17-01 · Vidrio y tamaños | Con las cifras de tablero de WP-B0 (AC-WKT-17-12) |
| AC-WKT-17-02 · SPM | Igual |
| AC-WKT-17-03 · Split y Watts | Igual |
| AC-WKT-17-05 · kcal y BPM | Con kcal que se desliza (AC-WKT-17-11) |
| AC-WKT-17-06 · Valores en vivo | También con el diálogo de completado abierto, detrás del velo (AC-WKT-21-08) |
| AC-WKT-17-07 · Orientación | Al rotar con el diálogo, el sello o la cuenta grande a la vista, todo se reacomoda y nada se repite |
| AC-WKT-17-08 · Nada se encima | Con las piezas nuevas (AC-WKT-13-13) |
| AC-WKT-17-09 · La zona del bote | Ninguna pieza nueva entra (la cuenta grande, `PAUSA` y el sello, tampoco) |
| AC-WKT-17-10 · Valores extremos | Con `R-4X`, también la barra con píldoras y el diálogo de `Maratón` (AC-WKT-21-12) |
| AC-HIS-20-04 · Las pruebas no tocan las bases del usuario | Vale para las pruebas, las capturas y los videos de la 4b, sin excepciones |

---

## 6. Casos de test legacy

El de ROADMAP §5 para la 4b. Se porta uno a uno, con el mismo nombre y las mismas afirmaciones.

| LT | Archivo:línea | Caso (nombre exacto) | Qué afirma | Dónde | AC |
|---|---|---|---|---|---|
| LT-46 | `test/step_pill_test.dart:19` | `pendiente, en curso, hecho y no cumplido se pintan distinto` | Con cinco píldoras de un trabajo de 60 s en una fila de 400: pendiente; en curso con progreso 0,5; hecha cumplida; hecha no cumplida; hecha sin resultado. La hecha tiene opacidad mayor que 0,8 y la hecha sin resultado es idéntica a la cumplida; la pendiente, menor que 0,3; el interior de la no cumplida tiene la opacidad de la pendiente y su borde, mayor que 0,8; la pendiente y la hecha, sin borde; la en curso, entre la pendiente y la hecha; su relleno es la mitad del ancho con 0,5; la en curso, sin borde; la pendiente, sin relleno | [dotnet] (la regla de cada estado) · [Editor-PM] (la píldora montada: los colores y el relleno que se ven) | AC-WKT-15-02 a -05 |

Los criterios nuevos del ROADMAP para la 4b quedan así:

| Criterio nuevo (ROADMAP §2, It. 4b) | AC |
|---|---|
| Estados visuales de la píldora: pendiente / en curso / hecho / no cumplido | AC-WKT-15-01 a -06, LT-46 |
| El último paso queda "en curso" (Q-52) | AC-WKT-14-13 (y sin sello: AC-WKT-09-08) |
| Al terminar a mano nunca aparece el diálogo de completado (Q-53) | AC-WKT-21-07 |
| Píldoras del mismo ancho (Q-59) | AC-WKT-14-08, -16 |
| Las animaciones nunca adelantan ni frenan el reloj ni las métricas | AC-WKT-13-14, AC-WKT-14-10 |
| P-17 de la 4a: el tiempo con el equipo suspendido no cuenta | AC-WKT-02-11, AC-WKT-02-04 |

---

## 7. Capturas, demo y videos

### 7.1 Reglas

Las de la 4a ([01-po-spec §7.1](../04a-motor-hud-funcional/01-po-spec.md)):

- Las renderiza Unity con la UI real, en `docs/migration/iterations/04b-hud-exr/snapshots/`, como `CAP-nn-<tema>.png`, `DEMO-nn-<tema>.png` y `VID-nn-<tema>.mp4`, con `manifest.json` e `INDEX.md` **sin rutas de la PC** (el repositorio es público).
- Salvo que se diga otra cosa: español, modo simulador sin ruido, reloj de prueba, zona de prueba UTC−03:00 (el reloj de pared, `10:00` en el primer minuto), el pulsómetro simulado sin conectar y la base indicada (§0.4).
- Las tomas con pulsos van a la fase fija del reloj común que dice cada una.
- **Ninguna toma cae en un segundo entero del motor**: en ese instante vence el segundo, los rodillos arrancan y el primer cuadro todavía muestra el valor viejo. Por eso las que irían en un segundo entero van a los +0,2 s, cuando el rodillo (180 ms) ya terminó, y CAP-06 va a los 30,25 s, para no caer justo en el final de la salida de la cuenta grande (04-design-notes §7.1, DN4b-25). No cambian las de "k,6 s" ni las de un instante en que el motor no avanza: en pausa (CAP-14) o con la rutina ya terminada (CAP-07 a los 32,0, CAP-12 a los 35,2, DEMO-11 a los 242). Lo esperado de cada toma ya está con su instante.
- Ninguna escribe en las bases del usuario (AC-HIS-20-04).
- QA verifica "Qué tiene que verse" y el Designer el aspecto.
- En toda toma con simulador corre la auditoría de la llave (AC-BLE-16-02).
- Los videos van a 30 cuadros por segundo.
- **Al empezar**, antes de las tomas nuevas, las de las It. 2, 3 y 4a se regeneran **una sola vez** con WP-B0 y el divisor (QA-V4a-03, -06), con dos corridas iguales, y QA explica cada diferencia.

### 7.2 Tamaños

**TV** 411 × 914 dp, **TH** 914 × 411, **L640** 640 × 360 y **W1280** 1280 × 720. Escala del HUD: 1,054, 1,054, 0,92 y 1,6.

### 7.3 Lista

| ID | Tamaño | Estado (cómo se llega) | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | TV | `Estados` con `R-4`, a los 40,2 s (el paso 3, de 20 s, a la mitad) | **Las píldoras en sus cuatro estados**, 5 del mismo ancho: la 0 sólida rosa (cumplida), la 1 hueca rosa con su borde (no cumplida), la 2 sólida verde (sin objetivo), la 3 en curso rosa (la pista tenue y el relleno sólido en 0,51, con su brillo y sin marco) y la 4 pendiente verde, tenue. Abajo, `Trabajo`, la pastilla `18 spm` **entera**, `Próximo: Descanso`, `0:10` y el reloj `10:00` con su ícono, con los aires en 6 (AC-WKT-14-15), y la cuenta y el reloj pegados al borde derecho. SPM con `obj. 18`. La cinta sobre el comienzo de la barra |
| **CAP-02** | TV | `Estados` a los 47,6 s (restante 3, `p` = 1) | **El pulso y la cuenta grande**: el halo rosa de la píldora 3 al máximo, solo por fuera; la cuenta grande `3`, centrada, fuera de la zona del bote, sin tapar tarjetas; la cuenta `0:03` agrandada y teñida; `Próximo: Descanso` |
| **CAP-03** | TV | `Estados` a los 10,6 s | **Sello cumplido**: `Objetivo cumplido` en verde con `24.0 / 18 spm`, entero, bajo la barra, en una línea; la píldora 0 sólida y la 1 en curso apenas empezada; nada tapado |
| **CAP-04** | TV | `Estados` a los 20,6 s | **Sello no alcanzado**: `No alcanzado` en ámbar con `24.0 / 30 spm`; la píldora 1 hueca, con el borde entero |
| **CAP-05** | TV | `Pirámide 500` con `R-4`, a los 725,6 s (fin del primer trabajo, de 500 m) | `Objetivo cumplido` con `150 / 150 W · 02:05 / 02:05/500m`, sin cifras cortadas; 14 píldoras del mismo ancho: la 0 (600 s) y la 1 (500 m) sólidas, la 2 (250 m) en curso sin relleno (Q-51), el resto pendientes; la cuenta `250m` |
| **CAP-06** | TV | `Corta` con `R-4`, a los 30,25 s (la rutina terminó a los 30; el diálogo todavía no entró) | **El último en curso (Q-52)**: la 0 y la 2 sólidas rosas, la 1 sólida verde y la 3 (el último descanso) en curso, **llena**, con el brillo del relleno que la distingue de una sólida, y sin borde; `Descanso` y `0:00`; sin `Próximo`, halo, cuenta grande ni sello |
| **CAP-07** | TV | `Corta` con `R-4`, a los 32,0 s | **El diálogo de rutina completada**: el velo; el check verde y `¡Entrenamiento completado!`; `Has completado tu rutina exitosamente.`; la caja con `Tiempo` `00:30`, `Distancia` `1120 m` y `Calorías` `43`, cada uno con su ícono; `Terminar` relleno. Detrás, la barra de CAP-06. La cinta y la llave por encima del velo |
| **CAP-08** | TV | CAP-07 en inglés | `Workout Completed!`, `You have successfully completed your routine.`, `Time` `00:30`, `Distance` `1120 m`, `Calories` `43`, `Finish` |
| **CAP-09** | TH | CAP-07 en horizontal | El diálogo centrado y entero; detrás, el HUD horizontal |
| **CAP-10** | L640 | CAP-07 con letra 1,3× | El diálogo entra, o su contenido se desplaza con `Terminar` a la vista; nada cortado |
| **CAP-11** | W1280 | CAP-07 en la ventana | El diálogo a escala de escritorio; detrás, la barra con las píldoras finales |
| **CAP-12** | TV | `Final por distancia` con `R-4`, a los 35,2 s | La 0 sólida y la 1 (100 m, la última) en curso con la pista sola: sin relleno y sin resultado, aunque se cumplió; la cuenta `100m`; sin sello |
| **CAP-13** | TV | Libre en marcha a 150 W y 24 spm, sin pulsómetro, a los 42,2 s | **La barra del libre**: `Entrenamiento Libre` **entero**, achicado a ≈ 0,8 (0,805; AC-WKT-16-06), el reloj `10:00` con su ícono y el chip `EN MARCHA` verde con su punto; la banda con el **divisor** visible entre `00:42` y la distancia (AC-WKT-17-04); el `1` sin aire de más (AC-WKT-17-12); lo demás como CAP-12 de la 4a |
| **CAP-14** | TV | Libre: `Pausa` a los 42 s y la toma a los 44 s | **La pausa**: el fondo oscurecido; `PAUSA` legible, en el lugar de la cuenta grande y fuera de la zona del bote; el chip `PAUSADO` en amarillo; `Reanudar` verde; las tarjetas y la barra al 100 % (sin atenuar), con los valores en vivo; la banda en `00:42` |
| **CAP-15** | TV | Libre con la base lenta (cada escritura 2 s), 1 s después de confirmar `Terminar` | El chip `FINALIZADO` con el texto en `#FF7A92` (DV-44); los controles al 0,38 y el indicador de `Terminar`; sin diálogo de completado |
| **CAP-16** | TH | CAP-13 con el pulsómetro simulado, en horizontal, a los 42,2 s | La barra del libre con el título, el reloj y el chip enteros; la fila de abajo como CAP-15 de la 4a; nada se encima |
| **CAP-17** | W1280 | CAP-16 en la ventana | Las zonas de `docs/image-03.png` (sin la escena), con el reloj y el chip a la derecha de la barra, a escala 1,6 |
| **CAP-18** | TV | CAP-13 en inglés | `Free Workout` entero y a su tamaño, el reloj y `ACTIVE` |
| **CAP-19** | TV | `Texto largo` a los 8,2 s | **La fila más larga**: `Calentamiento`, `Próximo: Enfriamiento`, `0:12` y `10:00` enteros, con los aires en 6 y `Próximo` con relleno 4 × 3, y **sin la pastilla**, que se fue al entrar `Próximo` (AC-WKT-14-15); nada sale de la barra |
| **CAP-20** | TV | CAP-19 en inglés | `Warmup`, `Up next: Cooldown`, la cuenta y el reloj enteros, con los aires normales y sin la pastilla, que no entra ni con `>150W…` |
| **CAP-21** | L640 | `Prueba 4a` con `R-4(23)`, a los 150,2 s (el trabajo 2 a la mitad) | 6 píldoras: la 0 sólida amarilla, la 1 hueca rosa (150 W contra 160), la 2 sólida verde, la 3 en curso (relleno 0,503), la 4 y la 5 pendientes; la pastilla entera; SPM y Watts fuera, con `▲`; nada se encima; la llave sin tapar nada |
| **CAP-22** | TV | `Serie larga` con `R-4`, a los 3016,6 s (el paso 100, a la mitad; `Próximo` entró a los 3015,0 y ya está quieto) | **200 pasos**, en orden, en la franja continua y sin salir de la barra; lo hecho, lo que falta y el **pulgar** del paso 100, con el relleno en 0,553, más alto y más ancho que la franja (AC-WKT-14-16); se distinguen los hechos, el en curso y los pendientes, sin marco; la fila de información como siempre (`Trabajo`, `Próximo: Descanso`, `0:14`, el reloj `10:50`) |
| **CAP-23** | TH | `Prueba 4a` con el simulador, a los 50,2 s (40 s del final del trabajo 1) | 6 píldoras del mismo ancho, la 1 en curso (relleno 0,337); la fila con la pastilla entera y el reloj; como CAP-23 de la 4a, sin encimarse |
| **CAP-24** | W1280 | CAP-23 en la ventana | Comparable con `docs/image-03.png`, que muestra un paso sin pastilla: las píldoras arriba, `Trabajo` y la pastilla entera a la izquierda, y la cuenta y el reloj pegados a la derecha. En el legacy, con pastilla, la cuenta y el reloj quedarían corridos hacia el centro (DV-46) |
| **CAP-25** | TH | `Estados` a los 47,6 s | El halo y la cuenta grande `3` en horizontal, fuera de la zona del bote |
| **CAP-26** | TH | `Estados` a los 20,6 s | El sello `No alcanzado` en horizontal, sin tapar Split ni SPM |
| **CAP-27** | TV | `Estados`: `Pausa` a los 47,0 s y la toma a los 47,6 s | El velo y `PAUSA`; **sin** la cuenta grande; la cuenta `0:03` agrandada y teñida (los pulsos siguen en pausa) y el halo |
| **CAP-28** | TV | `Corta` con `R-4` y la base que falla al cerrar la sesión; `Terminar` en el diálogo | Workout en reposo con el aviso `No se pudo guardar la sesión`, entero; ningún diálogo encima |
| **CAP-29** | L640 | `Estados` a los 47,6 s | La cuenta grande y el halo en la ventana baja: entran sin tapar tarjetas ni la zona del bote |
| **CAP-30** | L640 | `Estados` a los 20,6 s | El sello en la ventana baja: en una línea, entre la barra y las tarjetas, sin cortarse |
| **CAP-31** | TV | `Texto largo` a los 2,2 s (18 s del final, todavía sin `Próximo`) | **El corte por fragmentos**: `Calentamiento`, la pastilla `>150W · 24 spm…` con el `…` **blanco** pegado (dos fragmentos enteros; `02:05/500m` no entra y ningún número queda cortado), `0:18` y `10:00`, con los aires en 6; nada sale de la barra. Es el mismo paso de CAP-19 antes de que entre `Próximo` (AC-WKT-14-03, -15) |

### 7.4 Secuencia de la demo

W1280, español, modo simulador sin ruido, `F-4A`, reloj de prueba y zona UTC−03:00. Cada toma sigue a la anterior sin rearmar nada.

| ID | Paso | Qué tiene que verse |
|---|---|---|
| **DEMO-01** | Workout | Reposo con `Entrenamiento libre` y `Prueba 4a` entre las de `F-4A` |
| **DEMO-02** | `Entrenamiento libre`, a los 3,2 s | El HUD entero después de la entrada; la barra del libre con `10:00` y `EN MARCHA` |
| **DEMO-03** | `Pausa` | El velo, `PAUSA`, `PAUSADO` y `Reanudar` |
| **DEMO-04** | `Reanudar`; `Terminar` → `Terminar` | Workout en reposo, sin diálogo ni aviso |
| **DEMO-05** | `Prueba 4a` → `Iniciar`, a los 18,2 s | 6 píldoras, la amarilla al 60,7 %; `Calentamiento`, `Próximo: Trabajo`, `0:12` y el reloj |
| **DEMO-06** | A los 27,6 s | El halo de la píldora 0 y la cuenta grande `3` |
| **DEMO-07** | A los 90,6 s (fin del trabajo 1, a 150 W) | `No alcanzado` · `150 / 160 W · 24.0 / 22 spm`; la píldora 1 hueca y la 2 empezando |
| **DEMO-08** | A los 100 s: llave → `+` de Watts dos veces (170) y `+` de Ritmo dos veces (26); la toma, a los 101,6 s, con el panel ya abierto | El panel abierto con Split, Watts y la barra a la vista. El sello ya se fue (se vio de los 90,0 a los 92,0 s): el panel termina 8 dp debajo de la barra en su alto mayor, así no tapa su lugar (AC-BLE-16-03) |
| **DEMO-09** | A los 180,6 s (fin del trabajo 2) | `Objetivo cumplido` · `170 / 160 W · 26.0 / 24 spm`; la píldora 3 sólida |
| **DEMO-10** | A los 239,2 s | La cuenta grande `1` y el halo de la píldora 5; sin `Próximo` |
| **DEMO-11** | A los 242 s | El diálogo con `Tiempo` `04:00`, la distancia y las kcal del monitor; detrás, las píldoras finales: la 1 hueca, la 3 sólida y la 5 llena, en curso |
| **DEMO-12** | `Terminar` | Workout en reposo, sin aviso |

### 7.5 Videos

| ID | Tamaño y duración | Qué se hace | Qué tiene que verse |
|---|---|---|---|
| **VID-01** | TV, 2,5 s | Workout → `Entrenamiento libre` | **Entrada "sumergirse"**: Workout funde a negro en 220; aparece el fondo; la cascada (barra a los 400, SPM 520, Split y Watts 600, banda 680, kcal 760, controles 840, cada uno en 280, con una subida corta); la banda entra con el reloj real y pasa a `00:01` sin esperar a la cascada; la llave entra al final; nada salta |
| **VID-02** | TV, 2,5 s | Libre a los 12 s: `Terminar` → `Terminar` | **Salida**: el HUD se va junto con su última foto (`00:12`, `EN MARCHA` o `FINALIZADO`), nunca `00:00` ni `—`; negro; Workout entra con su barra de pestañas; sin aviso |
| **VID-03** | W1280, 2,5 s | Workout → `Prueba 4a` → `Iniciar` | La entrada con rutina: la barra de pasos con sus 6 píldoras llega primero; la cuenta ya corre |
| **VID-04** | TV, 18,5 s | `Estados` con `R-4`, de los 4,5 a los 23 s | El halo desde `0:05` y la cuenta grande `3`, `2`, `1`; a los 10 s la píldora 0 se solidifica con el destello, la 1 empieza a llenarse de forma continua y entra `Objetivo cumplido`, que se va a los 2 s; a los 20 s el borde de la 1 se dibuja y entra `No alcanzado`. El reloj de la banda, la cuenta y la cuenta grande cambian en el mismo cuadro de cada segundo |
| **VID-05** | TV, 8 s | `Corta` con `R-4`, de los 26 a los 34 s; `Terminar` a los 33 s | La cuenta grande del último paso; a los 30 s la última píldora completa su relleno y no se solidifica; a los 30,3 entra el diálogo: el check se dibuja y los totales cuentan hasta `00:30`, `1120 m` y `43`; `Terminar` → la salida |
| **VID-06** | TV, 5 s | Libre: `Pausa`, 2 s, `Reanudar` | El velo en 300, `PAUSA` entra, el chip cambia en el acto; al reanudar, todo vuelve en 240; el reloj de la banda quieto en pausa |
| **VID-07** | TV, 6 s | Libre con el pulsómetro simulado: llave → `Simular caída del pulsómetro` | BPM se va y kcal baja deslizándose; a los 3 s BPM vuelve y kcal sube; nada salta |
| **VID-08** | TV, 2 s | VID-01 con "reducir movimiento" | Un fundido de 120, sin negro ni cascada; el reloj corre igual |

---

## 8. Qué se verifica dónde

| Dónde | Qué |
|---|---|
| **[dotnet]** | El estado de cada píldora por índice y su resultado, y la regla de LT-46 (AC-WKT-14-08, -13, AC-WKT-15-02 a -05). Los lugares de la fila con muchos pasos y el pulgar (AC-WKT-14-16). La regla de la fila de información: el corte por fragmentos, los aires y cuándo no se muestra la pastilla (AC-WKT-14-03, -15). Cuándo hay cuenta grande y qué número (AC-WKT-14-18). El sello: cuándo, cuál y sus textos con los casos de redondeo (AC-WKT-09-08, -09). El diálogo de completado: una vez por sesión, nunca a mano, los valores fijos, la espera del motor, `Terminar` que espera el cierre y el segundo plano (AC-WKT-21-01, -04, -06 a -09, AC-WKT-13-11). El chip por fase (AC-WKT-16-03). El reloj de pared con el reloj y la zona de prueba (AC-WKT-19-02, -04). P-17 de la 4a con el ciclo de vida de prueba en los dos casos del reloj monótono (AC-WKT-02-11, -04). La secuencia del motor con y sin HUD (AC-WKT-13-14). El pedido de las barras del sistema, en orden (AC-APP-15-04). El catálogo de 214 claves (AC-APP-10-11). El oráculo con los guiones de QA-V4a-L02 (AC-WKT-21-13). La regla de la llave y el techo del panel en el entrenamiento (AC-BLE-16-02, -03) |
| **[Editor-EM]** | Las mismas pruebas dentro de Unity, con y sin simulador |
| **[Editor-PM]** | El HUD montado. La fila de píldoras (los cuatro estados que se ven, LT-46, el halo, el cambio de paso, el último en curso, muchos pasos y la fluidez con 200). La fila de información con el reloj pegado al borde, el corte por fragmentos con el `…` blanco y la fila más larga. La barra del libre con el reloj, el título que se achica, el chip en sus tres fases y el punto. El velo de pausa. El sello y la cuenta grande, en vertical y horizontal. El diálogo de completado (contenido, modal, teclado, tamaños, la espera del cierre, el error después del diálogo, el segundo plano y la vuelta). La entrada y la salida (estados, no tiempos) y nada animando al salir. El pedido de las barras con la interfaz de prueba. Las condiciones (el ancho del divisor y el primer Tab con anillo en el HUD, en Workout y en el editor). Nada se encima en los cuatro perfiles. La auditoría de la llave. Todo sin y con `-Simulator` (la cinta y la llave, solo con) |
| **[Captura]** | CAP-01 a CAP-31, DEMO-01 a DEMO-12 y VID-01 a VID-08 (§7), más la regeneración única de las tomas de las It. 2, 3 y 4a |
| **[Device-Win]** | Build de simulador: la demo en la ventana real; la ventana que no cambia al entrar ni al salir (AC-APP-15-02); el teclado con el primer Tab (AC-APP-05-07, AC-WKT-21-11); minimizar con la rutina por terminar y ver el diálogo al restaurar (AC-WKT-02-09, AC-WKT-21-09); el registro del build sin avisos de elementos fuera de pantalla después de tres entrenamientos (AC-WKT-13-09); la fluidez con `Serie larga` (AC-WKT-14-19). **P-17 de la 4a la verifica el usuario** suspendiendo el equipo (demo (c), AC-WKT-02-11). Build normal: el entrenamiento anda igual sin la capa del simulador |
| **[Device-And]** | Solo con el módulo y el teléfono: las barras del sistema ocultas y su regreso (AC-APP-15-01), "Atrás" en la entrada y con el diálogo de completado (AC-WKT-13-07, AC-WKT-21-02) y el primer cuadro al volver (AC-WKT-02-08). Si no están: NO VERIFICABLE |
| **[Apple]** | La barra de estado de iOS y la ventana de macOS (AC-APP-15-03), en la 10b |
| **[QA]** | Strava ausente en el diálogo (AC-WKT-21-10); bases del usuario y del legacy intactas (AC-HIS-20-04, R-14); el repositorio legacy sin cambios; sin rutas de la PC en lo versionado (también en `manifest.json`, `INDEX.md` y los registros que se citen) |

---

## 9. Checklist de paridad

| # | Comportamiento del legacy | Legacy | Test legacy | AC |
|---|---|---|---|---|
| 1 | Cuatro estados de la píldora; el matiz es el del tipo | `lib/features/workout/step_pill.dart:6`, `:36`, `:41-55` | `test/step_pill_test.dart:19` (LT-46) | AC-WKT-15-01 a -05 |
| 2 | Opacidades 18 / 30 / 85 % | `step_pill.dart:30-32` | LT-46 | AC-WKT-15-02 a -05 |
| 3 | No cumplida: interior al 18 % y borde de 1,5 del color al 85 % | `step_pill.dart:49-51` | LT-46 | AC-WKT-15-05 |
| 4 | Hecha sin objetivo = hecha cumplida | `step_pill.dart:52-54` | LT-46 | AC-WKT-15-04 |
| 5 | En curso: relleno sólido = progreso acotado; sin relleno con 0; sin borde ni marco | `step_pill.dart:71-76`; commit `3ae0f3a` | LT-46 | AC-WKT-15-03 |
| 6 | Alto 8, radio 4 | `step_pill.dart:57-60` | — | AC-WKT-15-01 |
| 7 | Halo del paso por terminar: color al 30 % + 40 % del pulso, desenfoque 8, expansión 2, radio 5. La sombra se pinta también debajo de la píldora y tiñe la pista | `step_pill.dart:82-101` | — | AC-WKT-14-11 (la pista sin teñir: §11.1.1) |
| 8 | Mismo ancho para todas, 3 px entre píldoras | `step_pill.dart:103`; `immersive_workout_screen.dart:363-364` | — | AC-WKT-14-08 (Q-59; el pulgar de las angostas: DV-40) |
| 9 | Una píldora por paso de la rutina expandida | `workout_provider.dart:122-123`; `immersive_workout_screen.dart:338` | — | AC-WKT-14-08 |
| 10 | Estado por índice: hecha antes, en curso la actual, pendiente después | `immersive_workout_screen.dart:367-369` | — | AC-WKT-14-08 |
| 11 | Progreso solo en la en curso; el resultado de cada paso | `immersive_workout_screen.dart:370-371` | — | AC-WKT-14-08, -09 |
| 12 | El halo solo por terminar y solo en la en curso | `immersive_workout_screen.dart:372`; `workout_provider.dart:39-41` | — | AC-WKT-14-11 |
| 13 | Fila de píldoras con relleno 10 / 8 / 10 / 4 | `immersive_workout_screen.dart:359-360` | — | AC-WKT-14-07 |
| 14 | Contenedor de dos filas | `immersive_workout_screen.dart:348-357` | — | AC-WKT-14-01 |
| 15 | El reloj de pared, a 10 de la cuenta | `immersive_workout_screen.dart:472-474` | — | AC-WKT-14-14 |
| 16 | Un solo reloj de pulso de 600 + 600 que sigue en pausa | `immersive_workout_screen.dart:319-322` | — | AC-WKT-14-06, -11 |
| 17 | El último paso queda en curso: el índice no avanza y el progreso es 1 | `workout_provider.dart:31-36`, `:203-216` | — | AC-WKT-14-13 (Q-52) |
| 18 | El último paso se evalúa, pero su resultado no se ve | `workout_provider.dart:204`; `immersive_workout_screen.dart:367-371` | `test/step_target_tracker_test.dart` (LT-39 a LT-44, It. 4a) | AC-WKT-14-13, AC-WKT-09-08 |
| 19 | El paso por distancia no se llena | `workout_provider.dart:31-36` | — | AC-WKT-14-09 (Q-51) |
| 20 | Con muchos pasos el ancho se reparte; con más de ≈ 120 en un teléfono, miden 0 | `immersive_workout_screen.dart:361-375` | — | AC-WKT-14-16 (cambia: DV-40) |
| 21 | La barra no responde al toque | `immersive_workout_screen.dart:348-482` | — | AC-WKT-14-17 |
| 22 | Barra del libre: contenedor | `immersive_workout_screen.dart:497-504` | — | AC-WKT-16-01 |
| 23 | Barra del libre: ícono, 8, título, espacio, reloj, 10, chip | `immersive_workout_screen.dart:505-519` | — | AC-WKT-16-02 |
| 24 | Chip: textos y colores por fase; `—` en otra | `immersive_workout_screen.dart:530-535` | — | AC-WKT-16-03, -04 (el `—`: DV-41) |
| 25 | Chip: fondo al 15 %, borde al 50 %, radio 8, 8 × 3, 11/800, tracking 0,8; texto en el color de la fase | `immersive_workout_screen.dart:536-547` | — | AC-WKT-16-03 (el texto: DV-44; el peso 700: §11.1.1) |
| 26 | Textos del chip y de la barra del libre, fijos en español | `immersive_workout_screen.dart:509-510`, `:531-533` | — | AC-WKT-16-03, AC-APP-10-11 (Q-01) |
| 27 | Reloj: `HH:mm`, ícono `schedule` 16 al 70 %, 5, 18/800 con cifras tabulares, del tamaño de la cuenta | `series_panels.dart:41-47`, `:92-103` | — | AC-WKT-19-01 |
| 28 | Reloj: mira la hora cada segundo y se redibuja al cambiar la hora o el minuto | `series_panels.dart:72-82` | — | AC-WKT-19-02 |
| 29 | Reloj: su propio temporizador, también en pausa y terminada; la hora real | `series_panels.dart:57-82` | — | AC-WKT-19-03 |
| 30 | Reloj: la variante con marco no se usa | `series_panels.dart:104-110`; `immersive_workout_screen.dart:474`, `:515` | — | AC-WKT-19-05 |
| 31 | Diálogo de completado: una vez por sesión | `immersive_workout_screen.dart:29`, `:49-55`, `:61` | — | AC-WKT-21-01, AC-WKT-13-11 |
| 32 | Aparece al pasar a terminada, sin esperar al guardado | `workout_provider.dart:189`, `:210-216` | — | AC-WKT-21-01 |
| 33 | Modal: no se cierra afuera, con "Atrás" ni con Escape | `immersive_workout_screen.dart:207-212` | — | AC-WKT-21-02 |
| 34 | Título: check `#06D6A0` de 28, 12 y `workoutCompleted` | `immersive_workout_screen.dart:213-219` | — | AC-WKT-21-03 |
| 35 | Texto `workoutCompletedMessage`, 16 y el resumen | `immersive_workout_screen.dart:220-229` | — | AC-WKT-21-03 |
| 36 | Resumen: caja blanca al 4 %, radio 8, relleno 12; filas con ícono `#00B4D8` de 20, 12, etiqueta al 70 % y valor en negrita de 16 | `immersive_workout_screen.dart:973-1019` | — | AC-WKT-21-03 |
| 37 | Tiempo = reloj activo; Distancia `{n} m` y Calorías del monitor, leídos una vez al armar el diálogo | `immersive_workout_screen.dart:50-54`, `:207-229`, `:965-971`, `:981-991` | — | AC-WKT-21-03, -04 (los del fin: DV-43) |
| 38 | Etiquetas del resumen fijas en español | `immersive_workout_screen.dart:981-990` | — | AC-WKT-21-03, AC-APP-10-11 (Q-01) |
| 39 | Sin Strava, un solo botón `workoutFinish` que cierra y vuelve a reposo | `immersive_workout_screen.dart:260-291` | — | AC-WKT-21-06, -10 |
| 40 | Variante de Strava: dos botones y subida automática | `immersive_workout_screen.dart:198-205`, `:231-259`, `:264-286` | — | It. 12 (AC-WKT-21-10) |
| 41 | Con el simulador, nada de Strava | `immersive_workout_screen.dart:198-201` | — | AC-WKT-21-10 (R-01) |
| 42 | `Terminar` del diálogo no espera al guardado | `immersive_workout_screen.dart:261-289`; `workout_provider.dart:252-303` | — | AC-WKT-21-06 (cambia: DV-42) |
| 43 | El motor queda en terminada hasta el `reset()` del diálogo | `immersive_workout_screen.dart:235`, `:255`, `:287` | — | AC-WKT-21-08, -13 |
| 44 | Al volver a reposo, el inmersivo se cierra | `immersive_workout_screen.dart:57-65` | — | AC-WKT-13-04, -08 |
| 45 | Terminar a mano: el diálogo de completado puede asomar | `immersive_workout_screen.dart:49-55`, `:125-148` | — | AC-WKT-21-07 (Q-53) |
| 46 | La rutina termina con el diálogo de terminar abierto: se apilan | `immersive_workout_screen.dart:49-65`, `:141` | — | AC-WKT-20-07 |
| 47 | Barras: modo inmersivo persistente al crear la página y "de borde a borde" al destruirla | `immersive_workout_screen.dart:32-41` | — | AC-APP-15-01, -04 |
| 48 | En escritorio el modo inmersivo no cambia la ventana | `immersive_workout_screen.dart:34` | — | AC-APP-15-02 |
| 49 | La salida redibuja la página en reposo: la barra del libre, `—` y `00:00` | `immersive_workout_screen.dart:57-65`, `:94-96`, `:534`, `:569-577` | — | AC-WKT-13-08 (cambia: DV-41) |
| 50 | El reloj cuenta disparos del temporizador: con el equipo suspendido no cuenta | `workout_provider.dart:147`, `:247` | — | AC-WKT-02-11 (P-17 de la 4a) |
| 51 | La entrada: la transición de página de la plataforma | `workout_screen.dart:169-175` | — | AC-WKT-13-06 (mejora EXR) |
| 52 | Promedios por paso: vatios y SPM sobre las lecturas remando, split sobre las mayores que 0 | `step_target_tracker.dart:30-40`, `:50-52` | `test/step_target_tracker_test.dart` | AC-WKT-09-09 |
| 53 | Los valores en vivo del HUD siguen con el diálogo abierto | `immersive_workout_screen.dart:99-108` | — | AC-WKT-21-08 |
| 54 | Fila de información: el tipo, 6, la pastilla en un `Flexible` con `…` (corte por carácter), un `Spacer`, `Up next` con relleno 7 × 3, 8, la cuenta, 10 y el reloj. La pastilla y el `Spacer` se reparten el espacio libre por mitades: la pastilla recibe a lo sumo la mitad, nunca se oculta, y con pastilla la cuenta y el reloj no quedan pegados al borde | `immersive_workout_screen.dart:380-476` | — | AC-WKT-14-03, -04, -14, -15 (cambia: DV-45, DV-46) |
| 55 | Barra del libre: el título sin `Flexible`, que en 390 y 360 se sale de la barra | `immersive_workout_screen.dart:505-519` | — | AC-WKT-16-06 (cambia: DV-47) |
| 56 | Diálogo: el título en una `Row` sin `Flexible`, que en un teléfono se sale del diálogo | `immersive_workout_screen.dart:213-219` | — | AC-WKT-21-12 (cambia: DV-47) |

---

## 10. Mejoras EXR (cómo se tiene que sentir)

Son agregados de presentación. Ninguno cambia un dato, un umbral, un orden ni una regla del legacy, y ninguno adelanta ni frena el reloj (AC-WKT-13-14).

1. **Una línea de tiempo de regata.** La rutina entera se lee arriba de un vistazo: lo que ya pasó, sólido o hueco, lo que está pasando, que se llena de forma continua, sin tics, y lo que falta, tenue. Al cambiar de paso, la píldora que termina "se solidifica" con un destello breve o se le dibuja el borde, y la siguiente toma la posta.
2. **El final del paso se siente venir.** En los últimos 15 s `Próximo` respira. En los últimos 5, la píldora late con un halo de su color y la cuenta crece. En los últimos 3, un `3`, `2`, `1` grande y sereno, que no tapa el bote. El remero sabe que se viene el cambio sin leer números.
3. **El resultado, en el momento.** Al cerrar un paso con objetivo, 2 s de sello: "Objetivo cumplido" en verde o "No alcanzado" en ámbar, con el promedio contra el objetivo. Informa sin regañar y se va solo.
4. **Sumergirse.** Entrar al entrenamiento es zambullirse: la app se apaga a negro, en el teléfono las barras del sistema desaparecen con ella, el agua aparece y el HUD llega en cascada, de arriba abajo. El reloj ya corre desde el toque. Salir es el camino inverso, sin pasar nunca por un tablero en cero.
5. **La pausa se ve.** El mundo se oscurece un poco, `PAUSA` aparece claro y todo sigue legible y en vivo. Reanudar devuelve la luz.
6. **Un cierre con peso.** El diálogo de rutina completada llega un instante después del último segundo: el check se dibuja y los totales suben hasta su valor exacto. Espera al remero todo lo que haga falta.
7. **La hora y el estado, siempre a mano.** La hora, con cifras de tablero que no bailan, y en el libre un chip `EN MARCHA` con un punto vivo que dice "esto está corriendo".
8. **Nada salta.** kcal se desliza cuando aparece el pulso, las cifras son las de tablero, el divisor de la banda está en su lugar y ningún elemento se corre de golpe: la cuenta y el reloj quedan siempre en el borde derecho de la barra, y la fila decide al empezar cada paso cómo entra, así no se reacomoda cuando aparece `Próximo`.
9. **Respeto por quien lo pide.** Con "reducir movimiento", fundidos y realces fijos; con teclado, el primer Tab ya muestra dónde está el foco.

Se comprueba con AC-WKT-14-10 a -12, -18, AC-WKT-09-08, AC-WKT-13-06, -08, -12, AC-WKT-16-05, AC-WKT-17-11, -12, AC-WKT-21-05, AC-APP-05-08 y los videos VID-01 a VID-08, que revisan QA y el Designer.

---

## 11. Decisiones aplicadas y gate de la It. 4b

| Decisión | Qué se aplica | Confirmar en el gate |
|---|---|---|
| **D-ANI-05** | La cuenta grande de los últimos 3 s (AC-WKT-14-18) y el sello "Objetivo cumplido / No alcanzado" de 2 s (AC-WKT-09-08, -09). Si el usuario la rechaza, queda la cuenta de la barra y el resultado solo en la píldora | **Sí** |
| Q-52 | El último paso queda en curso, sin resultado ni sello (AC-WKT-14-13, AC-WKT-09-08) | No |
| Q-53 | Al terminar a mano nunca aparece el diálogo de completado (AC-WKT-21-07). La parte de Strava ("preguntar" que no se cierra solo) es de la It. 12 | No |
| Q-59 | Píldoras del mismo ancho (AC-WKT-14-08) | No |
| Q-51 | El paso por distancia no se llena (AC-WKT-14-09) | No |
| Q-01 | **12 claves nuevas** (§11.2), 214 en total; el español idéntico al legacy donde el legacy tiene el texto fijo | No (ya aceptada en la It. 1) |
| Q-06 | El reloj de pared, siempre de 24 h; el promedio del sello, con punto decimal | No |
| P-17 de la 4a | **Aceptada en el gate de la 4a**: el tiempo con el equipo suspendido no cuenta (AC-WKT-02-11). Lo verifica el usuario en la demo (c) | No (ya aceptada) |
| DT4a-15 | El motor espera al diálogo de completado (AC-WKT-21-08) | No |
| Q-88 | En escritorio, el inmersivo no pone la ventana en pantalla completa (AC-APP-15-02); la pantalla completa real sigue en el backlog | No |
| DV-40 a DV-47 | Diferencias visibles con el legacy, a propósito (§11.1). DV-45 a DV-47 salen del gate de armonía (§16) | **Sí**, con las DV, como en las iteraciones anteriores |
| Sistema visual | Diferencias de aspecto que no cambian datos, textos, orden ni significados (§11.1.1) | No (informativas) |
| DT4b-02 a -05, -09, -17 | Las decisiones de 02-tech-design §8.1 que tocaban al PO, confirmadas en §16: el sello en una tercera fila de la barra (AC-WKT-09-08, -14-01); la entrada y la salida con los números de esta spec; la cuenta grande de 64 en su disco; las barras del sistema con dos inmersivos (AC-APP-15-04); los 15 s del cierre desde el fin de la rutina (AC-WKT-21-06); el peor caso del presupuesto del HUD | No |
| DN4b-23 | El techo del panel del simulador en el entrenamiento (AC-BLE-16-03) | No (es la herramienta del simulador) |
| P-01 a P-16 | Preguntas abiertas con su decisión por defecto (§14) | Las marcadas "Usuario" |

### 11.1 Diferencias visibles con el legacy (a propósito)

Siguen la numeración de la 4a. No incluyen las mejoras EXR de §10 (la entrada y la salida, el velo, el halo continuo, el sello, la cuenta grande, el conteo) ni el sistema visual de la app (tipografía, íconos, tamaños por la escala, vidrio), que no cambian datos, textos, orden ni significados; las diferencias del sistema visual que tocan un criterio están en §11.1.1.

| ID | Legacy (verificado) | App | Motivo | AC |
|---|---|---|---|---|
| DV-40 | Las píldoras son `Expanded` separadas por 3 px. Cuando las separaciones se comen todo el ancho (más de ≈ 120 pasos en un teléfono de 411; ≈ 410 en una ventana de 1280), cada píldora mide 0 y la fila queda vacía: solo se ve el halo de la en curso (`immersive_workout_screen.dart:361-375`). Antes de eso, con píldoras de pocos dp, la en curso y la hueca casi no se distinguen | Siempre se ven todos los pasos, en orden y en lugares del mismo ancho: la separación se achica y, en el extremo, la fila es una franja continua donde cada tramo conserva el color y el estado de su paso. Con píldoras de menos de 6 base (en TV, desde 50 pasos), la en curso es un **pulgar** más ancho y más alto sobre su lugar, y la hueca lleva dos rieles en vez del borde entero | Defecto: el editor deja guardar hasta 999 repeticiones (Q-26, Q-92). El pulgar es lo que deja ver "acá estás" en una franja de tramos de 5 px | AC-WKT-14-08, -16, AC-WKT-15-05 |
| DV-41 | Después de `reset()`, la página sale con la transición de la plataforma y se redibuja en reposo mientras sale: la banda en `00:00`, la barra de una rutina pasa a ser la del libre (`:94-96`) y el chip dice `—` (`:534`). Es O-01 de la 4a | Mientras sale, el HUD conserva la última foto de la sesión | Defecto visual | AC-WKT-13-08, AC-WKT-16-04 |
| DV-42 | `Terminar` del diálogo de completado llama a `reset()` sin esperar al guardado: si todavía no había terminado, el cierre falla en silencio (`_startedAt` ya vacío, `workout_provider.dart:283-299`) y la sesión queda sin fin ni totales | `Terminar` espera el cierre, con el indicador si tarda más de 300 ms; si el cierre falla, Workout avisa `No se pudo guardar la sesión` | Q-13; es la misma carrera de Q-54 | AC-WKT-21-06 |
| DV-43 | El resumen lee el reloj, la distancia y las kcal **una sola vez**, cuando arma el diálogo, un cuadro después del fin (`:50-54`, `:207-229`, `:965-991`), y no los cambia: ni las lecturas nuevas, ni rotar, ni cambiar el tamaño de la ventana lo vuelven a leer (la sonda de QA, [03-qa-harmony-paridad §3.3](03-qa-harmony-paridad.md)). Si la rutina termina con la app en segundo plano en Android, arma el diálogo recién al volver, con la lectura de ese momento: el cuadro siguiente al fin no llega antes ([03-qa-harmony-paridad §8](03-qa-harmony-paridad.md)) | Siempre los totales del instante en que terminó la rutina (los del punto final, AC-WKT-06-06 de la 4a), también si terminó en segundo plano | En primer plano se ve lo mismo: entre el fin y el diálogo pasa un cuadro. La regla no depende de cuándo se arma el diálogo: el resumen es el de la sesión que se guardó | AC-WKT-21-04, -09 |
| DV-44 | El texto del chip va en el color de la fase sobre su tinte al 15 % (`:543-547`): `FINALIZADO` en `#EF476F` da ≈ 3,7:1 en el peor vidrio, como el tipo trabajo en la pastilla (DV-38) | `FINALIZADO` en **`#FF7A92`** (`phase.finished.hud`, un color nuevo del sistema visual), la variante legible del mismo matiz: 4,677:1 sobre su tinte. `#FF5D7F`, la del tipo trabajo, no llega (3,942:1). `EN MARCHA` y `PAUSADO` siguen en `#06D6A0` y `#FFD166`; el tinte y el borde, en el color base | Contraste del texto chico (D-ART-01, R-10), con la regla de color de tipo de DV-12. Lo midió el Designer (04-design-notes §2.1, §8.1) | AC-WKT-16-03 |
| DV-45 | La pastilla de objetivo es un `Flexible` que comparte el espacio libre con un `Spacer` (`:394-416`): recibe **a lo sumo la mitad**, se corta por carácter con `…` y nunca se oculta. En un teléfono de 411, en el estado de CAP-01, `18 spm` ya se corta (40 de 48 dp) con 40 dp vacíos al lado, y con `Texto largo` queda una franja teñida de 8 dp sin texto (la sonda de QA, [03-qa-harmony-paridad §3.1](03-qa-harmony-paridad.md)). Los aires (6, 8 y 10) y el relleno de `Up next` (7 × 3) no cambian nunca | La pastilla puede usar todo el espacio libre. Se corta por **fragmentos enteros**, con el `…` blanco pegado, y nunca en medio de un número. Para que entre su primer fragmento, antes se achican los aires (8 y 10 → 6) y el relleno de `Próximo` (7 → 4). Si ni así entra, no se muestra. Todo se decide al empezar cada paso, así nada salta cuando entra `Próximo` | Un número cortado informa mal (`>15…` se lee "más de 15") y una franja sin texto no informa nada. Los objetivos que cuentan siguen en las tarjetas (`obj.`) | AC-WKT-14-03, -04, -14, -15 |
| DV-46 | Con pastilla, el grupo de la derecha (`Up next`, la cuenta y el reloj) **no queda pegado al borde**: lo que la pastilla no usa de su mitad queda vacío al final de la fila (`:397-416`). En el estado de CAP-01, el reloj queda a 427 dp del borde en una ventana de 1280 y a 244 en TH. Además **salta**: en un teléfono de 411, el reloj se corre 43 dp cuando entra `Up next`, y vuelve al borde al pasar a un paso sin pastilla. Sin pastilla queda pegado al borde, como en `docs/image-03.png` (la sonda de QA, [03-qa-harmony-paridad §3.1](03-qa-harmony-paridad.md)) | La cuenta y el reloj van siempre pegados al borde derecho de la fila, sin saltos, desde la 4a | Nada salta (mejora EXR §10.8). La hora y la cuenta quedan siempre en el mismo lugar | AC-WKT-14-14; CAP-24 |
| DV-47 | Dos títulos sin `Flexible` que se salen de su caja: el de la barra del libre (`:505-519`), que en 411 entra justo y en 390 y 360 se sale de la barra y empuja afuera el reloj y el chip, y el del diálogo de completado (`:213-219`), que en un teléfono de 411 o 360 termina a 406 dp con el diálogo hasta 371 o 320 (la sonda de QA, [03-qa-harmony-paridad §3.4](03-qa-harmony-paridad.md)) | El título del libre se achica hasta 0,8 y, solo si ni así entra, se recorta con `…`: en TV y en 360, en español, entra entero a ≈ 0,8 (0,805). El del diálogo se parte en dos líneas junto al check | Defecto visual: nada sale de su caja (AC-WKT-16-06, AC-WKT-21-12). Achicar es la regla del HUD para lo que no entra, como el sello | AC-WKT-16-06, AC-WKT-21-12 |

#### 11.1.1 Del sistema visual (informativas)

No van al gate: cambian el aspecto, no datos, textos, orden ni significados. Se registran para que la "paridad" de cada criterio quede dicha.

| Legacy (verificado) | App | Dónde |
|---|---|---|
| La sombra del halo se pinta debajo de toda la píldora en curso y se ve a través de la pista al 30 %, que se aclara con el pulso (`step_pill.dart:82-101`; la sonda de QA, [03-qa-harmony-paridad §3.2](03-qa-harmony-paridad.md)) | El halo va solo por fuera y la pista queda al 30 %, así el avance del relleno se sigue leyendo en los últimos 5 s | AC-WKT-14-11 |
| El halo se extiende ≈ 14 dp y pasa por debajo de la fila de información; se dibuja encima de la píldora anterior y debajo de la siguiente | El halo se apaga antes del borde de su fila y va debajo de todas las píldoras | AC-WKT-14-11 |
| El texto del chip, en 800 (`:546`) | En 700, porque la letra de los textos de la app no tiene 800; con el tracking se ve igual | AC-WKT-16-03 |
| El chip mide lo que su etiqueta: al pausar se angosta y el reloj se corre | El chip tiene un ancho fijo por idioma, así el reloj no se mueve (nada salta) | AC-WKT-16-03 |
| El relleno de la en curso, sin brillo | Un brillo fino en el borde de arriba del relleno: distingue la en curso llena de la hecha | AC-WKT-15-03 |
| El check del diálogo es el ícono de 28; los valores, en negrita de 16 | El check de 32, que se dibuja; los valores en las cifras de tablero | AC-WKT-21-03, -05; AC-WKT-17-12 |

### 11.2 Claves nuevas del catálogo (Q-01)

Español idéntico al legacy donde el legacy tiene el texto fijo, y su inglés. Con estas, el catálogo pasa de 202 a **214** claves por idioma.

| Clave | es | en | Parámetros | Dónde |
|---|---|---|---|---|
| `hudPhaseActive` | `EN MARCHA` | `ACTIVE` | — | chip de fase, en marcha (AC-WKT-16-03) |
| `hudPhasePaused` | `PAUSADO` | `PAUSED` | — | chip de fase, en pausa (AC-WKT-16-03) |
| `hudPhaseFinished` | `FINALIZADO` | `FINISHED` | — | chip de fase, terminada (AC-WKT-16-03) |
| `hudPausedLabel` | `PAUSA` | `PAUSED` | — | velo de pausa (AC-WKT-13-12) |
| `hudSealMet` | `Objetivo cumplido` | `Target met` | — | sello (AC-WKT-09-08) |
| `hudSealMissed` | `No alcanzado` | `Target missed` | — | sello (AC-WKT-09-08) |
| `hudSealWatts` | `{avg} / {target} W` | `{avg} / {target} W` | `avg`, `target` (texto: `150`, `--`) | detalle del sello (AC-WKT-09-09) |
| `hudSealSpm` | `{avg} / {target} spm` | `{avg} / {target} spm` | `avg`, `target` (texto: `24.0`, `22`) | detalle del sello (AC-WKT-09-09) |
| `hudSealSplit` | `{avg} / {target}/500m` | `{avg} / {target}/500m` | `avg`, `target` (texto: `02:06`, `02:05`) | detalle del sello (AC-WKT-09-09) |
| `workoutCompletedTime` | `Tiempo` | `Time` | — | resumen del diálogo (AC-WKT-21-03) |
| `workoutCompletedDistance` | `Distancia` | `Distance` | — | resumen del diálogo (AC-WKT-21-03) |
| `workoutCompletedCalories` | `Calorías` | `Calories` | — | resumen del diálogo (AC-WKT-21-03) |

Se reusan, sin cambios:

- del legacy: `workoutCompleted` (ahora en el título del diálogo, ya no en un aviso), `workoutCompletedMessage`, `workoutFinish` y `stepType*`;
- de la 4a: `hudFreeTitle`, `hudUpNext`, `workoutSaveError`, `stepDurationMeters` y `stepDurationNone`.

Las claves `historyStatTime` y `historyStatDistance` (`Tiempo`, `Distancia`) no se reusan en el diálogo: son del Historial (It. 9) y `historyStatCalories` dice `kcal`, no `Calorías`. `profileUploadToStrava` y las de los avisos de Strava son de la It. 12.

---

## 12. Restricciones de producto para el Architect

1. **Primero lo de la 4a.**
   - Las dos condiciones, cada una con su prueba: el divisor que no se achica (QA-V4a-03) y el primer Tab con anillo (QA-V4a-04).
   - Después WP-B0 (las cifras de tablero y el kerning, QA-V4a-06) y QA-V4a-05 (nada animando fuera del panel).
   - Una sola regeneración de `it2`, `it3` e `it4a`, con dos corridas iguales.

   QA las verifica antes de construir encima (§5.1).
2. **Determinismo y aislamiento**, como en la 4a: base, reloj, planificador y ciclo de vida de prueba. Se suman la zona horaria de prueba, la suspensión del equipo, la interfaz del sistema de prueba y la fase fija del pulso en las tomas (§0.4). **Nunca** las bases del usuario (AC-HIS-20-04). Ningún doble llega a un build instalable.
3. **Un motor; la presentación, aparte.** Ninguna animación (entrada, salida, velo, halo, cuenta grande, sello, relleno, diálogo, conteo) decide cuándo pasa algo: la secuencia del motor es la misma con y sin HUD y con reducir movimiento (AC-WKT-13-14).
4. **El fin espera al diálogo (DT4a-15).** Con la rutina terminada, el motor no vuelve a reposo ni borra los resultados de los objetivos hasta `Terminar` del diálogo (AC-WKT-21-08). `Terminar` espera el cierre de la sesión y nunca lo pierde (AC-WKT-21-06). Así el fin automático es el del legacy, y el oráculo lo compara entero (AC-WKT-21-13).
5. **P-17 de la 4a.**
   - La suspensión del equipo se distingue del segundo plano, de la ventana minimizada o sin foco y de la pantalla bloqueada.
   - El lapso suspendido no se procesa: no suma segundos, pasos, puntos ni lecturas de objetivos, y no cuenta como demora del cierre (AC-WKT-02-11).
   - Tiene que valer tanto si el reloj monótono del equipo cuenta la suspensión como si no; las pruebas cubren los dos casos.
   - El Architect elige la señal del sistema y deja documentado qué pasa si no llega.
   - En Android no se aplica: la pantalla bloqueada es segundo plano (Q-91).
6. **El presupuesto del HUD (R4a-03).** La fila de píldoras, con cualquier cantidad de pasos (200 en `Serie larga`, hasta los que deje guardar el editor), entra en el tope de elementos del HUD sin reservar memoria por cuadro y sin tirones (AC-WKT-14-16, -19). Para sumar las piezas nuevas hay que juntar elementos.
7. **La salida conserva la última foto (O-01).** Mientras el inmersivo sale, nada del HUD vuelve a dibujarse con el estado de reposo (AC-WKT-13-08).
8. **Nada huérfano al salir.** Al cerrarse el inmersivo no queda nada animando, como pidieron QA-V4a-02 y -05 (AC-WKT-13-09). Antes de salir, primero se cierra el diálogo de terminar, sin efecto (AC-WKT-20-07).
9. **Las barras del sistema, por plataforma** (ARCHITECTURE, tabla de plataforma): en Android, ocultas en modo persistente al entrar y visibles al salir; en iOS, la barra de estado (10b); en escritorio, sin efecto. Todo con una interfaz que la prueba pueda reemplazar (AC-APP-15-04).
10. **Textos**: las 12 claves de §11.2. Ningún texto visible nuevo queda fuera del catálogo, salvo los formatos (números, `:`, `.`, `--`, `--:--`, `?`, los dígitos de la cuenta grande).
11. **Identificadores estables para las pruebas**, por ejemplo: `hud-pill-row`, `hud-pill-<n>` (desde 0), `hud-wall-clock`, `hud-phase-chip`, `hud-phase-dot`, `hud-seal`, `hud-big-countdown`, `hud-paused-label`, `completion-dialog`, `completion-time`, `completion-distance`, `completion-calories`, `completion-finish`. Los nombres finales los fija el Architect.
12. **Strava**: ninguna llamada ni regla en la 4b. El diálogo deja lugar para la variante de dos botones de la It. 12.
13. **Builds de desarrollo**: solo en la carpeta de salida por defecto, para no disparar avisos nuevos del Firewall. Si el usuario tiene abierto su `RowMate.exe`, no se lo cierra: se espera y se reintenta (QA-V4a-08). Si no se puede, esa parte queda NO VERIFICABLE.

---

## 13. Fuera de alcance

- **It. 5**: los paneles de parciales y repeticiones (F-WKT-07, F-WKT-08, F-WKT-18).
- **It. 6a**: la escena; en la entrada, el vuelo de cámara que baja hasta el agua; en el diálogo de completado, la toma aérea; en la pausa, la desaturación de la escena y el bote que frena; la hora que conduce la luz; el fundido cruzado del HUD al rotar.
- **It. 6b**: el vidrio con desenfoque, también el del diálogo de completado.
- **It. 10a**: el remo real, el aviso "Reconectando…" sobre el HUD (Q-34) y el reloj de Android con la pantalla bloqueada (RA-19). **It. 10b**: lo [Apple] (AC-APP-15-03).
- **It. 12**: Strava en el diálogo de completado (dos botones, subida automática y sus avisos), el diálogo "preguntar" al terminar a mano que no se cierra solo (Q-53) y la subida que espera el guardado (Q-54).
- **It. 14**: la háptica (pausa y reanudar, cambio de paso, "éxito" al completar), los lectores de pantalla (DT-05) y el cierre de F-APP-15 (orientación por plataforma, ventana de escritorio).
- **Backlog** (fuera de la paridad):
  - píldoras proporcionales a la duración (Q-59: mismo ancho);
  - `Próximo: Descanso 1:00` con la duración y los metros que faltan en un paso por distancia (Q-51);
  - el sonido: el tic de los últimos segundos y la cuenta 3-2-1 sonora (BKL-03);
  - la onda de color que recorre la barra y el nombre del paso nuevo en grande;
  - las píldoras dentro del diálogo de completado ("la rutina resumida", P-14) y una celebración de llegada más allá del diálogo;
  - la pantalla completa real y la ventana que recuerda su tamaño en escritorio (Q-88, BKL-18);
  - un aviso al despertar el equipo.

---

## 14. Preguntas abiertas

| ID | Pregunta | Por defecto | Para |
|---|---|---|---|
| P-01 | ¿Se agregan la cuenta grande de los últimos 3 s y el sello de resultado (D-ANI-05)? | **Sí**. Si no: la cuenta de la barra, como hoy, y el resultado solo en la píldora | Usuario (gate) |
| P-02 | ¿El último paso lleva sello? | **No**: queda en curso sin mostrar su resultado (Q-52), y un instante después llega el diálogo de completado | — (PO) |
| P-03 | ¿Cómo se redondea el promedio del sello? | Hacia el lado desfavorable (vatios y SPM hacia abajo, split hacia arriba), para que el número nunca contradiga el veredicto (`159 / 160 W` es "No alcanzado") | — (PO) |
| P-04 | ¿En pausa se ve la cuenta grande? | **No**: en su lugar va `PAUSA`; la cuenta de la barra y el halo siguen latiendo (paridad) | — (PO) |
| P-05 | ¿Qué pasa con tantos pasos que no entran en la fila? | **Nunca desaparecen** (DV-40): la separación se achica y, en el extremo, la fila es una franja continua. Con píldoras de menos de 6 base, la en curso es un pulgar y la hueca lleva dos rieles (04-design-notes §3.2) | Usuario (gate, con las DV) |
| P-06 | ¿Qué textos en inglés tienen el chip, la pausa y el sello? | `ACTIVE`, `PAUSED`, `FINISHED`; `PAUSED`; `Target met` y `Target missed` (§11.2) | — (PO; se ven en CAP-08 y CAP-18) |
| P-07 | ¿El reloj de pared mide como la cuenta de la barra (20 por la escala) o 18, como dice ART_BIBLE §3.3? | **Como la cuenta**: es la regla del legacy (`series_panels.dart:99`, "mismo tamaño que la cuenta regresiva"), y la cuenta pasó de 18 a 20 con D-ART-02. **Confirmado** por el Designer (04-design-notes §1); la biblia se alinea en WP-D2 | Designer (alinear la biblia) |
| P-08 | ¿El diálogo de completado es opaco o de vidrio? | **Opaco** (`surface.overlay`) hasta la 6b, como el de terminar (DN4a-11): lo decidió el Designer (04-design-notes §3.8). Con el desenfoque de la 6b, el vidrio de ART_BIBLE §3.5 | Designer (decidido) |
| P-09 | ¿Qué es el velo de pausa sin escena? | El fondo provisorio se oscurece un 15 %; la desaturación llega con la escena (6a) | — (PO) |
| P-10 | ¿La salida conserva la última foto de la sesión? | **Sí** (DV-41) | Usuario (gate, con las DV) |
| P-11 | ¿`Terminar` del diálogo espera el cierre de la sesión? | **Sí** (DV-42) | Usuario (gate, con las DV) |
| P-12 | P-17 de la 4a en Android: ¿la pantalla bloqueada es una suspensión? | **No**: es segundo plano y la sesión sigue (Q-91; AC-WKT-02-10). La 10a mide el reloj del teléfono (RA-19) | — (PO) |
| P-13 | ¿Los totales del diálogo son siempre los del fin de la rutina? | **Sí** (DV-43). En primer plano el legacy muestra lo mismo; cambia solo si la rutina terminó en segundo plano en Android | Usuario (gate, con las DV) |
| P-14 | ¿El diálogo muestra la rutina resumida con sus píldoras finales (mejora EXR de F-WKT-21)? | **No** en la 4b: la barra queda a la vista detrás del velo. Backlog | — (PO) |
| P-15 | ¿Los controles responden durante la cascada de entrada? | **Sí**, desde que empiezan a aparecer; "Atrás", desde el primer cuadro (AC-WKT-13-07) | — (PO) |
| P-16 | ¿Con el diálogo de completado abierto se muestran avisos? | **No**: se muestran una vez al volver a Workout (AC-APP-03-11) | — (PO) |

---

## 15. Discrepancias encontradas al especificar

| Dónde | Qué dice | Qué es (verificado en el legacy) | Acción |
|---|---|---|---|
| ANIMATION_BIBLE §2.7 | "Marcador de píldora activa: se mueve a la siguiente con `spring.snappy`" | El legacy no tiene marcador: probó un marco blanco en la píldora en curso y lo sacó (commit `3ae0f3a`; `docs/superpowers/specs/2026-10-02-step-timeline-states-design.md:17`). El estado en curso es la pista con el relleno | AC-WKT-15-03, AC-WKT-14-12. **Resuelto**: la biblia ya dice "no hay marcador que viaje" (§2.7) |
| ART_BIBLE §3.3 | El reloj de pared, "18/700" | El legacy lo hace del mismo tamaño que la cuenta regresiva (`series_panels.dart:99`), y en la app la cuenta mide 20 (D-ART-02) | AC-WKT-19-01, P-07. **Pendiente** (Designer, en WP-D2: 04-design-notes §12) |
| ART_BIBLE §3.3 | "El relleno avanza continuo (ANIMATION_BIBLE §2.5)" | El relleno continuo está en §2.6; §2.5 son los contadores del diálogo | **Resuelto**: la biblia ya cita §2.6 |
| workout-engine-hud F-WKT-14, casos borde | "Con muchos pasos las píldoras quedan muy finas, pero no se recortan ni se desplazan" | Vale hasta que las separaciones de 3 px se comen el ancho (≈ 120 pasos en un teléfono): desde ahí las píldoras miden 0 y la fila queda vacía (`immersive_workout_screen.dart:361-375`) | DV-40. **Pendiente** (PO, al cerrar): precisar el inventario |
| workout-engine-hud F-WKT-16, casos borde | "`FINALIZADO` casi no se llega a ver, porque terminar a mano lleva al reset de inmediato" | También se ve si llega una lectura durante el guardado (el legacy redibuja con cada lectura), y mientras la página sale el chip pasa a `—` (`:534`) | AC-WKT-16-04, DV-41. **Pendiente** (PO): precisar el inventario |
| workout-engine-hud F-WKT-16, mejora EXR | "En pausa, el chip y la barra se atenúan" | ART_BIBLE §3.2 prohíbe bajar la opacidad del HUD en pausa (el contraste); la pausa la dicen el velo, `PAUSA`, el chip y `Reanudar` | AC-WKT-13-12. **Pendiente** (PO): ajustar la mejora del inventario |
| workout-engine-hud F-WKT-21, punto 5 | Las filas del resumen, sin los íconos | Son `timer`, `straighten` y `local_fire_department` (`immersive_workout_screen.dart:981-991`) | AC-WKT-21-03. **Pendiente** (PO): sumarlos al inventario |
| workout-engine-hud F-WKT-21, punto 2 | "No se cierra tocando afuera ni con 'atrás'" | Tampoco con Escape en escritorio: el diálogo no se cierra tocando afuera, y Escape solo cierra lo que se cierra así (`:207-212`; AC-APP-03-02 de la It. 3) | AC-WKT-21-02. **Pendiente** (PO): sumarlo |
| workout-engine-hud F-WKT-21, casos borde | Solo la carrera de la subida a Strava | Un toque en `Terminar` antes de que termine el guardado también pierde el cierre de la sesión (`workout_provider.dart:283-299`, con `_startedAt` vacío después del `reset()`) | DV-42. **Pendiente** (PO): sumarlo |
| workout-engine-hud F-WKT-21, punto 5 | "La distancia actual del monitor" | Se lee una sola vez, al armar el diálogo un cuadro después del fin, o al volver si la rutina terminó en segundo plano en Android; rotar o cambiar la ventana no la vuelve a leer (`:50-54`, `:207-229`, `:965-991`; la sonda de QA). La versión anterior de esta spec decía que se volvía a leer al redibujar: no pasa | DV-43. **Pendiente** (PO, al cerrar): precisar el inventario |
| workout-engine-hud F-WKT-14, punto 1 | La pastilla de objetivo "se recorta con '…' en pantallas angostas", seguida de "un espacio flexible" | La pastilla y el espacio flexible se reparten el espacio libre por mitades: la pastilla recibe a lo sumo la mitad y, con pastilla, la cuenta y el reloj no quedan pegados al borde y saltan cuando entra `Up next` (`:394-416`; la sonda de QA) | DV-45, DV-46. **Pendiente** (PO, al cerrar): precisar el inventario |
| workout-engine-hud F-WKT-14, punto 6 | El halo "pulsa entre 30 % y 70 % de opacidad, con desenfoque 8, expansión 2 y radio 5" | La sombra del halo también se pinta debajo de la píldora y tiñe la pista de la en curso, que se aclara con el pulso (`step_pill.dart:82-101`; la sonda de QA) | §11.1.1. **Pendiente** (PO, al cerrar): sumarlo al inventario |
| workout-engine-hud F-WKT-13, punto 1; app-shell-platform F-APP-15 | Las barras se ocultan "al entrar" y se restauran "al cerrarse" | Se ocultan al crear la página (al empezar su transición, `:34`) y vuelven al destruirla (al terminar la salida, `:39`), en modo "de borde a borde", que queda así el resto de la sesión de uso | AC-APP-15-01, -04. **Pendiente** (PO): precisar los dos inventarios |
| STATUS (deuda para la 4b) y ROADMAP §2, It. 4b | P-17 "según lo que conteste el usuario en su gate" | El usuario aceptó los valores por defecto del gate de la 4a: no cuenta | AC-WKT-02-11. **Pendiente** (PO, al cerrar): marcarla aceptada en STATUS y en el ROADMAP §6 |
| ROADMAP §2, It. 4b, "También toca" | F-APP-15, F-WKT-04 y F-WKT-09 | Esta iteración también cambia criterios de F-WKT-01, -02, -03, -06 y -20 (el fin espera al diálogo y P-17 de la 4a) y usa F-APP-03, -05, -10 y F-BLE-16 | §5. **Pendiente** (PO, al cerrar): sumarlos a "También toca" de la 4b en el ROADMAP §2 y §4 |

---

## 16. Cambios por gate QA

Responde a los issues del PO de las dos lentes del gate de armonía: [paridad](03-qa-harmony-paridad.md) (QA-G4b-P01 a P07) y [técnica](03-qa-harmony-tecnica.md) (QA-G4bT-02, -06 y -12). Ninguno se disputa. Cada decisión se volvió a mirar en el legacy (§16.3).

### 16.1 Decisiones

| Issue | Decisión | Qué cambió en esta spec |
|---|---|---|
| **QA-G4b-P01** (mayor) y **QA-G4bT-02 (b)** · la pastilla en la fila de información | **Camino (a): la regla del Designer** (DN4b-05, DN4b-16), con una DV nueva que describe el legacy. El camino (b) no se sostiene en TV en español: con `Texto largo`, la fila no entra ni sin la pastilla (346,5 contra 339,9 base), así que la pastilla "recortada con `…`" no tiene lugar y algo más tiene que achicarse igual. Además, cortar por carácter deja números que informan mal (`>15…`) | AC-WKT-14-03 y -04 pasan de §5.3 a §5.2, con el corte por fragmentos, los aires y el relleno compactos. Se reescriben AC-WKT-14-14 (6 en modo compacto) y AC-WKT-14-15 (el orden de lo que cede y la decisión por paso). Cambia lo esperado de CAP-01 (`18 spm` entera, con los aires en 6), CAP-19 y CAP-20 (sin pastilla en TV). **CAP-31, nueva**, prueba el `…` blanco. **DV-45** registra el legacy: a lo sumo la mitad del espacio libre, corte por carácter y nunca se oculta |
| **QA-G4b-P02** · el grupo de la derecha | Se suma la DV. El diseño no cambia | **DV-46**. AC-WKT-14-14 dice que la cuenta y el reloj van pegados al borde y no se mueven. CAP-24 aclara que `docs/image-03.png` es un paso sin pastilla |
| **QA-G4b-P03** y **QA-G4bT-12** · el halo tiñe la pista | **Camino (a)**: es del sistema visual. La pista sin teñir deja leer el avance del relleno en los últimos 5 s | AC-WKT-14-11 reescrito ("solo por fuera"; se saca "mide lo del legacy" y se dice qué cambia). Nueva lista §11.1.1. §9 fila 7 y §15 |
| **QA-G4b-P04** y **QA-G4bT-02 (f)** · la barra libre en TV | **Camino (a)**: se acepta DN4b-27. El título se achica hasta 0,8 antes del `…`; en TV entra entero a ≈ 0,82, más cerca del legacy, que en 411 lo muestra entero | AC-WKT-16-06 reescrito. CAP-13 lo espera entero y achicado. **DV-47** registra el desborde del legacy en 390 y 360. §9 fila 55 |
| **QA-G4b-P05** · DV-43 y el título del diálogo | DV-43 se reescribe con lo que hace el legacy, verificado en su código: `MaterialApp` no escucha ningún *provider* (`lib/main.dart:93-111`), así que nada vuelve a armar el diálogo con cada lectura. Se mantiene como DV porque hay un caso que sí se ve: la rutina que termina en segundo plano en Android, donde el legacy arma el diálogo recién al volver. El título va a **DV-47** | DV-43, la frase del legacy en AC-WKT-21-04, P-13, §9 fila 37 y §15. AC-WKT-21-12 dice que el título se parte en dos líneas. §9 fila 56 |
| **QA-G4b-P06** y **QA-G4bT-02 (a)** · el chip `FINALIZADO` | `#FF7A92` (`phase.finished.hud`, 4,677:1). El peso 700 es del sistema visual | AC-WKT-16-03, DV-44, CAP-15, §9 fila 25 y §11.1.1 |
| **QA-G4b-P07** y **QA-G4bT-02 (c)** · la excepción a Q-59 | **Se confirma**: con píldoras de menos de 6 base, en cualquier régimen, la en curso es un pulgar (al menos 4 × 10 base) y la hueca lleva dos rieles. Q-59 sigue igual: los lugares miden lo mismo y nada es proporcional a la duración | AC-WKT-14-08, -16, AC-WKT-15-05, DV-40, P-05, AC-APP-05-08 (el pulgar con reducir movimiento) y CAP-22. **Precisión**: el pulgar empieza en TV desde **50 pasos**, no desde 39. Entre 39 y 49 rige la separación 1, pero las píldoras todavía miden 6 base o más (7,6 con 40). En TH empieza desde 118, en L640 desde 93 y en W1280 desde 108 |
| **QA-G4bT-02 (d)** · el panel del simulador | Se acepta DN4b-23: el techo baja para que el sello y el lugar de la cuenta grande se vean con el panel abierto, como pide AC-WKT-09-08 | **AC-BLE-16-03 entra en §5.2** con los cinco altos nuevos. DEMO-08 deja de esperar el sello, que se fue a los 92 s: espera que el panel no tape su lugar (02-tech-design §8.5) |
| **QA-G4bT-02 (e)** · los instantes de las tomas | Se acepta DN4b-25 | Regla nueva en §7.1. Cambian CAP-01 (40,2), CAP-06 (30,25), CAP-13 y CAP-16 a CAP-18 (42,2), CAP-19 y CAP-20 (8,2), CAP-21 (150,2), CAP-23 y CAP-24 (50,2), DEMO-02 (3,2) y DEMO-05 (18,2), con los rellenos esperados (0,51, 0,503, 0,337 y 60,7 %) |
| **QA-G4bT-02 (g)** · "dos filas" | La barra tiene dos filas y una tercera mientras se ve el sello (DT4b-02) | AC-WKT-14-01 y AC-WKT-09-08 |
| **QA-G4bT-02**, las precisiones DT4b-05 y DT4b-09 | Se confirman las dos | AC-APP-15-04: con dos inmersivos, la salida del viejo no pide mostrar las barras. AC-WKT-21-06: los 15 s se cuentan desde el fin de la rutina, y un cierre tardío que vuelve antes del toque cuenta como guardado |
| **QA-G4bT-06** · CAP-22 cae cuando entra `Próximo` | CAP-22 a los **3016,6 s**: `0:14`, relleno 0,553 y `Próximo` ya quieto (entró a los 3015,0) | CAP-22, con el pulgar y el reloj `10:50` |

**Confirmaciones del pedido de 02-tech-design §8.5** (sin cambios de texto, salvo lo de arriba):
- **DT4b-02**: el sello en la tercera fila de la barra, con `#FFD166` y el detalle en blanco.
- **DT4b-03**: la entrada y la salida con los números de esta spec, que el Designer confirmó (DN4b-01).
- **DT4b-04**: la cuenta grande de 64 en su disco de 72·s (DN4b-03).
- **DT4b-17**: el peor caso del presupuesto, aceptado por QA.
- **D-ANI-05** sigue yendo al gate del usuario (P-01).

**Lo que se mantiene, aunque toca otros issues**:
- **QA-G4bT-03** (del Architect): con reducir movimiento, el cambio de paso va **sin destello ni trazo**, como dice AC-APP-05-08. No hay motivo para cambiarlo: es un realce que destella, y reducir movimiento pide fundidos. Lo alinean el diseño técnico y las notas.
- **QA-G4bT-07 (c)**: DEMO-08 toma a los 101,6 s, con el panel ya abierto.

### 16.2 Conteos y lo que tienen que alinear los otros documentos

**Conteos**:
- **101 criterios** = 61 nuevos + 40 de §5. §5.2 pasa de 20 a 23: suma AC-WKT-14-03, AC-WKT-14-04 y AC-BLE-16-03. §5.3 pasa de 17 a 15. La matriz de 02-tech-design §6.4 suma la fila de AC-BLE-16-03, con [dotnet], y AC-WKT-14-03 y -04 cambian de "sin cambios" a "cambian".
- **31 capturas**, con CAP-31.

**02-tech-design** (Architect):
- §2.2 y `PillRowGeometryTests`: el pulgar (≥ 4 × 10 base) y los rieles en toda píldora de menos de 6 base, desde 50 pasos en TV; sale `StripCurrentMin` 2.
- §2.7: `PhaseChipSpec.Text` de `FINALIZADO` pasa a `phase.finished.hud`.
- DT4b-12: lo reemplaza DN4b-16, que ya no contradice la spec.
- §5.2 punto 1: la regla de los +0,2 s.
- §5.3: CAP-01, -06, -13, -15, -16 a -24 y CAP-31 nueva.
- §5.4: DEMO-02, -05 y -08.
- §5.7 y `SimulatorPanelLayout`: los altos de AC-BLE-16-03.
- §6.4: la fila de AC-BLE-16-03.

**04-design-notes** (Designer):
- **AUX-01 pasa a ser CAP-31, en otro estado**: `Texto largo` a los 2,2 s, con `>150W · 24 spm…`. Medido con las tablas K2 de `measure4b.py`, sobran 6,3 base. En el estado de AUX-01 (`Prueba 4a` a los 78,2 s), `>160W…` entra por 0,3, que es menos que el error de la medida. Además, su reloj diría `10:01`, no `10:00`.
- §7.1: CAP-22 a los 3016,6 (era 3015,2), con el relleno del pulgar en 0,553.
- §7.2: los valores de sonda de CAP-31.
- §10: las decisiones del PO quedan tomadas.

### 16.3 Verificación hecha

1. **El legacy, otra vez en su código** (solo lectura, `3ae0f3a`):
   - `step_pill.dart` entero: el `BoxShadow` es la decoración de un `Container` que envuelve la píldora, así que se pinta debajo de ella;
   - en `immersive_workout_screen.dart`:
     - `:20-99`, el diálogo una vez y la vuelta a reposo;
     - `:190-234`, el título en una `Row` sin `Flexible`;
     - `:330-550`, la fila con el `Flexible` y el `Spacer`, la barra libre sin `Flexible` y el chip 11/800;
     - `:955-1019`, `_CompletionStats`, que lee `w.data` al armarse;
   - `lib/main.dart:70-114`: `MaterialApp` no escucha ningún *provider*.

   Los números de las sondas son de QA (03-qa-harmony-paridad §3) y no se repitieron.
2. **Los anchos de CAP-31**: una sonda propia en `<scratch>/it4b/po-gate/frag.py`. Usa las funciones de `measure4b.py` del Designer, con las tablas K2 del repo.
   - `Texto largo` en TV, en modo compacto 2 (el del paso): a los 2,2 s, 114,7 base de lugar para la pastilla; dos fragmentos miden 108,4 y los tres, 173,9.
   - A los 8,2 s, con `Próximo`, no queda lugar.
   - `Prueba 4a` con `Próximo`: `>160W…` mide 60,1 y hay 60,4.
   - El título libre necesita 0,818 en TV y en 360 (127,0 de 155,4).
3. **El umbral del pulgar**: con los anchos útiles de 02-tech-design §2.2 y la regla de regímenes, las píldoras bajan de 6 base desde 50 pasos en TV y 360, 118 en TH, 93 en L640 y 108 en W1280.
4. **Repo público**: este documento solo tiene rutas relativas, el legacy citado por su URL o `../rowmate-legacy` y el scratch como `<scratch>`. No tiene rutas de la PC ni datos del equipo. Sin commits.
5. **Revalidación** (otra corrida del paso del PO, el mismo 2026-10-29, con WP-A ya en curso). La spec **no se reescribió**: el diseño técnico, las 04-design-notes y las dos lentes del gate ya están alineados con ella (02-tech-design §8.5: "PO: nada pendiente"), y el catálogo ya tiene las claves de §11.2. Se volvió a leer en el legacy (`3ae0f3a`):
   - `step_pill.dart` y `test/step_pill_test.dart`, enteros;
   - en `immersive_workout_screen.dart`, el ciclo de vida y el diálogo (`:27-65`, `:196-292`, `:959-1019`), la barra de pasos (`:303-480`) y la barra libre con el chip (`:491-550`);
   - `WallClock` (`series_panels.dart:57-110`);
   - en `workout_provider.dart`, `StepProgress` (`:31-41`), los temporizadores (`:147`, `:247`), el fin de la rutina (`:192-216`) y `finish()` (`:252-303`);
   - las claves `workoutCompleted`, `workoutCompletedMessage` y `workoutFinish` de los `.arb`, y `docs/image-03.png`.

   Todo coincide con lo que dice esta spec. Cada pedido del ROADMAP (§2, It. 4b) y del REPORT de la 4a (§7.2 y §9.2, punto 3) tiene su criterio (§6). No cambian los criterios, las capturas ni los conteos.

`git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después de escribir esta spec, y también antes y después de esta corrección y de la revalidación. No se leyó `lib/core/strava/strava_secrets.dart`.

---

## 17. Cambios al cerrar

El PO, 2026-10-30, con la verificación de QA en su ronda 2 ([05-qa-verification](05-qa-verification.md), "Ronda 2": 98 PASS, 0 FAIL, 3 NO VERIFICABLE) y las dos lentes. Ningún criterio cambia de sentido.

### 17.1 En esta spec

| Issue | Qué cambió |
|---|---|
| **QA-V4b-R2-02** (del PO) | El título libre en TV y en 360, en español, entra entero a **≈ 0,8 (0,805)** y no a ≈ 0,82: con el chip a su ancho real (el *tracking* corregido en V1-02) y el aire mínimo de 2 entre el título y el reloj (04-design-notes §16). La regla no cambia: se achica hasta 0,8 y, solo si ni así entra, se recorta con `…`. Cambian AC-WKT-16-06, la fila de CAP-13 (§7.3) y DV-47 (§11.1). §16.1 y §16.3 quedan como registro del gate, con el 0,818 que se midió entonces con fontTools |

### 17.2 Los pendientes del PO de §15

Hechos, otra vez contra el legacy (`3ae0f3a`: `immersive_workout_screen.dart:27-65`, `:205-215`, `:359-377`, `:392-418`, `:528-537`, `:980-992`; `step_pill.dart:82-101`; `workout_provider.dart:252-303`):

- **[workout-engine-hud](../../product/inventory/workout-engine-hud.md)**: F-WKT-13 (las barras al crear y al destruir la página), F-WKT-14 (la pastilla que recibe a lo sumo la mitad del espacio libre, el halo que tiñe la pista y la fila vacía con más de ≈ 120 pasos), F-WKT-16 (`FINALIZADO` con una lectura durante el guardado, el `—` mientras la página sale y la mejora EXR sin atenuar el HUD) y F-WKT-21 (los íconos del resumen, Escape, los totales leídos una vez y el `Terminar` que pierde el cierre). El detalle, en su sección "Cambios al cerrar la It. 4b".
- **[app-shell-platform](../../product/inventory/app-shell-platform.md)**: F-APP-15, cuándo se ocultan y vuelven las barras.
- **[ROADMAP](../../ROADMAP.md)**: P-17 aceptada en el gate de la 4a (§2, It. 4b, y §6); "También toca" de la 4b con F-WKT-01, -02, -03, -06 y -20, F-APP-03, -05 y -10 y F-BLE-16 (§2 y §4).
- **[STATUS](../../STATUS.md)**: P-17 aceptada; la 4b cerrada, esperando el OK del usuario.

### 17.3 Decididas por el PO al cerrar

| Tema | Decisión |
|---|---|
| **QA-V4b-06** (el primer cuadro de cada inmersivo tarda 31-69 ms de CPU) | **Se acepta como desvío** hasta el comienzo de la It. 5. Es un tirón al empezar el fundido a negro de "sumergirse", sin efecto en los datos: la sesión y el reloj corren desde el toque (AC-WKT-13-01) y las demás ventanas del build dan ≤ 9,3 ms. El Architect lo resuelve al empezar la 5 (02-tech-design V1-06: armar el HUD en un cuadro del negro o tenerlo listo al abrir Workout) y lo mide con `FrameTimeProbe` en el build |
| **QA-V4b-R2-01** (en TV y P360, en español, el título libre queda a ≈ 4 dp del ícono del reloj) | AC-WKT-16-06 se cumple: nada se encima y nada se corta. Pero la barra pierde su jerarquía (el título a la izquierda; el reloj y el chip, juntos, a la derecha). **Lo corrige el Designer al empezar la It. 5**, sin bajar el título de 0,8 (por ejemplo, el relleno horizontal del chip de 8 a 6), con CAP-13 a CAP-15 regeneradas en dos corridas |
| **QA-V4b-R2-03** (Unity revienta a veces al cerrar un *batchmode* que cambió los *defines*) | No toca la app: los *scripts* siempre restauran el *define*. El Architect lo resuelve en las herramientas al empezar la It. 5 (esperar la compilación o reintentar una vez) y lo anota en `tools/unity/README.md` |
| **AC-WKT-02-11** en el equipo | Lo verifica el usuario con la demo (c). Si al despertar el reloj saltó el tiempo suspendido, el criterio se reabre y se corrige al empezar la It. 5 (R4b-01: sumar otra señal del sistema) |
| O-2 de la lente de motion (60 cuadros por segundo con el diálogo de completado abierto) | Se decide en la 6a, con el teléfono: con el diálogo arriba y nada en movimiento se podría bajar a 30 |
| O-01 de QA (CAP-10: el diálogo a 1,3× en L640 se desplaza sin un indicio de que hay más) | Lo permite AC-WKT-21-12. El Designer suma el indicio cuando la It. 12 cambie el diálogo (los dos botones de Strava) |
| O-4 y O-5 de la lente de motion (una excepción en una animación o al armar el diálogo podría trabar la salida o dejar la sesión en `Finished`) | No se encontró ningún camino que falle hoy. Endurecimiento del Architect al empezar la 5, junto con QA-V4b-06, que toca la misma página |
| O-03 de QA (VID-08: `0.0`, `--:--` y `0` el primer medio segundo, hasta la primera lectura) | Es la conducta de la 4a: el HUD muestra lo que tiene hasta que llega la primera lectura del remo. Con la entrada normal lo tapa la cascada. Se acepta |

### 17.4 Verificación hecha

- Se miraron las 43 capturas de [snapshots/](snapshots/INDEX.md) contra §7.3 y §7.4.
- `git -C ../rowmate-legacy status --porcelain` vacío antes y después; no se leyó `lib/core/strava/strava_secrets.dart`.
- Este documento, los inventarios, el ROADMAP y el STATUS citan el repo con rutas relativas, el legacy por su URL o `../rowmate-legacy` y el scratch como `<scratch>`. Sin *commits*.
