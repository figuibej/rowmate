# Inventario de producto: Historial, detalle de sesión y persistencia local

- **Área:** `HIS` (IDs `F-HIS-NN`).
- **Autor:** rowmate-po. **Fecha:** 2026-10-27.
- **Fuente de verdad:** `https://github.com/figuibej/rowmate-legacy` (solo lectura). Cuando el código y los docs difieren, manda el código. Las diferencias están en la sección 4.
- **Contrato de paridad:** lo que no figure acá se pierde en la migración. Por defecto se replica **todo**, defectos incluidos. Cada defecto está marcado **[DEFECTO LEGACY]** y en la sección 7 tiene una propuesta de cambio de producto, que necesita el OK del usuario.

---

## 0. Alcance

**Incluye**
- Cómo se graba una sesión en la base local: alta al empezar, muestreo de telemetría y cierre.
- Estadísticas de sesión y formatos de display.
- Pantalla **Historial**, con lista, tarjetas, estados y recarga.
- Pantalla **Detalle de sesión**, con resumen, rendimiento por paso, selector de métrica, gráfico, bandas de pasos y líneas objetivo.
- Indicador de Strava y subida manual desde el detalle.
- Base de datos local: archivo, esquema, versiones, consultas y borrados. Incluye la persistencia de rutinas a nivel datos, la base separada del simulador, la reparación de sesiones, la limpieza de sesiones de Strava y cómo se ven en el historial las sesiones importadas de Strava.

**No incluye**, porque va en otros inventarios:
- Motor de entrenamiento y avance de pasos (`WKT`).
- Edición de rutinas, aplanado y progresiones (`RUT`).
- OAuth, TCX, subida y descarga de Strava (`STR`).
- Pantalla Perfil (`PRF`).
- Simulador (`SIM`).
- Pulsómetro (`HRM`).

Acá solo se describe lo que esas áreas **leen o escriben** en el historial.

**Archivos legacy leídos completos:**
- `lib/features/history/history_provider.dart`, `history_screen.dart` y `session_detail_screen.dart`.
- `lib/core/database/database_service.dart`.
- `lib/core/models/workout_session.dart`, `routine.dart`, `interval_step.dart` y `rowing_data.dart`.
- `lib/features/workout/workout_provider.dart`.
- `lib/features/profile/profile_provider.dart`.
- `lib/features/routines/routines_provider.dart`.
- `lib/main.dart`, `lib/shared/theme.dart` y `lib/core/dev/dev_config.dart`.
- `lib/core/dev/rowing_simulator.dart`.
- `test/session_stats_test.dart` y `test/routine_positions_test.dart`.
- Las partes relevantes de `profile_screen.dart`, `workout_screen.dart`, `immersive_workout_screen.dart`, `strava_config.dart` y `tcx_builder.dart`.
- `lib/l10n/app_es.arb` y `app_en.arb`.
- `CLAUDE.md`, `README.md` y `docs/superpowers/specs/*.md`.
- Capturas `docs/image-05.png` (detalle) y `docs/image-06.png` (historial).

**Glosario**
- **Sesión:** un entrenamiento grabado, libre o con rutina.
- **Punto:** una muestra de telemetría tomada cada 5 s de remo activo.
- **Pasos expandidos:** la lista de pasos de la rutina con las series repetidas y las progresiones aplicadas (ver `F-RUT`). El "índice de paso" de un punto es la posición, base 0, en esa lista.
- **p99 (como lo calcula el legacy):** se descartan los valores ≤ 0, se ordena ascendente y se toma el elemento en la posición `piso((n − 1) × 0,99)`, base 0, sin interpolar (ver F-HIS-04).
- **Contadores del monitor:** tiempo, distancia, paladas y calorías **acumulados que informa el monitor**. No son deltas de la sesión.

---

## 1. Mapa de funcionalidades

| ID | Funcionalidad | Lógica pura | Tests legacy |
|---|---|---|---|
| F-HIS-01 | Alta de la sesión al empezar | No | — |
| F-HIS-02 | Muestreo de telemetría cada 5 s | Sí (regla de muestreo) | — |
| F-HIS-03 | Cierre y guardado de la sesión | Sí (armado de totales) | — |
| F-HIS-04 | Estadísticas de sesión (p99, totales, pulso) | Sí | `session_stats_test.dart` (3 casos) |
| F-HIS-05 | Formatos de duración, ritmo, fecha y números | Sí | — |
| F-HIS-06 | Acceso al Historial y recargas | No | — |
| F-HIS-07 | Lista de sesiones (pantalla Historial) | No | — |
| F-HIS-08 | Indicador de sincronización con Strava | No | — |
| F-HIS-09 | Detalle: apertura, carga y estados | No | — |
| F-HIS-10 | Detalle: Resumen | No (usa F-HIS-04) | indirecto: `session_stats_test.dart` |
| F-HIS-11 | Detalle: Rendimiento por paso | Sí (cálculos) | — |
| F-HIS-12 | Detalle: selector de métrica | No | — |
| F-HIS-13 | Detalle: gráfico de la métrica | Sí (escala, inversión, ejes) | — |
| F-HIS-14 | Detalle: bandas y separadores de pasos | Sí (cálculo de bandas) | — |
| F-HIS-15 | Detalle: líneas objetivo por paso | Sí (cálculo de objetivos) | indirecto: `routine_positions_test.dart` |
| F-HIS-16 | Detalle: subida manual a Strava | No | — |
| F-HIS-17 | Base local: archivo, esquema y versiones | No | — |
| F-HIS-18 | Persistencia de rutinas (capa de datos) | No | — |
| F-HIS-19 | Consultas de sesiones y telemetría | No (salvo agregación) | — |
| F-HIS-20 | Base separada en modo simulador | No | — |
| F-HIS-21 | Reparación de sesiones incompletas | Sí (regla de reparación) | — |
| F-HIS-22 | Limpieza de sesiones vinculadas a Strava | No | — |
| F-HIS-23 | Sesiones importadas de Strava dentro del historial | Sí (cálculo de ritmo) | — |

---

## 2. Datos de una sesión (vista de producto)

**Sesión**

| Campo | Tipo | Notas |
|---|---|---|
| id | entero | Autoincremental. No se reutiliza. |
| rutina (id) | entero o vacío | Vacío = entrenamiento libre o sesión importada de Strava. |
| nombre de rutina | texto o vacío | Copia del nombre al empezar. En las importadas es el nombre de la actividad de Strava. |
| inicio | fecha-hora | Guardada en UTC (ISO-8601 con `Z`). Se muestra en hora local. |
| fin | fecha-hora o vacío | Vacío = nunca se cerró. |
| distancia total (m) | entero | Default 0. |
| tiempo total (s) | entero | Default 0. |
| "potencia media" (W) | entero | Default 0. **Guarda el p99 de la potencia, no la media.** |
| "spm medio" | decimal | Default 0. **Guarda el p99 del SPM, no la media.** |
| calorías totales (kcal) | entero | Default 0. |
| id de actividad Strava | texto o vacío | Vacío = no vinculada. |

**Punto de telemetría**

| Campo | Tipo | Notas |
|---|---|---|
| sesión | entero | Obligatorio. |
| tiempo transcurrido (s) | entero | Contador del monitor. Obligatorio. |
| paladas por minuto | decimal | Default 0. |
| paladas totales | entero | Default 0. Contador del monitor. No se muestra en el historial. |
| distancia (m) | entero | Default 0. Contador del monitor. |
| ritmo /500 m (s) | entero | Default 0. 0 = sin ritmo. |
| potencia (W) | entero | Default 0. |
| calorías (kcal) | entero | Default 0. Contador del monitor. |
| pulso (bpm) | entero | Default 0. 0 = sin lectura. |
| índice de paso | entero o vacío | Vacío en entrenamiento libre. |
| tipo de paso | texto o vacío | `warmup`, `work`, `rest`, `cooldown` o vacío. En las importadas puede traer el texto de la metadata de Strava. |

---

## 3. Funcionalidades

### F-HIS-01: Alta de la sesión al empezar

**Valor para el usuario:** cada entrenamiento queda registrado desde el primer segundo, con el nombre de la rutina y la hora de inicio.

**Comportamiento observable**
1. Al iniciar un entrenamiento, libre o con rutina, se crea **en el acto** una sesión con estos valores:
   - rutina (id) y nombre de la rutina original, sin expandir; ambos vacíos en libre;
   - inicio = momento en que se inicia, según el reloj del dispositivo;
   - fin vacío;
   - todos los totales en 0;
   - sin vínculo con Strava.
2. El nombre de la rutina se **copia** en la sesión. Renombrar o borrar la rutina después no cambia el nombre que muestra el historial.
3. El inicio se guarda en UTC y siempre se muestra convertido a la hora local vigente del dispositivo.
4. Se vacía la lista en memoria de puntos y el contador de muestreo vuelve a 0.

**Casos borde y errores**
- La sesión existe aunque el usuario termine sin remar. En ese caso aparece en el historial, ver F-HIS-03.
- Si falla el alta, no hay manejo visible: el entrenamiento queda marcado activo, pero no cuenta tiempo. Es un comportamiento indefinido. No replicar el cuelgue: ver la pregunta abierta Q-14.

**Datos persistidos:** una fila nueva de sesión con rutina (id), nombre de rutina e inicio. Fin vacío y totales en 0.

**Fuente legacy:**
- `lib/features/workout/workout_provider.dart:133-149`; el alta está en `:145`.
- `:120-131`: usa la rutina original para el nombre y el id (`:130`).
- `lib/core/models/workout_session.dart:39-51`; el inicio en UTC está en `:43`.

**Tests legacy:** ninguno.

