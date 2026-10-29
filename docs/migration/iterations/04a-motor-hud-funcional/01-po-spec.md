# It. 4a — Entrenar: motor de sesión, guardado incremental, reposo y HUD funcional: spec de producto

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-28 |
| Iteración | 4a · `04a-motor-hud-funcional` ([ROADMAP §2, It. 4a](../../ROADMAP.md)) |
| Estado | **Cerrada, espera el OK del usuario** (§17; [REPORT](REPORT.md)). Corregida por el gate de armonía de QA ([03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md); §16) y, al cerrar, por la verificación de QA ([05-qa-verification](05-qa-verification.md), ronda 2: 156 PASS, 2 FAIL y 4 NO VERIFICABLE). Se construyó con [02-tech-design](02-tech-design.md) y [04-design-notes](04-design-notes.md), después de la deuda de la It. 3 ([00-deuda-it3](00-deuda-it3.md), [00-deuda-diseno](00-deuda-diseno.md)) |
| IDs dueños | **19 = 14 canónicos + 5 alias**: F-WKT-01, F-WKT-02, F-WKT-03, F-WKT-04, F-WKT-05 (+ F-HRM-14), F-WKT-06 (+ F-HIS-01, F-HIS-02, F-HIS-03), F-WKT-09, F-WKT-10, F-WKT-11 (+ F-APP-12), F-WKT-12, F-WKT-20, F-WKT-23 (descarte), F-BLE-10, F-APP-14 (sus criterios [Apple], en la 10b) |
| También toca | F-WKT-13, F-WKT-14 (solo la fila de información; el ROADMAP no la lista: §15), F-WKT-16 y F-WKT-17 (versión funcional del inmersivo, las barras superiores y las tarjetas: las cierra la 4b); F-HIS-17 y F-HIS-19 (sesiones y puntos en la base); F-SCN-01 (fondo provisorio); F-RUT-09 y F-RUT-10 (la colección y la tarjeta de Rutinas, en Workout; con D-ART-10 cambian AC-RUT-10-04 y -09); F-APP-02 (Workout deja de ser provisoria); F-APP-03 (el inmersivo, sus diálogos y sus avisos); F-APP-05 (letra, teclado y movimiento en las pantallas nuevas); F-APP-06 (la paleta en el HUD); F-APP-10 (12 claves nuevas); F-BLE-16 (la cinta y la llave sobre el entrenamiento, QA-V3-09; con D-ART-10, la llave y el panel en toda la app: AC-BLE-16-02 y -03) |
| Criterios | **162** = **147 nuevos** (WKT 130: F-WKT-01 11, -02 10, -03 10, -04 7, -05 6, -06 15, -09 7, -10 9, -11 16, -12 9, -20 7, -23 1, -13 5, -14 6, -16 1, -17 10; BLE 5: F-BLE-10; APP 8: F-APP-14 4, F-APP-03 3, F-APP-10 1; HIS 3: F-HIS-17 1, F-HIS-19 2; SCN 1: F-SCN-01) + **15 de iteraciones anteriores** que se verifican, se completan o cambian acá (§5). Además: **11 casos de test legacy** (LT-35 a LT-45, §6) y **30 capturas, 14 tomas de la demo y 3 videos** (§7) |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Las rutas `lib/...:línea` y `test/...` de esta spec son relativas a ese repositorio. Cada valor se leyó en `lib/features/workout/workout_provider.dart`, `step_target_tracker.dart`, `workout_screen.dart`, `immersive_workout_screen.dart`, `series_panels.dart` (formatos y reloj de pared), `series_tracker.dart` (saneamiento de la distancia), `lib/core/database/database_service.dart`, `lib/core/models/workout_session.dart`, `rowing_data.dart`, `interval_step.dart`, `routine.dart`, `lib/shared/theme.dart`, `lib/main.dart`, `lib/features/device/device_provider.dart`, `lib/core/bluetooth/heart_rate_service.dart` y `lib/l10n/app_es.arb` / `app_en.arb`, y en los tests `test/workout_provider_hr_test.dart`, `test/step_target_tracker_test.dart` y `test/series_panels_test.dart`. Capturas de referencia: `docs/image-02.png` (Workout en reposo, en inglés) y `docs/image-03.png` (HUD inmersivo). La pantalla siempre encendida se leyó en el paquete que usa el legacy (`<pub-cache>/wakelock_plus-1.4.0`, la versión de su `pubspec.lock`) |
| Insumos | [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) §3 y §4 (Q-01, Q-02, Q-06, Q-12, Q-13, Q-15, Q-51 a Q-53, Q-55 a Q-60, Q-91, Q-92); inventarios [workout-engine-hud](../../product/inventory/workout-engine-hud.md) (F-WKT-01 a -06, -09 a -14, -16, -17, -20, -23), [heart-rate](../../product/inventory/heart-rate.md) (F-HRM-14), [history-persistence](../../product/inventory/history-persistence.md) (F-HIS-01 a -04, -17, -19, -21), [app-shell-platform](../../product/inventory/app-shell-platform.md) (F-APP-11, -12, -14, -15), [rower-ble-device-sim](../../product/inventory/rower-ble-device-sim.md) (F-BLE-10) y [routines-models](../../product/inventory/routines-models.md) (F-RUT-03 a -07); [REPORT de la It. 3](../03-rutinas/REPORT.md) §5.4, §7, §8 y §9; specs de las [It. 1](../01-fundaciones/01-po-spec.md) (AC-BLE-01, AC-RUT-03 a -07, -16 a -18, AC-APP-10), [It. 2](../02-dispositivo-simulador/01-po-spec.md) (AC-BLE-10, -14 a -16, AC-APP-01, -02, -05, -06, DV-08) y [It. 3](../03-rutinas/01-po-spec.md) (AC-RUT-08 a -14, AC-HIS-17, -20, AC-APP-03, `F-*` de §0.4, DV-09 a DV-20); [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §6.1 y la tabla de plataforma; [ADR 017](../../architecture/adr/017-segundo-plano.md); [ART_BIBLE](../../design/ART_BIBLE.md) §2.2-§2.3, §3.2-§3.7, §3.10, §5.2-§5.3, §10; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §2.4, §2.6, §2.8, §2.9, §2.11; [STATUS](../../STATUS.md) |

---

## 0. Cómo leer esta spec

### 0.1 Identificadores

- **`AC-<ÁREA>-<NN>-<nn>`**: criterio `nn` de la ficha `F-<ÁREA>-<NN>`. Los alias se verifican con su canónico: F-HRM-14 con F-WKT-05; F-HIS-01, -02 y -03 con F-WKT-06; F-APP-12 con F-WKT-11.
- Los IDs de "también toca" siguen la numeración de su iteración dueña o de la última que los usó: AC-BLE-10-03 sigue a los 2 de la It. 2; AC-HIS-17-07 a los 6 de la It. 3; AC-APP-03-09 a los 8 de la It. 3; AC-APP-10-10 a los 9 de la It. 1.
- **§5** repite IDs de iteraciones anteriores con lo que falta ver ahora; si fallan, se reabren acá. Cuatro además **cambian** por D-ART-10 (DV-39): AC-BLE-16-02, AC-BLE-16-03, AC-RUT-10-04 y AC-RUT-10-09.
- **`LT-nn`**: casos de test legacy, con la numeración de las It. 1 y 2 (terminaron en LT-34; la It. 3 no tuvo).
- **`CAP-nn`**, **`DEMO-nn`** y **`VID-nn`**: capturas, tomas de la demo y videos de esta iteración (§7), en `docs/migration/iterations/04a-motor-hud-funcional/snapshots/`.
- **`DV-nn`**: diferencias visibles con el legacy, a propósito (§11.1). Siguen la numeración de la It. 3 (terminó en DV-20).
- **`F-*`** y **`R-4*`**: bases, rutinas y lecturas de prueba (§0.4).

### 0.2 Cómo se verifica cada criterio

Las mismas marcas de la It. 3:

| Marca | Qué es | Disponible |
|---|---|---|
| **[dotnet]** | `dotnet test` del Core, del motor y de los repositorios, fuera de Unity, con SQLite real, reloj y planificador de prueba | Sí |
| **[Editor-EM]** | Unity Test Framework, EditMode, Editor 6000.3.27f1 | Sí |
| **[Editor-PM]** | Unity Test Framework, PlayMode: la UI real montada, con la base, el tamaño, el idioma, los servicios y el ciclo de vida que pide el criterio | Sí |
| **[Captura]** | Imagen o video renderizado por Unity con la UI real (no una maqueta), revisado por QA y el Designer | Sí |
| **[Device-Win]** | Build de Windows abierto en la PC (con simulador y normal), lanzado siempre desde su carpeta de salida por defecto (QA-V3-08) | Sí con Mono; IL2CPP solo con el módulo |
| **[Device-And]** | Build de Android en un teléfono 8.0+ de 64 bits | Solo con el módulo de Android y el teléfono |
| **[QA]** | Revisión estática de QA (código, base, build) | Sí |

Si falta el módulo de Android, el de IL2CPP o el teléfono, el criterio (o su parte de equipo) queda **NO VERIFICABLE** con el motivo y no frena la iteración. Nunca se da por cumplido sin verlo. Los criterios [Apple] van a la 10b (ROADMAP §4.1).

### 0.3 Notación

- Textos entre comillas invertidas: **exactos al carácter**. `·` es U+00B7 con un espacio a cada lado; `␣` marca un espacio que importa (por ejemplo, los dos del comienzo de `workoutOrPickRoutine`); `⏎` es un salto de línea; `×` es U+00D7; `—` es U+2014; `▲` y `▼` son U+25B2 y U+25BC.
- Tamaños en dp (Windows al 100 %: 1 dp = 1 px). En el HUD, toda medida que viene del legacy se multiplica por la **escala del HUD** (`clamp(lado corto / 390; 0,9; 1,6)`, ART_BIBLE §5.3): 0,92 en L640, 1,054 en TV y TH, 1,6 en W1280.
- "Segundo" del motor = un paso del reloj activo de 1 s (el *tick* del legacy). "En marcha", "en pausa", "terminada" y "reposo" son las fases `active`, `paused`, `finished` e `idle`.
- "Aviso" = mensaje breve abajo, sin acción, de 4 s, que reemplaza al visible (DV-14 de la It. 3). "Diálogo" = ventana modal centrada.
- Los textos visibles se citan por clave del catálogo y su valor es / en. Las claves **nuevas** de esta iteración están en §11.2.
- `<pub-cache>` es la caché local de paquetes de Dart y `<flutter-sdk>` el SDK de Flutter, con las versiones que usa el legacy. Nunca se citan rutas de una PC.

### 0.4 Estado de prueba

Para que pruebas, capturas y videos den siempre lo mismo:

- **Base de prueba**: cada prueba y cada captura usa un archivo de base propio y temporal. **Nunca** se abre ni se escribe `rower_app.db` ni `rower_app_dev.db` del usuario (AC-HIS-20-04). La herramienta de la demo (§12.9) solo lee.
- **Reloj y planificador de prueba**: la hora de pared arranca en `2026-10-28T13:00:00.000Z`; el tiempo del motor avanza solo cuando la prueba lo pide. La sesión se crea en el instante 0 y su segundo `k` cae en el instante `k` s.
- **Ciclo de vida de prueba**: la prueba pasa la app a segundo plano y la trae de vuelta en los instantes que pide, y también le saca y le devuelve el foco de la ventana; mientras tanto las lecturas del remo y del pulsómetro quedan guardadas con su hora, como en el equipo (ADR 017). Sin un pedido, la app está adelante y con foco, también en las corridas sin ventana: así CAP-28 y DEMO-13 ven el aviso de completado en el acto (AC-WKT-02-06).
- **Bases sembradas** (en v8): las de la It. 3 ([01-po-spec §0.4](../03-rutinas/01-po-spec.md)) **`F-VACIA`**, **`F-LISTA`** (`Escalera SPM`, `Pirámide 500`, `Solo calentamiento`, `Intervalos 4×`, de la más nueva a la más vieja) y **`F-ROTA`**; **`F-RARA`** con estas rutinas, cada una en su base cuando el criterio lo pide: `Serie enorme` (una serie ×2 000 000 000 de trabajo 60 s), `Vatios imposibles` (un trabajo de 60 s con vatios mín. 50 000), `Serie ×0` (solo una serie ×0 de trabajo 60 s) y `Sprint` (un paso de tipo `sprint`). Nuevas:
  - **`F-4A`**, con:
    - `Prueba 4a` (la de la demo): calentamiento 30 s; serie ×2 [trabajo 60 s, vatios mín. 160, SPM 22, progresión SPM +2; descanso 30 s]; enfriamiento 30 s. Expandida: 6 pasos, 240 s. Resumen `1 serie · 1 descanso`.
    - `Corta`: serie ×2 [trabajo 10 s, SPM 18, progresión SPM +2; descanso 5 s]. Expandida: trabajo 10 s (18), descanso 5 s, trabajo 10 s (20), descanso 5 s: 30 s.
    - `Distancia 100`: trabajo 100 m; descanso 5 s (sueltos).
    - `Sin meta`: un trabajo sin meta (por distancia y sin metros, `?`); descanso 5 s.
- **Remo de prueba `R-4`**: manda la lectura `L(k)` en el instante `k − 0,5` s (`L(0)` medio segundo antes de iniciar) y sigue el tiempo real, también en pausa y en segundo plano: tiempo del monitor `300 + k` s, paladas `100 + k`, distancia `1000 + 4k` m, split 125 s, vatios 150, SPM 24.0, kcal `40 + ⌊k / 10⌋`, pulso 0. El monitor ya traía 1000 m, 300 s y 40 kcal (Q-15). Variantes: **`R-4(s)`** con SPM `s` (por ejemplo `R-4(19)`); **`R-4R`**, reinicio del monitor: desde `L(10)` la distancia es `4 (k − 10)`; **`R-4Z`**, un paquete sin distancia: `L(10)` llega con distancia 0 y las demás como `R-4`; **`R-4P`**, parado: SPM 0, 0 W, split 0, distancia y tiempo quietos.
- **Pulsómetro de prueba**: manda el bpm y el estado (`buscando`, `conectando`, `conectado`, `desconectado`) que pide la prueba.
- **Base que falla o tarda a pedido**: al crear la sesión, al guardar un punto, al cerrar la sesión o al cargar las rutinas; o tarda lo que pida la prueba en cada escritura.
- **Simulador sin ruido** (AC-BLE-14-06) cuando la prueba usa el modo simulador: a 150 W y 24 spm da split `02:13` (AC-BLE-14-06 de la It. 1).
- **Idioma, letra del sistema y "reducir movimiento" de prueba**, como en las It. 2 y 3.
- Ninguno de estos dobles existe en un build que se instala.

---

## 1. Objetivo y demo

**Objetivo.** Desde la pestaña Workout el usuario arranca un entrenamiento libre o una de sus rutinas y rema con un HUD **funcional**: métricas grandes con semáforo de objetivos, el paso en curso con su cuenta regresiva y el aviso del que viene, pausa, reanudación y terminar con confirmación. La sesión y su telemetría se guardan **mientras rema**, así que ni un cierre de la app la pierde; el entrenamiento sigue con la app en segundo plano y la pantalla no se apaga. El vidrio usa el respaldo sin desenfoque (`glass.fallback`, `#070D16 @86 %`) y el fondo es provisorio hasta la It. 6a.

**Demo (se le muestra al usuario antes de la It. 4b).** Con el build de simulador de Windows (o el Editor con el modo simulador), con la ventana de 1280 × 720. Si Windows está en inglés, el build sale en inglés: el texto en inglés va entre paréntesis.

**(a) Entrenamiento libre.**

1. Dispositivo muestra `Remo simulado` · `Conectado`. *(Opcional, para ver el pulso)*: en la tarjeta del pulsómetro, `Buscar` → `Pulsómetro simulado` → `Conectar`.
2. **Workout** (`Workout`): título `Entrenamiento` (`Workout`), el botón `Entrenamiento libre` (`Free workout`) habilitado y, si hay rutinas, `␣␣O elegí una rutina:` (`␣␣Or pick a routine:`) con sus tarjetas.
3. `Entrenamiento libre` → se abre en el acto el inmersivo: arriba `Entrenamiento Libre` (`Free Workout`) con el ícono de remo; SPM grande arriba a la izquierda (`24.0`); `Split /500m` (`02:13`) y `Watts` (`150 W`) arriba a la derecha; `kcal` y, con el pulsómetro, `BPM` abajo a la derecha; abajo a la izquierda la banda con el reloj (`00:01`, `00:02`…) y la distancia (la del monitor: el simulador ya venía sumando desde que se abrió la app, Q-15); abajo al centro `Terminar` y `Pausa`. Todo sobre tarjetas de vidrio oscuro; la llave no tapa nada del HUD y la cinta `SIMULADOR` solo el comienzo de la barra superior, como en el legacy.
4. Llave → `+` de Watts: la tarjeta pasa a `160` en la lectura siguiente.
5. `Pausa` → el reloj se detiene (los valores siguen llegando) y el botón pasa a `Reanudar` verde. `Reanudar` → el reloj sigue.
6. `Terminar` → diálogo `Terminar entrenamiento` / `Se guardará la sesión. ¿Continuar?` (`Finish workout` / `The session will be saved. Continue?`) → `Cancelar`: sigue. `Terminar` otra vez → `Terminar`: vuelve Workout en reposo, sin resumen.

**(b) Una rutina** (`Prueba 4a`, 4 minutos; se crea en Rutinas como en la It. 3: calentamiento 30 s; `Serie` con trabajo 1 min, `W mín` 160, `SPM objetivo` 22 e `Incremento SPM` 2, y descanso 30 s, ×2; enfriamiento 30 s).

1. En Workout, la tarjeta `Prueba 4a` muestra `1 serie · 1 descanso` y su mini-barra de 6 tramos. Tocarla → diálogo `Prueba 4a` / `1 serie · 1 descanso⏎⏎¿Iniciar el entrenamiento?` → `Iniciar` (`Start`).
2. Arriba, `Calentamiento` (`Warmup`) en amarillo y la cuenta `0:30`, `0:29`… En `0:15` aparece `Próximo: Trabajo` (`Up next: Work`), que late suave; en los últimos 5 s la cuenta crece y se tiñe.
3. **Trabajo**: `Trabajo` con la pastilla `>160W · 22 spm · ` + ícono de tendencia + `SPM +2/rep` y la cuenta `1:00`. A 150 W (si quedaron 160 de la parte a, la llave → `−` de Watts), **Watts se pone en rojo** con `▲` y debajo `obj. 160`; SPM `24.0` en blanco con `obj. 22`. Llave → `+` de Watts (160) → Watts vuelve a blanco.
4. **Repetición 2**: el objetivo de SPM pasa a 24 (`obj. 24`); a 24 spm cumple; llave → `−` de Ritmo (23) → SPM en rojo con `▲`; `+` → blanco.
5. Al terminar el enfriamiento, la rutina termina sola: la sesión se guarda y vuelve Workout en reposo con el aviso `¡Entrenamiento completado!` (`Workout Completed!`). El diálogo de rutina completada llega en la 4b.

**(c) Cerrar la app a mitad de una sesión.** Iniciar un entrenamiento libre, remar unos 30 s y cerrar la ventana (o terminar el proceso desde el Administrador de tareas). Al abrir otra vez, la app arranca en Dispositivo, como siempre. Con la herramienta de solo lectura de la demo (§12.9) se ve la sesión con su hora de inicio, sin hora de fin y con sus puntos (uno cada 5 s: unos 6 en 30 s). En el Historial se va a ver desde la It. 9.

**(d) Sin remo.** Dispositivo → `Desconectar` (o la llave → `Simular desconexión`, que dura 3 s). En Workout aparece el aviso naranja `Conectá el rower para iniciar un entrenamiento` (`Connect the rower to start a workout`), `Entrenamiento libre` queda deshabilitado y las tarjetas de rutina al 50 %, sin responder. `Buscar dispositivos` en Dispositivo lo vuelve todo a la normalidad.

**(e) La pantalla no se apaga** mientras la app está abierta, en cualquier pestaña (con el tiempo de apagado de pantalla de Windows, sin tocar nada). **(f) Segundo plano**: minimizar la ventana 1 min en pleno entrenamiento y volver: el reloj avanzó 1 min y la sesión siguió.

**(g) Las pruebas**: `dotnet test` en verde con el motor, el guardado y los 11 casos legacy; las PlayMode de Workout y del HUD en verde; la carpeta de capturas de §7, con la secuencia DEMO-01 a DEMO-14.

---

## 2. Alcance

| ID | Funcionalidad | Qué se cierra en la 4a | Vuelve en |
|---|---|---|---|
| F-WKT-01 | Ciclo de vida y fases | Completo | 4b (completado), 12 (Strava al terminar) |
| F-WKT-02 | Reloj activo | Completo, con segundo plano (Q-91) en Windows; Android con el teléfono | 10a (remo real en segundo plano), 10b [Apple] |
| F-WKT-03 | Avance de pasos y fin automático | Completo; el fin muestra un aviso provisorio | 4b (diálogo de rutina completada) |
| F-WKT-04 | Estado del paso en curso | Completo: la regla y la fila de información | 4b (píldoras y su pulso) |
| F-WKT-05 (+ F-HRM-14) | Fusión del pulso | Completo, con el pulsómetro simulado y el de prueba | 11 (sensor real) |
| F-WKT-06 (+ F-HIS-01, -02, -03) | Telemetría y guardado de la sesión | Completo, incremental (Q-12) | 9 (se ve), 12 (se sube), 13 (se repara) |
| F-WKT-09 | Cumplimiento de objetivos por paso | Completo como regla (no se ve todavía) | 4b (píldoras) |
| F-WKT-10 | Semáforo de objetivos | Completo | — |
| F-WKT-11 (+ F-APP-12) | Formatos de display | Los 15 formatos | 4b, 5 y 9 (los usan) |
| F-WKT-12 | Workout en reposo | Completo, salvo el selector de escenario | 8 (selector) |
| F-WKT-20 | Controles y confirmación de terminar | Completo | 12 (subida al terminar) |
| F-WKT-23 | Pantalla clásica | Descarte | — |
| F-BLE-10 | Señal "remo conectado" | Completo | 10a (remo real) |
| F-APP-14 | Pantalla siempre encendida | Windows; Android con el teléfono | 10a (Android), 10b [Apple] |

---

## 3. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero**, quiero arrancar un entrenamiento libre con un toque, para remar sin preparar nada. |
| H-02 | Como **remero que entrena por intervalos**, quiero iniciar una de mis rutinas y que avance sola de paso en paso, con la cuenta regresiva y el aviso del paso que viene, para no tocar nada mientras remo. |
| H-03 | Como **remero**, quiero leer de un vistazo, a 1,5 m, mi SPM, split, vatios, kcal, pulso, tiempo y distancia, para regular el esfuerzo. |
| H-04 | Como **remero con objetivos**, quiero que la tarjeta se ponga en rojo con una flecha cuando estoy por debajo del objetivo (o más lento en el split), y ver el objetivo, para corregir al instante. |
| H-05 | Como **remero**, quiero pausar, reanudar y terminar con botones grandes, y que terminar pida confirmación, para no cortar la sesión por error. |
| H-06 | Como **usuario**, quiero que cada sesión quede guardada con su curva mientras remo, aunque la app se cierre de golpe, para no perder lo remado. |
| H-07 | Como **usuario con pulsómetro**, quiero que el pulso del sensor mande sobre el del monitor, para ver y guardar el dato confiable. |
| H-08 | Como **usuario**, quiero que la app me pida conectar el remo antes de empezar, para no arrancar un entrenamiento vacío. |
| H-09 | Como **usuario**, quiero que la pantalla no se apague y que el entrenamiento siga si cambio de app o minimizo la ventana, para remar sin interrupciones. |
| H-10 | Como **usuario con una rutina dañada** (una base tocada a mano), quiero que la app me diga que no la puede iniciar, en lugar de colgarse. |
| H-11 | Como **desarrollador o tester**, quiero probar todo con el simulador, con su capa siempre encima y su propia base, sin ensuciar los datos reales. |

---

## 4. Criterios de aceptación

### 4.0 Conteo

| Ficha | AC | Ficha | AC | Ficha | AC |
|---|---|---|---|---|---|
| F-WKT-01 | 11 | F-WKT-10 | 9 | F-WKT-14 | 6 |
| F-WKT-02 | 10 | F-WKT-11 | 16 | F-WKT-16 | 1 |
| F-WKT-03 | 10 | F-WKT-12 | 9 | F-WKT-17 | 10 |
| F-WKT-04 | 7 | F-WKT-20 | 7 | F-HIS-17 | 1 |
| F-WKT-05 | 6 | F-WKT-23 | 1 | F-HIS-19 | 2 |
| F-WKT-06 | 15 | F-BLE-10 | 5 | F-SCN-01 | 1 |
| F-WKT-09 | 7 | F-APP-14 | 4 | F-APP-03 | 3 |
| F-WKT-13 | 5 | | | F-APP-10 | 1 |

Total: **147 criterios nuevos** (§4.1-§4.15) + **15 de iteraciones anteriores** (§5) = **162**.

### 4.1 F-WKT-01 — Ciclo de vida y fases de la sesión

- **AC-WKT-01-01 · Fases.** *Entonces* la sesión tiene cuatro fases: reposo, en marcha, en pausa y terminada, y la app abre en reposo. Las lecturas del remo y del pulsómetro llegan al motor en **todas** las fases, también en reposo. [dotnet] · `lib/features/workout/workout_provider.dart:13`, `:82-86`
- **AC-WKT-01-02 · Iniciar libre.** *Cuando* se inicia un entrenamiento libre, *entonces* la fase pasa a en marcha, se registra la hora de inicio, la cuenta de muestreo vuelve a 0 sin puntos en memoria y se crea la sesión en la base (AC-WKT-06-01) sin rutina. El reloj de 1 s arranca cuando la sesión quedó creada (en la PC, a más tardar 100 ms después del toque): el primer segundo se ve 1 s después. El inmersivo se abre sin esperar a la base (AC-WKT-13-01). [dotnet] · [Editor-PM] · `workout_provider.dart:115-117`, `:133-149`
- **AC-WKT-01-03 · Iniciar con rutina.** *Cuando* se confirma una rutina que se puede iniciar (AC-WKT-12-08), *entonces*, antes de pasar a en marcha, el motor expande la rutina (AC-RUT-06), se ubica en el paso 0 con 0 s, toma la distancia del monitor de ese momento como inicio del paso, guarda la posición de cada paso en su serie (AC-RUT-07) y reinicia el cumplimiento de objetivos (AC-WKT-09-07); después sigue como el inicio libre. La sesión guarda el id y el nombre de la rutina **de la plantilla**, copiados: renombrar o borrar la rutina después no los cambia. [dotnet] · `workout_provider.dart:120-131`
- **AC-WKT-01-04 · Pausar.** *Cuando* se pausa, *entonces* la fase pasa a en pausa y no avanzan el reloj total, el reloj del paso, el muestreo, los objetivos ni los metros de un paso por distancia. Las lecturas del monitor y del pulsómetro se siguen recibiendo y mostrando (AC-WKT-17-06). [dotnet] · `workout_provider.dart:239-243`
- **AC-WKT-01-05 · Reanudar (Q-58).** *Cuando* se reanuda, *entonces* la fase vuelve a en marcha y el ritmo de 1 s empieza de nuevo desde ese instante: la fracción de segundo que corría al pausar se pierde. *Dado* una pausa a los 10,7 s de iniciar (con 10 segundos contados) y 30 s de pausa, *entonces* el segundo 11 llega 1 s después de reanudar, a los 41,7 s (no a los 41,0). La distancia del monitor al reanudar es la referencia nueva de los metros del paso: lo remado en pausa no cuenta (AC-WKT-03-03). [dotnet] · `workout_provider.dart:245-250`
- **AC-WKT-01-06 · Terminar.** *Cuando* la sesión termina (a mano, AC-WKT-20-04, o al completar la rutina, AC-WKT-03-05), *entonces* la fase pasa a terminada, el reloj se detiene y se guarda la sesión (AC-WKT-06-06); cuando el guardado termina, la sesión vuelve a reposo (AC-WKT-01-07) y el inmersivo se cierra (AC-WKT-13-04). [dotnet] · [Editor-PM] · `workout_provider.dart:252-303`
- **AC-WKT-01-07 · Volver a reposo.** *Cuando* la sesión vuelve a reposo, *entonces* se borran la rutina, el índice de paso, los dos relojes (a 0), la sesión en curso, la hora de inicio, los puntos en memoria, las posiciones y los objetivos. **No** se borran las últimas lecturas del monitor y del pulso ni "la última sesión terminada" (AC-WKT-06-11): al iniciar otra sesión, el HUD muestra los últimos valores del monitor hasta la lectura siguiente (paridad). [dotnet] · `workout_provider.dart:305-318`
- **AC-WKT-01-08 · Sin deshacer.** *Entonces* desde terminada solo se pasa a reposo: no hay "deshacer terminar" ni vuelta a en marcha. Desde reposo solo se puede iniciar. [dotnet]
- **AC-WKT-01-09 · La pestaña según la fase.** *Entonces* Workout muestra la vista de reposo **solo** en reposo. En marcha, en pausa o terminada, debajo del inmersivo muestra solo `workoutInProgress` (`Entrenamiento en curso...` / `Workout in progress...`), centrado y en `text.tertiary` (el legacy, al 38 %: DV-36), **sin la barra superior** `Entrenamiento` (paridad: esa vista del legacy es un `Scaffold` sin `AppBar`, `workout_screen.dart:180-191`). Se alcanza a ver solo mientras el inmersivo entra o sale. [Editor-PM] · `workout_screen.dart:19-35`, `:180-201`
- **AC-WKT-01-10 · Un toque, una sesión.** *Dado* Workout en reposo con el remo conectado, *cuando* se toca dos veces seguidas `Entrenamiento libre`, o dos veces `Iniciar` en el diálogo de inicio, *entonces* se crea **una** sesión y se abre **un** inmersivo: desde el primer toque la vista de reposo y el diálogo dejan de responder (como Q-19 en el editor). El legacy podía crear dos (DV-30). [Editor-PM] · `workout_screen.dart:86-89`, `:163-165`
- **AC-WKT-01-11 · Error al crear la sesión (Q-13).** *Dado* la base que falla al crear la sesión, *cuando* se inicia, *entonces* el entrenamiento se ve y anda igual (el reloj arranca, los pasos avanzan, el semáforo funciona), aparece el aviso `workoutSaveError` y no se guarda nada de esa sesión; al terminar se vuelve a reposo con el mismo aviso. En el legacy el error no se maneja: el reloj queda congelado en `00:00`, sin mensaje (DV-29). [dotnet] · [Editor-PM] · [Captura] CAP-29 · `workout_provider.dart:145-147`

### 4.2 F-WKT-02 — Reloj activo

- **AC-WKT-02-01 · Un segundo por segundo en marcha.** *Entonces* el motor cuenta un segundo por cada segundo en marcha, y ninguno en pausa, en reposo ni terminada. [dotnet] · `workout_provider.dart:151-152`
- **AC-WKT-02-02 · Orden dentro del segundo.** *Entonces*, en cada segundo y en este orden: (1) suman 1 el reloj del paso, el reloj total y la cuenta de muestreo; (2) si la cuenta es múltiplo de 5, se toma un punto (AC-WKT-06-02), también si el alta de la sesión todavía no volvió: se guarda cuando vuelve, o se descarta si el alta falló (AC-WKT-01-11; en el legacy el reloj recién arranca cuando vuelve el alta, `:145-147`, así que con la base al día da lo mismo); (3) con rutina, se alimenta el cumplimiento de objetivos con la lectura del paso en curso (AC-WKT-09-07) y se evalúa si el paso terminó (AC-WKT-03); (4) el HUD se actualiza. [dotnet] · `workout_provider.dart:153-190`
- **AC-WKT-02-03 · El reloj del HUD no es el del monitor.** *Entonces* el reloj de la banda (formato AC-WKT-11-06) es el reloj activo de la app, no el tiempo que informa el monitor; el del monitor es el que se guarda (AC-WKT-06-08). Con `R-4`, a los 12 s en marcha el HUD dice `00:12` y el monitor `312`. [dotnet] · [Editor-PM] · `immersive_workout_screen.dart:103`, `:569-577`
- **AC-WKT-02-04 · No se pierden ni se duplican segundos (DV-27).** *Entonces* el reloj activo sigue el tiempo real en marcha: si un cuadro llega tarde (la app estuvo trabada o en segundo plano), se procesan en orden los segundos que faltan, cada uno con la lectura que había en ese instante. Con el reloj de prueba, 600 s en marcha dan exactamente 600 segundos, también con un cuadro demorado 2,5 s en el medio. El legacy cuenta disparos del temporizador y pierde los segundos en que la app estuvo trabada o suspendida (`workout_provider.dart:147`); el cambio viene de Q-91 y del ADR 017. [dotnet]
- **AC-WKT-02-05 · Segundo plano: como si nada (Q-91).** *Dado* `Corta` con `R-4`, *cuando* la app pasa a segundo plano a los 8 s y vuelve a los 23 s, *entonces* al volver todo es igual que si hubiera seguido en primer plano: el reloj marca `00:23`; el paso es el 2 (el trabajo de la repetición 2) con la cuenta en `0:02`, por terminar y con `Próximo: Descanso`; la base tiene los puntos de los segundos 5, 10, 15 y 20 con los mismos valores e índices de paso (0, 0, 1 y 2) que sin segundo plano, y los objetivos dan el mismo resultado. [dotnet] (ciclo de vida de prueba) · ADR 017; FEATURE_INVENTORY Q-91
- **AC-WKT-02-06 · Segundo plano: la rutina termina mientras tanto.** *Dado* lo mismo, *cuando* la app vuelve a los 40 s (la rutina terminó a los 30), *entonces* la sesión quedó cerrada como si hubiera terminado en primer plano (AC-WKT-06-06), con los 7 puntos de AC-WKT-06-03 y la hora de fin del segundo 30 (`2026-10-28T13:00:30.000Z`, no la de la vuelta), y al volver se ve Workout en reposo (si el cierre todavía no volvió, mientras tanto el inmersivo muestra la rutina terminada: AC-WKT-03-06). Cuando aparece Workout en reposo se muestra **una sola vez** el aviso `workoutCompleted` (o `workoutSaveError` si algo no se guardó), con sus 4 s contados desde ese momento: el usuario se entera aunque no haya visto terminar la rutina. En el legacy el diálogo de completado es modal y queda hasta que se lo toca (`immersive_workout_screen.dart:207-212`); en la 4b lo reemplaza ese diálogo (DV-34). Lo mismo si la vuelta llega mucho después del fin (por ejemplo, a los 330 s): el tiempo que la app pasó en segundo plano no cuenta como demora del guardado, así que una sesión que se guardó bien no se da por fallada ni aparece `workoutSaveError`, y queda como "la última terminada" (AC-WKT-06-11). [dotnet] (ciclo de vida de prueba) · [Editor-PM] (ciclo de vida de prueba: el aviso)
- **AC-WKT-02-07 · Segundo plano en pausa.** *Dado* una sesión en pausa, *cuando* la app pasa 60 s en segundo plano y vuelve, *entonces* la sesión sigue en pausa y no cambiaron el reloj, el paso ni los puntos. [dotnet]
- **AC-WKT-02-08 · Al volver, el presente.** *Entonces*, al volver del segundo plano, el primer cuadro ya muestra el estado actual (reloj, valores, paso, cuenta regresiva): no se ve el reloj "corriendo" hasta alcanzar la hora, ningún número pasa por valores intermedios (a lo sumo un cambio del valor viejo al nuevo) y no se acumulan avisos viejos: de lo que pasó en segundo plano se muestra, una vez y con sus 4 s desde la vuelta, solo el último aviso (por ejemplo, el del fin de la sesión: AC-WKT-02-06). [Editor-PM] (ciclo de vida de prueba) · [Device-And] (NO VERIFICABLE sin módulo y teléfono; en Windows minimizada la app sigue andando y no hay vuelta que alcanzar: AC-WKT-02-09) · FEATURE_INVENTORY Q-91 ("sin saltos raros")
- **AC-WKT-02-09 · Windows: ventana minimizada o sin foco.** *Dado* el build de simulador con un entrenamiento libre en marcha, *cuando* la ventana se minimiza 2 min (o queda 2 min detrás de otra ventana, sin foco) y se restaura, *entonces* el reloj avanzó `2:00` (± 1 s), la distancia siguió sumando lo que remó el simulador y la base tiene 24 puntos más (uno cada 5 s). Con una rutina, los pasos por tiempo avanzaron como en primer plano y, si la rutina terminó, se ve Workout en reposo con la sesión guardada; el aviso `workoutCompleted` aparece una sola vez al restaurar la ventana o al volver el foco (AC-WKT-02-06), aunque la app siguió andando mientras tanto. [Device-Win] · ARCHITECTURE §6.1
- **AC-WKT-02-10 · Android: otra app o pantalla bloqueada.** *Dado* Android con el simulador y un entrenamiento en marcha, *cuando* se pasa a otra app o se bloquea la pantalla durante 2 min y se vuelve, *entonces* pasa lo mismo que en AC-WKT-02-09. Si el sistema cierra la app mientras está en segundo plano, la sesión queda como una sesión cortada con los puntos hasta que pasó a segundo plano (AC-WKT-06-15; riesgo RA-08, que se mide en la It. 10a). Con el remo real (que el monitor no salga del modo Bluetooth) se verifica en la 10a, y en iOS y macOS en la 10b ([Apple]). [Device-And] (NO VERIFICABLE sin módulo y teléfono) · ARCHITECTURE §6.1

### 4.3 F-WKT-03 — Avance de pasos y fin automático de la rutina

- **AC-WKT-03-01 · La rutina expandida, con progresiones.** *Entonces* el motor recorre la rutina **expandida** de AC-RUT-06: cada serie repetida N veces y, en la repetición r (desde 0), cada objetivo con progresión vale base + progresión × r, solo si existen los dos; los pasos sueltos quedan como están. `Prueba 4a` da 6 pasos: calentamiento 30 s; trabajo 60 s con vatios mín. 160 y SPM 22; descanso 30 s; trabajo 60 s con vatios mín. 160 y SPM 24; descanso 30 s; enfriamiento 30 s. [dotnet] · `lib/core/models/routine.dart:29-70`; `workout_provider.dart:122`
- **AC-WKT-03-02 · Paso por tiempo.** *Entonces* un paso por tiempo termina en el segundo en que su reloj llega a la duración (≥); si el paso tiene tiempo y distancia, manda el tiempo. En `Corta` los cambios de paso caen en los segundos 10, 15 y 25 y la rutina termina en el 30. [dotnet] · `workout_provider.dart:196-197`
- **AC-WKT-03-03 · Paso por distancia, con el saneamiento de los parciales (Q-55, DV-26).** *Entonces* un paso por distancia termina en el primer segundo en que los metros contados desde que empezó el paso llegan a su largo (≥). Los metros se cuentan como los de los parciales (F-WKT-07, puntos 6 y 7): en cada segundo suma lo que avanzó el monitor desde la lectura anterior, salvo que sea negativo o más de 10 m por segundo, en cuyo caso no suma y esa lectura pasa a ser la referencia; al reanudar, la referencia es la distancia de ese momento. Se evalúa solo en el segundo, así que puede cerrar hasta 1 s después de cruzar la marca. Con `Distancia 100`: [dotnet] · `workout_provider.dart:198-201`; `lib/features/workout/series_tracker.dart:118-127`

  | Remo | Termina en el segundo | Legacy (distancia cruda contra la del inicio) |
  |---|---|---|
  | `R-4` | 25 | 25 |
  | `R-4R` (el monitor se reinicia en el segundo 10) | 26 | 285 |
  | `R-4Z` (un paquete sin distancia en el segundo 10) | 27 | 25 |
  | `R-4`, con 20 s de pausa a los 40 m, remando en la pausa (80 m) | 25 (segundos en marcha) | 11 (cuenta lo remado en pausa) |

- **AC-WKT-03-04 · Cambio de paso.** *Entonces*, al terminar un paso que no es el último, en ese mismo segundo: se cierra su evaluación de objetivos (AC-WKT-09-07), el índice sube 1, el reloj del paso vuelve a 0 y el paso nuevo arranca desde la distancia del monitor de ese segundo (los metros de más quedan en el que terminó). [dotnet] · `workout_provider.dart:203-209`
- **AC-WKT-03-05 · Fin automático (provisorio hasta la 4b).** *Entonces*, al terminar el último paso, en ese mismo segundo la fase pasa a terminada (ningún otro segundo vuelve a evaluar), el reloj se detiene y empieza el guardado (AC-WKT-06-06). Cuando el guardado termina, el inmersivo se cierra y Workout en reposo muestra el aviso `workoutCompleted` (`¡Entrenamiento completado!` / `Workout Completed!`); si el guardado falló, el aviso es `workoutSaveError`. Si la app estaba en segundo plano o minimizada, el aviso aparece al volver (AC-WKT-02-06); si el diálogo de terminar estaba abierto, se cierra sin efecto (AC-WKT-20-07). Es **provisorio**: en la 4b lo reemplaza el diálogo de rutina completada (F-WKT-21; DV-34). Al terminar a mano nunca aparece este aviso (Q-53). [dotnet] · [Editor-PM] · [Captura] CAP-28, DEMO-13 · `workout_provider.dart:210-216`; `immersive_workout_screen.dart:49-55`
- **AC-WKT-03-06 · La rutina terminada.** *Entonces*, entre el fin y el cierre del inmersivo, el índice queda en el último paso (no avanza). Si ese paso es por tiempo, su reloj queda en la duración y la cuenta muestra `0:00` (AC-WKT-04-07); si es por distancia, la cuenta sigue mostrando su largo (`{metros}m`, AC-WKT-11-08), sin restante ni progreso (AC-WKT-04-02, -03). Un paso sin meta nunca termina solo (AC-WKT-03-08). [dotnet] · `workout_provider.dart:31-36`, `:101-104`, `:210-216`; `immersive_workout_screen.dart:456-458`
- **AC-WKT-03-07 · Paso de 0 s o de duración negativa (DV-37).** *Entonces* un paso de 0 s termina en el primer segundo. Uno de duración negativa también: el editor lo deja guardar (un `Min` negativo con el teclado con signo de DV-15, y los topes de Q-92 van por valor absoluto), su restante es 0 y, mientras dura, la cuenta muestra `0:00`, sin "por terminar" (con `Próximo` si hay un paso siguiente: AC-WKT-04-05). En el legacy el motor también lo termina en el primer segundo (`workout_provider.dart:196-197`), pero el inmersivo falla mientras ese paso está en curso: el restante usa `clamp(0, d)` con `d < 0`, que lanza (`:103`). [dotnet] · [Editor-PM]
- **AC-WKT-03-08 · Paso sin meta (DV-25).** *Dado* `Sin meta` (el editor permite un paso sin meta, Q-25; AC-RUT-03-03), *entonces* ese paso nunca termina solo, no tiene restante ni progreso, su cuenta regresiva muestra `?` (`stepDurationNone`) y la sesión se termina con `Terminar`. El legacy muestra `nullm` (`immersive_workout_screen.dart:456-458`). [dotnet] · [Editor-PM] · [Captura] CAP-30
- **AC-WKT-03-09 · Libre.** *Entonces* el entrenamiento libre no tiene pasos ni fin automático: solo termina a mano. [dotnet]
- **AC-WKT-03-10 · El remo se desconecta durante la sesión.** *Dado* una rutina en marcha, *cuando* el remo se desconecta (con el simulador, `Simular desconexión`), *entonces* la sesión sigue: el HUD queda con la última lectura, el reloj y los pasos por tiempo avanzan, el paso por distancia queda quieto, los puntos de ese lapso repiten la última lectura y no aparece ningún aviso (el de caída es de la It. 10a, Q-34). Cuando el remo vuelve, todo sigue con las lecturas nuevas. [dotnet] · [Editor-PM] · `workout_provider.dart:220-224`

### 4.4 F-WKT-04 — Estado del paso en curso

- **AC-WKT-04-01 · Cuándo existe.** *Entonces* el estado del paso existe solo con rutina y en marcha, en pausa o terminada; en libre y en reposo no existe. Tiene el paso (con los objetivos ya progresados), su índice desde 0, el total de pasos expandidos, los segundos transcurridos en el paso y el restante. [dotnet] · `workout_provider.dart:98-112`
- **AC-WKT-04-02 · Restante.** *Entonces* el restante de un paso por tiempo es duración − transcurrido, acotado entre 0 y la duración; con una duración negativa, 0 (DV-37). Un paso por distancia o sin meta no tiene restante. [dotnet] · `workout_provider.dart:101-104`
- **AC-WKT-04-03 · Progreso.** *Entonces* el progreso (de 0 a 1) es transcurrido / duración, acotado, solo en pasos por tiempo con duración mayor que 0; en cualquier otro caso, 0 (Q-51: el paso por distancia no muestra avance). [dotnet] · `workout_provider.dart:31-36`
- **AC-WKT-04-04 · Por terminar.** *Entonces* el paso está "por terminar" con un restante de 1 a 5 s, inclusive; con 0 o sin restante, no. [dotnet] · `workout_provider.dart:39-41`
- **AC-WKT-04-05 · A continuación.** *Entonces* aplica "a continuación" con un restante de 0 a 15 s, inclusive, y un paso siguiente; nunca en el último paso ni en pasos por distancia o sin meta. [dotnet] · `immersive_workout_screen.dart:342-346`
- **AC-WKT-04-06 · Ejemplos.** *Entonces*: en un paso de 60 s que no es el último, "a continuación" desde que la cuenta muestra `0:15` (45 s transcurridos) hasta el cambio, y "por terminar" de `0:05` a `0:01`; `0:00` nunca se ve, porque en ese segundo el motor ya pasó al paso siguiente. En un paso de 10 s, "a continuación" durante todo el paso. En el último paso, "por terminar" sí y "a continuación" no. En un paso por distancia, ninguno de los dos. [dotnet] · [Editor-PM] · [Captura] CAP-21, CAP-22
- **AC-WKT-04-07 · La rutina terminada.** *Entonces*, con la rutina terminada, si el último paso es por tiempo tiene restante 0 (`0:00`) y progreso 1; si es por distancia, no tiene restante (la cuenta sigue mostrando su largo) y su progreso es 0. En los dos casos no está por terminar ni hay "a continuación". [dotnet] · `workout_provider.dart:31-36`, `:101-104`

### 4.5 F-WKT-05 (+ F-HRM-14) — Fusión del pulso

Con el remo de prueba y el pulsómetro de prueba (§0.4). Los cuatro primeros son los casos legacy LT-35 a LT-38.

- **AC-WKT-05-01 · El sensor manda sobre el monitor (LT-35).** *Entonces*: una lectura del remo con 100 W y 90 bpm da pulso 90; un bpm 142 del sensor da pulso 142 con los vatios en 100, y la vista se enteró exactamente 2 veces (la lectura y el bpm); una lectura del remo con 120 W y 90 bpm da 142 y 120 W; un bpm 0 da 0; un bpm 130 seguido de "desconectado" da 0; una lectura del remo con 90 bpm da 90. [dotnet] · `test/workout_provider_hr_test.dart:80`; `workout_provider.dart:220-237`
- **AC-WKT-05-02 · Orden real de una caída (LT-36).** *Entonces* una lectura del remo con 90 y un bpm 140 dan 140; un bpm 0 seguido de "desconectado" da 0; la lectura siguiente del remo, con 90, da 90 (el remo repone su valor). [dotnet] · `test/workout_provider_hr_test.dart:125`
- **AC-WKT-05-03 · Un bpm durante "conectando" se conserva (LT-37).** *Entonces* "conectando", un bpm 120 y "conectado" dan 120; una lectura del remo con 90 deja el pulso en 120. [dotnet] · `test/workout_provider_hr_test.dart:148`
- **AC-WKT-05-04 · Un estado sin bpm previo no toca nada (LT-38).** *Entonces* "conectado" y "desconectado" sin ningún bpm antes no cambian los datos y la vista no se entera (0 avisos). [dotnet] · `test/workout_provider_hr_test.dart:167`
- **AC-WKT-05-05 · En todas las fases y para todo lo que sigue.** *Entonces* la fusión anda en todas las fases, también en reposo y en pausa; cualquier estado del sensor distinto de "conectado" (`buscando`, `conectando`, `desconectado`) después de un bpm pone el pulso en 0, y la lectura siguiente del remo repone el suyo. Todo lo que sigue usa el pulso fusionado: la tarjeta BPM del HUD (AC-WKT-17-05) y la columna `heart_rate` de los puntos (AC-WKT-06-02). [dotnet] · `workout_provider.dart:68-70`, `:232-237`; `lib/core/bluetooth/heart_rate_service.dart:10`
- **AC-WKT-05-06 · Con el pulsómetro simulado.** *Dado* el simulador con el pulsómetro simulado conectado desde Dispositivo, *entonces* el HUD muestra la tarjeta BPM con el pulso del sensor (el remo simulado manda 0: AC-BLE-15-03). *Cuando* se toca `Simular caída del pulsómetro`, *entonces* la tarjeta desaparece mientras dura y los puntos de ese lapso tienen pulso 0; cuando el sensor vuelve, la tarjeta vuelve. [Editor-PM] · [Captura] CAP-13

### 4.6 F-WKT-06 (+ F-HIS-01, F-HIS-02, F-HIS-03) — Telemetría y guardado de la sesión

- **AC-WKT-06-01 · Alta de la sesión (F-HIS-01).** *Cuando* se inicia (AC-WKT-01-02, -03), *entonces* se crea en el acto una fila en `workout_sessions` con: `routine_id` y `routine_name` de la rutina de la plantilla (vacíos en libre); `started_at` = el momento del inicio, en UTC con el formato de AC-RUT-16-04 (con el reloj de prueba, `2026-10-28T13:00:00.000Z`); `finished_at` vacío; los totales en 0; `strava_activity_id` vacío; `origin` = `app`. La sesión existe aunque después no se reme. [dotnet] · `workout_provider.dart:133-145`; `lib/core/models/workout_session.dart:39-51`
- **AC-WKT-06-02 · Un punto cada 5 s en marcha (F-HIS-02).** *Entonces* en los segundos 5, 10, 15… en marcha (sin contar pausas) se toma un punto con la última lectura: `elapsed_seconds` = el tiempo **del monitor** (no el reloj del HUD), `stroke_rate`, `stroke_count`, `distance_meters`, `pace_500m_seconds`, `power_watts`, `calories`, `heart_rate` (el fusionado, AC-WKT-05) y, con rutina, `step_index` (índice expandido, desde 0) y `step_type` (`warmup`, `work`, `rest` o `cooldown`); en libre, esos dos vacíos. Con `R-4`, libre: [dotnet] · `workout_provider.dart:158-173`

  | Segundo | `elapsed_seconds` | `stroke_rate` | `stroke_count` | `distance_meters` | `pace_500m_seconds` | `power_watts` | `calories` | `heart_rate` | `step_index` / `step_type` |
  |---|---|---|---|---|---|---|---|---|---|
  | 5 | 305 | 24.0 | 105 | 1020 | 125 | 150 | 40 | 0 | vacíos |
  | 10 | 310 | 24.0 | 110 | 1040 | 125 | 150 | 41 | 0 | vacíos |

- **AC-WKT-06-03 · El punto es del paso que estaba en curso.** *Entonces* el punto de un segundo se toma antes de evaluar el fin del paso, así que pertenece al paso que termina en ese segundo. `Corta` con `R-4` da **7 puntos** (segundos 5, 10, 15, 20, 25 y 30, más el final) con `step_index` 0, 0, 1, 2, 2, 3, 3 y `step_type` `work`, `work`, `rest`, `work`, `work`, `rest`, `rest`. [dotnet] · `workout_provider.dart:158-187`
- **AC-WKT-06-04 · En pausa no se muestrea.** *Entonces* en pausa no se toman puntos y la cuenta de 5 s sigue al reanudar desde donde estaba: con 7 s en marcha, 30 s de pausa y 5 s más, los puntos caen en los segundos en marcha 5 y 10, y ninguno en la pausa. [dotnet] · `workout_provider.dart:156`, `:239-250`
- **AC-WKT-06-05 · Guardado incremental (Q-12).** *Entonces* cada punto queda guardado en la base, de forma que sobrevive a un cierre forzado, a más tardar 1 s después de tomarse. El legacy los tiene en memoria hasta terminar y una sesión cortada queda sin puntos (DC-09). *Dado* una sesión libre con `R-4`, *cuando* el proceso se termina a la fuerza a los 31,5 s, *entonces* la base tiene los 6 puntos de los segundos 5 a 30. [dotnet] (SQLite real, cierre simulado) · [Device-Win] (Administrador de tareas) · FEATURE_INVENTORY Q-12
- **AC-WKT-06-06 · Cierre de la sesión (F-HIS-03).** *Cuando* la sesión termina (a mano o sola), *entonces*: (1) se toma un último punto con la lectura de ese momento y el paso en curso (con el tipo vacío si el índice quedara fuera de rango); (2) se calculan las estadísticas sobre **todos** los puntos (AC-RUT-18); (3) con todos los puntos ya guardados, incluido el final, se cierra la sesión: `finished_at` = ahora (UTC), `total_distance_meters` = máximo de la distancia, `total_time_seconds` = máximo del tiempo del monitor, `avg_power_watts` = P99 de vatios redondeado, `avg_stroke_rate` = P99 de SPM, `total_calories` = máximo de kcal, `strava_activity_id` vacío (AC-RUT-16-02). Con `R-4`, libre, terminada a los 12,3 s: 3 puntos (tiempos del monitor 305, 310 y 312); la sesión queda con `finished_at` `2026-10-28T13:00:12.300Z`, 1048 m, 312 s, 150 W, 24.0 spm y 41 kcal. [dotnet] · `workout_provider.dart:252-296`
- **AC-WKT-06-07 · Igual que el legacy (Q-12).** *Entonces* una sesión terminada normalmente queda **idéntica** a la que guarda el legacy con las mismas lecturas: los mismos puntos (cantidad, orden y valores) y los mismos totales. En particular, si el cierre cae en un segundo múltiplo de 5 (pasa siempre que la rutina termina sola y todos sus pasos son por tiempo de múltiplos de 5 s), el punto final repite al de ese segundo: en `Corta` los dos últimos puntos son iguales (F-HIS-03; el "Rendimiento por paso" de la It. 9 cuenta con eso). Se comprueba con los escenarios de §0.4 y, si el Architect puede correr el motor del legacy en el oráculo Dart, también con sus vectores (QA-P-23). [dotnet]
- **AC-WKT-06-08 · Los contadores del monitor (Q-15).** *Entonces* la distancia, las kcal y el tiempo guardados son los contadores del monitor, no lo remado desde el inicio: con `R-4` (el monitor ya tenía 1000 m y 300 s), 12 s guardan 1048 m y 312 s. Si el monitor no manda el tiempo (llega 0), la sesión guarda 0 aunque el HUD haya mostrado un reloj; si el monitor sigue contando en pausa, el tiempo guardado puede ser mayor que el del HUD. [dotnet] · FEATURE_INVENTORY Q-15
- **AC-WKT-06-09 · Sesiones cortas y sin remar.** *Entonces* una sesión de menos de 5 s tiene un solo punto, el final, y una sesión que se termina sin remar existe igual, con su punto. [dotnet]
- **AC-WKT-06-10 · Sesión cortada.** *Dado* que la app se cierra o se cae a mitad de una sesión (se cierra la ventana, se termina el proceso, se corta la luz), *entonces* la sesión queda con `finished_at` vacío, los totales en 0 y los puntos guardados hasta ese momento (AC-WKT-06-05). Al volver a abrir, la app arranca en Dispositivo en reposo (AC-APP-01-01), sin ofrecer retomar ni cerrar esa sesión, y no la modifica (paridad; la repara la It. 13, F-HIS-21, y el Historial la muestra con sus puntos desde la It. 9). [dotnet] · [Device-Win] · [QA] (con la herramienta de §12.9)
- **AC-WKT-06-11 · La última sesión terminada.** *Entonces*, si el cierre salió bien, la app recuerda esa sesión como "la última terminada" (la usa la subida a Strava de la It. 12); si falló, sigue la anterior, o ninguna. [dotnet] · `workout_provider.dart:78-80`, `:296`
- **AC-WKT-06-12 · Errores al guardar (Q-13).** *Dado* la base que falla al guardar un punto o al cerrar la sesión, *entonces* aparece el aviso `workoutSaveError` (`No se pudo guardar la sesión` / `Couldn't save the session`), a lo sumo una vez mientras se rema; el entrenamiento sigue en pantalla sin cambios; lo que se pudo guardar queda guardado; al terminar se vuelve a reposo con el mismo aviso, salvo que el reintento del cierre (abajo) haya guardado todo: entonces el aviso del final es el de un guardado bueno, `workoutCompleted` si la rutina terminó sola y ninguno al terminar a mano (DT4a-17, decidido al cerrar: §17). Una sesión que queda cerrada tiene **todos** sus puntos: los que no se pudieron guardar mientras se remaba se vuelven a intentar junto con el cierre y, si tampoco entonces entran, la sesión no se cierra y queda como una cortada (AC-WKT-06-10), con lo que sí se guardó. Así los totales de una sesión cerrada siempre salen de sus puntos guardados, y el Historial (It. 9), que calcula los suyos desde los puntos (`lib/core/database/database_service.dart:289`; `lib/features/history/history_provider.dart:31`), no da otros números que la sesión. En el legacy los puntos y el cierre van en el mismo intento (`workout_provider.dart:257-299`): nunca queda una sesión cerrada sin sus puntos. Nunca se cuelga ni queda un indicador eterno. El legacy no avisa nada (DV-29). [dotnet] · [Editor-PM] · [Captura] CAP-29 · `workout_provider.dart:297-299`
- **AC-WKT-06-13 · El guardado no frena el HUD.** *Dado* la base lenta (cada escritura tarda 2 s), *entonces* el reloj, los valores y los pasos siguen al día y sin tirones; los puntos se guardan en el orden en que se tomaron y el cierre de la sesión va después del último; al terminar, el inmersivo se cierra cuando el cierre quedó guardado. [dotnet] · [Editor-PM]
- **AC-WKT-06-14 · Lo que no se guarda.** *Entonces* los parciales, las repeticiones y el resultado de objetivos de cada paso no se guardan (paridad). [QA] · `workout_provider.dart:57-61`
- **AC-WKT-06-15 · Al pasar a segundo plano.** *Cuando* la app pasa a segundo plano con una sesión en curso, *entonces* en ese momento se manda a guardar todo lo pendiente, también los puntos tomados mientras el alta de la sesión estaba en vuelo, y la app espera **a lo sumo 1 s** a que quede guardado (con la base al día alcanza de sobra; el tope evita que el sistema dé la app por colgada). Si el sistema la cierra estando en segundo plano, la sesión queda como una cortada (AC-WKT-06-10) con los puntos guardados hasta ese momento: con la base al día, todos los de hasta que pasó a segundo plano; con una base que tarda más de 1 s, pueden faltar los últimos (RA-08; ADR 017, decisión 5). [dotnet] (ciclo de vida de prueba, también con un alta lenta y el paso a segundo plano a los 6 s)

### 4.7 F-WKT-09 — Cumplimiento de objetivos por paso

Los seis primeros son los casos legacy LT-39 a LT-44 (lecturas de prueba con 150 W, 24 spm y split 130 salvo lo que se indica, sobre un trabajo de 60 s).

- **AC-WKT-09-01 · Cumplido por el promedio de vatios (LT-39).** *Entonces* lecturas de 140 y 160 W con un objetivo de 150 W dan **cumplido**. [dotnet] · `test/step_target_tracker_test.dart:20`
- **AC-WKT-09-02 · Falla uno, no se cumple (LT-40).** *Entonces* 200 W y 22 spm contra objetivos de 150 W y 26 spm dan **no cumplido**. [dotnet] · `test/step_target_tracker_test.dart:28`
- **AC-WKT-09-03 · El split se cumple por debajo (LT-41).** *Entonces* un split de 125 contra 130 se cumple y uno de 140 contra 130, no. [dotnet] · `test/step_target_tracker_test.dart:35`
- **AC-WKT-09-04 · Sin objetivos no hay resultado (LT-42).** *Entonces* un paso sin objetivos no tiene resultado. [dotnet] · `test/step_target_tracker_test.dart:46`
- **AC-WKT-09-05 · Sin remar no se cumple; los ceros no cuentan (LT-43).** *Entonces* un paso con objetivo y sin ninguna lectura remando da **no cumplido**; otro con una lectura parada (0 W, 0 spm, split 0) y otra de 120 W, 24 spm y split 0, contra objetivos de 100 W y split 200, da **cumplido**: lo parado no baja el promedio y el split sin dato no se evalúa. [dotnet] · `test/step_target_tracker_test.dart:53`
- **AC-WKT-09-06 · Sin completar no hay resultado; volver a reposo borra (LT-44).** *Entonces* antes de completar el paso no hay resultado; completado (150 W contra 100) es **cumplido**; después de reiniciar, otra vez sin resultado. [dotnet] · `test/step_target_tracker_test.dart:65`
- **AC-WKT-09-07 · Desde el motor.** *Entonces*: un paso tiene objetivos si define vatios mín., SPM o split (los vatios máx. no cuentan); en cada segundo en marcha, antes de evaluar el fin del paso, se acumula la lectura del paso en curso, salvo una lectura parada (SPM ≤ 0 **y** vatios ≤ 0), y el split solo si es mayor que 0; al completarse el paso (por tiempo o por distancia) se comparan los promedios: vatios ≥ mín., SPM ≥ objetivo y split ≤ objetivo (sin split informado, ese objetivo no se evalúa), y es cumplido solo si cumple todos los definidos. Un paso cortado con `Terminar` queda sin resultado; el último paso también se evalúa. Todo se reinicia al iniciar una rutina y al volver a reposo, y no se guarda. `Corta` con `R-4`: los pasos 0 y 2 cumplidos y los descansos sin resultado; con `R-4(19)`, el paso 2 (objetivo 20) no cumplido. En la 4a no se ve: lo muestran las píldoras de la 4b. [dotnet] · `lib/features/workout/step_target_tracker.dart:1-71`; `workout_provider.dart:129`, `:185`, `:204`, `:316`

### 4.8 F-WKT-10 — Semáforo de objetivos en vivo

- **AC-WKT-10-01 · Qué tarjetas.** *Entonces* el semáforo aplica solo a las tarjetas SPM, Split y Watts, y a cada una solo si el paso en curso define ese objetivo (el de la repetición, ya progresado). [dotnet] (regla) · [Editor-PM] · `immersive_workout_screen.dart:585-602`
- **AC-WKT-10-02 · SPM.** *Entonces* SPM está fuera de objetivo si el actual es menor que el objetivo; igual o mayor cumple: `24.0` contra 24 cumple, `23.9` no. [dotnet] · `immersive_workout_screen.dart:582`
- **AC-WKT-10-03 · Watts.** *Entonces* Watts está fuera si el actual es menor que los vatios mín.; los máx. no se miran: 190 W contra 150-180 cumple. [dotnet] · `immersive_workout_screen.dart:594-597`
- **AC-WKT-10-04 · Split.** *Entonces* Split está fuera si el actual es mayor (más lento) que el objetivo, y cumple si es igual o menor (paridad: `val > target`, sin casos especiales). Un split 0 (`--:--`) cumple con un objetivo positivo y también con un objetivo **0**; con un objetivo 0, cualquier split mayor que 0 queda fuera. Con un objetivo **negativo**, Split está siempre fuera, también con `--:--`. Los objetivos 0 y negativos los deja una progresión negativa, que los topes de Q-92 permiten porque van por valor absoluto (split 4 s con −2 s por repetición, ×3: 4, 2 y 0; AC-RUT-04-04 muestra `00:00/500m`). Casos: `--:--` contra 130 → cumple; contra 0 → cumple; contra −5 → fuera; 125 contra 0 → fuera. [dotnet] · `immersive_workout_screen.dart:579-583`, `:598-602`
- **AC-WKT-10-05 · Aspecto (Q-60, D-ART-01, D-ART-09; DV-32).** *Entonces* una tarjeta sin objetivo va en el color de su métrica, con el borde `#FFFFFF @10 %` de 1 dp y sin halo (AC-APP-06-01). Con objetivo: la etiqueta en el color base de la métrica; el borde del color base al 70 %, de 1,5 dp, con un halo del mismo color al 20 % solo por fuera; debajo del valor, `hudTarget` con el objetivo, 12/600 al 70 % (`obj. 22` / `target 22`; Watts, los vatios mín.: `obj. 160`; Split con el formato de su objetivo en AC-WKT-11-13: `obj. 02:05`); y el valor en blanco `#FFFFFF` si cumple, o en `state.offTarget.hud` `#FF625B` con una flecha a su izquierda si está fuera: `▲` en SPM y Watts (hay que subir), `▼` en Split (hay que bajar). La unidad `W` no cambia con el semáforo: sigue en `metric.watts.hud` al 80 % (DV-31). El legacy pone el valor y la etiqueta en blanco o en `#FF4444`, y el borde y el halo en ese mismo color. Si el usuario rechaza D-ART-01, el valor fuera va en `#FF4444` sin flecha; si rechaza D-ART-09, no hay `obj.` [Editor-PM] · [Captura] CAP-19, CAP-20, CAP-24 · `immersive_workout_screen.dart:768-783`
- **AC-WKT-10-06 · En el acto, también en pausa.** *Entonces* se evalúa con cada lectura, en el instante, sin promedios ni margen de tolerancia, también en pausa (las lecturas siguen llegando: AC-WKT-17-06). El cambio de color se ve con una transición corta (150 ms) que nunca demora el umbral: la tarjeta pasa a "fuera" desde la lectura misma (ANIMATION_BIBLE §2.8). [dotnet] (regla) · [Captura] VID-02
- **AC-WKT-10-07 · Parado.** *Entonces* parado (SPM 0 y 0 W; con el simulador, `Remando` apagado) SPM y Watts con objetivo están fuera y Split en `--:--` cumple (salvo un objetivo negativo: AC-WKT-10-04). [Editor-PM]
- **AC-WKT-10-08 · Libre.** *Entonces* en el entrenamiento libre ninguna tarjeta tiene semáforo. [Editor-PM]
- **AC-WKT-10-09 · Con el simulador (la demo).** *Dado* `Prueba 4a` y el simulador sin ruido, *entonces*: en el trabajo de la repetición 1, a 150 W, Watts está fuera (`obj. 160`, `▲`) y SPM cumple (`24.0` contra 22); con `+` de Watts (160 W), Watts cumple desde la lectura siguiente; en la repetición 2 (objetivo 24) SPM cumple a 24 spm y queda fuera con `−` de Ritmo (23); en el calentamiento, los descansos y el enfriamiento no hay semáforo. [Editor-PM] · [Captura] CAP-19, DEMO-09, DEMO-10, DEMO-11

### 4.9 F-WKT-11 (+ F-APP-12) — Formatos de display

Los 15 formatos de la ficha, como reglas puras, más su uso en es y en. Los ejemplos son los resultados esperados; los marcados con LT-45 son del caso legacy "formatos".

- **AC-WKT-11-01 · Split en vivo.** *Entonces* `MM:SS` con los minutos a dos dígitos; ≤ 0 se ve `--:--`: 133 → `02:13`; 59 → `00:59`; 6000 → `100:00`; 0 y −5 → `--:--`. Es la regla de AC-BLE-01-02. [dotnet] · `lib/core/models/rowing_data.dart:24-29`
- **AC-WKT-11-02 · SPM.** *Entonces* un decimal con **punto**, en los dos idiomas: 24.0 → `24.0`; 23.5 → `23.5`; 24.25 → `24.3`; 0 → `0.0` (la regla de AC-BLE-01-05). [dotnet] · `immersive_workout_screen.dart:625`
- **AC-WKT-11-03 · Watts.** *Entonces* entero, con la unidad `hudUnitWatts` (`W`) aparte: 149 → `149` y `W`; −10 → `-10` y `W` (Q-38: se muestra crudo). [dotnet] · `immersive_workout_screen.dart:655-656`
- **AC-WKT-11-04 · Distancia en el HUD.** *Entonces* entero sin separador de miles, con la unidad `hudUnitMeters` (`m`) aparte: 1484 → `1484` y `m`; 123456 → `123456`. [dotnet] · `immersive_workout_screen.dart:698-699`
- **AC-WKT-11-05 · kcal y pulso.** *Entonces* enteros sin unidad: 89 → `89`; 142 → `142`. [dotnet] · `immersive_workout_screen.dart:717`, `:725`
- **AC-WKT-11-06 · Reloj de la sesión.** *Entonces* `H:MM:SS` desde 1 h y `MM:SS` por debajo: 0 → `00:00`; 175 → `02:55`; 3725 → `1:02:05`; 36000 → `10:00:00` (la regla de AC-BLE-01-03). [dotnet] · `immersive_workout_screen.dart:569-577`
- **AC-WKT-11-07 · Cuenta regresiva de un paso por tiempo.** *Entonces* `M:SS` con los minutos sin cero a la izquierda: 65 → `1:05`; 5 → `0:05`; 720 → `12:00`; 0 → `0:00`; 359 999 → `5999:59`. [dotnet] · `immersive_workout_screen.dart:456-457`
- **AC-WKT-11-08 · Cuenta de un paso por distancia.** *Entonces* el largo total del paso, `{metros}m` sin espacio ni separador de miles (`stepDurationMeters`): 500 → `500m`; 1000 → `1000m`; no muestra lo que falta (Q-51). Un paso sin meta muestra `?` (`stepDurationNone`; DV-25). [dotnet] · `immersive_workout_screen.dart:458`
- **AC-WKT-11-09 · Tiempo de vuelta (LT-45).** *Entonces* `M:SS.d`, redondeado a las décimas totales y la mitad hacia arriba: 166.667 → `2:46.7`; 65 → `1:05.0`; 55 → `0:55.0`; 59.96 → `1:00.0`. Lo usan los parciales de la It. 5. [dotnet] · `lib/features/workout/series_panels.dart:9-14`
- **AC-WKT-11-10 · Split de una repetición (LT-45).** *Entonces* `M:SS`, redondeado al segundo y la mitad hacia arriba: 114.4 → `1:54`; 133.5 → `2:14`; sin metros de trabajo, `—`. Lo usa el panel de repeticiones de la It. 5. [dotnet] · `series_panels.dart:16-20`, `:340`
- **AC-WKT-11-11 · Metros de los paneles (LT-45).** *Entonces* con separador de miles con **punto**, siempre al estilo español aunque la app esté en inglés, y `␣m`: 1245 → `1.245 m`; 300 → `300 m`; 1 000 000 → `1.000.000 m`; 0 → `0 m`. [dotnet] · `series_panels.dart:22-24`
- **AC-WKT-11-12 · Hora.** *Entonces* `HH:mm` de 24 h: 09:05 → `09:05`; 13:54 → `13:54`; 00:00 → `00:00`; 23:59 → `23:59`. Lo usa el reloj de pared de la 4b. [dotnet] · `series_panels.dart:98`
- **AC-WKT-11-13 · Etiqueta de objetivo del paso.** *Entonces* es la regla de AC-RUT-04-01 a -05 (It. 1) aplicada al paso expandido, con el objetivo ya progresado y su `📈`: `Prueba 4a`, trabajo de la repetición 2 → `>160W · 24 spm · 📈 SPM +2/rep`; `Pirámide 500`, primer trabajo → `150-180W · 02:05/500m · 📈 W +10/rep, Split -2s/rep`; sin objetivos, vacía. El mismo texto en es y en. [dotnet] · `lib/core/models/interval_step.dart:79-109`
- **AC-WKT-11-14 · Resumen de una rutina.** *Entonces* es AC-RUT-05-02 (It. 1), del catálogo, en la tarjeta y en el diálogo de inicio: `Escalera SPM` y `Prueba 4a` → `1 serie · 1 descanso` / `1 interval · 1 rest`; `Solo calentamiento` → `1 paso` / `1 step` (el legacy, `1 series · 1 descansos` y `1 pasos`, iguales en los dos idiomas: Q-01, Q-02). [dotnet] · `routine.dart:100-105`
- **AC-WKT-11-15 · Resumen final.** *Entonces* la distancia como `{n} m` sin separador de miles (1484 → `1484 m`) y las calorías como número sin unidad (`89`). Lo usa el diálogo de rutina completada de la 4b. [dotnet] · `immersive_workout_screen.dart:986`, `:991`
- **AC-WKT-11-16 · Iguales en es y en (Q-06).** *Entonces* todos estos formatos son los mismos con la app en español y en inglés y no dependen de la configuración regional del sistema: punto decimal, miles con punto solo en los paneles y 24 h. [dotnet] · FEATURE_INVENTORY Q-06

### 4.10 F-WKT-12 — Workout en reposo

- **AC-WKT-12-01 · Pestaña y título.** *Entonces* Workout es el destino 1 de la barra (`navWorkout`, `Workout` en los dos idiomas, Q-08; íconos de AC-APP-02-01) y su barra superior dice `workoutTitle` (`Entrenamiento` / `Workout`), sin acciones. Deja de ser provisoria (AC-APP-02-08). [Editor-PM] · [Captura] CAP-01 · `workout_screen.dart:53`; `lib/main.dart:156-160`
- **AC-WKT-12-02 · Qué se ve y en qué orden.** *Entonces*, de arriba abajo y con 16 de margen: el aviso sin remo (AC-BLE-10-03), `Entrenamiento libre` (AC-WKT-12-03), 24, y la lista de rutinas con su etiqueta o su estado (AC-WKT-12-04, -06). En la 4a no están `workoutScene` ni el selector de escenario: llegan con los escenarios en la It. 8 (F-SCN-22; DV-34). [Editor-PM] · `workout_screen.dart:52-142`
- **AC-WKT-12-03 · Entrenamiento libre.** *Entonces* `workoutFree` (`Entrenamiento libre` / `Free workout`), con el ícono de play, es el botón primario del sistema visual, de ancho completo y 56 de alto (ART_BIBLE §3.6). Está habilitado solo con el remo conectado (AC-BLE-10-04). Un toque inicia la sesión libre (AC-WKT-01-02) y abre el inmersivo en el acto, sin confirmación. [Editor-PM] · [Captura] CAP-01 · `workout_screen.dart:85-94`
- **AC-WKT-12-04 · Lista de rutinas (DV-21).** *Dado* `F-LISTA` con el remo conectado, *entonces*, después de `workoutOrPickRoutine` (`␣␣O elegí una rutina:` / `␣␣Or pick a routine:`, con los dos espacios del legacy) en `text.tertiary` de 13, hay una tarjeta por rutina en el orden de la colección (`Escalera SPM`, `Pirámide 500`, `Solo calentamiento`, `Intervalos 4×`). Es la misma tarjeta de Rutinas (AC-RUT-10-03: ícono `fitness_center` en `#00B4D8`, nombre tal cual, descripción si tiene, resumen de AC-WKT-11-14 y mini-barra de AC-RUT-10-11) con un *chevron* de 18 en `text.tertiary` a la derecha en lugar del `⋮`; se toca entera. El legacy muestra solo el nombre y el resumen en 12, con el *chevron*. [Editor-PM] · [Captura] CAP-01, CAP-10 · `workout_screen.dart:98-127`
- **AC-WKT-12-05 · La colección compartida.** *Entonces* Workout usa la misma colección que Rutinas (AC-RUT-09): no vuelve a leer la base. *Dado* `F-LISTA`, *cuando* en Rutinas se crea `Nueva`, se renombra `Solo calentamiento` como `Solo entrada en calor` y se borra `Pirámide 500`, *entonces* al ir a Workout se ve `Nueva` primera, `Solo entrada en calor` en su lugar y ya no está `Pirámide 500`, sin indicador de carga. [Editor-PM] · `lib/features/routines/routines_provider.dart:27-52`
- **AC-WKT-12-06 · Cargando, error y sin rutinas (DV-23, DV-36).** *Entonces*, debajo de `Entrenamiento libre`: mientras la colección carga, el indicador de Rutinas (AC-RUT-10-02: aparece si la carga sigue a los 120 ms, con fundido); si la carga falló (`F-ROTA`, `Sprint`; Q-13), `routinesLoadError` (`No se pudieron cargar las rutinas`) y `retry` (`Reintentar`), que vuelve a cargar para Workout y Rutinas a la vez; sin rutinas, centrado en el espacio que queda, `workoutNoRoutines` (`Creá una rutina en la pestaña "Rutinas"` / `Create a routine in the "Routines" tab`) en un tono de texto de al menos 54 % (el legacy, al 38 %: D-ART-03). La etiqueta `workoutOrPickRoutine` está solo con rutinas. En ninguno de estos estados se deshabilita `Entrenamiento libre`. El legacy muestra "sin rutinas" mientras carga y no tiene estado de error. [Editor-PM] · [Captura] CAP-03, CAP-06, CAP-07 · `workout_screen.dart:129-138`
- **AC-WKT-12-07 · Diálogo de inicio.** *Cuando* se toca una tarjeta con el remo conectado y la rutina se puede iniciar, *entonces* se abre un diálogo con: el **nombre de la rutina** tal cual como título (`workoutStartTitle` sigue sin usarse: paridad); el texto `workoutStartContent` con su resumen (`1 serie · 1 descanso⏎⏎¿Iniciar el entrenamiento?` / `1 interval · 1 rest⏎⏎Start the workout?`; el `¿` es Q-02); y los botones `cancel` (`Cancelar` / `Cancel`, de texto) y `workoutStart` (`Iniciar` / `Start`, relleno primario). `Iniciar` inicia la rutina (AC-WKT-01-03) y abre el inmersivo; `Cancelar`, tocar afuera, Escape y "Atrás" lo cierran sin iniciar nada. [Editor-PM] · [Captura] CAP-08, DEMO-07 · `workout_screen.dart:145-167`
- **AC-WKT-12-08 · Una rutina que no se puede iniciar (R3-08, DV-28).** *Dado* `F-RARA` con `Serie enorme`, `Vatios imposibles` y `Serie ×0`, cada una en su base, con el remo conectado, *cuando* se toca su tarjeta, *entonces* no se abre el diálogo de inicio: aparece el aviso `workoutRoutineCannotStart` (`Esta rutina no se puede iniciar: revisá sus pasos en Rutinas` / `This routine can't be started: check its steps in Routines`), no se crea ninguna sesión (la base no cambia), no se abre el inmersivo, la memoria no crece y la app sigue respondiendo (después, `Entrenamiento libre` inicia normalmente). La regla: no se puede iniciar una rutina que el editor no dejaría guardar por los topes de Q-92 (la validación 3 de AC-RUT-11-05, con sus mismas excepciones) ni una que expandida no tiene ningún paso (AC-RUT-06-08). En la PC se decide en menos de 50 ms y sin expandir la rutina, también con ×2 000 000 000. En el legacy, `Serie enorme` traba el inicio (expande 2 000 millones de pasos), `Serie ×0` hace fallar el inmersivo (no hay paso 0) y `Vatios imposibles` arranca. `Sprint` no llega acá: hace fallar la carga (AC-WKT-12-06). [dotnet] · [Editor-PM] · [Captura] CAP-09 · `workout_provider.dart:122`, `:100`
- **AC-WKT-12-09 · Desplazamiento (DV-22).** *Entonces* todo el contenido de Workout (aviso, botón, etiqueta y lista) se desplaza junto, como el editor de la It. 3 (DV-18). *Dado* `F-LISTA` más 26 rutinas en L640, con letra 1,3× y el remo desconectado, *entonces* se llega a la última tarjeta, que queda por encima de la barra de pestañas y de la llave, y nada desborda; la posición se conserva al ir y volver de otra pestaña o de un entrenamiento. En los perfiles de teléfono no hay barra de desplazamiento fija (AC-APP-05-11). En el legacy solo se desplaza la lista y lo de arriba queda fijo. [Editor-PM] · [Captura] CAP-05 · `workout_screen.dart:102-126`

### 4.11 F-WKT-20 — Controles y confirmación de terminar

- **AC-WKT-20-01 · Controles.** *Entonces* abajo al centro, a `margen seguro inferior + 16` y con 24 de relleno a los lados (×escala), una fila con `workoutFinish` (`Terminar` / `Finish`), 16 de separación y `workoutPause` (`Pausa` / `Pause`, con el ícono de pausa) en marcha o `workoutResume` (`Reanudar` / `Resume`, con el ícono de play) en pausa. Con el aspecto de ART_BIBLE §3.5 (DV-31): `Terminar` con contorno de 1,5 y texto en `state.dangerText` `#FF7272` sobre el vidrio, mínimo 128 × 56; `Pausa` con contorno `border.strong` y texto blanco sobre el vidrio, mínimo 148 × 56; `Reanudar` relleno `#06D6A0` con texto `text.onAccent`, mínimo 148 × 56. El legacy: contorno `#FF5252` de 1,2 con fondo negro al 45 %, 120 × 48 y 140 × 48. [Editor-PM] · [Captura] CAP-12, CAP-14 · `immersive_workout_screen.dart:893-953`
- **AC-WKT-20-02 · Pausa y Reanudar, al instante.** *Cuando* se toca `Pausa` o `Reanudar`, *entonces* actúa en el acto, sin confirmación (AC-WKT-01-04, -05), y el botón cambia en el mismo cuadro. [Editor-PM] · `immersive_workout_screen.dart:923-948`
- **AC-WKT-20-03 · Confirmación de terminar.** *Cuando* se toca `Terminar` (o "Atrás" en Android), *entonces* se abre el diálogo `workoutFinishTitle` (`Terminar entrenamiento` / `Finish workout`) con `workoutFinishContent` (`Se guardará la sesión. ¿Continuar?` / `The session will be saved. Continue?`) y los botones `cancel` (`Cancelar` / `Cancel`, de texto) y `workoutFinish` (`Terminar` / `Finish`, relleno primario), con el aspecto de los diálogos de la app: opaco, el mismo que el de inicio (AC-WKT-12-07; el legacy usa un `AlertDialog` estándar, opaco). `Cancelar`, tocar afuera, Escape o "Atrás" lo cierran sin cambios; mientras está abierto, la sesión sigue en la fase en que estaba (en marcha, el reloj sigue), salvo que la rutina termine sola (AC-WKT-20-07). Los textos salen del catálogo, con el español idéntico al legacy, que los tiene fijos (Q-01). [Editor-PM] · [Captura] CAP-26 · `immersive_workout_screen.dart:125-140`
- **AC-WKT-20-04 · Confirmar.** *Cuando* se confirma `Terminar`, *entonces* la sesión termina (AC-WKT-01-06), se espera el guardado y se vuelve a Workout en reposo, **sin resumen** ni aviso de completado (Q-53); el paso en curso queda sin resultado (AC-WKT-09-07). No se intenta ninguna subida ni aparece nada de Strava: no está configurado hasta la It. 12, y con el simulador nunca (R-01). [Editor-PM] · `immersive_workout_screen.dart:141-147`
- **AC-WKT-20-05 · Desde la pausa.** *Entonces* se puede terminar con la sesión en pausa. [Editor-PM]
- **AC-WKT-20-06 · Sin dobles.** *Entonces*, desde que se confirma `Terminar` hasta que se cierra el inmersivo, los controles no responden, y dos toques seguidos en `Terminar` del diálogo cierran la sesión una sola vez (AC-APP-03-03). [Editor-PM]
- **AC-WKT-20-07 · La rutina termina con el diálogo de terminar abierto.** *Dado* una rutina con el diálogo de terminar abierto (por ejemplo, se tocó `Terminar` en los últimos segundos del enfriamiento), *cuando* el último paso se completa, *entonces*, en el cuadro en que la fase pasa a terminada, el diálogo se cierra sin efecto, como con `Cancelar` (con su salida y sin responder mientras sale, AC-APP-03-03), y sigue AC-WKT-03-05: el guardado, el cierre del inmersivo y el aviso `workoutCompleted`. Un toque en `Terminar` del diálogo que llega después de ese cuadro no cambia nada: la sesión se cierra una sola vez y cuenta como terminada sola. Si el toque llega antes (la fase todavía estaba en marcha), es un terminar a mano (AC-WKT-20-04): la rutina ya no termina sola y no hay aviso. Vale también si la rutina termina con la app en segundo plano (AC-WKT-02-06). Nunca queda un diálogo del inmersivo abierto sobre Workout en reposo. En el legacy, el diálogo de completado se apila encima del de terminar y, al cerrarlo, el de terminar se cierra sin efecto (`ok != true`). [Editor-PM] · `immersive_workout_screen.dart:49-65`, `:125-147`

### 4.12 F-WKT-23 — Pantalla clásica (descarte)

- **AC-WKT-23-01 · No se migra.** *Entonces* la pantalla clásica `_FullscreenWorkoutPage` (`workout_screen.dart:205-988`) no se migra: QA confirma que en el legacy ninguna navegación la abre (la clase solo se declara) y que la app no la necesita (Q-57; FEATURE_INVENTORY §6.1). Sus claves siguen en el catálogo (AC-APP-10-08): `workoutFinishTitle`, `workoutFinishContent`, `workoutPause` y `workoutResume` las usa ahora el HUD; `metricFreeLabel` y `metric*` no (las `metric*` son de Dispositivo). [QA]

### 4.13 F-BLE-10 — Señal "remo conectado" (se cierra en la It. 4a)

AC-BLE-10-01 y -02 (la primera pestaña y el bloqueo cruzado de búsquedas) se cumplieron en la It. 2.

- **AC-BLE-10-03 · Aviso sin remo.** *Dado* el remo sin conectar, es decir, en **cualquier** estado distinto de conectado (apagado —el Bluetooth del equipo apagado—, buscando, conectando o desconectado; paridad: el legacy mira solo `status == connected`, `lib/features/device/device_provider.dart:94`, sobre los cinco estados de `lib/core/bluetooth/ble_service.dart:8`), *entonces* arriba de Workout está el aviso: fondo `state.warningBg` (`#FF9800 @15 %`), radio 12, relleno 12 y 16 de margen abajo; el ícono de advertencia (`warning_amber`) de 18 en `#FF9800`, 8 de separación y el texto `workoutRowerNotConnected` (`Conectá el rower para iniciar un entrenamiento` / `Connect the rower to start a workout`) en `state.warning` `#FF9800`, de 13, que pasa a otra línea si no entra. Con el remo conectado, no está. Se prueba con los cuatro estados sin remo, también en AC-BLE-10-04 y -05. [dotnet] (la regla, con los cuatro estados) · [Editor-PM] (los cuatro estados) · [Captura] CAP-02 · `workout_screen.dart:50`, `:59-77`; `lib/features/device/device_provider.dart:94`
- **AC-BLE-10-04 · El inicio, deshabilitado.** *Dado* el remo sin conectar, *entonces* `Entrenamiento libre` no responde y se ve deshabilitado (al 38 %), y las tarjetas de rutina se ven al 50 % y no responden. [Editor-PM] · [Captura] CAP-02 · `workout_screen.dart:85-94`, `:107-120`
- **AC-BLE-10-05 · En vivo.** *Entonces* el aviso y la habilitación siguen al remo en vivo, sin salir de Workout: con el simulador, `Simular desconexión` muestra el aviso y deshabilita todo durante los 3 s y al volver se va el aviso y todo se habilita; `Desconectar` en Dispositivo lo deja así hasta `Buscar dispositivos` (AC-BLE-07-03). [Editor-PM] · [Captura] DEMO-14
- **AC-BLE-10-06 · Se mira solo al tocar.** *Dado* el diálogo de inicio abierto, *cuando* el remo se desconecta y se toca `Iniciar`, *entonces* la rutina se inicia igual (paridad: la conexión se mira al tocar la tarjeta); el HUD queda con la última lectura (AC-WKT-03-10). [Editor-PM] · `workout_screen.dart:118-119`, `:163-165`
- **AC-BLE-10-07 · El motor no mira la conexión.** *Entonces* el motor solo recibe las lecturas: no cambia de fase ni avisa nada cuando el remo se conecta o se desconecta (AC-WKT-03-10). [dotnet] · `workout_provider.dart:83`, `:220-224`

### 4.14 F-APP-14 — Pantalla siempre encendida

- **AC-APP-14-01 · Toda la app, desde el arranque (Q-56).** *Entonces* desde que la app arranca (antes de la primera pantalla) y mientras está abierta, en cualquier pestaña y no solo en un entrenamiento, la pantalla no se apaga ni el equipo se suspende por inactividad. *Dado* Windows con el apagado de pantalla del sistema en un tiempo menor que 3 min, *cuando* la app queda 3 min sin tocar en Dispositivo, en Rutinas, en Workout en reposo y en un entrenamiento, *entonces* la pantalla sigue encendida; `powercfg /requests` (en una consola de administrador) muestra el proceso `RowMate.exe` en `DISPLAY` y en `SYSTEM` (DT4a-09). Esa salida trae la ruta completa del ejecutable, con la carpeta del usuario: en lo versionado se anota solo `RowMate.exe` y la sección (§7.1; repositorio público). Termina DV-08 de la It. 2. QA no cambia la configuración de energía del usuario: usa la que tiene, y el usuario lo ve en la demo (e). [Device-Win] · `lib/main.dart:42`; `<pub-cache>/wakelock_plus-1.4.0/lib/src/wakelock_plus_windows_plugin.dart:23`
- **AC-APP-14-02 · Minimizada y al volver del segundo plano.** *Entonces* el pedido sigue al volver del segundo plano (Q-56). En Windows, también con la ventana minimizada (el legacy lo pide para todo el proceso, no para la ventana) y después de restaurarla. En Android vale mientras la app está adelante, también después de ir a otra app y volver. [Device-Win] · [Device-And] (NO VERIFICABLE sin módulo y teléfono) · `<pub-cache>/wakelock_plus-1.4.0/android/src/main/kotlin/dev/fluttercommunity/plus/wakelock/Wakelock.kt:24`
- **AC-APP-14-03 · Al cerrar, se libera.** *Cuando* la app se cierra, *entonces* el pedido se libera: `powercfg /requests` ya no la muestra y la pantalla vuelve a apagarse con el tiempo del sistema. [Device-Win]
- **AC-APP-14-04 · Sin avisos.** *Entonces*, si el sistema rechaza el pedido, la app no muestra nada (paridad) y lo anota en el registro de diagnóstico. En iOS y macOS se verifica en la 10b ([Apple], ROADMAP §4.1). [dotnet] · [QA] · `lib/main.dart:42`

### 4.15 También toca

**F-WKT-13 (estructura del inmersivo; la cierra la 4b).**

- **AC-WKT-13-01 · Se abre al iniciar.** *Cuando* se toca `Entrenamiento libre` o `Iniciar`, *entonces* el inmersivo se abre en el acto, a pantalla completa y por encima de toda la app, sin esperar a la base: la sesión y el reloj corren desde el toque (AC-WKT-01-02) y ninguna transición los demora. En la 4a las barras del sistema de Android siguen visibles: ocultarlas es de la 4b (F-WKT-13, F-APP-15; DV-34). [Editor-PM] · `workout_screen.dart:169-175`
- **AC-WKT-13-02 · Capas.** *Entonces*, de atrás hacia adelante: el fondo provisorio (AC-SCN-01-01); la barra superior, a `margen seguro superior + 8` (la de pasos con rutina, AC-WKT-14; la libre sin rutina, AC-WKT-16-01); el HUD (AC-WKT-17); y los controles (AC-WKT-20-01). Con el simulador, encima de todo, la cinta y la llave (AC-BLE-16-01, -02). [Editor-PM] · [Captura] CAP-12, CAP-18 · `immersive_workout_screen.dart:74-117`
- **AC-WKT-13-03 · "Atrás" y Escape.** *Entonces* "Atrás" de Android no cierra el inmersivo: abre la confirmación de terminar (AC-WKT-20-03). En la PC, Escape sin un diálogo abierto no hace nada (paridad: en el legacy Escape solo cierra lo que se cierra tocando afuera, AC-APP-03-02); con el diálogo abierto, lo cancela. [Editor-PM] · [Device-And] · `immersive_workout_screen.dart:67-71`
- **AC-WKT-13-04 · Se cierra solo.** *Entonces*, cuando la sesión vuelve a reposo (terminar a mano o fin de la rutina), el inmersivo se cierra solo y se ve Workout en reposo con la barra de pestañas. Nunca queda el inmersivo abierto en reposo, ni Workout en reposo con una sesión en curso, ni un diálogo del inmersivo abierto sobre Workout en reposo (AC-WKT-20-07). [Editor-PM] · `immersive_workout_screen.dart:57-65`
- **AC-WKT-13-05 · Sin el nombre de la rutina.** *Entonces* el inmersivo no muestra el nombre de la rutina (paridad). [Editor-PM]

**F-WKT-14 (barra de pasos: en la 4a, solo la fila de información; la cierra la 4b).**

- **AC-WKT-14-01 · Contenedor.** *Dado* una rutina, *entonces* la barra superior tiene 12 de margen a cada lado, el vidrio de AC-WKT-17-01 con borde `#FFFFFF @8 %` y radio 14, y una sola fila: la de información. La fila de píldoras y el reloj de pared llegan en la 4b (DV-34). No responde al toque. [Editor-PM] · [Captura] CAP-18 · `immersive_workout_screen.dart:348-354`, `:380-382`
- **AC-WKT-14-02 · Tipo del paso.** *Entonces* a la izquierda va el tipo del paso en curso, localizado (`stepTypeWarmup`, `stepTypeWork`, `stepTypeRest`, `stepTypeCooldown`), de 13/700, en el color de texto de su tipo (`step.*.hud`: trabajo `#FF5D7F`, descanso `#06D6A0`, calentamiento `#FFD166`, enfriamiento `#37A2CB`; el legacy usa el color base, como en DV-12 de la It. 3). [Editor-PM] · `immersive_workout_screen.dart:385-393`
- **AC-WKT-14-03 · Pastilla de objetivo (DV-35, DV-38).** *Entonces*, si el paso tiene etiqueta de objetivos (AC-WKT-11-13), a 6 del tipo va una pastilla con fondo del color base del tipo al 20 %, radio 6, relleno 6 × 2 y texto de 11/600 **en blanco** (`text.primary`; el legacy, en el color del tipo: DV-38), que se recorta con `…` si no entra y nunca empuja afuera la cuenta regresiva. El `📈` se dibuja como el ícono `trending_up` en `metric.distance`, como en el editor (DV-10). Sin objetivos, no hay pastilla. [Editor-PM] · [Captura] CAP-19, CAP-23 · `immersive_workout_screen.dart:394-415`
- **AC-WKT-14-04 · Próximo (DV-24, DV-38).** *Entonces*, cuando aplica "a continuación" (AC-WKT-04-05), a la derecha y a 8 de la cuenta va una pastilla con `hudUpNext` (`Próximo:` / `Up next:`) en blanco al 70 % (`text.secondary`), de 10, un espacio y el tipo del paso siguiente localizado, **en blanco**, de 10/700; fondo del color base del paso siguiente al **20 %**, borde del mismo color al 50 %, radio 8 y relleno 7 × 3. **El fondo y el borde** laten: su opacidad va de 0,6 a 1,0 en 600 ms y vuelve en 600 ms, sin parar, hasta el cambio de paso; el texto queda siempre entero. El legacy pone `Up next:` en blanco al 60 % y el tipo siguiente en su color, sobre un tinte al 25 %, y late la pastilla entera, texto incluido (DV-38). No muestra la duración del paso siguiente (paridad). El legacy dice `Up next: ` en los dos idiomas (DV-24). [Editor-PM] · [Captura] CAP-21, VID-01 · `immersive_workout_screen.dart:319-322`, `:418-450`
- **AC-WKT-14-05 · Cuenta regresiva.** *Entonces* a la derecha va la cuenta (AC-WKT-11-07, -08), blanca, de 20 (el legacy, 18/800). Por terminar (AC-WKT-04-04) se agranda a 22 (el legacy, 20) y su color va de blanco al color del paso y vuelve, al mismo ritmo de 600 + 600 ms. En un paso por distancia muestra el largo total, sin "por terminar" ni `Próximo` (Q-51). [Editor-PM] · [Captura] CAP-22, CAP-20 · `immersive_workout_screen.dart:451-471`
- **AC-WKT-14-06 · En pausa y al terminar.** *Entonces* en pausa la fila queda quieta, pero si el paso estaba por terminar o con `Próximo`, los pulsos siguen (paridad). Con la rutina terminada, hasta que se cierra el inmersivo, muestra el último paso con `0:00` (si es por distancia, su largo: AC-WKT-03-06), sin `Próximo` ni "por terminar". No hay "paso N de M" ni el nombre de la rutina. [Editor-PM] · `immersive_workout_screen.dart:333-340`

**F-WKT-16 (barra superior del entrenamiento libre; la cierra la 4b).**

- **AC-WKT-16-01 · Barra libre, funcional.** *Dado* un entrenamiento libre, *entonces* la barra superior (12 de margen a cada lado, relleno 14 × 8, el vidrio de AC-WKT-17-01 con borde `@8 %` y radio 14) muestra el ícono `rowing` de 20 en `#00B4D8`, 8 de separación y `hudFreeTitle` (`Entrenamiento Libre`, con la L mayúscula del legacy / `Free Workout`) de 16/600 en blanco. El reloj de pared y el chip de fase llegan en la 4b (F-WKT-16, F-WKT-19; DV-34). [Editor-PM] · [Captura] CAP-12 · `immersive_workout_screen.dart:491-522`

**F-WKT-17 (tarjetas y banda; la cierra la 4b).**

- **AC-WKT-17-01 · Vidrio y tamaños (QA-P-17, D-ART-02; DV-31).** *Entonces* todas las piezas de vidrio del HUD (tarjetas, banda, barra superior y controles) usan el respaldo sin desenfoque `glass.fallback` (`#070D16 @86 %`, ART_BIBLE §3.2) con el borde de 1 dp `#FFFFFF @10 %` (en la barra superior, `@8 %`) y el radio 14 del legacy (la banda, una cápsula); el legacy usa negro al 55 % en las tarjetas y la barra de pasos, al 50 % en la banda y la barra libre, y al 45 % en los controles. Tamaños de D-ART-02 (ART_BIBLE §3.2): valores de SPM 64, de Split y Watts 44, de kcal y BPM 32 y de la banda 34, con las etiquetas, unidades, anchos mínimos y rellenos de esa tabla, todo por la escala del HUD (§0.3). Si el usuario rechaza D-ART-02, los del legacy: 52, 36, 28 y 28, sin escala. El HUD no sigue la letra del sistema (ART_BIBLE §10.4; el legacy sí). [Editor-PM] · [Captura] CAP-12, CAP-15, CAP-16, CAP-17 · `immersive_workout_screen.dart:741-837`
- **AC-WKT-17-02 · SPM.** *Entonces* arriba a la izquierda (14 del borde, `margen seguro superior + 90`) va la tarjeta grande `hudSpm` (`SPM`) con el valor de AC-WKT-11-02, en `metric.spm.hud` `#3ACCF1` sin objetivo, con la etiqueta en `#00B4D8`. [Editor-PM] · `immersive_workout_screen.dart:611-636`
- **AC-WKT-17-03 · Split y Watts.** *Entonces* arriba a la derecha (14 del borde, `margen seguro superior + 90`), alineadas a la derecha y a 8 una de otra, van las medianas `hudSplit` (`Split /500m`), con el valor de AC-WKT-11-01 en `#FFAA47` y la etiqueta en `#FF9F1C`, y `hudWatts` (`Watts`), con el entero en `#FFD60A` y `hudUnitWatts` (`W`) al lado, en `metric.watts.hud` al 80 % (el legacy, en el color del valor al 55 %: DV-31). [Editor-PM] · `immersive_workout_screen.dart:639-663`, `:821-829`
- **AC-WKT-17-04 · Banda.** *Entonces* abajo a la izquierda (14 del borde) va la cápsula con: el ícono de cronómetro y el reloj activo (AC-WKT-11-06) en `metric.time.hud` `#E0E0E0`; un divisor vertical de 1 × 32 al 12 %; y el ícono de regla con la distancia del monitor (AC-WKT-11-04) en `#4ECBF2`, con `hudUnitMeters` (`m`) al lado, en `metric.distance.hud` al 80 % (DV-31). En vertical va a 140 del borde inferior (sin el margen seguro), con 20 de relleno a los lados y 16 a cada lado del divisor, como el legacy; en horizontal, a `margen seguro inferior + 16`, al nivel de los controles, y es **compacta**: 14 de relleno a los lados y 10 a cada lado del divisor (DV-33). [Editor-PM] · `immersive_workout_screen.dart:669-706`, `:839-887`
- **AC-WKT-17-05 · kcal y BPM.** *Entonces* abajo a la derecha, a 14 del borde: en vertical, en columna a 140 del borde inferior, alineadas a la derecha, con kcal arriba y BPM abajo, a 8 una de otra (paridad); en horizontal, **en fila** a la altura de los controles (`margen seguro inferior + 16`), con kcal en el rincón y BPM a su izquierda, a 8 una de otra (DV-33). Son la chica `hudKcal` (`kcal`) en `#06D6A0`, siempre, también en 0; y, solo si el pulso fusionado (AC-WKT-05) es mayor que 0, la chica `hudBpm` (`BPM`) en `metric.hr.hud` `#FF5D7F`, con el corazón `favorite` del tamaño de la etiqueta. Con pulso 0 (sin sensor y sin pulso del monitor, o un 0 del sensor), la tarjeta BPM no está. [Editor-PM] · [Captura] CAP-12, CAP-13, CAP-15 · `immersive_workout_screen.dart:709-733`
- **AC-WKT-17-06 · Valores en vivo.** *Entonces* los valores cambian con cada lectura del monitor y con cada bpm, también en pausa (en pausa solo se congela el reloj), y son los totales del monitor: la distancia y las kcal no vuelven a 0 al iniciar (Q-15). Con el simulador, la distancia sigue la que trae desde que se abrió la app (AC-BLE-15-09). Al iniciar se ven los últimos valores del monitor hasta la lectura siguiente (AC-WKT-01-07). El HUD no tiene otras interacciones que los controles. [Editor-PM] · `immersive_workout_screen.dart:99-108`
- **AC-WKT-17-07 · Orientación.** *Entonces* el HUD está en horizontal cuando el ancho es mayor que el alto; al rotar o cambiar el tamaño de la ventana se reacomoda sin reiniciar la sesión ni los valores. [Editor-PM] · `immersive_workout_screen.dart:605-606`
- **AC-WKT-17-08 · Nada se encima (DV-33).** *Entonces*, en TV, TH, L640 y W1280, en libre y con rutina, con pulsómetro y con los valores largos de CAP-16, ninguna pieza del HUD (barra superior, tarjetas, banda y controles) se encima con otra ni sale de la pantalla; la llave no tapa ninguna y la cinta solo el comienzo de la barra superior (AC-BLE-16-01). Se conservan las zonas y el orden del legacy (ART_BIBLE §5.2). En el legacy, en un teléfono horizontal (914 × 411 y 640 × 360) la tarjeta kcal se encima con la de Watts (calculado sobre `immersive_workout_screen.dart:639-663`, `:709-733`). [Editor-PM] · [Captura] CAP-15, CAP-16, CAP-23, CAP-24
- **AC-WKT-17-09 · La zona del bote.** *Entonces* ninguna pieza del HUD entra en la zona protegida del bote (un rectángulo centrado de 44 % del ancho por 30 % del alto, ART_BIBLE §5.2), con la posición vertical que fije el Designer para el fondo provisorio y que la It. 6a mantiene. QA lo verifica con el contorno de depuración. [Captura] CAP-16 · ART_BIBLE §5.2
- **AC-WKT-17-10 · Valores extremos.** *Dado* el remo de prueba con los extremos que puede mandar un monitor FTMS (Q-38): SPM `127.5`, Watts `-32768`, Split `1092:15`, distancia `16777215`, kcal `65535`, pulso `255` y el reloj en `9:59:59`, *entonces* el HUD no se cuelga, ningún valor se corta ni sale de su tarjeta (lo que no entra se achica, como D-10 de la It. 2) y nada se encima. [Editor-PM]

**F-HIS-17 (base local; se cerró en la It. 3).**

- **AC-HIS-17-07 · Sesiones y puntos en la base v8.** *Entonces* las tablas `workout_sessions` y `data_points` de la base v8 (AC-HIS-17-02) se llenan con las columnas y los tipos del legacy: una fila de sesión por entrenamiento, con `origin` = `app`, y sus puntos con su `session_id`. El esquema no cambia: sigue en v8. QA lo comprueba con una herramienta de SQLite sobre una base de prueba y sobre la del simulador después de la demo. [dotnet] · [QA] · `lib/core/database/database_service.dart:106-138`, `:216-229`, `:254-261`

**F-HIS-19 (consultas; se cierra en la It. 9).**

- **AC-HIS-19-01 · Sesión por id.** *Entonces* leer una sesión por su id devuelve la sesión, con las fechas en hora local (AC-RUT-16-04), o "no existe". [dotnet] · `database_service.dart:241-246`
- **AC-HIS-19-02 · Puntos de una sesión.** *Entonces* los puntos de una sesión salen ordenados por el tiempo del monitor, de menor a mayor; entre dos con el mismo tiempo (el punto final repetido), el orden no importa. Las usan las pruebas de la 4a y la herramienta de la demo; el Historial (It. 9) las vuelve a verificar. [dotnet] · `database_service.dart:263-270`

**F-SCN-01 (escena; se cierra en la It. 6b).**

- **AC-SCN-01-01 · Fondo provisorio.** *Entonces*, hasta que llega la escena (It. 6a), el fondo del inmersivo es provisorio: quieto, oscuro y parejo (lo fija el Designer, por ejemplo con los tonos de cielo y agua del legacy al mediodía, sin dibujos), sin animación ni movimiento con los datos, y sin simular agua, bote ni orilla. El HUD se diseña contra el peor fondo (ART_BIBLE §3.2), así que se va a ver igual sobre la escena. [Editor-PM] · [Captura] CAP-12 · DV-34

**F-APP-03 (navegación secundaria; se cerró en la It. 3).**

- **AC-APP-03-09 · El inmersivo se apila.** *Entonces* el inmersivo se apila sobre la app como el editor (AC-APP-03-01): la barra de pestañas no se ve ni se puede tocar, no hay flecha de volver ni lo cierra Escape (AC-WKT-13-03), se cierra solo al volver a reposo (AC-WKT-13-04) y lo que se está cerrando no responde. Entra y sale sin saltos, con la transición que fije el Designer (la entrada "sumergirse" es de la 4b); con "reducir movimiento", un fundido. [Editor-PM] · [Captura] VID-03
- **AC-APP-03-10 · Los diálogos de Workout y del inmersivo.** *Entonces* el diálogo de inicio y el de terminar se comportan como los de la It. 3 (AC-APP-03-03): modales, centrados sobre el velo, tocar afuera es cancelar, Tab recorre solo el diálogo y el que se está cerrando no responde. La cinta y la llave quedan por encima del velo. [Editor-PM] · [Captura] CAP-08, CAP-26
- **AC-APP-03-11 · Avisos.** *Entonces* `workoutRoutineCannotStart`, `workoutSaveError` y el `workoutCompleted` de AC-WKT-03-05 son avisos como los de la It. 3 (AC-APP-03-05): abajo, 4 s, el nuevo reemplaza al visible, se leen enteros y no los tapa la llave; en el inmersivo no tapan los controles. [Editor-PM] · [Captura] CAP-09, CAP-28, CAP-29

**F-APP-10 (catálogo; se cerró en la It. 1).**

- **AC-APP-10-10 · 12 claves nuevas.** *Entonces* el catálogo pasa de 190 a **202** claves en es y en, con las 12 de §11.2 (textos y parámetros exactos); el control de la cantidad pasa a 202 y ninguna clave queda en un solo idioma. Los textos del HUD que el legacy tiene fijos (`SPM`, `Split /500m`, `Watts`, `W`, `kcal`, `BPM`, `m`, `Entrenamiento Libre`, `Up next: `, los de los controles y del diálogo de terminar) salen del catálogo (Q-01). [dotnet] · FEATURE_INVENTORY Q-01; app-shell-platform F-APP-11

---

## 5. Criterios de iteraciones anteriores que se verifican acá

Si fallan, se reabren en esta iteración. AC-BLE-16-02, AC-BLE-16-03, AC-RUT-10-04 y AC-RUT-10-09 además **cambian** por D-ART-10 (DV-39, que reemplaza a DV-13; QA-V3-09): desde esta iteración valen los textos de abajo, que reemplazan a los de la [It. 2](../02-dispositivo-simulador/01-po-spec.md) (§4.10) y la [It. 3](../03-rutinas/01-po-spec.md) (§4.3 y §5).

- **AC-BLE-16-01 · Cinta SIMULADOR sobre el entrenamiento (se completa).** *Entonces*, con el simulador, la cinta queda por encima del inmersivo, de sus diálogos y de sus avisos; tapa en parte el comienzo de la barra superior, como en el legacy (`docs/image-03.png`), sin tapar la cuenta regresiva y sin recibir toques. Con esto AC-BLE-16-01 queda completo. [Editor-PM] · [Captura] CAP-12, CAP-26 · `lib/main.dart:108-110`
- **AC-BLE-16-02 · La llave (cambia por D-ART-10, DV-39; QA-V3-09).** *Entonces*, con el panel cerrado (como arranca siempre), hay un botón flotante chico de 40 × 40 dp con el ícono `build` (llave):
  - **Fuera del entrenamiento** (las pestañas y las pantallas apiladas), abajo a la izquierda: a 16 dp del borde izquierdo y 16 dp por encima de la barra de pestañas o, con una pantalla apilada que tapa la barra (el editor), a 16 dp del borde de abajo, más los márgenes seguros. Con un diálogo o un menú abierto no se mueve y queda por encima del velo (AC-APP-03-07, AC-APP-03-10).
  - **En el entrenamiento a pantalla completa**: en vertical, a 16 dp del borde derecho y 8 dp por encima del lugar de las tarjetas kcal y BPM (el que ocupan con BPM, así no se mueve si el pulso aparece o se va); en horizontal, 8 dp a la derecha de `Pausa`, centrada en su alto, o, si a la derecha no queda lugar para kcal y BPM (pasa en L640), 8 dp por encima de `Pausa`, alineada con su borde derecho. El lugar se decide por ventana: no cambia con los valores, con BPM ni con el paso.

  En reposo (con la pantalla quieta, en cualquier posición de desplazamiento) no tapa ninguna acción: ni el `+`, ni el `⋮`, el lápiz, la cruz o el tacho, ni `+ Paso` o `Serie`, ni el *chevron* de una tarjeta o de una fila, ni los botones de los diálogos, ni los controles del entrenamiento; en el entrenamiento tampoco tapa ninguna pieza del HUD, en TV, TH, L640 y W1280, en libre y con rutina. "Tapar" es que la llave, agrandada 8 dp, se cruce con el área táctil de un control (00-deuda-diseno §1.2). Los menús emergentes no se abren debajo de ella. Pasa de un lugar al otro sin saltos: con un desplazamiento vertical corto entre una pestaña y una pantalla apilada, y con un fundido al entrar y al salir del entrenamiento (con "reducir movimiento", en el acto o solo con el fundido); se puede tocar mientras se mueve. Tocarla abre el panel en su lugar, creciendo desde la llave (AC-BLE-16-03); mientras el panel está abierto, la llave no se ve. Si la llave cambia de lugar con el panel abierto (se abre o se cierra una pantalla apilada, empieza o termina un entrenamiento, cambia la ventana), el panel sigue abierto y pasa al lugar nuevo sin saltos, como en el legacy, donde queda abierto por encima de todas las pantallas: nunca queda en el lugar de una pantalla que ya se cerró. Con el panel se cambian Watts, Ritmo, `Remando` y `Simular desconexión` durante la sesión, y el HUD lo muestra desde la lectura siguiente. Reemplaza el texto de la It. 3 (DV-13). [dotnet] (la regla) · [Editor-PM] (con `-Simulator`) · [Captura] CAP-01, CAP-12, CAP-15, CAP-16, CAP-17, DEMO-03 y las tomas de las It. 2 y 3 de abajo; en toda toma con simulador, la auditoría de la llave · `lib/core/dev/simulator_overlay.dart:28-50`; 00-deuda-diseno §1; 04-design-notes §3.9
- **AC-BLE-16-03 · El panel (cambia por D-ART-10, DV-39).** *Entonces* el panel mide **280 dp** de ancho fijo y se abre desde la llave, creciendo desde su esquina: hacia arriba y a la derecha fuera del entrenamiento, hacia arriba y a la izquierda en el entrenamiento, con la transición de la It. 2 (solo cambia el origen). Nunca pasa del borde de arriba (deja libres el margen seguro superior y al menos 16 dp) y su alto máximo tiene un mínimo de 120 dp: en las pestañas es el de la It. 2 (sin margen seguro: ventana de 360 dp → 248; de 914 → 802; de 200 → 120) y, con una pantalla apilada que tapa la barra (el editor), 64 dp más (360 → 312; 914 → 866; 200 → 152). En el entrenamiento, **Split y Watts quedan a la vista** con el panel abierto, para ver el efecto de cambiarlos (demo b, AC-WKT-10-09): en vertical el panel termina 8 dp debajo del lugar de la tarjeta de Watts con su objetivo (así no cambia de alto al cambiar de paso) y, en horizontal, no llega a ellas. En el legacy, en un teléfono vertical, el panel tampoco las tapa. Si el contenido no entra, se desplaza adentro del panel, y el botón de cerrar queda siempre visible. En Rutinas, el panel abierto deja el `+` libre desde 368 dp de ancho (en un teléfono de 360 dp le tapa 8 dp de la izquierda). [dotnet] (la regla) · [Editor-PM] (con `-Simulator`) · [Captura] DEMO-03 · `simulator_overlay.dart:40-49`, `:83-95`; 00-deuda-diseno §1.7; 04-design-notes §3.9
- **AC-APP-02-08 · Contenido provisorio (cambia).** *Entonces* Workout deja de ser provisoria (esta spec); Historial (It. 9) sigue con su título y su ícono apagado. [Editor-PM] · [Captura] CAP-01
- **AC-APP-06-01 · Colores, en el HUD.** *Entonces*, sin objetivo, cada valor del HUD va en la variante `.hud` de su métrica (Split `#FFAA47`, SPM `#3ACCF1`, Watts `#FFD60A`, distancia `#4ECBF2`, tiempo `#E0E0E0`, kcal `#06D6A0`, pulso `#FF5D7F`) y cada etiqueta en el color base (pulso en `#FF5D7F`); con objetivo, blanco o `#FF625B` (AC-WKT-10-05). Medido en la captura sobre el trazo del valor: el hex exacto, ±2 por canal. Mismo matiz que el legacy, más claro (R-10, D-ART-01; DV-31); el legacy pone las etiquetas al 80 %. [Captura] CAP-12, CAP-13, CAP-19
- **AC-APP-05-05 · Letra del sistema 1,3×, en las pantallas nuevas.** *Entonces* Workout en reposo y los diálogos de inicio y de terminar crecen hasta 1,3× sin cortes ni desbordes, y lo que no entra se desplaza. El HUD no cambia con la letra del sistema (AC-WKT-17-01). [Editor-PM] · [Captura] CAP-05, CAP-11
- **AC-APP-05-07 · Teclado y foco, en las pantallas nuevas.** *Entonces* Tab recorre `Entrenamiento libre`, las tarjetas de rutina, `Reintentar`, los botones de los diálogos, `Terminar` y `Pausa` / `Reanudar`, con el anillo de foco; en el inmersivo, `Terminar` → `Pausa` / `Reanudar` → la llave (con el simulador) → `Terminar`. Enter o Espacio activan el control enfocado; Escape cancela el diálogo abierto. Al cerrarse un diálogo, el foco vuelve al control que lo abrió **solo si se venía usando el teclado**; si se lo abrió con el mouse o con un toque, no queda ningún control enfocado, así un Enter después de `Cancelar` no vuelve a abrir el diálogo de terminar (paridad con el legacy en la PC, donde un clic no enfoca el botón; DT4a-01). [Editor-PM] · [Device-Win]
- **AC-APP-05-08 · Reducir movimiento, en las pantallas nuevas.** *Entonces* el rodillo de dígitos pasa a un fundido de 80 ms; los pulsos de `Próximo` y de la cuenta regresiva quedan como un realce fijo; el corazón de BPM no late; fuera de objetivo no hay empujón (quedan el color y la flecha); el inmersivo entra y sale con un fundido. [Editor-PM]
- **AC-RUT-06-08 · Una rutina sin pasos al expandir.** "El motor de la It. 4a no puede arrancarla": ahora no se inicia y se avisa (AC-WKT-12-08). [dotnet] · [Editor-PM]
- **AC-RUT-10-04 · Botón `+` (cambia por D-ART-10, DV-39).** *Cuando* se toca `+`, *entonces* se abre el editor en modo nueva rutina (AC-RUT-11-01). *Dado* el modo simulador, *entonces* el `+` se ve entero y se toca en toda su superficie, con el panel cerrado y también abierto (desde 368 dp de ancho): la llave y el panel están del otro lado, abajo a la izquierda (AC-BLE-16-02, -03). En el legacy la llave tapa la mitad del `+` y el panel abierto, el `+` entero (`routines_screen.dart:17-23`; `simulator_overlay.dart:40-49`). Reemplaza el texto de la It. 3: sale "la llave queda 16 dp por encima del `+`" (DV-13). [Editor-PM] (con `-Simulator`) · [Captura] CAP-01 de la It. 3
- **AC-RUT-10-09 · Desplazamiento y estado (cambia por D-ART-10, DV-39).** *Dado* 30 rutinas, *entonces* la lista se desplaza, la última tarjeta queda por encima del `+` al final del desplazamiento (con el simulador, también por encima de la llave, que está abajo a la izquierda: AC-BLE-16-02), y al abrir y cerrar el editor o al cambiar de pestaña la lista vuelve a la misma posición. En los perfiles de teléfono no hay barra de desplazamiento fija (AC-APP-05-11). Solo cambia dónde está la llave: sale "que en Rutinas está sobre el `+`". [Editor-PM] · [Captura] CAP-06 de la It. 3
- **AC-RUT-10-11 · La mini-barra, en Workout.** *Entonces* cada tarjeta de Workout lleva la misma mini-barra que en Rutinas (`Prueba 4a` → 6 tramos: amarillo, rosa, verde, rosa, verde, azul); con `F-RARA` la lista carga igual; sin remo va al 50 % con el resto de la tarjeta. [Editor-PM] · [Captura] CAP-01, CAP-02, DEMO-01
- **AC-HIS-17-02 · Esquema v8.** *Entonces* las tablas de sesiones y puntos dejan de estar vacías (AC-HIS-17-07) y el esquema sigue igual. [dotnet] · [QA]
- **AC-HIS-20-04 · Las pruebas no tocan las bases del usuario.** *Entonces* vale también para las pruebas, las capturas y los videos de la 4a, sin excepciones, y para la herramienta de la demo, que solo lee: QA compara el contenido y la fecha de `rower_app.db` y `rower_app_dev.db` antes y después de todo. [QA]
- **AC-APP-01-09 · Las rutinas, cargadas al arrancar.** *Entonces*, al ir a Workout después de la carga del arranque (sin haber visitado Rutinas), la lista está, sin indicador. [Editor-PM]

**Tomas de las It. 2 y 3 que cambian por D-ART-10.** Se regeneran con la base nueva de WP-B0 ([02-tech-design §7.4](02-tech-design.md)) y en todas corre la auditoría de la llave. Cambia solo lo que sigue de "Qué tiene que verse"; lo demás queda igual:

| Toma | Ahora |
|---|---|
| It. 2 CAP-01 | El botón de la llave abajo a la izquierda, 16 dp sobre la barra |
| It. 2 CAP-16 | El panel de 280 dp anclado abajo a la izquierda, crecido desde la llave |
| It. 3 CAP-01 | El `+` abajo a la derecha, entero y solo en su rincón; la llave abajo a la izquierda, a 16 dp del borde y 16 dp sobre la barra |
| It. 3 CAP-06 | La última tarjeta entera, por encima del `+` y de la llave; la llave no tapa el `+` |
| It. 3 CAP-09 | La llave abajo a la izquierda, a 16 dp del borde de abajo (la barra no se ve) |
| It. 3 CAP-18, CAP-19, CAP-28 | El aviso entero, 8 dp por encima de la llave |

---

## 6. Casos de test legacy

Los 11 de ROADMAP §5 para la 4a. Se portan uno a uno, con el mismo nombre y las mismas afirmaciones.

| LT | Archivo:línea | Caso (nombre exacto) | Qué afirma | Dónde | AC |
|---|---|---|---|---|---|
| LT-35 | `test/workout_provider_hr_test.dart:80` | `el pulsómetro manda sobre el FTMS y se actualiza sin paquetes del remo` | FTMS 100 W / 90 → 90; bpm 142 → 142 con 100 W y 2 avisos; FTMS 120 W / 90 → 142 y 120 W; bpm 0 → 0; bpm 130 + `disconnected` → 0; FTMS 90 → 90 | [dotnet] | AC-WKT-05-01 |
| LT-36 | `test/workout_provider_hr_test.dart:125` | `orden real de caída: bpm 0 y después disconnected` | FTMS 90 + sensor 140 → 140; 0 + `disconnected` → 0; FTMS 90 → 90 | [dotnet] | AC-WKT-05-02 |
| LT-37 | `test/workout_provider_hr_test.dart:148` | `un bpm recibido durante connecting se conserva al pasar a connected` | `connecting`, 120, `connected` → 120; FTMS 90 → 120 | [dotnet] | AC-WKT-05-03 |
| LT-38 | `test/workout_provider_hr_test.dart:167` | `connected del sensor sin bpm previo no toca los datos` | `connected` + `disconnected` sin bpm → 0 avisos | [dotnet] | AC-WKT-05-04 |
| LT-39 | `test/step_target_tracker_test.dart:20` | `cumplido cuando el promedio de vatios alcanza el mínimo` | 140 y 160 W contra 150 → cumplido | [dotnet] | AC-WKT-09-01 |
| LT-40 | `test/step_target_tracker_test.dart:28` | `no cumplido si falla cualquiera de los objetivos definidos` | 200 W / 22 spm contra 150 W / 26 spm → no cumplido | [dotnet] | AC-WKT-09-02 |
| LT-41 | `test/step_target_tracker_test.dart:35` | `el split se cumple por debajo del objetivo (más rápido)` | 125 contra 130 → cumplido; 140 contra 130 → no cumplido | [dotnet] | AC-WKT-09-03 |
| LT-42 | `test/step_target_tracker_test.dart:46` | `un paso sin objetivos no tiene resultado` | sin objetivos → sin resultado | [dotnet] | AC-WKT-09-04 |
| LT-43 | `test/step_target_tracker_test.dart:53` | `sin lecturas remando el objetivo no se cumple; los ceros no cuentan` | sin lecturas → no cumplido; parado + 120 W / 24 / split 0 contra 100 W / split 200 → cumplido | [dotnet] | AC-WKT-09-05 |
| LT-44 | `test/step_target_tracker_test.dart:65` | `un paso no completado no tiene resultado y reset borra todo` | sin completar → nada; completado → cumplido; reinicio → nada | [dotnet] | AC-WKT-09-06 |
| LT-45 | `test/series_panels_test.dart:32` | `formatos` | `formatLapTime(166.667)` = `2:46.7`; `formatLapTime(65)` = `1:05.0`; `formatSplit(114.4)` = `1:54`; `formatMeters(1245)` = `1.245 m` | [dotnet] | AC-WKT-11-09, -10, -11 |

Los criterios nuevos del ROADMAP para la 4a quedan así:

| Criterio nuevo (ROADMAP §2, It. 4a) | AC |
|---|---|
| Fases y reloj (F-WKT-01/02) | AC-WKT-01-01 a -11, AC-WKT-02-01 a -04 |
| Avance y fin con progresiones; al terminar, se guarda y se vuelve a reposo | AC-WKT-03-01 a -10 |
| "Por terminar" y "a continuación" ≤ 15 s | AC-WKT-04-04 a -06, AC-WKT-14-04, -05 |
| Telemetría cada 5 s + último punto + P99, guardada en forma incremental (Q-12) | AC-WKT-06-01 a -15 |
| Semáforo con los colores de §6 (D-ART-01) | AC-WKT-10-01 a -09 |
| Formatos 1-15 de F-WKT-11 | AC-WKT-11-01 a -16 |
| Segundo plano según Q-91 (QA-P-06) | AC-WKT-02-04 a -10, AC-WKT-06-15, AC-APP-14-02 |
| Rutina fuera de los topes de Q-92 (R3-08) | AC-WKT-12-08 |

---

## 7. Capturas, demo y videos

### 7.1 Reglas

Las de las It. 2 y 3: las renderiza Unity con la UI real; van a `docs/migration/iterations/04a-motor-hud-funcional/snapshots/` como `CAP-nn-<tema>.png`, `DEMO-nn-<tema>.png` y `VID-nn-<tema>.mp4`, con `manifest.json` e `INDEX.md` sin rutas de la PC (el repositorio es público). Salvo que se diga otra cosa: español, modo simulador sin ruido, reloj de prueba, el pulsómetro simulado sin conectar y la base indicada (§0.4). Las tomas con pulsos (`Próximo`, la cuenta por terminar) se hacen a una fase fija del pulso, la que fije el arnés. Ninguna escribe en las bases del usuario (AC-HIS-20-04). QA verifica "Qué tiene que verse" y el Designer el aspecto. En toda toma con simulador corre la auditoría de la llave (AC-BLE-16-02): si la llave tapa un control, la toma falla. Los builds de desarrollo se lanzan solo desde su carpeta de salida por defecto (QA-V3-08). La evidencia de la pantalla encendida (AC-APP-14-01, -03) se anota en lo versionado solo con `RowMate.exe` y la sección de `powercfg /requests` (`DISPLAY`, `SYSTEM`): esa salida trae la ruta completa del ejecutable, con la carpeta del usuario, y no se copia.

### 7.2 Tamaños

Los de las It. 2 y 3: **TV** 411 × 914 dp, **TH** 914 × 411 dp, **L640** 640 × 360 dp, **W1280** 1280 × 720 dp. Escala del HUD: 1,054, 1,054, 0,92 y 1,6 (§0.3).

### 7.3 Lista

| ID | Tamaño | Estado (cómo se llega) | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | TV | Workout con `F-LISTA` y el remo simulado conectado | Barra `Entrenamiento`; `Entrenamiento libre` habilitado, a todo el ancho; `␣␣O elegí una rutina:`; las 4 tarjetas en orden (`Escalera SPM` con mini-barra de 8, `Pirámide 500` con su descripción y 14 tramos, `Solo calentamiento` con 1, `Intervalos 4×` con 7), cada una con su ícono, nombre, resumen y *chevron*; sin aviso; sin escenario (DV-34); la cinta y la llave (abajo a la izquierda, sin tapar ningún *chevron*: AC-BLE-16-02); Workout elegida en la barra |
| **CAP-02** | TV | Igual, con el remo desconectado (`Desconectar` en Dispositivo) | El aviso naranja arriba, con su ícono y `Conectá el rower para iniciar un entrenamiento`; `Entrenamiento libre` deshabilitado; las tarjetas al 50 % |
| **CAP-03** | TV | `F-VACIA` | Sin etiqueta ni tarjetas; centrado debajo del botón, `Creá una rutina en la pestaña "Rutinas"`, legible |
| **CAP-04** | TV | CAP-01 en inglés | `Workout`, `Free workout`, `␣␣Or pick a routine:`, `1 interval · 1 rest`, `2 intervals · 1 rest`, `1 step`, `4 intervals · 3 rests`; los nombres y la descripción, como se escribieron |
| **CAP-05** | L640 | `F-LISTA` más 26 rutinas, letra 1,3×, remo desconectado, desplazada al final | La última tarjeta entera, por encima de la barra y de la llave; nada desborda |
| **CAP-06** | TV | Base lenta, mientras cargan las rutinas | `Entrenamiento libre` y, debajo, solo el indicador circular; nunca `Creá una rutina…` |
| **CAP-07** | TV | `F-ROTA` | Debajo del botón libre (habilitado): `No se pudieron cargar las rutinas` y `Reintentar` |
| **CAP-08** | TV | `F-LISTA`, tocar `Escalera SPM` | Velo; diálogo `Escalera SPM` con `1 serie · 1 descanso`, una línea en blanco y `¿Iniciar el entrenamiento?`; `Cancelar` de texto e `Iniciar` relleno; la cinta y la llave por encima del velo |
| **CAP-09** | TV | `F-RARA`, tocar `Serie enorme` | Sin diálogo; abajo el aviso `Esta rutina no se puede iniciar: revisá sus pasos en Rutinas`, entero, sin que lo tape la llave |
| **CAP-10** | W1280 | CAP-01 en la ventana | Lo mismo a lo ancho, comparable con `docs/image-02.png` (sin el escenario: DV-34) |
| **CAP-11** | TV | `F-LISTA` con letra 1,3× | Textos más grandes; nada cortado ni fuera de su tarjeta |
| **CAP-12** | TV | Libre en marcha a 150 W y 24 spm, sin pulsómetro, a los 42 s | Barra `Entrenamiento Libre` con el ícono de remo; SPM `24.0` grande arriba a la izquierda; `Split /500m` `02:13` y `Watts` `150 W` arriba a la derecha; la banda `00:42` │ distancia `m` abajo a la izquierda; `kcal` abajo a la derecha, sin BPM; `Terminar` y `Pausa` abajo al centro; colores `.hud` (AC-APP-06-01); vidrio `#070D16 @86 %`; el fondo provisorio; la cinta sobre el comienzo de la barra y la llave sobre kcal, sin tapar nada (AC-BLE-16-02) |
| **CAP-13** | TV | Como CAP-12, con el pulsómetro simulado conectado | Además, debajo de kcal, `BPM` con el corazón y el pulso, en rosa |
| **CAP-14** | TV | Como CAP-13, en pausa | `Reanudar` verde en lugar de `Pausa`; el reloj igual que al pausar; los valores en vivo |
| **CAP-15** | TH | Como CAP-13, en horizontal | La fila de abajo: la banda compacta a la izquierda, los controles al centro y, a la derecha, BPM y kcal en fila con kcal en el rincón, sin tocar Watts (DV-33); la llave a la derecha de `Pausa`; nada se encima |
| **CAP-16** | L640 | Remo de prueba con valores largos: reloj `1:02:05`, distancia `123456`, Watts `-999`, Split `--:--`, kcal `9999`, BPM `188`, SPM `40.5`; contorno de depuración de la zona del bote | Nada se encima, nada se corta ni sale de la pantalla; la zona del bote libre; la llave sobre `Pausa`, sin tapar nada (AC-BLE-16-02) |
| **CAP-17** | W1280 | Como CAP-13, en la ventana | Las mismas zonas que `docs/image-03.png` (sin la escena), a escala 1,6; abajo a la derecha, BPM y kcal en fila (DV-33) y la llave a la derecha de `Pausa` |
| **CAP-18** | TV | `Prueba 4a`, calentamiento, a 24 s del final | Barra con `Calentamiento` en amarillo y la cuenta `0:24`; sin pastilla ni `Próximo`; las tarjetas sin semáforo |
| **CAP-19** | TV | `Prueba 4a`, trabajo de la repetición 1, a 40 s del final, a 150 W y 24 spm | `Trabajo` en rosa; la pastilla `>160W · 22 spm · ` + ícono + `SPM +2/rep`, con el texto en blanco sobre el tinte rosa (DV-38); Watts `150` en `#FF625B` con `▲` y `obj. 160`, etiqueta y borde amarillos con halo; SPM `24.0` blanco con `obj. 22`, etiqueta y borde cian; Split sin semáforo |
| **CAP-20** | TV | `Pirámide 500`, primer trabajo (500 m), preset `Suave` | La cuenta `500m`; la pastilla `150-180W · 02:05/500m · ` + ícono + `W +10/rep, Split -2s/rep` (recortada con `…` si no entra); Watts fuera con `▲` y `obj. 150`; Split fuera con `▼` y `obj. 02:05`; SPM sin semáforo |
| **CAP-21** | TV | `Prueba 4a`, trabajo de la repetición 1, a 12 s del final | `Próximo: Descanso` entero antes de la cuenta `0:12`: el texto en blanco (`Próximo:` al 70 %) sobre el tinte y el borde verdes (DV-38) |
| **CAP-22** | TV | Igual, a 3 s del final | La cuenta `0:03` más grande y teñida; `Próximo: Descanso` |
| **CAP-23** | TH | Como CAP-19, en horizontal | Nada se encima; la pastilla cabe o se recorta con `…` |
| **CAP-24** | L640 | `Prueba 4a`, trabajo de la repetición 2, a 23 spm y 150 W | SPM fuera (`▲`, `obj. 24`) y Watts fuera (`▲`, `obj. 160`); nada se encima |
| **CAP-25** | W1280 | Como CAP-19, en la ventana | Lo mismo, a escala 1,6 |
| **CAP-26** | TV | Libre, `Terminar` | Velo; `Terminar entrenamiento`, `Se guardará la sesión. ¿Continuar?`, `Cancelar` y `Terminar`; el HUD detrás; la cinta y la llave encima |
| **CAP-27** | TV (3 tomas) | En inglés: a) libre en marcha; b) el diálogo de terminar; c) `Prueba 4a` a 12 s del fin del primer trabajo, a 150 W | a) `Free Workout`, `SPM`, `Split /500m`, `Watts`, `kcal`, `Finish`, `Pause`; b) `Finish workout`, `The session will be saved. Continue?`, `Cancel`, `Finish`; c) `Work`, `Up next: Rest` (como CAP-21, en blanco), `target 160` |
| **CAP-28** | TV | `Corta` terminada sola | Workout en reposo con el aviso `¡Entrenamiento completado!` abajo |
| **CAP-29** | TV | Base que falla al crear la sesión, libre en marcha | El aviso `No se pudo guardar la sesión`, entero y sin tapar los controles; el reloj andando |
| **CAP-30** | TV | `Sin meta`, en el primer paso | La cuenta `?`; sin `Próximo` |

