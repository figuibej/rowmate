# It. 1 — Fundaciones: verificación de QA

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-27 |
| Iteración | 1 · `01-fundaciones` ([ROADMAP §2, It. 1](../../ROADMAP.md)) |
| Contra | [01-po-spec.md](01-po-spec.md) (139 AC + 19 LT), [02-tech-design.md](02-tech-design.md), [03-qa-harmony.md](03-qa-harmony.md), [04-design-notes.md](04-design-notes.md) |
| Legacy | `rowmate-legacy` @ `3ae0f3a8bed5966700e830db3d8a5f050f34a0bc`, solo lectura. `git status --porcelain` vacío antes y después. No se leyó `lib/core/strava/strava_secrets.dart` |
| Herramientas | .NET SDK 8.0.427 (`C:/Program Files/dotnet/dotnet.exe`), Dart 3.13.5 (`<flutter-sdk>/bin/cache/dart-sdk/bin/dart.exe`). Unity Editor **no instalado** |
| Todo lo generado | En el *scratchpad* de la sesión (`…/scratchpad/qa-r1`, `qa-r1-matrix`, `oracle-regen`, `qa-probe`). No se tocó ningún archivo del proyecto salvo este reporte |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.**

- Compilación: 9 proyectos, **0 advertencias, 0 errores** (Debug y Release).
- Tests: **912** (843 EditMode + 69 Simulator): **894 pasan, 0 fallan, 18 ignorados** (todos `[Ignore("NO VERIFICABLE → It. N …")]`, uno o más por AC con parte [Editor]/[Device]/[Apple]/[CI]).
- AC: **131 PASS · 0 FAIL · 8 NO VERIFICABLE** (de 139). Los 8 NV son los que la spec define solo como [Device]/[Apple]/[Editor]/[CI]; su parte estática se revisó y pasa (§2).
- Los **19 casos legacy** están portados 1 a 1, con el nombre original, el comentario de origen y los mismos casos y valores (§3).
- Vectores del oráculo: **regenerados por QA** con Dart 3.13.5 sobre el legacy, en el *scratchpad*: los 8 JSON dan **idénticos byte a byte** a los versionados (salvo `generatedAt`) (§4).
- Issues: **0 bloqueantes, 0 mayores, 11 menores** (§6). Ninguno impide cerrar la It. 1. QA-V-01, QA-V-03 y QA-V-05 conviene resolverlos antes de que la It. 2 los use.

---

## 1. Corrida real de build y tests

### 1.1 Comandos pedidos (Debug, `--artifacts-path …/scratchpad/qa-r1`)

```
"C:/Program Files/dotnet/dotnet.exe" build tools/dotnet/RowMate.sln --artifacts-path …/scratchpad/qa-r1 --no-incremental
  RowMate.Core -> …\qa-r1\bin\RowMate.Core\debug\RowMate.Core.dll
  RowMate.App -> …\qa-r1\bin\RowMate.App\debug\RowMate.App.dll
  RowMate.Infrastructure -> …\qa-r1\bin\RowMate.Infrastructure\debug\RowMate.Infrastructure.dll
  RowMate.Tokens -> …\qa-r1\bin\RowMate.Tokens\debug\RowMate.Tokens.dll
  RowMate.Simulator -> …\qa-r1\bin\RowMate.Simulator\debug\RowMate.Simulator.dll
  RowMate.Tests.Support -> …\qa-r1\bin\RowMate.Tests.Support\debug\RowMate.Tests.Support.dll
  RowMate.Tests.UnityApiCheck -> …\qa-r1\bin\RowMate.Tests.UnityApiCheck\debug\RowMate.Tests.UnityApiCheck.dll
  RowMate.Tests.Simulator -> …\qa-r1\bin\RowMate.Tests.Simulator\debug\RowMate.Tests.Simulator.dll
  RowMate.Tests.EditMode -> …\qa-r1\bin\RowMate.Tests.EditMode\debug\RowMate.Tests.EditMode.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

"C:/Program Files/dotnet/dotnet.exe" test tools/dotnet/RowMate.sln --artifacts-path …/scratchpad/qa-r1 --logger "trx;LogFilePrefix=qa" --results-directory …/qa-r1/results
Passed!  - Failed:     0, Passed:    69, Skipped:     0, Total:    69, Duration: 53 ms - RowMate.Tests.Simulator.dll (net8.0)
  Skipped RealPlatformScan, NoSelectorOnScreen, SystemTextsFollowSystemLanguage, VisibleChangeWithoutRestart,
          NativeListOnDevices, StartDialogOnScreen, DeviceGridShowsTheTextsOnScreen, EditorSubtitleOnScreen, IconsInEditor,
          AndroidMinimumOnDevices, InstallsNextToTheLegacyApp, IosMinimumOnDevices, VisibleNameOnDevices,
          WindowsDataFolderAndCoexistence, RunsOnFirstOpen, RedJobOnGitHub, RunsOnGitHub, ProjectOpensWithoutCompileErrors
Passed!  - Failed:     0, Passed:   825, Skipped:    18, Total:   843, Duration: 1 s - RowMate.Tests.EditMode.dll (net8.0)
```

### 1.2 El comando documentado (`tools/dotnet/run-tests.ps1`, Release)

| Corrida | Resultado |
|---|---|
| `run-tests.ps1 -Matrix -ArtifactsPath …/qa-r1-matrix` | `build` 0 advertencias / 0 errores → `test --no-build`: 69/69 y 825 + 18 ignorados de 843. Matriz: **PASA 157, FALLA 0, IGNORADO 1, SIN TEST 0 (de 158 IDs = 139 AC + 19 LT)**. El único IGNORADO es AC-APP-09-07 (NV por spec). `exit 0` |
| `run-tests.ps1 -Demo` | Salen las líneas `[DEMO]` de la spec §1 punto 2: los 8 paquetes FTMS (`spm=24.0 paladas=298 m=482 split=133 (02:13) W=150 …`), el simulador a 150 W / 24 spm en el tick 60 (`m=226 … kcal=13 bpm=142 t=60`), los 9 resúmenes es/en (`1 serie · 1 descanso` / `1 interval · 1 rest`) y el P99 (`W=198 spm=29.8 split=198 (03:18)`). Cada línea sale dos veces (QA-V-07). `exit 0` |
| `run-tests.ps1 -Ci -Filter "FullyQualifiedName~RepoHygiene"` | 20/20 con `ROWMATE_CI=1` (git disponible). `exit 0` |
| `dotnet test … --no-build --filter "TestCategory=AC-BLE-02-02\|TestCategory=LT-16"` | 5 tests (filtrar por ID de AC/LT funciona) |

