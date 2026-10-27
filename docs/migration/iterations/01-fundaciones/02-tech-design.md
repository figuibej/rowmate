# It. 1 — Fundaciones: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-27 |
| Iteración | 1 · `01-fundaciones` ([ROADMAP §2, It. 1](../../ROADMAP.md)) |
| Entrada | [01-po-spec.md](01-po-spec.md) (139 AC + 19 LT), [ARCHITECTURE](../../architecture/ARCHITECTURE.md), [LEGACY_MAPPING](../../architecture/LEGACY_MAPPING.md), ADR 001-005, 011-016, [QA_PLAN_REVIEW](../../QA_PLAN_REVIEW.md) (QA-P-02, -03, -05, -15, -23, -25), [ART_BIBLE](../../design/ART_BIBLE.md) §2-§3 y §10-§11, [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §0 |
| Legacy leído | `rowmate-legacy` @ `3ae0f3a8bed5966700e830db3d8a5f050f34a0bc` (solo lectura; `git status --porcelain` vacío antes y después). **No** se leyó `lib/core/strava/strava_secrets.dart` |
| Estado | **Corregido según el gate de armonía** ([03-qa-harmony.md](03-qa-harmony.md), APROBADO CON OBSERVACIONES): QA-01, QA-03 a QA-10, QA-13 y QA-14 resueltos, ninguno disputado (ver "Cambios por gate QA", al final). Alineado con la spec corregida por el PO (QA-02, QA-11, QA-12: AC-APP-16-02, AC-RUT-17-01, AC-APP-09-07) |
| Verificado en esta máquina | .NET SDK 8.0.427 (`dotnet test --artifacts-path` disponible); Dart 3.13.5 corre scripts fuera de los repos importando el legacy por `package:rowmate/...` con un `package_config` propio (sin `pubspec`, sin `pub get`, sin escribir en el legacy); números de contraste de P-05 recalculados. **En la corrección del gate** (proyectos y scripts en el *scratchpad*, fuera de los dos repos): `dotnet build` + `dotnet test --no-build` con `UnityApiCheck` contra NUnit 3.5.0 (§2.3-§2.6); `Fmt.DartToString` de §3.1 contra `toString()` de Dart en 982 699 `double` (§3.1); firmas de §3.3 compiladas con las reglas de Core; tamaño de `fmt.json` con el formato de §6.2 (§6.3); contrastes de `DesignerPairs` (§4.4) |
| Siguiente paso | Implementación: WP-A y, en paralelo, WP-B y WP-C (§9). QA verifica en `05-qa-verification.md` |

Convenciones de este documento: rutas relativas a `<repo>` salvo que digan otra cosa. `legacy:` = `rowmate-legacy/lib/...`. "Puro" = compila con `dotnet` (netstandard2.1, sin `UnityEngine`). **NV** = NO VERIFICABLE sin Editor (revisión estática de QA, se verifica en la It. 2).

---

## 0. Resumen del diseño

1. **Proyecto Unity escrito a mano solo en texto estable** (ADR 014): `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, 14 asmdefs referenciados por nombre, código C#, `Tokens.uss` generado y un `ProjectBootstrapper` mínimo del Editor. Todo lo demás lo crea Unity en el primer open (NV).
2. **`tools/dotnet`** compila los 4 assemblies puros y los 3 de tests con **un `.csproj` por asmdef** (QA-P-03), que toman las fuentes de `Assets/` con **globs** (`Compile Include="…/**/*.cs"`): agregar un archivo nunca toca un `.csproj`. Un proyecto extra (`RowMate.Tests.UnityApiCheck`) compila las fuentes de test contra `netstandard2.1` y **NUnit 3.5.0** para garantizar que también compilan en Unity; `run-tests.ps1` compila la solución entera antes de correr los tests, así que ese proyecto se compila siempre (QA-01).
3. **Piezas base** (WP-A): observables, reloj y programador inyectables con su versión falsa, RNG inyectable, log, JSON mínimo, `BleUuid`/`BleScanResult`, paridad numérica con Dart (`Fmt`, `DartMath`, `IsoDate`, `TimeFormats`), tokens de diseño con generador de USS y test WCAG, soporte de tests, oráculo Dart, *lint* del proyecto, higiene del repositorio, trazabilidad y `ci-core.yml`.
4. **Dominio** en dos paquetes paralelos y disjuntos: WP-B (lectura del remo y formatos, FTMS, pulso, filtro de búsqueda, física del simulador) y WP-C (pasos, rutinas, series, sesión, telemetría, estadísticas, catálogo es/en con `MessageFormatter` y elección de idioma).
5. **Vectores dorados**: cada WP escribe sus scripts Dart en `tools/oracle/bin/` y su JSON en `Assets/RowMate/Tests/Golden/oracle/`; los tests de `dotnet` los leen sin Dart. Los `double` viajan con sus **bits IEEE-754** para que ningún *parser* de ningún runtime los altere.
6. **Un solo comando** (`tools/dotnet/run-tests.ps1`, que imprime y ejecuta los dos comandos exactos de §2.6: `dotnet build` de la solución y `dotnet test --no-build`) corre todo, incluida la higiene del repositorio, igual en la PC y en el CI (AC-APP-19-04).

---

## 1. Árbol de archivos, asmdefs, manifest, ProjectSettings y bootstrapper

### 1.1 Árbol exacto (221 archivos; dueño de cada uno en la última columna)

El árbol respeta ARCHITECTURE §2 (única referencia de rutas, QA-P-15). Solo se crean las carpetas que llevan archivos en esta iteración; el resto del árbol (`Art/`, `UI/`, `Slots/`, `Scenes/`, `Settings/`, `ThirdParty/`, `Plugins/`, `native/`) lo crean sus iteraciones o el bootstrapper.

#### Raíz y configuración

| Archivo | Propósito | WP |
|---|---|---|
| `.gitattributes` | `* text=auto`; `*.cs text diff=csharp`; `*.json *.uss *.asmdef *.md *.yml *.dart *.sh text eol=lf`; LFS para `*.png *.psd *.exr *.wav *.dll *.bundle *.a *.ttf *.otf` (ARCH §2; en la It. 1 no hay binarios) | A |
| `.gitignore` | **Modificar**: agregar `/tools/dotnet/.artifacts/`. El resto (secretos, `ThirdParty`, `tools/**/bin|obj`, oráculo) ya está | A |
| `.editorconfig` | 4 espacios, llaves en línea nueva, `var` si el tipo es evidente, `charset = utf-8`, `insert_final_newline = true` (ARCH §17) | A |
| `global.json` | `{"sdk": {"version": "8.0.427", "rollForward": "latestFeature"}}`. En la raíz porque `dotnet` busca `global.json` desde el directorio actual | A |
| `.github/workflows/ci-core.yml` | CI de los tests del Core (§7) | A |
| `Packages/manifest.json` | Paquetes con versión exacta (§1.3) | A |
| `ProjectSettings/ProjectVersion.txt` | `m_EditorVersion: 6000.3.27f1` (§1.4) | A |
| `Assets/csc.rsp` | `-nullable:enable` (mismo contexto de nulabilidad que `dotnet`; NV: si Unity no lo aplica a los asmdefs, las anotaciones `?` solo dan advertencias) | A |

#### `Assets/RowMate/` — un asmdef por carpeta (WP-A) + `AssemblyInfo.cs` en cada una

Cada carpeta de assembly lleva su `.asmdef` (§1.2) y un `AssemblyInfo.cs` (así ningún assembly queda vacío: Unity no compila un asmdef sin scripts). En los puros, `AssemblyInfo.cs` declara `[assembly: InternalsVisibleTo(...)]` hacia los assemblies de test; en los de test es solo un comentario (sin atributos, para no duplicar atributos en `RowMate.Tests.UnityApiCheck`, §2.4).

| Carpeta | Asmdef | `AssemblyInfo.cs` | Otros archivos de WP-A |
|---|---|---|---|
| `Core/` | `RowMate.Core.asmdef` | IVT a `RowMate.Tests.Support`, `.EditMode`, `.Simulator`, `.UnityApiCheck` | `Internal/IsExternalInit.cs` |
| `App/` | `RowMate.App.asmdef` | IVT a tests | `Internal/IsExternalInit.cs` |
| `Infrastructure/` | `RowMate.Infrastructure.asmdef` | IVT a tests | — |
| `Simulator/` | `RowMate.Simulator.asmdef` | IVT a `RowMate.Tests.Simulator`, `.UnityApiCheck` | `Internal/IsExternalInit.cs` |
| `Platform/` | `RowMate.Platform.asmdef` | marcador | — |
| `Presentation/` | `RowMate.Presentation.asmdef` | marcador | `Styles/Tokens.uss` (GENERADO, §4) |
| `Scene/` | `RowMate.Scene.asmdef` | marcador | — |
| `SimulatorPresentation/` | `RowMate.Simulator.Presentation.asmdef` | marcador | — |
| `Bootstrap/` | `RowMate.Bootstrap.asmdef` | marcador | `BuildFlags.cs` (`const bool IsSimulator` según `#if ROWMATE_SIMULATOR`; legacy: `lib/core/dev/dev_config.dart:5`) |
| `Editor/` | `RowMate.Editor.asmdef` | marcador | `Bootstrap/ProjectBootstrapper.cs`, `Bootstrap/BootstrapState.cs`, `Bootstrap/PlayerSettingsStep.cs`, `Bootstrap/UrpProvisioner.cs` (§1.5) |
| `Tests/Support/` | `RowMate.Tests.Support.asmdef` | comentario | soporte (§2.5); **sin** *shim* (QA-03) |
| `Tests/EditMode/` | `RowMate.Tests.EditMode.asmdef` | comentario | tests de WP-A (§8) |
| `Tests/Simulator/` | `RowMate.Tests.Simulator.asmdef` | comentario | — |
| `Tests/PlayMode/` | `RowMate.Tests.PlayMode.asmdef` | comentario | — |

`IsExternalInit` (shim de `record`/`init`): `namespace System.Runtime.CompilerServices { #if !NET5_0_OR_GREATER internal static class IsExternalInit { } #endif }`, uno **interno** por assembly de **producción** que declara records (Core, App, Simulator). Los assemblies de test **no** declaran `record` ni `init` y no llevan *shim* (regla de §2.4, QA-03): un assembly `netstandard2.1` que ve por `InternalsVisibleTo` dos o más *shims* internos no puede declarar un `record` (`CS0518`), y eso es lo que pasa en Unity con `Tests.Support`, `Tests.EditMode` y `Tests.Simulator` y en `dotnet` con `Tests.UnityApiCheck`. Usar los records de Core desde un test (`new X(…) { P = … }`, `with { … }`) sí compila.

#### `Assets/RowMate/Core/` (puro) — archivos de dominio y piezas base

| Archivo | Tipo(s) | WP |
|---|---|---|
| `Common/IReadOnlyObservable.cs` · `ObservableValue.cs` · `EventStream.cs` | Reactividad (ADR 004) | A |
| `Common/IClock.cs` · `IScheduler.cs` | Tiempo inyectable (ADR 005) | A |
| `Common/IRandom.cs` · `SystemRandom.cs` | Azar inyectable (AC-BASE-07) | A |
| `Common/LogLevel.cs` · `IAppLogger.cs` · `NullAppLogger.cs` · `TaskExtensions.cs` | Log de diagnóstico y `Forget` | A |
| `Common/PlatformKind.cs` | `Android, Ios, MacOs, Windows` | A |
| `Common/AppIdentity.cs` | Constantes de F-APP-16 (nombre, empresa, versión, ids, mínimos) | A |
| `Common/Json/JsonKind.cs` · `JsonNode.cs` · `JsonParser.cs` | JSON mínimo y estricto (catálogo, vectores dorados, *lint*) | A |
| `Text/Fmt.cs` · `DartMath.cs` · `DoubleParts.cs` (interno) · `IsoDate.cs` · `TimeFormats.cs` | Paridad numérica Dart (ADR 012) | A |
| `Ble/BleUuid.cs` · `BleScanResult.cs` | Ids y resultados de búsqueda BLE | A |
| `Design/Rgb.cs` · `TokenKind.cs` · `IDesignToken.cs` · `ColorRole.cs` · `ColorToken.cs` · `LengthToken.cs` · `DurationToken.cs` · `EasingToken.cs` · `FontFamilyRole.cs` · `TypeScaleToken.cs` · `TokenCatalog.cs` · `UssTokenWriter.cs` · `Contrast.cs` | Mecanismo de tokens (ADR 016, §4) | A |
| `Design/Tokens.cs` | **Fuente única** de tokens: la llena el **Designer** (§4.3). WP-A solo la crea si al empezar no existe, con los obligatorios de §4.4, y nunca pisa valores del Designer | Designer / A |
| `Models/RowingData.cs` · `RowingFormats.cs` | F-BLE-01 | B |
| `Ble/FtmsUuids.cs` · `FtmsParser.cs` · `HeartRateReading.cs` · `HeartRateParser.cs` · `ScanFilter.cs` | F-BLE-02, F-HRM-01, F-BLE-03 | B |
| `Models/StepType.cs` · `StepTypeNames.cs` · `StepGoal.cs` · `StepTypeInfo.cs` · `IntervalStep.cs` · `StepLabels.cs` | F-RUT-01 a -04 | C |
| `Models/Routine.cs` · `StepPosition.cs` · `RoutineExpansion.cs` · `RoutineCounts.cs` · `RoutineSummary.cs` | F-RUT-05 a -07 | C |
| `Models/WorkoutSession.cs` · `WorkoutSessionRecord.cs` · `WorkoutSessionCodec.cs` · `DataPoint.cs` · `SessionStats.cs` | F-RUT-16 a -18 | C |
| `Localization/AppLanguage.cs` · `LanguageTag.cs` · `LocaleResolver.cs` · `LanguageState.cs` | F-APP-09 | C |
| `Localization/MessageArg.cs` · `MessageArgKind.cs` · `MessageFormatter.cs` · `CompiledMessage.cs` · `PlaceholderInfo.cs` · `LocalizationCatalog.cs` · `Localizer.cs` · `LocalizationSet.cs` · `MissingTranslationException.cs` · `L10nKeys.cs` | F-APP-10 | C |
| `Ports/ISystemLocale.cs` | Puerto de la lista de idiomas del sistema (se implementa en Platform, It. 2) | C |

#### Otros assemblies y datos

| Archivo | Propósito | WP |
|---|---|---|
| `Assets/RowMate/Simulator/RowingSimulator.cs` | F-BLE-14 (assembly `RowMate.Simulator`, `ROWMATE_SIMULATOR`) | B |
| `Assets/RowMate/Localization/strings.es.json` · `strings.en.json` | Catálogos (174 claves cada uno, formato `.arb`, §3.3) | C |
| `Assets/RowMate/Tests/Golden/oracle/fmt.json` | Vectores de `Fmt`/`DartMath`/`IsoDate` | A |
| `…/Golden/oracle/rowing_data.json` · `ftms.json` · `heart_rate.json` · `simulator.json` | Vectores de WP-B | B |
| `…/Golden/oracle/routines.json` · `session.json` · `l10n_legacy.json` | Vectores de WP-C | C |

#### Tests (fuentes compartidas dotnet + Unity)

| Carpeta | Archivos | WP |
|---|---|---|
| `Tests/Support/` | `RepoPaths.cs`, `GoldenVectors.cs`, `GoldenFile.cs`, `GoldenTable.cs`, `GoldenJsonExtensions.cs`, `HexBytes.cs`, `FakeScheduler.cs`, `TestMainThreadContext.cs`, `SequenceRandom.cs`, `RecordingLogger.cs` | A |
| `Tests/EditMode/Text/` | `FmtFixedTests.cs`, `FmtDartToStringTests.cs`, `DartMathTests.cs`, `IsoDateTests.cs`, `TimeFormatsTests.cs`, `CultureIndependenceTests.cs` | A |
| `Tests/EditMode/Common/` | `ObservableValueTests.cs`, `EventStreamTests.cs`, `FakeSchedulerTests.cs`, `TestMainThreadContextTests.cs`, `SequenceRandomTests.cs`, `JsonParserTests.cs` | A |
| `Tests/EditMode/Ble/` | `BleUuidTests.cs` | A |
| `Tests/EditMode/Ble/` | `FtmsParserTests.cs`, `HeartRateParserTests.cs`, `ScanFilterTests.cs` | B |
| `Tests/EditMode/Design/` | `TokensContractTests.cs`, `TokenCatalogTests.cs`, `ContrastTests.cs`, `TokensUssSyncTests.cs` | A |
| `Tests/EditMode/Project/` | `AsmdefLintTests.cs`, `ManifestAndVersionTests.cs`, `AppIdentityTests.cs`, `BootstrapperStaticTests.cs`, `PureCodeRulesTests.cs`, `NUnitCompatibilityTests.cs`, `RepoHygieneTests.cs`, `CiWorkflowTests.cs`, `GoldenCoverageTests.cs` | A |
| `Tests/EditMode/Traceability/` | `It01Ids.cs`, `TraceabilityTests.cs` | A |
| `Tests/EditMode/Models/` | `RowingDataTests.cs`, `RowingFormatsTests.cs` | B |
| `Tests/EditMode/Models/` | `RoutineFixtures.cs`, `StepTypeTests.cs`, `IntervalStepTests.cs`, `StepLabelsTests.cs`, `RoutineTests.cs`, `RoutineSummaryTests.cs`, `RoutineExpansionTests.cs`, `RoutinePositionsTests.cs`, `WorkoutSessionTests.cs`, `DataPointTests.cs`, `SessionStatsTests.cs` | C |
| `Tests/EditMode/Localization/` | `LocalizationFixture.cs`, `LocaleResolverTests.cs`, `LanguageStateTests.cs`, `MessageFormatterTests.cs`, `LocalizerTests.cs`, `CatalogParityTests.cs`, `L10nKeysTests.cs` | C |
| `Tests/Simulator/` | `RowingSimulatorTests.cs` | B |

#### `tools/`

| Archivo | Propósito | WP |
|---|---|---|
| `tools/dotnet/RowMate.sln` | Solución (creada con `dotnet new sln -n RowMate` + `dotnet sln add` de los 9 `.csproj`) | A |
| `tools/dotnet/Directory.Build.props` | Propiedades comunes (§2.2) | A |
| `tools/dotnet/run-tests.ps1` | El comando documentado (§2.6); PowerShell 5.1 y 7 | A |
| `tools/dotnet/README.md` | Cómo correr, filtrar por AC, modo demo, matriz | A |
| `tools/dotnet/RowMate.Core/RowMate.Core.csproj` · `RowMate.App/…` · `RowMate.Infrastructure/…` · `RowMate.Simulator/…` · `RowMate.Tests.Support/…` · `RowMate.Tests.EditMode/…` · `RowMate.Tests.Simulator/…` · `RowMate.Tests.UnityApiCheck/…` · `RowMate.Tokens/…` | Un `.csproj` por asmdef puro + 2 herramientas (§2.3) | A |
| `tools/tokens/Program.cs` | Genera o chequea `Presentation/Styles/Tokens.uss` (§4.5) | A |
| `tools/oracle/README.md` · `run-oracle.ps1` · `lib/golden.dart` · `bin/fmt_vectors.dart` | Oráculo Dart: infraestructura y vectores de A (§6) | A |
| `tools/oracle/bin/rowing_data_vectors.dart` · `ftms_vectors.dart` · `heart_rate_vectors.dart` · `simulator_vectors.dart` | Vectores de B | B |
| `tools/oracle/bin/routine_vectors.dart` · `session_vectors.dart` · `l10n_vectors.dart` | Vectores de C | C |
| `tools/l10n/arb_to_catalog.dart` · `it01-changes.json` · `README.md` | Conversión única `.arb` → catálogos (Q-02 y 15 claves nuevas, §3.3) | C |

Totales: WP-A 139, WP-B 22, WP-C 60 → **221**. Ningún archivo tiene dos dueños (verificable: la unión de las listas de §9 no tiene repetidos). En la corrección del gate, WP-A pierde `Tests/Support/Internal/IsExternalInit.cs` (QA-03) y gana `Tests/Support/GoldenTable.cs` (QA-06): los totales no cambian.

### 1.2 Asmdefs (JSON tal cual se escriben)

Todos con `autoReferenced: false`, `allowUnsafeCode: false`, `versionDefines: []` y referencias **por nombre** (ningún GUID). Orden de claves = el que escribe Unity. Nombres de paquetes de Unity usados: `Unity.InputSystem`, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Universal.Editor`, `Unity.Animation.Rigging`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner` (NV hasta el primer open).

`Assets/RowMate/Core/RowMate.Core.asmdef`
```json
{
    "name": "RowMate.Core",
    "rootNamespace": "RowMate.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Assets/RowMate/App/RowMate.App.asmdef`
```json
{
    "name": "RowMate.App",
    "rootNamespace": "RowMate.App",
    "references": ["RowMate.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Assets/RowMate/Infrastructure/RowMate.Infrastructure.asmdef` (la referencia a `Gilzoide.SqliteNet` y a `Newtonsoft.Json.dll` entra en la It. 3, con su primer uso; ver D-07)
```json
{
    "name": "RowMate.Infrastructure",
    "rootNamespace": "RowMate.Infrastructure",
    "references": ["RowMate.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/Simulator/RowMate.Simulator.asmdef`
```json
{
    "name": "RowMate.Simulator",
    "rootNamespace": "RowMate.Simulator",
    "references": ["RowMate.Core", "RowMate.App"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": ["ROWMATE_SIMULATOR"],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Assets/RowMate/Platform/RowMate.Platform.asmdef`
```json
{
    "name": "RowMate.Platform",
    "rootNamespace": "RowMate.Platform",
    "references": ["RowMate.Core", "Unity.InputSystem"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/Presentation/RowMate.Presentation.asmdef`
```json
{
    "name": "RowMate.Presentation",
    "rootNamespace": "RowMate.Presentation",
    "references": ["RowMate.Core", "RowMate.App", "Unity.InputSystem"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/Scene/RowMate.Scene.asmdef`
```json
{
    "name": "RowMate.Scene",
    "rootNamespace": "RowMate.Scene",
    "references": [
        "RowMate.Core",
        "RowMate.App",
        "Unity.RenderPipelines.Core.Runtime",
        "Unity.RenderPipelines.Universal.Runtime",
        "Unity.Animation.Rigging"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/SimulatorPresentation/RowMate.Simulator.Presentation.asmdef`
```json
{
    "name": "RowMate.Simulator.Presentation",
    "rootNamespace": "RowMate.Simulator.Presentation",
    "references": ["RowMate.Core", "RowMate.App", "RowMate.Simulator", "RowMate.Presentation"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": ["ROWMATE_SIMULATOR"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/Bootstrap/RowMate.Bootstrap.asmdef`
```json
{
    "name": "RowMate.Bootstrap",
    "rootNamespace": "RowMate.Bootstrap",
    "references": [
        "RowMate.Core",
        "RowMate.App",
        "RowMate.Infrastructure",
        "RowMate.Platform",
        "RowMate.Presentation",
        "RowMate.Scene",
        "RowMate.Simulator",
        "RowMate.Simulator.Presentation"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/Editor/RowMate.Editor.asmdef` (namespace `RowMate.Editor`: dentro de él, `UnityEditor.Editor` se escribe calificado)
```json
{
    "name": "RowMate.Editor",
    "rootNamespace": "RowMate.Editor",
    "references": [
        "RowMate.Core",
        "RowMate.App",
        "RowMate.Infrastructure",
        "RowMate.Platform",
        "RowMate.Presentation",
        "RowMate.Scene",
        "RowMate.Bootstrap",
        "RowMate.Simulator",
        "RowMate.Simulator.Presentation",
        "Unity.RenderPipelines.Core.Runtime",
        "Unity.RenderPipelines.Universal.Runtime",
        "Unity.RenderPipelines.Universal.Editor"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/Tests/Support/RowMate.Tests.Support.asmdef` (QA-P-03: hermano de los de test, sin `[Test]`, sin API de Unity; no referencia `RowMate.Simulator`)
```json
{
    "name": "RowMate.Tests.Support",
    "rootNamespace": "RowMate.Tests.Support",
    "references": ["RowMate.Core", "RowMate.App", "RowMate.Infrastructure"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Assets/RowMate/Tests/EditMode/RowMate.Tests.EditMode.asmdef`
```json
{
    "name": "RowMate.Tests.EditMode",
    "rootNamespace": "RowMate.Tests.EditMode",
    "references": [
        "RowMate.Tests.Support",
        "RowMate.Core",
        "RowMate.App",
        "RowMate.Infrastructure",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/Tests/Simulator/RowMate.Tests.Simulator.asmdef`
```json
{
    "name": "RowMate.Tests.Simulator",
    "rootNamespace": "RowMate.Tests.Simulator",
    "references": [
        "RowMate.Tests.Support",
        "RowMate.Core",
        "RowMate.App",
        "RowMate.Simulator",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS", "ROWMATE_SIMULATOR"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/RowMate/Tests/PlayMode/RowMate.Tests.PlayMode.asmdef` (solo `UnityEngine.TestRunner`, para que pueda entrar en un *player* de test)
```json
{
    "name": "RowMate.Tests.PlayMode",
    "rootNamespace": "RowMate.Tests.PlayMode",
    "references": [
        "RowMate.Tests.Support",
        "RowMate.Core",
        "RowMate.App",
        "RowMate.Presentation",
        "RowMate.Scene",
        "RowMate.Bootstrap",
        "UnityEngine.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 1.3 `Packages/manifest.json` (completo)

Idéntico a ARCHITECTURE §1.2 (ADR 001). `packages-lock.json` lo genera Unity en el primer open y se versiona desde entonces.

```json
{
  "dependencies": {
    "com.gilzoide.sqlite-net": "https://github.com/gilzoide/unity-sqlite-net.git#1.3.3",
    "com.unity.animation.rigging": "1.4.1",
    "com.unity.ide.visualstudio": "2.0.28",
    "com.unity.inputsystem": "1.20.1",
    "com.unity.mobile.android-logcat": "1.4.7",
    "com.unity.nuget.newtonsoft-json": "3.2.2",
    "com.unity.render-pipelines.universal": "17.3.0",
    "com.unity.shadergraph": "17.3.0",
    "com.unity.test-framework": "1.6.0",
    "com.unity.ugui": "2.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0"
  }
}
```

`ManifestAndVersionTests` lo compara contra este mapa **exacto** (un paquete nuevo obliga a tocar el test: decisión explícita y aprobada por QA, ADR 001).

### 1.4 `ProjectSettings/` escrito a mano (ADR 014)

Solo `ProjectSettings/ProjectVersion.txt`, con una línea y salto final:

```
m_EditorVersion: 6000.3.27f1
```

Nada más de `ProjectSettings/` se escribe a mano. `ProjectSettings/RowMateBootstrap.json` lo escribe el bootstrapper (§1.5) y se versiona después del primer open, junto con los `.asset` que genera Unity. El test acepta además la línea `m_EditorVersionWithRevision: 6000.3.27f1 (…)` que Unity agrega al abrir.

### 1.5 `ProjectBootstrapper` (Editor, NV)

**Qué es.** `RowMate.Editor.ProjectBootstrapper`, clase estática con `[InitializeOnLoad]`. Su constructor estático solo agenda `EditorApplication.delayCall += () => Run(force: false)`. Menú `RowMate/Bootstrap/Reaplicar` → `Run(force: true)`. Unas 250 líneas en 4 archivos; toda constante sale de `RowMate.Core.Common.AppIdentity` (testeado en `dotnet`), sin literales repetidos.

**Idempotencia.**
- Estado en `ProjectSettings/RowMateBootstrap.json` (`BootstrapState`, serializable con `JsonUtility`): `{ "version": 1, "editorVersion": "6000.3.27f1", "appliedAtUtc": "…", "steps": [ { "id": "player.identity", "status": "ok|failed|skipped", "detail": "…" } ] }`.
- `Run` sale sin hacer nada si `!force && state.version >= CurrentVersion (=1)`; así el costo en cada recarga de dominio es leer un archivo chico.
- Si el Editor está compilando, actualizando o entrando en *play mode*, se reagenda con `delayCall`.
- Cada paso **asigna valores** (no suma ni alterna): correrlo dos veces deja el mismo estado. El paso de URP crea los assets solo si no existen y vuelve a asignar las referencias.
- Cada paso corre en su propio `try/catch`: un paso que falla queda `failed` con el mensaje y los demás siguen. Si falla, la versión no se marca como aplicada (se reintenta en el próximo open) y la consola muestra las instrucciones manuales de ese paso.
- En *batchmode* no abre diálogos: solo registra en la consola.

**Pasos de la versión 1 (mínimo de la It. 1, ARCHITECTURE §16):**

| Id | Qué aplica | API (Unity 6000.3) |
|---|---|---|
| `player.identity` | Empresa `iguisoft`, producto `RowMate`, versión `1.0.0`, compilación 1; ids: Android `com.iguisoft.rowmate`, iOS `com.iguisoft.rowmate.unity`, Standalone `com.iguisoft.rowmate.unity` (lo usa macOS; Windows no tiene id propio, D-02) | `PlayerSettings.companyName/productName/bundleVersion`, `PlayerSettings.Android.bundleVersionCode = 1`, `PlayerSettings.iOS.buildNumber = "1"`, `PlayerSettings.macOS.buildNumber = "1"`, `PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android/iOS/Standalone, …)` |
| `player.scripting` | IL2CPP y .NET Standard 2.1 en Android, iOS y Standalone | `PlayerSettings.SetScriptingBackend(nbt, ScriptingImplementation.IL2CPP)`, `PlayerSettings.SetApiCompatibilityLevel(nbt, ApiCompatibilityLevel.NET_Standard)` |
| `player.android` | **minSdk 26**, target = el más alto instalado, **solo ARM64**, Internet obligatorio (Q-90) | `PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26`, `.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto`, `.targetArchitectures = AndroidArchitecture.ARM64`, `.forceInternetPermission = true` |
| `player.ios` | iOS 15.0 | `PlayerSettings.iOS.targetOSVersionString = AppIdentity.IosMinimumVersion` |
| `player.rendering` | Linear; Android Vulkan + GLES3; Windows DX11; Apple Metal (por defecto) | `PlayerSettings.colorSpace = ColorSpace.Linear`, `SetUseDefaultGraphicsAPIs(BuildTarget.Android, false)` + `SetGraphicsAPIs(BuildTarget.Android, { Vulkan, OpenGLES3 })`, ídem `StandaloneWindows64` con `{ Direct3D11 }` |
| `player.runtime` | `runInBackground = true` (ARCHITECTURE §6.1); *Frame Timing Stats* | `PlayerSettings.runInBackground = true`, `PlayerSettings.enableFrameTimingStats = true` |
| `player.input` | Manejador de entrada = Input System (1). Si cambió, avisa "Reiniciá el Editor" | `SerializedObject` sobre `ProjectSettings/ProjectSettings.asset`, propiedad `activeInputHandler` (no hay API pública) |
| `urp.provisional` | `Assets/RowMate/Settings/URP/RowMate_Renderer.asset` (Universal Renderer) y **un solo** `RowMate_URP_Default.asset`, asignado en Graphics y en los niveles de calidad **existentes**. No crea, borra ni renombra niveles (QA-P-10) | `ScriptableObject.CreateInstance<UniversalRendererData>()` + `AssetDatabase.CreateAsset`; `UniversalRenderPipelineAsset.Create(rendererData)` (pública en URP 17.3); `GraphicsSettings.defaultRenderPipeline = asset`; por nivel `QualitySettings.SetQualityLevel(i, false)` + `QualitySettings.renderPipeline = asset`, y vuelve al nivel original. Respaldo si falla: 3 pasos manuales en consola |
| `defines.check` | Verifica que `ROWMATE_SIMULATOR` **no** esté en Standalone, Android ni iOS. Si está, advierte (no lo agrega nunca; tampoco lo saca: el menú de modo simulador llega en la It. 2) (R-01) | `PlayerSettings.GetScriptingDefineSymbols(nbt)` |

Al final: `AssetDatabase.SaveAssets()`, escribe el estado y un resumen `[BOOTSTRAP] …` en la consola. Lo que **no** hace en la It. 1: `PanelSettings`, `Boot.unity`, Build Settings, niveles de calidad, *smoke test* IL2CPP (It. 2 y 6a).

`BootstrapperStaticTests` (dotnet) revisa el texto de `Assets/RowMate/Editor/Bootstrap/*.cs`: tiene `[InitializeOnLoad]` y `RowMateBootstrap.json`; los ids salen de `AppIdentity.` (ningún literal `com.`); no usa `AppIdentity.WindowsApplicationId` (reservado para el MSIX de la It. 14, AC-APP-16-02); no llama a `SetScriptingDefineSymbols`; no menciona `RowMate_URP_High|Medium|Low` ni modifica `QualitySettings.names`.

---

## 2. `tools/dotnet`

### 2.1 Proyectos y grafo (copia del grafo de asmdefs)

| Proyecto | TFM | Espeja asmdef | `Compile Include` (glob) | Referencias |
|---|---|---|---|---|
| `RowMate.Core` | netstandard2.1 | `RowMate.Core` | `$(UnityAssets)Core/**/*.cs` | — |
| `RowMate.App` | netstandard2.1 | `RowMate.App` | `$(UnityAssets)App/**/*.cs` | Core |
| `RowMate.Infrastructure` | netstandard2.1 | `RowMate.Infrastructure` | `$(UnityAssets)Infrastructure/**/*.cs` | Core (sqlite-net-pcl 1.9.172 y Newtonsoft.Json 13.0.2 entran en la It. 3) |
| `RowMate.Simulator` | netstandard2.1 | `RowMate.Simulator` | `$(UnityAssets)Simulator/**/*.cs` | Core, App; `ROWMATE_SIMULATOR` |
| `RowMate.Tests.Support` | netstandard2.1 | `RowMate.Tests.Support` | `$(UnityAssets)Tests/Support/**/*.cs` | Core, App, Infrastructure; NUnit 3.14.0 |
| `RowMate.Tests.EditMode` | net8.0 | `RowMate.Tests.EditMode` | `$(UnityAssets)Tests/EditMode/**/*.cs` | Support, Core, App, Infrastructure; NUnit 3.14.0, NUnit3TestAdapter 4.6.0, Microsoft.NET.Test.Sdk 17.11.1 |
| `RowMate.Tests.Simulator` | net8.0 | `RowMate.Tests.Simulator` | `$(UnityAssets)Tests/Simulator/**/*.cs` | Support, Core, App, Simulator; ídem paquetes; `ROWMATE_SIMULATOR` |
| `RowMate.Tests.UnityApiCheck` | netstandard2.1 | — (herramienta) | Support + EditMode + Simulator (los mismos globs; Support **por glob**, no por `ProjectReference`, QA-10) | Core, App, Infrastructure, Simulator; **NUnit 3.5.0** (el de Unity, QA-10); `ROWMATE_SIMULATOR`. Solo compila: prueba que las fuentes de test usan APIs que existen en .NET Standard 2.1 y en NUnit 3.5, y que no declaran records (AC-BASE-08). Lo compila el paso 1 de `run-tests.ps1` (QA-01) |
| `RowMate.Tokens` | net8.0 (Exe) | — (herramienta) | `$(RepoRoot)tools/tokens/**/*.cs` | Core |

El *lint* (`AsmdefLintTests`) reconoce como espejo a todo `.csproj` con la propiedad `RowMateAsmdef` y trata como herramientas a `RowMate.Tests.UnityApiCheck` y `RowMate.Tokens`.

### 2.2 `tools/dotnet/Directory.Build.props`

```xml
<Project>
  <!-- Comunes a tools/dotnet (ADR 013). Las fuentes viven en Assets/: no se copian. -->
  <PropertyGroup>
    <RepoRoot>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', '..', '..'))</RepoRoot>
    <UnityAssets>$(RepoRoot)Assets/RowMate/</UnityAssets>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <Deterministic>true</Deterministic>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

