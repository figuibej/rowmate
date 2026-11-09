# ADR 024: Exportar archivos por un solo puerto

- **Estado:** Aceptado (2026-11-09), al implementar WP-B de la It. 12 (DT12-07 a DT12-10 y DT12-18 de [02-tech-design](../../iterations/12-exportar-tcx/02-tech-design.md))
- **Ámbito:** Core (`Ports/IFileExporter`, `ExportRequest`, `ExportedFile`, `ExportLocation`, `ExportDelivery`; `Export/*`), Infrastructure (`Export/FolderExportWriter`), App (`Export/TcxExportService`), Platform (`Export/WindowsFileExporter`, `ExplorerReveal`, `AndroidFileExporter`, `FileExporterFactory`), Bootstrap (`ExportLocations`), Presentation (`History/ExportTcxButton`, `Shell/ExportNoticePresenter`, `ToastLayer.ShowAction`), `Assets/Plugins/Android/RowMateExport`
- **Relacionados:** [ADR 002](002-capas-y-asmdefs.md) (capas), [ADR 009](009-ble-nativo.md) (plugins nativos propios), [ADR 021](021-preferencias.md) (la carpeta de datos y `ROWMATE_DATA_DIR`)

## Contexto

El usuario descartó Strava (la API pide suscripción paga) y la It. 12 deja solo el archivo TCX: el usuario lo sube a mano. Windows tiene que guardar en una carpeta visible con un aviso y "Abrir carpeta"; Android, abrir la hoja de compartir; iOS y macOS quedan para la 10b. Las pruebas, las capturas y los humos no pueden escribir en Documentos ni en Descargas ni abrir el Explorador. Armar el TCX de una sesión de 3 h y escribirlo no puede trabar el cuadro de la UI.

## Decisión

1. **Un puerto, `IFileExporter`** (Core): `Delivery` (`None`, `SaveToFolder`, `Share`), `Location` (para el registro sin rutas), `ExportAsync(ExportRequest)` y `Reveal(ExportedFile)`. "Abrir carpeta" es parte del puerto (no hay un `IFolderRevealer` aparte). `Delivery = None` (sin exportador: dotnet, iOS) apaga el botón.
2. **El contenido es una función**: `ExportRequest.Content` es un `Func<byte[]>` que el exportador real corre en `Task.Run` (con datos inmutables) y el falso en línea. App y Core no ven hilos; la continuación vuelve al hilo principal por el contexto de Unity.
3. **Escribir es de Infrastructure** (`FolderExportWriter`, netstandard2.1, probado con dotnet): temporal `~rowmate-*.part` + `Flush(true)` + `File.Move` sin pisar, sufijos `_2`, `_3`… sin distinguir mayúsculas, y el temporal se borra siempre. Los tres exportadores (Windows, Android y el falso de las pruebas) lo usan: colisiones y atomicidad se prueban una vez. Por eso `RowMate.Platform` referencia a `RowMate.Infrastructure` (la flecha sigue hacia adentro).
4. **La carpeta la elige Bootstrap** (`ExportLocations`): `{ROWMATE_DATA_DIR}\exports` con la etiqueta `ROWMATE_DATA_DIR\exports` en el Editor y en desarrollo (la cadena solo bajo `#if`), si no `GetFolderPath(MyDocuments)\RowMate\TCX` (respeta OneDrive). Calcular la ruta no crea nada: la carpeta la crea el escritor al escribir. El aviso nombra la carpeta por su etiqueta (`ExportFolderLabel`), nunca por la ruta real.
5. **Android sin dependencias ni permisos**: un `ContentProvider` propio (`RmExportProvider`, `exported=false`, `grantUriPermissions=true`, solo lectura de `cacheDir/exports`, un segmento de ruta canónico) en lugar del `FileProvider` de las bibliotecas de soporte, y `RmExport.share` con `ACTION_SEND` + `createChooser` y `FLAG_GRANT_READ_URI_PERMISSION`. Los `.tcx` de más de 24 h se purgan antes de escribir. `native/android/check.ps1` compila el Kotlin con `-Werror` y compara las firmas de `RmExport` con `golden/export-signatures.txt`.
6. **En las pruebas y el arnés, `FakeFileExporter`** (por defecto en `UiTestHost`): escribe con el escritor real en una carpeta temporal que el anfitrión borra, muestra la etiqueta de Documentos, `Hold()` y `FailNext()` para las tomas, y "Abrir carpeta" solo se registra.

## Consecuencias

- Una plataforma nueva (iOS con la hoja de compartir, macOS con `open -R`) es un exportador más detrás del mismo puerto; el botón, el servicio y el aviso no cambian.
- Ningún test, toma ni humo escribe en las carpetas del usuario ni abre el Explorador (`ExplorerRevealTests` prueba solo las cadenas y un lanzador falso).
- Si `explorer.exe /select` falla en algún equipo, queda solo en el registro (issue: `SHOpenFolderAndSelectItems` por P/Invoke).
- La hoja de compartir real y el `ContentProvider` en un teléfono quedan NO VERIFICABLES hasta "Android Build Support".
