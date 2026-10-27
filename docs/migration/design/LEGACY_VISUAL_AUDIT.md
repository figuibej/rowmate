# Auditoría visual del legacy y referencia EXR

> Autor: `rowmate-designer`. Fecha: 2026-10-27. Alcance: todo lo que se ve o se mueve en `rowmate-legacy` (Flutter), qué es contrato visual, qué hay que corregir y qué falta para llegar al nivel EXR.
>
> Fuentes leídas (solo lectura): `docs/image-01..06.png`, `lib/shared/theme.dart`, `lib/shared/widgets/*`, `lib/features/workout/{immersive_workout_screen,workout_screen,step_pill,series_panels}.dart`, `lib/features/workout/scene/**`, `shaders/water.frag`, `lib/features/device/{device_screen,heart_rate_card}.dart`, `lib/features/history/{history_screen,session_detail_screen}.dart`, `lib/features/routines/routine_editor_screen.dart`, `lib/core/dev/simulator_overlay.dart`, `lib/main.dart`, specs `2026-10-02-immersive-scene-2-5d-design.md` y `2026-10-02-step-timeline-states-design.md`, tests `test/scene/*`.
>
> **Regla de lectura:** cuando el spec y el código difieren, manda el **código** (es lo que validan los tests). Las diferencias están en la sección 8.
>
> Convención de color: `#RRGGBB` opaco; si hay transparencia se indica `@NN %` (alfa). Los `Colors.*` de Material se traducen a hex.

---

## 1. Resumen ejecutivo

- El legacy tiene una **semántica de color muy consistente** (7 colores por métrica, 4 por tipo de paso, 3 por fase) usada en dispositivo, HUD inmersivo, historial y gráficos. Es el activo visual más valioso: se conserva tal cual.
- La UI no inmersiva es **Material 3 por defecto** (seed `#0077B6`, modo oscuro) sobre fondo `#0D1B2A` y tarjetas `#1A2E45`. Funcional y legible, pero genérica: no tiene identidad de marca, ni tipografía propia, ni transiciones propias.
- El HUD inmersivo usa un lenguaje **"glass" (negro 55 %, radio 14, borde blanco 10 %)** con valores grandes en peso 800. El layout (SPM arriba a la izquierda, Split/Watts arriba a la derecha, tiempo+distancia abajo a la izquierda, kcal/BPM abajo a la derecha, controles abajo al centro, barra de etapas arriba) es correcto para un teléfono en el remo y se conserva.
- La escena es **2.5D pintada** (CustomPainter + 1 fragment shader): cámara de persecución fija, 4 entornos procedurales deterministas, paleta por hora real, remero articulado esquemático. Toda la **lógica** (cámara, palada, hora, generación de props, velocidad) es portable 1:1 a Core; el **render** hay que rehacerlo en 3D URP.
- Brechas principales contra EXR: geometría 3D real con orillas/terreno (en el legacy los edificios "flotan" en el agua), agua con reflejos reales, remero 3D con rig, remada biomecánica (ratio fijo 1:2, pala que "salta", sin feathering ni surge), cámaras múltiples, sonido, otros botes (fantasma/bots), mundo vivo (público, vegetación, LOD).

---

## 2. Inventario de pantallas (capturas)

| Captura | Pantalla | Qué se ve |
|---|---|---|
| `image-01` | Dispositivo conectado (horizontal, 1600×1080, simulador) | AppBar "Device" + acción "Disconnect" roja. Fila centrada: ícono remo + "Remo simulado" + chip verde "Connected". Grilla 3 columnas de tarjetas `#1A2E45` con borde superior de 3 px del color de la métrica: SPLIT 500m `02:13` (naranja), SPM `23.5` (cian), WATTS `149` (amarillo), DISTANCE `652m` (celeste). Tarjeta "HEART RATE SENSOR" a ancho completo con `143 bpm` rosa y "Disconnect". FAB llave inglesa (simulador). NavigationBar M3 de 5 destinos. Cinta "SIMULADOR" naranja arriba a la izquierda. |
| `image-02` | Entrenar (reposo) | "Scenery": 4 miniaturas 120×80 (Alpine lake, City river seleccionada con borde cian, Coast, Regatta course). Botón lleno ancho completo "Free workout" celeste claro (`#9ACBFA` medido). "Or pick a routine:" + tarjeta de rutina con ícono cian, título, resumen y chevron. |
| `image-03` | Entrenamiento inmersivo con rutina (río urbano, mediodía) | Barra superior glass con pills de pasos (rojo = trabajo), etiqueta "Work", cuenta regresiva `1:05` y hora `13:54`. HUD: SPM `24.1` grande; panel "Parciales 500 m" expandido (`0:55.0 207 m`, `Rep 2/3 · 207 m`); "Series anteriores" (`Rep 1 2:13 452 m`); Split `02:13`; Watts `149 W`; kcal `89`; BPM `142`; banda `02:55 │ 1484 m`. Botones "Terminar" (contorno rojo) y "Pausa". Escena: horizonte al 42 %, skyline gris, sol con halo, nubes de manchas, edificios y boathouses en las orillas, faroles, boyas amarillas, bote blanco con borde cian, remero de frente con remos y palas cian. |
| `image-04` | Editor de rutina | Campos outlined (nombre, descripción). "Steps (1)" + acciones "+ Step" / "Series". Bloque de serie: encabezado azul con "Series" + chip `×3`, paso "Work 2m" con borde izquierdo rosa, pie "+ Add step to series". |
| `image-05` | Detalle de sesión | Tarjeta "Summary" (6 stats coloreados por métrica). "Step Performance": filas con índice en chip cian. Selector segmentado de 5 métricas (seleccionado `#0077B6`). Gráfico de área 220 px (HR rosa) con línea punteada de cambio de paso. |
| `image-06` | Historial | Lista de tarjetas: ícono remo cian, nombre, fecha `dd/MM/yyyy  HH:mm` gris, chevron; fila de 6 stats con valor coloreado y etiqueta gris. |

Navegación: `NavigationBar` M3 con `IndexedStack` (Device, Workout, Routines, History y Profile solo si Strava está configurado). Cambiar de pestaña no tiene transición. El entrenamiento inmersivo se abre con `MaterialPageRoute` (transición por defecto de la plataforma) y modo `immersiveSticky`.

---

## 3. Color

### 3.1 Base de la app

| Token propuesto | Hex | Uso en el legacy |
|---|---|---|
| `bg.app` | `#0D1B2A` | `scaffoldBackgroundColor`, AppBar, selector segmentado no seleccionado |
| `bg.card` | `#1A2E45` | `cardTheme`, tarjetas de métricas, editor de pasos |
| `bg.cardRaised` | `#23364C` (medido) | filas internas de tarjetas (`white @4 %` sobre `#1A2E45`) |
| `bg.navLegacyUnused` | `#12243A` | `bottomNavigationBarTheme` (**no se aplica**: se usa `NavigationBar` M3) |
| `nav.bg` (real) | `#1C2024` (medido) | fondo de `NavigationBar` (surfaceContainer M3) |
| `nav.indicator` (real) | `#394857` (medido) | pastilla del destino seleccionado (secondaryContainer M3) |
| `brand.seed` | `#0077B6` | seed M3 "azul agua"; segmento seleccionado del gráfico; encabezado/pie de serie del editor (`@12 %` / `@6 %`, borde izquierdo 3 px) |
| `brand.primaryM3` | `#9ACBFA` (medido) | `FilledButton` ("Free workout"), primary M3 derivado del seed |
| `accent.cyan` | `#00B4D8` | acento general: íconos de remo/rutina, borde de miniatura seleccionada, chip `×N`, índices de pasos, íconos del diálogo de fin. Es el mismo valor que `metric.spm`. |
| `text.primary` | `#FFFFFF` | |
| `text.secondary` | `#FFFFFF @70 %` / `@60 %` / `@54 %` | subtítulos, etiquetas de filas |
| `text.tertiary` | `#FFFFFF @38 %` | fechas, etiquetas de stats, ejes del gráfico |
| `divider` | `#FFFFFF @12 %` / `@10 %` | separadores, grillas |

### 3.2 Métricas (`MetricColors`) — **contrato**

| Métrica | Hex | Nombre en el código |
|---|---|---|
| Split /500 m | `#FF9F1C` | naranja ámbar |
| SPM | `#00B4D8` | cian |
| Watts | `#FFD60A` | amarillo eléctrico |
| Distancia | `#4CC9F0` | celeste |
| Frecuencia cardíaca | `#EF476F` | rosa/rojo |
| Tiempo | `#E0E0E0` | gris claro |
| Calorías | `#06D6A0` | verde menta |