### 7.4 Secuencia de la demo

W1280, español, modo simulador, `F-4A` y reloj de prueba. Cada toma sigue a la anterior sin rearmar nada.

| ID | Paso | Qué tiene que verse |
|---|---|---|
| **DEMO-01** | Workout | Reposo con `Entrenamiento libre` y la tarjeta `Prueba 4a` (`1 serie · 1 descanso`, mini-barra de 6) entre las de `F-4A` |
| **DEMO-02** | `Entrenamiento libre`, a los 5 s | HUD libre en marcha: `00:05`, `24.0`, `150 W`, la distancia del monitor |
| **DEMO-03** | Llave → `+` de Watts | El panel abierto desde la llave con `160 W`, con Split y Watts a la vista (AC-BLE-16-03); la tarjeta `160` desde la lectura siguiente |
| **DEMO-04** | Cerrar el panel; `Pausa` | `Reanudar` verde; el reloj quieto |
| **DEMO-05** | `Reanudar`; `Terminar` | El diálogo de terminar |
| **DEMO-06** | `Terminar` | Workout en reposo, sin aviso |
| **DEMO-07** | Tocar `Prueba 4a` | El diálogo de inicio de `Prueba 4a` |
| **DEMO-08** | `Iniciar`, a los 18 s | `Calentamiento` con `0:12` y `Próximo: Trabajo` |
| **DEMO-09** | Trabajo de la repetición 1, a 0:45 | Watts fuera (`150`, `▲`, `obj. 160`); SPM cumple (`obj. 22`) |
| **DEMO-10** | Llave → `+` de Watts | Watts `160` en blanco, con `obj. 160` |
| **DEMO-11** | Repetición 2: `−` de Ritmo (23) | SPM fuera (`▲`, `obj. 24`) |
| **DEMO-12** | Enfriamiento, a 3 s del final | La cuenta `0:03` grande y teñida, sin `Próximo` |
| **DEMO-13** | Fin de la rutina | Workout en reposo con `¡Entrenamiento completado!` |
| **DEMO-14** | Dispositivo → `Desconectar`; Workout | El aviso naranja; `Entrenamiento libre` y las tarjetas deshabilitados |

