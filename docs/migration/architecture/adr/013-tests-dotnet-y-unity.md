# ADR 013: Tests compartidos entre dotnet (net8.0) y Unity Test Framework

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** `tools/dotnet/`, `Assets/RowMate/Tests/` (R-09)

## Contexto

Todo test del legacy se porta y toda regla pura se verifica con `dotnet test`. Cuando haya Editor, los mismos tests tienen que correr en Unity Test Framework 1.6 (NUnit 3.5 propio de Unity).

## Decisión

- `tools/dotnet/RowMate.sln` con **un `.csproj` por asmdef puro, con las mismas referencias** (`netstandard2.1`, `LangVersion 9`, `Nullable enable`), que **incluyen las fuentes de `Assets/`** (`EnableDefaultCompileItems=false`, `Compile Include`): Core, App, Infrastructure, Simulator, `RowMate.Tests.Support` (netstandard2.1) y `RowMate.Tests.EditMode` / `RowMate.Tests.Simulator` (`net8.0`, NUnit 3.14.0). Así `dotnet build` detecta las mismas referencias faltantes que Unity.
- **`RowMate.Tests.Support`** es un asmdef propio en `Tests/Support/` (`UNITY_INCLUDE_TESTS`, `noEngineReferences`, sin `[Test]`), referenciado por EditMode, Simulator y PlayMode; no referencia `RowMate.Simulator`. EditMode y Simulator con `includePlatforms: ["Editor"]`.
- Un test de *lint* valida todos los `.asmdef`, el manifest y `ProjectVersion.txt`, y compara el grafo de asmdefs con el de los `.csproj` (revisión estática sin Editor).
- `Tests/Support` trae `TestMainThreadContext` e `InlineDbExecutor` (asincronía determinista sin `UnitySynchronizationContext`) y `GoldenVectors`, que lee el JSON dorado de `Assets/RowMate/Tests/Golden/oracle/` generado con el oráculo Dart (ADR 012 §4; ruta cambiada en la It. 1, D-01).
- Los mismos archivos los compila Unity con `RowMate.Tests.EditMode` y `RowMate.Tests.Simulator`. Los tests de UI y escena (`Tests/PlayMode`) son solo Unity.
- **Dentro de Unity** se corren con `tools/unity/run-unity-tests.ps1` (batchmode, `-runTests`, resultados NUnit en `tools/unity/.artifacts/`). `RowMate.Tests.Simulator` solo existe con `ROWMATE_SIMULATOR` (como `RowMate.Simulator`), y el define nunca queda puesto (R-01): con `-Simulator` el script lo activa **solo para esa corrida** con `RowMate.EditorTools.SimulatorDefine.EnableForTests`, corre los tests y **siempre** (en `finally`) lo deja como estaba con `RestoreAfterTests` (QA-V-05 de la It. 1). Mientras dura la corrida, `SimulatorBuildGuard` hace fallar cualquier build; en el CI, un define olvidado en `ProjectSettings` hace fallar `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator`. Desde el Editor, lo mismo a mano: menú `RowMate/Modo simulador`, Test Runner y otra vez el menú.
- API de NUnit permitida: la intersección de 3.5 y 3.14 (sin `Assert.Multiple` ni NUnit 4).
- Datos de prueba ubicados con `RepoPaths` (sube hasta `ProjectSettings/ProjectVersion.txt`).
- Trazabilidad: comentario `// legacy: test/<archivo>:<línea>` y `[Category("AC-…")]` por criterio del PO.
- `ci-core.yml` corre `tools/dotnet/run-tests.ps1 -Ci` en cada push desde la It. 1: `dotnet build` de la solución entera y después `dotnet test --no-build` (QA-01 de la It. 1).

## Alternativas descartadas

- **Copiar las fuentes a un proyecto aparte**: se desincronizan.
- **NUnit 4 en dotnet**: su API clásica cambió de lugar (`ClassicAssert`) y no compilaría igual en Unity.

## Consecuencias

- Un test que use API de Unity no compila en `dotnet`: el límite de "puro" se verifica solo.
- Los tests de widgets del legacy se parten en reglas (dotnet) y vista (PlayMode).
- Regenerar el JSON dorado necesita Dart y el legacy al lado; correr los tests, no (el CI no instala Dart).

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-03 | Asmdef `RowMate.Tests.Support`, `.csproj` por asmdef y *lint* del grafo |
| QA-P-23 | Vectores dorados de `tools/oracle` leídos por `GoldenVectors` desde la It. 1 |
| QA-P-25 | *Lint* de asmdefs, manifest y `ProjectVersion.txt` como revisión estática sin Editor |
| QA-P-26 | `TestMainThreadContext` e `InlineDbExecutor` |

## Cambios de la It. 1 (implementación, WP-A)