Se usan en: grilla de Dispositivo, tarjeta del pulsómetro, HUD inmersivo, paneles de series (parciales en naranja, reps/metros en celeste), resumen e historial, color de línea/área del gráfico.

### 3.3 Tipos de paso (`stepColor`) — **contrato**

| Tipo | Hex |
|---|---|
| Trabajo (`work`) | `#EF476F` |
| Descanso (`rest`) | `#06D6A0` |
| Calentamiento (`warmup`) | `#FFD166` |
| Enfriamiento (`cooldown`) | `#118AB2` |
| Desconocido | `#607D8B` |

Íconos por tipo (editor): calentamiento `whatshot`, trabajo `fitness_center`, descanso `pause_circle_outline`, enfriamiento `ac_unit`.

### 3.4 Estados y feedback

| Estado | Hex | Dónde |
|---|---|---|
| Fase EN MARCHA | `#06D6A0` | chip de fase (bg `@15 %`, borde `@50 %`) |
| Fase PAUSADO | `#FFD166` | idem |
| Fase FINALIZADO | `#EF476F` | idem |
| Objetivo no cumplido | `#FF4444` | valor de SPM/Watts/Split con objetivo (si cumple: blanco) |
| Reanudar | `#06D6A0` con texto negro | botón lleno |
| Terminar / desconectar / error | `#FF5252` (`redAccent`), `#E57373` (`red.shade300`), `#D32F2F` (`red.shade700`, borrar rutina) | |
| Conectado | `#69F0AE` (`greenAccent`) sobre `#4CAF50 @20 %` | chip "Connected", "RECEIVING" |
| Rower no conectado | `#FF9800` (`orange`) sobre `@15 %` | aviso en Entrenar |
| Strava | `#FC4C02` | botones de Strava, ícono `cloud_done` |
| Simulador | `#FF5722` (`deepOrange`) | cinta "SIMULADOR" |
| Debug BLE | `#18FFFF` (`cyanAccent`) monoespaciada; panel `#000000 @38 %` | |
| Completado | `#06D6A0` | ícono `check_circle` del diálogo |

### 3.5 Glass / overlays del HUD

| Elemento | Fondo | Borde | Radio |
|---|---|---|---|
| Tarjeta de métrica, paneles de series, reloj enmarcado | `#000000 @55 %` | `#FFFFFF @10 %` 1 px | 14 |
| Tarjeta con objetivo activo | `#000000 @55 %` | color de la métrica `@70 %` 1,5 px + glow color `@20 %` blur 12 | 14 |
| Barra de etapas | `#000000 @55 %` | `#FFFFFF @8 %` | 14 |
| Barra de libre | `#000000 @50 %` | `#FFFFFF @8 %` | 14 |
| Banda tiempo + distancia | `#000000 @50 %` | `#FFFFFF @10 %` | 40 (píldora) |
| Botones Terminar/Pausa | `#000000 @45 %` | `#FF5252` 1,2 px / `#FFFFFF @24 %` 1,2 px | 12 |

Sin blur de fondo (no es glass real, es negro translúcido). Medido sobre la escena: `≈#1C2E4D`.

### 3.6 Escena: bote, remero y cielo

| Elemento | Hex |
|---|---|
| Casco (degradé arriba→abajo) | `#F5F5F5` → `#B0BEC5` |
| Borde del casco y palas | `#00B4D8` |
| Hueco del remero | `#263238` |
| Traje | `#1565C0` |
| Piel | `#FFCC80` |
| Pelo | `#3E2723` |
| Carro y pies | `#0D3B6E` |
| Caña del remo | `#BCAAA4` |
| Rigger | `#90A4AE` |
| Sombra del bote | `#000000 @(8 % × ambient)` |
| Charcos / salpicadura | blanco `@30 %→0` / `@80 %→0` |

Todo lo de la escena se tiñe por `ambient`: `rgb × ambient`, con el azul `× min(1, ambient + 0.12)` (las noches quedan azuladas). Los props además se mezclan con la niebla (sección 7.5). Paletas por hora en 7.3; colores de props en 7.6.

---

## 4. Tipografía

- **Familia:** ninguna propia (`pubspec` no declara fuentes). Se ve la del sistema: Roboto (Android), SF Pro (iOS/macOS), Segoe UI (Windows, capturas). Consecuencia: el producto se ve distinto en cada plataforma.
- **Cifras tabulares:** solo en paneles de series y reloj (`FontFeature.tabularFigures`). En el resto los números "bailan" al cambiar de ancho.
- **Pesos:** valores 800 (HUD, dispositivo), 700 (títulos de sección, chips), 600 (etiquetas con tracking 0,8–1,0).

| Componente | Valor | Etiqueta | Unidad |
|---|---|---|---|
| Glass grande (SPM) | 52 / w800 / alto 1,0 | 13 / w600 / tracking 0,8 / color `@80 %` | 13 / w500 / `@55 %` |
| Glass mediano (Split, Watts) | 36 / w800 | 12 | 12 |
| Glass chico (kcal, BPM) | 28 / w800 | 11 | 11 |
| Banda tiempo/distancia | 28 / w800 | ícono 16 `@70 %` | 13 / w500 |
| Barra de etapas: tipo de paso | 13 / w700 / tracking 0,5 | | |
| Barra de etapas: objetivo | 11 / w600 en chip `@20 %` radio 6 | | |
| Cuenta regresiva | 18 / w800 (20 en los últimos 5 s) | | |
| "Up next" | 10 (texto) / 10 w700 (tipo) | | |
| Reloj de pared | 18 / w800 tabular, ícono 16 | | |
| Libre: título / chip de fase | 16 w600 / 11 w800 tracking 0,8 | | |
| Parciales: vuelta en curso | 30 / w800 / alto 1,1 tabular | 13 w600 | 14 w500 |
| Filas de parciales y reps | 22 w800 tabular; rótulo 15 w600 `@60 %`; secundario 16 w800 celeste | | |
| Dispositivo: tarjeta de métrica | 52 / w800 (FittedBox) | 12 / w600 / tracking 0,8 / `@70 %`, ícono 14 | |
| Pulsómetro | 52 / w800 | título 12 en mayúsculas | "bpm" 14 `@70 %` |
| Historial / resumen | stat 14 bold | 10 `@38 %` | |
| Historial: título / fecha | 15 w600 / 11 `@38 %` | | |
| Detalle: título de sección | 16 w700 | | |
| Detalle: filas de paso | 12 w600 (stats), 11 `@54 %` (duración · metros) | | |
| Gráfico: ejes / tooltip / segmentos | 9 `@38 %` / 12 w600 / 11 | | |
| Controles inmersivos | 15 | | |

---

## 5. Componentes y estados

### 5.1 HUD inmersivo (`immersive_workout_screen.dart`)

**Layout** (márgenes laterales 14, sobre `SafeArea`):

| Zona | Contenido | Posición |
|---|---|---|
| Arriba | Barra de etapas (rutina) o barra de libre | `top = padding.top + 8`, margen horizontal 12 |
| Arriba izquierda | SPM (grande) + paneles de series debajo (scroll propio) | `top = padding.top + 90`; la columna termina 8 px arriba de la banda (vertical: `bottom 200`; horizontal: `bottom = padding.bottom + 90`) |
| Arriba derecha | Split /500 m y Watts apilados (separación 8) | `top = padding.top + 90` |
| Abajo izquierda | Banda tiempo │ distancia | vertical: `bottom 140`; horizontal: `bottom = padding.bottom + 16` |
| Abajo derecha | kcal y BPM (BPM solo si `heartRate > 0`) | `bottom 140` |
| Abajo centro | Terminar (120×48) · 16 · Pausa/Reanudar (140×48) | `bottom = padding.bottom + 16` |
| Centro | El bote, siempre visible: nada del HUD lo tapa | |

**`_GlassMetricCard`** — estados: normal (color de la métrica) / con objetivo (valor y etiqueta en blanco si cumple o `#FF4444` si no, borde del color `@70 %` 1,5 px y glow). Ancho mínimo 130 / 110 / 90. Padding 14×10.

**Barra de etapas (`_StageTimelineBar`)** — fila de `StepPill` (alto 8, radio 4, separación 3, mismo ancho para cada paso sin importar su duración) + fila de info: tipo de paso, chip de objetivo (recortable con "…"), chip "Up next" (cuando faltan ≤ 15 s y hay siguiente), cuenta regresiva (`m:ss`, o `N m` si el paso es por distancia), reloj.

