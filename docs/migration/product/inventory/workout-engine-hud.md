# Inventario de producto: motor de entrenamiento, pantalla Workout (reposo) y HUD inmersivo

| Campo | Valor |
|---|---|
| Área | **WKT** (IDs `F-WKT-NN`) |
| Autor | rowmate-po |
| Fecha | 2026-10-27 |
| Fuente | `https://github.com/figuibej/rowmate-legacy` (solo lectura). El código manda sobre los docs; las diferencias están en la sección "Discrepancias". |
| Uso | Contrato de paridad. Lo que no esté acá se pierde en la migración. |

## Alcance

**Entra:** el motor que corre una sesión (fases, reloj de 1 s, avance de pasos, fin automático, fusión del pulso, muestreo de telemetría y guardado de la sesión), los parciales de 500 m, las repeticiones de serie, el cumplimiento de objetivos por paso, la pantalla "Workout" en reposo, la pantalla inmersiva (barra de pasos, barra de entrenamiento libre, tarjetas glass, banda de tiempo y distancia, paneles colapsables, reloj de pared, controles, diálogos de terminar y de rutina completada) y las reglas de subida a Strava que se disparan al terminar.

**No entra (otros inventarios):** el render de la escena 2.5D y el selector de escenario (SCN), el parseo FTMS y la conexión BLE (BLE), el servicio del pulsómetro (HRM), el modelo y el editor de rutinas (RUT), el historial y el detalle de sesión (HIS), el esquema de la base (DB), Strava y el perfil (STR/PRF), el simulador y su panel (SIM) y el shell de la app (APP). Acá solo se nombran como dependencias.

## Glosario

- **Paso:** un intervalo de la rutina, de tipo calentamiento, trabajo, descanso o enfriamiento. Se define por tiempo o por distancia y puede tener objetivos de vatios mínimos/máximos, SPM y split.
- **Serie:** un grupo de pasos que comparten grupo y se repiten N veces. Puede tener progresiones por repetición.
- **Rutina expandida:** la lista lineal de pasos que resulta de repetir cada serie N veces aplicando sus progresiones. Es la lista que recorre el motor y la que dibuja la barra de pasos.
- **Paso suelto:** un paso sin grupo.
- **Repetición (rep):** una pasada completa por los pasos de una serie.
- **Vuelta (lap):** cada 500 m completos remados dentro de un paso de trabajo.
- **Reloj activo:** los segundos que contó la app con la sesión en marcha. Las pausas no cuentan.
- **Datos en vivo:** el último paquete del monitor, con el pulso reemplazado por el del pulsómetro cuando lo hay.

## Mapa de funcionalidades

| ID | Funcionalidad | Lógica pura | Tests legacy |
|---|---|---|---|
| F-WKT-01 | Ciclo de vida y fases de la sesión | sí | — |
| F-WKT-02 | Reloj activo (tick de 1 s) | sí | — |
| F-WKT-03 | Avance de pasos y fin automático de la rutina | sí | `routine_positions_test` (indirecto) |
| F-WKT-04 | Estado del paso en curso (progreso, restante, "por terminar", "a continuación") | sí | — |
| F-WKT-05 | Fusión del pulso (pulsómetro por encima de FTMS) | sí | `workout_provider_hr_test` (4) |
| F-WKT-06 | Telemetría y guardado de la sesión | sí | `session_stats_test` (relacionado) |
| F-WKT-07 | Parciales de 500 m | sí | `series_tracker_test` (8) |
| F-WKT-08 | Repeticiones de la serie y comparación | sí | `series_tracker_test` (6), `routine_positions_test` |
| F-WKT-09 | Cumplimiento de objetivos por paso | sí | `step_target_tracker_test` (6) |
| F-WKT-10 | Semáforo de objetivos en vivo | sí | — |
| F-WKT-11 | Formatos de display | sí | `series_panels_test` ("formatos") |
| F-WKT-12 | Pantalla Workout en reposo | no | — |
| F-WKT-13 | Pantalla inmersiva: estructura, capas y navegación | no | — |
| F-WKT-14 | Barra de pasos de la rutina | no | — |
| F-WKT-15 | Píldora de paso: estados visuales | no | `step_pill_test` (1) |
| F-WKT-16 | Barra superior del entrenamiento libre y chip de fase | no | — |
| F-WKT-17 | HUD: tarjetas glass y banda de tiempo y distancia | no | — |
| F-WKT-18 | Paneles colapsables de parciales y repeticiones | no | `series_panels_test` (3) |
| F-WKT-19 | Reloj de pared | no | — |
| F-WKT-20 | Controles y confirmación de terminar | no | — |
| F-WKT-21 | Diálogo de rutina completada | no | — |
| F-WKT-22 | Reglas de subida a Strava al terminar | no | — |
| F-WKT-23 | Pantalla clásica y tarjeta de métrica (código muerto) | no | — |

## Constantes visuales de referencia

Se usan en varias funcionalidades. Fuente: `lib/shared/theme.dart:32-69`.

| Uso | Color |
|---|---|
| Split | `#FF9F1C` (ámbar) |
| SPM | `#00B4D8` (cian) |
| Vatios | `#FFD60A` (amarillo) |
| Distancia | `#4CC9F0` (celeste) |
| Pulso | `#EF476F` (rosa/rojo) |
| Tiempo | `#E0E0E0` (gris claro) |
| Calorías | `#06D6A0` (verde menta) |
| Paso de trabajo | `#EF476F` |
| Paso de descanso | `#06D6A0` |
| Paso de calentamiento | `#FFD166` |
| Paso de enfriamiento | `#118AB2` |
| Tipo de paso desconocido | `#607D8B` |
| Métrica fuera de objetivo | `#FF4444` |
| Acento de la app (ícono de remo, íconos del resumen) | `#00B4D8` |
| Fondo general de la app | `#0D1B2A`; tarjetas `#1A2E45` |

Los porcentajes de este documento ("negro 55 %") son opacidades.

---

## F-WKT-01: Ciclo de vida y fases de la sesión

**Valor para el usuario:** puede arrancar una sesión libre o con rutina, pausarla, reanudarla y terminarla. La app sabe siempre en qué estado está y nunca pierde la cuenta.

**Comportamientos observables**

1. Hay cuatro fases: `idle` (en reposo), `active` (en marcha), `paused` (pausada) y `finished` (terminada). Al abrir la app la fase es `idle`.
2. **Iniciar libre:** la fase pasa a `active`, se registra la hora de inicio, se vacía el buffer de telemetría, el contador de muestreo vuelve a 0 y se crea la fila de la sesión en la base, con rutina nula. El reloj de 1 s recién arranca cuando la base devuelve el ID de la sesión.
3. **Iniciar con rutina:** antes de pasar a `active`, el motor:
   - expande la rutina (F-WKT-03);
   - se ubica en el paso 0 con 0 s transcurridos;
   - toma la distancia actual del monitor como "distancia al inicio del paso";
   - guarda la posición de cada paso (grupo, número de rep, cantidad de reps);
   - reinicia los parciales, con base en la distancia actual del monitor y en el reloj activo;
   - reinicia el cumplimiento de objetivos.

   Después sigue igual que el inicio libre. La sesión guarda el ID y el nombre de la rutina original.
4. **Pausar:** la fase pasa a `paused` y el reloj se detiene. Mientras dura la pausa no avanzan el reloj total, el reloj del paso, el muestreo de telemetría, los parciales ni los objetivos. Los datos en vivo del monitor se siguen recibiendo y mostrando.
5. **Reanudar:** la fase vuelve a `active` y se arranca un reloj de 1 s nuevo, desde cero. La fracción de segundo que corría antes de la pausa se pierde. La distancia actual del monitor pasa a ser la referencia de los parciales: lo remado en pausa no cuenta para vueltas ni para metros de la repetición.
6. **Terminar** (manual o automático): la fase pasa a `finished`, el reloj se detiene y se guarda la sesión (F-WKT-06). La UI se entera del cambio recién cuando termina el guardado.
7. **Reset:** vuelve a `idle` y borra la rutina, el índice de paso, el reloj total y el del paso (a 0), el ID de la sesión, la hora de inicio, el buffer y las posiciones. Reinicia los parciales (base 0) y los objetivos. **No** borra los últimos datos en vivo ni `lastFinishedSessionId`.
8. Desde `finished` solo se sale con reset, que lleva a `idle`. No hay "deshacer terminar".
9. La pestaña Workout muestra la vista de reposo (F-WKT-12) solo en `idle`. En `active`, `paused` o `finished` muestra el texto centrado `workoutInProgress` en blanco 38 %, que queda tapado por la pantalla inmersiva.
10. Los datos en vivo del monitor y del pulsómetro llegan en todas las fases, también en `idle`.

**Casos borde y errores**

- Si falla la creación de la fila de la sesión al iniciar, el error no se maneja. La fase queda en `active` pero el reloj nunca arranca: la pantalla inmersiva se abre con el reloj congelado en 00:00 y no aparece ningún mensaje.
- Entre que se abre la pantalla inmersiva y el primer segundo pasa lo que tarda la base en crear la fila. La pantalla se abre sin esperar.
- Iniciar libre no limpia la rutina ni los relojes: confía en que antes hubo un reset. La UI solo deja iniciar desde `idle`, que siempre viene de un reset o del arranque de la app.
- Como el reset no borra los datos en vivo, una sesión nueva muestra los últimos valores del monitor hasta que llega el paquete siguiente.
- Una rutina siempre tiene al menos un paso expandido: el editor exige al menos un paso y repeticiones mayores que 0. El motor no contempla una rutina vacía.

**UI:** no tiene UI propia. La disparan F-WKT-12 (iniciar) y F-WKT-20/21 (pausar, reanudar, terminar, reset).

**Datos persistidos:** fila en `workout_sessions` (detalle en F-WKT-06).

**Archivos legacy:** `lib/features/workout/workout_provider.dart:13` (fases), `:114-149` (iniciar), `:239-250` (pausar/reanudar), `:252-303` (terminar), `:305-318` (reset); `lib/features/workout/workout_screen.dart:19-35`, `:178-201` (qué muestra la pestaña según la fase).

**Tests legacy:** ninguno directo. Hay que escribir criterios de aceptación nuevos.

**Dependencias:** F-WKT-02, F-WKT-03, F-WKT-06, F-WKT-07, F-WKT-09; RUT (rutina y expansión); BLE (datos en vivo); HRM (pulso); DB (sesiones).

**Mejora EXR:** cada cambio de estado tiene que sentirse físico y claro. Al pausar, el bote se desliza hasta frenar y el HUD se cubre con un velo tenue con un "PAUSA" bien legible. Al reanudar, el remero retoma la palada con naturalidad. Al terminar, la cámara se aleja con un movimiento cinematográfico mientras aparece el resumen. Ninguna animación puede adelantar, demorar ni frenar el reloj.

---

## F-WKT-02: Reloj activo (tick de 1 s)

**Valor para el usuario:** el tiempo de la sesión refleja solo lo que remó, sin contar las pausas. Todas las reglas de pasos se evalúan con un ritmo fijo y predecible.

**Comportamientos observables**

1. Hay un tick por segundo, solo en `active`. Un tick que llega fuera de `active` se ignora.
2. Cada tick suma 1 al reloj del paso, al reloj total y al contador de muestreo, en ese orden, antes de cualquier evaluación.
3. Orden fijo dentro del tick:
   1. incrementar los relojes;
   2. si el contador de muestreo es múltiplo de 5 y la sesión tiene ID, agregar un punto de telemetría (F-WKT-06);
   3. si hay rutina, alimentar los parciales con el paso actual (F-WKT-07/08), alimentar los objetivos con el paso actual (F-WKT-09) y evaluar si terminó el paso (F-WKT-03);
   4. avisar a la UI.
4. El reloj que muestran el HUD y el resumen es el reloj total activo, con formato `H:MM:SS` desde 1 h y `MM:SS` por debajo (F-WKT-11).
5. Este reloj no es el tiempo que informa el monitor. El del monitor se usa para guardar la sesión (F-WKT-06).

**Casos borde y errores**

- El reloj cuenta disparos del temporizador, no diferencias de hora. Si el sistema demora o suspende la app, esos segundos no se recuperan.
- En cada pausa se pierde la fracción de segundo que estaba corriendo (F-WKT-01, punto 5).

**UI:** se ve en la banda de tiempo del HUD (F-WKT-17) y en el resumen (F-WKT-21).

**Datos persistidos:** ninguno directo. El contador de muestreo define cuándo se guarda cada punto.

**Archivos legacy:** `workout_provider.dart:147`, `:151-190`, `:247`; `immersive_workout_screen.dart:569-577` (formato).

**Tests legacy:** ninguno.

**Dependencias:** F-WKT-01, F-WKT-03, F-WKT-06, F-WKT-07, F-WKT-09.

**Mejora EXR:** dígitos de ancho fijo que no "bailan" al cambiar, con una transición suave de cada dígito. El tiempo se lee de un vistazo a 2 o 3 m de la pantalla.

---

## F-WKT-03: Avance de pasos y fin automático de la rutina

**Valor para el usuario:** la rutina avanza sola de paso en paso, por tiempo o por distancia, y termina sola. El remero no toca nada mientras rema.