`LangVersion 9.0` también en los tests `net8.0`: tienen que compilar con el C# 9 de Unity. **No** se usa `InvariantGlobalization` (rompería los tests de cultura de AC-BASE-02).

### 2.3 `.csproj` (exactos)

`tools/dotnet/RowMate.Core/RowMate.Core.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>RowMate.Core</AssemblyName>
    <RootNamespace>RowMate.Core</RootNamespace>
    <RowMateAsmdef>Core/RowMate.Core.asmdef</RowMateAsmdef>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(UnityAssets)Core/**/*.cs" LinkBase="Core" />
  </ItemGroup>
</Project>
```

`RowMate.App.csproj` y `RowMate.Infrastructure.csproj`: iguales, con su carpeta, `RowMateAsmdef` y `<ProjectReference Include="../RowMate.Core/RowMate.Core.csproj" />`.

`tools/dotnet/RowMate.Simulator/RowMate.Simulator.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>RowMate.Simulator</AssemblyName>
    <RootNamespace>RowMate.Simulator</RootNamespace>
    <RowMateAsmdef>Simulator/RowMate.Simulator.asmdef</RowMateAsmdef>
    <DefineConstants>$(DefineConstants);ROWMATE_SIMULATOR</DefineConstants>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(UnityAssets)Simulator/**/*.cs" LinkBase="Simulator" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../RowMate.Core/RowMate.Core.csproj" />
    <ProjectReference Include="../RowMate.App/RowMate.App.csproj" />
  </ItemGroup>
</Project>
```

`tools/dotnet/RowMate.Tests.Support/RowMate.Tests.Support.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>RowMate.Tests.Support</AssemblyName>
    <RootNamespace>RowMate.Tests.Support</RootNamespace>
    <RowMateAsmdef>Tests/Support/RowMate.Tests.Support.asmdef</RowMateAsmdef>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(UnityAssets)Tests/Support/**/*.cs" LinkBase="Support" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../RowMate.Core/RowMate.Core.csproj" />
    <ProjectReference Include="../RowMate.App/RowMate.App.csproj" />
    <ProjectReference Include="../RowMate.Infrastructure/RowMate.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Respaldo de RepoPaths en dotnet (Unity no lo tiene: ahí sube desde el directorio actual). -->
    <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
      <_Parameter1>RowMateRepoRoot</_Parameter1>
      <_Parameter2>$(RepoRoot)</_Parameter2>
    </AssemblyAttribute>
  </ItemGroup>
</Project>
```

`tools/dotnet/RowMate.Tests.EditMode/RowMate.Tests.EditMode.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>RowMate.Tests.EditMode</AssemblyName>
    <RootNamespace>RowMate.Tests.EditMode</RootNamespace>
    <RowMateAsmdef>Tests/EditMode/RowMate.Tests.EditMode.asmdef</RowMateAsmdef>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(UnityAssets)Tests/EditMode/**/*.cs" LinkBase="EditMode" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../RowMate.Tests.Support/RowMate.Tests.Support.csproj" />
    <ProjectReference Include="../RowMate.Core/RowMate.Core.csproj" />
    <ProjectReference Include="../RowMate.App/RowMate.App.csproj" />
    <ProjectReference Include="../RowMate.Infrastructure/RowMate.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

`RowMate.Tests.Simulator.csproj`: igual, con `Tests/Simulator/**/*.cs`, `DefineConstants` `ROWMATE_SIMULATOR` y referencias Support, Core, App y Simulator.

`tools/dotnet/RowMate.Tests.UnityApiCheck/RowMate.Tests.UnityApiCheck.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Solo compila: las fuentes de test tienen que existir en .NET Standard 2.1 (el perfil de Unity)
         y en NUnit 3.5 (com.unity.ext.nunit). Lo compila run-tests.ps1 (dotnet build de la solución). -->
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>RowMate.Tests.UnityApiCheck</AssemblyName>
    <DefineConstants>$(DefineConstants);ROWMATE_SIMULATOR</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <!-- Support entra por glob (no por ProjectReference): así todo se compila contra NUnit 3.5 sin mezclar versiones. -->
    <Compile Include="$(UnityAssets)Tests/Support/**/*.cs" LinkBase="Support" />
    <Compile Include="$(UnityAssets)Tests/EditMode/**/*.cs" LinkBase="EditMode" />
    <Compile Include="$(UnityAssets)Tests/Simulator/**/*.cs" LinkBase="Simulator" />
  </ItemGroup>
  <ItemGroup>
    <!-- NuGet elige lib/net45 por AssetTargetFallback (NU1701, esperado): es la variante completa, como la de Unity. -->
    <PackageReference Include="NUnit" Version="3.5.0" NoWarn="NU1701" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../RowMate.Core/RowMate.Core.csproj" />
    <ProjectReference Include="../RowMate.App/RowMate.App.csproj" />
    <ProjectReference Include="../RowMate.Infrastructure/RowMate.Infrastructure.csproj" />
    <ProjectReference Include="../RowMate.Simulator/RowMate.Simulator.csproj" />
  </ItemGroup>
</Project>
```

Probado en el *scratchpad* con una réplica del grafo (Core, App y Simulator con *shim* interno e IVT; Support, EditMode, Simulator de test y este proyecto), SDK 8.0.427: compila sin advertencias; **falla** con `CS1061`/`CS0117`/`CS0246`/`CS0103` si una fuente de test usa `TestCaseData.SetArgDisplayNames`, `TestContext.AddTestAttachment`, `Is.Not.Default`, `Is.AnyOf`, `Assert.Multiple`, `Warn.`, `[NonParallelizable]` o `[DefaultFloatingPointTolerance]` (posteriores a 3.5), y con `CS0518` si declara un `record` (QA-03). Compilan, en cambio, todas las APIs que usa este diseño: `TestCaseData.SetName/SetCategory/SetDescription/Returns`, `[TestCase(TestName = …, Category = …, ExpectedResult = …)]`, `[TestCaseSource]`, `[SetCulture]`, `[Values]`, `[Range]`, `Is.EqualTo(…).Within(…)`, `Throws.TypeOf<T>()`, `Assert.Throws<T>`, `Assert.Inconclusive/Ignore`, `Does.Contain/StartWith/Match`, `Has.Count`, `TestContext.Out.WriteLine` y `TestContext.CurrentContext.TestDirectory`.

`tools/dotnet/RowMate.Tokens/RowMate.Tokens.csproj`: `net8.0`, `<OutputType>Exe</OutputType>`, `Compile Include="$(RepoRoot)tools/tokens/**/*.cs"`, referencia a Core.

Reglas para que los globs sigan funcionando:
- Los namespaces de test se diferencian por assembly (`RowMate.Tests.Support`, `RowMate.Tests.EditMode.*`, `RowMate.Tests.Simulator.*`) para que `UnityApiCheck` no tenga tipos repetidos.
- Ningún `AssemblyInfo.cs` de `Tests/` declara atributos de assembly (solo un comentario): `UnityApiCheck` junta las tres carpetas y los duplicaría.
- Código que solo existe en `net8.0` (por ejemplo `GC.GetAllocatedBytesForCurrentThread` en el test de 0 B) va bajo `#if NET5_0_OR_GREATER`: en Unity y en `UnityApiCheck` ese test no se compila.

### 2.4 Cómo se comparten las fuentes de test con Unity (QA-P-03)

- **Mismos archivos, dos compiladores.** Unity compila `Tests/EditMode/**` con su asmdef (UTF 1.6, NUnit 3.5 de `com.unity.ext.nunit`); `dotnet` los compila con `RowMate.Tests.EditMode.csproj` (NUnit 3.14). Ídem `Tests/Simulator/**`. `Tests/PlayMode` es solo Unity.
- **`RowMate.Tests.Support`** es un assembly propio y hermano (un asmdef no ve carpetas hermanas). `noEngineReferences: true`, `UNITY_INCLUDE_TESTS`, referencia `nunit.framework.dll`, no referencia `RowMate.Simulator` (si no, sin el define no compilaría). Lo usan EditMode, Simulator y PlayMode.
- **API de NUnit permitida**: la intersección de 3.5 y 3.14 (ARCHITECTURE §15.1). La **fuente de verdad es el compilador** (QA-10): `RowMate.Tests.UnityApiCheck` compila las fuentes de `Tests/Support`, `Tests/EditMode` y `Tests/Simulator` contra el paquete NUnit **3.5.0**, así que una API posterior a 3.5 rompe la compilación de la solución aunque no figure en ninguna lista. `NUnitCompatibilityTests` queda para lo que **compila** en 3.5 pero no se admite (`[Parallelizable]`, métodos `[Test] async Task`, `Assert.ThrowsAsync`/`CatchAsync`) y como mensaje claro y temprano para los casos frecuentes (`Assert.Multiple`, `ClassicAssert`, `Warn.`, `[NonParallelizable]`, `[DefaultFloatingPointTolerance]`, `SetArgDisplayNames`, `AddTestAttachment`, `Is.Not.Default`, `Is.AnyOf`). Los tests son sincrónicos (`FakeScheduler`, `TestMainThreadContext`). El NUnit de Unity es un 3.5 propio (`com.unity.ext.nunit`): la equivalencia exacta con el paquete de NuGet es NV hasta la It. 2.
- **Sin `record` ni `init` en las fuentes de test** (QA-03): `Tests/Support`, `Tests/EditMode`, `Tests/Simulator` y `Tests/PlayMode` usan `class`/`struct` con constructor para sus datos (casos, filas, *fixtures*). En Unity esos assemblies ven por IVT dos o más *shims* internos de `IsExternalInit` (Core, App, Simulator) y declarar un `record` da `CS0518`; en `net8.0` compila, así que sin esta regla el error aparecería recién en Unity. Usar los records de Core/App/Simulator (inicializadores y `with`) está permitido. Dos controles: `NUnitCompatibilityTests.NoRecordOrInitInTestSources`, sobre el texto **sin comentarios ni literales** de `Tests/**/*.cs`, con dos regex: declaración `(?m)(^\s*|\b(?:public|internal|private|protected|sealed|abstract|partial)\s+)record\s+(?!with\b)(?:class\s+|struct\s+)?[A-Za-z_]\w*\s*[({:<]` y accesor `[{;]\s*init\s*([;{]|=>)` (el test incluye casos propios que **no** deben contar: `var record = …`, `return record with { … }`, `x = init;`); y la compilación de `UnityApiCheck`, que falla con `CS0518` (probado).
- **API de .NET permitida**: la de `netstandard2.1`, garantizada por `RowMate.Tests.UnityApiCheck`, que se compila en **cada** corrida de `run-tests.ps1` (paso 1, `dotnet build` de la solución, §2.6; QA-01). `dotnet test` solo no alcanza: compila únicamente los proyectos de test y sus dependencias, y `UnityApiCheck` y `RowMate.Tokens` no lo son.
- **Datos**: `RepoPaths` (§2.5) los ubica igual en `dotnet test` y en el Editor.
- **Trazabilidad**: `// legacy: test/<archivo>:<línea> '<nombre>'` en cada caso portado; `[Category("AC-…")]`/`.SetCategory("AC-…")` por criterio; `[Category("LT-nn")]` en los 19 casos legacy; `[TestCase(TestName = "<nombre original exacto>")]` en los casos legacy sin parámetros. Las categorías y los nombres se escriben como **literales de texto** en el atributo o en la llamada (nunca armados en tiempo de ejecución), porque `TraceabilityTests` los lee del código fuente (§8.6, QA-09).

### 2.5 `Tests/Support` (WP-A)

```csharp
namespace RowMate.Tests.Support
{
    public static class RepoPaths
    {
        // Orden: variable ROWMATE_REPO_ROOT → AssemblyMetadata "RowMateRepoRoot" (solo dotnet) →
        // subir desde TestContext.CurrentContext.TestDirectory → desde Directory.GetCurrentDirectory()
        // → desde AppContext.BaseDirectory, hasta la carpeta con ProjectSettings/ProjectVersion.txt.
        public static string Root { get; }
        public static string RowMateAssets { get; }                      // <root>/Assets/RowMate
        public static string Golden(string fileName);                    // <root>/Assets/RowMate/Tests/Golden/oracle/<file>
        public static string Catalog(string languageCode);               // <root>/Assets/RowMate/Localization/strings.<lang>.json
        public static string Combine(params string[] relativeParts);    // relativo a Root
    }

    public sealed class GoldenHeader { public int Schema; public string Generator; public string DartVersion;
                                       public string LegacyCommit; public string GeneratedAt; }   // propiedades de solo lectura
    public sealed class GoldenFile
    {
        public string FileName { get; }
        public GoldenHeader Header { get; }
        public JsonNode Body { get; }
        public JsonNode Section(string name);                            // falla con mensaje claro si no existe
        public GoldenTable Table(string name);                           // sección en formato de filas (§6.2)
        public long SizeBytes { get; }                                   // para el tope de §6.2
    }
    // GoldenTable.cs (QA-06): sección { "columns": [...], "rows": [[...], ...] }
    public sealed class GoldenTable
    {
        public IReadOnlyList<string> Columns { get; }
        public int Count { get; }
        public GoldenRow this[int index] { get; }
        public int ColumnIndex(string column);                           // falla con mensaje claro si no existe
    }
    public readonly struct GoldenRow
    {
        public JsonNode Cell(string column);
        public double F64(string column);                                // celda "<16 hex>" (bits IEEE-754)
        public long Int(string column);
        public string Text(string column);
    }
    public static class GoldenVectors
    {
        public static GoldenFile Load(string fileName);                  // cacheado; valida schema == 1, commit de 40 hex, dartVersion "3."
        public static IEnumerable<TestCaseData> Cases(string fileName, string section,
                                                      Func<JsonNode, TestCaseData> map); // para [TestCaseSource]
        public static IEnumerable<GoldenRow> Rows(string fileName, string section);  // barridos: un solo test recorre las filas
    }
    public static class GoldenJsonExtensions
    {
        public static double AsF64(this JsonNode node);                  // {"v": …, "bits": "<16 hex>"} o "<16 hex>"; siempre por los bits
        public static byte[] AsBytes(this JsonNode node);                // "2C 00 30" o [44, 0, 48]
    }
    public static class HexBytes { public static byte[] Parse(string spaced); public static string Format(ReadOnlySpan<byte> b); }

    public sealed class FakeScheduler : IScheduler
    {
        public FakeScheduler(DateTimeOffset? startUtc = null, TimeSpan? localOffset = null); // por defecto 2026-01-01T00:00Z, UTC−3
        public DateTimeOffset UtcNow { get; }
        public DateTimeOffset LocalNow { get; }
        public double MonotonicSeconds { get; }                          // arranca en 0
        public bool IsCatchingUp { get; set; }
        public IDisposable Schedule(TimeSpan delay, Action callback);
        public IDisposable SchedulePeriodic(TimeSpan period, Action callback); // disparos en múltiplos exactos del período
        public Task Delay(TimeSpan delay, CancellationToken ct = default);     // se completa en línea dentro de Advance
        public void Advance(TimeSpan by);   // ejecuta en orden (vencimiento, luego orden de alta) los vencidos ≤ destino,
                                            // incluidos los que agendan los callbacks; al final Now = destino
        public void RunDue();               // = Advance(TimeSpan.Zero)
        public int PendingTimers { get; }
    }
    public sealed class TestMainThreadContext : SynchronizationContext
    {
        public static void Run(Func<Task> body, int maxSteps = 100000);  // instala el contexto, bombea hasta completar
        public void RunUntilIdle();
        public override void Post(SendOrPostCallback d, object? state);
        public override void Send(SendOrPostCallback d, object? state);
    }
    public sealed class SequenceRandom : IRandom
    {
        public SequenceRandom(params double[] values);                   // sin valores: cualquier llamada falla
        public double NextDouble();                                      // devuelve en orden; si se agotan, InvalidOperationException
        public int Calls { get; }
    }
    public sealed class RecordingLogger : IAppLogger { public IReadOnlyList<string> Lines { get; } /* "[TAG] Level: msg" */ }
}
```