**Dependencias:**
- `F-WKT`: iniciar entrenamiento libre o con rutina.
- `F-RUT`: rutina.
- F-HIS-17.

**Mejora EXR:** empezar se siente como salir a la pista. La sesión queda "en el agua" sin demora perceptible y el registro arranca sin que el usuario tenga que hacer nada. Nunca se pierde el comienzo de un entrenamiento.

---

### F-HIS-02: Muestreo de telemetría cada 5 s

**Valor para el usuario:** queda una traza completa del entrenamiento para revisarla después en gráficos, por paso, y para mandarla a Strava.

**Comportamiento observable**
1. Mientras el entrenamiento está **activo**, cada segundo suma 1 a un contador de muestreo. Cuando el contador es múltiplo de 5 (5, 10, 15…), se toma una foto de la **última lectura** disponible y se agrega como punto.
2. En **pausa** no corre el reloj: no se muestrea y el contador queda congelado. Al reanudar sigue desde donde estaba, así que los puntos se toman cada 5 s **de entrenamiento activo**, no de reloj de pared.
3. Contenido del punto:
   - tiempo transcurrido **que informa el monitor**, no el reloj de la app;
   - SPM con decimales, paladas totales, distancia, ritmo /500 m, potencia y calorías;
   - pulso, ver punto 4;
   - con rutina: índice del paso actual en los pasos expandidos y su tipo (`warmup`, `work`, `rest` o `cooldown`). En libre, ambos vacíos.
4. **Pulso:** si hay pulsómetro conectado, manda su valor. Si no, vale el que traiga el monitor. 0 significa "sin lectura".
5. El punto del segundo en que se completa un paso **pertenece al paso que termina**, porque se muestrea antes de avanzar. Ejemplo: en un paso de 60 s, los puntos 5, 10, …, 60 son de ese paso, en total 12 puntos.
6. Los puntos quedan **solo en memoria** hasta terminar. Durante el entrenamiento no se escribe nada en la base.

**Casos borde y errores**
- Si el último paquete del monitor no trae un campo, ese campo queda en 0 en el punto. Por eso aparecen picos a 0 en los gráficos y distancias que "caen" a 0.
- **Contadores absolutos del monitor:** si el monitor no se reinició entre sesiones, la sesión arranca con el tiempo, la distancia y las calorías acumulados de antes. El simulador nunca se reinicia. Evidencia en `docs/image-05.png`: la rutina duró 3:45 (2:00 + 1:45 por paso), pero el resumen muestra 07:20 y 1657 m, y el gráfico arranca en 3:44.
- Si la app se cierra o se cuelga durante el entrenamiento, **se pierden todos los puntos**. La sesión queda huérfana, ver F-HIS-03.
- Un paso que dura menos de 5 s puede no tener ningún punto.

**Datos persistidos:** ninguno durante el entrenamiento. Los puntos se escriben al cerrar (F-HIS-03).

**Fuente legacy:**
- `lib/features/workout/workout_provider.dart:151-173` (muestreo) y `:72-76` (lista en memoria).
- `:220-237` (fusión del pulso) y `:239-250` (pausa y reanudar).
- `lib/core/models/rowing_data.dart:2-21`.

**Tests legacy:**
- No hay tests del muestreo en sí.
- `test/workout_provider_hr_test.dart` cubre la fusión de pulso que termina en el punto (área `HRM`/`WKT`):
  - `:80`: el pulsómetro manda sobre el FTMS y se actualiza sin paquetes del remo;
  - `:125`: si se cae el sensor, el bpm pasa a 0;
  - `:148`: el bpm recibido durante "conectando" se conserva;
  - `:167`: "conectado" sin bpm previo no toca los datos.

**Dependencias:**
- `F-WKT`: reloj de 1 s, fases, avance de pasos.
- `F-BLE`: lectura del monitor.
- `F-HRM`: pulso.
- `F-RUT`: pasos expandidos.

**Mejora EXR:** el registro es invisible y confiable, como una caja negra. El usuario no piensa en "guardar". La traza tiene resolución suficiente para que el gráfico se vea fluido y fiel a lo que pasó en el agua.

---

### F-HIS-03: Cierre y guardado de la sesión

**Valor para el usuario:** al terminar, el entrenamiento queda guardado con sus totales y aparece en el historial.

**Comportamiento observable**
1. **Disparadores**
   - (a) El usuario confirma terminar. El gesto "atrás" de la pantalla de entrenamiento abre el mismo diálogo de confirmación.
     - En la pantalla de inicio de entrenamiento el diálogo usa `workoutFinishTitle` y `workoutFinishContent`, con los botones `cancel` y `workoutFinish`.
     - En la pantalla inmersiva los textos están fijos en español: "Terminar entrenamiento", "Se guardará la sesión. ¿Continuar?", "Cancelar" y "Terminar".
   - (b) La rutina completa su último paso y el cierre es automático.
2. **Al cerrar**
   - Se agrega un **último punto** con la lectura actual, con el índice y el tipo del paso actual si hay rutina.
   - Se calculan las estadísticas (F-HIS-04) sobre **todos** los puntos.
   - Se escriben todos los puntos de una sola vez.
   - Se actualiza la sesión:
     - fin = ahora;
     - distancia total = máximo de distancia;
     - tiempo total = máximo de tiempo;
     - "potencia media" = p99 de la potencia;
     - "spm medio" = p99 del SPM;
     - calorías = máximo de calorías;
     - sin vínculo con Strava.
3. La sesión queda como **"última sesión terminada"**, que usa la subida automática a Strava (`F-STR`).
4. Después se reinicia el entrenamiento: fase en reposo y lista de puntos vacía.

**Casos borde y errores**
- **Error al guardar:** se registra en el log interno y se ignora. El usuario no ve ningún aviso y la app sigue como si se hubiera guardado.
- **Cierre antes de los primeros 5 s:** la sesión queda con un solo punto, el final.
- **Cierre justo en un múltiplo de 5 s de remo activo:** el punto final **duplica** el último punto, con el mismo tiempo. Pasa siempre en el cierre automático de una rutina cuyos pasos son todos por tiempo y duran múltiplos de 5 s (las pausas no cambian esto, porque el contador solo suma segundos activos), ya que el cierre ocurre en el mismo segundo del último muestreo. En "Rendimiento por paso" eso suma 5 s de más al último paso.
- **Sesiones huérfanas:** si la app se cierra durante el entrenamiento, la sesión queda para siempre sin fin, con totales en 0 y sin puntos.
  - En el historial se ve como "Libre" o el nombre de la rutina, con `00:00 · 0m · 0 · 0.0 · --:--`.
  - El detalle muestra `sessionNoTelemetry`.
  - **Nunca se repara**, porque F-HIS-21 necesita puntos.
  - Evidencia: las varias tarjetas "Free 00:00 0m" de `docs/image-06.png`.
- Si falla entre escribir los puntos y actualizar la sesión, queda una sesión con puntos pero sin fin, que sí es reparable (F-HIS-21).

**Datos persistidos:** todos los puntos de la sesión, más la actualización completa de la fila de la sesión.

**Fuente legacy:**
- `lib/features/workout/workout_provider.dart`:
  - `:252-303` (cierre), `:258-273` (punto final), `:276` (estadísticas);
  - `:279` (escritura de puntos), `:283-295` (actualización), `:296` (última terminada), `:297-299` (error ignorado);
  - `:203-216` (cierre automático) y `:305-318` (reinicio).
- `lib/features/workout/workout_screen.dart:532-556` (diálogo localizado).
- `lib/features/workout/immersive_workout_screen.dart:67-70` ("atrás" abre la confirmación) y `:125-147` (diálogo con texto fijo).

**Tests legacy:** ninguno.

**Dependencias:** F-HIS-04, F-HIS-17, `F-WKT` (fin de rutina), `F-STR` (subida automática a partir de la última terminada).

**Mejora EXR:** terminar se siente como cruzar la meta: hay una confirmación clara de que la sesión quedó guardada y un pasaje sin cortes hacia el resumen. Nunca hay una falla silenciosa: si algo no se pudo guardar, el usuario se entera y no pierde lo remado.

---

### F-HIS-04: Estadísticas de sesión

**Valor para el usuario:** un resumen numérico de cada sesión (tiempo, distancia, potencia, ritmo de paladas, split y pulso) para comparar entrenamientos.

**Comportamiento observable.** La entrada es la lista de puntos de una sesión.

1. **Sin puntos:** tiempo 0, distancia 0, potencia 0, SPM 0.0, split 0 (se muestra `--:--`), calorías 0, pulso medio 0, pulso máximo 0 y "sin pulso".
2. **Potencia (W):** se descartan los puntos con potencia 0 y se calcula el p99, redondeado al entero más cercano (las mitades suben).
3. **SPM:** se descartan los ceros y se calcula el p99. Queda con decimales y se muestra con 1 decimal.
4. **Split (s/500 m):** se descartan los ceros, se calcula el p99 y se redondea.
   - **[DEFECTO LEGACY]** Un ritmo en segundos más alto es más lento. Por eso el "p99 del split" es casi el **split más lento** de la sesión, mientras que en potencia y SPM es casi el máximo. Un descanso lento "arrastra" el split mostrado (ver Q-02).
5. **Posición p99** = `piso((n − 1) × 0,99)` sobre la lista ordenada de menor a mayor, base 0:

   | n | Posición | Valor elegido |
   |---|---|---|
   | 1 | 0 | el único |
   | 2 | 0 | **el menor** |
   | 3 a 101 | n − 2 | el segundo mayor |
   | 102 a 201 | n − 3 | el tercero mayor (por ejemplo, n = 201: posición 198) |
   | 1001 | 990 | el undécimo mayor |

