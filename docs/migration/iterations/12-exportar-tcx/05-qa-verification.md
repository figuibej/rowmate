# It. 12 — Exportar TCX: verificación de QA

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-09 |
| Entrada | [01-po-spec](01-po-spec.md) (28 AC), [02-tech-design](02-tech-design.md) (§13 y §14 con los desvíos), [03-qa-gate](03-qa-gate.md), [04-design-notes](04-design-notes.md), [snapshots/](snapshots/) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `3ae0f3a`, solo lectura (`tcx_builder.dart`, `profile_provider.dart:145-200`, `workout_session.dart:100-140`). No se leyó el módulo de credenciales. `git status --porcelain` vacío antes y después |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 FAIL, 0 bloqueantes, 0 mayores; 6 menores |

## 1. Corridas (conteos reales)

| Corrida | Resultado |
|---|---|
| `run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it12tcx/qa-r1` | Compila sin advertencias. Core: **3886** (3847 pasan, 39 omitidas, 0 fallan); herramientas: **194** (194 pasan). Las 39 omitidas son las de la línea de base del gate: ninguna de la 12 |
| Unity EditMode | **3999**: 3951 pasan, 0 fallan, 48 ignorados |
| Unity PlayMode | **660**: 660 pasan |
| Unity EditMode `-Simulator` | **4193**: 4145 pasan, 0 fallan, 48 ignorados |
| Unity PlayMode `-Simulator` | **735**: 735 pasan |
| Matriz con `-UnityResults` (los 4 XML) | 1048 IDs: PASA 1029, FALLA 0, IGNORADO 4, PENDIENTE UNITY 0, SIN TEST 15 (de la 12: solo AC-STR-09-25 [Apple] y -27 [Manual]) |
| `native/android/check.ps1` | OK: 9 fuentes Kotlin (7 BLE, 2 de `RowMateExport`) contra android-36; firmas JNI (24) y de `RmExport` (2) iguales a sus golden |
| `build.ps1 -Target Windows` (normal) | Éxito. Inspector: "sin credenciales ni red de Strava" y "sin rastros del simulador, de diagnósticos ni de tests" |
| `build.ps1 -Target Windows -Simulator` | Éxito. Inspector de credenciales en verde |
| `build.ps1 -Target Windows -ExportSmoke` | **ok**: `RowMate_2026-11-05_1840_Escalera.tcx` y `…_Escalera_2.tcx` en `ROWMATE_DATA_DIR\exports`; XML bien formado, 8 vueltas, 133 puntos, 53 612 bytes, igual byte a byte al de `tools/sessions tcx`; "Documentos sin cambios" |
| Guardias | ProjectSettings sin cambios en todas las corridas; `HorizonFar.mat`/`HorizonNear.mat` sin cambios. `Documentos\RowMate` **no existe** antes ni después de todas las corridas y builds (listado propio): nada se escribió en las carpetas del usuario. No se abrió el Explorador ni el build normal |

`TcxExportFrameTests` (exportador real de Windows sobre una carpeta temporal): aviso a los 317 ms, peor cuadro de trabajo 2,4 ms.

## 2. Paridad del TCX con el legacy (punto 4)

Port literal de `TcxBuilder.build` del legacy a Dart (en `<scratch>`, sin `StravaMetadata`, con la reparación de `pp:159-188` en memoria), corrido con el `DateTime` local y el `round()` de Dart reales, contra `TcxBuilder` de Unity con las mismas sesiones (zona del equipo, UTC−03:00):

| Sesión | Vueltas | Puntos | Unity sin `<Notes>` vs. legacy |
|---|---|---|---|
| T-1 | 3 | 6 | **idéntico byte a byte**; además Unity = golden `Tests/Golden/tcx/T-1.tcx` |
| T-3 (cortada) | 1 | 3 | idéntico |
| 3 h | 19 | 2 160 | idéntico |
| F-9 n.º 1, 2, 3, 5, 6 | 8, 1, 2, 1, 3 | 133, 362, 36, 746, 49 | idénticos |
| T-2, T-6 (tiempo 0) | 1 | 1 | el legacy lanza `UnsupportedError` (`Infinity.round()`); Unity da `Calories` 0 (DV-119, decidido) |

Única diferencia: `<Notes>` (nombre de la rutina, escapado) después de la última vuelta (DV-116/-117); el legacy ponía ahí los metadatos de Strava antes de la primera. Las 10 salidas de Unity se validaron con `XmlReader` (DTD prohibida): bien formadas, raíz en el espacio de nombres TCX v2, una `Activity`, `Notes` como último hijo de `Activity` (orden de `Activity_t`). El archivo del humo del build (F-9 n.º 1) mide lo mismo que el de esta comparación.

## 3. Capturas (punto 2)

