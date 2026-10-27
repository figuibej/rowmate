# tools/oracle — oráculo Dart de los vectores dorados

Genera los valores esperados de los tests corriendo el **código del legacy** (y la VM de Dart misma) con Dart 3.13.5, y los guarda en `Assets/RowMate/Tests/Golden/oracle/*.json`. Los tests de `dotnet` y de Unity los leen **sin Dart** (AC-BASE-03, ADR 012 §4, 02-tech-design §6).

## Cómo se corre

```
powershell -NoProfile -File tools/oracle/run-oracle.ps1 [-LegacyPath <ruta>] [-DartExe <ruta>] [-Only <nombre>[,…]] [-L10n]
```

- Por defecto `-LegacyPath` = `../rowmate-legacy` (al lado del repo) y `-DartExe` = `<flutter-sdk>/bin/cache/dart-sdk/bin/dart.exe` (o `dart` del `PATH`).
- `-Only fmt_vectors` corre solo ese script (cada WP corre los suyos).
- `-L10n` corre además `tools/l10n/arb_to_catalog.dart` (conversión única de los catálogos, WP-C) con `--legacy <ruta> --out Assets/RowMate/Localization --changes tools/l10n/it01-changes.json`, dentro de la misma guardia.

## El legacy no se toca

1. **R-02**: si algún `.dart` de `tools/oracle/` o `tools/l10n/` nombra el módulo de credenciales del legacy, aborta. Nadie lee ese archivo.
2. **Guardia antes**: `git --no-optional-locks status --porcelain` del legacy tiene que estar vacío; se guardan el listado `status --porcelain --ignored --untracked-files=all` (archivo por archivo) y una instantánea de metadatos (ruta, tamaño, fecha) de todo archivo fuera de `.git/`, sin abrir ninguno.
3. `tools/oracle/.dart_tool/oracle_package_config.json` (ignorado por git) mapea `package:rowmate/` a `<legacy>/lib/`. No hay `pubspec.yaml` ni `pub get`: nada se descarga ni se escribe en el legacy.
4. Cada `bin/*.dart` recibe `--out <Assets/RowMate/Tests/Golden/oracle> --legacy-commit <sha>`.
5. **Guardia después** (también si un script falla): repite los tres controles y sale con 1 si algo cambió, mostrando las diferencias.

Los scripts importan solo `dart:*`, `../lib/golden.dart` y archivos del legacy que solo importan `dart:` (`core/models/*`, `core/bluetooth/ftms_parser.dart`, `core/bluetooth/heart_rate_parser.dart`, `core/dev/rowing_simulator.dart`).

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

`run-oracle.ps1` es solo ASCII a propósito (Windows PowerShell 5.1 lee un `.ps1` sin BOM como ANSI).
