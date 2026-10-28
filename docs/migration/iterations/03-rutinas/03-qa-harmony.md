# It. 3 — Rutinas: lista, editor y base local: gate de armonía de QA

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-27 |
| Iteración | 3 · `03-rutinas` ([ROADMAP §It. 3](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (101 AC, 28 CAP, 12 DEMO, 3 VID), [02-tech-design](02-tech-design.md), [04-design-notes](04-design-notes.md), [00-deuda-it2](00-deuda-it2.md), [00-deuda-diseno](00-deuda-diseno.md) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/routines/{routine_editor_screen,routines_screen,routines_provider}.dart` (enteros), `lib/core/database/database_service.dart:1-215`, `lib/core/models/{routine,interval_step}.dart` (enteros), `lib/shared/theme.dart:1-69`, `lib/main.dart:70-181`, `lib/core/dev/simulator_overlay.dart:25-60`, `lib/l10n/app_{es,en}.arb` (claves de rutinas, editor, tipos y botones), `pubspec.yaml`, `windows/runner/Runner.rc`. SDK de Flutter: `<flutter-sdk>/packages/flutter/lib/src/widgets/{editable_text,routes}.dart`, `material/{snack_bar,tooltip}.dart`, `flutter_localizations/lib/src/l10n/material_{es,en}.arb`, `bin/cache/dart-sdk/lib/_internal/vm/lib/string_patch.dart:480-500`, `vm_shared/lib/integers_patch.dart:84-125`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío antes y después |
| Verificación hecha | `dotnet test` de línea base (§1); paquete `com.gilzoide.sqlite-net` 1.3.3 inspeccionado en el caché de paquetes (opciones de compilación de SQLite, atributos, rutas de IL2CPP); los 23 casos de fidelidad de §3 recalculados contra el legacy y el SDK. **Unity no se abrió**: el gate no lo necesita (regla de una instancia) |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** **0 bloqueantes, 7 mayores, 14 menores** (§10).

- **Cobertura completa**: los 101 AC tienen un componente responsable y al menos una verificación planificada en 02-tech-design §6.3. 100 tienen un test automático (dotnet o Unity). AC-HIS-17-03 queda solo con equipo y revisión estática. Las 10 partes "sin test" que declara el Architect están justificadas (§2.2).
- **Fidelidad**: de 23 casos que comparé contra el legacy, 20 coinciden al carácter o al número. Los otros 3 son una contradicción del propio spec (CAP-13: 8 dp contra 16 en el legacy), un comportamiento del legacy que el diseño no trae (una pantalla o un diálogo que se cierra no recibe toques) y un caso borde sin regla (§3).
- **Lo que más pesa**: 04-design-notes se escribió **después** de 02-tech-design y le hace 23 pedidos (DN3-01 a DN3-23) que cambian el contrato USS y las comprobaciones del arnés. Además, tres textos de AC chocan con las correcciones de contraste del Designer. Hay que reconciliar los tres documentos antes de B1 y B2 (QA-G3-02 a -05).

Qué hay que cerrar antes de cada paquete:

| Antes de | Issues |
|---|---|
| **WP-A** | QA-G3-07 (API de test para SQL), QA-G3-08 (pragmas probados en los dos SQLite), y la parte de reglas de QA-G3-13 y QA-G3-19 |
| **WP-B · B1** (capas, lista, base) | QA-G3-01 (lo que se cierra no recibe toques), QA-G3-02 (anexo con los DN3 de la lista, el aviso y la llave), QA-G3-05 (AC-BLE-16-02), QA-G3-14 (matriz) |
| **WP-B · B2** (editor, diálogos, texto) | QA-G3-02 (DN3 del editor), QA-G3-03 (DV-12), QA-G3-04 (CAP-13), QA-G3-10 y QA-G3-12 (sumar a la sonda S-B3a), QA-G3-11 (foco y desplazamiento), QA-G3-16 |
| **WP-B · B3** (arnés y scripts) | QA-G3-06 (CAP-27 sobre la base real), QA-G3-20, QA-G3-21 |

---

## 1. Evidencia corrida

| Qué | Comando | Resultado |
|---|---|---|
| dotnet, línea base antes de WP-A | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it3/qa-gate` | `Build succeeded. 0 Warning(s) 0 Error(s)`; simulador **149/149**; EditMode **1124: 1107 pasan, 0 fallan, 17 omitidos**. Total 1273 / 1256 / 0 / 17: igual a 00-deuda-it2 §9 |
| Opciones del SQLite de Unity | lectura de `Library/PackageCache/com.gilzoide.sqlite-net@34340bf93526/Plugins/sqlite-amalgamation/sqlite3_defines.h` y `sqlite3.h:197` | SQLite **3.53.4**; `SQLITE_DQS 0` (:5), `SQLITE_DEFAULT_WAL_SYNCHRONOUS 1` (:7), `SQLITE_OMIT_DECLTYPE 1` (:11), `SQLITE_TEMP_STORE 2`. Binarios: Windows x86/x86_64/arm64, Android arm32/arm64/x86/x86_64; iOS con `__Internal` (`SQLite.cs:4584-4588`) |
| Atributos e IL2CPP en gilzoide | `Runtime/sqlite-net/SQLite.cs` | `ColumnAttribute`, `PrimaryKeyAttribute`… heredan de `UnityEngine.Scripting.PreserveAttribute` (:2457-2548); `TableAttribute` no (:2439). `FastColumnSetter` usa `MakeGenericMethod` solo si `typeof(T) != map.MappedType` (:3143-3147) |
| Herramienta de glifos | `Get-Command uvx` | `uvx` está instalado. La receta de ASSET_SLOTS §4.1 (`uvx --from fonttools pyftsubset …`) no necesita Python (QA-G3-18) |
| Legacy intacto | `git -C ../rowmate-legacy status --porcelain` | vacío antes y después |

AC-BLE-11-02 (primera tarea de la iteración): 00-deuda-it2 §9.1 informa PASS con 3 pruebas PlayMode que fallaban antes del arreglo. No lo volví a correr en Unity. Lo re-verifico en 05-qa-verification.

---

## 2. Cobertura: AC → componente → test

### 2.1 Matriz

"Gate": **OK** = componente y test planificados y coherentes; **OK · QA-G3-nn** = cubierto, con una observación; **OBS QA-G3-nn** = lo afecta un issue mayor; **Parcial (justificado)** = una parte queda para equipo, sin test automático. Abreviaturas de tests: 02-tech-design §6.2.

**F-RUT-08 (+ F-HIS-18)**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-RUT-08-01 | `SqliteRoutineRepository`, `RoutineRowMapper` | d `RRT.InsertKeepsTheFourteenAttributesAndText`, `.InsertNumbersStepsFromZero` | OK |
| AC-RUT-08-02 | `SqliteRoutineRepository.UpdateAsync` | d `RRT.UpdateReplacesStepsAndKeepsCreatedAt`, `.UpdateWithoutChangesIsIdentical`, `.UpdateLeavesTheOthers` | OK |
| AC-RUT-08-03 | `DeleteAsync` (Q-17) | d `RRT.DeleteRemovesTheRoutineAndItsSteps`, `.NewRoutineNeverSeesDeletedSteps` | OK |
| AC-RUT-08-04 | `GetByIdAsync` | d `RRT.GetByIdReturnsStepsInOrder`, `.GetByIdOfMissingOrDeletedIsNull` | OK |
| AC-RUT-08-05 | `ORDER BY created_at DESC, id DESC`, `RoutinesStore` | d `RRT.ListIsNewestFirst`, `RST.CreatedAtIsUtcAcrossATimeZoneChange`, `RST.EditKeepsDateAndPlace` | OK |
| AC-RUT-08-06 | `SeriesIdGenerator`, `EditorLayout` | d `SIG.SameMillisecondGivesDistinctIds`, `RRT.SeriesSurviveTheRoundTrip`; p `RPS.SeriesBlocksSurviveReopen` | OK · QA-G3-19 |
| AC-RUT-08-07 | transacción, `synchronous = FULL`, el editor espera el *commit* | d `RRT.InsertIsAllOrNothing`, `.UpdateIsAllOrNothing` (*trigger*); eq cierre forzado | Parcial (justificado) · QA-G3-08 |
| AC-RUT-08-08 | `RoutineRowMapper` + `StepTypeNames.Parse`, `RoutinesStore` | d `RRT.UnknownTypeFailsTheLoadWithoutWriting`, `RST.UnknownTypeGivesLoadFailed`; p `RLT.UnknownTypeShowsTheLoadError` | OK |
| AC-RUT-08-09 | reapertura (`HostRoutines.SameFile`) | p `RPS.ReopenShowsTheSame`; DEMO-10; eq | OK |

**F-RUT-09**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-RUT-09-01 | `RoutinesStore`, `AppContext.Start` | d `RST.LoadsOnceAtStart`, `ACT.StartLoadsRoutines`; p `RLT.SecondVisitDoesNotShowTheIndicator` | OK · QA-G3-02 (gracia de 120 ms) |
| AC-RUT-09-02 | `RoutinesStore`, `RoutineListView` (*diff* por `Id`) | d `RST.CreateGoesFirst`, `.UpdateKeepsPosition`, `.DeleteKeepsOrder`; p `RLT.CreateEditDeleteInPlace` | OK |
| AC-RUT-09-03 | `RoutineEditorStore.SaveAsync`, `PageStack` | d `RET.SaveWhileSavingIsIgnored`; p `RED.DoubleTapSaveCreatesOne`, `.TapAndEnterCreateOne`, `.EditSavesOnce` | **OBS QA-G3-01** |
| AC-RUT-09-04 | `RoutinesStore` (`LoadFailed`), `#routines-error` | d `RST.LoadFailureThenRetry`; p `RLT.LoadErrorAndRetry`, `.DeviceTabStillWorksWithLoadError`; CAP-03 | OK |
| AC-RUT-09-05 | `RoutineEditorStore` (`Failed`), `ToastLayer` | d `RET.SaveFailureKeepsEverything`; p `RED.SaveErrorKeepsTheEditorOpen`; CAP-28 | OK · QA-G3-09 |
| AC-RUT-09-06 | `RoutinesStore.DeleteAsync`, `ToastLayer` | d `RST.DeleteFailureKeepsTheRoutine`; p `RLT.DeleteErrorKeepsTheCard`, `.DeleteRemovesWhenDone` | OK · QA-G3-09 |

**F-RUT-10**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-RUT-10-01 | `RoutinesScreenView`, `Fab` | p `RLT.TabTitleWithoutActionsAndFab`; CAP-01, CAP-04 | OK |
| AC-RUT-10-02 | `#routines-loading`, `#routines-empty`, `Spinner` | p `RLT.LoadingShowsOnlyTheIndicator`, `.EmptyStateTextsAndColors`; CAP-01, CAP-02 | OK · QA-G3-02 |
| AC-RUT-10-03 | `RoutineCardView`, `RoutineCardModel` | d `RCM.DescriptionOnlyWhenNotEmpty`; p `RLT.CardContent`, `.LongNameAndDescriptionWrap`; CAP-04, CAP-06 | OK · QA-G3-10, -13 |
| AC-RUT-10-04 | `Fab`, obstáculos de `ShellLayout`, `SimulatorPanelLayout.FabBottom`, `SimulatorLayerView` | p `RLT.FabOpensANewEditor`; ps `RSL.WrenchNeverCoversTheFab`, `.FabTappableEverywhere`; CAP-01 | OK · QA-G3-05 |
| AC-RUT-10-05 | `RoutineCardView` (`Clickable`, `NavigationSubmitEvent`) | p `RLT.TapCardOpensItsEditor` | OK |
| AC-RUT-10-06 | `PopupMenu`, `Tooltip` | p `NAV.CardMenuEditAndDelete`, `.MenuClosesOnOutsideEscapeAndBack`, `.MenuButtonTooltip`, `RLT.MenuButtonDoesNotOpenTheEditor`; CAP-07 | **OBS QA-G3-01** |
| AC-RUT-10-07 | `DeleteRoutineDialog`, `ModalDialog` | p `NAV.DeleteDialogOnlyConfirmDeletes`; CAP-08, DEMO-11 | OK · QA-G3-10 |
| AC-RUT-10-08 | (no existen esas acciones) | qa, p `RLT.NoOtherActions` | OK |
| AC-RUT-10-09 | `RoutineListView`, `ScrollPolicy`, relleno inferior por obstáculos | p `RLT.ThirtyRoutinesScrollAboveTheFab`, `.ScrollPositionSurvivesEditorAndTabs`; CAP-06 | OK |
| AC-RUT-10-10 | `RoutineCardModel` / `RoutineSummary` | d `RCM.SummariesEsEn`; p `RLT.SummariesOnScreen`; CAP-04, CAP-05 | OK |
| AC-RUT-10-11 | `RoutineStructure` (WP-A), `StructureBarView` | d `RSB.FixtureSegments`, `.ImpossibleRepeatsNeverExpand`; p `RLT.StructureBarStaysInOneLine`, `.ImpossibleRoutinesLoadAndRespond`; CAP-04, DEMO-07, DEMO-09 | OK · QA-G3-02 |

**F-RUT-11**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-RUT-11-01 | `#editor-appbar`, `Tooltip` | p `RED.AppBarNewAndEdit`; CAP-09, CAP-10, CAP-24 | OK |
| AC-RUT-11-02 | `RmTextField` (una línea, multilínea) | p `RED.FieldsAndPrefill`, `TXT.SingleLineDropsPastedNewlines`, `TXT.DescriptionTwoLinesEnterAndScroll`; CAP-09 | OK · QA-G3-10 |
| AC-RUT-11-03 | `RoutineTemplate`, `EditorStepsView` | d `RTT.CountIsTheTemplate`; p `RED.StepsCountUpdatesAtOnce` | OK |
| AC-RUT-11-04 | `#editor-empty` | p `RED.EmptyStepsText`; CAP-09 | OK |
| AC-RUT-11-05 | `SaveAsync`, `StepLimitCheck` | d `RET.ValidationOrder`, `.OutOfRangeStepBlocksTheSave`; p `RED.ValidationToasts`; CAP-18, CAP-19 | OK · QA-G3-13 |
| AC-RUT-11-06 | `SaveAsync`, `DartText` | d `RET.SaveTrimsRenumbersAndKeepsId`, `DTT.TrimLikeDart`; p `RED.SaveClosesAndTheListShowsIt` | OK |
| AC-RUT-11-07 | `SaveAsync` | d `RET.NoUniquenessNoMaxLength`; p `RED.SameNameTwiceAndFiveHundredChars` | OK |
| AC-RUT-11-08 | `IPage.OnBackRequested`, `BackNavigation` | p `NAV.BackDiscardsWithoutAsking`, `.EscapeDoesNotCloseTheEditor`; [Device-And] | OK · QA-G3-10 |
| AC-RUT-11-09 | `RmTextField`, `TextContextMenu`, `IClipboard` | p `TXT.ContextMenuItemsByState`, `.CutCopyPasteWithTheTestClipboard`, `.TabSelectsAllClickPlacesTheCaret`, `.DescriptionDoesNotSelectAll`, `.NoMenuWithoutActions`; [Device-Win]; CAP-26 | OK · QA-G3-10 |
| AC-RUT-11-10 | `KeyboardAvoider`, `SoftKeyboard`, `hideMobileInput` | p `KAV.FocusedFieldStaysAboveTheKeyboard`, `TXT.MobileInputIsHidden` (simulados); [Device-And] | Parcial (justificado) · QA-G3-16 |

**F-RUT-12**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-RUT-12-01 | `RoutineTemplate.AppendLoose`, `StepDialogForm.ForNew` | d `RTT.AppendLooseGoesLastEvenAfterASeries`; p `RED.AddStepAtTheEnd`, `.CancelAddsNothing` | OK |
| AC-RUT-12-02 | `StepRowView`, `StepRowModel` | d `SRM.TitleColorAndIcon`; p `RED.StepRowContract`; CAP-10, CAP-12 | **OBS QA-G3-03** |
| AC-RUT-12-03 | `RoutineTemplate.Replace`, `StepDialogForm.ForEdit` | d `RTT.ReplaceKeepsSeriesAndCount`, `SFT.LooseStepKeepsHiddenProgressions`; p `RED.EditInPlace` | OK · QA-G3-13 |
| AC-RUT-12-04 | `RoutineTemplate.RemoveAt` | d `RTT.RemoveAt`; p `RED.DeleteStepAtOnce` | OK |
| AC-RUT-12-05 | (no existen esas acciones) | qa, p `RED.NoReorderOrMoveAffordances` | OK |
| AC-RUT-12-06 | `StepSubtitle`, `StepSubtitleView` (glifo por respaldo de fuente) | d `SST.BorderCasesTable`; p `RED.BorderSubtitlesOnScreen`; CAP-12 | OK · QA-G3-12 |

**F-RUT-13**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-RUT-13-01 | `StepDialogForm.ForNew`, `StepDialog` | d `SFT.NewFormDefaults`; p `SDT.TitlesAndDefaults`; CAP-14 | OK |
| AC-RUT-13-02 | `StepDialog` | p `SDT.FieldsInOrder`, `.ProgressionsOnlyInASeries`, `.HintsWhenEmpty`; CAP-14, CAP-15, CAP-24 | OK · QA-G3-17 (DT-02) |
| AC-RUT-13-03 | `SegmentedControl` (mosaicos) | d `StepTypeTests.SelectorOrder`; p `SDT.FourTypesOneSelected` | OK |
| AC-RUT-13-04 | `StepDialogForm.ByDistance` | d `SFT.OnlyTheChosenModeCounts`; p `SDT.ModeSwitchKeepsTheText` | OK · QA-G3-13 |
| AC-RUT-13-05 | `DartIntParser` (`BigInteger`) | d `DIP.LegacyTable`, `.AnyLengthNeverOverflows`, `SFT.HugeNumbersAreOutOfRange` | OK |
| AC-RUT-13-06 | `_build()` por tiempo | d `SFT.ByTimeTable`; p `SDT.EmptyTimeConfirmsZero` | OK |
| AC-RUT-13-07 | `_build()` por distancia | d `SFT.ByDistanceTable`; p `SDT.EmptyDistanceIsNoGoal` | OK |
| AC-RUT-13-08 | `_build()` split | d `SFT.SplitTable` | OK · QA-G3-13 |
| AC-RUT-13-09 | `_build()` sin validaciones | d `SFT.NoSenseValidation` | OK |
| AC-RUT-13-10 | `ForEdit`, `DartMath` | d `SFT.PrefillTable`; p `SDT.PrefillOnScreen`; CAP-15 | OK |
| AC-RUT-13-11 | `StepLimits`, `Confirm()`, error del `RmTextField` | d `SLT.FieldCaps`, `SFT.FieldErrorsAndMaxTexts`; p `SDT.RangeErrorKeepsTheDialogAndClearsOnChange`; CAP-20a | OK · QA-G3-13 |
| AC-RUT-13-12 | `StepLimits.FirstRepetitionOutOfRange` | d `SLT.FirstRepetitionClosedForm`, `.TwoBillionRepeatsWithoutLoops`, `SFT.RepetitionErrors`, `.LooseStepProgressionsAreNotChecked`; p `SDT.RepetitionErrorUnderTheProgression`; CAP-20b | OK |
| AC-RUT-13-13 | `keyboardType`, `FocusTrap` | p `SDT.TabStaysInsideTheDialog`, `.NumericFieldsUseSignedKeyboard`; [Device-And] | Parcial (justificado) · QA-G3-11 |
| AC-RUT-13-14 | `ModalDialog`, `BackNavigation` | p `SDT.CancelOutsideEscapeAndBackChangeNothing` | OK |
| AC-RUT-13-15 | `ModalDialog` (alto máximo, compacto) | p `SDT.LowWindowScrollsAndKeepsTheButtons`; CAP-23 | OK · QA-G3-11 |
| AC-RUT-13-16 | `Preview()`, `#step-preview` | d `SFT.PreviewIsTheConfirmedRow`; p `SDT.PreviewMatchesTheEditorRow`; CAP-14, CAP-15 | OK |

**F-RUT-14**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-RUT-14-01 | `RoutineTemplate.AppendDefaultSeries`, `SeriesIdGenerator` | d `RTT.DefaultSeries`; p `RED.SeriesButton`; DEMO-04 | OK |
| AC-RUT-14-02 | `SeriesBlockView` | p `RED.SeriesBlockContract`; CAP-10, CAP-11 | OBS QA-G3-02 (DN3-05) |
| AC-RUT-14-03 | `EditorLayout`, chip | d `ELT.HeaderRepeatDefaultsToOne`, `.ZeroRepeatShowsZero`; p `RED.ChipTextAndTouchArea`; CAP-10, CAP-12 | **OBS QA-G3-03** |
| AC-RUT-14-04 | `RepeatDialog`, foco del `RmTextField` | p `RPT.PrefillAndFocus`; CAP-17 | OK |
| AC-RUT-14-05 | `ChangeRepeatCount` | d `RCT.Table`; p `RPT.Rules` | OK · QA-G3-13 |
| AC-RUT-14-06 | `ChangeRepeatCount` + `StepLimits` | d `RCT.MaxAndProgressedTargets`; p `RPT.OutOfRangeKeepsTheDialog`; CAP-21 | OK |
| AC-RUT-14-07 | `RoutineTemplate.InsertIntoSeries` | d `RTT.InsertIntoSeriesAfterItsLast`; p `RED.AddStepToSeries` | OK |
| AC-RUT-14-08 | `RoutineTemplate.Replace` | d `RTT.ReplaceInSeries`; p `RED.EditSeriesStep` | OK |
| AC-RUT-14-09 | `RoutineTemplate.RemoveSeries` | d `RTT.RemoveSeries`; p `RED.DeleteSeriesAtOnce` | OK |
| AC-RUT-14-10 | `EditorLayout` | d `ELT.EmptiedSeriesDisappears`, `.OneStepSeries`; p `RED.SeriesEmptiesAndDisappears` | OK |
| AC-RUT-14-11 | `EditorLayout`, `SeriesBlockView` | d `ELT.ContiguousSeriesAreTwoBlocks`, `RTT.RepeatChangeTouchesOnlyItsSeries`; p `RED.ContiguousSeries`; CAP-13 | **OBS QA-G3-04** |
| AC-RUT-14-12 | (no existen esas acciones) | qa, p `RED.NoReorderOrMoveAffordances` | OK |

**F-HIS-17, F-HIS-20**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-HIS-17-01 | `AppDataLocation`, `Bootstrap/DataLocation` | u `DLT.WindowsUsesLocalLow`, `.AndroidUsesTheDatabasesFolder`; [Device-Win]; [Device-And] | Parcial (justificado) |
| AC-HIS-17-02 | `DatabaseSchema`, `RowMateDatabase` | d `DSC.SchemaIsV8`; qa con una herramienta de SQLite | **OBS QA-G3-07**, QA-G3-08 |
| AC-HIS-17-03 | (ningún camino hacia el legacy) | qa, eq | Sin test automático (justificado) · QA-G3-14 |
| AC-HIS-17-04 | protocolo de apertura (§2.3) | d `DOT.ExistingV8IsUsedAsIs`; p `RPS.OpensAnExistingBase` | OK · QA-G3-07 |
| AC-HIS-17-05 | `DatabaseHeader`, sonda de solo lectura | d `DOT.NotADatabaseIsUntouched`, `.NewerVersionIsUntouched`, `.NewerVersionOnlyInTheWalIsUntouched`, `.UnrecognizedIsUntouched`; p `RLT.UnreadableBaseShowsTheError` | OK · QA-G3-13 |
| AC-HIS-17-06 | `SerialExecutor` | d `SET.WorkRunsOffTheCallingThread`; p `PRF.FiftyFourRoutinesUnderOneSecond`, `.NoLongFramesWhileLoadingOrSaving`; [Device-Win] | OK · QA-G3-21 |
| AC-HIS-20-01 | `DatabaseFiles.NameFor`, `AppRoot` | d `DFT.NameByVariant`; dB `ITT.OnlyAppRootChoosesTheDatabaseFile`; eq | OK |
| AC-HIS-20-02 | dos composiciones sobre la misma carpeta | p `RPS.VariantsDoNotMix`; eq | OK · QA-G3-14 (nombre) |
| AC-HIS-20-03 | ningún texto del modo | ps `RSL.SameScreensOnlyTheRibbon`; CAP-04 | OK |
| AC-HIS-20-04 | `TempDatabase` (guardia), `ROWMATE_DATA_DIR`, scripts | d `TDT.RefusesUserDataPaths`; dB `ITT.ScriptsIsolateTheDataFolder`; p `BOOT.UsesTheTestDataFolder`; qa antes/después | **OBS QA-G3-06** |

**F-APP-03 y "también toca"**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-APP-03-01 | `PageStack` (`CoversShellChanged`) | p `NAV.EditorCoversTheTabBar`; CAP-09 | OK |
| AC-APP-03-02 | `BackNavigation.HandleBack(BackSource)` | p `NAV.BackClosesOnlyTheTop`, `.EscapeClosesOnlyMenusAndDialogs`; [Device-And] | Parcial (justificado) · QA-G3-10 |
| AC-APP-03-03 | `DialogLayer`, `ModalDialog`, `FocusTrap` | p `NAV.DialogsAreModal`, `.ScrimCancels`, `.TabIsTrappedInTheDialog`; CAP-08, CAP-14, CAP-17 | OK · QA-G3-01 |
| AC-APP-03-04 | `PopupLayer`, `PopupMenu` | p `NAV.MenuAboveEverythingButTheSimulator`; CAP-07 | OK · QA-G3-01 |
| AC-APP-03-05 | `ToastLayer` (reemplazo, obstáculos) | p `NAV.ToastFourSecondsAndReplaces`; ps `RSL.ToastAndWrenchDoNotOverlap`; CAP-18, CAP-19, VID-03 | OK · QA-G3-02 (DN3-13) |
| AC-APP-03-06 | `Tooltip` | p `NAV.TooltipsOnHoverAndLongPress` | Parcial (justificado: lector de pantalla, DT-05) |
| AC-APP-03-07 | orden de capas, `pickingMode` | ps `RSL.SimulatorLayerAboveEverything`, `.BackArrowUnderTheRibbonWorks`, `.PanelWorksWithADialogOpen`; CAP-08, CAP-09, DEMO-02 | OK |
| AC-APP-03-08 | `PageTransition`, `DialogTransition`, `ToastTransition`, `MotionSpecs` | p `NAV.TransitionsFollowTheBible`, `.ReducedMotionFades`; VID-02 | OK · QA-G3-02 |
| AC-APP-01-09 | `AppContext.Start` | d `ACT.StartLoadsRoutines`; p `RLT.ListIsReadyBeforeTheFirstVisit` | OK |
| AC-APP-05-10 | `WindowsTitleBar` (deuda de la It. 2) | u `WindowsTitleBarTests`; eq; CAP-27 | Parcial (justificado: modo claro y cambio en vivo) · QA-G3-06 |
| AC-APP-05-11 | `ScrollPolicy` | p `RLT/RED/SDT.NoScrollbarOnPhone`; CAP-04, CAP-10; [Device-And] | OK |

**§5 (iteraciones anteriores)**

| AC | Componente | Tests / capturas planificados | Gate |
|---|---|---|---|
| AC-BLE-11-02 | `DeviceScreenView` (vista congelada) | p `DeviceScreenTests.OutgoingConnectedViewIsFrozen`; ps `DeviceSimulatorTests.DisconnectKeeps…`, `.SimulatedDropKeeps…` | OK (PASS según 00-deuda-it2; QA lo re-verifica en 05) |
| AC-BLE-16-01 | capa del simulador como último hijo | ps `RSL.SimulatorLayerAboveEverything`; CAP-08, CAP-09 | OK · QA-G3-05 |
| AC-RUT-01-04 | `StepRowView` | p `RED.StepRowIcons`; CAP-12 | OK |
| AC-RUT-01-05 | `RoutineRowMapper` | d `RRT.UnknownTypeFailsTheLoadWithoutWriting`; p `RLT.UnknownTypeShowsTheLoadError` | OK |
| AC-RUT-04-06 | `StepSubtitle` | d `SST.EditorSeparatorHasTwoSpaces`; p `RED.BorderSubtitlesOnScreen`; CAP-12 | OK |
| AC-APP-09-07 | `TextContextMenu` (catálogo) | p `TXT.ContextMenuInTheAppLanguage`; [Device-Win]; [Device-And] | Parcial (justificado) · QA-G3-16 |
| AC-APP-02-08 | `RoutinesScreenView` en lugar del provisorio | p `RLT.RoutinesIsNotAPlaceholder`, `ShellTests`; CAP-01 | OK |
| AC-APP-05-05 | textos que ajustan | p `RLT/RED/SDT.TextScale130`; CAP-25 | OK |
| AC-APP-05-07 | orden de Tab, anillo, `NavigationSubmitEvent` | p `NAV.TabOrderAndActivation`; [Device-Win] | OK · QA-G3-10, -11 |
| AC-APP-05-08 | `MotionSettings` | p `NAV.ReducedMotionFades`, `RED.RowMotionReduced`, `RLT.StructureBarStaticWithReducedMotion` | OK |

Cuenta: 70 RUT + 10 HIS + 11 APP + 10 de §5 = **101**. Todos tienen fila en 02-tech-design §6.3.

### 2.2 Partes sin test automático (lista del Architect)

| AC | Por qué | Gate |
|---|---|---|
| AC-HIS-17-03 | Tocar el legacy instalado solo se ve en el equipo | Justificado. Riesgo bajo: el legacy de Windows guarda en `.dart_tool/sqflite_common_ffi/databases` (sqflite_common_ffi, `lib/main.dart:39-40`) y las preferencias en la carpeta `com.example/rower_app` (`windows/runner/Runner.rc:92`, `:98`). Ninguna de las dos coincide con `LocalLow/iguisoft/RowMate`. Sugiero además una prueba estática (QA-G3-14) |
| AC-RUT-08-07 (cierre forzado) | Matar el proceso | Justificado; el todo o nada tiene *trigger* |
| AC-RUT-11-10, AC-RUT-13-13, AC-APP-03-02, AC-APP-09-07, AC-HIS-17-01 (Android) | Sin módulo ni teléfono | Justificado (NO VERIFICABLE); ver QA-G3-16 |
| AC-APP-03-06 (lector de pantalla) | UI Toolkit 6000.3 no expone el panel de runtime a los lectores de pantalla (DT-05) | Justificado; necesita el visto bueno del PO (QA-G3-17) |
| AC-APP-05-10 (modo claro y cambio en vivo) | No se cambia el tema del usuario | Justificado |
| AC-HIS-20-04 (comparación de las bases) | La hace QA antes y después de todas las corridas | Justificado; la cubren además `TDT` y `ITT`. Choca con CAP-27 (QA-G3-06) |

---

## 3. Fidelidad con el legacy (muestra de 23 casos)

| # | Caso | Legacy (evidencia) | Spec / diseño | Resultado |
|---|---|---|---|---|
| 1 | Textos de la lista, el editor y los diálogos | `lib/l10n/app_es.arb:86-92`, `:117-167` y las mismas líneas de `app_en.arb` | AC-RUT-10, -11, -13, -14; catálogo del repo (174 claves) | Iguales al carácter en es y en |
| 2 | Ayudas y menú de texto de Flutter | `material_es.arb:5` (`Atrás`), `:14` (`Mostrar menú`), `:27-32`; `material_en.arb:19`, `:74`, `:190-231` | §11.2 (`navBack`, `menuShow`, `text*`) | Iguales |
| 3 | Validaciones en orden y recorte | `routine_editor_screen.dart:42-53`; `trim()` con `string_patch.dart:485-499` (U+0085, U+00A0, U+1680, U+2000-200A, U+2028/29, U+202F, U+205F, U+3000, **U+FEFF**) | AC-RUT-11-05, -06; `DartText` | Iguales |
| 4 | Serie por defecto | `:110-133` (Trabajo 120 s + Descanso 60 s, ×3, al final, `g{ms}_{contador}`) | AC-RUT-14-01; `RoutineTemplate` | Igual |
| 5 | Repeticiones | `:153` (`?? 1`), `:173` (`int.tryParse(...) ?? 1`), `:179-186` (`> 0`, todos los pasos del grupo) | AC-RUT-14-05; §3.5 | Igual. Hueco: `-99999999999999999999` da ×1 en el legacy (desborde → `null` → 1) y el diseño dice "sin cambio" (QA-G3-13) |
| 6 | `int.tryParse` | `integers_patch.dart:90-125`: signo, `0`/`0x`, decimal que desborda → `null`, hexadecimal sin signo de 64 bits | AC-RUT-13-05; `DartIntParser`; DT-11 | Igual (el hexadecimal de más de 64 bits es DT-11) |
| 7 | Precarga con `~/` y `%` | `:505-516` | AC-RUT-13-10 | Igual: −90 → `-1`/`30` → −30; −30 → `0`/`30` → 30; sin meta → tiempo vacío → 0 s |
| 8 | Tiempo, distancia y split | `:531-562` | AC-RUT-13-06, -07, -08 | Igual. Hueco: qué gana entre "split ≤ 0 = sin split" y el tope (QA-G3-13) |
| 9 | Progresiones ocultas de un paso suelto | `:517-519` (precarga siempre) y `_build()` `:551-553` (siempre las lee) | AC-RUT-12-03 | Igual. Hueco: ¿qué se valida si superan el tope? (QA-G3-13) |
| 10 | Agregar a una serie | `:144-147` (`lastIndexWhere` + 1, repeticiones de `firstWhere`) | AC-RUT-14-07 | Igual |
| 11 | Orden de la lista | `database_service.dart:145` (`created_at DESC`), `routine.dart:111` (hora local sin zona) | AC-RUT-08-05, DT-08 | Corrección Q-18, documentada |
| 12 | Alta, edición y baja | `database_service.dart:154-186` (sin transacción; la edición reescribe `created_at` con el mismo valor; la baja deja pasos huérfanos) | §3.1 | Igual + Q-17 + todo o nada |
| 13 | Esquema y cascadas | `database_service.dart:75-139`; sqflite nunca activa `foreign_keys` | §2.1: el mismo DDL + `origin` + índice + `user_version` 8, `foreign_keys = OFF`, borrado explícito de los pasos | Igual (la sangría del texto del DDL cambia, sin efecto en el esquema) |
| 14 | Diálogo de borrado | `routines_screen.dart:97-115` (título, `routineDeleteContent(name)`, `Colors.red.shade700` = `#D32F2F`, solo `true` borra, `p.delete` sin `await`) | AC-RUT-10-07, AC-RUT-09-06 | Igual |
| 15 | Subtítulo del editor | `routine_editor_screen.dart:345-349` (dos espacios), `interval_step.dart:67-109` | AC-RUT-12-06: recalculé las 11 filas | Las 11 coinciden |
| 16 | Resumen de la plantilla | `routine.dart:100-105` | AC-RUT-10-10: las 5 filas recalculadas | Coinciden (con los plurales del catálogo de la It. 1) |
| 17 | Bloque de serie | `routine_editor_screen.dart:375-445`: encabezado `margin top 8` (:377), pie `margin bottom 8` (:427), `#0077B6` al 12 % y al 6 %, borde 3, chip `×$n` w800 14 | AC-RUT-14-02, -03; CAP-13 | El AC es igual al legacy. **CAP-13 dice 8 dp entre dos series contiguas y son 16** (QA-G3-04) |
| 18 | Seleccionar todo al enfocar en escritorio | `editable_text.dart:2115-2128` (escritorio sí, Android no), `:4963-4981` (solo una línea; en el teléfono, cursor al final) | AC-RUT-11-09, AC-RUT-14-04; §4.10 | Igual |
| 19 | Lo que se cierra no recibe toques | `routes.dart:1154-1156` y `:1220-1222`: `IgnorePointer` mientras la ruta sale (animación en reversa), también en menús y diálogos | 02-tech-design §4.2 no lo dice | **Hueco** (QA-G3-01) |
| 20 | Escape solo cierra lo que se cierra tocando afuera | `routes.dart:975-990` (`barrierDismissible`) | P-08, DT-04 | Igual |
| 21 | Aviso y ayudas | `snack_bar.dart:29` (4 s); `tooltip.dart:392-394` (1,5 s, 0,1 s, espera 0) | AC-APP-03-05, -06 | Iguales (DV-14: el aviso nuevo reemplaza al visible en vez de encolarse) |
| 22 | Tramos de la rutina expandida | `routine.dart:29-70` | AC-RUT-10-11: 8, 10, 14, 1 y 7 | Recalculados: coinciden. Q-92 también: 18 + 200 × 5 = 1018 (rep. 6); 9 990 + 10 (rep. 2); ×491 → 998; ×492 → 1000 |
| 23 | Carga al arrancar | `lib/main.dart:83`: el provider es perezoso; lo crea el primer `build` de `RoutinesScreen` dentro del `IndexedStack` (`main.dart:143`), que arma todas las pestañas | AC-RUT-09-01, AC-APP-01-09 | Igual. La spec cita la línea del provider; el motivo real es el `IndexedStack` (no cambia nada) |

---

## 4. Contradicciones entre spec, diseño técnico y notas de diseño

### 4.1 Entre la spec y el diseño

| Dónde | Contradicción | Issue |
|---|---|---|
| Spec CAP-13 (`01-po-spec.md:410`) frente a AC-RUT-14-02 (`:286`) y al legacy | "8 dp entre los dos" contra 8 arriba + 8 abajo = 16; 02-tech-design §5.4 (`:943`) copia los 8 | QA-G3-04 |
| AC-RUT-12-02 (`:207`), AC-RUT-14-03 (`:287`), DV-12 (`:586`) frente a 04-design-notes §2.3 y §4.7 | Título del tipo "en su color" y `×N` en `#00B4D8`; el Designer pasa título e ícono a `step.*.hud` y el número a `accent.onTonal`, porque `#EF476F` da 3,815:1 y `#00B4D8` sobre la cápsula, 4,370:1 | QA-G3-03 |
| AC-HIS-20-04 (`:322`) y §7.1 (`:388`) frente a DT-12 (`02-tech-design.md:1311`, §5.5) | "Ninguna captura escribe en las bases del usuario" contra CAP-27, que reemplaza `rower_app.db` real y después lo restaura | QA-G3-06 |
| AC-BLE-16-02 de la It. 2 (`../02-dispositivo-simulador/01-po-spec.md:256`) frente a DV-13 y DT-03 | La llave sube 72 dp en Rutinas y el panel sigue anclado como en AC-BLE-16-03, pero el AC dice "abre el panel en ese mismo lugar"; además, AC-BLE-16-02 no está en §5 | QA-G3-05 |
| DT-01, DT-02 | Diferencias visibles con el legacy que no están en §11.1 | QA-G3-17 |

### 4.2 Pedidos del Designer al Architect (04-design-notes §9)

04-design-notes (más nuevo que 02-tech-design) le pide 23 cambios. Ninguno cambia datos ni reglas, pero varios cambian el contrato o las comprobaciones del arnés:

| DN3 | Qué | Estado |
|---|---|---|
| 01 | 6 tokens nuevos: 127 → 133 | Compatible (WP-D0). Corregir 02-tech-design §7.1 (`:1227`, dice 128) |
| 02, 03 | Clase y color por elemento; `.hud` para títulos e íconos de tipo | **Cambia §7.5** → QA-G3-02. El 03 cambia además AC → QA-G3-03 |
| 04 | Chip como caja de 48 con `.rm-chip__pill` adentro | Cambia §7.5 y AC-RUT-14-03 → QA-G3-02, -03 |
| 05 | Riel continuo; `--in-series` con `margin-left` 13 (sangría visible 16) | Cambia §7.5 (dice 16) → QA-G3-02. AC-RUT-14-02 se sigue cumpliendo |
| 06, 08, 11, 12, 17, 23 | Tarjeta, mosaicos, menú, ayuda, tamaño de ícono, sondas | Compatibles. La ayuda emergente, además, con `pickingMode = Ignore` (QA-G3-01) |
| 07 | Mini-barra: capacidad `⌊(W + 2) / 4⌋`, ancho de 2 a 24, los últimos 6 que se apagan, pista vacía | §4.3 dice "capacidad = ancho / 4" y solo `SegmentCount`/`SegmentTypes` → QA-G3-02 |
| 09 | Campo: etiqueta adentro, ayuda siempre visible | Compatible; DT-02 queda para el PO (QA-G3-17) |
| 10 | Ancho de diálogo 400 o 560, compacto, vista previa fija, foco al primer error | §4.5 dice `min(560, ventana − 80)` para todos → QA-G3-02 |
| 13 | Un obstáculo cuenta si **se cruza** con el aviso | §4.7 (`:831`) dice "que caen en su ancho" → QA-G3-02 |
| 14 | Llave a barra + 88 solo en Rutinas | Compatible con DT-03 → AC-BLE-16-02 (QA-G3-05) |
| 15 | Indicador de 32 con 120 ms de gracia | Compatible con la spec; hay que esperar la gracia en `RLT.LoadingShowsOnlyTheIndicator` y CAP-02 → QA-G3-02 |
| 16 | Constantes de motion a `MotionSpecs` (WP-A); `GrowMotion` y `CollapseMotion` animan también los márgenes | Asigna a WP-A clases que son de `Presentation/Motion` (WP-B) → QA-G3-02 |
| 18 | Desplazar hasta lo nuevo; teclado | Compatible; falta además desplazar hasta el foco (QA-G3-11) |
| 19, 20 | P2 | Fuera de los AC |
| 21 | Respaldos de fuente (íconos, Noto Emoji) | QA-G3-12 |
| 22 | `enableRichText = false` en el texto del usuario | Necesario para AC-RUT-08-01 en pantalla; ampliarlo (QA-G3-10) |

---

## 5. Paquetes de trabajo disjuntos

El orden (WP-D0 → WP-A → WP-B en B1/B2/B3 → WP-D) evita trabajo en paralelo sobre el mismo archivo. Pero la tabla de dueños no es disjunta (QA-G3-15):

- `Tests/EditMode/**` es de WP-A (`02-tech-design.md:162`), pero WP-D0 cambia `Tests/EditMode/Design/{IconsTests,ContrastTests}` y los tests de tokens, y `Tests/EditMode/Project/ItThreeToolingTests.cs` es de WP-B (`:1043`).
- `Presentation/Styles/Typography.uss` (generado por DN3-01) no está en §1.1.
- La fuente Noto Emoji (DN3-21) necesita un `FontAsset`, una entrada en `FontAssetPaths.All` y la protección de `GeneratedAssetGuard` (QA-V2-07). Ninguna de las tres cosas tiene dueño.
- DN3-16 le asigna a WP-A `GrowMotion` y `CollapseMotion`, que son de WP-B.

El resto es disjunto: Core, App e Infrastructure (WP-A); Platform, Presentation, Bootstrap, arnés y scripts (WP-B); `Skin.uss`, `Motion.uss` y las biblias (WP-D); tokens, glifos y ASSET_SLOTS (WP-D0).

---

## 6. SQLite en Unity (gilzoide) y en dotnet (sqlite-net-pcl), hilos e IL2CPP

- **El mismo código en las dos librerías**: S3-a dio 22/22 en las dos (Anexo A.1). Los atributos de gilzoide heredan de `PreserveAttribute`, y el código de Infrastructure solo usa los atributos de sqlite-net: compila en los dos.
- **Las opciones de compilación son distintas** (§1). gilzoide usa `SQLITE_DEFAULT_WAL_SYNCHRONOUS 1`: en WAL, el valor por defecto es `NORMAL`. `e_sqlite3` (bundle_green) queda en `FULL`. Si alguna conexión de escritura no ejecuta `PRAGMA synchronous = FULL`, dotnet sigue en verde y el build pierde la durabilidad de DT-07 (H-05) sin que nada avise. gilzoide también usa `SQLITE_DQS 0`: un literal entre comillas dobles anda en dotnet y falla en Unity. Hay que probar los dos casos en las dos librerías (QA-G3-08).
- **Acceso a SQL desde los tests**: para leer `sqlite_master`, `table_info`, `foreign_key_list`, `user_version`, `journal_mode` y `sqlite_version`, DSC, DOT y SQU necesitan consultas que devuelvan filas. El diseño solo da `ExecuteRawForTestsAsync`, que no devuelve resultados, y prohíbe que otro asmdef referencie `Gilzoide.SqliteNet`. Si un test usa `RunAsync(Func<SQLiteConnection, T>)`, compila en dotnet (sqlite-net-pcl llega en forma transitiva por la `ProjectReference`) y no compila en Unity (`Tests/EditMode/RowMate.Tests.EditMode.asmdef`, con `overrideReferences: true` y sin gilzoide). La ruptura aparecería recién en WP-B (QA-G3-07).
- **Hilos**: un solo hilo dedicado con `FullMutex`, `RunContinuationsAsynchronously`, prohibido `ConfigureAwait(false)` y cierre en `OnDestroy` y en `DomainUnload`. Está bien. Falta cubrir cuatro cosas (QA-G3-09): `RecordingLogger` no es seguro entre hilos (`Tests/Support/RecordingLogger.cs:22-28`) y `SET` y `PRF` registran desde el hilo de la base; las continuaciones que llegan después de `Dispose` (el editor cerrado durante un guardado lento, o la app que se cierra); la baja de la suscripción a `DomainUnload`; y el nivel de registro de las fallas esperadas, porque `HostLogger` convierte un `Error` en una prueba fallida (`Tests/UiSupport/UiTestHost.cs:567-583`).
- **IL2CPP**: NO VERIFICABLE sin el módulo. El plan está bien: `link.xml` con los dos ensamblados enteros, `Preserve` y `DatabaseSmoke` en un *player*. Hay que usar solo `Query<T>` genérico: el camino no genérico de `FastColumnSetter` usa `MakeGenericMethod` (`SQLite.cs:3143-3147`).
- **Rutas con acentos**: sqlite-net pasa la ruta a UTF-8 antes de `sqlite3_open_v2`. Igual conviene una prueba con `ñ` en la carpeta en los dos mundos, porque es común en los perfiles de Windows de los usuarios (QA-G3-13).

---

## 7. UI Toolkit: ciclo de vida y lecciones de la It. 2

| Lección (It. 2) | En el diseño de la It. 3 | Gate |
|---|---|---|
| `rootVisualElement` es nulo en `Awake` (QA-G2-01) | Base, ejecutor y repositorio se arman en `Awake` sin tocar la UI: bien. No dice dónde se asigna la copia de `PanelTextSettings` con los respaldos de fuente | QA-G3-12: en `Awake`, sobre la copia del `PanelSettings` (antes del `OnEnable` del `UIDocument`), y la misma fábrica en `UiTestHost` y el arnés |
| *Hit-test* (cinta con `Ignore`, toques reales en las pruebas) | Capas con `Ignore`; `Tap` con *hit-test*; `.rm-highlight` y `.rm-series__stack` con `Ignore` | Falta que la página, el diálogo y el menú que salen dejen de recibir toques, como en el legacy, y la ayuda emergente con `Ignore` (QA-G3-01) |
| Unidades de USS (`letter-spacing` en 1/100 em, 1 px = 1 dp) | Sin `letter-spacing` nuevo; escalas y orígenes por código; `<size=117%>` en el subtítulo | OK |
| Transiciones de USS congeladas en batchmode; `:active` de 100 ms de reloj real | Todo el movimiento por código; USS solo bajo `:hover`; la toma falla si algo queda congelado más de 2 s | OK. Falta sondear el `TextField` del tema por defecto (`unity-theme://default`, importado en `RowMateTheme.tss:5`): transiciones propias y parpadeo del cursor en CAP-17, CAP-26 y DEMO-02/-05 (QA-G3-10) |
| `ScrollView` conserva el desplazamiento con `display: none` (medido) | Lo usa para AC-RUT-10-09 | OK |
| El foco no desplaza el `ScrollView` | No está contemplado | QA-G3-11 |
| `TextField` en runtime | S3-b midió selección, `Focus()`, clic derecho y la API de foco | Faltan Tab en multilínea, Escape y Enter con foco en un campo, doble manejo de Escape (`AppRoot.Update` + `NavigationCancelEvent`) y `parseEscapeSequences` (QA-G3-10) |
| Tamaño conocido recién después del *layout* | `StructureBarView` calcula la capacidad con el ancho | Calcularla en `GeometryChangedEvent`, y que los tests esperen el *layout* antes de leer `SegmentCount` (dentro de QA-G3-02) |

---

## 8. Regla de repo público

- **Los cinco documentos de la iteración** (spec, diseño técnico, notas de diseño y las dos deudas) cumplen la regla. Busqué rutas absolutas, nombres de usuario y datos de hardware: no hay. Las rutas son relativas, el legacy se cita por URL y aparecen `<scratch>` y `<flutter-sdk>`. 00-deuda-it2 §11 ya había sacado un dato del equipo de ADR 019.
- **CAP-27 sobre la base real** (QA-G3-06): la base sembrada tiene que ser sintética. Nunca se captura la base del usuario, porque la imagen se publica.
- **Riesgos de implementación** (QA-G3-20):
  - las líneas `[DB]`, `DatabaseSmoke` y las excepciones de `TempDatabase` no deben imprimir rutas absolutas, o los informes tienen que taparlas;
  - `capture-window.ps1` tiene que calcular `<scratch>` en el momento y no escribir rutas en `manifest.json` ni en `INDEX.md`.

---

## 9. Decisiones

**Las que me tocan a mí (QA):**

| ID | Decisión |
|---|---|
| DT-06 | **Aceptado**: atajos, acentos, AltGr y portapapeles real en [Device-Win]; en PlayMode, con `FakeClipboard`. Ninguna prueba toca el portapapeles del usuario |
| DT-12 | **Aceptado con condiciones** (QA-G3-06) |
| DD-02 (00-deuda-it2) | **Aceptado**: los `FontAsset` quedan con su tabla de kerning. La evidencia cumple lo que pedía QA-V2-07: capturas iguales byte a byte entre corridas y el `mtime` de los archivos sin cambios |
| DD-03 (00-deuda-it2) | **Aceptado**: la sonda de `bpm` se mide en CAP-12b |

**Para el PO**: DT-01, -02, -03 (texto de AC-BLE-16-02), -05, -09, -10, -13, -19; DD-01; DV-12 y CAP-13 (04-design-notes §10); la redacción de AC-HIS-20-04 (QA-G3-06); las filas nuevas de QA-G3-13.

**Para el usuario** (valores por defecto de la spec §14): P-01 (repeticiones de 1 a 999), P-02 (los números de los topes) y Q-11 (no se importa la base del legacy).

---

## 10. Issues

| ID | Severidad | Responsable | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-G3-01 | mayor | architect | El editor, los diálogos y los menús siguen recibiendo toques mientras salen. `SaveAsync` solo ignora toques con `IsSaving`, y no dice si queda bloqueado después de `Saved`. Escenario: dos toques en `Guardar` con 50 ms de diferencia; el primero guarda y empieza el `Pop` de 200 ms, el segundo cae sobre la página que sale y crea **otra** rutina (Q-19). Con `InlineDbExecutor` el guardado termina en el acto, así que `RED.DoubleTapSaveCreatesOne` solo puede fallar por esta vía. Igual con el menú: dos toques en `Eliminar` durante sus 90 ms de salida abren dos diálogos | `02-tech-design.md:502-509` (pasos 1 y 6), `:708-715` (`Push` ignora el segundo; `Pop` no dice nada); el legacy ignora los toques mientras la ruta sale: `<flutter-sdk>/packages/flutter/lib/src/widgets/routes.dart:1154-1156`, `:1220-1222` | Después de `Saved`, el *store* queda cerrado: todo `SaveAsync` posterior devuelve `Ignored`. `PageStack`, `DialogLayer` y `PopupLayer` ponen `pickingMode = Ignore` y quitan el foco del elemento al empezar su salida; `BackNavigation` lo saltea. `Tooltip` con `pickingMode = Ignore`. Pruebas: `RED.TapDuringTheExitCreatesNothing` y `NAV.MenuItemTwiceOpensOneDialog` |
| QA-G3-02 | mayor | architect | 04-design-notes llegó después de 02-tech-design y le hace 23 pedidos (DN3-01 a -23) que el diseño no incorpora. Algunos cambian el contrato de §7.5 o lo que comprueban los tests y el arnés: capacidad de la mini-barra y `SegmentWidth`/`Overflow` (DN3-07), aviso con obstáculos "que se cruzan" (DN3-13), `--in-series` 13 + riel (DN3-05), chip con cápsula (DN3-04), clases de texto (DN3-02), ancho de diálogo 400/560 (DN3-10), gracia de 120 ms del indicador (DN3-15), dueños de DN3-16. Si WP-B implementa el contrato viejo, el Designer y los checks chocan | `02-tech-design.md:767`, `:831`, `:1260`, `:1267`, `:1272-1287`; `04-design-notes.md:202`, `:348`, `:268`, `:261`, `:636-658` (§9) | Anexo en 02-tech-design ("Cambios por 04-design-notes") que acepte o rechace cada DN3, con la tabla de §7.5 y las comprobaciones de §5.4 al día. Lo de la lista, el aviso y la llave antes de B1; lo del editor y los diálogos antes de B2 |
| QA-G3-03 | mayor | po | Tres textos de la spec contradicen la corrección de contraste del Designer. AC-RUT-12-02: título "en el color del tipo". AC-RUT-14-03: `×N` en `#00B4D8`. DV-12: solo el enfriamiento en `#35A0C9`. El Designer usa `step.*.hud` en títulos e íconos (`#FF5D7F`: 3,815 → 4,685; `#37A2CB`: 2,744 → 3,723 en la insignia) y `accent.onTonal` en el número del chip (4,370 → 5,620). Un test escrito según el AC falla contra la implementación. Resuelve también DT-19 | `01-po-spec.md:207`, `:287`, `:586`; `04-design-notes.md:98-103`, `:279`, `:666` | Ampliar DV-12 con la propuesta de 04-design-notes §10 y corregir AC-RUT-12-02 y AC-RUT-14-03 antes de B2 |
| QA-G3-04 | mayor | po | CAP-13 dice "8 dp entre los dos" bloques de serie. AC-RUT-14-02 (8 arriba y 8 abajo) y el legacy dan 16. 02-tech-design copió los 8 en la comprobación del arnés: la toma fallaría con la geometría correcta, o se implementaría mal para pasarla | `01-po-spec.md:410` frente a `:286`; legacy `routine_editor_screen.dart:377` (`margin top 8`) y `:427` (`margin bottom 8`); `02-tech-design.md:943`; `04-design-notes.md:281`, `:667` | PO: CAP-13 → 16 dp. Architect: la comprobación de CAP-13 con 16 ± 1 |
| QA-G3-05 | mayor | po | DV-13 cambia AC-BLE-16-02 de la It. 2, que está cerrado (la llave sube a barra + 88 en Rutinas), pero AC-BLE-16-02 no figura en §5. Además, el AC dice "tocarlo abre el panel en ese mismo lugar", y el panel sigue anclado según AC-BLE-16-03 (barra + 16). El texto que propone el Designer no dice dónde abre el panel en Rutinas | `../02-dispositivo-simulador/01-po-spec.md:256-257`; `01-po-spec.md:352-353`; `02-tech-design.md:837`, DT-03; `04-design-notes.md:360-369`, `:668` | Sumar AC-BLE-16-02 (y AC-BLE-16-03 si cambia) a §5, con texto nuevo que diga también dónde se abre el panel en Rutinas. Llevarlo a `It03Ids` y a `SimulatorOverlayTests` |
| QA-G3-06 | mayor | architect | DT-12: CAP-27 reemplaza `rower_app.db` real del usuario y lo restaura al terminar. Eso contradice AC-HIS-20-04 y spec §7.1 ("ninguna captura escribe en las bases del usuario"). Si el script se corta entre reemplazar y restaurar, el usuario pierde sus rutinas | `01-po-spec.md:322`, `:388`; `02-tech-design.md:965-974`, `:1311` | QA acepta DT-12 con estas condiciones: (a) nunca corre solo: no forma parte de `capture-snapshots.ps1` ni del CI, y pide un parámetro explícito; (b) se niega a correr si el build o el Editor están abiertos o si hay archivos bloqueados; (c) resguarda `db`, `-wal` y `-shm` con su *hash* en `<scratch>` y verifica la copia **antes** de reemplazar; (d) restaura en `finally`, compara *hash* y fecha, y si algo falla deja la copia e imprime cómo recuperarla; (e) la base sembrada es sintética (el repo es público). El PO agrega la excepción explícita en AC-HIS-20-04 y en §7.1 |
| QA-G3-07 | mayor | architect | Los tests de esquema y apertura (DSC, DOT, SQU) necesitan consultas que devuelvan filas, pero el diseño solo da `ExecuteRawForTestsAsync` (sin resultado) y prohíbe que otro asmdef referencie `Gilzoide.SqliteNet`. Si un test usa `RunAsync(Func<SQLiteConnection, T>)`, compila en dotnet (sqlite-net-pcl llega en forma transitiva) y rompe la compilación de `RowMate.Tests.EditMode` en Unity. WP-A termina solo con dotnet, así que se descubriría recién en WP-B | `02-tech-design.md:196`, `:424-425`, `:1001-1002`, `:1042`; `Tests/EditMode/RowMate.Tests.EditMode.asmdef` (`overrideReferences: true`, sin gilzoide); `tools/dotnet/RowMate.Tests.EditMode/RowMate.Tests.EditMode.csproj:21` | `internal Task<IReadOnlyList<object?[]>> QueryRawForTestsAsync(string sql, params object?[] args)` en `RowMateDatabase`, que solo exponga tipos primitivos, y su uso en DSC, DOT y SQU. Una regla en `PureCodeRulesTests`: ningún archivo fuera de `Infrastructure/` hace `using SQLite` |
| QA-G3-08 | menor | architect | La durabilidad y el SQL dependen de opciones que cambian entre los dos SQLite. gilzoide: `SQLITE_DEFAULT_WAL_SYNCHRONOUS 1` (NORMAL en WAL) y `SQLITE_DQS 0`. `e_sqlite3`: `FULL` y comillas dobles aceptadas. Si falta el pragma en una conexión, dotnet sigue en verde y el build pierde DT-07 (H-05, AC-RUT-08-07) | `Library/PackageCache/com.gilzoide.sqlite-net@34340bf93526/Plugins/sqlite-amalgamation/sqlite3_defines.h:5`, `:7`; `02-tech-design.md:272`; DSC solo mira `journal_mode` (`:1001`) | Prueba `DSC.WriteConnectionIsWalFullAndNoForeignKeys` (`journal_mode = wal`, `synchronous = 2`, `foreign_keys = 0`) que corra en dotnet y en Unity EditMode. Literales solo entre comillas simples |
| QA-G3-09 | menor | architect | Detalles de hilos sin cubrir. (a) `RecordingLogger` usa `List.Add` sin lock y `SET`/`PRF` registran desde el hilo de la base: pruebas inestables. (b) Continuaciones después de `Dispose`: el editor cerrado con "Atrás" durante un guardado lento, o la app que se cierra. (c) Baja de `DomainUnload` en `Dispose`. (d) El nivel de las fallas esperadas: solo la de carga dice `Warning`, y `HostLogger` convierte un `Error` en una prueba fallida (guardar y borrar) | `Tests/Support/RecordingLogger.cs:22-28`; `Tests/UiSupport/UiTestHost.cs:567-583`; `02-tech-design.md:347-352`, `:456` | Logger con lock (o uno propio de las pruebas de hilos); guardia `_disposed` en los *stores* y en las páginas; desuscribir en `Dispose`; `Warning` para las fallas de cargar, guardar y borrar, o `LogAssert.Expect` en esas pruebas |
| QA-G3-10 | menor | architect | Comportamientos de `TextField` en runtime que S3-b no sondeó y que tocan los AC: (a) Tab en la descripción multilínea: ¿inserta `\t` o mueve el foco? El legacy mueve el foco (AC-APP-05-07). (b) Escape con foco en un campo: UI Toolkit puede revertir el valor o quitar el foco (P-08: Escape no hace nada en el editor). (c) Escape manejado dos veces: `AppRoot.Update` y `NavigationCancelEvent`. (d) Enter en un campo de una línea. (e) Parpadeo del cursor y transiciones del tema por defecto en batchmode (determinismo de CAP-17, CAP-26 y DEMO-02/-05). (f) `parseEscapeSequences`: DN3-22 solo pide `enableRichText = false` | `02-tech-design.md:1216` (S-B3a), Anexo A.2; `Bootstrap/AppRoot.cs:155-159`; `Presentation/Styles/RowMateTheme.tss:5`; `UnityEngine.UIElementsModule.xml` (`TextElement.parseEscapeSequences`); `04-design-notes.md:657` | Sumar los seis casos a S-B3a. `parseEscapeSequences = false` junto con `enableRichText = false` en tarjeta, diálogo de borrado y campos. Pruebas con `<b>A</b>` y `Serie\n2` (barra invertida y `n` escritas a mano) en el nombre |
| QA-G3-11 | menor | architect | Un `ScrollView` de UI Toolkit no lleva a la vista al hijo que recibe el foco por Tab. Con 60 filas en el editor, o con el diálogo de paso a 640 × 360, el foco queda fuera de pantalla (AC-APP-05-07, AC-RUT-13-13, -13-15) | `02-tech-design.md:885`; `04-design-notes.md:478` (solo para lo nuevo, el error y el teclado) | `ScrollTo` en `FocusInEvent` dentro de `#routines-list`, `#editor-scroll` y `.rm-dialog__content` (resorte ω 22 y 16 dp de aire, en el acto con reducir movimiento). Prueba `NAV.TabScrollsTheFocusedIntoView` |
| QA-G3-12 | menor | architect | No dice dónde se crea ni dónde se asigna la copia de `PanelTextSettings` con los respaldos de fuente (`trending_up`, íconos, Noto Emoji). Si el arnés o `UiTestHost` no la tienen, PlayMode y las capturas dibujan `□`. El `FontAsset` de Noto Emoji tiene que entrar en `FontAssetPaths.All` y en `GeneratedAssetGuard`, o vuelve QA-V2-07 | `02-tech-design.md:807`; `04-design-notes.md:379`, `:656`; lección QA-G2-01 (`../02-dispositivo-simulador/03-qa-harmony.md:77`) | Una fábrica única que usen `AppRoot.Awake` (sobre la copia del `PanelSettings`, antes de su `OnEnable`), `UiTestHost` y el arnés. Sumar el `FontAsset` nuevo a la lista protegida |
| QA-G3-13 | menor | architect | Casos borde sin regla ni test. (a) Split ≤ 0 contra el tope: `Split min` `-200` da −12 000; en el legacy es "sin split" (`:557-562`); en el diseño, ¿error o null? (b) `-99999999999999999999` en repeticiones: el legacy da ×1; el diseño, "sin cambio". (c) Progresiones ocultas de un paso suelto por encima del tope (F-RARA): `Confirm()` o el guardado no pueden marcar un error en un campo que no se ve. (d) Min/Seg ocultos en modo distancia: no se validan. (e) Encabezado de menos de 100 bytes que empieza con la firma de SQLite → `Unreadable`. (f) Una palabra de 200 caracteres sin espacios tiene que ajustar (AC-RUT-10-03). (g) Ruta con `ñ` | `02-tech-design.md:554-563`, `:588-595`, `:291-294`; `01-po-spec.md:165`, `:248`, `:277` | Filas nuevas en `SFT`, `RCT`, `DOT`, `RLT` y `TDT` (dotnet y Unity). El PO confirma (a) y (b); propongo "sin split" en (a), que es lo del legacy y no rompe nada, y "sin cambio" en (b), que es coherente con ≤ 0 |
| QA-G3-14 | menor | architect | AC-HIS-17-03 tiene solo "qa eq": `run-tests.ps1 -Matrix` lo va a dar como SIN TEST (la It. 2 cerró con 0). Además, el nombre de la prueba de AC-HIS-20-02 difiere entre §2.5 y §6.3 | `tools/dotnet/run-tests.ps1:164-170`; `02-tech-design.md:1160`, `:331` (`ModesDoNotMix`) frente a `:1165` (`VariantsDoNotMix`) | Un test estático dB (ningún código nombra `com.example`, `.dart_tool`, `shared_preferences` ni `rower_app` fuera de `DatabaseFiles`) o un `[Ignore("NO VERIFICABLE en automático: …")]` con su categoría, como AC-APP-09-07 en la It. 2. Unificar el nombre |
| QA-G3-15 | menor | architect | Los dueños de los archivos no son disjuntos: `Tests/EditMode/**` [A] contra los tests de diseño [D0] e `ItThreeToolingTests` [B]; `Typography.uss` no figura; Noto Emoji y `FontAssetPaths` no tienen dueño | `02-tech-design.md:162`, `:1043`, `:1221-1229`; `04-design-notes.md:682-685` | Excepciones explícitas en §1.1 y §7 |
| QA-G3-16 | menor | architect | Android: `hideMobileInput = true` (sin la caja extra) y "el menú de copiar y pegar del sistema" (AC-RUT-11-10, P-06) probablemente no se pueden tener juntos en Unity. Con la entrada oculta, el sistema no muestra su barra de selección sobre un campo que dibuja UI Toolkit. Tampoco está probado que `TouchScreenKeyboard.area` sea confiable. Es NO VERIFICABLE hoy, pero no figura como riesgo | `01-po-spec.md:202`; `02-tech-design.md:872`, `:879`; R3-02 solo habla del módulo | Riesgo R3-13 con alternativa: menú propio al mantener presionado, con textos del catálogo (el mismo del escritorio), y la variante de P-06 para el PO. Se verifica en [Device-And] |
| QA-G3-17 | menor | po | Diferencias visibles que no están en §11.1: DT-01 (los campos del editor se desplazan con la lista) y DT-02 (las ayudas se ven sin foco; el legacy las muestra solo con foco, `input_decorator.dart:2346`). Quedan además para el PO DT-05, DT-09, DT-10, DT-13 y DD-01 | `02-tech-design.md:1300-1313`; `00-deuda-it2.md:250` | Registrar DV-18 y DV-19, o pedir paridad. Confirmar las demás en el cierre del gate |
| QA-G3-18 | menor | designer | (a) 04-design-notes §10 dice que WP-D0 necesita instalar Python, pero `uvx` está y la receta de ASSET_SLOTS §4.1 ya lo usa. (b) R3-06 (kerning de `ñ`, `¿`, `¡`, `×`, `±` en `FontAsset` dinámicos: depende del orden en que aparecen los glifos, 00-deuda-it2 §3) quedó asignado al Designer y 04-design-notes no lo decide | `04-design-notes.md:672`; `../../design/ASSET_SLOTS.md:254`; `02-tech-design.md:1332`; `00-deuda-it2.md:101-115` | (a) Usar `uvx --from fonttools`. (b) Decidir: generar el kerning de un juego fijo dentro de `AllowWrites`, o aceptarlo y comparar capturas solo entre corridas completas del catálogo |
| QA-G3-19 | menor | architect | `SequenceClock` "un minuto más **por lectura**" (§3.2) contra spec §0.4 "por cada **rutina creada**". Si `SeriesIdGenerator` u otro lee el mismo reloj, las fechas se corren y el "mismo milisegundo" de AC-RUT-08-06 no se puede armar en PlayMode | `02-tech-design.md:463`, `:629`; `01-po-spec.md:56`, `:147` | Relojes separados en `UiHostOptions` (creación por un lado, *scheduler* para los ids de serie). `SIG` con reloj fijo |
| QA-G3-20 | menor | architect | Repo público: las líneas `[DB]`, la salida de `DatabaseSmoke` (`[DB] humo: ok \| <error>`) y las excepciones de `TempDatabase` pueden incluir la ruta absoluta de la PC; `capture-window.ps1` guarda copias de resguardo | `02-tech-design.md:325`, `:969-973`, `:992` | Registrar solo el nombre del archivo, o la ruta relativa a la carpeta de datos; `<scratch>` calculado en el momento; ninguna ruta en `manifest.json` ni `INDEX.md`. QA lo revisa en la verificación |
| QA-G3-21 | menor | architect | `PRF.NoLongFramesWhileLoadingOrSaving` mide cuadros en PlayMode en batchmode, donde el tiempo de cuadro depende de la máquina: la prueba puede ser inestable | `02-tech-design.md:1037`, `:1163`, R3-09 | Medir el tiempo de nuestro código en el hilo principal por cuadro (`Stopwatch`), con un umbral declarado; la sensación de fluidez queda en [Device-Win] |

---

## 11. Lo que este gate no verificó

- **Unity no se abrió.** Lo de UI Toolkit en runtime se apoya en S3-b del Architect y en la documentación XML de `UnityEngine.UIElementsModule` 6000.3.27f1.
- **IL2CPP y Android**: NO VERIFICABLE sin los módulos ni el teléfono.
- **Comportamientos de Flutter**: los leí en el código del SDK local, sin ejecutarlos.
- **AC-BLE-11-02**: tomo la evidencia de 00-deuda-it2 §9.1; lo re-verifico en 05-qa-verification.