---

## 2. Matriz AC → test → resultado (139)

Criterio de QA: **PASS** = la parte [dotnet] del AC está probada por los tests indicados (leídos uno por uno contra la spec y el legacy) y pasan; cuando el AC tiene además una parte en pantalla o en equipo, esa parte figura como "NV → It. N" con su `[Ignore]` y no cambia el PASS, como define la spec §0.1 y §12. **NV** = el AC solo se verifica en [Device]/[Apple]/[Editor]/[CI]; QA hizo la revisión estática y no encontró defectos. Nombres de test abreviados; todos con `[Category("<AC>")]`.

### 2.1 BLE (37: 37 PASS)

| AC | Test(s) | Resultado |
|---|---|---|
| AC-BLE-01-01 | `RowingDataTests.EmptyReadingIsZeroWithDashesAndZeroTime`, `EightFieldsAndOnlyStrokeRateIsDecimal`, `EmptyMatchesOracle` | PASS |
| AC-BLE-01-02 | `RowingDataTests.SplitText` (13 casos de la tabla) + `MatchesOracle_Split` (−10…7000 y bordes); `TimeFormatsTests.SplitOrDashes` | PASS |
| AC-BLE-01-03 | `RowingDataTests.ElapsedText` (10) + `MatchesOracle_Elapsed`; `TimeFormatsTests.Elapsed` | PASS |
| AC-BLE-01-04 | `RowingDataTests.CopyWithChangesOnlyNamedField`, `EachNamedFieldChangesAlone`, `CopyWithMatchesOracle` | PASS |
| AC-BLE-01-05 | `RowingFormatsTests.DeviceGridTexts`, `MatchesOracle_GridTexts` | PASS (regla) · pantalla NV → It. 2 |
| AC-BLE-01-06 | `RowingFormatsTests.UsesSharedTimeRules`, `NoOwnTimeFormattingInRowingSources`; `SessionStatsTests.FormatsUseSharedTimeRules` | PASS |
| AC-BLE-02-01 | `FtmsParserTests.Uuids`, `UuidsMatchOracle` (0x2AD1, no 0x2AD2) | PASS |
| AC-BLE-02-02 … -09 | `FtmsParserTests.ReferencePackets` (8 casos, bytes y valores de la tabla, `Demo`), `ReferenceSplitTexts`, `MatchesOracle` | PASS (8) |
| AC-BLE-02-10 | `StrokeRateIsHalfRaw` (`30`→24.0, `F6`→123.0, los 256 bytes) | PASS |
| AC-BLE-02-11 | `SignedPowerAndAverageOnlyIfZero` (`21 00 F6 FF`→−10, `61 00 00 00 64 00`→100, `61 00 32 00 64 00`→50) | PASS |
| AC-BLE-02-12 | `SkippedFields` (los 4 paquetes de la spec) | PASS |
| AC-BLE-02-13 | `TruncatedAndTrailingBytes` (3), `NoPacketThrows` (> 400 000 paquetes), `MatchesOracle_Fuzz` (1 000) | PASS |
| AC-BLE-02-14 | `RawUnavailableValues` (65535 kcal, tiempo, distancia, split; potencia negativa) | PASS |
| AC-BLE-02-15 | `MissingFieldIsZeroNotPrevious` | PASS |
| AC-BLE-02-16 | `EmptyNotificationGivesNoReading` | PASS |
| AC-BLE-03-01 | `ScanFilterTests.ClientSideOnlyOnWindows` | PASS (regla) · plataforma real NV → It. 10a/11, [Apple] 10b |
| AC-BLE-03-02 | LT-08 + `AnyAdvertisedServiceIsEnough`, `ResultWithoutServiceListIsNotAnAdvertiser` | PASS |
| AC-BLE-03-03 | LT-09 | PASS |
| AC-BLE-03-04 | `BleUuidTests.ShortThirtyTwoAndLongFormsAreEqual` (las 5 formas de `180d` y las de `1826`), `ScanFilterTests.EveryFormOfTheServicePasses`. Semántica comparada contra `flutter_blue_plus_platform_interface` 9.0.3 `guid.dart` (pub cache): igualdad por `str128` | PASS |
| AC-BLE-03-05 | `SameRuleForRower` | PASS |
| AC-BLE-14-01 | `RowingSimulatorTests.InitialValues`, `InitialValuesMatchOracle` | PASS |
| AC-BLE-14-02 | `TargetsAreClamped_Watts`/`_Spm` (18 casos), `TargetsAreClamped` (oráculo) + LT-05 | PASS |
| AC-BLE-14-03 | `Concept2Split` (los 8 W de la tabla) + LT-01, LT-02 + `MatchesOracle_Pace` (1-600 W, `Within(1e-9)`) | PASS |
| AC-BLE-14-04 | `Concept2Calories` (8) + `MatchesOracle_Pace` (bit a bit) | PASS |
| AC-BLE-14-05 | `RowingWithoutNoise` (ticks 1/60/120/600 y los 12 pulsos, `Demo`) + LT-03 + `MatchesOracle_NoNoise600`, `_NoNoiseTargets` | PASS |
| AC-BLE-14-06 | `StopAndResume` (115 bpm; 45 m, 12 s, 118 bpm al volver) + LT-04 + `MatchesOracle_StopResume` | PASS |
| AC-BLE-14-07 | `HeartRateTargetAndApproach` (8 W y valores estables), `HeartRateTargetUsesTheNoisyPowerOfThatTick` | PASS |
| AC-BLE-14-08 | LT-07 + `ExposedHeartRateIsTheReadingOne` (77) | PASS |
| AC-BLE-14-09 | `NoiseRanges` (1 000 ticks; extremos de u), `NoiseCallOrderIsWattsThenSpm` + LT-06 + `MatchesOracle_NoiseReplay` | PASS |
| AC-BLE-14-10 | `OutputRounding` (24,25→24.3; 23,95→23.9; 23,99999999999998→24; 75,5→76 y 88,5→89) | PASS |

### 2.2 HRM (9: 9 PASS)

