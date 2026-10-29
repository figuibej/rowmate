# It. 4a — Gate de armonía, lente "paridad" (QA)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-28 |
| Iteración | 4a · `04a-motor-hud-funcional` ([ROADMAP §It. 4a](../../ROADMAP.md)) |
| Lente | **Paridad**: el QUÉ ([01-po-spec](01-po-spec.md)) y el CÓMO ([02-tech-design](02-tech-design.md), [04-design-notes](04-design-notes.md)) contra el legacy, verificados en el código del legacy y no en los documentos. También se leyeron [00-deuda-it3](00-deuda-it3.md) y [00-deuda-diseno](00-deuda-diseno.md) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy). Enteros: `lib/features/workout/workout_provider.dart` (línea por línea), `step_target_tracker.dart`, `series_tracker.dart`, `immersive_workout_screen.dart`, `lib/core/models/{rowing_data,interval_step,routine,workout_session}.dart`, `lib/features/routines/routines_provider.dart` y los tests `test/workout_provider_hr_test.dart`, `test/step_target_tracker_test.dart`, `test/series_panels_test.dart`, `test/series_tracker_test.dart` y `test/widget_test.dart`. En parte: `workout_screen.dart:1-240`, `series_panels.dart:1-30`, `:85-110` y `:325-350`, `lib/core/database/database_service.dart:1-60` y `:95-300`, `lib/core/bluetooth/heart_rate_service.dart:1-40`, `ble_service.dart:8`, `simulated_ble_service.dart:60-112`, `simulated_heart_rate_service.dart:86-160`, `lib/features/device/device_provider.dart:80-105`, `lib/features/routines/routines_screen.dart:20-40`, `lib/shared/theme.dart:30-70`, `lib/main.dart:1-170` y las claves de Workout de `lib/l10n/app_es.arb` y `app_en.arb`. Pantalla encendida: `<pub-cache>/wakelock_plus-1.4.0/lib/src/wakelock_plus_windows_plugin.dart:17-32` y `android/src/main/kotlin/dev/fluttercommunity/plus/wakelock/Wakelock.kt:15-30` |
| Herramientas | Unity **no** se abrió. dotnet solo para una sonda propia en `<scratch>/it4a/gate-paridad/` (§1.2), que no escribe en el repo |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 bloqueantes, 3 mayores y 7 menores (§2) |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.**

El motor del diseño es un puerto fiel de `WorkoutProvider`:

- **Cada línea del legacy tiene su lugar.** Cada línea de `workout_provider.dart` tiene un AC que la fija y una sección del diseño que la implementa (§3).
- **Los LT están todos.** Los 11 casos legacy de la iteración están mapeados con su nombre al carácter y su línea, en la spec y en el diseño (§5).
- **Las diferencias declaradas son correctas.** Las 16 DV (DV-21 a DV-36) describen bien lo que hace el legacy. Los desvíos del Architect (DT4a-01 a -15) no cambian ningún dato de una sesión terminada normalmente (§7).

La sonda contra el código ya construido confirmó **68 de 68** ejemplos de la spec: formatos, etiquetas, resumen, expansión, R3-08, P99 y la tabla de Q-55 (§1.2). La muestra de **36 casos concretos** del legacy (§4) da tres problemas, todos con arreglo simple:

- **QA-G4a-P01 · semáforo de Split con objetivo 0.** La spec y el diseño dicen "≤ 0, siempre fuera". El legacy compara `val > target`, así que con objetivo 0 un split `--:--` **cumple**.
- **QA-G4a-P02 · el diálogo de terminar abierto cuando la rutina termina sola.** Nadie lo define. Con el diseño actual el diálogo quedaría sobre Workout en reposo, atado a un *store* ya dispuesto.
- **QA-G4a-P03 · las pastillas y `Próximo`.** La spec y el diseño técnico siguen al legacy; las notas de diseño proponen otra cosa (textos en blanco, latido solo en el fondo) y sus sondas de captura esperan esos valores. Falta la decisión del PO antes de WP-B2.

Los menores son precisiones de la spec (P04, P07, P08, P10), dos alineaciones del diseño técnico (P05, P06) y un detalle visual del Designer (P09).

**Orden sugerido.** P01 antes de que WP-A escriba `TargetLightTests`; P02 y P05 antes de WP-B1; P03 y P04 antes de WP-B2.

---

## 1. Método

### 1.1 Lectura

Todo contra el legacy. La spec y el diseño se usaron solo para saber qué buscar. Cada AC que cita `archivo:línea` se abrió en esa línea. Las citas de la spec y del diseño están bien en todos los casos revisados (más de 90 referencias), con una excepción: la tabla del diseño §5 sobre `Sprint` (§9, observación O-02).

### 1.2 Sonda contra el código ya construido

Es un proyecto de consola desechable en `<scratch>/it4a/gate-paridad/probe/`:

- **Qué compila.** `net8.0`, con una referencia a `tools/dotnet/RowMate.Core/RowMate.Core.csproj`, sin código nuevo en el repo.
- **Dónde deja todo.** `dotnet build … -c Release --artifacts-path <scratch>/it4a/gate-paridad/artifacts`, así que nada queda en `tools/dotnet/`.
- **Qué prueba.** Llama a las piezas de Core que el diseño dice **reusar** (`Fmt`, `DartMath`, `TimeFormats`, `StepLabels`, `RoutineSummary`, `RoutineExpansion`, `StepLimitCheck`, `RoutineStructure`, `SessionStats` y el catálogo). Además transcribe dos reglas del legacy: el saneamiento de `series_tracker.dart:118-127` y la comparación del semáforo `val > target` (`immersive_workout_screen.dart:579-583`).

Salida real (la consola muestra mal los caracteres no ASCII; las comparaciones se hacen en memoria, con los textos exactos):

