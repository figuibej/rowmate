# Inventario de producto — Modelos de dominio, rutinas, pasos, series y progresiones

| Campo | Valor |
|---|---|
| Área | **RUT** |
| Autor | RowMate PO |
| Fecha | 2026-10-27 |
| Fuente | `rowmate-legacy` (solo lectura). El código manda sobre los docs; las diferencias están en §6. |
| Alcance | `lib/core/models/*` (todo), `lib/features/routines/*` (todo), `lib/shared/widgets/interval_tile.dart`, y lo necesario de `database_service.dart`, `shared/theme.dart`, `main.dart`, `.arb` y los consumidores (workout, history, profile/Strava). |
| Tests legacy | `test/routine_positions_test.dart`, `test/session_stats_test.dart` (directos). Otros tests usan estos modelos solo como datos de prueba (§5.2). |

Este documento es el **contrato de paridad** del área. Lo que no esté acá se pierde en la migración. No contiene decisiones técnicas: describe el QUÉ. Las rutas `archivo:línea` son relativas a `https://github.com/figuibej/rowmate-legacy`.

Siglas de otras áreas que se citan como dependencias: **DB** (persistencia), **WKT** (motor de entrenamiento), **HUD** (pantalla de entrenamiento y barra de pasos), **HIS** (historial y detalle de sesión), **STR** (Strava), **SIM** (simulador de desarrollo), **APP** (shell, navegación, tema, l10n), **SCN** (escena).

---

## 0. Glosario

| Término | Significado en el producto |
|---|---|
| **Rutina** | Plan de entrenamiento con nombre, descripción opcional y una lista ordenada de pasos (la "plantilla"). |
| **Paso** | Un tramo de la rutina: tipo (calentamiento, trabajo, descanso, enfriamiento), una meta de duración (por tiempo **o** por distancia) y objetivos opcionales (vatios, SPM, split). |
| **Paso suelto** | Paso que no pertenece a ninguna serie. |
| **Serie** | Grupo de pasos consecutivos que comparten un identificador de grupo y se repiten N veces ("×N"). |
| **Repetición** | Una pasada completa por todos los pasos de la serie. Se numeran desde 1. |
| **Progresión** | Incremento (o decremento) por repetición de un objetivo de un paso dentro de una serie: SPM, vatios o split. |
| **Rutina expandida** | La lista de pasos que efectivamente se reman: cada serie se desenrolla N veces con las progresiones aplicadas. Es la que usan el entrenamiento, el historial y Strava. |
| **Posición** | Para cada paso de la rutina expandida: a qué serie pertenece, en qué repetición está y cuántas repeticiones tiene la serie. |
| **Split** | Tiempo cada 500 m, en segundos. Mayor = más lento. |
| **P99** | Percentil 99 según la regla exacta de F-RUT-18 (no es el promedio). |

---

## 1. Mapa de funcionalidades

| ID | Nombre | Lógica pura | Tests legacy directos |
|---|---|---|---|
| F-RUT-01 | Tipos de paso: identidad, nombre, color e ícono | Sí | — |
| F-RUT-02 | Modelo de paso (meta, objetivos, serie, progresiones) | Sí | (fixture en varios) |
| F-RUT-03 | Etiqueta de duración del paso | Sí | — |
| F-RUT-04 | Etiqueta de objetivos del paso (incluye progresiones) | Sí | — |
| F-RUT-05 | Modelo de rutina y resumen de la rutina | Sí | — |
| F-RUT-06 | Expansión de series con progresiones | Sí | `routine_positions_test` (solo largo) |
| F-RUT-07 | Posición de cada paso dentro de su serie | Sí | `routine_positions_test` |
| F-RUT-08 | Persistencia de rutinas y pasos | No | — |
| F-RUT-09 | Estado de la colección de rutinas (carga, alta, edición, baja) | No | — |
| F-RUT-10 | Pantalla "Rutinas" (lista) | No | — |
| F-RUT-11 | Editor de rutina: pantalla, datos generales y guardado | No | — |
| F-RUT-12 | Pasos sueltos en el editor (agregar, editar, eliminar) | No | — |
| F-RUT-13 | Diálogo de paso (formulario, interpretación de campos) | No | — |
| F-RUT-14 | Series en el editor (crear, ×N, agregar paso, eliminar) | No | — |
| F-RUT-15 | Lectura en vivo del remo y sus formatos | Sí | (fixture en varios) |
| F-RUT-16 | Sesión de entrenamiento (registro) | Sí | — |
| F-RUT-17 | Punto de telemetría | Sí | (fixture) |
| F-RUT-18 | Estadísticas de sesión (P99, totales, pulso) | Sí | `session_stats_test` |
| F-RUT-19 | Tarjeta de paso compartida (sin uso en el legacy) | No | — |
| F-RUT-20 | Vínculo rutina ↔ sesiones pasadas | No | — |

---

## 2. Fichas

### F-RUT-01 — Tipos de paso: identidad, nombre, color e ícono

**Valor para el usuario.** Distinguir de un vistazo qué tipo de tramo es cada paso (esfuerzo, recuperación, entrada en calor, vuelta a la calma), con un código de color coherente en toda la app.

**Comportamiento observable.**
- Existen exactamente 4 tipos, en este orden (el orden se usa en el selector del diálogo de paso): `warmup`, `work`, `rest`, `cooldown`. Ese nombre en minúsculas es el que se guarda (columna `type` de pasos, `step_type` de telemetría).
- Nombre visible (localizado):

  | Tipo | Clave `.arb` | es | en |
  |---|---|---|---|
  | warmup | `stepTypeWarmup` | Calentamiento | Warmup |
  | work | `stepTypeWork` | Trabajo | Work |
  | rest | `stepTypeRest` | Descanso | Rest |
  | cooldown | `stepTypeCooldown` | Enfriamiento | Cooldown |

- Color por tipo (identidad visual del producto, usada en editor, entrenamiento, barra de pasos, historial):

  | Tipo | Color |
  |---|---|
  | work | `#EF476F` (rojo rosado) |
  | rest | `#06D6A0` (verde menta) |
  | warmup | `#FFD166` (amarillo) |
  | cooldown | `#118AB2` (azul) |
  | cualquier otro valor (p. ej. telemetría sin tipo) | `#607D8B` (gris azulado) |

- Ícono por tipo en el editor: warmup = llama/fuego, work = pesa, rest = círculo de pausa, cooldown = copo de nieve.

**Casos borde.** Un nombre de tipo desconocido leído de la base hace fallar la carga de esa rutina (ver F-RUT-08). Un tipo desconocido en telemetría se pinta gris `#607D8B`.

**UI.** No tiene pantalla propia; se ve en F-RUT-10..14 y en HUD/HIS.

**Datos persistidos.** Nombre del tipo como texto (`warmup|work|rest|cooldown`).

**Fuente legacy.** `lib/core/models/interval_step.dart:1` (enum), `:3-15` (nombres en español fijos, sin uso, ver §7), `:17` (`isRest`, sin uso); `lib/shared/theme.dart:42-53` (nombre localizado), `:56-69` (colores); `lib/features/routines/routine_editor_screen.dart:447-458` (íconos); `lib/l10n/app_es.arb:117-120`, `app_en.arb:117-120`; `CLAUDE.md` "UI Conventions".

**Tests legacy.** Ninguno directo. `test/step_pill_test.dart` usa un paso `work` para verificar estados visuales de la barra de pasos (área HUD).

**Dependencias.** APP (l10n, tema), HUD, HIS.

**Mejora EXR.** El color de cada tipo se mantiene como identidad, pero se vive como "estado del agua y del esfuerzo": el trabajo se siente intenso y cálido, el descanso calmo y fresco. Íconos con estilo propio, nítidos y consistentes, nunca genéricos.

---

### F-RUT-02 — Modelo de paso

**Valor para el usuario.** Poder describir cada tramo del entrenamiento con precisión: cuánto dura (tiempo o metros), a qué intensidad apuntar y cómo evoluciona en cada repetición de una serie.

**Comportamiento observable (atributos del paso).**

| Atributo | Obligatorio | Significado |
|---|---|---|
| Identificador | No (lo asigna la base al guardar) | — |
| Rutina a la que pertenece | Sí | — |
| Orden | Sí | Posición 0-based dentro de la plantilla. Se renumera 0..n-1 al guardar. |
| Tipo | Sí | F-RUT-01 |
| Duración en segundos | Opcional | Meta por tiempo. |
| Distancia en metros | Opcional | Meta por distancia. |
| Vatios mínimo / máximo | Opcionales | Rango de potencia objetivo. |
| SPM objetivo | Opcional | Paladas por minuto. |
| Split objetivo (s/500 m) | Opcional | — |
| Identificador de serie | Opcional | Vacío = paso suelto. |
| Repeticiones de la serie | Opcional | Se toma la del **primer** paso de la serie. Vacío = 1. |
| Progresión SPM / vatios / split por repetición | Opcionales | Enteros con signo. |

- Todos los valores numéricos son **enteros**.
- Reglas de meta: si hay duración, el paso es "por tiempo" (aunque también tenga distancia, la duración gana en la etiqueta y en el motor de entrenamiento). Si no hay duración pero hay distancia, es "por distancia". Si no hay ninguna, el paso no tiene meta (ver casos borde).
- Fin del paso durante el entrenamiento (área WKT, se documenta acá porque depende del modelo): por tiempo termina cuando los segundos activos del paso ≥ duración; por distancia, cuando los metros remados desde el inicio del paso ≥ distancia (`lib/features/workout/workout_provider.dart:192-218`).
- Al modificar un paso con la operación de "copiar con cambios", un valor existente **no se puede borrar** (dejarlo vacío conserva el anterior). En la práctica el editor siempre arma el paso de cero desde el formulario, por lo que esto no se nota, salvo lo dicho en F-RUT-12.

**Casos borde.**
- Paso sin duración ni distancia (posible desde el editor: modo distancia con metros vacíos o no numéricos): etiqueta `?` y, durante el entrenamiento, **nunca termina solo**; el usuario queda en ese paso hasta finalizar manualmente.
- Paso con duración 0 (modo tiempo con minutos y segundos vacíos): etiqueta `0s`; durante el entrenamiento termina en el primer segundo.
- No hay validación de rangos: se aceptan negativos, vatios mín > máx, SPM 0, etc.

**UI.** Se edita en F-RUT-13.

**Datos persistidos.** Tabla de pasos (F-RUT-08).

**Fuente legacy.** `lib/core/models/interval_step.dart:21-62` (atributos), `:27` (comentario "uno de los dos debe estar definido", no se hace cumplir), `:64-65` (por tiempo / por distancia, sin uso fuera del modelo), `:111-145` (conversión a/desde base), `:147-180` (copiar con cambios).