| AC | Test(s) | Resultado |
|---|---|---|
| AC-HRM-01-01 | LT-15 + `ProfileUuidsAsBleUuid`, `UuidsMatchOracle` | PASS |
| AC-HRM-01-02 | LT-10 + `SpanAndArrayGiveTheSameReading`, `MatchesOracle` | PASS |
| AC-HRM-01-03 | LT-11 + `MatchesOracle` | PASS |
| AC-HRM-01-04 | LT-12 + `MatchesOracle` (pulsos 70 y 0 incluidos) | PASS |
| AC-HRM-01-05 | LT-13 + `MatchesOracle` | PASS |
| AC-HRM-01-06 | LT-14 + `Uint16WithTwoBytesDoesNotFallBackToUint8` (256 banderas), `MatchesOracle_Fuzz` (500) | PASS |
| AC-HRM-01-07 | `NoRangeValidation` | PASS |
| AC-HRM-01-08 | `ReservedBitsAndTrailingBytes` | PASS |
| AC-HRM-01-09 | `Uint16WithContact` | PASS |

### 2.3 RUT (58: 58 PASS)

| AC | Test(s) | Resultado |
|---|---|---|
| AC-RUT-01-01 | `StepTypeTests.FourTypesInSelectorOrderWithStorageNames` | PASS |
| AC-RUT-01-02 | `StepTypeTests.NamesFromCatalog` (4 × es/en) | PASS |
| AC-RUT-01-03 | `StepTypeTests.ColorsComeFromTokens` (`""`, `sprint`, `null` → `#607D8B`) | PASS |
| AC-RUT-01-04 | `StepTypeTests.IconNames` (`whatshot`, `fitness_center`, `pause_circle_outline`, `ac_unit`) | PASS (dato) · editor NV → It. 3 |
| AC-RUT-01-05 | `UnknownNameIsError` (6 casos), `NullNameIsError` | PASS |
| AC-RUT-02-01 | `IntervalStepTests.Attributes` | PASS |
| AC-RUT-02-02 | `IntervalStepTests.GoalKind` | PASS |
| AC-RUT-02-03 | `IntervalStepTests.CopyWithNeverClears` | PASS |
| AC-RUT-02-04 | `IntervalStepTests.NoValidation` (`>0W`, `-10-5W`, `0 spm`) | PASS |
| AC-RUT-03-01 | `StepLabelsTests.DurationByTime` (los 10 de la tabla) + `MatchesLegacyDurationSweep` | PASS |
| AC-RUT-03-02 | `DurationByDistance` (3) | PASS |
| AC-RUT-03-03 | `DurationNoneAndBoth` | PASS |
| AC-RUT-03-04 | `DurationNegatives` (−30→`30s`, −60→`0s`, −90→`30s` y más) | PASS |
| AC-RUT-03-05 | `DurationSameInBothLanguages`, `MatchesLegacyLabels` (oráculo, es y en) | PASS |
| AC-RUT-04-01 | `Targets` (la tabla completa), `MatchesLegacyLabels`, `MatchesLegacyTargetFuzz` | PASS |
| AC-RUT-04-02 | `Progressions` (incluye `20 spm · 📈 SPM -1/rep` y la progresión sin objetivo) | PASS |
| AC-RUT-04-03 | `FullLine` | PASS |
| AC-RUT-04-04 | `SplitWithoutFloor` (`00:00`, `00:55`, `-1:55`) | PASS |
| AC-RUT-04-05 | `ExpandedStepKeepsProgression` (`150-170W · 📈 W +10/rep`) | PASS |
| AC-RUT-04-06 | `EditorSubtitle` (`5m␣␣·␣␣18 spm · 📈 SPM +2/rep`) | PASS (regla) · editor NV → It. 3 |
| AC-RUT-05-01 | `RoutineTests.AttributesAndDefaults`, `StepsAreTheTemplateNotTheExpandedRoutine` | PASS |
| AC-RUT-05-02 | `RoutineSummaryTests.SummaryTable` (9 plantillas: columna Legacy, es y en; `Demo`), `CountsMatchLegacy` | PASS |
| AC-RUT-05-03 | `RoutineSummaryTests.WorkoutStartContent` | PASS (texto) · diálogo NV → It. 4a |
| AC-RUT-05-04 | `CountsMatchLegacy` (oráculo), `CountsUseTheTemplateNotTheExpansion` | PASS |
| AC-RUT-06-01 | `RoutineExpansionTests.T1ExpandsToEight` + LT-16 + `MatchesOracle` | PASS |
| AC-RUT-06-02 … -13 | `Case1SpmProgression`, `Case2WattsRange`, `Case3MinOnly`, `Case4Split`, `Case5ProgressionWithoutTarget`, `Case6LooseStepNotProgressed`, `Case7EmptyOrNonPositiveRepeats`, `Case8ContiguousSeries`, `FirstStepRepeatsRule`, `SameIdNotConsecutive`, `NoFloor`, `OtherFieldsKept` + `MatchesOracle` (plantillas con nombre y fuzz) | PASS (12) |
| AC-RUT-07-01 | LT-16 + `AlwaysAligned` | PASS |
| AC-RUT-07-02 | `RoutinePositionsTests.T1Complete` | PASS |
| AC-RUT-07-03 | `EmptyOrZeroRepeats` | PASS |
| AC-RUT-07-04 | `ContiguousSeries` | PASS |
| AC-RUT-07-05 | `AlwaysAligned` (todas las plantillas de §4.11 + golden) | PASS |
| AC-RUT-16-01 | `WorkoutSessionTests.AttributesAndDefaults`, `RecordMapsEveryColumn` | PASS |
| AC-RUT-16-02 | `ClosedStoresP99AndMaxima` (igual a `workout_provider.dart:276-292`) | PASS |
| AC-RUT-16-03 | `Duration` (59→`00:59`, 3661→`1:01:01`), `FormatsMatchOracle` | PASS |
| AC-RUT-16-04 | `DatesUtcIsoWithMicrosOnlyIfNonZero`, `LocalConversionUtcMinus3`, `MatchesOracle`, `IsoDateTests.*` (fmt.json `iso8601*`, `isoParse*`) | PASS |
| AC-RUT-16-05 | `TolerantRead`, `MatchesOracle` (`fromMap`) | PASS |
| AC-RUT-17-01 | `DataPointTests.AttributesAndDefaults`, `StepTypeIsStoredByName` (4) | PASS |
| AC-RUT-17-02 | `StepIndexRefersToExpandedRoutine` (índice 3, `work`) | PASS |
| AC-RUT-18-01 | `SessionStatsTests.NoPoints` + LT-19 + `MatchesOracle` | PASS |
| AC-RUT-18-02 | `P99Index` (los 9 n de la spec; 1…361 → 357) + golden 1-1100 | PASS |
| AC-RUT-18-03 | `P99Examples` (`Demo`), `MatchesOracle` | PASS |
| AC-RUT-18-04 | `P99Decimals` (24.5; 198 W / split 198 / 29.8 spm) | PASS |
| AC-RUT-18-05 | `TotalsAreMaxima` (2000 m, 615 s, 120 kcal) | PASS |
| AC-RUT-18-06 | LT-17 + `MatchesOracle` | PASS |
| AC-RUT-18-07 | LT-18 + `MatchesOracle` | PASS |
| AC-RUT-18-08 | `HeartRateRounding` ([120, 121] → 121) | PASS |
| AC-RUT-18-09 | `Formats`, `FormatsMatchOracle` | PASS |