### 7.5 Videos

| ID | Tamaño y duración | Qué se hace | Qué tiene que verse |
|---|---|---|---|
| **VID-01** | TV, 17 s (DT4a-04) | `Prueba 4a`, de los 14,5 s a los 31,5 s: los últimos 15,5 s del calentamiento y el primer segundo y medio del trabajo | `Próximo: Trabajo` entra suave y late; en `0:05` la cuenta crece y se tiñe al ritmo de 600 + 600 ms; en el cambio, la fila pasa a `Trabajo` con su pastilla y las tarjetas con objetivo toman borde, halo y `obj.`; nada salta |
| **VID-02** | TV, 10 s | Trabajo de la repetición 1 de `Prueba 4a`: `−` de Ritmo hasta 21 (fuera), `+` a 22 y, antes de que pasen 4 s del primer cruce, `−` a 21 otra vez; después `+` hasta 23 | SPM pasa a rojo con la flecha en una transición corta y vuelve a blanco sin parpadeo; el empujón solo en el primer cruce (el segundo, antes de 4 s, sin empujón: ANIMATION_BIBLE §2.8); el número es siempre el de la lectura |
| **VID-03** | TV, 10 s | Libre: `Pausa`, `Reanudar`, `Terminar`, `Cancelar`, `Terminar`, `Terminar` | El botón cambia en el acto; el diálogo entra y sale con su transición; el inmersivo se cierra y vuelve Workout |