```
== F-WKT-11: formatos que el diseño reusa ==            31 OK
== AC-WKT-03-01, AC-WKT-11-13, -14: Prueba 4a y Pirámide 500 ==   13 OK
== AC-WKT-12-08 (R3-08): la regla del diseño §5 con las piezas de la It. 3 ==   5 OK
      tiempo 0.262 ms, reservado 40 B (primera llamada, con JIT)
== AC-WKT-06-06: estadísticas del cierre (libre, R-4, 12,3 s) ==   5 OK
== AC-WKT-03-03: Distancia 100, saneamiento (Q-55) contra legacy crudo ==   10 OK
== AC-WKT-10-04: semáforo de Split, regla legacy val > target ==   4 OK
RESUMEN: 68 OK, 0 FAIL
```

| Grupo | Qué confirma | AC |
|---|---|---|
| Formatos | `24.25` → `24.3`, `0` → `0.0`; split `02:13`, `00:59`, `100:00`, `--:--` (0 y −5); `-10`; `123456`; reloj `00:00`, `02:55`, `1:02:05`, `10:00:00`; cuenta `1:05`, `0:05`, `12:00`, `0:00`, `5999:59`; `500m`, `1000m` (el entero del catálogo sale sin miles); `?`; vuelta `2:46.7`, `1:05.0`, `0:55.0`, `1:00.0`; split de repetición `1:54`, `2:14`; `obj.` del split `02:05`, `00:00` (0) y `00:55` (−5) | AC-WKT-11-01 a -10, AC-WKT-10-05, LT-45 |
| Rutinas | `Prueba 4a` da 6 pasos con SPM 22 y 24. La etiqueta de la repetición 2 es `>160W · 24 spm · 📈 SPM +2/rep`, igual en es y en; sin objetivos sale vacía. La primera de `Pirámide 500` es `150-180W · 02:05/500m · 📈 W +10/rep, Split -2s/rep`. El resumen es `1 serie · 1 descanso` / `1 interval · 1 rest`, y el diálogo `…⏎⏎¿Iniciar el entrenamiento?` / `…⏎⏎Start the workout?` | AC-WKT-03-01, -11-13, -11-14, -12-07 |
| R3-08 | `StepLimitCheck.TemplateIsWithinLimits(t) && RoutineStructure.Segments(t, 0).Total > 0` da `false` para `Serie enorme`, `Vatios imposibles` y `Serie ×0`, y `true` para `Prueba 4a` y `Pirámide 500`. Con ×2 000 000 000 tarda 0,26 ms y reserva 40 B | AC-WKT-12-08 (diseño §5: < 50 ms y < 64 KB) |
| Cierre | `SessionStats.Compute` de los 3 puntos da 1048 m, 312 s, 150 W, 24.0 spm y 41 kcal | AC-WKT-06-06 |
| Q-55 | `Distancia 100`, en el orden R-4 / R-4R / R-4Z / pausa: saneado 25, 26, 27 y 25; legacy crudo 25, 285, 25 y 11. Sin lectura previa, con el monitor en 5000: saneado 26, legacy 1 | AC-WKT-03-03 (y P06) |
| Semáforo | `val > target`: con objetivo 130 y split 0 cumple; con objetivo **0** y split 0 **cumple**; con −5 y 0 queda fuera; con 0 y 125 queda fuera | AC-WKT-10-04 (P01) |

