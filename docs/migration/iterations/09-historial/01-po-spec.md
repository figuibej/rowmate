# It. 9 — Historial y detalle con gráficos: spec de producto

| Campo | Valor |
|---|---|
| Iteración | 9 (`09-historial`), [ROADMAP §It. 9](../../ROADMAP.md) |
| Incluye | F-HIS-05, F-HIS-06, F-HIS-07, F-HIS-09, F-HIS-10, F-HIS-11, F-HIS-12, F-HIS-13, F-HIS-14, F-HIS-15, F-HIS-19 ([history-persistence](../../product/inventory/history-persistence.md)); F-RUT-20 ([routines-models](../../product/inventory/routines-models.md)) |
| También toca | F-APP-01 (el historial se carga al arrancar), F-APP-02 (tocar Historial recarga; reemplaza el contenido provisorio de AC-APP-02-08), F-APP-03 (el detalle se apila), F-APP-05 (teclado, letra 1,3×, reducir movimiento), F-APP-10 (5 claves nuevas: 228 por idioma) |
| Decisiones | Q-01, Q-06, Q-09, Q-13, Q-16, Q-21, Q-22, Q-23, Q-24, Q-28 ([FEATURE_INVENTORY §4](../../product/FEATURE_INVENTORY.md)) y D-ART-03 ([ART_BIBLE](../../design/ART_BIBLE.md)) |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Leídos enteros: `lib/features/history/history_provider.dart`, `history_screen.dart`, `session_detail_screen.dart` (el botón de Strava, `:74-120`, es de la It. 12), `lib/core/models/workout_session.dart:1-137`, `lib/core/database/database_service.dart:205-322`, `lib/shared/theme.dart:50-69`; para la escala de los ejes, `fl_chart` 1.1.1 (`utils.dart:117-187`, la versión de `pubspec.lock`). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío |
| Modo | Entrega rápida, sin debates. Ante una duda, el legacy o un valor por defecto, anotado en una línea (§10) |
| Criterios | **43** nuevos (§3), 17 capturas, 2 videos y 5 tomas de demo (§7). Sin tests legacy directos (`session_stats_test` ya está en la It. 1: LT-17 a LT-19) |

---

## 0. Cómo leer esta spec

- Notación, tamaños (**TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720, en dp), marcas (`[dotnet]`, `[Editor-EM]`, `[Editor-PM]`, `[Captura]`, `[Device-Win]`, `[Device-And]`, `[PERF]`) y estado de prueba (reloj de prueba, **zona UTC−03:00**, simulador sin ruido, `ROWMATE_DATA_DIR` temporal): los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md), sin cambios. Textos entre comillas invertidas: **exactos al carácter** (los dos espacios de la fecha cuentan).
- IDs: `AC-HIS-nn-mm` por ficha (AC-HIS-19 sigue a -01 y -02 de la 4a); `DV-90` a `DV-95` siguen a DV-89.
- Las fuentes `archivo:línea` son del legacy: `hs` = `lib/features/history/history_screen.dart`, `sd` = `lib/features/history/session_detail_screen.dart`, `ws` = `lib/core/models/workout_session.dart`, `db` = `lib/core/database/database_service.dart`.
- **Bases de prueba de esta iteración** (§6): `F-9` (6 sesiones), `F-9V` (vacía), `F-9P` (200 sesiones, rendimiento) y `F-ROTA` (la de la It. 3: un archivo que no es SQLite). Ninguna prueba, captura ni demo usa las bases del usuario (AC-HIS-20-04).

---

## 1. Objetivo y demo

**Objetivo.** La pestaña Historial lista todas las sesiones, la más nueva arriba, con fecha, tiempo, distancia, vatios, SPM, split y pulso `{prom}/{máx}`. El detalle muestra el resumen, el "Rendimiento por paso" y el gráfico de cada métrica, con franjas por tipo de paso y objetivos punteados. Hay paridad con el legacy, salvo tres correcciones decididas: no parpadea al recargar (Q-22), el split se dibuja bien (Q-24) y los errores de carga se avisan (Q-13).

**Demo** (build de simulador, W1280, español; el remo y el pulsómetro simulados conectados, con 150 W y 24 spm):

1. Workout → `Entrenamiento libre`, remar ≈ 2 min y `Terminar`. Tocar **Historial**: la sesión aparece arriba sin parpadeo, con `Libre`, la fecha `dd/MM/yyyy  HH:mm` y 6 columnas (`Tiempo`, `Distancia`, `Watts`, `SPM`, `Split` y `Pulso` `{prom}/{máx}`).
2. Crear la rutina `Demo 9`: calentamiento 1:00; trabajo 2:00 con 140–160 W, 24 SPM y split 2:13; enfriamiento 1:00. Remarla completa.
3. Abrir la sesión `Demo 9`: el `Resumen`; el `Rendimiento por paso`, con 3 filas y la 2 en ≈ `2:00  ·  433m` / `150W` `24.x spm` `2:13/500m`; el gráfico en `Vatios`, con las franjas amarilla, roja y azul, y dos líneas punteadas en 140 y 160 sobre el trabajo.
4. Pasar por `SPM`, `Split 500m` (lo más rápido arriba, con el objetivo de 2:13 en su lugar), `Distancia` y `Pulso`. En la PC, pasar el cursor sobre la curva muestra el valor (`150.0 W`, `2:13 s`).
5. Empezar `Demo 9` otra vez, cerrar la ventana a mitad del trabajo y volver a abrir la app. En Historial está la sesión cortada, con sus números calculados a partir de sus puntos, y su detalle tiene el gráfico hasta el corte.

**Lo que depende del usuario:** nada que bajar. Strava no aparece porque todavía no está configurado (es un estado válido del legacy, It. 12).

---

## 2. Historias de usuario

- Como remero quiero ver todas mis sesiones con sus números clave para comparar entrenamientos de un vistazo.
- Como remero quiero el detalle de una sesión con rendimiento por paso y gráficos para entender cómo rendí en cada intervalo y contra el objetivo.
- Como remero quiero que una sesión cortada por un cierre de la app no se pierda y se pueda revisar.

---

## 3. Criterios de aceptación

### F-HIS-05 · Formatos (tabla completa en §4)

