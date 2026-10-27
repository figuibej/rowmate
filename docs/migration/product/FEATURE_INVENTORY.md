# Inventario maestro de funcionalidades (RowMate legacy → Unity)

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-27 |
| Fuente | `https://github.com/figuibej/rowmate-legacy` (solo lectura). Manda el código cuando difiere de los docs. |
| Insumos | Los 8 inventarios de detalle de [inventory/](inventory/) y la auditoría visual [LEGACY_VISUAL_AUDIT.md](../design/LEGACY_VISUAL_AUDIT.md) |
| Uso | Índice único del contrato de paridad. Cada ID tiene **una** iteración dueña en el [ROADMAP](../ROADMAP.md). El detalle (comportamientos, números, textos, casos borde) vive en el inventario de detalle; este documento no lo repite. |

---

## 0. Cómo leer este documento

- **ID canónico:** el que se especifica, se implementa y se verifica. **Alias:** ID de otro inventario que describe lo mismo (o una parte) y queda absorbido por el canónico. Un alias se cierra junto con su canónico, en la misma iteración. Al escribir specs, criterios y tests se usa **siempre el canónico**; el alias se cita entre paréntesis para no perder la trazabilidad.
- **Área:** el inventario de origen. Subáreas que usan los docs como dependencia: `SIM` (simulador de desarrollo), `HUD` (pantalla inmersiva), `DB` (base local), `PRF` (pantalla Perfil).
- **Pura:** `Sí` = regla o cálculo sin pantalla ni hardware, que se verifica con `dotnet test` fuera de Unity. `Parcial` = tiene una regla pura adentro (se testea esa parte). `No` = necesita Unity Editor, hardware o servicio externo para verificarse.
- **It.:** iteración dueña según el [ROADMAP](../ROADMAP.md) (la que cierra todos sus criterios de aceptación). Si otra iteración la toca antes o después, figura en la matriz de trazabilidad del roadmap (§4, columna "También toca"). Las It. 4, 6 y 10 están divididas en **a/b** (ROADMAP §1 y §7). En las tablas de decisiones (§4), "It. 4", "It. 6" o "It. 10" quiere decir la sub-iteración de la ficha afectada; la asignación exacta está en "Decisiones aplicadas" de cada iteración del ROADMAP. Los criterios que solo se pueden ver en iOS o macOS (**[Apple]**) se verifican en la tanda Apple (It. 10b) o en la It. 14 (ROADMAP §0, regla 8).
- **Tests legacy:** archivo de `rowmate-legacy/test/` y cantidad de casos. `—` = sin test en el legacy (hueco: el PO fija criterios nuevos en la spec de la iteración).

### Totales

| Área | Inventario de detalle | IDs | Canónicos | Alias | Canónicos con lógica pura (Sí/Parcial) |
|---|---|---|---|---|---|
| BLE (remo, Dispositivo, simulador) | [rower-ble-device-sim.md][ble] | 16 | 16 | 0 | 6 |
| HRM (pulsómetro) | [heart-rate.md][hrm] | 16 | 15 | 1 | 3 |
| RUT (modelos y rutinas) | [routines-models.md][rut] | 20 | 18 | 2 | 10 |
| WKT (motor y HUD) | [workout-engine-hud.md][wkt] | 23 | 22 | 1 | 11 |
| SCN (escena inmersiva) | [immersive-scene.md][scn] | 23 | 23 | 0 | 14 |
| HIS (historial y base local) | [history-persistence.md][his] | 23 | 15 | 8 | 6 |
| STR (Strava y Perfil) | [strava-profile.md][str] | 18 | 18 | 0 | 6 |
| APP (shell y plataformas) | [app-shell-platform.md][app] | 19 | 15 | 4 | 4 |
| **Total** | | **158** | **142** | **16** | **60** |

Código legacy a migrar: ~11 500 líneas Dart/GLSL vivas (de ~12 450; se descartan ~930 de código muerto), 416 líneas de catálogo `.arb` y 2 129 líneas de tests en 26 archivos (≈ 145 casos contando los parametrizados; el placeholder vacío `widget_test.dart` no se porta).

---

## 1. Deduplicación: alias → canónico

| Alias | Canónico | Qué se solapa | Por qué gana el canónico |
|---|---|---|---|
| F-RUT-15 | **F-BLE-01** | Lectura en vivo del remo (8 campos) y sus formatos | Mismo modelo `rowing_data.dart`; BLE es el origen del dato |
| F-HRM-14 | **F-WKT-05** | Fusión del pulso del sensor con los datos del remo | La fusión vive en el motor (`workout_provider.dart:226-237`); mismo test `workout_provider_hr_test` |
| F-HIS-01 | **F-WKT-06** | Alta de la sesión al empezar | WKT-06 punto 1 describe exactamente lo mismo |
| F-HIS-02 | **F-WKT-06** | Muestreo de telemetría cada 5 s activos | WKT-06 punto 2-3 |
| F-HIS-03 | **F-WKT-06** | Cierre de la sesión, último punto y totales | WKT-06 puntos 4-7 |
| F-HIS-04 | **F-RUT-18** | Estadísticas de sesión (P99, totales, pulso) | Mismo modelo y mismo test `session_stats_test` |
| F-HIS-18 | **F-RUT-08** | Persistencia de rutinas y pasos | Misma tabla y mismas operaciones; RUT es el dueño funcional |
| F-HIS-08 | **F-STR-17** | Ícono de sincronización con Strava en Historial y Detalle | Solo existe con Strava configurado; se cierra con Strava |
| F-HIS-22 | **F-STR-15** | "Limpiar sesiones de Strava" (Perfil → Mantenimiento) | La acción está en Perfil; HIS documenta el efecto en la base |
| F-HIS-23 | **F-STR-13** | Sesiones importadas: ritmo recalculado, vueltas, tipo de paso | Es la misma reconstrucción; HIS solo describe cómo se ven (se verifica dentro de F-STR-13) |
| F-WKT-22 | **F-STR-06** | Reglas de subida a Strava al terminar (auto/preguntar/manual, bloqueo en simulador) | Es comportamiento de Strava disparado por el fin de sesión |
| F-APP-04 | **F-STR-01** | Strava existe solo si el build trae credenciales reales | Misma regla; STR es el dueño |
| F-APP-07 | **F-RUT-01** | Colores, nombres e íconos por tipo de paso | Mismo dato; el modelo del paso es el dueño |
| F-APP-12 | **F-WKT-11** | Convenciones de formato (punto decimal fijo, miles con punto en paneles, 24 h) | Mismo test `series_panels_test` "formatos". Las fechas del historial quedan en **F-HIS-05** |
| F-APP-18 | **F-BLE-13** | Variante de compilación "simulador" vista desde el shell | Misma activación y mismo aislamiento |
| F-RUT-19 | **F-APP-08** | Tarjeta de paso compartida `interval_tile.dart` (sin uso) | Ambos describen widgets compartidos muertos; se descartan juntos |

Solapamientos parciales que **no** se fusionan (son piezas distintas y se verifican por separado):

| IDs | Relación |
|---|---|
| F-BLE-13 / F-HIS-20 / F-STR-18 | Activación del simulador / base separada `rower_app_dev.db` / bloqueo de subidas a Strava |
| F-BLE-16 / F-SCN-23 | Control "Hora escena" del panel / efecto de la hora forzada sobre la luz de la escena |
| F-STR-07 / F-HIS-16 | Operación "subir una sesión" / botón de subida en el Detalle |
| F-STR-11 / F-HIS-21 | Subida en lote / regla de reparación que corre antes |
| F-BLE-04 / F-APP-13 | Estado del adaptador y vistas de permiso en Dispositivo / declaraciones de permisos por plataforma |
| F-WKT-11 / F-HIS-05 | Formatos del HUD / formatos del historial (duración, ritmo de paso, fecha `dd/MM/yyyy  HH:mm`) |
| F-BLE-10 | Agrega comportamientos que viven en F-WKT-12 (aviso), F-APP-02 (primera pestaña) y F-BLE-05 (exclusión); se cierra en la It. 4a |

---

## 2. Índice maestro

Columnas: **ID** · **Funcionalidad** · **Área** · **Pura** · **Depende de** (canónicos o áreas) · **Tests legacy** · **It.** · **Detalle**.

### 2.1 BLE — remo FTMS, pestaña Dispositivo y simulador

| ID | Funcionalidad | Área | Pura | Depende de | Tests legacy | It. | Detalle |
|---|---|---|---|---|---|---|---|
| F-BLE-01 | Lectura en vivo del remo y formatos de display *(alias F-RUT-15)* | BLE | Sí | — (base de toda la app) | — (hueco) | 1 | [ficha][ble] |
| F-BLE-02 | Decodificación del paquete FTMS Rower Data `0x2AD1` | BLE | Sí | F-BLE-01 | — (hueco: tabla de 8 ejemplos) | 1 | [ficha][ble] |
| F-BLE-03 | Filtro de candidatos por servicio anunciado (Windows filtra en la app) | BLE | Sí | — | `scan_filter_test` (2) | 1 | [ficha][ble] |
| F-BLE-04 | Estado del Bluetooth, permisos y requisitos de plataforma | BLE | No | F-APP-13 | — | 10a | [ficha][ble] |
| F-BLE-05 | Búsqueda de remos FTMS (10 s, lista en vivo, exclusión con el pulsómetro) | BLE | No | F-BLE-03, F-BLE-04, F-BLE-06, F-HRM-02 | `heart_rate_card_test` (:121, :142, :161) | 10a | [ficha][ble] |
| F-BLE-06 | Conexión y toma de control FTMS (Request Control, Start, keepalive 2 s) | BLE | No | F-BLE-02, F-BLE-05, F-BLE-09, F-BLE-11 | — | 10a | [ficha][ble] |
| F-BLE-07 | Desconexión manual (olvida el equipo para la reconexión) | BLE | No | F-BLE-11 | `simulated_ble_service_test` (:64) | 2 | [ficha][ble] |
| F-BLE-08 | Reconexión automática tras una caída (3 s) | BLE | No | F-BLE-06, F-WKT-01 | `simulated_ble_service_test` (:39, solo simulada) | 10a | [ficha][ble] |
| F-BLE-09 | Watchdog de monitor apagado (≥ 5 s sin paquetes → desconecta) | BLE | Sí (regla) | F-BLE-06, F-BLE-07 | — | 10a | [ficha][ble] |
| F-BLE-10 | Señal "remo conectado" para el resto de la app | BLE | No | F-WKT-12, F-HRM-13, F-APP-02 | `heart_rate_card_test` (:161) | 4a | [ficha][ble] |
| F-BLE-11 | Pestaña Dispositivo, vista conectada (tablero en vivo, 2/3 columnas) | BLE | No | F-BLE-01, F-BLE-07, F-HRM-13, F-APP-05, F-APP-06 | `heart_rate_card_test` (:83) | 2 | [ficha][ble] |
| F-BLE-12 | Diagnóstico BLE oculto ("Show BLE debug") | BLE | No | F-BLE-02, F-BLE-06, F-BLE-11 | — | 10a | [ficha][ble] |
| F-BLE-13 | Modo simulador: activación solo en build de desarrollo y aislamiento *(alias F-APP-18)* | BLE·SIM | No | F-BLE-14..16, F-HRM-15, F-HIS-20, F-STR-18 | — (indirecto) | 2 | [ficha][ble] |
| F-BLE-14 | Física del remo simulado (fórmulas Concept2, pulso simulado) | BLE·SIM | Sí | F-BLE-01 | `rowing_simulator_test` (7) | 1 | [ficha][ble] |
| F-BLE-15 | Remo simulado "conectado" (1 lectura/s, caída simulada 3 s) | BLE·SIM | Sí (reloj falso) | F-BLE-13, F-BLE-14 | `simulated_ble_service_test` (3) | 2 | [ficha][ble] |
| F-BLE-16 | Panel de control del simulador (presets, W, SPM, hora, fallas) | BLE·SIM | No | F-BLE-14, F-BLE-15, F-HRM-15, F-SCN-23 | `simulator_overlay_test` (2) | 2 | [ficha][ble] |