**`StepPill`** — 4 estados (contrato del spec de estados):

| Estado | Pintado |
|---|---|
| Pendiente | color del paso `@18 %` |
| En curso | pista `@30 %` + relleno 100 % proporcional al avance |
| Hecho (cumplido o sin objetivo) | sólido `@85 %` |
| Hecho, objetivo no cumplido | hueco: interior `@18 %` + borde 1,5 px `@85 %` |
| Últimos 5 s del paso en curso | glow del color, alfa 0,3→0,7, blur 8, spread 2 |

**Barra de libre** — ícono remo cian 20, "Entrenamiento Libre" 16 w600, reloj, chip de fase.

**Paneles de series (`SeriesPanels`)** — colapsados por defecto, estado persistido (`immersive.lapsExpanded`, `immersive.repsExpanded`). Colapsado: ícono + "500 m"/"Reps" + `expand_more`. Expandido: ancho fijo 200 (no invade la columna derecha en 360 dp), título + `expand_less`, contenido. Parciales: vuelta en curso grande + últimas 4 vueltas (más reciente primero) + "Rep x/y · N m". Fuera de un paso de trabajo: "Sin parciales en este paso". Reps: "Rep N · split · metros" o "Sin repeticiones completas". Expandir/colapsar **no** está animado.

**Controles** — en marcha: Terminar (contorno rojo) + Pausa (contorno blanco 24 %); en pausa: Reanudar lleno `#06D6A0`. Terminar pide confirmación (AlertDialog M3).

**Diálogo de fin** — `check_circle` `#06D6A0` 28, título, mensaje, caja `white @4 %` radio 8 con Tiempo / Distancia / Calorías (íconos cian 20). Variantes de botones según preferencia de Strava (auto / preguntar / manual).

### 5.2 Dispositivo

- **Conectado:** nombre (titleLarge) + chip verde. Grilla 3 columnas si ancho > 600 (aspecto 1,6) o 2 columnas (1,3), separación 12. Tarjeta: `#1A2E45`, radio 16, borde superior 3 px color `@60 %`, ícono + etiqueta arriba centrados, valor centrado. Debug BLE oculto: aparece "Show BLE debug" tras 3 scrolls hacia abajo.
- **Buscando / sin resultados / con resultados:** botón lleno "Buscar" con spinner 16; lista de tarjetas (ícono remo cian, nombre, MAC 11 `@38 %`, chevron o spinner al conectar); vacío con ícono 48 `@24 %`.
- **Bluetooth apagado / sin permiso:** ícono 64 `#FF5252`, título 18 bold, hint 14 `@54 %`, botón "Activar".
- **Error:** caja `red @15 %` radio 12 con texto `#FF5252` 13.

**Pulsómetro (`HeartRateCard`)** — misma tarjeta con borde superior rosa. Estados: sin sensor (texto + hint + botón tonal "Buscar"), buscando (spinner + lista), conectando (spinner + "Conectando a …"), conectado (nombre `@70 %` + `143 bpm` + "Desconectar"), recordado (`nombre · no encontrado/reconectando…` + Conectar / Buscar otro / Olvidar), error (texto rojo 12, hasta 3 líneas). Corazón `@40 %` si no está conectado.

### 5.3 Entrenar (reposo)

Aviso naranja si no hay remo; "Escenario" + `EnvironmentPicker` (alto 112, miniatura 120×80 radio 12, borde cian 2,5 si está elegida o blanco 12 % 1 px; nombre 12, w700 blanco elegido / w400 `@70 %`); botón "Entrenamiento libre"; lista de rutinas (opacidad 0,5 y deshabilitadas sin remo); confirmación al elegir rutina.

### 5.4 Rutinas y editor

- Lista: tarjeta M3 con ícono cian, nombre, resumen 12, descripción 11 `@54 %`; FAB "+"; borrar con confirmación (`#D32F2F`).
- Editor: paso = contenedor `#1A2E45` radio 10, borde izquierdo 3 px del color del paso, chip 28×28 radio 6 `@20 %` con ícono 14, título 13 w600 color del paso, subtítulo 11 (`duración · objetivo`), editar / borrar (`#E57373`). Serie: encabezado `#0077B6 @12 %` radio superior 12 + borde izquierdo 3 px `#0077B6`, "Series" 13 w700 cian, chip `×N` 14 w800 cian sobre `@20 %` radio 12, papelera; pasos de la serie con sangría 16; pie `@6 %` con "+ Agregar paso a la serie".
- `IntervalTile` (widget compartido): radio 12, borde izquierdo 4, chip 36×36 radio 8 con número, activo `@15 %`.

### 5.5 Historial y detalle

- Tarjeta de sesión: padding 16, ícono remo cian 18, nombre 15 w600, `cloud_done` Strava 14, fecha, chevron 16 `@24 %`; 6 columnas (Tiempo, Distancia, Watts, SPM, Split, FC `prom/máx`) con FittedBox para 360 dp. Split vacío se muestra `--:--`.
- Detalle: Resumen (igual que la tarjeta), "Rendimiento por paso" (chip índice 28×28 radio 6 cian `@16 %`, `m:ss · N m`, `W · spm · m:ss/500m`), selector segmentado (Watts, SPM, Split, Distancia, FC), gráfico.
- **Gráfico (fl_chart):** alto 220; línea curva (suavizado 0,2, 2 px) del color de la métrica con área `@12 %`; franjas verticales por paso con `stepColor @10 %`; líneas de objetivo por paso blancas `@78 %` 2 px punteadas `[8,5]` con área `@7 %`; límites entre pasos `@24 %` 1 px punteados `[4,4]`; grilla horizontal `@10 %` 0,5 px; ejes 9 `@38 %`; intervalo X 30 s (< 5 min), 60 s (< 10), 120 s (< 30), 300 s. **Split invertido** (más rápido arriba) y etiquetas en `m:ss`. Tooltip `valor unidad`. Sin animación de entrada propia (la de fl_chart por defecto).

### 5.6 Perfil (Strava) y simulador

- Perfil: tarjetas M3 (conexión, preferencia de subida, sincronización, pendientes, debug), botones `#FC4C02`.
- Simulador: cinta "SIMULADOR" `#FF5722` arriba a la izquierda, FAB chico (llave), panel tarjeta 280 de ancho con presets Suave/Medio/Fuerte, watts ±10, ritmo ±1, **"Hora escena" ±1 h** (override de la hora de la escena), switch "Remando", simular desconexión.

### 5.7 Radios y espaciados usados

Radios: 4 (pill), 6 (chips chicos), 8 (chips, cajas internas), 10 (paso del editor), 12 (botones, avisos, miniaturas), 14 (glass), 16 (tarjetas), 40 (píldora de banda). Paddings habituales: 16 (pantalla, tarjetas), 14×10 (glass), 12 (avisos). Separaciones: 3, 4, 6, 8, 10, 12, 16, 24.

---

## 6. Animaciones existentes

### 6.1 UI

| Animación | Duración | Curva | Detalle |
|---|---|---|---|
| Pulso de la barra de etapas | 600 ms, `repeat(reverse)` | lineal | Un solo controller: glow del pill en curso (alfa 0,3→0,7) en los últimos 5 s; opacidad del chip "Up next" 0,6→1,0; color de la cuenta regresiva blanco→color del paso. |
| Tamaño de la cuenta regresiva | instantáneo | — | 18→20 px al entrar en los últimos 5 s (salto). |
| Relleno del pill en curso | sin animación | — | Se actualiza 1 vez por segundo con el tick del provider: avanza a saltos. |
| Valores de métricas | sin animación | — | Saltan al llegar cada notificación BLE (~1 Hz). |
| Expandir/colapsar paneles de series | instantáneo | — | Cambio de ancho y contenido sin transición. |
| Reloj de pared | — | — | Redibuja al cambiar el minuto. |
| Navegación por pestañas | ninguna | — | `IndexedStack`. |
| Push de pantalla inmersiva / detalle | por defecto de la plataforma | Material | `MaterialPageRoute`. |
| Diálogos, snackbars, NavigationBar, SegmentedButton | por defecto M3 | Material | |
| `_CompactStepBanner` (código muerto) | 500 ms reverse | lineal | No se usa (ver 8). |

### 6.2 Escena (Ticker a la tasa de refresco, `dt` acotado a 0,1 s)