- **AC-HIS-05-01 · Fecha de la tarjeta (Q-06).** *Entonces* la fecha es la del inicio en hora local con el formato `dd/MM/yyyy  HH:mm`: **dos espacios**, 24 h y los mismos en es y en. En la zona UTC−03:00, `2026-10-02T16:51:00Z` se ve `02/10/2026  13:51`, `2026-01-05T12:07:00Z` se ve `05/01/2026  09:07` y `2026-10-02T00:05:00Z` se ve `01/10/2026  21:05`. [dotnet] · `hs:74`; `ws:56`
- **AC-HIS-05-02 · Duración y split de la sesión.** *Entonces* la duración es `MM:SS` y, desde 1 h, `H:MM:SS`: 0 → `00:00`, 440 → `07:20`, 3725 → `1:02:05` y 36000 → `10:00:00`. El split es `MM:SS` y `--:--` si vale ≤ 0: 135 → `02:15`, 605 → `10:05` y 0 → `--:--`. Son las reglas de AC-RUT-18-09 y AC-BLE-01-03, que se vuelven a verificar en la tarjeta y en el resumen. [dotnet] · `ws:94-109`
- **AC-HIS-05-03 · Formatos de un paso.** *Entonces* la duración del paso es `m:ss`, con los minutos sin relleno: 120 → `2:00`, 45 → `0:45` y 3600 → `60:00`. El split del paso es `m:ss/500m`, **sin** caso especial para 0: 133 → `2:13/500m` y 0 → `0:00/500m`. La potencia es `{n}W`, el SPM `{x.x} spm` y la distancia `{n}m`. [dotnet] · `sd:218-224`, `:254-261`
- **AC-HIS-05-04 · Números.** *Entonces* el SPM lleva 1 decimal con **punto**, también en español (24.04 → `24.0`, 0 → `0.0`, 24.96 → `25.0`). La distancia y la potencia son enteros **sin separador de miles** (`13796m`, `150`), y el pulso es `{prom}/{máx}` (`139/150`). [dotnet] · `hs:111-121`

### F-HIS-06 · Acceso y recargas (también F-APP-01, F-APP-02)

- **AC-HIS-06-01 · Carga al arrancar (F-APP-01).** *Dado* `F-9`, *cuando* se abre la app y se toca Historial por primera vez, *entonces* la lista ya está: no aparece el indicador de carga si la carga del arranque terminó. El contenido provisorio de AC-APP-02-08 desaparece de Historial; su barra superior dice `historyTitle` (`Historial` / `History`). [Editor-PM] · `lib/main.dart:84`; `history_provider.dart:12-14`
- **AC-HIS-06-02 · Cuándo recarga (F-APP-02, Q-22).** *Entonces* la lista y sus números se vuelven a leer de la base en **cada toque** de la pestaña Historial (también si ya estaba elegida), al volver del detalle y **al terminar una sesión**, sea a mano, por fin de rutina o con la sesión cortada que se detecta al arrancar. *Dado* que se terminó una sesión y se toca Historial, *entonces* ya está arriba desde el primer cuadro. Las otras pestañas no recargan nada. [Editor-PM] · `lib/main.dart:146-148`; `hs:53-56`
- **AC-HIS-06-03 · Sin parpadeo (Q-22, DV-92).** *Dado* una lista visible, *cuando* recarga, *entonces* la lista sigue a la vista y se puede usar. No hay indicador ni pantalla en blanco, la posición del desplazamiento se conserva y las tarjetas que no cambiaron no se vuelven a animar. Una sesión nueva entra arriba con una entrada breve (ANIMATION_BIBLE §2.3, "Recarga"), que con "reducir movimiento" aparece sin animar. El indicador centrado se ve solo en la primera carga, cuando todavía no hay datos. [Editor-PM] · [Captura] VID-01
- **AC-HIS-06-04 · Error de carga (Q-13, DV-95).** *Dado* `F-ROTA`, *entonces* Historial no queda en el indicador: muestra centrado `historyLoadError` (`No se pudo cargar el historial` / `Couldn't load history`) y el botón `retry` (`Reintentar` / `Retry`). *Cuando* la base responde y se toca `Reintentar`, aparece la lista. *Dado* una lista visible, *cuando* falla una recarga, *entonces* la lista queda como estaba y aparece abajo el aviso `historyLoadError` durante 4 s (AC-APP-03-05). [Editor-PM] · [Captura] CAP-17

### F-HIS-07 · Lista de sesiones