**Comportamientos observables**

1. El motor recorre la **rutina expandida**: cada serie aparece repetida tantas veces como indique su cantidad de repeticiones, y en la repetición r (empezando en 0) los objetivos con progresión valen:
   - SPM objetivo + progresión de SPM · r;
   - vatios mínimos + progresión de vatios · r, y lo mismo para vatios máximos;
   - split objetivo + progresión de split · r.

   Un objetivo solo progresa si existen el valor base y la progresión. Los pasos sueltos quedan como están. El nombre y el ID de la rutina no cambian.
2. **Paso por tiempo:** termina cuando el reloj del paso llega a la duración (≥). Si un paso tiene tiempo y distancia, manda el tiempo.
3. **Paso por distancia:** termina cuando la distancia del monitor menos la distancia que tenía al empezar el paso llega a los metros del paso (≥). Usa la distancia cruda del monitor, que incluye lo remado en pausa.
4. La evaluación solo ocurre en el tick, cada 1 s. Un paso por distancia puede cerrar hasta 1 s después de cruzar la marca. Los metros de más quedan en el paso que termina: el siguiente arranca desde la distancia del monitor en ese tick.
5. Al terminar un paso, primero se cierra su evaluación de objetivos (F-WKT-09). Si hay un paso siguiente, el índice sube en 1, el reloj del paso vuelve a 0 y la distancia de inicio pasa a ser la distancia actual del monitor.
6. Al terminar el **último** paso, la fase pasa a `finished` en el acto, para que otro tick no vuelva a evaluar. El reloj se detiene y el guardado arranca sin esperar a que termine. La UI muestra el diálogo de rutina completada (F-WKT-21).
7. Con la rutina terminada, el índice queda en el último paso (no avanza) y el reloj del paso queda en la duración del paso.
8. Un paso de 0 s termina en el primer tick.
9. El entrenamiento libre no tiene pasos ni fin automático: solo termina a mano.

**Casos borde y errores**

- Un paquete FTMS sin el dato de distancia llega como distancia 0. En ese tick el recorrido da negativo y el paso no termina; al tick siguiente se corrige solo.
- Si el monitor se reinicia a mitad de un paso por distancia, la distancia vuelve a 0 y el paso no termina hasta que el monitor supere la distancia que tenía al empezar el paso. Acá no hay saneamiento, a diferencia de los parciales (F-WKT-07). En la app el paso usa el saneamiento de los parciales (Q-55; It. 4a, DV-26).
- Si el monitor se desconecta, los datos quedan congelados en el último paquete. Los pasos por tiempo siguen avanzando; los de distancia quedan detenidos.
- Un paso sin tiempo ni distancia (sin meta) no termina nunca solo: la sesión se termina con `Terminar`. El editor lo permite (Q-25), y la cuenta regresiva del inmersivo muestra `nullm` (`immersive_workout_screen.dart:456-458`). En la app se ve `?`, como en el editor (It. 4a, DV-25).
- Un paso de duración negativa (el editor deja escribir un `Min` negativo y los topes de Q-92 van por valor absoluto) también termina en el primer tick, pero mientras dura el inmersivo falla: el restante usa `clamp(0, d)` con `d < 0`, que lanza (`workout_provider.dart:103`). En la app la cuenta muestra `0:00` (It. 4a, DV-37).

**UI:** se ve en la barra de pasos (F-WKT-14), en el semáforo (F-WKT-10) y en el diálogo final (F-WKT-21).

**Datos persistidos:** el índice y el tipo del paso viajan en cada punto de telemetría (F-WKT-06).

**Archivos legacy:** `lib/core/models/routine.dart:29-70` (expansión con progresiones), `:74-97` (posiciones); `workout_provider.dart:120-131`, `:175-187`, `:192-218`.

**Tests legacy:** `test/routine_positions_test.dart` ("las posiciones quedan alineadas con flattenedSteps"). Con un calentamiento suelto, una serie ×3 de (trabajo 500 m + enfriamiento 60 s) y un enfriamiento suelto, afirma que:

- hay 8 posiciones, tantas como pasos expandidos;
- la 0 es (sin grupo, 1, 1);
- las 1 y 2 son (a, 1, 3);
- la 3 es (a, 2, 3);
- la 6 es (a, 3, 3);
- la 7 es (sin grupo, 1, 1).

Ningún test cubre las progresiones ni el fin de paso.

**Dependencias:** RUT (modelo y expansión), BLE (distancia), F-WKT-01, F-WKT-02, F-WKT-09, F-WKT-21.

**Mejora EXR:** cada transición entre pasos tiene que ser inconfundible:

- una cuenta 3-2-1 visual y sonora al final de cada paso por tiempo;
- una onda del color del paso nuevo que recorre la barra;
- el nombre del paso nuevo grande en el centro durante 1 o 2 s, por ejemplo "TRABAJO · 1000 m · >180 W";
- al terminar la rutina, una celebración sobria, al estilo de un cruce de línea de llegada.

---

## F-WKT-04: Estado del paso en curso (progreso, restante, "por terminar", "a continuación")

**Valor para el usuario:** el remero sabe cuánto le falta del paso y cuándo se viene el siguiente.

**Comportamientos observables**

1. El estado del paso existe solo con rutina y con la fase distinta de `idle`, es decir, también en `paused` y `finished`. Sin rutina no existe.
2. Contiene: el paso (con los objetivos ya progresados), su índice (desde 0), la cantidad total de pasos expandidos, los segundos transcurridos en el paso y los segundos restantes.
3. **Restante:** en un paso por tiempo es la duración menos lo transcurrido, acotado entre 0 y la duración. En un paso por distancia vale −1, que significa "sin restante en tiempo".
4. **Progreso** (de 0 a 1): solo en pasos por tiempo con duración mayor que 0, y vale transcurrido / duración, acotado. En pasos por distancia vale siempre 0: no hay avance visual.
5. **Por terminar:** el restante está entre 1 y 5 s inclusive. Con 0 o −1 no aplica, así que solo pasa en pasos por tiempo.
6. **A continuación:** el restante está entre 0 y 15 s y existe un paso siguiente. Solo pasa en pasos por tiempo.

**Casos borde y errores**

- En un paso que no es el último, el restante 0 no se llega a mostrar: en el mismo tick en que llega a 0 el motor ya pasó al paso siguiente.
- Con la rutina terminada, el último paso queda con restante 0 (la cuenta muestra `0:00`), progreso 1 y sin "por terminar".
- Los pasos por distancia nunca muestran progreso, "por terminar" ni "a continuación".

**UI:** lo usan F-WKT-14 y F-WKT-15.

**Datos persistidos:** ninguno.

**Archivos legacy:** `workout_provider.dart:16-42`, `:98-112`; `immersive_workout_screen.dart:342-346`.

**Tests legacy:** ninguno.

**Dependencias:** F-WKT-03.

**Mejora EXR:** un avance continuo y suave entre segundo y segundo. En los últimos 5 s, un tic audible por segundo y un realce del paso. En pasos por distancia, la experiencia ideal sería ver los metros que faltan (ver Preguntas abiertas: hoy no se muestran).

---

## F-WKT-05: Fusión del pulso (pulsómetro por encima de FTMS)

**Valor para el usuario:** si tiene pulsómetro, el pulso que ve y que se guarda es el del sensor, que es más confiable que el del monitor, y se actualiza aunque el remo no mande datos.

**Comportamientos observables**

1. Mientras el pulsómetro no haya mandado ningún bpm (sensor no conectado), el pulso es el que trae cada paquete FTMS.
2. Cada bpm del pulsómetro reemplaza el pulso al instante y avisa a la UI, aunque no llegue ningún paquete del remo. El resto de las métricas no cambia.
3. Mientras haya bpm del sensor, cada paquete FTMS nuevo se toma completo salvo el pulso, que sigue siendo el del sensor.
4. Un bpm 0 del sensor (sin contacto) también manda: el pulso queda en 0 y la tarjeta BPM se oculta (F-WKT-17).
5. Si el sensor pasa a cualquier estado distinto de "conectado" (buscando, conectando o desconectado) después de haber mandado algún bpm, el motor olvida el bpm, pone el pulso en 0 y avisa a la UI. El paquete FTMS siguiente repone su propio valor.
6. Un cambio de estado del sensor sin ningún bpm previo no toca los datos ni avisa a la UI.
7. Un bpm recibido mientras el sensor está "conectando" se conserva cuando pasa a "conectado".
8. La fusión funciona en todas las fases. Todo lo que viene después (telemetría, HUD, sesión, TCX, gráficos) usa este pulso fusionado.

**Casos borde y errores:** el orden real de una caída del sensor es primero un bpm 0 y después "desconectado". El pulso queda en 0 hasta el paquete FTMS siguiente.

**UI:** la tarjeta BPM del HUD (F-WKT-17).

**Datos persistidos:** `data_points.heart_rate` (F-WKT-06).

**Archivos legacy:** `workout_provider.dart:68-70`, `:82-86`, `:220-237`; `lib/core/bluetooth/heart_rate_service.dart:10` (estados del sensor: buscando, conectando, conectado, desconectado).

**Tests legacy:** `test/workout_provider_hr_test.dart`.

- **"el pulsómetro manda sobre el FTMS y se actualiza sin paquetes del remo"**, paso por paso:
  - un FTMS con 100 W y 90 bpm da pulso 90;
  - un bpm 142 del sensor da pulso 142 con los vatios todavía en 100, después de exactamente 2 avisos a la UI;
  - un FTMS nuevo con 120 W y 90 bpm da pulso 142 y 120 W;
  - un bpm 0 da pulso 0;
  - un bpm 130 seguido de "desconectado" da pulso 0;
  - un FTMS con 90 bpm da pulso 90.
- **"orden real de caída: bpm 0 y después disconnected"**: un FTMS con 90 y un sensor con 140 dan 140; un 0 seguido de "desconectado" da 0; el FTMS siguiente con 90 da 90.
- **"un bpm recibido durante connecting se conserva al pasar a connected"**: "conectando", después un bpm 120 y después "conectado" dan 120; un FTMS con 90 deja el pulso en 120.
- **"connected del sensor sin bpm previo no toca los datos"**: "conectado" seguido de "desconectado", sin bpm, no genera ningún aviso a la UI.

**Dependencias:** HRM (bpm y estados), BLE (paquetes FTMS), F-WKT-06, F-WKT-17.

**Mejora EXR:** el corazón de la tarjeta late al ritmo real del pulso. La tarjeta aparece y desaparece con un fundido, no de golpe. Sería deseable un indicador claro de "sensor sin contacto" en lugar de que la tarjeta simplemente desaparezca (agregado, ver Preguntas abiertas).

---

## F-WKT-06: Telemetría y guardado de la sesión

**Valor para el usuario:** cada sesión queda en el historial con sus totales y su curva de datos, lista para ver o subir a Strava.

**Comportamientos observables**

1. **Al iniciar** se inserta una fila en `workout_sessions` con:
   - `routine_id` y `routine_name` de la rutina original (nulos en libre);
   - `started_at` = ahora, guardado en UTC con formato ISO-8601;
   - los totales en 0 y `finished_at` nulo.
2. **Cada 5 s activos** (ticks 5, 10, 15…, sin contar pausas) se agrega un punto a un buffer en memoria, con estas columnas:

   | Columna | Valor |
   |---|---|
   | `elapsed_seconds` | tiempo que informa el **monitor**, no el reloj de la app |
   | `stroke_rate` | SPM |
   | `stroke_count` | paladas |
   | `distance_meters` | distancia |
   | `pace_500m_seconds` | split |
   | `power_watts` | vatios |
   | `calories` | calorías |
   | `heart_rate` | pulso fusionado (F-WKT-05) |
   | `step_index` | índice expandido, desde 0; nulo en libre |
   | `step_type` | `warmup`, `work`, `rest` o `cooldown`; nulo en libre |

3. El punto de cada tick se toma antes de evaluar el fin de paso, así que pertenece al paso que estaba en curso.
4. **Al terminar** se agrega un último punto con los datos del momento y el paso final. Si el índice quedara fuera de rango, el tipo va nulo.
5. Sobre todos los puntos se calcula:
   - el percentil 99 de vatios, de SPM y de split, contando solo valores mayores que 0;
   - el máximo de distancia, el máximo de tiempo del monitor y el máximo de calorías;
   - el pulso promedio (redondeado) y el máximo, contando solo pulsos mayores que 0.

   El percentil 99 es el valor en la posición `floor((n − 1) · 0,99)` de la lista ordenada de menor a mayor. Sin valores da 0.
6. Todos los puntos se insertan en `data_points` en lote, **antes** de actualizar la sesión.
7. Después se actualiza la sesión:

   | Columna | Valor |
   |---|---|
   | `finished_at` | ahora, en UTC |
   | `total_distance_meters` | máximo de distancia |
   | `total_time_seconds` | máximo de tiempo del monitor |
   | `avg_power_watts` | percentil 99 de vatios, redondeado |
   | `avg_stroke_rate` | percentil 99 de SPM |
   | `total_calories` | máximo de calorías |
   | `strava_activity_id` | nulo |

   Las columnas "avg" guardan el percentil 99, no un promedio.