| Elemento | Parámetros |
|---|---|
| Velocidad del bote | objetivo `500 / split` m/s (0 si no está activo o SPM = 0); filtro de primer orden `speed += (target − speed) · min(1, dt / 1,5)`; se corta a 0 bajo 0,01 m/s. |
| Fase de palada | avanza `spm / 60` ciclos/s; sin paladas termina el ciclo a 12 spm y queda en el catch; en pausa se congela. |
| Cabeceo | `0,02 · sin(2π·fase) + 0,006 · sin(0,8·t)` rad. |
| Salpicadura del catch | mientras `fase < 0,08` (≈ 0,2 s a 24 spm): 6 gotas en abanico (−90° ± (i − 2,5)·0,35 rad), se abren 0,15→0,5 m, suben `sin(π·t)·0,25 m`, radio 0,05→0,03 m, alfa 0,8→0. |
| Charcos | 2 por palada al soltar la pala, vida 4 s, anillo elíptico (alto = 0,35 × ancho), radio 0,3→0,9 m, trazo 0,06 m, alfa 0,3→0; quedan fijos en el mundo y se alejan. |
| Estrellas | 80, semilla 42, en el 85 % superior del cielo, radio 0,6–1,6 px, titileo `0,5 + 0,5·sin(2t + i)`, alfa `(0,4 + 0,5·titileo) · oscuridad`, `oscuridad = (1 − ambient) / 0,65`. |
| Nubes | 7, deriva `x = (fx + distancia·0,0004·v + t·0,004·v) mod 1,2`. |
| Gaviotas (costa) | 5, cruzan a `8 + 3i` px/s, aleteo `sin(6t + i)·3 px`, vaivén `sin(2t + i)·4 px`. |
| Banderas | orilla `sin(4t + seed)·0,2 m`; horizonte `sin(3t + i)·2 px`. |
| Agua | ondas por tiempo (0,6 y 0,9 rad/s), ruido que deriva (+0,15 y −0,25), destellos (0,8). |

---

## 7. Escena 2.5D

### 7.1 Cámara (`SceneCamera`)

- Mundo en metros: `x` lateral (+ derecha), `y` altura, `z` hacia adelante desde la cámara.
- `camHeight = 2,2 m`, remero en `boatZ = 9,5 m`, recorte `minZ = 0,5 m`.
- Horizonte en `0,40·h` (vertical) o `0,42·h` (horizontal). `focal = lado corto` (px).
- Proyección: `sx = w/2 + focal·x/z`, `sy = horizonY + focal·(camHeight − y)/z`; escala `focal/z` px/m. Inversa sobre el agua: `z = focal·camHeight/(sy − horizonY)`.
- **Es una cámara nivelada con desplazamiento de lente** (no está inclinada): las verticales quedan verticales y el horizonte queda arriba del centro.
- Valores derivados para replicar en Unity (cámara física, `lensShift.y` negativo = el encuadre baja y el horizonte sube en pantalla; validar el signo en el Editor):

| Orientación | FOV vertical sin shift | FOV horizontal | `lensShift.y` | Ángulo al remero |
|---|---|---|---|---|
| Vertical (390×844) | ≈ 94,5° | ≈ 53° | −0,10 | 13° bajo el horizonte |
| Horizontal (1600×1080) | ≈ 53,1° | ≈ 73° | −0,08 | 13° |

- Una sola cámara, fija, sin movimiento propio (no sigue el cabeceo ni la palada).

### 7.2 Estado de la escena (`SceneState`)

`speedTau 1,5 s`, `puddleLife 4 s`, `oarLength 2,9 m`, `riggerX 0,85 m`, `idleSpm 12`. `shaderDistance = distancia mod 4096` (precisión float). Distancia de mundo = integral de la velocidad filtrada (no la del monitor directamente).

### 7.3 Hora del día (`TimeOfDay`)

- `elevación = sin((h − 6)/12 · π)`; **noche** si `elevación ≤ −0,4` (≈ 19:35 a 4:25).
- Sol/luna en pantalla: `x = w · clamp(0,25 + 0,5·(h' − 6)/12, 0,15, 0,85)`, `y = horizonY − clamp(elev', 0, 1) · horizonY · 0,9`, con `h' = h` de día y `h + 12` de noche (la luna recorre el cielo como un sol espejado).
- Fuerza del brillo: noche 0,45; día `clamp(elev·1,2, 0,2, 1)`.
- Paleta por keyframes con interpolación lineal: 5 h amanecer → 7 mañana → 12 mediodía → 18 atardecer → 20 anochecer → 22 noche → (4 h) noche → 5 amanecer.

| Keyframe | skyTop | skyHorizon | waterNear | waterFar | sunColor | sunGlow | fog | ambient |
|---|---|---|---|---|---|---|---|---|
| Amanecer 5 h | `#2A1B5E` | `#FF9A5C` | `#1A2B4A` | `#3A2F55` | `#FFB070` | `#FF7A3D` | `#C98A7A` | 0,70 |
| Mañana 7 h | `#3A7BD5` | `#FFD9A0` | `#1E5A9E` | `#5C8FC9` | `#FFE4A0` | `#FFC46B` | `#DCE8F5` | 0,95 |
| Mediodía 12 h | `#0B4FA8` | `#8FD3FF` | `#0E4F8F` | `#2F8FD6` | `#FFF6D5` | `#FFE9A6` | `#CFE9FF` | 1,00 |
| Atardecer 18 h | `#2E3C8C` | `#FF8C5A` | `#163A66` | `#7C5C7A` | `#FFB36B` | `#FF6B3D` | `#E0A28A` | 0,85 |
| Anochecer 20 h | `#0D1A4A` | `#4A3A7A` | `#0B1E3A` | `#2A2A55` | `#DDE6FF` | `#8FA3FF` | `#30335A` | 0,50 |
| Noche 22–4 h | `#050A1E` | `#121C3A` | `#06122A` | `#0E1E3C` | `#E6EEFF` | `#A6B8FF` | `#0F1730` | 0,35 |

Umbral "noche" para props (ventanas, faroles, faro): `ambient < 0,6`.

### 7.4 Cielo (`SkyPainter`)

Degradé `skyTop → skyHorizon` hasta el horizonte; estrellas (6.2); sol: disco radio 22 px + halo radial radio 88 px (`sunGlow` alfa 0,7 / 0,25 / 0); luna: disco 17,6 px **sin fase ni sombreado**; 7 nubes de 4 manchas radiales (opacas hasta el 55 % del radio) + sombra inferior; cuerpo `lerp(blanco, skyHorizon, 0,25)` con alfa `0,08 + 0,5·ambient²`, sombra `skyTop @(12 %·ambient)`. Datos de nubes `(fx, fy, px, v)`: (0,08, 0,10, 34, 1,0), (0,26, 0,18, 22, 1,4), (0,42, 0,07, 40, 0,8), (0,58, 0,22, 26, 1,2), (0,72, 0,12, 36, 0,9), (0,88, 0,20, 24, 1,3), (0,97, 0,05, 30, 1,1); `y = horizonY·(0,12 + 0,6·fy)`. Después se pinta la silueta lejana del entorno.

### 7.5 Entornos

**Común:** segmentos de 50 m; props deterministas con `Random(id·1000003 + k·7919 + 17)` (mismo paisaje a la misma distancia en cada sesión; contrato testeado); orilla en `|x| ≥ 9` (río de 18 m); visibles de `distancia + 1` a `distancia + 400 m`, pintados de lejos a cerca; tope por frame (primero se descartan las boyas lejanas). Tinte de props: ambient + niebla `f = clamp((z − 80)/320, 0, 1) · 0,8`. Silueta lejana: `horizonColor = lerp(tono, fog, 0,35 + 0,4·profundidad)`.

