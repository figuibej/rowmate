# It. 3 — Verificación de QA (después de implementar)

> **Ronda 3 (2026-10-28, 07:21-07:47): APROBADO CON OBSERVACIONES.** QA-V3-11 queda cerrado: `capture-window.ps1` ya arranca sin `-Arguments` en Windows PowerShell 5.1, y la prueba nueva lo corre de verdad. No quedan issues bloqueantes ni mayores. Siguen abiertos 10 menores: de QA-V3-02 a QA-V3-10, y QA-V3-12. Detalle en [Ronda 3](#ronda-3), al final.
>
> **Ronda 2 (06:19-06:47):** cerró QA-V3-01 y encontró QA-V3-11 ([Ronda 2](#ronda-2)). Las secciones 0 a 9 son la ronda 1.

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-28 (05:03-05:40) |
| Iteración | 3 · `03-rutinas` |
| Entrada | [01-po-spec](01-po-spec.md) (102 AC: 91 nuevos + 11 de iteraciones anteriores; 28 CAP, 12 DEMO, 3 VID), [02-tech-design](02-tech-design.md) (§5 arnés, §6 tests y matriz), [03-qa-harmony](03-qa-harmony.md), [04-design-notes](04-design-notes.md) (incluida §16, WP-D), [00-deuda-it2](00-deuda-it2.md), código en `Assets/RowMate/`, legacy [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, `3ae0f3a`; `lib/features/routines/*`, `lib/core/database/database_service.dart`, `docs/image-04.png`; no se leyó `lib/core/strava/strava_secrets.dart`) y el SDK de Flutter (`<flutter-sdk>/packages/flutter/lib/src/services/text_formatter.dart`, `widgets/editable_text.dart`) |
| Herramientas | .NET 8 (`tools/dotnet/run-tests.ps1`), Unity **6000.3.27f1** (Windows Standalone Mono; **sin** los módulos Windows IL2CPP ni Android), `tools/unity/run-unity-tests.ps1`, `tools/unity/capture-snapshots.ps1`, `tools/build/build.ps1`, scripts propios en `<scratch>/it3/qa-tools/` (comparación de PNG, cuadros de video con PyAV, lectura de la base con `sqlite3` de Python 3.14 / SQLite 3.50.4) |
| Legacy | `git -C ../rowmate-legacy status --porcelain` **vacío** al empezar y al terminar |
| Repo | Sin commits. Ninguna corrida dejó cambios en `git status` (comparado antes y después de los builds); `ProjectSettings/ProjectSettings.asset` con el mismo SHA-1 (`0922f776…`) y sin `ROWMATE_SIMULATOR`; `Library/RowMate/` vacía; sin `Assets/Resources` |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.**

- **Todo lo automatizable está en verde**: dotnet **1595 pruebas (1578 pasan, 0 fallan, 17 ignoradas)**; Unity **EditMode 1475 / 1459 / 0 / 16** y **con simulador 1626 / 1610 / 0 / 16**; **PlayMode 187 / 187** y **con simulador 234 / 234**. La matriz automática da **377 de 377 IDs en PASA** (los 102 de la It. 3 incluidos) y las 248 pruebas que nombra la matriz de 02-tech-design §6.3 existen y pasan (dos con otro nombre, §3).
- **Arnés `it3`**: 47 tomas, 0 con problemas, 78 sondas de aspecto sin fallas. La corrida de QA dio **los mismos bytes que el repo en los 47 archivos** (solo cambian las duraciones de `manifest.json`): la evidencia corresponde al código actual. Miré las 44 PNG una por una y los 3 videos cuadro a cuadro en los momentos que pide la spec.
- **La condición de la It. 2 queda cumplida: AC-BLE-11-02 en PASS** (tres PlayMode que reproducen la prueba de QA y el VID-03 de la It. 2 regenerado sin `Rower` en ningún cuadro). Los **15 issues de la deuda de la It. 2** están resueltos como dice [00-deuda-it2](00-deuda-it2.md) (§5), con una salvedad menor de motivos de `[Ignore]` (QA-V3-05).
- **Persistencia real en un player de Windows** (build de simulador recién compilado, Mono): ida y vuelta entre dos procesos por la composición real (store → repositorio → hilo de la base → SQLite nativo), **cierre forzado del proceso justo después del *commit*** (al reabrir, la rutina está completa) y humo de la base. El build de simulador usa `rower_app_dev.db`; el normal crea `rower_app.db` vacía en `LocalLow/iguisoft/RowMate` e **ignora `ROWMATE_DATA_DIR`**. Barra de título oscura en los dos builds. Esquema v8 leído con una herramienta propia de SQLite: igual al DDL v7 del legacy más `origin` y el índice.
- **Lo que no se pudo en la PC**: manejar la ventana real con mouse y teclado (crear la rutina de la demo con la UI, atajos, acentos). Había avisos del Firewall de Windows pendientes, uno por cada build de desarrollo nuevo, que oscurecen la pantalla y no dejan llegar la entrada a las otras ventanas (QA-V3-08). Además, mandar teclas al escritorio del usuario es riesgoso. Queda NO VERIFICABLE con motivo; la demo entera la cubren DEMO-01 a DEMO-12 (UI real en el arnés, con `Reopen()`) y las PlayMode.
- **AC: 101 PASS, 0 FAIL, 1 NO VERIFICABLE** (AC-RUT-11-10, solo Android). De los 101, **13 pasan en lo verificable y tienen una parte de equipo** (Android, la ventana real, lector de pantalla) que queda NV.
- **10 issues: 0 bloqueantes, 1 mayor, 9 menores.** El mayor (QA-V3-01) es de herramienta: `capture-cap27.ps1` no puede producir CAP-27 nunca (su base sintética la rechaza la guardia de `TempDatabase`), así que CAP-27 no está; no afecta a ningún AC, porque la barra oscura se verificó en los dos builds.

---

## 1. Qué se corrió (evidencia)

Carpetas propias en `<scratch>/it3/`: `qa-r1` (dotnet), `qa-r1m2` (dotnet + matriz con Unity), `qa-unity` (Unity), `qa-snap-it3`, `qa-snap-it2`, `qa-snap-it2b` (arnés, con `-Out` para no pisar la evidencia del repo), `qa-build`, `qa-build-normal`, `qa-build-logs` (builds), `qa-data*`, `qa-dev-shots` (equipo). Una sola instancia de Unity por vez, en serie. No había Editor abierto (`Temp/UnityLockfile` no existía); el `unity.exe serve` de la Hub y un `RowMate.exe` de la It. 2 que ya estaba abierto (no usa base) no se tocaron.

### 1.1 dotnet (`run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it3/qa-r1`)

```
Build succeeded.    0 Warning(s)    0 Error(s)
RowMate.Tests.Simulator:  Total tests: 151   Passed: 151
RowMate.Tests.EditMode:   Total tests: 1444  Passed: 1427  Skipped: 17
[run-tests] matriz: … (PASA 312, FALLA 0, IGNORADO 0, PENDIENTE UNITY 65, PENDIENTE WP-B 0, SIN TEST 0)
exit=0
```

Con los XML de Unity (`-UnityResults <scratch>/it3/qa-unity`, carpeta `qa-r1m2`, corrida con Unity cerrado): **PASA 377, FALLA 0, IGNORADO 0, PENDIENTE UNITY 0, PENDIENTE WP-B 0, SIN TEST 0**; los mismos 1595 / 1578 / 0 / 17. Una corrida intermedia que coincidió con el arnés dio 18 ignoradas: `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` queda inconclusa a propósito mientras el define está puesto. No es una falla; la corrida válida es la de Unity cerrado.

Línea base al empezar la It. 3 ([00-deuda-it2 §9](00-deuda-it2.md)): 1273 / 1256 / 0 / 17 → **+322 pruebas**.

### 1.2 Unity 6000.3.27f1

| Suite | Comando | Resultado | Antes (00-deuda-it2 §9.1) |
|---|---|---|---|
| EditMode | `run-unity-tests.ps1 -Platform EditMode` | **total 1475, pasan 1459, fallan 0, ignorados 16**, exit 0 (34 s) | 1149 / 1133 / 0 / 16 |
| EditMode + simulador | `… -Simulator` | **total 1626, pasan 1610, fallan 0, ignorados 16, inconclusos 0**; Enable → 0, Restore → 0 (52 s) | 1298 / 1282 / 0 / 16 |
| PlayMode | `-Platform PlayMode` | **187 / 187 pasan** (1 min 47 s) | 55 / 55 |
| PlayMode + simulador | `-Platform PlayMode -Simulator` | **234 / 234 pasan** (2 min 23 s) | 89 / 89 |

- **Conteos dotnet ↔ Unity (AC-BASE-08).** Unity EditMode = 1440 de `Tests/EditMode` + 35 de `Tests/UnityEditMode`; con simulador, + 151 de `Tests/Simulator`, el mismo número que en dotnet. 1626 − 1595 = 31 = 35 − 4 pruebas que solo existen en `net8.0`. Ignoradas: 17 en dotnet y 16 en Unity (`ProjectOpensWithoutCompileErrors` solo corre dentro de Unity).
- **Rendimiento (QA-G3-21)**, en el registro de PlayMode: `[PRF] 54 rutinas a la vista en 195 ms` y `[PRF] hilo principal: lista de 54 5.2 ms, editor de 20 pasos 0.8 ms, Tick durante el guardado 0.00 ms` (con simulador: 192 ms; 5.1 / 0.9 / 0.00 ms). El umbral declarado es 16 ms.

### 1.3 Arnés de capturas

| Corrida | Resultado |
|---|---|
| `capture-snapshots.ps1 -Set it3 -Out <scratch>/it3/qa-snap-it3` | exit 0 en 1 min 04 s: `listo: 47 tomas, 0 con problemas, 0 sondas de aspecto que fallan`. Contra `03-rutinas/snapshots/` del repo: **las 47 tomas (44 PNG y 3 MP4) e `INDEX.md` iguales byte a byte**; `manifest.json` solo difiere en 5 `durationMs` |
| `capture-snapshots.ps1 -Set it2 -Out <scratch>/it3/qa-snap-it2` (justo después de la de `it3`) | exit 0: `41 tomas, 0 con problemas, 0 sondas de aspecto que fallan`. Contra el repo: 25 PNG iguales y **13 PNG con diferencias de ≤ 2 por canal en 1 a 9 píxeles sueltos** (bordes de glifos de las cifras de la grilla y del panel), más los 3 MP4 |
| La misma, otra vez (`qa-snap-it2b`) | **igual al repo byte a byte** en las 41 tomas. El primer resultado depende de lo que corrió antes en otro proceso del Editor: QA-V3-06 |
| `INDEX.md` y `manifest.json` (los dos conjuntos) | Sin rutas de la PC ni nombres de usuario: búsqueda de `Users`, `AppData`, `Temp`, `scratch` y `LocalLow` en el texto y en los binarios, 0 resultados |

**Las capturas de la It. 2 no cambiaron sin motivo.** Comparé las 44 versionadas (`HEAD`, recuperadas de Git LFS) con las del repo de hoy, regeneradas por WP-B. Las diferencias son las que explica [00-deuda-it2 §7](00-deuda-it2.md): tokens del HUD del Designer (≤ 3 por canal, en 24 tomas), CAP-05 sin la barra de desplazamiento del teléfono (QA-V2-13), VID-03 con el panel (QA-V2-15), `INDEX.md`, `manifest.json` y `rt1-measurement.json`. Se suma **CAP-21b**, que ahora muestra la pantalla real de Rutinas con la base vacía, porque Rutinas dejó de ser provisoria (AC-APP-02-08). El catálogo y el `INDEX.md` lo dicen: "Rutinas (desde la It. 3 la pantalla real, base vacía)". Iguales byte a byte: CAP-03, -09, -16, -17, -20a a -20e, -21a, -21c y -23.

### 1.4 Builds de Windows (recién compilados por QA, salida en el scratchpad)

| Build | Resultado |
|---|---|
| Simulador, Mono (`build.ps1 -Target Windows -Simulator -Out <scratch>/it3/qa-build`) | `[BUILD] Windows simulator Mono2x Development` · `PerformanceTestRunCleaner: 4 entradas …` · **Success** en 27 s |
| Normal, Mono (`-Out <scratch>/it3/qa-build-normal`) | **Success** en 37 s · `[BUILD] BuildOutputInspector: sin rastros del simulador, de diagnósticos ni de tests.` En `Managed/`: `RowMate.{App,Bootstrap,Core,Infrastructure,Platform,Presentation,Scene}.dll` y `Gilzoide.SqliteNet.dll`; binario nativo `Plugins/x86_64/gilzoide-sqlite-net.dll` (y ARM64) |
| Búsqueda propia de rastros en el build normal (UTF-8 y UTF-16, todos los archivos) | **0** para `ROWMATE_DATA_DIR`, `-rowmate-db-smoke`, `-rowmate-db-roundtrip`, `Ida y vuelta`, `Remo simulado`, `Pulsómetro simulado`, `SIMULADOR`, `RowMate.Simulator`, `InlineDbExecutor`, `RoutineSeeds`, `ScriptedRoutineRepository`, `FakeClipboard`, `FakeSoftKeyboard`, `SequenceClock`, `-rowmate-color-probe`, `-rowmate-ui-pipeline` y `PerformanceTestRunInfo`. Aparecen, sin camino que las use: la constante `rower_app_dev.db` de `DatabaseFiles` (Infrastructure es pura; `BuildFlags.IsSimulator` es `const false`) y el texto de `DatabaseSmoke`, inalcanzable sin el argumento, que el build normal no tiene. `TempDatabase` aparece solo dentro del binario de SQLite (un símbolo propio de SQLite, no la clase de pruebas) |
| Normal, IL2CPP (`-Backend IL2CPP`) | **3** en segundos: `[BUILD] NO VERIFICABLE: falta el módulo IL2CPP de Windows en este Editor.`; log propio `build-windows-normal-il2cpp.log` (QA-V2-03) |
| Android | NO VERIFICABLE: no está el módulo (`PlaybackEngines` solo tiene `windowsstandalonesupport`, con variantes Mono) |

Después de los tres builds: `git status` igual que antes, sin `Assets/Resources`, sin el define.

### 1.5 En la PC (Device-Win, Mono, escala 100 %)

| Qué | Evidencia |
|---|---|
| Build de simulador con `ROWMATE_DATA_DIR` = `<scratch>/it3/qa-data` | Ventana `RowMate` de 1280 × 720, barra de título **oscura** (Windows con las apps en Oscuro: `AppsUseLightTheme` = 0, que no cambié). Registro: `[APP] RowMate 1.0.0 (1) · WindowsPlayer · simulador=sí`, `[PLATFORM] barra de título oscura (AppsUseLightTheme)`, `[APP] base rower_app_dev.db (carpeta de desarrollo)`, `[DB] base rower_app_dev.db creada (v8)`. En la carpeta: **solo `rower_app_dev.db`**. Ninguna línea del registro propio lleva una ruta |
| Ida y vuelta entre dos procesos (`-rowmate-db-roundtrip`, sobre una base sembrada con `Escalera SPM` por `SnapshotCli.SeedDatabase`) | 1.ª corrida, exit 0: `[DB] base rower_app_dev.db abierta (v8)`, `[DB] ida y vuelta: guardada`. 2.ª, exit 0: `[DB] ida y vuelta: sigue después de reabrir (2 pasos, 2 rutinas)`. La base después: `Ida y vuelta` con `created_at` `2026-10-28T08:26:09.062Z` (UTC con 3 decimales, Q-18) y `Escalera SPM` intacta (4 pasos; la serie con `group_repeat_count` 3 y progresión de SPM 2) |
| **Cierre forzado** (AC-RUT-08-07): el proceso se mata apenas aparece `ida y vuelta: guardada` en el registro, sobre una carpeta nueva | El proceso no llegó a cerrar solo: queda el `-wal` de 12 392 bytes, sin *checkpoint*. Al reabrir: `[DB] ida y vuelta: sigue después de reabrir (2 pasos, 1 rutinas)`. Lo confirmado en el *commit* sobrevive |
| Humo de la base (`-rowmate-db-smoke <carpeta>`, R3-01) | exit 0: `[DB] base smoke.db creada (v8)`, `[DB] humo: ok`; la subcarpeta temporal se borró |
| Build normal con `ROWMATE_DATA_DIR` apuntando a una carpeta señuelo | Barra de título oscura con `RowMate`, sin cinta, sin llave y sin "Development Build". Registro: `[APP] … simulador=no`, `[APP] base rower_app.db`, `[DB] base rower_app.db creada (v8)`. La carpeta señuelo quedó **vacía** (el build normal no lee la variable) y apareció **`rower_app.db` en `LocalLow/iguisoft/RowMate`**: 28 672 bytes, `user_version` 8, `wal`, `integrity_check` ok, 0 filas en las 4 tablas. No apareció `rower_app_dev.db`. Al cerrar no quedaron ni `-wal` ni `-shm` |
| Esquema (AC-HIS-17-02), leído en solo lectura con `sqlite3` de Python | `routines` 4 columnas, `interval_steps` 15, `workout_sessions` 12 (con `origin TEXT NOT NULL DEFAULT 'app'` al final), `data_points` 12; el texto de cada `CREATE TABLE` es el del legacy (`database_service.dart:75-139`), columna por columna (tipos, `NOT NULL` y `DEFAULT`), con las dos `FOREIGN KEY … ON DELETE CASCADE`; `sqlite_sequence`; único índice `idx_data_points_session` sobre `data_points(session_id, elapsed_seconds)`; `user_version` 8 |
| **No se pudo**: manejar la ventana con mouse y teclado | Los clics en la ventana no llegaban a la app. Al abrir el build de desarrollo, Windows mostró el aviso del Firewall ("Windows Security"), y había dos más pendientes de corridas anteriores. Esos avisos oscurecen toda la pantalla (medido: 0,55 de brillo en las capturas, también en la barra de título) y retienen la entrada. No los contesté: cualquiera de los dos botones cambia la configuración del Firewall. Tampoco mandé teclas: la ventana no tomaba el primer plano y las teclas habrían ido a otra aplicación del usuario. Ver QA-V3-08 |

### 1.6 Bases del usuario y del legacy (AC-HIS-20-04, AC-HIS-17-03)

Foto de las bases (tamaño, fecha y SHA-256) **antes** de correr nada (05:03), **después** de todas las corridas automáticas (05:27) y **al final**:

| Lugar | Antes | Después de dotnet, las 4 suites, los 3 arneses, la semilla y los builds | Final |
|---|---|---|---|
| Carpeta de datos de la app (`%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`) | sin `rower_app.db` ni `rower_app_dev.db` | **igual** (sin bases) | igual: sin bases. La única que hubo la creó, a propósito, la verificación de AC-HIS-17-01 con el build normal (§1.5); la revisé en una copia y la borré |
| Bases del legacy (`../rowmate-legacy/.dart_tool/sqflite_common_ffi/databases/` y la del build `Debug`) | 3 archivos: `rower_app.db` `67DB9CA6…` (2026-10-01), `rower_app_dev.db` `AD939A2B…` y `70C99D03…` (2026-10-02) | igual | igual |
| Preferencias del legacy (`%APPDATA%\com.example\rower_app\shared_preferences.json`) | `29D5F31A…` (2026-10-02) | igual | igual |

`<temp>/RowMate.Tests/` quedó vacía: ninguna base de prueba quedó sin borrar. Unity guarda su `TestResults.xml` en la carpeta del Editor (`LocalLow/iguisoft/RowMate`, la misma que la del build normal en esta PC); no es una base y la app no lo abre.

### 1.7 Limpieza

- Borré `rower_app.db` de la carpeta real. La creó el build normal de QA y antes no existía.
- Cerré todos los procesos que lancé: dos players de simulador, uno normal y los de ida y vuelta, cierre forzado y humo. No quedó ningún Editor.
- En el sistema queda algo que no puedo deshacer: los builds de desarrollo nuevos disparan el aviso del Firewall de Windows. Mientras el aviso no se contesta, Windows crea reglas entrantes de **bloqueo** para el `.exe`. Así quedaron dos para el build de QA en `<scratch>`. Las reglas viejas de otras corridas, la mayoría de permiso, ya estaban. Los tres avisos siguen en pantalla (QA-V3-08).

---

## 2. Capturas: revisión de las 44 PNG y los 3 videos

Todas a la densidad del perfil: TV a 1080 × 2400 (2,625), L640 y W1280 a 1. Fuentes e íconos definitivos, sin cuadrados vacíos. `LayoutAudit`: 0 desbordes y 0 textos cortados en las 47 tomas. Todas las comprobaciones del catálogo pasan, y las 78 sondas de aspecto dan dentro de tolerancia. Frente a `docs/image-04.png` del legacy, CAP-10 y CAP-11 tienen la misma estructura: encabezado de la serie con `×N` y tacho a la derecha, pasos con sangría, pie centrado `+ Add step to series` / `+ Agregar paso a serie`, lápiz y cruz a la derecha de cada fila, lista con 12 dp a los costados y campos con 16 dp, como el legacy.

| Captura | Qué tiene que verse (spec §7.3-§7.5) | Resultado |
|---|---|---|
| CAP-01 | Vacío legible, `+` entero, llave 16 dp arriba alineada, cinta, Rutinas elegida | **OK** (sondas 6/6) |
| CAP-02 | Solo el indicador; `+` visible | **OK** |
| CAP-03 | Error, `Reintentar`, `+`, sin indicador | **OK** |
| CAP-04 | 4 tarjetas en orden; resúmenes; mini-barras 8 / 14 / 1 / 7 con sus colores; sin barra de desplazamiento | **OK.** Conté los tramos a ojo, uno por uno: 8 (amarillo, rosa, verde ×3 pares, azul), 14, 1, 7 |
| CAP-05 | Lo mismo en inglés | **OK.** `1 interval · 1 rest`, `2 intervals · 1 rest`, `1 step`, `4 intervals · 3 rests` |
| CAP-06 | L640 al final: la última tarjeta entera sobre el `+` y la llave | **OK** |
| CAP-07 | Menú junto al `⋮` con `Editar` y `Eliminar` en rojo | **OK.** Debajo del botón y con el borde derecho alineado (lo comprueba el catálogo con ± 1) |
| CAP-08 | Velo, diálogo, `Eliminar` `#D32F2F`; cinta y llave sin oscurecer | **OK** |
| CAP-09 | Editor nuevo; flecha bajo la cinta; sin barra de pestañas; llave a 80 dp del borde inferior | **OK** |
| CAP-10 | `Escalera SPM` con la serie, subtítulos con `␣␣·␣␣`, tendencia, variantes legibles, dos hojas | **OK** (15/15 sondas; tendencia `#4CC9F0`) |
| CAP-11 | Lo mismo en W1280, comparable con `image-04.png` | **OK.** La llave tapa en reposo la cruz de la 4.ª fila (QA-V3-09) |
| CAP-12a / b | Los 11 títulos y subtítulos de AC-RUT-12-06, `×1` sin hojas y `×2` con una, sin "…" | **OK.** Los comparé uno por uno con la tabla de la spec; la fila 9 pasa a dos líneas |
| CAP-13 | Dos bloques con 16 dp entre el pie y el encabezado | **OK** |
| CAP-14 | `Nuevo paso`, mosaicos 2 × 2, sin progresiones, vista previa `0s` | **OK** |
| CAP-15a / b | Precarga `5`/`0`/`18`; ayudas `Ej: 5` y `Ej: -2 (más rápido)` sin foco, etiqueta arriba (DV-19) | **OK** |
| CAP-16 | `Por distancia`, `Metros` `500`, vista previa `500m` | **OK.** El cursor queda al principio (`|500`), detalle del arnés (QA-V3-04) |
| CAP-17 | `Repeticiones`, `Cantidad` `3` con foco, cursor al final (teléfono) | **OK** |
| CAP-18 / 19 / 28 | Aviso entero abajo, debajo de la llave; el editor sigue | **OK** los tres |
| CAP-20a / b | Error de campo y error de repetición en rojo, debajo del campo que lo causa | **OK.** `Fuera de rango: hasta ±9999` en dos líneas, sin "…" |
| CAP-21 | `Máximo 999`, diálogo abierto | **OK** |
| CAP-22 | L640: barra y campos enteros, un solo desplazamiento | **OK.** En reposo la llave tapa `Serie` (QA-V3-09) |
| CAP-23 | Diálogo compacto, mosaicos en una fila, botones fijos | **OK** |
| CAP-24a / b | Inglés completo | **OK.** Todos los textos de la spec, al carácter |
| CAP-25a / b | Letra 1,3×, nada cortado | **OK** |
| CAP-26 | Menú con `Cortar`, `Copiar`, `Pegar` y sin `Seleccionar todo` | **OK** |
| CAP-27 | Ventana del build normal | **No está.** Es solo a pedido y, además, la herramienta no puede tomarla (QA-V3-01). La barra oscura se verificó en los dos builds (§1.5) |
| DEMO-01 a 12 | La secuencia de la spec §7.4 | **OK**, con una observación. DEMO-10 es igual a DEMO-09 byte a byte (reabrir sobre la misma base) y DEMO-12 igual a DEMO-01. En DEMO-05 no se ven las progresiones, porque el contenido quedó arriba (QA-V3-03) |
| VID-01 (10 s, 300 cuadros) | Diálogo, fila que crece, serie que se despliega, lo borrado que se desvanece y colapsa, nada salta | **Parcial.** Todo bien hasta los 6,5 s: la cruz funde la fila y el hueco se cierra. Pero el tacho de la última serie (8,000 s) la saca en el cuadro 0 y el vacío aparece entero, sin colapso ni fundido. Es el defecto que encontró el Designer (04-design-notes §16.6) y sigue (QA-V3-02) |
| VID-02 (8 s, 240) | El editor entra y sale; menú y diálogo sobre el velo; la tarjeta colapsa y las de abajo suben | **OK.** La llave baja y sube con su resorte al apilar y desapilar el editor |
| VID-03 (8 s, 240) | Un solo aviso que se reinicia y se va 4 s después del último toque | **OK.** Medí la luminancia de la zona del aviso cuadro por cuadro: aparece en el cuadro 15 (0,5 s), nunca se va entre toques, empieza a salir en el 196 (6,53 s) y ya no está en el 199 |

---

## 3. Matriz AC → test → resultado (102)

Suites: [d] dotnet + Unity EditMode, [s] con simulador, [u] Unity EditMode, [p] PlayMode, [ps] PlayMode con simulador, [cap] captura, [win] PC, [qa] revisión. Los nombres de las pruebas son los de 02-tech-design §6.3. Comprobé que las 248 existen y pasan: `RPT.PrefillAndFocus` está partida en `PrefillAndFocusOnDesktop` / `OnPhone`, y `StepTypeTests.SelectorOrder` se llama `FourTypesInSelectorOrderWithStorageNames`. **PASS\*** quiere decir que pasa en lo verificable; la parte que se nombra queda NV.

### 3.1 F-RUT-08 (+ F-HIS-18) · F-RUT-09 (15)

| AC | Verif. | Evidencia | Resultado |
|---|---|---|---|
| RUT-08-01 | d | `RRT.InsertKeepsTheFourteenAttributesAndText`, `InsertNumbersStepsFromZero`, también contra el SQLite de gilzoide en Unity EditMode | PASS |
| RUT-08-02 | d | `RRT.UpdateReplacesStepsAndKeepsCreatedAt`, `UpdateWithoutChangesIsIdentical`, `UpdateLeavesTheOthers` | PASS |
| RUT-08-03 | d | `RRT.DeleteRemovesTheRoutineAndItsSteps`, `NewRoutineNeverSeesDeletedSteps` | PASS |
| RUT-08-04 | d | `RRT.GetByIdReturnsStepsInOrder`, `GetByIdOfMissingOrDeletedIsNull` | PASS |
| RUT-08-05 | d win | `RRT.ListIsNewestFirst`, `RST.CreatedAtIsUtcAcrossATimeZoneChange`, `EditKeepsDateAndPlace`; en el player, `created_at` en UTC con 3 decimales | PASS |
| RUT-08-06 | d p | `SIG.SameMillisecondGivesDistinctIds`, `RRT.SeriesSurviveTheRoundTrip`, `RPS.SeriesBlocksSurviveReopen` | PASS |
| RUT-08-07 | d win | `RRT.InsertIsAllOrNothing`, `UpdateIsAllOrNothing` (*trigger*); `DSC.WriteConnectionIsWalFullAndNoForeignKeys` en los dos SQLite; cierre forzado del player justo después del *commit* (§1.5) | PASS |
| RUT-08-08 | d p | `RRT.UnknownTypeFailsTheLoadWithoutWriting`, `Int32OverflowFailsTheLoadWithoutWriting`, `RST.UnknownTypeGivesLoadFailed`, `RLT.UnknownTypeShowsTheLoadError` | PASS |
| RUT-08-09 | p cap win | `RPS.ReopenShowsTheSame`; DEMO-10 = DEMO-09 byte a byte; ida y vuelta entre dos procesos del player (§1.5) | PASS\* (en la ventana real, con la UI: NV, §1.5) |
| RUT-09-01 | d p | `RST.LoadsOnceAtStart`, `ACT.StartLoadsRoutines`, `RLT.SecondVisitDoesNotShowTheIndicator` | PASS |
| RUT-09-02 | d p | `RST.CreateGoesFirst`, `UpdateKeepsPosition`, `DeleteKeepsOrder`, `RLT.CreateEditDeleteInPlace` | PASS |
| RUT-09-03 | d p | `RET.SaveWhileSavingIsIgnored`, `SaveAfterSavedIsIgnored`, `RED.DoubleTapSaveCreatesOne`, `TapAndEnterCreateOne`, `EditSavesOnce`, `TapDuringTheExitCreatesNothing` (toque a los 50 ms con el editor saliendo), `EnterDuringTheExitDoesNothing` | PASS |
| RUT-09-04 | d p cap | `RST.LoadFailureThenRetry`, `RLT.LoadErrorAndRetry`, `DeviceTabStillWorksWithLoadError`; CAP-03 | PASS |
| RUT-09-05 | d p cap | `RET.SaveFailureKeepsEverything`, `RED.SaveErrorKeepsTheEditorOpen`, `BackDuringASlowSaveClosesAndTheSaveLands`; CAP-28 | PASS |
| RUT-09-06 | d p | `RST.DeleteFailureKeepsTheRoutine`, `RLT.DeleteErrorKeepsTheCard`, `DeleteRemovesWhenDone` | PASS |

### 3.2 F-RUT-10 · F-RUT-11 (21)

| AC | Verif. | Evidencia | Resultado |
|---|---|---|---|
| RUT-10-01 | p cap | `RLT.TabTitleWithoutActionsAndFab`; CAP-01, CAP-04 | PASS |
| RUT-10-02 | p cap | `RLT.LoadingShowsOnlyTheIndicator`, `FastLoadNeverShowsTheIndicator`, `EmptyStateTextsAndColors`; CAP-01, CAP-02 | PASS |
| RUT-10-03 | d p cap | `RCM.DescriptionOnlyWhenNotEmpty`, `RLT.CardContent`, `LongNameAndDescriptionWrap`, `LongWordWithoutSpacesWraps`, `UserTextIsShownAsWritten`; CAP-04, CAP-06 | PASS |
| RUT-10-04 | p ps cap | `RLT.FabOpensANewEditor`, `RSL.WrenchNeverCoversTheFab`, `FabTappableEverywhere`, `SimulatorOverlayTests.WrenchSitsAboveTheFabInRoutines`; CAP-01 | PASS |
| RUT-10-05 | p | `RLT.TapCardOpensItsEditor` | PASS |
| RUT-10-06 | p cap | `NAV.CardMenuEditAndDelete`, `MenuClosesOnOutsideEscapeAndBack`, `MenuButtonTooltip`, `MenuItemTwiceOpensOneDialog`; CAP-07 | PASS |
| RUT-10-07 | p cap | `NAV.DeleteDialogOnlyConfirmDeletes`, `DialogButtonTwiceActsOnce`; CAP-08, DEMO-11 | PASS |
| RUT-10-08 | qa p | `RLT.NoOtherActions`; en las capturas no hay búsqueda, orden, deslizar ni deshacer | PASS |
| RUT-10-09 | d p ps cap | `BOT.PositionsOfTheDesignNotes`, `RLT.ThirtyRoutinesScrollAboveTheFab`, `ScrollPositionSurvivesEditorAndTabs`, `RSL.LastCardEndsAboveTheWrench`; CAP-06 | PASS |
| RUT-10-10 | d p cap | `RCM.SummariesEsEn`, `RLT.SummariesOnScreen`; CAP-04, CAP-05 | PASS |
| RUT-10-11 | d p cap | `RSB.FixtureSegments`, `ImpossibleRepeatsNeverExpand`, `SBL.WidthCapacityOverflowAndEmptyTrack`, `RLT.StructureBarStaysInOneLine`, `ImpossibleRoutinesLoadAndRespond`; CAP-04, DEMO-07 (8), DEMO-09 (10) | PASS |
| RUT-11-01 | p cap | `RED.AppBarNewAndEdit`; CAP-09, CAP-10, CAP-24a | PASS |
| RUT-11-02 | p cap | `RED.FieldsAndPrefill`, `TXT.SingleLineDropsPastedNewlines`, `DescriptionTwoLinesEnterAndScroll`, `EnterInASingleLineFieldBlurs`; CAP-09 | PASS (QA-V3-07: además de `\n`, quita `\r` y `\t`) |
| RUT-11-03 | d p | `RTT.CountIsTheTemplate`, `RED.StepsCountUpdatesAtOnce` | PASS |
| RUT-11-04 | p cap | `RED.EmptyStepsText`; CAP-09 | PASS |
| RUT-11-05 | d p cap | `RET.ValidationOrder`, `OutOfRangeStepBlocksTheSave`, `LooseStepProgressionsDoNotBlockTheSave`, `ZeroAndNegativeRepeatsDoNotBlockTheSave`, `RED.ValidationToasts`; CAP-18, CAP-19 | PASS |
| RUT-11-06 | d p | `RET.SaveTrimsRenumbersAndKeepsId`, `DTT.TrimLikeDart`, `RED.SaveClosesAndTheListShowsIt` | PASS |
| RUT-11-07 | d p | `RET.NoUniquenessNoMaxLength`, `RED.SameNameTwiceAndFiveHundredChars` | PASS |
| RUT-11-08 | p eq | `NAV.BackDiscardsWithoutAsking`, `EscapeDoesNotCloseTheEditor`, `RED.BackDuringASlowSaveClosesAndTheSaveLands` | PASS\* ("Atrás" real de Android: NV) |
| RUT-11-09 | p cap eq | `TXT.ContextMenuItemsByState`, `CutCopyPasteWithTheTestClipboard`, `TabSelectsAllClickPlacesTheCaret`, `DescriptionDoesNotSelectAll`, `NoMenuWithoutActions`; CAP-26 | PASS\* (en la ventana real, con el teclado del sistema: atajos, teclas muertas y AltGr. NV, §1.5) |
| RUT-11-10 | eq | Solo [Device-And]. La parte simulada pasa (`KAV.FocusedFieldStaysAboveTheKeyboard`, `TXT.MobileInputIsHidden`, `LongPressMenuInAppMenuMode`) | **NO VERIFICABLE** (sin módulo de Android ni teléfono) |

### 3.3 F-RUT-12 · F-RUT-13 · F-RUT-14 (34)

| AC | Verif. | Evidencia | Resultado |
|---|---|---|---|
| RUT-12-01 | d p | `RTT.AppendLooseGoesLastEvenAfterASeries`, `RED.AddStepAtTheEnd`, `CancelAddsNothing` | PASS |
| RUT-12-02 | d p cap | `SRM.TitleColorAndIcon`, `RED.StepRowContract`; CAP-10 (insignias, títulos `.hud`, riel), CAP-12 | PASS |
| RUT-12-03 | d p | `RTT.ReplaceKeepsSeriesAndCount`, `SFT.LooseStepKeepsHiddenProgressions`, `LooseStepHiddenProgressionsAreNotChecked`, `RED.EditInPlace` | PASS |
| RUT-12-04 | d p | `RTT.RemoveAt`, `RED.DeleteStepAtOnce` | PASS |
| RUT-12-05 | qa p | `RED.NoReorderOrMoveAffordances`; sin manijas ni gestos en las capturas | PASS |
| RUT-12-06 | d p cap | `SST.BorderCasesTable` (11 filas), `RED.BorderSubtitlesOnScreen`; CAP-12a/b comparadas al carácter | PASS |
| RUT-13-01 | d p cap | `SFT.NewFormDefaults`, `SDT.TitlesAndDefaults`; CAP-14 | PASS |
| RUT-13-02 | p cap | `SDT.FieldsInOrder`, `ProgressionsOnlyInASeries`, `HintsWhenEmpty`; CAP-14, CAP-15, CAP-24b | PASS |
| RUT-13-03 | d p | `StepTypeTests.FourTypesInSelectorOrderWithStorageNames`, `SDT.FourTypesOneSelected` | PASS |
| RUT-13-04 | d p | `SFT.OnlyTheChosenModeCounts`, `SDT.ModeSwitchKeepsTheText` | PASS |
| RUT-13-05 | d | `DIP.LegacyTable`, `AnyLengthNeverOverflows`, `SFT.HugeNumbersAreOutOfRange`, `NonPositiveSplitIsNoSplitEvenPastTheCap` (105 casos) | PASS |
| RUT-13-06 | d p | `SFT.ByTimeTable`, `SDT.EmptyTimeConfirmsZero` | PASS |
| RUT-13-07 | d p | `SFT.ByDistanceTable`, `SDT.EmptyDistanceIsNoGoal` | PASS |
| RUT-13-08 | d | `SFT.SplitTable`, `NonPositiveSplitIsNoSplitEvenPastTheCap`; coincide con `_buildSplitSeconds` del legacy (`routine_editor_screen.dart:557-562`) | PASS |
| RUT-13-09 | d | `SFT.NoSenseValidation` | PASS |
| RUT-13-10 | d p cap | `SFT.PrefillTable` (−90 → `-1`/`30` → −30 s; sin meta → 0 s), `SDT.PrefillOnScreen`; CAP-15a | PASS |
| RUT-13-11 | d p cap | `SLT.FieldCaps`, `SFT.FieldErrorsAndMaxTexts`, `HiddenModeFieldsAreNotChecked`, `SDT.RangeErrorKeepsTheDialogAndClearsOnChange`, `FocusGoesToTheFirstError`; CAP-20a | PASS |
| RUT-13-12 | d p cap | `SLT.FirstRepetitionClosedForm`, `TwoBillionRepeatsWithoutLoops`, `SFT.RepetitionErrors`, `SDT.RepetitionErrorUnderTheProgression`; CAP-20b | PASS |
| RUT-13-13 | p eq | `SDT.TabStaysInsideTheDialog`, `NumericFieldsUseSignedKeyboard`, `NAV.TabScrollsTheFocusedIntoView` | PASS\* (teclado numérico con signo en el teléfono: NV) |
| RUT-13-14 | p | `SDT.CancelOutsideEscapeAndBackChangeNothing`, `NAV.DialogButtonTwiceActsOnce` | PASS |
| RUT-13-15 | p cap | `SDT.LowWindowScrollsAndKeepsTheButtons`; CAP-23 | PASS |
| RUT-13-16 | d p cap | `SFT.PreviewIsTheConfirmedRow`, `SDT.PreviewMatchesTheEditorRow`; CAP-14, CAP-15 | PASS |
| RUT-14-01 | d p cap | `RTT.DefaultSeries`, `RED.SeriesButton`; DEMO-04 | PASS |
| RUT-14-02 | p cap | `RED.SeriesBlockContract`; CAP-10, CAP-11, CAP-12 | PASS |
| RUT-14-03 | d p cap | `ELT.HeaderRepeatDefaultsToOne`, `ZeroRepeatShowsZero`, `RED.ChipTextAndTouchArea`; CAP-10, CAP-12 | PASS |
| RUT-14-04 | p cap | `RPT.PrefillAndFocusOnDesktop`, `PrefillAndFocusOnPhone`; CAP-17 | PASS |
| RUT-14-05 | d p | `RCT.Table`, `HugeNegativeIsUnchanged`, `RPT.Rules`; coincide con `routine_editor_screen.dart:173`, `:179-186` (salvo el negativo de 20 cifras: DV-17) | PASS |
| RUT-14-06 | d p cap | `RCT.MaxAndProgressedTargets` (×491 sí, ×492 no), `RPT.OutOfRangeKeepsTheDialog`; CAP-21 | PASS |
| RUT-14-07 | d p | `RTT.InsertIntoSeriesAfterItsLast`, `RED.AddStepToSeries` | PASS |
| RUT-14-08 | d p | `RTT.ReplaceInSeries`, `RED.EditSeriesStep` | PASS |
| RUT-14-09 | d p | `RTT.RemoveSeries`, `RED.DeleteSeriesAtOnce` | PASS |
| RUT-14-10 | d p | `ELT.EmptiedSeriesDisappears`, `OneStepSeries`, `RED.SeriesEmptiesAndDisappears` | PASS |
| RUT-14-11 | d p cap | `ELT.ContiguousSeriesAreTwoBlocks`, `RTT.RepeatChangeTouchesOnlyItsSeries`, `RED.ContiguousSeries`; CAP-13 | PASS |
| RUT-14-12 | qa p | `RED.NoReorderOrMoveAffordances` | PASS |

### 3.4 F-HIS-17 · F-HIS-20 (10)

| AC | Verif. | Evidencia | Resultado |
|---|---|---|---|
| HIS-17-01 | u qa win | `DLT.WindowsUsesLocalLow`, `AndroidUsesTheDatabasesFolder`; el build normal creó `rower_app.db` vacía (v8) en `LocalLow/iguisoft/RowMate` (§1.5) | PASS\* (almacenamiento privado de Android: NV) |
| HIS-17-02 | d qa | `DSC.SchemaIsV8`, `WriteConnectionIsWalFullAndNoForeignKeys`, `SqliteVersionIsTheExpectedOne`; leído por QA con `sqlite3` (SQLite 3.50.4) contra el DDL del legacy (§1.5) | PASS |
| HIS-17-03 | d qa win | `LIT.NoCodeReachesTheLegacyData`; bases y preferencias del legacy con el mismo SHA-256 y la misma fecha antes y después de todo, también de los dos builds (§1.6) | PASS |
| HIS-17-04 | d p | `DOT.ExistingV8IsUsedAsIs`, `RPS.OpensAnExistingBase`; en el player, la base sembrada se abrió con `abierta (v8)` y `Escalera SPM` quedó igual | PASS |
| HIS-17-05 | d p | `DOT.NotADatabaseIsUntouched`, `ShortFilesAreUnreadableAndUntouched`, `NewerVersionIsUntouched`, `NewerVersionOnlyInTheWalIsUntouched`, `UnrecognizedIsUntouched`, `ProblemsNeverCarryTheFolder`, `RLT.UnreadableBaseShowsTheError`; CAP-03 (hash de F-ROTA igual) | PASS |
| HIS-17-06 | d p eq | `SET.WorkRunsOffTheCallingThread`, `PRF.FiftyFourRoutinesUnderOneSecond` (195 ms), `MainThreadWorkStaysUnderTheFrameBudget` (máx. 5,2 ms) | PASS\* (la fluidez a la vista en la ventana real: NV, §1.5) |
| HIS-20-01 | d dB qa win | `DFT.NameByVariant`, `ITT.OnlyAppRootChoosesTheDatabaseFile`; el simulador escribe solo `rower_app_dev.db` y el normal solo `rower_app.db`, y el normal no lee `ROWMATE_DATA_DIR` (§1.5) | PASS |
| HIS-20-02 | p win | `RPS.VariantsDoNotMix` (las dos composiciones sobre la misma carpeta); en la PC, cada build abre solo su archivo (registro y carpeta) | PASS\* (ver en la ventana del build normal que no aparece la rutina del simulador: NV, §1.5) |
| HIS-20-03 | ps cap | `RSL.SameScreensOnlyTheRibbon`; CAP-04 | PASS |
| HIS-20-04 | d dB p qa | `TDT.RefusesUserDataPaths`, `ITT.ScriptsIsolateTheDataFolder`, `Cap27RunsOnlyOnRequest`, `Cap27ProtectsTheUserDatabase`, `BOOT.UsesTheTestDataFolder`; comparación de QA antes y después de todo (§1.6); `capture-cap27.ps1` sin el *switch* sale con 4 sin tocar nada | PASS |

### 3.5 F-APP-03 y "también toca" (11)

| AC | Verif. | Evidencia | Resultado |
|---|---|---|---|
| APP-03-01 | p cap | `NAV.EditorCoversTheTabBar`, `BackSkipsWhatIsClosing`, `RED.TapDuringTheExitCreatesNothing`, `EnterDuringTheExitDoesNothing`; CAP-09 | PASS |
| APP-03-02 | p eq | `NAV.BackClosesOnlyTheTop`, `EscapeClosesOnlyMenusAndDialogs`, `EscapeClosesOneLevelAtATime`, `TXT.EscapeInAFieldKeepsTextAndFocus` | PASS\* ("Atrás" de Android: NV) |
| APP-03-03 | p cap | `NAV.DialogsAreModal`, `ScrimCancels`, `TabIsTrappedInTheDialog`, `DialogButtonTwiceActsOnce`; CAP-08, CAP-14, CAP-17 | PASS |
| APP-03-04 | p cap | `NAV.MenuAboveEverythingButTheSimulator`, `MenuItemTwiceOpensOneDialog`; CAP-07 | PASS |
| APP-03-05 | d p ps cap | `BOT.PositionsOfTheDesignNotes`, `NAV.ToastFourSecondsAndReplaces`, `RSL.ToastAndWrenchDoNotOverlap`; CAP-18, -19, -28; VID-03 medido cuadro a cuadro | PASS |
| APP-03-06 | p | `NAV.TooltipsOnHoverAndLongPress`, `MenuButtonTooltip` | PASS\* (lector de pantalla: NV por DT-05, pasa a la It. 14) |
| APP-03-07 | ps cap | `RSL.SimulatorLayerAboveEverything`, `BackArrowUnderTheRibbonWorks`, `PanelWorksWithADialogOpen`; CAP-08, CAP-09, DEMO-02 | PASS |
| APP-03-08 | p cap | `NAV.TransitionsFollowTheBible`, `ReducedMotionFades`; VID-02 | PASS |
| APP-01-09 | d p | `ACT.StartLoadsRoutines`, `SlowLoadDoesNotBlockTheStart`, `RLT.ListIsReadyBeforeTheFirstVisit` | PASS |
| APP-05-10 | u win | `WindowsTitleBarTests` (4); barra oscura en el build de simulador y en el normal, con `[PLATFORM] barra de título oscura` | PASS\* (modo claro y cambio en vivo: NV, porque nunca se cambia el tema del usuario. CAP-27 no se tomó: QA-V3-01) |
| APP-05-11 | p cap | `RLT.NoScrollbarOnPhone`, `RED.NoScrollbarOnPhone`, `SDT.NoScrollbarOnPhone`; CAP-04, CAP-10 | PASS\* (Android: NV) |

### 3.6 Criterios de iteraciones anteriores (11)

| AC | Verif. | Evidencia | Resultado |
|---|---|---|---|
| BLE-11-02 | p ps cap | `DeviceScreenTests.OutgoingConnectedViewIsFrozen`, `DeviceSimulatorTests.DisconnectKeepsTheNameWhileTheConnectedViewLeaves`, `.SimulatedDropKeepsTheNameWhileTheConnectedViewLeaves` (a los 60 ms: `Remo simulado` y `Conectado`, y en cada cuadro hasta ocultarse); VID-03 de la It. 2: 1,033 s y 1,067 s con `Remo simulado` mientras sale; nunca `Rower` | **PASS** (condición de la It. 2 cumplida) |
| BLE-16-01 | ps cap | `RSL.SimulatorLayerAboveEverything`, `SimulatorOverlayTests.RibbonIsAlwaysOnTop`; CAP-08, CAP-09 | PASS |
| BLE-16-02 | d s ps cap | `SPL.FabBottomAvoidsTheFab`, `PanelBottomNeverMoves`, `BOT.PositionsOfTheDesignNotes`, `SimulatorOverlayTests.WrenchSitsAboveTheFabInRoutines`, `WrenchStaysOnOtherTabsAndWithTheEditor`, `WrenchMovesWithoutJumpsAndStaysTappable`, `PanelOpensInTheSamePlaceInRoutines`, `ReducedMotionMovesTheWrenchAtOnce`; CAP-01, CAP-06, CAP-09; VID-02 | PASS |
| RUT-01-04 | p cap | `RED.StepRowIcons`; CAP-12 | PASS (QA-V3-05: queda un `[Ignore]` con el motivo viejo) |
| RUT-01-05 | d p | `RRT.UnknownTypeFailsTheLoadWithoutWriting`, `RLT.UnknownTypeShowsTheLoadError` | PASS |
| RUT-04-06 | d p cap | `SST.EditorSeparatorHasTwoSpaces`, `RED.BorderSubtitlesOnScreen`; CAP-12 | PASS (QA-V3-05) |
| APP-09-07 | p eq | `TXT.ContextMenuInTheAppLanguage`; CAP-26 (el menú lo dibuja la app con el catálogo, en el idioma de la app) | PASS\* (en la ventana real: NV, §1.5. Android: NV) |
| APP-02-08 | p cap | `RLT.RoutinesIsNotAPlaceholder`, `ShellTests`; CAP-01 y CAP-21b de la It. 2 | PASS |
| APP-05-05 | p cap | `RLT.TextScale130`, `RED.TextScale130`, `SDT.TextScale130`; CAP-25a/b | PASS |
| APP-05-07 | p eq | `NAV.TabOrderAndActivation`, `TabScrollsTheFocusedIntoView`, `TXT.TabFromTheDescriptionMovesFocus`, `EnterInASingleLineFieldBlurs`, `EnterInADialogFieldDoesNotConfirm` | PASS\* (Tab en la ventana real: NV, §1.5) |
| APP-05-08 | p | `NAV.ReducedMotionFades`, `RED.RowMotionReduced`, `RLT.StructureBarStaticWithReducedMotion` | PASS |

### 3.7 Conteo

| | AC |
|---|---|
| PASS (todo lo que pide el AC, verificado) | 88 |
| PASS\* (pasa en lo verificable; una parte de equipo NV) | 13: RUT-08-09, -11-08, -11-09, -13-13, HIS-17-01, -17-06, -20-02, APP-03-02, -03-06, -05-10, -05-11, -09-07, -05-07 |
| FAIL | 0 |
| NO VERIFICABLE | 1: RUT-11-10 |
| **Total** | **102** |

---

## 4. Casos de test legacy

El área no tiene tests legacy (spec §6, ROADMAP §5). Los casos 16 a 21 de [routines-models §5.3](../../product/inventory/routines-models.md) están cubiertos y en verde: 16 → `RET.ValidationOrder`, `RET.SaveTrimsRenumbersAndKeepsId`, `DTT.TrimLikeDart`; 17 → `RTT.DefaultSeries`; 18 → `RCT.Table`; 19 → `RTT.InsertIntoSeriesAfterItsLast`; 20 → `SFT.ByTimeTable`, `ByDistanceTable`, `SplitTable`; 21 → `RST.CreateGoesFirst`, `UpdateKeepsPosition`, `NAV.DeleteDialogOnlyConfirmDeletes`. Volví a leer en el legacy las reglas que más se prestan a error (`routine_editor_screen.dart:150-188`, `:528-562`) y coinciden con `RepeatCountTests` y `StepDialogFormTests`. Las únicas diferencias son las declaradas (DV-17 y Q-92).

---

## 5. Deuda de la It. 2 ([00-deuda-it2](00-deuda-it2.md))

| ID | Qué verifiqué | Resultado |
|---|---|---|
| QA-V2-01 | Arreglo en `DeviceScreenView.Render` (solo dibuja la vista conectada si `ViewFor(status) == Connected`); las tres PlayMode en verde con y sin simulador; VID-03 de la It. 2 sin `Rower` | Cerrado (AC-BLE-11-02 PASS) |
| QA-V2-02 | En los dos builds, `PerformanceTestRunCleaner: 4 entradas …`; el inspector no encuentra `PerformanceTestRunInfo`; no queda `Assets/Resources`; `.gitignore` con `/Assets/Resources/PerformanceTestRun*` | Cerrado |
| QA-V2-03 | `-Backend IL2CPP` → 3 con `NO VERIFICABLE: falta el módulo IL2CPP …`; logs separados por backend | Cerrado |
| QA-V2-04 | `INDEX.md` de los dos conjuntos con la columna de sondas; CAP-04b `1/1`; resumen `0 sondas de aspecto que fallan` | Cerrado |
| QA-V2-05 | El arnés `it2` pasa CAP-17 con sus comprobaciones | Cerrado |
| QA-V2-06 | Ningún motivo dice "→ It. 2". Quedan tres "→ It. 3" que ya se verifican en esta iteración | Cerrado (QA-V3-05) |
| QA-V2-07 | Las 10 `FontAsset` generadas y los 4 assets de URP con el mismo `mtime` después de todas mis corridas (4 suites, 3 arneses, semilla, 3 builds); `git status` sin cambios en `UI/`, `Settings/` ni `ProjectSettings/` | Cerrado |
| QA-V2-08 | Barra de título oscura en el build normal y en el de simulador (§1.5) | Cerrado (AC-APP-05-10 PASS\*) |
| QA-V2-09 | `-rowmate-color-probe` y `-rowmate-ui-pipeline`: 0 apariciones en el build normal | Cerrado |
| QA-V2-10 | `SimulatorModeTests.BuildOutputInspectorFindsTextsDiagnosticsAndTestMetadata` en verde; mi búsqueda propia da 0 | Cerrado |
| QA-V2-11 | `SimulatorModeTests.SimulatorDoesNotCompileInANonDevelopmentPlayer` en verde | Cerrado |
| QA-V2-12 | `ContrastTests` en verde (dotnet y Unity), incluidos los pares del Designer | Cerrado |
| QA-V2-13 | `DeviceScreenTests.VerticalScrollbarOnlyOnDesktop` en verde; sin barra fija en las capturas de teléfono de Rutinas | Cerrado |
| QA-V2-14 | `ci-unity.yml` pasa por un *parser* de YAML: `workflow_dispatch`, matriz `testMode × simulator` (4 jobs), `customParameters`, `restore` al final; `ci-simulator-define.sh` en ASCII con LF | Cerrado en el código; la corrida en GitHub sigue NV (P-08) |
| QA-V2-15 | VID-03 de la It. 2: el panel abierto a los 0,9 s y la caída desde `Simular desconexión` a 1 s | Cerrado |

También quedó saldada la deuda del arnés de REPORT It. 2 §7.2. Las sondas del Designer están en el catálogo (78, `aspect: true`), y la regla de toma falla si algo no queda quieto: `SnapshotRunner`, con `Shell.IsSettled` y un solo reintento de ventana, lanza `regla de toma: no quedó quieta …`.

---

## 6. Revisión estática

- **Hilos de SQLite.** `SerialExecutor` usa un hilo *background* con una cola FIFO (`BlockingCollection`). Completa cada trabajo con `RunContinuationsAsynchronously`, así que ninguna continuación corre en el hilo de la base. `Dispose` termina lo encolado y espera el hilo con un tope de 5 s, se desuscribe de `DomainUnload` y es idempotente. La conexión de `RowMateDatabase` solo se toca dentro del ejecutor (`EnsureOpen` adentro de `Run`). Los objetos que vuelven son inmutables y de Core. En producción no hay `.Result`, `.Wait()`, `GetResult()`, `ConfigureAwait(false)` ni `Task.Run`. Las fallas esperadas se registran como `Warning`, solo con el nombre del archivo; ninguna `SQLiteException` lleva la ruta al registro (lo vi en los registros del player).
- **IL2CPP.** `Infrastructure/link.xml` preserva `RowMate.Infrastructure` y `Gilzoide.SqliteNet` enteros. Las filas (`RoutineRow`, `IntervalStepRow`) se leen con `Query<T>` genérico, sin `MakeGenericMethod`, y los atributos del paquete heredan de `PreserveAttribute`. El *callback* nativo de `WindowsTitleBar` es estático y tiene `[AOT.MonoPInvokeCallback]`. **NO VERIFICABLE** sin el módulo (R3-01). El humo de la base pasa en Mono.
- **Asignaciones por cuadro.** Los caminos de todos los cuadros (`ShellHandle.Tick` → `ShellLayout.Poll`, `AppShellView.Tick` → `KeyboardAvoider.Tick`, `MotionDriver.Tick`, `TitleBar.Poll` cada 2 s) no crean objetos ni *closures*. Producción no usa LINQ. `StructureBarView` es un solo elemento con `Painter2D` y se recalcula solo si cambian el ancho o la rutina. Las animaciones se crean al empezar, no en cada cuadro.
- **Composición.** Solo `AppRoot` elige el archivo, con `DatabaseFiles.NameFor(BuildFlags.IsSimulator)`. `DataLocation` lee `ROWMATE_DATA_DIR` solo con `UNITY_EDITOR || DEVELOPMENT_BUILD`, y lo comprobé en el build normal. El orden de cierre en `OnDestroy` es shell → app → *backend* → repositorio y ejecutor → superficie.
- **Robustez.** `Guardar`, `Eliminar` y `Reintentar` lanzan sus tareas con `_ =`. Una excepción inesperada que no sea de la base quedaría sin registro (QA-V3-10). Hoy no pasa, porque `RoutinesStore` captura todas las de la base.
- **Regla del repositorio público.** El `git grep -nIE` de rutas absolutas de la PC que pide la tarea (la carpeta de perfiles de Windows con barra o doble barra, su forma de Git Bash y la del scratchpad) da **0**, sobre lo versionado y con `--untracked`, este documento incluido. El patrón no se copia acá porque el propio texto del patrón daría un resultado. `manifest.json` e `INDEX.md` de los dos conjuntos no llevan rutas. Los PNG y MP4 nuevos no tienen metadatos con rutas. Este documento usa rutas relativas y `<scratch>`.
- **Legacy.** `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó `strava_secrets.dart`.

---

## 7. Issues

### 7.1 Tabla

| ID | Sev. | Resp. | Qué pasa | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V3-01 | **mayor** | architect | `tools/unity/capture-cap27.ps1 -ReplaceUserDatabase` **nunca puede tomar CAP-27**. (a) La base sintética se escribe como `semilla\rower_app.db`, fuera de `<temp>/RowMate.Tests/`, y `TempDatabase.Guard` la rechaza, así que siempre sale con 7. (b) La negativa "hay un proceso `Unity`" compara `ProcessName -eq 'Unity'` sin distinguir mayúsculas y también encuentra el `unity.exe serve` de la Hub, que está siempre abierto con la Hub: sale con 5. (c) `-ClickRelative '0.5,0.96'` no es "la pestaña del medio": con 4 destinos, 0,5 es el borde entre Workout y Rutinas (los centros están en 0,125 / 0,375 / 0,625 / 0,875). Falla siempre en forma segura (no toca nada), pero la evidencia CAP-27 de la spec §7.3 no se puede producir. `ITT.Cap27*` solo revisa el texto del script | Reproducido: `SnapshotCli.SeedDatabase -snapshotSeedDb <scratch>/…/semilla/rower_app.db` → exit 1, `TempDatabase: rower_app.db solo se usa dentro de la carpeta temporal de pruebas.`, sin archivo creado. `tools/unity/capture-cap27.ps1` (pasos 1 y 2, llamada a `capture-window.ps1`); `Tests/Support/TempDatabase.cs:84-89`; centros de las pestañas en DEMO-01 | Sembrar con un nombre neutro (`semilla\cap27.db`) y copiarlo como `rower_app.db`; buscar el Editor por la ruta (`…\Editor\Unity.exe`) o por el `Temp/UnityLockfile` tomado, y no por el nombre; pulsar en `0.625,0.96`; agregar una prueba que corra el paso 1 de verdad (semilla en una carpeta temporal) |
| QA-V3-02 | menor | architect | En VID-01, el último bloque (o el último paso) se va sin colapsar: el bloque desaparece en el cuadro del toque y el vacío aparece entero, sin fundido. La spec pide "lo borrado se desvanece y colapsa; nada salta". También siguen los otros puntos de motion de 04-design-notes §16.6: las hojas aparecen en el acto, el color del tipo cambia sin cruce y la línea de error aparece sin crecer | Cuadros de VID-01: 7,900 s con la serie visible y 8,000 s con `Todavía no hay pasos.` entero. `RoutineEditorPage.Render` pasa `#editor-steps` a `display: none` en el mismo cuadro en que arranca el `CollapseMotion`. VID-01 de QA igual byte a byte al del repo | La propuesta del Designer (§16.6, puntos 1 a 4): con 0 pasos, dejar visible `#editor-steps` hasta que termine el colapso y recién ahí fundir el vacío |
| QA-V3-03 | menor | architect | En DEMO-05 no se ven las progresiones: el contenido del diálogo quedó arriba, porque `Type` no lleva a la vista el campo que escribe. 04-design-notes §7.3 pide la vista previa fija con el contenido desplazado al final | DEMO-05: barra de desplazamiento del contenido arriba, `Tipo` visible; `RoutinesCatalog.cs` (`Type("step-prog-spm", "2")` y después `Shot`) | `ScrollTo("step-prog-spm")` (o `TapInView`) antes de la toma |
| QA-V3-04 | menor | architect | `UiTestHost.Type` asigna el valor y deja el cursor en 0. Es solo estético y es del arnés, no de la app | CAP-16 `|500`, CAP-19 y CAP-28 `|Prueba`, DEMO-02 `|Escalera SPM`, DEMO-03 `|5` (04-design-notes §16.6, punto 5) | `SelectRange(largo, largo)` después de asignar, como al escribir |
| QA-V3-05 | menor | architect | Quedan tres `[Ignore]` con "NO VERIFICABLE → It. 3", que esta iteración ya verifica en pantalla. Es la misma clase de problema que QA-V2-06 | `StepLabelsTests.EditorSubtitleOnScreen`, `StepTypeTests.IconsInEditor`, `LanguageStateTests.SystemTextsFollowSystemLanguage` (en el XML de Unity y en dotnet) | Que el motivo diga dónde se verifica: `RoutineEditorTests.BorderSubtitlesOnScreen`, `RoutineEditorTests.StepRowIcons`, `TextEditingTests.ContextMenuInTheAppLanguage` y [Device-And] para el teclado del sistema |
| QA-V3-06 | menor | architect | El arnés no es determinista byte a byte justo después de una corrida del otro conjunto. El `it2` corrido después del `it3` dio 13 PNG distintos del repo (≤ 2 por canal en 1 a 9 píxeles de borde de glifo) y los 3 MP4; la corrida siguiente de `it2` dio los mismos bytes que el repo. No contradice la regla de ±1-3 por canal, pero sí la frase "byte a byte entre corridas completas" de [00-deuda-it2 §3](00-deuda-it2.md) y 04-design-notes §7.1 (regla 5). Hay estado que pasa de un proceso del Editor al siguiente | `<scratch>/it3/qa-snap-it2` contra `qa-snap-it2b` y contra el repo: lista de píxeles distintos de CAP-01, -04a, -06, -07 | Documentarlo: comparar con tolerancia, o correr dos veces seguidas antes de versionar. Si se quiere byte a byte, buscar qué estado pasa de un proceso al otro (caché de `Library/`) |
| QA-V3-07 | menor | po | Los campos de una línea quitan `\n`, `\r` y `\t` al cambiar el valor. El legacy solo quita `\n`: `FilteringTextInputFormatter.singleLineFormatter` = `deny('\n')`. El comentario de `RmTextField` dice que es "el singleLineFormatter del legacy". Escenarios: pegar `Serie⇥2` en el nombre da `Serie⇥2` en el legacy y `Serie2` en la app; pegar `A⏎B` copiado en Windows (CRLF) da `A\rB` en el legacy y `AB` en la app | `<flutter-sdk>/packages/flutter/lib/src/services/text_formatter.dart:425`, `widgets/editable_text.dart:967-970`; `Presentation/Controls/RmTextField.cs` (comentario de cabecera, punto "Una línea") | Aceptarlo como diferencia visible nueva (DV-20: quitar el `\r` y el `\t` evita caracteres invisibles en el nombre), o alinear con el legacy. Corregir el comentario en los dos casos |
| QA-V3-08 | menor | architect | Cada build de **desarrollo** abre el puerto de PlayerConnection, y cada ruta de `.exe` nueva dispara el aviso del Firewall de Windows ("Windows Security"). El aviso oscurece toda la pantalla y retiene la entrada de las otras ventanas hasta que se contesta. Mientras no se contesta, Windows deja reglas entrantes de **bloqueo** para ese `.exe`. Al terminar esta verificación había **tres avisos pendientes en la pantalla del usuario**: dos de corridas anteriores (03:56 y 03:57) y uno del build de QA (05:20). Eso impidió manejar la ventana real (§1.5) | Procesos `PickerHost` de las 03:56, 03:57 y 05:20; reglas `rowmate.exe` de bloqueo para las rutas de esos builds; capturas con brillo 0,55 | Usar siempre la misma carpeta de salida para los builds de desarrollo (una sola regla para siempre); explicar en `tools/unity/README.md` qué contestar (`Cancelar`: la app no necesita red); y que el coordinador le avise al usuario que hay avisos pendientes |
| QA-V3-09 | menor | designer | Con el simulador, la llave tapa acciones en reposo en algunos tamaños: la cruz de la 4.ª fila en W1280 (CAP-11, CAP-26, DEMO-06, DEMO-08), la cruz de la fila 8 en TV (CAP-12a) y el botón `Serie` en L640 (CAP-22). Se destapan desplazando, porque los rellenos esquivan la llave. AC-BLE-16-02 fija esa posición en el editor, como el legacy, donde la capa también tapa contenido. Ningún AC falla | Las capturas citadas | Decidir si se acepta, como en el legacy, o si la regla de DV-13 se extiende al editor |
| QA-V3-10 | menor | architect | `Guardar`, `Eliminar` y `Reintentar` lanzan la tarea con `_ = …`. Una excepción inesperada, fuera de las de la base que ya captura `RoutinesStore`, quedaría sin observar y sin registro. `RoutineEditorStore.SaveAsync` las relanza | `Presentation/Routines/RoutineEditorPage.cs:57`, `RoutinesScreenView.cs:82`, `:267` | `.Forget(logger, tag)`, como `AppContext.Start()` |

### 7.2 Escenarios concretos

- **QA-V3-01.** El usuario pide CAP-27 con la app y el Editor cerrados y la Hub abierta. `capture-cap27.ps1 -ReplaceUserDatabase` sale con 5 ("hay un proceso RowMate o Unity abierto"), porque el `unity.exe serve` de la Hub cuenta como Unity. Con la Hub cerrada, Unity arranca para sembrar, `TempDatabase.Guard` rechaza `rower_app.db` y el script sale con 7 ("no se pudo armar la base sintética"). Las dos veces termina sin tocar la base del usuario, pero sin captura.
- **QA-V3-02.** En el editor de una rutina nueva: `Serie`, y después el tacho del encabezado. El bloque de 3 filas desaparece de un cuadro al siguiente y en su lugar aparece entero `Todavía no hay pasos.`. Con dos bloques, borrar uno sí colapsa bien.
- **QA-V3-07.** En el bloc de notas de Windows se copia `Pirámide⏎500` (CRLF) y se pega en `Nombre de la rutina`. El legacy guarda `Pirámide\r500`, con un retorno de carro invisible. La app guarda `Pirámide500`. El comportamiento de la app es mejor, pero es distinto y no está declarado.

---

## 8. NO VERIFICABLE (con motivo)

| Qué | Motivo | Cuándo |
|---|---|---|
| AC-RUT-11-10 entero; las partes Android de AC-RUT-11-08, -13-13, AC-HIS-17-01, AC-APP-03-02, -05-11, -09-07 | Sin el módulo de Android ni un teléfono 8.0+ de 64 bits | Cuando estén (prerrequisito del usuario) |
| IL2CPP (R3-01: mapeo de filas, `link.xml`) | Sin el módulo Windows IL2CPP (`build.ps1` sale con 3) | Cuando esté el módulo: `build.ps1 -Backend IL2CPP -DbSmoke` |
| La ventana real con mouse y teclado: crear la rutina de la demo con la UI, atajos, acentos y AltGr, Tab, la fluidez a la vista, que la rutina del simulador no aparezca en el build normal (AC-RUT-08-09, -11-09, AC-HIS-17-06, -20-02, AC-APP-05-07, -09-07) | Avisos del Firewall pendientes que oscurecen la pantalla y retienen la entrada (QA-V3-08). Mandar teclas al escritorio del usuario es riesgoso. Lo demás de esos AC se verificó en el arnés, en PlayMode y en el player sin UI | 5 minutos del usuario con el build de simulador, después de contestar los avisos: la demo de spec §1 |
| Barra de título en modo claro y el cambio en vivo (AC-APP-05-10) | Nunca se cambia el tema del usuario | `WindowsTitleBarTests` cubre la regla |
| CAP-27 | Solo a pedido, y la herramienta no puede tomarla (QA-V3-01) | Después de corregir QA-V3-01, si el usuario la pide |
| Lector de pantalla (AC-APP-03-06) | DT-05: el motor no expone la UI a los lectores del sistema | It. 14 |
| `ci-unity.yml` en GitHub | Sin la licencia de Unity en los secretos (P-08) | Cuando el usuario la cargue |

---

## 9. Resumen por rol

- **Architect:** QA-V3-01 (mayor, la herramienta de CAP-27), y los menores QA-V3-02 (motion del último bloque y el resto de 04-design-notes §16.6), -03 (DEMO-05), -04 (cursor de `Type`), -05 (motivos de `[Ignore]`), -06 (determinismo del arnés entre conjuntos), -08 (avisos del Firewall de los builds de desarrollo) y -10 (`_ =` sin registro). Ninguno frena el cierre. Si se corrige QA-V3-02 o -03, regenerar las capturas: `capture-snapshots.ps1`, dos veces seguidas por QA-V3-06.
- **PO:** QA-V3-07 (decidir si `\r` y `\t` en los campos de una línea pasan a DV-20). Para el REPORT: avisar al usuario que hay **tres avisos del Firewall de Windows pendientes** en su pantalla (contestar `Cancelar`: la app no necesita red) y pedirle los 5 minutos de demo en el build de simulador (§8).
- **Designer:** QA-V3-09 (la llave sobre las acciones del editor en reposo) y el visto bueno de QA-V3-02 cuando esté.
- **Usuario (prerrequisitos):** con los módulos IL2CPP y Android y un teléfono se cierran IL2CPP y las partes Android de 7 AC.

---

## Ronda 2

| Campo | Valor |
|---|---|
| Fecha | 2026-10-28 (06:19-06:47) |
| Entrada | La corrección de QA-V3-01 en [02-tech-design, "Cambios por verificación (ronda 1)"](02-tech-design.md). Es el único issue que pasó a esta ronda |
| Qué cambió desde la ronda 1 | Solo herramientas, pruebas y documentos: `tools/unity/capture-cap27.ps1`, `capture-window.ps1` y `README.md`, `Assets/RowMate/Snapshots/SnapshotCli.cs`, `Assets/RowMate/Tests/Support/RoutineSeeds.cs`, `Tests/EditMode/Project/ItThreeToolingTests.cs`, `Tests/PlayMode/NormalBuildTests.cs` y `02-tech-design.md`. Ningún archivo de producción (`App`, `Core`, `Infrastructure`, `Platform`, `Presentation`, `Bootstrap`) ni ninguna captura cambió desde que empezó la ronda 1: lo busqué por fecha de modificación |
| Carpetas propias | `<scratch>/it3/qa-r2*` (dotnet, Unity, arneses, semilla, datos, ventana). Los builds van a `qa-build` y `qa-build-normal`, las mismas carpetas que en la ronda 1, a propósito: así no aparece otro aviso del Firewall (QA-V3-08) |
| Legacy | `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart` |
| Repo | Sin commits. `git status` igual antes y después de todas las corridas. `ProjectSettings/ProjectSettings.asset` con el mismo SHA-1 que en la ronda 1 y sin `ROWMATE_SIMULATOR`. `Library/RowMate/` vacía y sin `Assets/Resources` |

### R2.0 Veredicto

**APROBADO CON OBSERVACIONES.**

- **Todo en verde, con los conteos que anunció el Architect.** dotnet: **1598 pruebas (1581 pasan, 0 fallan, 17 ignoradas)**. Unity: **EditMode 1478 / 1462 / 0 / 16**, **con simulador 1629 / 1613 / 0 / 16**, **PlayMode 188 / 188** y **con simulador 235 / 235**. La matriz da **377 de 377 IDs en PASA**, con los 102 de la It. 3.
- **QA-V3-01: las tres causas quedaron corregidas y las probé de verdad** (§R2.2):
  - la semilla de Unity sigue rechazando `rower_app.db` (1) y con `cap27.db` deja una base v8 que solo tiene `Escalera SPM` (0);
  - el ensayo `-Rehearsal` sale con 0 sin tocar la carpeta del usuario, y el `unity.exe serve` de la Hub ya no cuenta;
  - el punto `0.625,0.98` elige Rutinas en 6 tamaños (PlayMode), y en la PC `capture-window.ps1` se negó sin hacer clic cuando otra ventana estaba adelante.
- **CAP-27 todavía no se puede tomar: QA-V3-11 (mayor).** `capture-window.ps1` falla en Windows PowerShell 5.1 antes de abrir el build cuando se lo llama sin `-Arguments`, porque `Start-Process -ArgumentList ''` no pasa la validación. `capture-cap27.ps1` lo llama justamente así. En la toma real, la base ya estaría sembrada, resguardada y reemplazada cuando la foto falle: el script sale con 8 y la restaura. El ensayo no lo detecta porque no llama a `capture-window.ps1`. El arreglo es de una línea y no afecta a ningún AC. Cuando la foto funcione, además, se escribe por defecto en `docs/` con franjas del escritorio que queda atrás (QA-V3-12, menor).
- **Arnés.**
  - `it3`: 47 tomas, 0 con problemas, 0 sondas que fallan. **Los 47 archivos y el `INDEX.md` son iguales byte a byte a los del repo**; en `manifest.json` solo cambia `durationMs`. Volví a mirar las 44 PNG y los 3 videos.
  - `it2`: 41 tomas iguales byte a byte al repo, esta vez en la primera corrida (QA-V3-06 no se repitió). Las capturas de la It. 2 no cambiaron desde la ronda 1.
- **AC-BLE-11-02 sigue en PASS.** Las tres PlayMode de QA-V2-01 están en verde, y el VID-03 de la It. 2 se regeneró igual: muestra `Remo simulado` mientras la vista sale. La deuda de la It. 2 sigue saldada (§R2.5).
- **Persistencia real en el build de simulador recién compilado** (§R2.4). Sembré la rutina de la demo (`Escalera SPM`) con el código de la app, corrí dos procesos con `-rowmate-db-roundtrip` y forcé un cierre después del *commit*: al reabrir, todo sigue. El build solo escribe `rower_app_dev.db` en la carpeta de desarrollo, y la carpeta real del usuario no cambió. Crear la rutina con mouse y teclado en la ventana real sigue NO VERIFICABLE, porque los avisos del Firewall siguen pendientes.
- **AC: 101 PASS (88 + 13 PASS\*), 0 FAIL y 1 NO VERIFICABLE**, igual que en la ronda 1.
- **Issues de esta ronda: 0 bloqueantes, 1 mayor (QA-V3-11) y 1 menor nuevo (QA-V3-12).** QA-V3-01 queda cerrado. Los menores QA-V3-02 a -10 de la ronda 1 siguen como estaban: no entraban en esta ronda y no cambian el veredicto.

### R2.1 Corridas

| Corrida | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it3/qa-r2`) | `Build succeeded. 0 Warning(s) 0 Error(s)`. Simulator **151/151**. EditMode **1447**: 1430 pasan, 0 fallan, 17 ignoradas. exit 0. Matriz sin Unity: PASA 312, PENDIENTE UNITY 65, FALLA 0, SIN TEST 0 |
| Unity EditMode (`run-unity-tests.ps1`, resultados en `<scratch>/it3/qa-r2-unity`) | **1478 / 1462 / 0 / 16**, exit 0 (34 s) |
| Unity EditMode + simulador | **1629 / 1613 / 0 / 16**, 0 inconclusos. Enable → 0, Restore → 0 (52 s) |
| Unity PlayMode | **188 / 188** (1 min 48 s) |
| Unity PlayMode + simulador | **235 / 235**. Enable → 0, Restore → 0 (2 min 24 s) |
| Matriz con los 4 XML (`-UnityResults`, con Unity cerrado; `<scratch>/it3/qa-r2m`) | **PASA 377, FALLA 0, IGNORADO 0, PENDIENTE UNITY 0, PENDIENTE WP-B 0, SIN TEST 0**, con los mismos 1598 / 1581 / 0 / 17. Los 102 IDs de `It03Ids` están en PASA |
| Arnés `-Set it3 -Out <scratch>/it3/qa-r2-snap-it3` | exit 0 en 62 s: `listo: 47 tomas, 0 con problemas, 0 sondas de aspecto que fallan` |
| Arnés `-Set it2 -Out <scratch>/it3/qa-r2-snap-it2`, justo después | exit 0 en 60 s: `41 tomas, 0 con problemas, 0 sondas de aspecto que fallan` |
| Build de simulador (`build.ps1 -Target Windows -Simulator -DbSmoke -Out <scratch>/it3/qa-build`) | `[BUILD] Windows simulator Mono2x Development`, `PerformanceTestRunCleaner: 4 entradas …`, `Build Finished, Result: Success` y `[DB] humo: ok` (43 s, humo incluido) |
| Build normal (`-Out <scratch>/it3/qa-build-normal`) | Success en 29 s. `BuildOutputInspector: sin rastros del simulador, de diagnósticos ni de tests` |
| Build normal IL2CPP | **3** en 9 s: `NO VERIFICABLE: falta el módulo IL2CPP de Windows en este Editor.` |

**Diferencias con la ronda 1** (1595 / 1475 / 1626 / 187 / 234):

- **+3** en dotnet y en las dos EditMode: `ITT.Cap27SeedStepRunsForReal`, `Cap27RehearsalNeverTouchesTheUserFolder` y `WindowCaptureOnlyClicksAndCapturesItsOwnWindow`.
- **+1** en las dos PlayMode: `NormalBuildTests.Cap27ClickSelectsTheRoutinesTab`.

Las pruebas de QA-V3-01 pasan en dotnet, en las dos EditMode de Unity (la semilla de verdad, con el SQLite de gilzoide) y en las dos PlayMode.

**Rendimiento (QA-G3-21).** `[PRF] 54 rutinas a la vista en 196 ms` (193 ms con simulador). En el hilo principal, `lista de 54` toma 7,2 ms (5,4 con simulador), `editor de 20 pasos` 1,0 ms (1,1) y `Tick durante el guardado` 0,00 ms. Todo queda bajo el umbral de 16 ms.

**Rastros en el build normal.** Busqué en todos los archivos del build, en UTF-8 y UTF-16. Dan 0:

- los argumentos `-rowmate-db-smoke` y `-rowmate-db-roundtrip`, los dos de diagnóstico y `ROWMATE_DATA_DIR`;
- los textos `Ida y vuelta`, `Remo simulado` y `SIMULADOR`;
- `RowMate.Simulator`, `PerformanceTestRunInfo` y las clases de prueba `InlineDbExecutor`, `RoutineSeeds`, `ScriptedRoutineRepository`, `FakeClipboard`, `FakeSoftKeyboard` y `SequenceClock`;
- lo nuevo de esta ronda: `SeedDatabase`, `snapshotSeedDb`, `Cap27` y `cap27.db`.

`RowMate.Tests…` y `RowMate.Snapshots` aparecen solo como nombres de `InternalsVisibleTo` en los metadatos de los ensamblados de producción. `TempDatabase` aparece solo dentro del binario nativo de SQLite, igual que en la ronda 1.

### R2.2 QA-V3-01: lo que se arregló y lo que falta

| Parte | Qué verifiqué | Resultado |
|---|---|---|
| (a) La semilla | Corrí el paso 1 con Unity, como el script: `SnapshotCli.SeedDatabase -snapshotSeedDb <scratch>/…/semilla/<nombre> -seed Cap27`, con `ROWMATE_DATA_DIR` temporal. **Con `rower_app.db`** sale con **1** y el mensaje `TempDatabase: rower_app.db solo se usa dentro de la carpeta temporal de pruebas.`, sin crear el archivo: la guardia no se aflojó. **Con `cap27.db`** sale con **0** y `base sintética de CAP-27 lista (Escalera SPM)`. El archivo pesa 28 672 bytes, tiene la firma `SQLite format 3` y no deja `-wal` ni `-shm`. Lo abrí en solo lectura con `sqlite3`: `user_version` 8, `integrity_check` ok y una sola rutina, `Escalera SPM` (`2026-10-23T13:00:00.000Z`), con 4 pasos: calentamiento 300 s; trabajo 300 s con SPM 18, serie ×3 y +2/rep; descanso 120 s en la misma serie; enfriamiento 300 s. `ITT.Cap27SeedStepRunsForReal` corre lo mismo en dotnet y en Unity | **Corregido** |
| (b) Los procesos | Probé la regla de `Test-UnityEditorPath` (`[\\/]Editor[\\/]Unity\.exe$`) con las rutas reales de la PC. El `unity.exe serve` de la Hub vive en la carpeta de la Hub y no es un Editor, aunque su `ProcessName` es `unity` y `-eq 'Unity'` lo toma. `…\Hub\Editor\6000.3.27f1\Editor\Unity.exe` sí es un Editor, y una ruta vacía o que no se puede leer también cuenta. En el ensayo, las dos comprobaciones (antes de sembrar y antes del resguardo) informan **solo** el `RowMate.exe` de la It. 2, abierto desde ayer. La Hub no aparece, y en la segunda tampoco queda vivo ningún Unity de la semilla | **Corregido** |
| (c) El clic | `NBT.Cap27ClickSelectsTheRoutinesTab` está en verde en PlayMode, con y sin simulador: el punto que lee del script cae en `#tab-routines` y la elige en W1280, L640, P800, W1280 a 1,5, 1920 × 1080 y 2560 × 2100. En DEMO-01 (W1280) el centro de `Rutinas` queda en x = 800 de 1280 (0,625) y la barra ocupa más o menos y 656-720 (0,98 × 720 = 705,6). En la PC, `capture-window.ps1` (con `-Arguments` para esquivar QA-V3-11) abrió el build de simulador de QA y **se negó con 3** (`en el punto del clic hay otra ventana adelante: no se hace clic ni se captura`), porque el aviso del Firewall de las 03:56 tiene el primer plano. Cerró su build y no dejó ningún proceso | **Corregido**: la negativa funciona en la PC |
| Ensayo (`capture-cap27.ps1 -Rehearsal`) | Sale con **0** en 9 s. Hace la semilla con Unity, el resguardo y el reemplazo (`mismo SHA-256 que la semilla, sin -wal ni -shm`), y después restaura y verifica (`mismos archivos, SHA-256 y fecha; el -shm que dejo el "build" se fue; rower_app_dev.db igual`). No quedó ninguna carpeta `RowMate.Cap27.*`, y la carpeta real del build normal quedó igual: comparé la lista de archivos, con tamaño y fecha, antes y después | OK |
| Sin el *switch* | Sale con **4**, explica lo que haría y no toca nada | OK |
| **(d) La foto** | `capture-cap27.ps1:330` llama a `capture-window.ps1` sin `-Arguments`. `capture-window.ps1:63` hace `Start-Process -FilePath $exePath -ArgumentList $Arguments -PassThru`, con `$Arguments = ''` por defecto (`:19`). En Windows PowerShell 5.1, que es el que usan los dos scripts, eso da un error de validación del parámetro. Lo reproduje sin abrir nada: pasé el `hostname.exe` del sistema como `-Exe`, con los mismos argumentos que usa el script, y salió `Start-Process : Cannot validate argument on parameter 'ArgumentList'. The argument is null or empty.`, con exit **1**, sin proceso ni archivo. Con `if ($Arguments -ne '')`, el mismo `Start-Process` arranca bien (lo probé en 5.1). En la PC no hay PowerShell 7 | **Falla: QA-V3-11** |

**Conclusión.** Las tres causas de QA-V3-01 están bien resueltas y probadas, así que QA-V3-01 se cierra. Lo que pedía el issue, que la herramienta pueda producir CAP-27, todavía no se cumple. Con RowMate y el Editor cerrados y los avisos contestados, la toma real sembraría, resguardaría y reemplazaría la base, y el paso 4 fallaría en el acto: sale con 8, con la base restaurada. Eso queda como QA-V3-11.

El ensayo no lo puede ver, porque por diseño no llama a `capture-window.ps1`, y `ITT.WindowCaptureOnlyClicksAndCapturesItsOwnWindow` solo revisa el texto. No corrí `-ReplaceUserDatabase`: CAP-27 es solo a pedido del usuario, y hoy se negaría (5) por el `RowMate.exe` abierto, que no lancé yo.

### R2.3 Capturas

Volví a mirar las **44 PNG** de `03-rutinas/snapshots/` contra la spec §7.3-§7.4, 04-design-notes §7 y `docs/image-04.png` del legacy. De los **3 videos** miré cuadros elegidos: VID-01 de 0,5 a 9,5 s, VID-02 de 0,3 a 7,9 s y VID-03 de 0,3 a 6,7 s. Son los mismos bytes que en la ronda 1, así que el resultado es el de la tabla del §2:

- **Está todo lo que pide "Qué tiene que verse".** Eso incluye:
  - los textos de CAP-12a/b al carácter (las 11 filas de AC-RUT-12-06) y los de CAP-24a/b en inglés;
  - las mini-barras de 8, 14, 1 y 7 tramos en CAP-04, y las de 8 y 10 en DEMO-07 y DEMO-09;
  - las hojas de la serie: dos con ×3, una con ×2, ninguna con ×1;
  - el aviso entero debajo de la llave (CAP-18, -19 y -28);
  - el velo con la cinta y la llave encima (CAP-08 y DEMO-11).

  CAP-10, CAP-11 y `image-04.png` tienen la misma estructura de serie: encabezado con `×N` y tacho, sangría, pie centrado, lápiz y cruz.
- **Siguen las observaciones de la ronda 1**, que no entraban en esta ronda:
  - en VID-01, a los 8,000 s, el último bloque desaparece sin colapsar (QA-V3-02);
  - DEMO-05 no muestra las progresiones (QA-V3-03);
  - el cursor queda al principio en CAP-16, -18, -19, -28, DEMO-02 y DEMO-03 (QA-V3-04);
  - la llave tapa la cruz de la 4.ª fila en W1280 y el botón `Serie` en L640 (QA-V3-09).
- **Una más del arnés, de la misma familia que QA-V3-04.** En DEMO-04 y DEMO-06 el nombre `Escalera SPM` aparece seleccionado entero. `UiTestHost.Tap` manda `NavigationSubmit` sin mover el foco; al cerrar el diálogo, `ExitLock` le devuelve el foco al campo del nombre, y `selectAllOnFocus` (en escritorio y en una línea) lo selecciona entero. En la app real no pasa: el clic en `+ Paso`, que es un `RmButton` enfocable, se lleva el foco, y al cerrar el diálogo vuelve al botón. En el legacy, en escritorio, el clic afuera le saca el foco al campo. No cambia nada de lo que pide "Qué tiene que verse". Se arregla junto con QA-V3-04, con un `Tap` que enfoque lo que toca si es enfocable.
- `INDEX.md` y `manifest.json` de los dos conjuntos no tienen rutas de la PC.

**Las capturas de la It. 2 no cambiaron sin motivo.**

- Los 42 archivos de `02-dispositivo-simulador/snapshots/` (PNG, MP4 e `INDEX.md`) tienen los mismos bytes que en la ronda 1, donde la corrida `qa-snap-it2b` dio igual al repo. Ninguno se modificó después de que empezó la ronda 1.
- La corrida `it2` de esta ronda también dio los mismos bytes, `rt1-measurement.json` incluido. En `manifest.json` solo cambia `durationMs`.
- Lo que difiere de `HEAD` es lo que ya explicó la ronda 1 (§1.3): los tokens del HUD, CAP-05, VID-03 y CAP-21b.
- En el VID-03 de la It. 2 volví a mirar la caída: a 1,033 s y a 1,067 s se ve `Remo simulado` + `Conectado` mientras la vista sale, y `Rower` no aparece nunca.

### R2.4 Persistencia real (build de simulador de QA, Mono)

| Paso | Evidencia |
|---|---|
| La rutina de la demo | `Escalera SPM` como queda en el paso 7 de la demo: calentamiento 5m, serie ×3 (trabajo 5m con SPM 18 y +2/rep, descanso 2m) y enfriamiento 5m. La sembré con el repositorio de la app (la semilla de §R2.2) y la copié como `rower_app_dev.db` en una carpeta nueva, `<scratch>/it3/qa-r2-data`, que pasé en `ROWMATE_DATA_DIR` |
| 1.ª corrida (`RowMate.exe -batchmode -nographics -rowmate-db-roundtrip`) | exit 0: `[APP] RowMate 1.0.0 (1) · WindowsPlayer · simulador=sí`, `[APP] base rower_app_dev.db (carpeta de desarrollo)`, `[DB] base rower_app_dev.db abierta (v8)` y `[DB] ida y vuelta: guardada` |
| 2.ª corrida, en otro proceso | exit 0: `[DB] ida y vuelta: sigue después de reabrir (2 pasos, 2 rutinas)` |
| La base después, en solo lectura | `integrity_check` ok, v8 y 2 rutinas. `Ida y vuelta` tiene `created_at` `2026-10-28T09:34:33.212Z` (UTC con 3 decimales). `Escalera SPM` está **igual a como entró**: 4 pasos, y la serie con `group_repeat_count` 3 y `progression_spm` 2. En la carpeta solo queda `rower_app_dev.db`: al cerrar no quedan `-wal` ni `-shm` |
| Cierre forzado (AC-RUT-08-07), en una carpeta nueva | Maté el proceso a los 712 ms de arrancar, apenas apareció `guardada`. Quedaron el `-wal` (12 392 bytes) y el `-shm`, sin *checkpoint*. Al reabrir: `[DB] ida y vuelta: sigue después de reabrir (2 pasos, 1 rutinas)`, y el WAL pasó al archivo al cerrar |
| Ventana real del build de simulador | Su registro dice `[PLATFORM] barra de título oscura (AppsUseLightTheme)`, `[APP] base rower_app_dev.db (carpeta de desarrollo)` y `[DB] base rower_app_dev.db abierta (v8)`. Ninguna línea propia de la app lleva una ruta. No la pude manejar: ver la fila (c) de §R2.2 |
| Separación de las bases | El build de simulador solo escribió `rower_app_dev.db` en su carpeta. La carpeta real (`LocalLow/iguisoft/RowMate`) no tiene `rower_app.db` ni `rower_app_dev.db`, ni antes ni después de todo: comparé la lista de archivos. El build normal recién compilado no tiene `ROWMATE_DATA_DIR` ni los textos del simulador (§R2.1). `DataLocation` y `DatabaseFiles` no cambiaron desde la ronda 1, en la que el build normal creó solo `rower_app.db` en `LocalLow` e ignoró la variable. No volví a abrir el build normal, para no crear otra vez esa base en la carpeta del usuario. `RPS.VariantsDoNotMix`, `DFT.NameByVariant` y las de `DLT` están en verde |
| **No se pudo**: crear la rutina con mouse y teclado en la ventana real, cerrar y reabrir | Los tres avisos del Firewall de la ronda 1 siguen pendientes, y uno tiene el primer plano: `capture-window.ps1` se negó con 3 sin hacer clic. No mandé teclas ni clics por otro camino. La demo entera sigue cubierta por DEMO-01 a DEMO-12 (UI real y SQLite real en el arnés, con `Reopen()`) y por `RPS.ReopenShowsTheSame` |

### R2.5 ACs y deuda de la It. 2

**Los 102 AC quedan como en la ronda 1 (§3): 88 PASS, 13 PASS\*, 0 FAIL y 1 NO VERIFICABLE (AC-RUT-11-10).** El código de producción es el mismo, y todas sus pruebas volvieron a pasar en esta ronda (en la matriz, 102 de 102 en PASA). Esta ronda suma evidencia a estos:

| AC | Evidencia nueva | Resultado |
|---|---|---|
| BLE-11-02 | En verde: `DeviceScreenTests.OutgoingConnectedViewIsFrozen` (PlayMode, con y sin simulador), `DeviceSimulatorTests.DisconnectKeepsTheNameWhileTheConnectedViewLeaves` y `.SimulatedDropKeepsTheNameWhileTheConnectedViewLeaves`. El VID-03 de la It. 2 se regeneró igual byte a byte, con `Remo simulado` a 1,033 s y a 1,067 s | **PASS** (condición de la It. 2) |
| RUT-08-07 | El cierre forzado del player después del *commit* (§R2.4) | PASS |
| RUT-08-09 | Ida y vuelta entre dos procesos del player, con la rutina de la demo; DEMO-10 = DEMO-09 | PASS\* (la UI en la ventana real: NV) |
| HIS-17-04 | El player abrió la base v8 sembrada (`abierta (v8)`) y dejó `Escalera SPM` igual | PASS |
| HIS-20-01 | `[APP] base rower_app_dev.db (carpeta de desarrollo)`, y solo ese archivo en la carpeta | PASS |
| HIS-20-04 | Ninguna corrida de esta ronda creó ni tocó bases en la carpeta del usuario: dotnet, las 4 suites, los 2 arneses, la semilla, el ensayo, los 3 builds y las 6 corridas del player. `<temp>/RowMate.Tests/` quedó vacía, y el ensayo de CAP-27 no tocó esa carpeta. Las bases y las preferencias del legacy tienen el mismo SHA-256 antes y después | PASS |
| APP-05-10 | `[PLATFORM] barra de título oscura (AppsUseLightTheme)` en el build de simulador de esta ronda. CAP-27 sigue sin tomarse (QA-V3-11) | PASS\* |

| Deuda It. 2 | Evidencia de esta ronda | Estado |
|---|---|---|
| QA-V2-01 | Lo de AC-BLE-11-02, en la tabla anterior | Cerrado |
| QA-V2-02 | En los dos builds, `PerformanceTestRunCleaner: 4 entradas …`, el inspector limpio y sin `Assets/Resources` después. `SimulatorModeTests.PerformanceTestRunFilesAreRemovedBeforeTheBuild` y `ItTwoDebtTests.PerformanceTestRunFilesNeverReachTheBuildOrTheRepository` en verde | Cerrado |
| QA-V2-03 | IL2CPP → 3 en 9 s, con su log propio. `Il2CppIsDetectedFromTheVariationsFolder` en verde | Cerrado |
| QA-V2-04 / -05 | Los arneses `it3` e `it2` con `0 sondas de aspecto que fallan`, y CAP-17 de la It. 2 en `ok` | Cerrado |
| QA-V2-06 | Ningún `[Ignore]` dice "→ It. 2". Siguen los tres "→ It. 3" (QA-V3-05) | Cerrado (QA-V3-05 sigue) |
| QA-V2-07 | Los 45 archivos de `UI/Fonts/Generated`, `UI/Icons` y `Settings/` tienen el mismo `mtime` después de todo. `GeneratedFontAssetsAreCommittedWithoutDynamicData` y `OnlyTheBootstrapperWritesGeneratedAssets` en verde | Cerrado |
| QA-V2-08 | Barra oscura en el registro del build de simulador de esta ronda. `WindowsTitleBarTests` (4) en verde | Cerrado |
| QA-V2-09 / -10 | El inspector del build normal, limpio, y mi búsqueda, en 0 (§R2.1). `BuildOutputInspectorFindsTextsDiagnosticsAndTestMetadata` e `InspectorLooksForTheTextsTheSimulatorShows` en verde | Cerrado |
| QA-V2-11 | `SimulatorDoesNotCompileInANonDevelopmentPlayer` en verde | Cerrado |
| QA-V2-12 | `ContrastTests` (189) en verde | Cerrado |
| QA-V2-13 | `VerticalScrollbarOnlyOnDesktop` en verde, y ninguna PNG de teléfono tiene la barra fija | Cerrado |
| QA-V2-14 | `CiUnityRunsTheSimulatorSuitesWithoutCommittingTheDefine` en verde. La corrida en GitHub sigue NV (P-08) | Cerrado en el código |
| QA-V2-15 | VID-03 de la It. 2: el panel está abierto a los 0,967 s y la caída empieza a los 1,000 s | Cerrado |

### R2.6 Revisión estática

- **Hilos de SQLite, IL2CPP y asignaciones.** El código de producción no cambió desde la ronda 1, así que vale lo del §6. Releí:
  - `SerialExecutor`: una cola FIFO en un hilo propio, con `RunContinuationsAsynchronously` y un `Dispose` con tope de 5 s;
  - `Infrastructure/link.xml`: `RowMate.Infrastructure` y `Gilzoide.SqliteNet` con `preserve="all"`.

  En producción no hay `.Wait()`, `GetResult()`, `ConfigureAwait(false)` ni `Task.Run`. Los `.Result` que aparecen son `SQLite3.Result` y `StepDialog.Result`. IL2CPP sigue NO VERIFICABLE porque falta el módulo; el humo de la base pasa en Mono.
- **El arreglo de QA-V3-01.**
  - `RoutineSeeds.WriteFile` pasa por `TempDatabase.Guard`, borra un `-wal` o `-shm` viejo, siembra con el ejecutor en línea y cierra, y al cerrar el WAL pasa al archivo. Ningún código de producción lo usa.
  - `SnapshotCli.SeedDatabase` solo acepta `-seed Cap27`.
  - En `capture-cap27.ps1`, las negativas van antes de sembrar y antes del resguardo, y el resguardo se verifica con SHA-256 antes del reemplazo. La restauración en `finally` vuelve a poner cada archivo con su fecha (`RoundtripKind`), verifica también `rower_app_dev.db` y solo cierra el build que abrió el script.
  - Los dos scripts son ASCII y no usan operadores de PowerShell 7 (lo prueba ITT).

  El único defecto es QA-V3-11. QA-V3-12 es lo que pasaría después, cuando la foto funcione.
- **Repositorio público.** El `git grep -nIE` con el patrón de rutas de la PC que pide la tarea da **0** en lo versionado y **0** con `--untracked`, este documento incluido. Lo nuevo tampoco tiene nombres de usuario ni datos del equipo, y `manifest.json` e `INDEX.md` no tienen rutas.
- **Legacy.** `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar.

### R2.7 Issues

| ID | Sev. | Resp. | Qué pasa | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V3-01 | — | architect | **Cerrado.** Las tres causas (semilla, procesos y clic) quedaron corregidas y probadas | §R2.2 | — |
| QA-V3-11 | **mayor** | architect | `capture-window.ps1` llamado sin `-Arguments` falla en Windows PowerShell 5.1 antes de abrir nada: `Start-Process -ArgumentList ''` no pasa la validación (`ParameterArgumentValidationError`). `capture-cap27.ps1` lo llama sin `-Arguments`. Por eso la toma real de CAP-27 siembra, resguarda y reemplaza la base del build normal, y después sale con 8 en el paso 4 (restaurando la base). CAP-27 sigue sin poder tomarse. El ensayo no lo detecta porque no llama a `capture-window.ps1`, y la prueba de ITT solo mira el texto | `tools/unity/capture-window.ps1:19` (`$Arguments = ''`) y `:63`; `tools/unity/capture-cap27.ps1:330`. Reproducido con el `hostname.exe` del sistema como `-Exe`: `Cannot validate argument on parameter 'ArgumentList'. The argument is null or empty.`, exit 1, sin proceso | Pasar `-ArgumentList` solo si `$Arguments -ne ''` (probado en 5.1). Agregar una prueba que corra de verdad el arranque de `capture-window.ps1` contra un ejecutable inofensivo sin ventana, y que espere `el build no abrio ninguna ventana` (exit 1) y no un error de parámetro. También puede hacerlo el ensayo de CAP-27 |
| QA-V3-12 | menor | architect | Cuando la foto funcione, va a salir con franjas de unos 8 px de lo que hay detrás de la ventana, a los costados y abajo: `GetWindowRect` incluye los bordes invisibles de Windows 10 y 11 (1296 × 759 para una ventana de 1280 × 720). Además, `capture-cap27.ps1` la escribe por defecto en `docs/migration/iterations/03-rutinas/snapshots/`, que va a un repositorio público. Puede terminar publicado un pedazo del escritorio del usuario | `tools/unity/capture-window.ps1:98-112`; `tools/unity/capture-cap27.ps1:57`; la medida de CAP-22 en la It. 2 y la nota del Architect en 02-tech-design ("QA-V3-01: lo que queda") | Recortar con `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)`, o capturar solo la ventana con `PrintWindow` y `PW_RENDERFULLCONTENT`. Escribir primero en una carpeta temporal y pasarla a `docs/` recién después de mirarla |

**Siguen abiertos, sin cambios, los menores de la ronda 1:**

- QA-V3-02: el motion del último bloque.
- QA-V3-03: DEMO-05.
- QA-V3-04: el cursor de `Type`; ahora también el foco de `Tap` (§R2.3).
- QA-V3-05: los motivos de `[Ignore]`.
- QA-V3-06: el determinismo del arnés. Esta vez no se repitió.
- QA-V3-07: `\r` y `\t` en los campos de una línea, para el PO.
- QA-V3-08: los avisos del Firewall. Siguen los tres; esta ronda no sumó otro porque usé la misma carpeta de build.
- QA-V3-09: la llave sobre acciones del editor, para el Designer.
- QA-V3-10: `_ =` sin registro.

### R2.8 NO VERIFICABLE

Lo mismo que en el §8, con dos precisiones:

- CAP-27 ahora depende de QA-V3-11. Para tomarla, además, hay que cerrar el `RowMate.exe` que está abierto y contestar los avisos del Firewall.
- Manejar la ventana real sigue sin poder hacerse por los avisos pendientes. Lo volví a comprobar en esta ronda: `capture-window.ps1` → 3.

### R2.9 Limpieza

- Cerré los procesos que lancé: las 6 corridas del player y los Unity de la semilla, el ensayo, los arneses, los builds y las pruebas. Quedan solo los que ya estaban: la Hub con su `unity.exe serve` y el `RowMate.exe` de la It. 2.
- No apareció ningún aviso nuevo del Firewall: los players de esta ronda salieron de la misma carpeta que los de la ronda 1. Siguen los tres de antes.
- La carpeta de datos del usuario sigue sin bases, como al empezar. Cambiaron dos archivos que no son bases:
  - `Player.log`, que reescribió la ventana del build de simulador de QA. Es el registro de Unity, y las líneas propias de la app no tienen rutas.
  - `TestResults.xml`, que escribe el Unity Test Framework al terminar las PlayMode.
- `<temp>/RowMate.Tests/` quedó vacía, y no quedó ninguna carpeta `RowMate.Cap27.*`.

---

## Ronda 3

| Campo | Valor |
|---|---|
| Fecha | 2026-10-28 (07:21-07:47) |
| Entrada | La corrección de QA-V3-11 en [02-tech-design, "Cambios por verificación (ronda 2)"](02-tech-design.md). Es el único issue que pasó a esta ronda |
| Qué cambió desde la ronda 2 | Solo herramientas, una prueba y documentos: `tools/unity/capture-window.ps1`, `capture-cap27.ps1`, `new-windowless-exe.ps1` (nuevo) y `README.md`, `Tests/EditMode/Project/ItThreeToolingTests.cs` y `02-tech-design.md`. Lo busqué por fecha de modificación: ningún archivo de producción (`App`, `Core`, `Infrastructure`, `Platform`, `Presentation`, `Bootstrap`) cambió desde que empezó la ronda 1, y ninguna captura de los dos conjuntos cambió desde antes de la ronda 1 |
| Carpetas propias | `<scratch>/it3/qa-r3*`: dotnet, matriz, Unity, arneses, prueba de la ventana, semilla, datos y registros. Los builds van a `qa-build` y `qa-build-normal`, las mismas carpetas que en las rondas 1 y 2, para no sumar avisos del Firewall (QA-V3-08) |
| Legacy | `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar (`3ae0f3a`). No se leyó `lib/core/strava/strava_secrets.dart` |
| Repo | Sin commits. `git status --porcelain` igual antes y después de todas las corridas (320 entradas). `ProjectSettings/ProjectSettings.asset` con el mismo SHA-1 (`0922f776…`) y sin `ROWMATE_SIMULATOR`. `Library/RowMate/` vacía, sin `Assets/Resources` y sin escenas de prueba sueltas |

### R3.0 Veredicto

**APROBADO CON OBSERVACIONES.**

- **QA-V3-11 queda cerrado.** Lo probé por mi cuenta con Windows PowerShell 5.1 y un ejecutable de prueba sin ventana (§R3.2):
  - con la llamada de `capture-cap27.ps1`, sin `-Arguments`, `capture-window.ps1` sale con 1 en 0,8 s con `el build no abrio ninguna ventana` y `el proceso termino sin ventana (codigo 0)`, y no deja captura ni procesos. Con `-Arguments`, igual;
  - dos copias con el código de antes reproducen los dos defectos: el que vio QA y el segundo que encontró el Architect. La prueba nueva fallaría con cualquiera de los dos;
  - el ensayo de CAP-27 ahora corre esa misma llamada y sale con 0.
- **Todo en verde, con los conteos que anunció el Architect.**
  - dotnet: **1599 pruebas (1582 pasan, 0 fallan, 17 ignoradas)**.
  - Unity: **EditMode 1479 / 1463 / 0 / 16**, **con simulador 1630 / 1614 / 0 / 16** y 0 inconclusos, **PlayMode 188 / 188** y **con simulador 235 / 235**.
  - Matriz con los 4 XML: **377 de 377 IDs en PASA**, incluidos los 102 de la It. 3.
  - `ITT.WindowCaptureStartsTheExeForReal` corre de verdad en dotnet (2 s) y en las dos EditMode de Unity (2,0 s y 1,9 s).
- **Arnés.** `it3` saca 47 tomas y `it2` 41, sin problemas ni sondas que fallen. Los dos conjuntos son **iguales byte a byte a los del repo**: en `manifest.json` solo cambia `durationMs`. Volví a mirar las 44 PNG, los 3 videos y el VID-03 de la It. 2. Las capturas de la It. 2 no cambiaron.
- **AC-BLE-11-02 sigue en PASS**, y la deuda de la It. 2 sigue saldada (§R3.5).
- **Persistencia real en el build de simulador recién compilado** (§R3.4). Sembré la rutina de la demo con el código de la app. Sobrevivió a dos procesos del player y a un cierre forzado que dejó el `-wal` sin *checkpoint*. El build solo escribe `rower_app_dev.db` en su carpeta, y la carpeta real del usuario no cambió. Crear la rutina con mouse y teclado en la ventana real sigue NO VERIFICABLE: el aviso del Firewall de las 03:56 sigue en primer plano (QA-V3-08).
- **AC: 101 PASS (88 + 13 PASS\*), 0 FAIL y 1 NO VERIFICABLE (AC-RUT-11-10)**, igual que en las rondas 1 y 2.
- **Issues: 0 bloqueantes, 0 mayores y ninguno nuevo.** Siguen abiertos los 10 menores: de QA-V3-02 a QA-V3-10, y QA-V3-12. Con QA-V3-11 cerrado, la foto de CAP-27 ya se puede sacar, así que QA-V3-12 hay que resolverlo antes de tomarla (§R3.7).

### R3.1 Corridas

| Corrida | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it3/qa-r3`) | `Build succeeded. 0 Warning(s) 0 Error(s)`. Simulator **151/151**. EditMode **1448**: 1431 pasan, 0 fallan, 17 ignoradas. exit 0. Matriz sin Unity: PASA 312, PENDIENTE UNITY 65, FALLA 0, SIN TEST 0 |
| Unity EditMode (`run-unity-tests.ps1`, resultados en `<scratch>/it3/qa-r3-unity`) | **1479 / 1463 / 0 / 16**, exit 0 (35 s) |
| Unity EditMode + simulador | **1630 / 1614 / 0 / 16**, 0 inconclusos. Enable → 0, Restore → 0 (55 s) |
| Unity PlayMode | **188 / 188** (1 min 48 s) |
| Unity PlayMode + simulador | **235 / 235**. Enable → 0, Restore → 0 (2 min 24 s) |
| Matriz con los 4 XML (`-UnityResults`, con Unity cerrado; `<scratch>/it3/qa-r3m`) | **PASA 377, FALLA 0, IGNORADO 0, PENDIENTE UNITY 0, PENDIENTE WP-B 0, SIN TEST 0**. dotnet da otra vez 1599 / 1582 / 0 / 17. Los 102 IDs de `It03Ids` están en PASA |
| Arnés `-Set it3 -Out <scratch>/it3/qa-r3-snap-it3` | exit 0 en 63 s: `listo: 47 tomas, 0 con problemas, 0 sondas de aspecto que fallan` |
| Arnés `-Set it2 -Out <scratch>/it3/qa-r3-snap-it2`, justo después | exit 0 en 61 s: `41 tomas, 0 con problemas, 0 sondas de aspecto que fallan` |
| Build de simulador (`build.ps1 -Target Windows -Simulator -DbSmoke -Out <scratch>/it3/qa-build`) | `[BUILD] Windows simulator Mono2x Development`, `PerformanceTestRunCleaner: 4 entradas …`, `Result: Success` y `[DB] humo: ok` (43 s) |
| Build normal (`-Out <scratch>/it3/qa-build-normal`) | Success en 30 s. `BuildOutputInspector: sin rastros del simulador, de diagnósticos ni de tests` |
| Build normal IL2CPP | **3** en 9 s: `NO VERIFICABLE: falta el módulo IL2CPP de Windows en este Editor.` |
| `capture-cap27.ps1` sin *switch* | **4**, sin tocar nada |
| `capture-cap27.ps1 -Rehearsal` | **0** en 11 s (§R3.2) |

**Diferencias con la ronda 2** (1598 / 1478 / 1629 / 188 / 235). Hay **+1** en dotnet y en las dos EditMode: `ITT.WindowCaptureStartsTheExeForReal`. Las PlayMode no cambian. Las cuentas cierran como antes: 1630 − 1599 = 31 = 35 pruebas de `Tests/UnityEditMode` − 4 que solo existen en `net8.0`.

**Rendimiento (QA-G3-21).** `[PRF] 54 rutinas a la vista en 197 ms` (192 ms con simulador). En el hilo principal, `lista de 54` toma 6,6 ms (7,9 con simulador), `editor de 20 pasos` 1,2 ms (0,8) y `Tick durante el guardado` 0,00 ms. Todo queda bajo el umbral de 16 ms. La corrida con simulador coincidió con mi prueba de la ventana de §R3.2, que no abre Unity.

**Rastros en el build normal.** Busqué en todos los archivos, en UTF-8 y UTF-16. Dan 0:

- los argumentos: `-rowmate-db-smoke`, `-rowmate-db-roundtrip`, `-rowmate-color-probe`, `-rowmate-ui-pipeline` y `ROWMATE_DATA_DIR`;
- los textos del simulador: `Ida y vuelta`, `Remo simulado`, `metro simulado` (por `Pulsómetro simulado`), `Simular desconexi…`, `SIMULADOR` y `simulador=sí`;
- `RowMate.Simulator`, `PerformanceTestRunInfo` y las clases de prueba `InlineDbExecutor`, `RoutineSeeds`, `ScriptedRoutineRepository`, `FakeClipboard`, `FakeSoftKeyboard` y `SequenceClock`;
- `SeedDatabase`, `snapshotSeedDb`, `Cap27`, `cap27.db`, y lo nuevo de esta ronda: `sin-ventana` y `new-windowless`.

Como control, el build de simulador sí tiene `Ida y vuelta`, `ROWMATE_DATA_DIR` y `metro simulado`. Igual que antes, aparecen la constante `rower_app_dev.db` en `RowMate.Infrastructure.dll` y `TempDatabase` dentro del binario nativo de SQLite.

### R3.2 QA-V3-11: verificación

Hice una prueba propia (`<scratch>/it3/qa-r3-window/probe.ps1`) con el `powershell.exe` 5.1 del sistema. La corrí en una carpeta con `ñá` y espacios, que borré al final. No abre ninguna ventana y no toca el mouse ni la pantalla.

| Qué | Resultado |
|---|---|
| `new-windowless-exe.ps1 -Out <carpeta>/sin-ventana.exe` | 0 en 0,4 s, `[sin-ventana] listo`. El archivo pesa 3584 bytes y es de subsistema 2 (gráfico). Con el mismo `-Out` otra vez sale con **2** y no pisa el archivo; sin `-Out`, también **2** |
| `capture-window.ps1` con la llamada de `capture-cap27.ps1` (sin `-Arguments`, con `-WaitSeconds 8 -ClickRelative '0.625,0.98'`) | **1 en 0,79 s**: `[ventana] el build no abrio ninguna ventana` y `[ventana] el proceso termino sin ventana (codigo 0)`. Sin captura |
| Lo mismo con `-Arguments '-screen-width 1280 -screen-height 720'`, como CAP-22 | **1 en 0,77 s**, con los mismos dos mensajes y sin captura |
| Con un `-Exe` que no existe | **2** |
| Con un ejecutable sin ventana que queda vivo 30 s, y `-WaitSeconds 0` | **1 a los 22,7 s**: los 20 s del plazo más los 2 s de `CloseMainWindow`, sin la línea de "termino". El `finally` lo cerró y no quedó ningún proceso |
| **Copia 1**: el arranque de antes (`Start-Process -FilePath $exePath -ArgumentList $Arguments -PassThru`), sin `-Arguments` | **4**: `error inesperado (linea 89): The argument is null or empty…`. Es el defecto de la ronda 2. Ahora el `trap` lo saca con 4 y su mensaje, y no con el 1 de "no apareció la ventana", que confundía |
| **Copia 2**: `Get-MainWindow` sin mirar si el proceso terminó | **4** a los 9 s: `error inesperado (linea 112): Cannot convert null to type "System.IntPtr".` Es el segundo defecto que encontró el Architect: `MainWindowHandle` da `$null` y el script se cae en `SetForegroundWindow` |
| **Copia 3**: un `throw` dentro del `try`, con el ejecutable vivo | **4** a los 2,6 s (`error inesperado (linea 95): …`). El `finally` cerró el proceso antes de salir |
| Ensayo `capture-cap27.ps1 -Rehearsal`, en la cadena de corridas y con Unity cerrado | **0** en 11 s. Siembra con Unity, resguarda y reemplaza (`mismo SHA-256 que la semilla, sin -wal ni -shm`). Da `[sin-ventana] listo` y, por `Invoke-WindowCapture`, `el build no abrio ninguna ventana` y `capture-window.ps1 arranca con la llamada de la toma real (sin ventana: 1, sin clic ni captura)`. Después restaura y verifica. Solo informa el `RowMate.exe` de la It. 2, abierto desde ayer; la Hub no aparece. No quedó ninguna carpeta `RowMate.Cap27.*` |

**La prueba nueva** (`ITT.WindowCaptureStartsTheExeForReal`) espera exit 1, los dos mensajes, menos de 20 s y ninguna captura, y que no aparezcan `Cannot validate argument` ni `error inesperado`. Con las copias 1 y 2 fallaría por el código (4) y por los mensajes, así que detecta los dos defectos. También revisé el texto de los scripts:

- `-ArgumentList` se pasa solo si `-Arguments` no está vacío (con *splatting*);
- `Get-MainWindow` da cero si el proceso terminó, antes y después de `Refresh()`;
- la espera se corta si el proceso termina y, después de los `WaitSeconds`, vuelve a leer la ventana;
- `capture-cap27.ps1` tiene una sola llamada a `capture-window.ps1` (`Invoke-WindowCapture`), sin `-Arguments`, y la usan la toma real y el ensayo;
- los tres scripts son ASCII y no usan operadores de PowerShell 7. Lo controla `ITT.WindowCaptureOnlyClicksAndCapturesItsOwnWindow`.

Para un proceso vivo con ventana, `Get-MainWindow` da lo mismo que el código de la ronda 2 (`Refresh()` y `MainWindowHandle`). Con ese código encontré la ventana del build en la ronda 2.

Un dato más para la parte (b) de QA-V3-01. Durante la ronda, la Hub abrió un `unity.exe --internal-auth-broker-serve`, que vive en `…\Unity\bin\` y no en `…\Editor\`. La regla de `Test-UnityEditorPath` tampoco lo cuenta como Editor, y está bien que no lo cuente.

**No corrí** `-ReplaceUserDatabase`, porque CAP-27 es solo a pedido del usuario. Además, hoy se negaría (5) por el `RowMate.exe` de la It. 2, que sigue abierto. Tampoco corrí el camino con ventana (clic y captura): toca el mouse y la pantalla del usuario, y el aviso del Firewall de las 03:56 tiene el primer plano.

### R3.3 Capturas

Volví a mirar las **44 PNG** de `03-rutinas/snapshots/` contra la spec §7.3-§7.4, 04-design-notes §7 y `docs/image-04.png` del legacy. De los **3 videos** miré cuadros elegidos: VID-01 de 0,5 a 9,5 s, VID-02 de 0,3 a 7,9 s y VID-03 de 0,3 a 7,5 s. Son los mismos bytes que en las rondas 1 y 2, y también los de la corrida de QA de esta ronda, así que vale la tabla del §2:

- **Está todo lo que pide "Qué tiene que verse".** Eso incluye:
  - los 11 títulos y subtítulos de CAP-12a/b al carácter y los textos en inglés de CAP-24a/b;
  - las mini-barras de 8, 14, 1 y 7 tramos en CAP-04 y CAP-05, y las de 8 y 10 en DEMO-07 y DEMO-09;
  - las hojas de las series: dos con ×3, una con ×2 y ninguna con ×1;
  - los 16 dp entre los dos bloques de CAP-13;
  - el aviso entero debajo de la llave en CAP-18, -19 y -28;
  - el velo con la cinta y la llave encima en CAP-08 y DEMO-11;
  - DEMO-10 igual a DEMO-09 y DEMO-12 igual a DEMO-01.

  CAP-10, CAP-11 y `image-04.png` tienen la misma estructura de serie: encabezado con `×N` y tacho a la derecha, sangría, pie centrado `+ Agregar paso a serie`, lápiz y cruz.
- **Siguen las observaciones de la ronda 1**, que no entraban en esta ronda:
  - en VID-01, el último bloque desaparece entre los 7,9 s y los 8,0 s sin colapsar (QA-V3-02);
  - DEMO-05 no muestra las progresiones (QA-V3-03);
  - el cursor queda al principio en CAP-16, -19 y -28 y en DEMO-02 y -03, y el nombre aparece seleccionado entero en DEMO-04 y -06 (QA-V3-04);
  - la llave tapa en reposo la cruz de la 4.ª fila en W1280 (CAP-11, CAP-26, DEMO-06 y DEMO-08), la de la fila 8 en CAP-12a y el botón `Serie` en CAP-22 (QA-V3-09).
- `INDEX.md` y `manifest.json` de los dos conjuntos no tienen rutas de la PC.

**Las capturas de la It. 2 no cambiaron sin motivo.**

- Los 44 archivos de `02-dispositivo-simulador/snapshots/` tienen fecha de antes de la ronda 1.
- La corrida `it2` de esta ronda dio los mismos bytes en 43 de ellos: las 41 tomas, `INDEX.md` y `rt1-measurement.json`. En `manifest.json` solo cambian 41 `durationMs`.
- En `INDEX.md`, CAP-04b da `1/1`, CAP-17 y VID-03 dan `ok`, y no hay ninguna ⚠.
- Lo que difiere de `HEAD` es lo que ya explicó la ronda 1 (§1.3).
- QA-V3-06 no se repitió por segunda ronda seguida: `it2`, corrido justo después de `it3`, dio los mismos bytes.

En el VID-03 de la It. 2 el panel está abierto a los 0,900 s y a los 0,967 s. A los 1,000 s se cierra y empieza la caída. A los 1,033 s y a los 1,067 s se ve `Remo simulado` + `Conectado` mientras la vista sale, y `Rower` no aparece nunca.

### R3.4 Persistencia real (build de simulador de QA, Mono)

| Paso | Evidencia |
|---|---|
| La rutina de la demo | Corrí `SnapshotCli.SeedDatabase -seed Cap27` con Unity (exit 0). Deja `Escalera SPM` como queda en el paso 7 de la demo, sembrada con el repositorio de la app en `<scratch>/it3/qa-r3-seed/semilla/cap27.db`: 28 672 bytes, sin `-wal` ni `-shm`. La leí en solo lectura (`sqlite3` 3.50.4): `user_version` 8, `integrity_check` ok y 1 rutina con 4 pasos. Son calentamiento 300 s; trabajo 300 s con SPM 18, serie ×3 y `progression_spm` 2; descanso 120 s en la misma serie; y enfriamiento 300 s. La copié como `rower_app_dev.db` en una carpeta nueva (`<scratch>/it3/qa-r3-data`), que pasé en `ROWMATE_DATA_DIR` |
| 1.ª corrida (`RowMate.exe -batchmode -nographics -rowmate-db-roundtrip`) | exit 0 en 0,8 s. El registro dice `[APP] RowMate 1.0.0 (1) · WindowsPlayer · simulador=sí`, `[APP] base rower_app_dev.db (carpeta de desarrollo)`, `[DB] base rower_app_dev.db abierta (v8)` y `[DB] ida y vuelta: guardada` |
| 2.ª corrida, en otro proceso | exit 0: `[DB] ida y vuelta: sigue después de reabrir (2 pasos, 2 rutinas)` |
| La base después | `integrity_check` ok, v8 y 2 rutinas. `Escalera SPM` está **igual a como entró**: 4 pasos, y la serie con `group_repeat_count` 3 y `progression_spm` 2. `Ida y vuelta` tiene `created_at` `2026-10-28T10:37:28.646Z` (UTC con 3 decimales). En la carpeta solo queda `rower_app_dev.db`: al cerrar no quedan `-wal` ni `-shm` |
| Cierre forzado (AC-RUT-08-07), en una carpeta nueva con la misma semilla | Lo maté a los 763 ms, apenas el registro dijo `guardada`. Quedaron el `-wal` (12 392 bytes) y el `-shm`, sin *checkpoint*. Al reabrir: `sigue después de reabrir (2 pasos, 2 rutinas)`, y al cerrar el WAL pasó al archivo. La base quedó con las 2 rutinas y `Escalera SPM` intacta. Un primer intento, con una espera más lenta, mató el proceso a los 723 ms y llegó tarde: el *checkpoint* ya estaba hecho. Ese también reabrió bien |
| Separación de las bases | El build de simulador solo escribió `rower_app_dev.db` en las carpetas de prueba. La carpeta real (`LocalLow/iguisoft/RowMate`) no cambió: comparé la lista de archivos, con tamaño y fecha, antes de la semilla y después de las corridas del player, y dio **0 cambios**. Sigue sin `rower_app.db` ni `rower_app_dev.db`. El build normal recién compilado no tiene `ROWMATE_DATA_DIR` ni los textos del simulador (§R3.1). `DataLocation` y `DatabaseFiles` no cambiaron desde la ronda 1, en la que el build normal creó solo `rower_app.db` en `LocalLow` e ignoró la variable. No volví a abrir el build normal, para no crear esa base en la carpeta del usuario. `RPS.VariantsDoNotMix`, `DFT.NameByVariant` y las de `DLT` están en verde |
| **No se pudo**: crear la rutina con mouse y teclado en la ventana real, cerrar y reabrir | La ventana en primer plano es la del aviso del Firewall de las 03:56. Lo leí con `GetForegroundWindow`, sin tocar nada. Siguen los tres avisos (03:56, 03:57 y 05:20). No mandé teclas ni clics. La demo entera la cubren DEMO-01 a DEMO-12, con la UI real y SQLite real en el arnés y con `Reopen()` en DEMO-10, que da los mismos bytes que DEMO-09. También la cubre `RPS.ReopenShowsTheSame` |

### R3.5 ACs y deuda de la It. 2

**Los 102 AC quedan como en las rondas 1 y 2 (§3): 88 PASS, 13 PASS\*, 0 FAIL y 1 NO VERIFICABLE (AC-RUT-11-10).** El código de producción es el mismo desde la ronda 1, y todas sus pruebas volvieron a pasar: en la matriz, 102 de 102 están en PASA. Esta ronda suma evidencia a estos:

| AC | Evidencia nueva | Resultado |
|---|---|---|
| BLE-11-02 | `DeviceScreenTests.OutgoingConnectedViewIsFrozen` en verde en PlayMode, con y sin simulador. `DeviceSimulatorTests.DisconnectKeepsTheNameWhileTheConnectedViewLeaves` y `.SimulatedDropKeepsTheNameWhileTheConnectedViewLeaves` en verde con simulador. El VID-03 de la It. 2 salió igual byte a byte, con `Remo simulado` a los 1,033 s y a los 1,067 s | **PASS** (condición de la It. 2) |
| RUT-08-07 | El cierre forzado del player con el `-wal` sin *checkpoint* (§R3.4) | PASS |
| RUT-08-09 | Ida y vuelta entre dos procesos del player, con la rutina de la demo; DEMO-10 = DEMO-09 | PASS\* (la UI en la ventana real: NV) |
| HIS-17-04 | El player abrió la base v8 sembrada (`abierta (v8)`) y dejó `Escalera SPM` igual | PASS |
| HIS-20-01 | `[APP] base rower_app_dev.db (carpeta de desarrollo)`, y solo ese archivo en la carpeta | PASS |
| HIS-20-04 y HIS-17-03 | Ninguna corrida de esta ronda creó ni tocó bases en la carpeta del usuario: dotnet, las 4 suites, los 2 arneses, la semilla, el ensayo, los 3 builds y las 6 corridas del player. `<temp>/RowMate.Tests/` quedó vacía. Las 3 bases y las preferencias del legacy tienen el mismo SHA-256 y la misma fecha antes y después | PASS |
| APP-05-10 | `WindowsTitleBarTests` (4) en verde. CAP-27 sigue sin tomarse: es solo a pedido, y antes hay que resolver QA-V3-12 | PASS\* |

| Deuda It. 2 | Evidencia de esta ronda | Estado |
|---|---|---|
| QA-V2-01 | Lo de AC-BLE-11-02, en la tabla anterior | Cerrado |
| QA-V2-02 | En los dos builds aparece `PerformanceTestRunCleaner: 4 entradas …`, el inspector queda limpio y después no hay `Assets/Resources`. `SimulatorModeTests.PerformanceTestRunFilesAreRemovedBeforeTheBuild` y `ItTwoDebtTests.PerformanceTestRunFilesNeverReachTheBuildOrTheRepository` en verde | Cerrado |
| QA-V2-03 | IL2CPP → 3 en 9 s, con su log propio. `Il2CppIsDetectedFromTheVariationsFolder` en verde | Cerrado |
| QA-V2-04 / -05 | Los dos arneses dan `0 sondas de aspecto que fallan`; CAP-04b da `1/1` y CAP-17 de la It. 2 da `ok` | Cerrado |
| QA-V2-06 | Ningún `[Ignore]` dice "→ It. 2". Siguen los tres "→ It. 3" (QA-V3-05) | Cerrado (QA-V3-05 sigue) |
| QA-V2-07 | Los 45 archivos de `UI/Fonts/Generated`, `UI/Icons` y `Settings/` tienen el mismo `mtime` después de todo. `GeneratedFontAssetsAreCommittedWithoutDynamicData` y `OnlyTheBootstrapperWritesGeneratedAssets` en verde | Cerrado |
| QA-V2-08 | `WindowsTitleBarTests` (4) en verde, en las dos EditMode. La barra oscura en la ventana real se verificó en las rondas 1 y 2; el código no cambió | Cerrado |
| QA-V2-09 / -10 | El inspector del build normal queda limpio y mi búsqueda da 0 (§R3.1). `BuildOutputInspectorFindsTextsDiagnosticsAndTestMetadata` e `InspectorLooksForTheTextsTheSimulatorShows` en verde | Cerrado |
| QA-V2-11 | `SimulatorDoesNotCompileInANonDevelopmentPlayer` en verde: el registro muestra el `error CS1029` esperado del `#error` | Cerrado |
| QA-V2-12 | `ContrastTests` en verde, en dotnet y en Unity | Cerrado |
| QA-V2-13 | `VerticalScrollbarOnlyOnDesktop` en verde, y ninguna PNG de teléfono tiene la barra fija | Cerrado |
| QA-V2-14 | `CiUnityRunsTheSimulatorSuitesWithoutCommittingTheDefine` en verde. La corrida en GitHub sigue NV (P-08) | Cerrado en el código |
| QA-V2-15 | En el VID-03 de la It. 2 el panel está abierto a los 0,967 s y la caída empieza a los 1,000 s | Cerrado |

### R3.6 Revisión estática

- **Hilos de SQLite, IL2CPP y asignaciones.** El código de producción no cambió desde la ronda 1, así que vale lo del §6. Releí:
  - `SerialExecutor`: un hilo propio en segundo plano con una cola FIFO, `RunContinuationsAsynchronously`, `Join` con tope y baja en `DomainUnload`;
  - `Infrastructure/link.xml`: `RowMate.Infrastructure` y `Gilzoide.SqliteNet` con `preserve="all"`.

  En producción no hay `.Wait()`, `GetResult()`, `ConfigureAwait(false)`, `Task.Run` ni LINQ. Los `.Result` que aparecen son `SQLite3.Result` y `StepDialog.Result`. IL2CPP sigue NO VERIFICABLE porque falta el módulo; el humo de la base pasa en Mono.
- **El arreglo de QA-V3-11.** Está bien, y no encontré defectos nuevos (§R3.2).
  - El `trap` sale con 4 también si falla algo dentro del `finally`. Por ejemplo, si el proceso termina justo entre `HasExited` y `Kill()`. Es un borde sin consecuencia: el build ya está cerrado.
  - Nota de entorno: la prueba nueva arranca un `.exe` sin firma recién compilado. En una PC con Smart App Control encendido, Windows podría bloquearlo y la prueba fallaría con 4. No es el caso de esta PC.
  - En `ci-core`, `windows-latest` corre la parte de verdad y `ubuntu-latest` solo la estática, con `Assert.Ignore`, así que ahí hay un ignorado más. No vi esa corrida en GitHub, porque no hubo *push*.
- **Repositorio público.** Corrí el `git grep -nIE` con el patrón de rutas de la PC que pide la tarea. Da **0** en lo versionado y **0** con `--untracked`, este documento incluido. El patrón no se copia acá, porque su propio texto daría un resultado. Lo nuevo no tiene nombres de usuario ni datos del equipo, y `manifest.json` e `INDEX.md` no tienen rutas. La única coincidencia de palabras como `Temp` en `INDEX.md` es el nombre de la clase `TempDatabase`.
- **Legacy.** `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar.

### R3.7 Issues

| ID | Sev. | Resp. | Estado |
|---|---|---|---|
| QA-V3-11 | — | architect | **Cerrado.** El arranque y la espera de `capture-window.ps1` funcionan en Windows PowerShell 5.1, con y sin `-Arguments`. La prueba nueva y el ensayo los corren de verdad (§R3.2) |

**Siguen abiertos, sin cambios, los menores de las rondas 1 y 2.** Ninguno afecta a un AC:

- **QA-V3-12 (architect).** Con QA-V3-11 cerrado, la foto de CAP-27 ya se puede sacar, así que este es el primero que hay que resolver si el usuario pide CAP-27. Hoy la foto saldría con franjas de unos 8 px de lo que hay detrás de la ventana, por los bordes invisibles de `GetWindowRect`, y se escribiría por defecto en `docs/…/snapshots/`, que va al repositorio público. Hasta que se corrija: usar `-Out` en una carpeta temporal y mirar la imagen antes de pasarla a `docs/`.
- QA-V3-02 (architect): el motion del último bloque en VID-01, y lo demás de 04-design-notes §16.6.
- QA-V3-03 (architect): DEMO-05 sin las progresiones a la vista.
- QA-V3-04 (architect): el cursor de `Type` y el foco de `Tap` en el arnés.
- QA-V3-05 (architect): tres motivos de `[Ignore]` que todavía dicen "→ It. 3".
- QA-V3-06 (architect): documentar el determinismo del arnés. No se repitió en las rondas 2 y 3.
- QA-V3-07 (po): `\r` y `\t` en los campos de una línea, para decidir si entra como DV-20.
- QA-V3-08 (architect): los avisos del Firewall de los builds de desarrollo. **Siguen los tres en la pantalla del usuario** (03:56, 03:57 y 05:20), y el de las 03:56 tiene el primer plano. Esta ronda no sumó otro, porque usé la misma carpeta de build. El coordinador tiene que avisarle al usuario: hay que contestar `Cancelar`, porque la app no necesita red.
- QA-V3-09 (designer): la llave sobre acciones del editor en reposo.
- QA-V3-10 (architect): los `_ =` sin registro.

**Por rol.** El Architect no tiene nada bloqueante. Si se corrige QA-V3-02, -03 o -04, hay que regenerar las capturas (`capture-snapshots.ps1`, dos veces seguidas por QA-V3-06). Antes de cualquier toma de CAP-27, QA-V3-12. El PO tiene QA-V3-07 y el Designer, QA-V3-09. Para el REPORT: los avisos del Firewall pendientes y los 5 minutos de demo del usuario en el build de simulador (§8).

### R3.8 NO VERIFICABLE

Lo mismo que en el §8, con estas precisiones:

- **CAP-27.** Es solo a pedido. Para tomarla, hay que cerrar el `RowMate.exe` de la It. 2 y el Editor, contestar los avisos del Firewall y resolver QA-V3-12, o usar `-Out` en una carpeta temporal. Después se corre `capture-cap27.ps1 -Rehearsal` y, si da 0, `-ReplaceUserDatabase`.
- **La ventana real con mouse y teclado.** Sigue sin poder hacerse, porque el aviso del Firewall de las 03:56 tiene el primer plano.
- **`ITT.WindowCaptureStartsTheExeForReal` en `ci-core` de GitHub** (`windows-latest` y `ubuntu-latest`). Hasta el próximo *push*.

### R3.9 Limpieza

- Cerré los procesos que lancé: las 6 corridas del player (con los dos intentos de cierre forzado y sus reaperturas), los Unity de las pruebas, de los arneses, de los builds, de la semilla y del ensayo, y los ejecutables de prueba sin ventana. Quedan solo procesos que no lancé yo: la Hub con su `unity.exe serve` y el `RowMate.exe` de la It. 2. El `unity.exe` del *auth broker*, que la Hub abrió a las 07:37, ya se cerró solo.
- No apareció ningún aviso nuevo del Firewall. Siguen los tres de antes.
- La carpeta de datos del usuario sigue sin bases. Solo cambió `TestResults.xml`, que el Unity Test Framework escribe al terminar las PlayMode.
- `<temp>/RowMate.Tests/` quedó vacía, no quedó ninguna carpeta `RowMate.Cap27.*`, y la carpeta de la prueba de la ventana se borró.