8. Si todo sale bien, `lastFinishedSessionId` pasa a ser el ID de esta sesión. Lo usan las reglas de Strava (F-WKT-22).
9. **Errores al guardar:** se registran en el log y el usuario no ve ningún mensaje. La sesión puede quedar incompleta y `lastFinishedSessionId` no cambia: sigue apuntando a la sesión anterior, si hubo una.
10. Los parciales, las repeticiones y el resultado de cada paso **no** se guardan.

**Casos borde y errores**

- Si la app se cierra o se cae a mitad de la sesión, la fila queda con `finished_at` nulo y totales en 0, y **sin puntos**: el buffer vivía en memoria.
- Si el monitor no informa tiempo transcurrido (ese campo FTMS es opcional y sin él llega 0), la sesión queda con tiempo total 0 aunque el HUD haya mostrado un reloj.
- La distancia y las calorías guardadas son los totales absolutos del monitor. Si el monitor no se reinicia entre dos sesiones, la segunda arrastra los valores de la primera.
- El tiempo guardado sale del monitor y puede no coincidir con el reloj activo del HUD, por ejemplo si el monitor sigue contando en pausa.
- Una sesión de menos de 5 s tiene un solo punto: el final.

**UI:** no tiene UI propia. Sin feedback de guardado.

**Datos persistidos:** tablas `workout_sessions` y `data_points` con las columnas de arriba. Esquema en `lib/core/database/database_service.dart:107-137` (inventario DB).

**Archivos legacy:** `workout_provider.dart:72-80`, `:133-149`, `:158-173`, `:252-303`; `lib/core/models/workout_session.dart:39-51` (columnas de la sesión), `:70-137` (estadísticas), `:140-183` (punto); `database_service.dart:216-229`, `:254-261`; `lib/core/bluetooth/ftms_parser.dart:61`, `:123` (distancia y tiempo son opcionales en FTMS).

**Tests legacy:** ninguno del guardado. Relacionado: `test/session_stats_test.dart` (inventario HIS/DB), con tres casos:

- "avgHeartRate y maxHeartRate ignoran los ceros": con 120, 150, 141 y ceros da promedio 137 y máximo 150, y "tiene pulso" es verdadero;
- "sin pulso quedan en 0": promedio y máximo 0, y "tiene pulso" es falso;
- "sin puntos quedan en 0": "tiene pulso" es falso.

**Dependencias:** DB, HIS (lee las sesiones), STR (sube por ID), BLE, F-WKT-02, F-WKT-05.

**Mejora EXR:** el guardado es invisible y confiable. El resumen final confirma con una marca discreta, por ejemplo "Sesión guardada", y si algo falla el usuario se entera con un mensaje claro y una opción de reintentar. Una sesión no debería perderse nunca, ni siquiera si la app se cierra (agregado de producto, ver Preguntas abiertas).

---

## F-WKT-07: Parciales de 500 m

**Valor para el usuario:** dentro de cada paso de trabajo ve cuánto tardó en cada 500 m y cómo viene la vuelta en curso, para mantener el ritmo sin frenar.

**Comportamientos observables**

1. Los parciales existen solo con rutina y solo en pasos de tipo **trabajo**. En el entrenamiento libre no hay parciales.
2. Una vuelta son 500 m completos remados dentro del paso de trabajo, contados desde el inicio de ese paso. Se numeran 1, 2, 3… dentro de cada paso y se reinician en cada paso nuevo.
3. Se alimentan una vez por segundo con la distancia del monitor y el reloj activo, sin pausas.
4. El instante en que se cruza cada marca se interpola linealmente entre la lectura anterior y la que la cruza: `t_cruce = t_anterior + (marca − d_anterior) / Δd · Δt`. El tiempo de la vuelta es `t_cruce` menos el final de la vuelta anterior (o el inicio del paso). Si una lectura cruza varias marcas, se generan varias vueltas.
5. **Vuelta en curso:**
   - metros = `floor(metros del paso − 500 · vueltas completas)`;
   - segundos = reloj activo − final de la última vuelta (o inicio del paso).

   Fuera de un paso de trabajo, metros y segundos valen 0.
6. **Saneamiento:** un salto de distancia negativo, o mayor que 10 m/s · Δt (con Δt de al menos 1 s), no suma metros, aunque el tiempo sí avanza. Esto cubre:
   - paquetes FTMS sin distancia, que llegan como 0;
   - reinicios del monitor;
   - el primer salto desde 0 al iniciar;
   - remar en pausa.
7. Al reanudar tras una pausa, la distancia actual del monitor pasa a ser la referencia nueva y el salto no se cuenta.
8. La distancia se toma relativa al inicio de la rutina: la base es la distancia del monitor en ese momento.
9. Un paso de trabajo donde se remaron menos de 500 m no genera vueltas. Esto no depende del largo configurado del paso: un paso por tiempo también genera vueltas.
10. Todo vive en memoria. Se reinicia al iniciar una rutina y en el reset.

**Casos borde y errores**

- Durante el primer segundo de un paso nuevo, hasta la primera lectura, el "es paso de trabajo" todavía refleja el paso anterior.
- Los metros de una lectura descartada se pierden. Por ejemplo, después de un 0 espurio, el salto 0 → 1004 se ignora entero.
- La distancia del monitor viene en metros enteros, pero el cálculo interno conserva fracciones gracias a la interpolación.

**UI:** el panel "Parciales 500 m" (F-WKT-18).

**Datos persistidos:** ninguno.

**Archivos legacy:** `lib/features/workout/series_tracker.dart:5-13` (vuelta), `:39-78` (constantes, vuelta en curso), `:86-108` (inicio y rebase), `:111-145` (lectura), `:147-182` (entrada a un paso); `workout_provider.dart:57-58`, `:128`, `:178-184`, `:248`, `:315`.

**Tests legacy:** `test/series_tracker_test.dart`. Los casos con velocidad constante simulan lecturas de 1 s:

- **"interpola el tiempo de la vuelta entre lecturas":** a 3 m/s durante 170 s en un trabajo de 1000 m da 1 vuelta (número 1) de 166,67 ± 0,01 s. La vuelta en curso tiene 10 m y 3,33 ± 0,01 s, y "es paso de trabajo" es verdadero.
- **"dos vueltas dentro del mismo paso":** a 5 m/s durante 220 s da vueltas de [100, 100] s.
- **"un paso de trabajo de menos de 500 m no genera vueltas":** a 5 m/s durante 80 s (400 m) no hay vueltas.
- **"el enfriamiento no genera vueltas pero suma metros a la repetición":** después de un enfriamiento, "es paso de trabajo" es falso, no hay vueltas y la vuelta en curso tiene 0 m (parte de repeticiones en F-WKT-08).
- **"una lectura de 0 dentro del paso no genera metros negativos ni dobles":**
  - se rema a 4 m/s durante 250 s, llega una lectura 0 y se sigue de 1004 a 1100;
  - la vuelta en curso y los metros de la repetición nunca dan negativo;
  - quedan 2 vueltas de 125 ± 2 s cada una;
  - al pasar a la rep 2, la rep anterior tiene entre 1060 y 1110 m de trabajo.
- **"un primer salto desde 0 no crea vueltas falsas":** si la primera lectura es 5000, no hay vueltas. Después de 140 lecturas de +4 m hay 1 vuelta de 125 ± 2 s.
- **"un reinicio del monitor no genera valores negativos":** se rema a 4 m/s durante 200 s, el monitor vuelve a 0 y sube de a 4 m. Los metros de la repetición nunca bajan, la vuelta en curso nunca da negativo y hay al menos una vuelta, la primera de 125 ± 2 s.
- **"rebase no cuenta el salto al reanudar":** 5 lecturas de +4 m dan 20 m en la repetición. Con una referencia nueva en 9000 y una lectura de 9004, la repetición queda en 24 m.

**Dependencias:** F-WKT-01, F-WKT-02, F-WKT-03; BLE (distancia); RUT (tipo de paso).

**Mejora EXR:** al cerrar cada vuelta aparece por 2 o 3 s un aviso breve con el tiempo de la vuelta y la diferencia contra la anterior: verde si fue más rápida, ámbar si fue más lenta. En el agua pasa una boya o marca de 500 m justo cuando se cruza, como en una cancha de regata.

---

## F-WKT-08: Repeticiones de la serie y comparación

**Valor para el usuario:** durante una serie sabe en qué repetición va, cuántos metros lleva en ella y cómo le fue en las últimas 3 repeticiones de la misma serie.

**Comportamientos observables**

1. Cada paso expandido tiene una posición: grupo, número de rep (desde 1) y cantidad de reps. Un paso suelto es (sin grupo, 1, 1).
2. La sesión está "en serie" cuando el paso actual tiene grupo.
3. **Metros de la repetición:** los metros saneados (F-WKT-07, punto 6) desde el inicio de la repetición, sumando todos sus pasos (trabajo, descanso, enfriamiento…), con `floor`.
4. Hay cambio de repetición cuando, al entrar a un paso nuevo, cambia el grupo o el número de rep.
5. Si la repetición que termina es del **mismo grupo** que la nueva, se guarda un resumen al frente de la lista, con la más reciente primero: rep, metros totales (`floor`), metros de trabajo (`floor`) y segundos de trabajo. La lista tiene **como máximo 3**; si se pasa, se descarta la más vieja.
6. Si se pasa a **otra serie** o a un **paso suelto**, la lista se vacía.
7. **Split de trabajo** de una repetición: segundos de trabajo / metros de trabajo · 500. Sin metros de trabajo no hay split y se muestra "—".
8. Los segundos de trabajo son la suma de Δt de las lecturas en pasos de trabajo, sin pausas. Los metros de trabajo son la suma de Δd saneados en esos mismos pasos.
9. La última repetición de una serie nunca aparece en la lista: al salir de la serie, la lista se vacía.
10. Todo vive en memoria. Se reinicia al iniciar una rutina y en el reset.

**Casos borde y errores:** dos series seguidas con grupos distintos no comparten la lista. Una serie que vuelve a aparecer más adelante con el mismo grupo no puede pasar, porque el editor agrupa los pasos contiguos.

**UI:** la línea "Rep r/R · N m" dentro del panel de parciales y el panel "Series anteriores" (F-WKT-18).

**Datos persistidos:** ninguno.

**Archivos legacy:** `routine.dart:186`, `:255-278`; `series_tracker.dart:16-34`, `:63-67`, `:80-84`, `:129-131`, `:147-174`.

**Tests legacy:** `test/series_tracker_test.dart`, con la serie g1 ×5:

- **"el enfriamiento no genera vueltas pero suma metros a la repetición":** trabajo de 500 m a 5 m/s durante 100 s y enfriamiento a 2 m/s durante 60 s. Afirma "en serie" verdadero, rep 1, 5 reps y 620 m en la repetición.
- **"al cambiar de repetición guarda el resumen de la anterior":** después de 10 s de la rep 2, la rep es 2 y lleva 50 m. Hay 1 resumen: rep 1, 620 m totales, 500 m de trabajo, 100 s de trabajo y split de 100 ± 0,001 s.
- **"guarda solo las últimas 3 repeticiones, la más reciente primero":** después de 5 reps, la rep actual es 5 y la lista es [4, 3, 2].
- **"una serie nueva o un paso suelto vacían la comparación":** después de la rep 2 de g1 hay 1 resumen. Al entrar a g2 la lista queda vacía. Al entrar a un paso suelto, "en serie" es falso y la lista sigue vacía.
- **"una repetición sin metros de trabajo no tiene split":** un resumen con 0 m de trabajo no tiene split.
- Del caso de hardening de F-WKT-07: los metros de trabajo de la rep anterior quedan entre 1060 y 1110.
- Además, `routine_positions_test` (ver F-WKT-03).

**Dependencias:** F-WKT-07 (misma fuente de distancia saneada), RUT (grupos y repeticiones), F-WKT-18.

**Mejora EXR:** que se sienta como regatear contra uno mismo. Sin leer números, el remero percibe si va mejor o peor que en la rep anterior, por ejemplo con una referencia de ritmo de la rep anterior en el agua o un indicador de adelanto o atraso en metros. Es un agregado: no reemplaza la lista.

---

## F-WKT-09: Cumplimiento de objetivos por paso

**Valor para el usuario:** al terminar cada paso sabe si cumplió el objetivo, y la barra de pasos lo muestra de un vistazo.

**Comportamientos observables**

1. Un paso **tiene objetivos** si define vatios mínimos, SPM o split. Los vatios máximos no cuentan.
2. En cada segundo activo se acumula la lectura del paso actual. Una lectura sin remar (SPM ≤ 0 **y** vatios ≤ 0) se ignora. El split solo se acumula cuando es mayor que 0.
3. El paso se evalúa cuando se **completa** (F-WKT-03), no cuando se corta con "Terminar":
   - sin objetivos, no hay resultado;
   - sin ninguna lectura remando, el resultado es **no cumplido**;
   - en el resto de los casos se comparan los promedios del paso: vatios ≥ mínimo, SPM ≥ objetivo y split ≤ objetivo. Si el monitor nunca informó split, ese objetivo no se evalúa.
   - El resultado es **cumplido** solo si se cumplen todos los objetivos definidos.