6. **Distancia total** = máximo de la distancia de todos los puntos, ceros incluidos. **Tiempo total** = máximo del tiempo transcurrido. **Calorías** = máximo de las calorías. Son contadores del monitor, no deltas.
7. **Pulso**
   - medio = promedio de los puntos con pulso > 0, redondeado;
   - máximo = el mayor de esos puntos;
   - "hay pulso" ⇔ pulso máximo > 0.

**Ejemplos verificables**

| Entrada | Resultado |
|---|---|
| Pulso `[0, 120, 150, 0, 141]` | medio 137 (= 411/3), máximo 150, hay pulso |
| Pulso `[0, 0]` | medio 0, máximo 0, sin pulso |
| Lista vacía | sin pulso, todo en 0 |
| Potencia `[0, 0, 150]` | p99 = 150 (los ceros no cuentan) |
| Potencia `[100, 120, 140, 160, 180]` | posición 3, p99 = 160 |
| Potencia `[100, 200]` | posición 0, p99 = 100 |
| Split `[120, 125, 130, 180]` | posición 2, p99 = 130, se muestra `02:10` |
| Distancia `[0, 350, 340]` | total 350 (máximo, aunque el último punto valga menos) |

**Casos borde**
- Los puntos con un campo en 0 por paquetes incompletos no afectan la potencia, el SPM, el split ni el pulso, porque se descartan los ceros. Tampoco afectan los máximos.
- Con contadores del monitor que no se reiniciaron, el tiempo, la distancia y las calorías incluyen lo anterior a la sesión (F-HIS-02).

**Datos persistidos:** no persiste por sí misma. Sus resultados se guardan en la sesión al cerrar (F-HIS-03), al reparar (F-HIS-21) y al importar (F-HIS-23).

**Fuente legacy:** `lib/core/models/workout_session.dart:70-137`:
- `:92`: "hay pulso";
- `:111-116`: posición p99;
- `:118-136`: cálculo.

**Tests legacy:** `test/session_stats_test.dart`. Cada punto es un caso del test:
- `:7-12` "avgHeartRate y maxHeartRate ignoran los ceros": con pulsos `[0,120,150,0,141]` afirma pulso medio = 137, pulso máximo = 150 y "hay pulso" verdadero.
- `:14-19` "sin pulso quedan en 0": con `[0,0]` afirma medio 0, máximo 0 y "hay pulso" falso.
- `:21-23` "sin puntos quedan en 0": con la lista vacía afirma "hay pulso" falso.
- No hay tests de potencia, SPM, split, distancia, tiempo ni calorías. Los ejemplos de arriba pasan a ser criterios de aceptación nuevos.

**Dependencias:** la consumen F-HIS-03, F-HIS-07, F-HIS-10, F-HIS-21 y F-HIS-23. También `F-STR`, para el TCX y la reparación.

**Mejora EXR:** los números del resumen tienen que ser creíbles para un remero: el split que se muestra representa cómo remó la sesión, sin que lo deforme un descanso. Las cifras aparecen con una animación de conteo breve y elegante.

---

### F-HIS-05: Formatos de duración, ritmo, fecha y números

**Valor para el usuario:** una lectura consistente y familiar para cualquier remero.

**Comportamiento observable.** Formatos exactos por lugar:

| Dato | Lugar | Formato | Ejemplos |
|---|---|---|---|
| Duración de la sesión | tarjeta, resumen, Perfil → pendientes | Con 1 h o más: `H:MM:SS` (horas sin relleno). Si no: `MM:SS` (minutos con 2 dígitos). | 0 → `00:00`; 440 → `07:20`; 3725 → `1:02:05`; 36000 → `10:00:00` |
| Split de la sesión | tarjeta, resumen | `MM:SS` con 2 dígitos; si es ≤ 0, `--:--` | 135 → `02:15`; 605 → `10:05`; 0 → `--:--` |
| Duración de un paso | rendimiento por paso | `m:ss` (minutos sin relleno) | 120 → `2:00`; 45 → `0:45`; 3600 → `60:00` |
| Split de un paso | rendimiento por paso | `m:ss/500m`, sin caso especial para 0 | 133 → `2:13/500m`; 0 → `0:00/500m` |
| Eje X del gráfico | gráfico | `m:ss`, sin horas | 224 → `3:44`; 3900 → `65:00` |
| Eje Y y tooltip de split | gráfico | `m:ss` | 135 → `2:15` |
| Fecha de la tarjeta | tarjeta | `dd/MM/yyyy  HH:mm` (**dos espacios**), 24 h, hora local, **igual en es y en** | `02/10/2026  13:51` |
| SPM | tarjeta, resumen | 1 decimal con punto, también en español | `24.9`, `0.0` |
| SPM de un paso | rendimiento por paso | 1 decimal + ` spm` | `24.1 spm` |
| Distancia | tarjeta, resumen, paso | entero + `m`, sin separador de miles | `1657m`, `12345m` |
| Potencia | tarjeta, resumen | entero | `157` |
| Potencia de un paso | rendimiento por paso | entero + `W` | `150W` |
| Pulso | tarjeta, resumen | `medio/máximo` | `142/143` |

**Casos borde:** no hay separador de miles ni coma decimal localizada. La fecha no se adapta al idioma.

**Fuente legacy:**
- `lib/core/models/workout_session.dart:29-37`, `:94-109`.
- `lib/features/history/history_screen.dart:74`, `:111-121`.
- `lib/features/history/session_detail_screen.dart:218-224`, `:254-261`, `:433-438`, `:446-462`, `:499-510`.

**Tests legacy:** ninguno.

**Dependencias:** F-HIS-04.

**Mejora EXR:** tipografía numérica de tablero de regata, con dígitos de ancho fijo para que los números no "bailen". Las unidades se ven discretas y las cifras protagonistas.

---

### F-HIS-06: Acceso al Historial y recargas

**Valor para el usuario:** encontrar los entrenamientos pasados siempre actualizados.

**Comportamiento observable**
1. La pestaña **Historial** (`navHistory`, ícono de reloj con flecha) está siempre en la **4.ª posición** de la barra inferior. Con Strava configurado existe una 5.ª pestaña (Perfil).
2. Las pestañas conservan su estado al cambiar entre ellas.
3. La lista y sus estadísticas se recargan completas en estos casos:
   - al arrancar la app;
   - en **cada toque** de la pestaña Historial, aunque ya esté seleccionada;
   - al volver del Detalle;
   - después de "Subir a Strava" o "Sincronizar desde Strava" en el Perfil.
4. **No** se recarga sola en estos casos:
   - al terminar un entrenamiento (se ve cuando el usuario toca la pestaña);
   - después de "Limpiar sesiones de Strava" (F-HIS-22);
   - después de subir desde "Subidas pendientes" del Perfil.
5. Cada recarga pasa por el estado "cargando" (spinner a pantalla completa), así que la lista **parpadea** en cada toque.

**Casos borde y errores:** si la carga falla, el spinner queda para siempre. No hay estado de error.

**Fuente legacy:**
- `lib/main.dart:84` (se crea y carga al arrancar), `:126-135` (orden de pestañas), `:141` (Historial es la 4.ª), `:146-148` (recarga al tocar) y `:166-170`.
- `lib/features/history/history_provider.dart:12-14`, `:23-34`.
- `lib/features/history/history_screen.dart:45-57`.
- `lib/features/profile/profile_screen.dart:242`, `:279`.

**Tests legacy:** ninguno.

**Dependencias:** `F-APP` (navegación principal), `F-PRF` y `F-STR` (recargas después de sincronizar).

**Mejora EXR:** el historial se mantiene vivo y al día sin parpadeos. Al terminar una sesión, la nueva aparece arriba con una entrada animada sutil, y nunca hay una pantalla en blanco con spinner cuando ya había datos.

---

### F-HIS-07: Lista de sesiones (pantalla Historial)

**Valor para el usuario:** ver de un vistazo todos los entrenamientos con sus métricas clave y entrar al detalle.

**Comportamiento observable**
1. Título de la barra superior: `historyTitle`.
2. Estados:
   - **Cargando:** spinner centrado.
   - **Vacío:** ícono de historial de 56 px en blanco al 12%, separación de 12 px y el texto `historyEmpty` en blanco al 38%, todo centrado.
   - **Lista:** una tarjeta por sesión.
3. **Orden:** por inicio, de la más reciente a la más vieja.
4. **Incluye todas las sesiones:** terminadas, huérfanas sin terminar (F-HIS-03) e importadas de Strava (F-HIS-23). No hay paginación, filtros ni búsqueda.
5. **Tarjeta, fila superior:**
   - ícono de remo cian `#00B4D8` de 18 px;
   - nombre = nombre de la rutina, o `historyFree` si no tiene ("Libre"/"Free"), en seminegrita de 15 px;
   - si corresponde, el ícono de Strava (F-HIS-08);
   - fecha (F-HIS-05) en 11 px y blanco al 38%;
   - flecha ">" de 16 px en blanco al 24%.
6. **Tarjeta, fila inferior:** 5 columnas de igual ancho, o 6 si hay pulso. Cada una tiene el valor en negrita de 14 px, que se achica para entrar en una línea, y la etiqueta en 10 px y blanco al 38%:

   | Columna | Etiqueta | Valor | Color del valor |
   |---|---|---|---|
   | Tiempo | `historyStatTime` | duración | blanco |
   | Distancia | `historyStatDistance` | `{n}m` | `#4CC9F0` |
   | Potencia | "Watts" (texto fijo, sin traducir) | p99 W | `#FFD60A` |
   | SPM | "SPM" (texto fijo) | p99 SPM con 1 decimal | `#00B4D8` |
   | Split | "Split" (texto fijo) | p99 split | `#FF9F1C` |
   | Pulso (solo si hay pulso) | `historyStatHeartRate` | `medio/máximo` | `#EF476F` |