### 2.2 HRM — pulsómetro

| ID | Funcionalidad | Área | Pura | Depende de | Tests legacy | It. | Detalle |
|---|---|---|---|---|---|---|---|
| F-HRM-01 | Lectura del paquete Heart Rate Measurement `0x2A37` | HRM | Sí | — | `heart_rate_parser_test` (6) | 1 | [ficha][hrm] |
| F-HRM-02 | Búsqueda de pulsómetros | HRM | No | F-BLE-03, F-BLE-05, F-HRM-11, F-HRM-13, F-HRM-16 | `simulated_heart_rate_service_test`, `heart_rate_card_test`, `scan_filter_test` | 11 | [ficha][hrm] |
| F-HRM-03 | Conexión manual a un sensor de la lista (15 s, validación 0x180D) | HRM | No | F-HRM-02, F-HRM-04..06, F-HRM-08, F-HRM-11, F-HRM-12 | `heart_rate_provider_test`, `heart_rate_card_test` | 11 | [ficha][hrm] |
| F-HRM-04 | Pulso en vivo | HRM | No | F-HRM-01, F-HRM-05 | `simulated_heart_rate_service_test`, `heart_rate_card_test` | 2 | [ficha][hrm] |
| F-HRM-05 | Watchdog de lectura vencida (10 s → pulso 0) | HRM | Sí | F-HRM-04 | `reconnect_loop_test` (StaleWatchdog, 1) | 11 | [ficha][hrm] |
| F-HRM-06 | Memoria del último sensor (`hrm.deviceId`, `hrm.deviceName`) | HRM | No | F-HRM-03, F-HRM-07, F-HRM-10 | indirectos | 11 | [ficha][hrm] |
| F-HRM-07 | Reconexión automática al arrancar (6 intentos, 10 s) y botón Conectar | HRM | No | F-HRM-06, F-HRM-09, F-HRM-11, F-HRM-13, F-APP-01 | `reconnect_loop_test`, `heart_rate_provider_test` | 11 | [ficha][hrm] |
| F-HRM-08 | Reconexión automática tras caída (cada 3 s, sin límite) | HRM | No | F-HRM-03, F-HRM-09, F-HRM-11 | `reconnect_loop_test`, `simulated_heart_rate_service_test`, `heart_rate_provider_test`, `simulator_overlay_test` | 11 | [ficha][hrm] |
| F-HRM-09 | Motor de reintentos (cadencia) | HRM | Sí | — | `reconnect_loop_test` (ReconnectLoop, 6) | 11 | [ficha][hrm] |
| F-HRM-10 | Desconectar y Olvidar | HRM | No | F-HRM-06, F-HRM-11 | `simulated_heart_rate_service_test`, `heart_rate_provider_test`, `heart_rate_card_test` | 11 | [ficha][hrm] |
| F-HRM-11 | Prioridad de las acciones del usuario sobre lo automático | HRM | No | F-HRM-02, F-HRM-03, F-HRM-07..10 | `reconnect_loop_test`, `heart_rate_provider_test`, `simulated_heart_rate_service_test` | 11 | [ficha][hrm] |
| F-HRM-12 | Estado de presentación del pulsómetro | HRM | No | F-HRM-02..10 (con el simulado en la It. 2) | `heart_rate_provider_test` (3) | 2 | [ficha][hrm] |
| F-HRM-13 | Tarjeta "Pulsómetro" en Dispositivo | HRM | No | F-HRM-12, F-BLE-11, F-APP-05 | `heart_rate_card_test` (4) | 2 | [ficha][hrm] |
| F-HRM-14 | *Alias de F-WKT-05* | — | — | — | `workout_provider_hr_test` | 4a | [ficha][hrm] |
| F-HRM-15 | Pulsómetro simulado (comparte atleta con el remo simulado) | HRM·SIM | No | F-BLE-14, F-BLE-16, F-HRM-13 | `simulated_heart_rate_service_test` (3), `simulator_overlay_test` | 2 | [ficha][hrm] |
| F-HRM-16 | Compatibilidad de sensores y plataformas | HRM | No | F-BLE-03, F-BLE-04, F-APP-13 | `scan_filter_test` | 11 | [ficha][hrm] |

### 2.3 RUT — modelos de dominio, rutinas, series y progresiones

| ID | Funcionalidad | Área | Pura | Depende de | Tests legacy | It. | Detalle |
|---|---|---|---|---|---|---|---|
| F-RUT-01 | Tipos de paso: identidad, nombre, color e ícono *(alias F-APP-07)* | RUT | Sí | F-APP-10 | indirecto (`step_pill_test`) | 1 | [ficha][rut] |
| F-RUT-02 | Modelo de paso (meta, objetivos, serie, progresiones) | RUT | Sí | F-RUT-01 | fixture en varios | 1 | [ficha][rut] |
| F-RUT-03 | Etiqueta de duración del paso | RUT | Sí | F-RUT-02 | — (casos §5.3) | 1 | [ficha][rut] |
| F-RUT-04 | Etiqueta de objetivos del paso (incluye progresiones) | RUT | Sí | F-RUT-02, F-RUT-06 | — (casos §5.3) | 1 | [ficha][rut] |
| F-RUT-05 | Modelo de rutina y resumen ("{n} series · {m} descansos") | RUT | Sí | F-RUT-02 | — (casos §5.3) | 1 | [ficha][rut] |
| F-RUT-06 | Expansión de series con progresiones | RUT | Sí | F-RUT-02, F-RUT-05 | `routine_positions_test` (largo) | 1 | [ficha][rut] |
| F-RUT-07 | Posición de cada paso en su serie | RUT | Sí | F-RUT-06 | `routine_positions_test` (1) | 1 | [ficha][rut] |
| F-RUT-08 | Persistencia de rutinas y pasos *(alias F-HIS-18)* | RUT·DB | No | F-HIS-17, F-HIS-20 | — | 3 | [ficha][rut] |
| F-RUT-09 | Estado de la colección de rutinas (carga, alta, edición, baja) | RUT | No | F-RUT-08 | — | 3 | [ficha][rut] |
| F-RUT-10 | Pantalla Rutinas (lista) | RUT | No | F-RUT-05, F-RUT-09, F-RUT-11, F-APP-03 | — | 3 | [ficha][rut] |
| F-RUT-11 | Editor de rutina: datos generales y guardado | RUT | No | F-RUT-09, F-RUT-12..14 | — | 3 | [ficha][rut] |
| F-RUT-12 | Pasos sueltos en el editor | RUT | No | F-RUT-01, F-RUT-03, F-RUT-04, F-RUT-13 | — | 3 | [ficha][rut] |
| F-RUT-13 | Diálogo de paso (formulario e interpretación de campos) | RUT | No | F-RUT-01, F-RUT-02 | — | 3 | [ficha][rut] |
| F-RUT-14 | Series en el editor (crear, ×N, agregar paso, eliminar) | RUT | No | F-RUT-06, F-RUT-07, F-RUT-12, F-RUT-13 | — | 3 | [ficha][rut] |
| F-RUT-15 | *Alias de F-BLE-01* | — | — | — | — | 1 | [ficha][rut] |
| F-RUT-16 | Sesión de entrenamiento (registro) | RUT | Sí | F-RUT-18 | — | 1 | [ficha][rut] |
| F-RUT-17 | Punto de telemetría | RUT | Sí | F-RUT-06 | fixture (`session_stats_test`) | 1 | [ficha][rut] |
| F-RUT-18 | Estadísticas de sesión: P99, totales y pulso *(alias F-HIS-04)* | RUT | Sí | F-RUT-17 | `session_stats_test` (3) | 1 | [ficha][rut] |
| F-RUT-19 | *Alias de F-APP-08* (descarte) | — | — | — | — | 2 | [ficha][rut] |
| F-RUT-20 | Vínculo rutina ↔ sesiones pasadas | RUT | No | F-RUT-06, F-RUT-08, F-RUT-16, F-RUT-17 | — | 9 | [ficha][rut] |

### 2.4 WKT — motor de entrenamiento, pantalla Workout y HUD inmersivo

| ID | Funcionalidad | Área | Pura | Depende de | Tests legacy | It. | Detalle |
|---|---|---|---|---|---|---|---|
| F-WKT-01 | Ciclo de vida y fases de la sesión | WKT | Sí | F-WKT-02, F-WKT-03, F-WKT-06, F-RUT-06, F-BLE-01 | — | 4a | [ficha][wkt] |
| F-WKT-02 | Reloj activo (tick de 1 s, las pausas no cuentan) | WKT | Sí | F-WKT-01 | — | 4a | [ficha][wkt] |
| F-WKT-03 | Avance de pasos y fin automático de la rutina | WKT | Sí | F-RUT-06, F-RUT-07, F-WKT-01, F-WKT-02 | `routine_positions_test` (indirecto) | 4a | [ficha][wkt] |
| F-WKT-04 | Estado del paso en curso (progreso, restante, "por terminar", "a continuación") | WKT | Sí | F-WKT-03 | — | 4a | [ficha][wkt] |
| F-WKT-05 | Fusión del pulso (pulsómetro por encima de FTMS) *(alias F-HRM-14)* | WKT | Sí | F-HRM-04, F-BLE-01 | `workout_provider_hr_test` (4) | 4a | [ficha][wkt] |
| F-WKT-06 | Telemetría cada 5 s y guardado de la sesión *(alias F-HIS-01, F-HIS-02, F-HIS-03)* | WKT·DB | Sí | F-RUT-16..18, F-HIS-17, F-WKT-02, F-WKT-05 | `session_stats_test` (relacionado) | 4a | [ficha][wkt] |
| F-WKT-07 | Parciales de 500 m | WKT | Sí | F-WKT-01..03 | `series_tracker_test` (8) | 5 | [ficha][wkt] |
| F-WKT-08 | Repeticiones de la serie y comparación | WKT | Sí | F-WKT-07, F-RUT-07 | `series_tracker_test` (6) | 5 | [ficha][wkt] |
| F-WKT-09 | Cumplimiento de objetivos por paso | WKT | Sí | F-WKT-02, F-WKT-03, F-RUT-04 | `step_target_tracker_test` (6) | 4a | [ficha][wkt] |
| F-WKT-10 | Semáforo de objetivos en vivo | WKT | Sí | F-WKT-03, F-WKT-04 | — | 4a | [ficha][wkt] |
| F-WKT-11 | Formatos de display del HUD *(alias F-APP-12)* | WKT | Sí | F-RUT-04, F-RUT-05 | `series_panels_test` ("formatos") | 4a | [ficha][wkt] |
| F-WKT-12 | Pantalla Workout en reposo (aviso sin remo, libre, rutinas) | WKT | No | F-BLE-10, F-RUT-09, F-SCN-22 | — | 4a | [ficha][wkt] |
| F-WKT-13 | Pantalla inmersiva: estructura, capas y navegación | WKT·HUD | No | F-WKT-01, F-SCN-01, F-APP-03 | — | 4b | [ficha][wkt] |
| F-WKT-14 | Barra de pasos de la rutina | WKT·HUD | No | F-WKT-04, F-WKT-09, F-WKT-15, F-WKT-19 | — | 4b | [ficha][wkt] |
| F-WKT-15 | Píldora de paso: estados visuales | WKT·HUD | No | F-WKT-04, F-WKT-09 | `step_pill_test` (1) | 4b | [ficha][wkt] |
| F-WKT-16 | Barra superior del entrenamiento libre y chip de fase | WKT·HUD | No | F-WKT-01, F-WKT-19 | — | 4b | [ficha][wkt] |
| F-WKT-17 | HUD: tarjetas glass y banda de tiempo y distancia | WKT·HUD | No | F-WKT-05, F-WKT-10, F-WKT-11 | — | 4b | [ficha][wkt] |
| F-WKT-18 | Paneles colapsables de parciales y repeticiones | WKT·HUD | No | F-WKT-07, F-WKT-08, F-WKT-11, F-WKT-17 | `series_panels_test` (3) | 5 | [ficha][wkt] |
| F-WKT-19 | Reloj de pared | WKT·HUD | No | F-WKT-14, F-WKT-16 | — | 4b | [ficha][wkt] |
| F-WKT-20 | Controles (Pausa/Reanudar/Terminar) y confirmación de terminar | WKT·HUD | No | F-WKT-01, F-WKT-06, F-WKT-13 | — | 4a | [ficha][wkt] |
| F-WKT-21 | Diálogo de rutina completada | WKT·HUD | No | F-WKT-03, F-WKT-06, F-WKT-13 | — | 4b | [ficha][wkt] |
| F-WKT-22 | *Alias de F-STR-06* | — | — | — | — | 12 | [ficha][wkt] |
| F-WKT-23 | Pantalla clásica y `MetricCard` (código muerto: **descarte**) | WKT | No | — | — | 4a | [ficha][wkt] |