4. Hay tres resultados posibles: cumplido, no cumplido o sin resultado (sin objetivos o todavía no completado).
5. Se reinicia al iniciar una rutina y en el reset.
6. Vive solo en memoria: no va al historial.

**Casos borde y errores**

- Una lectura con SPM mayor que 0 y vatios en 0 sí cuenta, y baja el promedio de vatios.
- El paso final también se evalúa, pero su píldora nunca muestra el resultado: queda "en curso" (F-WKT-14).
- Un paso cortado con "Terminar" queda sin resultado.

**UI:** el estado "hecho" de cada píldora (F-WKT-15).

**Datos persistidos:** ninguno.

**Archivos legacy:** `lib/features/workout/step_target_tracker.dart:1-71` (tiene objetivos `:23-26`, lectura `:30-40`, cierre `:43-62`, resultado `:65`, reinicio `:67-70`); `workout_provider.dart:59-61`, `:129`, `:185`, `:204`, `:316`.

**Tests legacy:** `test/step_target_tracker_test.dart`:

- **"cumplido cuando el promedio de vatios alcanza el mínimo":** con 140 y 160 W y un objetivo de 150, el resultado es cumplido.
- **"no cumplido si falla cualquiera de los objetivos definidos":** con 200 W y 22 SPM, y objetivos de 150 W y 26 SPM, el resultado es no cumplido.
- **"el split se cumple por debajo del objetivo (más rápido)":** un split de 125 contra 130 se cumple; uno de 140 contra 130 no se cumple.
- **"un paso sin objetivos no tiene resultado":** el resultado es nulo.
- **"sin lecturas remando el objetivo no se cumple; los ceros no cuentan":** sin lecturas, el resultado es no cumplido. En otro paso llegan una lectura toda en 0 y otra de 120 W, 24 SPM y split 0, con objetivos de 100 W y split 200. El resultado es cumplido: el 0 de estar parado no baja el promedio y el split sin dato no se evalúa.
- **"un paso no completado no tiene resultado y reset borra todo":** antes de completar, el resultado es nulo; después de completar es cumplido; después del reset vuelve a ser nulo.

**Dependencias:** F-WKT-02, F-WKT-03, F-WKT-15; RUT (objetivos y progresiones).

**Mejora EXR:** al cerrar un paso con objetivo aparece durante 2 s un sello breve: "Objetivo cumplido" en verde o "No alcanzado" en ámbar, con el promedio logrado contra el objetivo (por ejemplo "176 W / 180 W"). La píldora se "solidifica" o queda "hueca" con una animación.

---

## F-WKT-10: Semáforo de objetivos en vivo

**Valor para el usuario:** mientras rema, ve al instante si está por debajo del objetivo de vatios o de SPM, o por encima del split objetivo.

**Comportamientos observables**

1. El semáforo aplica solo a las tarjetas de SPM, Split y Vatios, y solo cuando el paso actual define ese objetivo.
2. **SPM:** rojo `#FF4444` si el SPM actual es menor que el objetivo; blanco si no.
3. **Vatios:** rojo si los vatios actuales son menores que los vatios mínimos; blanco si no. Los vatios máximos se ignoran.
4. **Split:** rojo si el split actual es mayor (más lento) que el objetivo; blanco si no (`val > target`, sin casos especiales: `immersive_workout_screen.dart:579-583`). Un split 0 (sin dato, que se ve `--:--`) da blanco con un objetivo positivo o 0. Con un objetivo 0, cualquier split mayor que 0 da rojo, y con un objetivo negativo da rojo siempre, también `--:--`. Esos objetivos los deja una progresión negativa, que los topes de Q-92 permiten porque van por valor absoluto (It. 4a, AC-WKT-10-04).
5. Sin objetivo, la tarjeta usa su color propio de métrica (ver "Constantes visuales").
6. Con objetivo, la tarjeta suma un borde del color de estado al 70 % de 1,5 px y un halo del mismo color al 20 %, con desenfoque 12. Sin objetivo el borde es blanco al 10 %, de 1 px, y no hay halo.
7. Se evalúa con cada lectura, sin promedios ni margen de tolerancia.

**Casos borde y errores**

- Parado (SPM 0 y 0 W), las tarjetas con objetivo se ponen rojas.
- El split en `--:--` queda blanco.
- En el entrenamiento libre el semáforo nunca aplica.
- El objetivo se lee del paso expandido, así que ya incluye la progresión de la repetición.

**UI:** las tarjetas glass (F-WKT-17).

**Datos persistidos:** ninguno.

**Archivos legacy:** `immersive_workout_screen.dart:579-602`, `:771-783`; la misma regla en la pantalla vieja: `workout_screen.dart:733-760`.

**Tests legacy:** ninguno.

**Dependencias:** F-WKT-03, F-WKT-04, F-WKT-17.

**Mejora EXR:** el color cambia con una transición corta y suave, sin parpadeo, sin tocar el umbral, que sigue siendo instantáneo. Debajo del valor se ve el objetivo en chico (por ejemplo "obj. 26"), para que el remero sepa cuánto le falta.

---

## F-WKT-11: Formatos de display

**Valor para el usuario:** los números se leen igual siempre, con formatos de remo reconocibles.

**Comportamientos observables**

1. **Split en vivo:** `MM:SS` con los minutos en dos dígitos (`02:13`). Si el split es ≤ 0 se ve `--:--`.
2. **SPM:** un decimal con punto (`24.1`, `0.0`), sea cual sea el idioma.
3. **Vatios:** entero, con la unidad "W" aparte.
4. **Distancia en el HUD:** entero sin separador de miles (`1484`), con la unidad "m" aparte.
5. **Calorías y pulso:** enteros.
6. **Reloj de la sesión** (HUD y resumen): `H:MM:SS` desde 1 h (`1:02:05`) y `MM:SS` por debajo (`02:55`).
7. **Cuenta regresiva del paso por tiempo:** `M:SS` con los minutos sin cero a la izquierda (`1:05`, `0:05`, `12:00`).
8. **Paso por distancia** en la cuenta regresiva: `{metros}m`, sin espacio (`500m`). Muestra los metros totales del paso, no los que faltan.
9. **Tiempo de vuelta:** `M:SS.d`, redondeado a décimas (`2:46.7`, `1:05.0`, `0:55.0`).
10. **Split de una repetición:** `M:SS`, redondeado al segundo (`1:54`). Sin dato se ve `—`.
11. **Metros en los paneles:** separador de miles con punto, siempre al estilo español aunque la app esté en inglés, y " m" (`1.245 m`, `300 m`).
12. **Hora:** `HH:mm` en 24 h.
13. **Etiqueta de objetivo del paso:** se arma con estas partes, unidas por `" · "`; sin objetivos queda vacía.
    - Vatios: `"{min}-{max}W"` si hay mínimo y máximo, `">{min}W"` si solo hay mínimo. Solo máximo no muestra nada.
    - SPM: `"{spm} spm"`.
    - Split: `"MM:SS/500m"` con minutos en dos dígitos (`02:05/500m`).
    - Progresiones: `"📈 "` seguido de las que no sean 0, separadas por `", "`: `SPM +2/rep`, `W +10/rep`, `Split -2s/rep`. Los valores positivos llevan "+".
14. **Resumen de una rutina** (lista de reposo y diálogo de inicio): `"{n} series · {m} descansos"`, donde n son los pasos de trabajo y m los de descanso de la definición, sin expandir. Si no hay pasos de trabajo es `"{total} pasos"`. El texto está fijo en español.
15. **Resumen final:** distancia como `"{n} m"` sin separador de miles; calorías como número sin unidad.

**Casos borde y errores:**

- Una vuelta de 59,96 s redondea a `1:00.0`: el redondeo es sobre las décimas totales.
- Con la app en inglés, la etiqueta de objetivo y el resumen de rutina siguen en español ("spm", "series", "descansos", "pasos").

**UI:** se usa en F-WKT-12 a F-WKT-21.

**Datos persistidos:** ninguno.

**Archivos legacy:** `lib/core/models/rowing_data.dart:24-29`; `lib/core/models/interval_step.dart:79-109`; `routine.dart:100-105`; `lib/features/workout/series_panels.dart:9-24`, `:98`; `immersive_workout_screen.dart:456-458`, `:569-577`, `:965-971`.

**Tests legacy:** `test/series_panels_test.dart`, caso **"formatos"**: `formatLapTime(166.667)` da `2:46.7`; `formatLapTime(65)` da `1:05.0`; `formatSplit(114.4)` da `1:54`; `formatMeters(1245)` da `1.245 m`.

**Dependencias:** RUT (etiqueta de objetivo y resumen de rutina).

**Mejora EXR:** números de ancho fijo, grandes y de alto contraste sobre cualquier cielo, legibles desde la posición de remo. Los formatos se mantienen idénticos; lo que mejora es la presentación.

---

## F-WKT-12: Pantalla Workout en reposo

**Valor para el usuario:** desde acá elige el escenario y arranca una sesión libre o una rutina, con un aviso claro si el remo no está conectado.

**Comportamientos observables**

1. Es la segunda pestaña de la navegación inferior. Se ve solo en la fase `idle`.
2. La barra superior muestra el título `workoutTitle` ("Entrenamiento" / "Workout").
3. **Sin remo conectado** aparece un banner arriba: fondo naranja al 15 %, radio 12, padding 12 y margen inferior 16, con un ícono de advertencia naranja de 18 y el texto `workoutRowerNotConnected` en naranja de 13.
4. Debajo va la etiqueta `workoutScene` ("Escenario" / "Scenery") en blanco 54 % de 13, y el selector de escenarios (inventario SCN): 4 tarjetas de 120×80 con la elegida resaltada. La elección se guarda en `scene.environment`.
5. El botón principal es `workoutFree` ("Entrenamiento libre" / "Free workout"), con ícono de play. Está habilitado **solo con el remo conectado**. Al tocarlo, inicia la sesión libre y abre la pantalla inmersiva en el acto, sin confirmación.
6. **Con rutinas** aparece la etiqueta `workoutOrPickRoutine`, que en el `.arb` empieza con dos espacios, y una lista de tarjetas, una por rutina:
   - ícono de pesa `#00B4D8`;
   - el nombre como título;
   - el resumen de la rutina (F-WKT-11, punto 14) como subtítulo de 12;
   - un chevron a la derecha.

   El orden es el que trae el proveedor de rutinas: la creada más recientemente primero.
7. Sin remo conectado, las tarjetas de rutina se ven al 50 % y no responden al toque.
8. Al tocar una rutina se abre un diálogo de confirmación con:
   - el **nombre de la rutina** como título;
   - el texto `workoutStartContent(summary)`: "{resumen}\n\nIniciar el entrenamiento?" / "{summary}\n\nStart the workout?";
   - los botones `cancel` y `workoutStart` ("Iniciar" / "Start").

   Si se confirma, inicia la rutina y abre la pantalla inmersiva.
9. **Sin rutinas** se ve centrado el texto `workoutNoRoutines` en blanco 38 %.
10. Al mostrarse, la vista precarga el agua de la escena, para que la sesión arranque con el agua completa y no con la versión simplificada (inventario SCN).

**Casos borde y errores**

- No hay estado "cargando": mientras se cargan las rutinas se ve "sin rutinas" y después aparece la lista.
- No hay estado de error.
- La conexión solo se controla al iniciar. Si el remo se desconecta durante la sesión, la sesión sigue (F-WKT-03).
- La clave `workoutStartTitle` existe en los `.arb` pero no se usa: el título del diálogo es el nombre de la rutina.
- Tocar afuera del diálogo equivale a cancelar.
- Dos toques rápidos en `workoutFree` o en `workoutStart` pueden crear dos sesiones y abrir dos pantallas inmersivas: `startFree()` y `startWithRoutine()` no se esperan (`workout_screen.dart:88-89`, `:164-165`). En la app, un toque crea una sesión (It. 4a, DV-30).

**UI:** tres estados (desconectado, sin rutinas, con rutinas) que se combinan. Interacciones: elegir escenario, iniciar libre, elegir rutina y confirmar o cancelar.

**Datos persistidos:** `scene.environment` (inventario SCN). La vista no persiste nada propio.

**Archivos legacy:** `workout_screen.dart:39-176`; `lib/features/workout/scene/environment_picker.dart` (SCN); `lib/features/device/device_provider.dart:94` (conectado); `lib/main.dart:130`, `:143` (pestaña); `lib/l10n/app_es.arb:50-56`, `:66-75`.

**Tests legacy:** ninguno propio. `test/scene/environment_picker_test.dart` pertenece a SCN.

**Dependencias:** BLE/DEV (estado de conexión), RUT (lista y resumen), SCN (selector y precarga), F-WKT-01, F-WKT-13.

**Mejora EXR:** una "previa de regata". De fondo se ve en vivo el escenario elegido, con agua y luz reales. Cada tarjeta de rutina trae una vista previa de su estructura (mini barra de pasos con los colores de cada tipo) y la duración o distancia estimada. El botón de inicio es grande y claro. El aviso de remo desconectado lleva directo a conectarlo.

---

## F-WKT-13: Pantalla inmersiva: estructura, capas y navegación

**Valor para el usuario:** una pantalla completa que lo mete en el agua, con todas las métricas superpuestas sin tapar el bote.