---

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección |
|---|---|---|---|---|---|
| **QA-G4a-P01** | mayor | po | **Semáforo de Split con objetivo 0.** AC-WKT-10-04 dice "con un objetivo ≤ 0 Split está siempre fuera, también con `--:--` (paridad: `val > target`)". AC-WKT-10-07 ("salvo un objetivo ≤ 0") y §15 (F-WKT-10 punto 4) dicen lo mismo, y el diseño lo repite (§2.6: "no con uno ≤ 0"). El legacy compara `val > target`: con objetivo 0 y split 0 da `0 > 0` = falso, o sea blanco (**cumple**). Solo un objetivo **< 0** queda siempre fuera. El objetivo 0 sale del editor: split 4 con progresión −2, en la repetición 3; Q-92 controla por valor absoluto y AC-RUT-04-04 ya muestra `00:00/500m` | `immersive_workout_screen.dart:579-583`, `:598-602`; spec líneas 275, 278 y 781; diseño línea 310; sonda: "objetivo 0, split 0 (--:--) fuera = False" | Cambiar "≤ 0" por "< 0" en AC-WKT-10-04, AC-WKT-10-07 y §15, y agregar: "con objetivo 0, `--:--` cumple y cualquier split > 0 queda fuera". El Architect corrige el comentario de §2.6 y le pone a `TLT.SplitAndNonPositiveTargets` los cuatro casos de §1.2 |
| **QA-G4a-P02** | mayor | po | **Hueco: el diálogo de terminar abierto cuando la sesión termina sola.** Es un caso probable: se toca `Terminar` en los últimos segundos del enfriamiento y la rutina termina mientras se decide. La spec no lo define. El diseño cierra el inmersivo por estado (§6.3) y dispone su `WorkoutHudStore` en `OnPopped`, pero el diálogo vive en el `DialogLayer` global de la It. 3, que no lo ata a la página. Quedaría abierto sobre Workout en reposo, y su `Terminar` llamaría a `FinishAsync()` de un *store* ya dispuesto. En el legacy el diálogo de completado se apila encima (`:49-55`). Al cerrarlo, el `pop` de reposo (`:57-65`) saca el de terminar sin efecto (`ok != true`, `:141`), y la página sale con la lectura siguiente: el de terminar queda cancelado | `immersive_workout_screen.dart:49-65`, `:125-147`; [03-rutinas/02-tech-design §4.5](../03-rutinas/02-tech-design.md) (`DialogLayer`); diseño §6.3 y §6.8; spec AC-WKT-20-03 y AC-WKT-13-04 | AC nuevo, por ejemplo AC-WKT-20-07: "*Dado* el diálogo de terminar abierto, *cuando* la sesión termina sola, *entonces* el diálogo se cierra sin efecto (como `Cancelar`) en el cuadro en que la fase pasa a terminada, y sigue AC-WKT-03-05 (aviso `workoutCompleted`); un `Terminar` del diálogo en ese mismo cuadro no cambia nada". El Architect cierra el diálogo de la página al cambiar la fase, antes del `Pop`, y suma `HCT.RoutineEndsWithTheFinishDialogOpen` |
| **QA-G4a-P03** | mayor | po | **Pastillas y `Próximo`: la spec y el diseño técnico contra las notas de diseño.** AC-WKT-14-03, AC-WKT-14-04, CAP-21 ("Descanso en verde"), CAP-27c y el diseño técnico siguen al legacy. El diseño técnico está en §6.6, §9.5 y la comprobación de CAP-21 en §7.3 (`Descanso` en `#06D6A0`). El legacy pone el texto de la pastilla en el color del paso, `Up next:` en blanco al 60 % sobre un tinte al 25 % y el tipo siguiente **en su color**, y la pastilla entera late de 0,6 a 1. Las notas de diseño proponen textos en blanco, tinte al 20 %, `Próximo:` en `text.secondary`, el tipo siguiente en blanco y que laten solo el fondo y el borde. Esa propuesta está en §4.5 y §5.4, en DN4a-16 y en §11, y sus sondas la esperan (§8.2: CAP-21 `Descanso` `#FFFFFF`; CAP-19 "pastilla con texto blanco"). Así escritos, el código de WP-B2 y las comprobaciones del arnés chocan con las sondas del Designer. Además, DT4a-12 ya pide la "¿DV-37?" | `immersive_workout_screen.dart:394-450`; spec líneas 359-360 (AC-WKT-14-03, -04), 490 (CAP-19), 492 (CAP-21) y 498 (CAP-27); diseño §6.6, §9.5 y §7.3 (CAP-21); notas §4.5, §8.2, §10 (DN4a-16) y §11 | El PO decide antes de WP-B2. **Si acepta:** DV nueva (numerada junto con DT4a-12), AC-WKT-14-03 ("texto … en blanco"), AC-WKT-14-04 (tinte al 20 %, `Próximo:` al 70 %, el tipo en blanco, "laten el fondo y el borde"), CAP-19, CAP-21 y CAP-27c; el Architect actualiza §6.6, §9.5 y §7.3. **Si rechaza:** el Designer saca los valores alternativos de §4.5, §8.2 y §13 y queda solo la excepción de §9.2 |
| QA-G4a-P04 | menor | po | **DV-31 y DV-33 incompletas frente a las notas de diseño.** Faltan dos diferencias visibles con el legacy. (1) Las unidades `W` y `m` van en `.hud` al 80 %; el legacy las pone en el color base al 55 % (`immersive_workout_screen.dart:826`, `:877`). (2) En horizontal, kcal y BPM van en fila a la altura de los controles, con BPM a la izquierda de kcal, y la banda es compacta (relleno 14 y 10 al divisor, en lugar de 20 y 16: `:676`, `:693`). El legacy pone kcal sobre BPM en columna, a `bottom: 140` en las dos orientaciones (`:709-733`). Las notas de diseño se lo piden al PO (§11) y la spec no lo tiene | notas §2.2, §3.1, §11; spec §11.1 DV-31 y DV-33 | Sumar esos textos a DV-31 y DV-33 (la redacción de las notas §11), para mostrárselos al usuario al cerrar |
| QA-G4a-P05 | menor | architect | **Aspecto del diálogo de terminar.** El diseño técnico le da `.rm-dialog--hud` (`surface.overlay @92 %`, título 20/700, §6.8 y §9.5). Las notas de diseño deciden el diálogo opaco de la app, con título en 600, y dejan `.rm-dialog--hud` sin reglas para la 4b (§4.7). El legacy usa un `AlertDialog` estándar, opaco (`immersive_workout_screen.dart:126-140`): la de las notas es la opción de paridad | diseño §6.8, §9.5; notas §4.7, §13 (ART_BIBLE §3.5) | Alinear §6.8 y §9.5 con las notas §4.7 antes de WP-B1 |
| QA-G4a-P06 | menor | architect | **Los casos legacy del saneamiento que porta la 4a quedan para la It. 5.** La 4a porta `series_tracker.dart:118-127` como `SanitizedDistance` y el paso por distancia depende de eso (Q-55). Los casos que lo prueban son el grupo `_hardening` de `test/series_tracker_test.dart:155-220`: 'una lectura de 0 dentro del paso…' (`:155`), 'un primer salto desde 0 no crea vueltas falsas' (`:180`), 'un reinicio del monitor no genera valores negativos' (`:191`) y 'rebase no cuenta el salto al reanudar' (`:211`). El ROADMAP §5 los deja enteros para la It. 5 y SDI (§8.2) cubre las ideas sin los nombres del legacy. El salto desde 0 muestra una diferencia real que ninguna tabla de la spec tiene: una rutina iniciada sin lectura previa, con el monitor en 5000 m, termina un paso de 100 m en el segundo 26; el legacy, en el 1 | `test/series_tracker_test.dart:155-220`; ROADMAP §5 (`series_tracker_test`, 12 casos, It. 5); sonda: "Sin lectura previa … nuevo = 26, legacy = 1" | Sumar a SDI las afirmaciones de metros de esos 4 casos con `// legacy: test/series_tracker_test.dart:<línea> '<nombre>'` (las vueltas y las repeticiones siguen en la It. 5), y a SAD el inicio sin lectura previa. En la matriz, marcarlos "parcial; completa en la It. 5" |
| QA-G4a-P07 | menor | po | **AC-BLE-10-03 no lista todos los estados sin remo.** Define "sin remo" como "desconectado, buscando o conectando". El legacy muestra el aviso con **cualquier** estado distinto de `connected` (`workout_screen.dart:50`, `:59`; `device_provider.dart:94`), y eso incluye `off`, el Bluetooth apagado (`lib/core/bluetooth/ble_service.dart:8`; `Assets/RowMate/Core/Ble/BleStatus.cs` tiene `Off`). La regla del diseño (`!IsConnected`, §2.11) está bien, pero una prueba escrita desde el AC dejaría `Off` sin cubrir, y es un estado real desde la It. 10a | citas | "*Dado* el remo en cualquier estado distinto de conectado (apagado, buscando, conectando o desconectado)", con los 4 en WST y WSC |
| QA-G4a-P08 | menor | po | **Hueco: el aviso de completado cuando la rutina termina en segundo plano.** En el legacy el diálogo de completado es modal (`barrierDismissible: false`, `immersive_workout_screen.dart:207-212`) y queda hasta que se lo toca: al volver siempre se ve. En la 4a el aviso provisorio dura 4 s. En Windows minimizada el *loop* sigue (AC-WKT-02-09), así que aparece y se va sin que nadie lo vea. En Android puede no aparecer por AC-WKT-02-08 ("no se acumulan avisos viejos"). AC-WKT-02-06 y -09 no dicen qué se ve | spec AC-WKT-02-06, -08 y -09; diseño §4.1, §4.3 | Decirlo en AC-WKT-02-06 y -09. Por ejemplo: "al volver aparece una vez `workoutCompleted`", o "provisorio: no aparece; lo resuelve el diálogo de la 4b". Cubrirlo en WBG y WBU |
| QA-G4a-P09 | menor | designer | **La barra superior de Workout fuera de reposo.** Fuera de reposo el legacy muestra `_ActiveView`: un `Scaffold` **sin** `AppBar`, con `workoutInProgress` en el centro (`workout_screen.dart:180-201`). El diseño técnico (§6.1: `#workout-appbar` queda afuera de `#workout-rest`) y las notas (§4.1: "centrado entre las dos barras") conservan la barra `Entrenamiento`. Se ve solo durante el fundido del inmersivo (240 / 200 ms), pero es una diferencia que no está declarada | citas | Elegir: ocultar `#workout-appbar` fuera de reposo (paridad) o declararlo junto con DV-36 |
| QA-G4a-P10 | menor | po | **AC-WKT-03-06 y AC-WKT-04-07 dan por hecho un último paso por tiempo.** Dicen que con la rutina terminada la cuenta muestra `0:00`, con restante 0 y progreso 1. Eso vale solo si el último paso es por tiempo. Si es por distancia, el legacy deja restante −1 y progreso 0 y sigue mostrando `{m}m` (`workout_provider.dart:31-36`, `:101-104`; `immersive_workout_screen.dart:456-458`), como dicen AC-WKT-04-02 y -03 | citas | Agregar "si el último paso es por tiempo; por distancia, la cuenta sigue mostrando su largo, sin restante ni progreso" |

