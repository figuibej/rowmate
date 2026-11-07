# tools/oracle — oráculo Dart de los vectores dorados

Genera los valores esperados de los tests corriendo el **código del legacy** (y la VM de Dart misma) con Dart 3.13.5, y los guarda en `Assets/RowMate/Tests/Golden/oracle/*.json`. Los tests de `dotnet` y de Unity los leen **sin Dart** (AC-BASE-03, ADR 012 §4, 02-tech-design §6).

## Cómo se corre

```
powershell -NoProfile -File tools/oracle/run-oracle.ps1 [-LegacyPath <ruta>] [-DartExe <ruta>] [-Only <nombre>[,…]] [-L10n]
powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Engine [-LegacyPath <ruta>] [-FlutterExe <ruta>]
powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Scene [-LegacyPath <ruta>] [-FlutterExe <ruta>]
powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Stroke [-LegacyPath <ruta>] [-FlutterExe <ruta>]
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
- Guiones (`scripts`, QA-V4a-L02, al empezar la It. 4b): los 15 caminos que la lente de paridad de la 4a comprobó a mano. Pasos de 0 s, de duración negativa y último de 0 s; sin meta con `Terminar`; terminar en pausa, en el segundo del cambio y en un múltiplo de 5; último paso por distancia; tiempo y distancia; el pulso del sensor en los puntos; pausa antes del primer segundo y pausa que cruza un cambio de paso; dos sesiones seguidas; varias pausas en un mismo segundo, y el monitor que informa la distancia cada 2 s (QA-V4a-L01). Cada guion es una lista de acciones `[ms, tipo, argumento]` con sus rutinas adentro, así `WorkoutGoldenTests` corre lo mismo en el motor nuevo. Diferencias esperadas: DV-37 (el `stepProgress` del legacy lanza con un paso negativo; queda anotado como `-99`) y QA-V4a-L01 (se comparan los puntos hasta el primer cambio de paso).

## La palada del legacy (`-Stroke`, It. 7)

Mismo proyecto `engine/` y mismos controles que `-Scene` (07-remero-palada/02-tech-design §6.2). `engine/test/stroke_vectors_test.dart` corre `StrokeCycle` en 1 001 fases, `BoatPainter.seatZ` / `handle` / `bladeTip` en las mismas y `SceneState.update` paso a paso (24 spm con pasos de 0,05 s; 24 y después 0 spm; 24 y después pausa; 24 spm a 1/60 s) y escribe `Assets/RowMate/Tests/Golden/oracle/scene_stroke.json`. `engine/test/boat_legacy_images_test.dart` dibuja **LEG-01** (`strokePhase` 0) y **LEG-02** (0,36): la LEG-02 de la 6a más el `BoatPainter`, en `docs/migration/iterations/07-remero-palada/snapshots/legacy/`. Los compara `Tests/EditMode/Scene/Stroke/StrokeGoldenTests.cs`: bit a bit la aritmética pura (ramps, barrido, fase, velocidad, distancia, tiempo, edad de los charcos) y `Within(1e-12)` lo que pasa por `sin`/`cos` (cabeceo, geometría, posición de los charcos).

## La escena del legacy (`-Scene`, It. 6a)

La escena del inmersivo del legacy importa `dart:ui` (`Color`, `Canvas`): necesita el motor de Flutter. Se corre en el mismo proyecto `engine/` que `-Engine` (06a-escena-lago/02-tech-design §6.2), **con los mismos controles**: las cinco líneas del `.gitignore` antes del primer `pub get`, `flutter pub get --offline`, la guardia del legacy antes y después (R-02, `status --porcelain` vacío, `--ignored` y metadatos iguales) y nada sin ignorar en `engine/` al terminar. `-Engine -Scene` corre las dos cosas.

- `engine/test/scene_vectors_test.dart` corre el código de la escena del legacy sin tocarlo (el `math.Random` de la VM, `TimeOfDay`, `SceneCamera`, `Environment.propsForSegment` y `visibleProps`, `horizonColor`, `tintWater` y las posiciones que dibujan `SkyPainter` y el respaldo de `WaterPainter`) y copia, con su cita, lo que es privado (el alfa de las estrellas, las nubes, el perfil de las cordilleras y sus picos nevados, el `_c` de la orilla y el alfa de las líneas del agua). Recibe `ROWMATE_ORACLE_OUT` y `ROWMATE_LEGACY_COMMIT`.
- `engine/test/scene_legacy_images_test.dart` dibuja las referencias **LEG-01 a LEG-07** (y `LEG-02`, `-05` y `-07` "con marcas": 4 dp en la base de cada objeto del legacy) con `SkyPainter`, el respaldo de `WaterPainter` y `ShorePainter`, y las escribe como PNG en `docs/migration/iterations/06a-escena-lago/snapshots/legacy/` (`ROWMATE_LEG_OUT`, que pone el script).
- Los comparan las pruebas de `Assets/RowMate/Tests/EditMode/Scene/` (sin Flutter), **cada valor con su comparador** (QA-6a-PAR-05): bit a bit la aritmética pura (el generador, los segmentos y su resumen FNV-1a de 64 bits, la paleta y el ambiente, las listas visibles, el `_c` de la orilla, las líneas del agua con la fuerza leída del archivo, las estrellas, las nubes y `horizonColor`); `Within(1e-12)` lo que pasa por funciones trascendentes y es del orden de 1 (`sunElevation`, la fuerza del astro, el perfil de las cordilleras); `Within(1e-9)` `lightFor` en dp. Las listas visibles se comparan con los tramos de `z` iguales ordenados por (tipo, `x`, semilla) en los dos lados (el `sort` de Dart no es estable).

| Archivo | Secciones |
|---|---|
| `scene_random.json` | `sequences`: 64 tiradas de `nextDouble`, `nextInt(1000)`, `nextInt(1 << 26)`, `nextInt(7)` y `nextBool` para 9 semillas |
| `scene_palette.json` | `minutes`: los 7 colores y el ambiente de `paletteFor`, `isNight` y `sunElevation` en cada minuto (1 440 filas, bits de `double`) |
| `scene_light.json` | `minutes`: `lightFor` (fuerza, luna, `x` e `y` en los siete tamaños) en cada minuto |
| `scene_sky.json` | `stars` (las 80 en los siete tamaños), `clouds` (las 7 a 0 y 1 234 m con t 0 y 3,2), `cloudColors`, `horizon` (`horizonColor` por hora entera), `profile`, `snowPeaks`, `shore` (el `_c` de la orilla) |
| `scene_lake.json` | `segments` (0 a 99 completos), `digests` (0 a 2 000 por FNV-1a de 64 bits), `visible` (0, 1, 49,5, 50, 920, 1 234, 5 000 y 42 195 m), `capSegments` y `capVisible` (el tope con el escenario de prueba, con `z` distintas alrededor del corte) |
| `scene_water.json` | `bands` (los tres tramos por hora entera) y `lines` (las 10 líneas a 0, 3, 920, 1 234, 4 096 y 42 195 m en V390 y H844 a 6, 12, 18 y 22 h) |

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
| `engine/test/engine_vectors_test.dart` (`-Engine`) | `workout_engine.json`: `scenarios` (11) y `scripts` (15) | A de la It. 4a |
| `engine/test/scene_vectors_test.dart` (`-Scene`) | `scene_random.json`, `scene_palette.json`, `scene_light.json`, `scene_sky.json`, `scene_lake.json`, `scene_water.json` | A de la It. 6a |
| `engine/test/scene_legacy_images_test.dart` (`-Scene`) | `docs/migration/iterations/06a-escena-lago/snapshots/legacy/LEG-01.png` a `LEG-07.png` y las tres con marcas | A de la It. 6a |
| `bin/series_vectors.dart` (`-Only series_vectors`) | `series_tracker.json`: `legacyCases` (los 12 de `test/series_tracker_test.dart`), `summaries`, `routines` (F-5 y el paso de 0 s) y `scripts` (las rutinas de F-5 con los remos de la spec, las pausas y el paso de 0 s; las entradas del tracker con el modelo de tiempo de la spec y la regla de pasos de la app, DV-26; las compara `SeriesGoldenTests`) | A de la It. 5 |

`run-oracle.ps1` es solo ASCII a propósito (Windows PowerShell 5.1 lee un `.ps1` sin BOM como ANSI).