**Comportamientos observables**

1. Se abre encima de toda la app al iniciar una sesión, libre o con rutina. Oculta las barras del sistema al crearse la página, cuando empieza su transición de entrada (`:34`), en modo inmersivo persistente: si se las revela con un gesto, se vuelven a ocultar solas. Las restaura al destruirse la página, cuando termina su salida (`:39`), en modo "de borde a borde", que queda así el resto de la sesión de uso.
2. Capas, de atrás hacia adelante:
   1. la escena 2.5D (SCN), con fondo negro;
   2. la barra superior, a `margen seguro superior + 8`: la barra de pasos (F-WKT-14) si hay rutina, o la barra libre (F-WKT-16) si no;
   3. el HUD (F-WKT-17);
   4. los controles (F-WKT-20), a `margen seguro inferior + 16`.
3. El botón o gesto de "atrás" del sistema no cierra la pantalla: abre la confirmación de terminar (F-WKT-20).
4. Cuando la sesión vuelve a `idle`, la pantalla se cierra sola y vuelve a la pestaña Workout.
5. Cuando la sesión pasa a `finished`, se muestra el diálogo de rutina completada (F-WKT-21) **una sola vez** por sesión.
6. La escena recibe los datos en vivo y se mueve solo en `active`. En pausa o terminada, el bote desacelera hasta frenar (SCN).
7. La pantalla del dispositivo no se apaga. Ojo: en el legacy queda encendida **en toda la app**, no solo en la sesión (ver Discrepancias).
8. La pantalla inmersiva no muestra el nombre de la rutina.

**Casos borde y errores**

- **Terminar a mano:** el guardado se espera y enseguida viene el reset. Normalmente no aparece el diálogo de rutina completada. Pero si durante el guardado llega un paquete del monitor, la pantalla puede llegar a mostrar el diálogo un instante antes de cerrarse.
- **Strava en modo "preguntar" al terminar a mano:** el reset dispara el cierre automático, que cierra lo que esté más arriba. Ese puede ser el diálogo de Strava y no la pantalla. Probablemente el diálogo se cierra solo y la pantalla se cierra con el paquete siguiente (ver Preguntas abiertas).
- Si el remo se desconecta durante la sesión, no hay ningún aviso en la pantalla inmersiva: los valores quedan congelados.

**UI:** pantalla completa sin estados de carga ni de error.

**Datos persistidos:** ninguno.

**Archivos legacy:** `immersive_workout_screen.dart:21-121` (subida y bajada del modo inmersivo `:34`, `:39`; diálogo final `:50-55`; cierre automático `:58-65`; atrás `:67-71`; capas `:80-116`); `workout_screen.dart:169-175` (apertura); `lib/main.dart:42` (pantalla siempre encendida); `lib/features/workout/scene/scene_state.dart:41-65` (la escena solo avanza en `active`, SCN).

**Tests legacy:** ninguno.

**Dependencias:** SCN, F-WKT-01, F-WKT-14, F-WKT-16, F-WKT-17, F-WKT-20, F-WKT-21, APP (pantalla siempre encendida).

**Mejora EXR:** entrada cinematográfica, con la cámara que baja hasta el agua detrás del bote y el HUD que aparece en cascada. Al salir, un fundido. Un aviso visible y elegante si se pierde la conexión con el remo, y otro cuando vuelve (agregado, ver Preguntas abiertas).

---

## F-WKT-14: Barra de pasos de la rutina

**Valor para el usuario:** de un vistazo ve la estructura completa de la rutina, en qué paso va, cuánto le falta, qué viene y la hora.

**Comportamientos observables**

1. **Contenedor:** margen horizontal 12, fondo negro 55 %, radio 14, borde blanco 8 %.
2. **Fila de píldoras** (padding 10/8/10/4):
   - una píldora por paso expandido;
   - **todas del mismo ancho**, sin importar la duración;
   - 3 px de separación;
   - estado de cada una: hecha (con el resultado de F-WKT-09) si su índice es menor que el actual, en curso (con el progreso de F-WKT-04) si es el actual y pendiente si es mayor (F-WKT-15).
3. **Fila de información** (padding 12/0/12/8), de izquierda a derecha:
   1. el tipo del paso localizado (`stepTypeWork`, `stepTypeRest`, `stepTypeWarmup`, `stepTypeCooldown`), en el color del tipo, tamaño 13, peso 700 y tracking 0,5;
   2. si el paso tiene etiqueta de objetivo (F-WKT-11, punto 13), a 6 px, un chip con fondo del color del tipo al 20 %, radio 6, padding 6×2 y texto de 11 peso 600. Va en un `Flexible` que se reparte el espacio libre por mitades con el espacio flexible del punto 3: recibe **a lo sumo la mitad**, se recorta por carácter con "…" y nunca se oculta. Por eso, con chip, el grupo de la derecha (a continuación, la cuenta y el reloj) no queda pegado al borde y se corre cuando entra "a continuación"; sin chip, sí queda pegado (`:394-416`);
   3. un espacio flexible;
   4. el chip "a continuación", cuando aplica;
   5. 8 px;
   6. la cuenta regresiva;
   7. 10 px;
   8. el reloj de pared, sin marco (F-WKT-19).
4. **Chip "a continuación":**
   - aparece con el restante entre 0 y 15 s y un paso siguiente (F-WKT-04);
   - texto fijo `"Up next: "` en blanco 60 % de 10 (en inglés en los dos idiomas), seguido del tipo del paso siguiente localizado, en su color, de 10 y en negrita;
   - fondo del color del paso siguiente al 25 %, borde al 50 %, radio 8, padding 7×3;
   - la opacidad pulsa entre 0,6 y 1,0: 600 ms de ida y 600 de vuelta (`AnimationController(600 ms)..repeat(reverse: true)`, `:319-322`).
5. **Cuenta regresiva:** formato de F-WKT-11, puntos 7 y 8. Normalmente es blanca, de 18 y peso 800. **Por terminar** (de 1 a 5 s) pasa a tamaño 20 y su color oscila entre blanco y el color del paso, al mismo ritmo de 600 + 600 ms. Un paso sin meta muestra `nullm` (`:456-458`; en la app, `?`: DV-25).
6. **Píldora en curso por terminar:** suma un halo del color del paso que pulsa entre 30 % y 70 % de opacidad, con desenfoque 8, expansión 2 y radio 5. Es la sombra de un contenedor que envuelve la píldora, así que también se pinta **debajo** de ella: se ve a través de la pista al 30 %, que se aclara con el pulso, y se extiende ≈ 14 dp, por debajo de la fila de información (`step_pill.dart:82-101`).
7. No muestra "paso N de M" ni el nombre de la rutina.
8. Con la rutina terminada, la barra sigue visible: el último paso queda "en curso" con la píldora llena y la cuenta en `0:00`.
9. En pausa, la barra queda congelada; los pulsos siguen si el paso estaba por terminar.

**Casos borde y errores**

- Con muchos pasos (por ejemplo 10 × 2 + 2 = 22) las píldoras quedan muy finas, pero no se recortan ni se desplazan. Eso vale hasta que las separaciones de 3 px se comen el ancho: con más de ≈ 120 pasos en un teléfono de 411 (≈ 410 en una ventana de 1280), cada píldora mide 0 y la fila queda vacía; solo se ve el halo de la en curso (`:361-375`).
- Los pasos por distancia nunca muestran relleno ni "a continuación", y la cuenta muestra su largo total (por ejemplo `1000m`).

**UI:** ver arriba. No tiene interacciones: la barra no responde al toque.

**Datos persistidos:** ninguno.

**Archivos legacy:** `immersive_workout_screen.dart:303-482` (pulso de 600 ms `:319-322`; "a continuación" `:343-346`, `:418-450`; contenedor `:348-354`; píldoras `:359-377`; información `:380-477`; cuenta regresiva `:453-471`; reloj `:473-474`); `lib/shared/theme.dart:42-69`.

**Tests legacy:** ninguno de la barra completa. Las píldoras están en F-WKT-15.

**Dependencias:** F-WKT-04, F-WKT-09, F-WKT-11, F-WKT-15, F-WKT-19; RUT.

**Mejora EXR:** una línea de tiempo de nivel profesional:

- la píldora en curso avanza de forma continua;
- al cambiar de paso, la barra "pasa la posta" con un desplazamiento suave;
- en los últimos 3 s aparece una cuenta grande y centrada, discreta, que no tapa el bote;
- el anuncio "a continuación" se lee como "Próximo: Descanso 1:00", con duración, como mejora sobre el texto actual.

Los cambios de texto son cambios de producto: ver Preguntas abiertas.

---

## F-WKT-15: Píldora de paso: estados visuales

**Valor para el usuario:** cada paso de la barra dice si ya pasó, si se cumplió, si está en curso o si falta.

**Comportamientos observables**

1. El matiz es siempre el color del tipo de paso (trabajo `#EF476F`, descanso `#06D6A0`, calentamiento `#FFD166`, enfriamiento `#118AB2`, otro `#607D8B`). La píldora mide 8 de alto con radio 4.
2. **Pendiente:** fondo del color al 18 %, sin borde y sin relleno. Es una silueta tenue que deja ver la estructura.
3. **En curso:** pista del color al 30 % más un relleno sólido (100 %) cuyo ancho es el progreso (0..1, acotado). Con progreso 0 no hay relleno. Sin borde.
4. **Hecha, con el objetivo cumplido o sin objetivo:** sólida, color al 85 %, sin borde.
5. **Hecha, con el objetivo no cumplido:** "hueca", con interior al 18 % (igual que pendiente) y borde de 1,5 px del color al 85 %.
6. Puede llevar un pulso opcional en los últimos segundos (F-WKT-14, punto 6).
7. Lectura del código visual: sólido = cumplido, hueco = intentado y no cumplido, tenue = falta, llenándose = en curso.

**Casos borde y errores:** el último paso nunca llega a "hecha" (F-WKT-14, punto 8).

**UI:** dentro de la barra de pasos.

**Datos persistidos:** ninguno.

**Archivos legacy:** `lib/features/workout/step_pill.dart:6` (estados), `:30-32` (opacidades 0,18 / 0,30 / 0,85), `:43-55`, `:57-80` (pintado y relleno), `:82-101` (pulso).

**Tests legacy:** `test/step_pill_test.dart`, caso **"pendiente, en curso, hecho y no cumplido se pintan distinto"**. Afirma que:

- "hecha" tiene opacidad mayor que 0,8 y "hecha sin objetivo" es idéntica a "hecha cumplida";
- "pendiente" tiene opacidad menor que 0,3;
- el interior de "no cumplida" tiene la misma opacidad que "pendiente", con un borde de opacidad mayor que 0,8;
- "pendiente" y "hecha" no tienen borde;
- "en curso" tiene una opacidad mayor que "pendiente" y menor que "hecha";
- el relleno de "en curso" es la mitad del ancho con progreso 0,5, y "en curso" no tiene borde;
- "pendiente" no tiene relleno.

**Dependencias:** F-WKT-04, F-WKT-09, F-WKT-14.

**Mejora EXR:** el relleno avanza de forma continua y no a saltos de 1 s. Al completarse, la píldora "se solidifica" con un destello corto. Un paso no cumplido se dibuja como un trazo que rodea la píldora.

---

## F-WKT-16: Barra superior del entrenamiento libre y chip de fase

**Valor para el usuario:** en el entrenamiento libre ve qué está haciendo, la hora y si la sesión corre o está en pausa.

**Comportamientos observables**

1. **Contenedor:** margen horizontal 12, padding 14×8, fondo negro 50 %, radio 14, borde blanco 8 %.
2. **Contenido:** ícono de remo `#00B4D8` de 20, el texto fijo **"Entrenamiento Libre"** (con L mayúscula, blanco 16, peso 600), un espacio flexible, el reloj de pared sin marco, 10 px y el chip de fase.
3. **Chip de fase:**

   | Fase | Texto | Color |
   |---|---|---|
   | en marcha | "EN MARCHA" | `#06D6A0` |
   | pausada | "PAUSADO" | `#FFD166` |
   | terminada | "FINALIZADO" | `#EF476F` |
   | otra | "—" | blanco 38 % |

   Fondo del color al 15 %, borde al 50 %, radio 8, padding 8×3, texto de 11 peso 800 con tracking 0,8.
4. Todos los textos están fijos en español, aun con la app en inglés. Existe la clave `metricFreeLabel` ("Entrenamiento libre" / "Free workout"), pero esta barra no la usa.

**Casos borde y errores:** "FINALIZADO" casi no se llega a ver, porque terminar a mano lleva al reset de inmediato. Se ve, en cambio, si llega una lectura del monitor durante el guardado, porque la pantalla se redibuja con cada lectura. Y mientras la página sale, después del reset, se redibuja en reposo: el chip pasa a "—" (`:534`).

**UI:** sin interacciones.

**Datos persistidos:** ninguno.

**Archivos legacy:** `immersive_workout_screen.dart:491-550`.

**Tests legacy:** ninguno.

**Dependencias:** F-WKT-01, F-WKT-19.