---

## 8. Qué se verifica dónde

| Dónde | Qué |
|---|---|
| **[dotnet]** | El motor entero con reloj, planificador y ciclo de vida de prueba: fases y reloj (AC-WKT-01, AC-WKT-02-01 a -07), avance y fin con progresiones y Q-55 (AC-WKT-03), estado del paso (AC-WKT-04), fusión del pulso (AC-WKT-05-01 a -05, LT-35 a LT-38), telemetría y guardado con SQLite real, incremental, cierre e identidad con el legacy (AC-WKT-06), objetivos (AC-WKT-09, LT-39 a LT-44), reglas del semáforo (AC-WKT-10-01 a -04, -06), formatos (AC-WKT-11, LT-45), la regla de la rutina que no se puede iniciar (AC-WKT-12-08), la regla "sin remo" con los cuatro estados (AC-BLE-10-03), el motor sin mirar la conexión (AC-BLE-10-07), sesiones y puntos (AC-HIS-17-07, AC-HIS-19), catálogo (AC-APP-10-10), registro del pedido de pantalla (AC-APP-14-04), la regla de la llave y del panel (AC-BLE-16-02, -03) |
| **[Editor-EM]** | Las mismas pruebas dentro de Unity, con y sin simulador |
| **[Editor-PM]** | Workout en reposo: estados, lista compartida, diálogo, rutina que no se puede iniciar, desplazamiento (AC-WKT-12, AC-BLE-10-03 a -06); el inmersivo y el HUD, libre y con rutina, en vertical y horizontal (AC-WKT-13, -14, -16, -17, AC-WKT-10-05, -07 a -09, AC-SCN-01-01); controles y terminar, también con la rutina que termina sola con el diálogo abierto (AC-WKT-20, -20-07); el pulsómetro simulado (AC-WKT-05-06); el segundo plano con el ciclo de vida de prueba, con el aviso del fin al volver (AC-WKT-02-06, -08); errores y base lenta (AC-WKT-01-11, AC-WKT-06-12, -13); navegación, diálogos y avisos (AC-APP-03-09 a -11); los de §5, con la llave y el panel de D-ART-10 en Dispositivo, Workout, Rutinas, el editor y el inmersivo (AC-BLE-16-02, -03, AC-RUT-10-04, -09). Sin y con `-Simulator` (los de la cinta y la llave, solo con) |
| **[Captura]** | CAP-01 a CAP-30, DEMO-01 a DEMO-14 y VID-01 a VID-03 (§7) |
| **[Device-Win]** | Build de simulador: la demo en la ventana real; segundo plano minimizado (AC-WKT-02-09); cerrar la ventana y terminar el proceso a mitad de sesión, y ver la sesión con sus puntos con la herramienta de §12.9 (AC-WKT-06-05, -10); pantalla siempre encendida, también minimizada y al cerrar (AC-APP-14-01 a -03; en lo versionado, solo `RowMate.exe` y la sección de `powercfg`); el aviso del fin al restaurar la ventana (AC-WKT-02-09); Tab (AC-APP-05-07). Build normal: que la pantalla encendida y Workout anden igual sin la capa del simulador |
| **[Device-And]** | Solo con módulo y teléfono: "Atrás" (AC-WKT-13-03, AC-WKT-20-03), segundo plano y el primer cuadro al volver (AC-WKT-02-08, -10), pantalla encendida (AC-APP-14-02), letra 1,3× y barras del sistema visibles en el inmersivo (4a) |
| **[QA]** | La base con una herramienta de SQLite (AC-HIS-17-07, AC-WKT-06-10); la pantalla clásica inalcanzable (AC-WKT-23-01); lo que no se guarda (AC-WKT-06-14); bases del usuario y del legacy intactas (AC-HIS-20-04, R-14); repositorio legacy sin cambios; sin rutas de la PC en lo versionado, tampoco en la evidencia de `powercfg` (AC-APP-14-01) |