---

## 3. `workout_provider.dart`, línea por línea

| Líneas | Qué hace el legacy | Spec | Diseño | Resultado |
|---|---|---|---|---|
| `:13`, `:49-50` | `enum WorkoutPhase { idle, active, paused, finished }`; arranca en `idle` con `RowingData()` en 0 | AC-WKT-01-01 | §2.1; `State` con `RowingData.Empty` (§2.10) | ✓ |
| `:16-42` | `StepProgress`: restante −1 sin duración; `progress` acotado solo si la duración es > 0; `isEndingSoon` de 1 a 5 | AC-WKT-04-02 a -04 | §2.5 | ✓ |
| `:52-61` | índice, relojes, distancia de inicio, `SeriesTracker`, `StepTargetTracker` y posiciones, solo en memoria | AC-WKT-06-14 | §2.8 ("No se guardan") | ✓ |
| `:68-76` | `_hrmBpm` (null = sin sensor; 0 = sin lectura válida), sesión, inicio, *buffer*, cuenta de muestreo | AC-WKT-05, -06 | §2.7, §2.8, §3.2 | ✓ |
| `:78-80`, `:296` | `lastFinishedSessionId`, solo si el cierre no lanzó | AC-WKT-06-11 | §2.9, §2.10 | ✓ |
| `:82-86` | se suscribe a datos, bpm y estado al construirse; vive con la app (`main.dart:82`; el `IndexedStack` construye Workout al arrancar) | AC-WKT-01-01 | §2.7, §2.13 | ✓ |
| `:98-112` | `stepProgress` es null sin rutina o en reposo; lee `steps[_currentStepIndex]` (falla con 0 pasos) | AC-WKT-04-01, AC-WKT-12-08 | §2.5, §5 | ✓ (×0 no llega: R3-08) |
| `:115-117` | `startFree()` = `_begin(null)` | AC-WKT-01-02 | §2.9 | ✓ |
| `:120-131` | expande con `copyWith(steps: flattenedSteps)`, que conserva id y nombre; paso 0, 0 s, distancia de inicio, posiciones, `_series.begin`, `_targets.reset`; después `_begin(routine)` | AC-WKT-01-03, AC-WKT-03-01 | §2.9 | ✓ |
| `:133-149` | fase en marcha, `DateTime.now()`, *buffer* y cuenta a 0, `await insertSession`. Recién después arma `Timer.periodic(1 s)` y avisa. Si el alta lanza no hay *timer*: el reloj queda en `00:00` | AC-WKT-01-02, -11; AC-WKT-06-01 | §2.2 (con la gracia de 100 ms), §3.2 | ✓ desvío declarado (DT4a-02, DV-29) |
| `:151-157` | sin efecto fuera de `active`; suma 1 al reloj del paso, al total y a la cuenta de muestreo | AC-WKT-02-01, -02 | §2.3 #0-1 | ✓ |
| `:158-173` | toma un punto si `_sampleTick % 5 == 0 && _sessionId != null`, con el tiempo **del monitor**, SPM, paladas, distancia, split, W, `totalCalories`, el pulso fusionado y, con rutina, el índice y `type.name` | AC-WKT-06-02 | §2.3 #2, §2.8 | ✓. El diseño toma el punto también con el alta en vuelo. En el legacy el *timer* no corre antes del alta, así que eso no cambia nada que se vea |
| `:176-187` | con rutina, en orden: `_series.update` (el saneamiento), `_targets.update` y `_checkStepCompletion` | AC-WKT-02-02, AC-WKT-09-07 | §2.3 #3 | ✓ |
| `:189` | un aviso por segundo, también cuando el paso cerró la rutina | AC-WKT-02-02 (4) | §2.3 #4 | ✓ |
| `:192-201` | por tiempo `>=`, y si tiene tiempo y distancia manda el tiempo. Por distancia, `_data.distanceMeters − _distanceAtStepStart >= m`. Sin ninguno de los dos, nunca termina | AC-WKT-03-02, -03, -07, -08 | §2.4 | ✓ con Q-55 (DV-26); sonda 25/26/27/25 contra 25/285/25/11 |
| `:203-209` | `_targets.complete`, índice + 1, reloj del paso a 0, distancia de inicio = la de ese segundo | AC-WKT-03-04 | §2.4 | ✓ |
| `:210-216` | último paso: `finished` en el acto, *timer* cancelado, `finish()` **sin** `await`. El punto final se agrega en el mismo *tick*, antes del primer `await`, con el mismo `_data` | AC-WKT-03-05, -06; AC-WKT-06-07 | §2.4, §2.8 | ✓. El punto final repite al del segundo: `Corta` da 7 puntos |
| `:220-224` | `_data = d.copyWith(heartRate: _hrmBpm ?? d.heartRate)`; avisa con cada lectura, en cualquier fase | AC-WKT-05-01, AC-WKT-17-06 | §2.7 | ✓ |
| `:226-230` | el bpm se guarda y pasa a `_data`; avisa | AC-WKT-05-01 | §2.7 | ✓ |
| `:232-237` | un estado ≠ `connected` con bpm previo (también un 0) → `_hrmBpm = null` y pulso 0; si no, nada y sin aviso | AC-WKT-05-02 a -05 | §2.7 | ✓ (`HrmStatus`: `scanning`, `connecting`, `connected`, `disconnected`, en `heart_rate_service.dart:10`) |
| `:239-243` | `pause()` no mira la fase y cancela el *timer* | AC-WKT-01-04 | §2.1 (solo desde `Active`) | ✓. En el legacy se podía pausar en `finished`, porque los controles no se apagan (`immersive_workout_screen.dart:923-948`); AC-WKT-20-06 lo corrige |
| `:245-250` | `resume()`: *timer* nuevo (pierde la fracción, Q-58) y `_series.rebase` | AC-WKT-01-05 | §2.2, §2.4 | ✓. En el legacy, dos `resume()` seguidos armaban dos *timers*; el diseño lo evita con la fase |
| `:252-256` | `finish()`: `finished`, *timer* cancelado; guarda solo si hay sesión creada | AC-WKT-01-06 | §2.9 | ✓ |
| `:259-273` | punto final con el paso en curso; tipo vacío si el índice queda fuera de rango | AC-WKT-06-06 (1) | §2.8 | ✓ |
| `:276` | `SessionStats.compute(_dataBuffer)`, sobre todos los puntos, con el final | AC-WKT-06-06 (2) | §2.8 | ✓ (sonda) |
| `:279-280` | `insertDataPoints`: un solo lote, al final | AC-WKT-06-05 | §3.2 | cambio declarado (Q-12, DC-09) |
| `:283-295` | `updateSession` con `toMap()` completo: id; rutina de la copia expandida (mismo id y nombre); inicio; `finishedAt: DateTime.now()` tomado **después** del lote; P99 y máximos; `strava_activity_id` NULL | AC-WKT-06-06 (3) | §3.3 (DT4a-06, DT4a-07) | ✓. El legacy toma la hora de fin unos milisegundos más tarde; con el reloj de prueba da igual |
| `:297-299` | un error se traga con un `debugPrint` | AC-WKT-06-12 | §2.9 (avisos) | cambio declarado (Q-13, DV-29) |
| `:302` | avisa al final del guardado, no al empezar | — | §2.9 avisa al empezar | sin efecto visible: en el legacy una lectura durante el guardado ya reconstruye la vista (y por eso abre el diálogo de completado, Q-53) |
| `:305-318` | `reset()` borra rutina, índice, relojes, sesión, inicio, *buffer*, posiciones, `_series.begin(0)` y `_targets.reset`. **No** toca `_data`, `_hrmBpm` ni `_lastFinishedSessionId` | AC-WKT-01-07 | §2.9 | ✓. En el legacy lo llama la pantalla (DT4a-15) |
| `:320-327` | `dispose()` cancela el *timer* y las suscripciones; la sesión queda abierta y sin puntos | AC-WKT-06-10 | §2.10 (`Dispose` + `Abandon`) | cambio declarado: ahora queda con sus puntos (Q-12) |