- **AC-HIS-07-01 · Vacía.** *Dado* `F-9V`, *entonces* se ven centrados el ícono de historial de 56 en blanco al 12 %, 12 de separación y `historyEmpty` (`No hay sesiones guardadas` / `No saved sessions`) en `text.tertiary` (DV-90). [Editor-PM] · [Captura] CAP-05 · `hs:24-35`
- **AC-HIS-07-02 · Orden y qué incluye.** *Entonces* hay una tarjeta por sesión, ordenadas por inicio de la más nueva a la más vieja (con el mismo inicio, va primero el id mayor). Entran **todas**: terminadas, cortadas (sin fin) y sin puntos. Con `F-9` el orden es `Escalera` 05/11, `Libre` 04/11, `Escalera` 03/11, `Libre` 02/11, `Libre` 02/10 y `Rutina borrada` 01/10. No hay paginación, filtros, búsqueda, borrar ni deslizar (Q-20). [dotnet] · [Editor-PM] · [Captura] CAP-01 · `db:248-252`
- **AC-HIS-07-03 · Fila superior.** *Entonces* la tarjeta muestra en una fila el ícono de remo de 18 en `#00B4D8`, el nombre (el de la rutina guardado en la sesión o `historyFree`: `Libre` / `Free`) en 15/600, que se recorta con `…` si no entra, la fecha (AC-HIS-05-01) en `text.tertiary` y la flecha `>` de 16 en blanco al 24 %. No se muestra el ícono de Strava, que llega en la It. 12. [Editor-PM] · [Captura] CAP-01 · `hs:85-106`
- **AC-HIS-07-04 · Fila de números.** *Entonces*, 12 más abajo, hay 5 columnas del mismo ancho, o 6 si la sesión tiene pulso (pulso máximo > 0). Cada columna lleva el valor en negrita con el color de la métrica y debajo la etiqueta en `text.tertiary`: `historyStatTime` (`Tiempo` / `Time`) en blanco; `historyStatDistance` (`Distancia` / `Distance`) `{n}m` en `#4CC9F0`; `historyStatPower` (`Watts`) en `#FFD60A`; `historyStatSpm` (`SPM`) en `#00B4D8`; `historyStatSplit` (`Split`) en `#FF9F1C`; y `historyStatHeartRate` (`Pulso` / `HR`) `{prom}/{máx}` en `#EF476F`. Las calorías no se muestran (Q-28). [Editor-PM] · [Captura] CAP-01 · `hs:109-123`; `lib/shared/theme.dart:32-40`
- **AC-HIS-07-05 · Valores calculados con los puntos.** *Entonces* los números salen de los **puntos** de cada sesión (F-HIS-04, AC-RUT-18-01 a -09) y **no** de los totales guardados en la sesión. Con `F-9`: `11:00` `2253m` `150` `24.1` `03:00` `139/150`; `33:44` `7923m` `160` `22.0` `02:10` `145/145`; la cortada `03:00` `651m` `150` `24.1` `02:30` `140/150` (sus totales guardados están en 0); la sin puntos `00:00` `0m` `0` `0.0` `--:--`, sin pulso; `1:02:05` `13796m` `140` `20.0` `02:15`, sin pulso (5 columnas); y `04:00` `851m` `150` `24.1` `02:30` `136/150`. [dotnet] · [Captura] CAP-01 · `hs:41`; `db:273-321`
- **AC-HIS-07-06 · Todo entra.** *Entonces*, en todos los tamaños, la lista es **una columna** a todo el ancho con margen de 12 y tarjetas separadas 10 (5 + 5). Un valor que no entra en su columna se **achica** hasta entrar en una línea y nunca se corta (`1:02:05`, `13796m`, `139/150` en P360 con letra 1,3×). Las etiquetas y la fecha no se superponen. [Editor-PM] · [Captura] CAP-02, CAP-03, CAP-04 · `hs:143-152`
- **AC-HIS-07-07 · Abrir.** *Cuando* se toca una tarjeta (con efecto de toque), *entonces* se abre su detalle. En la PC, el cursor sobre la tarjeta la resalta, Tab recorre las tarjetas con anillo de foco, Enter o Espacio la abren y la rueda desplaza la lista. [Editor-PM] · [Device-Win] · `hs:45-57`

### F-HIS-09 y F-HIS-10 · Detalle: apertura, estados y resumen

- **AC-HIS-09-01 · Se apila.** *Entonces* el detalle se abre a pantalla completa **por encima** de la barra de pestañas (AC-APP-03-01). Su título es el nombre de la rutina o `workoutFree` (`Entrenamiento libre` / `Free workout`): no es el `Libre` de la tarjeta (Q-09). La flecha de la barra, "Atrás" de Android y Escape vuelven a la lista, que recarga (AC-HIS-06-02). [Editor-PM] · [Device-And] · `sd:85-87`
- **AC-HIS-09-02 · Qué lee.** *Entonces* al abrir se vuelve a leer la sesión por su id (si ya no existe, se usa la de la tarjeta) y **todos** sus puntos, ordenados por el tiempo del monitor de menor a mayor (AC-HIS-19-02). Si la sesión tiene rutina y esa rutina existe, también se leen sus pasos expandidos **actuales**, con progresiones (F-RUT-06), para las líneas objetivo (Q-16). [dotnet] · `sd:35-54`
- **AC-HIS-09-03 · Estados y orden.** *Entonces* mientras carga se ve el indicador centrado. Sin puntos se ve solo `sessionNoTelemetry` (`No hay datos de telemetría` / `No telemetry data`) centrado en `text.tertiary`, sin resumen. Con puntos hay una lista desplazable con relleno de 16, que tiene el Resumen, 20 de separación, el Rendimiento por paso (solo si algún punto tiene índice de paso) y otros 20, el selector de métrica, 8 de separación y el gráfico de 220 de alto. [Editor-PM] · [Captura] CAP-06, CAP-07, CAP-13 · `sd:122-145`
- **AC-HIS-09-04 · Error de carga (Q-13, DV-95).** *Dado* que la base falla al abrir el detalle, *entonces* no queda en el indicador: muestra centrado `sessionLoadError` (`No se pudo cargar la sesión` / `Couldn't load the session`) con `retry` (`Reintentar` / `Retry`), y la flecha sigue volviendo. [Editor-PM]
- **AC-HIS-10-01 · Resumen.** *Entonces* es una tarjeta con el título `sessionSummary` (`Resumen` / `Summary`) en 16/700, 12 de separación y la **misma** fila de 5 o 6 columnas de AC-HIS-07-04, con los mismos valores de AC-HIS-07-05, calculada con los puntos que leyó el detalle. [Editor-PM] · [Captura] CAP-06, CAP-07 · `sd:149-179`

### F-HIS-11 · Rendimiento por paso