### 2.4 APP (25: 17 PASS, 8 NV)

| AC | Test(s) | Resultado |
|---|---|---|
| AC-APP-09-01 | `LocaleResolverTests.VariantsMapToLanguage`, `LanguageTagParts` | PASS |
| AC-APP-09-02 | `FlutterParityTable` (las 17 filas, † incluidas), `FlutterAlgorithmDetails`. QA comparó `LocaleResolver.cs` línea a línea con `basicLocaleListResolution` (`flutter/packages/flutter/lib/src/widgets/app.dart:146-238`) | PASS |
| AC-APP-09-03 | `FallbackIsEnglish` | PASS |
| AC-APP-09-04 | `LanguageStateTests.NoSetterNoPersistence`, `ConstructorAcceptsTheSameInputsAsTheResolver` | PASS (regla) · NV → It. 2 |
| AC-APP-09-05 | `RefreshNotifiesChange`, `SubscribeEmitsCurrentLanguage` | PASS (resolución) · visible NV → It. 2 |
| AC-APP-09-06 | `UsesWholeList` (estático); `NativeListOnDevices` ignorado | **NV** → It. 2 [Device]; [Apple] 10b |
| AC-APP-09-07 | `SystemTextsFollowSystemLanguage` ignorado | **NV** → It. 3 [Device] (P-06 aceptada) |
| AC-APP-10-01 | `CatalogParityTests.SameKeysInBothLanguages` (174/174), `FilesAreUtf8LfWithoutBom` | PASS |
| AC-APP-10-02 | `UnchangedLegacyTextsAreIdentical` (155 × es/en contra `l10n_legacy.json`), `SpecialCharacters` | PASS (ver QA-V-06) |
| AC-APP-10-03 | `FourChangedKeys` | PASS |
| AC-APP-10-04 | `SameParametersNamesAndTypes` + `LocalizerTests.ArgumentsMustMatchMetadata` | PASS |
| AC-APP-10-05 | `LocalizerTests.TextsWithParameters` | PASS |
| AC-APP-10-06 | `LocalizerTests.Plurals`, `UploadPlurals` | PASS |
| AC-APP-10-07 | `FifteenNewKeys` (QA releyó las 15 en los dos catálogos: idénticas a §4.17.2) | PASS |
| AC-APP-10-08 | `NoLegacyKeyLost` | PASS |
| AC-APP-10-09 | `LocalizerTests.MissingKeyThrows`, `L10nKeysTests.*` | PASS |
| AC-APP-16-01 | `AppIdentityTests.NameCompanyVersion`, `BootstrapperStaticTests.UsesAppIdentity` (estático) | **NV** → It. 2 [Editor]/[Device]; [Apple] 10b |
| AC-APP-16-02 | `ProvisionalIdsAndNoComExample`, `BootstrapperStaticTests.DoesNotApplyTheReservedWindowsId` | PASS (estático [QA]) · carpeta de datos y convivencia NV → It. 2 |
| AC-APP-16-03 | `IdsDifferFromLegacy` (estático) | **NV** → It. 2 [Device]; [Apple] 10b |
| AC-APP-16-04 | `AndroidMin26Arm64`, `PlatformMinimumsComeFromAppIdentity` (estático) | **NV** → It. 2 [Device] |
| AC-APP-16-05 | `IosMin15` (estático) | **NV** → It. 10b [Apple] |
| AC-APP-19-01 | `CiWorkflowTests.RunsOnPushAndPrOnWindowsAndLinuxWithoutUnityDartOrSecrets` (estático); §5.7 | **NV** (sin remoto, P-03) |
| AC-APP-19-02 | `FailingTestFailsJob` (estático) | **NV** (sin remoto, P-03) |
| AC-APP-19-03 | `RepoHygieneTests.*` (20/20 también con `-Ci`), `PureCodeRulesTests.*`, `TokensUssSyncTests.UssMatchesTokens` | PASS (ver QA-V-02) |
| AC-APP-19-04 | `LocalEqualsCi`, `ScriptBuildsWholeSolutionThenTestsWithoutRebuilding`, `ScriptsAreAsciiForWindowsPowerShell51`; corrida real §1.2 | PASS |

### 2.5 BASE (10: 10 PASS)

| AC | Test(s) | Resultado |
|---|---|---|
| AC-BASE-01 | `FmtFixedTests.SpecTable`, `DartMathTests.SpecTable`, `RoundToFixed`, `MatchesOracle*` (fmt.json: `toStringAsFixed`, `round`, `parseFixed`, `toString`, 2 098 potencias de 2, 500 empates) | PASS · en Unity NV → It. 2 |
| AC-BASE-02 | `CultureIndependenceTests.*`, `RowingFormatsTests.CultureIndependent` (es-AR, de-DE, fr-FR, sv-SE) | PASS |
| AC-BASE-03 | `GoldenCoverageTests.*` + todos los `MatchesOracle*`; **regeneración de QA idéntica** (§4) | PASS |
| AC-BASE-04 | `TokensContractTests.LegacyContractHex` (los 21 colores de la tabla), `TokenCatalogTests.*`, `TokensUssSyncTests.UssMatchesTokens`, `PureCodeRulesTests.NoColorLiteralsOutsideTokens` | PASS |
| AC-BASE-05 | `ContrastTests.*` (método de P-05: mezcla sRGB sin redondear; `metric.split.hud` 7,004, `metric.distance.hud` 7,016, `metric.hr.hud` ≥ 4,5) | PASS (margen ≤ 0,02; R-T1 sigue abierto para la 4a) |
| AC-BASE-06 | `FakeSchedulerTests.ThreeSimulatedSecondsInUnderOneRealSecond`, `DeterministicOrder` y 7 más | PASS |
| AC-BASE-07 | `SequenceRandomTests.*`, `RowingSimulatorTests.NoNoiseIsDeterministicAndConsumesNoRandom` | PASS |
| AC-BASE-08 | `PureCodeRulesTests.NoEngineInPureCode`, `AsmdefLintTests.PureAssembliesHaveNoEngineReferences`, `NUnitCompatibilityTests.*`, compilación de `RowMate.Tests.UnityApiCheck` (netstandard2.1 + NUnit 3.5.0) en el paso 1 | PASS · en Unity NV → It. 2 (ver QA-V-05) |
| AC-BASE-09 | `TraceabilityTests.*` (139 AC y 19 LT con test; nombres literales); matriz real de §1.2 | PASS |
| AC-BASE-10 | `AsmdefLintTests.*`, `ManifestAndVersionTests.*`, `BootstrapperStaticTests.*` + revisión estática §5 | PASS (estático) · abrir sin errores NV → It. 2 |