---

## 9. Checklist de paridad

| # | Comportamiento del legacy | Legacy | Test legacy | AC |
|---|---|---|---|---|
| 1 | Cuatro fases; la app abre en reposo | `lib/features/workout/workout_provider.dart:13`, `:49` | — | AC-WKT-01-01 |
| 2 | Lecturas del remo y del pulsómetro en todas las fases | `workout_provider.dart:82-86` | `workout_provider_hr_test.dart` | AC-WKT-01-01, AC-WKT-05-05 |
| 3 | Iniciar libre: fase, hora, muestreo en 0, alta de sesión, reloj después del alta | `workout_provider.dart:115-117`, `:133-149` | — | AC-WKT-01-02, AC-WKT-06-01 |
| 4 | Iniciar con rutina: expande, paso 0, distancia inicial, posiciones, objetivos | `workout_provider.dart:120-131` | `routine_positions_test.dart:6` (LT-16, It. 1) | AC-WKT-01-03 |
| 5 | La sesión guarda id y nombre de la rutina original | `workout_provider.dart:130`, `:140-144` | — | AC-WKT-06-01 |
| 6 | Un segundo solo en marcha, en un orden fijo | `workout_provider.dart:151-190` | — | AC-WKT-02-01, -02 |
| 7 | El reloj del HUD es el activo, no el del monitor | `immersive_workout_screen.dart:103`, `:569-577` | — | AC-WKT-02-03 |
| 8 | Punto cada 5 s en marcha, con la última lectura | `workout_provider.dart:158-173` | — | AC-WKT-06-02, -04 |
| 9 | El punto se toma antes del fin de paso | `workout_provider.dart:158-187` | — | AC-WKT-06-03 |
| 10 | Paso por tiempo: ≥ duración; el tiempo manda | `workout_provider.dart:196-197` | — | AC-WKT-03-02 |
| 11 | Paso por distancia: distancia − inicio ≥ metros (cambia por Q-55) | `workout_provider.dart:198-201` | — | AC-WKT-03-03 |
| 12 | Al completar: objetivos, índice + 1, reloj 0, distancia inicial | `workout_provider.dart:203-209` | — | AC-WKT-03-04 |
| 13 | Último paso: terminada en el acto, reloj detenido, guardado | `workout_provider.dart:210-216` | — | AC-WKT-03-05, -06 |
| 14 | Estado del paso solo con rutina y fuera de reposo | `workout_provider.dart:98-112` | — | AC-WKT-04-01 |
| 15 | Restante (−1 si no es por tiempo) y progreso acotados, también con la rutina terminada: un último paso por distancia sigue sin restante y con su largo | `workout_provider.dart:16-36`, `:101-104`; `immersive_workout_screen.dart:456-458` | — | AC-WKT-04-02, -03, -07, AC-WKT-03-06 |
| 16 | Por terminar: 1 a 5 s | `workout_provider.dart:39-41` | — | AC-WKT-04-04 |
| 17 | A continuación: 0 a 15 s con un paso siguiente | `immersive_workout_screen.dart:342-346` | — | AC-WKT-04-05, -06 |
| 18 | Fusión: el pulso del sensor reemplaza al del remo | `workout_provider.dart:220-230` | `workout_provider_hr_test.dart:80`, `:148` | AC-WKT-05-01, -03 |
| 19 | Un estado ≠ conectado con bpm previo pone 0; sin bpm previo no hace nada | `workout_provider.dart:232-237` | `workout_provider_hr_test.dart:80`, `:125`, `:167` | AC-WKT-05-01, -02, -04 |
| 20 | Pausar detiene el reloj | `workout_provider.dart:239-243` | — | AC-WKT-01-04 |
| 21 | Reanudar arranca un reloj nuevo (Q-58) y fija la referencia de distancia | `workout_provider.dart:245-250` | — | AC-WKT-01-05 |
| 22 | Terminar: último punto, estadísticas, puntos, cierre, última terminada | `workout_provider.dart:252-296` | `session_stats_test.dart` (LT-17 a LT-19, It. 1) | AC-WKT-06-06, -07, -11 |
| 23 | Error al guardar en silencio (cambia por Q-13) | `workout_provider.dart:297-299` | — | AC-WKT-06-12 |
| 24 | Volver a reposo: qué borra y qué no | `workout_provider.dart:305-318` | — | AC-WKT-01-07 |
| 25 | Workout: reposo solo en reposo; si no, `workoutInProgress` | `workout_screen.dart:19-35`, `:180-201` | — | AC-WKT-01-09 |
| 26 | Aviso naranja sin remo | `workout_screen.dart:59-77` | — | AC-BLE-10-03 |
| 27 | `Escenario` y selector | `workout_screen.dart:79-83` | — | Fuera de alcance (It. 8) |
| 28 | `Entrenamiento libre` con remo, sin confirmación | `workout_screen.dart:85-94` | — | AC-WKT-12-03, AC-BLE-10-04 |
| 29 | Etiqueta, tarjetas en el orden de la colección, al 50 % sin remo | `workout_screen.dart:98-127` | — | AC-WKT-12-04, AC-BLE-10-04 |
| 30 | Sin rutinas: `workoutNoRoutines` | `workout_screen.dart:129-138` | — | AC-WKT-12-06 |
| 31 | Diálogo de inicio con el nombre y `workoutStartContent`; afuera = cancelar | `workout_screen.dart:145-167` | — | AC-WKT-12-07 |
| 32 | El inmersivo se abre en el acto | `workout_screen.dart:169-175` | — | AC-WKT-13-01 |
| 33 | "Atrás" abre la confirmación | `immersive_workout_screen.dart:67-71` | — | AC-WKT-13-03 |
| 34 | El inmersivo se cierra solo en reposo | `immersive_workout_screen.dart:57-65` | — | AC-WKT-13-04 |
| 35 | Capas del inmersivo | `immersive_workout_screen.dart:74-117` | — | AC-WKT-13-02 |
| 36 | Diálogo de terminar, con textos fijos | `immersive_workout_screen.dart:125-140` | — | AC-WKT-20-03 |
| 37 | Confirmar: terminar, Strava (no aplica), reposo | `immersive_workout_screen.dart:141-147` | — | AC-WKT-20-04 |
| 38 | Barra de pasos: contenedor | `immersive_workout_screen.dart:348-354` | — | AC-WKT-14-01 |
| 39 | Fila de información: tipo, pastilla, espacio, `Up next`, cuenta (los colores de las pastillas y qué late cambian: DV-38) | `immersive_workout_screen.dart:380-471` | — | AC-WKT-14-02 a -05 |
| 40 | Pulso de 600 ms de ida y 600 de vuelta | `immersive_workout_screen.dart:319-322` | — | AC-WKT-14-04, -05 |
| 41 | Cuenta `M:SS` o `{metros}m` (`nullm` sin meta: cambia) | `immersive_workout_screen.dart:456-458` | — | AC-WKT-11-07, -08, AC-WKT-03-08 |
| 42 | Barra libre: ícono y título (reloj y chip, en la 4b) | `immersive_workout_screen.dart:491-522` | — | AC-WKT-16-01 |
| 43 | Reloj `H:MM:SS` / `MM:SS` | `immersive_workout_screen.dart:569-577` | — | AC-WKT-11-06 |
| 44 | Semáforo SPM, Watts y Split | `immersive_workout_screen.dart:579-602` | — | AC-WKT-10-01 a -04 |
| 45 | Horizontal si el ancho es mayor que el alto | `immersive_workout_screen.dart:605-606` | — | AC-WKT-17-07 |
| 46 | SPM arriba a la izquierda | `immersive_workout_screen.dart:611-636` | — | AC-WKT-17-02 |
| 47 | Split y Watts arriba a la derecha | `immersive_workout_screen.dart:639-663` | — | AC-WKT-17-03 |
| 48 | Banda: a 140 en vertical, a `+ 16` en horizontal; relleno 20 a los lados y 16 a cada lado del divisor (en horizontal cambia: DV-33) | `immersive_workout_screen.dart:669-706` | — | AC-WKT-17-04 |
| 49 | kcal siempre; BPM solo con pulso > 0; en columna, kcal arriba, a 140 del borde inferior en las dos orientaciones (en horizontal cambia: DV-33) | `immersive_workout_screen.dart:709-733` | — | AC-WKT-17-05 |
| 50 | Tarjeta de vidrio: tamaños, borde y halo con objetivo, etiqueta, unidad (`W` y `m` en el color del valor al 55 %: cambian, DV-31) | `immersive_workout_screen.dart:741-837`, `:877` | — | AC-WKT-17-01, -03, -04, AC-WKT-10-05 |
| 51 | Controles: `Terminar`, `Pausa` / `Reanudar` | `immersive_workout_screen.dart:893-953` | — | AC-WKT-20-01, -02 |
| 52 | Pantalla siempre encendida desde el arranque | `lib/main.dart:42`; `<pub-cache>/wakelock_plus-1.4.0/lib/src/wakelock_plus_windows_plugin.dart:23` | — | AC-APP-14-01 a -03 |
| 53 | La capa del simulador por encima de todas las rutas | `lib/main.dart:108-110` | `simulator_overlay_test.dart:17` (LT-26, It. 2) | AC-BLE-16-01, -02 |
| 54 | Tiene objetivos: vatios mín., SPM o split | `step_target_tracker.dart:23-26` | `step_target_tracker_test.dart:46` | AC-WKT-09-04, -07 |
| 55 | Lectura parada ignorada; split solo > 0 | `step_target_tracker.dart:30-40` | `step_target_tracker_test.dart:53` | AC-WKT-09-05 |
| 56 | Cierre con promedios contra los objetivos | `step_target_tracker.dart:43-62` | `step_target_tracker_test.dart:20`, `:28`, `:35` | AC-WKT-09-01 a -03 |
| 57 | Resultado y reinicio | `step_target_tracker.dart:65-70` | `step_target_tracker_test.dart:65` | AC-WKT-09-06 |
| 58 | El motor alimenta, cierra y reinicia los objetivos | `workout_provider.dart:129`, `:185`, `:204`, `:316` | — | AC-WKT-09-07 |
| 59 | Vuelta `M:SS.d` | `series_panels.dart:9-14` | `series_panels_test.dart:32` | AC-WKT-11-09 |
| 60 | Split de repetición `M:SS` y `—` | `series_panels.dart:16-20`, `:340` | `series_panels_test.dart:32` | AC-WKT-11-10 |
| 61 | Metros con miles en español | `series_panels.dart:22-24` | `series_panels_test.dart:32` | AC-WKT-11-11 |
| 62 | Hora `HH:mm` | `series_panels.dart:98` | — | AC-WKT-11-12 |
| 63 | Split en vivo `MM:SS` / `--:--` | `rowing_data.dart:24-29` | — | AC-WKT-11-01 |
| 64 | SPM con un decimal | `immersive_workout_screen.dart:625` | — | AC-WKT-11-02 |
| 65 | Watts entero y `W` aparte | `immersive_workout_screen.dart:655-656` | — | AC-WKT-11-03 |
| 66 | Distancia sin miles y `m` aparte | `immersive_workout_screen.dart:698-699` | — | AC-WKT-11-04 |
| 67 | Resumen final `{n} m` y kcal | `immersive_workout_screen.dart:986`, `:991` | — | AC-WKT-11-15 |
| 68 | Resumen de la rutina en la tarjeta y en el diálogo | `routine.dart:100-105`; `workout_screen.dart:115`, `:152` | — | AC-WKT-11-14 |
| 69 | Etiqueta de objetivo en la pastilla | `interval_step.dart:79-109`; `immersive_workout_screen.dart:394-415` | — | AC-WKT-11-13, AC-WKT-14-03 |
| 70 | Tablas de sesiones y puntos; alta, cierre y puntos | `database_service.dart:106-138`, `:216-229`, `:254-261` | — | AC-HIS-17-07, AC-WKT-06 |
| 71 | Sesión por id; puntos por tiempo | `database_service.dart:241-246`, `:263-270` | — | AC-HIS-19-01, -02 |
| 72 | Fechas de la sesión en UTC | `workout_session.dart:39-51` | — | AC-WKT-06-01 (AC-RUT-16-04) |
| 73 | Pantalla clásica, inalcanzable | `workout_screen.dart:205-988` | — | AC-WKT-23-01 |
| 74 | "Conectado" = estado del remo conectado; cualquier otro de los cinco (`off`, `scanning`, `connecting`, `disconnected`) es "sin remo" | `device_provider.dart:94`; `lib/core/bluetooth/ble_service.dart:8`; `workout_screen.dart:50` | — | AC-BLE-10-03 |
| 75 | La rutina termina con el diálogo de terminar abierto: el de completado se apila encima y, al cerrarlo, el de terminar se cierra sin efecto | `immersive_workout_screen.dart:49-65`, `:141` | — | AC-WKT-20-07 |
| 76 | El diálogo de completado es modal y queda hasta que se lo toca: al volver del segundo plano siempre se ve | `immersive_workout_screen.dart:207-212` | — | AC-WKT-02-06, -09 (el aviso provisorio aparece una vez al volver) |
| 77 | Un paso de duración negativa termina en el primer segundo; su restante (`clamp(0, d)` con `d < 0`) lanza | `workout_provider.dart:103`, `:196-197` | — | AC-WKT-03-07, AC-WKT-04-02 (cambia: DV-37) |
| 78 | Llave a 16 de la derecha y 96 de abajo en toda la app, también en el entrenamiento; el panel se abre ahí y queda abierto al cambiar de pantalla | `lib/core/dev/simulator_overlay.dart:28-50` | `simulator_overlay_test.dart:17` (LT-26, It. 2) | AC-BLE-16-02, -03 (cambia: DV-39) |