- **AC-HIS-11-01 · Qué filas hay.** *Entonces* la tarjeta lleva el título `sessionStepBreakdown` (`Rendimiento por paso` / `Step Performance`) en 16/700 y 12 de separación. Los puntos con índice de paso se agrupan por índice, con **una fila por índice que tenga al menos un punto**, en orden ascendente. Si un índice aparece en tramos no contiguos, se suma en una sola fila. No se muestran el tipo, el nombre del paso, el objetivo ni el cumplimiento. Con `F-9`, `Escalera` tiene 8 filas y la cortada tiene 2. [dotnet] · [Captura] CAP-07 · `sd:183-212`
- **AC-HIS-11-02 · La fila.** *Entonces* la fila tiene fondo blanco al 4 %, radio 8, relleno de 10 a los costados y 8 arriba y abajo, y 6 de separación con la siguiente. Lleva una insignia de 28 × 28 con radio 6, fondo `#00B4D8` al 16 % y el número del paso (índice + 1) en `#00B4D8` 12/700. Al lado van la línea 1, `{duración}  ·  {distancia}m` (dos espacios a cada lado del punto medio), en 11 y `text.secondary`, y la línea 2, con `{W}W`, `{spm} spm` y `{split}/500m` separados por 12, en 12/600 blanco. [Editor-PM] · [Captura] CAP-07 · `sd:226-268`
- **AC-HIS-11-03 · Cálculos.** *Entonces* la potencia es el promedio simple de todos los puntos del paso, **con los ceros**, redondeado. El SPM es el promedio con 1 decimal y el split el promedio redondeado al segundo. La distancia es la del último punto del paso menos la del primero, y puede dar negativa si un punto trae 0, igual que en el legacy. Con `F-9`, `Escalera`: fila 1 `1:00  ·  183m` / `100W` `20.0 spm` `2:30/500m`; fila 2 `2:00  ·  432m` / `150W` `24.1 spm` `2:13/500m`; fila 6 (con 2 puntos en 0) `2:00  ·  433m` / `138W` `22.1 spm` `2:02/500m`. Los ceros bajan el promedio y hacen que el split parezca más rápido, como en el legacy. [dotnet] · [Captura] CAP-07 · `sd:215-224`
- **AC-HIS-11-04 · Duración (Q-23).** *Entonces* la duración del paso es la cantidad de sus puntos por el intervalo de muestreo de la sesión: 5 s en las sesiones grabadas por la app, así que el resultado es el mismo del legacy. El punto final repetido suma 5 s al último paso: con `F-9`, la fila 8 de `Escalera` tiene 13 puntos y se ve `1:05  ·  183m`. Las sesiones importadas, con otro intervalo, son de la It. 13. [dotnet] · `sd:218`

### F-HIS-12 · Selector de métrica

- **AC-HIS-12-01 · Opciones (Q-21).** *Entonces* el selector es un control segmentado de 5 opciones, en este orden: `sessionChartWatts` (`Vatios` / `Watts`), `sessionChartSpm` (`SPM`), `sessionChartSplit` (`Split 500m`), `sessionChartDistance` (`Distancia` / `Distance`) y `sessionChartHr` (`Pulso` / `HR`). Se elige una sola. La elegida tiene fondo `#0077B6`, texto blanco y un tilde. Cada vez que se abre un detalle arranca en **Vatios**, porque la elección no se guarda. Pulso está siempre, también sin pulso (la curva queda plana en 0). En P360 con letra 1,3× ningún texto se corta. [Editor-PM] · [Captura] CAP-07 a CAP-11 · `sd:25`, `:67-73`, `:274-287`
- **AC-HIS-12-02 · Cambio y teclado.** *Cuando* se elige otra métrica, *entonces* la curva pasa a la nueva con un fundido de 220 ms y el eje Y se reescala en 300 ms (ANIMATION_BIBLE §2.12, "Gráfico del detalle"). La animación no cambia los datos. Con "reducir movimiento", el cambio es instantáneo. En la PC, Tab llega al selector, las flechas cambian la opción y Enter o Espacio la eligen. [Editor-PM] · [Captura] VID-02

### F-HIS-13 · Gráfico

- **AC-HIS-13-01 · Eje X.** *Entonces* cada punto se ubica en el tiempo **del monitor**. El eje va del primer punto al último y no necesariamente arranca en 0. Con `F-9`, la sesión `Libre` del 04/11 arranca en `3:44`, porque los contadores del monitor no se reinician (Q-15). Con un solo punto, el gráfico no falla. [dotnet] · [Captura] CAP-06 · `sd:299-311`
- **AC-HIS-13-02 · Valores por métrica.** *Entonces* los vatios, el SPM, la distancia y el pulso se dibujan tal cual, **con los ceros**, que se ven como caídas a 0. Con `F-9`, en `Escalera` hay dos caídas a 0 de vatios en la fila 6 (425 y 430 s; gate M-03, E-02: en 420 caía en el descanso y la fila 6 daba `144W`). [dotnet] · [Captura] CAP-07 · `sd:301-308`
- **AC-HIS-13-03 · Split invertido (Q-24, DV-93).** *Entonces* el split se dibuja como `ritmo más lento − ritmo del punto`, donde el ritmo más lento es el máximo entre los puntos con ritmo > 0. Así lo más rápido queda **arriba**. Un punto con ritmo 0 va al **piso**, como una parada, y no arriba, como el ritmo más rápido (en el legacy va arriba). Las etiquetas del eje Y y el tooltip se vuelven a pasar a ritmo `m:ss`. Si ningún punto tiene ritmo, la curva queda plana en el piso y el eje Y no lleva etiquetas. [dotnet] · [Captura] CAP-09 · `sd:294-305`, `:446-456`
- **AC-HIS-13-04 · Escala Y.** *Entonces* el eje Y va de 0 a 1,1 × el mayor valor dibujado, contando la curva y las líneas objetivo (AC-HIS-15-01); si ese máximo es ≤ 0, se usa 100. Las marcas y la grilla van cada `paso`, que sale de redondear `máximo / 4` así (`fl_chart`): se toma la mantisa *s* en [1; 10), y queda 10 si *s* ≥ 7,6, 5 si *s* ≥ 2,6, 2 si *s* ≥ 1,6 y 1 en otro caso, por su potencia de 10. Las etiquetas incluyen el 0 y el máximo. Son enteros truncados, salvo el split, que va en `m:ss` del ritmo truncado. Con `F-9`, `Escalera`: Vatios `0` `50` `100` `150` `198`; SPM `0` `5` `10` `15` `20` `25` `26`; Split `3:00` `2:50` `2:40` `2:30` `2:20` `2:10` `2:00`; Pulso `0` `50` `100` `150` `165`. [dotnet] · [Captura] CAP-07 a CAP-10 · `sd:390-402`, `:441-463`
- **AC-HIS-13-05 · Eje X: marcas.** *Entonces* el paso de las marcas depende del tiempo del **último** punto: con menos de 300 s va cada 30 s, con menos de 600 s cada 60 s, con menos de 1800 s cada 120 s y si no cada 300 s. Las marcas van en los múltiplos del paso y además en el inicio y el fin, en `m:ss` sin horas (3900 → `65:00`). Con `F-9`: `Escalera` `0:05` `2:00` `4:00` `6:00` `8:00` `10:00` `11:00`; `Libre` del 04/11 `3:44` `5:00` `10:00` … `30:00` `33:44`; la del 02/10 termina en `62:05`. [dotnet] · [Captura] CAP-06, CAP-07 · `sd:427-439`, `:525-532`
- **AC-HIS-13-06 · Estilo.** *Entonces* la curva es suavizada (0,2) de 2 px, en el color de la métrica (vatios `#FFD60A`, SPM `#00B4D8`, split `#FF9F1C`, distancia `#4CC9F0` y pulso `#EF476F`), sin marcadores, con un área debajo del mismo color (degradé de 24 % a 0 %, DV-91). La grilla es solo horizontal, en las marcas de AC-HIS-13-04, y no hay borde. Los ejes tienen texto de 11 en `text.tertiary` con cifras tabulares (D-ART-03, DV-90). [Editor-PM] · [Captura] CAP-07 · `sd:404-483`; ART_BIBLE §3.9
- **AC-HIS-13-07 · Tooltip.** *Mientras* el dedo o el cursor están sobre el gráfico, *entonces* se ven una línea guía vertical y el valor del punto más cercano de **la curva**: `{valor con 1 decimal} {unidad}`, con las unidades `W`, `spm`, `m` y `bpm` (`150.0 W`, `24.1 spm`, `651.0 m`, `150.0 bpm`). En el split se ve `m:ss s` (`2:13 s`) y, si el punto no tiene ritmo, `--:-- s`. Al soltar o al salir el cursor, desaparece. Los valores de las líneas objetivo no aparecen (DV-94). [Editor-PM] · [Device-Win] · [Captura] CAP-15 · `sd:495-519`