**Mejora EXR:** el chip "EN MARCHA" lleva un punto "en vivo" que late con suavidad. En pausa no se atenúa nada del HUD, por el contraste (ART_BIBLE §3.2): la pausa la dicen el velo, `PAUSA`, el chip y `Reanudar`.

---

## F-WKT-17: HUD: tarjetas glass y banda de tiempo y distancia

**Valor para el usuario:** las métricas clave del remo flotan sobre la escena, grandes y en el mismo lugar siempre, sin tapar el bote.

**Comportamientos observables**

1. **Estilo glass común:** fondo negro 55 %, radio 14, padding 14×10 y borde blanco 10 % de 1 px (con objetivo, F-WKT-10). La etiqueta va arriba, en el color de la tarjeta al 80 %, peso 600 y tracking 0,8. El valor va debajo, peso 800, alto de línea 1,0. La unidad, si hay, va al lado en el color al 55 %, peso 500.
2. **Tamaños:**

   | Tamaño | Valor | Etiqueta | Ancho mínimo |
   |---|---|---|---|
   | grande | 52 | 13 | 130 |
   | mediano | 36 | 12 | 110 |
   | chico | 28 | 11 | 90 |

3. **Arriba a la izquierda** (`margen seguro superior + 90`, izquierda 14): la tarjeta **SPM**, grande, con la etiqueta fija "SPM" y el valor con 1 decimal. Debajo, a 10 px, y solo con rutina, van los paneles de parciales y repeticiones (F-WKT-18). La columna tiene un límite inferior y, si los paneles no entran, se desplazan dentro de su espacio. El límite es:
   - en horizontal, `margen seguro inferior + 16 + 66 + 8`, es decir, 8 px sobre la banda de tiempo;
   - en vertical, `margen seguro inferior + 200`.
4. **Arriba a la derecha** (`margen seguro superior + 90`, derecha 14), alineadas a la derecha:
   - la tarjeta **"Split /500m"**, mediana, con el split en `MM:SS` o `--:--`;
   - a 8 px, la tarjeta **"Watts"**, mediana, con el valor entero y la unidad "W".
5. **Banda de tiempo y distancia** (abajo a la izquierda, a 14 de la izquierda, para no tapar el bote):
   - Estilo: cápsula de fondo negro 50 %, radio 40, borde blanco 10 % y padding 20×10.
   - Contenido: el **tiempo** (ícono de cronómetro de 16 al 70 % y el valor de 28 peso 800 en `#E0E0E0`), un divisor vertical de 1×32 blanco 12 % con 16 px de margen a cada lado y la **distancia** (ícono de regla, el valor entero de 28 en `#4CC9F0` y la unidad "m" de 13 al 55 %).
   - Altura: en vertical va a 140 px del borde inferior, sin sumar el margen seguro. En horizontal baja a `margen seguro inferior + 16`, al nivel de los controles.
6. **Abajo a la derecha** (140 px del borde inferior, derecha 14), alineadas a la derecha:
   - la tarjeta **"kcal"**, chica, en `#06D6A0`;
   - a 8 px, y **solo si el pulso es mayor que 0**, la tarjeta **"BPM"**, chica, en `#EF476F`, con un ícono de corazón del tamaño de la etiqueta.
7. La orientación es horizontal cuando el ancho es mayor que el alto.
8. Las etiquetas "SPM", "Split /500m", "Watts", "W", "kcal", "BPM" y "m" están fijas en el código y no se traducen.
9. Los valores se actualizan con cada paquete del monitor y con cada bpm, también en pausa. Lo único que se congela en pausa es el tiempo.
10. La distancia, las calorías y el resto de los valores en vivo son los totales que informa el monitor. La app no los pone en 0 al iniciar la sesión.

**Casos borde y errores**

- Sin pulsómetro y con un monitor que no manda pulso, la tarjeta BPM no aparece.
- Con un bpm 0, la tarjeta desaparece.
- Los paquetes FTMS que no traen algún campo lo muestran como 0 hasta el paquete siguiente. Puede verse un parpadeo de distancia o split en monitores que reparten los datos en varios paquetes (BLE).
- En pantallas de 360 dp, los paneles expandidos tienen un ancho fijo de 200 para no invadir la columna de Split y Watts (F-WKT-18).
- **Zonas que se enciman en un teléfono horizontal.** En 914 × 411 y 640 × 360 la tarjeta kcal se encima con la de Watts: Split y Watts ocupan de 90 a 242 desde arriba y kcal termina a 140 del borde inferior (en 411 de alto, de 208 a 271). En 640 × 360 la banda se encima además con los controles (calculado sobre `immersive_workout_screen.dart:639-663`, `:709-733`). En la app, en horizontal, kcal y BPM van en fila a la altura de los controles y la banda es compacta (It. 4a, DV-33; LEGACY_VISUAL_AUDIT §8, hallazgo 17).

**UI:** HUD superpuesto sin interacciones, salvo los paneles.

**Datos persistidos:** ninguno.

**Archivos legacy:** `immersive_workout_screen.dart:556-737` (diseño), `:739-837` (tarjeta glass y tamaños `:762-766`), `:839-887` (métrica de la banda); referencia visual: `rowmate-legacy/docs/image-03.png`.

**Tests legacy:** ninguno.

**Dependencias:** F-WKT-05, F-WKT-10, F-WKT-11, F-WKT-18; BLE (datos en vivo).

**Mejora EXR:** vidrio de verdad, con el paisaje desenfocado detrás de cada tarjeta y un brillo sutil en el borde. Los números cambian con transiciones cortas, sin parpadeos. El contraste es automático, para leer igual a pleno sol que de noche. La disposición respeta siempre la silueta del bote y del remero.

---

## F-WKT-18: Paneles colapsables de parciales y repeticiones

**Valor para el usuario:** puede abrir, cuando quiere, el detalle de vueltas de 500 m y de repeticiones anteriores sin ensuciar la pantalla. La app recuerda su preferencia.

**Comportamientos observables**

1. Se muestran solo con rutina, debajo de la tarjeta SPM. Arriba va el panel de **parciales** y, a 8 px, el de **repeticiones**, que aparece **solo si la sesión está en serie** (F-WKT-08).
2. **Estilo:** el mismo glass que las tarjetas (negro 55 %, radio 14, borde blanco 10 %, padding 14×10), con un ancho mínimo de 130 (igual que la tarjeta SPM). Expandido, el ancho queda fijo en 200 para no invadir la columna de Split y Watts en teléfonos de 360 dp.
3. **Colapsado** es un chip con un ícono de 14 al 80 %, el texto corto en el color del panel con estilo de etiqueta (13, peso 600, tracking 0,8) y una flecha "expandir" de 18 en blanco 54 %.
   - Parciales: ícono de cronómetro, texto **"500 m"**, color `#FF9F1C`.
   - Repeticiones: ícono de historial, texto **"Reps"**, color `#4CC9F0`.
4. **Expandido** muestra una cabecera con el ícono, el título (se recorta con "…") y una flecha "contraer". A 6 px va el contenido.
   - Parciales: título **"Parciales 500 m"**.
   - Repeticiones: título **"Series anteriores"**.
5. Un toque en cualquier parte del panel alterna entre colapsado y expandido, y la elección se guarda al instante.
6. **Contenido de parciales:**
   - **En un paso que no es de trabajo:** el texto "Sin parciales en este paso", en blanco 54 % de 15.
   - **En un paso de trabajo:**
     1. la vuelta en curso, con el tiempo de la vuelta (`M:SS.d`, de 30, peso 800, en `#FF9F1C`, en una sola línea) y al lado sus metros como `"{n} m"` (14 al 55 %);
     2. debajo, a 4 px, las **últimas 4 vueltas, de la más reciente a la más vieja**, una fila por vuelta: etiqueta `#n` (blanco 60 %, 15, peso 600, en una columna de 52 px) y el tiempo `M:SS.d` (22, peso 800, ámbar).
   - **Si está en serie**, a 6 px y aunque el paso no sea de trabajo: `"Rep {r}/{R} · {metros de la repetición}"` con metros al estilo español (por ejemplo `Rep 2/3 · 207 m`), de 16, peso 800, en `#4CC9F0`.
7. **Contenido de repeticiones:**
   - Sin repeticiones completas: "Sin repeticiones completas", en blanco 54 % de 15.
   - Con repeticiones: una fila por cada una (hasta 3, la más reciente primero) con la etiqueta `"Rep {n}"`, el split de trabajo `M:SS` o `—` (22, ámbar) y, a la derecha, los metros totales al estilo español (16, celeste). Por ejemplo: `Rep 1 · 2:13 · 452 m`, en columnas y sin los puntos medios.
8. **Preferencias:** `immersive.lapsExpanded` e `immersive.repsExpanded`, booleanas. Por defecto (sin clave) los dos paneles arrancan **colapsados**. Si el usuario toca un panel antes de que termine de leerse la preferencia, gana lo que eligió.
9. Todos los textos de los paneles están fijos en español ("500 m", "Reps", "Parciales 500 m", "Series anteriores", "Sin parciales en este paso", "Sin repeticiones completas", "Rep").

**Casos borde y errores**

- Con poco alto disponible, el contenido se desplaza dentro de la columna izquierda (F-WKT-17, punto 3).
- Al pasar de un paso de trabajo a uno de descanso, la lista de vueltas desaparece y se ve el texto "Sin parciales en este paso".

**UI:** colapsado y expandido por panel; vacío ("Sin parciales…" / "Sin repeticiones…") y con datos.

**Datos persistidos:** las preferencias `immersive.lapsExpanded` e `immersive.repsExpanded`.

**Archivos legacy:** `series_panels.dart:28-55` (estilos), `:115-197` (paneles, claves `:120-121`, lectura `:140-151`, guardado `:153-156`, condición de serie `:178`), `:199-269` (panel colapsable, anchos `:225`, `:227`), `:271-321` (parciales), `:323-348` (repeticiones), `:351-392` (fila); `immersive_workout_screen.dart:106`, `:630-633`.

**Tests legacy:** `test/series_panels_test.dart`, con un tracker a 300 m en el trabajo de la rep 1 de 3:

- **"arrancan colapsados y al tocar se expanden y se recuerda":**
  - sin preferencias no se ven "Parciales 500 m" ni "Series anteriores", y se ven "500 m" y "Reps";
  - al tocar "500 m" aparecen "Parciales 500 m" y "Rep 1/3 · 300 m", y `immersive.lapsExpanded` queda en verdadero;
  - al tocar "Reps" aparece "Sin repeticiones completas".
- **"respeta la preferencia guardada":** con `immersive.lapsExpanded` en verdadero, arranca mostrando "Parciales 500 m".
- **"sin serie no aparece el panel de repeticiones":** sin grupo se ve "500 m" y no se ve "Reps".

**Dependencias:** F-WKT-07, F-WKT-08, F-WKT-11, F-WKT-17.

**Mejora EXR:** la apertura y el cierre son una animación suave de altura y opacidad, sin saltos. Cada vuelta nueva entra arriba con un deslizamiento corto y un realce que se apaga. En una mejora futura, cada fila podría colorear la diferencia contra la anterior (más rápida o más lenta), sin cambiar los datos que se muestran.

---

## F-WKT-19: Reloj de pared

**Valor para el usuario:** ve la hora sin salir de la sesión.

**Comportamientos observables**

1. Muestra la hora local en `HH:mm` de 24 h, con un ícono de reloj de 16 en blanco 70 %, 5 px de separación y el texto blanco de 18, peso 800, con números de ancho fijo. El tamaño es el mismo que el de la cuenta regresiva.
2. Consulta la hora cada segundo pero solo se redibuja cuando cambia la hora o el minuto.
3. Tiene su propio ritmo: sigue andando en pausa y en la fase terminada.
4. Se usa **sin marco** dentro de la barra de pasos y de la barra libre. Existe una variante con marco (chip glass de padding 14×8) que no se usa en ninguna pantalla.

**Casos borde y errores:** el cambio de horario de verano y el cambio de día se reflejan en el minuto siguiente.

**UI:** sin interacciones.

**Datos persistidos:** ninguno.

**Archivos legacy:** `series_panels.dart:59-111`; usos en `immersive_workout_screen.dart:474`, `:515`.

**Tests legacy:** ninguno.

**Dependencias:** F-WKT-14, F-WKT-16.

**Mejora EXR:** tipografía limpia y consistente con el resto del HUD. La hora también conduce la luz de la escena (SCN), y esa coherencia entre reloj y cielo es parte de la sensación de realismo.

---

## F-WKT-20: Controles y confirmación de terminar

**Valor para el usuario:** pausar, reanudar y terminar con botones grandes. Terminar pide confirmación para no cortar la sesión por error.

**Comportamientos observables**

1. Los controles van en una fila centrada a `margen seguro inferior + 16`, con padding horizontal de 24:
   - **"Terminar":** botón de contorno rojo de 1,2 px y texto rojo de 15, con fondo negro 45 %, mínimo 120×48 y radio 12;
   - 16 px de separación;
   - **"Pausa"** (en marcha): contorno blanco 24 % de 1,2 px, fondo negro 45 %, ícono de pausa, mínimo 140×48 y radio 12;
   - **"Reanudar"** (en pausa): relleno `#06D6A0` con texto negro, ícono de play, mínimo 140×48 y radio 12.