---

## 3. Los 19 casos legacy y su port

QA leyó los 5 archivos de `rowmate-legacy/test/` y su port. En los 19: mismo nombre (`TestName`), comentario `// legacy: test/<archivo>:<línea> '<nombre>'` con la línea correcta de la declaración `test(`, mismas entradas (`noise: false`, objetivos, *fixtures*) y las **mismas aserciones con los mismos valores**. Ninguno agrega ni quita aserciones; los criterios nuevos van en tests aparte.

| LT | Legacy | Port | Casos y valores |
|---|---|---|---|
| LT-01 | `rowing_simulator_test.dart:7` | `RowingSimulatorTests.MoreWattsFasterPace` | `paceForWatts(300) < paceForWatts(150)` — igual |
| LT-02 | `:14` | `TwoHundredThreeWattsIsTwoMinutes` | `closeTo(120, 0.5)` → `Within(0.5)` — igual |
| LT-03 | `:18` | `AccumulatesWhileRowing` | 60 ticks; 150, 24, 133, 60, 24, 226, 13, > 70 — igual |
| LT-04 | `:36` | `NotRowingFreezesTotals` | 10 + 1 ticks, apagar, 1 tick; las 8 aserciones — igual |
| LT-05 | `:54` | `TargetsAreLimitedToTheirRange` | `RowingSimulator()` (con ruido), 9999 → max, 1 → min — igual |
| LT-06 | `:62` | `NoisyWattsWithinFivePercent` | azar real, 200 W, 100 ticks en [190; 210] — igual |
| LT-07 | `:70` | `HeartRateExposesTheLastTickPulse` (método aparte, como fuera del grupo) | `heartRate == d.heartRate` y `> 70` — igual |
| LT-08 | `scan_filter_test.dart:24` | `ClientSideFilterOnlyAcceptsAdvertisers` | `[]`, `[febe]` rechazados; `[180d]` aceptado (con el mismo mensaje); `[febe, hrs]` aceptado — igual |
| LT-09 | `:32` | `WithoutClientSideFilterEverythingPasses` | `[]`, `[febe]` aceptados — igual |
| LT-10 | `heart_rate_parser_test.dart:5` | `Uint8WithoutContactInformation` | `[0x00, 72]` → 72, contacto null — igual |
| LT-11 | `:11` | `Uint16LittleEndian` | `[0x01, 0x2C, 0x01]` → 300 — igual |
| LT-12 | `:16` | `ContactBits` | `[0x02,70]` null, `[0x04,0]` false, `[0x06,65]` true y 65 — igual |
| LT-13 | `:23` | `IgnoresEnergyExpendedAndRrIntervals` | `[0x18, 80, 0x10, 0, 0, 0x04]` → 80 — igual |
| LT-14 | `:29` | `EmptyOrTruncatedGiveNull` | `[]`, `[0x00]`, `[0x01, 72]` → null — igual |
| LT-15 | `:35` | `ProfileUuids` | los 2 UUID — igual |
| LT-16 | `routine_positions_test.dart:6` | `RoutinePositionsTests.LegacyPositionsAlignedWithFlattenedSteps` | T1; largo = expandido = 8; [0], [1], [2], [3], [6], [7] — igual |
| LT-17 | `session_stats_test.dart:7` | `SessionStatsTests.LegacyHeartRateIgnoresZeros` | `_p(hr)` = `DataPoint(1, 0) { HeartRate = hr }`; 137, 150, true — igual |
| LT-18 | `:14` | `LegacyNoHeartRate` | [0, 0] → 0, 0, false — igual |
| LT-19 | `:21` | `LegacyNoPoints` | `[]` → `hasHeartRate` false — igual |

Las implementaciones bajo prueba se compararon con el legacy: `RowingSimulator.cs` ↔ `rowing_simulator.dart:1-83`, `FtmsParser.cs` ↔ `ftms_parser.dart:26-140`, `HeartRateParser.cs` ↔ `heart_rate_parser.dart:24-40`, `ScanFilter.cs`/`BleUuid.cs` ↔ `scan_filter.dart:12-20` y `guid.dart` (FBP 9.0.3), `RoutineExpansion.cs` ↔ `routine.dart:29-97`, `StepLabels.cs` ↔ `interval_step.dart:67-109`, `SessionStats.cs` ↔ `workout_session.dart:111-136`, `WorkoutSession.Closed` ↔ `workout_provider.dart:276-292`, `LocaleResolver.cs` ↔ `app.dart:146-238`. Sin diferencias de regla (salvo el rango de enteros, QA-V-11).

---

## 4. Oráculo Dart y vectores

**Regeneración independiente.** QA corrió los 8 scripts de `tools/oracle/bin/` con Dart 3.13.5 y el `package_config` del oráculo, con `--out` al *scratchpad* (no a `Assets/`) y `--legacy-commit 3ae0f3a…`:

```
[oracle] fmt.json: 683549 bytes        [oracle] routines.json: 298314 bytes
[oracle] ftms.json: 95639 bytes        [oracle] rowing_data.json: 278733 bytes
[oracle] heart_rate.json: 15309 bytes  [oracle] session.json: 419718 bytes
[oracle] l10n_legacy.json: 14217 bytes [oracle] simulator.json: 118846 bytes
diff (sin la línea generatedAt) contra Assets/RowMate/Tests/Golden/oracle/*.json: 0 líneas en los 8 archivos
```