7. Los valores se calculan con los **puntos** de la sesión (F-HIS-04). **No** se usan los totales guardados en la sesión. Una sesión sin puntos muestra `00:00`, `0m`, `0`, `0.0` y `--:--`, sin pulso.
8. Tocar la tarjeta (con efecto de onda y radio 12) abre el Detalle. Al volver, se recarga la lista.
9. **Estilo:**
   - fondo de pantalla `#0D1B2A`;
   - lista con margen de 12 px;
   - tarjetas `#1A2E45` con radio 16, sin sombra, separadas 5 px arriba y abajo, con relleno interno de 16 px;
   - 12 px entre las filas de la tarjeta.

**Casos borde y errores**
- Si falla la carga, el spinner queda para siempre.
- Con muchas sesiones, la carga se vuelve lenta: se leen todos los puntos de todas las sesiones de una sola vez (F-HIS-19).
- Una sesión importada de Strava sin streams muestra la tarjeta en 0, aunque la sesión tenga distancia y tiempo de Strava.
- No existen las acciones de borrar, editar, compartir ni deslizar.

**Datos persistidos:** solo lectura.

**Fuente legacy:**
- `lib/features/history/history_screen.dart:12-63` (pantalla y estados), `:65-130` (tarjeta) y `:132-158` (columna).
- `lib/shared/theme.dart:7-30` (tema) y `:32-40` (colores de métricas).

**Tests legacy:** ninguno. Evidencia visual en `docs/image-06.png`.

**Dependencias:** F-HIS-04, F-HIS-05, F-HIS-06, F-HIS-08 y F-HIS-19.

**Mejora EXR:** una bitácora de remo premium.
- Tarjetas con profundidad y materiales, y transición fluida al detalle: la tarjeta "se abre" y no salta.
- Carga con siluetas de tarjeta en lugar de spinner, y desplazamiento suave a 60 fps aun con cientos de sesiones.
- Las sesiones vacías o incompletas se distinguen visualmente de las reales.
- El color de cada métrica se mantiene idéntico en toda la app.

---

### F-HIS-08: Indicador de sincronización con Strava

**Valor para el usuario:** saber qué sesiones ya están en Strava.

**Comportamiento observable**
1. **En la tarjeta del historial:** ícono de nube con tilde de 14 px, naranja Strava `#FC4C02`, justo antes de la fecha. Se muestra solo si el build tiene Strava configurado **y** la sesión tiene id de Strava. Cuenta cualquier valor distinto de vacío, incluido el texto vacío `""`.
2. **En el Detalle:** el mismo ícono, de 20 px, en la barra superior a la derecha, con las mismas condiciones.
3. Sin Strava configurado en el build, nunca se muestra.
4. Las sesiones no sincronizadas **no** tienen indicador (no existe un estado "pendiente").
5. Las sesiones importadas de Strava también lo muestran, porque tienen id.

**Fuente legacy:**
- `lib/features/history/history_screen.dart:96-100`.
- `lib/features/history/session_detail_screen.dart:89-93`.
- `lib/core/strava/strava_config.dart:19-21`: "configurado" significa que el secreto no está vacío ni es el valor de ejemplo.

**Tests legacy:** ninguno.

**Dependencias:** `F-STR`, F-HIS-16 y F-HIS-19.

**Mejora EXR:** un sello de sincronización discreto y reconocible, que aparece con una microanimación cuando la subida termina.

---

### F-HIS-09: Detalle, apertura, carga y estados

**Valor para el usuario:** revisar a fondo un entrenamiento.

**Comportamiento observable**
1. Se abre como pantalla apilada sobre el Historial, con flecha atrás.
2. **Título:** el nombre de la rutina, o `workoutFree` si no tiene ("Entrenamiento libre"/"Free workout"). Es **distinto** del "Libre" de la tarjeta.
3. **Carga:**
   - Se vuelve a leer la sesión por id para tener su estado más actual. Si ya no existe, se usa la recibida.
   - Se leen **todos** los puntos ordenados por tiempo ascendente.
   - Si la sesión tiene rutina y la rutina todavía existe, se toman sus **pasos expandidos actuales**, con progresiones, para las líneas objetivo.
4. **Estados:**
   - **Cargando:** spinner centrado.
   - **Sin puntos:** solo el texto centrado `sessionNoTelemetry`, en blanco al 38%. No se muestra el resumen aunque la sesión tenga totales.
   - **Con puntos:** lista desplazable con relleno de 16 px que contiene:
     1. el Resumen (F-HIS-10) y 20 px de separación;
     2. si algún punto tiene índice de paso, el Rendimiento por paso (F-HIS-11) y 20 px de separación;
     3. el selector de métrica (F-HIS-12) y 8 px de separación;
     4. el gráfico (F-HIS-13 a F-HIS-15), de 220 px de alto.

**Casos borde y errores**
- Si falla la carga, el spinner queda para siempre.
- Las líneas objetivo dependen de la definición **actual** de la rutina (ver F-HIS-15).

**Fuente legacy:** `lib/features/history/session_detail_screen.dart`:
- `:12-33` (pantalla), `:35-54` (carga), `:56-61` (relectura);
- `:85-87` (título), `:122-142` (estados) y `:145` ("hay datos de paso").

**Tests legacy:** ninguno.

**Dependencias:** F-HIS-19, F-HIS-18 (rutina por id) y `F-RUT` (pasos expandidos).

**Mejora EXR:** el detalle se siente como el análisis post-regata de una app profesional. Las secciones entran en cascada suave y el gráfico se dibuja de izquierda a derecha al abrir.

---

### F-HIS-10: Detalle, Resumen

**Valor para el usuario:** los números clave de la sesión en la cabecera del análisis.

**Comportamiento observable**
1. Es una tarjeta con el título `sessionSummary` (negrita, 16 px) y 12 px de separación.
2. Muestra la misma fila de 5 o 6 columnas, con las mismas etiquetas, colores, formatos y reglas que la tarjeta del historial (F-HIS-07 punto 6). Se calcula con los puntos de la sesión (F-HIS-04).
3. Las etiquetas "Watts", "SPM" y "Split" son texto fijo. Tiempo, Distancia y Pulso salen de los `.arb`.

**Fuente legacy:** `lib/features/history/session_detail_screen.dart:149-179` (resumen) y `:537-564` (columna).

**Tests legacy:** indirecto, `test/session_stats_test.dart`, por el pulso medio y máximo y "hay pulso" (ver F-HIS-04). Evidencia visual en `docs/image-05.png`.

**Dependencias:** F-HIS-04 y F-HIS-05.

**Mejora EXR:** tablero de resultados con jerarquía clara (tiempo y distancia como protagonistas), cifras que cuentan hacia su valor al entrar y color por métrica coherente con el HUD del entrenamiento.

---

### F-HIS-11: Detalle, Rendimiento por paso

**Valor para el usuario:** ver cómo rindió en cada intervalo de la rutina.

**Comportamiento observable**
1. Se muestra **solo si algún punto tiene índice de paso**: sesiones con rutina, o importadas de Strava con vueltas.
2. Es una tarjeta con el título `sessionStepBreakdown` (negrita, 16 px) y 12 px de separación.
3. Los puntos se agrupan por índice de paso y las filas se ordenan por índice ascendente. Hay **una fila por paso que tenga al menos un punto**. Los pasos sin puntos no aparecen, por ejemplo uno de menos de 5 s.
4. **Fila:**
   - **Contenedor:** fondo blanco al 4%, radio 8, relleno horizontal de 10 y vertical de 8, y 6 px de separación inferior.
   - **Insignia:** cuadrado de 28×28 px con radio 6 y fondo cian `#00B4D8` al 16%. Muestra el número de paso (índice + 1) en cian, negrita de 12 px.
   - **Línea 1** (11 px, blanco al 54%): `{duración}  ·  {distancia}m`, con dos espacios a cada lado del punto medio.
   - **Línea 2** (12 px, seminegrita, blanco): `{W}W`, `{spm} spm` y `{split}/500m`, separados por 12 px.
5. **Cálculos por paso.** Los promedios son simples e **incluyen los ceros**, a diferencia de F-HIS-04:
   - potencia = promedio de todos los puntos del paso, redondeado;
   - SPM = promedio con 1 decimal;
   - split = promedio redondeado al segundo, en formato `m:ss`;
   - duración = cantidad de puntos × 5 s, en formato `m:ss`;
   - distancia = distancia del último punto del paso − distancia del primero.
6. No muestra el tipo de paso, su nombre, el objetivo ni el cumplimiento.

**Ejemplo verificable:** un paso con 24 puntos, potencia promedio 149,6, SPM 24,04 y split promedio 132,5, cuyo primer punto está en 500 m y el último en 933 m, se muestra así:
- `2:00  ·  433m`
- `150W` `24.0 spm` `2:13/500m`

**Casos borde y errores**
- Los ceros (paradas, paquetes incompletos) bajan los promedios. Un split en 0 hace que el promedio parezca **más rápido**.
- La distancia queda subestimada en hasta 5 s de remo, porque no cuenta el tramo entre el último punto del paso anterior y el primero de este. Si un punto trae distancia 0, el resultado puede ser negativo.
- **[DEFECTO LEGACY]** La duración supone 5 s por punto. En las sesiones importadas de Strava (≈ 1 punto por segundo) muestra unas 5 veces la duración real.
- El punto final duplicado (F-HIS-03) suma 5 s al último paso.
- Si un mismo índice aparece en tramos no contiguos, se suman en una sola fila.