### 2.5 SCN — escena inmersiva

| ID | Funcionalidad | Área | Pura | Depende de | Tests legacy | It. | Detalle |
|---|---|---|---|---|---|---|---|
| F-SCN-01 | Escena viva de fondo: composición y ciclo de vida | SCN | No | F-WKT-01, F-BLE-01, F-SCN-02..21 | `scene_view_test` (16) | 6b | [ficha][scn] |
| F-SCN-02 | Cámara de persecución (2,2 m de alto, remero a 9,5 m) | SCN | Sí | — | `scene_camera_test` (4) | 6a | [ficha][scn] |
| F-SCN-03 | Velocidad y distancia de la escena (500/split, τ = 1,5 s) | SCN | Sí | F-BLE-01, F-WKT-01 | `scene_state_test` (:14, :26, :55) | 6a | [ficha][scn] |
| F-SCN-04 | Avance de la fase de palada | SCN | Sí | F-SCN-03 | `scene_state_test` (:37, :46, :60) | 7 | [ficha][scn] |
| F-SCN-05 | Secuencia biomecánica de la palada | SCN | Sí | F-SCN-04 | `stroke_cycle_test` (4), `boat_painter_test` | 7 | [ficha][scn] |
| F-SCN-06 | Cabeceo del bote | SCN | Sí | F-SCN-04 | — | 7 | [ficha][scn] |
| F-SCN-07 | Charcos de pala | SCN | Sí | F-SCN-03..05 | `scene_state_test` (:69), `boat_painter_test` | 7 | [ficha][scn] |
| F-SCN-08 | Bote, remero, remos, salpicadura y sombra | SCN | No | F-SCN-02, F-SCN-04..07, F-SCN-09, F-SCN-10 | `boat_painter_test` (1 + 2 tamaños) | 7 | [ficha][scn] |
| F-SCN-09 | Hora efectiva y paleta por hora | SCN | Sí | F-SCN-23 | `time_of_day_test` | 6a | [ficha][scn] |
| F-SCN-10 | Sol, luna y fuerza de la luz | SCN | Sí | F-SCN-02, F-SCN-09 | `time_of_day_test`, `water_painter_test` | 6a | [ficha][scn] |
| F-SCN-11 | Cielo: degradé, estrellas, sol/luna y nubes | SCN | No | F-SCN-02, F-SCN-09, F-SCN-10 | `sky_shore_painter_test` | 6a | [ficha][scn] |
| F-SCN-12 | Agua avanzada: ondas, reflejo, brillo, estela y niebla | SCN | No | F-SCN-02, F-SCN-03, F-SCN-09, F-SCN-10, F-SCN-13 | `water_painter_test`, `scene_state_test` (:55) | 6b | [ficha][scn] |
| F-SCN-13 | Agua degradada y disponibilidad del agua avanzada | SCN | No | F-SCN-09, F-SCN-10, F-SCN-12, F-WKT-12 | `water_painter_test` (:11), `scene_view_test` | 6b | [ficha][scn] |
| F-SCN-14 | Orilla: segmentos deterministas, visibilidad y tope | SCN | Sí | F-SCN-03 | `environment_test` | 6a | [ficha][scn] |
| F-SCN-15 | Escenario Lago alpino | SCN | Sí | F-SCN-14, F-SCN-19, F-SCN-20 | `environment_test` (lake) | 6a | [ficha][scn] |
| F-SCN-16 | Escenario Río urbano | SCN | Sí | F-SCN-14, F-SCN-19, F-SCN-20 | `environment_test` (river) | 8 | [ficha][scn] |
| F-SCN-17 | Escenario Costa | SCN | Sí | F-SCN-12, F-SCN-14, F-SCN-19, F-SCN-20 | `environment_test` (coast), `water_painter_test` (:27) | 8 | [ficha][scn] |
| F-SCN-18 | Escenario Canal de regata (carteles cada 250 m) | SCN | Sí | F-SCN-03, F-SCN-14, F-SCN-19, F-SCN-20 | `environment_test` (regatta) | 8 | [ficha][scn] |
| F-SCN-19 | Siluetas lejanas del horizonte por escenario | SCN | No | F-SCN-02, F-SCN-03, F-SCN-09, F-SCN-11 | `sky_shore_painter_test` (:16) | 8 | [ficha][scn] |
| F-SCN-20 | Dibujo de objetos de orilla (catálogo, luz, niebla, noche) | SCN | No | F-SCN-02, F-SCN-09, F-SCN-14..18 | `sky_shore_painter_test` | 8 | [ficha][scn] |
| F-SCN-21 | Preferencia de escenario (`scene.environment`, por defecto `lake`) | SCN | Sí | F-APP-01 | `environment_picker_test` (:38) | 8 | [ficha][scn] |
| F-SCN-22 | Selector de escenario con miniaturas | SCN | No | F-SCN-13..21, F-WKT-12 | `environment_picker_test` (:11) | 8 | [ficha][scn] |
| F-SCN-23 | Hora de escena forzada (solo simulador) | SCN·SIM | No | F-BLE-16, F-SCN-09 | `simulator_overlay_test` (:54-56) | 6a | [ficha][scn] |

### 2.6 HIS — historial, detalle y base local

| ID | Funcionalidad | Área | Pura | Depende de | Tests legacy | It. | Detalle |
|---|---|---|---|---|---|---|---|
| F-HIS-01 | *Alias de F-WKT-06* (alta de la sesión) | — | — | — | — | 4a | [ficha][his] |
| F-HIS-02 | *Alias de F-WKT-06* (muestreo cada 5 s) | — | — | — | — | 4a | [ficha][his] |
| F-HIS-03 | *Alias de F-WKT-06* (cierre y guardado) | — | — | — | — | 4a | [ficha][his] |
| F-HIS-04 | *Alias de F-RUT-18* | — | — | — | `session_stats_test` | 1 | [ficha][his] |
| F-HIS-05 | Formatos del historial: duración, ritmo, fecha y números | HIS | Sí | F-RUT-18 | — | 9 | [ficha][his] |
| F-HIS-06 | Acceso al Historial y recargas | HIS | No | F-APP-02, F-APP-03, F-HIS-19 | — | 9 | [ficha][his] |
| F-HIS-07 | Lista de sesiones | HIS | No | F-HIS-05, F-HIS-06, F-HIS-19, F-RUT-18 | — | 9 | [ficha][his] |
| F-HIS-08 | *Alias de F-STR-17* | — | — | — | — | 12 | [ficha][his] |
| F-HIS-09 | Detalle: apertura, carga y estados | HIS | No | F-HIS-19, F-RUT-06, F-RUT-08 | — | 9 | [ficha][his] |
| F-HIS-10 | Detalle: Resumen | HIS | No | F-RUT-18, F-HIS-05 | `session_stats_test` (indirecto) | 9 | [ficha][his] |
| F-HIS-11 | Detalle: Rendimiento por paso | HIS | Sí | F-WKT-06, F-HIS-05 | — | 9 | [ficha][his] |
| F-HIS-12 | Detalle: selector de métrica | HIS | No | F-HIS-13 | — | 9 | [ficha][his] |
| F-HIS-13 | Detalle: gráfico de la métrica (escala, inversión del split, ejes) | HIS | Sí | F-HIS-05, F-HIS-12, F-HIS-14, F-HIS-15 | — | 9 | [ficha][his] |
| F-HIS-14 | Detalle: bandas y separadores de pasos | HIS | Sí | F-WKT-06, F-HIS-13 | — | 9 | [ficha][his] |
| F-HIS-15 | Detalle: líneas objetivo por paso | HIS | Sí | F-RUT-06, F-RUT-20, F-HIS-13 | `routine_positions_test` (indirecto) | 9 | [ficha][his] |
| F-HIS-16 | Detalle: subida manual a Strava | HIS | No | F-STR-07, F-STR-17 | — | 12 | [ficha][his] |
| F-HIS-17 | Base local: archivo, esquema y versiones (4 tablas) | HIS·DB | No | — | — | 3 | [ficha][his] |
| F-HIS-18 | *Alias de F-RUT-08* | — | — | — | — | 3 | [ficha][his] |
| F-HIS-19 | Consultas de sesiones y telemetría | HIS·DB | No (salvo agregación) | F-HIS-17, F-RUT-18 | — | 9 | [ficha][his] |
| F-HIS-20 | Base separada en modo simulador (`rower_app_dev.db`) | HIS·DB·SIM | No | F-BLE-13, F-HIS-17 | — | 3 | [ficha][his] |
| F-HIS-21 | Reparación de sesiones incompletas (antes de subir en lote) | HIS | Sí | F-RUT-18, F-HIS-19, F-STR-11 | — | 13 | [ficha][his] |
| F-HIS-22 | *Alias de F-STR-15* | — | — | — | — | 13 | [ficha][his] |
| F-HIS-23 | *Alias de F-STR-13* | — | — | — | — | 13 | [ficha][his] |

### 2.7 STR — Strava y pantalla Perfil