**Tests legacy.** Ninguno directo. Usado como dato de prueba en `series_tracker_test`, `series_panels_test`, `step_target_tracker_test`, `step_pill_test`, `routine_positions_test`.

**Dependencias.** F-RUT-01; consumidores WKT, HUD, HIS, STR.

**Mejora EXR.** Un paso se piensa como "un tramo de regata": su meta se lee como el entrenador la diría ("2 000 m a 1:58", "4 min a 22 paladas"), sin ambigüedad entre minutos y metros.

---

### F-RUT-03 — Etiqueta de duración del paso

**Valor para el usuario.** Leer rápido cuánto dura un paso en el editor.

**Comportamiento observable (texto exacto, sin localizar).**
- Por tiempo, con `m` = segundos ÷ 60 (división entera) y `s` = resto:
  - `m > 0` y `s > 0` → `"{m}m {s}s"`.
  - `m > 0` y `s = 0` → `"{m}m"`.
  - `m = 0` → `"{s}s"`.
  - No hay horas: 3600 s → `60m`.
- Por distancia → `"{metros}m"`, sin separador de miles.
- Sin meta → `?`.

| Entrada | Etiqueta |
|---|---|
| 0 s | `0s` |
| 45 s | `45s` |
| 60 s | `1m` |
| 90 s | `1m 30s` |
| 120 s | `2m` |
| 3600 s | `60m` |
| 3725 s | `62m 5s` |
| 500 m | `500m` |
| 2000 m | `2000m` |
| sin duración ni distancia | `?` |
| duración y distancia | etiqueta de tiempo |

**Casos borde.** "m" significa minutos en tiempo y metros en distancia (`2m` = 2 minutos; `500m` = 500 metros). Duraciones negativas no se bloquean y dan etiquetas incoherentes. En el legacy el texto **no está localizado** (igual en es/en).

**En la app nueva (Q-01, Q-07, QA-P-04).** Los textos salen del catálogo con claves propias (§3.1), con el valor **idéntico al legacy y el mismo en es y en**. La tabla de arriba es el resultado esperado en los dos idiomas.

**UI.** Subtítulo de cada paso en el editor (F-RUT-12, F-RUT-14) y en la tarjeta compartida (F-RUT-19).

**Datos persistidos.** No.

**Fuente legacy.** `lib/core/models/interval_step.dart:67-77`.

**Tests legacy.** Ninguno. Ver casos sugeridos en §5.3.

**Dependencias.** F-RUT-02.

**Mejora EXR.** Unidades explícitas y localizadas ("2:00 min", "500 m", "2 km") con tipografía de cronómetro; nunca la letra "m" sola para dos cosas distintas. (Cambio de producto: ver §8.)

---

### F-RUT-04 — Etiqueta de objetivos del paso (incluye progresiones)

**Valor para el usuario.** Ver en una línea a qué intensidad hay que remar y cómo cambia en cada repetición.

**Comportamiento observable (texto exacto, sin localizar).** Se arman partes en este orden y se unen con `" · "` (espacio, punto medio, espacio):
1. Vatios: si hay mínimo **y** máximo → `"{mín}-{máx}W"`; si solo hay mínimo → `">{mín}W"`; si solo hay máximo → **no se muestra nada** de vatios.
2. SPM: si hay → `"{spm} spm"`.
3. Split: si hay → `"MM:SS/500m"` con minutos y segundos a 2 dígitos (125 → `02:05/500m`).
4. Progresiones: se listan las que existen y son distintas de 0, en orden SPM, vatios, split, separadas por `", "`, y la parte completa va precedida por el emoji `📈` y un espacio:
   - SPM → `"SPM +{n}/rep"` (o `"SPM -{n}/rep"` si es negativa).
   - Vatios → `"W +{n}/rep"`.
   - Split → `"Split +{n}s/rep"` / `"Split -{n}s/rep"`.
   - El signo `+` se agrega solo si es positiva; el negativo trae su propio `-`.
- Sin ninguna parte → texto vacío.
- La progresión se muestra aunque el objetivo que modifica esté vacío (en ese caso no tiene efecto, F-RUT-06).
- Los pasos de la rutina expandida **conservan** sus progresiones, así que durante el entrenamiento la etiqueta muestra el objetivo ya progresado **y** el indicador `📈 ...` (p. ej. repetición 2: `150-170W · 📈 W +10/rep`).

| Objetivos | Etiqueta |
|---|---|
| ninguno | (vacío) |
| vatios mín 150 | `>150W` |
| vatios 120–180 | `120-180W` |
| solo vatios máx 180 | (vacío) |
| SPM 22 | `22 spm` |
| split 59 / 125 / 600 | `00:59/500m` / `02:05/500m` / `10:00/500m` |
| 120–180 W, 22 SPM, split 125 | `120-180W · 22 spm · 02:05/500m` |
| lo anterior + prog. SPM 2, W 5, split −2 | `120-180W · 22 spm · 02:05/500m · 📈 SPM +2/rep, W +5/rep, Split -2s/rep` |
| solo prog. SPM 2 | `📈 SPM +2/rep` |
| prog. 0 | no se muestra |

- Combinación con la duración en los subtítulos del editor: si la etiqueta de objetivos está vacía se ve solo la duración; si no, `"{duración}  ·  {objetivos}"` (**dos** espacios a cada lado del punto medio). En la pantalla de entrenamiento la combinación es `"{tipo localizado} · {objetivos}"` (área HUD, `workout_screen.dart:672`) o los objetivos en una pastilla aparte (`immersive_workout_screen.dart:394-409`).

**Casos borde.** Splits ≤ 0 producidos por progresiones negativas dan etiquetas incoherentes (no hay piso). En el legacy el texto no está localizado.

**En la app nueva (Q-01, Q-10, QA-P-04).** Cada parte sale del catálogo con su clave (§3.1), con el valor **idéntico al legacy y el mismo en es y en** (incluido el `📈`). Los separadores `" · "` y `", "` son formato fijo, no texto traducible. La tabla de arriba es el resultado esperado en los dos idiomas.

**UI.** Editor (F-RUT-12/14), HUD.

**Datos persistidos.** No.

**Fuente legacy.** `lib/core/models/interval_step.dart:79-109`; `routine_editor_screen.dart:345-349`; `interval_tile.dart:53-56`.

**Tests legacy.** Ninguno. Ver §5.3.

**Dependencias.** F-RUT-02, F-RUT-06; HUD.

**Mejora EXR.** Objetivos como "chips" de métrica con el color de cada métrica (split ámbar, SPM cian, vatios amarillo) y una flecha de tendencia animada para las progresiones, en lugar del emoji. En el entrenamiento, la progresión se anuncia al empezar la repetición ("Rep 2 de 3 · ahora 24 spm").

---

### F-RUT-05 — Modelo de rutina y resumen de la rutina

**Valor para el usuario.** Tener rutinas con nombre propio, una descripción y un resumen corto que permita elegirlas rápido.

**Comportamiento observable.**
- Atributos: identificador (lo asigna la base), nombre (obligatorio), descripción (por defecto vacía), fecha de creación, lista de pasos (plantilla, sin expandir).
- **Resumen.** Se cuentan los pasos de la **plantilla** (no la rutina expandida):
  - Si no hay pasos de tipo trabajo → `"{total de pasos} pasos"`.
  - Si hay → `"{pasos de trabajo} series · {pasos de descanso} descansos"`.
  - "series" acá cuenta **pasos de trabajo**, no series del editor: una serie ×3 de trabajo+descanso cuenta como `1 series · 1 descansos`.
  - **Legacy:** texto fijo **sin localizar** (se ve igual con la UI en inglés) y **sin singular** (`1 pasos`, `1 descansos`).
  - **App nueva (Q-01 + Q-02, QA-P-04):** mismas reglas de conteo; el texto sale del catálogo (claves `routineSummarySteps` y `routineSummaryWorkRest`, §3.1), con singular correcto y con traducción al inglés. En inglés, "series" se traduce como *interval* (lo que cuenta son pasos de trabajo), para no confundirlo con la serie ×N del editor (`editorSeries` = "Series").

| Plantilla | Legacy (oráculo; igual en es y en) | App nueva, es | App nueva, en |
|---|---|---|---|
| calentamiento | `1 pasos` | `1 paso` | `1 step` |
| calentamiento + enfriamiento | `2 pasos` | `2 pasos` | `2 steps` |
| descanso + descanso (sin trabajo) | `2 pasos` | `2 pasos` | `2 steps` |
| trabajo | `1 series · 0 descansos` (visible en `docs/image-02.png` con la UI en inglés) | `1 serie · 0 descansos` | `1 interval · 0 rests` |
| calentamiento + serie ×3 (trabajo, descanso) + enfriamiento | `1 series · 1 descansos` | `1 serie · 1 descanso` | `1 interval · 1 rest` |
| 2 trabajos sueltos y 1 descanso | `2 series · 1 descansos` | `2 series · 1 descanso` | `2 intervals · 1 rest` |
| 1 trabajo y 2 descansos | `1 series · 2 descansos` | `1 serie · 2 descansos` | `1 interval · 2 rests` |
| 4 trabajos sueltos y 3 descansos sueltos | `4 series · 3 descansos` | `4 series · 3 descansos` | `4 intervals · 3 rests` |
| sin pasos (no alcanzable desde el editor) | `0 pasos` | `0 pasos` | `0 steps` |

- Dónde se ve el resumen: lista de rutinas (F-RUT-10), lista de rutinas de la pestaña Entrenar y su diálogo de confirmación "Iniciar" (`workoutStartContent` = legacy `"{summary}\n\nIniciar el entrenamiento?"`, app nueva `"{summary}\n\n¿Iniciar el entrenamiento?"` por Q-02 / `"{summary}\n\nStart the workout?"`; área WKT).
- **Duración total estimada**: suma de las duraciones por tiempo de la plantilla (ignora pasos por distancia y repeticiones). Existe en el modelo pero **no se muestra en ningún lado** (§7).

**Casos borde.** La fecha de creación se guarda como fecha-hora local sin zona; ver F-RUT-08.

**UI.** Ver F-RUT-10.

**Datos persistidos.** Tabla de rutinas (F-RUT-08).

**Fuente legacy.** `lib/core/models/routine.dart:8-21` (atributos), `:24-25` (duración estimada), `:100-105` (resumen), `:107-137` (conversión y copia); `lib/features/workout/workout_screen.dart:115`, `:152`; `app_es.arb:67`.

**Tests legacy.** Ninguno. Ver §5.3.

**Dependencias.** F-RUT-02; WKT (selector de rutina).