---

## 10. Mejoras EXR (cómo se tiene que sentir)

Son agregados de presentación: ninguno cambia un dato, un umbral, un orden ni una regla del legacy.

1. **Un tablero de regata a 1,5 m.** Los números son grandes, de ancho fijo y alto contraste sobre cualquier fondo (D-ART-02, R-10); no "bailan" cuando cambian: cambian con un rodillo corto, como en Dispositivo, y lo que se ve es siempre el dato real.
2. **El objetivo, a la vista.** La tarjeta con objetivo conserva el color de su métrica y muestra en chico cuánto pide (`obj. 26`). Fuera de objetivo, el número pasa al rojo y una flecha dice hacia dónde ir; el cambio es suave, sin parpadeo, y el pequeño empujón que llama la atención no se repite más de una vez cada 4 s (ANIMATION_BIBLE §2.8).
3. **El paso se lee de un vistazo.** El tipo en su color, el objetivo en una pastilla y la cuenta regresiva grande. En los últimos 15 s, `Próximo` late suave; en los últimos 5, la cuenta crece y respira el color del paso: el remero sabe que se viene el cambio sin mirar el reloj.
4. **El pulso como signo vital.** El corazón de la tarjeta BPM late a la frecuencia real (ANIMATION_BIBLE §2.9) y la tarjeta aparece y se va con un fundido.
5. **Empezar es inmediato.** Un toque y ya está remando: la sesión y el reloj corren desde el toque, sin esperas ni pantallas intermedias.
6. **Terminar con confianza.** La confirmación es clara y el guardado, invisible: la sesión se guarda mientras se rema y ni un corte de luz la pierde.
7. **Volver sin sobresaltos.** Al volver del segundo plano se ve el presente, sin relojes que corren ni avisos acumulados.
8. **Workout como previa.** Cada rutina muestra su silueta (la mini-barra) y su objetivo de un vistazo; el botón libre es grande y claro; el aviso de remo desconectado es inconfundible.
9. **Respeto por quien lo pide.** Con "reducir movimiento", fundidos y realces fijos; con letra grande, el reposo y los diálogos entran; con teclado, todo se alcanza.

Se comprueba con AC-WKT-10-05, -06, AC-WKT-14-04, -05, AC-WKT-17-01, AC-WKT-02-08, AC-WKT-06-05, AC-APP-05-08 y los videos VID-01 a VID-03, que revisan QA y el Designer.

---

## 11. Decisiones aplicadas y gate de la It. 4a