| ID | Funcionalidad | Área | Pura | Depende de | Tests legacy | It. | Detalle |
|---|---|---|---|---|---|---|---|
| F-STR-01 | Disponibilidad de Strava según credenciales del build *(alias F-APP-04)* | STR | Sí (regla) | F-APP-02, F-APP-19 | — | 12 | [ficha][str] |
| F-STR-02 | Conectar con Strava (autorización OAuth) | STR | No | F-STR-01, F-STR-03, F-APP-17 | — | 12 | [ficha][str] |
| F-STR-03 | Sesión persistente y renovación de credenciales (V − 60 s) | STR | Parcial | F-STR-02 | — | 12 | [ficha][str] |
| F-STR-04 | Desconectar Strava | STR | No | F-STR-02, F-STR-17 | — | 12 | [ficha][str] |
| F-STR-05 | Preferencia de subida (`strava_upload_pref`: auto/ask/manual) | STR·PRF | No | — | — | 12 | [ficha][str] |
| F-STR-06 | Subida al terminar un entrenamiento *(alias F-WKT-22)* | STR | No | F-STR-05, F-STR-07, F-STR-18, F-WKT-20, F-WKT-21 | — | 12 | [ficha][str] |
| F-STR-07 | Subir una sesión (operación individual) | STR | No | F-STR-03, F-STR-08..10, F-HIS-19, F-RUT-06 | — | 12 | [ficha][str] |
| F-STR-08 | Envío, confirmación y duplicados ("duplicate of activity N") | STR | Parcial | F-STR-03, F-STR-09, F-STR-18 | — | 12 | [ficha][str] |
| F-STR-09 | Archivo de actividad TCX | STR | Sí | F-STR-10, F-WKT-06 | — | 12 | [ficha][str] |
| F-STR-10 | Metadatos de rutina ocultos en la descripción | STR | Sí | F-RUT-01, F-RUT-04, F-RUT-06 | — | 12 | [ficha][str] |
| F-STR-11 | Sincronizar a Strava (subida en lote + auto-reparación) | STR·PRF | Parcial | F-STR-07, F-STR-08, F-HIS-21 | `session_stats_test` (indirecto) | 13 | [ficha][str] |
| F-STR-12 | Sincronizar desde Strava (importación, Rowing/VirtualRow, 50 actividades) | STR·PRF | No | F-STR-03, F-STR-10, F-STR-13 | `session_stats_test` (indirecto) | 13 | [ficha][str] |
| F-STR-13 | Reconstrucción de la telemetría importada *(alias F-HIS-23)* | STR | Sí | F-STR-10, F-STR-12, F-RUT-18 | `session_stats_test` (indirecto) | 13 | [ficha][str] |
| F-STR-14 | Tarjeta "Subidas pendientes" | STR·PRF | No | F-STR-07, F-STR-16, F-HIS-05 | — | 13 | [ficha][str] |
| F-STR-15 | Mantenimiento: limpiar sesiones de Strava *(alias F-HIS-22)* | STR·PRF | No | F-STR-12, F-HIS-06 | — | 13 | [ficha][str] |
| F-STR-16 | Pantalla Perfil: estructura y estados | STR·PRF | No | F-STR-01..15 | — | 13 | [ficha][str] |
| F-STR-17 | Estado de sincronización en Historial y Detalle *(alias F-HIS-08)* | STR | No | F-STR-01, F-STR-07, F-HIS-07, F-HIS-09 | — | 12 | [ficha][str] |
| F-STR-18 | Strava en modo simulador (subidas bloqueadas en todo camino) | STR·SIM | No | F-BLE-13, F-STR-06, F-STR-08 | — | 12 | [ficha][str] |

### 2.8 APP — shell, tema, l10n y plataformas

| ID | Funcionalidad | Área | Pura | Depende de | Tests legacy | It. | Detalle |
|---|---|---|---|---|---|---|---|
| F-APP-01 | Arranque de la app y servicios activos desde el inicio | APP | No | F-APP-02, F-APP-14, F-BLE-13 | `widget_test` (vacío, no se porta) | 2 | [ficha][app] |
| F-APP-02 | Barra de pestañas inferior (4 o 5 pestañas, estado conservado) | APP | No | F-APP-10, F-STR-01 | — | 2 | [ficha][app] |
| F-APP-03 | Navegación secundaria: pantallas apiladas, diálogos y avisos | APP | No | F-APP-02 | — | 3 | [ficha][app] |
| F-APP-04 | *Alias de F-STR-01* | — | — | — | — | 12 | [ficha][app] |
| F-APP-05 | Tema visual oscuro y estilos base (`#0D1B2A`, `#1A2E45`, semilla `#0077B6`) | APP | No | — | — | 2 | [ficha][app] |
| F-APP-06 | Paleta de colores por métrica (contrato visual) | APP | Sí (datos) | — | — | 2 | [ficha][app] |
| F-APP-07 | *Alias de F-RUT-01* | — | — | — | — | 1 | [ficha][app] |
| F-APP-08 | Componentes compartidos sin uso: `MetricCard`, `IntervalTile` (**descarte**) *(alias F-RUT-19)* | APP | No | — | — | 2 | [ficha][app] |
| F-APP-09 | Idiomas soportados (es/en) y elección por idioma del sistema | APP | Sí (regla) | F-APP-10 | indirectos (`environment_picker_test`, `heart_rate_card_test`) | 1 | [ficha][app] |
| F-APP-10 | Catálogo de textos `.arb` (159 claves es/en) | APP | Sí (datos) | — | indirectos | 1 | [ficha][app] |
| F-APP-11 | Textos visibles fuera del catálogo (hardcodeados) | APP | Sí (datos) | F-APP-10 | indirectos (`series_panels_test`, `simulator_overlay_test`) | 14 | [ficha][app] |
| F-APP-12 | *Alias de F-WKT-11* | — | — | — | `series_panels_test` ("formatos") | 4a | [ficha][app] |
| F-APP-13 | Permisos de Bluetooth y privacidad por plataforma | APP | No | F-BLE-04 | — | 10a | [ficha][app] |
| F-APP-14 | Pantalla siempre encendida | APP | No | F-APP-01 | — | 4a | [ficha][app] |
| F-APP-15 | Modo inmersivo del sistema, orientación y ventana | APP | No | F-WKT-13 | indirectos (`heart_rate_card_test` :142, `simulator_overlay_test` :89) | 14 | [ficha][app] |
| F-APP-16 | Identidad: nombre, identificadores, versión, ícono y pantalla de arranque | APP | No | F-APP-05, F-APP-19 | — | 14 | [ficha][app] |
| F-APP-17 | Regreso a la app después del login externo (`rowmate://`) | APP | No | F-STR-02 | — | 12 | [ficha][app] |
| F-APP-18 | *Alias de F-BLE-13* | — | — | — | `simulator_overlay_test` | 2 | [ficha][app] |
| F-APP-19 | Compilación y distribución (CI/CD, firma, configuraciones) | APP | No | F-APP-16, F-BLE-13, F-STR-01 | — | 14 | [ficha][app] |

---

## 3. Discrepancias docs vs código (consolidado)

**Regla:** en todos los casos manda el código del legacy (es lo que validan sus tests). Cuando la diferencia abre una decisión de producto, la columna "Decisión" remite a §4. El detalle y las líneas exactas están en la sección "Diferencias" de cada inventario de detalle.