**Fuente legacy:** `lib/features/history/session_detail_screen.dart:145`, `:183-212` (tarjeta), `:214-270` (fila y cálculos) y `:566-581` (dato chico).

**Tests legacy:** ninguno. Evidencia visual en `docs/image-05.png` (filas "2:00 · 433m / 150W 24.1 spm 2:13/500m").

**Dependencias:** F-HIS-02 (índice y tipo de paso por punto), F-HIS-05 y F-HIS-23.

**Mejora EXR:** cada paso se presenta como el "parcial" de una regata, con el color de su tipo y un indicador visual de si cumplió el objetivo. Esto último es una mejora que se suma a lo actual, no un reemplazo. Tocar un paso resalta su tramo en el gráfico.

---

### F-HIS-12: Detalle, selector de métrica

**Valor para el usuario:** elegir qué métrica ver en el gráfico.

**Comportamiento observable**
1. Es un control segmentado de 5 opciones con texto de 11 px. En orden:
   1. `sessionChartWatts`
   2. `sessionChartSpm`
   3. `sessionChartSplit`
   4. `sessionChartDistance`
   5. `sessionChartHr`
2. La selección es única. La opción elegida se ve con fondo `#0077B6`, texto blanco y un tilde.
3. **Por defecto** se muestra Vatios **cada vez** que se abre el detalle. La selección no se recuerda y no hay clave guardada.
4. Pulso está siempre disponible, aunque la sesión no tenga pulso. En ese caso el gráfico queda plano en 0.

**Fuente legacy:** `lib/features/history/session_detail_screen.dart:25`, `:67-73` y `:274-287`.

**Tests legacy:** ninguno.

**Dependencias:** F-HIS-13.

**Mejora EXR:** el cambio de métrica es una transición animada: la curva se transforma de una métrica a otra en lugar de redibujarse de golpe.

---

### F-HIS-13: Detalle, gráfico de la métrica

**Valor para el usuario:** ver la evolución de la métrica a lo largo de la sesión.

**Comportamiento observable**
1. **Eje X:** el tiempo del monitor de cada punto, en segundos. Arranca en el primer punto, que no necesariamente es 0 (F-HIS-02).
2. **Valores por métrica**
   - Potencia, SPM, distancia y pulso se dibujan tal cual, **con los ceros**: se ven como caídas a 0.
   - **Split invertido:** se dibuja `ritmo máximo de la sesión − ritmo del punto`, para que lo más rápido quede **arriba**. Las etiquetas del eje Y y el tooltip se convierten de vuelta a ritmo `m:ss`.
3. **Eje Y:**
   - mínimo = 0;
   - máximo = 1,1 × el mayor valor entre los datos y las líneas objetivo (F-HIS-15);
   - si ese máximo es ≤ 0, se usa 100.
4. **Curva:**
   - línea suavizada (curvatura leve, 0,2) de 2 px;
   - color de la métrica: potencia `#FFD60A`, SPM `#00B4D8`, split `#FF9F1C`, distancia `#4CC9F0`, pulso `#EF476F`;
   - relleno debajo de la curva del mismo color al 12%;
   - sin marcadores de punto.
5. **Grilla y bordes:** solo líneas horizontales, en blanco al 10% y de 0,5 px. Sin borde.
6. **Eje Y (izquierdo):**
   - 40 px reservados, texto de 9 px en blanco al 38%;
   - valores enteros, o `m:ss` para el split.
   - Las etiquetas incluyen el mínimo y el máximo del eje. Ejemplo de `image-05`: 0, 50, 100, 150 y 157.
7. **Eje X (abajo):**
   - 24 px reservados, texto de 9 px en blanco al 38%, formato `m:ss`;
   - la separación de las etiquetas depende del tiempo del **último punto**: menos de 300 s → cada 30 s; menos de 600 s → cada 60 s; menos de 1800 s → cada 120 s; si no, cada 300 s.
   - Se muestran también el inicio y el fin. Ejemplo de `image-05`: 3:44, 4:00, 5:00, 6:00, 7:00 y 7:20.
8. **Tooltip** al tocar o al pasar el cursor:
   - formato `{valor} {unidad}`, con el valor a 1 decimal y las unidades `W`, `spm`, `m` y `bpm`. Ejemplos: `150.0 W`, `1657.0 m`;
   - para el split: `m:ss s`, por ejemplo `2:15 s`;
   - texto blanco, de 12 px, en seminegrita.

**Casos borde y errores**
- **[DEFECTO LEGACY]** En el split, los puntos en 0 (parado) se dibujan **arriba de todo**, como si fueran el ritmo más rápido.
- Con un split sin ningún ritmo, el gráfico queda plano en 0 y el eje se estira a 100. Las etiquetas del eje Y por encima del ritmo máximo dan ritmos sin sentido, por ejemplo `0:50` donde correspondería un valor negativo.
- Con un solo punto, el gráfico queda degenerado (un punto).
- Como los contadores no se reinician, el eje X puede empezar en minutos avanzados.
- El tooltip puede listar también el valor de una línea objetivo cuando se toca cerca de sus extremos.

**Fuente legacy:** `lib/features/history/session_detail_screen.dart`:
- `:291-311` (serie y transformación del split), `:334-350` (colores y unidades);
- `:390-402` (escala Y), `:404-483` (ejes, grilla y curva), `:495-519` (tooltip) y `:525-532` (separación del eje X).

Colores: `lib/shared/theme.dart:32-40`.

**Tests legacy:** ninguno. Evidencia visual en `docs/image-05.png`.

**Dependencias:** F-HIS-12, F-HIS-14, F-HIS-15 y F-HIS-05.

**Mejora EXR:**
- Gráfico de análisis profesional, con trazo nítido y suavizado.
- Cursor que sigue el dedo y muestra todas las métricas del instante.
- Zoom y desplazamiento con gestos naturales, y animación de dibujado al abrir.
- Las paradas se distinguen como "sin remo" y no como picos de rendimiento.

---

### F-HIS-14: Detalle, bandas y separadores de pasos en el gráfico

**Valor para el usuario:** ubicar cada intervalo de la rutina sobre la curva.

**Comportamiento observable**
1. Solo aparecen si hay datos de paso (F-HIS-09).
2. **Armado de bandas:**
   - Se recorren los puntos en orden. Cada vez que aparece un índice de paso distinto al de la banda abierta, se cierra la anterior en el tiempo de ese punto y se abre una nueva desde ahí.
   - El tipo de la banda es el del punto que la abre; si no tiene tipo, vale `work`.
   - La última banda termina en el tiempo del **último punto de la sesión**.
   - Los puntos sin índice no cortan bandas.
3. **Fondo de cada banda:** franja vertical del color del tipo al 10%, según esta tabla:

   | Tipo | Color |
   |---|---|
   | `work` | `#EF476F` |
   | `rest` | `#06D6A0` |
   | `warmup` | `#FFD166` |
   | `cooldown` | `#118AB2` |
   | cualquier otro | `#607D8B` |

4. **Separador:** línea vertical punteada (4 px de trazo y 4 de espacio), en blanco al 24% y de 1 px, al inicio de cada banda **salvo la primera**.

**Casos borde**
- Las bandas de las importadas sin tipo se pintan de rojo `work`.
- Si un paso no tiene puntos, no tiene banda: la anterior se estira hasta el siguiente.

**Fuente legacy:**
- `lib/features/history/session_detail_screen.dart:313-332` (armado), `:410-418` (fondos) y `:484-494` (separadores).
- `lib/shared/theme.dart:56-69` (color por tipo).

**Tests legacy:** ninguno.

**Dependencias:** F-HIS-02 y F-HIS-13.

**Mejora EXR:** las bandas tienen el color del tipo de paso, consistente con la barra de pasos del entrenamiento. Al tocar una banda se muestra el resumen de ese paso.

---

### F-HIS-15: Detalle, líneas objetivo por paso

**Valor para el usuario:** comparar lo remado con el objetivo de cada paso.

**Comportamiento observable**
1. Solo aparecen si la sesión tiene rutina y la rutina todavía existe.
2. Para cada banda (F-HIS-14) cuyo índice exista en los pasos expandidos **actuales**:
   - **Vatios:** una línea en el objetivo mínimo y otra en el máximo, las que estén definidas;
   - **SPM:** una línea en el objetivo de SPM;
   - **Split:** una línea en el objetivo de split;
   - **Distancia y pulso:** ninguna.
3. **Estilo:**
   - línea horizontal punteada (8 px de trazo y 5 de espacio), en blanco al 78% y de 2 px, desde el inicio hasta el fin de la banda;
   - relleno debajo de la línea, hasta 0, del color del tipo de paso al 7%.
4. Los objetivos incluyen las progresiones por repetición de las series. Por ejemplo, con SPM 18 y +2 por repetición, la línea queda en 18, 20 y 22.
5. Las líneas cuentan para el máximo del eje Y (F-HIS-13).

**Casos borde y errores**
- **[DEFECTO LEGACY]** En el split, la línea objetivo se dibuja en segundos **sin invertir**, sobre una escala que sí está invertida. Queda en un lugar sin relación con el ritmo objetivo y además estira el eje Y.
- Si se editó la rutina después de la sesión, las líneas reflejan la rutina **nueva**. Si se borró, no hay líneas. Un índice fuera de rango no tiene línea.
- Con mínimo y máximo de vatios, los dos rellenos se superponen.

**Fuente legacy:**
- `lib/features/history/session_detail_screen.dart:42-47` (carga de la rutina), `:352-388` (líneas) y `:392-397` (efecto en el eje).
- `lib/core/models/routine.dart:29-70` (expansión con progresiones).