| Decisión | Qué se aplica | Confirmar en el gate |
|---|---|---|
| Q-01 | Los textos del HUD, de los controles y del diálogo de terminar salen del catálogo, con el español idéntico al legacy; **12 claves nuevas** (§11.2), 202 en total. El chip `Up next:` se traduce (DV-24) | No (ya aceptada en la It. 1); DV-24 **sí** |
| Q-02 | `workoutStartContent` con `¿` (corregido desde la It. 1) | No |
| Q-06 | Formatos fijos: punto decimal, miles en español en los paneles, 24 h (AC-WKT-11-16) | No |
| Q-12 | **Telemetría incremental**: cada punto queda guardado al tomarse; una sesión cortada conserva sus puntos y una terminada queda igual que en el legacy (AC-WKT-06-05, -07, -10) | **Sí** |
| Q-13 | Errores visibles: `workoutSaveError` y el estado de error de la lista; nunca un cuelgue (AC-WKT-01-11, AC-WKT-06-12, AC-WKT-12-06) | No |
| Q-15 | Totales del monitor, sin ponerlos en 0 (AC-WKT-06-08, AC-WKT-17-06) | No |
| Q-51 | El paso por distancia muestra su largo total y no avanza (AC-WKT-11-08) | No |
| Q-53 | Al terminar a mano no aparece nada de "completado" (AC-WKT-20-04); el diálogo es de la 4b | No |
| Q-55 | El paso por distancia cuenta con el saneamiento de los parciales (AC-WKT-03-03; DV-26) | No (se muestra con las DV) |
| Q-56 | Pantalla siempre encendida en toda la app, desde el arranque (AC-APP-14) | No |
| Q-57 | La pantalla clásica no se migra (AC-WKT-23-01) | No |
| Q-58 | La fracción de segundo se pierde en cada pausa (AC-WKT-01-05) | No |
| Q-60 | Con objetivo, etiqueta y borde en el color de la métrica; el valor, blanco o rojo (AC-WKT-10-05) | No |
| Q-91 | La sesión sigue en segundo plano; criterios en AC-WKT-02-04 a -10 y AC-WKT-06-15; el equipo real en la 10a y la 10b | No |
| D-ART-01 | Contraste del HUD: ≥ 7:1 en blanco y en las métricas no rojas; la familia roja ≥ 4,5:1 con `#FF625B` y la flecha `▲` / `▼` (AC-WKT-10-05) | **Sí** |
| D-ART-02 | HUD un 15-25 % más grande (SPM 64, Split y Watts 44, kcal y BPM 32, banda 34) **y escalado con la pantalla** hasta 1,6× (en la ventana de 1280 × 720 el SPM mide 102 dp; en el legacy, 52), sin seguir la letra del sistema (AC-WKT-17-01) | **Sí** |
| D-ART-09 | `obj. 26` debajo del valor con objetivo (AC-WKT-10-05) | No |
| D-ART-10 | La llave del simulador abajo a la izquierda fuera del entrenamiento y, en el entrenamiento, sobre kcal y BPM en vertical y junto a `Pausa` en horizontal; el panel se abre desde la llave, la sigue si cambia de lugar y, en el entrenamiento, deja a la vista Split y Watts (AC-BLE-16-02, -03, AC-RUT-10-04, -09; DV-39). Reemplaza a DV-13 de la It. 3 (00-deuda-diseno §1, 04-design-notes §3.9; QA-V3-09) | **Sí** (se muestra con las DV) |
| QA-P-17 | Respaldo del vidrio `glass.fallback` (`#070D16 @86 %`) con el borde y el radio del legacy (AC-WKT-17-01) | No |
| R3-08 | Una rutina que el editor no dejaría guardar, o sin pasos al expandir, no se inicia y se avisa (AC-WKT-12-08; DV-28) | No (se muestra con las DV) |
| DV-21 a DV-39 | Diferencias visibles con el legacy, a propósito (§11.1); DV-39 reemplaza a DV-13 de la It. 3 | **Sí**: se le muestran al usuario al cerrar, como las DV de las It. 2 y 3 |
| DT4a-01 | Al cerrarse un diálogo, el foco vuelve a quien lo abrió solo si se usaba el teclado (AC-APP-05-07): es la conducta del legacy en la PC | No |
| DT4a-04 | VID-01 dura 17 s, para que entren la entrada de `Próximo`, el "por terminar" y el cambio de paso (§7.5) | No |
| DT4a-12 | Un paso de duración negativa muestra `0:00` y termina en el primer segundo (AC-WKT-03-07; DV-37) | No (se muestra con las DV) |
| P-03 | Cerrar la ventana a mitad de sesión la deja cortada, como un corte de luz (§14) | **Sí** (sin urgencia) |
| P-17 | (Al cerrar, QA-V4a-ROB-05.) El tiempo con el equipo suspendido no cuenta como tiempo de sesión, como en el legacy (§14); el criterio va en la 4b | **Sí** |
| Strava | Se comporta como "no configurado" hasta la It. 12, un estado válido del legacy; con el simulador, nunca se sube nada (R-01) | No |

### 11.1 Diferencias visibles con el legacy (a propósito)

Siguen la numeración de la It. 3. No incluyen las mejoras EXR de §10 ni el sistema visual de la app de las It. 2 y 3 (tipografía, íconos, botones, superficies, colores de tipo legibles de DV-12), que no cambian datos, textos, orden ni significados.

| ID | Legacy (verificado) | App | Motivo | AC |
|---|---|---|---|---|
| DV-21 | La tarjeta de Workout es un `ListTile` con el nombre y el resumen en 12 y el *chevron* (`workout_screen.dart:107-123`) | La misma tarjeta de Rutinas, con la descripción (si hay) y la mini-barra, y el *chevron* en lugar del `⋮` | Una sola tarjeta de rutina en la app; la descripción y la silueta ayudan a elegir (ART_BIBLE §3.7, F-WKT-12 Mejora EXR) | AC-WKT-12-04 |
| DV-22 | Solo la lista se desplaza; aviso, botón y etiqueta quedan fijos (`workout_screen.dart:52-127`) | Todo el contenido se desplaza junto | En 640 × 360 con el aviso y letra 1,3× la parte fija no deja lugar ni para una tarjeta (como DV-18) | AC-WKT-12-09 |
| DV-23 | Mientras carga se ve "sin rutinas"; si la carga falla no hay estado de error (`workout_screen.dart:98-138`) | El indicador de Rutinas mientras carga; `No se pudieron cargar las rutinas` y `Reintentar` si falla | Q-13; la colección compartida ya tiene esos estados (It. 3) | AC-WKT-12-06 |
| DV-24 | `Up next: ` en inglés también con la app en español (`immersive_workout_screen.dart:435`) | `Próximo:` en español; `Up next:` en inglés | Es el único texto en inglés del HUD en español; `Watts`, `SPM`, `kcal` y `BPM` quedan como en el legacy porque son términos de remo | AC-WKT-14-04 |
| DV-25 | Un paso sin meta muestra `nullm` en la cuenta (`immersive_workout_screen.dart:458`) | `?`, como en el editor | Defecto evidente | AC-WKT-03-08 |
| DV-26 | El paso por distancia compara la distancia cruda contra la del inicio: un reinicio del monitor lo traba (con `R-4R`, termina en el segundo 285 en lugar del 26) y cuenta lo remado en pausa (`workout_provider.dart:198-201`) | El saneamiento de los parciales: un salto negativo o de más de 10 m/s no suma y lo remado en pausa no cuenta. Un paquete aislado sin distancia hace perder los metros de esa lectura y la siguiente (≈ 2 s de remo), como en los parciales. Un monitor que no manda la distancia una vez por segundo (cada 2 s a más de 5 m/s, o en ráfagas) puede trabar el paso, que no termina nunca; el legacy sí termina (QA-V4a-L01, al cerrar; se decide en la 10a: P-04) | Q-55. Así el paso y los parciales de la It. 5 cuentan los mismos metros | AC-WKT-03-03 |
| DV-27 | El reloj cuenta disparos del temporizador: los segundos en que la app estuvo trabada o suspendida se pierden (`workout_provider.dart:147`) | Se procesan al volver, en orden y con la lectura de cada instante | Q-91 (la sesión sigue en segundo plano) y ADR 017 | AC-WKT-02-04 |
| DV-28 | Una serie ×2 000 000 000 traba el inicio; una rutina sin pasos al expandir hace fallar el inmersivo; una con valores fuera de los topes de Q-92 arranca | No se inicia: aviso `Esta rutina no se puede iniciar: revisá sus pasos en Rutinas`, sin sesión | R3-08 y Q-92: lo que el editor no deja guardar no se rema. Solo pasa con una base tocada a mano | AC-WKT-12-08 |
| DV-29 | Si la base falla al crear la sesión, el reloj queda en `00:00`; si falla al guardar, nadie se entera (`workout_provider.dart:145`, `:297-299`) | El entrenamiento anda igual y aparece `No se pudo guardar la sesión` | Q-13 | AC-WKT-01-11, AC-WKT-06-12 |
| DV-30 | Dos toques rápidos en `Entrenamiento libre` o en `Iniciar` pueden crear dos sesiones y abrir dos inmersivos (`workout_screen.dart:88-89`, `:164-165`, sin esperar) | Un toque, una sesión | Corrección, como Q-19 en el editor | AC-WKT-01-10 |
| DV-31 | Vidrio negro al 55 / 50 / 45 %; valores en el color base y etiquetas al 80 %; las unidades `W` y `m` en el color del valor al 55 % (con objetivo, blanco o `#FF4444` al 55 %: `immersive_workout_screen.dart:826`, `:877`); 52 / 36 / 28 / 28 dp, sin escalar con la pantalla, siguiendo la letra del sistema; `Terminar` `#FF5252` 120 × 48, `Pausa` 140 × 48 con texto del color primario | `glass.fallback` `#070D16 @86 %`; valores en `.hud` y etiquetas en el color base al 100 %; las unidades `W` y `m` en la variante `.hud` de su métrica al 80 %, también con objetivo; tamaños de D-ART-02 por la escala del HUD, sin la letra del sistema; `Terminar` en `#FF7272` 128 × 56, `Pausa` 148 × 56 en blanco | QA-P-17, R-10, D-ART-01 y D-ART-02: contraste en el peor fondo (las unidades al 55 % dan 3,2-4,0:1; al 80 % en `.hud`, ≥ 5,1:1: 04-design-notes §9) y botones para manos transpiradas | AC-WKT-17-01, AC-WKT-17-03, AC-WKT-17-04, AC-WKT-10-05, AC-WKT-20-01, AC-APP-06-01 |
| DV-32 | Con objetivo, valor y etiqueta en blanco o `#FF4444`, y el borde y el halo de ese color | Etiqueta y borde en el color de la métrica; el valor en blanco o `#FF625B` con `▲` / `▼`; `obj. N` debajo | Q-60, D-ART-01, D-ART-09: el significado no depende solo del color | AC-WKT-10-05 |
| DV-33 | kcal sobre BPM, en columna, a 140 del borde inferior en las dos orientaciones (`immersive_workout_screen.dart:709-733`), y la banda con 20 de relleno a los lados y 16 a cada lado del divisor (`:676`, `:693`): en un teléfono horizontal (914 × 411 y 640 × 360) la tarjeta kcal se encima con la de Watts (calculado sobre `:639-663`, `:709-733`) | En horizontal, kcal y BPM van en fila a la altura de los controles, abajo a la derecha (kcal en el rincón y BPM a su izquierda), y la banda, con sus espacios internos más cortos (14 a los lados y 10 a cada lado del divisor). Nada se encima; lo que no entra se achica (en 640 × 360, la banda). En vertical, como el legacy | Defecto de diseño del legacy. Con D-ART-02, en horizontal la columna de la derecha no entra en ninguna ventana (00-deuda-diseno §4.1); sigue siendo la misma zona, abajo a la derecha (ART_BIBLE §5.2; 04-design-notes §3.1) | AC-WKT-17-04, AC-WKT-17-05, AC-WKT-17-08 |
| DV-34 | (Provisorio) El legacy tiene el selector de escenario en reposo, la escena, la fila de píldoras, el reloj de pared, el chip de fase, el diálogo de rutina completada y las barras del sistema ocultas | En la 4a no están: fondo provisorio, una sola fila en la barra de pasos, la barra libre solo con su título, y al terminar la rutina el aviso `¡Entrenamiento completado!` | Llegan en la 4b (píldoras, reloj, chip, diálogo, barras), la 6a (escena) y la 8 (selector) | AC-WKT-12-02, AC-WKT-03-05, AC-WKT-13-01, AC-WKT-14-01, AC-WKT-16-01, AC-SCN-01-01 |
| DV-35 | `📈` emoji en la pastilla de objetivo (`interval_step.dart:105`) | Ícono `trending_up`; el texto que sigue, idéntico | Como DV-10 en el editor | AC-WKT-14-03 |
| DV-36 | `workoutNoRoutines` y `workoutInProgress` al 38 % (`workout_screen.dart:134`, `:199`) | Al menos 54 % | D-ART-03 | AC-WKT-12-06, AC-WKT-01-09 |
| DV-37 | Un paso de duración negativa (el editor la deja guardar: un `Min` negativo con el teclado con signo de DV-15, y los topes de Q-92 van por valor absoluto) hace fallar el inmersivo mientras ese paso está en curso: el restante usa `clamp(0, d)` con `d < 0`, que lanza (`workout_provider.dart:103`); el motor igual lo termina en el primer segundo (`:196-197`) | Restante 0: la cuenta muestra `0:00` (con `Próximo` si hay un paso siguiente) y el paso termina en el primer segundo, como en el legacy | Defecto evidente; el motor no cambia (DT4a-12) | AC-WKT-03-07, AC-WKT-04-02 |
| DV-38 | En la barra de pasos, el texto de la pastilla de objetivo va en el color del tipo sobre su tinte al 20 %; `Up next:` en blanco al 60 % y el tipo siguiente en su color, sobre un tinte al 25 %; la pastilla `Up next` entera, texto incluido, late de 0,6 a 1 (`immersive_workout_screen.dart:394-450`) | Los textos de las dos pastillas en blanco (`Próximo:` al 70 %), los dos tintes al 20 %, y en `Próximo` laten solo el fondo y el borde, con el texto siempre entero. El tipo en curso sigue en su color a la izquierda, y los tintes y el borde conservan el color de cada paso | Contraste en el peor fondo (D-ART-01; ART_BIBLE §3.2): en el legacy, el texto de la pastilla en el color del tipo da 3,7:1 (trabajo y enfriamiento), `Up next:` 3,8-4,1:1, el tipo siguiente en su color 3,5-4,9:1 y, en el punto tenue del latido, ≈ 3:1; en blanco dan ≥ 8,1:1 y `Próximo:` ≥ 4,9:1 (04-design-notes §4.5, §9) | AC-WKT-14-03, AC-WKT-14-04 |
| DV-39 | La llave del simulador a 16 dp de la derecha y 96 de abajo en toda la app, también en el entrenamiento; el panel se abre en ese lugar y queda abierto al cambiar de pantalla (`lib/core/dev/simulator_overlay.dart:28-50`). En la app, DV-13 (It. 3) la subía 16 dp sobre el `+` de Rutinas | Fuera del entrenamiento, **abajo a la izquierda** (16 dp del borde; 16 sobre la barra o, con una pantalla apilada, sobre el borde de abajo); en el entrenamiento, abajo a la derecha: en vertical sobre kcal y BPM, en horizontal junto a `Pausa` (o encima, si no queda lugar). El panel se abre desde la llave y la sigue si cambia de lugar; en el entrenamiento deja a la vista Split y Watts; los menús no se abren debajo de ella. **Reemplaza a DV-13** | QA-V3-09: en el carril derecho la llave tapaba acciones que se repiten en cada fila (`⋮`, cruz, tacho, *chevron*) y `Serie`, y en una lista que se desplaza no hay altura que la salve; a la izquierda las filas ponen lo que las identifica, no lo que se toca (00-deuda-diseno §1, D-ART-10). En el entrenamiento así deja libre el hueco de los avisos y tiene lugar en todas las ventanas, también en 640 × 360 (04-design-notes §3.9) | AC-BLE-16-02, AC-BLE-16-03, AC-RUT-10-04, AC-RUT-10-09 |

### 11.2 Claves nuevas del catálogo (Q-01)

Español idéntico al legacy donde el legacy tiene el texto fijo, y su inglés. Con estas, el catálogo pasa de 190 a **202** claves por idioma.

| Clave | es | en | Parámetros | Dónde |
|---|---|---|---|---|
| `hudSpm` | `SPM` | `SPM` | — | tarjeta SPM (AC-WKT-17-02) |
| `hudSplit` | `Split /500m` | `Split /500m` | — | tarjeta Split (AC-WKT-17-03) |
| `hudWatts` | `Watts` | `Watts` | — | tarjeta Watts (AC-WKT-17-03) |
| `hudUnitWatts` | `W` | `W` | — | unidad de Watts |
| `hudKcal` | `kcal` | `kcal` | — | tarjeta kcal (AC-WKT-17-05) |
| `hudBpm` | `BPM` | `BPM` | — | tarjeta BPM (AC-WKT-17-05) |
| `hudUnitMeters` | `m` | `m` | — | unidad de la distancia de la banda (AC-WKT-17-04) |
| `hudFreeTitle` | `Entrenamiento Libre` | `Free Workout` | — | barra libre (AC-WKT-16-01) |
| `hudUpNext` | `Próximo:` | `Up next:` | — | pastilla "a continuación" (AC-WKT-14-04; DV-24) |
| `hudTarget` | `obj. {value}` | `target {value}` | `value` (texto: `22`, `160`, `02:05`) | objetivo debajo del valor (AC-WKT-10-05; D-ART-09) |
| `workoutSaveError` | `No se pudo guardar la sesión` | `Couldn't save the session` | — | AC-WKT-01-11, AC-WKT-06-12 |
| `workoutRoutineCannotStart` | `Esta rutina no se puede iniciar: revisá sus pasos en Rutinas` | `This routine can't be started: check its steps in Routines` | — | AC-WKT-12-08 |

Se reusan, sin cambios: `workoutTitle`, `workoutFree`, `workoutRowerNotConnected`, `workoutOrPickRoutine`, `workoutNoRoutines`, `workoutInProgress`, `workoutStartContent`, `workoutStart`, `cancel`, `workoutFinish`, `workoutFinishTitle`, `workoutFinishContent`, `workoutPause`, `workoutResume`, `workoutCompleted` (en el aviso provisorio), `stepType*`, `stepDurationMeters`, `stepDurationNone`, las de la etiqueta de objetivo y del resumen (It. 1), `routinesLoadError` y `retry` (It. 3). `workoutStartTitle` y `metricFreeLabel` siguen sin uso (paridad).

---

## 12. Restricciones de producto para el Architect

1. **Determinismo y aislamiento.** Pruebas, capturas y videos usan base temporal, reloj, planificador y ciclo de vida de prueba, el remo y el pulsómetro de prueba de §0.4; **nunca** las bases del usuario (AC-HIS-20-04). Ningún doble llega a un build instalable.
2. **Un solo motor, el mismo en primer y en segundo plano (Q-91).** Con las mismas lecturas y su hora, la sesión da el mismo resultado haya o no segundo plano (AC-WKT-02-05, -06), sin perder ni duplicar segundos (AC-WKT-02-04). Los topes de espera (por ejemplo, el del cierre de la sesión) no cuentan el tiempo en segundo plano: una sesión que terminó y se guardó mientras tanto no se da por fallada al volver (AC-WKT-02-06). Es lo que fijó el ADR 017; lo que en Android no se puede igualar sin un servicio en primer plano sigue siendo el riesgo RA-08, que se mide en la 10a.
3. **Guardado incremental, durable y en orden (Q-12).** Cada punto, guardado a más tardar 1 s después de tomarse; el cierre de la sesión después de todos sus puntos; nada de esto frena el HUD (AC-WKT-06-05, -13). Un error se avisa y el entrenamiento sigue, y una sesión cerrada siempre tiene todos sus puntos (AC-WKT-06-12). Al pasar a segundo plano se guarda todo lo pendiente, también lo tomado con el alta en vuelo, con una espera de 1 s como mucho (AC-WKT-06-15).
4. **Identidad con el legacy.** Una sesión terminada normalmente tiene los mismos puntos y totales que en el legacy, incluido el punto final repetido (AC-WKT-06-07). Si el oráculo Dart puede correr el motor del legacy, que dé los vectores (QA-P-23).
5. **R3-08.** Antes de expandir, comprobar los topes y que la rutina tenga pasos, sin expandirla y sin depender de la cantidad de repeticiones (AC-WKT-12-08). Nunca se expande una rutina que no se puede iniciar.
6. **El HUD no se encima** en las 4 ventanas de captura, con los valores largos y con los extremos de FTMS, y deja libre la zona del bote (AC-WKT-17-08 a -10). La llave sigue D-ART-10 en toda la app y en el entrenamiento no tapa el HUD (AC-BLE-16-02; QA-V3-09); el panel se abre desde ella y, en el entrenamiento, deja a la vista Split y Watts (AC-BLE-16-03). La cinta tapa solo el comienzo de la barra superior, como en el legacy; la capa del simulador sigue siendo la última.
7. **Pantalla encendida**: pedida al arrancar, en toda la app, vigente al volver del segundo plano y liberada al cerrar (AC-APP-14).
8. **Textos**: las 12 claves de §11.2; ningún texto del HUD fuera del catálogo, salvo los formatos (números, `:`, `.`, `--:--`, `?`).
9. **Herramienta de la demo**: una forma de **solo lectura** de listar las sesiones de una base (id, inicio, fin, rutina y cantidad de puntos), para la demo (c) y para QA. Nunca modifica la base (QA compara contenido y fecha antes y después), no va dentro de la app y no imprime rutas de la PC en nada versionado.
10. **Identificadores estables para las pruebas**, por ejemplo: `workout-free`, `workout-banner`, `workout-routine-<n>`, `workout-retry`, `dialog-cancel`, `dialog-confirm`, `hud-finish`, `hud-pause`, `hud-resume`, `hud-spm`, `hud-split`, `hud-watts`, `hud-kcal`, `hud-bpm`, `hud-time`, `hud-distance`, `hud-target-<métrica>`, `hud-step-type`, `hud-step-target`, `hud-up-next`, `hud-countdown`, `hud-free-title`. Los nombres finales los fija el Architect.
11. **Strava**: ninguna llamada ni regla en la 4a. "La última sesión terminada" queda lista para la It. 12 (AC-WKT-06-11).
12. **Builds de desarrollo**: se lanzan solo desde la carpeta de salida por defecto, para no disparar avisos nuevos del Firewall de Windows (QA-V3-08).
13. **Nada del inmersivo queda huérfano.** Si el inmersivo se cierra solo, su diálogo de terminar se cierra antes, sin efecto (AC-WKT-20-07), y el panel del simulador pasa al lugar nuevo de la llave (AC-BLE-16-02). De los avisos que se producen sin que el usuario los vea (en segundo plano, con la ventana minimizada o sin foco), al volver se muestra una vez solo el último, con sus 4 s desde ese momento; así el del fin de la sesión nunca se pierde (AC-WKT-02-06, -08, -09).

---

## 13. Fuera de alcance

- **It. 4b**: la fila de píldoras y sus estados (F-WKT-14, F-WKT-15), el reloj de pared (F-WKT-19), el chip de fase de la barra libre (F-WKT-16), el diálogo de rutina completada (F-WKT-21; reemplaza el aviso provisorio), la entrada "sumergirse" y la salida del inmersivo, el velo de pausa, las barras del sistema ocultas (F-WKT-13 punto 1, F-APP-15), la cuenta grande de los últimos 3 s y el sello de resultado (D-ANI-05), Q-52, Q-53 (la parte del diálogo) y Q-59.
- **It. 5**: parciales de 500 m, repeticiones y sus paneles (F-WKT-07, F-WKT-08, F-WKT-18), con `immersive.lapsExpanded` e `immersive.repsExpanded`.
- **It. 6a y 6b**: la escena (F-SCN-*), el bote que avanza con el split, el agua, los *scrims* y el vidrio con desenfoque.
- **It. 8**: el selector de escenario en reposo (F-SCN-22) y `scene.environment`.
- **It. 9**: Historial y detalle; ahí se ven las sesiones de la 4a, también las cortadas.
- **It. 10a**: el remo real, el aviso "Reconectando con {remo}…" sobre el HUD (Q-34), el keepalive en segundo plano y el segundo plano en Android con el remo real. **It. 10b**: iOS y macOS ([Apple]).
- **It. 11**: el pulsómetro real. **It. 12**: la subida a Strava al terminar (F-WKT-22, F-STR-06). **It. 13**: la reparación de sesiones (F-HIS-21).
- **It. 14**: háptica, lectores de pantalla (DT-05) y la pantalla encendida solo en Dispositivo y en el entrenamiento (BKL-24).
- **Backlog** (no paridad): retomar una sesión cortada, confirmar al cerrar la ventana, los metros que faltan en un paso por distancia (Q-51), la duración en `Próximo`, el indicador "sensor sin contacto" (Q-46), el atajo del aviso de remo a Dispositivo (§14 P-07), cámaras, sonido, botes fantasma.

---

## 14. Preguntas abiertas