**Mejora EXR.** La tarjeta de la rutina muestra una "silueta" de la sesión: una franja de bloques con los colores de cada tipo, proporcional a la duración, más duración y distancia estimadas totales de la rutina expandida. El resumen se dice en lenguaje natural y localizado ("3 × (2 min trabajo + 1 min descanso)").

---

### F-RUT-06 — Expansión de series con progresiones

**Valor para el usuario.** Armar una serie una sola vez y que la app la reme N veces, subiendo (o bajando) la exigencia en cada repetición automáticamente.

**Comportamiento observable (regla exacta).**
1. Se recorre la plantilla en orden.
2. Un paso suelto se copia tal cual.
3. Un paso con serie inicia un **bloque**: se juntan todos los pasos **consecutivos** con el mismo identificador de serie. La cantidad de repeticiones es la del **primer** paso del bloque (vacía = 1).
4. El bloque completo se repite N veces, en orden, para r = 0, 1, …, N−1 (r es la repetición 0-based). En cada repetición, para cada paso del bloque:
   - SPM objetivo = SPM + progresión SPM × r (solo si existen ambos; si no, queda igual).
   - Vatios mín = mín + progresión vatios × r; vatios máx = máx + progresión vatios × r (la misma progresión se aplica a los dos extremos; cada uno solo si existe).
   - Split objetivo = split + progresión split × r.
   - Todo lo demás (tipo, duración, distancia, serie, repeticiones, las progresiones mismas) se conserva.
5. El último paso de la serie se incluye en todas las repeticiones, incluida la última (si la serie termina con un descanso, la rutina expandida tiene ese descanso también después de la última repetición).
6. La rutina expandida conserva identificador y nombre de la rutina original.

**Ejemplos (contrato numérico).**
- Plantilla: calentamiento 300 s · serie "a" ×3 (trabajo 500 m, enfriamiento 60 s) · enfriamiento 300 s → 8 pasos: calentamiento, trabajo, enfriamiento 60, trabajo, enfriamiento 60, trabajo, enfriamiento 60, enfriamiento 300.
- Serie ×3 (trabajo 300 s a 18 SPM con progresión +2; descanso 120 s) → 6 pasos: trabajo 18, descanso, trabajo 20, descanso, trabajo 22, descanso.
- Trabajo 150 W (solo mínimo), 22 SPM, progresión W +10 y SPM +2, ×3 → 150 W/22, 160 W/24, 170 W/26.
- Vatios 140–160 con progresión +10, ×3 → 140–160, 150–170, 160–180.
- Split 120 con progresión −2, ×3 → 120, 118, 116 (`02:00`, `01:58`, `01:56`).
- Progresión sin objetivo (SPM vacío, progresión +2) → sin efecto.

**Casos borde.**
- Sin límites: una progresión negativa puede llevar objetivos a 0 o negativos (SPM 4, −3, ×3 → 4, 1, −2).
- Repeticiones 0 o negativas (solo posibles desde la base, no desde el editor) → la serie no aporta pasos. Si toda la rutina queda sin pasos, el entrenamiento con esa rutina no puede arrancar correctamente (el motor asume al menos un paso).
- Si el mismo identificador de serie aparece en dos tramos no consecutivos (no alcanzable desde el editor), cada tramo se expande como un bloque independiente.
- Dos series contiguas con identificadores distintos se expanden por separado.
- Progresiones en un paso suelto (no alcanzable desde el editor) se ignoran: los pasos sueltos no se progresan.

**UI.** No visible en el editor (el editor muestra solo la plantilla). Se ve en el entrenamiento (HUD), en el detalle de historial (HIS) y en las vueltas de Strava (STR).

**Datos persistidos.** No se persiste la rutina expandida; se recalcula cada vez desde la plantilla actual (ver F-RUT-20). La telemetría guarda el índice del paso **expandido** (F-RUT-17).

**Fuente legacy.** `lib/core/models/routine.dart:29-70` (repeticiones por defecto `:40`, progresiones `:50-63`); consumidores `workout_provider.dart:120-131`, `session_detail_screen.dart:40-47`, `profile_provider.dart:109-111`, `:207-209`.

**Tests legacy.** `test/routine_positions_test.dart` afirma que la cantidad de posiciones es igual al largo de la rutina expandida (8) para la plantilla del primer ejemplo. **Las progresiones no tienen ningún test** (ver §5.3).

**Dependencias.** F-RUT-02, F-RUT-05; consumida por F-RUT-07, WKT, HUD, HIS, STR.

**Mejora EXR.** Una vista previa "desplegada" de la serie dentro del editor, con cada repetición y su objetivo progresado; durante el entrenamiento, el entorno acompaña la subida de exigencia (por ejemplo, la tensión visual y sonora de una regata que se acerca a la meta).

---

### F-RUT-07 — Posición de cada paso dentro de su serie

**Valor para el usuario.** Saber en todo momento "repetición 2 de 3" y que los parciales por serie se agrupen bien.

**Comportamiento observable.**
- Lista alineada **índice por índice** con la rutina expandida (mismo largo, mismo orden).
- Cada elemento: (identificador de serie, repetición desde 1, total de repeticiones).
- Paso suelto → (sin serie, 1, 1).
- Paso de serie en la repetición r (1-based) → (serie, r, N), con N tomado del primer paso del bloque (vacío = 1).
- Ejemplo (plantilla del primer ejemplo de F-RUT-06): índice 0 → (sin serie, 1, 1); 1 y 2 → (a, 1, 3); 3 y 4 → (a, 2, 3); 5 y 6 → (a, 3, 3); 7 → (sin serie, 1, 1).

**Casos borde.** Los mismos de F-RUT-06 (repeticiones ≤ 0 → sin posiciones para esa serie; ambos listados siguen alineados).

**UI.** No propia; la consume el seguimiento de series (paneles "500 m" y "Reps", área HUD/WKT).

**Datos persistidos.** No (se guarda en memoria al empezar la rutina).

**Fuente legacy.** `lib/core/models/routine.dart:5` (forma de la posición), `:74-97`; `workout_provider.dart:127`, `:181`; spec `docs/superpowers/specs/2026-10-02-series-splits-design.md` §"Routine.flattenedStepPositions".

**Tests legacy.** `test/routine_positions_test.dart` — "las posiciones quedan alineadas con flattenedSteps": con calentamiento 300 s suelto, serie "a" ×3 (trabajo 500 m, enfriamiento 60 s), enfriamiento 300 s suelto, afirma: largo de posiciones = largo de la rutina expandida = 8; posición 0 = (sin serie, 1, 1); 1 = (a, 1, 3); 2 = (a, 1, 3); 3 = (a, 2, 3); 6 = (a, 3, 3); 7 = (sin serie, 1, 1). (No verifica 4 y 5, que valen (a, 2, 3) y (a, 3, 3).)

**Dependencias.** F-RUT-06; WKT (seguimiento de series), HUD.

**Mejora EXR.** Indicador de repetición siempre legible, tipo marcador de regata ("REP 2/3"), con transición suave al cambiar de repetición.

---

### F-RUT-08 — Persistencia de rutinas y pasos

**Valor para el usuario.** Las rutinas creadas quedan guardadas en el dispositivo y están disponibles cada vez que abre la app.

**Comportamiento observable.**
- Base local en el dispositivo. Nombre: `rower_app.db`; con el simulador de desarrollo, `rower_app_dev.db` (separada, área SIM). Versión de esquema actual: 7.
- **Rutinas** (`routines`): `id` entero autoincremental, `name` texto obligatorio, `description` texto obligatorio con valor por defecto vacío, `created_at` texto obligatorio (fecha-hora ISO-8601 **local, sin zona**, p. ej. `2026-10-27T14:03:12.123456`).
- **Pasos** (`interval_steps`): `id` autoincremental, `routine_id` obligatorio, `step_order` obligatorio, `type` obligatorio (`warmup|work|rest|cooldown`), y opcionales `duration_seconds`, `distance_meters`, `target_watts_min`, `target_watts_max`, `target_spm`, `target_split_seconds`, `group_id` (texto), `group_repeat_count`, `progression_spm`, `progression_watts`, `progression_split_seconds`. Declara borrado en cascada respecto de la rutina (ver casos borde).
- Listado: todas las rutinas ordenadas por `created_at` descendente (la más nueva arriba); cada una con sus pasos ordenados por `step_order` ascendente.
- Alta: se inserta la rutina y luego cada paso con `routine_id` = id nuevo y `step_order` = 0..n-1.
- Edición: se actualizan nombre/descripción/fecha (la fecha de creación se conserva), se **borran todos** los pasos de la rutina y se reinsertan con orden 0..n-1 (los pasos quedan con identificadores nuevos).
- Baja: se borra la fila de la rutina.
- Lectura de una rutina por id (la usan historial y Strava): devuelve la rutina con sus pasos o "no existe".
- Historial de esquema relevante para el área (para importar bases viejas, si se decidiera): v2 agregó `target_split_seconds`; v3 agregó `group_id` y `group_repeat_count`; v7 agregó las tres progresiones.
- Identificador de serie guardado: texto con formato `g{milisegundos desde 1970}_{contador}` (p. ej. `g1729950000000_0`). No es un UUID aunque el comentario del modelo lo diga. No es visible para el usuario; lo único que importa es que sea único dentro de la rutina.

**Casos borde y errores.**
- **La cascada no ocurre**: la base nunca activa la verificación de claves foráneas, así que al borrar una rutina sus pasos quedan huérfanos en la tabla. No tiene efecto visible (nadie los lee y los ids de rutina no se reutilizan).
- Un `type` desconocido en una fila hace fallar la carga de toda la lista de rutinas (ver F-RUT-09: queda cargando para siempre).
- No hay transacción: si la app se corta a mitad de una edición, la rutina puede quedar con pasos parciales o sin pasos.
- Las sesiones que apuntan a una rutina borrada conservan `routine_id` y `routine_name` (F-RUT-20).

**UI.** Sin UI propia.

**Datos persistidos.** Los descriptos arriba. No usa SharedPreferences.

**Fuente legacy.** `lib/core/database/database_service.dart:11-12` (nombre y versión), `:30-73` (migraciones; v2 `:31-35`, v3 `:36-43`, v7 `:62-72`), `:77-83` (rutinas), `:86-104` (pasos), `:143-152` (listado), `:154-163` (alta), `:165-181` (edición), `:183-186` (baja), `:190-199` (pasos por rutina), `:206-212` (rutina por id); `lib/core/models/interval_step.dart:111-145`; `lib/core/models/routine.dart:107-122`; `routine_editor_screen.dart:111` (formato del id de serie).

**Tests legacy.** Ninguno.