| ID | Qué cambió |
|---|---|
| QA-01 | El comando es `tools/dotnet/run-tests.ps1`: `dotnet build tools/dotnet/RowMate.sln --configuration Release --artifacts-path tools/dotnet/.artifacts` y después `dotnet test` de la misma solución con `--no-build`. Propaga el primer código distinto de 0; si el build falla no corre tests. `dotnet test` solo no compila `RowMate.Tests.UnityApiCheck` ni `RowMate.Tokens` |
| QA-10 / D-10 | `RowMate.Tests.UnityApiCheck` (`netstandard2.1` + paquete NUnit **3.5.0**) compila `Tests/Support` (por glob), `Tests/EditMode` y `Tests/Simulator`: la compatibilidad con el NUnit y el perfil de Unity la verifica el compilador. `NUnitCompatibilityTests` queda para lo que compila en 3.5 pero no se admite |
| QA-03 | Las fuentes de `Tests/` no declaran `record` ni `init` (en Unity: `CS0518`); `Tests/Support` no tiene *shim* |
| QA-06 / D-01 | Vectores en `Assets/RowMate/Tests/Golden/oracle/`; secciones de más de 200 entradas en formato de tabla; tope de 1 MiB por archivo (`GoldenCoverageTests`) |
| QA-09 | `TraceabilityTests` lee el texto de `Tests/EditMode/**` y `Tests/Simulator/**` (categorías y nombres literales), sin reflexión; el resultado por AC sale de `run-tests.ps1 -Matrix` |
| Implementación | El logger `trx` no guarda las categorías de NUnit: `-Matrix` agrega `-- NUnit.TestOutputXml=<TestResults>/nunit` y arma `ac-matrix.md` desde ese XML. `run-tests.ps1` suma `-ArtifactsPath` (por defecto el documentado) para compilar en paralelo sin pisarse. `dotnet run --artifacts-path` no encuentra el ejecutable con el SDK 8.0.427: `RowMate.Tokens` se corre con `dotnet <artifacts>/bin/RowMate.Tokens/release/RowMate.Tokens.dll` |
| D-04 | La higiene del repositorio son tests de `dotnet` (`RepoHygieneTests` con git por `Process`, `PureCodeRulesTests`, `TokensUssSyncTests`); no hay `tools/ci/` |

## Cambios de la It. 2 (deuda de la It. 1)

| ID | Qué cambió |
|---|---|
| QA-V-05 | Procedimiento para correr en Unity todas las fuentes de test, incluidas las del simulador, sin dejar `ROWMATE_SIMULATOR` puesto: `tools/unity/run-unity-tests.ps1 [-Simulator] [-RestoreOnly]` + `Editor/Simulator/SimulatorDefine.cs` + `SimulatorBuildGuard.cs`. Revisión estática en `SimulatorInUnityTests`; la corrida real queda para el primer open de la It. 2 |
| QA-V-02 | `RepoHygieneTests` mira también lo que `git add -A` agregaría (`ls-files --others --exclude-standard`) y prueba la detección en un repositorio git temporal |
| QA-V-07 | `run-tests.ps1 -Demo` usa `verbosity=normal` con `NUnit.ConsoleOut=1`: cada línea `[DEMO]` sale una vez |

## Cambios de la It. 2 (primer open real)

Primera corrida de estas fuentes dentro de Unity 6000.3.27f1 ([00-primer-open.md](../../iterations/02-dispositivo-simulador/00-primer-open.md)). `RowMate.Tests.UnityApiCheck` compila contra NUnit 3.5.0 pero no corre los tests: estas tres diferencias compilan igual y solo aparecieron al correr.

| ID | Qué cambió |
|---|---|
| R-T5 | **Categorías con guiones**: el NUnit 3.5 de Unity (`com.unity.ext.nunit` 2.0.5) deja *NotRunnable* todo test cuya `[Category]` tenga `,`, `!`, `+` o `-` (464 de 885 en la primera corrida). `RowMate.Tests.CategoryAttribute` (Tests/Support) re-implementa `IApplyToTest` sin esa validación y agrega la misma propiedad `Category`. Está en el namespace `RowMate.Tests` a propósito: en los archivos de test, `[Category("…")]` resuelve a ella antes que a la de `NUnit.Framework` (que entra por el `using` del archivo), así las fuentes, `TraceabilityTests` y `-Matrix` no cambian. `TestCategoryTests` controla que todas las categorías usen esa clase |
| R-T5 | `[SetCulture]` no tiene efecto en el Unity Test Framework (`UnityTestExecutionContext.CurrentCulture` no toca el hilo): la cultura se fija a mano en `try/finally`. `Has.Count` en NUnit 3.5 busca una propiedad `Count` por reflexión y falla con un arreglo detrás de `IReadOnlyList<T>`: `.Count, Is.EqualTo(n)`. Los dos, prohibidos en `NUnitCompatibilityTests` |
| QA-V-05 | El Unity Test Framework sale con 2 si hay un Inconclusive. `run-unity-tests.ps1 -Simulator` pone `ROWMATE_SIMULATOR_TEST_RUN=1` solo para `-runTests`, y `CommittedSettingsDoNotEnableTheSimulator` pasa dentro de esa corrida (una marca sin la variable sigue siendo Inconclusive: corrida cortada) |
| AC-BASE-08 | `ManifestAndVersionTests.ProjectOpensWithoutCompileErrors` corre en Unity (en dotnet sigue ignorado) y `BootstrapperStaticTests.RanOnTheFirstOpenAndLeftItsState` lee el estado versionado del *bootstrapper* (corre en los dos). Diferencia de conteo esperada: 4 tests solo de `net8.0` (`#if NET5_0_OR_GREATER`: medición de memoria y parser de .NET 8) |