| ID | Pregunta | Por defecto | Para |
|---|---|---|---|
| P-01 | ¿`Up next:` se traduce a `Próximo:` en español? | **Sí** (DV-24). Alternativa: `Up next:` en los dos idiomas, como el legacy | Usuario (gate) |
| P-02 | ¿Hasta la 4b, al terminar la rutina sola se avisa `¡Entrenamiento completado!`? | **Sí**, provisorio; la 4b pone el diálogo | Usuario (gate, informativo) |
| P-03 | ¿Cerrar la ventana a mitad de una sesión la debería terminar como `Terminar`? | **No**: queda cortada, igual que con un corte de luz, en todas las plataformas (en Android y iOS no hay un aviso confiable de cierre); la It. 13 la repara | Usuario (sin urgencia) |
| P-04 | Q-55 aplicado tal cual: ¿se acepta que lo remado en pausa no cuente y que un paquete aislado sin distancia haga perder ≈ 2 s de metros? | **Sí**: son los mismos metros que los parciales. Si el AMS-670B manda paquetes sin distancia seguido, la 10a lo resuelve para los dos a la vez. **Ampliada al cerrar (QA-V4a-L01):** un monitor que no informa la distancia una vez por segundo (cada 2 s a más de 5 m/s, dos lecturas en el mismo segundo o una ráfaga después de una ventana trabada) puede dejar un paso por distancia sin terminar nunca; el legacy sí termina. Con el simulador no pasa (una lectura por segundo, hasta 5,6 m/s). Requisito de producto para la 10a: **un paso por distancia nunca queda trabado si el monitor informa avance**. La 10a mide la cadencia del AMS-670B y, si no es estable, cambia la comparación en el paso y en los parciales a la vez | Architect y QA (10a) |
| P-05 | ¿El HUD crece con la pantalla hasta 1,6×? | **Sí**, dentro de D-ART-02 | Usuario (gate) |
| P-06 | ¿La tarjeta de Workout muestra también la descripción? | **Sí** (DV-21) | Usuario (gate, con las DV) |
| P-07 | ¿El aviso de remo desconectado lleva a Dispositivo al tocarlo (Mejora EXR de F-BLE-10)? | **No** en la 4a: agregado para la It. 14 o el backlog | — (PO) |
| P-08 | ¿Escape en el inmersivo abre la confirmación de terminar? | **No** (paridad); "Atrás" de Android sí | — (PO) |
| P-09 | Una rutina que no se puede iniciar, ¿avisa al tocar la tarjeta o al confirmar? | Al tocar la tarjeta, sin diálogo (AC-WKT-12-08) | — (PO) |
| P-10 | ¿Un toque, una sesión? | **Sí** (DV-30) | — (PO) |
| P-11 | ¿La pantalla queda encendida con la ventana minimizada? | **Sí** (paridad en Windows) | — (PO) |
| P-12 | ¿En qué lugar va la llave del simulador en el inmersivo? | **Resuelta**: D-ART-10 con el ajuste del inmersivo, sobre kcal y BPM en vertical y junto a `Pausa` en horizontal; el panel se abre desde la llave (AC-BLE-16-02, -03; 04-design-notes §3.9) | Usuario (gate, con DV-39) |
| P-13 | Si la rutina termina con la app en segundo plano o minimizada, ¿se ve `¡Entrenamiento completado!` al volver? | **Sí**, una sola vez (AC-WKT-02-06, -09). En la 4b lo reemplaza el diálogo, que queda hasta que se lo toca (paridad) | — (PO; QA-G4a-P08) |
| P-14 | ¿Con el panel del simulador abierto en el entrenamiento se ven Split y Watts? | **Sí**: en vertical el panel termina 8 dp debajo de Watts; en horizontal no llega a ellas (AC-BLE-16-03). En el legacy, en un teléfono vertical, tampoco las tapa | — (PO; 00-deuda-diseno §1.7) |
| P-15 | ¿Las pastillas de la barra de pasos llevan el texto en blanco y en `Próximo` late solo el fondo? | **Sí** (DV-38). Alternativa: los colores y el latido del legacy, con la excepción de contraste de 04-design-notes §9.2 | Usuario (gate, con las DV) |
| P-16 | Una sesión con puntos que no se pudieron guardar, ¿se cierra igual? | **No**: los vuelve a intentar al cerrar y, si no entran, queda cortada; una sesión cerrada siempre tiene todos sus puntos (AC-WKT-06-12) | — (PO; QA-G4aT-13) |
| P-17 | (Nueva al cerrar, QA-V4a-ROB-05.) Si Windows suspende el equipo con una sesión en marcha, ¿el tiempo suspendido cuenta como tiempo de sesión? AC-WKT-02-09 cubre solo la ventana minimizada o sin foco, con el equipo despierto | **No**, como en el legacy, que pierde esos segundos: al despertar, la sesión sigue en la fase en que estaba, desde el segundo en que se suspendió, sin puntos ni pasos del lapso suspendido y sin avisos. Contarlo llenaría la sesión de puntos repetidos de la última lectura, sin nadie remando. Mientras la app está abierta Windows no se suspende por inactividad (AC-APP-14-01): pasa solo si se suspende a mano o se cierra la tapa. Hoy no está medido qué hace la app (depende de si el reloj monótono cuenta la suspensión). Se especifica en la 4b y lo verifica el usuario en esa demo, porque QA no suspende su equipo | Usuario (gate) |

---

## 15. Discrepancias encontradas al especificar

| Dónde | Qué dice | Qué es (verificado en el legacy) | Acción |
|---|---|---|---|
| ROADMAP §2, It. 4a, "También toca" | F-WKT-13, F-WKT-16 y F-WKT-17 | La demo (b) necesita la fila de información de la barra de pasos, que es F-WKT-14 | Se especifica (AC-WKT-14-01 a -06). **Pendiente** (PO, al cerrar): sumar F-WKT-14 a "También toca" de la 4a en el ROADMAP §2 y §4 |
| ROADMAP §2 (demo de la 4a) y §6 (D-ART-01); FEATURE_INVENTORY Q-60 | Fuera de objetivo `#FF5F59` | `state.offTarget.hud` es `#FF625B` desde la It. 3 (QA-V2-12; ART_BIBLE §2.3) | Esta spec usa `#FF625B`. **Pendiente** (PO): corregir el ROADMAP y FEATURE_INVENTORY |
| workout-engine-hud F-WKT-03, F-WKT-08 y F-WKT-11 | Expansión en `routine.dart:210-251`, posiciones en `:255-278`, resumen en `:281-286` | `routine.dart` tiene 138 líneas: expansión `:29-70`, posiciones `:74-97`, resumen `:100-105` (como routines-models) | **Pendiente** (PO): corregir el inventario |
| workout-engine-hud F-WKT-14 punto 4 | Pulso "en un ciclo de 600 ms de ida y vuelta" | 600 ms de ida y 600 de vuelta (`AnimationController(600 ms)..repeat(reverse: true)`, `immersive_workout_screen.dart:319-322`) | AC-WKT-14-04. **Pendiente** (PO): precisar el inventario |
| workout-engine-hud F-WKT-14 y F-WKT-03 | Sin meta: "no debería existir" | El editor lo permite (Q-25) y la cuenta del legacy muestra `nullm` (`:458`) | DV-25. **Pendiente** (PO): sumarlo al inventario |
| workout-engine-hud F-WKT-17 | Zonas sin superposición | En un teléfono horizontal la tarjeta kcal se encima con la de Watts: Split y Watts ocupan de 90 a 242 desde arriba y kcal termina a 140 del borde inferior (en 411 de alto, de 208 a 271) | DV-33. **Pendiente** (PO): sumarlo al inventario y a LEGACY_VISUAL_AUDIT §8 |
| workout-engine-hud F-WKT-12 | — | Dos toques rápidos pueden crear dos sesiones (`startFree()` y `startWithRoutine()` sin esperar, `workout_screen.dart:88-89`, `:164-165`) | DV-30. **Pendiente** (PO): sumarlo al inventario |
| workout-engine-hud F-WKT-10 punto 4 | "Un split 0 da blanco" | Con un objetivo positivo o 0; con un objetivo negativo (progresión negativa, que el editor permite: los topes de Q-92 van por valor absoluto) da rojo siempre, y con un objetivo 0 da rojo cualquier split mayor que 0 (`val > target`, `immersive_workout_screen.dart:581`). La primera versión de esta spec decía "≤ 0" (QA-G4a-P01) | AC-WKT-10-04. **Pendiente** (PO): precisar el inventario |
| workout-engine-hud F-WKT-02 | "Si el sistema demora o suspende la app, esos segundos no se recuperan" | Es el legacy; con Q-91 y el ADR 017 la app los procesa al volver | DV-27 |
| FEATURE_INVENTORY Q-55 | "El mismo saneamiento de saltos que los parciales (negativos o > 10 m/s)" | El saneamiento de los parciales también deja afuera lo remado en pausa y, ante un paquete aislado sin distancia, pierde los metros de esa lectura y la siguiente (`series_tracker.dart:118-127`) | AC-WKT-03-03, DV-26, P-04. **Pendiente** (PO): precisar Q-55 |
| ART_BIBLE §3.3 | Chip `Up next` "con el texto actual del legacy hasta que Q-01 lo localice" | Esta spec lo localiza (`Próximo:`, DV-24) | **Pendiente** (Designer) |
| ARCHITECTURE, tabla de plataforma (pantalla siempre encendida) | Windows: `ES_CONTINUOUS \| ES_DISPLAY_REQUIRED \| ES_SYSTEM_REQUIRED` | El legacy pide solo `ES_CONTINUOUS \| ES_DISPLAY_REQUIRED` (`wakelock_plus_windows_plugin.dart:23`). El criterio es el que espera el usuario: con la app abierta, ni se apaga la pantalla ni se suspende el equipo (AC-APP-14-01) | Sin acción |
| workout-engine-hud F-WKT-13 punto 7, app-shell F-APP-14 | Pantalla encendida "en toda la app" | En Windows también con la ventana minimizada (el pedido es del proceso); en Android, mientras la app está adelante (`FLAG_KEEP_SCREEN_ON` de la ventana, `Wakelock.kt:24`) | AC-APP-14-02 |
| STATUS (deuda para la 4a) | R3-08: "el spec de la 4a fija qué ve el usuario" | Fijado: AC-WKT-12-08 (DV-28) | **Pendiente** (PO, al cerrar): marcarlo en STATUS |
| 00-deuda-diseno §1.7 (alto máximo del panel) | "Alto de la ventana − borde de abajo − margen seguro superior − 16 … en las pestañas da la fórmula de siempre (360 → 248; 914 → 802)" | Con la llave a 80 dp del borde de abajo (barra de 64 + 16, DV-07), esa fórmula da 264 y 818. Los valores de la It. 2 salen de `alto − 96 − margen − 16` (`Assets/RowMate/Simulator/Panel/SimulatorPanelLayout.cs`), que deja 32 dp libres arriba | AC-BLE-16-03 fija los valores de la It. 2 en las pestañas y 64 dp más en el editor; en el entrenamiento, Split y Watts a la vista. **Pendiente** (Designer): precisar §1.7 |
| ROADMAP §6 (decisiones por gate) | Sin D-ART-10 | D-ART-10 se decide en el gate de la 4a (ART_BIBLE §0.2; 00-deuda-diseno §5) | §11, DV-39. **Pendiente** (PO, al cerrar): sumarla al ROADMAP §6, con su gate |

`git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después de escribir esta spec. No se leyó `lib/core/strava/strava_secrets.dart`.

---

## 16. Cambios por gate QA

Correcciones por los dos gates de armonía de QA de la 4a, del 2026-10-28: [lente de paridad](03-qa-harmony-paridad.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 3 mayores, 7 menores) y [lente técnica](03-qa-harmony-tecnica.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 5 mayores, 13 menores). Cada valor nuevo se volvió a leer en el legacy: `immersive_workout_screen.dart:40-219`, `:300-475` y `:560-887`; `workout_provider.dart` entero; `workout_screen.dart:1-205`; `lib/core/bluetooth/ble_service.dart:8`; `lib/features/device/device_provider.dart:88-98`; `lib/core/dev/simulator_overlay.dart:1-100`; `lib/core/database/database_service.dart:280-300`. También en la app: `Assets/RowMate/Core/Ble/BleStatus.cs` y `Assets/RowMate/Simulator/Panel/SimulatorPanelLayout.cs`. Se alinearon con [02-tech-design](02-tech-design.md) (§2.3, §2.6, §6.3, §6.6, §6.8, §6.10, §7.3, §10.1) y con [04-design-notes](04-design-notes.md) (§2.2, §3.1, §3.4, §3.9, §4.5, §4.7, §8, §9, §11) tal como estaban; ninguno de los dos había cambiado después de los gates.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-G4a-P01, QA-G4aT-08 | mayor, menor | Split con objetivo 0: "≤ 0" pasa a "< 0", como el legacy (`val > target`, `:581`). Con objetivo 0, `--:--` cumple y cualquier split mayor que 0 queda fuera; con un objetivo negativo, Split está siempre fuera. Casos: `--:--` contra 130, 0 y −5 → cumple, cumple y fuera; 125 contra 0 → fuera. Para el Architect: el comentario de §2.6 y estos 4 casos en `TLT.SplitAndNonPositiveTargets` | AC-WKT-10-04, -07; §15 |
| QA-G4a-P02 (y la parte del PO de QA-G4aT-04) | mayor | AC nuevo **AC-WKT-20-07**: si la rutina termina sola con el diálogo de terminar abierto, el diálogo se cierra sin efecto, como `Cancelar`, en el cuadro en que la fase pasa a terminada, y sigue AC-WKT-03-05; un `Terminar` del diálogo que llega después no cambia nada (la sesión se cierra una vez y cuenta como terminada sola) y uno que llega antes es un terminar a mano. Vale también en segundo plano. Nunca queda un diálogo del inmersivo sobre Workout en reposo. F-WKT-20 pasa a 7. Para el Architect: cerrar el diálogo antes del `Pop` y `HCT.RoutineEndsWithTheFinishDialogOpen`. La parte del panel del simulador, en AC-BLE-16-02 (fila de QA-G4aT-02) | §4.0, AC-WKT-20-03, -20-07, AC-WKT-13-04, AC-WKT-03-05; §8; §9 fila 75; §12 punto 13 |
| QA-G4a-P03 (y la DV de las pastillas de QA-G4aT-02) | mayor | **Se acepta** la propuesta del Designer como **DV-38**: los textos de las dos pastillas en blanco (`Próximo:` al 70 %), los dos tintes al 20 % y, en `Próximo`, laten solo el fondo y el borde, con el texto siempre entero. El motivo es el contraste (D-ART-01): los textos de color del legacy quedan en 3,5-4,9:1 y ≈ 3:1 en el punto tenue del latido; en blanco, ≥ 8,1:1 y `Próximo:` ≥ 4,9:1. El tipo en curso sigue en su color, y tintes y bordes conservan el color de cada paso. El usuario lo ve en el gate (P-15). Para el Architect: §6.6, §9.5 y las comprobaciones de CAP-21 (`Descanso` en blanco) y CAP-27c en §7.3. Para el Designer: quedan los valores de §4.5, §8.2 y §13, y sale la excepción del latido de §9.2 | AC-WKT-14-03, -04; §7.3 CAP-19, CAP-21, CAP-27; §9 fila 39; §11.1 DV-38; §14 P-15 |
| QA-G4a-P04 | menor | DV-31 suma las unidades `W` y `m`: en `.hud` al 80 %, también con objetivo; el legacy las pone en el color del valor al 55 % (con objetivo, blanco o `#FF4444`). DV-33 suma la fila de abajo en horizontal: kcal y BPM en fila a la altura de los controles (kcal en el rincón, BPM a su izquierda) y la banda compacta (14 y 10 en lugar de 20 y 16). Son los textos de 04-design-notes §11, verificados en `:676`, `:693`, `:709-733`, `:826` y `:877`. AC-WKT-17-03, -04 y -05 y AC-WKT-10-05 los dicen, y "donde lo ubique el Designer" sale de AC-WKT-17-05 | AC-WKT-10-05, AC-WKT-17-03, -04, -05; §7.3 CAP-15, CAP-17; §9 filas 48 a 50; §11.1 DV-31, DV-33 |
| QA-G4a-P07 | menor | "Sin remo" = **cualquier** estado distinto de conectado: apagado (Bluetooth apagado), buscando, conectando o desconectado (`device_provider.dart:94` sobre los cinco estados de `ble_service.dart:8`; `BleStatus.cs` tiene `Off`). Se prueba con los cuatro, en dotnet (la regla, WST) y en PlayMode (WSC) | AC-BLE-10-03; §8; §9 fila 74 |
| QA-G4a-P08 | menor | Si la rutina termina con la app en segundo plano, minimizada o sin foco, al volver el aviso del fin (`workoutCompleted`, o `workoutSaveError`) aparece **una sola vez**, con sus 4 s desde la vuelta; de lo que pasó mientras tanto se muestra solo el último aviso. Así el usuario se entera, como con el diálogo modal del legacy (`:207-212`), hasta que llegue el de la 4b. Para el Architect: cubrirlo en WBG y WBU | AC-WKT-02-06, -08, -09, AC-WKT-03-05; §9 fila 76; §12 punto 13; §14 P-13 |
| QA-G4a-P10 | menor | Con la rutina terminada, la cuenta muestra `0:00`, con restante 0 y progreso 1, solo si el último paso es por tiempo; por distancia sigue mostrando su largo, sin restante y con progreso 0 (`workout_provider.dart:31-36`, `:101-104`; `immersive_workout_screen.dart:456-458`) | AC-WKT-03-06, AC-WKT-04-07, AC-WKT-14-06; §9 fila 15 |
| QA-G4aT-02 | mayor | **D-ART-10 integrada**, con el ajuste del inmersivo de 04-design-notes §3.9 y §11. AC-BLE-16-02: la llave abajo a la izquierda fuera del entrenamiento; en el entrenamiento, sobre kcal y BPM en vertical y junto a `Pausa` (o encima, en L640) en horizontal; no tapa ninguna acción en reposo; los menús no se abren debajo; el panel la sigue si cambia de lugar (también al cerrarse solo el inmersivo). AC-BLE-16-03: el panel crece desde la llave; en las pestañas, el alto de la It. 2; en el editor, 64 dp más; en el entrenamiento, **Split y Watts a la vista** (se acepta el tope de Watts, que 02-tech-design §6.10 dejaba apagado esperando al PO). Se reabren AC-RUT-10-04 y -09, y se describen las tomas de las It. 2 y 3 que cambian. **DV-39** reemplaza a DV-13 y va al gate. §5 pasa de 12 a 15 criterios. La DV de las pastillas y la precisión de DV-31 y DV-33 están en las filas de P03 y P04 | §0.1; encabezado; §5; §7.1; §7.3 CAP-01, CAP-12, CAP-15 a CAP-17; §7.4 DEMO-03; §8; §9 fila 78; §11, §11.1 DV-39; §12 puntos 6 y 13; §14 P-12, P-14; §15 |
| QA-G4aT-16 | menor | (a) AC-WKT-02-02: el punto se toma también con el alta en vuelo, y se guarda cuando vuelve o se descarta si falló; con la base al día da lo mismo que el legacy. (b) AC-WKT-06-15: se guarda todo lo pendiente, también lo tomado con el alta en vuelo, con una espera de 1 s como mucho; con una base más lenta pueden faltar los últimos puntos si el sistema cierra la app. (c) AC-APP-14-01: `powercfg` muestra `RowMate.exe` en `DISPLAY` y `SYSTEM`; en lo versionado va solo eso, sin la ruta. (d) La parte de equipo de AC-WKT-02-08 es [Device-And] | AC-WKT-02-02, -02-08, AC-WKT-06-15, AC-APP-14-01; §7.1; §8 |
| Pedidos del Architect al PO (02-tech-design §10.1 y §10.4) | — | **DT4a-01** aceptado: el foco vuelve a quien abrió el diálogo solo con el teclado, que es la conducta del legacy en la PC. Se suma el orden de Tab del inmersivo (DN4a-10). **DT4a-04** aceptado: VID-01 de 17 s. **DT4a-11** es QA-G4aT-02. **DT4a-12** aceptado como **DV-37**: el editor deja guardar una duración negativa, el motor la termina en el primer segundo y el HUD muestra `0:00` en lugar de fallar | AC-APP-05-07; §7.5 VID-01; AC-WKT-03-07, AC-WKT-04-02; §9 fila 77; §11, §11.1 DV-37 |
| Alineación (partes del PO en issues de otros) | — | **QA-G4aT-03**: el tiempo en segundo plano no cuenta como demora del guardado; una sesión que se guardó bien no se da por fallada al volver, aunque sea 5 min después (AC-WKT-02-06; §12 punto 2). **QA-G4aT-06**: AC-WKT-06-15 acepta solo el tope de 1 s; los puntos con el alta en vuelo se guardan (la otra opción de QA). **QA-G4aT-13**: no se acepta una sesión cerrada con huecos; una sesión cerrada siempre tiene todos sus puntos y, si no entran, queda cortada (AC-WKT-06-12; P-16). **QA-G4a-P05**: el diálogo de terminar es el de la app, opaco, como el legacy (AC-WKT-20-03). **QA-G4a-P09 / QA-G4aT-17**: la vista "en curso" va sin la barra superior, como el legacy (AC-WKT-01-09); si el Designer prefiere conservarla, es una DV más. **QA-G4aT-12 (b)**: VID-02 cruza dos veces en menos de 4 s y el empujón sale solo en el primero, como esperan las notas de diseño (§7.5) | AC-WKT-01-09, -02-06, -06-12, -06-15, -20-03; §7.5 VID-02; §12; §14 P-16 |

**No se disputa ningún issue.**

**Conteo.** 147 criterios nuevos (suma AC-WKT-20-07) + 15 de iteraciones anteriores (§5 suma AC-BLE-16-03, AC-RUT-10-04 y AC-RUT-10-09) = **162**. DV-21 a DV-39: 19 diferencias, y DV-39 reemplaza a DV-13. Siguen igual las 30 capturas, las 14 tomas de la demo, los 3 videos (VID-01 pasa a 17 s), los 11 casos legacy y las 12 claves nuevas (202 en total).

**Para el Architect (derivado del gate).**

- **IDs y matriz.** `It04aIds` y la matriz de §8.4 pasan a 162 IDs, con AC-WKT-20-07, AC-BLE-16-03, AC-RUT-10-04 y AC-RUT-10-09.
- **Secciones del diseño.**
  - §2.6: el comentario del Split.
  - §6.6 y §9.5: las pastillas (DV-38).
  - §6.8: el diálogo opaco y su cierre cuando la rutina termina sola.
  - §6.10: D-ART-10 integrada, con el tope de Watts encendido y el panel que sigue a la llave.
  - §7.3: CAP-19, CAP-21, CAP-27c, VID-01 de 17 s y VID-02 con dos cruces.
- **Pruebas.**
  - WBG y WBU: el aviso del fin al volver, y el tope del cierre en tiempo real con la vuelta a los 330 s. El foco sale del ciclo de vida de prueba (§0.4): sin pedido, la app cuenta como adelante y con foco, también en las corridas sin ventana.
  - WST y WSC: los cuatro estados sin remo.
  - SRC: un alta lenta y el paso a segundo plano a los 6 s.
  - SSR: el cierre de una sesión con puntos de un lote que falló.
  - SPG: la duración negativa.
  - HCT: el diálogo abierto cuando la rutina termina sola.

**Para el Designer.**

- **QA-G4a-P09.** La spec deja la vista "en curso" sin barra superior. 04-design-notes §4.1 lo alinea, o propone la DV.
- **DV-38 aceptada.** Valen §4.5, §8.2 y §13; sale la excepción del latido de §9.2.
- **00-deuda-diseno §1.7.** En las pestañas, el alto máximo del panel es el de la It. 2 (§15).

`git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después de estas correcciones. No se leyó `lib/core/strava/strava_secrets.dart`.

---

## 17. Cambios al cerrar (PO)

Por la verificación de QA del 2026-10-28 ([05-qa-verification](05-qa-verification.md), rondas 1 y 2, con las lentes [de paridad](05-qa-lens-paridad.md) y [de robustez](05-qa-lens-robustez.md)); cierre del 2026-10-29. Resultado final: **156 PASS, 2 FAIL** (AC-WKT-17-04 y AC-APP-05-07, menores) **y 4 NO VERIFICABLE** (AC-WKT-02-10 y AC-APP-14-01 a -03). Estos cambios no mueven el resultado de ningún criterio. El cierre, con sus condiciones, está en el [REPORT](REPORT.md).

| Origen | Qué cambió | Dónde |
|---|---|---|
| DT4a-17 (02-tech-design §10.1, pedido al PO) | Se acepta la propuesta del Architect. Si el reintento del cierre recupera los puntos de un lote que falló, el aviso del final es el de un guardado bueno: `workoutCompleted` si la rutina terminó sola, ninguno al terminar a mano. `workoutSaveError` al final sale solo si la sesión no quedó cerrada con todos sus puntos, porque repetirlo con todo guardado sería falso. Lo prueba `WST.TransientFailureRecoveredAtTheClose` | AC-WKT-06-12 |
| QA-V4a-L01 (lente de paridad) | P-04 y DV-26 suman el monitor que no informa la distancia una vez por segundo, con un requisito de producto para la 10a: un paso por distancia nunca queda trabado si el monitor informa avance. Con el simulador no pasa | §11.1 DV-26; §14 P-04; [ROADMAP](../../ROADMAP.md) §2, It. 10a; [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) Q-55 |
| QA-V4a-ROB-05 (pregunta al PO) | **P-17**, nueva: el tiempo con el equipo suspendido no cuenta como tiempo de sesión, como en el legacy. Va al gate y su criterio, a la 4b | §11; §14 P-17; [ROADMAP](../../ROADMAP.md) §2 (It. 4b) y §6 |
| QA-V4a-03 y QA-V4a-04 | Son las **condiciones del cierre**: se corrigen al empezar la 4b, antes de construir encima, y AC-WKT-17-04 y AC-APP-05-07 quedan reabiertos hasta que QA los vea pasar. QA-V4a-04 apareció también en el editor de Rutinas: un Enter con el foco invisible en `←` sale sin guardar. El editor sale sin confirmar como el legacy (`lib/features/routines/routine_editor_screen.dart` no tiene `PopScope`), así que se corrige el anillo y no se agrega una confirmación | [REPORT](REPORT.md) §5.1 y §9.2; [STATUS](../../STATUS.md) |
| Observaciones de QA (O-01 a O-05, O-R2-01) | Se aceptan sin cambios para la 4a. O-02 (con el ruido del simulador, a 160 W contra 160 la tarjeta alterna) es la regla sin margen del legacy. O-01 (la foto de reposo durante el fundido de salida) se revisa con la salida del inmersivo de la 4b. La llave dentro de la zona del bote en L640 (04-design-notes §3.9) se revisa cuando el bote se vea, en la It. 7 | [REPORT](REPORT.md) §8.2 |
| §15, pendientes del PO | Hechos. En el ROADMAP: F-WKT-14 en "También toca" de la 4a y en la matriz; `#FF625B` en la demo de la 4a y en D-ART-01; D-ART-10 en §6, con su gate. En FEATURE_INVENTORY, Q-55 y Q-60. En [workout-engine-hud](../../product/inventory/workout-engine-hud.md), las líneas de `routine.dart`, el pulso de 600 + 600 ms, el paso sin meta, el split con objetivo 0 o negativo, los dos toques y las zonas que se enciman (sección "Cambios al cerrar la It. 4a"). En STATUS, R3-08 resuelta. El Designer ya había hecho LEGACY_VISUAL_AUDIT §8 (hallazgo 17) y ART_BIBLE §3.3 (`Próximo:`); le quedan 00-deuda-diseno §1.6 y §1.7 | [ROADMAP](../../ROADMAP.md) §2, §4, §6 y §8; [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) §4 |

`git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después. Se releyeron `lib/core/models/routine.dart:25-106`, `lib/features/workout/immersive_workout_screen.dart:317-323`, `:451-460` y `:576-603`, y `lib/features/workout/workout_screen.dart:84-92` y `:160-170`. No se leyó `lib/core/strava/strava_secrets.dart`.