Revisadas las 12 de [snapshots/](snapshots/) contra §7 de la spec y 04-design-notes: botón de descarga a la derecha del título (CAP-01 a -03, mismo peso que la flecha de volver), indicador en su lugar (CAP-04), aviso de éxito con check, carpeta abreviada, nombre del archivo y `Abrir carpeta` (CAP-05, -06, -10 en inglés a 1,3×, -11), error sin rojo con `Reintentar` (CAP-07, -08), sin botón en la n.º 4 (CAP-09) y ayuda `Exportar TCX` al pasar el cursor (CAP-12). `manifest.json`: 12 tomas `passed`, sin desbordes ni comprobaciones fallidas. Ninguna muestra rutas ni datos del equipo. `capture-snapshots.ps1 -Set it12` escribe en `12-exportar-tcx/snapshots` (línea 173) y ninguna carpeta de capturas de otra iteración cambió. Observaciones: m-01 y m-03.

## 4. Matriz AC → resultado

| AC | Resultado | Evidencia |
|---|---|---|
| 01 Estructura y golden | PASS | `TB.T1EqualsGoldenByteForByte`, `.Utf8NoBomLfFinalNewline`; §2; `.gitattributes` `*.tcx eol=lf` aplicado |
| 02 Fecha y hora | PASS | `TT.TruncatesMilliseconds`, `TB.IdLapAndPointTimesFromStart`, `ES.ZoneAtExportTime` |
| 03 Desfases | PASS | `TT.T2InUtcKolkataKathmanduStJohns`, `.T2MEachInstantHasItsOwnOffset` en .NET 8 **y** en Mono (Unity EditMode) |
| 04 Vueltas | PASS | `TL.*` (F-9: 8/1/2), `TB.SameFileWithoutRoutine`; §2 |
| 05 Datos de la vuelta | PASS | `TB.LapFieldsInOrder`, `.SinglePointLapZeroZero`, `.NegativeDistanceKept`, `.GapBetweenLapsNotCounted` |
| 06 Calorías por vuelta | PASS | `TR.LapCaloriesHalfAwayFromZero` (50/3/2/1), `.ZeroTimeGivesZero`; §2 (round de Dart) |
| 07 Sesión cortada | PASS | `TR.CutSessionUsesPointMaxima`, `TB.CutSessionCaloriesFromPoints`, `ES.NeverWritesTheRepository`; T-3 = legacy reparado |
| 08 Puntos | PASS | `TB.OptionalFieldsOnlyWhenPositive`, `.CadenceHalfAwayFromZero` (24.4/24.6/22.5/23.5) |
| 09 Notas | PASS | `TB.NotesAfterLastLap`, `.NoNotesForFreeOrBlank`, `.SimNotes`, `.NoRowmateComment`, `TX.*` |
| 10 Exportables | PASS | `TR.IsExportable`, `TB.T6OneLapZeros`, `SDS.CanExport…`, `TED.NoButtonWithoutTelemetry`, CAP-09 |
| 11 Determinista, solo lectura | PASS | `TB.Deterministic`, `ES.NeverWritesTheRepository`, `TED.HistoryUnchangedAfterExport`, `T12.ExportCodeHasNoNetwork` |
| 12 Rápido | PASS | `TB.ThreeHoursUnder200Ms` (23 ms); `TEF`: 317 ms y 2,4 ms (con F-9 n.º 5, ver m-02) |
| 13 Nombre | PASS | `FN.*` (tabla de la spec, `TrimsDashAfterCut`, pares sustitutos); humo del build |
| 14 Sin colisiones | PASS | `FW.CollisionSuffixCaseInsensitive`, `.FailureLeavesNoPartialNorTemp`, `.RaceOnMoveTakesNextSuffix`; humo `_2` |
| 15 Simulador | PASS | `FN.SimBeforeCollisionSuffix`, `ES.SimulatorBuildMarksNameAndNotes`, PlayMode `-Simulator` |
| 16 Carpeta en Windows | PASS | `FL.*`, `ELT.DataDirGoesToExportsNeverDocuments`, `.WindowsUsesMyDocumentsRowMateTcx`, `.ResolveCreatesNothing`; humo |
| 17 Botón | PASS | `TED.ButtonOnlyWhenExportable`, `.NoButtonOnLoadError`, `.TooltipIsAccessibleName`, `.TabThenEnterOrSpaceExports`, `.LongTitleEllipsizesBeforeButton`, `.ShareDeliveryShowsShareGlyph`; CAP-01 a -03, -09, -12 |
| 18 Exportando | PASS | `ES.BusyAtLeast300Ms`, `.SecondRequestSameTask`; `TED.SpinnerEighteenByTwoAndDisabled`, `.BackDuringExportStillNotifies`, `.ReopenShowsBusy`; CAP-04 |
| 19 Aviso de éxito | PASS | `TAT.SuccessTwoLinesSixSecondsWithAction`, `.NeverFullPathOrUserName`, `.WrapsNeverEllipsis`; CAP-05, -06, -10, -11 |
| 20 Abrir carpeta | PASS | `TAT.OpenFolderRevealsAndHides` (destino falso), `ERT.SelectWhenFileExistsElseFolder`. El Explorador real, en la demo del usuario |
| 21 Error al escribir | PASS | `ES.FailureNoticeAndScrubbedLog`, `LG.NoUserPathInMessage`, `TAT.RetryExportsAgain`, `.ButtonBackToIcon`; CAP-07, -08 |
| 22 Avisos y otras pantallas | PASS | `TAT.RowerHrmAndCommonReplaceExportNotice`, `.StickyReturnsAfterSixSeconds`, `.ReducedMotionNoMovement`; CAP-10, -11 |
| 23 Textos | PASS | `L10.SevenItTwelveKeys`, paridad de 252 claves, `T12.ProfileKeysUnusedInPresentation`, `TED.AllTextsFromCatalog` |
| 24 Android | NO VERIFICABLE | [Device-Android] sin "Android Build Support". Lo verificable pasa: `check.ps1`, manifiesto sin permisos y proveedor `exported=false` (`T12`), purga de 24 h (`FW.DeleteOlderThan*`), tipo y título del pedido (`ES.RequestHasTcxMimeAndShareTitle`) |
| 25 Apple | NO VERIFICABLE | [Apple], 10b |
| 26 Build de Windows | PASS | `build.ps1 -ExportSmoke` (§1); `Documentos\RowMate` no existe antes ni después |
| 27 Subida a Strava | NO VERIFICABLE | [Manual], demo del usuario (§1 de la spec, pasos 4-5) |
| 28 Nada de Strava | PASS | 4 pestañas (tests del shell), `BOI.*`, `T12.InspectorListsCredentialNeedles`, `.NoCredentialNeedleInShippedSources`; inspector de los builds normal y simulador en verde |