- Los 8 encabezados dicen `dartVersion` 3.13.5 y `legacyCommit` = HEAD actual del legacy.
- Legacy: `git status --porcelain` vacío antes y después; `status --porcelain --ignored --untracked-files=all` **idéntico** antes y después.
- Los scripts importan el código del legacy directamente (`ftms_parser`, `heart_rate_parser`, `rowing_simulator`, `rowing_data`, `interval_step`, `routine`, `workout_session`); los únicos valores "reproducidos" son las 4 expresiones de la grilla de `device_screen.dart:253-258`, que QA comparó con el archivo: son las mismas. `l10n_vectors.dart` lee los `.arb` con `dart:io` (solo lectura). Ningún script ni `run-oracle.ps1` nombra el módulo de credenciales (grep sin resultados).
- Cobertura mínima de AC-BASE-03: presente (`fmt` 15 secciones; `ftms`/`heart_rate` paquetes con nombre + fuzz; `simulator` 7 secciones; `routines` 8; `session` 8; `rowing_data` 8; `l10n_legacy` 3).

---

## 5. Revisión estática de lo que depende de Unity

### 5.1 `Packages/manifest.json` y `ProjectSettings/ProjectVersion.txt`

- `ProjectVersion.txt`: `m_EditorVersion: 6000.3.27f1` y salto final. Correcto (ADR 001, AC-BASE-10).
- `manifest.json`: idéntico a 02-tech-design §1.3 (lo fija `ManifestAndVersionTests`). Versiones exactas; URP y Shader Graph 17.3.0 son los de Unity 6.3; `com.unity.ugui` 2.0.0 es el de Unity 6. `com.gilzoide.sqlite-net` por git con tag `#1.3.3` (exacto por tag; necesita `git` en la PC del primer open). `com.unity.test-framework` 1.6.0 e Input System 1.20.1 no se pueden confirmar sin Unity/red: **NV → It. 2** (si el Editor fija otra versión de un paquete núcleo, la reescribe).
- No hay `.meta`: los genera Unity en el primer open (ADR 014). Todas las referencias entre asmdefs son por **nombre**, así que no dependen de GUID.

### 5.2 Asmdefs (14)

| Asmdef | `noEngineReferences` | `overrideReferences` / precompilados | `defineConstraints` / plataformas | Referencias | Observación |
|---|---|---|---|---|---|
| RowMate.Core | true | true / — | — | — | puro, OK |
| RowMate.App | true | true / — | — | Core | OK |
| RowMate.Infrastructure | false | true / — | — | Core | OK (sqlite-net y Newtonsoft en la It. 3: con `overrideReferences` habrá que listar `Newtonsoft.Json.dll`) |
| RowMate.Simulator | true | true / — | `ROWMATE_SIMULATOR` | Core, App | OK |
| RowMate.Platform | false | false | — | Core, `Unity.InputSystem` | nombre de asmdef del paquete correcto |
| RowMate.Presentation | false | false | — | Core, App, `Unity.InputSystem` | OK |
| RowMate.Scene | false | false | — | Core, App, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.Animation.Rigging` | nombres correctos |
| RowMate.Simulator.Presentation | false | false | `ROWMATE_SIMULATOR` | Core, App, Simulator, Presentation | OK |
| RowMate.Bootstrap | false | false | — | todas las de runtime, incluidas Simulator y Simulator.Presentation | sin el define Unity descarta esas dos referencias; `BuildFlags` usa `#if ROWMATE_SIMULATOR`. OK |
| RowMate.Editor | false | false | `includePlatforms: [Editor]` | + `Unity.RenderPipelines.Universal.Editor` | OK; ver QA-V-04 (namespace) |
| RowMate.Tests.Support | true | true / `nunit.framework.dll` | `UNITY_INCLUDE_TESTS` | Core, App, Infrastructure (no Simulator) | OK |
| RowMate.Tests.EditMode | false | true / `nunit.framework.dll` | `UNITY_INCLUDE_TESTS`, `[Editor]` | Support, Core, App, Infrastructure, `UnityEngine.TestRunner`, `UnityEditor.TestRunner` | assembly de test válido (UTF 1.x) |
| RowMate.Tests.Simulator | false | true / `nunit.framework.dll` | `UNITY_INCLUDE_TESTS`, `ROWMATE_SIMULATOR`, `[Editor]` | + Simulator | OK; ver QA-V-05 |
| RowMate.Tests.PlayMode | false | true / `nunit.framework.dll` | `UNITY_INCLUDE_TESTS` | Support, Core, App, Presentation, Scene, Bootstrap, `UnityEngine.TestRunner` | OK (sin tests en la It. 1) |

Regla de dependencias (ADR 002) respetada: ningún assembly puro referencia uno con motor; nadie de runtime referencia Editor. El grafo de los 7 asmdefs espejo es igual al de los `.csproj` (`AsmdefLintTests.GraphMatchesTheDotnetProjects`).

### 5.3 `ProjectBootstrapper` (4 archivos, solo Unity)

API de Unity 6000.3 revisada una por una: `PlayerSettings.companyName/productName/bundleVersion`, `Android.bundleVersionCode`, `iOS.buildNumber`, `macOS.buildNumber`, `SetApplicationIdentifier(NamedBuildTarget, …)`, `SetScriptingBackend`/`SetApiCompatibilityLevel(NamedBuildTarget, …)` con `ApiCompatibilityLevel.NET_Standard`, `Android.minSdkVersion = (AndroidSdkVersions)26`, `targetSdkVersion = AndroidApiLevelAuto`, `targetArchitectures = ARM64`, `forceInternetPermission`, `iOS.targetOSVersionString`, `colorSpace`, `SetUseDefaultGraphicsAPIs`/`SetGraphicsAPIs(BuildTarget, …)`, `runInBackground`, `enableFrameTimingStats`, `GetScriptingDefineSymbols(NamedBuildTarget)`, `NamedBuildTarget.TargetName`; `SerializedObject` sobre `ProjectSettings.asset` con `activeInputHandler` (1 = Input System); URP: `ScriptableObject.CreateInstance<UniversalRendererData>()`, `UniversalRenderPipelineAsset.Create(ScriptableRendererData)`, `GraphicsSettings.defaultRenderPipeline`, `QualitySettings.SetQualityLevel(i, false)` + `renderPipeline`. Todas existen con esas firmas. `BootstrapState` es `[Serializable]` con campos públicos y `List<Step>`: serializable con `JsonUtility`.