---

## 4. Muestra de casos concretos del legacy (36)

| # | Caso (entrada) | Legacy | Qué da el legacy | Spec | Diseño | ¿Igual? |
|---|---|---|---|---|---|---|
| M-01 | `Corta` con R-4: cambios de paso | `workout_provider.dart:196-216` | 10, 15, 25; fin en 30 | AC-WKT-03-02 | §2.4; Anexo A | ✓ |
| M-02 | `Corta` con R-4: puntos | `:158-173`, `:259-273` | 7; índices 0, 0, 1, 2, 2, 3, 3; `work`, `work`, `rest`, `work`, `work`, `rest`, `rest` | AC-WKT-06-03 | §2.8; Anexo A | ✓ |
| M-03 | Libre con R-4, `Terminar` a los 12,3 s | `:252-296`; `workout_session.dart:111-136` | 3 puntos (305, 310, 312); 1048 m, 312 s, 150 W, 24.0, 41 kcal | AC-WKT-06-06 | §2.9, §3.3 | ✓ (sonda) |
| M-04 | Pausa a los 10,7 s durante 30 s | `:239-250` | el segundo 11 llega a los 41,7 s | AC-WKT-01-05 | §2.2 | ✓ (Anexo A) |
| M-05 | `Distancia 100` con R-4R | `:198-201`; `series_tracker.dart:118-127` | legacy 285; saneado 26 | AC-WKT-03-03 | §2.4 | ✓ (sonda) |
| M-06 | Ídem con R-4Z | ídem | 25; saneado 27 | ídem | ídem | ✓ (sonda) |
| M-07 | Ídem, 20 s de pausa a los 40 m, remando | ídem | 11; saneado 25 | ídem | ídem | ✓ (sonda) |
| M-08 | Rutina iniciada sin lectura previa (distancia 0), con el monitor en 5000 m | ídem | legacy: 1 s; saneado: 26 | no está (cae en DV-26) | §2.4 | ✓ regla; ver P06 |
| M-09 | LT-35: FTMS con 100 W y 90, después bpm 142 | `:220-230`; test `:80` | 142, 100 W, 2 avisos | AC-WKT-05-01 | §2.7 | ✓ |
| M-10 | LT-36: bpm 0 y después `disconnected` | `:232-237`; test `:125` | 0; el FTMS repone 90 | AC-WKT-05-02 | §2.7 | ✓ |
| M-11 | Caída del pulsómetro simulado | `simulated_heart_rate_service.dart:107-139` | bpm 0 y después `disconnected`; vuelve con `connected` y el bpm 1 s más tarde | AC-WKT-05-06 | §2.7 | ✓ (el `connected` sin bpm previo no cambia nada) |
| M-12 | `Simular desconexión` del remo | `simulated_ble_service.dart:81-102` | se corta el *timer* de datos: no llega ninguna lectura en 0 | AC-WKT-03-10 | §2.4 | ✓ |
| M-13 | Objetivos de `Corta` con R-4(19) | `step_target_tracker.dart:43-62` | paso 0 (objetivo 18) cumplido; paso 2 (objetivo 20) no | AC-WKT-09-07 | §2.6; Anexo A | ✓ |
| M-14 | Lectura parada (0 W y 0 spm) | `step_target_tracker.dart:31` | no cuenta: hacen falta las dos en 0 (es Y, no O) | AC-WKT-09-05, -07 | §2.6 | ✓ |
| M-15 | Semáforo de Split: objetivo 130, split 0 | `immersive_workout_screen.dart:579-583`, `:598-602` | blanco (cumple) | AC-WKT-10-04 | §2.6 | ✓ |
| M-16 | Semáforo de Split: objetivo **0**, split 0 | ídem | `0 > 0` es falso: blanco (**cumple**) | AC-WKT-10-04 dice "fuera" | §2.6 dice "no cumple" | **✗ P01** |
| M-17 | Semáforo de Split: objetivo −5, split 0 | ídem | fuera | AC-WKT-10-04 | §2.6 | ✓ |
| M-18 | Watts 190 contra 150-180 | `:594-597` | cumple (el máximo no se mira) | AC-WKT-10-03 | §2.6 | ✓ |
| M-19 | Cuenta de un paso sin meta | `:456-458` | `nullm` | AC-WKT-03-08 (DV-25: `?`) | §2.12 #8 | ✓ cambio declarado |
| M-20 | `Up next` en un paso de 60 s | `:342-346` | desde que quedan 15 s hasta el cambio; nunca en el último paso ni por distancia | AC-WKT-04-05, -06 | §2.5 | ✓ |
| M-21 | Pulsos en pausa | `:319-322` (`repeat` sin parar) | siguen | AC-WKT-14-06 | §6.6 | ✓ |
| M-22 | BPM con pulso 0 | `:721` | sin tarjeta | AC-WKT-17-05 | §2.11 | ✓ |
| M-23 | Diálogo de terminar | `:125-147` | textos fijos `Terminar entrenamiento`, `Se guardará la sesión. ¿Continuar?`, `Cancelar` y `Terminar`; tocar afuera cancela; Strava solo si está configurado; después `reset()` | AC-WKT-20-03, -04 | §6.8 | ✓ (`app_es.arb:61-63` y el catálogo de la app dicen lo mismo) |
| M-24 | `Terminar` a mano con una lectura durante el guardado | `:49-55` + `:302` | aparece el diálogo de completado (Q-53) | AC-WKT-20-04 | §2.11 | ✓ corrección declarada |
| M-25 | La rutina termina con el diálogo de terminar abierto | `:49-65`, `:125-147` | el de completado se apila encima; al cerrarlo, el `pop` de reposo saca el de terminar sin efecto y la página sale con la lectura siguiente | **no está** | **no está** | **✗ P02** |
| M-26 | `Pausa` durante el guardado | `:923-948`, `:239-243` | pasa a `paused` (los controles no se apagan) | AC-WKT-20-06 | §6.8 | ✓ corrección |
| M-27 | Dos toques en `Entrenamiento libre` | `workout_screen.dart:85-94` | dos sesiones, dos *timers* y dos inmersivos | AC-WKT-01-10 (DV-30) | §6.1 | ✓ |
| M-28 | Bluetooth apagado | `workout_screen.dart:50`, `:59-77`; `device_provider.dart:94`; `ble_service.dart:8` | aviso: cualquier estado ≠ `connected`, también `off` | AC-BLE-10-03 enumera tres | §2.11 (`IsConnected`) | ⚠ P07 |
| M-29 | `Sprint` (tipo desconocido) | `interval_step.dart:133`; `routines_provider.dart:19-25`; `workout_screen.dart:129-138`; `routines_screen.dart:24-26` | la carga lanza. Workout muestra `Creá una rutina…`; Rutinas, el indicador para siempre | DV-23 | §5 (dice "la lista queda cargando") | ✓ spec; O-02 |
| M-30 | Diálogo de inicio | `workout_screen.dart:145-162`; `app_es.arb:67` | título = el nombre; `{summary}\n\nIniciar el entrenamiento?`, sin `¿` | AC-WKT-12-07 (Q-02) | §6.2 | ✓ (sonda) |
| M-31 | Resumen de `Prueba 4a` | `routine.dart:100-105` | `1 series · 1 descansos` | AC-WKT-11-14 (Q-01, Q-02: `1 serie · 1 descanso`) | §2.12 #14 | ✓ (sonda) |
| M-32 | Etiqueta de la repetición 2 de `Prueba 4a` | `interval_step.dart:79-109`; `routine.dart:50-63` | `>160W · 24 spm · 📈 SPM +2/rep` | AC-WKT-11-13 | §2.12 #13 | ✓ (sonda) |
| M-33 | Formatos de los paneles | `series_panels.dart:9-24`; test `:32` | `2:46.7`, `1:05.0`, `1:54`, `1.245 m` | AC-WKT-11-09 a -11 | §2.12 | ✓ (sonda) |
| M-34 | Split en vivo de 6000 s | `rowing_data.dart:24-29` | `100:00` | AC-WKT-11-01 | §2.12 | ✓ (sonda) |
| M-35 | Pantalla encendida | `main.dart:42`; `wakelock_plus_windows_plugin.dart:23`; `Wakelock.kt:24` | Windows: `ES_CONTINUOUS \| ES_DISPLAY_REQUIRED`, una sola vez. Android: `FLAG_KEEP_SCREEN_ON` | AC-APP-14 | §4.4 (DT4a-09 suma `ES_SYSTEM_REQUIRED`) | ✓ |
| M-36 | Alta y cierre de la sesión | `workout_session.dart:39-51`; `database_service.dart:216-229` | alta con los totales en 0 y Strava NULL; cierre con `toMap()` | AC-WKT-06-01, -06 | §3.3 | ✓ |