| ID | Tema | Los docs dicen | El código hace | Origen | Decisión |
|---|---|---|---|---|---|
| DC-01 | UUID de Rower Data | `0x2AD2` (CLAUDE.md, README, comentarios) | `0x2AD1`, el correcto en FTMS (`ftms_parser.dart:22`) | BLE | Se usa `0x2AD1` |
| DC-02 | Reconexión del remo | "cada 3 s hasta lograrlo" | Un solo intento a los 3 s (timeout 10 s) y se corta | BLE | Q-29 |
| DC-03 | Pantalla siempre encendida | "durante los entrenamientos" | Toda la app, desde el arranque, sin liberarse (`main.dart:42`) | BLE, WKT, APP | Q-56 |
| DC-04 | Navegación | "go_router" | Declarado y sin uso; pestañas + pantallas apiladas | BLE, RUT, HIS, APP | Sin impacto |
| DC-05 | `BleService` | "Singleton" | Instancia común creada por la app | BLE | Sin impacto |
| DC-06 | Borrado en cascada | "4 tablas con cascade deletes" | Nunca se activa; los pasos quedan huérfanos | RUT, HIS | Q-17 |
| DC-07 | Columnas "promedio" | `avg_power_watts`, `avg_stroke_rate` = promedio | Guardan el P99 | RUT, HIS, STR | Q-14 |
| DC-08 | "Percentil 99" del split | Percentil 99 | Da casi el split más lento; con n = 2 devuelve el menor | RUT, HIS | Q-14 |
| DC-09 | Auto-reparación (README) | Repara sesiones cortadas a mitad | La telemetría se escribe al final: esas sesiones no tienen puntos y se saltean | WKT, HIS, STR | Q-12 |
| DC-10 | Spec del simulador: panel | No menciona "Hora escena", "Hora real" ni "Simular caída del pulsómetro" | Existen | BLE | Se migran |
| DC-11 | Spec del simulador: datos | El tick emite pulso | El remo simulado emite pulso 0; el pulso viaja solo por el pulsómetro simulado | BLE | Código |
| DC-12 | Bloqueo de Strava en simulador | Solo en `workout_screen` | También en el inmersivo y los diálogos de completado; el botón del Detalle **no** está protegido | BLE, HIS, STR | Q-78 |
| DC-13 | Spec del simulador: tests | Lista de tests | Incompleta (recorte de objetivos, ruido ±5 %, pulso expuesto, ventana baja, hora, caída del pulsómetro) | BLE | Se portan todos |
| DC-14 | Bilingüismo | App localizada es/en | Muchos textos fijos en español (y "Unknown device", "Up next:" en inglés); claves `.arb` sin usar | BLE, WKT, HIS, STR, APP | Q-01 |
| DC-15 | Métricas decodificadas | README lista Calories | Se decodifican, pero Dispositivo no muestra calorías, tiempo ni paladas | BLE | Paridad |
| DC-16 | Smoke test | "smoke test" | `test/widget_test.dart` está vacío | BLE, APP | No se porta |
| DC-17 | Filtro de búsqueda del pulsómetro | "no se filtra por UUID en la app" | Filtra en Windows/Linux | HRM | Código |
| DC-18 | Error al suscribirse al pulsómetro | Lleva a reintentos de caída | Queda `disconnected` sin cadencia de caída (manual: error; automático: intento fallido) | HRM | Código |
| DC-19 | Nombre recordado del sensor | `platformName` | Nombre del sistema o el anunciado; si faltan ambos, conserva el anterior | HRM | Q-41 |
| DC-20 | Reintentos al arrancar | "6 veces, ~2 min" | 6 intentos en total (1 + 5 a 10 s), peor caso 140 s | HRM, APP | Código |
| DC-21 | Pulsómetro simulado | `autoConnect` conecta; `forget` corta | `autoConnect` no hace nada; `forget` solo cancela la reconexión pendiente | HRM | Código |
| DC-22 | Fusión del pulso | Nulo si el sensor no está `connected` | Acepta pulsos en cualquier estado y vuelve a 0 ante cualquier estado ≠ `connected`, solo si el sensor había emitido | HRM | Código |
| DC-23 | Chip de pulso del historial | `avg · max` | `{prom}/{máx}` | HRM, HIS | Código |
| DC-24 | Tests del pulsómetro | `heart_rate_service_test` | No existe; la lógica está en `reconnect_loop_test` | HRM | Código |
| DC-25 | Tarjeta del pulsómetro | Dentro de `device_screen`; "Pulsómetro · Sin sensor"; solo "Buscar" se deshabilita; conectando sin acciones; con BT apagado "queda detrás" | Archivo propio; título en mayúsculas y pista con "(Wear OS, Apple Watch)"; también se deshabilitan "Buscar otro" y "Conectar"; reintentando muestra "recordado · reconectando..." con botones; con BT apagado no se dibuja | HRM | Código |
| DC-26 | Comportamientos no documentados del pulsómetro | — | Conectar mientras conecta se ignora; sin BLE la búsqueda no hace nada; error nativo de escaneo termina en silencio; error de búsqueda se muestra como "Error al conectar"; el error se borra al conectar; el simulado no pasa por `connecting` | HRM | Código (Q-42, Q-47) |
| DC-27 | Objetivos de paso | Solo vatios y SPM | También split y 3 progresiones (SPM, W, split) | RUT, README | Código |
| DC-28 | Ejemplo "escalera de SPM" (README) | 5 tramos | 6: la última repetición incluye su descanso | RUT | Código |
| DC-29 | Identificador de serie | "UUID del grupo" | `g{epochMs}_{contador}` | RUT | Sin impacto visible |
| DC-30 | Meta del paso | "duración o distancia obligatoria" | No se exige: el editor crea pasos sin meta (`?`) que nunca terminan solos | RUT | Q-25 |
| DC-31 | Resumen y etiquetas de rutina | (App bilingüe) | Fijos en español: "1 series · 0 descansos" en la UI en inglés | RUT | Q-01 |
| DC-32 | Nombres de tipo de paso en el modelo | — | Nombres fijos en español sin uso; la UI usa claves `stepType*` | RUT, APP | No se migra |
| DC-33 | Reloj de pared | Centrado arriba a `padding.top + 90` | Sin marco, dentro de la barra de pasos y de la barra libre | WKT | Código |
| DC-34 | Formato de vueltas y repeticiones | `▸ 340 m · 1:15.0`; `Rep 4 · 1:54 · 741 m` | Tiempo grande + metros chicos; filas en columnas sin `·`; vueltas de la más reciente a la más vieja | WKT | Código |
| DC-35 | Inicio de los parciales | Recibe solo la distancia | También el reloj activo, referencia nueva al reanudar y saneamiento de saltos (negativos o > 10 m/s) | WKT | Código |
| DC-36 | Semáforo de objetivos | Rojo para vatios y SPM | También split; la tarjeta se pone blanca al cumplir; los vatios máximos nunca se evalúan | WKT | Código (Q-60 solo visual) |
| DC-37 | Último paso de la rutina | Los pasos hechos se evalúan | El último se evalúa pero su píldora queda "en curso" | WKT | Q-52 |
| DC-38 | Código muerto | Pantalla clásica y `shared/` reutilizables | `_FullscreenWorkoutPage`, `MetricCard`, `IntervalTile` y respaldos `?? '...'` sin uso | WKT, APP | Q-57 |
| DC-39 | Escena: velocidad y palada | Ratio 1:2; split > 0 alcanza | Exige también spm > 0; sin spm cierra el ciclo a 12 spm; paso por cuadro acotado a 0,1 s; ratio real 0,40/0,60 (1:1,5) | SCN | Código (Q-70) |
| DC-40 | Escena: hora y luz | Noche ≤ −0,1; luna con sombra | Noche ≤ −0,4 (≈ 19:34 a 4:26); noche cerrada 22-4 h con transición 4-5 h; elevación acotada a [0;1] + fuerza de luz (día 0,2-1,0, noche 0,45); estrellas graduales; luna sin sombra, disco 0,8× | SCN | Código (Q-62, Q-63) |
| DC-41 | Escena: cielo y horizonte | — | 7 nubes con parallax 0,0004·vel y opacidad por ambiente; siluetas con parallax por escenario (costa sin desplazamiento), altos 12/12/8/6 %; el horizonte recibe distancia y tiempo | SCN | Código (Q-65) |
| DC-42 | Escena: orilla | Tipo `gull`, `mountainCabin`; tope 40 descartando los más lejanos | Sin `gull` (gaviotas en la silueta de la costa); `cabin`; topes 120/60/200 con prioridad de hitos sobre boyas; puente con tablero 6-7,7 m y arco 17 m; playa reemplaza al acantilado cada 250 m; miniatura con todos los objetos a < 150 m | SCN | Código (Q-61) |
| DC-43 | Escena: bote y remero | Riggers ±0,8 en z ±0,3; tronco 0,55, cabeza 0,24; salpicadura 0,3 s; charcos 0,3→1,2 m | Barra por lado hasta x = ±0,85; tronco 0,62 m, cabeza 0,28 m; salpicadura con p < 0,08 (0,2 s a 24 spm); charcos 0,3→0,9 m ovalados; sombra óvalo plano 0,35 m opuesto al astro | SCN | Código |
| DC-44 | Escena: agua | Entradas `uSunY`, `uSunElevation`; espuma 50 % | Usa `uLight` y `uSternZ` (28 valores); ondas, brillo y espuma con otras fórmulas (espuma máx. 35 %); distancia módulo 4096 m; agua degradada con degradé de 3 tramos | SCN | Código (Q-68) |
| DC-45 | Escena: API y simulador | `hour`/`isPaused` | `hourOverride`; "Hora escena" muestra `real` y agrega "Hora real" | SCN | Código |
| DC-46 | Historial "solo lectura" | "Read-only list of completed sessions" | Lista todas (huérfanas sin terminar e importadas); el Detalle escribe (sube y guarda el id) | HIS, STR | Código |
| DC-47 | Ícono de Strava por sesión | En cada sesión | Solo en las sincronizadas y solo con Strava configurado; no hay "pendiente" | HIS, STR | Código |
| DC-48 | Rendimiento por paso | "cada punto = 5 s" | Falso en las importadas (~1 s) | HIS | Q-23 |
| DC-49 | "Limpiar sesiones de Strava" | Borra las "descargadas de Strava" | Borra toda sesión con id de Strava, también las grabadas en la app | HIS, STR | Q-71 |
| DC-50 | Claves del historial | `historyStatWatts`, `historyStatCalories` | No se usan; "Watts", "SPM", "Split" fijos | HIS, APP | Q-01 |
| DC-51 | Subida manual | Solo desde Detalle/Historial | También desde Perfil → Subidas pendientes | STR | Código |
| DC-52 | Metadatos: letra de trabajo | `k = work` | Trabajo escribe `w` (igual que calentamiento) y al leer vuelve como calentamiento | STR | Q-72 |
| DC-53 | Metadatos: tamaño | "~255 con wrapper" | Carga ≤ 200, comentario completo ≤ 217 | STR | Código |
| DC-54 | Metadatos: lectura | — | Rechaza el relleno `=`: ~2/3 de los metadatos no se reconocen | STR | Q-73 |
| DC-55 | Marcador de secreto | `YOUR_SECRET` | Solo `TU_CLIENT_SECRET` o vacío cuentan como "no configurado" | STR | Q-84 |
| DC-56 | Re-subir una sesión | Se vincula a la existente | La UI no permite re-subir; el vínculo solo ocurre ante "duplicate of activity N" | STR | Código |
| DC-57 | Perfil | Conexión + ajustes | También sincronización en ambos sentidos, pendientes y mantenimiento | STR | Código |
| DC-58 | Colores de la barra inferior | `#12243A`, `#00B4D8`, `#607D8B` | No se aplican; la barra usa los derivados de la semilla | APP | Código |
| DC-59 | Android `minSdk` | 21 | Mínimo por defecto del kit (`flutter.minSdkVersion` = 24, Android 7.0, con el Flutter 3.47.6 instalado); el 21 solo figura en la config de íconos | APP | Q-90 |
| DC-60 | Permisos iOS | `NSBluetoothAlwaysUsageDescription` | También `NSBluetoothPeripheralUsageDescription`; textos solo en español, sin mencionar el pulsómetro | APP | Q-89 |
| DC-61 | CI | "verifica los lints" | Ningún CI corre análisis ni tests | APP | It. 14 |
| DC-62 | Arranque en iOS | Imágenes con logo | Pantalla de arranque blanca (las imágenes no se usan) | APP | Q-86 |
| DC-63 | Identidad | "RowMate" | Windows: `rower_app`, empresa `com.example`, ícono de Flutter; Android: `com.example.rower_app` | APP | Q-85 |
| DC-64 | Ícono | `icon.png` | Es un JPEG | APP | It. 14 |
| DC-65 | Estructura | `features/` sin `profile` | Existe `features/profile/` | APP | Sin impacto |
| DC-66 | macOS | "anda sin configuración extra" | No registra `rowmate://` para el login de Strava | APP | Q-80 |
| DC-67 | Recargas del historial | Nada | Tocar Historial siempre recarga (con parpadeo) y volver del Detalle también | APP | Q-22 |
| DC-68 | Reconexión inicial del pulsómetro | — | Un fallo solo va al log | APP | Paridad |
| DC-69 | Codemagic | Equivalente a GitHub Actions | No genera el archivo de credenciales de Strava y falla si no existe | APP | It. 14 |
| DC-70 | Linux | `main.dart` prepara la base para Linux | No hay plataforma Linux | APP | Q-50 |

---

## 4. Preguntas abiertas y decisión de producto por defecto

**Cómo se aplica:** si el usuario no responde antes de la iteración afectada, se implementa la **decisión por defecto**. Tipos: **Paridad** (igual al legacy) · **Corrección** (defecto evidente del legacy; no cambia lo que el usuario quiere obtener) · **Cambio** (cambio de producto explícito, con motivo) · **Agregado** (comportamiento nuevo que no quita nada) · **Backlog** (fuera de la paridad; ver §6) · **Restricción** (condición de producto para el Architect). "Confirmar" = Sí cuando conviene que el usuario lo apruebe en el gate de la iteración.

### 4.1 Textos y localización

| ID | Pregunta | Origen | Decisión por defecto | Tipo | It. | Confirmar |
|---|---|---|---|---|---|---|
| Q-01 | ¿Se localizan los textos fijos (HUD, paneles, diálogos de terminar/completar, errores y "Unknown device"/"Rower" de Dispositivo, resumen y etiquetas de rutina, Watts/SPM/Split del historial, Mantenimiento y errores de Strava, textos de permiso)? | BLE Q4, RUT Q1, WKT Q1, HIS Q-09, STR, APP P-02/P-10, audit #11 | **Sí.** Claves nuevas es/en; el español queda **idéntico** al legacy (salvo Q-02) y se agrega el inglés. Cada iteración localiza los textos de sus pantallas y su `01-po-spec` fija la tabla exacta es/en de cada clave nueva; la It. 14 audita que no quede ninguno fijo. Las 15 claves nuevas de la It. 1 (resumen y etiquetas de rutina) están en [routines-models §3.1](inventory/routines-models.md) | Cambio | todas | Sí |
| Q-02 | ¿Se corrigen puntuación y plurales ("Iniciar entrenamiento?", "1 sesiones")? ¿Se unifica el voseo? | APP P-03, STR | Se corrigen signos de apertura y plurales (plural ICU `one`/`other` en es y en). No se reescribe el tono de los textos existentes (el voseo queda como está). Se aplica **desde la It. 1**: el catálogo se convierte ya corregido (§4.1.1) y las claves nuevas nacen con plural | Corrección | 1 (catálogo) y todas | No |
| Q-03 | ¿Se localizan el panel del simulador, "Remo simulado", "Pulsómetro simulado", "Hora escena"/"Hora real"? | BLE Q4, HRM Q9, SCN Q8 | No: quedan fijos en español (herramienta de desarrollo) | Paridad | 2, 6 | No |
| Q-04 | ¿Se localiza el diagnóstico BLE ("Show BLE debug", "RECEIVING"…)? | BLE Q4 | No: queda en inglés fijo (herramienta de soporte) | Paridad | 10 | No |
| Q-05 | ¿Selector de idioma dentro de la app? | APP P-13 | No: sigue al idioma del sistema, con respaldo en inglés | Paridad | 1 | No |
| Q-06 | ¿Decimales, miles y fecha según el idioma? | APP P-14, HIS Q-11 | No: formatos fijos del legacy (punto decimal, `dd/MM/yyyy  HH:mm`, 24 h) | Paridad | 4, 9 | No |
| Q-07 | ¿Unidades explícitas en la etiqueta de duración ("m" = minutos o metros)? | RUT Q2 | No | Paridad | 1 | No |
| Q-08 | ¿La pestaña "Workout" se llama así también en español? | APP P-04 | Sí (`navWorkout` = "Workout" en es) | Paridad | 2 | No |
| Q-09 | ¿Se unifica "Libre" (tarjeta) con "Entrenamiento libre" (título del Detalle)? | HIS Q-10 | No | Paridad | 9 | No |
| Q-10 | ¿Se mantiene el emoji 📈 en la etiqueta de objetivos? | RUT Q11 | Se mantiene la misma información; el Designer puede reemplazar el emoji por un ícono propio | Agregado (visual) | 1, 3 | No |