**Tests legacy:** indirecto, `test/routine_positions_test.dart:6-27`: "las posiciones quedan alineadas con flattenedSteps". Afirma que una rutina de calentamiento + serie ×3 de (trabajo 500 m + enfriamiento 60 s) + enfriamiento se expande en 8 pasos, con posiciones `(null,1,1)`, `('a',1,3)`, `('a',1,3)`, `('a',2,3)`, …, `('a',3,3)` en el índice 6 y `(null,1,1)` en el 7. Es la base de la correspondencia índice → paso.

**Dependencias:** `F-RUT` (pasos expandidos y progresiones), F-HIS-18, F-HIS-13 y F-HIS-14.

**Mejora EXR:** el objetivo se ve como una "zona" en lugar de una línea suelta. Los tramos dentro y fuera de objetivo se distinguen con claridad, como en un análisis de entrenador.

---

### F-HIS-16: Detalle, subida manual a Strava

**Valor para el usuario:** subir a Strava una sesión puntual que todavía no está ahí.

**Comportamiento observable**
1. **Botón** (nube con flecha hacia arriba) en la barra superior. Es visible solo si se cumplen **todas** estas condiciones:
   - Strava está configurado en el build;
   - el usuario está conectado a Strava;
   - la sesión **no** tiene id de Strava (vacío o texto vacío);
   - la sesión **está terminada**, es decir, tiene fin.
2. Su tooltip es `profileUploadToStrava`.
3. Mientras haya **cualquier** subida en curso, incluida la subida masiva del Perfil, el botón muestra un spinner de 18 px con trazo 2 y queda deshabilitado.
4. **Al tocarlo** se sube la sesión.
   - Si sale bien, se relee la sesión: aparece el ícono de nube con tilde (F-HIS-08), desaparece el botón y se muestra el aviso `profileUploaded`.
   - Si falla, se muestra el aviso `profileUploadFailed`.
5. Al volver al historial, la tarjeta ya muestra el ícono gracias a la recarga.

**Casos borde y errores**
- En **modo simulador** el botón se muestra igual, pero la subida siempre falla y aparece "Error al subir".
- Con id de Strava igual a texto vacío se muestran **a la vez** el ícono de sincronizada y el botón de subir.
- Las sesiones huérfanas no muestran el botón, porque no tienen fin.

**Datos persistidos:** id de actividad de Strava en la sesión (F-HIS-19).

**Fuente legacy:**
- `lib/features/history/session_detail_screen.dart:56-61` y `:74-120`.
- `lib/features/profile/profile_provider.dart:88-137`.
- `lib/core/strava/strava_api_service.dart:75-81` (bloqueo en simulador).

**Tests legacy:** ninguno.

**Dependencias:** `F-STR` (subida y TCX), `F-PRF` (conexión) y F-HIS-08.

**Mejora EXR:** subir se siente como "publicar la regata": el progreso es visible, la confirmación llega con una animación del sello Strava y los errores tienen un mensaje claro y la opción de reintentar.

---

### F-HIS-17: Base local, archivo, esquema y versiones

**Valor para el usuario:** todo el historial y las rutinas viven en el dispositivo. Funciona sin conexión y sin cuenta.

**Comportamiento observable**
1. Hay una sola base local por instalación, guardada en el lugar estándar de datos de la app de cada plataforma (Android, iOS, macOS, Windows). Su nombre es `rower_app.db`. En modo simulador se usa `rower_app_dev.db` (F-HIS-20).
2. **Versión del esquema: 7.**
3. **Tablas y columnas** (nombre, tipo y restricciones):
   - `routines`: `id` (entero, autoincremental), `name` (texto obligatorio), `description` (texto obligatorio, default `''`), `created_at` (texto obligatorio).
   - `interval_steps`: `id` (autoincremental), `routine_id` (obligatorio), `step_order` (obligatorio), `type` (texto obligatorio), `duration_seconds`, `distance_meters`, `target_watts_min`, `target_watts_max`, `target_spm`, `target_split_seconds`, `group_id` (texto), `group_repeat_count`, `progression_spm`, `progression_watts` y `progression_split_seconds`. Todos los opcionales son enteros, salvo `group_id`.
   - `workout_sessions`: `id` (autoincremental), `routine_id`, `routine_name` (texto), `started_at` (texto obligatorio), `finished_at` (texto), `total_distance_meters`, `total_time_seconds` y `avg_power_watts` (enteros, default 0), `avg_stroke_rate` (decimal, default 0), `total_calories` (entero, default 0) y `strava_activity_id` (texto).
   - `data_points`: `id` (autoincremental), `session_id` (obligatorio), `elapsed_seconds` (obligatorio), `stroke_rate` (decimal, default 0), `stroke_count`, `distance_meters`, `pace_500m_seconds`, `power_watts`, `calories` y `heart_rate` (enteros, default 0), `step_index` (entero) y `step_type` (texto).
4. **Historial de versiones**, que importa si se quieren importar bases del legacy:

   | Versión | Cambio |
   |---|---|
   | 2 | Se agrega `target_split_seconds` a los pasos. |
   | 3 | Se agregan `group_id` y `group_repeat_count` a los pasos. |
   | 4 | Se agrega `step_index` a los puntos y **se borran todas las sesiones y todos los puntos existentes**. |
   | 5 | Se agrega `step_type` a los puntos. |
   | 6 | Se agrega `strava_activity_id` a las sesiones. |
   | 7 | Se agregan `progression_spm`, `progression_watts` y `progression_split_seconds` a los pasos. |

5. **Relaciones:** están declaradas con borrado en cascada (pasos → rutina, puntos → sesión), pero **no se aplican**. En la práctica, borrar una rutina deja sus pasos huérfanos e invisibles. No existe el borrado de sesiones, salvo F-HIS-22, que borra los puntos explícitamente.
6. **Fechas:**
   - inicio y fin de la sesión: ISO-8601 en UTC con `Z` (por ejemplo `2026-10-02T16:51:23.123Z`), convertidos a hora local al leerlos;
   - creación de la rutina: ISO-8601 en hora local, **sin zona**.
7. Los ids autoincrementales no se reutilizan, así que los pasos huérfanos nunca quedan "pegados" a una rutina nueva.

**Casos borde y errores**
- Un `type` de paso desconocido en la base impide leer la rutina.
- Si se cambia la zona horaria del dispositivo, las horas del historial se ven corridas acorde (comportamiento correcto). La creación de las rutinas no se ajusta.

**Fuente legacy:**
- `lib/core/database/database_service.dart:9-28` (archivo y versión), `:30-73` (versiones) y `:75-139` (esquema).
- `lib/core/models/workout_session.dart:39-67`, `:169-182`.
- `lib/core/models/routine.dart:107-122`.
- `lib/core/models/interval_step.dart:111-145`.
- `lib/main.dart:37-41`: en Windows se usa un motor alternativo con el mismo comportamiento.

**Tests legacy:** ninguno.

**Dependencias:** todas las de esta área, más `F-RUT` y `F-STR`.

**Mejora EXR:** robustez de nivel profesional:
- no se pierde ni un entrenamiento por un cierre inesperado;
- las actualizaciones de la app nunca borran el historial (a diferencia de la versión 4 del legacy);
- el historial abre rápido aunque tenga años de datos.

---

### F-HIS-18: Persistencia de rutinas (capa de datos)

**Valor para el usuario:** las rutinas se guardan y se recuperan tal cual se armaron.

**Comportamiento observable**
1. **Listado:** de la más nueva a la más vieja según la creación. Los pasos de cada rutina se ordenan por posición ascendente.
2. **Alta:** se guarda la rutina y sus pasos con posiciones 0…n−1, en el orden del editor.
3. **Edición:** se actualizan el nombre y la descripción, y se **reemplazan todos los pasos**. Se borran los anteriores y se guardan los nuevos con posiciones 0…n−1 renumeradas e ids nuevos. La fecha de creación se mantiene.
4. **Baja:** se borra solo la rutina. Sus pasos quedan huérfanos y ocultos. Las sesiones que la usaron conservan el nombre y quedan apuntando a una rutina inexistente, así que el detalle no muestra líneas objetivo (F-HIS-15).
5. **Lectura por id:** devuelve la rutina con sus pasos, o "no existe". La usan el detalle y la subida a Strava.
6. El tipo de paso se guarda como texto: `warmup`, `work`, `rest` o `cooldown`.

**Casos borde:** editar una rutina cambia retroactivamente las líneas objetivo de las sesiones viejas (F-HIS-15).

**Fuente legacy:**
- `lib/core/database/database_service.dart:141-212`.
- `lib/features/routines/routines_provider.dart:82-115`.
- `lib/core/models/routine.dart:107-122`.
- `lib/core/models/interval_step.dart:111-145`.

**Tests legacy:** ninguno de persistencia. La expansión está cubierta por `test/routine_positions_test.dart` (área `RUT`).

**Dependencias:** `F-RUT` y F-HIS-17.

**Mejora EXR:** las rutinas nunca se pierden ni se desordenan. Lo que el usuario armó es exactamente lo que encuentra.

---

### F-HIS-19: Consultas de sesiones y telemetría

**Valor para el usuario:** que el historial, el detalle y Strava vean siempre los mismos datos.

**Comportamiento observable**
1. **Todas las sesiones:** ordenadas por inicio descendente.
2. **Sesión por id:** devuelve la sesión o "no existe".
3. **Puntos de una sesión:** ordenados por tiempo ascendente. Entre puntos con el mismo tiempo, el orden queda indefinido.
4. **Estadísticas para la lista:**
   - Se leen **de una vez** los puntos de todas las sesiones listadas, sin el índice ni el tipo de paso, y se calcula F-HIS-04 por sesión.
   - Las sesiones sin puntos no tienen estadísticas, así que muestran los valores por defecto.
   - Si falta el pulso, se toma como 0.