**Dependencias.** DB (área transversal de persistencia), SIM (nombre de base).

**Mejora EXR.** Guardado instantáneo y a prueba de cortes: nunca una rutina a medio guardar. (Detalle técnico derivado al Unity Architect.)

---

### F-RUT-09 — Estado de la colección de rutinas

**Valor para el usuario.** La lista de rutinas siempre refleja lo último que hizo, en todas las pestañas (Rutinas y Entrenar) a la vez.

**Comportamiento observable.**
- Al iniciar la app se cargan las rutinas: estado "cargando" → lista.
- Crear: se guarda con fecha de creación = ahora y la rutina nueva se agrega **al principio** de la lista (coincide con el orden por fecha descendente).
- Actualizar: se guarda y se reemplaza en el mismo lugar de la lista (no se reordena).
- Eliminar: se borra y se quita de la lista.
- Los cambios se ven al instante en la pestaña Rutinas y en el selector de la pestaña Entrenar (comparten la misma colección).
- No se vuelve a cargar desde la base al cambiar de pestaña.

**Casos borde y errores.**
- Si la carga falla, la pantalla queda en "cargando" indefinidamente (no hay estado de error).
- Si guardar falla, el editor no se cierra y no hay mensaje (el error no se muestra).
- No hay protección contra doble toque en "Guardar": dos toques rápidos pueden crear la rutina dos veces.
- Al confirmar la eliminación, la pantalla no espera el resultado: la tarjeta desaparece cuando la base termina de borrar; si el borrado falla, no hay aviso y la tarjeta queda.

**UI.** F-RUT-10.

**Datos persistidos.** F-RUT-08.

**Fuente legacy.** `lib/features/routines/routines_provider.dart:12-14` (carga al iniciar), `:19-25`, `:27-38` (alta, inserción al tope `:35`), `:40-46`, `:48-52`; `lib/main.dart:83`; `workout_screen.dart:47`.

**Tests legacy.** Ninguno.

**Dependencias.** F-RUT-08; WKT (selector).

**Mejora EXR.** Estados de carga y error claros (con reintento) y una animación de entrada/salida suave de cada tarjeta al crear o borrar.

---

### F-RUT-10 — Pantalla "Rutinas" (lista)

**Valor para el usuario.** Ver, abrir, crear y borrar sus rutinas.

**Comportamiento observable.**
- Es la **tercera** pestaña de la barra inferior (orden: Dispositivo, Entrenar, Rutinas, Historial, Perfil), ícono pesa, etiqueta `navRoutines` (Rutinas / Routines).
- Título de la barra superior: `routinesTitle` (Rutinas / Routines).
- Botón flotante "+" → abre el editor en modo **nueva rutina** (F-RUT-11).
- Estados:
  - **Cargando**: indicador circular centrado.
  - **Vacío**: ícono de pesa grande (56) muy tenue (blanco 12 %), debajo `routinesEmpty` (No hay rutinas todavía / No routines yet) en blanco 38 %, y debajo `routinesEmptyHint` (Presioná + para crear una / Press + to create one) en blanco 24 %, tamaño 13.
  - **Lista**: una tarjeta por rutina, en orden de creación descendente.
- Cada tarjeta: ícono pesa en `#00B4D8` a la izquierda; título = nombre (seminegrita); debajo, la descripción (tamaño 12) **solo si no está vacía**, y el resumen (F-RUT-05) en tamaño 11, blanco 54 %; a la derecha un menú de tres puntos.
- Tocar la tarjeta → abre el editor en modo **editar** esa rutina.
- Menú: `routineEdit` (Editar / Edit) → mismo efecto que tocar; `routineDelete` (Eliminar / Delete) en rojo → diálogo de confirmación.
- Diálogo de eliminación: título `routineDeleteTitle` (Eliminar rutina / Delete routine), contenido `routineDeleteContent` con el nombre (`¿Eliminar "{name}"?` / `Delete "{name}"?`), botones `cancel` (Cancelar / Cancel) y `delete` (Eliminar / Delete, relleno rojo oscuro). Solo "Eliminar" borra; cancelar o tocar fuera no hace nada.
- Desde esta pantalla **no** se inicia un entrenamiento (eso está en la pestaña Entrenar).
- El editor se abre a pantalla completa por encima de la barra inferior.

**Casos borde.** Ver F-RUT-09 (carga fallida, borrado). No hay búsqueda, orden alternativo, duplicar ni deshacer.

**UI.** Fondo de tarjeta `#1A2E45`, radio 16; margen de lista 12; separación vertical entre tarjetas 4.

**Datos persistidos.** No directamente.

**Fuente legacy.** `lib/features/routines/routines_screen.dart:16` (título), `:17-23` (botón +), `:24-25` (cargando), `:26-40` (vacío), `:41-84` (lista y tarjeta; descripción `:56`, resumen `:59`, menú `:64-75`, toque `:76-80`), `:88-117` (acciones y diálogo); `lib/main.dart:126-180` (pestañas); `lib/shared/theme.dart:15-18` (tarjeta); `app_es.arb:6`, `:86-92`, `:122-123`.

**Tests legacy.** Ninguno.

**Dependencias.** F-RUT-05, F-RUT-09, F-RUT-11; APP (navegación).

**Mejora EXR.** Galería de rutinas con tarjetas que muestran la silueta coloreada de la sesión, duración/distancia estimadas y el último resultado con esa rutina; deslizar para borrar con "Deshacer"; estado vacío con una escena calma de agua y un llamado claro a crear la primera rutina; transición fluida y continua al abrir el editor.

---

### F-RUT-11 — Editor de rutina: pantalla, datos generales y guardado

**Valor para el usuario.** Crear o modificar una rutina completa en un solo lugar.

**Comportamiento observable.**
- Título: `editorNewRoutine` (Nueva rutina / New routine) al crear; `editorEditRoutine` (Editar rutina / Edit routine) al editar.
- Acción en la barra superior: botón de texto `save` (Guardar / Save).
- Campos: nombre (`editorRoutineName`: Nombre de la rutina / Routine name) y descripción (`editorDescription`: Descripción (opcional) / Description (optional)), esta última de 2 líneas. Al editar vienen precargados.
- Separador, y luego una fila con `editorStepsCount` (`Pasos ({count})` / `Steps ({count})`) — `count` = cantidad de pasos de la **plantilla** (una serie de 2 pasos ×3 suma 2) — y dos botones: "+ `editorStep`" (Paso / Step) y "repetir `editorSeries`" (Serie / Series).
- Sin pasos: texto centrado `editorEmptySteps` (`Todavía no hay pasos.\nPresioná "Paso" o "Serie".` / `No steps yet.\nPress "Step" or "Series".`), en blanco 38 %.
- Con pasos: lista de pasos sueltos y bloques de serie en el orden de la plantilla (F-RUT-12, F-RUT-14).
- **Guardar** valida en este orden:
  1. Nombre sin espacios al principio/fin vacío → mensaje temporal `editorNameEmpty` (El nombre no puede estar vacío / Name cannot be empty) y no guarda.
  2. Sin pasos → mensaje temporal `editorNoSteps` (Agregá al menos un paso / Add at least one step) y no guarda.
  3. Si pasa: nombre y descripción se guardan **recortados** (sin espacios al principio/fin); el orden de los pasos se renumera 0..n-1; al crear, fecha de creación = ahora; al editar se conservan id y fecha de creación. Luego se cierra el editor y se vuelve a la lista.
- No se valida unicidad del nombre ni largo máximo.
- Salir con "atrás" **descarta** todos los cambios sin preguntar.

**Casos borde.** Doble toque en Guardar (F-RUT-09). Error al guardar: el editor queda abierto sin aviso.

**UI.** Campos con borde; márgenes 16. Ver F-RUT-12..14 para la lista.

**Datos persistidos.** F-RUT-08.

**Fuente legacy.** `lib/features/routines/routine_editor_screen.dart:23-31` (precarga), `:40-73` (guardar: nombre `:42-47`, pasos `:48-53`, renumeración `:56-60`, alta `:62-63`, edición `:65-69`, cierre `:72`), `:202-273` (pantalla: título `:207`, guardar `:209`, campos `:218-233`, contador `:244`, botones `:247-257`, vacío `:263-267`); `app_es.arb:126`, `:128-146`.

**Tests legacy.** Ninguno.

**Dependencias.** F-RUT-09, F-RUT-12, F-RUT-13, F-RUT-14.

**Mejora EXR.** Constructor visual de la sesión: la rutina se arma como una línea de tiempo de bloques coloreados con duración/distancia total recalculada en vivo; validación amable en línea (no solo mensajes al guardar); aviso de "cambios sin guardar" al salir; guardado con una confirmación visual sutil.

---

### F-RUT-12 — Pasos sueltos en el editor

**Valor para el usuario.** Agregar, corregir y quitar tramos individuales.

**Comportamiento observable.**
- "+ Paso" abre el diálogo de paso en modo **nuevo** y **sin** sección de progresiones (F-RUT-13). Confirmar agrega el paso **al final** de la plantilla, como paso suelto (aunque lo último sea una serie, queda después de ella). Cancelar no hace nada.
- Cada paso se muestra como una fila con: borde izquierdo de 3 en el color del tipo; cuadradito de 28 con el ícono del tipo sobre el color al 20 %; título = nombre localizado del tipo, en el color del tipo, seminegrita 13; subtítulo = duración (y objetivos) según F-RUT-03/04, tamaño 11; a la derecha, lápiz (editar) y cruz roja (eliminar).
- Lápiz → diálogo en modo **editar** con los valores del paso; muestra progresiones solo si el paso está en una serie. Confirmar reemplaza el paso en su lugar conservando su serie y repeticiones. Cancelar no cambia nada.
- Cruz → elimina el paso **al instante**, sin confirmación ni deshacer.
- No se puede reordenar, duplicar, mover entre series ni convertir un paso suelto en serie.

**Casos borde.** Ver F-RUT-13 (campos vacíos o inválidos). Si se edita un paso suelto que (por datos viejos) tuviera progresiones, se conservan aunque no se vean.

**UI.** Fila con fondo de tarjeta, radio 10, separación vertical 3; pasos dentro de serie con sangría izquierda de 16 (F-RUT-14).

**Datos persistidos.** Al guardar la rutina (F-RUT-11).

**Fuente legacy.** `routine_editor_screen.dart:75-88` (agregar), `:90-108` (editar, conserva serie `:100-105`), `:190-194` (eliminar), `:316-369` (fila), `:447-458` (íconos); `app_es.arb:138`.

**Tests legacy.** Ninguno.

**Dependencias.** F-RUT-01, F-RUT-03, F-RUT-04, F-RUT-13.