Cumple la spec: toma todo de `AppIdentity`, no aplica `WindowsApplicationId`, no toca `ScriptingDefineSymbols` (solo advierte, R-01), no crea, borra ni renombra niveles de calidad (QA-P-10), `try/catch` por paso con instrucciones manuales, sin diálogos en *batchmode*. Desvío del diseño: reintenta en **cada recarga de dominio**, no "en el próximo open" (QA-V-03). Abrir el proyecto: NV → It. 2.

### 5.4 Globs de los `.csproj`

- Espejos: `Core/**`, `App/**`, `Infrastructure/**`, `Simulator/**`, `Tests/Support/**`, `Tests/EditMode/**`, `Tests/Simulator/**`. `UnityApiCheck` repite los 3 de test. **Todo `.cs` de un assembly puro de `Assets/` queda compilado** (incluidos los 3 *shims* `Internal/IsExternalInit.cs`).
- `.cs` que dotnet no compila (12, todos de assemblies con motor): `Bootstrap/{AssemblyInfo,BuildFlags}.cs`, `Editor/AssemblyInfo.cs`, `Editor/Bootstrap/*.cs` (4), `Platform`, `Presentation`, `Scene`, `SimulatorPresentation` y `Tests/PlayMode` (`AssemblyInfo.cs`). Correcto por diseño. Ningún `.cs` fuera de una carpeta con asmdef (`AsmdefLintTests.NoScriptOutsideAnAssemblyFolder`).
- `Directory.Build.props`: `LangVersion 9.0`, `Nullable enable`, `ImplicitUsings disable` para los 9 proyectos (ningún `.csproj` lo pisa); `TreatWarningsAsErrors` en Core, App, Infrastructure y Simulator.

### 5.5 C# 9

- Todo lo que compila dotnet está forzado a C# 9 por el compilador (`LangVersion 9.0`) y los puros a `netstandard2.1` (sin covariantes de retorno ni `ModuleInitializer`, que Unity no admite). `UnityApiCheck` compila las fuentes de test contra `netstandard2.1` + NUnit 3.5.0.
- *Grep* en `Assets/` y `tools/` (namespaces de archivo, `global using`, `required`, *raw strings*, `record struct`, `file class`, `u8`, `scoped`, `static abstract`, patrones de lista, atributos genéricos, lambdas con tipo natural, `const` interpolados, `ModuleInitializer`, `SkipLocalsInit`, punteros a función): **0 usos reales** (solo literales *verbatim* con `""` y los casos de prueba de `NUnitCompatibilityTests`). Los 12 `.cs` solo de Unity se leyeron enteros: C# 9 válido.
- `Assets/csc.rsp` = `-nullable:enable` (R-T12: si Unity no lo aplicara a los asmdefs, solo habría advertencias CS8632).

### 5.6 `ci-core.yml`

`push`, `pull_request` y `workflow_dispatch`; matriz `ubuntu-latest` + `windows-latest` con `fail-fast: false`; `permissions: contents: read`; `actions/setup-dotnet@v4` con `global-json-file`; corre `./tools/dotnet/run-tests.ps1 -Ci` en `pwsh` (el mismo comando de la PC, AC-APP-19-04) y sube `TestResults/` con `if: always()`. Sin Unity, sin licencia, sin Dart y sin secretos; sin `continue-on-error`. El job queda rojo si falla el `build` o algún test (el script propaga el primer código ≠ 0). Correcto. La corrida real queda NV (P-03: no hay remoto). Nota: con `rollForward: latestFeature`, `setup-dotnet` puede instalar el último SDK 8.0.x en lugar del 8.0.427 exacto de la PC (aceptable).

---

