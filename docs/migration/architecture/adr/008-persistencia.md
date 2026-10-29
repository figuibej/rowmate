# ADR 008: SQLite con sqlite-net, esquema v7 + origin, preferencias JSON con las mismas claves

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Infrastructure, Core (puertos); F-HIS-17, F-HIS-20, F-RUT-08

## Contexto

El legacy guarda rutinas, pasos, sesiones y telemetría en SQLite (`sqflite`, esquema v7 de 4 tablas), preferencias en `SharedPreferences` (6 claves) y tokens en almacenamiento seguro. La base del simulador es otra (`rower_app_dev.db`). Hay que funcionar en Android, iOS, Windows y macOS con IL2CPP, sin plugins pagos, y testear en `dotnet`.

## Decisión

- **SQLite** con `com.gilzoide.sqlite-net` 1.3.3 (MIT; sqlite-net 1.9.172 + SQLite 3.53.4 precompilado para las 4 plataformas, compatible con IL2CPP). En `dotnet`, `sqlite-net-pcl` 1.9.172 (misma API pública): los repositorios se testean con SQLite real.
- Una conexión en un hilo propio (`SerialExecutor`), `journal_mode=WAL`, `foreign_keys=OFF` (paridad, DC-06), versión con `PRAGMA user_version`.
- **Esquema**: DDL idéntico a v7 + `workout_sessions.origin TEXT NOT NULL DEFAULT 'app'` + índice de `data_points(session_id, elapsed_seconds)`; `user_version = 8`. `origin` permite que "Limpiar sesiones de Strava" borre solo las importadas (Q-71).
- Archivo en `Application.persistentDataPath`, `rower_app.db` o `rower_app_dev.db`.
- **Preferencias**: `JsonFilePreferences` (archivo JSON propio, escritura atómica e inmediata) con las claves `hrm.deviceId`, `hrm.deviceName`, `scene.environment`, `immersive.lapsExpanded`, `immersive.repsExpanded`, `strava_upload_pref`, más `app.installId` (nueva).
- **Tokens**: `ISecureStore` con las mismas 5 claves; Keystore (Android), Keychain (iOS/macOS), DPAPI (Windows).
- **Base legacy**: no se importa por defecto (Q-11). `LegacyDatabaseImporter` queda diseñado (migraciones v1→v7 portadas) por si el PO lo pide.

## Alternativas descartadas

- **SQLitePCLRaw + NuGet en Unity**: integración manual de binarios por plataforma y `__Internal` en iOS; más frágil.
- **SQLite del sistema**: en Android es privado desde Android 7.
- **Archivos JSON para todo**: consultas de estadísticas y limpiezas más caras y sin paridad con el esquema.
- **`PlayerPrefs`**: registro de Windows, guardado diferido, no testeable en `dotnet`.

## Consecuencias

- El mismo código de repositorio corre en Unity y en `dotnet`.
- `link.xml` preserva los DTOs (sqlite-net mapea por reflexión).
- Si el PO pide importar, la columna `origin` de las sesiones legacy se infiere con una heurística que hay que validar.

## Cambios de la It. 3 (WP-A y WP-B)

Detalle en [03-rutinas/02-tech-design](../../iterations/03-rutinas/02-tech-design.md) §2 y Anexo A.1.

- **Carpeta** (AC-HIS-17-01): `Application.persistentDataPath` solo fuera de Android (en Windows `LocalLow/iguisoft/RowMate`). En Android, el almacenamiento **interno** de la app (`getDatabasePath(...).getParentFile()` por JNI, el mismo lugar que usa `sqflite`): `persistentDataPath` es el externo, visible por USB. Lo resuelve `Platform/Storage/AppDataLocation` en el hilo principal; `Bootstrap/DataLocation` lo reemplaza por `ROWMATE_DATA_DIR` **solo** en el Editor y en builds de desarrollo (tests, arnés y humo: AC-HIS-20-04; `BuildOutputInspector` lo busca en el build normal).
- **Archivo**: solo `AppRoot` elige el nombre, con `DatabaseFiles.NameFor(BuildFlags.IsSimulator)` (`rower_app.db` / `rower_app_dev.db`, AC-HIS-20-01, -02).
- **Pragmas**: `synchronous = FULL` (DT-07: con WAL y `NORMAL` un *commit* puede perderse ante un corte de luz), además de `journal_mode = WAL` y `foreign_keys = OFF`. La baja de una rutina borra sus pasos explícitamente en la misma transacción.
- **Nunca tocar lo que no se entiende** (AC-HIS-17-04, -05): sonda de solo lectura antes de abrir para escribir (encabezado y `user_version`, también desde el WAL); un archivo que no es una base, más corto que 100 bytes, de otra versión o sin reconocer queda con sus bytes intactos y la pantalla muestra el error de carga.
- **Hilos**: `SerialExecutor` (un hilo propio, cola, `Dispose` que termina lo encolado) en la app; `InlineDbExecutor` en las pruebas y el arnés (determinista cuadro a cuadro).
- **Humo en un *player*** (R3-01): `-rowmate-db-smoke <carpeta>` y `-rowmate-db-roundtrip` (solo en desarrollo; `tools/build/build.ps1 -DbSmoke`).

## Cambios de la It. 4a

Detalle en [04a-motor-hud-funcional/02-tech-design](../../iterations/04a-motor-hud-funcional/02-tech-design.md) §3.

- **Una base, dos repositorios**: `LocalStore` (Infrastructure/Database) es el dueño de la `RowMateDatabase` y de su `SerialExecutor`; `SqliteRoutineRepository` y `SqliteSessionRepository` la comparten (`ownsDatabase = false`). `AppRoot` la abre en `Awake` y la cierra al final de `OnDestroy`, después de la app y del backend, así termina lo encolado (`Dispose` espera la cola).
- **Sesiones y puntos, con el esquema v8 sin cambios** (AC-HIS-17-02, -07): `workout_sessions` y `data_points` dejan de estar vacías. El guardado es incremental (`SessionRecorder`): el alta en el toque; un punto cada 5 s encolado detrás del alta (`AddPointsAfterCreateAsync`, también si el alta falla); y el cierre en una transacción con los puntos que faltaban o que fallaron (un lote que falla una vez se reintenta en el cierre; uno que falla siempre deja la sesión cortada). Si el proceso se corta a mitad de la sesión quedan la fila sin `finished_at` y los puntos escritos hasta ahí (verificado en el build de simulador matando el proceso, con `tools/dotnet/RowMate.SessionList` de solo lectura).
- **Errores sin cortar el entrenamiento** (Q-13): un alta, un punto o un cierre que falla muestra `workoutSaveError` a lo sumo una vez mientras se rema, y el entrenamiento sigue en pantalla.
- **Pruebas**: SQLite real en dotnet (`SSR.*`, `LST.*`, también con `SerialExecutor` y una segunda conexión); en Unity, `WPS.*` sobre el archivo temporal compartido con las rutinas (`HostSessions.SameFileAsRoutines`, `InlineDbExecutor`) o sobre `ScriptedSessionRepository` (demoras y fallas). Ninguna prueba ni captura abre `rower_app.db` ni `rower_app_dev.db` (AC-HIS-20-04).