### F-HIS-14 · Franjas y separadores de pasos

- **AC-HIS-14-01 · Armado.** *Entonces* solo hay franjas si algún punto tiene índice de paso. Se recorren los puntos en orden: cada vez que aparece un índice distinto del de la franja abierta, esa franja se cierra en el tiempo de ese punto y se abre una nueva desde ahí. El tipo de la franja es el del punto que la abre, y si ese punto no tiene tipo vale `work`. La última franja termina en el último punto de la sesión. Los puntos sin índice no cortan franjas, y un paso sin puntos no tiene franja (la anterior se estira). Con `F-9`, `Escalera` tiene 8 franjas: `warmup` 5–65, `work` 65–185, `rest` 185–245 … `cooldown` 605–660 (gate M-03, E-01: el trabajo dura 120 s y la fila 2 tiene 24 puntos). [dotnet] · `sd:313-332`
- **AC-HIS-14-02 · Color.** *Entonces* cada franja ocupa todo el alto con el color de su tipo al 10 %: `work` `#EF476F`, `rest` `#06D6A0`, `warmup` `#FFD166`, `cooldown` `#118AB2` y cualquier otro `#607D8B`. [dotnet] · [Captura] CAP-07 · `sd:410-418`; `lib/shared/theme.dart:56-69`
- **AC-HIS-14-03 · Separadores.** *Entonces* al inicio de cada franja, **salvo la primera**, hay una línea vertical punteada (4 de trazo y 4 de espacio) de 1 px en blanco al 24 %. Una sesión libre no tiene franjas ni separadores. [Editor-PM] · [Captura] CAP-06, CAP-07 · `sd:484-494`

### F-HIS-15 y F-RUT-20 · Líneas objetivo