**Mejora EXR.** Arrastrar para reordenar, deslizar para borrar con "Deshacer", duplicar con un gesto, microanimaciones y respuesta háptica al soltar un bloque.

---

### F-RUT-13 — Diálogo de paso

**Valor para el usuario.** Definir con precisión el tramo: tipo, meta y objetivos.

**Comportamiento observable.**
- Título: `editorNewStep` (Nuevo paso / New step) o `editorEditStep` (Editar paso / Edit step).
- Valores iniciales en modo nuevo: tipo **Trabajo**, modo **Por tiempo**, todos los campos vacíos.
- Campos, en orden:
  1. Selector `editorStepType` (Tipo / Type) con los 4 tipos localizados (F-RUT-01).
  2. Selector de dos segmentos: `editorByTime` (Por tiempo / By time) y `editorByDistance` (Por distancia / By distance).
  3. Por tiempo: `editorMinutes` (Min) y `editorSeconds` (Seg / Sec), lado a lado. Por distancia: `editorMeters` (Metros / Meters).
  4. Separador y rótulo `editorOptionalTargets` (Objetivos opcionales / Optional targets), tamaño 12, blanco 54 %.
  5. `editorWattsMin` (W mín / W min) y `editorWattsMax` (W máx / W max), lado a lado.
  6. `editorTargetSpm` (SPM objetivo / Target SPM).
  7. `editorSplitMin` (Split min) y `editorSplitSec` (Split seg / Split sec), lado a lado.
  8. **Solo si el paso está en una serie**: separador, rótulo `editorProgressions` (Progresiones (solo aplica en series) / Progressions (only applies to series)) y tres campos con texto de ayuda:
     - `editorProgressionSpm` (Incremento SPM (+/rep) / SPM increment (+/rep)), ayuda `editorProgressionSpmHint` (Ej: 2 / e.g: 2).
     - `editorProgressionWatts` (Incremento Watts (+/rep) / Watts increment (+/rep)), ayuda `editorProgressionWattsHint` (Ej: 5 / e.g: 5).
     - `editorProgressionSplit` (Cambio Split segundos (+/-/rep) / Split change in seconds (+/-/rep)), ayuda `editorProgressionSplitHint` (Ej: -2 (más rápido) / e.g: -2 (faster)).
- Todos los campos numéricos abren teclado numérico (en móvil).
- Botones: `cancel` (Cancelar) cierra sin cambios; `confirm` (Confirmar / Confirm) devuelve el paso. **No hay validación**: siempre se puede confirmar.
- Interpretación al confirmar (cada texto se recorta y se interpreta como entero; si no es un entero válido, se toma como vacío):
  - Modo tiempo: duración = minutos × 60 + segundos, donde un campo vacío/inválido vale 0. Se guarda **siempre** una duración (aunque sea 0) y la distancia queda vacía.
  - Modo distancia: distancia = metros (vacío/inválido → sin distancia); la duración queda vacía.
  - Vatios mín, vatios máx, SPM, progresiones: entero o vacío. Se aceptan negativos, el signo "+", ceros a la izquierda (`0150` = 150) y hexadecimal (`0x10` = 16): es la regla de `int.tryParse` de Dart (`<flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm_shared/lib/integers_patch.dart:84-123`). Un número con decimales (`5.0`, `1,5`, `1e3`), con el signo menos tipográfico (`−2`) o con dígitos de ancho completo cuenta como vacío.
  - Split: total = minutos × 60 + segundos (vacíos valen 0); si el total es ≤ 0, **no hay** objetivo de split.
- Precarga en modo editar: tipo; modo = distancia si el paso tiene distancia, si no tiempo; duración desglosada en minutos (división entera) y segundos (resto) sin ceros a la izquierda (90 s → "1" y "30"; 45 s → "0" y "45"); metros; vatios; SPM; split desglosado igual (125 → "2" y "5"); progresiones. Con una duración negativa (solo posible escribiéndola) vale la regla de Dart, con el resto nunca negativo: −90 s se precarga "-1" y "30", y al confirmar sin tocar nada queda −30 s (la etiqueta dice `30s` antes y después); −30 s se precarga "0" y "30" y queda 30 s. Un paso sin meta (`?`) se abre en modo tiempo con los campos vacíos y, al confirmar, queda de 0 s.
- Cambiar de modo no borra lo escrito en el otro modo mientras el diálogo esté abierto, pero al confirmar solo se usa el modo elegido.
- Se pueden cargar segundos > 59 (p. ej. 0 min 90 s = 90 s; al reabrir se ve 1 min 30 s).

**Casos borde.**
- Modo tiempo con todo vacío → paso de 0 s (`0s`), que en el entrenamiento dura ~1 s.
- Modo distancia con metros vacíos → paso sin meta (`?`), que en el entrenamiento nunca termina solo.
- Pasar un paso de distancia a tiempo sin cargar minutos/segundos → paso de 0 s.
- Vatios mín > máx, SPM 0 (`0 spm`), vatios 0 (`>0W`), negativos: todo se acepta tal cual.

**UI.** Diálogo con desplazamiento vertical; campos con borde; separaciones de 8–12.

**Datos persistidos.** Al guardar la rutina.

**Fuente legacy.** `routine_editor_screen.dart:474-739` (valores iniciales `:484-485`, precarga `:498-521`, interpretación `:531-555`, split `:557-562`, campos `:574-686`, progresiones `:688-725`, botones `:729-736`); `app_es.arb:124-125`, `:147-167`.

**Tests legacy.** Ninguno.

**Dependencias.** F-RUT-01, F-RUT-02; F-RUT-12 y F-RUT-14 lo abren.

**Mejora EXR.** Selectores tipo rueda de cronómetro para tiempo y split, selector de distancia con valores típicos (250, 500, 1 000, 2 000 m), vista previa en vivo de la etiqueta y, en series, de cómo queda cada repetición con la progresión; mensajes claros cuando una combinación no tiene sentido.

---

### F-RUT-14 — Series en el editor

**Valor para el usuario.** Construir intervalos repetidos ("3 × 2 min trabajo + 1 min descanso") sin cargar cada repetición a mano.

**Comportamiento observable.**
- Botón "Serie" agrega **al final** una serie nueva con identificador único y **dos pasos de ejemplo**: Trabajo 120 s y Descanso 60 s, ambos con **×3** repeticiones y sin objetivos. No abre ningún diálogo.
- Cada serie se muestra como un bloque: **encabezado**, los pasos de la serie (con sangría 16) y un **pie**.
- Encabezado: fondo `#0077B6` al 12 %, esquinas superiores redondeadas (12), borde izquierdo de 3 en `#0077B6`; ícono de repetir y texto `editorSeries` (Serie / Series) en `#00B4D8` negrita; una pastilla tocable con `×{N}` (signo de multiplicación) en `#00B4D8` sobre `#00B4D8` al 20 %; a la derecha un tacho rojo con ayuda emergente `editorDeleteSeries` (Eliminar serie / Delete series).
- Tocar `×N` abre un diálogo: título `editorRepetitions` (Repeticiones / Repetitions), un campo numérico precargado con N, con foco automático, rótulo `editorAmount` (Cantidad / Amount), botones `cancel` y `ok` (OK). Al aceptar:
  - Texto no numérico o vacío → se toma **1**.
  - Valor > 0 → se aplica a **todos** los pasos de la serie.
  - Valor ≤ 0 → se ignora (queda el N anterior).
  - Cancelar → sin cambios. Sin máximo.
- Pie: fondo `#0077B6` al 6 %, esquinas inferiores redondeadas, mismo borde; botón "+ `editorAddStepToSeries`" (Agregar paso a serie / Add step to series) en `#00B4D8`, tamaño 12. Abre el diálogo de paso en modo nuevo **con** progresiones; al confirmar inserta el paso **al final de esa serie** (después de su último paso) con las mismas repeticiones que la serie.
- Tacho del encabezado → elimina la serie completa (todos sus pasos) **al instante**, sin confirmación.
- Editar un paso de serie: diálogo con progresiones; conserva serie y repeticiones.
- Si se borran uno a uno todos los pasos de una serie, la serie desaparece (encabezado y pie incluidos).
- Una serie puede tener un solo paso. Varias series pueden quedar contiguas; cada una es independiente.
- No se puede mover un paso suelto dentro de una serie ni sacar un paso de una serie, ni reordenar series.

**Casos borde.** Repeticiones vacías en datos viejos → se muestran y se reman como ×1.

**UI.** Ver arriba. Separación de 8 arriba del encabezado y abajo del pie.

**Datos persistidos.** `group_id` y `group_repeat_count` en cada paso de la serie; progresiones por paso (F-RUT-08).

**Fuente legacy.** `routine_editor_screen.dart:110-133` (nueva serie, id `:111`), `:135-149` (agregar paso a serie), `:151-188` (repeticiones: interpretación `:173`, regla > 0 `:179`), `:196-200` (eliminar serie), `:275-314` (armado de bloques), `:371-422` (encabezado), `:424-445` (pie); `app_es.arb:139-144`.

**Tests legacy.** Ninguno.

**Dependencias.** F-RUT-06, F-RUT-07, F-RUT-12, F-RUT-13.

**Mejora EXR.** La serie se ve como un bloque "apilado" que deja intuir sus N repeticiones (capas que se despliegan con una animación suave al tocar), con control de repeticiones tipo "− N +", vista previa expandida con las progresiones y la duración/distancia total de la serie.

---

### F-RUT-15 — Lectura en vivo del remo y sus formatos

**Valor para el usuario.** Ver las métricas del remo con formatos consistentes en toda la app.

**Comportamiento observable.**
- Lectura instantánea con: ritmo de paladas (SPM, con decimales), paladas totales, distancia (m), split (s/500 m), potencia (W), calorías totales (kcal), pulso (lpm; **0 = sin pulso**) y tiempo transcurrido (s). Todo en 0 por defecto.
- Split formateado: si es ≤ 0 → `--:--`; si no, `MM:SS` con minutos y segundos a 2 dígitos (125 → `02:05`; 600 → `10:00`; minutos > 99 se muestran completos).
- Tiempo formateado: si hay horas → `H:MM:SS` (horas sin ceros a la izquierda); si no → `MM:SS`. (59 → `00:59`; 3599 → `59:59`; 3600 → `1:00:00`; 3661 → `1:01:01`.)
- Copia con cambios: permite reemplazar campos sueltos (por ejemplo, el pulso del pulsómetro reemplaza al del remo; área HRM/WKT).

**Casos borde.** Pulso 0 significa "sin dato", no "0 lpm".

**UI.** La consumen Dispositivo, HUD, escena.

**Datos persistidos.** No directamente (se muestrea a telemetría, F-RUT-17).