2. Pausa y Reanudar actúan al instante, sin confirmación.
3. "Terminar" y el "atrás" del sistema abren un diálogo con:
   - título **"Terminar entrenamiento"**;
   - texto **"Se guardará la sesión. ¿Continuar?"**;
   - botones **"Cancelar"** y **"Terminar"** (relleno).

   Tocar afuera o "Cancelar" cierra el diálogo sin cambios, y la sesión sigue en la fase en que estaba.
4. Al confirmar:
   1. se espera el guardado (F-WKT-06);
   2. si Strava está configurado, se aplican las reglas de F-WKT-22;
   3. viene el reset, la pantalla inmersiva se cierra y la app vuelve a la pestaña Workout en reposo.

   **No hay resumen** al terminar a mano.
5. El paso en curso al terminar queda sin resultado de objetivo (F-WKT-09).
6. Los textos de los controles y del diálogo están fijos en español: "Terminar", "Pausa", "Reanudar", "Terminar entrenamiento", "Se guardará la sesión. ¿Continuar?" y "Cancelar". Existen las claves `workoutFinish`, `workoutPause`, `workoutResume`, `workoutFinishTitle`, `workoutFinishContent` y `cancel` con los mismos textos en español, pero la pantalla inmersiva no las usa.

**Casos borde y errores**

- Se puede terminar desde la pausa.
- Mientras se guarda (entre la confirmación y el cierre) no hay indicador de progreso.
- En la fase terminada, los controles quedan detrás del diálogo de rutina completada, que es modal.

**UI:** dos variantes según la fase (en marcha o pausada) y el diálogo de confirmación.

**Datos persistidos:** indirectos, a través de F-WKT-06.

**Archivos legacy:** `immersive_workout_screen.dart:111-116`, `:125-148` (confirmación), `:893-953` (controles); `lib/l10n/app_es.arb:61-65`, `:122`.

**Tests legacy:** ninguno.

**Dependencias:** F-WKT-01, F-WKT-06, F-WKT-13, F-WKT-22.

**Mejora EXR:** botones grandes, fáciles de tocar con las manos transpiradas, con respuesta táctil al tocar. La confirmación de terminar sigue el lenguaje glass del HUD. Mientras se guarda, una animación breve de "guardando". Al pausar, el velo de pausa de F-WKT-01.

---

## F-WKT-21: Diálogo de rutina completada

**Valor para el usuario:** al terminar la rutina recibe un cierre con el tiempo, la distancia y las calorías, y si corresponde la opción de subir a Strava.

**Comportamientos observables**

1. Aparece una sola vez, cuando la sesión llega a la fase terminada **y** el diálogo todavía no se mostró. En la práctica pasa solo cuando la rutina termina sola (F-WKT-03); ver el caso borde de F-WKT-13.
2. Es modal: no se cierra tocando afuera, ni con "atrás", ni con Escape en escritorio (`barrierDismissible: false` y `PopScope(canPop: false)`, `:207-212`).
3. **Título:** un ícono de check verde `#06D6A0` de 28 y el texto `workoutCompleted` ("¡Entrenamiento completado!" / "Workout Completed!").
4. **Texto:** `workoutCompletedMessage` ("Has completado tu rutina exitosamente." / "You have successfully completed your routine.").
5. **Resumen:** un recuadro de fondo blanco al 4 % (alfa 10 de 255), radio 8 y padding 12, con tres filas separadas 8 px. Cada fila tiene un ícono `#00B4D8` de 20, la etiqueta en blanco 70 % y el valor en negrita de 16:
   - ícono `timer`, **"Tiempo"**: el reloj activo, con formato `H:MM:SS` o `MM:SS`;
   - ícono `straighten`, **"Distancia"**: la distancia del monitor, como `"{n} m"`;
   - ícono `local_fire_department`, **"Calorías"**: las calorías del monitor, sin unidad.

   Las etiquetas están fijas en español (`:981-991`). Los tres valores se leen **una sola vez**, cuando se arma el diálogo, un cuadro después del fin: ni las lecturas nuevas, ni rotar, ni cambiar el tamaño de la ventana los vuelven a leer, porque nada vuelve a armar el diálogo (`:50-54`, `:207-229`, `:965-991`). Si la rutina termina con la app en segundo plano en Android, el diálogo se arma recién al volver, con la lectura de ese momento.
6. **Botones:**
   - Si se cumple todo esto: Strava configurado, la app no está en modo simulador, el perfil está conectado y la preferencia de subida es **"preguntar"**, hay dos botones:
     - **`workoutFinish`** ("Terminar", con contorno): cierra el diálogo y hace el reset;
     - **`profileUploadToStrava`** ("Subir a Strava", relleno, con ícono de nube): cierra el diálogo, sube la sesión `lastFinishedSessionId` si existe y hace el reset. Al terminar la subida muestra un aviso: `profileUploaded` ("Subido a Strava") o `profileUploadFailed` ("Error al subir").
   - En cualquier otro caso hay un solo botón, **`workoutFinish`**, que cierra el diálogo. Si Strava está configurado, la app no está en simulador, el perfil está conectado y la preferencia es **"automática"**, muestra el aviso `profileUploadingToStrava` ("Subiendo a Strava..."), sube la sesión y muestra el aviso del resultado. Después hace el reset.
7. Después del reset, la pantalla inmersiva se cierra (F-WKT-13). Los avisos de Strava aparecen sobre la pantalla principal.

**Casos borde y errores**

- El guardado arranca sin esperar al terminar la rutina. Si el usuario toca un botón antes de que termine, `lastFinishedSessionId` puede seguir apuntando a la sesión **anterior**, o estar vacío, y la subida usaría ese valor (ver Preguntas abiertas).
- Ese mismo toque temprano también **pierde el cierre de la sesión**: el reset vacía el inicio de la sesión mientras el guardado espera a la base, el cierre falla en silencio y la sesión queda sin hora de fin ni totales (`workout_provider.dart:283-299`).
- La distancia y las calorías son los totales del monitor, no los guardados.
- El texto habla siempre de "rutina", lo que es correcto porque las sesiones libres no terminan solas.

**UI:** dos variantes de botones según la preferencia de Strava.

**Datos persistidos:** ninguno directo. La subida escribe `strava_activity_id` (STR).

**Archivos legacy:** `immersive_workout_screen.dart:49-55`, `:196-296`, `:959-1019`; `lib/core/dev/dev_config.dart:5` (modo simulador); `lib/core/strava/strava_config.dart:19-21` (configurado); `lib/l10n/app_es.arb:74-75`, `:203-207`.

**Tests legacy:** ninguno.

**Dependencias:** F-WKT-03, F-WKT-06, F-WKT-13, F-WKT-22; STR/PRF (preferencia, conexión, subida); SIM (modo simulador).

**Mejora EXR:** una pantalla de resultado con peso: la cámara se aleja del bote sobre el agua, los totales aparecen uno a uno con conteo animado y la rutina se ve resumida con sus píldoras finales. Subir a Strava se siente como un paso natural, con el estado de la subida visible en el mismo resumen. Los datos y las reglas son los mismos.

---

## F-WKT-22: Reglas de subida a Strava al terminar

**Valor para el usuario:** según su preferencia, la sesión se sube sola, se le pregunta o no se hace nada, y en modo simulador nunca se sube nada.

**Comportamientos observables**

1. **Condición general:** las reglas solo se evalúan con Strava configurado (credenciales reales, distintas del texto de ejemplo), la app fuera del modo simulador, el perfil conectado y un `lastFinishedSessionId` disponible. Si falta algo, no pasa nada.
2. **Al terminar a mano** (F-WKT-20), según la preferencia:
   - **automática:** aviso "Subiendo a Strava...", subida y aviso del resultado ("Subido a Strava" / "Error al subir");
   - **preguntar:** un diálogo con título `profileUploadToStrava`, texto `profileAskUploadDialog` ("¿Subir esta sesión a Strava?") y botones `cancel` / `profileUploadToStrava`. Al confirmar se sube **sin** aviso del resultado;
   - **manual:** nada.
3. **Al completar la rutina:** las reglas de F-WKT-21, punto 6.
4. **En modo simulador** nunca se sube ni se pregunta, en ningún camino. Esta protección tiene que valer para cualquier punto nuevo de subida.
5. Sin Strava configurado no existe el perfil, y la app no lo consulta.

**Casos borde y errores**

- En el modo "preguntar" al terminar a mano, el diálogo puede cerrarse solo por el cierre automático de la pantalla (F-WKT-13).
- El ID puede quedar desactualizado (F-WKT-21).

**UI:** avisos y diálogo.

**Datos persistidos:** `strava_activity_id` (STR). La preferencia de subida es de PRF.

**Archivos legacy:** `immersive_workout_screen.dart:141-192`, `:198-205`, `:231-291`; `rowmate-legacy/CLAUDE.md` ("Nothing is uploaded to Strava" en modo simulador).

**Tests legacy:** ninguno.

**Dependencias:** STR, PRF, SIM, F-WKT-06, F-WKT-20, F-WKT-21.

**Mejora EXR:** el estado de la subida (subiendo, subido o error con opción de reintentar) se ve de forma persistente en el resumen o en el historial, no solo en un aviso que desaparece.

---

## F-WKT-23: Pantalla clásica y tarjeta de métrica (código muerto)

**Valor para el usuario:** ninguno en la app actual: ninguna navegación llega a estas pantallas. Se documentan como referencia y para dejar explícito que no se migran.

**Comportamientos observables (no alcanzables hoy)**

1. La **pantalla clásica** a pantalla completa tiene, de arriba hacia abajo:
   - una barra superior con el nombre de la rutina o `metricFreeLabel`, y un botón `workoutFinish` que abre una confirmación con las claves `workoutFinishTitle` y `workoutFinishContent`;
   - un banner del paso con el tipo, la etiqueta de objetivo y la cuenta regresiva, "N/M", una mini barra de progreso de 45 px y un pulso de 500 ms en los últimos 5 s;
   - una grilla de métricas de 2 columnas en vertical y 4 en horizontal, con las métricas que tienen objetivo en una fila superior, el pulso solo si es mayor que 0 y las etiquetas tomadas de los `.arb` (`metricTime`, `metricSplit`, `metricDistance`, `metricWatts`, `metricSpm`, `metricCalories`, `metricHeartRate`);
   - controles de pausa y reanudar (`workoutPause`, `workoutResume`).

   Usa el mismo semáforo de F-WKT-10.
2. La **tarjeta de métrica** compartida (etiqueta de 11, valor de 32 y unidad) no se usa en ninguna pantalla.

**Casos borde y errores:** no aplica.

**UI:** no alcanzable.

**Datos persistidos:** ninguno.

**Archivos legacy:** `workout_screen.dart:205-988`; `lib/shared/widgets/metric_card.dart:1-68`.

**Tests legacy:** ninguno.

**Dependencias:** ninguna.

**Mejora EXR:** no se migra. Lo único útil es que esta pantalla usa las claves `.arb` correctas, que pueden servir para localizar la pantalla inmersiva (ver Preguntas abiertas).

---

## Textos visibles del subsistema

### Claves `.arb` que se usan

| Clave | es | en | Dónde |
|---|---|---|---|
| `workoutTitle` | Entrenamiento | Workout | F-WKT-12 |
| `workoutRowerNotConnected` | Conectá el rower para iniciar un entrenamiento | Connect the rower to start a workout | F-WKT-12 |
| `workoutScene` | Escenario | Scenery | F-WKT-12 |
| `workoutFree` | Entrenamiento libre | Free workout | F-WKT-12 |
| `workoutOrPickRoutine` | "  O elegí una rutina:" (con dos espacios al inicio) | "  Or pick a routine:" | F-WKT-12 |
| `workoutNoRoutines` | Creá una rutina en la pestaña "Rutinas" | Create a routine in the "Routines" tab | F-WKT-12 |
| `workoutStartContent` | {summary}\n\nIniciar el entrenamiento? | {summary}\n\nStart the workout? | F-WKT-12 |
| `workoutStart` | Iniciar | Start | F-WKT-12 |
| `cancel` | Cancelar | Cancel | F-WKT-12, F-WKT-22 |
| `workoutInProgress` | Entrenamiento en curso... | Workout in progress... | F-WKT-01 |
| `stepTypeWarmup` / `Work` / `Rest` / `Cooldown` | Calentamiento / Trabajo / Descanso / Enfriamiento | Warmup / Work / Rest / Cooldown | F-WKT-14 |
| `workoutCompleted` | ¡Entrenamiento completado! | Workout Completed! | F-WKT-21 |
| `workoutCompletedMessage` | Has completado tu rutina exitosamente. | You have successfully completed your routine. | F-WKT-21 |
| `workoutFinish` | Terminar | Finish | F-WKT-21 |
| `profileUploadToStrava` | Subir a Strava | Upload to Strava | F-WKT-21, F-WKT-22 |
| `profileUploadingToStrava` | Subiendo a Strava... | Uploading to Strava... | F-WKT-21, F-WKT-22 |
| `profileUploaded` | Subido a Strava | Uploaded to Strava | F-WKT-21, F-WKT-22 |
| `profileUploadFailed` | Error al subir | Upload failed | F-WKT-21, F-WKT-22 |
| `profileAskUploadDialog` | ¿Subir esta sesión a Strava? | Upload this session to Strava? | F-WKT-22 |