#### 4.1.1 Qué cambia en el catálogo al convertirlo en la It. 1 (Q-01 + Q-02)

El catálogo se convierte **una sola vez** en la It. 1 (F-APP-10). De las 159 claves del legacy, **155 quedan con el texto idéntico** en es y en. Cambian estas 4 (Q-02); el resto de los textos con `¿`/`¡` ya está bien en el legacy (`workoutFinishContent`, `workoutCompleted`, `routineDeleteContent`, `profileDisconnectContent`, `profileAskUploadDialog`):

| Clave | es legacy | es nuevo | en legacy | en nuevo | Dónde se ve |
|---|---|---|---|---|---|
| `workoutStartTitle` | `Iniciar entrenamiento?` | `¿Iniciar entrenamiento?` | `Start workout?` | sin cambio | F-WKT-12 (It. 4a) |
| `workoutStartContent` | `{summary}⏎⏎Iniciar el entrenamiento?` | `{summary}⏎⏎¿Iniciar el entrenamiento?` | `{summary}⏎⏎Start the workout?` | sin cambio | F-WKT-12 (It. 4a) |
| `profileSyncResult` | `{count} sesiones sincronizadas` | `{count, plural, one{# sesión sincronizada} other{# sesiones sincronizadas}}` | `{count} sessions synced` | `{count, plural, one{# session synced} other{# sessions synced}}` | F-STR-12 (It. 13) |
| `profileUploadResult` | `{count} sesiones subidas` | `{count, plural, one{# sesión subida} other{# sesiones subidas}}` | `{count} sessions uploaded` | `{count, plural, one{# session uploaded} other{# sessions uploaded}}` | F-STR-11 (It. 13) |

Resultado esperado de los plurales: `count` = 1 → `1 sesión sincronizada` / `1 session synced`; 0 → `0 sesiones sincronizadas` / `0 sessions synced`; 3 → `3 sesiones subidas` / `3 sessions uploaded`. Las claves y los parámetros no cambian (`count` sigue siendo entero). A estas 159 claves se suman las 15 nuevas de [routines-models §3.1](inventory/routines-models.md), así que el catálogo de la It. 1 tiene **174 claves** en cada idioma.

### 4.2 Datos, persistencia e historial

| ID | Pregunta | Origen | Decisión por defecto | Tipo | It. | Confirmar |
|---|---|---|---|---|---|---|
| Q-11 | ¿Se importan datos de la app Flutter instalada (base `rower_app.db` v1-v7, preferencias, tokens de Strava)? | RUT Q6, HIS Q-01, HRM Q10, APP P-05, STR | **No.** Instalación nueva: el usuario empareja el pulsómetro y conecta Strava una vez. Restricción: una sesión nueva nunca puede ser tomada por Strava como duplicada de una sesión legacy distinta (R-11). La importación queda **diseñada pero no implementada** (propuesta del Architect, aceptada). En Android no sería posible: la app nueva tiene otro identificador (`com.example.rower_app` en el legacy) y no puede leer los datos de la vieja. La app nueva nunca abre ni modifica la base del legacy (R-14) | Paridad + Restricción | 3, 12 | Sí |
| Q-12 | ¿Se guarda la telemetría de forma incremental para no perder la sesión ante un cierre de la app? | WKT Q10, HIS Q-06 | **Sí.** Los puntos quedan guardados durante la sesión. Una sesión cortada aparece en el historial con sus puntos y la reparación (F-HIS-21) la puede arreglar, como promete el README. Una sesión terminada normalmente queda idéntica a la del legacy | Agregado | 4 | Sí |
| Q-13 | ¿Qué ve el usuario ante un error de la base al iniciar, terminar o cargar? | WKT Q11, HIS Q-14 | Aviso no bloqueante y comprensible. El entrenamiento sigue en pantalla; la lista no queda en un spinner eterno | Agregado | 3, 4, 9 | No |
| Q-14 | ¿"Promedio" de W/SPM = P99 y split P99 (casi el más lento)? | RUT Q3, HIS Q-02 | **Paridad**: mismos números en historial y Strava. Se revisa después de la paridad (BKL-25) | Paridad | 1 | Sí |
| Q-15 | ¿Totales con contadores absolutos del monitor y tiempo del monitor (puede ser 0)? | WKT Q7/Q8, HIS Q-07 | Paridad. QA verifica en la It. 10 si el AMS-670B reinicia sus contadores al conectar | Paridad | 4, 10 | No |
| Q-16 | ¿Las líneas objetivo y el desglose usan la rutina de ese día (copia guardada)? | RUT Q5, HIS Q-08 | No: se recalculan con la rutina actual | Paridad | 9 | No |
| Q-17 | Borrar una rutina deja pasos huérfanos | DC-06 | Sin efecto visible: el nombre en las sesiones se conserva. El Architect puede limpiar huérfanos sin cambiar nada visible | Paridad | 3 | No |
| Q-18 | ¿Se normaliza `created_at` (hora local sin zona)? | RUT Q9 | El orden visible ("la más nueva arriba") se mantiene; el formato de guardado lo decide el Architect | Paridad | 3 | No |
| Q-19 | Doble toque en Guardar crea rutinas duplicadas | RUT Q8 | Se corrige: un toque, una rutina | Corrección | 3 | No |
| Q-20 | ¿Se agrega borrar sesión? | HIS Q-12 | No | Backlog | — | No |
| Q-21 | ¿El selector de métrica recuerda la última elegida? | HIS Q-13 | No | Paridad | 9 | No |
| Q-22 | Recarga del historial con parpadeo y sin refresco al terminar una sesión | APP P-12, D-15 | Recarga en los mismos momentos que el legacy **y además** al terminar una sesión. Sin parpadeo: la lista se ve mientras recarga | Corrección | 9 | No |
| Q-23 | Duración por paso = puntos × 5 (×5 en las importadas) | HIS Q-05 | Se calcula con el tiempo real del paso. Para las sesiones grabadas en la app el resultado es el mismo | Corrección | 9 | No |
| Q-24 | Gráfico de split: línea objetivo sin invertir y paradas dibujadas como el ritmo más rápido | HIS Q-03 | Se corrige: el objetivo de split se dibuja invertido como la serie, y los ceros no se dibujan como ritmo rápido | Corrección | 9 | No |
| Q-25 | ¿Validaciones en el diálogo de paso (0 s, sin meta, mín > máx, negativos)? | RUT Q4 | No se bloquea nada | Paridad | 3 | No |
| Q-26 | ¿Máximo de repeticiones? | RUT Q10 | Sin máximo | Paridad | 3 | No |
| Q-92 | ¿Qué rango de números aceptan los objetivos de un paso y sus progresiones (vatios, SPM, split)? | QA-V-11 (cierre de la It. 1) | El legacy no pone tope: lee los campos con `int.tryParse` (`routine_editor_screen.dart:535-559`) y suma la progresión en enteros de 64 bits (`routine.dart:50-61`). La app nueva calcula en un rango más chico (hasta 2 147 483 647): una serie ×3 con vatios mín. 2 000 000 000 y progresión +100 000 000 da 2,0e9 / 2,1e9 / 2,2e9 en el legacy y en la app nueva no se puede expandir. **Por defecto: ningún número que el editor deje guardar puede hacer fallar la expansión, el resumen ni el entrenamiento.** La spec de la It. 3 fija con el Architect un tope por campo muy por encima de cualquier uso real, y el editor no deja guardar un paso cuyo valor, o el de alguna repetición con su progresión, quede fuera (con un mensaje es/en). Debajo del tope todo es idéntico al legacy, sin otras validaciones (Q-25). Alternativa: paridad estricta (mismo rango que el legacy) | Corrección | 3 | Sí (sin urgencia, gate de la It. 3) |
| Q-27 | ¿Mejoras al editor (reordenar, duplicar, deshacer, aviso de cambios sin guardar)? | RUT Q7 | Después de la paridad | Backlog | — | No |
| Q-28 | ¿Se muestran las calorías en el historial? | HIS Q-09 | No | Paridad | 9 | No |

### 4.3 Remo BLE

| ID | Pregunta | Origen | Decisión por defecto | Tipo | It. | Confirmar |
|---|---|---|---|---|---|---|
| Q-29 | Reconexión del remo: ¿un intento (código) o reintentar hasta reconectar (docs)? | BLE Q1 | **Reintenta cada 3 s** hasta reconectar o hasta que el usuario actúe (Desconectar, Buscar, elegir un remo). Motivo: es lo que prometen docs, comentario y README; el corte es un defecto | Cambio | 10 | Sí |
| Q-30 | ¿Recordar el último remo y conectarse solo al arrancar? | BLE Q2 | No | Backlog | — | No |
| Q-31 | Watchdog de 5 s y pausas del remero | BLE Q3 | Misma regla (desconecta sin reintentar) **más** un mensaje claro "El monitor dejó de enviar datos". QA verifica con el AMS-670B si notifica durante las pausas | Agregado | 10 | No |
| Q-32 | "Activar Bluetooth" no hace nada fuera de Android | BLE Q5, APP P-11 | Fuera de Android abre los ajustes de Bluetooth del sistema donde la plataforma lo permita; en Android sigue pidiendo encenderlo | Cambio (menor) | 10 | No |
| Q-33 | ¿Confirmar al desconectar a mano el remo o el pulsómetro durante una sesión? | BLE Q6, HRM Q3 | Sin confirmación. Durante una sesión el inmersivo tapa la pestaña Dispositivo, así que el caso no se da | Paridad | 2, 11 | No |
| Q-34 | ¿Avisar la caída del remo durante un entrenamiento? | BLE Q7, WKT Q14 | **Sí**: aviso discreto "Reconectando con {remo}…" sobre el HUD y confirmación al volver. Las métricas y el reloj se comportan como en el legacy | Agregado | 10 | Sí |
| Q-35 | Equipos FTMS que no son remos aparecen y "conectan" sin datos | BLE Q8 | Se mantienen en la lista | Paridad | 10 | No |
| Q-36 | ¿Se mantiene la exclusión de búsquedas remo/pulsómetro? | BLE Q9 | Sí | Paridad | 10 | No |
| Q-37 | El diagnóstico es inaccesible sin scroll (pantallas grandes) | BLE Q10 | Se agrega un gesto alternativo (mantener presionada la cabecera del remo); el de 3 desplazamientos sigue | Agregado | 10 | No |
| Q-38 | Valores FTMS "no disponible" (0xFFFF) y potencia negativa | BLE Q11 | Se muestran crudos | Paridad | 1, 10 | No |
| Q-39 | ¿Botón para detener la búsqueda de 10 s? | BLE Q12 | No | Paridad | 10 | No |
| Q-40 | Simulador en builds de distribución y modo "remo simulado + pulsómetro real" | BLE Q13, HRM Q9 | Nunca activable en distribución (R-01). Reemplaza remo y pulsómetro a la vez | Restricción + Paridad | 2 | No |

### 4.4 Pulsómetro