**Fuente legacy.** `lib/core/models/rowing_data.dart:2-21`, `:23-29`, `:31-40`, `:42-62`; CLAUDE.md "Data Flow".

**Tests legacy.** Usado como dato de prueba en `step_target_tracker_test`, `workout_provider_hr_test`, `scene/scene_view_test` (áreas WKT/HRM/SCN).

**Dependencias.** BLE (origen), SIM, HRM, WKT, HUD.

**Mejora EXR.** Números con tipografía de alto contraste y transiciones suaves entre valores (sin saltos bruscos), legibles a distancia mientras se rema.

---

### F-RUT-16 — Sesión de entrenamiento (registro)

**Valor para el usuario.** Cada entrenamiento queda registrado con su rutina, fecha y totales.

**Comportamiento observable.**
- Atributos: id; rutina (id y nombre; ambos vacíos en entrenamiento libre); inicio; fin (vacío mientras está en curso); distancia total (m); tiempo total (s); "potencia promedio" (W); "SPM promedio" (decimal); calorías totales; id de actividad de Strava (vacío si no se subió).
- **Ojo**: los campos "promedio" guardan en realidad el **P99** de F-RUT-18 (al finalizar: potencia = P99 de vatios, SPM = P99 de SPM). Distancia, tiempo y calorías = máximos de la telemetría.
- Fechas: se guardan en UTC (ISO-8601 con `Z`) y se muestran en hora local.
- Duración formateada: igual regla que el tiempo de F-RUT-15 (`H:MM:SS` o `MM:SS`).
- Sin nombre de rutina, las pantallas muestran `workoutFree` (Entrenamiento libre / Free workout) o `historyFree` (Libre / Free); Strava usa `RowMate Session` (área STR).
- Valores faltantes al leer: numéricos en 0.

**Casos borde.** La sesión se crea al empezar (para tener id) y se completa al terminar (área WKT). Si la rutina se borra después, la sesión conserva nombre e id (F-RUT-20).

**UI.** Historial, detalle, perfil (áreas HIS/PRF).

**Datos persistidos.** Tabla `workout_sessions`: `id`, `routine_id`, `routine_name`, `started_at` (obligatorio), `finished_at`, `total_distance_meters`, `total_time_seconds`, `avg_power_watts`, `avg_stroke_rate` (decimal), `total_calories` (los cinco obligatorios con 0 por defecto), `strava_activity_id`.

**Fuente legacy.** `lib/core/models/workout_session.dart:2-68` (duración `:29-37`, guardado `:39-51`, lectura `:53-67`); `database_service.dart:107-120`; `workout_provider.dart:139-145`, `:276-292`; `strava_api_service.dart:93`.

**Tests legacy.** Ninguno directo.

**Dependencias.** F-RUT-18; WKT, HIS, STR, PRF, DB.

**Mejora EXR.** El registro se presenta como "resultado de regata": tiempo, distancia, split y potencia representativos, con la rutina y el escenario donde se remó.

---

### F-RUT-17 — Punto de telemetría

**Valor para el usuario.** Gráficos y desglose por paso de cada sesión.

**Comportamiento observable.**
- Atributos: id, sesión, tiempo transcurrido (s, **tomado del monitor**, no del reloj de la app), SPM (decimal), paladas, distancia, split, potencia, calorías, pulso, índice del paso **en la rutina expandida** (vacío en entrenamiento libre) y tipo del paso (`warmup|work|rest|cooldown`, vacío en libre).
- Se toma uno cada 5 s activos y uno final al terminar (área WKT).
- Al leer, se ordenan por tiempo transcurrido ascendente.

**Casos borde.** El índice del paso solo tiene sentido contra la rutina expandida **actual** (F-RUT-20).

**UI.** Gráfico y regiones por paso del detalle (HIS), vueltas TCX (STR).

**Datos persistidos.** Tabla `data_points`: `id`, `session_id`, `elapsed_seconds`, `stroke_rate` (decimal), `stroke_count`, `distance_meters`, `pace_500m_seconds`, `power_watts`, `calories`, `heart_rate` (todos obligatorios con 0 por defecto salvo id/sesión/tiempo), `step_index`, `step_type`.

**Fuente legacy.** `lib/core/models/workout_session.dart:140-183`; `database_service.dart:123-138`, `:254-287`; `workout_provider.dart:158-172`, `:258-273`.

**Tests legacy.** Usado como dato en `session_stats_test`.

**Dependencias.** F-RUT-06; WKT, HIS, STR, DB.

**Mejora EXR.** Sin cambios de experiencia directos; habilita repeticiones de la sesión y comparativas ricas.

---

### F-RUT-18 — Estadísticas de sesión

**Valor para el usuario.** Un resumen representativo de cada sesión (potencia, ritmo, split, pulso, totales).

**Comportamiento observable (regla exacta).**
- Sin puntos → todo en 0 (sin pulso).
- **P99** de una lista: se toman solo los valores **> 0**, se ordenan de menor a mayor y se elige el de índice `piso((n − 1) × 0,99)` (0-based), calculado en doble precisión. Lista vacía → 0.
  - Vatios P99 → redondeado a entero. SPM P99 → decimal (sin redondear). Split P99 → redondeado a entero.
  - Tabla de índices: n=1 → 0; **n=2 → 0 (el menor)**; n=3 → 1; n=10 → 8; n=100 → 98; n=101 → 99; n=102 → 99; n=361 → 356.
  - Como el split mayor es el más lento, el "split P99" es casi el **más lento** de la sesión (al revés que vatios, donde es casi el máximo).
- Distancia total, tiempo total y calorías totales = **máximo** de cada columna entre todos los puntos (incluye ceros).
- Pulso promedio = promedio de los pulsos > 0, redondeado al entero más cercano (.5 hacia arriba: 120,5 → 121). Pulso máximo = máximo de los pulsos > 0. Sin pulsos → ambos 0.
- "Tiene pulso" = pulso máximo > 0.
- Formatos: split → `--:--` si ≤ 0, si no `MM:SS`; duración → `H:MM:SS` o `MM:SS` (como F-RUT-15).
- Dónde se usa: tarjeta de historial y detalle (vatios como entero, SPM con 1 decimal, split formateado, chip de pulso solo si tiene pulso), cierre de sesión e importación/subida de Strava (los "promedios" de F-RUT-16).

**Ejemplos.** Vatios [0, 100, 200, 300] → positivos [100, 200, 300], n=3, índice 1 → **200**. Splits [130, 0, 125, 140] → [125, 130, 140] → **130** (`02:10`). Pulsos [0, 120, 150, 0, 141] → promedio 137, máximo 150.

**Casos borde.** Con muy pocos puntos el P99 puede ser el mínimo (n=2). La consulta agregada del historial no carga paladas ni índices de paso (no los necesita).

**UI.** HIS (chips), PRF.

**Datos persistidos.** No (se calcula al vuelo desde telemetría; los valores de cierre quedan en la sesión, F-RUT-16).

**Fuente legacy.** `lib/core/models/workout_session.dart:71-136` (tiene pulso `:92`, split `:94-99`, duración `:101-109`, P99 `:111-116`, cálculo `:118-135`); `database_service.dart:289-322`; `history_screen.dart:111-116`; `session_detail_screen.dart:150-167`; `profile_provider.dart:165-186`, `:376-385`; `workout_provider.dart:276-292`; spec `2026-10-02-heart-rate-monitor-design.md` §"Estadísticas de sesión".

**Tests legacy.** `test/session_stats_test.dart`:
- "avgHeartRate y maxHeartRate ignoran los ceros": pulsos [0, 120, 150, 0, 141] → promedio 137 ((120+150+141)/3), máximo 150, tiene pulso = sí.
- "sin pulso quedan en 0": pulsos [0, 0] → promedio 0, máximo 0, tiene pulso = no.
- "sin puntos quedan en 0": lista vacía → tiene pulso = no.

**Dependencias.** F-RUT-17; HIS, PRF, STR, WKT.

**Mejora EXR.** Resumen post-sesión estilo "podio": métricas destacadas con animación de conteo, comparativa con la mejor sesión de la misma rutina y gráfico limpio y fluido.

---

### F-RUT-19 — Tarjeta de paso compartida (sin uso en el legacy)

**Valor para el usuario.** Ninguno hoy: el componente existe pero ninguna pantalla lo usa.

**Comportamiento observable (si se usara).** Fila con borde izquierdo de 4 en el color del tipo, radio 12; fondo del color al 15 % si es el paso activo; recuadro de 36 con el número de orden + 1; título = tipo localizado; subtítulo = duración (y objetivos) con la misma regla de F-RUT-04, tamaño 12; botones editar y eliminar solo si se pide edición.

**Casos borde.** —

**UI.** No alcanzable.

**Datos persistidos.** No.

**Fuente legacy.** `lib/shared/widgets/interval_tile.dart:7-76`.

**Tests legacy.** Ninguno.

**Dependencias.** F-RUT-01, F-RUT-03, F-RUT-04.

**Mejora EXR.** No migrar como pantalla; su idea (número de paso + paso activo resaltado) se puede reutilizar en la vista previa de la rutina.

---

### F-RUT-20 — Vínculo rutina ↔ sesiones pasadas

**Valor para el usuario.** Ver en el historial y en Strava el desglose por paso de una sesión hecha con rutina.

**Comportamiento observable.**
- La sesión guarda solo el id y el nombre de la rutina; no guarda una copia de los pasos.
- El detalle de historial y la subida a Strava vuelven a leer la rutina **actual** por id y la expanden (F-RUT-06) para mapear cada índice de paso de la telemetría.
- Consecuencias:
  - Si después de entrenar se **edita** la rutina, el desglose por paso de sesiones pasadas (regiones del gráfico, vueltas de Strava) se recalcula con la definición nueva y puede quedar desalineado.
  - Si se **borra** la rutina, las sesiones pasadas siguen mostrando el nombre, pero pierden el desglose por paso (se tratan como sin pasos). El tipo de paso guardado en cada punto sigue disponible.
  - Renombrar la rutina no cambia el nombre ya guardado en las sesiones pasadas.

**Casos borde.** Ver arriba.

**UI.** HIS, STR.

**Datos persistidos.** `workout_sessions.routine_id`, `routine_name`; `data_points.step_index`, `step_type`.

**Fuente legacy.** `session_detail_screen.dart:40-47`; `profile_provider.dart:107-113`, `:205-210`; `workout_provider.dart:139-145`, `:285-286`.

**Tests legacy.** Ninguno.

**Dependencias.** F-RUT-06, F-RUT-08, F-RUT-16, F-RUT-17; HIS, STR.

