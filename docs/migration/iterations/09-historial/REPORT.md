# It. 9 — Historial y detalle con gráficos: reporte

| | |
|---|---|
| Estado | **Cerrada — esperando tu OK.** El PO la acepta sin criterios que fallen |
| QA | APROBADO CON OBSERVACIONES ([05-qa-verification](05-qa-verification.md)), sin bloqueantes ni mayores |
| Criterios | 43: **42 cumplen**, 0 fallan y 1 no se puede verificar (AC-HIS-19-05, en parte) |
| Próxima | **It. 10a**: Bluetooth real en Android y Windows. Necesita el remo AMS-670B (u otro FTMS). La 6b sigue postergada (issue #6) |

## 1. Qué se entregó

- **Pestaña Historial**: lista todas las sesiones, de la más nueva a la más vieja. Cada tarjeta tiene el nombre (`Libre` o el de la rutina), la fecha `dd/MM/yyyy  HH:mm` y las columnas `Tiempo`, `Distancia`, `Watts`, `SPM`, `Split` y `Pulso` `{prom}/{máx}`. El pulso aparece solo si hubo pulsómetro, y cada cifra va con el color de su métrica en el HUD.
- **Carga y recargas sin parpadeo** (Q-22): la lista se carga al abrir la app. Se recarga al tocar la pestaña (también si ya estaba elegida), al volver del detalle y al terminar una sesión. Si los datos no cambiaron, no se mueve nada, y la posición de desplazamiento se conserva. Una sesión nueva entra arriba con una animación corta.
- **Errores** (Q-13): si la base no se puede leer, aparece `No se pudo cargar el historial` con `Reintentar`. Si falla una recarga con la lista a la vista, la lista queda y sale un aviso.
- **Detalle de la sesión**: se abre sobre las pestañas y tiene:
  - el `Resumen`, con la misma fila de la tarjeta;
  - el `Rendimiento por paso`: una fila por paso con su número, `m:ss  ·  NNNm` y `NNNW  NN.N spm  m:ss/500m`;
  - el gráfico, con un selector `Vatios / SPM / Split 500m / Distancia / Pulso` que marca la opción elegida.
- **Gráfico**: franjas de color por tipo de paso, con separadores punteados. Los objetivos de la rutina son líneas punteadas, con su progresión de una serie a otra. El split va invertido, con lo rápido arriba y las paradas en el piso (Q-24). El gráfico entra dibujándose de izquierda a derecha y el cambio de métrica es un fundido cruzado. Con "reducir movimiento", todo aparece al instante.
- **Cursor** (PC y dedo): al pasar sobre la curva aparecen una guía, un punto y un tooltip con el valor (`145.0 W`, `2:13 s`).
- **Sesiones cortadas**: si cerrás la app a mitad de una sesión, al volver a abrirla la sesión está en la lista. Sus números salen de sus puntos, y su gráfico llega hasta el corte.
- **Rutina editada o borrada**: los objetivos son los de la rutina actual (Q-16). Si la rutina ya no existe, el gráfico se ve con las franjas y sin objetivos.
- **Todo lo que calcula se prueba en .NET**: formatos, columnas, rendimiento por paso, franjas, objetivos, escalas de `fl_chart` portadas tal cual, la geometría de la malla y el punto más cercano al cursor.

## 2. Cómo probarlo en `Builds/Windows/simulator`

El build ya tiene la It. 9. Abrí un solo `RowMate.exe` a la vez. Si aparece el aviso del Firewall, contestá `Cancelar`. En inglés, la pestaña se llama `History` y el detalle de una libre, `Free workout`.

**(a) Sesión libre** (3 min)
1. Andá a **Workout** → `Entrenamiento libre`. Con la llave del simulador elegí un ritmo (por ejemplo, 150 W y 24 spm), remá unos 2 minutos y tocá `Terminar` → `Terminar`.
2. Tocá **Historial**: la sesión ya está arriba, sin indicador de carga, con `Libre`, la fecha y las columnas. → [DEMO-01](snapshots/DEMO-01-libre-arriba.png)
3. Tocá Historial otra vez: no parpadea ni salta.

**(b) Rutina con objetivos** (6 min)
1. En **Rutinas**, creá `Demo 9` con tres pasos: calentamiento de 1:00; trabajo de 2:00 con 140–160 W, 24 SPM y split 2:13; enfriamiento de 1:00. Guardala.
2. Remala completa con el simulador. → [DEMO-02](snapshots/DEMO-02-demo9-remada.png)
3. En Historial, abrí `Demo 9`. Vas a ver el `Resumen` y 3 filas en `Rendimiento por paso` (la 2 es `2:00  ·  433m` / `150W  24.0 spm  2:13/500m`). El gráfico está en `Vatios`, con las franjas amarilla, roja y azul y las líneas punteadas en 140 y 160 sobre el trabajo. → [DEMO-03](snapshots/DEMO-03-detalle-vatios.png)
4. Pasá por `SPM`, `Split 500m` (lo rápido arriba, con el objetivo de 2:13 en su lugar), `Distancia` y `Pulso`. Pasá el mouse sobre la curva y vas a ver el tooltip con el valor. → [DEMO-04](snapshots/DEMO-04-split-cursor.png)
5. Con la flecha (o `Esc`) volvés a la lista en la misma posición.

**(c) Sesión cortada** (2 min)
1. Empezá `Demo 9` otra vez y, a mitad del trabajo, cerrá la ventana.
2. Volvé a abrir la app y andá a Historial: la sesión cortada está arriba, con sus números. Su detalle muestra solo los pasos remados y el gráfico hasta el corte. → [DEMO-05](snapshots/DEMO-05-cortada.png)

**Algo que vas a notar (es paridad):** el `Tiempo` y la `Distancia` de la tarjeta son los del monitor (el máximo de los puntos), como en el legacy. El simulador no pone su reloj en cero entre sesiones, así que si remás varias sin reconectarlo, cada sesión arranca desde lo que el monitor ya tenía acumulado. Pasa en DEMO-05: la segunda `Demo 9` marca `08:22`, y el eje X del detalle arranca en `2:11`. Con un remo real que arranca en cero no se nota.

## 3. Resultados (corridas reales de QA)

| Suite | Resultado |
|---|---|
| `run-tests.ps1 -Matrix` (dotnet 8) | build OK. **3500 tests: 3461 pasan, 39 omitidos, 0 fallan.** Herramientas 192/192. Matriz: PASA 737, FALLA 0, IGNORADO 4, PENDIENTE UNITY 163, SIN TEST 0 |
| Unity EditMode | 3603: 3556 pasan, 47 ignorados, 0 fallan |
| Unity EditMode `-Simulator` | 3795: 3748 pasan, 47 ignorados, 0 fallan |
| Unity PlayMode | 588 / 588 |
| Unity PlayMode `-Simulator` | 663 / 663 (los 37 del historial y los 2 con el simulador, en verde) |
| Build Windows simulador | exit 0. `ProjectSettings` sin cambios |
| Prueba a mano en el build | Libre, rutina completa, rutina cortada, 3 reinicios, cursor y rueda: todo bien. Con 200 sesiones (F-9P), la lista está a la vista a los **0,25 s** del toque y el detalle de 30 min a los **0,4–0,5 s** |
| Repo público | 0 rutas o datos de la PC en lo versionado ni en las tomas. UTF-8 sin doble codificación. Legacy intacto |

**Rendimiento en el Editor (solo como indicio)**: 746 puntos se preparan en 0,19 ms y se dibujan en 0,52 ms. Con 200 sesiones, el peor cuadro de una recarga es de 96 ms, con la base en el hilo principal del Editor. Hay basura por cuadro: la entrada asigna en 15 de 48 cuadros, el cambio de métrica en 10 de 24 y el cursor en 3 de 60. *Corrección:* el §13 del diseño decía que el cursor no asignaba nada.

## 4. Criterios que no se pueden verificar

| AC | Qué falta | Por qué |
|---|---|---|
| AC-HIS-19-05 (en parte) | El peor cuadro de recarga (≤ 50 ms) y los 60 fps al desplazar, medidos en el build | El build no tiene una sonda de cuadros para el historial. Lo que sí se vio en el build: lista a 0,25 s y detalle a 0,5 s. Va a issue (sección 7, n.º 3) |

Además, los videos VID-01 y VID-02 no se miraron porque no hay `ffmpeg` en la máquina. Sus criterios (recarga sin parpadeo y cambio de métrica) los cubren los tests y la prueba en el build. El "Atrás" de Android queda para cuando haya teléfono.

## 5. Decisiones por defecto (respondé solo si querés cambiar alguna)

| Decisión | Por defecto | Si la rechazás |
|---|---|---|
| DV-90 y DV-91: fecha, etiquetas y ejes en blanco al 54 % y ejes en 11 (el legacy usa 38 % y 9); tamaños de tablero EXR y área en degradé | Se aceptan | Colores y tamaños del legacy |
| DV-92: la lista no parpadea al recargar y también se recarga al terminar una sesión | Se acepta | Indicador en cada recarga |
| DV-93: split invertido (lo rápido arriba), paradas en el piso y objetivo invertido | Se acepta | Split sin invertir, como el legacy |
| DV-94: el tooltip muestra solo el valor de la curva | Se acepta | Que agregue el de la línea objetivo cercana |
| DV-95: los errores de carga se avisan con `Reintentar` | Se acepta | Indicador para siempre, como el legacy |
| Tiempo y distancia de la tarjeta = los del monitor (sección 2) | Paridad | Descontar el primer punto (cambio de producto) |
| Etiquetas X de los bordes que se pisan (`3:44` con `5:00` se lee "3:45:00") | Paridad con `fl_chart`; issue con prioridad | Ocultar la marca intermedia que choca |

**Ya decididas por el PO** (una línea cada una):
- Con el mismo inicio, va primero la sesión de id mayor.
- La sesión cortada no lleva marca (paridad).
- El selector no recuerda la última métrica (Q-21).
- No hay borrar sesión (Q-20) ni calorías (Q-28).
- La duración por paso es la cantidad de puntos × 5 s (Q-23).
- El eje Y tiene 4 divisiones (la regla de `fl_chart`).
- Strava no aparece hasta la It. 12.

## 6. Capturas

Todas las tomas usan datos sembrados (F-9, F-9V, F-9P, F-ROTA) o del simulador, con una base temporal. El índice completo está en [snapshots/INDEX.md](snapshots/INDEX.md).

- Lista: [CAP-01 TV](snapshots/CAP-01-lista-tv.png) · [CAP-02 TH](snapshots/CAP-02-lista-th.png) · [CAP-03 P360 en inglés a 1,3×](snapshots/CAP-03-lista-p360-en-130.png) · [CAP-04 W1280](snapshots/CAP-04-lista-w1280.png) · [CAP-05 vacía](snapshots/CAP-05-vacia-tv.png) · [CAP-17 error](snapshots/CAP-17-error-tv.png)
- Detalle: [CAP-06 libre](snapshots/CAP-06-detalle-libre-tv.png) · [CAP-07 Escalera en Vatios](snapshots/CAP-07-detalle-escalera-vatios-tv.png) · [CAP-08 SPM](snapshots/CAP-08-detalle-spm-tv.png) · [CAP-09 Split](snapshots/CAP-09-detalle-split-tv.png) · [CAP-10 Pulso](snapshots/CAP-10-detalle-pulso-tv.png) · [CAP-11 Distancia](snapshots/CAP-11-detalle-distancia-tv.png) · [CAP-14 TH](snapshots/CAP-14-detalle-th.png)
- Casos borde: [CAP-12 cortada](snapshots/CAP-12-detalle-cortada-tv.png) · [CAP-13 sin datos](snapshots/CAP-13-detalle-sin-datos-tv.png) · [CAP-16 rutina borrada](snapshots/CAP-16-detalle-rutina-borrada-tv.png) · [CAP-15 cursor](snapshots/CAP-15-cursor-w1280.png)
- Demo: [DEMO-01](snapshots/DEMO-01-libre-arriba.png) · [DEMO-02](snapshots/DEMO-02-demo9-remada.png) · [DEMO-03](snapshots/DEMO-03-detalle-vatios.png) · [DEMO-04](snapshots/DEMO-04-split-cursor.png) · [DEMO-05](snapshots/DEMO-05-cortada.png)
- Videos: [VID-01 recarga sin parpadeo](snapshots/VID-01-recarga-sin-parpadeo.mp4) · [VID-02 las 5 métricas](snapshots/VID-02-metricas.mp4)

Lo que más se nota al mirarlas:
- Las etiquetas X de los bordes se pisan (CAP-06, CAP-09).
- En CAP-07 (TV) el gráfico queda cortado abajo; CAP-14 lo muestra entero.
- En W1280, la llave del simulador tapa la primera etiqueta X y la cinta `SIMULADOR` tapa la flecha atrás (DEMO-03, DEMO-05).
- La sonda de color de CAP-01 marca 0/2 por un nombre de elemento repetido, pero los colores de la toma son los correctos.

## 7. Deuda: va a issues de GitHub (8, agrupados)

1. Etiquetas de los ejes que se pisan y casos raros de la escala
2. Interacción EXR del gráfico y del detalle
3. Rendimiento del historial: sonda en el build, GC y basura por cuadro
4. Nombres repetidos en las columnas de la tarjeta y sonda de aspecto de CAP-01
5. Diseño en pantallas chicas y en escritorio: fila a 1,3×, CAP-07 y la llave del simulador
6. Pulido de arte del historial
7. Casos borde chicos en el código del historial
8. Erratas de la spec, del arte y del diseño de la It. 9

Un punto de la lista ya no va: "WP-D1 y WP-D2: HistoryMotion, Skin.uss, Motion.uss y 04-design-notes" quedó hecho en esta iteración ([04-design-notes](04-design-notes.md)). Lo mismo con el cambio de idioma en caliente (m-03 del gate), que ya se resuelve con `OnTexts` → `RenderTexts`.

## 8. Próximo paso

Si das el OK a la 9, sigue la **It. 10a** (Bluetooth real en Android y Windows). Para esa iteración hacen falta el remo AMS-670B (u otro FTMS), la PC con Bluetooth LE y, para Android, el teléfono con depuración USB.
