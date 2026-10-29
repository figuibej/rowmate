# tools/dotnet — tests del Core sin Unity

Compila los assemblies puros de `Assets/RowMate/` (`noEngineReferences`) y corre sus tests con .NET 8, sin Unity, sin red (después de la primera restauración de NuGet) y sin Dart. Es la red de seguridad de la migración (ADR 013, ARCHITECTURE §15).

## El comando

Desde la raíz del repositorio (Windows PowerShell 5.1 o `pwsh` 7; en el CI, el mismo script):

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1
```

El script imprime y ejecuta, en este orden, los dos comandos de 02-tech-design §2.6:

```
"C:/Program Files/dotnet/dotnet.exe" build tools/dotnet/RowMate.sln --configuration Release --artifacts-path tools/dotnet/.artifacts
"C:/Program Files/dotnet/dotnet.exe" test tools/dotnet/RowMate.sln --configuration Release --no-build --artifacts-path tools/dotnet/.artifacts --results-directory tools/dotnet/.artifacts/TestResults --logger "console;verbosity=normal" --logger "trx;LogFilePrefix=rowmate-core"
```

- **Por qué dos comandos.** `dotnet test` solo compila los proyectos de test y sus dependencias: `RowMate.Tests.UnityApiCheck` (las fuentes de test contra NUnit 3.5.0 y `netstandard2.1`, el perfil de Unity) y las herramientas `RowMate.Tokens` y `RowMate.SessionList` quedarían sin compilar. El `build` compila los 10 proyectos; si falla, el script sale con ese código y **no** corre tests (QA-01).
- Sale con el primer código distinto de 0. Un test rojo deja rojo el job del CI (`.github/workflows/ci-core.yml`).
- Todo lo que se genera (binarios, `obj`, resultados) queda en `tools/dotnet/.artifacts/`, ignorado por git.

## Parámetros

| Parámetro | Qué hace |
|---|---|
| `-Filter "<expresión>"` | Agrega `--filter` al `dotnet test`. El paso 1 compila igual la solución entera |
| `-Ci` | Pone `ROWMATE_CI=1`: los tests de higiene que necesitan git fallan en vez de quedar *inconclusos* |
| `-Demo` | Solo `TestCategory=Demo`: se ven las líneas `[DEMO]` de la demo (02-tech-design §8.7), **cada una una sola vez** (QA-V-07): el adaptador de NUnit las escribe en vivo (`NUnit.ConsoleOut=1`) y con `verbosity=normal` no se repiten en "Standard Output Messages" |
| `-Matrix` | Además escribe `TestResults/ac-matrix.md` (AC → test → resultado, AC-BASE-09) desde el XML de NUnit que el adaptador deja en `TestResults/nunit/` (el `.trx` no guarda las categorías) |
| `-ArtifactsPath <carpeta>` | Por defecto `tools/dotnet/.artifacts`. Otra carpeta permite dos compilaciones en paralelo (por ejemplo WP-B y WP-C) sin pisarse |

Ejemplos:

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Filter "TestCategory=AC-BLE-02-02"
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Filter "TestCategory!=Integration"
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Demo
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix
```

Filtros de trabajo de la It. 1 (02-tech-design §9.0): WP-B `"FullyQualifiedName~Ble|FullyQualifiedName~RowingData|FullyQualifiedName~RowingFormats|FullyQualifiedName~RowingSimulator"`; WP-C `"FullyQualifiedName~Models|FullyQualifiedName~Localization"`. La categoría `Integration` (trazabilidad y cobertura de los vectores dorados) necesita los archivos de los tres paquetes.

Las mismas fuentes de test se corren **dentro de Unity** con `tools/unity/run-unity-tests.ps1` (ver `tools/unity/README.md`).

## Proyectos