5. **Vincular con Strava:** guarda el id de la actividad de Strava (texto) en la sesión.
6. **Quiénes usan los totales guardados en la sesión** (no la UI del Historial ni del Detalle, que recalculan desde los puntos):
   - la tarjeta "Subidas pendientes" del Perfil, que muestra la duración guardada;
   - la exportación TCX a Strava;
   - la reparación (F-HIS-21).
7. **Subidas pendientes** (Perfil) = sesiones sin id de Strava **y** terminadas.

**Casos borde:**
- Con cientos de sesiones largas, la carga de la lista lee cientos de miles de puntos y se vuelve lenta.
- No hay paginación.

**Fuente legacy:**
- `lib/core/database/database_service.dart:214-322`.
- `lib/features/history/history_provider.dart:20-34`.
- `lib/features/profile/profile_screen.dart:327-336`, `:392`.
- `lib/core/strava/tcx_builder.dart:179`.

**Tests legacy:** ninguno.

**Dependencias:** F-HIS-04, F-HIS-17, `F-STR` y `F-PRF`.

**Mejora EXR:** el historial abre al instante, con los resúmenes listos aunque haya años de entrenamientos.

---

### F-HIS-20: Base separada en modo simulador

**Valor para el usuario (desarrollador):** probar sin ensuciar el historial real.

**Comportamiento observable**
1. Con el modo simulador, que solo existe en builds de desarrollo, se usa la base `rower_app_dev.db` en lugar de `rower_app.db`. Las sesiones de prueba nunca se mezclan con las reales.
2. Las reglas, el esquema y las pantallas son los mismos. El único aviso visible es el cartel global "SIMULADOR".
3. Las subidas a Strava fallan siempre en este modo (F-HIS-16). La importación desde Strava sí funciona y guarda en la base de desarrollo.

**Fuente legacy:**
- `lib/core/database/database_service.dart:10-11`.
- `lib/core/dev/dev_config.dart:1-5`.
- `docs/superpowers/specs/2026-10-01-dev-simulator-design.md:70-72`.

**Tests legacy:** ninguno.

**Dependencias:** `F-SIM` y F-HIS-17.

**Mejora EXR:** no aplica a la experiencia final. Solo hay que mantener la separación absoluta.

---

### F-HIS-21: Reparación de sesiones incompletas (antes de subir a Strava)

**Valor para el usuario:** las sesiones que quedaron mal guardadas se corrigen solas antes de subirlas.

**Comportamiento observable.** Forma parte de "Subir a Strava" masivo, en el Perfil.

1. Se toman las sesiones **sin id de Strava**.
2. Se saltean las que **no tienen puntos**, y también aquellas cuyos puntos dan tiempo total 0 **y** distancia total 0 (F-HIS-04).
3. Si la sesión **no tiene fin**, o su tiempo total guardado es 0, se reescribe:
   - fin = inicio + tiempo total calculado;
   - distancia, tiempo, "potencia media" (p99), "spm medio" (p99) y calorías calculados desde los puntos;
   - se conservan la rutina, el nombre y el inicio.
4. Después se intenta subir cada sesión pendiente. Una falla individual no corta las demás.
5. **Efecto visible en el historial:** la sesión reparada pasa a estar terminada, así que en el detalle aparece el botón de subida si la subida falló.

**Casos borde:** las sesiones huérfanas por cierre de la app no tienen puntos y **nunca se reparan** (ver la diferencia con el README en la sección 4).

**Fuente legacy:** `lib/features/profile/profile_provider.dart:141-238`; la reparación está en `:155-196`.

**Tests legacy:** ninguno.

**Dependencias:** `F-STR` (subida masiva), F-HIS-04 y F-HIS-19.

**Mejora EXR:** la app cuida los datos del usuario sin pedirle nada. Las correcciones son silenciosas pero visibles en el resultado.

---

### F-HIS-22: Limpieza de sesiones vinculadas a Strava

**Valor para el usuario:** volver a sincronizar desde cero las actividades de Strava.

**Comportamiento observable**
1. El camino es Perfil → tarjeta "Mantenimiento" → botón "Limpiar sesiones de Strava".
2. Se abre un diálogo de confirmación, con textos fijos en español:
   - título "Limpiar sesiones de Strava";
   - cuerpo "Esto eliminará todas las sesiones descargadas de Strava. Podrás volver a sincronizarlas después.\n\n¿Continuar?";
   - botones "Cancelar" y "Eliminar".
3. Al confirmar se borran todos los puntos y todas las sesiones **que tengan id de Strava**. Después aparece el aviso "Sesiones de Strava eliminadas. Puedes sincronizar de nuevo.".
4. El historial **no** se recarga solo: muestra datos viejos hasta el próximo toque en la pestaña.

**Casos borde y errores**
- **[DEFECTO LEGACY, pérdida de datos]** También borra las sesiones **grabadas en la app** que se subieron a Strava, porque tienen id, aunque el diálogo dice "descargadas de Strava". Se pierde la telemetría completa de 5 s, los pasos y la rutina asociada. Al volver a sincronizar vuelve la versión de Strava, con el nombre de la actividad, sin rutina y con otra resolución.

**Fuente legacy:**
- `lib/core/database/database_service.dart:324-337`.
- `lib/features/profile/profile_provider.dart:407-410`.
- `lib/features/profile/profile_screen.dart:410-462`.

**Tests legacy:** ninguno.

**Dependencias:** `F-PRF`, `F-STR` y F-HIS-06.

**Mejora EXR:** las acciones destructivas son explícitas sobre qué se borra, cuántas sesiones y cuáles, y nunca tocan entrenamientos grabados en la app sin decirlo.

---

### F-HIS-23: Sesiones importadas de Strava dentro del historial

**Valor para el usuario:** ver en un solo historial también los entrenamientos de remo hechos con otros dispositivos.

**Comportamiento observable.** La importación en sí pertenece a `F-STR`. Acá se describe cómo quedan guardadas y cómo se ven.

1. **Sesión importada:**
   - nombre = nombre de la actividad, que se muestra donde iría el de la rutina;
   - sin rutina;
   - inicio = inicio de la actividad;
   - fin = inicio + tiempo transcurrido de la actividad;
   - id de Strava.
   - Antes de cargar los puntos, guarda la distancia, el tiempo y las calorías de Strava.
2. **Puntos:**
   - Uno por muestra del stream de Strava (≈ 1 por segundo).
   - Tiempo, distancia, potencia, SPM y pulso salen de Strava. Las paladas totales y las calorías quedan en 0.
   - **Ritmo recalculado:**
     - entre dos muestras, si avanzó al menos 2 m, el ritmo es `(Δtiempo / Δdistancia) × 500`;
     - la primera muestra usa el promedio acumulado;
     - los valores fuera de 60–600 s/500 m se reemplazan por el último válido o, si no hay ninguno, por el próximo válido. Si tampoco existe, se usa 120 s.
     - al final se suaviza con un promedio móvil centrado de 15 muestras.
   - El índice de paso sale de las vueltas (laps) de Strava. El tipo de paso sale de la metadata de intervalos, si existe; si no, queda vacío.
3. **Si hay streams,** la sesión se actualiza con las estadísticas de los puntos (F-HIS-04). Como los puntos no traen calorías, **las calorías quedan en 0** y pisan las de Strava.
4. **En el historial:** tarjeta normal con el ícono de Strava. Si la actividad no trajo streams, la tarjeta queda en 0 y el detalle muestra "sin telemetría".
5. **En el detalle:**
   - hay bandas por vuelta, en rojo `work` si no hay tipo;
   - no hay líneas objetivo;
   - el rendimiento por paso muestra una **duración ×5** (defecto de F-HIS-11).
6. Las actividades que ya existen localmente con el mismo id de Strava no se vuelven a importar.

**Fuente legacy:** `lib/features/profile/profile_provider.dart:241-404`:
- `:271-281` (sesión), `:284-333` (ritmo y suavizado);
- `:335-372` (vueltas y tipo) y `:375-389` (actualización).

**Tests legacy:** ninguno.

**Dependencias:** `F-STR`, F-HIS-04, F-HIS-11 y F-HIS-14.

**Mejora EXR:** las sesiones externas se integran sin costuras, con una marca de origen y la misma calidad de análisis que las grabadas en la app.

---

## 4. Diferencias entre docs y código (manda el código)

1. **CLAUDE.md** dice "Schema has 4 tables with cascade deletes". Las cascadas están declaradas, pero **nunca se activan**: borrar una rutina deja sus pasos huérfanos (F-HIS-17, F-HIS-18).
2. **CLAUDE.md** dice "history/: Read-only list of completed WorkoutSession records". En realidad lista **todas** las sesiones, incluidas las huérfanas sin terminar y las importadas de Strava, y el detalle **escribe**: permite subir a Strava y guarda el id.
3. **CLAUDE.md** dice "navigation via go_router". `go_router` es una dependencia, pero no se usa en `lib/`: la navegación es por pestañas más pantallas apiladas.
4. **README** dice "Sync status: each session in history shows a Strava icon indicating sync status". Solo las sesiones **sincronizadas** muestran el ícono, y solo si Strava está configurado. No hay indicador de "pendiente".
5. **README** dice "Session auto-repair: sessions that didn't save properly (e.g. app closed mid-workout) are automatically repaired from telemetry". Si la app se cierra a mitad del entrenamiento **no queda telemetría**, porque los puntos viven en memoria hasta terminar: esas sesiones se **saltean**, no se reparan. La reparación solo sirve cuando hay puntos pero la sesión no se actualizó.
6. **Spec del pulsómetro** (`2026-10-02-heart-rate-monitor-design.md:214-215`) dice que el chip "Pulso" muestra `avg · max`. El código muestra `avg/max`, por ejemplo `142/143`.
7. **El diálogo** "Limpiar sesiones de Strava" dice que borra "sesiones descargadas de Strava". En realidad borra **toda sesión con id de Strava**, incluidas las grabadas localmente y subidas (F-HIS-22).
8. **El comentario** del código "cada punto = 5 segundos" en el rendimiento por paso es falso para las sesiones importadas de Strava (≈ 1 s por punto).
9. **Las columnas** `avg_power_watts` y `avg_stroke_rate` guardan el **p99**, no la media.
10. **Textos sin localizar** dentro del área:
    - "Watts", "SPM" y "Split" en la tarjeta y el resumen;
    - el diálogo y el aviso de "Limpiar sesiones de Strava";
    - el diálogo de terminar de la pantalla inmersiva.
    - A la vez, `historyStatWatts` y `historyStatCalories` existen en los `.arb` y **no se usan**: las calorías no se muestran en el historial ni en el detalle.