| Entorno | Ondas (amp / escala m) | Tinte agua | Tope props | Silueta lejana | Orilla (frecuencias) |
|---|---|---|---|---|---|
| **Lago alpino** (por defecto) | 0,25 / 2,5 | — | 120 | 2 cordones de montañas: lejano `#5C6E8A` (prof. 1, alto 0,12h, parallax 0,004, nieve `#F2F6FA` en picos > 75 %) y cercano `#2F4A3A` (prof. 0,3, alto 0,084h, parallax 0,008) | Pinos cada 6–12 m en ambas orillas (`x = ±10..16`, escala 0,7–1,3); muelle cada 400 m (izq.); cabaña cada 600 m (der., `x = 13`) |
| **Río urbano** | 0,35 / 2,0 | — | 120 | Skyline `#3A4660` en bloques de 34 px (alto 0,3–1·0,12h), ventanas `#FFE08A @85 %` de noche, parallax 0,004 | Edificios (70 %) o boathouses (30 %) cada 15–30 m en `x = ±13..17`, escala 0,6–1,0; faroles cada 25 m en `±9,5`; boyas en `±3` cada 50 m; puente de piedra cada 800 m |
| **Costa** | 0,80 / 4,0 | `lerp(base, #0E6B7A, 0,3)` | 60 | Acantilado `#4A3F3A` en el 35 % izquierdo (alto 0,08h) que se desvanece al centro; gaviotas `#2B2B2B` | Acantilados en `x = −14` cada 25 m (escala 0,8–1,3); playa con 3 sombrillas cada 250 m; faro cada 1000 m; a la derecha mar abierto |
| **Canal de regata** | 0,30 / 1,5 | — | 200 | Franja de tierra `#55704A`, tribunas `#6E7B8C`, torre `#B8C0CC`, 4 banderas, repetidos cada media pantalla | Boyas cada 10 m en `x = −9, −3, 3, 9` (color cambia cada 250 m: rojo/amarillo/blanco); cartel de distancia cada 250 m en `x = −10,5` con "N m"; tribuna (der., `x = 12`) y torre de llegada (izq., `x = −12`) cada 1000 m; bandera cada 50 m alternando `±10,5` |

### 7.6 Catálogo de props (medidas en m, colores antes del tinte)

| Prop | Geometría | Colores |
|---|---|---|
| Pino | tronco 0,3×1; 3 pisos triangulares de 2,2 de alto, semiancho 1,3/1,0/0,7 | tronco `#5D4037`; 3 paletas de verdes: (`#1B5E20`,`#2E7D32`,`#388E3C`), (`#145A32`,`#1E8449`,`#27AE60`), (`#2E5E2A`,`#3E7B38`,`#4F9A48`) |
| Cabaña | 4×2,6, techo a 4,2, puerta 0,8×1,4, ventana 0,8×0,7 | `#6D4C41`, techo `#4E342E`, puerta `#3E2723`, ventana `#90CAF9` / noche `#FFE082` |
| Muelle | tablero 3×0,4 hacia el agua, 3 postes 0,2×0,7 | `#8D6E63`, postes `#5D4037` |
| Edificio | 6 de ancho, 3–6 pisos de 3 m, 3 ventanas 1×1,2 por piso | `#78909C` / `#8D6E63` / `#B0BEC5`; ventanas `#263238`, encendidas (60 % de noche) `#FFE082` |
| Boathouse | 8×4, techo a 6, portón 3×2,8, franja 7,2×0,4 | `#B0BEC5`, techo `#546E7A`, portón `#37474F`, franja `#1565C0` |
| Puente | cara con arco de 19 m, pilares 1×1,5, tablero 24×1,2 a 6–7,2, parapeto 0,5 | `#9E9E9E`, pilares `#757575`, tablero `#7A7A7A`, parapeto `#616161` |
| Boya | esfera radio 0,25 + reflejo | `#E53935` / `#FDD835` / `#ECEFF1`; reflejo blanco `@25 %` |
| Farol | poste 0,12×4, cabeza radio 0,3 | `#455A64`, cabeza `#90A4AE`; noche núcleo `#FFF8E1` + halo `#FFE082 @35 %` radio 1,5 |
| Acantilado | 8 de alto, 7 de fondo, borde superior irregular, cueva 0,6×1,2 | `#5D4A42`, pasto `#556B2F`, cueva `#3E2F2A` |
| Faro | torre 2,2×9 con 3 bandas, linterna 1,6×1,2 | `#ECEFF1`, bandas `#D32F2F`, linterna `#37474F`; noche halo `#FFF59D @35 %` radio 3 |
| Sombrilla | palo 2,2, copa radio 0,9, toalla 1,2×0,1 | `#F06292` / `#4FC3F7` / `#FFD54F`, palo `#5D4037`, toalla `#FFF3E0` |
| Tribuna | 12 de ancho, 4 gradas de 1,2, techo 13×0,5 | `#8D9DAE` / `#6E7B8C` alternadas, techo `#37474F` |
| Torre de llegada | 3×10, banda 1,2, reloj radio 0,6, mástil 1,5 | `#CFD8DC`, banda `#263238`, reloj blanco con agujas negras, mástil `#90A4AE` |
| Bandera | mástil 5, paño triangular 1,6 | `#E53935` / `#FFB300` / `#1E88E5` / `#43A047`, mástil `#9E9E9E` |
| Cartel de distancia | poste 1,5, cartel 1,8×1 con "N m" (texto 0,55 m, se oculta < 4 px) | `#9E9E9E`, cartel `#FAFAFA`, texto negro 87 % w700 |

### 7.7 Bote y remero (`BoatPainter`)

- **Bote (single):** largo 8,2 m (popa `boatZ − 4,2`, proa `boatZ + 4,0`), flota a `y = 0,08`, semiancho máximo 0,28 m; contorno `(±0,14 popa) (±0,28 a −1,5) (±0,26 a +0,5) (±0,12 a +2,5) (0 proa)`; hueco 0,34×1,7; rotado por el cabeceo.
- **Remos:** rigger a `±0,85` (barra desde `±0,3`, alto 0,12→0,15); largo 2,9 m, interior 0,9 m; mango a alto fijo 0,45; pala en el agua a `y = −0,05` como rectángulo 0,25×0,5, fuera del agua a `y = +0,35` como óvalo plano 0,5×0,12 (cambio **instantáneo**).
- **Remero (de frente a la cámara: mira a la popa):** pies en `boatZ − 1,15`, cadera a 0,25, carro `boatZ − 0,6·(1 − piernas)` (recorrido 0,6 m), rodilla sube hasta 0,65; tronco 0,62 m (exagerado para legibilidad), hombros 0,54 de ancho, cadera 0,38; cabeza radio 0,14 a +0,27 sobre los hombros con pelo en la mitad superior; brazos 0,09 de grosor hasta el mango; muslos 0,16 (traje), pantorrillas 0,12 (piel); pies círculo 0,07.
- **Sombra:** óvalo plano 0,5 m de ancho del lado opuesto al sol (`x = ±0,35`), sin blur.

### 7.8 Palada (`StrokeCycle`)

Fase `p ∈ [0,1)`, ratio **fijo** drive:recuperación = 1:2 (drive 0–0,40 incluido el finish), todo con `smoothstep`.

| Segmento | Drive | Recuperación |
|---|---|---|
| Piernas (0 plegadas → 1 extendidas) | 0,00–0,18 | 0,60–1,00 |
| Espalda (+25° adelante → −20° atrás) | 0,10–0,30 | 0,50–0,75 |
| Brazos (0 estirados → 1 al pecho) | 0,22–0,36 | 0,40–0,60 |
| Remo (catch −55° → finish +35°) | 0,00–0,36 | 0,40–1,00 (vuelta) |
| Finish (remo quieto) | 0,36–0,40 | |
| Pala en el agua | 0,00–0,38 | |

Secuencia piernas→espalda→brazos y su inversa: correcta. Tests del legacy: `p=0` piernas plegadas/brazos estirados/pala adentro; `p=0,36` piernas extendidas/brazos flexionados; piernas antes que espalda antes que brazos; `oarSweep(0) < 0 < oarSweep(0,36)`.

### 7.9 Agua (`shaders/water.frag`)

Uniforms en orden: `uSize(vec2)`, `uHorizonY`, `uFocal`, `uCamHeight`, `uDistance`, `uTime`, `uWaveAmp`, `uWaveScale`, `uSunX`, `uLight`, `uWaterNear(vec3)`, `uWaterFar(vec3)`, `uSkyHorizon(vec3)`, `uSunColor(vec3)`, `uFog(vec3)`, `uWakeStrength = clamp(speed/4, 0, 1)`, `uSternZ = boatZ − 4,2 = 5,3`.

