# It. 12 — Exportar TCX (diseño técnico)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-09 |
| Iteración | 12 · `12-exportar-tcx` ([ROADMAP §It. 12](../../ROADMAP.md)); reemplaza a la vieja It. 12 de Strava (descartada por el usuario el 2026-11-09) |
| Entrada | [01-po-spec](01-po-spec.md) (28 AC, CAP-01 a -12, DV-116 a -127); [00-arte-exportar](00-arte-exportar.md) (D12-01 a -08, A12-01 a -08); [09/02-tech-design](../09-historial/02-tech-design.md) (detalle, store, base); [11/02-tech-design](../11-ble-pulsometro/02-tech-design.md) (avisos en el `ToastLayer`) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`): `lib/core/strava/tcx_builder.dart` entero; `lib/core/models/workout_session.dart:1-200`; `lib/features/profile/profile_provider.dart:150-195`; `lib/features/history/session_detail_screen.dart:70-135`. No se leyó el módulo de credenciales. `git status --porcelain` del legacy, vacío |
| Código leído (este repo) | `Core/Models/{WorkoutSession,DataPoint,SessionStats}.cs`, `Core/Ports/ISessionRepository.cs`, `Core/Text/{IsoDate,DartMath}.cs`, `Core/Design/{Icons,HeartGlyph,CheckGlyph}.cs`, `App/History/SessionDetail{Store,State}.cs`, `App/Composition/{AppComposer,AppContext,AppServices}.cs`, `Bootstrap/{DataLocation,BuildFlags}.cs`, `Bootstrap/AppRoot.cs` (composición), `Platform/{PlatformServices,PlatformFactory}.cs`, `Platform/Storage/AppDataLocation.cs`, `Platform/Time/LocalOffsetSource.cs`, `Presentation/History/SessionDetailPage.cs`, `Presentation/Shell/{ToastLayer,AppBarView,HrmNoticePresenter}.cs`, `Presentation/Controls/{IconButton,IconView,Tooltip,Spinner,RmButton}.cs`, `Editor/Build/{BuildOutputInspector,BuildRequest}.cs`, `Snapshots/{HistoryCatalog,SnapshotCatalog,ScenarioScript}.cs`, `Tests/UiSupport/UiHostOptions.cs`, `Tests/Support/HistorySeeds.cs`, `Assets/Plugins/Android/RowMateBle.androidlib/AndroidManifest.xml`, `native/android/check.ps1`, `tools/build/build.ps1`, `tools/sessions/Program.cs`, `tools/unity/capture-snapshots.ps1`, `.gitattributes` |
| Estado | Diseño compacto, **sin código**. Orden: **WP-A** → (**WP-B1** ∥ **WP-D**) → **WP-B2** (§8) |

Convenciones: rutas relativas a `Assets/RowMate/` salvo `Assets/Plugins/`, `native/`, `tools/` y `docs/`. **N** nuevo, **C** cambia. `tcx` = `tcx_builder.dart`, `ws` = `workout_session.dart`, `pp` = `profile_provider.dart`, `sd` = `session_detail_screen.dart`.

---

## 0. Resumen

1. **El archivo es Core puro y se prueba con dotnet.** `TcxBuilder` porta `tcx` línea por línea (mismas cadenas, mismo orden, `round()` de Dart) con los cambios decididos por la spec: sin metadatos, `Notes` después de la última vuelta, vueltas sin la rutina, tiempo 0 sin error, escape de los caracteres inválidos y marca del simulador. La zona horaria se inyecta (`TimeZoneInfo`) y **cada instante** se convierte con su desfase.
2. **Exportar solo lee.** `TcxExportService` (App, uno por app) relee los puntos por el repositorio (hilo de la base), arma el nombre y le pasa al exportador **una función** que produce los bytes. El exportador de la plataforma la corre fuera del hilo principal, escribe sin pisar y sin dejar archivos a medias (`FolderExportWriter`, Infrastructure) y devuelve dónde quedó.
3. **Un solo puerto, `IFileExporter`** (Core/Ports): Windows guarda en `Documentos\RowMate\TCX` y abre el Explorador con el archivo seleccionado; Android escribe en la caché y abre la hoja de compartir (Kotlin propio, sin dependencias ni permisos); las pruebas y el arnés usan `FakeFileExporter`, que escribe en la carpeta temporal y solo **registra** "abrir carpeta". `ROWMATE_DATA_DIR` (Editor y desarrollo) manda la exportación a `{ROWMATE_DATA_DIR}\exports`.
4. **Vistas**: un botón de ícono en la AppBar del Detalle (manda la spec sobre el arte), con un glifo **dibujado** (descarga en Windows, compartir en Android), indicador de 18/2 mientras exporta y ayuda `Exportar TCX`. Los avisos van en la cápsula del `ToastLayer` con un segundo renglón y una acción (`ShowAction`), con la prioridad de un aviso común.
5. **Nada de Strava**: ni red, ni cuentas, ni pestaña. Los restos de Strava en el código y la arquitectura se quedan y van a issues (§11).

### 0.1 Decisiones del Architect (una línea cada una)

| ID | Decisión |
|---|---|
| DT12-01 | `TcxBuilder.Build(session, points, options)` devuelve `string` con `\n`; `TcxDocument.Encode` lo pasa a UTF-8 **sin BOM**. Sin `XmlWriter`: la salida tiene que ser idéntica a la del legacy, línea por línea (`tcx:18-77`) |
| DT12-02 | Hora: `TcxTime.Format(instant, zone)` = `TimeZoneInfo.ConvertTime(instant, zone)` y los componentes con `D4`/`D2`, sin milisegundos y `+00:00` para cero (`tcx:130-143`). El instante de un punto o de una vuelta es `StartedAt + ElapsedSeconds` (`tcx:45`, `:173`) |
| DT12-03 | Zona = la del equipo **al exportar**: `AppServices.ExportZone` (`Func<TimeZoneInfo>`). En la app, `TimeZoneInfo.ClearCachedData()` + `TimeZoneInfo.Local` en el hilo principal, una vez por exportación (no `LocalOffsetSource`, que da el desfase de **ahora** y no el de cada instante). En pruebas y en el arnés, `HistorySeeds.Zone` y las zonas de `TestZones` |
| DT12-04 | Base de calorías (AC-STR-09-07, DV-119): si `FinishedAt == null` o `TotalTimeSeconds == 0`, tiempo y calorías = máximos de los puntos (`SessionStats.Compute`, la reparación de `pp:173-188` **en memoria**); si no, los de la sesión. Tiempo ≤ 0 → 0 kcal. Fórmula en `double` en el orden de Dart: `cal × n / (t / 5.0)` y `DartMath.Round` |
| DT12-05 | Exportable (AC-STR-09-10) = `Stats.TotalTimeSeconds > 0 || Stats.TotalDistance > 0` con al menos un punto (`pp:159-170`). Lo expone `SessionDetailState.CanExport` (propiedad calculada desde `Phase == Ready` y `Stats`: **sin** campo nuevo en el registro) |
| DT12-06 | El servicio relee **los puntos** (`GetPointsAsync`, hilo de la base) y usa la sesión del estado del Detalle (ya releída por la It. 9). Nunca llama a una escritura del repositorio (`ScriptedSessionRepository` lo cuenta) |
| DT12-07 | Un puerto `IFileExporter` con `Delivery` (`None` / `SaveToFolder` / `Share`), `ExportAsync(ExportRequest)` y `Reveal(ExportedFile)`. **A12-05 entra acá**: no hay `IFolderRevealer` aparte |
| DT12-08 | El TCX se arma **dentro** del exportador: `ExportRequest.Content` es un `Func<byte[]>` que el exportador real corre en `Task.Run` (con datos inmutables: registros y listas que nadie modifica). El falso lo corre en línea. Así ni App ni Core ven hilos (ARCHITECTURE §6) |
| DT12-09 | Escritura atómica y sin colisiones en `Infrastructure/Export/FolderExportWriter` (netstandard2.1, probado con dotnet sobre carpetas temporales): enumera la carpeta (comparación `OrdinalIgnoreCase`, también en Android), escribe `~rowmate-{guid:N}.part`, `Flush(true)`, `File.Move` sin pisar (si otro lo tomó, siguiente sufijo, hasta 999) y borra el `.part` en `finally` |
| DT12-10 | Carpetas (`Bootstrap/ExportLocations`): Editor y desarrollo con `ROWMATE_DATA_DIR` → `{dir}\exports` con etiqueta fija `ROWMATE_DATA_DIR\exports` (la cadena se compila **solo** bajo `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`: el build normal no puede tenerla, `BuildOutputInspector`); Windows → `Environment.GetFolderPath(MyDocuments)` + `RowMate\TCX` (respeta la redirección de OneDrive) con la etiqueta de Documentos; Android → `getCacheDir()/exports` (sin etiqueta: no hay aviso) |
| DT12-11 | `TcxExportService` vive en `AppContext.Export` (no en la página): salir del Detalle no cancela y el aviso igual llega (AC-STR-09-18). Una exportación en curso por sesión: un segundo pedido devuelve la misma tarea |
| DT12-12 | El indicador dura **al menos 300 ms** (spec) con `IScheduler.Delay` del resto, antes de publicar el resultado. `ExportMotionSpecs.MinBusyMs = 300` en Core (A12-03 con el número de la spec) |
| DT12-13 | Avisos: `TcxExportService.Notices` (`EventStream<TcxExportNotice>`: `Saved` o `Failed`; `Share` no avisa). Los muestra `ExportNoticePresenter` (Shell, como `HrmNoticePresenter`) con `ToastLayer.ShowAction`. Reintentar vuelve a llamar `ExportAsync` con la misma sesión |
| DT12-14 | `ShowAction` tiene la prioridad de un aviso **común** (`ToastSlot.Common`): lo reemplazan `Show`, `ShowLink` y `ShowHrm`; un fijo del remo vuelve al vencer, como en la It. 11 (AC-STR-09-22) |
| DT12-15 | Nombre: fecha y hora del inicio **en la zona de exportación**; nombre de la rutina normalizado a NFC y saneado (`char.IsLetterOrDigit(string, i)`, que acepta pares sustitutos); el tope de 40 cuenta unidades UTF-16 sin cortar un par; una libre usa `historyFree` del idioma vigente; un nombre solo de espacios no lleva `Notes` |
| DT12-16 | Simulador: `AppServices.SimulatorBuild` (AppRoot pasa `BuildFlags.IsSimulator`): `_SIM` en el nombre y `[SIM]` en `Notes`. El build normal nunca la lleva |
| DT12-17 | Botón: `ExportTcxButton` en `AppBar.Actions` (manda AC-STR-09-17 sobre D12-01). La caja de 48 de `IconButton`, el glifo **dibujado** `ExportGlyph` (no hay `download` ni `share` en el recorte de la fuente, D12-03) y un `Spinner(18, 2)` en su lugar mientras exporta. Sin `Delivery` (`None`) o sin id de sesión: no hay botón |
| DT12-18 | Android: un `ContentProvider` propio (`RmExportProvider`, `exported=false`, `grantUriPermissions=true`, solo lectura de `cache/exports`) en lugar de `androidx.core.FileProvider`: no suma dependencias a Gradle. `ACTION_SEND` + `createChooser(title)` con `FLAG_GRANT_READ_URI_PERMISSION`. Los `.tcx` de más de 24 h se borran antes de escribir. Sin permisos nuevos |
| DT12-19 | Registro con tag `EXPORT`: `[EXPORT] TCX guardado: {archivo} en {etiqueta}` (Info) y `[EXPORT] no se pudo exportar: {Tipo}: {mensaje}` (Warning). `ExportLog.Scrub` cambia la carpeta real por su etiqueta y la carpeta del usuario por `%USERPROFILE%` antes de registrar |
| DT12-20 | Humo del build: `-rowmate-export-smoke <n>` (solo desarrollo) exporta la sesión más nueva `n` veces sin UI y registra los nombres; `build.ps1 -ExportSmoke` siembra F-9 con `tools/sessions seed`, corre el player con `ROWMATE_DATA_DIR` temporal y compara el primer archivo, byte a byte, con `tools/sessions tcx` (dotnet, la misma zona local) |

---

## 1. Árbol de archivos

### 1.1 WP-A: Core, App e Infrastructure puros (dotnet)

```
Core/Export/TcxBuilder.cs                         N  puerto de tcx (§2.1)
Core/Export/TcxOptions.cs  TcxLap.cs  TcxLaps.cs  N  zona + simulador; agrupación de vueltas (§2.2)
Core/Export/TcxTime.cs  TcxXml.cs  TcxDocument.cs N  fecha con desfase; escape y limpieza; UTF-8 sin BOM
Core/Export/TcxExportRules.cs                     N  IsExportable, CalorieBasis (§2.3)
Core/Export/TcxFileName.cs                        N  Sanitize, Base, WithCollision (§2.4)
Core/Export/ExportFolderLabel.cs  ExportLog.cs    N  etiqueta de la carpeta y registro sin rutas (§2.5)
Core/Ports/IFileExporter.cs  ExportRequest.cs  ExportedFile.cs  ExportDelivery.cs  ExportLocation.cs  N  (§3)
Core/Design/ExportMotionSpecs.cs                  N  MinBusyMs 300, NoticeMs 6000, SpinnerDp 18, StrokeDp 2
Core/Localization/L10nKeys.cs                     C  7 claves (252)
Localization/strings.es.json  strings.en.json     C
App/Export/TcxExportService.cs  TcxExportNotice.cs  N  (§4)
App/History/SessionDetailState.cs                 C  + CanExport (calculada)
App/Composition/AppServices.cs  AppComposer.cs  AppContext.cs  C  Exporter, ExportZone, SimulatorBuild; AppContext.Export
Infrastructure/Export/FolderExportWriter.cs       N  escritura atómica, colisiones, purga por antigüedad (§3.2)
Tests/Support/TcxSeeds.cs  TestZones.cs  FakeFileExporter.cs  N  T-1 a T-6, T-2M, 3 h; zonas; exportador falso (§6.2)
Tests/Golden/tcx/T-1.tcx                          N  el golden de la spec §6, LF, sin BOM
.gitattributes                                    C  *.tcx text eol=lf
Tests/EditMode/Export/{TcxBuilder,TcxLaps,TcxTime,TcxXml,TcxExportRules,TcxFileName,ExportFolderLabel,ExportLog}Tests.cs  N
Tests/EditMode/Export/TcxExportServiceTests.cs     N
Tests/EditMode/Infrastructure/FolderExportWriterTests.cs  N
Tests/EditMode/History/SessionDetailStoreTests.cs  C  CanExport por fase
Tests/EditMode/Localization/*                      C  252 claves
Tests/EditMode/Composition/AppComposerTests.cs     C
Tests/EditMode/Project/ItTwelveToolingTests.cs     N  manifiesto Android, .gitattributes, set it12, claves profile* sin uso, agujas de credenciales, sin red en Export
Tests/EditMode/Traceability/It12Ids.cs             N
```

### 1.2 WP-B: plataforma, vistas, arnés y builds (Unity)

```
Platform/Export/WindowsFileExporter.cs  ExplorerReveal.cs   N  carpeta + Explorador con /select (§3.3)
Platform/Export/AndroidFileExporter.cs                      N  caché + RmExport.share por JNI (§3.4)
Platform/Export/FileExporterFactory.cs                      N
Bootstrap/ExportLocations.cs                                N  ROWMATE_DATA_DIR / Documentos / caché (DT12-10)
Bootstrap/AppRoot.cs                                        C  Exporter, ExportZone, SimulatorBuild; -rowmate-export-smoke
Assets/Plugins/Android/RowMateExport/RmExport.kt  RmExportProvider.kt          N
Assets/Plugins/Android/RowMateExport.androidlib/{AndroidManifest.xml,build.gradle,project.properties}  N
native/android/check.ps1  native/android/golden/export-signatures.txt         C/N  compila y revisa también RowMateExport
Presentation/History/ExportTcxButton.cs                     N  (§5.1)
Presentation/History/SessionDetailPage.cs                   C  botón en la AppBar, enlazado a CanExport y al servicio
Presentation/Shell/ToastLayer.cs  ActionToast.cs            C/N  ShowAction (§5.2)
Presentation/Shell/ExportNoticePresenter.cs  AppShellView.cs N/C
Snapshots/ExportCatalog.cs  SnapshotCatalog.cs  ScenarioScript.cs  N/C  set it12; ScenarioScript.Hover (§7)
Tests/UiSupport/UiHostOptions.cs  UiTestHost.cs             C  Exporter (FakeFileExporter por defecto), ExportZone, SimulatorBuild
Tests/PlayMode/{TcxExportDetail,ToastAction,TcxExportFrame}Tests.cs  N
Tests/UnityEditMode/{ExportLocations,ExplorerReveal}Tests.cs  N
Editor/Build/BuildOutputInspector.cs                        C  CredentialNeedles (§3.6) + -rowmate-export-smoke
Tests/UnityEditMode/BuildOutputInspectorCredentialTests.cs  N  BOI (§3.6), junto a los de SimulatorModeTests
tools/unity/capture-snapshots.ps1  tools/unity/README.md    C  -Set it12 (por defecto) → docs/…/12-exportar-tcx/snapshots
tools/build/build.ps1  tools/sessions/Program.cs            C  -ExportSmoke; subcomando tcx
docs/migration/architecture/adr/024-exportar-archivos.md    N  ADR corto (puerto, hilos, carpetas, Android sin androidx)
```

### 1.3 WP-D: arte

```
Core/Design/ExportGlyph.cs                    N  download y share de Material en la caja de 24 (datos, como HeartGlyph)
Presentation/Controls/ExportGlyphView.cs      N  lo dibuja con Painter2D en el color resuelto; SetShown(bool)
Presentation/Styles/Skin.uss  Base.uss        C  .rm-export-button(--busy), .rm-toast--action, .rm-toast__detail, .rm-toast__action
Tests/EditMode/Design/ExportGlyphTests.cs     N  coordenadas en 0-24, contornos cerrados
docs/…/12-exportar-tcx/04-design-notes.md     N
```

---

## 2. Core (WP-A)

### 2.1 `TcxBuilder` (`tcx:7-80`; AC-STR-09-01, -02, -05, -08, -09)

```csharp
public sealed record TcxOptions(TimeZoneInfo Zone, bool Simulator);
public static class TcxBuilder
{
    public static string Build(WorkoutSession session, IReadOnlyList<DataPoint> points, TcxOptions options);
}
public static class TcxDocument { public static byte[] Encode(string xml); } // new UTF8Encoding(false)
```

- Encabezado, raíz con los 3 espacios de nombres, `<Activities>`, `<Activity Sport="Other">`, `<Id>` = `TcxTime.Format(StartedAt)`: las mismas cadenas de `tcx:18-24`, cada línea con `\n` (no `AppendLine`, que en Windows da `\r\n`).
- Por vuelta (`tcx:35-42`, `:171-187`): `StartTime` = primer punto; `TotalTimeSeconds` = último − primero; `DistanceMeters` = último − primero (sin tope, DV-127); `Calories` (DT12-04); `Active`, `Manual`, `<Track>`.
- Por punto (`tcx:44-69`): `Time`, `DistanceMeters` siempre; `HeartRateBpm` si `HeartRate > 0`; `Cadence` = `DartMath.Round(StrokeRate)` si `StrokeRate > 0`; las 5 líneas de `Extensions/ns3:TPX/ns3:Watts` si `PowerWatts > 0`.
- `Notes` (DV-116, -117, -123): después de la última `</Lap>` con 6 espacios; texto = `RoutineName` (si no está vacío ni es solo espacios), con `[SIM] ` delante en el simulador, o `[SIM]` solo en una libre del simulador; `TcxXml.Escape`.
- Sin puntos: `ArgumentException` (el servicio nunca lo llama así: DT12-05). `StringBuilder` con capacidad `512 + 260 × puntos` (AC-STR-09-12).

### 2.2 Vueltas, hora y XML

- `TcxLaps.Group(points)` → `IReadOnlyList<TcxLap>` (`Start`, `Count`): ningún índice → una vuelta; si no, corte cuando `StepIndex` cambia entre puntos consecutivos, comparando `int?` (una racha de `null` es su vuelta, un índice que vuelve abre otra; `tcx:88-115`, DV-118). No recibe pasos.
- `TcxTime.Format(DateTimeOffset instant, TimeZoneInfo zone)`: `yyyy-MM-ddTHH:mm:ss±HH:mm` con `CultureInfo.InvariantCulture`, signo `-` solo si el desfase es negativo (`tcx:131-134`).
- `TcxXml.Escape(text)`: primero quita lo que XML 1.0 no admite (controles salvo `\t`, `\n`, `\r`; sustitutos sueltos; `U+FFFE`, `U+FFFF`; DV-120) y después `&` → `&amp;` (primero), `<`, `>`, `"`, `'` (`tcx:119-126`).

### 2.3 Reglas de la exportación (`TcxExportRules`)

```csharp
public static bool IsExportable(SessionStats stats, int pointCount);          // DT12-05
public static (int TotalSeconds, int TotalCalories) CalorieBasis(WorkoutSession s, SessionStats pointStats); // DT12-04
public static int LapCalories(int totalCalories, int lapPoints, int totalSeconds);   // 0 si totalSeconds <= 0
```

### 2.4 Nombre del archivo (`TcxFileName`; AC-STR-09-13, -15)

```csharp
public static string Sanitize(string? name);   // NFC; letras/dígitos Unicode, '-' y '_'; el resto → '-'; junta '-'; recorta bordes; ≤ 40
public static string Base(DateTimeOffset start, TimeZoneInfo zone, string? routineName, string freeLabel, bool simulator);
    // "RowMate_2026-11-05_1840[_{nombre}][_SIM]"
public static string Resolve(string baseName, string extension, ICollection<string> taken); // ".tcx", "_2.tcx"… (OrdinalIgnoreCase)
```

La tabla de ejemplos de la spec es el test. `..\..\CON` → `CON`: no hay nombres reservados de Windows porque el archivo siempre empieza con `RowMate_`.

### 2.5 Etiqueta de la carpeta y registro

- `ExportLocation(string Directory, string? FixedLabel)` (Core/Ports): `FixedLabel == null` = Documentos.
- `ExportFolderLabel.For(ExportLocation, Localizer)` → `{exportTcxDocuments}\RowMate\TCX` o la etiqueta fija. Nunca la unidad ni el usuario (A12-06 simplificado: la spec pide solo estos dos casos y un texto que se ajusta, sin recorte en el medio).
- `ExportLog.Scrub(string message, IReadOnlyList<KeyValuePair<string, string>> replacements)`: reemplazo `OrdinalIgnoreCase`, del más largo al más corto.

---

## 3. Puerto y plataformas

### 3.1 `IFileExporter` (Core/Ports, WP-A)

```csharp
public enum ExportDelivery { None, SaveToFolder, Share }
public sealed record ExportRequest(string BaseName, string Extension, string MimeType, string ShareTitle, Func<byte[]> Content);
public sealed record ExportedFile(string FileName, string FullPath, ExportLocation Location);  // FullPath nunca se muestra
public interface IFileExporter
{
    ExportDelivery Delivery { get; }
    // Corre Content fuera del hilo principal (el falso: en línea), escribe sin pisar ni dejar restos y devuelve el archivo.
    // En Share, además abre la hoja de compartir (en el hilo principal, después del await). Cualquier excepción = falla.
    Task<ExportedFile> ExportAsync(ExportRequest request);
    // Explorador con el archivo seleccionado; si ya no está, la carpeta (AC-STR-09-20). Sin efecto si no es SaveToFolder.
    void Reveal(ExportedFile file);
}
```

`MimeType` = `TcxDocument.MimeType` (`application/vnd.garmin.tcx+xml`) y `Extension` = `TcxDocument.Extension` (`.tcx`), constantes de Core; `ShareTitle` = `exportTcxShareTitle` del `Localizer` vigente **al pedir** la exportación. El servicio arma el mismo `ExportRequest` en las tres entregas (QA-G12-02: `ES.RequestHasTcxMimeAndShareTitle`).

### 3.2 `FolderExportWriter` (Infrastructure, WP-A)

```csharp
public static ExportedFile Write(ExportLocation location, string baseName, string extension, byte[] content,
                                 Func<string, Stream>? openWrite = null);   // openWrite: las pruebas simulan disco lleno
public static int DeleteOlderThan(string directory, string extension, DateTime utcLimit); // Android: ahora − ShareCacheMaxAge
public static readonly TimeSpan ShareCacheMaxAge = TimeSpan.FromHours(24);              // AC-STR-09-24
```

Crea la carpeta si falta, resuelve el nombre (DT12-09) y devuelve `ExportedFile`. Lo usan los dos exportadores reales y `FakeFileExporter`, así las colisiones y la atomicidad se prueban una vez con dotnet.

`DeleteOlderThan` (QA-G12-02): sin recursión; solo los archivos cuya extensión es **exactamente** `extension` (`OrdinalIgnoreCase`: `.tcx` y `.TCX`, no `.tcx.part` ni `~rowmate-*.part`) y con `File.GetLastWriteTimeUtc < utcLimit` (estricto); un archivo que no se puede borrar (en uso) se saltea sin lanzar; carpeta inexistente → 0. Devuelve cuántos borró. Nunca la crea (m-02).

### 3.3 Windows (`WindowsFileExporter`, WP-B)

- `Delivery = SaveToFolder`; `ExportAsync`: `Task.Run(() => FolderExportWriter.Write(location, …, request.Content()))`.
- `Reveal`: `ExplorerReveal.Arguments(file)` = `/select,"<ruta>"` si el archivo existe, si no `"<carpeta>"`; `Process.Start("explorer.exe", args)` con `UseShellExecute = false`. Una falla solo queda en el registro (issue de la spec §10.4). `ExplorerRevealTests` prueba las cadenas, **nunca** abre el Explorador.
- macOS (Editor de Mac, si lo hubiera): el mismo exportador con `~/Documents/RowMate/TCX` y `open -R`; la versión con `Mostrar en Finder` es de la 10b. iOS: `Delivery = None` hasta la 10b (no hay botón).

### 3.4 Android (`AndroidFileExporter` + Kotlin, WP-B)

- Carpeta: `RmExport.exportDir(activity)` (crea `cacheDir/exports`) leída al construir, en el hilo principal.
- `ExportAsync`: en `Task.Run`, `DeleteOlderThan(dir, TcxDocument.Extension, UtcNow − FolderExportWriter.ShareCacheMaxAge)` y `Write`; después del `await` (hilo principal), `RmExport.share(activity, path, mime, title)`. `Delivery = Share`: el servicio no publica aviso de éxito; un `false` o una excepción de JNI cuentan como falla (AC-STR-09-21).
- Kotlin (`com.rowmate.export`):
  - `RmExport.exportDir(Activity): String` y `RmExport.share(Activity, String, String, String): Boolean` (`runOnUiThread`; `Intent.ACTION_SEND`, `EXTRA_STREAM`, `ClipData`, `FLAG_GRANT_READ_URI_PERMISSION`, `Intent.createChooser`). El bloque de `runOnUiThread` atrapa sus excepciones y las registra con `Log.w("RowMateExport", …)` (m-08): ya devolvió `true` y no llegan al aviso de error; queda NO VERIFICABLE.
  - `RmExportProvider : ContentProvider`: autoridad `${applicationId}.rowmate.export`; `openFile` solo `r` y solo bajo `cache/exports` (canónico, sin `..`); `query` con `OpenableColumns.DISPLAY_NAME` y `SIZE`; `getType` por la extensión; `insert`/`update`/`delete` lanzan `UnsupportedOperationException`.
  - `RowMateExport.androidlib/AndroidManifest.xml`: solo el `<provider>` dentro de `<application>`, **sin** `uses-permission` (AC-STR-09-24, F-APP-13).
- `check.ps1`: compila `RowMateExport/*.kt` con el resto, revisa que no aparezcan `INTERNET`, `WRITE_EXTERNAL_STORAGE` ni `UnityPlayer`, y compara `javap -s -public com.rowmate.export.RmExport` con `golden/export-signatures.txt`.

### 3.5 Composición (WP-B)

`AppRoot`: `Exporter = FileExporterFactory.Create(ExportLocations.Resolve(_platform), logger)`, `ExportZone = () => { TimeZoneInfo.ClearCachedData(); return TimeZoneInfo.Local; }`, `SimulatorBuild = BuildFlags.IsSimulator`. `UiTestHost`: `options.Exporter ?? new FakeFileExporter(<temp del anfitrión>, FixedLabel: null)`, `ExportZone = () => options.LocalZone`, `SimulatorBuild = options.SimulatorBuild`. `AppServices.Exporter == null` → un exportador `None` (sin botón; dotnet y pruebas que no lo miran). `ExportLocations.Resolve` solo **calcula** la ruta: nunca crea `Documentos\RowMate\TCX` ni `exports` (m-02); la carpeta la crea `FolderExportWriter.Write` al escribir (`ELT.ResolveCreatesNothing`).

### 3.6 Inspector del build: credenciales y red (AC-STR-09-28, QA-G12-01)

Los restos de Strava que se quedan (§11.1: `StravaActivityId`, `strava_activity_id`, `Tokens.BrandStrava`, las claves y textos `profile*`) **no** son credenciales ni red, así que la palabra `Strava` no es aguja. El inspector busca lo que delataría una integración viva:

```csharp
public static readonly string[] CredentialNeedles =
{
    "strava.com/api", "strava.com/oauth", "api/v3/uploads",   // endpoints de la API y del OAuth
    "client_secret", "refresh_token",                         // credenciales y tokens
    "rowmate://",                                             // regreso del OAuth
};
```

- Entran en `FindForbiddenTraces` (build normal, `BuildCli`, código 2) y en un `FindCredentialTraces` nuevo que `BuildCli` corre también sobre el build del simulador (que hoy no pasa por el inspector). Búsqueda de bytes como las demás (UTF-8 y UTF-16, sensible a mayúsculas).
- Hoy ninguna fuente de `Assets/` ni el catálogo las contiene (revisado al cerrar el gate); el build normal tiene que quedar en verde sin tocar los restos.
- `BOI` (`Tests/UnityEditMode/BuildOutputInspectorCredentialTests.cs`, Editor): `EachCredentialNeedleIsFlagged` (por cada aguja, un archivo de prueba `.dll`, `.json` y sin extensión con `xx {aguja} yy` en UTF-8 y UTF-16 → `FindForbiddenTraces` y `FindCredentialTraces` la nombran) y `StravaLeftoversAreNotFlagged` (un `.assets` con `strava_activity_id`, `StravaActivityId`, `BrandStrava`, `Strava` y el contenido **real** de `Localization/strings.{es,en}.json`, con sus textos `profile*` → ninguna traza de credenciales).
- `T12` (dotnet, sin referenciar el ensamblado del Editor): `InspectorListsCredentialNeedles` (el texto de `BuildOutputInspector.cs` contiene cada `"{aguja}",` y `FindCredentialTraces` en `BuildCli.cs` para los dos builds) y `NoCredentialNeedleInShippedSources` (ningún `.cs`, `.json`, `.uss`, `.uxml`, `.kt` ni `.xml` bajo `Assets/` fuera de `Tests/` y `Editor/` contiene una aguja).

---

## 4. App (WP-A): `TcxExportService`

```csharp
public sealed class TcxExportService : IDisposable
{
    public TcxExportService(ISessionRepository sessions, IFileExporter exporter, IScheduler scheduler,
                            Func<TimeZoneInfo> zone, IReadOnlyObservable<Localizer> texts, bool simulator, IAppLogger logger);
    public ExportDelivery Delivery { get; }
    public IReadOnlyObservable<IReadOnlyCollection<long>> Busy { get; }   // ids en curso (cambia 2 veces por exportación)
    public IEventSource<TcxExportNotice> Notices { get; }
    public Task ExportAsync(WorkoutSession session);   // nunca lanza; dedup por id (DT12-11)
    public void Reveal(ExportedFile file);
}
public sealed record TcxExportNotice(TcxExportNoticeKind Kind, WorkoutSession Session, ExportedFile? File);  // Saved | Failed
```

Flujo: (1) id ya en curso → la misma tarea; (2) `Busy += id`, `t0 = scheduler.MonotonicSeconds`; (3) `points = await GetPointsAsync(id)`; si no es exportable → falla; (4) `zone = _zone()`, nombre con `historyFree` del `Localizer` vigente; (5) `file = await exporter.ExportAsync(new ExportRequest(base, ".tcx", mime, shareTitle, () => TcxDocument.Encode(TcxBuilder.Build(session, points, options))))`; (6) `await scheduler.Delay(300 ms − transcurrido)` si falta; (7) `Busy −= id`, registro (DT12-19) y aviso: `Saved` solo con `SaveToFolder`, `Failed` siempre. `Dispose` corta los avisos pendientes (la tarea del exportador termina sola).

`AppContext.Export` lo crea `AppComposer` y lo dispone `AppContext.Dispose` (antes del historial).

---

## 5. Vistas (WP-B1 la estructura y el comportamiento; WP-D el aspecto)

### 5.1 Botón del Detalle

```
#session-appbar .rm-appbar
├── #session-back
├── título (recorta con …, flex-shrink 1; ya en Base.uss)
└── .rm-appbar__actions
    └── #session-export .rm-icon-button .rm-export-button[--busy]   48 × 48, foco con Tab, Enter/Espacio
        ├── .rm-button__state
        ├── ExportGlyphView 24 (Download en SaveToFolder, Share en Share)   ← oculto mientras exporta
        ├── Spinner 18/2 (ExportMotionSpecs)                                ← solo mientras exporta
        └── foco
```

- Visible solo con `State.CanExport && Export.Delivery != None && Session.Id != null` (AC-STR-09-17, DV-126): en carga, error de carga y sin telemetría, `display: none`.
- `Tooltip.Attach(ui, button, () => busy ? exportTcxExporting : exportTcx)`: ayuda al pasar el cursor y nombre accesible (DT-05 de la It. 2), con la regla del toque largo de `IconButton`.
- `busy = Export.Busy` contiene el id (se enlaza con `Root.Bind`): `SetEnabled(false)` y spinner; `.rm-export-button--busy` evita el 0,38 de `:disabled` sobre el spinner (WP-D). Al reabrir el Detalle durante una exportación, ya aparece ocupado.

### 5.2 `ToastLayer.ShowAction` (A12-04)

```csharp
public sealed record ActionToast(string Title, string? Detail, bool Check, string ActionText, Action OnAction, TimeSpan Duration);
public void ShowAction(ActionToast toast);
```

- Reusa `#toast`: suma, creados **una vez**, `#toast-detail` (`Label` `caption`, ajusta, nunca `…`) y `#toast-action` (`RmButton` variante `text`, `.rm-type-button-s`, `pickingMode = Position`, el resto de la cápsula sigue en `Ignore`). Clase `.rm-toast--action` (y `.rm-toast--link` para el check). Los otros avisos los ocultan.
- Éxito: título `exportTcxSaved` con `{folder}` = `ExportFolderLabel.For`, detalle = nombre del archivo, `LinkIndicator` en `Check` (D12-04), acción `exportTcxOpenFolder` → `Export.Reveal(file)` y salida en el acto. Error: título `exportTcxFailed`, sin detalle ni indicador, acción `retry` → `Export.ExportAsync(session)`. Duración 6 s (`ExportMotionSpecs.NoticeMs`).
- Ancho (≥ 600 dp): `clamp(max(título, detalle) + 32 + indicador + acción + 8; 344; 560)`; < 600: de 16 a 16 y el texto en varias líneas (CAP-06). Posición y obstáculos: los de hoy (sobre la barra de pestañas en las pestañas; en el Detalle, al pie de la pantalla, CAP-11. *Corregido en la It. 15*, #72, QA-V12-03).
- Prioridad (DT12-14): `VisibleSlot` = `Common`. Reducir movimiento: el `ToastTransition` de siempre.
- `ExportNoticePresenter(ui, toasts, export)`: se suscribe a `Notices` y arma el `ActionToast` con el `Localizer` vigente. Lo crea `AppShellView` junto a `HrmNotices`.

---

## 6. Tests y verificación

### 6.1 Comandos

`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it12tcx`; `tools/unity/run-unity-tests.ps1` y `-Simulator`; `native/android/check.ps1` (y `-Golden`, sin cambios en el BLE); `tools/build/build.ps1 -Target Windows` (normal: inspector), `-Simulator` y `-ExportSmoke`; `tools/unity/capture-snapshots.ps1 -Set it12` dos veces con comparación byte a byte. Ninguna corrida escribe en Documentos ni en Descargas ni abre el Explorador: `ROWMATE_DATA_DIR` temporal en los scripts, `FakeFileExporter` en el arnés y las PlayMode; el humo usa su carpeta temporal y QA no toca `Abrir carpeta`. Si el bootstrapper reescribe `HorizonFar.mat`/`HorizonNear.mat`, se revierten.

### 6.2 Dobles y datos

- `TcxSeeds`: `T1` … `T6`, `T2M` y `ThreeHours` (2 160 puntos con todos los campos), con los datos inventados de la spec §6. `HistorySeeds.Points(n)` da F-9.
- `TestZones`: `UtcMinus3` (= `HistorySeeds.Zone`), `Utc`, `Kolkata` (+5:30), `Kathmandu` (+5:45), `StJohns` (−3:30 fija: el 5/11 ya no rige el horario de verano), `Madrid` (+1 con regla de verano: último domingo de marzo 02:00 → último domingo de octubre 03:00). Todas con `CreateCustomTimeZone`: iguales en .NET 8 y en Mono.
- `FakeFileExporter(directory, fixedLabel, delivery = SaveToFolder)`: escribe con `FolderExportWriter` en línea; `Hold()` deja la próxima exportación pendiente hasta `Release()` (CAP-04); `FailNext(Exception)` (CAP-07); `Reveals` registra sin abrir nada; `Requests` guarda cada `ExportRequest` recibido (QA-G12-02). Con `delivery: Share` escribe igual en la carpeta temporal y no abre nada.
- `TestZones.Madrid` (m-10): en `CreateCustomTimeZone`, la transición de fin de verano va en hora de verano (`03:00`) y la de inicio en hora estándar (`02:00`); los tres instantes de AC-STR-09-03 se prueban también en Unity (R-02).

### 6.3 Matriz AC → test (`It12Ids.cs`)

Abreviaturas: `TB` TcxBuilderTests, `TL` TcxLapsTests, `TT` TcxTimeTests, `TX` TcxXmlTests, `TR` TcxExportRulesTests, `FN` TcxFileNameTests, `FL` ExportFolderLabelTests, `LG` ExportLogTests, `FW` FolderExportWriterTests, `ES` TcxExportServiceTests, `SDS` SessionDetailStoreTests, `L10` catálogo, `T12` ItTwelveToolingTests, `BOI` BuildOutputInspectorTests; PlayMode `TED` TcxExportDetailTests, `TAT` ToastActionTests, `TEF` TcxExportFrameTests; UnityEditMode `ELT` ExportLocationsTests, `ERT` ExplorerRevealTests. Marcas: d = dotnet, e = Editor/PlayMode, cap = captura, win = PC, hw = NO VERIFICABLE, man = demo del usuario.

| AC | Marca | Tests |
|---|---|---|
| AC-STR-09-01 | d | `TB.T1EqualsGoldenByteForByte` (`Tests/Golden/tcx/T-1.tcx`), `.Utf8NoBomLfFinalNewline`; `TX.ParsesWithXmlReaderThreeNamespacesOneActivity` |
| AC-STR-09-02 | d | `TT.TruncatesMilliseconds`, `.PadsYearToFour`; `TB.IdLapAndPointTimesFromStart` |
| AC-STR-09-03 | d e | `TT.T2InUtcKolkataKathmanduStJohns` (cero lleva `+`), `.T2MEachInstantHasItsOwnOffset` (Madrid: los tres instantes, también con Mono en `run-unity-tests.ps1`, m-10) |
| AC-STR-09-04 | d | `TL.NoIndexOneLap`, `.ReturningIndexOpensAnother` (T-1 → 3), `.NullRunIsItsOwnLap`, `.F9Laps_1is8_2is1_3is2`; `TB.SameFileWithoutRoutine` |
| AC-STR-09-05 | d | `TB.LapFieldsInOrder`, `.SinglePointLapZeroZero`, `.NegativeDistanceKept`, `.GapBetweenLapsNotCounted` |
| AC-STR-09-06 | d | `TR.LapCaloriesHalfAwayFromZero` (60 → 50, 3 → 3, 2 → 2, 1 → 1), `.ZeroTimeGivesZero` |
| AC-STR-09-07 | d | `TR.CutSessionUsesPointMaxima` (T-3 → 3); `ES.NeverWritesTheRepository` |
| AC-STR-09-08 | d | `TB.OptionalFieldsOnlyWhenPositive`, `.CadenceHalfAwayFromZero` (24.4/24.6/22.5), `.NoPaceSpeedStrokesPosition`, `.PointsInMonitorOrder` |
| AC-STR-09-09 | d | `TX.EscapesAmpFirst`, `.StripsInvalidXmlChars`; `TB.NotesAfterLastLap`, `.NoNotesForFreeOrBlank`, `.SimNotes`, `.NoRowmateComment` |
| AC-STR-09-10 | d e | `TR.IsExportable` (T-5 no, sin puntos no, T-6 sí); `TB.T6OneLapZeros`; `SDS.CanExportOnlyWhenReadyAndExportable`; `TED.NoButtonWithoutTelemetry` |
| AC-STR-09-11 | d e | `TB.Deterministic`; `ES.ReadOnlyNoPrefsNoWrites` (repositorio con guion, `RecordingPreferences`); `TED.HistoryUnchangedAfterExport` |
| AC-STR-09-12 | d e | `TB.ThreeHoursUnder200Ms` (una corrida de calentamiento y la mediana de 5, m-09); `TEF.NoticeUnderOneSecondNoFrameOver50Ms` (exportador real de Windows sobre una carpeta temporal, `FrameAllocations`/tiempos de cuadro) |
| AC-STR-09-13 | d | `FN.SpecExamples` (la tabla), `.NeverForbiddenCharacters` (todo ASCII y controles), `.MaxFortyWithoutSplittingPairs`, `.TrimsDashAfterCut` (recorta a 40 y **después** quita el `-` del borde, m-03), `.FreeUsesHistoryFree`, `.LocalTimeOfExportZone` |
| AC-STR-09-14 | d | `FW.CollisionSuffixCaseInsensitive`, `.NeverOverwrites`, `.FailureLeavesNoPartialNorTemp` (`openWrite` que falla a mitad), `.RaceOnMoveTakesNextSuffix` |
| AC-STR-09-15 | d | `FN.SimBeforeCollisionSuffix`; `ES.SimulatorBuildMarksNameAndNotes` |
| AC-STR-09-16 | d e win | `FL.DocumentsAndFixedLabels`; `ELT.DataDirGoesToExportsNeverDocuments`, `.WindowsUsesMyDocumentsRowMateTcx` (solo la cadena), `.ResolveCreatesNothing` (m-02); `-ExportSmoke` |
| AC-STR-09-17 | e cap | `TED.ButtonOnlyWhenExportable` (carga, error, n.º 4), `.TooltipIsAccessibleName`, `.TabThenEnterOrSpaceExports`, `.LongTitleEllipsizesBeforeButton`, `.ShareDeliveryShowsShareGlyph` (`FakeFileExporter(delivery: Share)`, m-07); CAP-01 a -03, -09, -12 |
| AC-STR-09-18 | d e cap | `ES.BusyAtLeast300Ms`, `.SecondRequestSameTask`; `TED.SpinnerEighteenByTwoAndDisabled`, `.BackDuringExportStillNotifies`, `.ReopenShowsBusy`; CAP-04 |
| AC-STR-09-19 | d e cap | `ES.SavedNoticeHasFileAndLocation`, `.ShareHasNoNotice`; `TAT.SuccessTwoLinesSixSecondsWithAction`, `.NeverFullPathOrUserName`, `.WrapsNeverEllipsis`; CAP-05, -06, -10, -11 |
| AC-STR-09-20 | d e | `ES.RevealDelegates`; `TAT.OpenFolderRevealsAndHides` (`FakeFileExporter.Reveals`); `ERT.SelectWhenFileExistsElseFolder` |
| AC-STR-09-21 | d e cap | `ES.FailureNoticeAndScrubbedLog`; `LG.NoUserPathInMessage`; `FW.FailureLeavesNoPartialNorTemp`; `TAT.RetryExportsAgain`, `.ButtonBackToIcon`; CAP-07, -08 |
| AC-STR-09-22 | e cap | `TAT.RowerHrmAndCommonReplaceExportNotice`, `.StickyReturnsAfterSixSeconds`, `.ReducedMotionNoMovement`; CAP-10, -11 |
| AC-STR-09-23 | d e | `L10.Has252KeysPerLanguage`, `.ExportKeysExact`; `T12.ProfileKeysUnusedInPresentation`; `TED.AllTextsFromCatalog` |
| AC-STR-09-24 | d hw | `T12.ExportManifestNoPermissionsProviderNotExported`, `.AndroidExporterPurgesWithShareCacheMaxAge`; `FW.DeleteOlderThanOnlyOldTcx` (fechas fijadas con `File.SetLastWriteTimeUtc`: borra los `.tcx`/`.TCX` de más de 24 h; deja los nuevos, el de justo 24 h, otras extensiones, los `.part` y las subcarpetas; devuelve la cuenta), `.DeleteOlderThanMissingFolderIsZero`, `.ShareCacheMaxAgeIs24h`; `ES.RequestHasTcxMimeAndShareTitle` (`FakeFileExporter(delivery: Share)`: `application/vnd.garmin.tcx+xml`, `.tcx`, `Compartir sesión` y, cambiado el idioma, el texto en inglés; sin aviso de éxito y `Busy` vuelve a vacío); `check.ps1` (compila y firmas); la hoja real, NO VERIFICABLE |
| AC-STR-09-25 | — | 10b |
| AC-STR-09-26 | win | `build.ps1 -ExportSmoke`: `exports\RowMate_2026-11-05_1840_Escalera.tcx` (con el equipo en UTC−03:00; en otra zona, el nombre que da `tools/sessions tcx` en la zona local, m-05) igual, nombre y bytes, al de `tools/sessions tcx`, después `…_Escalera_2.tcx`, y Documentos sin cambios (lista de la carpeta antes y después) |
| AC-STR-09-27 | man | Demo del usuario, §1 pasos 4-5 |
| AC-STR-09-28 | d e win | Pruebas de la barra de 4 pestañas (ya existen); `BOI.EachCredentialNeedleIsFlagged`, `.StravaLeftoversAreNotFlagged`; `T12.InspectorListsCredentialNeedles`, `.NoCredentialNeedleInShippedSources`, `.ExportCodeHasNoNetwork` (ningún archivo de `App/Export`, `Core/Export`, `Infrastructure/Export` ni `Platform/Export` nombra `System.Net`, `UnityWebRequest` ni `HttpClient`, m-06); inspector de los builds normal y simulador en verde (§3.6) |

Se vuelven a correr sin cambios: `HudBudgetTests`, `SessionDetailStoreTests` de la It. 9, `HrmToastTests` y `LinkToast*` (el `ToastLayer` cambia).

---

## 7. Capturas (`it12`, `ExportCatalog`, `FakeFileExporter`, dos corridas idénticas)

`UiTestHost` + `UseHistory(F-9)` + `LocalZone` UTC−03:00 + reloj de prueba + `ROWMATE_DATA_DIR` temporal + `FakeFileExporter(<temp>, FixedLabel: null)` (muestra `Documentos\RowMate\TCX`, escribe en la temporal). Salida en `docs/migration/iterations/12-exportar-tcx/snapshots/` (`capture-snapshots.ps1 -Set it12`: sumar el caso a `ValidateSet` y `Get-SetFolder`, por defecto `it12`; QA revisa que no escriba en la carpeta de la 11). Nombres `CAP-NN-<slug>-<tamaño>[-en].png`. `ScenarioScript.Hover(name)` (nuevo): `PointerEnter` + `PointerMove` sobre el centro y avanza el retardo de la ayuda.

| CAP | Guion | Tamaño |
|---|---|---|
| 01 | Historial → n.º 1, asentado | W1280 |
| 02 | Igual | TV |
| 03 | Igual | TH |
| 04 | 01 + `Hold()` + tocar `session-export`, +100 ms | W1280 |
| 05 | 01 + tocar, +300 ms + asentado (aviso a la vista) | W1280 |
| 06 | 05 | TV |
| 07 | 01 + `FailNext(IOException)` + tocar, +300 ms + asentado | W1280 |
| 08 | 07 | TH |
| 09 | Historial → n.º 4 | TV |
| 10 | 05 en inglés, letra 1,3× | W1280 |
| 11 | 05 | L640 |
| 12 | 01 + `Hover("session-export")` | W1280 |

Controles de cada toma: texto exacto del aviso, nombre `RowMate_2026-11-05_1840_Escalera.tcx`, botón presente o ausente, spinner a la vista (CAP-04), ningún texto con `:\` ni `Users`.

---

## 8. Paquetes de trabajo (archivos disjuntos)

| WP | Dueño | Qué | Hecho cuando |
|---|---|---|---|
| **WP-A** | Architect | §1.1: `TcxBuilder` y sus piezas, reglas, nombre, etiqueta y registro, el puerto, `ExportMotionSpecs`, `FolderExportWriter`, `TcxExportService`, `CanExport`, composición, 7 claves, `TcxSeeds`/`TestZones`/`FakeFileExporter`, el golden, `.gitattributes` y todos los tests `d` | `run-tests.ps1` en verde con los `d` de §6.3; golden idéntico byte a byte |
| **WP-B1** | Architect | §1.2 sin capturas ni builds: exportadores de Windows y Android, Kotlin y manifiesto, `check.ps1`, `ExportLocations`, `AppRoot`, `ExportTcxButton`, Detalle, `ShowAction`, `ExportNoticePresenter`, `UiTestHost`, PlayMode y UnityEditMode `e`, `BuildOutputInspector`, ADR 024 | PlayMode en verde (normal y `-Simulator`); `check.ps1` en verde |
| **WP-D** | Designer | §1.3: `ExportGlyph` (download, share), `ExportGlyphView`, USS del botón y del aviso con acción, `04-design-notes.md` | `ExportGlyphTests` y `TokensUssSyncTests` en verde; capturas con el arte |
| **WP-B2** | Architect | `ExportCatalog`, `ScenarioScript.Hover`, `capture-snapshots.ps1 -Set it12`, CAP-01 a -12 dos veces idénticas, `tools/sessions tcx`, `build.ps1 -ExportSmoke`, builds `normal` y `simulator` en `Builds/Windows/` (si están en uso, reintentar cada 2 min hasta 20) | Capturas idénticas; humo en verde; inspector del normal en verde |

Contrato WP-B1 ↔ WP-D: `ExportGlyph.Kind { Download, Share }`, `new ExportGlyphView(ExportGlyph.Kind, float sizeDp)` con `SetShown(bool)`; clases `.rm-export-button`, `.rm-export-button--busy`, `.rm-toast--action`, `.rm-toast__detail`, `.rm-toast__action`. WP-D entrega primero el glifo; hasta entonces WP-B1 usa un `IconView` con `Icons.CloudDownload` como relleno provisorio que **no** llega a las capturas.

---

## 9. Integración del arte y la spec

### 9.1 Pedidos al Architect (00-arte §7)

| ID | Decisión |
|---|---|
| A12-01 | **Rechazado** (manda AC-STR-09-17): botón de ícono en la AppBar, no un `RmButton --tonal` con texto en la cabecera del Resumen |
| A12-02 | **No aplica**: en la AppBar el botón ya tiene la caja de 48 de `IconButton` |
| A12-03 | **Adoptado con la spec**: `ExportMotionSpecs` en Core con `dotnet test`; el indicador reemplaza al ícono en el acto y dura ≥ 300 ms (no 250/400); el ancho no cambia porque la caja es fija |
| A12-04 | **Adoptado**: `ShowAction` con segundo renglón, acción, 6 s, elementos reusados y 0 B por cuadro. La pausa del reloj con el puntero o el foco va a issues |
| A12-05 | **Adoptado dentro de `IFileExporter.Reveal`**; el falso solo registra |
| A12-06 | **Adoptado simplificado**: `ExportFolderLabel` puro con los dos casos de la spec, con pruebas de que nunca sale la unidad ni el usuario; sin recorte en el medio (la spec pide ajustar en varias líneas) |
| A12-07 | **Adoptado**: Android sin aviso de éxito; el error, como en Windows |
| A12-08 | **Adoptado con las 12 tomas de la spec §7** (no la lista de §6 del arte) |

### 9.2 Arte contra spec (manda la spec)

- D12-01/-02: botón de ícono en la AppBar, no tonal en la tarjeta.
- D12-03: **con** ícono; como no está en el recorte de la fuente (y no hay red), se dibuja (`ExportGlyph`, el mismo camino que `HeartGlyph` de la 11).
- §3 del arte: título `Guardado en Documentos\RowMate\TCX` (no `TCX guardado`), la carpeta `TCX` (no `Exportaciones`) y el **nombre del archivo** en el segundo renglón.
- §5 del arte: las claves son las 7 de la spec §4 (`retry` y `historyFree` se reusan).
- D12-04, -06, -07: adoptados tal cual.

---

## 10. Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R-01 | `Process.Start("explorer.exe")` falla con IL2CPP o con el `/select` | Hoy el build es Mono (D-16); `ERT` fija los argumentos; si falla, issue: `SHOpenFolderAndSelectItems` por P/Invoke |
| R-02 | La zona personalizada de Madrid se comporta distinto en Mono que en .NET 8 | Los tests corren en los dos; si Mono difiere, la prueba de Unity usa un `TimeZoneInfo` fijo por tramo y queda anotado |
| R-03 | Android no se puede probar (sin "Android Build Support") | `check.ps1` compila y firma; `T12` revisa el manifiesto; demo del usuario |
| R-04 | La cadena `ROWMATE_DATA_DIR` se cuela en el build normal | Solo en `Bootstrap/ExportLocations` bajo `#if`; `BuildOutputInspector` ya la busca |
| R-05 | El botón de acción del aviso no recibe el toque porque la capa ignora el puntero | `#toast-action` con `pickingMode = Position`; `TAT.OpenFolderRevealsAndHides` toca de verdad |
| R-06 | `TimeZoneInfo.ClearCachedData` en el hilo principal mientras la base lee fechas | El repositorio guarda su propia instancia de zona; la caché solo afecta a `TimeZoneInfo.Local` |
| R-07 | Documentos en OneDrive "solo en la nube" o sin permiso | Es una falla común (aviso con Reintentar); `GetFolderPath` vacío → falla con registro |
| R-08 | Un glifo dibujado se ve distinto de los de la fuente | WP-D lo dibuja con el trazo de 2,25/24 de ASSET_SLOTS §4.4; issue para pasar al glifo de la fuente |

---

## 11. Para issues

1. Restos de Strava, **sin borrar** (decisión del usuario): ARCHITECTURE §6 (hilo de OAuth y red), §9.5 (almacenamiento seguro), §11 entero, ADR 010 y las filas de Strava de `LEGACY_MAPPING.md`; `ShellOptions`/`TabDestinationId` (pestaña Perfil), `ShellStore` (índice de Perfil); el comentario de `WorkoutEngine.cs:127` (`lastFinishedSessionId` para la subida); `WorkoutSession.StravaActivityId` y `Closed()` que lo limpia, `WorkoutSessionRecord`/`Codec`/`SessionRowMapper`/`WorkoutSessionRow` y la columna `strava_activity_id` (se queda por la base); `Tokens.BrandStrava`; los glifos `cloud_upload`, `cloud_download`, `cloud_done`, `person`, `link`; el tag `STRAVA_AUTH` en el comentario de `IAppLogger`; los comentarios "It. 12" de `HistoryCardView`, `HistoryScreenView`, `SessionDetailPage` y `CompletionDialog`; las claves `profile*` (spec §10.5).
2. Glifos `download` y `share` en el recorte de la fuente (ASSET_SLOTS §4.1, necesita red) en lugar del dibujo (D12-03, R-08).
3. Pausa del aviso con acción mientras el puntero o el foco están encima (A12-04).
4. `Abrir carpeta` con `SHOpenFolderAndSelectItems` si `explorer.exe` falla (R-01); hoy una falla solo queda en el registro (spec §10.4).
5. Lo de la spec §10 (acción en la tarjeta, Guardar como, MediaStore, FIT y `<Creator>`, distancia negativa) y del arte §8 (exportar varias, sello de "exportada").

---

## 12. Cambios por gate ([03-qa-gate](03-qa-gate.md))

| Issue | Cambio |
|---|---|
| QA-G12-01 (mayor) | §3.6 nuevo: `CredentialNeedles` = `strava.com/api`, `strava.com/oauth`, `api/v3/uploads`, `client_secret`, `refresh_token`, `rowmate://` (la palabra `Strava` no es aguja: los restos de §11.1 se quedan); también sobre el build del simulador (`FindCredentialTraces`). §1.2 y §6.3: `BOI.EachCredentialNeedleIsFlagged` y `.StravaLeftoversAreNotFlagged` (`strava_activity_id`, `StravaActivityId`, `BrandStrava` y el catálogo real con `profile*`), más `T12.InspectorListsCredentialNeedles` y `.NoCredentialNeedleInShippedSources` con dotnet |
| QA-G12-02 (mayor) | §3.1: `TcxDocument.MimeType`/`Extension` y el título del idioma vigente al pedir. §3.2: semántica de `DeleteOlderThan` (extensión exacta, estricto, sin recursión, sin lanzar) y `ShareCacheMaxAge` = 24 h. §6.2: `FakeFileExporter.Requests` guarda los pedidos. §6.3 (AC-STR-09-24): `FW.DeleteOlderThanOnlyOldTcx`, `.DeleteOlderThanMissingFolderIsZero`, `.ShareCacheMaxAgeIs24h`, `ES.RequestHasTcxMimeAndShareTitle`, `T12.AndroidExporterPurgesWithShareCacheMaxAge` |
| m-02 | §3.2 y §3.5: `ExportLocations.Resolve` y `DeleteOlderThan` no crean carpetas; `ELT.ResolveCreatesNothing` |
| m-03 | §6.3: `FN.TrimsDashAfterCut` (recorta a 40 y después quita el `-` del borde) |
| m-05 | §6.3 (AC-STR-09-26): el humo compara también el nombre con el de `tools/sessions tcx` en la zona local |
| m-06 | §6.3: `TED.NoNetworkAccess` sale; entra `T12.ExportCodeHasNoNetwork` |
| m-07 | §6.3: `TED.ShareDeliveryShowsShareGlyph` |
| m-08 | §3.4: el bloque de `runOnUiThread` atrapa y registra; sigue NO VERIFICABLE |
| m-09 | §6.3: `TB.ThreeHoursUnder200Ms` con calentamiento y mediana de 5 |
| m-10 | §6.2: transiciones de `TestZones.Madrid` (fin en 03:00 de verano, inicio en 02:00 estándar); §6.3: AC-STR-09-03 también en Unity |
| m-11 | Este título apunta a `03-qa-gate.md` |
| m-04 (po) | [01-po-spec](01-po-spec.md) AC-STR-09-08: cadencia sobre el SPM crudo (sin cambio de diseño: §2.1 ya lo hacía) |
| m-01 (designer) | Sin cambio acá: §9.2 manda; `04-design-notes.md` (WP-D) tiene que reflejarlo |

## 13. Implementación de WP-A (desvíos, una línea cada uno)

| # | Desvío o precisión | Motivo |
|---|---|---|
| I-01 | `IFileExporter` suma `ExportLocation? Location { get; }` | El registro de una falla cambia la carpeta real por su etiqueta (`ExportLog.Scrub`) aunque no haya `ExportedFile` |
| I-02 | `TcxFileName.Resolve(baseName, extension, IEnumerable<string> taken)` | Acepta la enumeración de la carpeta tal cual; compara `OrdinalIgnoreCase` igual |
| I-03 | `TcxExportRules.IsExportable` = al menos un punto y `!(tiempo == 0 && distancia == 0)` de los puntos | La condición exacta del legacy (`pp:164`), no `> 0` |
| I-04 | `NoFileExporter` (App/Export) cuando `AppServices.Exporter == null`; `ExportZone == null` → `TimeZoneInfo.Local` | dotnet y las pruebas que no miran la exportación |
| I-05 | `TcxExportService.IsBusy(id)`; una sesión sin id solo se registra (sin aviso) | Ayuda de la vista; sin id no hay botón (DT12-17) |
| I-06 | La falla se registra sin el objeto de la excepción: `[EXPORT] no se pudo exportar: {Tipo}: {mensaje depurado}` | El mensaje crudo y la pila traen la ruta del equipo |
| I-07 | `ExportLog.Scrub` también cambia la ruta con la otra barra (`/` ↔ `\`) | Los mensajes de .NET mezclan barras |
| I-08 | `TcxFileName.Sanitize` cambia sustitutos sueltos y U+FFFE/U+FFFF por `-` antes de NFC (y sigue sin normalizar si el runtime rechaza otro) | `string.Normalize` lanza con esos caracteres |
| I-09 | `Notes` va si el nombre, ya limpio (`TcxXml.Clean`), no queda vacío ni en blanco | Un nombre solo de controles daría `<Notes></Notes>` |
| I-10 | `It12Ids`: AC-STR-09-26 lleva también `d` (`ES.SavedNoticeHasFileAndLocation`: nombre y `_2` con dotnet); -25 y -27 son `eq` (solo equipo o demo) | Trazabilidad con lo que sí corre en dotnet |
| I-11 | `ItTwelveToolingTests` ignora con el motivo lo de WP-B1/B2 mientras no existe (manifiesto, `AndroidFileExporter`, `CredentialNeedles`, set `it12`) | Mismo patrón que `ItSixAToolingTests` |
| I-12 | Para WP-B1: la `ExportLocation` de Android lleva una `FixedLabel` (p. ej. `cache\exports`) para el registro; el aviso no la usa | `ExportFolderLabel.For` sin etiqueta nombra Documentos |

## 14. Implementación de WP-B (desvíos, una línea cada uno)

| # | Desvío o precisión | Motivo |
|---|---|---|
| B-01 | WP-D no había entregado el arte: `ExportGlyph` (Core, polígonos de download y share en la caja de 24), `ExportGlyphView` (Painter2D) y `ExportGlyphTests` los hizo WP-B según el contrato de §8; el Designer puede pulirlos (esquinas redondeadas) | Las capturas no podían salir con el relleno provisorio |
| B-02 | El USS del botón y del aviso con acción va en un bloque "It. 12" de `Base.uss` (estructura: `.rm-toast__body`, `.rm-toast--action`, `.rm-toast__detail`, `.rm-toast__action`, `.rm-export-button--busy`); `Skin.uss` sin cambios | Ídem B-01; cero tokens nuevos |
| B-03 | El aviso de error también usa la cápsula de vidrio (`.rm-toast--link`) sin indicador, no solo el de éxito | 00-arte §4: "la misma cápsula con acción" |
| B-04 | `ToastLayer` mete el texto en `#toast-body` (columna con `#toast-detail`); `#toast-action` es un `RmButton` `text` con `.rm-type-button-s` y el único hijo con `pickingMode = Position`. Tocar la acción saca el aviso y después la ejecuta | §5.2; Reintentar puede publicar otro aviso |
| B-05 | `ActionToast` es una clase, no un `record` | Presentation no tiene el shim de `IsExternalInit` |
| B-06 | `RowMate.Platform` referencia `RowMate.Infrastructure` (`FolderExportWriter`); `ARCHITECTURE` §3 actualizado | DT12-09: el mismo escritor en los tres exportadores |
| B-07 | `ExportLocations.CreateExporter` (Bootstrap) envuelve `FileExporterFactory`: lo usan `AppRoot` y `TcxExportFrameTests` | PlayMode no puede referenciar Platform (AsmdefLint) |
| B-08 | `WindowsFileExporter` sirve también al Editor de Mac (`open -R`); iOS y Android en el Editor: sin exportador | §3.3; la app de Mac es de la 10b |
| B-09 | `UiHostOptions`: `Exporter` (null = `FakeFileExporter` propio en `<temp>/RowMate.Tests/exports-<id>`, borrado en `Dispose`), `FakeExportDelivery`, `ExportZone`, `SimulatorBuild`; `UiTestHost.FakeExporter`, `ExportFolder` y `PointerMove` | §6.2; el falso sobrevive a `Reopen` |
| B-10 | `ScenarioScript.Hover` deja sin transición de USS la capa de estado del elemento antes del puntero | El `:hover` de `Motion.uss` corre con el reloj real: CAP-12 salía distinta entre corridas |
| B-11 | Los `.kt` y el manifiesto no nombran `androidx` ni siquiera en comentarios; `check.ps1` lo prohíbe (junto con `java.net`, `INTERNET`, `WRITE_EXTERNAL_STORAGE` y el reproductor de Unity) | DT12-18 |
| B-12 | `RowMateExport.androidlib` suma `proguard-rules.pro` (`-keep class com.rowmate.export.**`) | R8 quitaría `RmExport`, que solo se llama por JNI |
| B-13 | `FindCredentialTraces` corre sobre TODO build de Windows (normal, desarrollo y simulador), antes del inspector del release | §3.6 |
| B-14 | `tools/sessions tcx --data-dir <d> --out <o> [--variant]`: la sesión más nueva de una COPIA de la base, zona `TimeZoneInfo.Local`, libres con "Libre" | DT12-20, m-05 |
| B-15 | El humo `-rowmate-export-smoke 2` corre en ventana (800 × 480) con `ROWMATE_BLE=off`, como el de la 10a | Sin tocar la radio |