---

## 5. Los 11 casos legacy (LT-35 a LT-45)

Se comprobó al carácter, con `grep -F`, que el nombre de cada `test('…')` del legacy está en la spec (§6) y en el diseño (§8.3), con su línea. Están todos.

| LT | Legacy | Spec | Diseño (método) | Qué afirma (en el código del legacy) |
|---|---|---|---|---|
| LT-35 | `test/workout_provider_hr_test.dart:80` | AC-WKT-05-01 | `SensorBeatsFtmsAndUpdatesWithoutRowerPackets` | 90; 142 con 100 W y **2** avisos; 142 con 120 W; 0; 130 + `disconnected` → 0; el FTMS → 90 |
| LT-36 | `:125` | AC-WKT-05-02 | `RealDropOrderZeroThenDisconnected` | 140; bpm 0 + `disconnected` → 0; el FTMS → 90 |
| LT-37 | `:148` | AC-WKT-05-03 | `BpmDuringConnectingIsKeptWhenConnected` | `connecting`, 120, `connected` → 120; el FTMS con 90 → 120 |
| LT-38 | `:167` | AC-WKT-05-04 | `ConnectedWithoutPreviousBpmTouchesNothing` | `connected` y `disconnected` → **0** avisos |
| LT-39 | `test/step_target_tracker_test.dart:20` | AC-WKT-09-01 | `MetWhenTheWattsAverageReachesTheMinimum` | 140 y 160 contra 150 → cumplido |
| LT-40 | `:28` | AC-WKT-09-02 | `MissedIfAnyDefinedTargetFails` | 200 W / 22 contra 150 / 26 → no cumplido |
| LT-41 | `:35` | AC-WKT-09-03 | `SplitIsMetBelowTheTarget` | 125 contra 130 → cumplido; 140 → no |
| LT-42 | `:46` | AC-WKT-09-04 | `StepWithoutTargetsHasNoResult` | sin objetivos → null |
| LT-43 | `:53` | AC-WKT-09-05 | `WithoutRowingItIsMissedAndZerosDoNotCount` | sin lecturas → no cumplido; parado + 120 W / 24 / split 0 contra 100 W / split 200 → cumplido |
| LT-44 | `:65` | AC-WKT-09-06 | `UncompletedStepHasNoResultAndResetClears` | null → cumplido → null |
| LT-45 | `test/series_panels_test.dart:32` | AC-WKT-11-09 a -11 | `SeriesPanelsTests.Formats` | `2:46.7`, `1:05.0`, `1:54`, `1.245 m` |