- **AC-HIS-15-01 · Cuándo y cuáles.** *Dado* una sesión cuya rutina existe, *entonces* por cada franja cuyo índice esté entre los pasos expandidos actuales se dibujan: en Vatios una línea en el mínimo y otra en el máximo, las que estén definidas; en SPM una en el objetivo de SPM; en Split una en el objetivo de split; en Distancia y Pulso, ninguna. Un índice fuera de rango no tiene línea. Las líneas cuentan para el máximo del eje Y (AC-HIS-13-04). [dotnet] · `sd:352-397`
- **AC-HIS-15-02 · Estilo.** *Entonces* cada línea es horizontal, punteada (8 de trazo y 5 de espacio), de 2 px en blanco al 78 %, y va del inicio al fin de su franja, con un relleno debajo hasta 0 del color del tipo de paso al 7 %. Si hay mínimo y máximo, los dos rellenos se superponen, como en el legacy. [Editor-PM] · [Captura] CAP-07 · `sd:370-385`
- **AC-HIS-15-03 · Progresiones y split invertido (Q-24).** *Entonces* los objetivos incluyen las progresiones por repetición. Con `F-9`, en `Escalera`, los tres trabajos tienen vatios `140`/`160`, `150`/`170` y `160`/`180`, SPM `18`, `20` y `22`, y split `2:10`, `2:08` y `2:06`. El objetivo de split se dibuja **invertido como la curva** (`ritmo más lento − objetivo`): queda a la altura de su ritmo (`2:10`, `2:08` o `2:06`) en el eje; con un paso de 10 solo `2:10` es marca (*Corregido en la It. 15*, #39). Un objetivo más lento que el ritmo más lento queda en el piso. (El legacy lo dibuja sin invertir, DV-93.) [dotnet] · [Captura] CAP-08, CAP-09 · `sd:366-367`
- **AC-RUT-20-01 · Con la rutina de hoy (Q-16).** *Entonces* las líneas objetivo usan la rutina **actual**. Si se editó, muestran los objetivos nuevos. Si se borró, no hay líneas, pero siguen las franjas (salen del tipo guardado en cada punto), el Rendimiento por paso y el nombre guardado (`Rutina borrada` en `F-9`). Si se renombró, la sesión conserva el nombre que tenía. [dotnet] · [Editor-PM] · [Captura] CAP-16 · `sd:40-47`

### F-HIS-19 · Consultas, sesiones cortadas y rendimiento

- **AC-HIS-19-03 · Lista y estadísticas.** *Entonces* la lista trae todas las sesiones, de la más nueva a la más vieja, y los números de cada una salen de sus puntos (F-HIS-04). Una sesión sin puntos lleva los valores por defecto, y un pulso que falta cuenta como 0. Las sesiones de las It. 4a a 8 que ya están en la base se ven igual que una nueva. [dotnet] · `db:248-252`, `:273-321`; `history_provider.dart:23-34`
- **AC-HIS-19-04 · Sesión cortada (Q-12).** *Dado* que la app se cerró a mitad de una sesión (AC-WKT-06-10), *entonces* esa sesión aparece en la lista con los números calculados con sus puntos guardados, no con los totales en 0 de la fila. Su detalle tiene resumen, rendimiento por paso, franjas y gráfico hasta el último punto guardado. No lleva marca de "cortada", igual que en el legacy. Con `F-9` es `Escalera` del 03/11: 2 filas (`1:00  ·  183m` y `2:00  ·  432m`), 2 franjas y el gráfico de `0:05` a `3:00`. [dotnet] · [Editor-PM] · [Captura] CAP-12
- **AC-HIS-19-05 · Rápido con muchas sesiones.** *Dado* `F-9P` (200 sesiones de 30 min, 72 400 puntos: 200 × 362, "como la n.º 2" con el punto final repetido; *Corregido en la It. 15*, #39) en el build de simulador de la PC, *entonces* la primera vez que se toca Historial la lista aparece completa en ≤ 1,0 s. Una recarga no congela la pantalla (ningún cuadro pasa de 50 ms), el desplazamiento va a 60 fps y un detalle de 1 h (746 puntos) se ve en ≤ 0,5 s desde el toque. [PERF] · [Device-Win]

---

## 4. Tabla de formatos

| Dato | Dónde | Formato | Ejemplos | Fuente |
|---|---|---|---|---|
| Fecha | tarjeta | `dd/MM/yyyy  HH:mm`, hora local, 24 h, igual en es y en | `02/10/2026  13:51` | `hs:74` |
| Duración de la sesión | tarjeta, resumen | `MM:SS`; desde 1 h `H:MM:SS` | `00:00`, `07:20`, `1:02:05`, `10:00:00` | `ws:101-109` |
| Split de la sesión | tarjeta, resumen | `MM:SS`; ≤ 0 → `--:--` | `02:15`, `10:05`, `--:--` | `ws:94-99` |
| Distancia | tarjeta, resumen, paso | `{n}m`, sin miles | `2253m`, `13796m` | `hs:112`, `sd:254` |
| Potencia | tarjeta, resumen | entero | `150` | `hs:113` |
| SPM | tarjeta, resumen | 1 decimal con punto | `24.1`, `0.0` | `hs:114` |
| Pulso | tarjeta, resumen | `{prom}/{máx}` (solo si máx > 0) | `139/150` | `hs:116-121` |
| Duración de un paso | rendimiento por paso | `m:ss` | `2:00`, `0:45`, `60:00` | `sd:221` |
| Línea 1 de un paso | rendimiento por paso | `{dur}  ·  {dist}m` | `2:00  ·  432m` | `sd:254` |
| Potencia / SPM / split de un paso | rendimiento por paso | `{n}W` / `{x.x} spm` / `m:ss/500m` (0 → `0:00/500m`) | `150W`, `24.1 spm`, `2:13/500m` | `sd:259-261` |
| Eje X | gráfico | `m:ss`, sin horas | `3:44`, `65:00` | `sd:434-436` |
| Eje Y | gráfico | entero truncado; split `m:ss` | `198`, `2:10` | `sd:447-461` |
| Tooltip | gráfico | `{x.x} {W\|spm\|m\|bpm}`; split `m:ss s`, sin ritmo `--:-- s` | `150.0 W`, `2:13 s` | `sd:501-510` |

Las unidades (`m`, `W`, ` spm`, `/500m`, `s`, `bpm`) son parte del formato, iguales en es y en (Q-06), y no son claves del catálogo.

## 5. Claves nuevas del catálogo (Q-01): 223 → 228 por idioma

| Clave | es | en | AC |
|---|---|---|---|
| `historyStatPower` | `Watts` | `Watts` | AC-HIS-07-04 |
| `historyStatSpm` | `SPM` | `SPM` | AC-HIS-07-04 |
| `historyStatSplit` | `Split` | `Split` | AC-HIS-07-04 |
| `historyLoadError` | `No se pudo cargar el historial` | `Couldn't load history` | AC-HIS-06-04 |
| `sessionLoadError` | `No se pudo cargar la sesión` | `Couldn't load the session` | AC-HIS-09-04 |

Las tres primeras son los textos fijos del legacy (`hs:113-115`, `sd:164-166`), con el español idéntico. `historyStatWatts` (`Vatios`) y `historyStatCalories` siguen sin uso, como en el legacy. Las dos últimas copian el estilo de `routinesLoadError`.

---

## 6. Datos sembrados (zona UTC−03:00; puntos cada 5 s; el último punto se repite al cerrar, como en el legacy)

**Valores por tipo de paso** (constantes): `warmup` y `cooldown` 100 W, 20.0 spm, split 150 y pulso 120 (calentamiento) o 125 (enfriamiento); `work` 150 W, 24.1 spm, split 133 y pulso 150; `rest` 60 W, 18.0 spm, split 180 y pulso 130. La distancia de cada punto es `redondeo(distancia exacta acumulada)` al ritmo de su paso.

**Rutina `Escalera`** (existe en `F-9`): calentamiento 60 s; serie ×3 [trabajo 120 s, 140–160 W con +10 por repetición, SPM 18 con +2 y split 2:10 con −2; descanso 60 s]; enfriamiento 60 s. Se expande en 8 pasos y dura 660 s.

| # | Sesión (inicio local) | Puntos | Qué prueba |
|---|---|---|---|
| 1 | `Escalera`, `05/11/2026  18:40`, terminada | 133 (5 a 660 s; la fila 6 tiene 0 W, 0 spm y split 0 en 425 y 430 s, y la distancia sigue; gate M-03, E-02) | 6 columnas, 8 filas, franjas, objetivos con progresión, ceros y split invertido |
| 2 | `Libre`, `04/11/2026  07:15`, terminada | 362 (224 a 2024 s; 160 W, 22.0 spm, split 130, pulso 145; de 1000 a 7923 m) | Eje X desde `3:44`, paso de 300 s, sin franjas |
| 3 | `Escalera`, `03/11/2026  19:02`, **cortada** (sin fin, totales en 0) | 36 (5 a 180 s: calentamiento y el primer trabajo) | AC-HIS-19-04 |
| 4 | `Libre`, `02/11/2026  12:00`, sin fin | 0 | Tarjeta en 0 y `sessionNoTelemetry` |
| 5 | `Libre`, `02/10/2026  13:51`, terminada | 746 (5 a 3725 s; 140 W, 20.0 spm, split 135, sin pulso) | `1:02:05`, 5 columnas, `62:05` |
| 6 | `Rutina borrada`, `01/10/2026  21:05`, terminada; su rutina ya no existe | 49 (calentamiento 60, trabajo 120 y enfriamiento 60) | Franjas sin objetivos (AC-RUT-20-01) |

`F-9V`: base vacía. `F-9P`: 200 sesiones como la n.º 2, una por día. `F-ROTA`: la de la It. 3. Ninguna prueba se siembra en las bases del usuario.

---

## 7. Capturas y videos

**Reglas:** las de la 6a §7.1 (español salvo que se diga, reloj de prueba, `ROWMATE_DATA_DIR` temporal, dos corridas iguales byte a byte, `manifest.json` e `INDEX.md` sin rutas de la PC), en `docs/migration/iterations/09-historial/snapshots/`. Solo datos sembrados: ninguna toma muestra sesiones reales del usuario.

| ID | Tamaño | Base y estado | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | TV | `F-9`, Historial | 6 tarjetas en orden, con los valores de AC-HIS-07-05; 6 columnas en las que tienen pulso y 5 en la n.º 5 |
| **CAP-02** | TH | `F-9`, Historial | Una columna a todo el ancho; números sin cortar |
| **CAP-03** | P360, inglés, letra 1,3× | `F-9`, Historial | `History`, `Free`, `Time`, `Distance`, `HR`; `1:02:05`, `13796m` y `139/150` achicados, sin cortes |
| **CAP-04** | W1280 | `F-9`, Historial | Lista en una columna, flecha `>` y fecha a la derecha |
| **CAP-05** | TV | `F-9V`, Historial | Ícono de 56 y `No hay sesiones guardadas`, centrados |
| **CAP-06** | TV | `F-9`, detalle n.º 2, Vatios | `Entrenamiento libre`, resumen con 6 columnas, sin rendimiento por paso, sin franjas; eje X `3:44` … `33:44` |
| **CAP-07** | TV | `F-9`, detalle n.º 1, Vatios | Resumen, 8 filas (1, 2, 6 y 8 como AC-HIS-11-03 y -04), 8 franjas, 7 separadores, líneas en 140/160, 150/170 y 160/180, eje Y `0` … `198` y dos caídas a 0 |
| **CAP-08** | TV | `F-9`, detalle n.º 1, SPM | Líneas en 18, 20 y 22; eje `0` … `26` |
| **CAP-09** | TV | `F-9`, detalle n.º 1, Split 500m | Lo rápido arriba; los dos ceros en el piso; objetivos a la altura de su ritmo (`2:10`, `2:08` y `2:06`; *Corregido en la It. 15*, #39); eje `3:00` … `2:00` |
| **CAP-10** | TV | `F-9`, detalle n.º 1, Pulso | Curva en `#EF476F`, sin líneas objetivo; eje `0` … `165` |
| **CAP-11** | TV | `F-9`, detalle n.º 1, Distancia | Curva que sube hasta 2253, sin líneas objetivo |
| **CAP-12** | TV | `F-9`, detalle n.º 3 (cortada), Vatios | 2 filas, 2 franjas y gráfico de `0:05` a `3:00` |
| **CAP-13** | TV | `F-9`, detalle n.º 4 | Solo `No hay datos de telemetría` |
| **CAP-14** | TH | `F-9`, detalle n.º 1, Vatios, desplazado hasta el gráfico | Detalle horizontal: selector y gráfico a todo el ancho |
| **CAP-15** | W1280 | `F-9`, detalle n.º 1, Split 500m, cursor en 300 s | Línea guía y tooltip `2:13 s` |
| **CAP-16** | TV | `F-9`, detalle n.º 6, Vatios | `Rutina borrada`: 3 filas, 3 franjas y ninguna línea objetivo |
| **CAP-17** | TV | `F-ROTA`, Historial | `No se pudo cargar el historial` y `Reintentar` |

| ID | Tamaño y duración | Qué se hace | Qué tiene que verse |
|---|---|---|---|
| **VID-01** | TV, ≈ 10 s | `F-9`: Historial → agregar una sesión terminada (como al terminar un entrenamiento) → tocar Historial dos veces | La sesión nueva entra arriba; nunca hay indicador ni pantalla en blanco (AC-HIS-06-03) |
| **VID-02** | TV, ≈ 8 s | `F-9`, detalle n.º 1: abrir y pasar por las 5 métricas | Entrada del gráfico (franjas, curva de izquierda a derecha, área) y fundido entre métricas (AC-HIS-12-02) |

**Demo** (`DEMO-01` a `DEMO-05`, W1280, simulador): los pasos 1 a 5 de §1, una toma por paso, en una base temporal.

---

## 8. Checklist de paridad

| Comportamiento legacy | Fuente | AC |
|---|---|---|
| Se carga al arrancar; recarga al tocar la pestaña y al volver del detalle | `main.dart:84`, `:146-148`; `hs:53-56` | AC-HIS-06-01, -02 |
| Indicador, vacío y lista | `hs:22-60` | AC-HIS-06-03, AC-HIS-07-01 |
| Orden por inicio descendente; todas las sesiones | `db:248-252` | AC-HIS-07-02, AC-HIS-19-03 |
| Tarjeta: ícono, nombre o `Libre`, fecha, flecha, 5 o 6 columnas con colores | `hs:85-123` | AC-HIS-07-03, -04 |
| Números con los puntos, no con los totales | `hs:41`; `db:273-321` | AC-HIS-07-05 |
| El valor que no entra se achica | `hs:143-152` | AC-HIS-07-06 |
| Detalle: título `Entrenamiento libre`; relee sesión, puntos y rutina actual | `sd:35-54`, `:87` | AC-HIS-09-01, -02 |
| Sin puntos → `sessionNoTelemetry`; orden y separaciones de las secciones | `sd:122-142` | AC-HIS-09-03 |
| Resumen igual a la tarjeta | `sd:149-179` | AC-HIS-10-01 |
| Rendimiento por paso: agrupación, fila, promedios con ceros, puntos × 5 | `sd:183-270` | AC-HIS-11-01 a -04 |
| Selector de 5 opciones, Vatios por defecto, no se recuerda | `sd:25`, `:274-287` | AC-HIS-12-01 |
| Eje X del monitor, ceros dibujados, split invertido, escala 1,1× o 100 | `sd:291-311`, `:390-402` | AC-HIS-13-01 a -04 |
| Marcas X 30/60/120/300 s; marcas Y de `fl_chart` con mínimo y máximo | `sd:427-463`, `:525-532` | AC-HIS-13-04, -05 |
| Curva 0,2 / 2 px, área, grilla horizontal, sin borde; tooltip `{x.x} {u}` | `sd:404-519` | AC-HIS-13-06, -07 |
| Franjas por tipo al 10 %; separadores salvo el primero | `sd:313-332`, `:410-418`, `:484-494` | AC-HIS-14-01 a -03 |
| Líneas objetivo por métrica, punteadas, con relleno; cuentan para el eje | `sd:352-397` | AC-HIS-15-01, -02 |
| Objetivos con progresiones y con la rutina actual | `sd:42-47`; `routine.dart:29-70` | AC-HIS-15-03, AC-RUT-20-01 |

**Se vuelven a verificar** en el Historial: AC-RUT-18-01 a -09 (estadísticas), AC-HIS-19-01 y -02 (sesión por id y puntos ordenados), AC-APP-02-03 y -08 (selección y contenido provisorio) y AC-HIS-20-04 (nunca las bases del usuario).

---

## 9. Mejoras EXR (cómo se tiene que sentir)

Una bitácora de regata: cifras tabulares que no bailan, el color de cada métrica igual al del HUD y una lista que nunca parpadea. Al abrir el detalle, las secciones entran en cascada suave y el gráfico se dibuja de izquierda a derecha. Entre métricas, la curva se transforma en lugar de saltar, y el tooltip sigue al dedo sin retraso. Las paradas se leen como paradas y no como picos. Las animaciones no cambian ningún dato, y con "reducir movimiento" todo aparece en su lugar al instante.

## 10. Decisiones aplicadas y diferencias visibles

**Decisiones del PO en una línea** (por defecto; sin preguntas abiertas):

- Q-23: la duración es la cantidad de puntos × el intervalo de muestreo de la sesión, que es 5 s en las sesiones de la app, así que da igual que en el legacy (también el +5 s del punto repetido). La corrección de las importadas llega con ellas en la It. 13.
- Q-24: un ritmo 0 va al piso, un objetivo de split más lento que el ritmo más lento queda en el piso y, sin ningún ritmo, el eje Y queda sin etiquetas.
- Sesiones con el mismo inicio: va primero el id mayor (en el legacy el orden no está definido).
- Una sesión cortada no lleva marca: es paridad, y se anota para issues.
- Escala Y: se usa la regla de `fl_chart` con 4 divisiones (el área de 220 − 24 da 196 px, y 196 / 40 = 4).
- Q-13: los errores de carga de la lista y del detalle llevan texto y `Reintentar`, igual que en Rutinas (AC-RUT-09-04).
- Sin borrar sesión (Q-20, *backlog*), sin calorías (Q-28), `Libre` y `Entrenamiento libre` siguen separados (Q-09) y el selector no se recuerda (Q-21).

| DV | Diferencia con el legacy | Motivo |
|---|---|---|
| DV-90 | Fecha, etiquetas, vacío y ejes en `text.tertiary` (blanco al 54 %, el legacy usa 38 %); ejes en 11 (el legacy usa 9) | D-ART-03, contraste AA |
| DV-91 | Tamaños de ART_BIBLE §3.7 y §3.9: fecha 12, valor `num.stat` 16/700, etiqueta 11/500, selector 13/600, fondo del gráfico `bg.sunken` y área en degradé (24 % → 0 %; el legacy usa 12 % plano) | Tablero EXR legible |
| DV-92 | Recarga sin parpadeo; también recarga al terminar una sesión | Q-22 |
| DV-93 | El split pone las paradas en el piso y el objetivo invertido | Q-24 |
| DV-94 | El tooltip muestra solo el valor de la curva (el legacy agrega el de una línea objetivo cercana) | Tooltip fluido y sin ruido |
| DV-95 | Los errores de carga se avisan con `Reintentar` (el legacy deja el indicador para siempre) | Q-13 |

## 11. Fuera de alcance

- El ícono de sincronización, el botón de subir y las recargas después de subir o sincronizar (F-HIS-08, F-HIS-16, F-STR-17; It. 12), con el botón oculto en el simulador (Q-78).
- Las sesiones importadas de Strava, su duración por paso (Q-23 para importadas), la reparación de sesiones incompletas y la limpieza (F-HIS-21, F-HIS-22, F-HIS-23; It. 13).
- Borrar, editar, filtrar o exportar sesiones; récords; calorías; maestro-detalle en tablet (backlog, §6 del inventario de historial).
- El agua avanzada y el desenfoque (It. 6b, issue #6): el historial no usa vidrio.

## 12. Para issues

- La sesión cortada o sin puntos podría distinguirse visualmente en la tarjeta (mejora EXR de F-HIS-07).
- Tocar una franja o una fila de paso para resaltar su tramo, y mostrar el cumplimiento del objetivo por paso (mejora EXR de F-HIS-11 y F-HIS-14).
- Zoom y desplazamiento del gráfico, y un cursor con todas las métricas del instante (mejora EXR de F-HIS-13).
- Las líneas objetivo con la rutina tal como era el día de la sesión (Q-08 del inventario; hoy se usa la rutina actual, Q-16).
- El "Split" del resumen es el p99, casi el más lento (BKL-25, Q-14).
- La etiqueta del máximo del eje Y puede quedar pegada a la última marca (`150` y `157`), como en el legacy.

## Cambios por gate

- M-03 ([03-qa-gate](03-qa-gate.md)): AC-HIS-14-01 y AC-HIS-13-02 y la tabla de §6 toman las erratas E-01 y E-02 de [02-tech-design §10.3](02-tech-design.md), para que el texto coincida con el oráculo. No cambia ningún resultado visible.