11. **Título del detalle de una sesión libre:** usa `workoutFree` ("Entrenamiento libre"), mientras que la tarjeta usa `historyFree` ("Libre").
12. **CLAUDE.md** (Dev Mode) dice "Any new Strava write or upload prompt must also be guarded with kSimulator". El botón de subida del detalle **no** tiene esa protección: se muestra en el simulador y la subida falla por el bloqueo central.
13. **CLAUDE.md** dice "Telemetry is buffered every 5 seconds…": es correcto, pero no aclara que el buffer se escribe **solo al terminar**, ni que los tiempos y distancias guardados son contadores absolutos del monitor.

## 5. Defectos del legacy (por defecto se replican; ver propuestas en la sección 7)

| # | Defecto | Funcionalidad | Impacto |
|---|---|---|---|
| D-1 | El "Split" del resumen es casi el split **más lento** (p99 sobre segundos) | F-HIS-04 | Número engañoso en el historial |
| D-2 | La línea objetivo de split se dibuja sin invertir | F-HIS-15 | Objetivo de split inservible en el gráfico |
| D-3 | En el gráfico de split, las paradas (ritmo 0) quedan arriba, como el ritmo más rápido | F-HIS-13 | Lectura errónea |
| D-4 | "Limpiar sesiones de Strava" borra también las sesiones locales subidas | F-HIS-22 | **Pérdida de datos** |
| D-5 | La duración por paso supone 5 s por punto, y sale ×5 en las importadas | F-HIS-11, F-HIS-23 | Dato incorrecto |
| D-6 | La telemetría se escribe solo al terminar, y un cierre de la app la pierde toda | F-HIS-02, F-HIS-03 | **Pérdida de datos** y sesiones huérfanas |
| D-7 | Los totales son contadores absolutos del monitor, no de la sesión | F-HIS-02, F-HIS-04 | Tiempo y distancia inflados si el monitor no se reinicia |
| D-8 | Los errores de guardado y de carga no se informan (silencio o spinner eterno) | F-HIS-03, F-HIS-06, F-HIS-09 | Falsa confianza |
| D-9 | Las líneas objetivo usan la rutina actual, no la del día de la sesión | F-HIS-15 | Historial que "cambia" |
| D-10 | Las calorías de las importadas quedan en 0 | F-HIS-23 | Dato perdido |

## 6. Lo que el legacy NO hace (no es paridad; candidatos a mejora)

- Borrar, editar, renombrar o compartir una sesión desde el historial.
- Filtrar, buscar, agrupar por semana o mes, o ver totales acumulados y récords personales.
- Mostrar calorías en el historial o en el detalle, aunque se guardan.
- Mostrar el escenario inmersivo usado, porque no se guarda.
- Mostrar los parciales de 500 m o las repeticiones de la serie en el historial: se calculan solo en vivo y no se persisten (ver la spec `2026-10-02-series-splits-design.md:80`).
- Mostrar el cumplimiento de objetivos por paso: se calcula en vivo y no se guarda.
- Exportar o importar el historial a un archivo local.
- Paginar la lista.

## 7. Preguntas abiertas

- **Q-01: Datos del legacy.** ¿La app Unity tiene que **importar** el historial y las rutinas de una instalación existente del legacy (la base `rower_app.db`, esquema v7)? Si no se importan, el usuario arranca con el historial vacío.
- **Q-02 (D-1).** ¿Se mantiene el "Split" como p99 sobre segundos (casi el más lento) o se cambia el producto para mostrar el split representativo? Propuesta PO: el split medio de los puntos con remo, o el p1. Afecta también lo que se sube a Strava.
- **Q-03 (D-2, D-3).** ¿Se corrige el gráfico de split (objetivo invertido y paradas fuera de escala)? Propuesta PO: sí, como cambio de producto explícito.
- **Q-04 (D-4).** ¿"Limpiar sesiones de Strava" debe borrar solo las sesiones **importadas**? Propuesta PO: sí, porque hoy se pierden datos.
- **Q-05 (D-5).** ¿La duración por paso se calcula con el tiempo real del paso y no con "puntos × 5"? Propuesta PO: sí.
- **Q-06 (D-6).** ¿Se guarda la telemetría de forma incremental, para que un cierre inesperado no pierda la sesión? Propuesta PO: sí. ¿Qué hacer con las sesiones huérfanas: ocultarlas, marcarlas o permitir borrarlas?
- **Q-07 (D-7).** ¿Los totales deben ser de **la sesión** (delta desde el inicio) en lugar de los contadores absolutos del monitor? Confirmar el comportamiento real del monitor AMS-670B: ¿reinicia sus contadores al iniciar cada sesión?
- **Q-08 (D-9).** ¿Las líneas objetivo deben reflejar la rutina **tal como era** el día de la sesión?
- **Q-09.** ¿Se localizan "Watts", "SPM" y "Split" y los diálogos de mantenimiento? ¿Se muestran las calorías (`historyStatCalories` ya existe)?
- **Q-10.** ¿Se unifica el nombre de la sesión libre (tarjeta "Libre" contra título "Entrenamiento libre")?
- **Q-11.** ¿La fecha de la tarjeta se adapta al idioma (por ejemplo `MM/dd/yyyy` en inglés) o se mantiene fija `dd/MM/yyyy  HH:mm`?
- **Q-12.** ¿Se agrega borrar sesión? El legacy no lo tiene: es una funcionalidad nueva.
- **Q-13.** ¿El selector de métrica debe recordar la última elegida? El legacy vuelve a Vatios cada vez.
- **Q-14.** Ante un fallo de la base al iniciar o al terminar, ¿cuál es el comportamiento deseado? El legacy no avisa nada. Propuesta PO: avisar sin bloquear el entrenamiento.
- **Q-15.** ¿El botón de subir del detalle debe ocultarse en el modo simulador? La regla de CLAUDE.md lo pide; el código no lo hace.

## 8. Referencia rápida

**Textos (.arb, es / en)**

| Clave | es | en |
|---|---|---|
| `navHistory` | Historial | History |
| `historyTitle` | Historial | History |
| `historyEmpty` | No hay sesiones guardadas | No saved sessions |
| `historyFree` | Libre | Free |
| `historyStatTime` | Tiempo | Time |
| `historyStatDistance` | Distancia | Distance |
| `historyStatHeartRate` | Pulso | HR |
| `historyStatWatts` (sin uso) | Vatios | Watts |
| `historyStatCalories` (sin uso) | kcal | kcal |
| `workoutFree` | Entrenamiento libre | Free workout |
| `sessionNoTelemetry` | No hay datos de telemetría | No telemetry data |
| `sessionSummary` | Resumen | Summary |
| `sessionStepBreakdown` | Rendimiento por paso | Step Performance |
| `sessionChartWatts` | Vatios | Watts |
| `sessionChartSpm` | SPM | SPM |
| `sessionChartSplit` | Split 500m | Split 500m |
| `sessionChartDistance` | Distancia | Distance |
| `sessionChartHr` | Pulso | HR |
| `profileUploadToStrava` | Subir a Strava | Upload to Strava |
| `profileUploaded` | Subido a Strava | Uploaded to Strava |
| `profileUploadFailed` | Error al subir | Upload failed |
| `workoutFinishTitle` | Terminar entrenamiento | Finish workout |
| `workoutFinishContent` | Se guardará la sesión. ¿Continuar? | The session will be saved. Continue? |
| `workoutFinish` | Terminar | Finish |
| `cancel` | Cancelar | Cancel |

Fuente: `lib/l10n/app_es.arb:7, 51, 61-63, 99-115, 122, 203-206` (las mismas líneas en `app_en.arb`).

**Paleta usada en el área**

| Uso | Color |
|---|---|
| Fondo de pantalla / barra superior | `#0D1B2A` |
| Fondo de tarjeta (radio 16) | `#1A2E45` |
| Color semilla / segmento elegido | `#0077B6` |
| Ícono de remo / SPM / insignia de paso | `#00B4D8` |
| Potencia | `#FFD60A` |
| Split | `#FF9F1C` |
| Distancia | `#4CC9F0` |
| Pulso | `#EF476F` |
| Calorías (sin uso en el área) | `#06D6A0` |
| Strava | `#FC4C02` |
| Tipo `work` / `rest` / `warmup` / `cooldown` / otro | `#EF476F` / `#06D6A0` / `#FFD166` / `#118AB2` / `#607D8B` |

Fuente: `lib/shared/theme.dart:5-69`.

**Preferencias guardadas (SharedPreferences):** ninguna en esta área. La preferencia de subida `strava_upload_pref` pertenece a `F-STR`/`F-PRF`.