No falta ningún otro test del legacy que toque código de la 4a:

- `test/step_pill_test.dart` es de la 4b.
- `test/series_tracker_test.dart` es de la It. 5 (ver P06), igual que los tres casos de `series_panels_test.dart` que no son "formatos".
- `test/widget_test.dart` está vacío: es un *placeholder* sin afirmaciones.

**Para WP-A (no es un issue).** LT-35 y LT-38 cuentan avisos. Si la prueba se suscribe a `WorkoutEngine.State` y esa suscripción entrega el valor actual, LT-35 contaría 3 y LT-38, 1. Hace falta `emitCurrent: false` también en esa suscripción, no solo en los dobles.

---

## 6. Huecos del spec (lo que el legacy hace y la spec no dice)

| Hueco | Legacy | Issue |
|---|---|---|
| El diálogo de terminar abierto cuando la sesión termina sola | M-25 | P02 |
| El aviso de completado cuando la rutina termina en segundo plano o minimizada | el diálogo de completado es modal y queda hasta que se lo toca (`:207-212`) | P08 |
| El aviso sin remo con el Bluetooth apagado | M-28 | P07 |
| La cuenta y el progreso al terminar con un último paso por distancia | `:31-36`, `:101-104`, `:456-458` | P10 |
| Rutina iniciada sin lectura previa (el saneamiento descarta el primer salto) | M-08 | P06 (prueba); el comportamiento ya está en DV-26 |
| La barra de Workout fuera de reposo | `_ActiveView` sin `AppBar` | P09 |

No se encontraron otros huecos. También se revisaron estos comportamientos, que ya están cubiertos:

- un estado del sensor ≠ conectado después de un bpm 0;
- un bpm sin lecturas del remo;
- terminar desde la pausa;
- un paso de 0 s;
- un paso con tiempo y distancia;
- el remo que se desconecta en libre;
- los textos fijos del HUD;
- la pantalla clásica, que nadie abre: el único `_FullscreenWorkoutPage(` del legacy es su declaración (`workout_screen.dart:206`).

---

## 7. ¿Las diferencias declaradas describen bien al legacy?

**DV de la spec (§11.1).** Las 16 se comprobaron en el código y describen bien al legacy:

| DV | Comprobado en |
|---|---|
| DV-21 | `workout_screen.dart:107-123`: un `ListTile` con el nombre, el resumen de 12 y el *chevron* |
| DV-22 | `:52-127`: solo la lista está en un `Expanded` con `ListView` |
| DV-23 | `:98-138`. Con la carga fallida se ve `Creá una rutina…`, porque `_routines` queda vacía (`routines_provider.dart:19-25`) |
| DV-24 | `immersive_workout_screen.dart:435`: `'Up next: '` |
| DV-25 | `:458`: `'${sp.step.distanceMeters}m'`, que con null da `nullm` |
| DV-26 | `workout_provider.dart:198-201`; los valores, por la sonda |
| DV-27 | `:147`: `Timer.periodic`, que no recupera los disparos perdidos |
| DV-28 | `routine.dart:47` (el bucle por repetición); `workout_provider.dart:100` (`steps[0]`) |
| DV-29 | `:145-147` y `:297-299` |
| DV-30 | `workout_screen.dart:88-89` y `:164-165`, sin `await` |
| DV-31 | `immersive_workout_screen.dart:762-783` (52 / 36 / 28, negro al 55 %) y `:798` (etiqueta al 80 %), `:676-680` (banda al 50 %), `:907-947` (`redAccent`, 120 × 48 y 140 × 48) |
| DV-32 | `:775-782`: borde y halo en el color del semáforo |
| DV-33 | `:639-663` y `:709-733` |
| DV-34 | `:34`, `:217`, `:358-377`, `:474`, `:515-517`; `workout_screen.dart:79-83` |
| DV-35 | `interval_step.dart:105` |
| DV-36 | `workout_screen.dart:134` y `:199` (`white38`) |