| Proyecto | TFM | Fuentes (glob sobre `Assets/RowMate/`) | Espeja |
|---|---|---|---|
| `RowMate.Core` · `.App` · `.Infrastructure` · `.Simulator` | netstandard2.1 | `<Carpeta>/**/*.cs` | su asmdef (`RowMateAsmdef`) |
| `RowMate.Tests.Support` | netstandard2.1 | `Tests/Support/**/*.cs` | `RowMate.Tests.Support.asmdef` |
| `RowMate.Tests.EditMode` · `.Tests.Simulator` | net8.0 | `Tests/EditMode/**`, `Tests/Simulator/**` | sus asmdefs |
| `RowMate.Tests.UnityApiCheck` | netstandard2.1 + NUnit 3.5.0 | Support + EditMode + Simulator | herramienta (solo compila) |
| `RowMate.Tokens` | net8.0 (Exe) | `tools/tokens/**/*.cs` | herramienta |
| `RowMate.SessionList` | net8.0 (Exe) | `tools/sessions/**/*.cs` | herramienta de la demo (It. 4a) |

Agregar un archivo nunca toca un `.csproj` (globs). `AsmdefLintTests` verifica que cada `.csproj` espejo tenga las mismas referencias que su asmdef.

## Reglas para las fuentes compartidas con Unity

- C# 9 (`LangVersion 9.0` también en los tests `net8.0`): bloques `namespace X { }`, sin `global using`, `required`, *raw strings* ni `record struct`.
- **Sin `record` ni `init` en `Tests/**`** (QA-03): en Unity daría `CS0518`. Usar `class`/`struct` con constructor. Usar los records de Core (`with`, inicializadores) sí está permitido.
- API de NUnit: la intersección de 3.5 y 3.14. Lo que no compila en 3.5 lo detecta `UnityApiCheck`; lo que compila pero no se admite (`[Parallelizable]`, métodos de test `async`, `Assert.ThrowsAsync`…), `NUnitCompatibilityTests`.
- Categorías (`[Category("AC-…")]`, `Category = "…"`, `.SetCategory("…")`) y nombres (`TestName = "…"`, `.SetName("…")`) siempre como literales: `TraceabilityTests` los lee del código fuente.
- Código que solo existe en `net8.0` (por ejemplo `GC.GetAllocatedBytesForCurrentThread`) va bajo `#if NET5_0_OR_GREATER`.
- Los vectores dorados se leen con `GoldenVectors` (`Cases` para secciones chicas, `Rows`/`CheckRows` para barridos) y todo `double` por sus bits (`AsF64`, `GoldenRow.F64`).

## Tokens de diseño

`Presentation/Styles/Tokens.uss` se genera desde `Core/Design/Tokens.cs` (ADR 016). Después de editar `Tokens.cs`:

```
"C:/Program Files/dotnet/dotnet.exe" build tools/dotnet/RowMate.sln --configuration Release --artifacts-path tools/dotnet/.artifacts
"C:/Program Files/dotnet/dotnet.exe" tools/dotnet/.artifacts/bin/RowMate.Tokens/release/RowMate.Tokens.dll
```

Con `--check` no escribe y sale con 1 si el archivo difiere. (`dotnet run --project … --artifacts-path …` no sirve con el SDK 8.0.427: busca el ejecutable en la carpeta del proyecto.) `TokensUssSyncTests.UssMatchesTokens` falla si el archivo quedó desactualizado.

## Sesiones guardadas (herramienta de la demo, It. 4a)

`tools/sessions/Program.cs` (02-tech-design §3.6 de la It. 4a) lista las sesiones de la base local, la más nueva arriba: `id · inicio (hora local) · fin (o —) · rutina (o libre) · puntos`. Nunca abre la base del usuario con SQLite: copia el archivo (y su `-wal`) a una carpeta temporal propia, sin bloquear a la app, lee la copia y la borra. No imprime rutas: solo el nombre del archivo, también en los tres errores de texto fijo (código 2).

```
"C:/Program Files/dotnet/dotnet.exe" run --project tools/dotnet/RowMate.SessionList -- --variant simulator
"C:/Program Files/dotnet/dotnet.exe" run --project tools/dotnet/RowMate.SessionList -- --variant normal
"C:/Program Files/dotnet/dotnet.exe" run --project tools/dotnet/RowMate.SessionList -- --db <archivo>
```

Sin `--variant` ni `--db`, la base del simulador (`rower_app_dev.db`) en la carpeta de datos de la app (en Windows, `LocalLow/iguisoft/RowMate`, calculada en el momento). `--verbose` agrega el tipo de la excepción de un error (nunca su mensaje, que trae la ruta). `SessionListToolTests` la corre como proceso sobre bases de prueba.