| ID | Pregunta | Origen | Decisión por defecto | Tipo | It. | Confirmar |
|---|---|---|---|---|---|---|
| Q-41 | Un sensor nuevo sin nombre hereda el nombre del anterior | HRM Q1 | Se corrige: se muestra `hrmUnknownSensor` | Corrección | 11 | No |
| Q-42 | `hrmConnectError` muestra el texto técnico y se usa para errores de búsqueda | HRM Q2 | Mismos disparadores; mensaje humano por situación (búsqueda / conexión) y el detalle técnico como texto secundario | Cambio (menor) | 11 | No |
| Q-43 | ¿Reanudar la reconexión si "Buscar otro" termina sin elegir? | HRM Q4 | No | Paridad | 11 | No |
| Q-44 | La lista desaparece al vencer la búsqueda sin elección | HRM Q5 | Se mantiene | Paridad | 11 | No |
| Q-45 | ¿"Cancelar" durante una conexión manual (hasta 15 s)? | HRM Q6 | No | Backlog | — | No |
| Q-46 | ¿Indicador explícito de "sin contacto"? | HRM Q7, WKT Q15 | No: `--` en la tarjeta y la tarjeta BPM del HUD desaparece | Backlog | — | No |
| Q-47 | Plataforma sin Bluetooth LE: "Buscar" no hace nada | HRM Q8, BLE-04.9 | Se agrega un mensaje claro en Dispositivo | Agregado | 10 | No |
| Q-48 | ¿Se valida el rango del pulso (0-65535)? | HRM Q12 | No | Paridad | 1 | No |
| Q-49 | Zonas de FC, batería 0x180F, varios sensores, pulso del sensor en la grilla del remo | HRM Q13 | Fuera de la paridad | Backlog | — | No |
| Q-50 | Linux | HRM Q11, D-18 | Fuera de alcance: plataformas objetivo = Android, iOS, Windows y macOS | Restricción | — | No |

### 4.5 Entrenamiento y HUD

| ID | Pregunta | Origen | Decisión por defecto | Tipo | It. | Confirmar |
|---|---|---|---|---|---|---|
| Q-51 | Paso por distancia: la cuenta regresiva muestra el largo total y la píldora no avanza | WKT Q2 | Se mantiene | Paridad | 4 | No |
| Q-52 | El último paso queda "en curso" sin mostrar su resultado | WKT Q3 | Se mantiene | Paridad | 4 | No |
| Q-53 | Carreras al terminar a mano: el diálogo de completado aparece un instante y el "preguntar" de Strava se cierra solo | WKT Q4/Q5, STR Q5 | Se corrige: al terminar a mano nunca aparece el diálogo de completado; el diálogo "preguntar" queda hasta que el usuario responda | Corrección | 4, 12 | No |
| Q-54 | Tocar subir antes de que termine el guardado sube la sesión anterior | WKT Q6 | Se corrige: la subida siempre usa la sesión recién terminada (espera el guardado) | Corrección | 12 | No |
| Q-55 | Paso por distancia trabado tras un reinicio del monitor | WKT Q9 | Se corrige con el mismo saneamiento de saltos que los parciales (negativos o > 10 m/s) | Corrección | 4 | No |
| Q-56 | Pantalla siempre encendida: ¿toda la app o solo la sesión? | WKT Q12, APP P-01 | Toda la app, desde el arranque. QA verifica que se conserve al volver del segundo plano | Paridad | 4 | No |
| Q-57 | ¿Se migran la pantalla clásica, `MetricCard` e `IntervalTile`? | WKT Q13, APP §7 | No (código muerto) | Paridad | 2, 4 | No |
| Q-58 | Se pierde la fracción de segundo en cada pausa | WKT Q16 | Se mantiene | Paridad | 4 | No |
| Q-59 | ¿Píldoras del mismo ancho o proporcionales a la duración? | WKT Q17 | Mismo ancho | Paridad | 4 | No |
| Q-60 | Con objetivo activo la tarjeta pierde su color semántico (valor y etiqueta blancos/rojos) | audit #12 | La etiqueta y el borde conservan el color de la métrica; el **valor** pasa a blanco (cumple) o al rojo de fuera de objetivo: `#FF4444` del contrato, mostrado en el HUD con su variante clara del mismo matiz `#FF5F59` más una flecha ▲/▼ si se aprueba D-ART-01 (ROADMAP §6). El significado no cambia | Cambio (visual) | 4a | No |
| Q-91 | ¿Qué pasa con un entrenamiento si la app pasa a segundo plano (otra app, pantalla bloqueada, ventana minimizada)? | QA-P-06 | **Paridad: la sesión sigue.** El reloj activo avanza, la conexión con el remo se mantiene (el monitor no sale del modo Bluetooth), el pulso y la telemetría se siguen registrando y la sesión se guarda igual. Al volver, el HUD muestra el estado actual sin saltos raros. Restricción para el Architect: en Unity la app pausada no corre su lógica, así que esto no sale solo | Paridad + Restricción | 4a (definición), 10a y 10b (verificación en dispositivo) | No |

### 4.6 Escena

| ID | Pregunta | Origen | Decisión por defecto | Tipo | It. | Confirmar |
|---|---|---|---|---|---|---|
| Q-61 | ¿Mismas posiciones exactas del paisaje que el legacy? | SCN Q1 | **Sí (revisada tras el gate del plan).** Se reproduce el generador aleatorio del legacy, así que a la misma distancia aparece el mismo paisaje que en el legacy: mismas posiciones, tamaños y variantes de cada objeto de orilla, además de los hitos fijos en los metros exactos. QA lo verifica con vectores dorados del oráculo Dart (QA_PLAN_REVIEW §4) | Paridad | 6a, 8 | No |
| Q-62 | Estrellas casi invisibles también de día | SCN Q2 | Se limitan a ambiente ≤ 0,85 (desde el atardecer) | Corrección | 6 | No |
| Q-63 | Salto del sol a la luna a las 19:34 y 4:26 | SCN Q3 | Puesta y salida graduales con los mismos umbrales de día y noche | Agregado (EXR) | 6 | No |
| Q-64 | ¿Carteles de la regata con la distancia del monitor? | SCN Q4 | No: distancia de escena, como el legacy | Paridad | 8 | No |
| Q-65 | El horizonte se desliza de costado al avanzar | SCN Q5 | En 3D el horizonte se acerca lentamente; se conserva la sensación de avance | Cambio | 6, 8 | No |
| Q-66 | Modos de cámara, botes fantasma/bots, sonido, clima, línea de llegada, personalización | SCN Q6/Q10, audit §13 | Fuera de la paridad. La cámara de persecución por defecto puede "respirar" (movimiento sutil) como mejora EXR | Backlog | — | Sí |
| Q-67 | ¿Sigue haciendo falta el agua degradada? | SCN Q7 | Sí: la app tiene que ser usable aunque el equipo no soporte el agua avanzada (R-04) | Restricción | 6 | No |
| Q-68 | Salto del patrón de olas cada 4096 m | SCN Q9 | No se exige; el agua no pierde calidad en sesiones de más de 10 km | Restricción | 6 | No |
| Q-69 | ¿La lógica pura de la escena entra en la It. 1? | SCN Q10 | No: va al Core puro, con tests en dotnet, en las It. 6-8 (cada pieza con su escena) | Decisión de plan | 6-8 | No |
| Q-70 | Ritmo de la palada fijo (0,40/0,60) | audit §12.2 | El ritmo visible depende del SPM (drive casi constante, recuperación más corta al subir el ritmo). La fase canónica y sus tests se conservan; distancia y métricas no cambian | Agregado (EXR) | 7 | No |

### 4.7 Strava

| ID | Pregunta | Origen | Decisión por defecto | Tipo | It. | Confirmar |
|---|---|---|---|---|---|---|
| Q-71 | "Limpiar sesiones de Strava" borra también las sesiones grabadas en la app y subidas | STR Q1, HIS Q-04 | **Borra solo las importadas desde Strava.** Motivo: es lo que dice el propio diálogo ("descargadas de Strava") y evita la pérdida de datos | Cambio | 13 | Sí |
| Q-72 | Letra `w` duplicada en los metadatos (trabajo = calentamiento) | STR Q2 | Al escribir, trabajo usa su propia letra; la lectura sigue aceptando lo que subió el legacy | Corrección | 12 | No |
| Q-73 | La lectura rechaza el relleno `=` | STR Q3 | Se acepta: recupera las rutinas de actividades ya subidas | Corrección | 13 | No |
| Q-74 | ¿Strava guarda las Notes del TCX como descripción? ¿Se ve el comentario ROWMATE? | STR Q4 | Se verifica con una cuenta real en la It. 12 | Verificación | 12 | No |
| Q-75 | Calorías de Strava pisadas con 0 al importar | STR Q6 | Se conservan las de Strava | Corrección | 13 | No |
| Q-76 | ¿Límite de 50 actividades sin paginar? | STR Q7 | Se mantiene | Paridad | 13 | No |
| Q-77 | La lista de pendientes queda desactualizada | STR Q9 | Se actualiza después de cada subida o sincronización | Corrección | 13 | No |
| Q-78 | Botones de subida visibles en simulador que siempre fallan (incluido el del Detalle) | STR Q10, HIS Q-15 | Se ocultan en modo simulador, en todo camino | Cambio (menor) | 12 | No |
| Q-79 | Credencial de cliente de Strava | STR Q12, APP P-09 | Se inyecta al compilar y nunca se versiona (R-02) | Restricción | 12, 14 | No |
| Q-80 | ¿El login funciona en Windows y macOS (sin `rowmate://` registrado en macOS)? | STR Q13, APP P-08 | Requisito: el login funciona en las 4 plataformas | Restricción | 12 | No |
| Q-81 | ¿Aviso de "volver a conectar" ante credenciales revocadas? | STR Q14 | No | Backlog | — | No |
| Q-82 | Doble sesión local cuando se agota la espera de confirmación | STR Q15 | Se mantiene | Paridad | 13 | No |
| Q-83 | ¿Strava funciona en el APK release (permiso de internet)? | APP P-07 | Requisito: funciona en release en todas las plataformas | Restricción | 12 | No |
| Q-84 | Marcadores de secreto ("YOUR_SECRET" del README vs "TU_CLIENT_SECRET") | DC-55 | Strava existe solo con credenciales reales: vacío o cualquier marcador de ejemplo = no configurado | Corrección | 12 | No |

### 4.8 Plataformas e identidad

| ID | Pregunta | Origen | Decisión por defecto | Tipo | It. | Confirmar |
|---|---|---|---|---|---|---|
| Q-85 | Identificadores y nombre (Windows "rower_app", `com.example`; título macOS "APP_NAME") | APP P-05/P-06/P-18 | "RowMate" en todas las plataformas (título, ejecutable, ícono). Identificadores propios nuevos, sin `com.example`. Hasta confirmarla, ids provisionales: `com.iguisoft.rowmate` en Android y `com.iguisoft.rowmate.unity` en iOS y macOS, distinto del legacy (R-14). En Windows el motor no tiene identificador (D-02 de la It. 1): la app es el producto `RowMate` de `iguisoft`, con datos en `AppData/LocalLow/iguisoft/RowMate`, y `com.iguisoft.rowmate` queda reservado para el paquete instalable (MSIX) de la It. 14 ([01-po-spec AC-APP-16-02](../iterations/01-fundaciones/01-po-spec.md)). Al confirmarla, el usuario elige en iOS/macOS entre reemplazar al legacy (mismo id) o convivir (id nuevo) | Corrección | 1 (provisorios), 14 (definitivos) | Sí |
| Q-86 | ¿Pantalla de arranque de iOS oscura con logo? | APP P-15 | Sí, oscura con marca, igual en todas las plataformas | Agregado (EXR) | 14 | No |
| Q-87 | Diálogo de permiso Bluetooth de iOS al abrir la app la primera vez | APP P-16 | Se mantiene; QA lo verifica en un dispositivo | Paridad | 10b | No |
| Q-88 | En escritorio, ¿recordar tamaño y posición de la ventana y pantalla completa real en el inmersivo? | APP P-17 | No: ventana inicial 1280×720 (Windows) y 800×600 (macOS) | Backlog | — | No |
| Q-89 | Textos de permiso de iOS/macOS solo en español y sin mencionar el pulsómetro | APP P-10, D-08 | Se localizan (es/en) y mencionan el remo y el pulsómetro | Corrección | 10b | No |
| Q-90 | ¿Cuál es el Android mínimo? | DC-59, QA-P-18 | **Android 8.0 (API 26), solo 64 bits (ARM64).** El legacy instala desde Android 7.0 (API 24) y en equipos de 32 bits. Se pierden Android 7.x y los teléfonos de 32 bits, una porción muy chica del parque activo; a cambio, la pila nueva y el Bluetooth LE son más estables. El nivel de calidad Bajo se piensa para Android 8+ ARM64 | Cambio | 1 (configuración), 14 (verificación) | Sí |

