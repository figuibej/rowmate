# It. 1 — Fundaciones: gate de armonía (QA)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-27 |
| Iteración | 1 · `01-fundaciones` ([ROADMAP §2, It. 1](../../ROADMAP.md)) |
| Entradas | [01-po-spec.md](01-po-spec.md) (139 AC + 19 LT), [02-tech-design.md](02-tech-design.md), [04-design-notes.md](04-design-notes.md), `Assets/RowMate/Core/Design/Tokens.cs` (122 tokens), [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §1-§3 |
| Contrato | `rowmate-legacy` @ `3ae0f3a8bed5966700e830db3d8a5f050f34a0bc`, `lib/` y `test/` (solo lectura). `git status --porcelain` vacío antes y después de cada corrida. **No** se leyó `lib/core/strava/strava_secrets.dart` |
| Herramientas | Dart 3.13.5 (`<flutter-sdk>/bin/cache/dart-sdk/bin/dart.exe`) con scripts en el *scratchpad* de la sesión, fuera de los dos repos, que importan el legacy por URI `file:///` (solo lectura); .NET SDK 8.0.427 (`C:/Program Files/dotnet/dotnet.exe`) con proyectos de prueba en el *scratchpad* |
| **Veredicto** | **APROBADO CON OBSERVACIONES** |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** El QUÉ y el CÓMO están en armonía: los 139 AC tienen componente y test planificado; cada número, fórmula y texto que muestreé da lo mismo en el legacy, en la spec y en el algoritmo del diseño; los 19 casos legacy están mapeados con su nombre y su línea exactos; `Tokens.cs` cumple el contrato y compila contra la API de §4.2; los WP son disjuntos.

No hay bloqueantes. Hay **2 mayores** y **12 menores** (§2). Condiciones para arrancar:

1. **QA-01** (Architect, antes de cerrar WP-A): `dotnet test RowMate.sln` **no compila** `RowMate.Tests.UnityApiCheck` (ni `RowMate.Tokens`). Sin el arreglo, la garantía estática de AC-BASE-08 no existe.
2. **QA-02** (PO, antes de que WP-A escriba `AppIdentityTests`): reescribir AC-APP-16-02 para Windows según D-02, o rechazar D-02.
3. **QA-05** (Architect, antes de WP-C): la firma de `WorkoutSessionRecord` tal como está escrita no compila en Core.

El resto se puede corregir durante la implementación. QA lo revisa en `05-qa-verification.md`.

---

## 1. Cómo se verificó

| Qué | Cómo | Resultado |
|---|---|---|
| Valores del spec contra el legacy | Script Dart (`qa_oracle.dart`) que importa `rowing_data`, `ftms_parser`, `heart_rate_parser`, `rowing_simulator`, `interval_step`, `routine` y `workout_session` por `file:///…/rowmate-legacy/lib/...` | 19 paquetes FTMS, 15 de pulso, 23 `toStringAsFixed`, 11 `round`, 12 `toString`, 23 formatos de split y tiempo, simulador (ticks 1/60/120/600, pulso de los 12 primeros ticks, parar y reanudar, 8 potencias, topes), 28 etiquetas, 8 expansiones, 12 estadísticas y 3 fechas: **todos iguales al spec** |
| Algoritmos de §3.1 (`Fmt.Fixed`, `DartMath.Round`, `RoundToFixed`) | Los reimplementé al pie de la letra en Dart (`qa_fixed.dart`) y los comparé con la VM: barridos `k/1000`, `k/100`, `k/20` y `k·0.05` con k en [−100 000; 100 000], 400 000 aleatorios y bordes | **4 800 064 / 1 200 014 / 4 799 073 casos, 0 diferencias** |
| `Fmt.DartToString` (§3.1) | Reimplementación del algoritmo descrito, comparada con `toString()` en las 2 094 potencias de 2 de `double` | **46 diferencias** (QA-04) |
| Catálogos | `jsonDecode` de `lib/l10n/app_{es,en}.arb` | 159 claves y 7 metadatos en cada idioma; 5 claves con salto de línea; `won't` literal en `profileDisconnectContent` (en) |
| Elección de idioma | Lectura de `basicLocaleListResolution` (Flutter 3.47.6, `packages/flutter/lib/src/widgets/app.dart:146-238`) y `lib/main.dart:103-106` | El paso 3 de §3.3 equivale al SDK con `supportedLocales = [en, es]`. Da las 17 filas de AC-APP-09-02, incluidas las dos † |
| Equivalencia de UUID | Lectura de `flutter_blue_plus_platform_interface-9.0.3/lib/src/guid.dart:24-90` (pub cache) | Igualdad por `str128` en minúsculas: coincide con `BleUuid` de §3.1 |
| Colores y contraste | Lectura de `lib/shared/theme.dart:5-69`, `immersive_workout_screen.dart:531-533` y `:579-582`, `history_screen.dart:99`, `profile_screen.dart:99`; contraste recalculado en Dart (`qa_contrast.dart`) | Los 21 hex de contrato coinciden. Los contrastes de §4.4 y de 04-design-notes coinciden con los míos hasta la tercera decimal |
| `Tokens.cs` | Parser propio (`qa_tokens.dart`) y compilación (C# 9, `netstandard2.1`, `TreatWarningsAsErrors`) contra un *stub* con las firmas exactas de §4.2 | 122 tokens, nombres válidos y únicos, campo = `FieldNameFor`, 21 contratos con hex y rol exactos, 122 variables USS sin colisión. **Build succeeded** |
| Cobertura | Extracción automática de los `AC-…` del spec y de las filas de la matriz §8 | 139 / 139, ninguna fila sin test, ninguna fila sobrante |
| Comportamiento de `dotnet test` | Solución de prueba: un proyecto de test y una biblioteca `netstandard2.1` con un error de compilación, que ningún test referencia | `dotnet test S.sln` → **exit 0, Passed 1** (la biblioteca no se compiló); `dotnet build S.sln` → exit 1 con `error CS0029` (QA-01) |
| `IsExternalInit` | Dos bibliotecas `netstandard2.1` con *shim* interno y `InternalsVisibleTo` hacia un tercer proyecto | Consumidor `netstandard2.1` que declara un `record` → **`error CS0518`**. El mismo consumidor en `net8.0`, o uno que solo usa `with`/`init` sin declarar records, o uno con *shim* propio → compila (QA-03) |
| Firmas C# 9 | Compilación de las firmas de `WorkoutSessionRecord`, `Routine` y `ObservableValue<T>` copiadas de §3.3 | `WorkoutSessionRecord` → **`error CS8618`** en `StartedAt` (QA-05) |
| Tamaño de `fmt.json` | Generé en Dart las secciones `toStringAsFixed` y `parseFixed` de §6.3 con el formato de §6.2 | 3,10 MB + 4,49 MB contra un tope de 1 MB (QA-06) |

---

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-01 | **mayor** | architect | `dotnet test RowMate.sln` solo compila los proyectos de test y sus dependencias. `RowMate.Tests.UnityApiCheck` y `RowMate.Tokens` no son de test ni los referencia ningún test, así que **nunca se compilan** con el comando de §2.6, ni en la PC ni en el CI. La garantía "compila en cada `dotnet test`" (§2.4, AC-BASE-08, R-T6, D-10) no existe. Lo mismo vale para `ROWMATE_SIMULATOR` en `UnityApiCheck` | Prueba en el *scratchpad*: `dotnet test S.sln --artifacts-path .artifacts` → exit 0 y en `.artifacts/bin` solo está el proyecto de test; `dotnet build S.sln` → exit 1 (`error CS0029` en la biblioteca). 02-tech-design no tiene ningún `dotnet build` ni `--no-build` (§2.6, §7.1) | `run-tests.ps1` imprime y corre `dotnet build tools/dotnet/RowMate.sln -c Release --artifacts-path tools/dotnet/.artifacts` y después el `dotnet test … --no-build` de §2.6, y propaga el primer código distinto de 0. Otra opción: `<ProjectReference Include="../RowMate.Tests.UnityApiCheck/…" ReferenceOutputAssembly="false" />` (y lo mismo con `RowMate.Tokens`) en `RowMate.Tests.EditMode.csproj`. En los dos casos, `CiWorkflowTests` verifica que el script compila la solución entera. La línea de AC-BASE-08 en §8.5 tiene que decir cuál de los dos mecanismos se usa |
| QA-02 | **mayor** | po | AC-APP-16-02 pide `com.iguisoft.rowmate` como id de Windows, pero Unity no tiene un *application identifier* propio para Windows: el de `Standalone` es uno solo para Windows y macOS, y por R-14 tiene que ser `com.iguisoft.rowmate.unity` (D-02). Tal como está escrito, el AC no se puede cumplir ni testear | 01-po-spec §4.19 AC-APP-16-02; 02-tech-design §1.5 (`player.identity`), §3.1 (`AppIdentity.WindowsApplicationId`, "documental"), §10.2 D-02 | El PO acepta D-02 y reescribe la parte de Windows de AC-APP-16-02: "producto `RowMate` de `iguisoft`, carpeta de datos `LocalLow/iguisoft/RowMate`, ningún `com.example`; `com.iguisoft.rowmate` queda reservado para el MSIX de la It. 14". También puede rechazar D-02 y pedir otra solución. `AppIdentityTests.ProvisionalIdsAndNoComExample` sigue el texto nuevo |
| QA-03 | menor | architect | Un assembly `netstandard2.1` que ve por `InternalsVisibleTo` dos o más *shims* internos de `IsExternalInit` no puede **declarar** un `record` ni una propiedad `init` (`CS0518`). Los afectados son `RowMate.Tests.UnityApiCheck` (ve los de Core, App y Simulator) y, en Unity, `RowMate.Tests.EditMode` y `RowMate.Tests.Simulator`. En `net8.0` compila, así que el error aparece **solo** en `UnityApiCheck` (que hoy no se compila, QA-01) o recién en Unity (It. 2). Nada en el diseño se lo advierte a WP-B y WP-C | Prueba en el *scratchpad*: `ChkStd` (`netstandard2.1`) con `public sealed record Case(...)` → `error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported`; `ChkNet` (`net8.0`) → Build succeeded; con *shim* propio (como `Tests.Support`) → Build succeeded | Regla en §2.4 ("las fuentes de test no declaran `record` ni `init`; usan `class`"), con chequeo en `NUnitCompatibilityTests` (regex `\brecord\b|\binit\s*;` en `Tests/EditMode` y `Tests/Simulator`). Otra opción: un *shim* propio en `Tests/EditMode/Internal/` y otro en `Tests/Simulator/Internal/`, este último bajo `#if !NET5_0_OR_GREATER && !ROWMATE_API_CHECK` (con el define en `UnityApiCheck`), para no duplicar el tipo |
| QA-04 | menor | architect | El algoritmo de `Fmt.DartToString` de §3.1 no reproduce `toString()` de Dart en dos casos. (1) Potencias de 2: el intervalo de redondeo es asimétrico y el diseño solo prueba el candidato de *p* cifras **más cercano**. Si ese candidato cae fuera del lado angosto, salta a *p*+1 aunque el del otro lado entraba. (2) Empates exactos: el diseño redondea la mitad hacia arriba y *double-conversion* desempata al par. No hay usos visibles en la It. 1 (solo las alfas del USS, que dan bien), pero la API se declara "= `toString()`", y los vectores planificados (500 aleatorios) no lo detectan | `qa_oracle.dart`: 46 de 2 094 potencias de 2 difieren. Por ejemplo, 2^−44: Dart `5.684341886080802e-14`, diseño `5.6843418860808015e-14`; 2^89: Dart `6.189700196426902e+26`, diseño `…69014`. Detalle de 2^−44: el de 16 cifras más cercano (`…0801`) está 4,87e−30 por debajo, con un semi-intervalo inferior de 3,15e−30; `…0802` está 5,13e−30 por encima, con un semi-intervalo superior de 6,3e−30 | Para cada *p*, probar los **dos** candidatos (piso y techo a *p* cifras), quedarse con los que entran en el intervalo, elegir el más cercano y, en empate, el de último dígito par. Otra opción: portar `BignumDtoa` en modo SHORTEST. Sumar a la sección `toString` de `fmt.json` las 2 094 potencias de 2 y casos de empate (por ejemplo 2^−25) |
| QA-05 | menor | architect | `WorkoutSessionRecord` declara `public string StartedAt { get; init; }` no anulable y sin constructor. En Core (C# 9, `Nullable`, `TreatWarningsAsErrors`) no compila, y C# 9 no tiene `required` | Compilación de la firma de §3.3 → `error CS8618: Non-nullable property 'StartedAt' must contain a non-null value when exiting constructor` | Constructor `WorkoutSessionRecord(string startedAt)`, o `= ""` con un test de que el *codec* siempre lo llena. Corregirlo en §3.3 antes de que WP-C empiece, porque la regla del WP es "cada archivo entra compilando" |
| QA-06 | menor | architect | Con las secciones de §6.3 y el formato de §6.2 (`withIndent('  ')` y `{v, bits}` por `double`), `fmt.json` supera por mucho el tope de 1 MB por archivo de §6.2 y R-T11 | `qa_size.dart`: `toStringAsFixed` (24 008 entradas) da 3,10 MB y `parseFixed` 4,49 MB. Aun compacto, `toStringAsFixed` ocupa 1,63 MB | Elegir una: reducir los barridos (por ejemplo `k/100` con `n ≤ 2` y 500 aleatorios, ya que la paridad masiva la cubren las propiedades internas de §3.1 punto 3); partir `fmt.json` en un archivo por sección; o usar un formato compacto (una fila por línea `[bitsX, n, "out"]`) y subir el tope con su justificación. Actualizar `GoldenCoverageTests` |
| QA-07 | menor | architect | Ni el spec ni el diseño fijan el **tipo** de los parámetros de las 15 claves nuevas. AC-APP-10-04 exige el mismo nombre y tipo en es y en, y `LocalizationCatalog.Parse` exige metadatos, pero §4.17.2 solo da nombres y el diseño delega todo en `it01-changes.json`, que todavía no existe | 01-po-spec §4.17.2 (columna "Parámetros" sin tipo) y AC-APP-10-04; 02-tech-design §3.3 ("Conversión única") y `MessageArg.Text/Number` | Fijarlo en §3.3: `int` para `count`, `work`, `rest`, `m`, `s`, `meters`, `min`, `max` y `spm`; `String` para `split` (`MM:SS`), `list` y `delta` (lleva el signo `+`). `CatalogParityTests.FifteenNewKeys` verifica nombre y tipo |
| QA-08 | menor | architect | El Designer pide que `ContrastTests` verifique 13 pares más (`DesignerPairs`: G1-G3 y T1-T10, 04-design-notes §4.3). No figuran en 02-tech-design §4.4 ni en §8.5: WP-A no tiene el pedido en su alcance | 04-design-notes §4.3 frente a 02-tech-design §8.5 (AC-BASE-05: solo `HudAgainstWorstGlass`, `UiTextOnCard` y `WorstGlassIs2A2F37`) | Agregar `ContrastTests.DesignerPairs` (datos del test, fuera de `Tokens.cs`) a §8.5 y a WP-A, o rechazarlo de forma explícita. Mis recálculos coinciden con los valores del Designer (`text.tertiary`/card 5,140; `state.dangerText`/raised 4,639; `step.unknown`/card 3,159) |
| QA-09 | menor | architect | Falta decir **cómo** `TraceabilityTests` (en `RowMate.Tests.EditMode`) ve los AC y LT de `RowMate.Tests.Simulator` (AC-BLE-14-01 a -10 y LT-01 a LT-07). EditMode no referencia ese assembly ni puede hacerlo sin `ROWMATE_SIMULATOR`, así que si la verificación es por reflexión fallaría | 02-tech-design §8.6 (último párrafo) y §1.2 (referencias de `RowMate.Tests.EditMode`) | Especificar que se escanea el **texto** de `Tests/EditMode/**` y `Tests/Simulator/**` (`[Category("…")]`, `SetCategory("…")`, `TestName = "…"`) con `RepoPaths`, y que el resultado real sale de la matriz `.trx` (`-Matrix`) |
| QA-10 | menor | architect | La compatibilidad con el NUnit 3.5 de Unity se apoya en una lista de prohibidos, y `UnityApiCheck` compila contra NUnit **3.14**. Una API posterior a 3.5 que no esté en la lista (por ejemplo `TestCaseData.SetArgDisplayNames`, de 3.12) pasa en `dotnet` y falla recién en Unity | 02-tech-design §2.3 (`UnityApiCheck`: NUnit 3.14.0) y §2.4 (lista de `NUnitCompatibilityTests`) | Evaluar compilar `UnityApiCheck` contra el paquete NUnit 3.5.0 (incluyendo las fuentes de `Tests/Support` por *glob* en lugar de `ProjectReference`, para no mezclar versiones). Si no es viable, sumar a la lista `SetArgDisplayNames`, `Assert.ThrowsAsync`, `TestContext.AddTestAttachment` y `Is.Not.Default`, y documentar que la lista es la fuente de verdad |
| QA-11 | menor | po | AC-RUT-17-01 dice que el tiempo transcurrido del punto vale "0 por defecto", pero en el legacy es obligatorio. El diseño (D-12) hace lo correcto | `lib/core/models/workout_session.dart:157` (`required this.elapsedSeconds`); 02-tech-design D-12 | Reescribir AC-RUT-17-01: "tiempo transcurrido (obligatorio, el del monitor); SPM, paladas, distancia, split, potencia, calorías y pulso en 0 por defecto" |
| QA-12 | menor | po | Dos diferencias de producto quedan sin aceptar. (1) P-06 / AC-APP-09-07: en Unity, los menús del sistema (copiar y pegar, teclado) siguen el idioma **del sistema**, mientras que Flutter los dibuja en el idioma de la app. Con el sistema en portugués, el legacy los muestra en inglés y la app nueva en portugués. (2) P-04: Unity 6.3 exige macOS 12 y Windows 10 21H1, y el legacy corre en macOS 10.15 y en Windows 10 anteriores | 02-tech-design §10.3 (P-04, P-06) | El PO registra las dos decisiones: aceptar (1) como diferencia conocida (o pedirle a la It. 2 una barra de edición propia) y agendar (2) para los gates de la It. 2 (Windows) y de la 10b (macOS), como dice el propio spec |
| QA-13 | menor | architect | La demo del spec (§1, punto 2) muestra "los 8 paquetes FTMS decodificados" y el P99, pero en la matriz solo el caso de AC-BLE-02-02 lleva la categoría `Demo`, y no se dice qué línea imprime cada test | 01-po-spec §1; 02-tech-design §8.1 (fila AC-BLE-02-02) y §2.6 (`-Demo`) | Poner `Demo` en los 8 casos de `FtmsParserTests.ReferencePackets`, en `RowingWithoutNoise` (tick 60), `RoutineSummaryTests.SummaryTable` y `SessionStatsTests.P99Examples`, con `TestContext.Out.WriteLine` de los valores decodificados, para que `run-tests.ps1 -Demo` muestre la demo sin leer código |
| QA-14 | menor | architect | La guardia del oráculo usa `git status --porcelain`, que **no ve** escrituras en rutas ignoradas del legacy (`.dart_tool/`, `build/`, `*/ephemeral/` y el propio `lib/core/strava/strava_secrets.dart`, que está ignorado). Además, `git status` puede refrescar `.git/index` | `git -C rowmate-legacy status --porcelain --ignored` lista `!! .dart_tool/`, `!! build/` y `!! lib/core/strava/strava_secrets.dart`, entre otros; 02-tech-design §6.1, pasos 2 y 5 | En `run-oracle.ps1`, comparar antes y después `git --no-optional-locks -C <legacy> status --porcelain --ignored` (que tiene que dar igual) además del `--porcelain` vacío, y abortar si cambia |

---

## 3. Cobertura AC → componente → test

Control automático: los 139 IDs del spec (los 132 en negrita más el rango AC-BLE-02-02 a -09, expandido) contra las filas de la matriz de 02-tech-design §8. Resultado: **139/139**, ninguna fila sin test y ningún ID de más. En el gate, "Resultado" quiere decir que el test planificado prueba el valor exacto del legacy (§4). PASS/FAIL se marca en `05-qa-verification.md`.

| Ficha | AC | Componente (diseño) | Test planificado | Fidelidad verificada | Obs. |
|---|---|---|---|---|---|
| F-BLE-01 | 01-01 a 01-06 | `RowingData`, `RowingFormats`, `TimeFormats` | `RowingDataTests`, `RowingFormatsTests`, `TimeFormatsTests`, `SessionStatsTests.FormatsUseSharedTimeRules` | 13 splits y 10 tiempos idénticos (`100:00`, `24:00:00`) | El test de B para AC-BLE-01-06 no tiene que referenciar `SessionStats` (de C) |
| F-BLE-02 | 02-01 | `FtmsUuids` | `FtmsParserTests.Uuids` | `ftms_parser.dart:22-24` | — |
| F-BLE-02 | 02-02 a 02-09 | `FtmsParser` | `ReferencePackets` (8 casos) + `MatchesOracle` | 8/8 (§4) | QA-13 |
| F-BLE-02 | 02-10 a 02-16 | `FtmsParser` | `StrokeRateIsHalfRaw`, `SignedPowerAndAverageOnlyIfZero`, `SkippedFields`, `TruncatedAndTrailingBytes`, `RawUnavailableValues`, `MissingFieldIsZeroNotPrevious`, `EmptyNotificationGivesNoReading` | 11/11 paquetes | — |
| F-BLE-03 | 03-01 a 03-05 | `ScanFilter`, `BleUuid` | `ScanFilterTests` (LT-08, LT-09, `ClientSideOnlyOnWindows`, `SameRuleForRower`), `BleUuidTests` | `guid.dart` (igualdad por `str128`); Linux queda fuera (Q-50) | — |
| F-BLE-14 | 14-01 a 14-10 | `RowingSimulator` | `RowingSimulatorTests` (LT-01 a LT-07) + 4 `MatchesOracle_*` | Ticks 1/60/120/600, pulsos 1-12, parar y reanudar, 8 potencias y topes 0/99 | R-T4 (`pow`) |
| F-HRM-01 | 01 a 09 | `HeartRateParser`, `HeartRateReading` | LT-10 a LT-15 + 3 propios + `MatchesOracle` | 15/15 paquetes | — |
| F-RUT-01 | 01 a 05 | `StepType*`, `Tokens` | `StepTypeTests` | Colores de `theme.dart:56-69` e íconos de `routine_editor_screen.dart:447-458` | — |
| F-RUT-02 | 01 a 04 | `IntervalStep` | `IntervalStepTests` | `>0W`, `-10-5W`, `0 spm`, `1m 30s` con distancia | — |
| F-RUT-03 | 01 a 05 | `StepLabels.Duration` | `StepLabelsTests` + `MatchesLegacyLabels` | 13 duraciones (incluidas −30/−60/−90) | QA-07 |
| F-RUT-04 | 01 a 06 | `StepLabels.Targets/EditorSubtitle` | `StepLabelsTests` | 12 etiquetas (incluidas −65 → `-1:55/500m` y la línea completa) | QA-07 |
| F-RUT-05 | 01 a 04 | `Routine`, `RoutineSummary` | `RoutineTests`, `RoutineSummaryTests` | Resúmenes legacy (`1 pasos`, `2 series · 1 descansos`, `0 pasos`) | — |
| F-RUT-06 | 01 a 13 | `RoutineExpansion.Flatten` | `RoutineExpansionTests` + LT-16 | T1, ×0, ×−1, primer paso manda (4), mismo id no consecutivo (5), sin piso (4, 1, −2), 04-05 | — |
| F-RUT-07 | 01 a 05 | `RoutineExpansion.Positions` | `RoutinePositionsTests` (LT-16) | Posiciones de T1, 06-10 y 06-11 | — |
| F-RUT-16 | 01 a 05 | `WorkoutSession`, `WorkoutSessionCodec`, `IsoDate` | `WorkoutSessionTests`, `IsoDateTests` | `.628537Z`, `.628Z`, `.000Z`; lectura tolerante; 7 decimales → truncado | QA-05 |
| F-RUT-17 | 01 a 02 | `DataPoint` | `DataPointTests` | El índice es de la rutina expandida: `workout_provider.dart:122-123` (`_routine = expanded`) | QA-11 |
| F-RUT-18 | 01 a 09 | `SessionStats` | `SessionStatsTests` (LT-17 a LT-19) | Índices P99 de 9 *n*, 1..361 → 357, caso de 100 puntos, totales, pulso | — |
| F-APP-09 | 01 a 07 | `LanguageTag`, `LocaleResolver`, `LanguageState` | `LocaleResolverTests` (17 filas), `LanguageStateTests`; 09-07 solo `Ign.` | Algoritmo del SDK leído línea por línea (§4, caso 17) | QA-12 |
| F-APP-10 | 01 a 09 | `LocalizationCatalog`, `MessageFormatter`, `Localizer`, `L10nKeys` | `CatalogParityTests`, `LocalizerTests`, `L10nKeysTests`, `MessageFormatterTests` | 159 claves y 7 metadatos (tipos `String`/`int`); 5 saltos de línea; `won't` literal | QA-07 |
| F-APP-16 | 01 a 05 | `AppIdentity`, *bootstrapper* | `AppIdentityTests`, `BootstrapperStaticTests` | `pubspec.yaml:4` (`1.0.0+1`) | **QA-02** |
| F-APP-19 | 01 a 04 | `ci-core.yml`, `run-tests.ps1`, higiene | `CiWorkflowTests`, `RepoHygieneTests`, `PureCodeRulesTests`, `TokensUssSyncTests` | `.gitignore` actual cubre todas las rutas de ejemplo de §7.2 | **QA-01** |
| BASE | 01 a 03 | `Fmt`, `DartMath`, `GoldenVectors` | `FmtFixedTests`, `DartMathTests`, `CultureIndependenceTests`, `GoldenCoverageTests` | Algoritmos validados en 4,8 M casos | QA-04, QA-06 |
| BASE | 04 a 05 | `Tokens`, `TokenCatalog`, `UssTokenWriter`, `Contrast` | `TokensContractTests`, `TokenCatalogTests`, `TokensUssSyncTests`, `ContrastTests` | 21 contratos; contraste recalculado | QA-08, R-T1 |
| BASE | 06 a 07 | `IScheduler`/`FakeScheduler`, `IRandom`/`SequenceRandom` | `FakeSchedulerTests`, `SequenceRandomTests`, `NoNoiseIsDeterministicAndConsumesNoRandom` | `_jitter` no consume azar sin ruido (`rowing_simulator.dart:73-74`) | — |
| BASE | 08 a 10 | asmdefs, `UnityApiCheck`, trazabilidad, manifest | `AsmdefLintTests`, `PureCodeRulesTests`, `NUnitCompatibilityTests`, `TraceabilityTests`, `ManifestAndVersionTests` | §8 y §10 de este informe | QA-01, QA-03, QA-09, QA-10 |

---

## 4. Fidelidad con el legacy (muestra)

Legacy = salida del oráculo Dart 3.13.5 sobre el código del legacy. Diseño = la regla o el algoritmo de 02-tech-design aplicado al caso.

| # | Caso | Spec | Legacy | Diseño | Resultado |
|---|---|---|---|---|---|
| 1 | FTMS `2C 00 30 2A 01 E2 01 00 85 00 96 00` (AC-BLE-02-02) | 24.0 · 298 · 482 m · 133 (`02:13`) · 150 W | `spm=24.0 strokes=298 m=482 split=133(02:13) W=150` | Bit 0 apagado → `byte·0.5` y uint16; uint24; `sint16` | ✔ |
| 2 | FTMS `01 0B 2D 00 00 00 00 8C 2C 01` (AC-BLE-02-07) | 45 kcal · 140 bpm · 300 s | `kcal=45 bpm=140 s=300(05:00)` | kcal/h (2) y kcal/min (1) se saltean solo si entran | ✔ |
| 3 | FTMS `81 01 05 00 2D 00 00 00 00` (AC-BLE-02-12, bit 7) | 45 kcal | `kcal=45` | El bit 7 suma 2 al `offset` sin verificar el largo (`:97-99`) | ✔ |
| 4 | Otros 16 paquetes FTMS (02-03 a 02-16, incluidos `00 00 30`, `2C`, `00 00`, `01 01 FF FF FF FF FF`, vacío) | tabla de §4.2 | iguales (`null` para `2C` y vacío; 65535) | `< 2` → `null`; `offset + k <= largo` | ✔ 16/16 |
| 5 | `Fmt.Fixed(24.25, 1)` (AC-BASE-01, empate exacto) | `24.3` | `"24.3"` | `q = ⌊m·10ⁿ·2ᵉ + ½⌋`, el empate se aleja del cero | ✔ |
| 6 | `Fmt.Fixed(−0.04, 1)` y `Fixed(−0.0, 0)` (signo de cero) | — (diseño §3.1, paso 6) | `"-0.0"` y `"-0"` | `-` si el bit de signo está prendido, aunque `q = 0` | ✔ |
| 7 | `Fmt.Fixed(1.005, 2)` y `Fixed(99.95, 1)` (valor binario exacto) | — | `"1.00"` y `"100.0"` | Redondeo del binario exacto | ✔ |
| 8 | `DartMath.Round` de 0.49999999999999994, −0.05 y 4503599627370495.5 | 0 · 0 | `0 · 0 · 4503599627370496` | `t = trunc(x)`, `f = x − t`, `f ≥ 0.5` | ✔ |
| 9 | Pulso `[0x01, 72]` y `[0x07, 0x2C, 0x01]` | sin lectura · 300 con contacto | `null` · `300 contact=true` | `largo < (is16 ? 3 : 2) → null`; contacto `(flags>>1)&3` | ✔ |
| 10 | Simulador sin ruido, tick 60 (AC-BLE-14-05, LT-03) | 24.0 · 24 · 226 · 133 · 150 · 13 · 142 · 60 | idéntico; paladas acumuladas = `23.99999999999998` | `Floor(x + 1e-6)`, `RoundToFixed(spm, 1)` | ✔ |
| 11 | Parar y reanudar (AC-BLE-14-06) | 115 bpm, después 45 m · 12 s · 118 bpm | `off: … bpm=115 s=11` / `resume: m=45 … bpm=118 s=12` | Objetivo 70 sin remar; `_hr += (obj − _hr)·0.1` | ✔ |
| 12 | Split y kcal/h de Concept2 a 180 W (AC-BLE-14-03/04) | 124,8145 → 125 · 919,488 | `124.81453978697513` · `919.4879999999999` | `500·Math.Pow(2.80/W, 1/3)`; `(W·4·0.8604 + 300)/3600` | ✔ (con `Within(1e-9)`, R-T4) |
| 13 | P99 de vatios 1..361 y caso de 100 puntos (AC-RUT-18-02/04) | 357 · 198 W, split 198 (`03:18`), 29.8 | `W=357` · `W=198 spm=29.8 split=198(03:18)` | Índice `⌊(n−1)·0.99⌋` en `double`; spm sin redondear | ✔ |
| 14 | Split objetivo −65 y −5 (AC-RUT-04-04) | `-1:55/500m` · `00:55/500m` | iguales | `MinSec` con `~/` y `%` de Dart (`Mod` no negativo) | ✔ |
| 15 | Duración −60 y −90 (AC-RUT-03-04) | `0s` · `30s` | iguales | `TruncDiv` + `Mod`; `m > 0` | ✔ |
| 16 | Serie ×−1 (AC-RUT-06-08) y su resumen | 0 pasos | `[]`; resumen legacy `1 series · 0 descansos` | `repeat ≤ 0` → ningún paso; el resumen cuenta la plantilla | ✔ |
| 17 | Idioma `fr-FR, es-AR, en` y `fr-FR, en-GB, es` (filas † de AC-APP-09-02) | en · es | `basicLocaleListResolution`: la coincidencia perfecta (`en`/`es` sin región) se revisa **antes** de devolver la coincidencia por idioma anterior | §3.3, paso 3, en el mismo orden | ✔ (por lectura del SDK) |
| 18 | Fechas (AC-RUT-16-04) | `….628537Z` · `….628Z` · `….000Z` | iguales; `DateTime.parse` con 7 decimales trunca a `.123456` | `IsoDate.FormatUtc` (µs solo si ≠ 0) y `Parse` (trunca) | ✔ |
| 19 | `BleUuid`: `180d` ≡ `0000180D-0000-1000-8000-00805F9B34FB` | aceptado | `str128` en minúsculas y `==` por `str128` | Igualdad de 128 bits (`High`/`Low`) | ✔ |

Comparación masiva (§1): `Fixed`, `Round` y `RoundToFixed` tal como los describe el diseño dan 0 diferencias en 10,8 M comparaciones. `DartToString` diverge en potencias de 2 y en empates (QA-04).

---

## 5. Los 19 casos legacy

Verifiqué el nombre carácter por carácter y la línea de la declaración `test(` en `rowmate-legacy/test/`. Todos coinciden con el spec §5 y con 02-tech-design §8.6.

| LT | Legacy (archivo:línea) | Nombre (exacto) | AC | Test (diseño) | WP |
|---|---|---|---|---|---|
| LT-01 | `rowing_simulator_test.dart:7` | `más watts → pace más rápido` | AC-BLE-14-03 | `RowingSimulatorTests` | B |
| LT-02 | `:14` | `203 W ≈ 2:00 /500m (fórmula Concept2)` | AC-BLE-14-03 | ídem (`Within(0.5)`) | B |
| LT-03 | `:18` | `acumula distancia, remadas y tiempo mientras rema` | AC-BLE-14-05 | ídem | B |
| LT-04 | `:36` | `sin remar: 0 spm/watts/pace y totales congelados` | AC-BLE-14-06 | ídem | B |
| LT-05 | `:54` | `los objetivos se limitan a su rango` | AC-BLE-14-02 | ídem | B |
| LT-06 | `:62` | `con ruido, los watts quedan dentro de ±5 %` | AC-BLE-14-09 | ídem (`SystemRandom`) | B |
| LT-07 | `:70` | `heartRate expone el último pulso calculado por tick()` | AC-BLE-14-08 | ídem (fuera del grupo, como en el legacy) | B |
| LT-08 | `scan_filter_test.dart:24` | `con filtro en el cliente solo pasan los que anuncian el servicio` | AC-BLE-03-02 | `ScanFilterTests` | B |
| LT-09 | `:32` | `sin filtro en el cliente (el sistema ya filtró) pasa todo` | AC-BLE-03-03 | ídem | B |
| LT-10 | `heart_rate_parser_test.dart:5` | `bpm en uint8 sin información de contacto` | AC-HRM-01-02 | `HeartRateParserTests` | B |
| LT-11 | `:11` | `bpm en uint16 little-endian` | AC-HRM-01-03 | ídem | B |
| LT-12 | `:16` | `bits de contacto: 01 = no soportado, 10 = sin contacto, 11 = con contacto` | AC-HRM-01-04 | ídem | B |
| LT-13 | `:23` | `ignora Energy Expended y RR intervals` | AC-HRM-01-05 | ídem | B |
| LT-14 | `:29` | `bytes vacíos o truncados devuelven null` | AC-HRM-01-06 | ídem | B |
| LT-15 | `:35` | `UUIDs del perfil` | AC-HRM-01-01 | ídem | B |
| LT-16 | `routine_positions_test.dart:6` | `las posiciones quedan alineadas con flattenedSteps` | AC-RUT-06-01, AC-RUT-07-01 | `RoutinePositionsTests` | C |
| LT-17 | `session_stats_test.dart:7` | `avgHeartRate y maxHeartRate ignoran los ceros` | AC-RUT-18-06 | `SessionStatsTests` | C |
| LT-18 | `:14` | `sin pulso quedan en 0` | AC-RUT-18-07 | ídem | C |
| LT-19 | `:21` | `sin puntos quedan en 0` | AC-RUT-18-01 | ídem | C |

Las entradas también coinciden: `noise: false`, objetivos 150/24, 9999/1, 200 W, la fixture de T1 con `DateTime(2026)` y `_p(hr)` con `elapsedSeconds: 0`. El `reason: 'uint16 necesita 3 bytes'` de LT-14 conviene conservarlo como mensaje de la aserción.

---

## 6. Contradicciones spec ↔ diseño técnico ↔ diseño visual ↔ tokens

| # | Entre | Qué | Estado |
|---|---|---|---|
| C-1 | spec ↔ diseño | AC-APP-16-02 (id de Windows) frente a D-02 | **Abierta** → QA-02 |
| C-2 | spec ↔ diseño | AC-RUT-17-01 ("0 por defecto") frente a D-12 (obligatorio, como el legacy) | El diseño tiene razón → QA-11 |
| C-3 | spec ↔ diseño | AC-APP-09-07 (textos del sistema en el idioma de la app) frente a P-06 (siguen el del sistema) | Diferencia de producto → QA-12 |
| C-4 | diseño visual ↔ diseño técnico | `DesignerPairs` (04 §4.3) sin lugar en §8.5 | → QA-08 |
| C-5 | diseño técnico (interna) | §6.2/R-T11 (tope de 1 MB) frente a §6.3 (barridos de `fmt.json`) | → QA-06 |
| C-6 | diseño técnico (interna) | §2.4 ("`UnityApiCheck` compila en cada `dotnet test`") frente a §2.6 (solo `dotnet test`) | → QA-01 |
| C-7 | spec ↔ diseño | AC-BASE-05 nombra "`#2A2F37`" como peor fondo; con ese valor redondeado, `#FEA845` da 6,987, `#4CC9F0` 6,999 y `#FF5A7E` 4,497 (no pasan). El diseño compone sin redondear (7,004 · 7,016 · 4,508) | Resuelto por el método de P-05 que el propio spec dejaba elegir. Margen ≤ 0,02: cualquier cambio de tono rompe el test, y es bueno que así sea |
| C-8 | spec ↔ tokens | Los 21 contratos de AC-BASE-04 frente a `Tokens.cs` | Sin diferencias (control automático) |
| C-9 | spec ↔ diseño visual | P-07: `pause_circle_outline` (legacy) frente a `pause_circle` (ART_BIBLE) | Resuelto: glifo `E1A2` de la fuente sin relleno; en la It. 1 el dato es `pause_circle_outline` (AC-RUT-01-04) |
| C-10 | diseño técnico (interna) | El comentario de `IClock.LocalNow` remite a "D-12", pero la decisión de `DateTimeOffset` es D-11 | Errata; corregir al cerrar |
| C-11 | diseño visual ↔ diseño técnico | ANIMATION_BIBLE §1 frente a `Tokens.cs` (7 duraciones y 6 curvas con palabra clave USS válida) | Sin diferencias. El ejemplo `ease.standard` de §4.3 es ilustrativo |

---

## 7. Paquetes de trabajo: disjuntos y dependencias

- **Conteo y dueños**: WP-A 139, WP-B 22, WP-C 60, total 221, igual que §1.1. No encontré archivos repetidos entre listas. En las carpetas compartidas (`Core/Ble`, `Core/Models`, `Tests/EditMode/Ble`, `Tests/EditMode/Models`, `tools/oracle/bin`, `Tests/Golden/oracle`) los nombres de archivo de cada WP son distintos.
- **B depende solo de A**: `TimeFormats`, `Fmt`, `DartMath`, `BleUuid`, `BleScanResult`, `PlatformKind`, `IRandom`, `SystemRandom`, `GoldenVectors`, `GoldenJsonExtensions`, `HexBytes`, `SequenceRandom`, `RepoPaths` y `lib/golden.dart`. Ninguna firma de B usa un tipo de C.
- **C depende solo de A** (y de `Tokens.cs`, que ya existe y compila): `TimeFormats`, `Fmt`, `DartMath`, `IsoDate`, `ObservableValue`/`IReadOnlyObservable`, `JsonParser`/`JsonNode`, `ColorToken`, `Tokens.Step*`, `GoldenVectors` y `RepoPaths`. Ninguna firma de C usa `RowingData` ni otro tipo de B (`DataPoint` se arma con campos sueltos).
- **A no depende de B ni de C en compilación**. `GoldenCoverageTests` y `TraceabilityTests` necesitan los archivos de B y C, pero son `Integration`, y el "Listo cuando" de A los excluye.
- **Atención WP-A**: `Tokens.cs` usa el argumento con nombre `ussKeyword:` y las fábricas `Contract/Hex/HexAlpha/Dp/Ms/CubicBezier/Of` con el orden de §4.2. WP-A no puede tocar `Tokens.cs`, así que esas firmas (nombres de parámetros incluidos) son contrato. Tampoco puede limitar `Y1/Y2` de `EasingToken` a [0; 1] (`ease.emphasis` usa 1,56).
- **Regla "el árbol compila siempre"**: las firmas de §3 tienen que compilar tal cual. Hoy falla `WorkoutSessionRecord` (QA-05), y un test de B o C que declare un `record` compila en `net8.0` y rompe `UnityApiCheck`/Unity (QA-03).

---

## 8. `tools/dotnet`: *globs*, comando y compilación

- **Globs** (§2.1-§2.3): correctos. `$(UnityAssets)Simulator/**` no toma `SimulatorPresentation/` ni `Tests/Simulator/`. `Core/**` incluye `Design/Tokens.cs`, `Internal/IsExternalInit.cs` y `AssemblyInfo.cs` (IVT). Con `EnableDefaultCompileItems=false` y `--artifacts-path`, ningún `obj/` ni `bin/` cae dentro de un *glob*. `UnityApiCheck` toma `EditMode/**` + `Simulator/**`, cuyos `AssemblyInfo.cs` son solo comentarios, así que no hay atributos duplicados.
- **Fuera de `Assets/`**: ningún `.cs` de `Assets/RowMate/` queda sin asmdef (`Localization/` y `Tests/Golden/` tienen solo JSON), así que nada cae en `Assembly-CSharp`.
- **Comando** (§2.6): `--artifacts-path` y `--results-directory` existen en el SDK 8.0.427, que es el instalado y el de `global.json`. Falta compilar la solución → **QA-01**.
- **CI** (§7.1): `actions/setup-dotnet@v4` con `global-json-file` y `rollForward: latestFeature` instala la última 8.0, compatible con 8.0.427. `pwsh` existe en los dos *runners*. No tiene `continue-on-error`. Los tests `[Ignore]` salen como *Skipped* y no ponen el job en rojo, como se espera para lo NV.
- **`.gitignore`**: el actual ya cubre todas las rutas de ejemplo de `RepoHygieneTests` (`**/StravaSecrets*.cs`, `/tools/strava/strava.local.json`, `*.keystore`, `*.ulf`, `/Assets/ThirdParty/**` con excepción de `.meta`). Falta `/tools/dotnet/.artifacts/`, que WP-A tiene en su lista. El repo no tiene commits: `git ls-files` y `git check-ignore --no-index` funcionan igual.

---

## 9. Compatibilidad C# 9 / `netstandard2.1` de las firmas

- Sin construcciones de C# 10 o posterior (`namespace X;`, `global using`, `record struct`, `required`, *raw strings*): namespaces en bloque e `ImplicitUsings` desactivado, con `LangVersion 9.0` también en los tests `net8.0`. Bien.
- APIs de `netstandard2.1` usadas en las firmas: `ReadOnlySpan<byte>`, `Span<char>`, `BigInteger`, `HashCode`, `ValueTuple` (`Contrast.Composite*`), `TimeZoneInfo.CreateCustomTimeZone`, `BitConverter.DoubleToInt64Bits`, `IObservable<T>` y `SynchronizationContext.Post(…, object?)`. Todas existen.
- Sobrecargas: `DartMath.Mod/TruncDiv(long|double)` con argumentos `int` eligen `long` (conversión mejor), que es lo correcto para duraciones y splits. `FtmsParser.ParseRowerData(byte[] | ReadOnlySpan<byte>)` no es ambigua. `EventStream.Subscribe(Action<T> | IObserver<T>)` tampoco lo es con una lambda.
- `ObservableValue<T>(…, IEqualityComparer<T>? …)` con `T` sin restricción es válido en C# 9 (compilado).
- **Fallan**: `WorkoutSessionRecord.StartedAt` (QA-05) y los `record` o `init` declarados en fuentes de test `netstandard2.1` (QA-03).
- `Tokens.cs`: compila (C# 9, `netstandard2.1`, `TreatWarningsAsErrors`) contra un *stub* con las firmas de §4.2.

---

## 10. Manifest, asmdefs, `ProjectVersion` y *bootstrapper* (revisión estática, NV)

- `Packages/manifest.json` (§1.3) = ARCHITECTURE §1.2, paquete por paquete y versión por versión (22 entradas, URP y Shader Graph 17.3.0, UTF 1.6.0, Input System 1.20.1).
- `ProjectSettings/ProjectVersion.txt` = `m_EditorVersion: 6000.3.27f1`, como ARCHITECTURE §1.1. El test acepta además la línea `WithRevision`.
- 14 asmdefs = ARCHITECTURE §3.1 en nombres, carpetas, `noEngineReferences`, `defineConstraints`, `includePlatforms` y referencias. Única diferencia: `RowMate.Infrastructure` no referencia todavía `Gilzoide.SqliteNet` ni `Newtonsoft.Json.dll` (D-07, documentada; entra en la It. 3). Todos tienen `autoReferenced: false` y referencias por nombre, sin GUID. Los de test tienen `overrideReferences: true` + `nunit.framework.dll` + `UNITY_INCLUDE_TESTS`.
- *Bootstrapper*: las APIs nombradas existen en Unity 6 (`PlayerSettings.SetApplicationIdentifier(NamedBuildTarget, …)`, `SetScriptingBackend`, `SetApiCompatibilityLevel(…, NET_Standard)`, `Android.minSdkVersion = AndroidApiLevel26`, `targetArchitectures = ARM64`, `iOS.targetOSVersionString`, `macOS.buildNumber`, `GraphicsSettings.defaultRenderPipeline`, `QualitySettings.renderPipeline`, `UniversalRenderPipelineAsset.Create(rendererData)`, `GetScriptingDefineSymbols`). No crea niveles de calidad (QA-P-10) y no agrega `ROWMATE_SIMULATOR` (R-01). Quedan **NV hasta la It. 2**: los nombres de assembly de los paquetes, `activeInputHandler` por `SerializedObject`, `UniversalRendererData` sin `postProcessData` (R-T2), `Assets/csc.rsp` aplicado a los asmdefs (R-T12), que un asmdef excluido por `defineConstraints` (`RowMate.Simulator`) no rompa a quien lo referencia por nombre (`Bootstrap`, `Editor`, `Tests.PlayMode`) y la versión de UTF 1.6.0 en 6000.3.27f1.

---

## 11. Tokens y diseño visual

- `Tokens.cs`: 122 tokens (ver §1). Los 38 obligatorios de §4.4 coinciden en nombre, valor, alfa, rol y contrato.
- Contrastes recalculados: peor vidrio `(41,72; 46,88; 54,62)`, L = 0,027820 → `#FEA845` 7,0041 · `#4CC9F0` 7,0163 · `#38CBEF` 7,0399 · blanco 13,4927 · `#FF5A7E` 4,5077 · `#FF5F59` 4,5188 · `#35A0C9` 4,5124 · `#7C9AA8` 4,5263. Mezcla lineal informativa: L = 0,1434 (`#696A6B`), blanco 5,43, split 2,82, pulso 1,81. Coincide con R-T1 y 04 §5.
- `state.dangerText` `#FF6B6B` → `#FF7272`: no es contrato y conserva el matiz. Hay que pasarlo a ART_BIBLE §3.5 al cerrar.
- **Riesgo R-T1 (no es un issue de la It. 1)**: `ContrastTests` va a pasar con mezcla sRGB, pero si Unity compone la UI en lineal, el respaldo del HUD da 2,8:1 en el split. Es NV hasta la medición de la It. 2 (04 §5). El Designer recomienda la opción A (`forceGammaRendering`). QA pide que la decisión quede tomada **antes del gate de la 4a**, con la captura de píxel como evidencia.

---

## 12. Oráculo y legacy sin escrituras

- Los scripts del diseño importan solo `dart:` y archivos del legacy que solo importan `dart:` o rutas relativas dentro de `core/models` (lo verifiqué con `grep ^import`). La configuración de paquetes se escribe en `tools/oracle/.dart_tool/`, que está ignorado, con `languageVersion 3.3` = `environment.sdk` de `pubspec.yaml:7`, igual que el legacy.
- Mis corridas (6 scripts Dart y la lectura de los `.arb`) dejaron `git -C ../rowmate-legacy status --porcelain` vacío (0 líneas) antes y después, y ningún archivo del legacy con fecha posterior al spec (`find -newer`).
- Mejora a la guardia de §6.1: QA-14 (`--ignored` y `--no-optional-locks`).

---

## 13. Huecos del spec y riesgos

| Tipo | Qué | Dónde se resuelve |
|---|---|---|
| Hueco | Tipos de los parámetros de las 15 claves nuevas | QA-07 |
| Hueco (fuera de la It. 1, para no perderlo) | `Routine.toMap` guarda `created_at` en hora **local** sin zona (`routine.dart:111`), a diferencia de la sesión, que va en UTC con `Z`. `IsoDate.FormatLocal` ya existe para eso | It. 3 (persistencia) |
| Hueco (fuera de la It. 1) | El *chip* de fase tiene un cuarto estado `('—', Colors.white38)` (`immersive_workout_screen.dart:534`) sin token de contrato | It. 4a |
| Riesgo | R-T4: `Math.Pow` en glibc, en el CRT de Windows y en la VM de Dart. Mitigado con `Within(1e-9)` y los enteros contra el oráculo en los dos SO del CI | Verificación (05) |
| Riesgo | R-T1: mezcla lineal en Unity | It. 2 (medición), decisión antes de la 4a |
| Riesgo | Márgenes de contraste ≤ 0,02 en 4 tokens del HUD | Está bien que el test falle ante cualquier cambio de tono |
| Riesgo | NV sin Editor: §10 | It. 2 (primer open obligatorio) |
| Riesgo | Sin repositorio remoto, AC-APP-19-01/-02 quedan NV (P-03) | QA corre `run-tests.ps1 -Ci` en la PC |

---

## 14. Qué revisa QA en `05-qa-verification.md`

1. Salida real de `run-tests.ps1` (con la compilación de la solución de QA-01) y de `run-tests.ps1 -Ci`, `-Demo` y `-Matrix`.
2. `UnityApiCheck` compilando de verdad: introduciré a propósito un `record` en una copia temporal de un test para confirmar que falla (QA-03) y lo revierto.
3. Los 19 LT con su nombre exacto en la salida y los 139 AC en la matriz `.trx`.
4. `fmt.json` y los demás JSON dentro del tope que se acuerde (QA-06), con el mismo `legacyCommit`.
5. Las potencias de 2 en `toString` (QA-04) y los empates.
6. `git status --porcelain` (y `--ignored`) del legacy, sin cambios.
7. Revisión estática de asmdefs, manifest, *bootstrapper* y `ci-core.yml` contra §10.