Hay claves definidas que esta pantalla no usa: `workoutStartTitle`, `workoutFinishTitle`, `workoutFinishContent`, `workoutPause`, `workoutResume`, `metricFreeLabel` y `metric*`. Las cinco últimas sí aparecen en la pantalla clásica muerta.

### Textos fijos en el código (no se traducen)

| Texto | Dónde | Observación |
|---|---|---|
| "Entrenamiento Libre" | barra libre (F-WKT-16) | existe `metricFreeLabel` ("Entrenamiento libre") |
| "EN MARCHA", "PAUSADO", "FINALIZADO", "—" | chip de fase (F-WKT-16) | sin clave |
| "Up next: " | barra de pasos (F-WKT-14) | en inglés en los dos idiomas |
| "SPM", "Split /500m", "Watts", "W", "kcal", "BPM", "m" | HUD (F-WKT-17) | sin clave |
| "Terminar", "Pausa", "Reanudar" | controles (F-WKT-20) | existen `workoutFinish`, `workoutPause`, `workoutResume` |
| "Terminar entrenamiento", "Se guardará la sesión. ¿Continuar?", "Cancelar", "Terminar" | confirmación (F-WKT-20) | existen `workoutFinishTitle`, `workoutFinishContent`, `cancel`, `workoutFinish` |
| "Tiempo", "Distancia", "Calorías" | resumen (F-WKT-21) | sin clave (HIS tiene `historyStat*` parecidas) |
| "500 m", "Reps", "Parciales 500 m", "Series anteriores", "Sin parciales en este paso", "Sin repeticiones completas", "Rep {r}/{R} · …", "Rep {n}" | paneles (F-WKT-18) | sin clave; los tests los buscan literalmente |
| "{n} series · {m} descansos", "{n} pasos" | resumen de rutina (F-WKT-11/12) | modelo RUT |
| "{min}-{max}W", ">{min}W", "{spm} spm", "/500m", "📈 SPM …/rep" | etiqueta de objetivo (F-WKT-11/14) | modelo RUT |

---

## Discrepancias entre docs y código

1. **Reloj de pared.** La spec `2026-10-02-series-splits-design.md` lo pone "centrado arriba, a la altura de las tarjetas de SPM y split (`padding.top + 90`)". El código lo pone sin marco dentro de la barra de pasos (a la derecha de la cuenta regresiva) y en la barra libre (antes del chip de fase). El `CLAUDE.md` del legacy coincide con el código.
2. **Vuelta en curso.** La spec dice "`▸ 340 m · 1:15.0`". El código muestra primero el tiempo grande (30) y al lado los metros chicos ("1:15.0 340 m"), sin "▸" ni "·".
3. **Filas de repeticiones.** La spec dice "`Rep 4 · 1:54 · 741 m`". El código las muestra en columnas, sin los "·".
4. **Orden de las vueltas.** La spec no lo dice. El código las muestra de la más reciente a la más vieja.
5. **Inicio de los parciales.** La spec define que el inicio solo recibe la distancia. El código también recibe el reloj activo y suma una referencia nueva al reanudar y el saneamiento de saltos (negativos o mayores que 10 m/s), que la spec no menciona. El `CLAUDE.md` del legacy sí lo documenta.
6. **Tests de la spec de parciales.** La spec menciona un test "la vuelta en curso" separado. En el código esa verificación está dentro de "interpola el tiempo de la vuelta…", y hay 4 tests de saneamiento que la spec no lista.
7. **Pantalla siempre encendida.** El `CLAUDE.md` y el README del legacy dicen "durante los entrenamientos". El código la activa al arrancar la app (`main.dart:42`) y la deja así toda la sesión de la app.
8. **Objetivos.** El README habla de "objetivos de vatios y SPM que se resaltan en rojo". El código también resalta el split, y cuando el objetivo se cumple la tarjeta se pone **blanca**, no conserva su color. Los vatios máximos se muestran en la etiqueta pero nunca se evalúan.
9. **Spec de la barra de pasos.** Dice que los pasos hechos se evalúan al completarse. En el código el último paso se evalúa pero su píldora nunca muestra el resultado, porque queda "en curso". La spec no lo contempla.
10. **README, "Session auto-repair".** Dice que se reparan desde la telemetría las sesiones que no se guardaron bien, por ejemplo con la app cerrada a mitad. La telemetría vive en memoria hasta el final, así que una sesión cortada por un cierre no tiene puntos para reparar. Solo es reparable la sesión cuya actualización final falló después de insertar los puntos.
11. **Textos visibles.** La regla del proyecto es que los textos salgan de los `.arb`. La pantalla inmersiva usa muchos textos fijos en español, más "Up next:" en inglés, aunque existen claves equivalentes para varios.
12. **Valores de respaldo inútiles.** Hay respaldos `?? '...'` en `workoutCompleted`, `workoutCompletedMessage` y `workoutFinish` que nunca se usan, porque las claves siempre existen.
13. **Código muerto.** La pantalla clásica (`_FullscreenWorkoutPage`) y la tarjeta de métrica compartida (`MetricCard`) siguen en el código sin usarse. El `CLAUDE.md` lo reconoce para la pantalla clásica.

---

## Preguntas abiertas

Para cada pregunta, la opción por defecto es hacer lo que hace el legacy hoy, salvo que se decida otra cosa.

1. **Textos fijos y localización.** ¿Se localizan en Unity todos los textos de la pantalla inmersiva y de los paneles (cambio de producto menor, con claves nuevas o con las existentes)? Hacerlo cambia lo que se ve en inglés. Por defecto, para la paridad, el texto en español queda idéntico.
2. **Cuenta regresiva de los pasos por distancia.** Hoy muestra el largo total ("1000m"), no lo que falta, y la píldora no muestra avance. ¿Se mantiene, o se muestran los metros restantes y el avance por distancia (cambio de producto)?
3. **Último paso de la rutina.** Al terminar queda "en curso" y no muestra si se cumplió el objetivo. ¿Se mantiene?
4. **Diálogo de completado al terminar a mano.** Puede aparecer un instante por una carrera. ¿El comportamiento esperado es que nunca aparezca al terminar a mano?
5. **Strava "preguntar" al terminar a mano.** El diálogo puede cerrarse solo por el cierre automático de la pantalla. ¿El esperado es que quede visible hasta que el usuario responda?
6. **ID para Strava al completar la rutina.** Si el usuario toca un botón antes de que termine el guardado, puede subirse la sesión anterior. ¿Los botones esperan el guardado?
7. **Totales del monitor.** No se ponen en 0 al iniciar la sesión, ni en el HUD ni en la sesión guardada. ¿Se mantiene, o la sesión muestra y guarda lo que se remó desde el inicio (cambio de producto)?
8. **Tiempo guardado.** Sale del monitor y puede diferir del reloj del HUD, o valer 0 si el monitor no lo informa. ¿Se mantiene?
9. **Paso por distancia tras un reinicio del monitor.** Puede no terminar nunca, o tardar de más. ¿Se aplica el mismo saneamiento que en los parciales (corrección de producto)?
10. **Telemetría solo en memoria hasta el final.** Un cierre de la app pierde toda la sesión. ¿Se guarda de forma incremental (agregado de producto)?
11. **Error al crear la sesión.** La pantalla queda con el reloj congelado y sin mensaje, y al guardar el error es silencioso. ¿Qué tiene que ver el usuario?
12. **Pantalla siempre encendida.** ¿Toda la app, como hoy, o solo durante la sesión?
13. **Pantalla clásica y tarjeta de métrica.** ¿Se confirma que no se migran?
14. **Remo desconectado durante la sesión.** Hoy no hay aviso y los valores quedan congelados. ¿Se agrega un aviso (agregado)?
15. **Sensor de pulso sin contacto.** Hoy la tarjeta BPM simplemente desaparece. ¿Se agrega un indicador explícito?
16. **Fracción de segundo en las pausas.** ¿Se acepta que se pierda en cada pausa?
17. **Píldoras del mismo ancho.** ¿Se mantienen iguales, o pasan a ser proporcionales a la duración en la mejora EXR? Cambiaría la lectura de la barra, así que sería un cambio de producto.

---

## Cobertura de tests legacy

| Test | Casos | Funcionalidad |
|---|---|---|
| `test/series_tracker_test.dart` | 12 | F-WKT-07, F-WKT-08 |
| `test/series_panels_test.dart` | 4 | F-WKT-11, F-WKT-18 |
| `test/step_pill_test.dart` | 1 | F-WKT-15 |
| `test/step_target_tracker_test.dart` | 6 | F-WKT-09 |
| `test/workout_provider_hr_test.dart` | 4 | F-WKT-05 |
| `test/routine_positions_test.dart` (RUT) | 1 | F-WKT-03, F-WKT-08 |
| `test/session_stats_test.dart` (HIS/DB) | 3 | F-WKT-06 (relacionado) |

**Sin ningún test legacy** (necesitan criterios de aceptación nuevos): F-WKT-01, 02, 04, 06 (el guardado), 10, 12, 13, 14, 16, 17, 19, 20, 21 y 22. Lo más crítico para escribir criterios en el Core puro: las fases y el reloj (01/02), el avance y el fin de pasos con progresiones (03), el estado del paso (04), la telemetría y las estadísticas de la sesión (06) y el semáforo (10).

---

## Cambios al cerrar la It. 4a

Pendientes del PO de la [spec de la It. 4a §15](../../iterations/04a-motor-hud-funcional/01-po-spec.md), releídos en el legacy (`lib/core/models/routine.dart:25-106`; `lib/features/workout/immersive_workout_screen.dart:317-323`, `:451-460`, `:576-603`; `lib/features/workout/workout_screen.dart:84-92`, `:160-170`), 2026-10-29. Describen el legacy; lo que la app hace distinto lleva su DV de la It. 4a.

| Dónde | Qué cambió |
|---|---|
| F-WKT-03 y F-WKT-11, archivos legacy | `routine.dart` tiene 138 líneas: la expansión está en `:29-70`, las posiciones en `:74-97` y el resumen en `:100-105` (antes decía `:210-251`, `:255-278` y `:281-286`) |
| F-WKT-03, casos borde | El paso sin meta sí existe: el editor lo permite (Q-25) y la cuenta muestra `nullm` (DV-25). El paso de duración negativa hace fallar el inmersivo (DV-37). El reinicio del monitor, con la nota de Q-55 (DV-26) |
| F-WKT-10, punto 4 | El split se evalúa con `val > target`, sin casos especiales: con un objetivo 0 o negativo cambia lo que da un split 0 (AC-WKT-10-04) |
| F-WKT-12, casos borde | Dos toques rápidos pueden crear dos sesiones (DV-30) |
| F-WKT-14, puntos 4 y 5 | El pulso es de 600 ms de ida y 600 de vuelta, y la cuenta de un paso sin meta muestra `nullm` |
| F-WKT-17, casos borde | En un teléfono horizontal, kcal se encima con Watts, y en 640 × 360 la banda con los controles (DV-33) |

## Cambios al cerrar la It. 4b

Pendientes del PO de la [spec de la It. 4b §15](../../iterations/04b-hud-exr/01-po-spec.md), releídos en el legacy (`3ae0f3a`: `lib/features/workout/immersive_workout_screen.dart:27-65`, `:205-215`, `:359-377`, `:392-418`, `:528-537`, `:980-992`; `lib/features/workout/step_pill.dart:82-101`; `lib/features/workout/workout_provider.dart:252-318`), 2026-10-30. Describen el legacy; lo que la app hace distinto lleva su DV de la It. 4b.

| Dónde | Qué cambió |
|---|---|
| F-WKT-13, punto 1 | Las barras se ocultan al crearse la página (al empezar su entrada) y vuelven al destruirse (al terminar su salida), en modo "de borde a borde" (AC-APP-15-01, -04) |
| F-WKT-14, punto 3.2 | La pastilla de objetivo recibe a lo sumo la mitad del espacio libre, se corta por carácter y nunca se oculta; con pastilla, la cuenta y el reloj no quedan pegados al borde y se corren cuando entra "a continuación" (DV-45, DV-46) |
| F-WKT-14, punto 6 | El halo también se pinta debajo de la píldora, tiñe la pista y pasa por debajo de la fila de información (§11.1.1 de la spec de la 4b) |
| F-WKT-14, casos borde | Con más de ≈ 120 pasos en un teléfono, las píldoras miden 0 y la fila queda vacía (DV-40) |
| F-WKT-16, casos borde | `FINALIZADO` también se ve si llega una lectura durante el guardado; mientras la página sale, el chip dice `—` (DV-41) |
| F-WKT-16, mejora EXR | En pausa no se atenúa nada del HUD (ART_BIBLE §3.2; AC-WKT-13-12) |
| F-WKT-21, punto 2 | Tampoco se cierra con Escape en escritorio (AC-WKT-21-02) |
| F-WKT-21, punto 5 | Los íconos de cada fila del resumen, y los totales leídos una sola vez al armar el diálogo (DV-43) |
| F-WKT-21, casos borde | Un `Terminar` antes de que termine el guardado deja la sesión sin hora de fin ni totales (DV-42) |