---

## 5. Restricciones de producto para el Unity Architect

| ID | Restricción | Fuente |
|---|---|---|
| R-01 | El simulador **nunca** es activable en un build de distribución. Reemplaza remo y pulsómetro a la vez, usa una base separada y bloquea toda subida a Strava (cualquier punto nuevo de subida incluido). Login e importación siguen funcionando | F-BLE-13, F-HIS-20, F-STR-18 |
| R-02 | Las credenciales de Strava se inyectan al compilar y no se versionan. Sin credenciales reales, Strava desaparece entera (4 pestañas). Nadie lee ni copia `lib/core/strava/strava_secrets.dart` del legacy | F-STR-01, F-APP-19 |
| R-03 | La app funciona completa sin internet; Strava es opcional | STR §0 |
| R-04 | Modo de agua degradada obligatorio: la escena sigue siendo usable si el equipo no soporta la avanzada | F-SCN-13 |
| R-05 | Plataformas: Android (8.0+, ARM64, Q-90), iOS, Windows y macOS. Linux fuera de alcance | decisión del usuario |
| R-06 | Bluetooth con plugins nativos propios y siempre una implementación simulada intercambiable. La UI no sabe si el servicio es real o simulado | F-BLE-13, decisión del usuario |
| R-07 | En iOS la conexión Bluetooth sigue con la app en segundo plano (como `bluetooth-central` del legacy) | BLE-04.11 |
| R-08 | Assets 3D solo gratuitos y detrás de *asset slots* intercambiables, cada uno con un reemplazo procedural digno | decisión del usuario, audit §13 |
| R-09 | Toda regla pura del índice (§2, Pura = Sí/Parcial) se verifica con `dotnet test` fuera de Unity; todo test del legacy se porta | CLAUDE.md |
| R-10 | HUD legible a 1,5 m en un teléfono sobre el remo. Contraste del valor contra el peor fondo posible: **≥ 7:1** en blanco y en las métricas que no son rojas; **≥ 4,5:1** en la familia roja (pulso y fuera de objetivo), que además lleva una señal de forma redundante (flecha ▲/▼ y borde) para no depender solo del color. La semántica de color (7 de métrica, 4 de tipo de paso, 3 de fase, `#FF4444` fuera de objetivo, `#FC4C02` Strava) no cambia de matiz ni de significado; en el HUD se permiten variantes más claras **del mismo matiz** (`*.hud` de ART_BIBLE §2.3). Redacción según D-ART-01; si el usuario la rechaza en el gate de la It. 4a, vuelve a "≥ 7:1 en todos los valores" y el Designer ajusta la familia roja | audit §9, D-ART-01 |
| R-11 | Una sesión nueva nunca puede ser rechazada por Strava como duplicada de una sesión legacy distinta (los ids externos del legacy son `rowmate_<id>`) | Q-11 |
| R-12 | 60 fps en un teléfono de gama media durante el entrenamiento | audit §10 |
| R-13 | Los números y reglas del legacy se reproducen tal cual (umbrales, tiempos, fórmulas, claves de preferencias): el comportamiento del legacy es el contrato | rowmate-po |
| R-14 | Mientras el usuario siga usando el legacy, instalar un build de la app nueva **nunca reemplaza ni borra** la app legacy ni sus datos en ese equipo, y la app nueva nunca abre ni modifica la base del legacy (Q-11). Hoy el legacy usa `com.iguisoft.rowmate` en iOS y macOS: un build nuevo con ese mismo identificador lo pisaría al instalarse. Hasta que se confirme Q-85 (It. 14), los builds de iOS y macOS usan un identificador provisional **distinto** del legacy (`com.iguisoft.rowmate.unity`); en Android vale el provisional `com.iguisoft.rowmate`, que no choca (el legacy es `com.example.rower_app`). En Windows el motor no tiene identificador (D-02 de la It. 1): el producto `RowMate` de `iguisoft` guarda sus datos en `AppData/LocalLow/iguisoft/RowMate`, distinta de las carpetas del legacy, y `com.iguisoft.rowmate` queda reservado para el MSIX de la It. 14. Q-85 decide el definitivo: el mismo del legacy (la app nueva reemplaza a la vieja y su historial queda inaccesible, porque no se importa) o uno nuevo (conviven) | rowmate-po, QA_PLAN_REVIEW §4 (Q-11, Q-85) |

---

## 6. Descartes y backlog post-paridad

### 6.1 Descartes (código muerto del legacy; se cierran como "no se migra")

| ID | Qué | It. que lo cierra | Verificación |
|---|---|---|---|
| F-APP-08 (+ F-RUT-19) | `MetricCard` e `IntervalTile` (sin uso) | 2 | QA confirma que ninguna pantalla del legacy los usa y que la app nueva no los necesita |
| F-WKT-23 | Pantalla clásica `_FullscreenWorkoutPage` (`workout_screen.dart:203-990`) | 4a | QA confirma que no es alcanzable en el legacy |

También quedan fuera: nombres fijos de `StepType.label`, "es descanso", "por tiempo/por distancia" y duración estimada de la rutina sin uso (RUT §7); respaldos `?? '...'` inalcanzables; colores de barra del tema sin efecto; dependencia `go_router`.

### 6.2 Backlog post-paridad (no son IDs del inventario; no entran en el roadmap de paridad)

| ID | Mejora | Origen |
|---|---|---|
| BKL-01 | Botes fantasma (récord propio), bots y pacers | EXR, audit §10 |
| BKL-02 | Cámaras adicionales (lateral, cinemática, POV) y selector | SCN Q6, audit §13 |
| BKL-03 | Sonido: agua, catch, carro, ambiente por escenario | audit §10 |
| BKL-04 | Clima, segmentos con arcos y línea de llegada | SCN Q6 |
| BKL-05 | Minimapa y escala configurable del HUD | audit §10 |
| BKL-06 | Personalización de remero, bote y remos | SCN Q10 |
| BKL-07 | Zonas de FC, batería del sensor, varios sensores, pulso del sensor en la grilla del remo | HRM Q13 |
| BKL-08 | Recordar el remo y conectarse solo al arrancar | BLE Q2 |
| BKL-09 | Aviso de equipos FTMS que no son remos | BLE Q8 |
| BKL-10 | "Cancelar" en la conexión manual del pulsómetro | HRM Q6 |
| BKL-11 | Indicador de "sin contacto" del pulsómetro | HRM Q7 |
| BKL-12 | Editor: reordenar, duplicar, deshacer, aviso de cambios sin guardar, validaciones | RUT Q4/Q7 |
| BKL-13 | Borrar, editar, renombrar y compartir sesiones | HIS §6 |
| BKL-14 | Filtros, búsqueda, totales, récords | HIS §6 |
| BKL-15 | Exportar/importar historial a archivo | HIS §6 |
| BKL-16 | Parciales, repeticiones y cumplimiento de objetivos en el historial | HIS §6 |
| BKL-17 | Copia de la rutina remada dentro de la sesión | Q-16 |
| BKL-18 | Recordar ventana y pantalla completa real en escritorio | Q-88 |
| BKL-19 | Aviso de credenciales de Strava revocadas | Q-81 |
| BKL-20 | Modo "remo simulado + pulsómetro real" | Q-40 |
| BKL-21 | Curva de fuerza y largo de palada | EXR |
| BKL-22 | Ver el escenario usado en el historial | HIS §6 |
| BKL-23 | Paginación de la importación de Strava | Q-76 |
| BKL-24 | Pantalla siempre encendida solo en Dispositivo y entrenamiento | F-APP-14 |
| BKL-25 | Revisión de la semántica P99 de "promedio" y del split del resumen | Q-14 |

[ble]: inventory/rower-ble-device-sim.md
[hrm]: inventory/heart-rate.md
[rut]: inventory/routines-models.md
[wkt]: inventory/workout-engine-hud.md
[scn]: inventory/immersive-scene.md
[his]: inventory/history-persistence.md
[str]: inventory/strava-profile.md
[app]: inventory/app-shell-platform.md

---

## Cambios por QA (plan)

Correcciones por el gate de armonía del plan ([QA_PLAN_REVIEW.md](../QA_PLAN_REVIEW.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-04 | Q-01 y Q-02 sin ambigüedad: Q-02 se aplica desde la It. 1. Nueva §4.1.1 con las 4 claves del catálogo que cambian (es/en, legacy y nuevo, con plural ICU) y el total de 174 claves. La tabla es/en de las 15 claves nuevas (resumen y etiquetas) está en [routines-models §3.1](inventory/routines-models.md) |
| QA-P-13 | R-10 reescrita según D-ART-01: ≥ 7:1 en blanco y métricas no rojas, ≥ 4,5:1 + flecha y borde en la familia roja, y variantes `*.hud` del mismo matiz. Q-60 apunta a la variante HUD del rojo. Tabla decisión → gate en ROADMAP §6 |
| QA-P-18 | Nueva Q-90: Android mínimo 8.0 (API 26) y solo ARM64. Se pierden Android 7.x y los equipos de 32 bits (el legacy instala desde API 24). DC-59 y R-05 actualizadas |
| QA-P-19 (E4) | Líneas de tests: 2 129 (no 2 062) en 26 archivos |
| QA-P-06 (parte PO) | Nueva Q-91: en segundo plano la sesión sigue (paridad), y queda como restricción para el Architect |
| P-03, P-05, P-06 | Columna "It." del índice actualizada a 4a/4b, 6a/6b y 10a (la 10b no es dueña de ningún ID: verifica los criterios [Apple], ROADMAP §0 regla 8). Q-87 y Q-89 pasan a la 10b |
| Aclaraciones del Architect | Q-61 revisada: el paisaje es idéntico al del legacy, porque se reproduce su generador aleatorio. Q-11: la importación queda diseñada y sin implementar (en Android no es posible). Nueva R-14 y Q-85 ajustada: los ids provisionales `com.iguisoft.rowmate` valen en Android y Windows; en iOS y macOS el provisional tiene que ser distinto del legacy para no pisarlo |

## Cambios al cerrar la It. 1

Por la verificación de QA de la It. 1 ([05-qa-verification §6](../iterations/01-fundaciones/05-qa-verification.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-V-10 | Q-85 y R-14 alineadas con AC-APP-16-02 (D-02 aceptada): `com.iguisoft.rowmate` solo en Android; `com.iguisoft.rowmate.unity` en iOS y macOS; Windows sin identificador en el motor (producto `RowMate` de `iguisoft`, datos en `AppData/LocalLow/iguisoft/RowMate`) y `com.iguisoft.rowmate` reservado para el MSIX de la It. 14. Cierra el pendiente de [01-po-spec §14](../iterations/01-fundaciones/01-po-spec.md) |
| QA-V-11 | Nueva Q-92 (rango de los números del paso y sus progresiones): por defecto, ningún número que el editor deje guardar hace fallar la expansión; la spec de la It. 3 fija el tope por campo y su mensaje |