| Paso | Constantes |
|---|---|
| Inversa de cámara | `z = focal·camH/(py − horizonY)`, `x = (px − w/2)·z/focal`, `zw = z + distancia` |
| Ruido de valor | hash `fract(sin(dot(p, (127.1, 311.7)))·43758.5453)`, celda `mod 289`, interpolación `f²(3 − 2f)` |
| Altura de ola | `sin(zw/s + 0,6t)·0,6 + sin((0,7·zw + 1,3·x)/s + 0,9t)·0,4 + (n(0,8d) − 0,5)·0,6 + (n(2,6d) − 0,5)·0,3`, con `d = clamp(6/z, 1, 6)` y ruidos que derivan `+0,15t` y `−0,25t` |
| Color base | `mix(near, far, smoothstep(2, 120, z)) · (1 + h·amp·0,25)` |
| Fresnel | `pow(1 − camH/‖(z, camH)‖, 3)`; `mix(col, skyHorizon, fres·0,6)` |
| Brillo del astro | columna gaussiana de ancho `25 + 4z` px, pico 0,4; crestas `smoothstep(0,3, 0,9, h·0,5 + 0,5)`; destellos `smoothstep(0,45, 0,95, n(xz·(3 + 12/z) + 0,8t))`; `col += sunColor · columna·cresta·(0,25 + 0,75·destello)·uLight · 0,9` |
| Estela (entre popa y cámara) | `atrás = sternZ − z`; ancho de la V `0,3 + 0,35·atrás`; bordes `1 − smoothstep(0, 0,35, ‖|x| − wv‖)`; centro `(1 − smoothstep(0, 0,5, |x|))·0,6`; desvanecido `1 − smoothstep(0, sternZ, atrás)`; ruido `0,6 + 0,4·n(3x, 2zw + t)`; mezcla a blanco `· 0,35` |
| Niebla | `mix(col, fog, smoothstep(80, 400, z))` |

Costo: una pasada por píxel, ~30 operaciones, sin texturas. **Fallback** sin shader: degradé `lerp(far, fog, 0,5) → far → near` + 10 líneas de brillo bajo el astro que avanzan con la distancia (sin estela). No hay normales reales, ni reflejo de la orilla, ni refracción, ni sombra sobre el agua.

### 7.10 Miniaturas

Escena estática a las 12:00 en `distancia = 20 m`, agua fallback (sin shader), props hasta 150 m, sin bote.

---

## 8. Defectos e inconsistencias del legacy

No portarlos como "comportamiento"; el Architect y el PO deciden los que tocan producto.

| # | Hallazgo | Recomendación |
|---|---|---|
| 1 | Edificios, boathouses, faroles y pinos se apoyan en `y = 0` **sobre el agua**: no hay orilla, muro ni terreno (se ve en `image-03`). | En 3D: orillas con terreno, muelles, muros de contención y vegetación de borde. |
| 2 | Props muy cercanos (boathouses a 13 m) ocupan medio costado y se cortan bruscamente al pasar `minZ`. | Distancia de recorte suave (fade por distancia) y LOD. |
| 3 | Pala: salto instantáneo de altura (−0,05 → +0,35 m) y de forma (rectángulo → óvalo); sin rotación de feathering. | Feathering/squaring continuos (sección 12). |
| 4 | Ratio drive:recuperación fijo 1:2 a cualquier SPM. | Ritmo dependiente del SPM (sección 12). |
| 5 | Sin *surge*: el bote no acelera/desacelera dentro de la palada. | Oscilación visual de velocidad (sección 12). |
| 6 | Mango a altura constante; la mano no sigue una trayectoria real. | Trayectoria del mango con IK. |
| 7 | Luna sin fase ni sombreado (el spec decía "disco con sombra"). | Luna con fase real o al menos terminador. |
| 8 | Spec vs código: umbral de noche −0,1 (spec) vs **−0,4** (código y tests); radio de charco 1,2 m (spec) vs **0,9 m** (código); espuma 0,5 (spec) vs **0,35** (código); spec lista `uSunY`/`uSunElevation` y realce por normal que el shader no tiene; tope de props 40 (spec) vs 60–200 (código). | Manda el código. |
| 9 | `bottomNavigationBarTheme` (`#12243A`, `#00B4D8`, `#607D8B`) declarado pero **sin efecto** (se usa `NavigationBar` M3). | Definir tokens de navegación explícitos en Unity. |
| 10 | Código muerto `_FullscreenWorkoutPage` (pantalla compacta vieja con banner de paso pulsante 500 ms y grilla). | No migrar. |
| 11 | Textos fijos en el HUD sin l10n y mezclando idiomas: "Terminar", "Pausa", "Reanudar", "Entrenamiento Libre", "EN MARCHA", "Parciales 500 m", "Series anteriores", "Sin parciales…", y "Up next:" en inglés. También el diálogo de confirmación. | Señalado al PO: decidir si la paridad incluye corregir l10n. |
| 12 | Con objetivo activo, la tarjeta pierde el color semántico (blanco/rojo). | Propuesta: etiqueta y borde siguen en el color de la métrica; solo el valor pasa a blanco/`#FF4444`. Requiere OK del PO. |
| 13 | Cifras no tabulares en casi todo el HUD: los valores cambian de ancho. | Fuente con cifras tabulares en todos los números. |
| 14 | Relleno del pill y valores saltan a 1 Hz. | Interpolar visualmente (sin cambiar el dato). |
| 15 | Tipografía distinta por plataforma. | Fuente propia OFL. |
| 16 | La salpicadura dura ≈ 0,2 s y son 6 puntos; los charcos casi no se ven (`@30 %`). | VFX reales de catch, gotas desde la pala en la recuperación y charcos con normales. |

---

## 9. Qué conservar sí o sí (contrato visual)

1. **Semántica de color**: los 7 colores de métrica, los 4 de tipo de paso, los 3 de fase, `#FF4444` para objetivo no cumplido, `#FC4C02` para Strava. Mismos hex como base; en Unity se permiten variantes de luminancia (hover/pressed/glow), nunca cambiar el matiz ni el significado.
2. **Legibilidad del HUD**: valores grandes en peso 800 (52/36/28 px a 1×) sobre fondo oscuro translúcido; etiqueta chica en el color de la métrica; unidades al 55 %. Requisito: leerse a 1,5 m en un teléfono en el remo (contraste mínimo 7:1 del valor contra el fondo glass).
3. **Layout del HUD**: SPM arriba a la izquierda (la más grande) con parciales/series debajo; Split y Watts arriba a la derecha; tiempo+distancia abajo a la izquierda; kcal y BPM abajo a la derecha; Terminar/Pausa abajo al centro; barra de etapas arriba; el bote libre en el centro. Variantes vertical/horizontal como las del legacy.
4. **Barra de etapas**: codificación de pills (tenue / llenándose / sólido / hueco) por color de tipo de paso, "Up next" con ≤ 15 s, pulso en los últimos 5 s, cuenta regresiva `m:ss` o metros, reloj de pared.
5. **Paneles de series**: colapsables, colapsados por defecto, estado recordado, últimas 4 vueltas con la más reciente primero, ancho expandido acotado.
6. **Formatos**: split `mm:ss` (`--:--` sin dato), vuelta `m:ss.d`, SPM con 1 decimal, distancia `N m`, FC `prom/máx` en resúmenes, tiempo `mm:ss` o `h:mm:ss`.
7. **Escena — comportamiento**: velocidad `500/split` con τ = 1,5 s; fase de palada por SPM y cierre del ciclo a 12 spm; pausa congela la palada y desacelera; luz por hora real con override del simulador; 4 entornos con contenido y frecuencias de la tabla 7.5 y **paisaje determinista por distancia**; carteles cada 250 m con número en regata; escenario persistido; selector con miniaturas antes de remar.
8. **Encuadre por defecto**: cámara de persecución nivelada a 2,2 m de alto, remero a 9,5 m, horizonte al 40 % (vertical) / 42 % (horizontal). Es el punto de partida de la cámara principal en Unity.
9. **Gráficos del detalle**: color por métrica, franjas por tipo de paso, líneas de objetivo punteadas, split invertido.

---

## 10. Brechas contra EXR

