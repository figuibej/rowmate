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