**Total: 25 PASS, 0 FAIL, 3 NO VERIFICABLE.** Los mayores del gate quedaron resueltos: QA-G12-01 (agujas de credenciales, `BOI` y `T12`) y QA-G12-02 (`FW.DeleteOlderThan*`, `ES.RequestHasTcxMimeAndShareTitle`). De los menores del gate, m-02, m-03, m-05, m-06, m-07, m-08, m-09 y m-10 están hechos.

## 5. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V12-01 | menor | designer | En TV el aviso cae sobre el gráfico y la cápsula de vidrio deja ver las líneas y las etiquetas del eje detrás del texto; el nombre del archivo se superpone con la etiqueta `50` | CAP-06 | Fondo más opaco para el aviso con acción (o revisar el contraste con el gráfico detrás) |
| QA-V12-02 | menor | architect | AC-STR-09-12 pide la medida en la app con una sesión de 3 h (2 160 puntos); `TEF` usa la n.º 5 (746 puntos). El margen es enorme (317 ms, 2,4 ms) | `Tests/PlayMode/TcxExportFrameTests.cs:18` | Sembrar `TcxSeeds.ThreeHours` en `TEF` |
| QA-V12-03 | menor | po | CAP-11 y AC-STR-09-22 hablan de "sobre la barra de pestañas", pero el Detalle es una página completa sin barra de pestañas (It. 9): el aviso queda al pie, sobre el contenido | CAP-11, CAP-05 | Ajustar el texto de la spec ("al pie, sin tapar la AppBar") |
| QA-V12-04 | menor | po | Secciones viejas del ROADMAP siguen diciendo que Strava es de la 12 y la 13 ("Perfil (It. 12-13)", "subida a Strava al terminar (It. 12)", "Indicadores y subida a Strava (It. 12)…") | `ROADMAP.md:120`, `:147`, `:231`, `:233`, `:264`, `:412`, `:418` | Agregar "(descartado)" o reescribir; el STATUS se actualiza al cerrar (hoy dice "diseño técnico… de la It. 12" en "Próximo paso") |
| QA-V12-05 | menor | architect | Comentario del árbol del Detalle con la sangría rota (el renglón nuevo empieza en la columna 0) | `Presentation/History/SessionDetailPage.cs:22` | Sangrar como los demás |
| QA-V12-06 | menor | po | Un monitor que reinicia la distancia daría `DistanceMeters` negativo en la vuelta, que el esquema TCX no admite (paridad decidida) | spec §10.6; `TcxBuilder.cs` (`last − first`) | Ya está en "Para issues" de la spec; subirlo como issue |

## 6. Repo público, codificación y documentos

- `git grep` (versionado y sin versionar, sin PNG) de rutas de la PC, nombre de usuario, carpeta de `<scratch>` y zona del equipo: sin resultados en lo de la 12 (solo la ruta genérica de Unity Hub de documentos viejos y una prueba de higiene). Golden `T-1.tcx` y semillas con datos inventados. Capturas sin rutas. **public_repo_clean = sí.**
- Los archivos nuevos y cambiados son UTF-8 sin BOM y sin doble codificación. El golden queda con LF (`git check-attr`: `eol: lf`).
- Legacy intacto (`git status --porcelain` vacío).
- ROADMAP y STATUS sin Strava activo: la 13 figura DESCARTADA, la 12 es "Exportar TCX" con 1 ID, 133 vivos; ver QA-V12-04.

## 7. Para issues

1. QA-V12-01 a -06.
2. Lo de la spec §10, el diseño §11 y las notas de diseño §6 (acción en la tarjeta, Guardar como, MediaStore, FIT y `<Creator>`, glifos de la fuente, pausa del aviso con el puntero, restos de Strava).