## 6. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V-01 | menor | architect | `ObservableValue` y `EventStream` no aíslan la excepción de un suscriptor: se corta la notificación a los demás y la excepción sube a quien asignó el valor. El comentario de la clase promete "semántica de `ChangeNotifier`", que atrapa el error de cada listener y sigue (y `StreamController.broadcast` tampoco corta a los demás). Escenario: vista A y servicio B suscriptos a un estado; A lanza al recibir `connected` → el setter lanza, el valor queda en `connected` y **B nunca se entera** (y no se va a enterar: un nuevo `connected` no notifica) | `Core/Common/ObservableValue.cs:71-82`, `EventStream.cs:148-159`. Sonda en el *scratchpad* (`qa-probe`): `setter: lanzo InvalidOperationException (vista rota)` · `segundo suscriptor recibio 0 notificaciones` · `EventStream segundo suscriptor recibio 0`. No hay test de esto (`ObservableValueTests`, `EventStreamTests`) ni regla en ADR 004 | Antes de la It. 2: `try/catch` por suscriptor que reporte a un sumidero de errores (`IAppLogger` o un *hook* estático) y siga con el resto; tests con un suscriptor que lanza. O, si se prefiere propagar, decirlo en ADR 004 y corregir el comentario |
| QA-V-02 | menor | architect | Higiene de secretos más débil que AC-APP-19-03 antes del push: `.gitignore` solo protege `**/StravaSecrets*.cs`, y la spec lista `StravaSecrets*` con cualquier extensión. `NoSecretsAreTracked` mira solo `git ls-files` (archivos ya versionados): en la PC no ve un archivo nuevo sin commit, y el CI recién lo detecta **después** del push (el secreto ya está en el remoto) | `git check-ignore --no-index`: `Assets/RowMate/Config/StravaSecrets.json`, `Assets/RowMate/StravaSecrets.asset` y `tools/strava/StravaSecrets.txt` → **no ignorados**. `RepoHygieneTests.cs:98-116` (`ls-files -z`). Hoy el repo no tiene commits: los dos tests de "versionados" pasan sin revisar nada | `.gitignore`: `**/StravaSecrets*` (y su `.meta`). En `RepoHygieneTests`, listar también `git ls-files --others --exclude-standard` (lo que `git add -A` agregaría) y casos de prueba con `.json`/`.asset` |
| QA-V-03 | menor | architect | El *bootstrapper* reintenta en **cada recarga de dominio** si un paso falla, no "en el próximo open" como dice 02-tech-design §1.5: `[InitializeOnLoad]` corre el constructor estático en cada recompilación y la versión queda en 0. Escenario: falla `urp.provisional` (R-T2) → cada vez que se guarda un script se repiten los 9 pasos, `SetGraphicsAPIs`, `SaveAssets` y el error con las instrucciones manuales | `Editor/Bootstrap/ProjectBootstrapper.cs:39-42`, `:59-63`, `:89` | Reintentar una vez por sesión del Editor (`SessionState.GetBool/SetBool`), y dejar el menú `RowMate/Bootstrap/Reaplicar` para forzar |
| QA-V-04 | menor | architect | El namespace `RowMate.Editor` tapa a `UnityEditor.Editor` dentro de ese namespace: un futuro `class StepInspector : Editor` en `namespace RowMate.Editor` resuelve `Editor` al namespace y da `CS0118` (trampa conocida de Unity) | `Editor/AssemblyInfo.cs`, `Editor/Bootstrap/*.cs` (`namespace RowMate.Editor`) | Renombrar el namespace (por ejemplo `RowMate.EditorTools`) o fijar en ARCHITECTURE §17 la regla de escribir `UnityEditor.Editor` completo |
| QA-V-05 | menor | architect | En Unity, los 69 tests del simulador (LT-01 a LT-07 y AC-BLE-14-01 a -10) solo existen con `ROWMATE_SIMULATOR`, que el *bootstrapper* nunca define (R-01, correcto). Por defecto el Test Runner del Editor no los muestra, y AC-BASE-08 pide que "las mismas pruebas" corran en Unity | `Tests/Simulator/RowMate.Tests.Simulator.asmdef` (`defineConstraints`); `PlayerSettingsStep.CheckSimulatorDefine` | Plan de la It. 2: procedimiento documentado (define solo para la corrida de tests del Editor o `ci-unity` con `-define`) y verificación de que los 912 tests corren en el Editor |
| QA-V-06 | menor | architect | `UnchangedLegacyTextsAreIdentical` compara el **patrón** crudo (`catalog.Message`), no el texto que ve el usuario (`Localizer.Get`). Hoy coinciden (ningún texto sin parámetros tiene `{`, `}` ni `#`, y el apóstrofo de `won't` es literal), pero un cambio futuro del formateador (escape con apóstrofos, por ejemplo) cambiaría lo visible sin que este test falle | `Tests/EditMode/Localization/CatalogParityTests.cs:91-113` | Agregar la comparación de `Localizer.Get(key)` con el texto legacy para las claves sin parámetros |
| QA-V-07 | menor | architect | `run-tests.ps1 -Demo` imprime cada línea `[DEMO]` dos veces (la salida del test y su eco con `verbosity=detailed`). Confunde la demo al usuario | Corrida `-Demo` de §1.2 | Imprimir una sola vez (filtrar el eco o escribir con `TestContext.Progress`) |
| QA-V-08 | menor | architect | Pendientes de 02-tech-design §10.4: el **cuerpo** de ADR 012 §4 todavía dice `tools/oracle/golden/*.json` y el de ADR 011 "devuelve el primero soportado"; solo se agregaron las filas D-01 y D-06 a sus tablas de cambios, y el texto principal contradice la tabla | `adr/012-paridad-numerica-dart.md:50`, `adr/011-localizacion.md:14` | Corregir el texto principal de los dos ADR |
| QA-V-09 | menor | designer | Pendiente de §10.4 (P-08): ART_BIBLE §9 conserva "Diferencia abierta: … ARCHITECTURE §20.1 dice `#070D16 @80 %`", ya resuelta | `design/ART_BIBLE.md:582` | Quitar la diferencia abierta |
| QA-V-10 | menor | po | Pendiente de la spec §14: FEATURE_INVENTORY Q-85 y R-14 siguen con `com.iguisoft.rowmate` "en Android y Windows" | `product/FEATURE_INVENTORY.md:498`, `:524` | Alinear con AC-APP-16-02 (Windows sin id; reservado al MSIX de la It. 14) |
| QA-V-11 | menor | po | Rango de enteros: en el legacy los números de paso son `int` de Dart (64 bits); en Core, `int` de 32 bits y la progresión con `checked`. Una serie ×3 con vatios mín. 2 000 000 000 y prog. +100 000 000 da en Dart 2,0e9, 2,1e9 y 2,2e9; en C# `Flatten` lanza `OverflowException` (el motor de la 4a no podría arrancarla). Valores absurdos, pero AC-RUT-02-04 dice "sin validación" | `Core/Models/RoutineExpansion.cs:235-243`; sonda: `Flatten lanzo OverflowException` | Spec de la It. 3: fijar el rango que acepta el editor (por ejemplo, que el valor y todas sus repeticiones entren en 32 bits) y su mensaje; o pasar a `long` |

---

## 7. Lo que pasa a la It. 2 (y después) como NO VERIFICABLE

| Qué | Dónde |
|---|---|
| Abrir el proyecto en 6000.3.27f1 sin errores; el *bootstrapper* aplica los 9 pasos (R-T2); `csc.rsp` aplicado a los asmdefs (R-T12); versiones de test-framework e Input System | It. 2 [Editor] |
| Los mismos tests en el Test Runner del Editor (NUnit propio de Unity, R-T6), incluidos los del simulador (QA-V-05); paridad de `Fmt`/`DartMath` en Mono e IL2CPP (R-T5) | It. 2 [Editor] y *smoke test* IL2CPP |
| AC-BLE-01-05, AC-APP-09-04/-05 en pantalla; AC-APP-09-06 y AC-APP-16-01 a -04 en equipos | It. 2 [Editor]/[Device] |
| AC-RUT-01-04, AC-RUT-04-06 (editor) y AC-APP-09-07 (teclado) | It. 3 |
| AC-RUT-05-03 (diálogo de inicio); R-T1 (mezcla lineal del vidrio, P-05 con margen ≤ 0,02) | It. 4a |
| AC-BLE-03-01 en plataforma real | It. 10a/11; [Apple] 10b |
| AC-APP-16-05 y la parte [Apple] de AC-APP-16-01 a -03 | It. 10b |
| AC-APP-19-01/-02: corrida real de `ci-core.yml` en GitHub | Cuando exista el remoto (P-03) |
| `TimeZoneInfo.Local` en IL2CPP Android (lo usa la lectura de fechas, AC-RUT-16-04) | Primer uso en pantalla (It. 3/9) |

---

## 8. Legacy intacto

```
git -C ../rowmate-legacy status --porcelain     → (vacío) al empezar
git -C … status --porcelain --ignored --untracked-files=all        → idéntico antes y después de correr el oráculo
git -C ../rowmate-legacy status --porcelain     → (vacío) al terminar
```

No se leyó ni se copió `lib/core/strava/strava_secrets.dart`. No se hicieron commits en ninguno de los dos repositorios.