| Área | Legacy | EXR (sección 11) | Dirección en Unity | Decide |
|---|---|---|---|---|
| Render | 2.5D CustomPainter, sin luz ni sombras | 3D con entornos realistas | URP 3D: luz direccional por hora, sombras en cascada (2 en móvil), fog, bloom, color grading por hora | Designer |
| Orillas y terreno | Props flotando en el agua | Orillas, vegetación, público, carteles, "floaties" | Terreno/orilla procedural por entorno, vegetación instanciada (GPU instancing), LOD 3 niveles | Designer + Architect |
| Agua | Ondas sinusoidales + ruido, Fresnel contra un color, estela en V | Agua realista con reflejos | Shader URP propio: ondas Gerstner (4–6), normales, reflejo del cielo (cubemap/sky) + reflejo planar opcional en gama alta, espuma de estela, anillos de charcos, glitter | Designer |
| Remero | Figura plana de 6 piezas | Avatar 3D personalizable (género, cara, piel, pelo, ropa) | Rig Mixamo en asset slot + palada procedural con IK (Animation Rigging); placeholder procedural digno | Designer; personalización: PO |
| Bote y remos | Polígono y líneas | Botes 3D desbloqueables (p. ej. B-100) y colores | Malla procedural de single + slot para modelo; remos con pala real y feathering | Designer |
| Remada | Ratio fijo, pala binaria, sin surge | Movimiento creíble sincronizado | Ritmo por SPM, feathering/squaring, altura de pala, surge, salpicadura (sección 12) | Designer |
| Cámara | Una, fija | Ángulos que cambian solos según el escenario + selector de vistas | Persecución (default, legacy) + lateral + cinemática + POV, con transiciones suaves | **PO** (cambia producto) |
| Hora y cielo | Paleta interpolada, sol/luna 2D, nubes de manchas | Atardeceres trabajados | Skybox procedural, sol/luna 3D con fase, nubes en capas, estrellas; mantener keyframes del legacy como datos | Designer |
| Otros botes | Ninguno | Fantasma blanco al ritmo del récord del segmento, remeros reales con nombre/país, bots azules, pacers con íconos | Dejar slots visuales (material "fantasma"); la funcionalidad es nueva | **PO** |
| HUD | Glass + pills | Timeline coloreada, "Up next", instrucciones de objetivo, minimapa, force curve, tamaño configurable | Conservar el HUD del legacy; minimapa y escalado del HUD como propuesta | **PO** |
| Sonido | Ninguno | Efectos auténticos | Agua, catch, carro, ambiente por entorno (assets libres) | **PO** |
| UI fuera de la escena | Material 3 por defecto | UI de marca | Sistema de diseño propio en UI Toolkit (USS), fuente OFL, transiciones y tween de números | Designer |
| Entornos | 4 ficticios genéricos | Lugares reales (Bled, Henley, Boston) + mundo propio | Mantener los 4 del legacy con look realista inspirado (lago alpino tipo Bled, río tipo Charles/Henley, costa, canal tipo Bosbaan) | Designer |
| Rendimiento | ~30 ops/píxel, 4 capas | Android 7+ arm64, 4 GB, GLES 3.2 | 60 fps en gama media: ≤ 150 draw calls, texturas ≤ 1024 en móvil, shaders sin lazos por píxel | Architect + Designer |

---

## 11. Referencia EXR (hallazgos)