**Mejora EXR.** Cada sesión debería conservar la rutina tal como se remó (cambio de producto propuesto, §8), para que el historial sea fiel aunque la rutina evolucione.

---

## 3. Textos visibles del área (claves `.arb`)

| Clave | es | en | Dónde |
|---|---|---|---|
| `navRoutines` | Rutinas | Routines | pestaña |
| `routinesTitle` | Rutinas | Routines | F-RUT-10 |
| `routinesEmpty` | No hay rutinas todavía | No routines yet | F-RUT-10 |
| `routinesEmptyHint` | Presioná + para crear una | Press + to create one | F-RUT-10 |
| `routineEdit` | Editar | Edit | F-RUT-10 |
| `routineDelete` | Eliminar | Delete | F-RUT-10 |
| `routineDeleteTitle` | Eliminar rutina | Delete routine | F-RUT-10 |
| `routineDeleteContent` | ¿Eliminar "{name}"? | Delete "{name}"? | F-RUT-10 |
| `stepTypeWarmup/Work/Rest/Cooldown` | Calentamiento / Trabajo / Descanso / Enfriamiento | Warmup / Work / Rest / Cooldown | F-RUT-01 |
| `cancel`, `delete`, `ok`, `confirm`, `save` | Cancelar, Eliminar, OK, Confirmar, Guardar | Cancel, Delete, OK, Confirm, Save | varios |
| `editorNewRoutine` / `editorEditRoutine` | Nueva rutina / Editar rutina | New routine / Edit routine | F-RUT-11 |
| `editorRoutineName` | Nombre de la rutina | Routine name | F-RUT-11 |
| `editorDescription` | Descripción (opcional) | Description (optional) | F-RUT-11 |
| `editorStepsCount` | Pasos ({count}) | Steps ({count}) | F-RUT-11 |
| `editorStep` / `editorSeries` | Paso / Serie | Step / Series | F-RUT-11, 14 |
| `editorEmptySteps` | Todavía no hay pasos.⏎Presioná "Paso" o "Serie". | No steps yet.⏎Press "Step" or "Series". | F-RUT-11 |
| `editorDeleteSeries` | Eliminar serie | Delete series | F-RUT-14 |
| `editorAddStepToSeries` | Agregar paso a serie | Add step to series | F-RUT-14 |
| `editorRepetitions` / `editorAmount` | Repeticiones / Cantidad | Repetitions / Amount | F-RUT-14 |
| `editorNameEmpty` | El nombre no puede estar vacío | Name cannot be empty | F-RUT-11 |
| `editorNoSteps` | Agregá al menos un paso | Add at least one step | F-RUT-11 |
| `editorNewStep` / `editorEditStep` | Nuevo paso / Editar paso | New step / Edit step | F-RUT-13 |
| `editorStepType` | Tipo | Type | F-RUT-13 |
| `editorByTime` / `editorByDistance` | Por tiempo / Por distancia | By time / By distance | F-RUT-13 |
| `editorMinutes` / `editorSeconds` / `editorMeters` | Min / Seg / Metros | Min / Sec / Meters | F-RUT-13 |
| `editorOptionalTargets` | Objetivos opcionales | Optional targets | F-RUT-13 |
| `editorWattsMin` / `editorWattsMax` | W mín / W máx | W min / W max | F-RUT-13 |
| `editorTargetSpm` | SPM objetivo | Target SPM | F-RUT-13 |
| `editorSplitMin` / `editorSplitSec` | Split min / Split seg | Split min / Split sec | F-RUT-13 |
| `editorProgressions` | Progresiones (solo aplica en series) | Progressions (only applies to series) | F-RUT-13 |
| `editorProgressionSpm` (+`Hint`) | Incremento SPM (+/rep) — Ej: 2 | SPM increment (+/rep) — e.g: 2 | F-RUT-13 |
| `editorProgressionWatts` (+`Hint`) | Incremento Watts (+/rep) — Ej: 5 | Watts increment (+/rep) — e.g: 5 | F-RUT-13 |
| `editorProgressionSplit` (+`Hint`) | Cambio Split segundos (+/-/rep) — Ej: -2 (más rápido) | Split change in seconds (+/-/rep) — e.g: -2 (faster) | F-RUT-13 |
| `workoutStartContent` | {summary}⏎⏎Iniciar el entrenamiento? (app nueva: `¿Iniciar el entrenamiento?`, Q-02) | {summary}⏎⏎Start the workout? | WKT (usa F-RUT-05) |
| `workoutFree` / `historyFree` | Entrenamiento libre / Libre | Free workout / Free | WKT/HIS (sesión sin rutina) |

Textos **sin clave en el legacy** (fijos en el código, iguales en ambos idiomas): resumen de rutina (`N pasos`, `N series · M descansos`), etiqueta de duración (`1m 30s`, `500m`, `?`), etiqueta de objetivos (`>150W`, `22 spm`, `02:05/500m`, `📈 SPM +2/rep`…), `×N` del encabezado de serie, `--:--`. Fuente: `app_es.arb`/`app_en.arb` líneas 6, 51, 67, 86-92, 101, 117-167. En la app nueva, el resumen y las etiquetas pasan al catálogo en la It. 1 (§3.1). El `×N` lo localiza la It. 3 (editor) y `--:--` es un formato numérico (F-BLE-01), no un texto.

### 3.1 Claves nuevas de la It. 1: resumen y etiquetas (Q-01 + Q-02, QA-P-04)

Tabla exacta para el `01-po-spec` de la It. 1. Los nombres de clave son los que se usan en specs y tests. Los valores están en la sintaxis de mensaje de los `.arb` (`{param}` y plural ICU `one`/`other`); en es y en, `one` es exactamente 1 y todo lo demás (incluido 0) es `other`. El Architect puede partir una clave en dos si el formateador no admite dos plurales en un mismo mensaje, **siempre que el texto final sea el mismo**.

| Clave | es | en | Parámetros | Cuándo |
|---|---|---|---|---|
| `routineSummarySteps` | `{count, plural, one{# paso} other{# pasos}}` | `{count, plural, one{# step} other{# steps}}` | `count`: total de pasos de la plantilla | la plantilla no tiene pasos de trabajo |
| `routineSummaryWorkRest` | `{work, plural, one{# serie} other{# series}} · {rest, plural, one{# descanso} other{# descansos}}` | `{work, plural, one{# interval} other{# intervals}} · {rest, plural, one{# rest} other{# rests}}` | `work`, `rest`: pasos de trabajo y de descanso de la plantilla | hay ≥ 1 paso de trabajo |
| `stepDurationMinSec` | `{m}m {s}s` | `{m}m {s}s` | `m` = segundos ÷ 60 (entero), `s` = resto | por tiempo, m > 0 y s > 0 |
| `stepDurationMin` | `{m}m` | `{m}m` | `m` | por tiempo, m > 0 y s = 0 |
| `stepDurationSec` | `{s}s` | `{s}s` | `s` | por tiempo, m = 0 |
| `stepDurationMeters` | `{meters}m` | `{meters}m` | `meters`: entero sin separador de miles | por distancia (sin duración) |
| `stepDurationNone` | `?` | `?` | — | sin duración ni distancia |
| `stepTargetWattsRange` | `{min}-{max}W` | `{min}-{max}W` | `min`, `max` | vatios mín. y máx. |
| `stepTargetWattsMin` | `>{min}W` | `>{min}W` | `min` | solo vatios mín. (solo máx. → no se muestra nada) |
| `stepTargetSpm` | `{spm} spm` | `{spm} spm` | `spm` | hay SPM objetivo |
| `stepTargetSplit` | `{split}/500m` | `{split}/500m` | `split`: `MM:SS` con 2 dígitos cada uno (125 → `02:05`) | hay split objetivo |
| `stepTargetProgressions` | `📈 {list}` | `📈 {list}` | `list`: progresiones unidas por `", "` en orden SPM, vatios, split | hay ≥ 1 progresión ≠ 0 |
| `stepProgressionSpm` | `SPM {delta}/rep` | `SPM {delta}/rep` | `delta`: con signo, `+2` o `-1` | progresión de SPM ≠ 0 |
| `stepProgressionWatts` | `W {delta}/rep` | `W {delta}/rep` | `delta` | progresión de vatios ≠ 0 |
| `stepProgressionSplit` | `Split {delta}s/rep` | `Split {delta}s/rep` | `delta` | progresión de split ≠ 0 |

Resultados esperados: los del resumen están en la tabla de F-RUT-05 (columnas "App nueva"); los de las etiquetas, en las tablas de F-RUT-03 y F-RUT-04 (iguales en es y en). Las 4 claves del catálogo legacy que cambian por Q-02 están en [FEATURE_INVENTORY §4.1.1](../FEATURE_INVENTORY.md).

---

## 4. Datos persistidos del área (resumen)

| Tabla | Columnas | Notas |
|---|---|---|
| `routines` | `id`, `name`, `description` (def. ''), `created_at` (ISO local sin zona) | orden de lista: `created_at` desc |
| `interval_steps` | `id`, `routine_id`, `step_order`, `type`, `duration_seconds`, `distance_meters`, `target_watts_min`, `target_watts_max`, `target_spm`, `target_split_seconds`, `group_id`, `group_repeat_count`, `progression_spm`, `progression_watts`, `progression_split_seconds` | se reemplazan todos al editar; quedan huérfanos al borrar la rutina |
| `workout_sessions` | `id`, `routine_id`, `routine_name`, `started_at` (UTC), `finished_at` (UTC), `total_distance_meters`, `total_time_seconds`, `avg_power_watts` (=P99), `avg_stroke_rate` (=P99), `total_calories`, `strava_activity_id` | — |
| `data_points` | `id`, `session_id`, `elapsed_seconds`, `stroke_rate`, `stroke_count`, `distance_meters`, `pace_500m_seconds`, `power_watts`, `calories`, `heart_rate`, `step_index` (rutina expandida), `step_type` | — |

SharedPreferences: ninguna clave en esta área. Base: `rower_app.db` (real) / `rower_app_dev.db` (simulador), esquema v7.

---

## 5. Tests legacy y casos de aceptación

### 5.1 Tests directos (deben portarse 1 a 1)