**Lo que falta declarar.** Faltan las diferencias que agregan las notas de diseño: P03, P04 y P09.

**Desvíos del Architect (§10.1).**

| Desvío | Qué cambia frente al legacy |
|---|---|
| DT4a-02 | Arranque del reloj. Con la base al día da lo mismo que el legacy, que espera el alta (`:145-147`) |
| DT4a-05, DT4a-06, DT4a-07, DT4a-08, DT4a-14 | Nada que se vea. Una sesión terminada queda idéntica |
| DT4a-09 | Suma `ES_SYSTEM_REQUIRED` a lo que pide el legacy. Lo que el usuario ve no cambia |
| DT4a-10 | Mantiene la conducta de la It. 2 en primer plano |
| DT4a-12 | Duración negativa. El legacy lanza en `clamp(0, d)` (`:103`), y el PO decide la DV |
| DT4a-15 | En el legacy, la vuelta a reposo la hace la pantalla con `reset()`, después del `await finish()` (`:141-147`). Al terminar a mano da lo mismo |
| DT4a-01 | Iguala a Flutter de escritorio, que no enfoca un botón con el clic |

**El oráculo del motor (diseño §3.7).** Es la mejor garantía de AC-WKT-06-07: compara puntos, cierre y objetivos contra el motor legacy real. Las únicas diferencias esperadas son las de Q-55. Ninguno de sus escenarios toca P01 (objetivo de split 0) ni DT4a-12.

---

## 8. AC sin test automático (lista del Architect), desde la paridad

| AC | Comentario |
|---|---|
| AC-WKT-02-09 | De acuerdo, solo [Device-Win]. En Windows minimizada el legacy (Flutter de escritorio) sigue con sus *timers*, y la app sigue por `runInBackground`. La lógica es la de primer plano, ya probada en dotnet. Lo propio del equipo es que Unity no frene el *loop* ni la base. Falta qué aviso se ve si la rutina termina mientras tanto (P08) |
| AC-WKT-02-10 | De acuerdo: NO VERIFICABLE sin módulo ni teléfono. WBG cubre en dotnet el mismo `FrameScheduler` |
| AC-APP-14-02 | De acuerdo. El legacy pide la pantalla una sola vez, para el proceso (`main.dart:42`), y no la vuelve a pedir. El diseño la vuelve a pedir al volver, que es más robusto |
| AC-WKT-06-14 | De acuerdo con [QA]. El legacy no tiene dónde guardarlo: sus tablas son las de `database_service.dart:106-138`. `SSR.SchemaStillV8` ya lo cubre en parte |
| Partes en equipo de AC-APP-14-01 y -03, AC-WKT-02-08, AC-WKT-06-05 y -10, AC-WKT-13-03 y AC-APP-05-07 | De acuerdo. Cada uno tiene además su prueba automática (WLK, WBU, SSR, HCT, `TabOrder`) |

---

## 9. Observaciones sin issue

- **O-01 · El origen del latido.** Las notas (§5.4) ponen el origen del reloj de 600 + 600 ms en el primer cuadro con "a continuación" o "por terminar". El legacy lo crea con la barra, al empezar la rutina, y no lo reinicia (`immersive_workout_screen.dart:317-322`). Es solo un corrimiento de fase, que no se ve. La rampa de color de 150 ms (legacy: `Color.lerp` con el pulso, que puede saltar) es una mejora EXR.
- **O-02 · `Sprint` en el diseño §5.** La tabla dice "en el legacy, la lista queda cargando". Es cierto en Rutinas (`routines_screen.dart:24-26`). En Workout el legacy muestra `Creá una rutina en la pestaña "Rutinas"` (`workout_screen.dart:129-138`), como bien dice DV-23. Es una corrección de redacción.
- **O-03 · AC-WKT-02-02 (2).** Dice "y la sesión existe". El diseño toma el punto también con el alta en vuelo (§2.3) y lo descarta si el alta falla. Con el tope de 100 ms (DT4a-02) no se ve ninguna diferencia, y el legacy cumple las dos lecturas porque su *timer* espera el alta.
- **O-04 · La hora de fin.** El legacy la toma después de escribir los puntos (`:279-288`); el diseño, en el segundo del cierre. Son milisegundos, y con el reloj de prueba coinciden (`2026-10-28T13:00:12.300Z`).
- **O-05 · La desconexión simulada.** La del remo y la del pulsómetro no mandan lecturas en cero, salvo el bpm 0 del sensor antes de `disconnected`. Por eso "el HUD queda con la última lectura" y "la tarjeta BPM desaparece" son paridad (M-11, M-12).

---

## 10. Verificación hecha

1. **Legacy, solo lectura.** `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart`.
2. **Sin Unity.** Unity no se abrió.
3. **dotnet solo en el scratch.** dotnet compiló y corrió la sonda en `<scratch>/it4a/gate-paridad/` (§1.2), con `--artifacts-path` propio. Se comprobó que no quedaron `obj/` ni `bin/` en `tools/dotnet/`.
4. **Un solo archivo escrito.** En el repo solo se escribió este archivo. No se hicieron *commits*.
5. **Sin rutas de la PC.** Este archivo cita el repo con rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy), el scratch como `<scratch>` y la caché de Dart como `<pub-cache>`. No tiene rutas absolutas, nombres de usuario ni datos del equipo.
