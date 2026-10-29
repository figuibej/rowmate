# tools/oracle — oráculo Dart de los vectores dorados

Genera los valores esperados de los tests corriendo el **código del legacy** (y la VM de Dart misma) con Dart 3.13.5, y los guarda en `Assets/RowMate/Tests/Golden/oracle/*.json`. Los tests de `dotnet` y de Unity los leen **sin Dart** (AC-BASE-03, ADR 012 §4, 02-tech-design §6).

## Cómo se corre

```
powershell -NoProfile -File tools/oracle/run-oracle.ps1 [-LegacyPath <ruta>] [-DartExe <ruta>] [-Only <nombre>[,…]] [-L10n]
powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Engine [-LegacyPath <ruta>] [-FlutterExe <ruta>]
```

- Por defecto `-LegacyPath` = `../rowmate-legacy` (al lado del repo) y `-DartExe` = `<flutter-sdk>/bin/cache/dart-sdk/bin/dart.exe` (o `dart` del `PATH`).
- `-Only fmt_vectors` corre solo ese script (cada WP corre los suyos).
- `-L10n` corre además `tools/l10n/arb_to_catalog.dart` (conversión única de los catálogos, WP-C) con `--legacy <ruta> --out Assets/RowMate/Localization --changes tools/l10n/it01-changes.json`, dentro de la misma guardia.

## El legacy no se toca

1. **R-02**: si algún `.dart` de `tools/oracle/` o `tools/l10n/` nombra el módulo de credenciales del legacy, aborta. Nadie lee ese archivo.
2. **Guardia antes**: `git --no-optional-locks status --porcelain` del legacy tiene que estar vacío; se guardan el listado `status --porcelain --ignored --untracked-files=all` (archivo por archivo) y una instantánea de metadatos (ruta, tamaño, fecha) de todo archivo fuera de `.git/`, sin abrir ninguno.
3. `tools/oracle/.dart_tool/oracle_package_config.json` (ignorado por git) mapea `package:rowmate/` a `<legacy>/lib/`. Los scripts de `bin/` no tienen `pubspec.yaml` ni `pub get`: nada se descarga ni se escribe en el legacy. La excepción es el proyecto `engine/` (abajo), con `pub get --offline` dentro de la misma guardia.
4. Cada `bin/*.dart` recibe `--out <Assets/RowMate/Tests/Golden/oracle> --legacy-commit <sha>`.
5. **Guardia después** (también si un script falla): repite los tres controles y sale con 1 si algo cambió, mostrando las diferencias.

Los scripts importan solo `dart:*`, `../lib/golden.dart` y archivos del legacy que solo importan `dart:` (`core/models/*`, `core/bluetooth/ftms_parser.dart`, `core/bluetooth/heart_rate_parser.dart`, `core/dev/rowing_simulator.dart`).

## El motor del legacy (`-Engine`, It. 4a)

`engine/` es un proyecto de Flutter aparte (02-tech-design §3.7 de la It. 4a, sonda S4-a): `engine/pubspec.yaml` depende del legacy **por ruta relativa** (`../../../../rowmate-legacy`, al lado del repo), de `flutter_blue_plus` y de `fake_async`; `engine/test/engine_vectors_test.dart` arma el `WorkoutProvider` del legacy con los dobles del remo y del pulsómetro de `test/workout_provider_hr_test.dart`, una `DatabaseService` que solo anota (nunca abre SQLite) y `fakeAsync`, y escribe `workout_engine.json` con `../../lib/golden.dart`.