| Test | Caso | Qué afirma | Feature |
|---|---|---|---|
| `test/routine_positions_test.dart` | las posiciones quedan alineadas con flattenedSteps | Con [calentamiento 300 s] + serie "a" ×3 [trabajo 500 m, enfriamiento 60 s] + [enfriamiento 300 s]: largo de posiciones = largo expandido = 8; pos[0] = (sin serie,1,1); pos[1] = pos[2] = (a,1,3); pos[3] = (a,2,3); pos[6] = (a,3,3); pos[7] = (sin serie,1,1). | F-RUT-06, F-RUT-07 |
| `test/session_stats_test.dart` | avgHeartRate y maxHeartRate ignoran los ceros | Pulsos [0,120,150,0,141] → promedio 137, máximo 150, tiene pulso. | F-RUT-18 |
| `test/session_stats_test.dart` | sin pulso quedan en 0 | Pulsos [0,0] → 0, 0, sin pulso. | F-RUT-18 |
| `test/session_stats_test.dart` | sin puntos quedan en 0 | Lista vacía → sin pulso. | F-RUT-18 |

### 5.2 Tests indirectos (usan estos modelos como datos; los porta su área)

`test/series_tracker_test.dart` (pasos de trabajo por distancia y enfriamiento por tiempo en serie ×5 con sus posiciones), `test/series_panels_test.dart` (trabajo 1 000 m en serie ×3), `test/step_target_tracker_test.dart` (trabajo con vatios/SPM/split y lecturas en vivo), `test/step_pill_test.dart` (trabajo 60 s), `test/workout_provider_hr_test.dart` y `test/scene/scene_view_test.dart` (lecturas en vivo). Todos dependen de que los atributos y valores por defecto de F-RUT-02 y F-RUT-15 sean idénticos.

### 5.3 Casos sin test legacy (el PO los fija como criterio de aceptación)

Los valores salen de leer el código; QA debe confirmarlos contra el legacy.

1. Expansión con progresión SPM: serie ×3 [trabajo 300 s, 18 SPM, prog +2; descanso 120 s] → 6 pasos; SPM de los trabajos = 18, 20, 22; los descansos sin cambios; el último paso es un descanso.
2. Progresión de vatios sobre rango: 140–160 W, prog +10, ×3 → 140–160, 150–170, 160–180.
3. Progresión de vatios sobre mínimo solo: 150 W, prog +10, ×3 → 150, 160, 170 (máximo sigue vacío).
4. Progresión de split: 120 s, prog −2, ×3 → 120, 118, 116.
5. Progresión sin objetivo: SPM vacío, prog +2 → SPM sigue vacío en las 3 repeticiones.
6. Paso suelto con progresiones (dato cargado a mano) → no se progresa.
7. Repeticiones vacías → ×1; repeticiones 0 → la serie aporta 0 pasos y 0 posiciones.
8. Dos series contiguas con identificadores distintos (×2 y ×3) → se expanden por separado, posiciones con su propio total.
9. Etiquetas de duración de la tabla de F-RUT-03 (0, 45, 60, 90, 120, 3600, 3725 s; 500 y 2000 m; sin meta; ambas metas), con el catálogo en es y en en (mismo resultado).
10. Etiquetas de objetivos de la tabla de F-RUT-04, incluida "solo vatios máx → vacío" y la línea completa con progresiones, en es y en en (mismo resultado).
11. Resúmenes de la tabla de F-RUT-05: columnas "App nueva, es" y "App nueva, en". La columna "Legacy" es la referencia del oráculo: confirma que las reglas de conteo no cambiaron.
12. P99: tabla de índices y ejemplos de F-RUT-18 (vatios → 200; split → 130; n=2 → el menor).
13. Totales de estadísticas = máximos de distancia, tiempo y calorías.
14. Redondeo del pulso promedio: [120, 121] → 121.
15. Formatos: split 0 → `--:--`, 125 → `02:05`, 6000 → `100:00`; duración 59 → `00:59`, 3600 → `1:00:00`, 3661 → `1:01:01`.
16. Editor: nombre "   " → `editorNameEmpty`; nombre válido sin pasos → `editorNoSteps`; nombre "  Mi rutina  " → se guarda "Mi rutina".
17. Editor: "Serie" agrega Trabajo 120 s + Descanso 60 s, ×3, al final.
18. Editor: repeticiones "abc" → ×1; "0" o "-2" (con guion) → sin cambio; "−2" (con el signo menos tipográfico, que `int.tryParse` no reconoce) → ×1; "5" → ×5 en todos los pasos de la serie.
19. Editor: agregar paso a una serie lo inserta después del último paso de esa serie, con sus repeticiones.
20. Diálogo: modo tiempo vacío → 0 s; modo distancia vacío → sin meta (`?`); split 0:00 → sin objetivo de split.
21. Lista: crear agrega arriba; editar no reordena; eliminar pide confirmación con el nombre.

---

## 6. Diferencias entre docs y código (manda el código)

1. **CLAUDE.md dice "navigation via go_router"**: la dependencia está declarada pero no se usa; la navegación es por pestañas fijas y pantallas apiladas (el editor se abre a pantalla completa sobre la barra inferior).
2. **CLAUDE.md dice "Schema has 4 tables with cascade deletes"**: el borrado en cascada está declarado pero nunca activado; al borrar una rutina sus pasos quedan huérfanos (sin efecto visible).
3. **CLAUDE.md / README describen objetivos "watt/SPM"**: el código también tiene objetivo de **split** y tres **progresiones**.
4. **README, ejemplo "Progressive SPM ladder"**: el resultado lista 5 tramos (sin descanso al final de la 3ª repetición); el código produce 6 (cada repetición incluye su descanso, también la última).
5. **Comentario del modelo "UUID del grupo"**: el identificador real es `g{milisegundos}_{contador}`.
6. **Comentario del modelo "uno de los dos debe estar definido" (duración o distancia)**: no se hace cumplir; el editor puede crear pasos sin meta.
7. **Campos `avg_power_watts` / `avg_stroke_rate` ("promedio")**: guardan el P99, no el promedio.
8. **Comentario "percentil 99"**: para el split da casi el más lento; con 2 valores da el menor.
9. **La app está localizada es/en**, pero el resumen de rutina, las etiquetas de duración y de objetivos están fijas en español/neutral (se ve "1 series · 0 descansos" en la UI en inglés, `docs/image-02.png`).
10. **Existe una traducción fija en español de los tipos de paso** en el modelo (sin uso); la UI usa las claves localizadas.
11. **README "Progression fields only appear when configuring steps inside a series"**: coincide; se agrega que, en datos viejos, un paso suelto con progresiones las conserva al editarlo pero no se aplican.

---

## 7. Código muerto o no alcanzable (no requiere paridad)

- Tarjeta de paso compartida (F-RUT-19), `interval_tile.dart`.
- Nombre fijo en español del tipo (`interval_step.dart:3-15`) y "es descanso" (`:17`).
- "Es por tiempo / por distancia" (`interval_step.dart:64-65`), sin uso fuera del modelo.
- Duración total estimada de la rutina (`routine.dart:24-25`), no se muestra.
- Agregar paso suelto "dentro de una serie" por la vía de "+ Paso" (`routine_editor_screen.dart:75-88`, parámetro nunca usado).

---

## 8. Preguntas abiertas (por defecto: se hace lo que hace el legacy)

1. **Localización de resumen y etiquetas** (F-RUT-03/04/05): **resuelta** (Q-01 + Q-02, QA-P-04). Pasan al catálogo en la It. 1 (§3.1). El resumen lleva singular correcto y traducción al inglés ("series" → *interval*). Las etiquetas quedan idénticas al legacy en los dos idiomas.
2. **Ambigüedad "m"** (minutos vs metros) en la etiqueta de duración: ¿se cambia a unidades explícitas? Por defecto: igual al legacy.
3. **Semántica P99**: ¿se mantiene "promedio" = P99 y el split P99 casi más lento? Por defecto: sí, idéntico (afecta historial y Strava).
4. **Validaciones del diálogo** (0 s, distancia vacía, mín > máx, negativos, progresiones que llevan objetivos a ≤ 0): ¿se agregan? Por defecto: no se bloquea nada; se recomienda validar como cambio de producto explícito.
5. **Historial fiel a la rutina remada** (F-RUT-20): ¿la sesión debe guardar una copia de la rutina expandida? Por defecto: igual al legacy (se recalcula con la rutina actual).
6. **Importación de datos del legacy**: ¿la app Unity debe importar rutinas y sesiones existentes de `rower_app.db`? Si sí, hay que soportar esquemas desde v1 (migraciones v2, v3, v7 del área).
7. **Limitaciones del editor** (no reordenar, no duplicar, borrado sin confirmar ni deshacer, salir sin aviso de cambios): ¿entran mejoras en la misma iteración del editor o después de la paridad? Por defecto: paridad primero.
8. **Doble toque en Guardar** crea duplicados: ¿se corrige? Recomendado: sí (corrección de defecto, no cambio de comportamiento deseado).
9. **Fecha de creación local sin zona** (orden de la lista si cambia la zona horaria): ¿se normaliza? Por defecto: mantener el orden "más nueva arriba".
10. **Máximo de repeticiones**: hoy no hay; ¿se fija uno?
11. **Emoji 📈** en la etiqueta de objetivos: ¿se mantiene o se reemplaza por un indicador visual propio? (Ver Mejora EXR de F-RUT-04.)

---

## Cambios por QA (plan)

Correcciones por el gate de armonía del plan ([QA_PLAN_REVIEW.md](../../QA_PLAN_REVIEW.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-04 | F-RUT-05: la tabla del resumen ahora tiene la columna legacy (oráculo) y las columnas es/en de la app nueva, con singular (Q-02) y "series" → *interval* en inglés. F-RUT-03 y F-RUT-04: en la app nueva salen del catálogo con el mismo texto en es y en. Nueva §3.1 con las 15 claves nuevas de la It. 1 (valores es/en, parámetros y cuándo se usa cada una). Casos 9-11 de §5.3 y pregunta abierta 1 (§8) actualizados. `workoutStartContent` con `¿` |

## Cambios al cerrar la It. 3

Pendientes del PO de la [spec de la It. 3 §15](../../iterations/03-rutinas/01-po-spec.md), releídos en el legacy (`routine_editor_screen.dart:173`, `:498-521`, `:531-562`), 2026-10-28.

| Dónde | Qué cambió |
|---|---|
| F-RUT-13, interpretación | Además de negativos y el signo "+", `int.tryParse` acepta ceros a la izquierda y hexadecimal; lo que tiene decimales, el signo menos tipográfico o dígitos de ancho completo cuenta como vacío (AC-RUT-13-05 de la It. 3) |
| F-RUT-13, precarga | La regla de Dart con duraciones negativas (resto nunca negativo: −90 s → "-1" y "30" → −30 s; −30 s → "0" y "30" → 30 s) y el paso sin meta, que se abre en modo tiempo y queda de 0 s (AC-RUT-13-10) |
| §5.3, caso 18 | Se distinguen `-2` con guion (sin cambio) y `−2` con el signo menos tipográfico (×1, porque no es un número para Dart) (AC-RUT-14-05) |