- **Qué es:** app de remo indoor con mundos 3D, de Salty Lemon Entertainment; se conecta al remo por Bluetooth; Windows, Mac, Android, iOS y Apple TV. ([British Rowing](https://www.britishrowing.org/2024/05/british-rowing-announces-new-partnership-with-indoor-rowing-online-platform-exr/), [Concept2](https://www.concept2.com/blog/concept2-compatible-apps-try-exr))
- **Mundos:** lugares reales: Lake Bled (isla de Bled, Alpes de fondo), Henley-on-Thames (Temple Island, la cascada), Charles River de Boston (incluye Muddy River y recorridos por las cloacas), más un mundo propio "EXR World"/Lake EXR con cordilleras lejanas. Rutas con segmentos de 50 m a 1 km marcados con **arcos de inicio y fin** y resaltados en amarillo en el minimapa. ([Segments](https://exrgame.com/support/segments), [Olympians' locations](https://exrgame.com/blog/article/olympians-rowing-locations), [Update 1.54](https://exrgame.com/blog/article/update-1.54))
- **Mundo vivo:** público en las orillas, floaties y carteles (1.13); vegetación nueva y mayor distancia de dibujado (1.12); atardeceres mejorados en Bled y Boston y Muddy River rehecho (1.54). ([1.13](https://exrgame.com/blog/update-1.13), [1.12](https://exrgame.com/blog/update-1.12), [1.54](https://exrgame.com/blog/article/update-1.54))
- **Cámara:** los ángulos cambian automáticamente según el escenario y lo que pasa; desde 1.13 hay opción para cambiar de vista en el menú de overlay; en 1.12 se optimizaron para el HUD nuevo y para mostrar más entorno. ([Garage Gym Life review](https://garagegymlifemedia.com/?p=18036), [1.13](https://exrgame.com/blog/update-1.13), [1.12](https://exrgame.com/blog/update-1.12))
- **HUD Just Row:** tiempo, distancia, velocidad, calorías, stroke rate, split (o watts, o ambos), minimapa con vueltas, segmentos y otros remeros; opcionales: objetivos, zonas de FC, reloj, force curve (solo Concept2/RP3) y longitud de palada. **HUD Training:** timeline de intervalos coloreada (azul más lento, verde a ritmo FTP, rojo más rápido), bloque "Up next", instrucciones del objetivo, stroke rate y tempo para sincronizar con la animación, feedback de potencia por zona, minimapa. Tamaño y datos configurables. ([HUD](https://exrgame.com/support/hud))
- **Otros botes:** fantasma **blanco** que aparece al cruzar el primer arco de un segmento y rema al promedio de tu récord; remeros reales con avatar, nombre, país y estado de seguimiento (punto verde/azul en el minimapa), lista de hasta 6 (Just Row) o 4 (Training) con una línea blanca para tu posición; bots **azules**; pacers con íconos flotantes. ([Other boats](https://exrgame.com/support/other-boats), [1.54](https://exrgame.com/blog/article/update-1.54))
- **Avatar y bote:** género, cara, piel, peinado, vello facial; gorros, anteojos, muñequeras, guantes, trajes, medias, zapatillas; botes y remos desbloqueables y colores. ([Customize](https://exrgame.com/support/customize-rower))
- **Otros:** más de 100 entrenamientos, editor, desafíos, logros, rankings, minijuegos (Buoy Blast, tiburón), efectos de sonido auténticos. ([Features](https://exrgame.com/features), [1.12](https://exrgame.com/blog/update-1.12))
- **Hardware mínimo:** Android 7.0 arm64, 4 GB RAM, OpenGL ES 3.2; iOS 15, 4 GB; Windows 10/11, 8 GB. ([Hardware](https://exrgame.com/support/compatible-hardware))
- No se encontró información pública sobre el motor que usa EXR; no se asume nada.

**Qué tomamos de EXR como dirección de arte:** agua protagonista con reflejos del cielo y las orillas, entornos naturales densos y legibles, avatar con volumen y ropa técnica, cámara que respira (no clavada), HUD limpio con bloques translúcidos y color por intensidad, mundo con vida (público, banderas, boyas). **Qué no copiamos:** la paleta de intensidad de su timeline (la nuestra es por tipo de paso y es contrato), su marca e íconos.

---

## 12. Referencia biomecánica e implicancias numéricas

### 12.1 Hallazgos

- **Secuencia:** piernas → tronco → brazos en el drive y brazos → tronco → piernas en la recuperación; la recuperación es el drive al revés, con los brazos alejando el mango a la misma velocidad con que lo acercaron. Las piernas hacen el primer ~80 % del drive. ([Concept2 NZ, drive](https://www.concept2.co.nz/BLOG/x_post/Understanding-rowing-technique-catch-drive-and-finish-00054.html), [Concept2 NZ, recovery](https://concept2.co.nz/Blog/x_post/Understanding-rowing-technique-the-recovery-sequence-00059.html), [Kleshnev, row2k](https://www.row2k.com/features/5722/the-biomechanics-of-the-recovery-phase-by-dr-valery-kleshnev/))
- **Posiciones:** catch con brazos rectos, tronco inclinado adelante desde la cadera, canillas verticales; finish con tronco apenas atrás, piernas extendidas y mango bajo las costillas; manos en línea recta. En la recuperación el tronco va a ~30° pasado la vertical. ([Concept2 technique](https://www.concept2.com/technique), [Wikipedia: Rowing stroke](https://en.wikipedia.org/wiki/Rowing_stroke))
- **Ritmo vs SPM:** a 20 spm el ciclo dura 3 s con ~1 s de drive y ~2 s de recuperación (≈ 33 %); a 36 spm el ciclo dura 1,66 s con drive 0,65–0,70 s y recuperación ~0,96 s (≈ 39–42 %). El drive casi no se acorta al subir el ritmo: lo que se achica es la recuperación (~50 %). El ritmo (drive/ciclo) correlaciona fuerte con el SPM (r ≈ 0,85). ([British Rowing Plus, timing](https://plus.britishrowing.org/2022/06/10/the-rowing-stroke-timing-and-micro-moments/), [Kleshnev, ISBS](https://ojs.ub.uni-konstanz.de/cpa/article/view/486))
- **Perfil de velocidades:** en la recuperación las curvas de velocidad se vuelven más triangulares a ritmo alto; el tronco prepara su movimiento a mitad de la recuperación ("principio espejo", r = 0,76). ([Kleshnev, row2k](https://www.row2k.com/features/5722/the-biomechanics-of-the-recovery-phase-by-dr-valery-kleshnev/))
- **Ángulos del remo (scull):** novatos catch 50–60° y finish 35–42°; experimentados catch 55–72° y finish 40–45°; arco total 100–115° según altura. El finish siempre es menor que el catch. ([World Rowing, Practical Boat Rigging](https://worldrowing.com/wp-content/uploads/2020/10/Practical-Boat-Rigging-Gianni-Postilione-Conny-Draper.pdf), [ISBS](https://ojs.ub.uni-konstanz.de/cpa/article/view/6682/6038))
- **Feathering y squaring:** al extraer la pala se gira el mango 90° (pala paralela al agua); el squaring empieza aproximadamente cuando las manos pasan sobre los tobillos en la recuperación. ([Wikipedia: Rowing stroke](https://en.wikipedia.org/wiki/Rowing_stroke))
- **Altura de la pala:** a mitad de la recuperación ~2,5° de ángulo vertical (8–10 cm sobre el agua); antes del catch sube para dejar lugar al squaring: ~5° (scull, ritmo bajo) a ~6° (ritmo alto); el mango baja 3–5 cm antes del catch. ([Kleshnev, blade profile](https://www.row2k.com/features/5655/blade-work-profile-during-recovery-by-dr-valery-kleshnev/))
- **Velocidad del bote dentro de la palada:** variación ~12–20 % de la media (scullers medidos: 12,4 % y 13,5 %), mínimo en el catch; la variación baja al subir el ritmo. ([Kleshnev, boat check](https://www.row2k.com/features/5467/on-the-topic-of-boat-check/), [Physics Today](https://physicstoday.aip.org/quick-study/row-bots))

### 12.2 Implicancias para la animación (propuestas para la biblia de animación)

Todas son **solo visuales**: no cambian distancia, velocidad media ni métricas.

1. **Ritmo por SPM** (reemplaza el 1:2 fijo). Duración del drive `T_d(spm) = clamp(1,05 − 0,0194·(spm − 18), 0,60, 1,15) s`; ritmo `r = T_d / (60/spm)`, acotado a ≤ 0,48.

   | SPM | Ciclo | Drive | Ritmo | Legacy (drive = 0,40·ciclo) |
   |---|---|---|---|---|
   | 18 | 3,33 s | 1,05 s | 32 % | 1,33 s |
   | 20 | 3,00 s | 1,01 s | 34 % | 1,20 s |
   | 24 | 2,50 s | 0,93 s | 37 % | 1,00 s |
   | 28 | 2,14 s | 0,86 s | 40 % | 0,86 s |
   | 32 | 1,88 s | 0,78 s | 41 % | 0,75 s |
   | 36 | 1,67 s | 0,70 s | 42 % | 0,67 s |

   Implementación compatible con los tests del legacy: se conserva `StrokeCycle(p)` con su fase canónica (drive en 0–0,40) y se agrega un mapeo tiempo real → fase canónica que estira el drive a `r` y la recuperación a `1 − r`. El SPM que alimenta el mapeo se suaviza (τ ≈ 0,8 s) para que los cambios de ritmo no den saltos.
2. **Ángulos**: catch −60°, finish +42° (arco 102°, sculler intermedio). Legacy: −55°/+35° (90°).
3. **Pala**: rotación de feathering de 90° durante la extracción (fase canónica 0,36–0,42, ease-in-out); altura 8–10 cm en la recuperación; squaring de 0,85 a 0,96 mientras la pala sube a ~5–6°; entrada al agua en 0,97–1,00 (sin salto).
4. **Surge**: offset visual del bote respecto de la cámara con amplitud ±6–8 % de la velocidad media (≈ 12–16 % pico a pico), mínimo en el catch, que baja con el SPM; la cámara lo sigue con suavizado para que se perciba sin marear. La distancia integrada no cambia.
5. **Catch y extracción**: salpicadura en el catch proporcional a la potencia (watts), gotas que caen de la pala en el primer tercio de la recuperación, charcos visibles de 2–4 s en pares.
6. **Mango**: trayectoria casi recta en el drive y baja 3–5 cm antes del catch; manos y muñecas por IK sobre el mango; tronco ~+30° en el catch y −15° a −20° en el finish.

---

## 13. Propuestas que necesitan decisión

| Para | Propuesta |
|---|---|
| PO | Cámaras adicionales (lateral, cinemática, POV) y selector, con la de persecución como default. |
| PO | Corregir l10n de los textos fijos del HUD (hallazgo 11). |
| PO | Mantener el color semántico de etiqueta y borde con objetivo activo (hallazgo 12). |
| PO | Sonido ambiente y de remada; minimapa; escala del HUD; otros botes (fantasma/bots). Todo fuera de la paridad estricta. |
| Architect | Lógica pura de escena en Core y testeable con dotnet: `SceneCamera`, `SceneState`, `StrokeCycle` + mapeo de ritmo, `TimeOfDay`, generación de props por segmento con el mismo RNG determinista (ojo: el RNG de Dart no es el de .NET; hay que portar el algoritmo o fijar tablas para mantener "mismo paisaje a la misma distancia"). |
| Architect | Asset slots para remero (rig Mixamo), bote, remos, props por entorno, skybox y agua, cada uno con su placeholder procedural. |

---

## 14. Fuentes

- EXR: [features](https://exrgame.com/features) · [HUD](https://exrgame.com/support/hud) · [otros botes](https://exrgame.com/support/other-boats) · [segmentos](https://exrgame.com/support/segments) · [avatar](https://exrgame.com/support/customize-rower) · [hardware](https://exrgame.com/support/compatible-hardware) · [update 1.12](https://exrgame.com/blog/update-1.12) · [update 1.13](https://exrgame.com/blog/update-1.13) · [update 1.54](https://exrgame.com/blog/article/update-1.54) · [lugares olímpicos](https://exrgame.com/blog/article/olympians-rowing-locations) · [Concept2 blog](https://www.concept2.com/blog/concept2-compatible-apps-try-exr) · [British Rowing](https://www.britishrowing.org/2024/05/british-rowing-announces-new-partnership-with-indoor-rowing-online-platform-exr/) · [Garage Gym Life](https://garagegymlifemedia.com/?p=18036)
- Biomecánica: [Kleshnev, recuperación (row2k)](https://www.row2k.com/features/5722/the-biomechanics-of-the-recovery-phase-by-dr-valery-kleshnev/) · [Kleshnev, perfil de pala (row2k)](https://www.row2k.com/features/5655/blade-work-profile-during-recovery-by-dr-valery-kleshnev/) · [Kleshnev, boat check (row2k)](https://www.row2k.com/features/5467/on-the-topic-of-boat-check/) · [Kleshnev, análisis temporal (ISBS)](https://ojs.ub.uni-konstanz.de/cpa/article/view/486) · [British Rowing Plus, timing](https://plus.britishrowing.org/2022/06/10/the-rowing-stroke-timing-and-micro-moments/) · [World Rowing, Practical Boat Rigging](https://worldrowing.com/wp-content/uploads/2020/10/Practical-Boat-Rigging-Gianni-Postilione-Conny-Draper.pdf) · [ISBS, ángulos](https://ojs.ub.uni-konstanz.de/cpa/article/view/6682/6038) · [Concept2 technique](https://www.concept2.com/technique) · [Concept2 NZ drive](https://www.concept2.co.nz/BLOG/x_post/Understanding-rowing-technique-catch-drive-and-finish-00054.html) · [Concept2 NZ recovery](https://concept2.co.nz/Blog/x_post/Understanding-rowing-technique-the-recovery-sequence-00059.html) · [Wikipedia: Rowing stroke](https://en.wikipedia.org/wiki/Rowing_stroke) · [Physics Today: Row bots](https://physicstoday.aip.org/quick-study/row-bots)
- Unity (referencia técnica de agua URP gratis): [Unity BoatAttack (Gerstner, reflejos planares, cáusticas, flotación)](https://github.com/Verasl/BoatAttack). Revisar su `LICENSE.md` antes de reutilizar código.
