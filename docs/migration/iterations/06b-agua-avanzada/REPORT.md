# It. 6b — Agua avanzada, reflejos, estela, niebla y vidrio: reporte

**Estado:** cerrada con el OK del usuario, después de una ronda extra que corrigió el salto del agua cada 16 m (AC-6B-20 b). QA la dio RECHAZADA en las rondas 1 y 2 y APROBADA CON OBSERVACIONES en la ronda del salto. Quedan **2 FAIL** con la causa medida, los dos de la entrada al entrenamiento (vuelven a #1). **32 PASS · 2 FAIL · 4 NO VERIFICABLE** (de 38 AC).

**Ronda del salto (DT6b-27):** la malla del agua ya no se re-ancla cada 16 m: queda quieta y las ondas que el paso del anillo no alcanza se apagan hacia su media. VID-01 da 1,41 × la mediana (tope 1,5; antes 4,06 ×) y la sonda de 400 m no encuentra saltos en ningún escenario. Re-verificación: dotnet 4143/4182 pasan y 0 fallan; EditMode 0 fallan; PlayMode 796/796 y con simulador 877/877, con `SceneBudgetTests(High)` en verde (p95 1,06 ms con la PC libre). Detalle en 02-tech-design §18 y 05-qa-verification, sección "Ronda del salto".

## 1. Qué se ve distinto

En los 4 escenarios el agua deja de ser el degradé con líneas de la 6a. La app elige sola qué agua usar según el equipo, sin ningún ajuste a la vista. En esta PC usa **Alto con el agua avanzada**.

| Escenario | Qué cambia |
|---|---|
| **Lago alpino** | Ondas finas, casi espejo. Bajo el sol aparece un camino de brillo que titila. Debajo de los pinos el agua refleja más oscuro, y en Alto se reflejan también el bote y el remero. La estela en V nace en la popa, dura 12 s y se abre. A lo lejos, la niebla. Las montañas contrastan más y a las 18 h hay halo. → [CAP-02](snapshots/CAP-02-lake-12-tv.png), [CAP-03](snapshots/CAP-03-lake-18-tv.png), [CAP-17](snapshots/CAP-17-lake-12-th.png) |
| **Río urbano** | Oleaje corto con corriente. De noche, cada farol y cada ventana dejan una columna de luz que ondula en el agua. → [CAP-06](snapshots/CAP-06-river-12-tv.png), [CAP-08](snapshots/CAP-08-river-22-tv.png), [VID-04](snapshots/VID-04-columnas-rio-tv.mp4) |
| **Costa** | Mar de fondo con un camino de sol ancho y espuma en las crestas. Las boyas suben y bajan con la misma ola que el bote. → [CAP-10](snapshots/CAP-10-coast-12-tv.png), [VID-05](snapshots/VID-05-costa-boya-th.mp4) |
| **Canal de regata** | Casi espejo, con el reflejo de la orilla. → [CAP-14](snapshots/CAP-14-regatta-12-tv.png) |

En todos los escenarios:
- **Vidrio desenfocado** en Alto y Medio: las tarjetas, la banda de tiempo y distancia, los paneles y el diálogo de completado dejan ver la escena borrosa detrás. El contraste se mantiene (≥ 13,5:1). En Bajo, el vidrio sigue siendo el plano de antes.
- **Diálogo de `Corta`**: detrás del vidrio se ve el bote frenando con la toma aérea. → [CAP-29](snapshots/CAP-29-dialogo-vidrio-tv.png)
- **Charcos de la pala**: se leen a la escala del remero, con el borde de espuma, los remolinos a 40° y micro-anillos. → [CAP-32](snapshots/CAP-32-charcos-anillos-th.png)
- **Popa del casco**: ya no es una tabla blanca. La cubierta tiene un degradé y se afina al final. → [CAP-31](snapshots/CAP-31-popa-reflejo-w1280.png)
- **Caída al agua degradada**: si el agua avanzada falla, o si el equipo no da después de bajar todo el detalle, aparece el agua de la 6a con un fundido de 0,6 s. La sesión sigue, sin errores a la vista. La degradada ahora dibuja **10 líneas** (antes 11). → [CAP-21](snapshots/CAP-21-lake-12-tv-degradada.png), [VID-02](snapshots/VID-02-falla-a-degradada-tv.mp4)
- **Deuda de la 6a**: Dispositivo ya no se redibuja debajo del inmersivo, y la escena ya no se abre en el cuadro del toque.

## 2. Cómo probarlo (≈ 15 min)

**`Builds/Windows/simulator`** (tiene todo):
1. Abrí `RowMate.exe`. En **Workout** elegí `Lago alpino` y tocá `Entrenamiento libre` apenas aparece la pantalla. Puede notarse un tironcito al entrar: es el FAIL de AC-6B-01/-02 (§4).
2. Llave → `Fuerte`. Mirá la estela que se abre detrás del bote, el brillo bajo el sol, el reflejo de la orilla y del bote, y las tarjetas con la escena borrosa detrás.
3. Llave → `Hora escena` en `06:00`, `18:00` y `22:00`. El brillo sigue al sol o a la luna. A las 22 h aparecen las columnas de luz (en el río se ven mejor).
4. Volvé a reposo y repetí con `Río urbano`, `Costa` y `Canal de regata`.
5. Iniciá `Corta` y dejala terminar. El diálogo de completado es de vidrio y detrás se adivina el bote.
6. **Forzar el agua degradada.** Desde PowerShell, en la carpeta del repo:
   - `& .\Builds\Windows\simulator\RowMate.exe -rowmate-water degraded` muestra el agua de la 6a con 10 líneas.
   - `-rowmate-water-fail 15`: a los 15 s de la sesión el agua pasa a la degradada con un fundido y la sesión sigue.
   - `-rowmate-quality medium` o `low` muestra los otros niveles. En Bajo no hay destellos y el vidrio es plano.
7. Opcional: remá un rato con `Fuerte`: el agua ya no da un saltito cada ~4 s (AC-6B-20 b, corregido en la ronda del salto).

**`Builds/Windows/normal`**: ignora esos argumentos y elige el agua sola (en esta PC, Alto con la avanzada). Si tenés el remo, hacé los pasos 1 a 5. Sin el remo, quedate con el simulador.

## 3. Resultados reales (QA, ronda 2)

**Pantalla en su modo normal** (no el de respaldo) (ventana del player de 1280 × 720). Ahora sí se mide la GPU. La carga de otros procesos (la VM de WSL entre 0,3 y 16 de 24 procesadores) quedó anotada en cada corrida. No se tocó ningún proceso.

| Suite | Resultado |
|---|---|
| dotnet `-Matrix` | 4 138 de 4 177 pasan, **0 fallan** (39 ignorados); 194/194. Matriz: PASA 919, FALLA 0 |
| EditMode / con simulador | 4 265 de 4 320 y 4 459 de 4 514: **0 fallan** |
| PlayMode / con simulador (×2) | 795/796 y 876/877 dos veces. La única falla es `SceneBudgetTests(High)`: CPU de escena 1,515-1,80 ms contra un tope de 1,5, con 25-31 % de carga externa; sola pasó una vez (1,35 ms). **No se relajó el tope** |
| Pruebas nuevas | `AdvancedLifecycleTests` 24/24, `WaterFallbackTests` 9/9, `WakeRealHistoryTests` 1/1, `EarlyEntryTests` 5/5. No se rompió nada de las It. 1-15 |
| Capturas | 38 tomas (33 PNG + 5 videos). Dos corridas dieron **38/38 iguales byte a byte** |

| Rendimiento en el build | Medido |
|---|---|
| Sesión en régimen (Alto y Medio × 4 escenarios × 12 y 22 h) | CPU del hilo principal p99 **2,2-3,9 ms**; **GPU máx. 5,6-8,9 ms**; SetPass ≤ 78; memoria 132-137 MB; 0 recolecciones de basura |
| Caída con `-rowmate-water-fail` | 1 sola línea en el registro, peor cuadro de 7,9 ms, la sesión se guarda |
| Sin forzar el nivel, 60 s | Se queda en Alto con la avanzada |
| Entrada temprana (8 arranques en frío) | Toque **22,9-36,1 ms**; peor cuadro con movimiento **14,0-20,2 ms** (6 de 16 > 16,7) |

Builds `normal`, `simulator` y `-BleSmoke`: OK. El repo público está limpio (sin rutas ni datos de la PC). `ProjectSettings` y los `.mat` del horizonte no cambiaron, y el legacy tampoco. Todo el arte es propio y procedural.

## 4. AC que fallan o no se pudieron verificar

- **AC-6B-01 (FAIL, #1)**: con la precarga sin terminar, a los ≈ 2 s del toque caen cuadros de hasta 20,2 ms con la escena en movimiento. Vienen de las unidades `mundo`, `orilla de 920 m` y `Tick sintético`. En la ronda 1 parecía resuelto, pero esa medida se tomó con la pantalla en modo de respaldo. Vuelve a #1.
- **AC-6B-02 (FAIL, #1)**: el primer toque cuesta 22,9-36,1 ms (`StartFree` 10-13 ms más ≈ 12 ms del panel). La escena ya no suma (0,2 ms). Vuelve a #1.
- **AC-6B-20 b (PASS en la ronda del salto, #91)**: antes el agua saltaba cada 16 m (4,06 ×); ahora VID-01 da 1,41 × contra un tope de 1,5 ×.
- **NO VERIFICABLE**:
  - AC-6B-15: la costura del horizonte no es concluyente sin una pasada sin siluetas.
  - AC-6B-26: la GPU del desenfoque sola no se mide todavía. El cuadro entero de Medio cuesta ≤ 6,5 ms.
  - AC-6B-29, solo la CPU de Alto: falla con carga externa y pasa con la PC más quieta.
  - AC-6B-32: 60 fps en Android, sin teléfono (#7).

## 5. Decisiones por defecto (rigen si no decís otra cosa)

- DV-149: el agua refleja el cielo real de la hora y la orilla (en Alto también el bote y el remero), con el peso del legacy.
- DV-150: cada escenario tiene su oleaje.
- DV-151: la estela dura 12 s y se abre. Es un cambio frente al legacy.
- DV-152: si el equipo no da, la degradada es el último paso y no vuelve hasta reiniciar la app.
- DV-153: los umbrales de las sondas de "se lee".
- Popa (#85): rige la cubierta 0,80-0,95 de la 6a (la media mide 0,897). No se agrega otro tope.
- Las miniaturas del selector siguen con el agua degradada, como en el legacy.
- Cierre: el usuario pidió corregir AC-6B-20 b antes del commit (hecho). Los 2 FAIL que quedan (AC-6B-01/-02) se aceptan con su dato y vuelven a #1.

## 6. Capturas (`snapshots/`, índice en [INDEX.md](snapshots/INDEX.md))

- Matriz en vertical, 4 escenarios × 6, 12, 18 y 22 h: [CAP-01](snapshots/CAP-01-lake-06-tv.png) a [CAP-16](snapshots/CAP-16-regatta-22-tv.png).
- En horizontal a las 12 h: [CAP-17](snapshots/CAP-17-lake-12-th.png) a [CAP-20](snapshots/CAP-20-regatta-12-th.png). Lago a las 18 h: [CAP-33](snapshots/CAP-33-lake-18-th.png).
- Agua degradada: [CAP-21](snapshots/CAP-21-lake-12-tv-degradada.png) a [CAP-24](snapshots/CAP-24-regatta-12-tv-degradada.png).
- Niveles Medio y Bajo: [CAP-25](snapshots/CAP-25-lake-12-tv-medium.png), [CAP-26](snapshots/CAP-26-lake-12-tv-low.png).
- Vidrio: [CAP-27](snapshots/CAP-27-vidrio-lake-tv.png) (`Series 600`), [CAP-28](snapshots/CAP-28-vidrio-river-w1280.png); el diálogo en [CAP-29](snapshots/CAP-29-dialogo-vidrio-tv.png).
- A los 42 km: [CAP-30](snapshots/CAP-30-lago-42k-w1280.png). Popa y reflejo del bote: [CAP-31](snapshots/CAP-31-popa-reflejo-w1280.png). Charcos: [CAP-32](snapshots/CAP-32-charcos-anillos-th.png).
- Videos:
  - [VID-01](snapshots/VID-01-cruce-4096-w1280.mp4): cruce de los 4096 m, donde se ve el FAIL.
  - [VID-02](snapshots/VID-02-falla-a-degradada-tv.mp4): la caída a la degradada.
  - [VID-03](snapshots/VID-03-estela-tv.mp4): la estela.
  - [VID-04](snapshots/VID-04-columnas-rio-tv.mp4): las columnas de luz.
  - [VID-05](snapshots/VID-05-costa-boya-th.mp4): las boyas de la costa.

Detalles de aspecto que ya están anotados en los issues:
- Los remolinos del charco parecen una "carita" (CAP-32).
- En la costa, un óvalo oscuro rodea la popa (CAP-09 y CAP-11).
- Detrás del diálogo el bote apenas se adivina, aunque la sonda pasa.

## 7. Qué queda abierto

- **Se cierran 6:** #2, #3, #12, #13, #22 y #85.
- **Siguen abiertos con su resto:**
  - #1: AC-6B-01 y -02.
  - #6: Android.
  - #31: el espejo de Alto no refleja las instancias de la orilla (pinos, cabaña, fachadas, tribunas).
  - #19: la composición de la popa en horizontal, sin cambios.
- **Issues nuevos (8, agrupados):**
  - el agua que salta cada 16 m (#91, ya corregido en la ronda del salto);
  - rendimiento de Alto y medidas del build;
  - reflejos y luz en el agua;
  - estela, charcos y línea de contacto;
  - lo que falta del vidrio;
  - escenarios (mar FFT, boyas, volver a la avanzada sin reiniciar);
  - capturas, sondas y pruebas;
  - erratas de los documentos de la 6b.
- **Antes del commit (ya hecho por el PO):** repuse el `INDEX.md` de la 6b (38 filas) y el de la 7 (55 filas, con la CAP-04 nueva). Los sets viejos con el lago (6a, 7, 8, 14 y 15) no se regeneraron: eso va al issue de capturas.
