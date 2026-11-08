# It. 9 — Historial y detalle con gráficos: gate de QA

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-08 |
| Entrada | [01-po-spec](01-po-spec.md) (43 AC), [02-tech-design](02-tech-design.md) (DT9-01 a -17, §10.3 E-01 y E-02), [00-arte-historial](00-arte-historial.md) (D9-01 a -14, A9-01 a -11) |
| Legacy verificado | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`, solo lectura): `lib/features/history/{history_provider,history_screen,session_detail_screen}.dart`, `lib/core/database/database_service.dart:205-322`, `lib/core/models/workout_session.dart`. `git status --porcelain` vacío antes y después |
| Base `dotnet` (antes de implementar) | `run-tests.ps1 -ArtifactsPath <scratch>/it9/gate`: **3350 tests, 3311 pasan, 39 omitidos, 0 fallan** |

## Veredicto: **APROBADO CON OBSERVACIONES**

Sin bloqueantes. Hay tres mayores chicos, que se corrigen en el documento antes de WP-A o WP-B1 (no frenan WP-A salvo M-03, que toca el oráculo).

## 1. Qué se verificó (con evidencia)

- **Números de F-9, recalculados a mano** con la regla de siembra de spec §6 y `SessionStats.Compute` (P99 con los valores > 0 y máximos de columna, independiente del orden): las 6 tarjetas de AC-HIS-07-05 (por ejemplo `139/150` = 18545 / 133 y `02:30` de la cortada = P99 de 12 × 150 + 24 × 133), las filas 1, 2, 6 y 8 de AC-HIS-11-03 y -04 (`138W` = 22 × 150 / 24 = 137,5 → 138; `22.1`; `2:02`; `433m` = 1887 − 1454), los ejes de AC-HIS-13-04 (`198` = 1,1 × 180, paso 50; SPM 26,51, paso 5; Pulso 165, paso 50; Split lento 180 y máximo 54 → 59,4, paso 10 → `3:00` … `2:10` `2:00`) y las marcas X de AC-HIS-13-05. Cuadran todos.
- **Erratas del Architect (§10.3)**: E-01 y E-02 son correctas. Con un cero en 420 s, cae en el descanso (índice 4) y la fila 6 da `144W` (23 × 150 / 24), no `138W`.
- **Paridad**: el orden de `getSessions` (`db:250`, con el desempate por id), las estadísticas de los puntos (`db:289-321`, pulso nulo = 0), la relectura del detalle (`sd:35-54`; `GetByIdAsync` devuelve `null` y no lanza: `ISessionRepository.cs`), la agrupación por paso (`sd:184-224`), el bucle de franjas (`sd:313-332`), los objetivos por métrica (`sd:352-397`), la escala de 1,1× o 100 (`sd:390-402`), los intervalos X (`sd:525-532`) y el tooltip (`sd:495-519`). Las diferencias son las DV-90 a -95, todas declaradas.
- **Cobertura**: los 43 AC tienen componente y test en la matriz §8.3. Ninguno queda sin test.
- **Construible**: `WorkoutSnapshot.Close` (`CloseStatus.Saved` y `Failed`) existe para DT9-05. `SqliteSessionRepository` ya acepta `localZone` (DT9-16). `foreign_keys = OFF` (`DatabaseSchema.cs:96`), así que la siembra de `Rutina borrada` funciona. El catálogo tiene hoy 223 claves (257 − 34 de metadatos) y las 5 nuevas no existen todavía.
- **Repo público**: ninguno de los tres documentos tiene rutas de la PC ni datos del usuario. Las tomas usan datos sembrados en el `ROWMATE_DATA_DIR` temporal.

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección |
|---|---|---|---|---|---|
| **M-01** | mayor | architect | La spec y el diseño no coinciden en reducir movimiento. La spec dice que el cambio de métrica es **instantáneo** (AC-HIS-12-02) y que con reducir movimiento "todo aparece en su lugar al instante" (§9). El diseño hace la entrada con un fundido lineal de 200 y el cambio con un fundido de 160. `ChartTimelineTests` probaría lo contrario al AC | spec `01-po-spec.md:92`, `:239`; diseño `02-tech-design.md:270` | Manda la spec (§10.2): con reducir movimiento, la entrada y el cambio quedan en su estado final en el cuadro 0, y el test lo verifica en `t = 0`. El tooltip sigue con su opacidad, que ningún AC prohíbe |
| **M-02** | mayor | architect | El selector no entra en P360 con letra 1,3×, así que AC-HIS-12-01 ("ningún texto se corta") falla tal como está diseñado. Con los números del arte, a 12/600 y 1,3× los textos miden ≈ 300 dp; a 13/600 son ≈ 325. El ancho útil es 360 − 32 = 328, y le restan ≈ 26 del tilde, 60 del relleno mínimo (6 por lado × 5) y ≈ 6 de bordes. Quedan ≈ 236 dp para el texto, que se tiene que achicar a 0,73. El tope del diseño es 0,85, y por debajo aparece `…` | arte `00-arte-historial.md:146`; diseño `02-tech-design.md:109`, `:636` | Bajar el tope del `.rm-segmented--chart` a **0,70**, el mismo del `FitBox` de las cifras. Queda en 13 × 1,3 × 0,70 ≈ 11,8 px, más grande que los 11 px del legacy (`sd:278`). `SelectorFitsP360` verifica que la escala sea ≥ 0,70 y que no haya `…` |
| **M-03** | mayor | po | La spec sigue con los números viejos que el diseño marcó como erratas: AC-HIS-14-01 (`work 65–125, rest 125–185`) y los ceros en 420 y 425 s de AC-HIS-13-02 y de §6. El oráculo (`HistorySeedsTests`) y las sondas de CAP-07 usan E-01 y E-02, así que la verificación final daría FAIL contra el texto de la spec | spec `:97`, `:106`, `:167`; diseño `:621-624` | Corregir la spec con E-01 (`work` 65–185, `rest` 185–245 … `cooldown` 605–660) y E-02 (ceros en 425 y 430 s, dentro de la fila 6). No cambia ningún resultado visible |

### Menores (una línea cada uno)

- m-01 (architect): DT9-02 compara un `record` cuyo `Entries` es una lista, y la igualdad de un record la compara por referencia. Para que "si es igual, no emite" funcione, hace falta `SequenceEqual` sobre las `HistoryEntry`. El test `EqualListDoesNotEmit` lo cubre.
- m-02 (architect): AC-HIS-13-01 con un solo punto deja `XAxis.For(first == last)` con un rango de 0. Hay que definir el mapeo de x (centrado) para no dividir por 0, con un caso en `ChartSeriesTests`.
- m-03 (architect): si el idioma cambia en caliente, las tarjetas tienen que volver a enlazarse (`HistoryCardModel` arma los textos con el `Localizer`), y eso no figura en §5.2.
- m-04 (architect, rendimiento): cada toque arma unos 72 000 `DataPoint` (más las filas de sqlite-net) con `F-9P`, y en Mono el GC detiene todos los hilos. Medirlo en el `[PERF]` de WP-B2. Si algún cuadro pasa de 50 ms, agregar con *buffers* reusados sin materializar los puntos.
- m-05 (po/architect): AC-HIS-15-03 y CAP-09 dicen "sobre las marcas `2:08` y `2:06`", pero con un paso de 10 solo `2:10` es marca. La sonda tiene que comparar la y del objetivo con la y de su ritmo, no con una marca.
- m-06 (architect): §5.5 dice que `PointerUp` oculta el cursor "en el acto" y a la vez "con la salida de 160". Hay que elegir una de las dos (la de 160, que no choca con AC-HIS-13-07).

## 3. Para issues

- La sesión cortada o sin puntos no se distingue en la tarjeta (spec §12; paridad).
- El contraste del pulso base `#EF476F` sobre `surface.card` es 3,81:1 (D9-02, diseño §12).
- Las etiquetas X de los bordes y la del máximo del eje Y se pueden encimar (paridad con `fl_chart`).
- El detalle en dos columnas en horizontal y en el escritorio (D9-14) y la transformación de la curva entre métricas (arte §5.6).

## 4. Matriz AC → test (resumen)

Es la de [02-tech-design §8.3](02-tech-design.md), sin huecos: los 43 AC tienen al menos un test. Los `[Device-Win]`, `[Device-And]` y `[PERF]` en Android quedan **NO VERIFICABLE** sin el equipo, y se declaran así en `REPORT.md` (diseño §11).