- `-Engine` comprueba primero que el `.gitignore` tenga las cinco líneas de `tools/oracle/engine/` (`.dart_tool/`, `build/`, `.flutter-plugins*`, `pubspec_overrides.yaml`, `pubspec.lock`): `pub get` y `flutter test` dejan ahí archivos con rutas absolutas de la caché de `pub` del perfil. Si falta una, aborta antes de `pub get`.
- Corre `flutter pub get --offline` (con la caché local; nada se descarga) y `flutter test test/engine_vectors_test.dart` en `tools/oracle/engine`, **dentro de la guardia del legacy** (los mismos tres controles de antes y después). Le pasa la carpeta de salida y el commit del legacy en `ROWMATE_ORACLE_OUT` y `ROWMATE_LEGACY_COMMIT`.
- Al terminar, `git status --porcelain --untracked-files=all tools/oracle/engine` no puede mostrar nada fuera de `pubspec.yaml` y `test/`: lo que `pub get` deje sin ignorar hace fallar la corrida.
- Con `-LegacyPath` escribe `engine/pubspec_overrides.yaml` (ignorado: tiene la ruta absoluta); sin `-LegacyPath` lo borra si quedó de antes. `-FlutterExe` (o `ROWMATE_FLUTTER`) elige el `flutter`; si no, el del `PATH` (`<flutter-sdk>/bin/flutter`). Solo corre el motor: los scripts de `bin/` siguen con su comando.
- Escenarios (spec §0.4, con `R-4`): libre terminado a los 12,3 s; `Corta`; `Corta` con `R-4(19)`; `Prueba 4a`; `Distancia 100` con `R-4`, `R-4R`, `R-4Z` y la pausa de 20 s; la pausa de Q-58; la pausa del muestreo; la sesión de 3 s. Cada uno con los puntos (sin `id`), la fila del alta y la del cierre (sin fechas: el legacy toma la hora real), los objetivos, los cambios de paso y el orden de las operaciones. Los compara `WorkoutGoldenTests` (sin Flutter).

## Formato (`lib/golden.dart`)

```json
{
  "header": { "schema": 1, "generator": "tools/oracle/bin/fmt_vectors.dart", "dartVersion": "3.13.5 …", "legacyCommit": "<40 hex>", "generatedAt": "…Z" },
  "<sección chica>": [ { "x": {"v": 24.25, "bits": "4038400000000000"}, "out": "24.3" } ],
  "<sección tabla>": {
    "columns": ["x", "n", "out"],
    "rows": [
["4038400000000000",1,"24.3"]
    ]
  }
}
```

- **Sección chica** (≤ 200 entradas, casos con nombre): arreglo de objetos; un `double` como `f64(x)`.
- **Sección tabla** (toda sección de más de 200 entradas): `table(columns, rows)`, una fila por línea, celdas escalares; un `double` como `f64bits(x)` (16 hex, big-endian), un entero como número JSON.
- **Tope: 1 MiB por archivo** (`GoldenCoverageTests.FilesWithinSizeCap`). Si no entra, se achica el barrido.
- En C#: `GoldenVectors.Load/Cases/Rows/CheckRows`, `GoldenTable`, `GoldenRow.F64/Int/Text`, `JsonNode.AsF64/AsBytes`.

## Archivos

| Script | Salida | WP |
|---|---|---|
| `bin/fmt_vectors.dart` | `fmt.json`: `toStringAsFixed`, `round`, `parseFixed`, `toString`, `toStringPow2` (las 2 098 potencias de 2), `toStringTies` (500 empates), `mod`, `modInt`, `truncDiv`, `truncDivF64`, `iso8601`, `iso8601Local`, `isoParse`, `isoParseLocal`, `isoParseInvalid` | A |
| `bin/rowing_data_vectors.dart`, `ftms_vectors.dart`, `heart_rate_vectors.dart`, `simulator_vectors.dart` | `rowing_data.json`, `ftms.json`, `heart_rate.json`, `simulator.json` | B |
| `bin/routine_vectors.dart`, `session_vectors.dart`, `l10n_vectors.dart` | `routines.json`, `session.json`, `l10n_legacy.json` | C |
| `engine/test/engine_vectors_test.dart` (`-Engine`) | `workout_engine.json`: `scenarios` (11) | A de la It. 4a |

`run-oracle.ps1` es solo ASCII a propósito (Windows PowerShell 5.1 lee un `.ps1` sin BOM como ANSI).