`InlineDbExecutor` (ADR 005) llega en la It. 3 junto con `IDbExecutor`, su primer uso (D-07).

`Tests.Support` no declara `record` ni `init` (§2.4, QA-03): `GoldenHeader` y los demás tipos son clases con propiedades de solo lectura asignadas en el constructor. Los barridos grandes de los vectores dorados (miles de filas) se recorren con `GoldenVectors.Rows` dentro de **un** test que junta las diferencias y falla con las primeras 20 (entrada, esperado, obtenido), en vez de un `TestCaseData` por fila: la salida de `dotnet test` y la matriz `.trx` siguen siendo legibles. Los casos de tabla (pocos y con nombre) sí van con `Cases`.

### 2.6 Comando exacto

Desde la raíz del repo, en Windows (la ruta completa porque `dotnet` no está en el `PATH`), **dos comandos en este orden** (QA-01):

```
"C:/Program Files/dotnet/dotnet.exe" build tools/dotnet/RowMate.sln --configuration Release --artifacts-path tools/dotnet/.artifacts
"C:/Program Files/dotnet/dotnet.exe" test tools/dotnet/RowMate.sln --configuration Release --no-build --artifacts-path tools/dotnet/.artifacts --results-directory tools/dotnet/.artifacts/TestResults --logger "console;verbosity=normal" --logger "trx;LogFilePrefix=rowmate-core"
```

Con rutas absolutas:

```
"C:/Program Files/dotnet/dotnet.exe" build tools/dotnet/RowMate.sln --configuration Release --artifacts-path tools/dotnet/.artifacts
"C:/Program Files/dotnet/dotnet.exe" test tools/dotnet/RowMate.sln --configuration Release --no-build --artifacts-path tools/dotnet/.artifacts --results-directory tools/dotnet/.artifacts/TestResults --logger "console;verbosity=normal" --logger "trx;LogFilePrefix=rowmate-core"
```

- **Por qué dos comandos.** `dotnet test <sln>` compila solo los proyectos de test y sus dependencias: `RowMate.Tests.UnityApiCheck` y `RowMate.Tokens` quedarían sin compilar (QA lo probó: una biblioteca rota que ningún test referencia da `exit 0`). `dotnet build <sln>` compila los 9 proyectos; `--no-build` hace que los tests corran sobre **esos** binarios. Los dos comandos usan la misma configuración y el mismo `--artifacts-path` (si no, `--no-build` no encuentra los binarios). Se descartó la otra opción de QA (`ProjectReference` con `ReferenceOutputAssembly="false"` desde `Tests.EditMode`): ataría el proyecto de test a las herramientas y metería NUnit 3.5 en su grafo de restauración.
- `--artifacts-path` deja `bin/`, `obj/` y resultados de **todos** los proyectos en `tools/dotnet/.artifacts/` (ignorado por git): nada se escribe junto a las fuentes de `Assets/` ni en las carpetas de los `.csproj`.
- `tools/dotnet/run-tests.ps1` (compatible con Windows PowerShell 5.1 y `pwsh` 7; sin `?.`, `??` ni `&&`; **solo ASCII** en el archivo, porque PowerShell 5.1 lee un `.ps1` sin BOM como ANSI y deforma los acentos) resuelve `dotnet` (PATH, si no `C:/Program Files/dotnet/dotnet.exe`) y:
  1. **imprime** el `dotnet build` de arriba y lo ejecuta; si su código no es 0, imprime `[run-tests] la compilacion fallo (<codigo>): no se corren tests` y **sale con ese código** sin correr tests (con `--no-build` correrían binarios viejos);
  2. **imprime** el `dotnet test --no-build` (con los agregados de los parámetros) y lo ejecuta;
  3. con `-Matrix`, arma la matriz (abajo) aunque haya tests rojos;
  4. sale con el código del `dotnet test`. Es decir, propaga el **primer** código distinto de 0.

  El flujo se probó en el *scratchpad* con Windows PowerShell 5.1 sobre una réplica de la solución: todo verde → `exit 0` y la línea de demo visible con `-Demo`; un `record` en una fuente de test → el `dotnet build` falla con `CS0518` en `UnityApiCheck`, no se corren tests y el script sale con 1. El paso 1 compila **siempre** la solución entera, también con `-Filter` (los filtros de trabajo de B y C, §9.0, igual compilan `UnityApiCheck`). Parámetros: `-Filter "<expresión>"` (se agrega `--filter` al `dotnet test`), `-Ci` (pone `ROWMATE_CI=1`: la higiene que depende de git falla en vez de quedar *inconclusa*), `-Demo` (`--filter "TestCategory=Demo"` y `console;verbosity=detailed`, para que se vean las líneas `[DEMO]` de §8.7; probado: con `detailed` la consola muestra la salida de los tests que pasan), `-Matrix` (después de correr, lee los `.trx` de los dos assemblies de test y escribe `tools/dotnet/.artifacts/TestResults/ac-matrix.md` con AC → test → resultado, AC-BASE-09).
- Filtrar por AC: `run-tests.ps1 -Filter "TestCategory=AC-BLE-02-02"` (o los dos comandos de arriba, con `--filter "TestCategory=AC-BLE-02-02"` en el segundo).
- `CiWorkflowTests.ScriptBuildsWholeSolutionThenTestsWithoutRebuilding` (QA-01) lee `run-tests.ps1` y exige: un `build` de `tools/dotnet/RowMate.sln` (la solución, no un `.csproj`) con `--configuration Release` y `--artifacts-path tools/dotnet/.artifacts`; después, un `test` de la misma solución con `--no-build` y el mismo `--artifacts-path`; que el `build` aparezca antes que el `test`; y que entre los dos haya una salida con `$LASTEXITCODE` si el `build` falla. `AsmdefLintTests` exige además que `RowMate.sln` contenga los 9 `.csproj` de §2.1.

---

## 3. Clases e interfaces (firmas, fuente legacy, paridad)

Reglas comunes: C# 9, bloques `namespace X { }`, un tipo público por archivo, `sealed` por defecto, cultura invariante explícita en todo número que se vuelve texto (`Fmt.Integer`, `CultureInfo.InvariantCulture`), sin `Math.Round` ni `ToString("F…")` en Core/App/Simulator, sin `System.Random` fuera de `SystemRandom`, sin `DateTime.Now/UtcNow` en Core/App/Simulator (reloj inyectable). Nada de esto toca hilos: todo el código de la It. 1 es sincrónico y de hilo principal.

### 3.1 WP-A — piezas base

#### Reactividad (`RowMate.Core.Common`, ADR 004, ARCHITECTURE §5)

```csharp
public interface IReadOnlyObservable<T>
{
    T Value { get; }
    IDisposable Subscribe(Action<T> onChange, bool emitCurrent = true); // sincrónico, en el hilo que cambia el valor
}
public sealed class ObservableValue<T> : IReadOnlyObservable<T>
{
    public ObservableValue(T initial, IEqualityComparer<T>? comparer = null);
    public T Value { get; set; }          // notifica solo si cambia según el comparador
    public void ForceNotify();            // = _reemitStatus() del legacy
    public IDisposable Subscribe(Action<T> onChange, bool emitCurrent = true);
}
public sealed class EventStream<T> : IObservable<T>   // broadcast sin buffer (StreamController.broadcast)
{
    public void Emit(T value);
    public IDisposable Subscribe(Action<T> handler);
    public IDisposable Subscribe(IObserver<T> observer); // solo OnNext
    public int SubscriberCount { get; }
}
```
Suscriptores en arreglo *copy-on-write*: emitir no reserva memoria; desuscribirse durante una emisión no afecta la emisión en curso; un `Value =` dentro de un callback notifica en el acto (anidado, como `ChangeNotifier`).

#### Tiempo, azar y log (`RowMate.Core.Common`, ADR 005)

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }        // offset 0
    DateTimeOffset LocalNow { get; }      // hora local con su offset (los modelos usan DateTimeOffset, D-11)
    double MonotonicSeconds { get; }      // inmune a cambios de hora
}
public interface IScheduler : IClock
{
    IDisposable Schedule(TimeSpan delay, Action callback);          // Timer(...)
    IDisposable SchedulePeriodic(TimeSpan period, Action callback); // Timer.periodic(...)
    Task Delay(TimeSpan delay, CancellationToken ct = default);     // Future.delayed(...)
    bool IsCatchingUp { get; }                                      // alcance tras segundo plano (ADR 017)
}
public interface IRandom { double NextDouble(); }                 // [0; 1), como Random.nextDouble() de Dart
public sealed class SystemRandom : IRandom { public SystemRandom(int? seed = null); public double NextDouble(); }
public enum LogLevel { Debug, Info, Warning, Error }
public interface IAppLogger { void Log(LogLevel level, string tag, string message, Exception? error = null); }
public sealed class NullAppLogger : IAppLogger { public static readonly NullAppLogger Instance; }
public static class TaskExtensions { public static void Forget(this Task task, IAppLogger logger, string tag); } // registra la excepción
public enum PlatformKind { Android, Ios, MacOs, Windows }
```
`IAppLogger` y no `ILogger` para no chocar con `UnityEngine.ILogger` en los assemblies de Unity (D-03). Los *tags* son los del legacy (`[BLE]`, `[HRM]`…).

#### Identidad (`AppIdentity`, F-APP-16)

```csharp
public static class AppIdentity
{
    public const string ProductName = "RowMate";
    public const string CompanyName = "iguisoft";
    public const string Version = "1.0.0";                        // legacy: pubspec.yaml:4 (1.0.0+1)
    public const int BuildNumber = 1;
    public const string AndroidApplicationId = "com.iguisoft.rowmate";
    public const string WindowsApplicationId = "com.iguisoft.rowmate"; // RESERVADO para el MSIX de la It. 14 (AC-APP-16-02, D-02):
                                                                       // Unity no tiene id de Windows y ningún paso lo aplica
    public const string AppleBundleId = "com.iguisoft.rowmate.unity";  // iOS y macOS (R-14: no pisar el legacy); también el
                                                                       // id único de Standalone que comparten macOS y Windows
    public const int AndroidMinSdk = 26;                          // Q-90
    public const string IosMinimumVersion = "15.0";
}
```

#### JSON mínimo (`RowMate.Core.Common.Json`)

```csharp
public enum JsonKind { Null, Bool, Number, String, Array, Object }
public sealed class JsonNode
{
    public JsonKind Kind { get; }
    public string AsString(); public bool AsBool(); public long AsLong(); public double AsDouble(); // invariante
    public string RawNumber { get; }                               // texto exacto del número
    public IReadOnlyList<JsonNode> Items { get; }
    public IReadOnlyList<KeyValuePair<string, JsonNode>> Properties { get; } // en orden de aparición
    public JsonNode this[string key] { get; }                      // KeyNotFoundException si falta
    public bool TryGet(string key, out JsonNode value);
}
public static class JsonParser { public static JsonNode Parse(string text); } // RFC 8259 estricto: FormatException con línea/columna;
                                                                              // claves duplicadas = error; profundidad ≤ 64; \uXXXX y pares sustitutos
```

#### Paridad numérica (`RowMate.Core.Text`, ADR 012, QA-P-02)

```csharp
public static class Fmt
{
    public static string Fixed(double x, int fractionDigits);       // = toStringAsFixed
    public static bool TryFormatFixed(double x, int fractionDigits, Span<char> destination, out int charsWritten);
    public static string DartToString(double x);                    // = '$x' / toString()
    public static string Integer(long value);                       // = int.toString(): invariante, sin miles, '-' ASCII
}
public static class DartMath
{
    public static long Round(double x);                             // = double.round()
    public static double RoundToFixed(double x, int fractionDigits); // = double.parse(x.toStringAsFixed(n))
    public static long Mod(long a, long b);                         // = a % b (no negativo)
    public static double Mod(double a, double b);
    public static long TruncDiv(long a, long b);                    // = a ~/ b
    public static long TruncDiv(double a, double b);
}
internal readonly struct DoubleParts { bool Negative; ulong Mantissa; int Exponent; static DoubleParts Of(double x); } // |x| = m·2^e exacto
public static class IsoDate
{
    public static string FormatUtc(DateTimeOffset value);           // = toUtc().toIso8601String() (con 'Z')
    public static string FormatLocal(DateTimeOffset value);         // = toIso8601String() de un DateTime local (sin zona)
    public static DateTimeOffset Parse(string text, TimeZoneInfo localZone); // = DateTime.parse(...).toLocal()
}
public static class TimeFormats
{
    public static string Pad2(long value);                          // = value.toString().padLeft(2, '0') ("-1" queda "-1")
    public static string MinSec(long totalSeconds);                 // Pad2(t ~/ 60) + ":" + Pad2(t % 60), con ~/ y % de Dart
    public static string SplitOrDashes(long seconds);               // ≤ 0 → "--:--"; si no MinSec
    public static string Elapsed(long seconds);                     // h ≥ 1 → "{h}:{MM}:{SS}"; si no "{MM}:{SS}"
}
```

**Algoritmo exacto de `Fmt.Fixed(x, n)`** (= `double.toStringAsFixed` de la VM de Dart: comprueba el rango, `NaN`, `|x| ≥ 1e21` → `toString()`, y si no `DoubleToStringAsFixed` de *double-conversion*, que redondea el valor binario exacto y desempata hacia arriba en magnitud):

1. `n ∉ [0; 20]` → `ArgumentOutOfRangeException`.
2. `NaN` → `"NaN"`. `x ≥ 1e21` o `x ≤ −1e21` (incluye `±∞`) → `DartToString(x)` (`"Infinity"`, `"-Infinity"`, `"1e+21"`).
3. Descomposición (`DoubleParts`): `bits = BitConverter.DoubleToInt64Bits(x)`; `neg = bits < 0` (bit de signo, así `-0.0` es negativo); `be = (bits >> 52) & 0x7FF`; `f = bits & 0xF_FFFF_FFFF_FFFF`. Si `be == 0`: `m = f`, `e = −1074` (subnormal o cero). Si no: `m = f | 2^52`, `e = be − 1075`. Entonces `|x| = m · 2^e` sin error.
4. `q = ⌊ m · 10^n · 2^e + ½ ⌋` exacto:
   - `m == 0` → `q = 0`.
   - `e ≥ 0` → `q = m · 10^n · 2^e` (entero exacto).
   - `e < 0`, con `s = −e` y `N = m · 10^n`: `q = N >> s`; `r = N − (q << s)`; si `2r ≥ 2^s` → `q + 1` (el empate exacto sube: se aleja del cero porque se trabaja con `|x|`).
5. Camino rápido sin asignar memoria (`ulong`): si `N = m·10^n` no desborda `ulong` (se chequea con `m ≤ ulong.MaxValue / 10^n`) y, en `e ≥ 0`, `N << e` tampoco (`e ≤ 63 − bitlen(N)`). Con `s ≥ 64`: `s > 64 → q = 0`; `s == 64 → q = (N ≥ 2^63) ? 1 : 0`. Cubre todo valor de la app (`n ≤ 3`, magnitudes de métricas). Si no, `System.Numerics.BigInteger` (netstandard2.1, compatible con IL2CPP).
6. Texto: dígitos de `q` (invariante); si tiene menos de `n + 1` dígitos se completa con ceros a la izquierda; si `n > 0`, punto antes de los últimos `n`; `-` adelante **si `neg`** (aunque `q` sea 0). Así `-0.04 → "-0.0"`, `-0.0 → "-0"` con `n = 0`, `-0.05 → "-0.1"`, `1.005/2 → "1.00"`, `24.45/1 → "24.4"`, `0.25/1 → "0.3"`, `0.35/1 → "0.3"`, `2.5/0 → "3"`, `99.95/1 → "100.0"`, `1e20/2 → "100000000000000000000.00"`, `5e-324/3 → "0.000"`.
7. `TryFormatFixed` hace lo mismo escribiendo en el `Span<char>` (devuelve `false` si no entra). Por el camino rápido, 0 B por llamada (test `#if NET5_0_OR_GREATER` con `GC.GetAllocatedBytesForCurrentThread`). Lo usan `TabularNumber` y el HUD desde la It. 2/4a.

**Algoritmo de `DartMath.Round(x) : long`** (= `double.round()`, mitad alejándose del cero, devuelve `int`):
1. `NaN` o `±∞` → `ArgumentException` (Dart: `UnsupportedError`). `|x| ≥ 2^63` → `ArgumentOutOfRangeException` (fuera del rango de la app).
2. `|x| ≥ 2^52` → `(long)x` (ya es entero).
3. Si no: `t = Math.Truncate(x)`; `f = x − t` (resta exacta para `|x| < 2^52`); `f ≥ 0.5 → t + 1`; `f ≤ −0.5 → t − 1`; devuelve `(long)t`. Nunca hay `-0`: `(-0.05).round() = 0`; `0.49999999999999994 → 0` (no cae en la trampa de `floor(x + 0.5)`); `4503599627370495.5 → 4503599627370496`; `-1.5 → -2`; `120.5 → 121`.

**`DartMath.RoundToFixed(x, n)`** (= `double.parse(x.toStringAsFixed(n))`, `rowing_simulator.dart:60`): calcula `q` y `neg` con los pasos 3-4 de `Fixed`; si `q < 2^53` devuelve `(neg ? −1 : 1) · ((double)q / 10^n)` (división IEEE de dos valores exactos = el `double` que da un *parser* correctamente redondeado; `n ≤ 20 ≤ 22`); si no, `double.Parse(Fixed(x, n), InvariantCulture)`. `24.25 → 24.3`, `24.35 → 24.4`.

**`DartMath.Mod`** (= `%` de Dart, `DartModulo` de la VM): `r = a % b`; `r < 0 → r + |b|`; en `double`, además `r == 0 → +0.0`. Oráculo: `(-7) % 3 = 2`, `(-7.5) % 4096.0 = 4088.5`, `-65 % 60 = 55`.

**`Fmt.DartToString(x)`** (= `Double_toString` de la VM: *double-conversion* `ToShortest` con `decimal_in_shortest_low = −6`, `high = 21`, `EMIT_POSITIVE_EXPONENT_SIGN | EMIT_TRAILING_DECIMAL_POINT | EMIT_TRAILING_ZERO_AFTER_POINT`): `NaN`, `Infinity`, `-Infinity`; `±0.0 → "0.0"/"-0.0"`. Si no, dígitos más cortos **exactos, sin parsear** (no se confía en el `double.Parse` de Mono), con el mismo resultado que `BignumDtoa` en modo `SHORTEST` de *double-conversion* (corregido por QA-04):

1. `neg`, `m`, `e`, `be` y `f` del paso 3 de `Fixed`. Todo se escala por `2^(e−2)` para trabajar con enteros (`BigInteger`): `X = 4m`; `H = 4m + 2` (extremo superior); `L = 4m − 1` si `f == 0 && be > 1` (potencia de 2 normal: el vecino de abajo está a la **mitad** de distancia, intervalo **asimétrico**), si no `L = 4m − 2`. Si `e − 2 ≥ 0`, `X`, `L`, `H` se corren `e − 2` bits a la izquierda y `den = 1`; si no, `den = 2^(2−e)`. Los extremos entran si `m` es par (`ROUND_HALF_EVEN` del *parser* IEEE) y no entran si es impar. (`be == 1` con `f == 0`, el menor normal, es simétrico: su vecino de abajo es el mayor subnormal, a la misma distancia; igual que `Double::LowerBoundaryIsCloser` de *double-conversion*.)
2. `k` = posición del punto decimal de `|x|`: `10^(k−1) ≤ |x| < 10^k`, estimado con `Math.Log10` y **corregido** con comparaciones exactas de `X/den` contra `10^j`.
3. Para `p = 1…17`, con `t = p − k`: si `t ≥ 0`, `X' = X·10^t`, `L' = L·10^t`, `H' = H·10^t` y `d = den`; si `t < 0`, `X' = X`, `L' = L`, `H' = H` y `d = den·10^(−t)`. Entonces `c = ⌊X' / d⌋`, y los dos candidatos de `p` cifras son el **piso** `c` y el **techo** `c + 1` (en unidades de `10^(k−p)`; en la escala común valen `c·d` y `(c+1)·d`). El piso vale si `c·d ≥ L'` y el techo si `(c+1)·d ≤ H'` (desigualdades estrictas si `m` es impar). Si ninguno vale, sigue con `p + 1`. Si vale uno, ese. Si valen los dos, el **más cercano** a `|x|` (comparando `X' − c·d` con `(c+1)·d − X'`); en **empate exacto**, el de último dígito **par** (`BignumDtoa` desempata al par; redondear la mitad hacia arriba da otro resultado, por ejemplo en `1125899906842624.25`, que en Dart es `1125899906842624.2`).
4. Si el elegido es `10^p` (acarreo: `9.99…` que sube), los dígitos son `1` y `k = k + 1`. Se sacan los ceros finales de los dígitos.
5. Formato, con los dígitos `d₁…dₙ` y `k`: si `−6 < k ≤ 21` → notación fija: `k ≤ 0` → `0.` + `−k` ceros + dígitos; `k ≥ n` → dígitos + `k − n` ceros + `.0`; si no, punto después de `k` dígitos. Si no → `d₁[.d₂…dₙ]e±(k−1)` (sin `.0` si `n = 1`). `-` adelante si `neg`.

**Verificado** (scratchpad, Dart 3.13.5 contra una implementación C# 9/`netstandard2.1` de estos pasos): **0 diferencias en 982 699 `double`**: las 2 098 potencias de 2 (2^−1074 … 2^1023) con los dos signos y sus vecinos, las potencias de 10 de 1e−323 a 1e308 con ±3 ulp, 300 000 patrones de bits aleatorios, 100 000 magnitudes 10^[−9; 22], `k/1000` para |k| ≤ 100 000, 22 000 subnormales, 200 000 diádicos `m·2^−j` y 100 000 empates `b + 0,25`/`b + 0,75` con `b ∈ [2^50; 2^51)`. En total pasaron por la regla de empate **103 983** casos. El algoritmo anterior (un solo candidato, mitad hacia arriba) daba 92 diferencias solo en las potencias de 2 (las 46 de QA, con los dos signos) y 50 000 en los empates. Vectores: `24.0`, `0.1`, `1e+21`, `1e-7`, `0.000001`, `-0.0`, `33.333333333333336` (100/3), `5e-324`, `5.684341886080802e-14` (2^−44), `6.189700196426902e+26` (2^89), `2.9802322387695312e-8` (2^−25, empate), `1.7976931348623157e+308`, `73786976294838210000.0` (2^66: ceros de relleno). No hay usos visibles en la It. 1; lo usa `UssTokenWriter` para alfas (`0.86`) y longitudes (`1.5`).

**`IsoDate`** (`toIso8601String`, `workout_session.dart:43-44`, `:57-60`): `yyyy-MM-ddTHH:mm:ss.mmm` + `uuu` **solo si los microsegundos no son 0** (`(ticks / 10) % 1000`; los 100 ns sobrantes se truncan, Dart no los tiene) + `Z` en `FormatUtc`. Años fuera de `[0; 9999]` → `ArgumentOutOfRangeException` (Dart usaría 6 dígitos; la app no los genera). `Parse` acepta `yyyy-MM-dd`, opcional `[T| ]HH[:mm[:ss[.f…]]]` (cualquier cantidad de decimales, se truncan a microsegundos como `DateTime.parse`) y opcional `Z|z|±HH[[:]mm]`; con zona → instante convertido a `localZone`; sin zona → hora de pared de `localZone`. Texto inválido → `FormatException`.

**`TimeFormats`** (`rowing_data.dart:24-40`, `workout_session.dart:29-37`, `:94-109`, `interval_step.dart:87-91`): única implementación de los formatos `MM:SS` y `H:MM:SS`; `RowingData`, `SessionStats`, `WorkoutSession` y `StepLabels` la llaman (AC-BLE-01-06). `MinSec(−65) = "-1:55"`, `MinSec(−5) = "00:55"`, `MinSec(6000) = "100:00"`, `Elapsed(86400) = "24:00:00"`.

**Cómo se prueba** (`FmtFixedTests`, `FmtDartToStringTests`, `DartMathTests`, `IsoDateTests`, `TimeFormatsTests`, `CultureIndependenceTests`):
1. `[TestCase]` explícitos con las tablas de AC-BASE-01, QA_PLAN_REVIEW §3.2 y ADR 012 §5 (literales de C#, que el compilador redondea correctamente).
2. Vectores dorados de `fmt.json` (§6): barridos y valores aleatorios de Dart, leídos por sus bits; para `DartToString`, además, las 2 098 potencias de 2 (`toStringPow2`) y 500 empates (`toStringTies`) (QA-04).
3. Propiedades internas (con `InternalsVisibleTo`): camino rápido = camino `BigInteger` en 10 000 `double` pseudoaleatorios (semilla fija); `long.Parse(Fixed(x, 0)) == Round(x)` para `|x| < 1e15`; `RoundToFixed(x, n) == double.Parse(Fixed(x, n))` en .NET 8.
4. Cultura: `[SetCulture("es-AR")]`, `de-DE`, `fr-FR`, `sv-SE` (signo menos U+2212) y `en-US`: mismos textos (AC-BASE-02).
5. Las mismas pruebas corren en Unity EditMode desde la It. 2 (NV ahora).

#### BLE (`RowMate.Core.Ble`)

```csharp
public readonly struct BleUuid : IEquatable<BleUuid>
{
    public const string BaseSuffix = "-0000-1000-8000-00805f9b34fb";
    public static readonly BleUuid Empty;
    public static BleUuid FromShort(ushort value);           // 0000xxxx-0000-1000-8000-00805f9b34fb
    public static BleUuid From32(uint value);                // xxxxxxxx-0000-1000-8000-00805f9b34fb
    public static BleUuid Parse(string text);                // FormatException
    public static bool TryParse(string? text, out BleUuid value);
    public ulong High { get; }  public ulong Low { get; }   // igualdad y hash sin asignar memoria
    public override string ToString();                       // 128 bits en minúsculas (= Guid.str128)
    public string ToShortString();                           // 16/32/128 como Guid.str de FBP
    public bool Equals(BleUuid other); public static bool operator ==(BleUuid a, BleUuid b); /* != , GetHashCode */
}
public sealed record BleScanResult(string DeviceId, string? PlatformName, string? AdvName, int Rssi,
                                   IReadOnlyList<BleUuid> ServiceUuids);
```
Paridad con `flutter_blue_plus_platform_interface` 9.0.3 `lib/src/guid.dart:24-90`: se sacan los `-`, se decodifican pares hex (mayúsculas o minúsculas), 2 bytes = 16 bits, 4 = 32 bits, 16 = 128 bits, otra longitud o un par no hex → error; `""` → `Empty` (como `List.filled(16, 0)`). La igualdad compara la forma de 128 bits (AC-BLE-03-04).

#### Tokens, contraste y USS: §4.

### 3.2 WP-B — lectura del remo, FTMS, pulso, filtro, simulador

```csharp
namespace RowMate.Core.Models
{
    // legacy: lib/core/models/rowing_data.dart:2-62
    public sealed record RowingData
    {
        public static readonly RowingData Empty = new RowingData();
        public double StrokeRate { get; init; }      // spm (decimal)
        public int StrokeCount { get; init; }
        public int DistanceMeters { get; init; }
        public int Pace500mSeconds { get; init; }
        public int PowerWatts { get; init; }
        public int TotalCalories { get; init; }
        public int HeartRate { get; init; }          // 0 = sin dato
        public int ElapsedSeconds { get; init; }
        public string Pace500mFormatted => TimeFormats.SplitOrDashes(Pace500mSeconds); // :24-29
        public string ElapsedFormatted => TimeFormats.Elapsed(ElapsedSeconds);         // :32-40
        public RowingData CopyWith(double? strokeRate = null, int? strokeCount = null, int? distanceMeters = null,
                                   int? pace500mSeconds = null, int? powerWatts = null, int? totalCalories = null,
                                   int? heartRate = null, int? elapsedSeconds = null); // :42-62, null = conservar
        public RowingData WithHeartRate(int bpm);    // el pulsómetro reemplaza solo el pulso
    }
    // legacy: lib/features/device/device_screen.dart:253-258 (regla de texto; la grilla es de la It. 2)
    public static class RowingFormats
    {
        public static string Spm(double strokeRate);  // Fmt.Fixed(x, 1): 24.0 → "24.0", 24.25 → "24.3"
        public static string Watts(int watts);        // Fmt.Integer: -10 → "-10"
        public static string Meters(int meters);      // Fmt.Integer + "m": 10000 → "10000m"
        public static string HeartRate(int bpm);      // Fmt.Integer
        public static string Split(int seconds);      // TimeFormats.SplitOrDashes
        public static string Elapsed(int seconds);    // TimeFormats.Elapsed
    }
}
namespace RowMate.Core.Ble
{
    // legacy: lib/core/bluetooth/ftms_parser.dart:22-24
    public static class FtmsUuids
    {
        public const string ServiceText = "00001826-0000-1000-8000-00805f9b34fb";
        public const string RowerDataText = "00002ad1-0000-1000-8000-00805f9b34fb";   // 0x2AD1 (no 0x2AD2, DC-01)
        public const string ControlPointText = "00002ad9-0000-1000-8000-00805f9b34fb";
        public static readonly BleUuid Service, RowerData, ControlPoint;
    }
    // legacy: ftms_parser.dart:26-140
    public static class FtmsParser
    {
        public static RowingData? ParseRowerData(ReadOnlySpan<byte> raw);
        public static RowingData? ParseRowerData(byte[] raw);
    }
    public sealed record HeartRateReading(int Bpm, bool? SensorContact); // null = no informa (heart_rate_parser.dart:4-11)
    // legacy: lib/core/bluetooth/heart_rate_parser.dart:20-40
    public static class HeartRateParser
    {
        public const string ServiceUuid = "0000180d-0000-1000-8000-00805f9b34fb";
        public const string MeasurementUuid = "00002a37-0000-1000-8000-00805f9b34fb";
        public static readonly BleUuid Service, Measurement;
        public static HeartRateReading? Parse(ReadOnlySpan<byte> bytes);
        public static HeartRateReading? Parse(byte[] bytes);
    }
    // legacy: lib/core/bluetooth/scan_filter.dart:12-20
    public static class ScanFilter
    {
        public static bool IsClientSide(PlatformKind platform);            // solo Windows (Linux fuera, Q-50)
        public static bool AdvertisesService(BleScanResult result, BleUuid service);
        public static bool Accept(BleScanResult result, BleUuid service, bool clientSide); // !clientSide || anuncia
    }
}
namespace RowMate.Simulator
{
    // legacy: lib/core/dev/rowing_simulator.dart:1-83
    public sealed class RowingSimulator
    {
        public const int MinWatts = 30, MaxWatts = 500, MinSpm = 14, MaxSpm = 40;   // :7-10
        public RowingSimulator(IRandom? random = null, bool noise = true);          // :12-13; por defecto SystemRandom
        public bool Noise { get; }
        public int TargetWatts { get; set; }         // 150, se recorta a [30; 500] (:20-22)
        public int TargetSpm { get; set; }           // 24, se recorta a [14; 40] (:24-26)
        public bool Rowing { get; set; }             // true (:29)
        public int HeartRate { get; }                // DartMath.Round(_heartRate) (:32); interno arranca en 70 (:37)
        public RowingData Tick();                    // 1 tick = 1 s (:40-70)
        public static double PaceForWatts(int watts);       // 500 * Math.Pow(2.80 / watts, 1.0 / 3.0) (:77-78)
        public static double CaloriesPerSecond(int watts);  // (watts * 4 * 0.8604 + 300) / 3600 (:81-82)
    }
}
```

Notas de paridad (B):
- **FTMS**: `< 2` bytes → `null` (`:27`), lo que cubre la notificación vacía (`ble_service.dart:292`). `flags` uint16 LE. Bit 0 apagado → ritmo `byte · 0.5` y paladas uint16, cada uno solo si hay bytes (`:43-53`). Bits 1, 7 y 10 suman al `offset` **sin** verificar longitud (`:56-58`, `:97-99`, `:118-120`); después de eso un campo con `offset + k > largo` se omite sin error. Distancia uint24 (`:61-66`); split instantáneo y promedio (el promedio solo si el instantáneo quedó en 0, `:75-80`); potencia **sint16** e igual con la promedio (`:83-94`); energía total uint16 y luego kcal/h (2) y kcal/min (1) solo si entran (`:102-109`); pulso uint8 (`:112-115`); tiempo uint16 (`:123-126`); bit 12 ignorado (`:128-129`). Valores crudos (65535, potencia negativa) sin filtrar (Q-38). Un campo ausente vale 0: no hay estado entre paquetes (`:33-40`). `int offset` y comparaciones `offset + k <= raw.Length` iguales al legacy, sin excepciones con ningún paquete.
- **Pulso**: `bytes.Length == 0 → null` (`:25`); `is16 = (flags & 1) != 0`; largo `< (is16 ? 3 : 2) → null` (`:28`, el de 16 bits con 2 bytes **no** cae a 8 bits); contacto `(flags >> 1) & 3`: `2 → false`, `3 → true`, otro → `null` (`:33-38`). Sin rango (Q-48).
- **Filtro**: `ScanFilter.Accept` = `acceptScanResult` con `clientSide` explícito (el llamador pasa `IsClientSide(plataforma)`); "anuncia" = alguno de `ServiceUuids` es igual (128 bits) al buscado.
- **Simulador**: orden de llamadas al azar idéntico (primero vatios, después spm, solo si `Rowing` y `Noise`; `_jitter = (NextDouble()·2 − 1)·amplitud`, `:73-74`). `watts = Math.Max(1, (int)DartMath.Round(TargetWatts * (1 + jitter(0.05))))`; `spm = Math.Max(1.0, TargetSpm + jitter(1.0))`; `pace = (int)DartMath.Round(paceExact)`; acumula `500 / paceExact`, `spm / 60`, `CaloriesPerSecond(watts)` y `_elapsed++` (`:46-53`); objetivo de pulso `Math.Min(190.0, 90 + watts * 0.35)` o `70.0` (`:56`), `_hr += (obj − _hr) * 0.1` (`:57`). Salida: `StrokeRate = DartMath.RoundToFixed(spm, 1)` (`:60`); paladas, distancia y kcal `(int)Math.Floor(x + 1e-6)` (`:62-66`); pulso `DartMath.Round`; tiempo `_elapsed`. Sin ruido no consume `IRandom` (lo prueba un `SequenceRandom` vacío, AC-BASE-07). `Math.Pow` puede diferir en el último bit del `pow` de Dart: el split exacto se compara con `Within(1e-9)` y los enteros con el oráculo (R-T4).

### 3.3 WP-C — pasos, rutinas, sesión, estadísticas, catálogo

```csharp
namespace RowMate.Core.Models
{
    public enum StepType { Warmup, Work, Rest, Cooldown }            // interval_step.dart:1 (orden del selector)
    public static class StepTypeNames                                 // nombres persistidos (:115, :133)
    {
        public static string ToStorage(StepType type);                // "warmup" | "work" | "rest" | "cooldown"
        public static StepType Parse(string name);                    // otro → FormatException (AC-RUT-01-05, no hay default)
        public static bool TryParse(string? name, out StepType type);
    }
    public enum StepGoal { Time, Distance, None }                     // interval_step.dart:64-77
    public static class StepTypeInfo                                  // theme.dart:42-69; routine_editor_screen.dart:447-458
    {
        public static IReadOnlyList<StepType> SelectorOrder { get; }  // warmup, work, rest, cooldown
        public static string NameKey(StepType type);                  // L10nKeys.StepTypeWarmup…
        public static string IconName(StepType type);                 // "whatshot", "fitness_center", "pause_circle_outline", "ac_unit"
        public static ColorToken Color(StepType type);                // Tokens.StepWarmup… (misma instancia)
        public static ColorToken ColorForStorageName(string? name);   // "work"… ; otro, vacío o null → Tokens.StepUnknown
    }
    // legacy: interval_step.dart:21-62, :147-180
    public sealed record IntervalStep
    {
        public IntervalStep(long routineId, int order, StepType type);
        public long? Id { get; init; }
        public long RoutineId { get; init; }  public int Order { get; init; }  public StepType Type { get; init; }
        public int? DurationSeconds { get; init; }  public int? DistanceMeters { get; init; }
        public int? TargetWattsMin { get; init; }  public int? TargetWattsMax { get; init; }
        public int? TargetSpm { get; init; }  public int? TargetSplitSeconds { get; init; }
        public string? GroupId { get; init; }  public int? GroupRepeatCount { get; init; }
        public int? ProgressionSpm { get; init; }  public int? ProgressionWatts { get; init; }  public int? ProgressionSplitSeconds { get; init; }
        public bool IsTimeBased { get; }  public bool IsDistanceBased { get; }
        public StepGoal Goal { get; }                                 // duración manda aunque haya distancia
        public IntervalStep CopyWith(long? id = null, long? routineId = null, int? order = null, StepType? type = null,
            int? durationSeconds = null, int? distanceMeters = null, int? targetWattsMin = null, int? targetWattsMax = null,
            int? targetSpm = null, int? targetSplitSeconds = null, string? groupId = null, int? groupRepeatCount = null,
            int? progressionSpm = null, int? progressionWatts = null, int? progressionSplitSeconds = null); // null = conservar
    }
    // legacy: interval_step.dart:67-109; routine_editor_screen.dart:345-349
    public static class StepLabels
    {
        public const string PartSeparator = " · ";           // une vatios, spm, split, progresiones
        public const string ProgressionSeparator = ", ";
        public const string SubtitleSeparator = "  ·  ";     // subtítulo del editor
        public static string Duration(IntervalStep step, Localizer l10n);
        public static string Targets(IntervalStep step, Localizer l10n);
        public static string EditorSubtitle(IntervalStep step, Localizer l10n);
    }
    public readonly struct StepPosition : IEquatable<StepPosition>   // routine.dart:5
    {
        public StepPosition(string? groupId, int rep, int repCount);
        public string? GroupId { get; }  public int Rep { get; }  public int RepCount { get; }
        public static readonly StepPosition Loose;                    // (null, 1, 1)
    }
    // legacy: routine.dart:8-21
    public sealed record Routine
    {
        public Routine(string name, DateTimeOffset createdAt);       // Name = name ?? throw ArgumentNullException; CreatedAt = createdAt
        public long? Id { get; init; }  public string Name { get; init; }  public string Description { get; init; } = "";
        public DateTimeOffset CreatedAt { get; init; }
        public IReadOnlyList<IntervalStep> Steps { get; init; } = Array.Empty<IntervalStep>(); // plantilla sin expandir
        public IReadOnlyList<IntervalStep> FlattenedSteps();          // = RoutineExpansion.Flatten(Steps)
        public IReadOnlyList<StepPosition> FlattenedStepPositions();  // = RoutineExpansion.Positions(Steps)
    }
    public static class RoutineExpansion                              // routine.dart:29-97
    {
        public static IReadOnlyList<IntervalStep> Flatten(IReadOnlyList<IntervalStep> template);
        public static IReadOnlyList<StepPosition> Positions(IReadOnlyList<IntervalStep> template);
    }
    public readonly struct RoutineCounts { public int Total { get; } public int Work { get; } public int Rest { get; } }
    public static class RoutineSummary                                // routine.dart:100-105; workout_screen.dart:152
    {
        public static RoutineCounts Count(IReadOnlyList<IntervalStep> template);
        public static string Text(Routine routine, Localizer l10n);   // routineSummarySteps | routineSummaryWorkRest
        public static string StartDialogContent(Routine routine, Localizer l10n); // workoutStartContent{summary}
    }
    // legacy: workout_session.dart:2-37
    public sealed record WorkoutSession
    {
        public WorkoutSession(DateTimeOffset startedAt);
        public long? Id { get; init; }  public long? RoutineId { get; init; }  public string? RoutineName { get; init; }
        public DateTimeOffset StartedAt { get; init; }  public DateTimeOffset? FinishedAt { get; init; }
        public int TotalDistanceMeters { get; init; }  public int TotalTimeSeconds { get; init; }
        public int AvgPowerWatts { get; init; }  public double AvgStrokeRate { get; init; }  public int TotalCalories { get; init; }
        public string? StravaActivityId { get; init; }
        public string DurationFormatted => TimeFormats.Elapsed(TotalTimeSeconds);
        public WorkoutSession Closed(SessionStats stats, DateTimeOffset finishedAt); // workout_provider.dart:276-292 (Q-14: P99)
    }
    public sealed record WorkoutSessionRecord   // columnas de workout_sessions: el DTO de la It. 3 mapea 1 a 1
    {
        // QA-05: C# 9 no tiene `required`; la única columna obligatoria entra por el constructor.
        // StartedAt = startedAt ?? throw new ArgumentNullException(nameof(startedAt)) (legacy: `map['started_at'] as String`, :57)
        public WorkoutSessionRecord(string startedAt);
        public long? Id { get; init; } public long? RoutineId { get; init; } public string? RoutineName { get; init; }
        public string StartedAt { get; init; } public string? FinishedAt { get; init; }
        public long? TotalDistanceMeters { get; init; } public long? TotalTimeSeconds { get; init; } public long? AvgPowerWatts { get; init; }
        public double? AvgStrokeRate { get; init; } public long? TotalCalories { get; init; } public string? StravaActivityId { get; init; }
    }
    public static class WorkoutSessionCodec                           // workout_session.dart:39-67
    {
        public static WorkoutSessionRecord ToRecord(WorkoutSession session);   // fechas con IsoDate.FormatUtc
        public static WorkoutSession FromRecord(WorkoutSessionRecord record, TimeZoneInfo localZone); // ?? 0, ?? 0.0, toLocal
    }
    // legacy: workout_session.dart:140-182
    public sealed record DataPoint
    {
        public DataPoint(long sessionId, int elapsedSeconds);         // tiempo = el del monitor (obligatorio, como el legacy)
        public long? Id { get; init; }  public long SessionId { get; init; }  public int ElapsedSeconds { get; init; }
        public double StrokeRate { get; init; }  public int StrokeCount { get; init; }  public int DistanceMeters { get; init; }
        public int Pace500mSeconds { get; init; }  public int PowerWatts { get; init; }  public int Calories { get; init; }
        public int HeartRate { get; init; }  public int? StepIndex { get; init; }       // índice en la rutina EXPANDIDA
        public string? StepTypeName { get; init; }                    // "warmup"… (string como el legacy)
        public DataPoint WithStep(int flattenedIndex, StepType type);
    }
    // legacy: workout_session.dart:71-136
    public sealed record SessionStats
    {
        public static readonly SessionStats Empty;
        public int P99Watts { get; init; }  public double P99Spm { get; init; }  public int P99SplitSeconds { get; init; }
        public int TotalDistance { get; init; }  public int TotalTimeSeconds { get; init; }  public int TotalCalories { get; init; }
        public int AvgHeartRate { get; init; }  public int MaxHeartRate { get; init; }
        public bool HasHeartRate => MaxHeartRate > 0;                 // :92
        public string SplitFormatted => TimeFormats.SplitOrDashes(P99SplitSeconds);   // :94-99
        public string DurationFormatted => TimeFormats.Elapsed(TotalTimeSeconds);     // :101-109
        public static int Percentile99Index(int count);               // (int)Math.Floor((count − 1) * 0.99) (:114)
        public static double Percentile99(IReadOnlyList<double> positiveValues);      // :111-116 (vacío → 0)
        public static SessionStats Compute(IReadOnlyList<DataPoint> points);           // :118-136
    }
}
namespace RowMate.Core.Localization
{
    public enum AppLanguage { En = 0, Es = 1 }                        // orden de supportedLocales (main.dart:103-106)
    public readonly struct LanguageTag { public string Language { get; } public string? Script { get; } public string? Region { get; }
                                         public static LanguageTag Parse(string tag); }
    public static class LocaleResolver { public static AppLanguage Resolve(IReadOnlyList<string>? preferredTags); }
    public sealed class LanguageState                                // AC-APP-09-04/05: sin setter ni persistencia
    {
        public LanguageState(IReadOnlyList<string>? initialTags);
        public IReadOnlyObservable<AppLanguage> Current { get; }
        public void Refresh(IReadOnlyList<string>? tags);             // re-resuelve; notifica solo si cambia
    }
    public enum MessageArgKind { Text, Number }
    public readonly struct MessageArg
    {
        public static MessageArg Text(string name, string value);
        public static MessageArg Number(string name, long value);
        public string Name { get; }  public MessageArgKind Kind { get; }  public string? TextValue { get; }  public long NumberValue { get; }
    }
    public sealed class CompiledMessage
    {
        public IReadOnlyCollection<string> ArgumentNames { get; }
        public bool IsPluralArgument(string name);
        public string Format(IReadOnlyList<MessageArg> args);
    }
    public static class MessageFormatter { public static CompiledMessage Parse(string pattern); } // FormatException
    public sealed class PlaceholderInfo { public string Name { get; } public string Type { get; } } // "String" | "int"
    public sealed class LocalizationCatalog
    {
        public static LocalizationCatalog Parse(string json);         // formato .arb (abajo); valida mensajes y metadatos
        public string Locale { get; }
        public IReadOnlyList<string> Keys { get; }                    // en orden del archivo
        public string Message(string key);
        public IReadOnlyList<PlaceholderInfo> Placeholders(string key);
    }
    public sealed class MissingTranslationException : KeyNotFoundException { public string Key { get; } }
    public sealed class Localizer
    {
        public Localizer(LocalizationCatalog catalog);
        public string LanguageCode { get; }
        public bool Contains(string key);
        public string Get(string key);                                // clave sin parámetros
        public string Format(string key, params MessageArg[] args);   // tipos y nombres exactos o ArgumentException
    }
    public sealed class LocalizationSet
    {
        public static LocalizationSet Load(string esJson, string enJson);
        public Localizer Es { get; }  public Localizer En { get; }
        public Localizer For(AppLanguage language);
    }
    public static class L10nKeys { public const string NavDevice = "navDevice"; /* … las 174, PascalCase de la clave */ }
}
namespace RowMate.Core.Ports
{
    public interface ISystemLocale { IReadOnlyList<string> PreferredLanguageTags { get; } } // Platform en la It. 2 (QA-P-20)
}
```

Regla de compilación de los records de Core (QA-05): C# 9 no tiene `required`, y Core compila con `Nullable` y `TreatWarningsAsErrors`, así que **toda propiedad de tipo referencia no anulable** de un record se asigna en su constructor (`Name`, `StartedAt`) o tiene inicializador (`Description = ""`, `Steps = Array.Empty<…>()`); si no, `CS8618`. Las firmas de `WorkoutSessionRecord` y `Routine` de arriba, con esos cuerpos, se compilaron en el *scratchpad* con las reglas de Core (`netstandard2.1`, C# 9, `Nullable`, `TreatWarningsAsErrors`, *shim* interno): 0 advertencias.

Notas de paridad (C):
- **Etiqueta de duración** (`:67-77`): `m = TruncDiv(d, 60)`, `s = Mod(d, 60)`; `m > 0 && s > 0 → stepDurationMinSec`, `m > 0 → stepDurationMin`, si no `stepDurationSec`; sin duración y con distancia `stepDurationMeters`; ninguna → `stepDurationNone`. Negativos como Dart: −30 → `30s`, −60 → `0s`, −90 → `30s`.
- **Etiqueta de objetivos** (`:79-109`): vatios rango si hay mín. **y** máx.; solo mín. → `>{min}W`; solo máx. → nada; spm; split con `TimeFormats.MinSec` sin piso (0 → `00:00`, −65 → `-1:55`); progresiones `≠ 0` en orden SPM, W, split, `delta = (p > 0 ? "+" : "") + Fmt.Integer(p)`, unidas por `", "` dentro de `stepTargetProgressions` (`📈 {list}`); todo unido por `" · "`. Se muestra la progresión aunque falte su objetivo.
- **Expansión** (`:29-70`): bloques de pasos **consecutivos** con el mismo `GroupId`; `repeat = GroupRepeatCount ?? 1` del **primer** paso del bloque; `for r in [0; repeat)` (≤ 0 → ningún paso); progresión solo si el objetivo **y** su progresión existen: `objetivo + prog · r` (sin piso); el resto se conserva (`CopyWith`). Pasos sueltos no se progresan. **Posiciones** (`:74-97`): mismo recorrido; `(gid, r, repeat)` para `r` desde 1; suelto `(null, 1, 1)`.
- **Resumen** (`:100-105`): cuenta pasos de la **plantilla**; `Work == 0 → routineSummarySteps(count = Total)`; si no `routineSummaryWorkRest(work, rest)`. El texto viejo (`'1 pasos'`) no se muestra: el test reconstruye la cadena legacy solo para probar que los números son los mismos (AC-RUT-05-04).
- **P99** (`:111-129`): filtra `> 0`, ordena ascendente (`List<double>.Sort`), índice `⌊(n − 1)·0.99⌋` en `double`; vatios y split → `(int)DartMath.Round(p99)`; spm sin redondear. Totales = máximo de cada columna con ceros (`:130-132`); pulso promedio `DartMath.Round((double)suma / cantidad)` con suma `long` (`:133`), máximo (`:134`).
- **Cierre** (`workout_provider.dart:276-292`): `Closed(stats, fin)` copia `TotalDistance`, `TotalTimeSeconds`, `P99Watts → AvgPowerWatts`, `P99Spm → AvgStrokeRate`, `TotalCalories` y pone `FinishedAt`; no copia `StravaActivityId`.
- **Fechas** (`:43-44`, `:57-60`): `ToRecord` → `IsoDate.FormatUtc`; `FromRecord` → `IsoDate.Parse(texto, localZone)`; el test usa una zona fija UTC−3 (`TimeZoneInfo.CreateCustomTimeZone`).
- **Idioma** (`basicLocaleListResolution`, Flutter 3.47.6 `widgets/app.dart:146-238`, con `supportedLocales = [en, es]` sin región ni escritura):
  1. Lista nula o vacía → `En`.
  2. `LanguageTag.Parse`: separa por `-` o `_`; idioma en minúsculas; subetiqueta de 4 letras = escritura; 2 letras o 3 dígitos = región; corta en un *singleton* (`u`, `x`…).
  3. Recorrer en orden con `match = null`: si la entrada no tiene escritura ni región y su idioma es `es`/`en` → devolverlo (coincidencia perfecta). Si `match != null` → devolver `match`. Si el idioma es `es`/`en`: `match = idioma`; si es la **primera** entrada y la siguiente no tiene el mismo idioma → devolverlo.
  4. Al terminar: `match ?? En`. (Las coincidencias por idioma+escritura, idioma+región y solo región de Flutter nunca dan resultado con este `supportedLocales`; el código lo explica en un comentario.) Da las 17 filas de AC-APP-09-02, incluidas las dos †.
- **`MessageFormatter`** (ICU mínimo, sin escape con apóstrofos, como `gen-l10n` sin `use-escaping`: `won't` es literal): `{nombre}` y `{nombre, plural, =N{…} one{…} other{…}}` (`other` obligatorio; `=N` exacto primero, `one` si `n == 1`, si no `other`); `#` dentro de un caso de plural = el número; argumentos anidados dentro de los casos; **varios plurales en un mensaje** (no hace falta partir `routineSummaryWorkRest`). Números con `Fmt.Integer`. Sintaxis inválida, `}` suelto, selector desconocido → `FormatException` al **cargar** el catálogo.
- **Catálogo** (`strings.<lang>.json`): mismo formato que los `.arb`: `"@@locale"`, `"clave": "texto"`, `"@clave": { "placeholders": { "p": { "type": "String" | "int" } } }`. UTF-8 sin BOM, LF, 2 espacios, claves en el orden del legacy y las 15 nuevas al final. Las claves con parámetros tienen metadatos; ninguna otra. `LocalizationCatalog.Parse` falla si un mensaje usa un argumento sin metadato o al revés, si un plural no es `int`, o si hay claves repetidas.
- **Tipos de los parámetros de las 15 claves nuevas** (QA-07; la spec §4.17.2 da solo los nombres). Regla: `int` si el valor sale de un campo entero del legacy y se escribe con `$x` (gen-l10n lo formatea con `toString()`, acá `Fmt.Integer`); `String` si el legacy arma el texto (relleno con ceros, signo `+`, unión de partes). Mismo tipo en es y en; van en `it01-changes.json` y en los metadatos `@clave` de los dos catálogos:

  | Clave | Parámetros (tipo) | Origen en el legacy (`interval_step.dart`, `routine.dart`) |
  |---|---|---|
  | `routineSummarySteps` | `count`: `int` (plural) | `steps.length` (`routine.dart:103`) |
  | `routineSummaryWorkRest` | `work`: `int` (plural), `rest`: `int` (plural) | conteos `where(...).length` (`routine.dart:101-102`) |
  | `stepDurationMinSec` · `stepDurationMin` · `stepDurationSec` | `m`: `int`, `s`: `int` (cada clave, los suyos) | `durationSeconds! ~/ 60` y `% 60` (`:69-70`); `m` puede ser negativo (−90 → `m = −1`, cae en `stepDurationSec`) |
  | `stepDurationMeters` | `meters`: `int` | `int? distanceMeters` (`:75`) |
  | `stepDurationNone` | — | `'?'` (`:76`) |
  | `stepTargetWattsRange` · `stepTargetWattsMin` | `min`: `int`, `max`: `int` / `min`: `int` | `int? targetWattsMin/Max` (`:82`, `:84`) |
  | `stepTargetSpm` | `spm`: `int` | `int? targetSpm` (`:86`) |
  | `stepTargetSplit` | `split`: `String` (`TimeFormats.MinSec`, `125 → "02:05"`, `−65 → "-1:55"`) | `padLeft(2, '0')` de `~/` y `%` (`:87-91`) |
  | `stepTargetProgressions` | `list`: `String` (partes unidas por `", "`) | `progressions.join(', ')` (`:105`) |
  | `stepProgressionSpm` · `stepProgressionWatts` · `stepProgressionSplit` | `delta`: `String` (`"+2"`, `"-1"`) | `'${p > 0 ? '+' : ''}$p'` (`:96`, `:99`, `:102`) |

  Con las 7 del legacy (`name`, `error`, `summary` `String`; `count` `int`), quedan **21 claves con parámetros** de 174. `CatalogParityTests.SameParametersNamesAndTypes` (AC-APP-10-04) y `CatalogParityTests.FifteenNewKeys` (AC-APP-10-07) verifican nombre **y** tipo contra esta tabla (copiada como datos del test en `CatalogParityTests`); `Localizer.Format` con un `MessageArg` de otro tipo da `ArgumentException`.
- **Conversión única** (`tools/l10n/arb_to_catalog.dart`, Dart sin importar código del legacy): lee `lib/l10n/app_{es,en}.arb` (solo lectura), aplica `tools/l10n/it01-changes.json` (las 4 claves de §4.17.1 y las 15 de §4.17.2 de la spec, con sus metadatos) y escribe los dos catálogos. Después se mantienen a mano (ADR 011: cada iteración agrega sus claves en los dos JSON). La verificación no depende del script: la hacen los tests contra `l10n_legacy.json`.

---

## 4. Fuente única de colores y tokens (QA-P-05, ADR 016)

### 4.1 Mecanismo

```
Assets/RowMate/Core/Design/Tokens.cs   (FUENTE ÚNICA, C# puro; la llena el Designer)
        │  reflexión solo en herramientas y tests (TokenCatalog)
        ├──► tools/tokens (RowMate.Tokens, net8.0) ──► Assets/RowMate/Presentation/Styles/Tokens.uss  (GENERADO, versionado)
        ├──► TokensUssSyncTests: regenera en memoria y compara con el archivo (falla si difieren)
        ├──► TokensContractTests: hex de contrato del legacy (AC-BASE-04)
        ├──► ContrastTests: umbrales WCAG contra el peor fondo del vidrio (AC-BASE-05)
        └──► consumidores en código: StepTypeInfo (It. 1), escena, gráficos, TabularNumber (It. 2+) usan Tokens.X directo
```

- **Una definición por color**: el hex existe solo en `Tokens.cs`. `PureCodeRulesTests` falla si aparece un literal de color (`"#RRGGBB"`, `"#RRGGBBAA"`) en cualquier `.cs` de Core, App, Simulator o Presentation fuera de `Tokens.cs`, o un hex en un `.uss` distinto de `Tokens.uss`.
- **Sin carga en tiempo de ejecución** ni nombres sin chequear: los consumidores usan campos (`Tokens.StepWork`), el compilador los verifica. La enumeración por reflexión (`TokenCatalog`) la usan solo el generador y los tests.
- Cambiar un color = editar `Tokens.cs` y correr `"C:/Program Files/dotnet/dotnet.exe" run --project tools/dotnet/RowMate.Tokens --artifacts-path tools/dotnet/.artifacts` (o ver el fallo de `run-tests.ps1`, que dice qué regenerar).

### 4.2 API que usa `Tokens.cs` (WP-A la implementa con estas firmas exactas)

```csharp
namespace RowMate.Core.Design
{
    public readonly struct Rgb : IEquatable<Rgb>
    {
        public Rgb(byte r, byte g, byte b);
        public byte R { get; } public byte G { get; } public byte B { get; }
        public static Rgb Parse(string hex);                 // "#RRGGBB" (mayúsculas o minúsculas)
        public string ToHex();                               // "#RRGGBB" en mayúsculas
    }
    public enum TokenKind { Color, Length, Duration, Easing, TypeScale }
    public interface IDesignToken { string Name { get; } TokenKind Kind { get; } }
    public enum ColorRole
    {
        None,          // sin chequeo de contraste
        Surface,       // fondos y vidrios
        Graphic,       // rellenos, bordes, acentos, colores base de métricas/pasos (≥ 3:1 si el Designer lo pide)
        UiText,        // texto de interfaz sobre surface.card: ≥ 4,5:1
        UiTextPrimary, // texto primario sobre surface.card: ≥ 7:1
        HudValue,      // valor del HUD sobre el peor vidrio: ≥ 7:1
        HudValueRed,   // familia roja del HUD (D-ART-01): ≥ 4,5:1
        HudText        // texto chico del HUD (barra de pasos, chips): ≥ 4,5:1
    }
    public sealed class ColorToken : IDesignToken
    {
        public static ColorToken Contract(string name, string hex, ColorRole role = ColorRole.Graphic); // [CONTRATO] legacy, opaco
        public static ColorToken Hex(string name, string hex, ColorRole role = ColorRole.None);         // opaco
        public static ColorToken HexAlpha(string name, string hex, int alphaPercent, ColorRole role = ColorRole.None); // "@86 %"
        public string Name { get; }  public TokenKind Kind { get; }   // Color
        public Rgb Rgb { get; }
        public double Alpha { get; }                                  // alphaPercent / 100.0, SIN redondear a byte
        public ColorRole Role { get; }
        public bool IsContract { get; }
    }
    public sealed class LengthToken : IDesignToken { public static LengthToken Dp(string name, double dp); public double Value { get; } }
    public sealed class DurationToken : IDesignToken { public static DurationToken Ms(string name, int milliseconds); public int Milliseconds { get; } }
    public sealed class EasingToken : IDesignToken
    {
        public static EasingToken CubicBezier(string name, double x1, double y1, double x2, double y2, string? ussKeyword = null);
        public double X1 { get; } public double Y1 { get; } public double X2 { get; } public double Y2 { get; }
        public string? UssKeyword { get; }   // USS no tiene cubic-bezier: solo se emite si hay palabra clave (ease-out-cubic…)
    }
    public enum FontFamilyRole { Ui, Numeric, Mono }                  // Inter, Barlow Semi Condensed, JetBrains Mono (ART_BIBLE §4.1)
    public sealed class TypeScaleToken : IDesignToken
    {
        public static TypeScaleToken Of(string name, FontFamilyRole family, double sizeDp, double lineHeightDp, int weight);
        public FontFamilyRole Family { get; } public double SizeDp { get; } public double LineHeightDp { get; } public int Weight { get; }
    }
    public static class TokenCatalog                                  // solo herramientas y tests
    {
        public static IReadOnlyList<IDesignToken> All { get; }        // campos public static readonly de Tokens, orden ordinal por Name
        public static IDesignToken Get(string name);
        public static ColorToken Color(string name);
        public static string FieldNameFor(string tokenName);          // "metric.split.hud" → "MetricSplitHud"
    }
    public static class UssTokenWriter
    {
        public static string Write(IReadOnlyList<IDesignToken> tokens); // texto completo de Tokens.uss (LF, salto final)
        public static string VariableName(string tokenName);           // "text.onAccent" → "--rm-text-on-accent"
    }
    public static class Contrast
    {
        public static double RelativeLuminance(double r, double g, double b); // canales 0-255 en double; sRGB → lineal con 0,04045
        public static double Ratio(double luminanceA, double luminanceB);    // (L1 + 0,05) / (L2 + 0,05), L1 ≥ L2, sin redondear
        public static (double R, double G, double B) CompositeSrgb(ColorToken over, (double R, double G, double B) under); // α·fg + (1−α)·bg, sin redondear
        public static (double R, double G, double B) CompositeLinear(ColorToken over, (double R, double G, double B) under); // informativo (R-T1)
        public static (double R, double G, double B) WorstGlassBackground();  // Tokens.GlassFallback sobre blanco
    }
}
```

### 4.3 Ruta y formato del archivo que llena el Designer

**Ruta:** `Assets/RowMate/Core/Design/Tokens.cs`.

**Formato** (C# 9; solo declaraciones, sin lógica):

```csharp
// FUENTE ÚNICA de tokens de diseño (ADR 016, QA-P-05). La mantiene el Designer.
// Después de editar: correr tools/tokens para regenerar Presentation/Styles/Tokens.uss.
// Reglas: un campo por token; nombre del campo = TokenCatalog.FieldNameFor(nombre); nombres de ART_BIBLE §2 / ANIMATION_BIBLE §1.
namespace RowMate.Core.Design
{
    public static class Tokens
    {
        // ── 2.1 Fondos, superficies, texto y bordes ───────────────────────────────
        public static readonly ColorToken BgApp = ColorToken.Contract("bg.app", "#0D1B2A", ColorRole.Surface);       // legacy theme.dart:13
        public static readonly ColorToken SurfaceCard = ColorToken.Contract("surface.card", "#1A2E45", ColorRole.Surface); // theme.dart:15
        public static readonly ColorToken TextSecondary = ColorToken.HexAlpha("text.secondary", "#FFFFFF", 70, ColorRole.UiText);
        // …
        // ── 2.3 Variantes del HUD ──────────────────────────────────────────────────
        public static readonly ColorToken MetricSplitHud = ColorToken.Hex("metric.split.hud", "#FEA845", ColorRole.HudValue);
        // …
        public static readonly ColorToken GlassFallback = ColorToken.HexAlpha("glass.fallback", "#070D16", 86, ColorRole.Surface);
        // ── 2.4 Radios y espaciado ────────────────────────────────────────────────
        public static readonly LengthToken RGlass = LengthToken.Dp("r.glass", 14);
        // ── ANIMATION_BIBLE §1 ─────────────────────────────────────────────────────
        public static readonly DurationToken DurBase = DurationToken.Ms("dur.base", 240);
        public static readonly EasingToken EaseStandard = EasingToken.CubicBezier("ease.standard", 0.2, 0.0, 0.0, 1.0, ussKeyword: "ease-out-cubic");
        // ── 4.3 Tipografía ─────────────────────────────────────────────────────────
        public static readonly TypeScaleToken NumHero = TypeScaleToken.Of("num.hero", FontFamilyRole.Numeric, 64, 64, 800);
    }
}
```

Reglas que hace cumplir `TokenCatalogTests`:
- Nombre del token: `^[a-z][a-zA-Z0-9]*(\.[a-z][a-zA-Z0-9]*)*$`, único.
- Nombre del campo = PascalCase por segmento (`metric.split.hud` → `MetricSplitHud`, `text.onAccent` → `TextOnAccent`, `r.glass` → `RGlass`, `step.unknown.hud` → `StepUnknownHud`).
- Alfas enteras en `[0; 100]`; hex válidos; `IsContract` solo en los colores de la tabla de contrato de §4.4.
- Los ejemplos de valores (`dur.base` 240, `ease.standard`) son ilustrativos: los números de motion, espaciado y tipografía los fija el Designer con ANIMATION_BIBLE §1 y ART_BIBLE §2.4 y §4.3.

### 4.4 Tokens obligatorios (nombre de campo fijo; los usan tests y código de la It. 1)

| Campo | Token | Valor | Rol | Contrato (AC-BASE-04) |
|---|---|---|---|---|
| `BgApp` | `bg.app` | `#0D1B2A` | Surface | sí (`theme.dart:13`) |
| `SurfaceCard` | `surface.card` | `#1A2E45` | Surface | sí (`:15`) |
| `BrandSeed` | `brand.seed` | `#0077B6` | Graphic | sí (`:5`) |
| `TextPrimary` | `text.primary` | `#FFFFFF` | UiTextPrimary | sí (texto de la barra superior, `:20-21`) |
| `TextSecondary` / `TextTertiary` | `text.secondary` / `text.tertiary` | `#FFFFFF @70` / `@54` | UiText | no |
| `MetricSplit` · `MetricSpm` · `MetricWatts` · `MetricDistance` · `MetricHr` · `MetricTime` · `MetricKcal` | `metric.*` | `#FF9F1C` · `#00B4D8` · `#FFD60A` · `#4CC9F0` · `#EF476F` · `#E0E0E0` · `#06D6A0` | Graphic | sí (`:33-39`) |
| `StepWork` · `StepRest` · `StepWarmup` · `StepCooldown` · `StepUnknown` | `step.*` | `#EF476F` · `#06D6A0` · `#FFD166` · `#118AB2` · `#607D8B` | Graphic | sí (`:56-68`) |
| `PhaseRunning` · `PhasePaused` · `PhaseFinished` | `phase.*` | `#06D6A0` · `#FFD166` · `#EF476F` | Graphic | sí (`immersive_workout_screen.dart:531-533`) |
| `StateOffTarget` · `BrandStrava` | `state.offTarget` · `brand.strava` | `#FF4444` · `#FC4C02` | Graphic | sí (`:581-582`; `history_screen.dart:99`) |
| `MetricSplitHud` · `MetricSpmHud` · `MetricWattsHud` · `MetricDistanceHud` · `MetricTimeHud` · `MetricKcalHud` · `TextHudPrimary` | `*.hud`, `text.hudPrimary` | `#FEA845` · `#38CBEF` · `#FFD60A` · `#4CC9F0` · `#E0E0E0` · `#06D6A0` · `#FFFFFF` | HudValue | no (ART_BIBLE §2.3) |
| `MetricHrHud` · `StateOffTargetHud` | `metric.hr.hud` · `state.offTarget.hud` | `#FF5A7E` · `#FF5F59` | HudValueRed | no |
| `StepWorkHud` · `StepRestHud` · `StepWarmupHud` · `StepCooldownHud` · `StepUnknownHud` | `step.*.hud` | `#FF5A7E` · `#06D6A0` · `#FFD166` · `#35A0C9` · `#7C9AA8` | HudText | no |
| `GlassFallback` | `glass.fallback` | `#070D16 @86` | Surface | no (ART_BIBLE §3.2) |

`TokensContractTests` fija los 21 hex de contrato por nombre; `ContrastTests` exige que los tokens de las filas HUD tengan **al menos** esos roles (el Designer no puede bajarlos) y que `WorstGlassBackground()` redondeado sea `#2A2F37`. Los colores de la barra inferior del legacy (`#12243A`, `#00B4D8`, `#607D8B`, `theme.dart:24-28`) no se migran como tokens visibles (DC-58).

**Método de contraste (respuesta a P-05):** composición en sRGB con el alfa **sin redondear** (0,86) y canales en `double` (sin pasar por bytes), luminancia WCAG con umbral 0,04045, relación sin redondear y comparación directa con el umbral. Con ese método (recalculado acá): `metric.split.hud` 7,004, `metric.distance.hud` 7,016, `metric.spm.hud` 7,040, blanco 13,49, `metric.hr.hud` 4,508, `state.offTarget.hud` 4,519, `step.cooldown.hud` 4,512; `text.secondary` sobre card 7,56 y `text.tertiary` 5,14. Todos pasan sin cambiar umbrales (R-10). El test imprime la tabla con los dos métodos (y el lineal de R-T1) para que QA y el Designer vean el margen.

**Pares que pide el Designer (`ContrastTests.DesignerPairs`, QA-08).** Además de los roles, `ContrastTests` verifica los 13 grupos de pares de [04-design-notes §4.3](04-design-notes.md) (G1-G3 con umbral 3 y T1-T10 con 4,5), uno por caso (`TestCaseData` con nombre `G1 metric.split / surface.card`, etc., categoría `AC-BASE-05`). La lista es **dato del test** (frente, fondo, composición del fondo, umbral), escrita en `ContrastTests.cs` con nombres de token (`TokenCatalog.Color("…")`), nunca hex: no va en `Tokens.cs`, que solo declara tokens. Composición: un fondo con alfa se compone primero sobre su base (`state.connectedBg` sobre `surface.card`, `dev.bleDebugBg` sobre `bg.app`, `surface.nav` sobre blanco, el vidrio con `WorstGlassBackground()`), y un frente con alfa (`text.secondary`, `text.tertiary`) se compone sobre ese fondo ya compuesto, con el método de arriba. Valores de control (recalculados para este diseño; coinciden con el Designer y con QA): mínimo de G1 `step.unknown`/card **3,159**; T8 mínimo `state.dangerText`/`surface.raised` **4,639**; `text.tertiary`/card **5,140**; T9 mínimo `metric.spm`/peor vidrio **5,474**; T10 `state.dangerText`/peor vidrio **5,077**. Las excepciones de 04-design-notes §4.4 (`text.disabled`, blanco sobre `brand.strava`, `border.strong`, rellenos sobre card) no se testean como umbral; el test las imprime como informativas.

### 4.5 `Tokens.uss` generado

```
/* GENERADO por tools/tokens desde Assets/RowMate/Core/Design/Tokens.cs. NO EDITAR A MANO (ADR 016). */
:root {
    --rm-bg-app: #0D1B2A;
    --rm-glass-fallback: rgba(7, 13, 22, 0.86);
    --rm-r-glass: 14px;
    --rm-dur-base: 240ms;
    --rm-ease-standard: ease-out-cubic;
    --rm-num-hero-size: 64px;
    …
}
```
Orden ordinal por nombre; colores opacos `#RRGGBB`, con alfa `rgba(R, G, B, α)` (α con `Fmt.DartToString`); longitudes y tamaños en `px` (1 px = 1 dp a 160 DPI, ART_BIBLE §2.4); duraciones en `ms`; *easing* solo con palabra clave de USS; tipografía solo el tamaño (USS no tiene `line-height`). `tools/tokens/Program.cs`: sin argumentos escribe el archivo (LF, UTF-8 sin BOM); `--check` sale con 1 si difiere. El test compara normalizando `CRLF → LF`.

---

## 5. Asset slots en la It. 1 (QA-P-15)

Lo único que la It. 1 escribe del modelo unificado (ARCHITECTURE §14.4, ASSET_SLOTS §0):

1. **El árbol**: este diseño crea solo las carpetas con archivos (§1.1); las rutas de slots (`Assets/RowMate/Slots/`), sus clases (`Assets/RowMate/Scene/`), arte (`Art/`), UI (`UI/Fonts`, `UI/Icons`, `UI/Brand`) y terceros (`Assets/ThirdParty/`) quedan fijadas por ARCHITECTURE §2 y las crea la iteración que las usa.
2. **El assembly `RowMate.Scene`** (asmdef de §1.2), donde vivirán `AssetSlotDefinition`, `AssetSlot<T>` y los subtipos.
3. **La protección del repositorio**: el `.gitignore` actual (terceros solo en `Assets/ThirdParty/`, versionados solo sus `.meta`) más `RepoHygieneTests` (§7.2), que lo verifica con `git check-ignore` sobre rutas de ejemplo y con `git ls-files`, y `PureCodeRulesTests`, que falla si hay un `.fbx` fuera de `Assets/ThirdParty/`.

No se escriben en la It. 1: `SlotIds` (Core/Scene), las clases de slot ni ninguna instancia `.asset`; llegan en la It. 6a con el primer slot (D-09).

---

## 6. Oráculo Dart (QA-P-23)

### 6.1 Cómo corre sin tocar el legacy

- `tools/oracle/run-oracle.ps1 [-LegacyPath <ruta>] [-DartExe <ruta>] [-Only <nombre>[,…]] [-L10n]`. Por defecto `-LegacyPath` = `<repo>/../rowmate-legacy` y `-DartExe` = `<flutter-sdk>/bin/cache/dart-sdk/bin/dart.exe` (o `dart` del `PATH`).
- Pasos:
  1. **Guardia R-02**: si algún `tools/oracle/**/*.dart` o `tools/l10n/*.dart` contiene `strava` (sin distinguir mayúsculas), aborta.
  2. **Guardia del legacy, antes** (QA-14). Todo `git` contra el legacy va con `--no-optional-locks` (así `status` no refresca `.git/index`). (a) `git --no-optional-locks -C <legacy> status --porcelain` tiene que estar vacío; si no, aborta. (b) Guarda la salida de `git --no-optional-locks -C <legacy> status --porcelain --ignored --untracked-files=all`, que lista **uno por uno** los archivos ignorados (`.dart_tool/`, `build/`, `*/ephemeral/`, `lib/core/strava/strava_secrets.dart`…; hoy 591 líneas), que `--porcelain` solo no ve. (c) Guarda una instantánea de **metadatos** (ruta relativa, tamaño, `LastWriteTimeUtc`) de todos los archivos del legacy fuera de `.git/` (hoy 863), con `Get-ChildItem -Recurse -File -Force`: lee solo la entrada del directorio, **no abre ningún archivo** (tampoco `strava_secrets.dart`, que entra solo por nombre, tamaño y fecha), y detecta escrituras dentro de carpetas ignoradas que (b) no distingue. Toma `legacyCommit = git --no-optional-locks -C <legacy> rev-parse HEAD`.
  3. Escribe `tools/oracle/.dart_tool/oracle_package_config.json` (ignorado por git): `{"configVersion": 2, "packages": [{"name": "rowmate", "rootUri": "file:///<legacy>/", "packageUri": "lib/", "languageVersion": "3.3"}]}` (`3.3` = `environment.sdk` del `pubspec.yaml` legacy).
  4. Por cada `tools/oracle/bin/*.dart` (o los de `-Only`): `& $DartExe --packages=<config> <script> --out <repo>/Assets/RowMate/Tests/Golden/oracle --legacy-commit <sha>`. Los scripts importan **solo** `dart:` y `package:rowmate/core/models/*`, `core/bluetooth/ftms_parser.dart`, `core/bluetooth/heart_rate_parser.dart` y `core/dev/rowing_simulator.dart` (archivos que solo importan `dart:`), más `../lib/golden.dart`.
  5. **Guardia del legacy, después** (QA-14): repite (a), (b) y (c) y sale con 1 si `--porcelain` no está vacío o si (b) o (c) difieren de lo guardado, imprimiendo las líneas que cambiaron. La guardia corre también si un script falla (`try/finally`). No se vigila `.git/`: un editor abierto sobre el legacy puede refrescar `.git/index` por su cuenta y daría falsas alarmas; lo que importa es el árbol de trabajo.
- `-L10n` corre `tools/l10n/arb_to_catalog.dart` (la conversión única de §3.3, que lee `lib/l10n/app_{es,en}.arb`) dentro de la **misma** guardia, para que ningún acceso al legacy quede sin control.
- No hay `pubspec.yaml` ni `pub get`: nada se descarga y nada se escribe fuera de `tools/oracle/.dart_tool/` y de la carpeta de salida. Probado en esta máquina con Dart 3.13.5 (§0 del encabezado). `run-oracle.ps1` también es **solo ASCII** (§2.6).

### 6.2 Formato común de los JSON dorados

```json
{
  "header": {
    "schema": 1,
    "generator": "tools/oracle/bin/ftms_vectors.dart",
    "dartVersion": "3.13.5 (stable) (Tue Sep 29 01:00:52 2026 -0700) on \"windows_x64\"",
    "legacyCommit": "3ae0f3a8bed5966700e830db3d8a5f050f34a0bc",
    "generatedAt": "2026-10-27T12:00:00.000Z"
  },
  "<sección chica>": [ { … }, … ],
  "<sección tabla>": {
    "columns": ["x", "n", "out"],
    "rows": [
["4038400000000000",1,"24.3"],
["4038400000000000",2,"24.25"],
…
    ]
  }
}
```
- **Dos formas de sección** (QA-06). *Chica* (casos con nombre, tablas de la spec, ≤ 200 entradas): arreglo de objetos, legible, con `f64(x)` → `{"v": <toString() o "NaN"/"Infinity">, "bits": "<16 hex>"}`. *Tabla* (barridos, aleatorios, fuzz: toda sección de más de 200 entradas): `{"columns": [...], "rows": [...]}`, **una fila por línea y sin sangría**, celdas escalares; un `double` va solo como `"<16 hex>"` (`f64bits(x)`), un entero como número JSON y un texto como cadena. Los casos con listas (por ejemplo las sesiones aleatorias de `session.json`) se aplanan con una columna `case`. En C#, `GoldenFile.Table` / `GoldenVectors.Rows` (§2.5).
- `lib/golden.dart` (WP-A): `header(generator, legacyCommit)`, `f64(x)`, `f64bits(x)` (`ByteData.setFloat64`), `bytesHex(List<int>)` → `"2C 00 30"`, `table(columns, rows)`, `writeGolden(nombre, cuerpo, args)`: escritor propio que indenta con 2 espacios los objetos y los arreglos de objetos, y escribe cada fila de una tabla con `jsonEncode` en su línea; LF, salto final, UTF-8 sin BOM.
- Todo `double` de entrada o de salida se lee en C# por sus bits (`GoldenJsonExtensions.AsF64`, `GoldenRow.F64`); los enteros van como números JSON (`JsonNode.RawNumber` los conserva exactos).
- **Tope: 1 MiB (1 048 576 bytes) por archivo** (R-T11), verificado por `GoldenCoverageTests.FilesWithinSizeCap`. Cada WP dimensiona sus barridos para entrar con margen; si un archivo no entra, se achica el barrido (la paridad masiva la cubren las propiedades internas de §3.1, punto 3), no se sube el tope.

### 6.3 Qué vectores captura cada archivo

| Archivo (WP) | Script | Secciones |
|---|---|---|
| `fmt.json` (A) | `fmt_vectors.dart` | (QA-06: barridos achicados y formato de tabla de §6.2; QA-04: potencias de 2 y empates.) `toStringAsFixed` (tabla `[x, n, out]`): tablas de AC-BASE-01, QA §3.2 y ADR 012 §5 con `n ∈ {0…3}`; `k/100` con `k ∈ [−1000; 1000]` y `n ∈ {0, 1}` (empates `x.5` y casi empates `x.x5`); `k/1000` con `k ∈ [−1000; 1000]` y `n = 2` (casi empates `x.xx5`, `1.005 → "1.00"`); `k/20` con `k ∈ [200; 1200]` y `n = 1` (spm de 10 a 60 en pasos de 0,05); 500 valores de `Random(1234)` en `[−500; 500)` con `n ∈ {0…3}`; casos especiales (`-0.0`, `5e-324`, `1e20`, `1e21`, `NaN`, `±∞`). `round` (tabla `[x, out]`): tabla de AC-BASE-01, ADR §5, `k/2` y `k·0.05` con `k ∈ [−200; 200]`, los 500 aleatorios. `parseFixed` (tabla `[x, n, out]`, `out` por bits): `double.parse(x.toStringAsFixed(n))` sobre las tablas, `k/20` y los 500 aleatorios. `toString` (tabla `[x, out]`): ADR §5 + 500 magnitudes `10^[−9; 22]` + 500 patrones de bits aleatorios finitos. `toStringPow2`: las **2 098** potencias `2^k`, `k ∈ [−1074; 1023]`. `toStringTies`: **500** empates exactos `b + 0.25` y `b + 0.75` con `b` entero de `Random(…)` en `[2^50; 2^51)` (todos empatan en la cifra 17; verificado). `mod`/`modInt`/`truncDiv`: enteros y `double` con signos mezclados. `iso8601`: `DateTime.utc(…)` con y sin milisegundos y microsegundos (salida y `microsecondsSinceEpoch`). `isoParse`: cadenas con `Z`, offsets, 0-9 decimales → instante UTC. **Tamaño medido** con un generador prototipo de estas secciones en el formato de §6.2: **633 404 bytes** (≈ 0,60 MiB; `toStringAsFixed` 9 060 filas, 279 KB). Antes: 3,10 MB + 4,49 MB (QA) |
| `rowing_data.json` (B) | `rowing_data_vectors.dart` | `pace500mFormatted` para la tabla de AC-BLE-01-02 y el barrido `[−10; 7000]`; `elapsedFormatted` para AC-BLE-01-03 y `{0…4000} ∪ {3599…3700} ∪ {35990…36010, 86400}`; textos de la grilla: `toStringAsFixed(1)` de spm (`23.5`, `24.25`, `24.0`, barrido `k/20`), `toString()` de vatios, `'${m}m'` |
| `ftms.json` (B) | `ftms_vectors.dart` | Los 8 paquetes de AC-BLE-02-02 a -09 y todos los de -10 a -16 (salida: los 8 campos o `null`); 1 000 paquetes aleatorios de `Random(2026)` (banderas de 13 bits al azar, largo 0-24) para fuzz de paridad |
| `heart_rate.json` (B) | `heart_rate_vectors.dart` | Los paquetes de AC-HRM-01 (`bpm`, `contact` o `null`) + 500 aleatorios |
| `simulator.json` (B) | `simulator_vectors.dart` | `paceForWatts`/`caloriesPerSecond` para `W ∈ [30; 500]` (bits); sin ruido a 150 W/24 spm, los 600 ticks (8 campos + `heartRate` expuesto); escenario de AC-BLE-14-06 (11 ticks, parar, 1 tick, reanudar, 1 tick); sin ruido a 30/14, 203/30 y 500/40 (120 ticks); **reproducción del ruido**: una clase `SeqRandom implements Random` devuelve una secuencia fija de `nextDouble()` (guardada por bits) y se registran 200 ticks con ruido a 200 W/24 spm; en C#, `SequenceRandom` con la misma secuencia tiene que dar los mismos ticks (fórmulas y orden de llamadas, AC-BLE-14-09) |
| `routines.json` (C) | `routine_vectors.dart` | Plantillas (pasos como `IntervalStep.toMap()`): T1, casos 1-8 de RUT §5.3, AC-RUT-06-10 a -13, y las de AC-RUT-05-02. Para cada una: `flattenedSteps` (todos los campos), `flattenedStepPositions`, `summary` legacy y los conteos `total`/`work`/`rest`. `durationLabel`/`targetLabel` de los casos de AC-RUT-03 y -04 (incluidos negativos y progresiones) |
| `session.json` (C) | `session_vectors.dart` | Índice P99 para `n ∈ [1; 1100]`; `SessionStats.compute` para los casos de AC-RUT-18 y 50 listas aleatorias de `Random(99)` de hasta 200 puntos (vatios, spm por bits, split, pulso, tiempo, metros, kcal; tabla aplanada con columna `case`); `durationFormatted`/`splitFormatted`; `WorkoutSession.toMap()` con fechas `DateTime.utc` (AC-RUT-16-04); `fromMap` con solo `started_at` (campos no fecha) |
| `l10n_legacy.json` (C) | `l10n_vectors.dart` | Instantánea de `app_es.arb` y `app_en.arb` (leídos con `dart:io` + `jsonDecode`): clave → texto y clave → parámetros con tipo, en orden. Es la referencia "carácter por carácter" de AC-APP-10-02/-04/-08 en el CI, que no tiene el legacy |

`GoldenCoverageTests` (categoría `Integration`) exige que existan los 8 archivos, con encabezado válido y el mismo `legacyCommit`, y que cada sección de la tabla tenga al menos un caso (AC-BASE-03). Además (QA-04, QA-06): `FilesWithinSizeCap` (cada archivo ≤ 1 048 576 bytes); toda sección de más de 200 entradas en formato de tabla; en `fmt.json`, `toStringPow2` con exactamente las 2 098 potencias de 2 (por bits: `f == 0`, todos los `be`, más los 52 subnormales `f = 2^j`) y `toStringTies` con al menos 500 filas.

---

## 7. CI (`.github/workflows/ci-core.yml`)

### 7.1 Workflow

```yaml
name: ci-core

on:
  push:
  pull_request:
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: ci-core-${{ github.ref }}
  cancel-in-progress: true

jobs:
  core:
    name: Core (${{ matrix.os }})
    runs-on: ${{ matrix.os }}
    timeout-minutes: 20
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest]
    env:
      ROWMATE_CI: "1"
      DOTNET_NOLOGO: "1"
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          lfs: false

      - name: .NET SDK (global.json)
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Tests del Core (mismo comando que en la PC)
        shell: pwsh
        run: ./tools/dotnet/run-tests.ps1 -Ci

      - name: Resultados (.trx)
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: core-test-results-${{ matrix.os }}
          path: tools/dotnet/.artifacts/TestResults/
          if-no-files-found: warn
```

Sin Unity, sin licencia, sin Dart, sin secretos (AC-APP-19-01). El paso de tests no tiene `continue-on-error`: un test rojo deja el job rojo y la consola (`verbosity=normal`) lo nombra con su nombre y categorías (AC-APP-19-02). `run-tests.ps1` es el mismo script de la PC (AC-APP-19-04). `pwsh` está instalado en los dos *runners*.

### 7.2 Higiene del repositorio como tests (AC-APP-19-03)

En `RowMate.Tests.EditMode` (así corre en el mismo comando local y en el CI; D-04):

| Test | Regla |
|---|---|
| `RepoHygieneTests.NoSecretsAreTracked` | `git ls-files -z` sin `StravaSecrets*`, `strava_secrets.*`, `strava.local.json`, `*.keystore`, `*.jks`, `*.p12`, `*.mobileprovision`, `*.ulf` |
| `RepoHygieneTests.GitignoreProtectsSecretsAndThirdParty` | `git check-ignore -q --no-index`: ignorados `Assets/RowMate/Bootstrap/Generated/StravaSecrets.g.cs`, `Assets/RowMate/Foo/StravaSecrets.cs`, `tools/strava/strava.local.json`, `Assets/ThirdParty/Mixamo/YBot/YBot.fbx`, `a.keystore`, `b.ulf`; **no** ignorado `Assets/ThirdParty/Mixamo/YBot/YBot.fbx.meta` |
| `RepoHygieneTests.ThirdPartyOnlyMetaTracked` | `git ls-files Assets/ThirdParty` solo `.meta`; ningún `*.fbx` versionado |
| `TokensUssSyncTests.UssMatchesTokens` | `Tokens.uss` = generado (AC-BASE-04) |
| `PureCodeRulesTests.*` | Sin `Math.Round(`, `MidpointRounding`, `ToString("F…"|"N…")` ni formato `{x:F1}` en Core, App y Simulator (ADR 012); sin `using UnityEngine`/`UnityEditor` en los puros y en Tests/Support, EditMode y Simulator; sin `ConfigureAwait(false)` ni `async void` en Core/App/Simulator; sin `System.Random` fuera de `SystemRandom.cs`; sin `DateTime.Now/UtcNow` en Core/App/Simulator; sin literales de color fuera de `Tokens.cs`; ningún `.fbx` fuera de `Assets/ThirdParty/` |

Si `git` no está o no hay repositorio, los tests que dependen de git quedan `Inconclusive` en la PC y **fallan** con `ROWMATE_CI=1`. Ningún test lee ni abre `lib/core/strava/strava_secrets.dart` (el oráculo lo impide, §6.1).

---

## 8. Matriz AC → clase → test

Clase de test = archivo en `Assets/RowMate/Tests/EditMode/…` salvo `RowingSimulatorTests` (`Tests/Simulator/`). Cada test lleva `[Category("<AC>")]` (o `.SetCategory` por caso). "Ign." = test con `[Ignore("NO VERIFICABLE → It. N …")]` que deja el AC visible en la salida (AC-BASE-09); su parte estática la cubre el test de la misma fila.

### 8.1 BLE (37)

| AC | Clase(s) bajo prueba | Test | WP |
|---|---|---|---|
| AC-BLE-01-01 | `RowingData` | `RowingDataTests.EmptyReadingIsZeroWithDashesAndZeroTime` | B |
| AC-BLE-01-02 | `RowingData`, `TimeFormats` | `RowingDataTests.SplitText` (13 casos) + `MatchesOracle_Split`; `TimeFormatsTests.SplitOrDashes` | B (A) |
| AC-BLE-01-03 | `RowingData`, `TimeFormats` | `RowingDataTests.ElapsedText` (10) + `MatchesOracle_Elapsed`; `TimeFormatsTests.Elapsed` | B (A) |
| AC-BLE-01-04 | `RowingData.CopyWith/WithHeartRate` | `RowingDataTests.CopyWithChangesOnlyNamedField` | B |
| AC-BLE-01-05 | `RowingFormats` | `RowingFormatsTests.DeviceGridTexts`; Ign. "texto en pantalla → It. 2" | B |
| AC-BLE-01-06 | `RowingData`, `SessionStats`, `WorkoutSession` → `TimeFormats` | `RowingFormatsTests.UsesSharedTimeRules`; `SessionStatsTests.FormatsUseSharedTimeRules` | B, C |
| AC-BLE-02-01 | `FtmsUuids` | `FtmsParserTests.Uuids` | B |
| AC-BLE-02-02 | `FtmsParser` | `FtmsParserTests.ReferencePackets` caso `2C 00 30 2A 01 E2 01 00 85 00 96 00` (`Demo`) | B |
| AC-BLE-02-03 | `FtmsParser` | ídem, caso `05 00 10 27 00` (`Demo`) | B |
| AC-BLE-02-04 | `FtmsParser` | ídem, caso `11 00 78 00` (`Demo`) | B |
| AC-BLE-02-05 | `FtmsParser` | ídem, caso `19 00 00 00 7D 00` (`Demo`) | B |
| AC-BLE-02-06 | `FtmsParser` | ídem, caso `19 00 85 00 7D 00` (`Demo`) | B |
| AC-BLE-02-07 | `FtmsParser` | ídem, caso `01 0B 2D 00 00 00 00 8C 2C 01` (`Demo`) | B |
| AC-BLE-02-08 | `FtmsParser` | ídem, caso `00 00 30` (`Demo`) | B |
| AC-BLE-02-09 | `FtmsParser` | ídem, caso `2C` (sin lectura) (`Demo`) | B |
| AC-BLE-02-10 | `FtmsParser` | `FtmsParserTests.StrokeRateIsHalfRaw` | B |
| AC-BLE-02-11 | `FtmsParser` | `FtmsParserTests.SignedPowerAndAverageOnlyIfZero` | B |
| AC-BLE-02-12 | `FtmsParser` | `FtmsParserTests.SkippedFields` (4) | B |
| AC-BLE-02-13 | `FtmsParser` | `FtmsParserTests.TruncatedAndTrailingBytes` (3) | B |
| AC-BLE-02-14 | `FtmsParser` | `FtmsParserTests.RawUnavailableValues` | B |
| AC-BLE-02-15 | `FtmsParser` | `FtmsParserTests.MissingFieldIsZeroNotPrevious` | B |
| AC-BLE-02-16 | `FtmsParser` | `FtmsParserTests.EmptyNotificationGivesNoReading` | B |
| (todas -02 a -16) | `FtmsParser` | `FtmsParserTests.MatchesOracle` (golden + fuzz; también `AC-BASE-03`) | B |
| AC-BLE-03-01 | `ScanFilter.IsClientSide` | `ScanFilterTests.ClientSideOnlyOnWindows`; Ign. "plataforma real → It. 10a/11; [Apple] 10b" | B |
| AC-BLE-03-02 | `ScanFilter`, `BleUuid` | `ScanFilterTests` LT-08 | B |
| AC-BLE-03-03 | `ScanFilter` | `ScanFilterTests` LT-09 | B |
| AC-BLE-03-04 | `BleUuid` | `BleUuidTests.ShortThirtyTwoAndLongFormsAreEqual` | A |
| AC-BLE-03-05 | `ScanFilter` | `ScanFilterTests.SameRuleForRower` | B |
| AC-BLE-14-01 | `RowingSimulator` | `RowingSimulatorTests.InitialValues` | B |
| AC-BLE-14-02 | `RowingSimulator` | `RowingSimulatorTests.TargetsAreClamped` + LT-05 | B |
| AC-BLE-14-03 | `RowingSimulator.PaceForWatts` | `RowingSimulatorTests.Concept2Split` (8 W) + LT-01, LT-02 + `MatchesOracle_Pace` | B |
| AC-BLE-14-04 | `RowingSimulator.CaloriesPerSecond` | `RowingSimulatorTests.Concept2Calories` | B |
| AC-BLE-14-05 | `RowingSimulator.Tick` | `RowingSimulatorTests.RowingWithoutNoise` (`Demo`) + LT-03 + `MatchesOracle_NoNoise600` | B |
| AC-BLE-14-06 | `RowingSimulator.Tick` | `RowingSimulatorTests.StopAndResume` + LT-04 + `MatchesOracle_StopResume` | B |
| AC-BLE-14-07 | `RowingSimulator` | `RowingSimulatorTests.HeartRateTargetAndApproach` | B |
| AC-BLE-14-08 | `RowingSimulator.HeartRate` | LT-07 | B |
| AC-BLE-14-09 | `RowingSimulator` + `IRandom` | `RowingSimulatorTests.NoiseRanges` + LT-06 + `MatchesOracle_NoiseReplay` | B |
| AC-BLE-14-10 | `RowingSimulator`, `DartMath` | `RowingSimulatorTests.OutputRounding` (24,25 → 24.3; 23,95 → 23.9; 23,99999999999998 → 24; mitad del pulso) | B |

### 8.2 HRM (9)

| AC | Clase | Test | WP |
|---|---|---|---|
| AC-HRM-01-01 | `HeartRateParser` | LT-15 | B |
| AC-HRM-01-02 | ídem | LT-10 | B |
| AC-HRM-01-03 | ídem | LT-11 | B |
| AC-HRM-01-04 | ídem | LT-12 | B |
| AC-HRM-01-05 | ídem | LT-13 | B |
| AC-HRM-01-06 | ídem | LT-14 | B |
| AC-HRM-01-07 | ídem | `HeartRateParserTests.NoRangeValidation` | B |
| AC-HRM-01-08 | ídem | `HeartRateParserTests.ReservedBitsAndTrailingBytes` | B |
| AC-HRM-01-09 | ídem | `HeartRateParserTests.Uint16WithContact` (+ `MatchesOracle`) | B |

### 8.3 RUT (58)

| AC | Clase(s) | Test | WP |
|---|---|---|---|
| AC-RUT-01-01 | `StepType`, `StepTypeNames`, `StepTypeInfo.SelectorOrder` | `StepTypeTests.FourTypesInSelectorOrderWithStorageNames` | C |
| AC-RUT-01-02 | `StepTypeInfo.NameKey`, `Localizer` | `StepTypeTests.NamesFromCatalog` (es/en) | C |
| AC-RUT-01-03 | `StepTypeInfo.Color*`, `Tokens` | `StepTypeTests.ColorsComeFromTokens` (hex + misma instancia; `""`, `sprint`, `null` → `#607D8B`) | C |
| AC-RUT-01-04 | `StepTypeInfo.IconName` | `StepTypeTests.IconNames`; Ign. "editor → It. 3" | C |
| AC-RUT-01-05 | `StepTypeNames.Parse` | `StepTypeTests.UnknownNameIsError` | C |
| AC-RUT-02-01 | `IntervalStep` | `IntervalStepTests.Attributes` | C |
| AC-RUT-02-02 | `IntervalStep.Goal`, `StepLabels.Duration` | `IntervalStepTests.GoalKind` | C |
| AC-RUT-02-03 | `IntervalStep.CopyWith` | `IntervalStepTests.CopyWithNeverClears` | C |
| AC-RUT-02-04 | `IntervalStep`, `StepLabels` | `IntervalStepTests.NoValidation` | C |
| AC-RUT-03-01 | `StepLabels.Duration` | `StepLabelsTests.DurationByTime` (10) | C |
| AC-RUT-03-02 | ídem | `StepLabelsTests.DurationByDistance` | C |
| AC-RUT-03-03 | ídem | `StepLabelsTests.DurationNoneAndBoth` | C |
| AC-RUT-03-04 | ídem, `DartMath` | `StepLabelsTests.DurationNegatives` | C |
| AC-RUT-03-05 | ídem | `StepLabelsTests.DurationSameInBothLanguages` + `MatchesLegacyLabels` (golden) | C |
| AC-RUT-04-01 | `StepLabels.Targets` | `StepLabelsTests.Targets` | C |
| AC-RUT-04-02 | ídem | `StepLabelsTests.Progressions` | C |
| AC-RUT-04-03 | ídem | `StepLabelsTests.FullLine` | C |
| AC-RUT-04-04 | ídem, `TimeFormats.MinSec` | `StepLabelsTests.SplitWithoutFloor` | C |
| AC-RUT-04-05 | `RoutineExpansion` + `StepLabels` | `StepLabelsTests.ExpandedStepKeepsProgression` | C |
| AC-RUT-04-06 | `StepLabels.EditorSubtitle` | `StepLabelsTests.EditorSubtitle`; Ign. "editor → It. 3" | C |
| AC-RUT-05-01 | `Routine` | `RoutineTests.AttributesAndDefaults` | C |
| AC-RUT-05-02 | `RoutineSummary` | `RoutineSummaryTests.SummaryTable` (9 × es/en, `Demo`) | C |
| AC-RUT-05-03 | `RoutineSummary.StartDialogContent` | `RoutineSummaryTests.WorkoutStartContent`; Ign. "diálogo → It. 4a" | C |
| AC-RUT-05-04 | `RoutineSummary.Count` | `RoutineSummaryTests.CountsMatchLegacy` (golden) | C |
| AC-RUT-06-01 | `RoutineExpansion.Flatten` | `RoutineExpansionTests.T1ExpandsToEight` (+ LT-16) | C |
| AC-RUT-06-02 | `RoutineExpansion.Flatten` | `RoutineExpansionTests.Case1SpmProgression` | C |
| AC-RUT-06-03 | ídem | `RoutineExpansionTests.Case2WattsRange` | C |
| AC-RUT-06-04 | ídem | `RoutineExpansionTests.Case3MinOnly` | C |
| AC-RUT-06-05 | ídem | `RoutineExpansionTests.Case4Split` | C |
| AC-RUT-06-06 | ídem | `RoutineExpansionTests.Case5ProgressionWithoutTarget` | C |
| AC-RUT-06-07 | ídem | `RoutineExpansionTests.Case6LooseStepNotProgressed` | C |
| AC-RUT-06-08 | ídem | `RoutineExpansionTests.Case7EmptyOrNonPositiveRepeats` | C |
| AC-RUT-06-09 | ídem | `RoutineExpansionTests.Case8ContiguousSeries` | C |
| AC-RUT-06-10 | ídem | `RoutineExpansionTests.FirstStepRepeatsRule` | C |
| AC-RUT-06-11 | ídem | `RoutineExpansionTests.SameIdNotConsecutive` | C |
| AC-RUT-06-12 | ídem | `RoutineExpansionTests.NoFloor` | C |
| AC-RUT-06-13 | ídem | `RoutineExpansionTests.OtherFieldsKept` (+ `MatchesOracle` para todo F-RUT-06) | C |
| AC-RUT-07-01 | `RoutineExpansion.Positions` | LT-16 | C |
| AC-RUT-07-02 | ídem | `RoutinePositionsTests.T1Complete` | C |
| AC-RUT-07-03 | ídem | `RoutinePositionsTests.EmptyOrZeroRepeats` | C |
| AC-RUT-07-04 | ídem | `RoutinePositionsTests.ContiguousSeries` | C |
| AC-RUT-07-05 | ídem + `Flatten` | `RoutinePositionsTests.AlwaysAligned` (todas las plantillas de §4.11 + golden) | C |
| AC-RUT-16-01 | `WorkoutSession` | `WorkoutSessionTests.AttributesAndDefaults` | C |
| AC-RUT-16-02 | `WorkoutSession.Closed`, `SessionStats` | `WorkoutSessionTests.ClosedStoresP99AndMaxima` | C |
| AC-RUT-16-03 | `WorkoutSession.DurationFormatted` | `WorkoutSessionTests.Duration` | C |
| AC-RUT-16-04 | `WorkoutSessionCodec`, `IsoDate` | `WorkoutSessionTests.DatesUtcIsoWithMicrosOnlyIfNonZero` + `LocalConversionUtcMinus3`; `IsoDateTests.*` | C (A) |
| AC-RUT-16-05 | `WorkoutSessionCodec.FromRecord` | `WorkoutSessionTests.TolerantRead` | C |
| AC-RUT-17-01 | `DataPoint` | `DataPointTests.AttributesAndDefaults` | C |
| AC-RUT-17-02 | `DataPoint.WithStep`, `RoutineExpansion` | `DataPointTests.StepIndexRefersToExpandedRoutine` | C |
| AC-RUT-18-01 | `SessionStats.Compute` | `SessionStatsTests.NoPoints` + LT-19 | C |
| AC-RUT-18-02 | `SessionStats.Percentile99Index` | `SessionStatsTests.P99Index` (+ golden 1-1100) | C |
| AC-RUT-18-03 | `SessionStats.Compute` | `SessionStatsTests.P99Examples` (`Demo`) | C |
| AC-RUT-18-04 | ídem | `SessionStatsTests.P99Decimals` | C |
| AC-RUT-18-05 | ídem | `SessionStatsTests.TotalsAreMaxima` | C |
| AC-RUT-18-06 | ídem | LT-17 | C |
| AC-RUT-18-07 | ídem | LT-18 | C |
| AC-RUT-18-08 | ídem, `DartMath.Round` | `SessionStatsTests.HeartRateRounding` | C |
| AC-RUT-18-09 | `SessionStats.*Formatted` | `SessionStatsTests.Formats` (+ `MatchesOracle`) | C |

### 8.4 APP (25)

| AC | Clase(s) | Test | WP |
|---|---|---|---|
| AC-APP-09-01 | `LocaleResolver`, `LanguageTag` | `LocaleResolverTests.VariantsMapToLanguage` | C |
| AC-APP-09-02 | `LocaleResolver` | `LocaleResolverTests.FlutterParityTable` (17 filas) | C |
| AC-APP-09-03 | `LocaleResolver` | `LocaleResolverTests.FallbackIsEnglish` | C |
| AC-APP-09-04 | `LanguageState` | `LanguageStateTests.NoSetterNoPersistence` (API pública: solo `Current` y `Refresh`); Ign. "→ It. 2" | C |
| AC-APP-09-05 | `LanguageState` | `LanguageStateTests.RefreshNotifiesChange`; Ign. "visible → It. 2" | C |
| AC-APP-09-06 | `LocaleResolver`, `ISystemLocale` | `LocaleResolverTests.UsesWholeList`; Ign. "[Device] Android/Windows → It. 2; [Apple] 10b" | C |
| AC-APP-09-07 | — | Ign. "[Device] → It. 3 (P-06 aceptada; primer campo de texto: editor de rutinas)" (spec corregida por QA-12) | C |
| AC-APP-10-01 | `LocalizationCatalog` | `CatalogParityTests.SameKeysInBothLanguages` (174 y 174) | C |
| AC-APP-10-02 | catálogos vs `l10n_legacy.json` | `CatalogParityTests.UnchangedLegacyTextsAreIdentical` (155) + `SpecialCharacters` | C |
| AC-APP-10-03 | catálogos | `CatalogParityTests.FourChangedKeys` | C |
| AC-APP-10-04 | `LocalizationCatalog.Placeholders` | `CatalogParityTests.SameParametersNamesAndTypes` (las 21 claves con parámetros, nombre **y** tipo según §3.3; ninguna otra tiene metadatos) | C |
| AC-APP-10-05 | `Localizer`, `MessageFormatter` | `LocalizerTests.TextsWithParameters` | C |
| AC-APP-10-06 | ídem | `LocalizerTests.Plurals` | C |
| AC-APP-10-07 | catálogos | `CatalogParityTests.FifteenNewKeys` (textos de la spec §4.17.2 y tipos de §3.3) | C |
| AC-APP-10-08 | catálogos | `CatalogParityTests.NoLegacyKeyLost` | C |
| AC-APP-10-09 | `Localizer`, `L10nKeys` | `LocalizerTests.MissingKeyThrows` + `L10nKeysTests.EveryConstantExistsInBothCatalogs` | C |
| AC-APP-16-01 | `AppIdentity` (+ bootstrapper) | `AppIdentityTests.NameCompanyVersion` + `BootstrapperStaticTests.UsesAppIdentity`; Ign. "[Editor]/[Device] → It. 2; [Apple] 10b" | A |
| AC-APP-16-02 | `AppIdentity` | `AppIdentityTests.ProvisionalIdsAndNoComExample` (texto corregido por el PO, QA-02: Android `com.iguisoft.rowmate`; iOS, macOS y el id único de Standalone `com.iguisoft.rowmate.unity`; Windows sin id, empresa `iguisoft` y producto `RowMate`; `WindowsApplicationId` = `com.iguisoft.rowmate` solo como dato reservado, que el bootstrapper no usa; ningún `com.example` en `AppIdentity`, bootstrapper ni manifest); Ign. "[Device] carpeta `LocalLow/iguisoft/RowMate` y convivencia → It. 2; [Apple] → It. 10b" | A |
| AC-APP-16-03 | `AppIdentity` | `AppIdentityTests.IdsDifferFromLegacy` (Android ≠ `com.example.rower_app`, Apple ≠ `com.iguisoft.rowmate`); Ign. "[Device] → It. 2; [Apple] 10b" | A |
| AC-APP-16-04 | `AppIdentity`, bootstrapper | `AppIdentityTests.AndroidMin26Arm64` (constante + texto del bootstrapper); Ign. device | A |
| AC-APP-16-05 | `AppIdentity` | `AppIdentityTests.IosMin15`; Ign. "[Apple] → It. 10b" | A |
| AC-APP-19-01 | `ci-core.yml` | `CiWorkflowTests.RunsOnPushAndPrOnWindowsAndLinuxWithoutUnityDartOrSecrets`; Ign. "[CI] sin remoto (P-03)" | A |
| AC-APP-19-02 | `ci-core.yml`, `run-tests.ps1` | `CiWorkflowTests.FailingTestFailsJob` (sin `continue-on-error`; el script propaga el primer código ≠ 0: el del `dotnet build` si falla, si no el del `dotnet test`); Ign. "[CI]" | A |
| AC-APP-19-03 | higiene | `RepoHygieneTests.*`, `PureCodeRulesTests.*`, `TokensUssSyncTests.UssMatchesTokens` | A |
| AC-APP-19-04 | `run-tests.ps1`, `ci-core.yml` | `CiWorkflowTests.LocalEqualsCi` (el workflow invoca `run-tests.ps1`; el script contiene los dos comandos de §2.6) + `CiWorkflowTests.ScriptBuildsWholeSolutionThenTestsWithoutRebuilding` (QA-01) | A |

### 8.5 BASE (10)

| AC | Clase(s) | Test | WP |
|---|---|---|---|
| AC-BASE-01 | `Fmt`, `DartMath` | `FmtFixedTests.SpecTable` + `MatchesOracle`, `DartMathTests.SpecTable` + `MatchesOracle`, `DartMathTests.RoundToFixed` | A |
| AC-BASE-02 | `Fmt`, `RowingFormats` | `CultureIndependenceTests.*` (A); `RowingFormatsTests.CultureIndependent` (B) | A, B |
| AC-BASE-03 | golden + `GoldenVectors` | `GoldenCoverageTests.AllGoldenFilesPresentWithHeader` + `FilesWithinSizeCap` + `BulkSectionsAreTables` (`Integration`; QA-06) + todos los `MatchesOracle*` | A |
| AC-BASE-04 | `Tokens`, `TokenCatalog`, `UssTokenWriter` | `TokensContractTests.LegacyContractHex`, `TokenCatalogTests.*`, `TokensUssSyncTests.UssMatchesTokens`, `PureCodeRulesTests.NoColorLiteralsOutsideTokens` | A |
| AC-BASE-05 | `Contrast`, `Tokens` | `ContrastTests.HudAgainstWorstGlass`, `ContrastTests.UiTextOnCard`, `ContrastTests.WorstGlassIs2A2F37`, `ContrastTests.DesignerPairs` (G1-G3 y T1-T10 de 04-design-notes §4.3, datos del test; QA-08) | A |
| AC-BASE-06 | `IScheduler`, `FakeScheduler` | `FakeSchedulerTests.ThreeSimulatedSecondsInUnderOneRealSecond` + `DeterministicOrder` | A |
| AC-BASE-07 | `IRandom`, `RowingSimulator` | `SequenceRandomTests.*` (A); `RowingSimulatorTests.NoNoiseIsDeterministicAndConsumesNoRandom` (B) | A, B |
| AC-BASE-08 | todo lo puro | `PureCodeRulesTests.NoEngineInPureCode`, `AsmdefLintTests.PureAssembliesHaveNoEngineReferences`, `NUnitCompatibilityTests.*` (incluido `NoRecordOrInitInTestSources`, QA-03), y la **compilación** de `RowMate.Tests.UnityApiCheck` (`netstandard2.1` + NUnit 3.5.0, QA-10). Mecanismo (QA-01): el paso 1 de `run-tests.ps1` es `dotnet build` de **toda** `RowMate.sln` y su fallo corta la corrida con código ≠ 0; lo garantiza `CiWorkflowTests.ScriptBuildsWholeSolutionThenTestsWithoutRebuilding`. Ign. "en Unity → It. 2" | A |
| AC-BASE-09 | todos los tests | `TraceabilityTests.EveryAcceptanceIdHasATest` + `EveryLegacyCaseKeepsItsName` (`Integration`; lectura del **texto** de `Tests/EditMode/**` y `Tests/Simulator/**`, §8.6, QA-09); resultado real por AC: `run-tests.ps1 -Matrix` (los `.trx`) | A |
| AC-BASE-10 | asmdefs, manifest, versión, bootstrapper | `AsmdefLintTests.*` (JSON, nombres, referencias existentes, grafo = `.csproj`, regla §3.2, `defineConstraints`, `includePlatforms`, `autoReferenced: false`), `ManifestAndVersionTests.*`, `BootstrapperStaticTests.*`; Ign. "abrir sin errores → It. 2" | A |

### 8.6 Los 19 casos legacy → NUnit

| LT | Legacy | Test NUnit (`TestName` = nombre exacto) | Categorías | WP |
|---|---|---|---|---|
| LT-01 | `test/rowing_simulator_test.dart:7` | `RowingSimulatorTests` · "más watts → pace más rápido" | LT-01, AC-BLE-14-03 | B |
| LT-02 | `:14` | "203 W ≈ 2:00 /500m (fórmula Concept2)" (`Within(0.5)`) | LT-02, AC-BLE-14-03 | B |
| LT-03 | `:18` | "acumula distancia, remadas y tiempo mientras rema" | LT-03, AC-BLE-14-05 | B |
| LT-04 | `:36` | "sin remar: 0 spm/watts/pace y totales congelados" | LT-04, AC-BLE-14-06 | B |
| LT-05 | `:54` | "los objetivos se limitan a su rango" (`new RowingSimulator()`) | LT-05, AC-BLE-14-02 | B |
| LT-06 | `:62` | "con ruido, los watts quedan dentro de ±5 %" (`SystemRandom`) | LT-06, AC-BLE-14-09 | B |
| LT-07 | `:70` | "heartRate expone el último pulso calculado por tick()" (fuera de la agrupación, como el legacy: método aparte) | LT-07, AC-BLE-14-08 | B |
| LT-08 | `test/scan_filter_test.dart:24` | `ScanFilterTests` · "con filtro en el cliente solo pasan los que anuncian el servicio" | LT-08, AC-BLE-03-02 | B |
| LT-09 | `:32` | "sin filtro en el cliente (el sistema ya filtró) pasa todo" | LT-09, AC-BLE-03-03 | B |
| LT-10 | `test/heart_rate_parser_test.dart:5` | `HeartRateParserTests` · "bpm en uint8 sin información de contacto" | LT-10, AC-HRM-01-02 | B |
| LT-11 | `:11` | "bpm en uint16 little-endian" | LT-11, AC-HRM-01-03 | B |
| LT-12 | `:16` | "bits de contacto: 01 = no soportado, 10 = sin contacto, 11 = con contacto" | LT-12, AC-HRM-01-04 | B |
| LT-13 | `:23` | "ignora Energy Expended y RR intervals" | LT-13, AC-HRM-01-05 | B |
| LT-14 | `:29` | "bytes vacíos o truncados devuelven null" | LT-14, AC-HRM-01-06 | B |
| LT-15 | `:35` | "UUIDs del perfil" | LT-15, AC-HRM-01-01 | B |
| LT-16 | `test/routine_positions_test.dart:6` | `RoutinePositionsTests` · "las posiciones quedan alineadas con flattenedSteps" | LT-16, AC-RUT-06-01, AC-RUT-07-01 | C |
| LT-17 | `test/session_stats_test.dart:7` | `SessionStatsTests` · "avgHeartRate y maxHeartRate ignoran los ceros" | LT-17, AC-RUT-18-06 | C |
| LT-18 | `:14` | "sin pulso quedan en 0" | LT-18, AC-RUT-18-07 | C |
| LT-19 | `:21` | "sin puntos quedan en 0" | LT-19, AC-RUT-18-01 | C |

Cada uno replica las mismas aserciones del legacy (y las mismas entradas: `noise: false`, objetivos, fixtures) y lleva `// legacy: test/<archivo>:<línea> '<nombre>'`. `TraceabilityTests` verifica que los 19 `TestName` existan literalmente y que los 139 AC (lista explícita en `It01Ids.cs`, con los rangos expandidos) tengan al menos un `Category`/`SetCategory`.

**Cómo ve `TraceabilityTests` los tests del simulador** (QA-09). `TraceabilityTests` vive en `RowMate.Tests.EditMode`, que no referencia ni puede referenciar `RowMate.Tests.Simulator` (en Unity ese assembly solo existe con `ROWMATE_SIMULATOR`). Por eso **no usa reflexión**: lee con `RepoPaths` el **texto** de `Assets/RowMate/Tests/EditMode/**/*.cs` y `Assets/RowMate/Tests/Simulator/**/*.cs` (sin comentarios) y extrae los literales de `[Category("…")]`, `Category = "…"`, `.SetCategory("…")` y `TestName = "…"`/`.SetName("…")`. Así cubre AC-BLE-14-01 a -10 y LT-01 a LT-07 igual que los demás, y funciona igual en `dotnet` y en el Editor. Para que sea posible, categorías y nombres se escriben siempre como literales (§2.4). Lo estático prueba que **existe** un test por AC; el resultado real (pasó, falló, ignorado) de cada AC sale de la matriz `ac-matrix.md` que `run-tests.ps1 -Matrix` arma desde los `.trx` de los dos assemblies de test (AC-BASE-09).

### 8.7 Qué imprime la demo (`run-tests.ps1 -Demo`, QA-13)

La spec (§1, punto 2) pide ver en la salida los 8 paquetes FTMS, el simulador a 150 W y 24 spm, los resúmenes es/en y el P99. Los tests con categoría `Demo` (además de sus categorías de AC) escriben **una línea por caso** con `TestContext.Out.WriteLine` (existe en NUnit 3.5; con `console;verbosity=detailed` la consola de `dotnet test` muestra la salida de los tests que pasan: probado). Los números se escriben con `Fmt`/`TimeFormats` (cultura invariante). Formato exacto:

| Test (categoría `Demo`) | Casos | Línea |
|---|---|---|
| `FtmsParserTests.ReferencePackets` (AC-BLE-02-02 a -09) | 8 | `[DEMO] FTMS 2C 00 30 2A 01 E2 01 00 85 00 96 00 -> spm=24.0 paladas=298 m=482 split=133 (02:13) W=150 kcal=0 bpm=0 t=0 (00:00)`; el `2C` → `[DEMO] FTMS 2C -> sin lectura` |
| `RowingSimulatorTests.RowingWithoutNoise` (AC-BLE-14-05) | 1 (tick 60) | `[DEMO] Simulador 150 W / 24 spm sin ruido, tick 60 -> spm=24.0 paladas=24 m=226 split=133 (02:13) W=150 kcal=13 bpm=142 t=60 (01:00)` |
| `RoutineSummaryTests.SummaryTable` (AC-RUT-05-02) | 9 | `[DEMO] Resumen <plantilla> -> es "1 serie · 1 descanso" / en "1 interval · 1 rest"` |
| `SessionStatsTests.P99Examples` (AC-RUT-18-03) | los de la spec | `[DEMO] P99 <caso> -> W=<w> spm=<spm> split=<s> (<MM:SS>)`, por ejemplo `W=198 spm=29.8 split=198 (03:18)` (`spm` con `Fmt.DartToString`, sin redondear, como el legacy) |

`TraceabilityTests.DemoCoversSpec` (`Integration`) exige que esos cuatro tests lleven `Demo` en el texto. Ningún otro test escribe líneas `[DEMO]`.

---

## 9. Paquetes de trabajo

### 9.0 Orden y reglas

```
WP-A (primero)  ──►  WP-B ─┐
                      WP-C ─┴──►  Cierre de integración (dueño: WP-A)
```

- **Disjuntos por archivo.** Cada WP crea y edita solo sus archivos de las listas de abajo. B y C **no** tocan ningún `.csproj`, `RowMate.sln`, `Directory.Build.props`, asmdef, `AssemblyInfo.cs`, `Tests/Support`, `Tokens.cs` ni nada de A: los globs incluyen sus archivos solos. Si B o C necesitan un cambio en A (un *bug* en `Fmt`, un helper nuevo), lo piden al coordinador y lo hace A.
- **El árbol compila siempre.** B y C comparten `RowMate.Core` y `RowMate.Tests.EditMode`; un archivo a medio escribir rompe la compilación del otro (R-T3). Regla: cada archivo nuevo entra compilando (tipo con sus firmas de §3 y cuerpo completo); nadie deja advertencias en los puros (`TreatWarningsAsErrors`).
- **Vectores**: cada WP corre `run-oracle.ps1 -Only <sus scripts>` y verifica que el legacy siga limpio.
- **Fuentes de test sin `record` ni `init`** (§2.4, QA-03): los casos, filas y *fixtures* de B y C son `class` o `struct` con constructor. Un `record` en un test compila en `net8.0` pero rompe el paso 1 de `run-tests.ps1` (`CS0518` en `UnityApiCheck`) y, más tarde, Unity.
- **Filtro de trabajo** (el filtro solo achica el `dotnet test`: el paso 1 compila siempre la solución entera): B corre `run-tests.ps1 -Filter "FullyQualifiedName~Ble|FullyQualifiedName~RowingData|FullyQualifiedName~RowingFormats|FullyQualifiedName~RowingSimulator"`; C, `"FullyQualifiedName~Models|FullyQualifiedName~Localization"` (mientras el otro trabaja); el cierre corre todo.

### 9.1 WP-A — esqueleto Unity, `tools/dotnet`, piezas base, soporte de tests, oráculo y CI (139 archivos)

**Raíz (8):** `.gitattributes`, `.gitignore` (modificar), `.editorconfig`, `global.json`, `.github/workflows/ci-core.yml`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, `Assets/csc.rsp`.

**Asmdefs y `AssemblyInfo.cs` (28):** `Assets/RowMate/{Core,App,Infrastructure,Simulator,Platform,Presentation,Scene,SimulatorPresentation,Bootstrap,Editor,Tests/Support,Tests/EditMode,Tests/Simulator,Tests/PlayMode}/` → `<asmdef de §1.2>` + `AssemblyInfo.cs`.

**Shims (3):** `Assets/RowMate/Core/Internal/IsExternalInit.cs`, `Assets/RowMate/App/Internal/IsExternalInit.cs`, `Assets/RowMate/Simulator/Internal/IsExternalInit.cs` (ninguno en `Tests/`, QA-03).

**Core/Common (16):** `Assets/RowMate/Core/Common/{IReadOnlyObservable,ObservableValue,EventStream,IClock,IScheduler,IRandom,SystemRandom,LogLevel,IAppLogger,NullAppLogger,TaskExtensions,PlatformKind,AppIdentity}.cs`, `Assets/RowMate/Core/Common/Json/{JsonKind,JsonNode,JsonParser}.cs`.

**Core/Text (5):** `Assets/RowMate/Core/Text/{Fmt,DartMath,DoubleParts,IsoDate,TimeFormats}.cs`.

**Core/Ble (2):** `Assets/RowMate/Core/Ble/{BleUuid,BleScanResult}.cs`.

**Core/Design (14):** `Assets/RowMate/Core/Design/{Rgb,TokenKind,IDesignToken,ColorRole,ColorToken,LengthToken,DurationToken,EasingToken,FontFamilyRole,TypeScaleToken,TokenCatalog,UssTokenWriter,Contrast,Tokens}.cs` (`Tokens.cs`: del Designer; A lo crea solo si falta, con §4.4).

**Unity (6):** `Assets/RowMate/Bootstrap/BuildFlags.cs`, `Assets/RowMate/Editor/Bootstrap/{ProjectBootstrapper,BootstrapState,PlayerSettingsStep,UrpProvisioner}.cs`, `Assets/RowMate/Presentation/Styles/Tokens.uss`.

**Tests/Support (10):** `Assets/RowMate/Tests/Support/{RepoPaths,GoldenVectors,GoldenFile,GoldenTable,GoldenJsonExtensions,HexBytes,FakeScheduler,TestMainThreadContext,SequenceRandom,RecordingLogger}.cs`.

**Tests/EditMode de A (28):** `Text/{FmtFixedTests,FmtDartToStringTests,DartMathTests,IsoDateTests,TimeFormatsTests,CultureIndependenceTests}.cs`, `Common/{ObservableValueTests,EventStreamTests,FakeSchedulerTests,TestMainThreadContextTests,SequenceRandomTests,JsonParserTests}.cs`, `Ble/BleUuidTests.cs`, `Design/{TokensContractTests,TokenCatalogTests,ContrastTests,TokensUssSyncTests}.cs`, `Project/{AsmdefLintTests,ManifestAndVersionTests,AppIdentityTests,BootstrapperStaticTests,PureCodeRulesTests,NUnitCompatibilityTests,RepoHygieneTests,CiWorkflowTests,GoldenCoverageTests}.cs`, `Traceability/{It01Ids,TraceabilityTests}.cs` (todos bajo `Assets/RowMate/Tests/EditMode/`).

**Golden (1):** `Assets/RowMate/Tests/Golden/oracle/fmt.json`.

**tools (18):** `tools/dotnet/{RowMate.sln,Directory.Build.props,run-tests.ps1,README.md}`, `tools/dotnet/RowMate.{Core,App,Infrastructure,Simulator,Tests.Support,Tests.EditMode,Tests.Simulator,Tests.UnityApiCheck,Tokens}/<mismo nombre>.csproj`, `tools/tokens/Program.cs`, `tools/oracle/{README.md,run-oracle.ps1,lib/golden.dart,bin/fmt_vectors.dart}`.

**Listo cuando:** `run-tests.ps1 -Filter "TestCategory!=Integration"` en verde con la salida real pegada, **incluido su paso 1** (`dotnet build` de la solución entera, 9 proyectos, 0 errores: `RowMate.Tests.UnityApiCheck` compila contra NUnit 3.5.0 y `RowMate.Tokens` compila); 0 advertencias en Core/App/Simulator; `Fmt.DartToString` con el algoritmo de §3.1 y en verde contra `toStringPow2` y `toStringTies`; `ContrastTests.DesignerPairs` en verde; `fmt.json` generado con el legacy limpio antes y después (guardia de §6.1) y ≤ 1 MiB; `Tokens.uss` sincronizado; `git -C ../rowmate-legacy status --porcelain` vacío.

### 9.2 WP-B — RowingData y formatos, FTMS, pulso, filtro, física del simulador (22 archivos)

- `Assets/RowMate/Core/Models/{RowingData,RowingFormats}.cs`
- `Assets/RowMate/Core/Ble/{FtmsUuids,FtmsParser,HeartRateReading,HeartRateParser,ScanFilter}.cs`
- `Assets/RowMate/Simulator/RowingSimulator.cs`
- `Assets/RowMate/Tests/EditMode/Models/{RowingDataTests,RowingFormatsTests}.cs`
- `Assets/RowMate/Tests/EditMode/Ble/{FtmsParserTests,HeartRateParserTests,ScanFilterTests}.cs`
- `Assets/RowMate/Tests/Simulator/RowingSimulatorTests.cs`
- `tools/oracle/bin/{rowing_data_vectors,ftms_vectors,heart_rate_vectors,simulator_vectors}.dart`
- `Assets/RowMate/Tests/Golden/oracle/{rowing_data,ftms,heart_rate,simulator}.json`

Depende de A: `TimeFormats`, `Fmt`, `DartMath`, `BleUuid`, `BleScanResult`, `PlatformKind`, `IRandom`, `SystemRandom`, `GoldenVectors`/`GoldenTable`, `HexBytes`, `SequenceRandom`, `RepoPaths`, `lib/golden.dart`. **Listo cuando:** sus tests (los AC de §8.1, §8.2, AC-BASE-02/07 parte B y LT-01 a LT-15) en verde, todo el árbol compilando (paso 1 de `run-tests.ps1`), las líneas `[DEMO]` de §8.7 (FTMS y simulador), sus 4 JSON generados con el legacy limpio, con las secciones grandes en formato de tabla y ≤ 1 MiB cada uno (§6.2).

### 9.3 WP-C — pasos, rutinas, series y progresiones, sesión, telemetría, estadísticas, catálogo y `MessageFormatter` (60 archivos)

- `Assets/RowMate/Core/Models/{StepType,StepTypeNames,StepGoal,StepTypeInfo,IntervalStep,StepLabels,Routine,StepPosition,RoutineExpansion,RoutineCounts,RoutineSummary,WorkoutSession,WorkoutSessionRecord,WorkoutSessionCodec,DataPoint,SessionStats}.cs`
- `Assets/RowMate/Core/Localization/{AppLanguage,LanguageTag,LocaleResolver,LanguageState,MessageArg,MessageArgKind,MessageFormatter,CompiledMessage,PlaceholderInfo,LocalizationCatalog,Localizer,LocalizationSet,MissingTranslationException,L10nKeys}.cs`
- `Assets/RowMate/Core/Ports/ISystemLocale.cs`
- `Assets/RowMate/Localization/{strings.es.json,strings.en.json}`
- `Assets/RowMate/Tests/EditMode/Models/{RoutineFixtures,StepTypeTests,IntervalStepTests,StepLabelsTests,RoutineTests,RoutineSummaryTests,RoutineExpansionTests,RoutinePositionsTests,WorkoutSessionTests,DataPointTests,SessionStatsTests}.cs`
- `Assets/RowMate/Tests/EditMode/Localization/{LocalizationFixture,LocaleResolverTests,LanguageStateTests,MessageFormatterTests,LocalizerTests,CatalogParityTests,L10nKeysTests}.cs`
- `tools/oracle/bin/{routine_vectors,session_vectors,l10n_vectors}.dart`
- `tools/l10n/{arb_to_catalog.dart,it01-changes.json,README.md}`
- `Assets/RowMate/Tests/Golden/oracle/{routines,session,l10n_legacy}.json`

Depende de A: `TimeFormats`, `Fmt`, `DartMath`, `IsoDate`, `ObservableValue`, `JsonParser`/`JsonNode`, `Tokens` (`StepWork`… `StepUnknown`), `ColorToken`, `GoldenVectors`/`GoldenTable`, `RepoPaths`, `lib/golden.dart`. **No** depende de B. Antes de escribir código: `WorkoutSessionRecord` y `Routine` con los constructores e inicializadores de §3.3 (QA-05) y los tipos de parámetros de las 15 claves nuevas de §3.3 en `it01-changes.json` (QA-07). **Listo cuando:** sus tests (§8.3, §8.4 APP-09/-10, LT-16 a LT-19) en verde, árbol compilando (paso 1 de `run-tests.ps1`), las líneas `[DEMO]` de §8.7 (resúmenes y P99), sus 3 JSON (≤ 1 MiB cada uno, tablas para lo grande) y los 2 catálogos generados con el legacy limpio (la conversión con `run-oracle.ps1 -L10n`).

### 9.4 Cierre de integración (WP-A, sin archivos nuevos)

1. `run-oracle.ps1` completo (los 8 scripts) con la guardia de §6.1 (`--porcelain` vacío, listado `--ignored` y metadatos sin cambios).
2. `run-tests.ps1` completo en verde (los dos pasos), incluida la categoría `Integration` (trazabilidad, cobertura y tamaño de vectores), y `run-tests.ps1 -Matrix` para QA; `run-tests.ps1 -Demo` para la demo (§8.7).
3. Actualizar documentos (§10.4).

---

## 10. Riesgos, desvíos propuestos y respuestas a la spec

### 10.1 Riesgos

| ID | Riesgo | Impacto | Mitigación |
|---|---|---|---|
| R-T1 | **Mezcla alfa en espacio lineal.** El proyecto es Linear; Unity mezcla la UI en lineal sobre un *backbuffer* sRGB. El peor caso `#2A2F37` (y P-05) supone mezcla sRGB. Con mezcla lineal, `#070D16 @86 %` sobre blanco da L = 0,143: blanco 5,4:1 y `metric.split.hud` 2,8:1 (recalculado) | El HUD con el respaldo plano no cumpliría R-10 aunque el test de `dotnet` pase | `ContrastTests` imprime también el cálculo lineal (informativo). **Designer + Architect** deciden antes de la It. 4a: `PanelSettings.forceGammaRendering` con *render texture* UNORM, vidrio con mezcla propia en el material, o alfa más alto. Se mide en Editor en la It. 2 (captura y lectura del color compuesto) |
| R-T2 | Código y configuración de Unity sin compilar (asmdefs de paquetes, `activeInputHandler` por `SerializedObject`, `UniversalRendererData` creado sin `postProcessData`) | Errores en el primer open | Capa delgada; *lint*; cada paso del bootstrapper aislado con respaldo manual; QA revisa en estático; primer open obligatorio en la It. 2 (RA-01, RA-13) |
| R-T3 | B y C comparten `RowMate.Core` y `RowMate.Tests.EditMode`: un archivo roto de uno frena al otro | Demoras en paralelo | Regla "el árbol compila siempre" (§9.0); archivos chicos; filtros por WP; el coordinador puede serializar las entregas sin tocar la disjunción |
| R-T4 | `Math.Pow` de .NET (glibc en Linux, CRT en Windows) y el `pow` de la VM de Dart pueden diferir en el último bit | Un entero derivado del simulador podría cambiar en un borde | Split exacto con `Within(1e-9)`; enteros contra el oráculo en Windows y Linux (CI); si difiere, se documenta y se decide con QA (no se tocan fórmulas) |
| R-T5 | Mono/IL2CPP formatean y parsean `double` distinto que .NET 8 | Paridad que pasa en `dotnet` y no en Unity | `Fmt`/`DartMath` sin APIs de formato del runtime; vectores dorados por bits; los mismos tests en EditMode (It. 2) y en el *smoke test* IL2CPP |
| R-T6 | NUnit 3.5 (Unity) ≠ 3.14 (dotnet) y `net8.0` ≠ perfil de Unity | Tests que solo corren en `dotnet` | `RowMate.Tests.UnityApiCheck` (`netstandard2.1` + paquete NUnit **3.5.0**, compilado en el paso 1 de `run-tests.ps1`, QA-01/QA-10) + `NUnitCompatibilityTests` (lo que compila en 3.5 pero no se admite) + regla sin `record`/`init` en tests (QA-03). Residual NV: el NUnit de Unity es un 3.5 propio (`com.unity.ext.nunit`); se confirma en la It. 2 |
| R-T7 | `dotnet restore` necesita red (NuGet) en la PC y en el CI | Sin red no hay build | Versiones fijas (NUnit 3.5.0 se suma a la lista); la caché de NuGet local sirve después de la primera vez. AC-BASE-08 ("sin red") se cumple desde la segunda corrida |
| R-T8 | Nombres de test con `≈`, `→`, `%`, paréntesis | Algún filtro `Name~` puede no matchear | Filtrar por `TestCategory` (estable); los nombres solo se leen |
| R-T9 | `core.autocrlf=true` en esta máquina; el legacy tiene `.arb` con CRLF | Diferencias falsas en archivos generados | `.gitattributes` con `eol=lf` para JSON/USS; los tests de sincronía normalizan `CRLF → LF`; la instantánea de l10n compara textos decodificados, no bytes |
| R-T10 | `TokenCatalog` por reflexión bajo *managed stripping* | Lista incompleta en un *player* | Solo se usa en herramientas, tests y Editor; los consumidores usan campos |
| R-T11 | Vectores dorados dentro de `Assets/` (Unity los importa como `TextAsset`) | Tiempo de importación, tamaño | ≤ 1 MiB por archivo, verificado por `GoldenCoverageTests.FilesWithinSizeCap`; secciones grandes en formato de tabla (§6.2; `fmt.json` medido en 0,60 MiB, QA-06); no se referencian desde escenas (no entran en *builds*) |
| R-T12 | `Assets/csc.rsp` podría no aplicarse a los asmdefs | Solo advertencias de nulabilidad en Unity | NV; si pasa, se agrega un `csc.rsp` junto a cada asmdef en la It. 2 |

### 10.2 Desvíos propuestos (para QA y, donde se indica, el PO)

| ID | Qué | Por qué | Afecta |
|---|---|---|---|
| D-01 | Vectores dorados en `Assets/RowMate/Tests/Golden/oracle/` (ADR 012 §4 y ARCHITECTURE §2/§15.1 decían `tools/oracle/golden/`) | Datos junto a los tests que los usan; los dos ejecutores los leen con `RepoPaths` | ADR 012, ADR 013, ARCH §2, §15.1 (se actualizan, §10.4) |
| D-02 | **Windows no tiene application identifier en Unity**: el identificador `Standalone` es uno solo para Windows y macOS; se fija en `com.iguisoft.rowmate.unity` (lo necesita macOS por R-14). En Windows lo visible es empresa `iguisoft` y producto `RowMate` (versión del `.exe`, carpeta de datos `AppData/LocalLow/iguisoft/RowMate`, distinta del legacy). `com.iguisoft.rowmate` de Windows queda como dato (`AppIdentity.WindowsApplicationId`) para el empaquetado MSIX de la It. 14 | Restricción del motor | **Aceptado por el PO** en el gate (QA-02): AC-APP-16-02 reescrito (Windows sin id; `RowMate` de `iguisoft`; `com.iguisoft.rowmate` reservado para el MSIX de la It. 14). Al cerrar: ARCHITECTURE §16 (punto 1) y §21 (Q-85 / R-14) |
| D-03 | `ILogger` → `IAppLogger` | `UnityEngine.ILogger` choca en todo archivo de Unity que use `RowMate.Core.Common` | ARCH §17 |
| D-04 | Higiene del repositorio como tests de `dotnet` (git por `Process`) en lugar de `tools/ci/check-repo-hygiene.ps1/.sh` | Un solo comando local = CI (AC-APP-19-04) | ARCH §2, §19 |
| D-05 | Conversión `.arb` → catálogo con un script Dart (`tools/l10n/arb_to_catalog.dart`) y `L10nKeys` mantenido a mano con test de sincronía (en vez de una consola `dotnet` que genera `L10nKeys`) | No suma un `.csproj` que B/C no pueden tocar; la verificación es el test | ARCH §2, LEGACY_MAPPING §3 |
| D-06 | `LocaleResolver` = algoritmo `basicLocaleListResolution` de Flutter (no "el primero soportado") | Restricción 3 de la spec, filas † de AC-APP-09-02 (paridad por defecto de P-01) | ARCH §12, ADR 011 |
| D-07 | Infrastructure sin referencia a sqlite-net/Newtonsoft hasta la It. 3; `IDbExecutor` e `InlineDbExecutor` también en la It. 3 | Menos superficie sin verificar; sin uso en la It. 1 | ARCH §3.1, ADR 005 (calendario) |
| D-08 | `Tests.Support` lee JSON con el `JsonParser` de Core (sin Newtonsoft) | Un solo lector para catálogo, vectores y *lint*; Core no referencia nada | — |
| D-09 | `Icons.cs` (codepoints) y `MetricPalette` pasan a la It. 2; `SlotIds` a la It. 6a | La fuente de íconos se descarga en la It. 2 (ASSET_SLOTS §4.1); F-APP-06 no es de la It. 1; los slots no se usan antes de la 6a. AC-RUT-01-04 se cubre con el nombre del ícono como dato | ARCH §7.7, ADR 016 |
| D-10 | Proyecto extra `RowMate.Tests.UnityApiCheck` (solo compila), contra `netstandard2.1` y NUnit 3.5.0, con las fuentes de `Tests/Support`, `Tests/EditMode` y `Tests/Simulator` por glob; lo compila el `dotnet build` de la solución que `run-tests.ps1` corre antes del `dotnet test --no-build` (QA-01, QA-10) | Garantía estática de AC-BASE-08 sin Editor | ARCH §15.1 |
| D-11 | `IClock` y los modelos usan `DateTimeOffset` (ARCH tenía `DateTime UtcNow/LocalNow`) | Representa "instante + zona" como el `DateTime` local de Dart sin las trampas de `DateTimeKind` | ARCH §6 |
| D-12 | `DataPoint.ElapsedSeconds` obligatorio (como el legacy, `workout_session.dart:143`, `:157`); el "0 por defecto" de AC-RUT-17-01 vale para SPM a pulso. `DataPoint.StepTypeName` es `string?` (como el legacy) | Paridad | **Aceptado** (QA-11): el PO reescribió AC-RUT-17-01 igual que el legacy |
| D-13 | `routineSummaryWorkRest` queda como **un** mensaje con dos plurales | El formateador propio lo admite (la spec lo permitía) | — |
| D-14 | AC-RUT-01-05: el error es `FormatException` | Tipo concreto para el test | — |

### 10.3 Respuestas a las preguntas abiertas de la spec

- **P-04 (mínimos de Unity 6000.3, verificado en el manual de 6.3, "System requirements")**: *player* de Windows: **Windows 10 21H1 (build 19043) o más nuevo, x64**; macOS: **12 Monterey o más nuevo**; Android: 7.1 (API 25) o más (la app pide 26, Q-90); iOS: 15 (= legacy). Pérdidas frente al legacy: **macOS 10.15 y 11** y **Windows 10 anteriores a 21H1**. **Agendada por el PO** (QA-12): decisión del usuario en el gate de la It. 2 (Windows) y en el de la 10b (macOS); por defecto se adopta.
- **P-05**: método fijado en §4.4 (mezcla sRGB sin redondear; pasan los tres tokens al límite). Queda abierto R-T1 (mezcla lineal real de Unity) para Designer + Architect.
- **P-06**: en una app de Unity, los textos del sistema son los del teclado nativo y su menú de copiar/pegar (Android `EditText` del teclado de Unity, iOS `UITextField`), que **siguen el idioma del sistema**, no el de la app; Unity no dibuja menús propios. Diferencia con el legacy: Flutter dibuja su barra de copiar/pegar con el idioma de la app, así que con el sistema en portugués el legacy muestra esa barra en inglés y la app nueva en portugués. No hay textos de accesibilidad propios del sistema (los de la app salen del catálogo). **Aceptada por el PO** como diferencia conocida (QA-12; AC-APP-09-07 reescrito). Se verifica en la **It. 3** [Device], con el primer campo de texto (editor de rutinas); el usuario puede pedir una barra de edición propia en el gate de la It. 2.
- **P-08**: los documentos se alinean en el cierre (§10.4): ARCHITECTURE RA-07 pasa a `glass.fallback` (`#070D16 @86 %`); ART_BIBLE §9 la corrige el Designer.

### 10.4 Actualizaciones de documentos (cierre de la It. 1)

| Documento | Cambio | Quién |
|---|---|---|
| ADR 012 §4, ADR 013, ARCHITECTURE §2 y §15.1 | Ruta de vectores (D-01); `run-oracle.ps1` con `package_config` (§6.1) | Architect (WP-A) |
| ARCHITECTURE §12, ADR 011 | Algoritmo de Flutter (D-06) | Architect |
| ARCHITECTURE §6, §17, §19, RA-07 | `IClock` con `DateTimeOffset` (D-11), `IAppLogger` (D-03), higiene como tests (D-04), `glass.fallback` (P-08) | Architect |
| ADR 016 | API y formato de `Tokens.cs` (§4.2-§4.4), roles de contraste, método de P-05, R-T1 | Architect |
| LEGACY_MAPPING §3 | `tools/l10n` en Dart (D-05); `Icons`/`MetricPalette` a la It. 2 (D-09) | Architect |
| ARCHITECTURE §15.1, ADR 013 | `run-tests.ps1` = `dotnet build` de la solución + `dotnet test --no-build` (QA-01); `UnityApiCheck` contra NUnit 3.5.0 con Support por glob (QA-10); fuentes de test sin `record`/`init` (QA-03); formato de tabla y tope de 1 MiB de los vectores (QA-06) | Architect |
| ARCHITECTURE §16 (punto 1) y §21 (Q-85 / R-14) | Windows sin identificador en el motor; `com.iguisoft.rowmate` reservado para el MSIX de la It. 14 (igual que AC-APP-16-02, QA-02) | Architect |
| ART_BIBLE §9 | Diferencia ya resuelta con ARCHITECTURE §20.1 (P-08) | Designer |

---

## Cambios por gate QA

Gate de armonía: [03-qa-harmony.md](03-qa-harmony.md) (APROBADO CON OBSERVACIONES). Issues asignados al Architect: QA-01 (mayor) y QA-03 a QA-10, QA-13 y QA-14 (menores). **Los 11 se resuelven; ninguno se disputa.** No hay bloqueantes. El número de AC (139), la matriz AC → test (139/139) y los totales de archivos (A 139, B 22, C 60 = 221) no cambian. Toda prueba se hizo en el *scratchpad* de la sesión, fuera de los dos repos; el legacy quedó con `git status --porcelain` vacío antes y después, y no se leyó `lib/core/strava/strava_secrets.dart`.

| ID | Sev. | Decisión | Qué cambió | Verificación |
|---|---|---|---|---|
| QA-01 | mayor | **Resuelto** con la primera opción de QA: `run-tests.ps1` corre `dotnet build tools/dotnet/RowMate.sln` y después `dotnet test … --no-build` con el mismo `--artifacts-path`, y propaga el primer código ≠ 0 (si el *build* falla, no corre tests). Se descartó la `ProjectReference` con `ReferenceOutputAssembly="false"`: ataría `Tests.EditMode` a las herramientas y metería NUnit 3.5 en su restauración | §0 (puntos 2 y 6), §2.1, §2.4, §2.6 (dos comandos, pasos del script, `CiWorkflowTests.ScriptBuildsWholeSolutionThenTestsWithoutRebuilding`), §8.4 (AC-APP-19-02/-04), §8.5 (AC-BASE-08 nombra el mecanismo), §9, R-T6, D-10, §10.4 | Réplica del grafo con SDK 8.0.427: `dotnet test` solo, con un `record` en una fuente de test → `exit 0` (reproduce QA); el script en Windows PowerShell 5.1 → `CS0518` en `UnityApiCheck`, no corre tests, `exit 1`; todo verde → `exit 0` |
| QA-03 | menor | **Resuelto** con la regla (primera opción): ninguna fuente de `Tests/` declara `record` ni `init`; `Tests.Support` pierde su *shim* (con el *glob* de QA-10 lo habría metido en `UnityApiCheck` y tapado el error) | §1.1, §2.3, §2.4 (regla y `NUnitCompatibilityTests.NoRecordOrInitInTestSources` con dos regex sobre el texto sin comentarios ni literales), §2.5, §9.0, §9.1 (*shims* 3) | `CS0518` reproducido en `UnityApiCheck`; usar los records de Core (`{ … }` y `with`) compila; las regex se probaron con 9 positivos y 9 negativos (`var record = …`, `return record with { … }`, `x = init;`) |
| QA-04 | menor | **Resuelto**: para cada *p*, piso y techo; los que entran en el intervalo; el más cercano; empate → par. Intervalo asimétrico solo si `f == 0 && be > 1` | §3.1 (algoritmo en 5 pasos y vectores), §6.3 (`toStringPow2` con las 2 098 potencias y `toStringTies` con 500 empates), `GoldenCoverageTests`, §9.1 | Implementación C# 9 / `netstandard2.1` contra `toString()` de Dart 3.13.5: **0 diferencias en 982 699 `double`** (103 983 empates). El algoritmo anterior: 92 diferencias en las potencias de 2 (las 46 de QA, con los dos signos) y 50 000 en los empates |
| QA-05 | menor | **Resuelto**: `WorkoutSessionRecord(string startedAt)`; `Routine` asigna `Name` en el constructor e inicializa `Description = ""` y `Steps = Array.Empty<…>()`; regla general para records de Core | §3.3 (firmas y regla), §9.3 | Firmas compiladas con las reglas de Core (`netstandard2.1`, C# 9, `Nullable`, `TreatWarningsAsErrors`): 0 advertencias |
| QA-06 | menor | **Resuelto** con barridos más chicos y formato de tabla (una fila por línea, `double` como 16 hex); el tope de 1 MiB **se mantiene** y se testea | §2.5 (`GoldenTable`, `GoldenRow`, `GoldenVectors.Rows`, `AsF64` con las dos formas; un test por barrido), §6.2, §6.3, R-T11, §8.5 (AC-BASE-03), §9 (`GoldenTable.cs` en WP-A, sin cambiar los totales) | Generador prototipo de las secciones de `fmt.json`: **633 404 bytes** (antes 3,10 + 4,49 MB) |
| QA-07 | menor | **Resuelto**: `int` para `count`, `work`, `rest`, `m`, `s`, `meters`, `min`, `max`, `spm`; `String` para `split`, `list`, `delta` (lo que propuso QA) | §3.3 (tabla con la línea del legacy de cada uno), §8.4 (AC-APP-10-04/-07), §9.3 | Releído `interval_step.dart:20-45`, `:67-106` y `routine.dart:100-104`; formato de metadatos igual a `app_es.arb:133-137` |
| QA-08 | menor | **Resuelto**: se acepta el pedido del Designer como datos del test | §4.4 (`ContrastTests.DesignerPairs`, composición de fondos y frentes con alfa), §8.5 (AC-BASE-05), §9.1 | Los 44 tokens que nombran los pares (incluido `glass.fallback`) existen una vez cada uno en `Tokens.cs`; recalculados 3,159 · 4,639 · 5,140 · 5,474 · 5,077 (= Designer y QA) |
| QA-09 | menor | **Resuelto**: lectura del texto, no reflexión; resultado real desde los `.trx` | §2.4 (categorías y nombres siempre literales), §8.5 (AC-BASE-09), §8.6 | — (regla de diseño) |
| QA-10 | menor | **Resuelto** con la primera opción: `UnityApiCheck` compila contra el paquete NUnit 3.5.0 (NuGet elige `lib/net45` por *AssetTargetFallback*; `NU1701` silenciado) con `Tests/Support` por *glob*. La lista de `NUnitCompatibilityTests` queda para lo que compila en 3.5 pero no se admite | §2.1, §2.3 (csproj y resultado de la prueba), §2.4, R-T6, R-T7, D-10 | Fallan al compilar `SetArgDisplayNames`, `AddTestAttachment`, `Is.Not.Default`, `Is.AnyOf`, `Assert.Multiple`, `Warn`, `[NonParallelizable]`, `[DefaultFloatingPointTolerance]`; compilan las ~35 APIs que usa el diseño. Residual NV: el NUnit propio de Unity, en la It. 2 |
| QA-13 | menor | **Resuelto** | §8.1 (`Demo` en los 8 casos de `ReferencePackets`), §8.7 (línea exacta de cada test de la demo y `TraceabilityTests.DemoCoversSpec`), §2.6, §9.2-§9.3 | `TestContext.Out.WriteLine` compila en NUnit 3.5 y, con `console;verbosity=detailed`, la línea sale en la consola de un test que pasa |
| QA-14 | menor | **Resuelto** y ampliado: además de comparar `status --porcelain --ignored`, se lista archivo por archivo (`--untracked-files=all`) y se compara una instantánea de metadatos, que detecta escrituras dentro de carpetas ya ignoradas; todo `git` con `--no-optional-locks`; la conversión de catálogos (`-L10n`) corre bajo la misma guardia | §6.1 (pasos 2 y 5, `-L10n`), §9.3, §9.4 | En el legacy: `--porcelain` 0 líneas, `--ignored` 10, `--ignored --untracked-files=all` 591; 863 archivos fuera de `.git/` (solo metadatos, sin abrir archivos) |

**Alineación con la spec corregida por el PO** (QA-02, QA-11, QA-12): `AppIdentity.WindowsApplicationId` queda como dato **reservado** para el MSIX de la It. 14, que el *bootstrapper* no usa (`BootstrapperStaticTests` lo verifica) y `AppleBundleId` documenta que también es el id único de Standalone (§3.1, §1.5); fila AC-APP-16-02 de §8.4 con el texto nuevo; D-02 y D-12 aceptados (§10.2); `Ignore` de AC-APP-09-07 → It. 3 y respuestas P-04/P-06 agendadas (§8.4, §10.3); §10.4 suma ARCHITECTURE §16/§21, como pidió el PO.

**Otros cambios chicos**: errata C-10 de QA (el comentario de `IClock.LocalNow` remite a D-11, no a D-12). `run-tests.ps1` y `run-oracle.ps1` **solo ASCII**: la prueba del script mostró que Windows PowerShell 5.1 lee un `.ps1` UTF-8 sin BOM como ANSI y deforma los acentos de los mensajes (§2.6, §6.1).
