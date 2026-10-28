# It. 3 — Rutinas: lista, editor y base local: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-27 |
| Iteración | 3 · `03-rutinas` ([ROADMAP §It. 3](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) corregida por el gate (102 AC: 91 nuevos + 11 de iteraciones anteriores, con AC-BLE-16-02 que cambia por DV-13; 28 CAP, 12 DEMO, 3 VID), [04-design-notes](04-design-notes.md) (DN3-01 a DN3-23: Anexo B), [03-qa-harmony](03-qa-harmony.md) (gate: "Cambios por gate QA", al final), [00-deuda-it2](00-deuda-it2.md) (los 15 issues de la It. 2 cerrados; AC-BLE-11-02 en PASS), [00-deuda-diseno](00-deuda-diseno.md), [02-tech-design de la It. 2](../02-dispositivo-simulador/02-tech-design.md) (AppComposer, stores, ShellHost, UiTestHost, arnés), [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §2-§9, §15, ADR 002, 005, 007, 008, 013, [ART_BIBLE](../../design/ART_BIBLE.md) §2, §3.6-§3.11, [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §2.2-§2.3 |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/core/database/database_service.dart:1-212`, `lib/core/models/routine.dart:95-138`, `lib/core/models/interval_step.dart:108-181`, `lib/features/routines/{routines_provider,routines_screen,routine_editor_screen}.dart` (enteros), `lib/main.dart:40-115`, `lib/core/dev/simulator_overlay.dart:25-60`. SDK de Flutter: `<flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm_shared/lib/integers_patch.dart:25-125`, `<flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm/lib/string_patch.dart:485-499`, `<flutter-sdk>/packages/flutter/lib/src/material/input_decorator.dart:1969`, `:2346`; para el gate, además `<flutter-sdk>/packages/flutter/lib/src/widgets/routes.dart:1153-1156`, `:1215-1223`, `:2273-2281`, `widgets/editable_text.dart:3736-3753`, `:3914-3947`, `:5642-5648`, `widgets/app.dart:1271-1276` y `widgets/focus_traversal.dart:204-213`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificación previa | **Sonda S3-a** (dotnet, `<scratch>/it3/arch-s3/`): el mismo código de acceso a datos compilado contra `sqlite-net-pcl` 1.9.172 y contra el `SQLite.cs` de `com.gilzoide.sqlite-net` 1.3.3 con su binario nativo; 22 de 22 comprobaciones en los dos (Anexo A.1). **Sonda S3-b** (Unity 6000.3.27f1 en batchmode, proyecto descartable del scratch, nunca el repo): menú contextual, selección, `PanelSettings.textSettings` y `GenericDropdownMenu` en un panel de runtime (Anexo A.2). **Para el gate**: `SQLite.cs` de gilzoide en `Library/PackageCache/com.gilzoide.sqlite-net@34340bf93526/Runtime/sqlite-net/` (alias de *handle* `:40-55`, la ruta dentro del mensaje de error `:411`, `QueryScalars` `:1149`, API de bajo nivel `:4802-4944`) y `Plugins/sqlite-amalgamation/sqlite3_defines.h` (`:5`, `:7`, `:10`); `sqlite-net-pcl` 1.9.172 por reflexión (los 13 métodos de bajo nivel que usa §3.1 existen, públicos y estáticos); documentación XML de `UnityEngine.UIElementsModule` 6000.3.27f1 (`WhiteSpace`, `TextElement.parseEscapeSequences`) |
| Estado | Diseño, sin código. **Corregido por el gate de armonía de QA** (APROBADO CON OBSERVACIONES: 0 bloqueantes, 7 mayores, 14 menores): los 4 mayores y los 12 menores del Architect quedan resueltos y 04-design-notes queda integrado (Anexo B; detalle en "Cambios por gate QA", al final). Sigue: **WP-D0** → **WP-A** → **WP-B** → **WP-D** (§7) |

Convenciones: rutas relativas a `Assets/RowMate/` salvo que empiecen con `tools/`, `docs/` o `ProjectSettings/`. Marcas: **N** nuevo, **C** cambia, **G** generado. `dp` = px de USS (ARCHITECTURE §7.3). Identificadores en inglés; comentarios y textos de diseño en español. Las citas `lib/...:línea` son del legacy.

---

## 0. Resumen

1. **Base propia v8 con SQLite real en los dos mundos.** `RowMate.Infrastructure` pasa a usar `sqlite-net` (en Unity, el paquete `com.gilzoide.sqlite-net` 1.3.3, ya instalado; en dotnet, `sqlite-net-pcl` 1.9.172 con `SQLitePCLRaw.bundle_green` 2.1.11). El mismo código de repositorio compila y corre en los dos (S3-a, Anexo A.1). Esquema = DDL v7 del legacy, más `workout_sessions.origin` y el índice de `data_points`, con `user_version = 8`. Archivo `rower_app.db` o `rower_app_dev.db` según la variante, en la carpeta **privada** de la app (en Android, `getDatabasePath`, no `persistentDataPath`, que es almacenamiento externo).
2. **Una base que no se entiende no se toca.** Antes de abrir, se lee el encabezado de 100 bytes con E/S común; si el archivo existe, se abre primero **en solo lectura** para leer `user_version` (S3-a: un `-wal` puede tener una versión más nueva que el encabezado). Solo con `user_version` 8, o con una base vacía, se abre en lectura y escritura. `F-ROTA`, una versión mayor que 8 y cualquier otra rareza quedan en "error de carga" con los bytes intactos (medido).
3. **Todo o nada y durable.** Alta, edición y baja en una transacción, `journal_mode = WAL` y `synchronous = FULL`. El editor se cierra recién cuando el *commit* volvió. La falla "a la mitad" se prueba con un *trigger* real en la base de prueba, sin costuras en el código de producción (S3-a). Los pragmas se leen de vuelta en los dos SQLite: el de Unity compila con WAL en `NORMAL` por defecto y sin comillas dobles (QA-G3-08), así que nada depende del valor por defecto.
4. **Fuera del hilo principal y de vuelta.** Toda llamada a SQLite pasa por `IDbExecutor`: en la app, `SerialExecutor` (un hilo dedicado con cola); la continuación del `await` vuelve al hilo principal por el `SynchronizationContext` de Unity (ADR 005). En pruebas de pantalla y en el arnés, `InlineDbExecutor` sobre SQLite real: determinista cuadro a cuadro.
5. **Reglas del editor puras y probadas en dotnet.** Lectura de enteros como `int.tryParse` de Dart (exacta para cualquier largo, con `BigInteger`), recorte con los espacios de Dart, topes de Q-92 por campo y por repetición en forma cerrada (una serie ×2 000 000 000 no hace ningún bucle), operaciones sobre la plantilla (agregar, serie por defecto, agregar a serie, reemplazar, borrar, borrar serie, ×N), validaciones al guardar y la secuencia de la mini-barra sin expandir. `RoutinesStore` porta `RoutinesProvider` con Q-13 y `RoutineEditorStore` porta el estado del editor con Q-19.
6. **Navegación secundaria completa.** `PageStack` (el editor tapa la barra; al terminar la entrada, pestañas y barra pasan a `display: none` y conservan su estado), `DialogLayer` (velo, foco atrapado, tocar afuera = cancelar), `ToastLayer` (4 s, el nuevo reemplaza al visible), `PopupLayer` nueva (menú `⋮`, menú de texto, ayudas emergentes), y la capa del simulador sigue siendo el último hijo. "Atrás" de Android cierra lo de arriba (menú, diálogo, página); Escape solo menús y diálogos (P-08). **Lo que sale no responde**, como en Flutter: la página, el diálogo o el menú que se cierran quedan inertes desde el primer cuadro de su salida y el editor queda cerrado después de guardar (QA-G3-01).
7. **Texto en UI Toolkit.** `TextField` de Unity envuelto en `RmTextField` (etiqueta, error, ayuda, foco, selección). S3-b confirmó lo que falta en runtime: no hay menú contextual (`contextualMenuManager` es `null`) ni ayudas (`tooltip` es solo del Editor), y `GenericDropdownMenu` se cuelga de la raíz del panel, por encima de la capa del simulador. Por eso el menú del clic derecho, el `⋮` y las ayudas son propios, en `#popup-layer`, con textos del catálogo. Las 4 claves `text*` se agregan: en la PC no hace falta la alternativa de P-06; en Android queda preparada (el mismo menú al mantener presionado, R3-13) y se decide con el teléfono. `RmTextField` fija lo que el `TextField` de runtime hace distinto de Flutter (Tab, Escape, Enter, marcas en el texto), con la sonda S-B3a antes de B2 (§4.10).
8. **42 IDs nuevos en el arnés** (27 CAP, 12 DEMO y 3 VID; 47 archivos con las cinco capturas dobles), en un conjunto propio (`-snapshotSet it3`) que escribe en `03-rutinas/snapshots/`, con secuencias de varias tomas (la demo) y "reabrir la app sobre la misma base" (DEMO-10). CAP-27 sale del build normal con `capture-cap27.ps1`, solo a pedido y fuera del CI, con la base del usuario resguardada y verificada (§5.5, QA-G3-06).
9. **Paquetes:** WP-D0 (Designer: dos glifos, seis tokens con `overlay.scrim` —127 → 133—, `Typography.uss`, pares de contraste y la fuente Noto Emoji) → WP-A (≈ 80 archivos, dotnet) → WP-B (≈ 75 archivos, Unity, en tres entregas) → WP-D (pulido). Archivos disjuntos y contrato USS en §7.5, al día con 04-design-notes (Anexo B).

| Pregunta de la tarea | Dónde |
|---|---|
| Árbol de archivos, asmdefs, manifest | §1 |
| Base de datos: DDL, apertura, versiones, ubicación, simulador, hilos, pruebas | §2 |
| Repositorio, stores y reglas del editor (firmas y fuente legacy) | §3 |
| UI: lista, editor, diálogos, menús, avisos, `PageStack`, capa del simulador, texto y teclado | §4 |
| Capturas, demo y videos en el arnés | §5 |
| Tests y matriz AC → test / captura | §6 |
| Paquetes de trabajo y contrato USS | §7 |
| Riesgos y desvíos | §8 |
| Evidencia de las sondas | Anexo A |
| Pedidos de 04-design-notes (DN3-01 a DN3-23) | Anexo B |
| Correcciones del gate de QA | "Cambios por gate QA" (al final) |

---

## 1. Estructura

### 1.1 Árbol de archivos

Dueño entre corchetes: **[A]** WP-A, **[B]** WP-B, **[D0]** WP-D0, **[D]** WP-D (§7).

#### Core (`RowMate.Core`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `Core/Text/DartText.cs` | N [A] | `IsWhitespace(char)` y `Trim(string)` con el juego de espacios de Dart (`string_patch.dart:485-499`: 9-13, 32, U+0085, U+00A0, U+1680, U+2000-U+200A, U+2028, U+2029, U+202F, U+205F, U+3000, **U+FEFF**). `string.Trim()` de .NET no trata U+FEFF como espacio |
| `Core/Text/DartIntParser.cs` | N [A] | `int.tryParse` de Dart sin base (`integers_patch.dart:84-125`): espacios de Dart a los costados, signo opcional, ceros a la izquierda, `0x`/`0X` hexadecimal. Devuelve `BigInteger?` **exacto** (null = vacío o no numérico). Nunca desborda ni lanza |
| `Core/Routines/StepLimits.cs` | N [A] | topes de Q-92 y textos `{max}` (§3.4); `FirstRepetitionOutOfRange` en forma cerrada |
| `Core/Routines/StepLimitCheck.cs` | N [A] | qué valores de un paso pasan su tope (por campo y por repetición) y si una plantilla entera los cumple (validación 3 de AC-RUT-11-05) |
| `Core/Routines/RoutineTemplate.cs` | N [A] | operaciones puras sobre la plantilla (`routine_editor_screen.dart:75-200`) |
| `Core/Routines/EditorLayout.cs`, `EditorItem.cs` | N [A] | bloques del editor: paso suelto, encabezado, paso de serie, pie (`:275-295`) |
| `Core/Routines/SeriesIdGenerator.cs` | N [A] | `g{milisegundos}_{contador}` (`:111`) |
| `Core/Routines/RoutineStructure.cs` | N [A] | secuencia de la mini-barra sin expandir, con total saturado (AC-RUT-10-11) |
| `Core/Routines/StructureBarLayout.cs` | N [A] | geometría de la mini-barra (DN3-07, 04-design-notes §4.3): capacidad `⌊(W + 2) / 4⌋`, ancho de tramo `min(24; (W − 2·(n − 1)) / n)` con mínimo 2, desborde (los primeros `capacidad` a 2 dp y los últimos 6 que se apagan) y pista vacía; constantes `SegmentHeightDp` 4, `SegmentRadiusDp` 2, `SegmentGapDp` 2, `SegmentMaxWidthDp` 24, `SegmentMinWidthDp` 2, `OverflowFadeSegments` 6 |
| `Core/Design/BottomObstacles.cs` | N [A] | regla pura de los obstáculos inferiores (DN3-13, DN3-14): un rectángulo sube hasta `gap` dp sobre cada obstáculo que **se cruza** con él agrandado `gap`, y se repite hasta no cruzar ninguno. La usan el aviso (gap 8), la llave (gap 16) y los rellenos inferiores de los contenidos desplazables |
| `Core/Routines/StepSubtitle.cs` | N [A] | subtítulo del editor partido en "antes del 📈", ícono y "después" (DV-10); el texto completo, igual al legacy |
| `Core/Ports/IRoutineRepository.cs` | N [A] | puerto de persistencia de rutinas (§3.1) |
| `Core/Ports/IClipboard.cs` | N [A] | `bool HasText`, `string Text { get; }`, `void SetText(string)` |
| `Core/Ports/ISoftKeyboard.cs` | N [A] | teclado del teléfono: `bool IsVisible`, `double OccludedHeightPx` (lo lee `KeyboardAvoider`) |
| `Core/Design/MotionSpecs.cs` | C [A] | las constantes de 04-design-notes §5.11 (DN3-16): pila, diálogo, menú, ayuda, aviso (con el empujón), lista, crecer y colapsar, resaltado, hojas, etiqueta del campo, llave, desplazamientos y la gracia del indicador (§4.9) |
| `Core/Design/UiTimings.cs` | N [A] | aviso 4 s (`snack_bar.dart:29`); ayuda: aparece al apoyar el puntero, se va 0,1 s después, 1,5 s al mantener presionado (`tooltip.dart:392-394`) |
| `Core/Localization/L10nKeys.cs` | C [A] | las 16 claves nuevas de spec §11.2 |
| `Core/Design/Icons.cs` | C [D0] | `ArrowBack` (U+E5C4) y `MoreVert` (U+E5D4) |
| `Core/Design/Tokens.cs` | C [D0] | `overlay.scrim` (`#000000`, rol gráfico) y los cinco de 04-design-notes §2.1 (DN3-01): `border.input`, `text.selection`, `titleXs` 14/600, `headerS` 13/700, `buttonS` 12/600; **127 → 133** |
| `Core/Design/FontAssetPaths.cs` | C [B] | `EmojiFallback` (Noto Emoji) dentro de `All` (9 → 10): así `GeneratedAssetGuard` también lo protege (QA-G3-12, QA-V2-07). Va en B2, junto con su `FontAsset` y el bootstrapper |

#### App (`RowMate.App`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `App/Routines/RoutinesPhase.cs`, `RoutinesState.cs`, `RoutinesStore.cs` | N [A] | puerto de `routines_provider.dart` con Q-13 (§3.2) |
| `App/Routines/EditorStep.cs`, `RoutineEditorState.cs`, `SaveOutcome.cs`, `RepeatChange.cs`, `RoutineEditorStore.cs` | N [A] | estado y acciones del editor (§3.3); cerrado después de `Saved` (QA-G3-01) |
| `App/Routines/StepField.cs`, `FieldError.cs`, `StepDialogResult.cs`, `StepDialogForm.cs` | N [A] | diálogo de paso: precarga, `_build()`, topes y vista previa (§3.4) |
| `App/Routines/RoutineCardModel.cs`, `StepRowModel.cs`, `RoutineTexts.cs` | N [A] | textos ya resueltos de la tarjeta, la fila y los errores (resúmenes, subtítulos, `editorErrorRange…`) |
| `App/Composition/AppServices.cs` | C [A] | + `IRoutineRepository Routines` (posicional) y `IClock? CreationClock { get; init; }`: lo lee **solo** `RoutinesStore` (la fecha de creación); los ids de serie usan el `Scheduler` (QA-G3-19) |
| `App/Composition/AppContext.cs` | C [A] | + `RoutinesStore Routines`, `CreateEditor(Routine?)`; `Start()` también carga las rutinas (AC-APP-01-09) |
| `App/Composition/AppComposer.cs` | C [A] | arma `RoutinesStore` |

#### Infrastructure (`RowMate.Infrastructure`, sin API de Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Infrastructure/RowMate.Infrastructure.asmdef` | C [A] | referencias `RowMate.Core` + **`Gilzoide.SqliteNet`** (§1.2) |
| `Infrastructure/link.xml` | N [A] | preserva `RowMate.Infrastructure` y `Gilzoide.SqliteNet` enteros (mapeo por reflexión, RA-05) |
| `Infrastructure/Threading/IDbExecutor.cs`, `SerialExecutor.cs` | N [A] | §2.6 |
| `Infrastructure/Database/DatabaseFiles.cs` | N [A] | `rower_app.db`, `rower_app_dev.db`, `SchemaVersion = 8` (`database_service.dart:10-12`) |
| `Infrastructure/Database/DatabaseSchema.cs` | N [A] | DDL v8 exacto y `WritePragmas` (§2.1) |
| `Infrastructure/Database/DatabaseHeader.cs` | N [A] | lectura de los 100 bytes del encabezado con `FileShare.ReadWrite`; un archivo de 1 a 99 bytes es `Unreadable`, empiece o no con la firma (QA-G3-13) |
| `Infrastructure/Database/DatabaseProblem.cs`, `DatabaseOpenException.cs` | N [A] | `Unreadable`, `TooNew`, `Unrecognized`, `Io` |
| `Infrastructure/Database/RowMateDatabase.cs` | N [A] | apertura perezosa, protocolo de §2.3, `RunAsync`, cierre; `QueryRawForTestsAsync` y `ExecuteRawForTestsAsync` (`internal`, solo tipos primitivos, QA-G3-07); registra el nombre del archivo, nunca su ruta (QA-G3-20) |
| `Infrastructure/Routines/RoutineRow.cs`, `IntervalStepRow.cs` | N [A] | filas con `[Table]`/`[Column]`; enteros como `long?` |
| `Infrastructure/Routines/RoutineRowMapper.cs` | N [A] | fila ↔ modelo (`routine.dart:107-122`, `interval_step.dart:111-145`), con `checked` a `int` |
| `Infrastructure/Routines/SqliteRoutineRepository.cs` | N [A] | §3.1 |
| `Infrastructure/Diagnostics/DatabaseSmoke.cs` | N [A] | prueba de humo de la base para un *player* (crea, escribe, lee y borra en una carpeta temporal; §6.1); su salida no lleva rutas (QA-G3-20) |

#### Simulator (`RowMate.Simulator`, puro, con el define)

| Archivo | Marca | Contenido |
|---|---|---|
| `Simulator/Panel/SimulatorPanelLayout.cs` | C [A] | posición de la llave con obstáculos (DV-13 y AC-BLE-16-02 nuevo, §4.8): `FabBottom(tabBarHeightDp, IReadOnlyList<BottomObstacle>)` con `BottomObstacles` (gap 16); `PanelBottom(tabBarHeightDp)` = barra + 16 **siempre** (el panel no sigue a la llave); el alto máximo del panel sigue la regla de AC-BLE-16-03 |

#### Platform (`RowMate.Platform`, Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Platform/Storage/AppDataLocation.cs` | N [B] | carpeta de la base por plataforma (§2.4) |
| `Platform/Input/SystemClipboard.cs` | N [B] | `IClipboard` sobre `GUIUtility.systemCopyBuffer` (el mismo que usan Ctrl+C/X/V de UI Toolkit) |
| `Platform/Input/SoftKeyboard.cs` | N [B] | `ISoftKeyboard` sobre `TouchScreenKeyboard.visible` y `.area` (Android/iOS); en escritorio, siempre oculto. Si en [Device-And] `.area` no es confiable, el alto sale del marco visible de la ventana por JNI (R3-13) |
| `Platform/PlatformServices.cs`, `PlatformFactory.cs` | C [B] | + `Storage`, `Clipboard`, `Keyboard` |

#### Presentation (`RowMate.Presentation`, Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Presentation/Shell/IPage.cs`, `PageStack.cs` | N [B] | §4.2 |
| `Presentation/Shell/DialogLayer.cs`, `ModalDialog.cs` | N [B] | §4.5 (con `ExitLock` y el velo que se traga los toques al salir) |
| `Presentation/Shell/ToastLayer.cs` | N [B] | §4.7 |
| `Presentation/Shell/PopupLayer.cs` | N [B] | menús, menú de texto y ayudas (§4.6) |
| `Presentation/Shell/FocusTrap.cs` | N [B] | Tab y Mayús+Tab dentro del diálogo o menú de arriba (`FocusController.IgnoreEvent`) |
| `Presentation/Shell/ExitLock.cs` | N [B] | lo que sale no responde (QA-G3-01, §4.2): al empezar la salida, todo el árbol de la página, el diálogo o el menú pasa a `pickingMode = Ignore` y `focusable = false`, y el foco sale de ahí |
| `Presentation/Shell/BackNavigation.cs` | C [B] | `HandleBack(BackSource)`: menú → diálogo → página → raíz (§4.2) |
| `Presentation/Shell/AppShellView.cs` | C [B] | crea las capas antes que las pantallas, `#popup-layer`, y `RoutinesScreenView` en lugar del provisorio |
| `Presentation/Shell/ShellLayout.cs` | C [B] | alto de la barra **conservado** mientras está oculta; obstáculos inferiores (llave, `+`) con `BottomObstacles`; *padding* inferior de cada contenido desplazable = tope del obstáculo más alto que se cruza + 16 (lista; editor con mínimo 24), más área segura y teclado (DN3-14) |
| `Presentation/UiContext.cs` | C [B] | + `Pages`, `Dialogs`, `Toasts`, `Popups`, `Clipboard`, `Keyboard` |
| `Presentation/Controls/RmTextField.cs`, `TextContextMenu.cs` | N [B] | campo de texto y menú del clic derecho (§4.10); el menú también puede abrirse al mantener presionado (modo táctil, R3-13) |
| `Presentation/Controls/FocusScroller.cs` | N [B] | lleva a la vista lo que recibe el foco dentro de `#routines-list`, `#editor-scroll` y `.rm-dialog__content` (QA-G3-11), y lo nuevo o el primer error (DN3-18, DN3-10) |
| `Presentation/Controls/PopupMenu.cs`, `Tooltip.cs` | N [B] | menú anclado y ayuda emergente (posiciones de 04-design-notes §4.4; la ayuda, entera con `pickingMode = Ignore`) |
| `Presentation/Controls/SegmentedControl.cs` | N [B] | tipo de paso en mosaicos (`.rm-segmented--tiles`, DV-09, DN3-08) y modo tiempo/distancia |
| `Presentation/Controls/Fab.cs` | N [B] | botón flotante 56 |
| `Presentation/Controls/KeyboardAvoider.cs` | N [B] | ARCHITECTURE §7.3 (F-APP-15.4) |
| `Presentation/Controls/IconButton.cs`, `RmButton.cs`, `Spinner.cs` | C [B] | ayuda emergente opcional; tamaño del ícono por parámetro (filas y encabezado 20, pie 16; por defecto 24 y 18, DN3-17); tamaño y trazo del *spinner* por parámetro (32 / 3, DN3-15) |
| `Presentation/Surface/RuntimePanelFactory.cs` | N [B] | la **única** fábrica de la copia de `PanelSettings` con su copia de `PanelTextSettings` y los respaldos de fuente de 04-design-notes §4.15 (QA-G3-12, §4.4); la usan `AppRoot`, `UiTestHost` y el arnés |
| `Presentation/Routines/RoutinesScreenView.cs`, `RoutineListView.cs`, `RoutineCardView.cs`, `StructureBarView.cs` | N [B] | §4.3 |
| `Presentation/Routines/RoutineEditorPage.cs`, `EditorStepsView.cs`, `StepRowView.cs`, `SeriesBlockView.cs`, `StepSubtitleView.cs` | N [B] | §4.4 |
| `Presentation/Routines/StepDialog.cs`, `RepeatDialog.cs`, `DeleteRoutineDialog.cs` | N [B] | §4.5 |
| `Presentation/Motion/PageTransition.cs`, `DialogTransition.cs`, `ToastTransition.cs`, `CollapseMotion.cs`, `GrowMotion.cs` | N [B] | §4.9 |
| `Presentation/Styles/Base.uss` | C [B] | estructura y contrato de §7.5 |
| `Presentation/Styles/Typography.uss` | G [D0] | regenerado por `tools/tokens` con `.rm-type-title-xs`, `.rm-type-header-s` y `.rm-type-button-s` (DN3-01) |
| `Presentation/Styles/Skin.uss`, `Motion.uss` | C [D] | aspecto |

#### Simulator.Presentation (Unity, con el define)

| Archivo | Marca | Contenido |
|---|---|---|
| `SimulatorPresentation/SimulatorLayerView.cs` | C [B] | llave con obstáculos y alto de barra conservado; registra la llave como obstáculo para los avisos y el contenido (§4.8) |
| `SimulatorPresentation/Styles/SimulatorSkin.uss` | C [D] | si cambia la llave |

#### Bootstrap (Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Bootstrap/DataLocation.cs` | N [B] | carpeta de datos con la variable `ROWMATE_DATA_DIR`, **solo** en `UNITY_EDITOR \|\| DEVELOPMENT_BUILD` (§2.4) |
| `Bootstrap/AppRoot.cs` | C [B] | base, ejecutor y repositorio; orden de cierre; `BackSource` por plataforma (el único camino de Escape: §4.10); `-rowmate-db-smoke` en desarrollo; la copia del panel sale de `RuntimePanelFactory` en `Awake`, antes del `OnEnable` del documento |
| `Bootstrap/UiAssets.cs` | C [B] | + referencias a los `FontAsset` de respaldo (`RowMateIcons-Outline-SDF` y Noto Emoji), que asigna el bootstrapper |
| `Bootstrap/ShellHost.cs`, `ShellHostOptions.cs`, `ShellHandle.cs` | C [B] | portapapeles y teclado; `HandleBack(BackSource)` |

#### Editor, arnés, soporte y herramientas

| Archivo | Marca | Contenido |
|---|---|---|
| `Editor/Build/BuildOutputInspector.cs` | C [B] | agujas nuevas de diagnóstico: `ROWMATE_DATA_DIR`, `-rowmate-db-smoke` (no pueden quedar en el build normal) |
| `Editor/Bootstrap/UiAssetsBuilder.cs`, `FontAssetBuilder.cs`, `ProjectBootstrapper.cs` | C [B] | el `FontAsset` dinámico de Noto Emoji (dentro de `AllowWrites`) y las referencias de respaldo de `UiAssets` (B2) |
| `UI/Fonts/NotoEmoji/NotoEmoji-Regular.ttf`, `UI/Fonts/NotoEmoji/OFL.txt` | N [D0] | la fuente estática monocroma de respaldo para emojis (P-09, DN3-21) y su licencia SIL OFL 1.1 (el repo es público) |
| `UI/Fonts/Generated/NotoEmoji-Regular-SDF.asset` | G [B] | lo crea el bootstrapper en B2; versionado sin datos dinámicos, como los otros 9 (QA-V2-07) |
| `Tests/Support/InlineDbExecutor.cs`, `TempDatabase.cs`, `RoutineSeeds.cs`, `ScriptedRoutineRepository.cs`, `InMemoryRoutineRepository.cs`, `SequenceClock.cs`, `FakeClipboard.cs`, `FakeSoftKeyboard.cs` | N [A] | dobles y semillas (§2.7, §6.1) |
| `Tests/Support/RecordingLogger.cs` | C [A] | seguro entre hilos: un `lock` en `Log` y `Clear`, y `Lines`, `Errors` y `Levels` devuelven una copia tomada con el `lock` (QA-G3-09) |
| `Tests/EditMode/**` | N/C [A] | §6.2, **salvo** estas excepciones (QA-G3-15): `Tests/EditMode/Design/{IconsTests,ContrastTests,TokenCatalogTests,TokensContractTests,MetricPaletteTests,TokensUssSyncTests,TypographyUssSyncTests}.cs` son de **[D0]**; `Tests/EditMode/Project/ItThreeToolingTests.cs` e `ItTwoDebtTests.cs` (el conteo 9 → 10 de `FontAssetPaths.All`) son de **[B]** |
| `Tests/UiSupport/UiTestHost.cs`, `UiHostOptions.cs` | C [B] | base de prueba, reabrir, escribir, teclas, "Atrás"/Escape, clic derecho, puntero encima, mantener presionado; el panel con `RuntimePanelFactory`; `CreationClock` aparte del *scheduler* (QA-G3-19) |
| `Tests/UiSupport/UiAssetsLocator.cs` | C [B] | `Assets()`: el mismo `UiAssets.asset` de la app (panel y fuentes de respaldo), para que `UiTestHost` y el arnés armen el panel igual que `AppRoot` |
| `Tests/UiSupport/HostRoutines.cs` | N [B] | fábricas de repositorio de prueba (§5.2) |
| `Tests/PlayMode/*`, `Tests/PlayModeSimulator/*`, `Tests/UnityEditMode/*` | N/C [B] | §6.2 |
| `Snapshots/SnapshotCatalog.cs`, `SnapshotCli.cs`, `SnapshotRunner.cs`, `ScenarioScript.cs` | C [B] | conjuntos, secuencias, `Type`, `Shot`, `Reopen` (§5.1) |
| `Snapshots/RoutinesCatalog.cs` | N [B] | los 42 escenarios de §5.4 |
| `tools/dotnet/RowMate.Infrastructure/RowMate.Infrastructure.csproj` | C [A] | `sqlite-net-pcl` 1.9.172 + `SQLitePCLRaw.bundle_green` 2.1.11 |
| `tools/dotnet/run-tests.ps1` | C [A] | `-Matrix` ya lee todos los `It0*Ids.cs` (también `It03Ids.cs`): solo cambia el comentario de los conteos |
| `tools/unity/run-unity-tests.ps1`, `capture-snapshots.ps1` | C [B] | ponen `ROWMATE_DATA_DIR` en una carpeta temporal propia de la corrida y la borran en `finally`; `capture-snapshots.ps1 -Set it2|it3` (por defecto `it3`). Ninguno llama a `capture-cap27.ps1` |
| `tools/unity/capture-cap27.ps1` | N [B] | CAP-27 con las condiciones de QA-G3-06 y AC-HIS-20-04 (§5.5): solo con `-ReplaceUserDatabase`, se niega a correr con la app o el Editor abiertos o con archivos bloqueados, resguardo con *hash* verificado antes de tocar, restauración en `finally` con *hash* y fecha, base sintética. Ensayo `-Rehearsal` sobre una carpeta temporal (QA-V3-01). Usa `capture-window.ps1` para la foto, con una sola llamada (`Invoke-WindowCapture`) que el ensayo corre con un ejecutable de prueba sin ventana (QA-V3-11) |
| `tools/unity/capture-window.ps1` | C [B] | `-ClickRelative` (la pestaña Rutinas de CAP-27); solo hace clic y captura si la ventana del build es la de adelante (QA-V3-01). Sin `-Arguments` no pasa `-ArgumentList`; deja de esperar si el proceso termina; 4 para un error no previsto (QA-V3-11) |
| `tools/unity/new-windowless-exe.ps1` | N [B] | ejecutable de prueba sin ventana (subsistema gráfico, `Main` vacío) para correr de verdad el arranque de `capture-window.ps1`: lo usan el ensayo de CAP-27 y `ITT.WindowCaptureStartsTheExeForReal`, en carpetas temporales (QA-V3-11) |
| `tools/unity/README.md` | C [B] | lo anterior |
| `UI/Icons/RowMateIcons-Outline.ttf` | C [D0] | + `arrow_back`, `more_vert` (receta de ASSET_SLOTS §4.1) |
| `Presentation/Styles/Tokens.uss` | G [D0] | regenerado por `tools/tokens` |
| `docs/…/03-rutinas/snapshots/*` | G [B] | arnés (WP-D las regenera mientras itera) |

**Manifest:** `Packages/manifest.json` **no cambia**. `com.gilzoide.sqlite-net` 1.3.3 ya está (desde la It. 1) y trae los binarios de Windows x64/x86/arm64, Android arm32/arm64/x86/x86_64, macOS universal e iOS estático (`__Internal`); verificado en el caché de paquetes.

### 1.2 Asmdefs

Un solo cambio de referencias en los asmdefs puros:

```json
// Infrastructure/RowMate.Infrastructure.asmdef
{
    "name": "RowMate.Infrastructure",
    "rootNamespace": "RowMate.Infrastructure",
    "references": ["RowMate.Core", "Gilzoide.SqliteNet"],
    "includePlatforms": [], "excludePlatforms": [], "allowUnsafeCode": false,
    "overrideReferences": true, "precompiledReferences": [], "autoReferenced": false,
    "defineConstraints": [], "versionDefines": [], "noEngineReferences": false
}
```

- `Gilzoide.SqliteNet` (`rootNamespace` `SQLite`, `autoReferenced: true`) se nombra igual porque los asmdefs propios no reciben referencias automáticas. Es el motivo por el que Infrastructure no lleva `noEngineReferences` (ARCHITECTURE §3.1 (\*)): los atributos `[Column]`, `[PrimaryKey]`… del paquete heredan de `UnityEngine.Scripting.PreserveAttribute`. En dotnet esa clase no existe, y el código de Infrastructure solo usa los atributos de sqlite-net, que en `sqlite-net-pcl` heredan de `Attribute`. Compila igual en los dos (S3-a).
- **Ningún otro asmdef referencia `Gilzoide.SqliteNet`.** Las semillas que escriben SQL a mano (F-RARA) pasan por un método `internal` de `RowMateDatabase`, visible para `RowMate.Tests.Support` por el `InternalsVisibleTo` que ya existe.
- **Ningún archivo fuera de `Infrastructure/` usa tipos de sqlite-net** (QA-G3-07). En dotnet, `sqlite-net-pcl` llega en forma transitiva a todo proyecto que referencia Infrastructure, así que un test con `RunAsync(Func<SQLiteConnection, T>)` compilaría con dotnet y rompería `RowMate.Tests.EditMode` en Unity (su asmdef tiene `overrideReferences: true` y no nombra el paquete). Los tests leen la base solo con `RowMateDatabase.QueryRawForTestsAsync` (§3.1), que devuelve tipos primitivos. Lo controla `PureCodeRulesTests.SqliteOnlyInInfrastructure`: fuera de `Assets/RowMate/Infrastructure/`, ningún `.cs` tiene `using SQLite` ni nombra `SQLiteConnection`, `SQLiteException`, `SQLite3` o `SQLiteOpenFlags`.
- `InternalsVisibleTo` de Infrastructure ya incluye `RowMate.Tests.Support`, `.EditMode`, `.Simulator` y `.UnityApiCheck` (`Infrastructure/AssemblyInfo.cs`), no `RowMate.Tests.UnityEditMode`: por eso la prueba de la versión de SQLite en Unity (antes SQU) pasa a `Tests/EditMode` como `DSC.SqliteVersionIsTheExpectedOne`, que corre en los dos mundos (§6.2).
- Sin asmdefs nuevos. `RowMate.Presentation` sigue sin referenciar Infrastructure ni Platform (los puertos nuevos están en Core).
- `AsmdefLintTests` (WP-A): `References("Infrastructure")` pasa a `{ RowMate.Core, Gilzoide.SqliteNet }`; el par asmdef ↔ `.csproj` acepta `Gilzoide.SqliteNet` ↔ `PackageReference sqlite-net-pcl`; ningún asmdef salvo Infrastructure nombra el paquete.

---

## 2. Base de datos

### 2.1 Esquema v8 (DDL exacto)

Texto del legacy (`database_service.dart:76-138`), con `origin` al final de `workout_sessions` y el índice. Se ejecuta tal cual con `Execute` (nunca `CreateTable<T>`, que generaría otro esquema). `DatabaseSchema.Statements` en este orden:

```sql
CREATE TABLE routines (
  id          INTEGER PRIMARY KEY AUTOINCREMENT,
  name        TEXT NOT NULL,
  description TEXT NOT NULL DEFAULT '',
  created_at  TEXT NOT NULL
)

CREATE TABLE interval_steps (
  id                INTEGER PRIMARY KEY AUTOINCREMENT,
  routine_id        INTEGER NOT NULL,
  step_order        INTEGER NOT NULL,
  type              TEXT NOT NULL,
  duration_seconds  INTEGER,
  distance_meters   INTEGER,
  target_watts_min  INTEGER,
  target_watts_max  INTEGER,
  target_spm            INTEGER,
  target_split_seconds  INTEGER,
  group_id              TEXT,
  group_repeat_count    INTEGER,
  progression_spm       INTEGER,
  progression_watts     INTEGER,
  progression_split_seconds INTEGER,
  FOREIGN KEY (routine_id) REFERENCES routines(id) ON DELETE CASCADE
)

CREATE TABLE workout_sessions (
  id                    INTEGER PRIMARY KEY AUTOINCREMENT,
  routine_id            INTEGER,
  routine_name          TEXT,
  started_at            TEXT NOT NULL,
  finished_at           TEXT,
  total_distance_meters INTEGER NOT NULL DEFAULT 0,
  total_time_seconds    INTEGER NOT NULL DEFAULT 0,
  avg_power_watts       INTEGER NOT NULL DEFAULT 0,
  avg_stroke_rate       REAL    NOT NULL DEFAULT 0,
  total_calories        INTEGER NOT NULL DEFAULT 0,
  strava_activity_id    TEXT,
  origin                TEXT    NOT NULL DEFAULT 'app'
)

CREATE TABLE data_points (
  id                INTEGER PRIMARY KEY AUTOINCREMENT,
  session_id        INTEGER NOT NULL,
  elapsed_seconds   INTEGER NOT NULL,
  stroke_rate       REAL    NOT NULL DEFAULT 0,
  stroke_count      INTEGER NOT NULL DEFAULT 0,
  distance_meters   INTEGER NOT NULL DEFAULT 0,
  pace_500m_seconds INTEGER NOT NULL DEFAULT 0,
  power_watts       INTEGER NOT NULL DEFAULT 0,
  calories          INTEGER NOT NULL DEFAULT 0,
  heart_rate        INTEGER NOT NULL DEFAULT 0,
  step_index        INTEGER,
  step_type         TEXT,
  FOREIGN KEY (session_id) REFERENCES workout_sessions(id) ON DELETE CASCADE
)

CREATE INDEX idx_data_points_session ON data_points(session_id, elapsed_seconds)
```

- **Cascadas.** Las cláusulas `ON DELETE CASCADE` se conservan (paridad del DDL), pero `PRAGMA foreign_keys = OFF` (ADR 008, DC-06): el legacy nunca las activa. La baja de una rutina borra sus pasos **explícitamente**, en la misma transacción (Q-17). Las sesiones (It. 4a) y su vínculo con rutinas borradas se resuelven en la It. 9 (F-RUT-20).
- **Índices.** Solo el de `data_points`: AC-HIS-17-02 pide "exactamente" el esquema más ese índice. La carga lee **todos** los pasos en una consulta ordenada por `routine_id, step_order` y los agrupa en memoria: sin N+1 y sin índice en `interval_steps` (50 rutinas × 20 pasos son 1 000 filas).
- **Fechas.** `routines.created_at` en ISO-8601 UTC con `Z`, **siempre con 3 decimales** (`2026-10-27T13:00:00.000Z`): la rutina nueva toma `UtcNow` truncado al milisegundo. Con largo fijo, el orden del texto es el orden del tiempo, y `ORDER BY created_at DESC, id DESC` da la más nueva arriba aunque cambie la zona horaria (Q-18, AC-RUT-08-05). El lector (`IsoDate.Parse`) acepta también otros formatos: no rompe con una base tocada a mano.
- **Pragmas** de cada conexión de escritura (`DatabaseSchema.WritePragmas`, en este orden): `journal_mode = WAL` (persistente en el archivo), `synchronous = FULL` (DT-07: un *commit* sobrevive a un corte de luz, H-05), `foreign_keys = OFF`, *busy timeout* 5 s. **Se ejecutan siempre, aunque coincidan con el valor por defecto**: los dos SQLite no compilan igual (QA-G3-08). El de Unity (`Plugins/sqlite-amalgamation/sqlite3_defines.h` de gilzoide) trae `SQLITE_DEFAULT_WAL_SYNCHRONOUS 1` (`:7`: en WAL, `NORMAL` por defecto) y `SQLITE_DQS 0` (`:5`: un literal entre comillas dobles es un error); `e_sqlite3` de dotnet queda en `FULL` y acepta comillas dobles. Sin el pragma, dotnet seguiría en verde y el build perdería DT-07 sin aviso. `DSC.WriteConnectionIsWalFullAndNoForeignKeys` lee de vuelta, sobre la conexión de escritura, `journal_mode = wal`, `synchronous = 2` y `foreign_keys = 0`; vive en `Tests/EditMode`, así que corre en dotnet **y** en Unity EditMode contra gilzoide (AC-BASE-08).
- **Literales SQL solo entre comillas simples** (`DEFAULT ''`, `DEFAULT 'app'`, `RAISE(ABORT, 'falla inyectada')`): `PureCodeRulesTests.SqlLiteralsUseSingleQuotes` revisa los literales de C# con SQL de `Infrastructure/` y `Tests/Support/` (ninguno lleva `\"` ni `""`), y el control dinámico es la corrida de DSC, DOT y RRT en Unity EditMode.
- **Versión**: `PRAGMA user_version = 8`, escrito en la misma transacción que el DDL.

### 2.2 Archivo, nombre y variante

| Variante | Archivo | Quién decide |
|---|---|---|
| Build normal y Editor sin `RowMate → Modo simulador` | `rower_app.db` | `DatabaseFiles.NameFor(simulator: false)` (`database_service.dart:11`) |
| Simulador (Editor o build de desarrollo) | `rower_app_dev.db` | `DatabaseFiles.NameFor(true)` |

`AppRoot` llama `DatabaseFiles.NameFor(BuildFlags.IsSimulator)`: `BuildFlags` ya es el `const bool` del `#if ROWMATE_SIMULATOR` (ARCHITECTURE §4.2). No hay otro lugar del código que elija el nombre (lo controla `ItThreeToolingTests.OnlyAppRootChoosesTheDatabaseFile`). Como en el legacy, el Editor sin simulador usa la misma base que el build normal de esa PC (spec §12.6).

### 2.3 Apertura: nunca tocar lo que no se entiende

`RowMateDatabase` abre en el primer uso (la carga del arranque) y conserva la conexión. Si la apertura falla, la próxima operación lo vuelve a intentar desde cero ("Reintentar"). Todo corre en el hilo de la base:

```
Open(path):
 1. Si el archivo NO existe → crear (paso 6).
 2. Encabezado (DatabaseHeader.Read, 100 bytes, E/S común, FileShare.ReadWrite):
      largo 0                                → base vacía: crear (paso 6)
      largo de 1 a 99 (aunque empiece con la firma) → Unreadable   (encabezado incompleto: no se abre con SQLite)
      largo > 0 y no empieza con "SQLite format 3\0" → Unreadable   (no se abre con SQLite)
      user_version del encabezado (offset 60, big-endian) > 8 → TooNew
 3. Sonda de SOLO LECTURA (SQLiteOpenFlags.ReadOnly | FullMutex):
      PRAGMA user_version; SELECT count(*) FROM sqlite_master
      SQLiteException NonDBFile (26) → Unreadable; otra → Io
      Se cierra siempre. Lee lo que haya en el -wal (S3-a: el encabezado decía 8 y el WAL, 9).
 4. Decidir:  8 → abrir (paso 5);  0 sin tablas → crear (paso 6);
              > 8 → TooNew;  0 con tablas, o 1 a 7 → Unrecognized (no hay migraciones: la base nace en v8, DT-09)
 5. Abrir ReadWrite | FullMutex (sin Create); pragmas de §2.1. Listo.
 6. Crear ReadWrite | Create | FullMutex, en modo rollback:
      BEGIN; 5 sentencias de §2.1; PRAGMA user_version = 8; COMMIT   (todo o nada: un corte deja user_version 0 y la
      próxima apertura vuelve a crear)
      después journal_mode = WAL y el resto de los pragmas.
```

- Con `Unreadable`, `TooNew` o `Unrecognized` el archivo solo se abrió en solo lectura (o ni eso): **bytes iguales** al terminar, también los del `-wal` (S3-a, Anexo A.1, casos 5 y 6). La excepción lleva el problema (para el registro) y la pantalla muestra el error de carga con `Reintentar` (AC-HIS-17-05, AC-RUT-09-04).
- **Ninguna ruta en el registro ni en los mensajes** (QA-G3-20, el repo es público y los informes citan el registro): `DatabaseOpenException` lleva el `DatabaseProblem`, el código de SQLite (`SQLite3.Result`) y el **nombre** del archivo; `[DB] …` registra lo mismo. El `Message` de una `SQLiteException` no se registra nunca, porque sqlite-net le pone la ruta completa al fallar la apertura (`Could not open database file: {ruta} (…)`, `SQLite.cs:411` de gilzoide). Lo prueba `DOT.ProblemsNeverCarryTheFolder` (ni la excepción ni el registro contienen la carpeta temporal).
- Una base v8 existente se usa tal cual (AC-HIS-17-04): la apertura y la carga **no escriben** (S3-a: mismos bytes después de abrir en escritura y leer).
- La sonda de solo lectura sobre una base WAL cerrada no crea archivos (S3-a); con un `-wal` presente, puede escribir el `-shm`, que es memoria compartida y no contenido.
- Se crea en modo rollback y después se pasa a WAL para que el `user_version` quede en el archivo principal desde el primer *commit* (S3-a, casos 1 y 3: bytes 18/19 = 2 y offset 60 = 8).

### 2.4 Ubicación por plataforma

| Plataforma | Carpeta | Cómo | Verificación |
|---|---|---|---|
| Windows (build y Editor) | `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate` | `Application.persistentDataPath` (empresa `iguisoft`, producto `RowMate`, AC-APP-16-02) | [Device-Win], `DataLocationTests` |
| Android | almacenamiento **interno** de la app: `getDatabasePath("x").getParentFile()` (`/data/user/0/<paquete>/databases`, el mismo lugar que usa `sqflite` en el legacy); se crea la carpeta si falta | JNI desde `AppDataLocation` | [Device-And] (NO VERIFICABLE sin módulo) |
| macOS / iOS | `Application.persistentDataPath` | — | [Apple] It. 10b |

`Application.persistentDataPath` **no sirve en Android**: Unity lo resuelve con `getExternalFilesDir`, que se ve desde la PC por USB. AC-HIS-17-01 pide el almacenamiento privado. La ruta se resuelve en el hilo principal (JNI) y llega a Infrastructure como texto.

**Pruebas y capturas nunca usan esa carpeta** (AC-HIS-20-04, §12.1 de la spec):

- `UiTestHost` y el arnés no pasan por `AppRoot`: reciben un repositorio de `HostRoutines` sobre un archivo de `TempDatabase` (`<temp del sistema>/RowMate.Tests/<guid>-ñá/`), que se borra al final. `TempDatabase` **se niega** a usar una ruta dentro de `Application.persistentDataPath` o un archivo llamado `rower_app.db` / `rower_app_dev.db` fuera de su carpeta temporal (lanza; el mensaje nombra el archivo, no la carpeta: QA-G3-20). La carpeta temporal de cada prueba lleva `ñ` y `á` en su nombre (`RowMate.Tests/<guid>-ñá/`), así toda la suite prueba rutas con acentos en los dos mundos, como en un perfil de Windows con `ñ` (QA-G3-13; sqlite-net pasa la ruta a UTF-8 antes de `sqlite3_open_v2`).
- Lo único que arma `AppRoot` en pruebas es `BootSceneTests`. `Bootstrap/DataLocation` lee `ROWMATE_DATA_DIR` **solo** con `UNITY_EDITOR || DEVELOPMENT_BUILD`. `BootSceneTests` la pone en su `SetUp` (y la saca en `TearDown`), y `run-unity-tests.ps1` y `capture-snapshots.ps1` la ponen para toda la corrida, por si alguien suma otra prueba de escena. En el build normal el nombre ni existe: lo busca `BuildOutputInspector`.
- QA compara contenido y fecha de las dos bases de la carpeta real antes y después de correr todo, también de CAP-27, la única excepción (§5.5).

### 2.5 Base separada del simulador

Mismo código, mismo esquema y mismas reglas; solo cambia el nombre del archivo (§2.2). Las pantallas no lo saben (AC-HIS-20-03: el único aviso del modo es la cinta). AC-HIS-20-02 en el Editor: `RoutinesPersistenceTests.VariantsDoNotMix` (`RPS.VariantsDoNotMix`, el mismo nombre en §6.3: QA-G3-14) arma dos composiciones sobre la **misma carpeta temporal**, una con `NameFor(false)` y otra con `NameFor(true)`; lo creado en una no aparece en la otra.

### 2.6 Hilos: serializado afuera, resultado en el hilo principal

```csharp
namespace RowMate.Infrastructure.Threading
{
    // Dónde corre el trabajo de base (ADR 005). Devuelve Task<T>: quien hace await desde el hilo principal vuelve al hilo
    // principal por el SynchronizationContext capturado (UnitySynchronizationContext en la app; TestMainThreadContext en
    // dotnet). Nadie en Core, App o Infrastructure usa ConfigureAwait(false) (PureCodeRulesTests).
    public interface IDbExecutor : IDisposable
    {
        Task<T> Run<T>(Func<T> work);
        Task Run(Action work);
    }

    // Un hilo propio (IsBackground) con una cola FIFO (BlockingCollection<Action>): el orden de llegada es el orden de
    // ejecución, así "cargar" y después "crear" nunca se cruzan. Cada trabajo completa un TaskCompletionSource con
    // RunContinuationsAsynchronously: ninguna continuación corre en el hilo de la base.
    // Dispose: no acepta más, termina lo encolado (un guardado en vuelo se completa), cierra la conexión EN ese hilo y
    // espera el hilo con tope de 5 s (si no termina, [DB] en el registro). También se dispone en AppDomain.DomainUnload,
    // para que una recarga de dominio del Editor no deje el hilo vivo; Dispose se desuscribe de DomainUnload (QA-G3-09) y
    // es idempotente. Un Run después de Dispose devuelve una tarea fallada con ObjectDisposedException (no lanza en línea).
    public sealed class SerialExecutor : IDbExecutor
    {
        public SerialExecutor(string threadName, IAppLogger logger);
        internal SerialExecutor(string threadName, IAppLogger logger, IUnloadHook unloadHook);   // para SET (suscribir/desuscribir)
        public int ThreadId { get; }
    }

    // Interno: Subscribe(EventHandler) / Unsubscribe(EventHandler). El de producción usa AppDomain.CurrentDomain.DomainUnload;
    // SET pasa uno que cuenta las suscripciones.
    internal interface IUnloadHook { void Subscribe(EventHandler handler); void Unsubscribe(EventHandler handler); }
}
```

- **App:** `AppRoot` crea un `SerialExecutor("RowMate.Db")` y lo pasa a `RowMateDatabase`. La carga, el guardado y la baja corren ahí; la pantalla sigue animando (AC-HIS-17-06). Los objetos que vuelven (`Routine`, `IntervalStep`) son inmutables y de Core: se arman en el hilo de la base sin tocar nada de Unity.
- **Pruebas de pantalla y arnés:** `InlineDbExecutor` (Tests/Support) ejecuta en el hilo que llama sobre SQLite real: las tareas terminan en el acto y cada cuadro es determinista. Solo `RoutinesPerformanceTests` usa el `SerialExecutor` real.
- **dotnet:** los tests de repositorio usan `InlineDbExecutor`. `SerialExecutorTests` corre adentro de `TestMainThreadContext.Run(async () => …)` y comprueba orden, que el trabajo corre en otro hilo, que la continuación vuelve al del test, que una excepción viaja en la tarea, que `Dispose` termina lo encolado y que se desuscribe del *hook* de descarga (`SET.DisposeUnsubscribesTheUnloadHook`).
- **Registro desde el hilo de la base** (QA-G3-09): `SET` y `PRF` registran desde ese hilo, así que `RecordingLogger` pasa a ser seguro entre hilos (`lock`, copias al leer; `RLG.ConcurrentLogsAreAllKept` con 4 hilos × 1 000 líneas). `UnityAppLogger` y el `HostLogger` de `UiTestHost` ya lo son (`Debug.Log*` acepta cualquier hilo).
- **Continuaciones que llegan tarde** (QA-G3-09): `RoutinesStore`, `RoutineEditorStore`, las páginas, los diálogos y las capas tienen un `_disposed`. Una continuación que vuelve después de `Dispose` (el editor cerrado con "Atrás" durante un guardado lento, la app que se cierra con una carga en vuelo) no cambia estado, no emite y no toca la UI. La única excepción es el aviso de §3.3 (paso 6), que sale por `ToastLayer` si la capa sigue viva; `ToastLayer.Show` después de su propio `Dispose` no hace nada.
- **Nivel de las fallas esperadas** (QA-G3-09): las de cargar, guardar y borrar se registran como `Warning` (`[DB] carga de rutinas: …`, `[DB] guardado de rutina: …`, `[DB] baja de rutina: …`, con el problema y el nombre del archivo). `HostLogger` convierte un `Error` en una prueba fallida (`Tests/UiSupport/UiTestHost.cs`, `Debug.LogError`): un `Error` queda solo para lo que no se espera (un bug).
- **Cierre de la app:** `AppRoot.OnDestroy` dispone en este orden: shell → app (stores) → *backend* → **repositorio y ejecutor** → superficie. Un guardado que ya cerró el editor ya hizo *commit* (el editor espera el *commit*): matar el proceso 1 s después no lo pierde (AC-RUT-08-07, [Device-Win]).

### 2.7 Cómo se prueba en dotnet con SQLite real

- `tools/dotnet/RowMate.Infrastructure.csproj` suma `sqlite-net-pcl` 1.9.172 y `SQLitePCLRaw.bundle_green` **2.1.11** fijado: con la dependencia por defecto (2.1.2) dotnet corre SQLite 3.39.2; con 2.1.11, **3.49.1**, más cerca del 3.53.4 de Unity (S3-a). Los binarios `e_sqlite3` vienen para Windows y Linux (`ci-core` corre en Ubuntu). La primera restauración necesita red (como NUnit).
- **Las mismas fuentes corren en Unity**: `Tests/EditMode` también lo compila Unity, así que los tests de repositorio corren además contra el binario de gilzoide (3.53.4) en `run-unity-tests.ps1` EditMode (AC-BASE-08).
- `TempDatabase` crea una carpeta temporal por test y la borra en `Dispose`; `RoutineSeeds` arma F-VACIA, F-LISTA, F-BORDE, cada F-RARA y F-ROTA (§6.1).
- **Falla a la mitad (AC-RUT-08-07)**: el test agrega a su base un *trigger* `BEFORE INSERT ON interval_steps` que hace `RAISE(ABORT, 'falla inyectada')` desde el segundo paso de la rutina; el guardado lanza `SQLiteException` (`Constraint`) y la base queda como antes (S3-a, caso 7). Sin costuras en el código de producción.
- **Leer la base desde un test** (QA-G3-07): DSC, DOT y RRT leen `sqlite_master`, `PRAGMA table_info`, `foreign_key_list`, `index_list`, `user_version`, `journal_mode`, `synchronous`, `foreign_keys` y `sqlite_version()` con `RowMateDatabase.QueryRawForTestsAsync` (§3.1), sin nombrar ningún tipo de sqlite-net. Así los mismos archivos compilan en `RowMate.Tests.EditMode` de Unity, que no referencia el paquete. Antes de entregar WP-A, se compila `RowMateDatabase.cs` también contra el `SQLite.cs` de gilzoide en un proyecto descartable de `<scratch>` (como S3-a): la API de bajo nivel tiene los mismos nombres en las dos librerías, pero sus *handles* son de tipos distintos.

---

## 3. Lógica pura: repositorio, stores y reglas del editor (WP-A)

Todo compila en `netstandard2.1`, C# 9, sin `UnityEngine`. Clases `sealed`, nulabilidad habilitada, `IAppLogger` en todo observable (ADR 004). Sin `DateTime.Now`: el reloj es `IClock`.

### 3.1 Repositorio

```csharp
namespace RowMate.Core.Ports
{
    // Persistencia de rutinas (F-RUT-08, F-HIS-18). Todas las operaciones son atómicas; las de escritura devuelven la rutina
    // tal como quedó guardada (ids, routineId y step_order 0…n−1).
    public interface IRoutineRepository
    {
        Task<IReadOnlyList<Routine>> GetAllAsync();         // created_at DESC, id DESC; pasos por step_order
        Task<Routine?> GetByIdAsync(long id);               // null si no existe (o se borró)
        Task<Routine> InsertAsync(Routine routine);         // created_at de la rutina; Id ignorado
        Task<Routine> UpdateAsync(Routine routine);         // nombre y descripción; reemplaza TODOS los pasos; no toca created_at
        Task DeleteAsync(long id);                          // pasos y rutina
    }
}
```

| Operación | SQL (en una transacción `RunInTransaction`) | Legacy |
|---|---|---|
| `GetAllAsync` | `SELECT id, name, description, created_at FROM routines ORDER BY created_at DESC, id DESC`; `SELECT * FROM interval_steps ORDER BY routine_id, step_order, id`; agrupa por `routine_id` | `database_service.dart:143-152`, `:190-199` |
| `GetByIdAsync` | las mismas dos con `WHERE id = ?` / `WHERE routine_id = ?` | `:206-212` |
| `InsertAsync` | `INSERT INTO routines (name, description, created_at)`; `SELECT last_insert_rowid()`; por cada paso `INSERT INTO interval_steps (…14 columnas…)` con `step_order = i` | `:154-163` |
| `UpdateAsync` | `UPDATE routines SET name = ?, description = ? WHERE id = ?` (si cambia 0 filas: `RoutineNotFoundException` y *rollback*); `DELETE FROM interval_steps WHERE routine_id = ?`; reinserta con `step_order = i` | `:165-181` (el legacy reescribe `created_at` con el mismo valor; acá no se escribe) |
| `DeleteAsync` | `DELETE FROM interval_steps WHERE routine_id = ?`; `DELETE FROM routines WHERE id = ?` | `:183-186` + Q-17 |

- **Lectura.** `RoutineRowMapper` convierte cada fila: `type` con `StepTypeNames.Parse` (desconocido → `FormatException`, `interval_step.dart:133`); enteros leídos como `long?` y pasados a `int?` con `checked` (DT-10); `created_at` con `IsoDate.Parse`. Cualquier falla de una fila hace fallar **toda** la carga (P-07) y no escribe nada (AC-RUT-08-08).
- **Escritura.** Los 14 atributos de AC-RUT-02-01 van como parámetros (null = NULL; 0 = 0). La rutina devuelta trae los ids nuevos.
- `last_insert_rowid()` se lee con `ExecuteScalar<long>` (no `SQLite3.LastInsertRowid(Handle)`, cuyo tipo de *handle* cambia entre las dos librerías).

```csharp
namespace RowMate.Infrastructure.Routines
{
    public sealed class SqliteRoutineRepository : IRoutineRepository, IDisposable
    {
        public SqliteRoutineRepository(RowMateDatabase database, IAppLogger logger);   // dispone la base (y su ejecutor)
    }
}

namespace RowMate.Infrastructure.Database
{
    public sealed class RowMateDatabase : IDisposable
    {
        public RowMateDatabase(string path, IDbExecutor executor, IAppLogger logger);
        public string Path { get; }                                     // nunca va al registro: solo System.IO.Path.GetFileName(Path)
        internal Task<T> RunAsync<T>(Func<SQLiteConnection, T> work);      // abre (§2.3) si hace falta, en el hilo de la base
        internal Task ExecuteRawForTestsAsync(string sql, params object?[] args);   // semillas F-RARA (InternalsVisibleTo)

        // Solo para tests (QA-G3-07): filas de cualquier consulta, sobre la MISMA conexión de escritura (abre si hace falta).
        // Argumentos: null, long/int, double o string. Cada celda vuelve como long, double, string, byte[] o null según el tipo
        // de almacenamiento de SQLite (sqlite3_column_type): ningún tipo de sqlite-net sale de Infrastructure.
        internal Task<IReadOnlyList<object?[]>> QueryRawForTestsAsync(string sql, params object?[] args);
    }
}
```

`QueryRawForTestsAsync` usa la API de bajo nivel de sqlite-net (`SQLite3.Prepare2`, `BindNull`/`BindInt64`/`BindDouble`/`BindText`, `Step`, `ColumnCount`, `ColumnType`, `ColumnInt64`, `ColumnDouble`, `ColumnString`, `ColumnByteArray`, `Finalize` en `finally`). Los trece métodos existen, públicos y estáticos, en las dos librerías (gilzoide `SQLite.cs:4802-4944`; `sqlite-net-pcl` 1.9.172, por reflexión). El *handle* es `IntPtr` en gilzoide y `sqlite3_stmt` en `sqlite-net-pcl` (alias de `SQLite.cs:48-55`), así que el código lo guarda en `var` y nunca nombra su tipo.

### 3.2 `RoutinesStore` (puerto de `routines_provider.dart`)

```csharp
namespace RowMate.App.Routines
{
    public enum RoutinesPhase { Loading, Ready, LoadFailed }

    // Estado de la colección (F-RUT-09). Routines siempre en el orden de la lista (la más nueva arriba).
    public sealed record RoutinesState(RoutinesPhase Phase, IReadOnlyList<Routine> Routines);

    public sealed class RoutinesStore : IDisposable
    {
        public RoutinesStore(IRoutineRepository repository, IClock creationClock, IAppLogger logger);
        public IReadOnlyObservable<RoutinesState> State { get; }       // arranca en Loading (= _loading del constructor, :12-14)
        public Task LoadAsync();                  // :19-25 + Q-13. Una carga en curso no se duplica (Reintentar mientras carga = nada)
        public Task<Routine?> CreateAsync(string name, string description, IReadOnlyList<IntervalStep> steps);  // :27-38; null = falló
        public Task<Routine?> UpdateAsync(Routine routine);           // :40-46; null = falló
        public Task<bool> DeleteAsync(long id);                       // :48-52; false = falló
        public Routine? Find(long id);
    }
}
```

| Regla | Fuente |
|---|---|
| `AppContext.Start()` llama `LoadAsync()` sin esperar (`Forget(logger)`), aunque nadie vaya a Rutinas; cambiar de pestaña no vuelve a cargar | `main.dart:83`, `routines_provider.dart:12-14`; AC-RUT-09-01, AC-APP-01-09 |
| `Loading → Ready` con la lista, o `→ LoadFailed` (lista vacía) con `[DB] carga de rutinas: <motivo>` como `Warning` | Q-13, AC-RUT-09-04 |
| Crear: `createdAt = truncar_ms(creationClock.UtcNow)`, **una lectura por llamada**; al volver, la rutina va **primera** | `:28-35`; Q-18 |
| Guardar o borrar que fallan: `[DB] guardado de rutina: …` / `[DB] baja de rutina: …` como `Warning` (QA-G3-09) | AC-RUT-09-05, -06 |
| Después de `Dispose`, una carga o una escritura que vuelve no cambia el estado ni emite | QA-G3-09 |
| Editar: reemplaza en **su lugar** (mismo índice) | `:41-43` |
| Borrar: la quita al terminar el borrado; si falla, sigue | `:49-50`; AC-RUT-09-06 |
| Una escritura que sale bien con la fase en `LoadFailed` dispara `LoadAsync()` (si la base volvió, aparece la lista) | (agregado: el legacy no tiene error de carga) |
| El orden de las operaciones es el de llegada (el ejecutor es FIFO): un alta tocada mientras carga aparece arriba de lo cargado | `:35` |

`CreationClock` es el *scheduler* en la app; las pruebas pasan `SequenceClock` (13:00:00.000Z en la primera lectura y un minuto más en cada lectura siguiente). Como **solo** lo lee `RoutinesStore.CreateAsync`, una vez por alta, "un minuto más por lectura" es "un minuto más por cada rutina creada" (spec §0.4; un alta que falla y se reintenta toma el minuto siguiente). Los ids de serie no lo leen: `SeriesIdGenerator` usa el `Scheduler` (§3.6, QA-G3-19), así dos series en el mismo milisegundo se pueden armar en PlayMode (AC-RUT-08-06).

### 3.3 `RoutineEditorStore` (estado del editor)

```csharp
namespace RowMate.App.Routines
{
    // Un paso de la plantilla con una clave estable mientras dura el editor (para animar altas y bajas; no se guarda).
    public readonly struct EditorStep { public int Key { get; } public IntervalStep Step { get; } }

    public sealed record RoutineEditorState(IReadOnlyList<EditorStep> Steps, IReadOnlyList<EditorItem> Layout,
                                            int StepCount, bool IsSaving);

    public enum SaveOutcome { Saved, Ignored, NameEmpty, NoSteps, StepOutOfRange, Failed }

    public sealed class RoutineEditorStore : IDisposable
    {
        public RoutineEditorStore(Routine? existing, RoutinesStore routines, IClock seriesClock, IAppLogger logger);  // :17-31
                                                                  // seriesClock = Scheduler (ids g{ms}_{n}), nunca CreationClock
        public bool IsNew { get; }
        public bool IsClosed { get; }                             // true desde Saved (o Dispose): nada más cambia (QA-G3-01)
        public string Name { get; set; }                          // texto del campo, sin recortar (lo escribe la vista)
        public string Description { get; set; }
        public IReadOnlyObservable<RoutineEditorState> State { get; }

        public StepDialogForm NewStepForm();                      // + Paso: sin progresiones (:75-88)
        public StepDialogForm NewSeriesStepForm(string groupId);  // + Agregar paso a serie: con progresiones y el N de la serie
        public StepDialogForm EditStepForm(int index);            // lápiz: progresiones si está en serie (:90-97)
        public void AddLooseStep(IntervalStep fromDialog);        // al final, suelto (:80-86)
        public void AddSeries();                                  // Trabajo 120 s + Descanso 60 s, ×3, id nuevo (:110-133)
        public void AddStepToSeries(string groupId, IntervalStep fromDialog);   // después del último de la serie, con su N (:135-149)
        public void ReplaceStep(int index, IntervalStep fromDialog);            // en su lugar, conserva serie y N (:98-106)
        public void DeleteStep(int index);                        // :190-194
        public void DeleteSeries(string groupId);                 // :196-200
        public int RepeatCountOf(string groupId);                 // ?? 1 (:153)
        public RepeatChange ChangeRepeatCount(string groupId, string text);     // :151-188 + Q-92 (§3.5)
        public Task<SaveOutcome> SaveAsync();                     // :40-73 + Q-13, Q-19, Q-92
    }
}
```

`SaveAsync`, en orden (AC-RUT-11-05 a -07, AC-RUT-09-03, -05):

1. `IsSaving` **o `IsClosed`** → `Ignored` (Q-19: los toques y el Enter que llegan mientras guarda no hacen nada; AC-RUT-09-03: tampoco los que llegan después de un `Saved`, mientras el editor sale).
2. `DartText.Trim(Name)` vacío → `NameEmpty` (aunque tampoco haya pasos).
3. Sin pasos → `NoSteps`.
4. `StepLimitCheck.TemplateIsWithinLimits(steps)` falso → `StepOutOfRange` (solo con una base tocada a mano).
5. `IsSaving = true`; pasos renumerados (`copyWith(order: i, routineId: existing?.id ?? 0)`, `:56-60`); nombre y descripción recortados; `CreateAsync` o `UpdateAsync(existing with { Name, Description, Steps })` (id y `CreatedAt` se conservan).
6. Al volver: si el *store* ya se dispuso (el editor se cerró con "Atrás" durante un guardado lento), no toca su estado y solo devuelve el resultado. La página, ya cerrada, no hace `Pop`; si el resultado es `Failed`, igual muestra `editorSaveError` por `ToastLayer`, porque la lista quedó a la vista (QA-G3-09). Con el *store* vivo: `null` → `IsSaving = false`, `Failed` (el editor sigue abierto con todo, AC-RUT-09-05); si no, `Saved`, **`IsClosed = true`** (todo `SaveAsync` y toda acción posterior no hacen nada) y la vista hace `Pop` **una vez**.

"Atrás" durante un guardado en curso cierra el editor sin preguntar (AC-RUT-11-08), y el guardado ya pedido termina igual. La rutina aparece en la lista porque `RoutinesStore` sigue vivo: es la paridad con `if (mounted) Navigator.of(context).pop()` después del `await` (`routine_editor_screen.dart:63-72`). Solo pasa con una base lenta (lo prueba `RED.BackDuringASlowSaveClosesAndTheSaveLands`).

Sin unicidad ni largo máximo (AC-RUT-11-07). Las reglas de la plantilla son de Core (§3.6), así que el *store* solo las llama y reemite el estado.

### 3.4 Diálogo de paso: `StepDialogForm` y topes de Q-92

```csharp
namespace RowMate.App.Routines
{
    public enum StepField { Duration, Meters, WattsMin, WattsMax, Spm, Split, ProgressionSpm, ProgressionWatts, ProgressionSplit }

    // Duration = el par Min/Seg; Split = el par Split min/Split seg (se marcan juntos, AC-RUT-13-11).
    public readonly struct FieldError
    {
        public StepField Field { get; }
        public int? Repetition { get; }     // null: editorErrorRange; desde 1: editorErrorRangeRep (AC-RUT-13-12)
        public string MaxText { get; }      // "9999", "99:59:59", "99:59"…
    }

    public sealed class StepDialogResult { public IntervalStep? Step { get; } public IReadOnlyList<FieldError> Errors { get; } }

    public sealed class StepDialogForm
    {
        public static StepDialogForm ForNew(bool inSeries, int seriesRepeatCount);                  // Trabajo, Por tiempo, vacíos (:484-485)
        public static StepDialogForm ForEdit(IntervalStep existing, bool inSeries, int seriesRepeatCount);   // precarga (:498-521)
        public bool IsNew { get; }
        public bool ShowsProgressions { get; }       // = inSeries (:688)
        public StepType Type { get; set; }
        public bool ByDistance { get; set; }
        public string Minutes { get; set; }  public string Seconds { get; set; }  public string Meters { get; set; }
        public string WattsMin { get; set; } public string WattsMax { get; set; } public string Spm { get; set; }
        public string SplitMin { get; set; } public string SplitSec { get; set; }
        public string ProgressionSpm { get; set; } public string ProgressionWatts { get; set; } public string ProgressionSplit { get; set; }
        public IntervalStep Preview();               // _build() sin topes; lo que no entra en int se ve vacío (AC-RUT-13-16)
        public StepDialogResult Confirm();           // _build() + Q-92: el paso, o los errores por campo
    }
}
```

**Precarga** (`:498-521`): modo distancia si hay distancia; duración y split partidos con `DartMath.TruncDiv` y `DartMath.Mod` (resto nunca negativo: −90 → `-1` y `30`); sin ceros a la izquierda; progresiones aunque el paso sea suelto (no se ven, pero vuelven al confirmar: AC-RUT-12-03).

**`_build()`** (`:531-562`), con `DartIntParser` sobre `DartText.Trim(texto)`:

| Campo | Regla | Tope (valor absoluto) | `{max}` |
|---|---|---|---|
| Por tiempo | `(Min ?? 0) × 60 + (Seg ?? 0)` siempre, distancia null (`:536-540`) | 359 999 s | `99:59:59` |
| Por distancia | `Metros` o null (sin meta); duración null (`:534-535`) | 999 999 | `999999` |
| `W mín`, `W máx` | cada uno o null | 9 999 | `9999` |
| `SPM objetivo` | o null | 999 | `999` |
| Split | `(min ?? 0) × 60 + (seg ?? 0)`; ≤ 0 → null (`:557-562`) | 5 999 s, **solo si da más de 0** | `99:59` |
| Progresiones SPM / W / split | o null | 999 / 9 999 / 5 999 | `999` / `9999` / `5999` |

- Las cuentas se hacen con `BigInteger`: un texto de 20 cifras o `Min` = `99999999999999999999` da "fuera de rango" y nunca desborda (spec §12.3). Lo que está dentro del tope es idéntico al legacy (siempre entra en 64 bits).
- **"≤ 0 = sin split" gana sobre el tope** (AC-RUT-13-08, QA-G3-13 a): `-200` y vacío (−12 000), `-99999999999999999999` y `0`, o `0` y `-6000` dan "sin split" y **ningún** error, como el legacy (`_buildSplitSeconds`: `total > 0 ? total : null`, `:557-562`). Solo un split positivo se compara con 5 999.
- **Solo cuenta el modo elegido** (AC-RUT-13-04, -11; QA-G3-13 d): en modo distancia, `Min` y `Seg` no se leen ni se controlan (`Min` `6000` oculto no da error); en modo tiempo, `Metros` tampoco (`_build()` lee solo el modo elegido, `:534-540`).
- **Progresiones de un paso suelto** (AC-RUT-12-03, -13-11, -11-05; QA-G3-13 c): se precargan y vuelven al confirmar aunque no se vean (`:517-519`, `:551-553`), pero **no se controlan**, ni en `Confirm()` ni al guardar (`StepLimitCheck` con `inSeries: false` las saltea). Un paso suelto de `F-RARA` con progresión SPM 5 000 se confirma y se guarda tal cual: el diálogo nunca marca un campo que no se ve.
- **Por repetición** (AC-RUT-13-12), solo con `ShowsProgressions` y N ≥ 1 (N = el de la serie): para cada objetivo con progresión, `StepLimits.FirstRepetitionOutOfRange(objetivo, progresión, N, tope)` da la primera r (desde 1) con `|objetivo + progresión × (r − 1)| > tope`, **en forma cerrada** (es lineal en r): con ×2 000 000 000 no hay bucle. W mín y W máx comparten la progresión de vatios: se informa la primera repetición entre los dos. El error va debajo de la **progresión**. Una progresión sin su objetivo no se controla. Un paso suelto no se progresa.
- Primero se controlan los campos y después las repeticiones (una progresión fuera de su propio tope da su error de campo). Con errores, `Step` es null y el diálogo sigue abierto; la vista borra el error de un campo en cuanto ese campo cambia.

`StepLimits` (Core):

```csharp
public static class StepLimits
{
    public const int MaxDurationSeconds = 359_999, MaxDistanceMeters = 999_999, MaxWatts = 9_999, MaxSpm = 999,
                     MaxSplitSeconds = 5_999, MaxProgressionSpm = 999, MaxProgressionWatts = 9_999,
                     MaxProgressionSplitSeconds = 5_999, MaxRepeatCount = 999;
    public const string DurationMaxText = "99:59:59", SplitMaxText = "99:59";
    public static int? FirstRepetitionOutOfRange(long target, long progression, long repeatCount, long max);  // sin bucles
}

public static class StepLimitCheck
{
    public static bool StepIsWithinLimits(IntervalStep step, int repeatCount, bool inSeries);
    // Validación 3 de AC-RUT-11-05: cada paso dentro de sus topes; en una serie con N ≥ 1, además cada repetición;
    // N > 999 cuenta como fuera de rango. N ≤ 0 (×0 o negativo, F-RARA) no se controla (DT-13). Un split ≤ 0 es "sin split"
    // (no se controla); las progresiones de un paso suelto no se controlan (inSeries: false).
    public static bool TemplateIsWithinLimits(IReadOnlyList<IntervalStep> template);
}
```

### 3.5 Repeticiones (`ChangeRepeatCount`)

| Texto (recortado con `DartText.Trim`) | Resultado | Fuente |
|---|---|---|
| no numérico, vacío, `5.0`, `1e3`, `−2` (U+2212) | ×1 en todos los pasos de la serie | `:173` (`?? 1`) |
| ≤ 0 (`0`, `-2`, uno negativo de cualquier largo: `-99999999999999999999`) | sin cambio (AC-RUT-14-05; en el legacy el de 20 cifras desborda, `int.tryParse` da `null` y vale ×1: DV-17) | `:179` (`result > 0`) |
| 1 a 999 y todos los objetivos progresados con el N nuevo dentro de su tope | ×N en todos los pasos de la serie | `:180-186` |
| 1 a 999, pero algún objetivo progresado se pasa | diálogo abierto, `editorRepetitionsOutOfRange{count}` | AC-RUT-14-06 |
| > 999 (de cualquier largo) | diálogo abierto, `editorRepetitionsMax{max = 999}` | AC-RUT-14-05, -06 |

`RepeatChange { Kind: Applied | Unchanged | TooMany | TargetOutOfRange; int Count }`. Bajar N siempre se acepta. `Escalera SPM` (18, +2): ×491 se acepta, ×492 no (forma cerrada, §3.4).

### 3.6 Reglas de la plantilla (Core, puras)

```csharp
namespace RowMate.Core.Routines
{
    public static class RoutineTemplate
    {
        public const int DefaultSeriesWorkSeconds = 120, DefaultSeriesRestSeconds = 60, DefaultSeriesRepeatCount = 3;   // :115-130
        public static IReadOnlyList<IntervalStep> AppendLoose(IReadOnlyList<IntervalStep> t, IntervalStep s);          // order = t.Count
        public static IReadOnlyList<IntervalStep> AppendDefaultSeries(IReadOnlyList<IntervalStep> t, string groupId);
        public static IReadOnlyList<IntervalStep> InsertIntoSeries(IReadOnlyList<IntervalStep> t, string groupId, IntervalStep s);
                                                  // lastIndexWhere(gid) + 1, con repeat del primero de gid (:144-147)
        public static IReadOnlyList<IntervalStep> Replace(IReadOnlyList<IntervalStep> t, int index, IntervalStep s);
                                                  // s.CopyWith(order: index, groupId: original, groupRepeatCount: original)
        public static IReadOnlyList<IntervalStep> RemoveAt(IReadOnlyList<IntervalStep> t, int index);
        public static IReadOnlyList<IntervalStep> RemoveSeries(IReadOnlyList<IntervalStep> t, string groupId);   // removeWhere
        public static IReadOnlyList<IntervalStep> SetRepeatCount(IReadOnlyList<IntervalStep> t, string groupId, int n);
        public static int RepeatCountOf(IReadOnlyList<IntervalStep> t, string groupId);    // firstWhere(gid).groupRepeatCount ?? 1
        public static IReadOnlyList<IntervalStep> Renumber(IReadOnlyList<IntervalStep> t, long routineId);
    }

    public enum EditorItemKind { LooseStep, SeriesHeader, SeriesStep, SeriesFooter }
    public readonly struct EditorItem
    {
        public EditorItemKind Kind { get; } public int StepIndex { get; } public string? GroupId { get; }
        public int SeriesOrdinal { get; } public int RepeatCount { get; }      // ordinal 0…; repeat del primero con ese gid
    }
    public static class EditorLayout { public static IReadOnlyList<EditorItem> Build(IReadOnlyList<IntervalStep> t); }  // :275-295

    public sealed class SeriesIdGenerator      // :111; contador por editor; si el id ya existe en la plantilla, sigue contando
    {
        public SeriesIdGenerator(IClock clock);                       // el Scheduler (nunca CreationClock, QA-G3-19)
        public string Next(IReadOnlyList<IntervalStep> template);     // "g" + UtcNow.ToUnixTimeMilliseconds() + "_" + n
    }

    public readonly struct StructureSegments
    {
        public long Total { get; }                         // tramos de la rutina expandida, con saturación (nunca desborda)
        public IReadOnlyList<StepType> Leading { get; }    // los primeros hasta `max`, en orden
    }
    public static class RoutineStructure { public static StructureSegments Segments(IReadOnlyList<IntervalStep> t, int max); }

    // Geometría de la mini-barra (DN3-07; 04-design-notes §4.3). Pura: StructureBarView la llama en cada
    // GeometryChangedEvent con el ancho resuelto de la columna (antes del primer layout, W = 0: no dibuja nada).
    public readonly struct StructureBarGeometry
    {
        public int Capacity { get; }          // ⌊(W + 2) / 4⌋ (tramo mínimo 2 + separación 2); 64 en TV (W = 255)
        public int Drawn { get; }             // min(Total, Capacity); 0 = pista vacía de todo el ancho
        public double SegmentWidth { get; }   // min(24; (W − 2·(n − 1)) / n), con mínimo 2; a 2 si hay desborde
        public bool Overflow { get; }         // Total > Capacity: los últimos 6 dibujados se apagan (alfa 6/7 … 1/7)
        public bool EmptyTrack { get; }       // Total = 0 (solo series ×0): pista border.default
    }
    public static class StructureBarLayout { public static StructureBarGeometry For(long total, double widthDp); }
}

namespace RowMate.Core.Design
{
    // Obstáculos inferiores en dp, medidos desde abajo (DN3-13, DN3-14).
    public readonly struct BottomObstacle { public double Left { get; } public double Right { get; } public double Bottom { get; } public double Height { get; } }

    public static class BottomObstacles
    {
        // Distancia desde abajo del rectángulo [left, right] × [bottom, bottom + height]: sube hasta `gap` sobre cada
        // obstáculo que se cruza con él agrandado `gap` dp, y repite hasta no cruzar ninguno.
        public static double Lift(double left, double right, double bottom, double height, IReadOnlyList<BottomObstacle> obstacles, double gap);
        // Relleno inferior de un contenido desplazable de ancho [left, right]: tope del obstáculo más alto que se cruza + gap.
        public static double ClearanceAbove(double left, double right, IReadOnlyList<BottomObstacle> obstacles, double gap, double minimum);
    }
}
```

- `StructureBarLayout` con los anchos de 04-design-notes §3 y §4.3 (W = 255 en TV): 1 tramo → 24; 7 y 8 → 24 (8 × 24 + 14 = 206); 10 → 23,7; 14 → 16,36; 64 → 2,02 sin desborde; 65 o ×2 000 000 000 → 64 a 2 con desborde; 0 → pista vacía. `SBL.WidthCapacityOverflowAndEmptyTrack`.
- `BottomObstacles` con las posiciones de 04-design-notes §4.12 y §4.13 (barra 64, aviso de 48): aviso en el editor (TV) a 16 con o sin la llave; en Rutinas (TV) a 144 por el `+` y a 200 con la llave encima; en W1280, a 80 (el `+` no se cruza); llave a barra + 88 sobre el `+` y a barra + 16 sin obstáculos. `BOT.PositionsOfTheDesignNotes`.

- Las operaciones de serie van **por `groupId`**, como el legacy (`firstWhere`, `lastIndexWhere`, `removeWhere`). Con ids únicos (los que da el editor) es lo mismo que "por bloque". Con el mismo id en dos bloques separados (solo en una base tocada a mano) cambian juntos, igual que en el legacy (DT-14).
- `RoutineStructure` nunca expande: cuenta por bloques con `repeat ≤ 0 → 0` y saturación, y arma como mucho `max` tramos. `Escalera SPM` ×3 → 8, ×4 → 10; `Pirámide 500` → 14; `Solo calentamiento` → 1; `Intervalos 4×` → 7. Con ×2 000 000 000, `Total` ≈ 4·10⁹ y `Leading` corta en `max`. La forma de la franja con valores imposibles ya está decidida (desborde que se apaga y pista vacía: `StructureBarLayout`, DN3-07).
- `StepSubtitle.For(step, l10n)`: `Text` = `StepLabels.EditorSubtitle` (igual al legacy, con `📈`); `Before`, `HasTrend` y `After` separan el `📈` (sale de `stepTargetProgressions` = `📈 {list}`) para dibujarlo como `trending_up`. `After` conserva el espacio que sigue al emoji. Así los tests comparan `Text` al carácter (AC-RUT-12-06) y la vista dibuja el ícono (DV-10).

### 3.7 Composición

```csharp
public sealed record AppServices(DeviceBackend Devices, IRoutineRepository Routines, IScheduler Scheduler, IAppLogger Logger,
                                 LocalizationSet Texts, ISystemLocale Locale, IAccessibilitySettings Accessibility, ShellOptions Shell)
{
    public IClock? CreationClock { get; init; }     // null = Scheduler
}

public sealed class AppContext : IDisposable
{
    public RoutinesStore Routines { get; }                            // nuevo (con CreationClock ?? Scheduler)
    public RoutineEditorStore CreateEditor(Routine? existing);        // con el Scheduler para los ids de serie; lo dispone la página
    public void Start();                                              // + Routines.LoadAsync().Forget(Logger)
    // Dispose: también Routines. El repositorio lo dispone quien lo creó (AppRoot, UiTestHost), como el DeviceBackend.
}
```

`AppRoot.Awake` (paso nuevo entre 4 y 5):

```csharp
// 4b. Base local (F-HIS-17, F-HIS-20): una por variante, en la carpeta privada de la app (§2.4).
var dbPath = Path.Combine(DataLocation.DatabaseDirectory(_platform), DatabaseFiles.NameFor(BuildFlags.IsSimulator));
_routines = new SqliteRoutineRepository(new RowMateDatabase(dbPath, new SerialExecutor("RowMate.Db", logger), logger), logger);
```

---

## 4. UI (WP-B, UI Toolkit)

Valen las reglas de la It. 2 (02-tech-design §3): vistas en C# sin UXML, nombres y clases de contrato, nada de LINQ ni *closures* nuevas en caminos por cuadro, binding manual (`ViewBinding.Bind`), colores solo de `Tokens`.

### 4.1 Capas del shell

```
#app-root .rm-app …
├── #tab-content       capa 0   pestañas (TabHost); Rutinas = RoutinesScreenView
├── #tab-bar           capa 1
├── #page-stack        capa 2   .rm-page-stack > .rm-page-stack__dim (oscurecido de lo de atrás) + páginas (.rm-page)
├── #dialog-layer      capa 3   .rm-dialog-host por diálogo: .rm-dialog__scrim + .rm-dialog
├── #toast-layer       capa 4   #toast .rm-toast
├── #popup-layer       capa 5   NUEVA: .rm-popup-catcher + .rm-menu (⋮ y menú de texto) · .rm-tooltip
└── #simulator-layer   capa 6   último hijo (solo con el define)
```

- `#popup-layer` va arriba de diálogos y avisos y abajo del simulador: el menú del clic derecho de un campo **dentro** de un diálogo queda encima del diálogo, y todo queda debajo de la cinta (AC-APP-03-04, -07). `GenericDropdownMenu` no sirve: S3-b mostró que se cuelga de la raíz del panel, por encima de `#app-root` (DT-15).
- Las capas tienen `pickingMode = Ignore`. Reciben eventos el velo, el atrapador de toques del menú, las páginas, y la llave y el panel del simulador; la cinta sigue con `Ignore`. La ayuda emergente (`.rm-tooltip`) y **todo su árbol** también van con `Ignore`: nunca tapa un toque (QA-G3-01). En UI Toolkit `pickingMode` no se hereda; por eso la cinta lo pone también en su `Label` (`SimulatorPresentation/SimulatorRibbon.cs:15`, `:18`).
- `AppShellView` crea los objetos de las capas **antes** que las pantallas (para que `RoutinesScreenView` las reciba por `UiContext`) y los agrega al árbol en el orden de arriba.

### 4.2 `PageStack` y "Atrás"

```csharp
namespace RowMate.Presentation.Shell
{
    public interface IPage
    {
        VisualElement Root { get; }
        void OnPushed();                // terminó la entrada
        void OnPopped();                // salió; se dispone acá (desuscribe y dispone su store)
        void OnBackRequested();         // "Atrás" del sistema con la página arriba: el editor hace Pop sin preguntar (AC-RUT-11-08)
    }

    public sealed class PageStack : VisualElement
    {
        public IPage? Top { get; }      // la de arriba que NO está saliendo (la que sale ya no cuenta para "Atrás" ni para Push)
        public bool HasPages { get; }
        public void Push(IPage page);   // ignora un segundo Push mientras dura la entrada de otro (doble toque en "+")
        public void Pop(IPage page);    // solo si es la de arriba; un segundo Pop de la misma página no hace nada
        public bool IsExiting(IPage page);
        public event Action<bool>? CoversShellChanged;
    }

    // Lo que sale no responde (QA-G3-01; Flutter: routes.dart:1153-1156, :1215-1223). Una sola vez, al empezar la salida:
    // cada elemento del árbol pasa a pickingMode = Ignore y focusable = false (pickingMode no se hereda en UI Toolkit), y
    // si el foco estaba adentro, vuelve a quien lo tenía antes de abrir (si sigue en el árbol y es enfocable) o se quita.
    // No se deshace: lo que sale se dispone al terminar. Costo: un recorrido del árbol (≈ 1 100 elementos con 60 filas).
    public static class ExitLock { public static void Apply(VisualElement root, Focusable? restoreFocusTo); }

    public enum BackSource { SystemBack, Escape }

    public sealed class BackNavigation
    {
        public BackNavigation(IBackButtonPolicy policy, PopupLayer popups, DialogLayer dialogs, PageStack pages);
        // Lo que ya está saliendo se saltea en cada paso (como en Flutter, donde la ruta que sale ya no es la actual).
        // 1. menú o ayuda abiertos → se cierran (= tocar afuera)
        // 2. diálogo abierto → se cancela
        // 3. Escape: nada más (P-08: no cierra pantallas)
        // 4. página arriba → OnBackRequested()
        // 5. raíz → policy.OnBackAtRoot() (Android: al fondo o salir; escritorio: nada)
        // Una pulsación cierra un solo nivel: Escape llega por un único camino (AppRoot.Update → ShellHandle.HandleBack;
        // ningún control registra NavigationCancelEvent, §4.10).
        public void HandleBack(BackSource source);
    }
}
```

- **Cubre la barra** (AC-APP-03-01): la página ocupa todo el panel, con fondo `bg.app`, y recibe los eventos. Cuando termina la entrada, `#tab-content` y `#tab-bar` pasan a `display: none` (`CoversShellChanged(true)`): no se dibujan, no se tocan y Tab no llega a ellos. Al empezar la salida vuelven a `flex`. `display: none` conserva el desplazamiento de un `ScrollView` (medido en la It. 2), así que la lista vuelve donde estaba (AC-RUT-10-09).
- **Al salir, la página no responde** (AC-APP-03-01, AC-RUT-09-03): `Pop` aplica `ExitLock` a la página en el cuadro 0 de su salida, vuelve `#tab-content` y `#tab-bar` a `flex` (`CoversShellChanged(false)`) y anima. Un toque que cae sobre la página que sale pasa a lo de atrás, como en Flutter: en el lugar de `Guardar` o de la flecha está la barra de Rutinas, que no tiene acciones. El teclado tampoco llega, porque el foco salió de la página. `RED.TapDuringTheExitCreatesNothing` (dos toques en `Guardar` con 50 ms de diferencia: una rutina, una salida) y `RED.EnterDuringTheExitDoesNothing`.
- **`BackSource`:** `AppRoot` manda `SystemBack` en Android (la tecla Atrás llega como Escape del Input System) y `Escape` en el resto. `UiTestHost.PressBack(BackSource)` prueba los dos. "Atrás" durante la salida de una página o de un diálogo actúa sobre el nivel siguiente (`NAV.BackSkipsWhatIsClosing`).
- **ARCHITECTURE §8 y ADR 007** dicen que Escape también cierra páginas: se alinean con P-08 al cerrar (DT-04).

### 4.3 Rutinas (lista)

```
#screen-routines .rm-screen .rm-routines                           (RoutinesScreenView)
├── #routines-appbar .rm-appbar > .rm-appbar__title                 routinesTitle, sin acciones
├── #routines-body .rm-routines__body                               flex-grow 1, position relative; los estados, centrados entre las dos barras
│   ├── #routines-loading .rm-routines__center > .rm-spinner        solo el indicador de 32 (Phase = Loading), tras 120 ms de gracia
│   ├── #routines-error .rm-routines__center                        Phase = LoadFailed
│   │   ├── #routines-error-icon .rm-icon  error 48 (DN3-15)
│   │   ├── #routines-error-text  routinesLoadError
│   │   └── #routines-retry .rm-button --tonal                      retry → RoutinesStore.LoadAsync()
│   ├── #routines-empty .rm-empty                                   Ready y sin rutinas
│   │   ├── .rm-empty__icon .rm-icon   fitness_center 56, opacidad 0,12
│   │   ├── .rm-empty__title           routinesEmpty   (text.secondary)
│   │   └── .rm-empty__hint            routinesEmptyHint (13, text.tertiary)
│   └── #routines-list (ScrollView, ScrollPolicy) > .rm-routines__list          Ready y con rutinas
│       └── #routine-card-<n> .rm-routine-card (focusable)          n = posición en la lista (0 = la más nueva)
│           ├── .rm-button__state, .rm-highlight                   capas absolutas, pickingMode Ignore (DN3-06)
│           ├── .rm-routine-card__icon .rm-icon   caja 36 accent.tonalBg, fitness_center 22 accent.primary
│           ├── .rm-routine-card__text
│           │   ├── .rm-routine-card__name          nombre (seminegrita, ajusta; texto del usuario tal cual)
│           │   ├── .rm-routine-card__description   solo si no está vacía (ajusta, respeta saltos de línea; tal cual)
│           │   ├── .rm-routine-card__summary       RoutineSummary.Text
│           │   └── .rm-structure-bar               StructureBarView (un elemento)
│           ├── #routine-menu-<n> .rm-icon-button   more_vert; ayuda menuShow; margin-top −14
│           └── .rm-focus-ring
└── #routines-add .rm-fab                                           add; right 16, bottom = alto real de la barra + 16
```

- Las tres vistas de estado se alternan con `display` (nunca un indicador eterno: Q-13). El `+` se ve en los tres estados.
- **Indicador** (AC-RUT-10-02, DN3-15): `Spinner` de 32 dp con trazo 3; si la carga sigue a los 120 ms (`MotionSpecs.LoadingGraceMs`, por el *scheduler*), entra con fundido de 160. Una carga más corta no lo muestra nunca. `RLT.LoadingShowsOnlyTheIndicator` mira a los 100 ms (cuerpo vacío) y a los 300 ms (indicador entero); CAP-02 se toma pasados los 300 ms.
- **Texto del usuario tal cual** (AC-RUT-10-03, -07; DN3-22, QA-G3-10): el nombre, la descripción y el contenido del diálogo de eliminar van con `enableRichText = false` y `parseEscapeSequences = false`, y con `white-space: pre-wrap` (respeta dos espacios y los saltos de línea, y ajusta). `<b>A</b>` no se pone en negrita y `Serie\n2` escrito a mano (la barra y la `n`) no salta de línea. Una palabra de 200 caracteres sin espacios se parte donde haga falta: lo confirma S-B3a y, si TextCore no la parte, la vista inserta U+200B cada 16 caracteres en los tramos sin espacios más largos que eso, **solo en lo que dibuja** (el dato no cambia y un nombre común no se toca). `RLT.UserTextIsShownAsWritten`, `RLT.LongWordWithoutSpacesWraps`.
- `RoutineListView` hace *diff* por `Routine.Id`: una tarjeta nueva crece y entra (alto **y margen** con `spring.smooth`), una borrada se desvanece y colapsa alto y margen (VID-02), una editada cambia sus textos en el lugar y se ilumina (`.rm-highlight` 0,08 → 0). La primera carga escalona 35 ms por tarjeta, hasta 8, **solo si la pestaña está a la vista**: si la lista llega antes de mostrarla (lo normal, AC-APP-01-09), entra con la transición de la pestaña (04-design-notes §5.5, DN3-16). El `name` `routine-card-<n>` se renumera después de cada cambio.
- **Tarjeta accionable con un botón adentro:** la tarjeta es un `VisualElement` con `Clickable` y `NavigationSubmitEvent`, no un `Button`. El `⋮` frena `PointerDown`/`Click`/`NavigationSubmit` (`StopPropagation`): tocarlo abre solo el menú (`RoutinesScreenTests.MenuButtonDoesNotOpenTheEditor`).
- **Menú `⋮`** (`PopupMenu` en `#popup-layer`): `#routine-menu-edit` (`routineEdit`) y `#routine-menu-delete` (`routineDelete`, `state.dangerText`), debajo del botón con 4 dp de aire y su borde derecho alineado con el del botón (arriba si no entra; a ≥ 8 dp de los bordes, 04-design-notes §4.4). Tocar afuera (`.rm-popup-catcher`), Escape o "Atrás" lo cierran. `Editar` = tocar la tarjeta; `Eliminar` = `DeleteRoutineDialog` (§4.5). El borrado no espera dentro del diálogo (`routines_screen.dart:115`); si falla, aviso `routineDeleteError`. Al elegir, el menú sale (90 ms) con `ExitLock` y su atrapador se traga los toques hasta irse: un segundo toque en `Eliminar` no abre otro diálogo ni llega al velo del que entra (AC-APP-03-04; DT-21; `NAV.MenuItemTwiceOpensOneDialog`).
- **Desplazamiento:** *padding* inferior dentro del contenido = tope del obstáculo inferior más alto que se cruza con la lista + 16 (`BottomObstacles.ClearanceAbove`): en TV, barra + 88 = 152 con el `+`; con la llave en Rutinas, barra + 144 = 208 (DN3-14). Así la última tarjeta termina por encima del `+` y de la llave (AC-RUT-10-09). Sin barra fija en teléfono (`ScrollPolicy`, AC-APP-05-11).
- **`StructureBarView`**: un solo elemento con `generateVisualContent` + `Painter2D`: tramos de alto 4, radio 2 y separación 2, alineados a la izquierda, en el color **base** de su tipo (ART_BIBLE §3.7), colores con `MeshColor` (tubería `GammaRt`). En cada `GeometryChangedEvent` toma el ancho resuelto W de la columna, pide `RoutineStructure.Segments(t, ⌊(W + 2) / 4⌋)` y con su `Total` calcula `StructureBarLayout.For(Total, W)` (§3.6); la malla se rearma solo si cambian la rutina o el ancho. Antes del primer *layout* no dibuja nada: los tests esperan el *layout* antes de leer. Nunca sale de un renglón. Con desborde, los primeros `Capacity` tramos a 2 dp y los últimos 6 que se apagan; con 0, la pista vacía (`border.default`) de todo el ancho (DN3-07, AC-RUT-10-11). Expone `SegmentCount` (dibujados), `SegmentTypes`, `SegmentWidth` y `Overflow`.

### 4.4 Editor de rutina (página)

```
#routine-editor .rm-page .rm-editor                                 (RoutineEditorPage : IPage)
├── #editor-appbar .rm-appbar
│   ├── #editor-back .rm-icon-button        arrow_back; ayuda navBack (en parte bajo la cinta: la cinta no recibe toques)
│   ├── .rm-appbar__title                   editorNewRoutine | editorEditRoutine
│   └── .rm-appbar__actions > #editor-save .rm-button --text        save
└── #editor-scroll (ScrollView, ScrollPolicy, FocusScroller) > .rm-editor__content   (DV-18)
    ├── .rm-editor__fields                                          márgenes 16
    │   ├── #editor-name         RmTextField de una línea           editorRoutineName
    │   └── #editor-description  RmTextField multilínea, 2 líneas   editorDescription
    ├── .rm-divider
    ├── .rm-editor__steps-header
    │   ├── #editor-steps-count              editorStepsCount{count} (.rm-type-title-xs)
    │   ├── #editor-add-step .rm-button --text     add + editorStep
    │   └── #editor-add-series .rm-button --text   repeat + editorSeries
    ├── #editor-empty .rm-editor__empty      editorEmptySteps (dos líneas, centrado en lo que queda, text.tertiary)
    └── #editor-steps .rm-editor__steps      un hijo por ítem de EditorLayout (EditorStepsView)
        ├── #step-row-<i> .rm-step-row .rm-step-row--<tipo>                       paso suelto; i = índice en la plantilla
        └── #series-<k> .rm-series                                                k = ordinal de la serie; 8 arriba y 8 abajo
            ├── #series-header-<k> .rm-series__header                             min-height 48; borde izq. 3 brand.seed
            │   ├── .rm-icon (repeat 16) + .rm-series__title (editorSeries, .rm-type-header-s)
            │   ├── #series-repeat-<k> .rm-chip --repeat    caja táctil ≥ 48 × 48, transparente (DN3-04)
            │   │   └── .rm-chip__pill                      cápsula 28: editorRepeatCount{count} en accent.onTonal, capa y anillo
            │   └── #series-delete-<k> .rm-icon-button --danger    delete 20; ayuda editorDeleteSeries
            ├── .rm-series__steps                           borde izq. 3 brand.seed (riel continuo), fondo seed @6 % (DN3-05)
            │   └── #step-row-<i> .rm-step-row .rm-step-row--<tipo> .rm-step-row--in-series     margin-left 13 (sangría visible 16)
            ├── #series-footer-<k> .rm-series__footer > #series-add-step-<k> .rm-button --text   add 16 + editorAddStepToSeries
            └── .rm-series__stack (absoluta bajo el pie, pickingMode Ignore) > 2 .rm-series__stack-layer   hojas: ×2 una, ×3+ dos

#step-row-<i>
├── .rm-highlight                                     capa absoluta, pickingMode Ignore (resaltado al editar)
├── .rm-step-row__badge > .rm-step-row__badge-bg (color base del tipo @20 %, por código) + .rm-icon (ícono 16 en step.<tipo>.hud)
├── .rm-step-row__text > .rm-step-row__title (nombre del tipo, step.<tipo>.hud) + .rm-step-row__subtitle (StepSubtitleView)
├── #step-edit-<i>   .rm-icon-button            edit 20, text.secondary
└── #step-delete-<i> .rm-icon-button --danger   close 20
```

- **Un solo `ScrollView` para campos, fila de pasos y lista** (DV-18, ex DT-01, aceptada por el PO). En el legacy los campos quedan fijos y solo la lista se desplaza; a 640 × 360 con letra 1,3× o con el teclado abierto, la parte fija no entra. Con un solo desplazamiento nada desborda y `KeyboardAvoider` puede llevar el campo con foco a la vista.
- `EditorStepsView` reusa los elementos por `EditorStep.Key` (filas) y por `groupId` (bloques), con los tiempos de 04-design-notes §5.6: lo nuevo crece (alto **y márgenes** con `spring.smooth`) y funde; lo borrado se desvanece (0-120) y colapsa (80-280); el bloque de serie se estira con el mismo resorte y, al nacer, sus piezas funden en escalera; lo editado se ilumina (`.rm-highlight`). Después de agregar un paso o una serie, el contenido se desplaza hasta mostrarlo con 16 dp de aire (`FocusScroller`, ω 12; DN3-18). Los `name` de contrato se renumeran después de cada cambio.
- Las acciones abren los diálogos de §4.5 y llaman al *store*; `Guardar` hace `SaveAsync()`: `Saved` → `Pop`; `NameEmpty`, `NoSteps`, `StepOutOfRange` y `Failed` → aviso (`editorNameEmpty`, `editorNoSteps`, `editorStepOutOfRange`, `editorSaveError`); `Ignored` → nada. P2 (DN3-19, si entra en B2): si el guardado tarda más de 300 ms, `Guardar` muestra un *spinner* de 16 con el mismo ancho.
- **Subtítulo con ícono (DV-10)**, `StepSubtitleView`. Camino principal: un solo `Label` con `Before + U+E8E5 + After`, con el glifo envuelto en texto enriquecido **generado por código** desde `Tokens` (`<color=…metric.distance…><size=117%>` + U+E8E5 + `</size></color>`, 04-design-notes §4.6; sin literales de color en el código). El subtítulo no lleva texto del usuario, así que es la única etiqueta con `enableRichText = true`; `parseEscapeSequences = false` y `white-space: pre-wrap` (los dos espacios del separador). El glifo sale de los respaldos de fuente de la copia de `PanelTextSettings` que arma `RuntimePanelFactory` (abajo). Así el ícono queda en la línea y el texto ajusta como cualquier párrafo. Respaldo si la sonda S-B3a falla (tamaño, línea de base o color del glifo): fila `flex-wrap` con tres piezas (antes, `IconView` 14, después). En los dos casos `StepSubtitleView.Text` es el texto del legacy con `📈` (lo que comparan los tests).
- **Una sola fábrica para el panel** (QA-G3-12): `RuntimePanelFactory.Create(PanelSettings source, FontAsset iconsOutline, FontAsset? emoji, string name)` (Presentation no conoce `UiAssets`, que es de Bootstrap: cada anfitrión le pasa los campos de su `UiAssets`) instancia la copia del `PanelSettings` (el asset no se toca, R2-05) y le asigna una copia de `PanelTextSettings` con `fallbackFontAssets` en el orden de 04-design-notes §4.15: `RowMateIcons-Outline-SDF` y después Noto Emoji. Inter va primero por la `-unity-font-definition` de cada clase. La llaman `AppRoot.Awake` (paso 6, antes del `OnEnable` del `UIDocument`: QA-G2-01), `UiTestHost.Create` (con el host inactivo, antes de asignar el panel) y el arnés, que usa `UiTestHost`. `UiTestHost` y el arnés leen el **mismo** `UiAssets.asset` con `UiAssetsLocator.Assets()`. Sin la copia, PlayMode y las capturas dibujarían `□` en lugar de `trending_up`. Lo prueban `RPF.TheThreeHostsShareTheFallbacks` (Unity EditMode) y la sonda de tendencia de CAP-10.
- Colores con transparencia por código desde `Tokens` (insignia y mosaico elegido `tipo @20 %`, encabezado `brand.seed @12 %`, cuerpo de la serie y pie `@6 %`, hojas `@20 %` / `@12 %`, cápsula del chip `accent.primary @20 %`, resaltado `text.primary` 0,08 → 0; 04-design-notes §2.4): USS no puede aplicar transparencia a un `var()`. Es contrato, como el borde del tablero de la It. 2.

### 4.5 Diálogos

`ModalDialog<T>` (base, `DialogLayer`): título `.rm-dialog__title`, contenido `.rm-dialog__content` (un `ScrollView` con `ScrollPolicy` y `FocusScroller`) y acciones `.rm-dialog__actions` abajo a la derecha (secundaria de texto, principal rellena). Geometría de 04-design-notes §3 y §4.9 (DN3-10): ancho `min(máx, ventana − 80)` con mínimo 280 y **máximo por diálogo**: 400 en eliminar y repeticiones, 560 en el de paso (TV: 331; L640 y W1280: 400 / 560). Alto máximo `ventana − 48 − área segura`; título y acciones **fuera** del desplazamiento (AC-RUT-13-15). `.rm-dialog--compact` con la ventana de menos de 480 dp de alto (rellenos de 16); `.rm-dialog__content--scrolls` mientras el contenido desborda. Devuelve `Task<T?>`; cerrar sin resultado = `default`. La continuación corre en el acto, en el hilo principal (determinista en pruebas).

| Diálogo | Nombres | Resultado | Fuente |
|---|---|---|---|
| `DeleteRoutineDialog` | `#dialog-cancel` (`cancel`, texto), `#dialog-confirm` (`delete`, `.rm-button--danger-fill`) | `bool` | `routines_screen.dart:97-115` |
| `StepDialog` | `#step-type-{warmup,work,rest,cooldown}` (`SegmentedControl`, DV-09), `#step-mode-time`, `#step-mode-distance`, `#step-min`, `#step-sec`, `#step-meters`, `#step-wmin`, `#step-wmax`, `#step-spm`, `#step-split-min`, `#step-split-sec`, `#step-prog-spm`, `#step-prog-watts`, `#step-prog-split`; errores `#step-<campo>-error` (pares: `#step-time-error`, `#step-split-error`); `#step-preview` (`StepRowView` sin acciones, `.rm-step-row--preview`); `#dialog-cancel`, `#dialog-confirm` | `IntervalStep` | `routine_editor_screen.dart:564-738` |
| `RepeatDialog` | `#repeat-amount` (con foco al abrir), `#repeat-error`, `#dialog-cancel`, `#dialog-confirm` (`ok`) | `int` vía `RepeatChange` | `:151-177` |

- Cada campo escribe en `StepDialogForm`; la vista previa se rearma con `Preview()` en cada cambio (AC-RUT-13-16) y `Confirmar` llama a `Confirm()`. Con errores, el diálogo queda abierto, cada error se borra al cambiar su campo, y el foco va al **primer campo con error** con el contenido desplazado hasta mostrarlo con su texto y 16 dp de aire (AC-RUT-13-11, DN3-10). En `RepeatDialog`, `OK` llama `ChangeRepeatCount`: `TooMany` y `TargetOutOfRange` dejan el diálogo abierto con su texto.
- **Vista previa del paso** (DN3-10): fija entre el título y el contenido (`.rm-dialog__pinned`) si el alto máximo del diálogo es ≥ 560 (TV, W1280); si no (L640, TH), primer elemento del contenido. **Tipo** en mosaicos (`.rm-segmented--tiles`, DN3-08): 2 × 2 si el contenido mide menos de 400 dp (TV) y 4 en fila desde 400.
- Velo `.rm-dialog__scrim` (`overlay.scrim`, opacidad 0 → 0,6 por código): tocarlo = cancelar. Escape y "Atrás" = cancelar. Lo de atrás no responde (el velo tapa) y `FocusTrap` mantiene Tab y Mayús+Tab dentro del diálogo (`NavigationMoveEvent` `Next`/`Previous` → foco al siguiente enfocable del diálogo en orden de árbol + `focusController.IgnoreEvent(evt)`, API confirmada en S3-b). En un campo, Enter no confirma (paridad; §4.10).
- **Al cerrarse, el diálogo no responde** (AC-APP-03-03, QA-G3-01): el primer cierre gana (`Close` es idempotente) y, en ese cuadro, `ExitLock` vuelve inerte el árbol del diálogo, `FocusTrap` se suelta y el foco vuelve al control que lo tenía al abrir (como Flutter al sacar una ruta). El velo sigue ahí durante los 150 ms de la salida y se **traga** los toques sin cancelar nada: un segundo toque en `Confirmar`, `OK` o `Eliminar` no hace nada y tampoco llega al editor de atrás, donde podría caer sobre una cruz o un tacho. En Flutter, en cambio, el velo deja pasar ese toque (`routes.dart:2278-2281`): es DT-21, para el PO. `NAV.DialogButtonTwiceActsOnce` (paso, repeticiones y eliminar: una sola acción cada uno).
- Foco inicial: solo `RepeatDialog` lo pone (en `#repeat-amount`; en escritorio con todo seleccionado, en el teléfono con el cursor al final, AC-RUT-14-04). Los demás diálogos lo dejan en el primer control al navegar con Tab, como Flutter.

### 4.6 Menús y ayudas (`PopupLayer`)

- `PopupMenu` (⋮ y menú de texto): elementos `Button` `.rm-menu__item` (y `--danger`), anclados al botón o al puntero, dentro de la ventana (posiciones de 04-design-notes §4.4: el `⋮`, debajo con 4 dp y borde derecho alineado; el de texto, en el puntero + (2; 2); los dos a ≥ 8 dp de los bordes); `.rm-popup-catcher` a pantalla completa debajo del menú cierra al tocar. El menú de texto lleva `.rm-menu--dense`; la capa de estado del ítem de peligro usa `--rm-overlay-scrim` (WP-D le pone 12 / 24 %); el anillo de foco del ítem va adentro (DN3-11). Foco al primer ítem si se abrió con teclado; flechas y Tab dentro del menú (`FocusTrap`).
- **Al cerrarse, el menú no responde** (AC-APP-03-04, QA-G3-01): al elegir, tocar afuera, Escape o "Atrás", el menú aplica `ExitLock` en el cuadro 0 de su salida (90 ms) y su atrapador se traga los toques hasta que el menú sale del árbol (DT-21). El foco vuelve al ancla.
- `Tooltip` (ayudas de `editor-back`, `routine-menu-*` y `series-delete-*`; AC-APP-03-06): en escritorio aparece en el `PointerEnterEvent` y se va 0,1 s después del `PointerLeaveEvent`. En el teléfono, al mantener presionado 0,5 s se muestra y se va 1,5 s después de soltar; ese toque no activa el botón. Una ayuda por vez, en `#popup-layer`, centrada 8 dp debajo del ancla (arriba si no entra), a ≥ 8 dp de los bordes y con ancho máximo 240 (DN3-12). **Todo su árbol con `pickingMode = Ignore`**: nunca recibe ni tapa un toque (QA-G3-01). El texto se guarda además como `AccessibleName` del elemento; que lo lea un lector de pantalla queda NO VERIFICABLE y pasa a la It. 14 (DT-05, decidido por el PO).

### 4.7 Avisos (`ToastLayer`)

`Show(string text)`: si hay uno visible, **reemplaza** su texto y reinicia sus 4 s (`IScheduler`, DV-14), con el empujón de escala 1 → 1,02 → 1 (y el cruce de texto si cambia); si no, entra (sube 16 dp + fundido en 220) y sale a los 4 s (160, bajando 8). `#toast .rm-toast > .rm-toast__text` (ajusta, nunca "…"). **Ancho** (04-design-notes §4.12): con la ventana de menos de 600 dp, de 16 a 16; si no, `clamp(texto + 32; 344; 560)`, centrado. **Posición** (DN3-13): la base es el alto de la barra visible (o el área segura) + 16; si su rectángulo **se cruza** con un obstáculo inferior (`+`, llave; §4.8) agrandado 8 dp, sube hasta 8 dp sobre ese obstáculo y repite (`BottomObstacles.Lift`, gap 8). No cuenta un obstáculo que solo cae en su ancho: en el editor el aviso queda abajo (16 dp) y la llave, en 80-120, no lo toca. Si un obstáculo cambia mientras se ve (la llave que sube), lo sigue con `CriticalSpring` ω 22. Así ni la llave lo tapa ni él tapa la llave (AC-APP-03-05). Después de `Dispose`, `Show` no hace nada.

### 4.8 Capa del simulador encima de todo y la llave sobre el `+` (DV-13)

- Sigue siendo el último hijo de `#app-root` (It. 2 §3.5): queda arriba de la pila, los diálogos con su velo, los avisos y los menús, sin código extra (AC-APP-03-07, AC-BLE-16-01). La cinta es `pickingMode = Ignore`: tocar la flecha de volver bajo la cinta vuelve (`UiTestHost.Tap` con *hit-test* lo prueba). Con un diálogo abierto, la llave está por encima del velo: abre el panel y el panel funciona (el velo no recibe ese toque).
- **Alto de la barra conservado:** `ShellLayout.TabBarHeight` guarda el último alto visible. Con el editor abierto (barra en `display: none`) la llave no salta a 16 del borde.
- **Obstáculos inferiores:** `ShellLayout.SetBottomObstacle(owner, Rect?)`. `RoutinesScreenView` registra su `+` **mientras Rutinas es la pestaña elegida y ninguna página la tapa**: lo quita con `CoversShellChanged(true)` (al terminar la entrada del editor) y lo vuelve a poner con `CoversShellChanged(false)` (al empezar su salida). `SimulatorLayerView` registra la llave. `SimulatorPanelLayout.FabBottom(barra, obstáculos)` (pura, dotnet, con `BottomObstacles` gap 16) da la posición de la llave: barra + 16 si no choca; en Rutinas, 16 sobre el `+`, o sea **barra + 88**, con los bordes derechos alineados (x fija, `right 16`), en cualquier estado de la pantalla (cargando, vacío, lista o error).
- **Decidido** (DV-13, DN3-14; AC-BLE-16-02 con el texto nuevo de spec §5, QA-G3-05): la llave pasa de un lugar al otro solo en y, con `CriticalSpring` ω 22 desde su posición y velocidad actuales (en el acto con "reducir movimiento"), al cambiar de pestaña, al terminar la entrada del editor y al empezar su salida, y se puede tocar mientras se mueve. El panel se abre **siempre en el mismo lugar**, en todas las pestañas: `SimulatorPanelLayout.PanelBottom(barra)` = barra + 16, con el alto de AC-BLE-16-03 (sin cambios). En Rutinas el panel abierto tapa el `+` (como el legacy) y, mientras está abierto, la llave no se ve. Pruebas: `SPL.FabBottomAvoidsTheFab`, `SPL.PanelBottomNeverMoves`; `SimulatorOverlayTests.WrenchSitsAboveTheFabInRoutines`, `.WrenchStaysOnOtherTabsAndWithTheEditor`, `.WrenchMovesWithoutJumpsAndStaysTappable`, `.PanelOpensInTheSamePlaceInRoutines`, `.ReducedMotionMovesTheWrenchAtOnce`.
- `ToastLayer` y el *padding* inferior del contenido desplazable de cada pantalla y página cuentan los obstáculos que **se cruzan** con ellos (`BottomObstacles`; DN3-13, DN3-14).

### 4.9 Movimiento

Mandan los tiempos y curvas de 04-design-notes §5 (tabla §5.0 y §5.1-§5.10). Resumen de lo que implementa WP-B:

| Qué | Implementación (por código, `MotionDriver`) | Reducir movimiento |
|---|---|---|
| Push / pop del editor | `PageTransition`: entra x +24 → 0 en 0-240 (`ease.enter`) y opacidad 0 → 1 en **0-160**; lo de atrás x 0 → −24 y `.rm-page-stack__dim` 0 → 0,08. Pop: x 0 → +24 en 0-200 (`ease.exit`), opacidad 1 → 0 en 40-200; lo de atrás vuelve en 0-200 (`ease.enter`) | fundido de 120 |
| Diálogo | `DialogTransition`: velo 0 → 0,6 en 0-200 (`ease.move`); diálogo escala 0,96 → 1 y fundido en 0-220 (`ease.enter`); salida a escala **0,98** y opacidad 0 en 150 (`ease.exit`); el resultado se aplica en el cuadro 0 de la salida | solo opacidad |
| Aviso | `ToastTransition`: sube 16 + fundido 220; sale 160 bajando 8; reemplazo con empujón 1,02 (0-90-240) y cruce de texto (0-80 / 80-200) | solo opacidad, sin empujón |
| Menú y ayuda | menú: escala 0,96 → 1 y fundido en 120 (origen en la esquina del ancla); ayuda: fundido 120; salida 90 | solo opacidad |
| Tarjetas, filas y bloques | `GrowMotion` (alto **y márgenes** 0 → N con `CriticalSpring` ω `spring.smooth`, fundido en 60-260) y `CollapseMotion` (fundido 0-120; alto y márgenes → 0 en 80-280); serie nueva con sus piezas en escalera de 40; resaltado `.rm-highlight` 0,08 → 0 en 600 | fundido sin cambio de alto; el resaltado igual |
| Colores de tipo (mosaico, vista previa, fila) | cruce en 160 (`ease.move`) | igual |
| Etiqueta del campo y línea de error | 160 (`ease.move`) | en el acto |
| Hojas de la serie | alto 0 ↔ 3 y opacidad en 160 | en el acto |
| Llave que se aparta del `+`, aviso que sigue a un obstáculo | `CriticalSpring` ω 22 en y | en el acto |
| Desplazar hasta lo nuevo / el foco, el error o el teclado | `CriticalSpring` ω 12 / ω 22 sobre `scrollOffset` (`FocusScroller`) | en el acto |
| Indicador de carga | gracia 120, fundido 160; vuelta en 900 | igual (indica espera) |
| Chip `×N` (EXR §10.2) | el número cambia con `TabularNumber` + `DigitRoll` (180) y el ancho de la cápsula interpola en los mismos 180 | fundido de 80 |

**Dueños** (DN3-16, corregido por QA-G3-02): las constantes de 04-design-notes §5.11 van a `MotionSpecs` (WP-A, dotnet: `MotionSpecsTests` con los valores); `GrowMotion`, `CollapseMotion` (que animan alto **y** márgenes) y la regla "la primera carga escalona solo con la pestaña a la vista" son de WP-B (`Presentation/Motion/*`, `RoutineListView`). Nada se anima antes del primer cuadro (It. 2) y ninguna propiedad animada por código tiene transición de USS. Mientras un campo de texto tiene foco, `RmTextField` pide 60 fps a `FramePacingPolicy`: escribir no queda a 30 fps (DT-17). P2 (DN3-20, si entra después de B3): el alto que pasa de una a dos líneas y la mini-barra que interpola sus tramos.

### 4.10 Texto y teclado

`RmTextField : VisualElement` envuelve un `TextField` de Unity:

```
#<name> .rm-field [--focused] [--filled] [--error] [--multiline] [--floating]
├── .rm-field__box                        borde (por USS; foco y error por clase); min-height 52 (72 la descripción), nunca height fijo
│   ├── TextField .rm-field__input         sin etiqueta propia de Unity; relleno superior calculado con las cajas resueltas (letra 1,3×)
│   └── .rm-field__label                   adentro de la caja; .rm-type-body con escala 0,8 por código al flotar (DN3-09)
└── .rm-field__error                       texto de error (state.dangerText); alto y opacidad por código; display none sin error
```

`--floating` = con foco, con texto **o con ayuda** (los tres campos de progresión tienen la etiqueta siempre arriba y la ayuda visible con el campo vacío: DV-19, ex DT-02). Sin `.rm-focus-ring` en los campos: el borde de 2 dp con el acento es el indicador. En un error de par (`#step-time-error`, `#step-split-error`) las **dos** cajas pasan a `--error` y hay un solo texto debajo del par.

| Ajuste | Una línea (nombre, números, cantidad) | Varias líneas (descripción) | Motivo |
|---|---|---|---|
| `multiline` | false | true, alto de 2 líneas, desplazamiento interno (`verticalScrollerVisibility` según `ScrollPolicy`) | AC-RUT-11-02 |
| `textSelection.selectAllOnFocus` | **true en escritorio** (Tab o foco por código seleccionan todo; S3-b: `Focus()` dejó 0…12); en el teléfono **false** y, con foco al abrir un diálogo, el cursor al final (AC-RUT-14-04) | **false** | AC-RUT-11-09 |
| `textSelection.selectAllOnMouseUp` | **false** (un clic pone el cursor donde se hizo clic) | false | S3-b: los dos vienen en true por defecto, también en multilínea |
| Saltos de línea | se quitan al cambiar el valor, como `FilteringTextInputFormatter.singleLineFormatter` (`deny('\n')`, `<flutter-sdk>/packages/flutter/lib/src/services/text_formatter.dart:425`, aplicado en `widgets/editable_text.dart:969`); acá también `\r`, que el portapapeles de Windows trae con `\n` | Enter agrega `\n` | AC-RUT-11-02 |
| `textEdition.hideMobileInput` | true | true | AC-RUT-11-10: sin la caja extra del sistema |
| `textEdition.keyboardType` | números: `NumbersAndPunctuation` (con signo, DV-15); nombre: `Default` | `Default` | AC-RUT-13-13 |
| `autoCorrection` | false | false | |
| Ayuda (`Ej: 2`…) | `textEdition.placeholder`, visible con el campo vacío aunque no tenga foco; color en `.unity-base-text-field__input--placeholder` | — | DV-19 (ex DT-02) |
| Texto que se muestra | `enableRichText = false` y `parseEscapeSequences = false` en el `TextElement` interno: `<b>A</b>` y `Serie\n2` (barra y `n`) se ven tal cual | ídem | AC-RUT-10-03, QA-G3-10 |
| Transiciones del tema por defecto | `transition-duration: 0s` en `.rm-field` y en las clases internas que anima `unity-theme://default` (importado en `RowMateTheme.tss`): lo animado es nuestro y va por código | ídem | determinismo de CAP-17, CAP-26, DEMO-02/-05 |

**Lo que `RmTextField` fija del `TextField` de runtime** (QA-G3-10; AC-APP-03-02, AC-APP-05-07, AC-RUT-11-02). Flutter lo resuelve con los atajos de la app: Escape → `DismissIntent`, Tab → `NextFocusIntent` (`<flutter-sdk>/packages/flutter/lib/src/widgets/app.dart:1271-1276`); con el menú de texto visible, Escape solo lo cierra (`editable_text.dart:5642-5648`); Enter en una línea termina la edición y quita el foco (`:3736-3753`, `:3914-3947`). S-B3a (§7.0) mide qué hace UI Toolkit en cada caso; la respuesta del diseño ya está fijada:

| Caso | Lo que pide la spec | Cómo lo logra `RmTextField` (según lo que mida S-B3a) |
|---|---|---|
| Tab en la descripción (multilínea) | pasa al control siguiente, nunca escribe `\t` | si el `TextField` inserta la tabulación: frena en *TrickleDown* el `KeyDownEvent` de Tab (y el de carácter `\t`). Si la navegación de UI Toolkit ya mueve el foco, no hace nada más; si no, lo mueve al siguiente enfocable con la regla de `FocusTrap` (Mayús+Tab, al anterior). Nunca se mueve dos veces |
| Escape con foco en un campo | el texto no se revierte y el campo sigue con el foco; dentro de un diálogo, el diálogo se cancela (como `DismissIntent` en Flutter) | frena en *TrickleDown* el `KeyDownEvent` de Escape (y el `NavigationCancelEvent`, si el campo lo usa) para que el `TextField` no revierta ni suelte el foco. El cierre de menú o diálogo llega por `AppRoot.Update` → `HandleBack(Escape)`, que no depende de eventos de UI Toolkit |
| Escape manejado una sola vez | una pulsación cierra un nivel (con el menú de texto abierto dentro de un diálogo, primero el menú) | ningún control propio registra `NavigationCancelEvent`; el único camino es `HandleBack` (§4.2). `NAV.EscapeClosesOneLevelAtATime` |
| Enter en un campo de una línea | quita el foco; no guarda ni confirma | frena `KeyDownEvent` Return/KeypadEnter y hace `Blur()`; el `NavigationSubmitEvent` del campo no llega al diálogo ni a la página |
| Enter en la descripción | agrega un salto de línea | lo hace el `TextField` multilínea; se frena la propagación |
| Cursor y tema por defecto en batchmode | tomas deterministas | si el cursor parpadea con el reloj real, la toma lo oculta con una clase solo del arnés (`--unity-cursor-color` transparente) y la nota de CAP-17 lo dice; las transiciones del tema, en 0 s (arriba) |

- **Ningún `\t` ni `\r` en una línea**: el filtro que quita `\n` al cambiar el valor también quita `\t` y `\r` (el pegado no debe meter lo que el teclado no deja escribir).
- **Atajos del teclado** (selección con mouse y Mayús + flechas, doble clic, Ctrl+C/X/V/A, teclas muertas y AltGr): los da el `TextField` de UI Toolkit con el portapapeles del sistema. Se verifican en [Device-Win]; en PlayMode, lo que se puede simular (selección y Ctrl+A con `KeyDownEvent`). Ningún test automatizado toca el portapapeles real (R3-10).
- **Menú del clic derecho** (`TextContextMenu`, solo escritorio): un `PointerDownEvent` con botón 1 en *TrickleDown* sobre el campo, frenado (S3-b: si no, Unity borra la selección: de 0…12 pasó a 0…0). Arma `textCut` y `textCopy` si hay selección, `textPaste` si `IClipboard.HasText`, `textSelectAll` si hay texto y no está todo seleccionado; si no queda ninguno, no abre nada (P-06). Textos del catálogo en el idioma de la app (AC-APP-09-07). Acciones: copiar = `IClipboard.SetText(selección)`; cortar = copiar y reemplazar la selección; pegar = insertar en la selección (sin `\n` en una línea); seleccionar todo = `textSelection.SelectAll()`. El cursor queda con `SelectRange`.
- **Teléfono** (AC-RUT-11-10, F-APP-15.4): `KeyboardAvoider` lee `ISoftKeyboard` cada `Tick`. Si el teclado tapa, suma su alto (en dp) al *padding* inferior de la página o del diálogo de arriba y lleva el campo con foco, con su texto de error, a 16 dp por encima del teclado (`FocusScroller`, ω 22); un diálogo se recentra en el área sobre el teclado y su alto máximo pasa a `área − 48` (DN3-18). El menú de copiar y pegar es el del sistema. [Device-And], NO VERIFICABLE sin módulo; en PlayMode, con `FakeSoftKeyboard`.
- **Riesgo en Android (R3-13, QA-G3-16)**: con `hideMobileInput = true` el texto se edita en el campo que dibuja UI Toolkit, y es probable que el sistema no muestre su barra de seleccionar, copiar y pegar sobre ese campo. Además no está probado que `TouchScreenKeyboard.area` sea confiable. La alternativa queda lista. `TextContextMenu` tiene un modo táctil (`TouchMode.AppMenu`) que abre **el mismo menú de la PC** al mantener presionado 0,5 s, en el punto del toque, con los textos del catálogo en el idioma **de la app**; es la variante de P-06 que el PO ya escribió (spec §14). Por defecto el modo es `SystemMenu`; si [Device-And] muestra que el sistema no da su menú, se cambia ese valor, sin tocar nada más. `TXT.LongPressMenuInAppMenuMode` lo prueba en PlayMode con `IsDesktop = false`. Si `.area` falla, `SoftKeyboard` lee el alto del marco visible de la ventana por JNI (`getWindowVisibleDisplayFrame`).

### 4.11 Idioma, letra, foco y barras

- Todo texto sale del catálogo y se rearma al cambiar el idioma (AC-APP-09-05). Textos del usuario (nombre, descripción) tal cual.
- Letra 1,3×: las clases `.rm-type-*` de `Typography.uss` en cada texto, también en `.rm-field__input` (el tamaño se hereda). Nada en una línea con "…" salvo los botones (ART_BIBLE §3.6); nombres, descripciones, subtítulos y avisos ajustan (AC-APP-05-05, CAP-25).
- Tab sigue el orden del árbol: barra superior, contenido, `+`, barra de pestañas, capa del simulador. Enter y Espacio activan (`RmButton`; tarjeta y chip con `NavigationSubmitEvent`). El anillo de foco va en tarjetas, `⋮`, la cápsula de los chips, lápices, cruces, tachos, pies, mosaicos y segmentos; los campos marcan el foco con su borde (AC-APP-05-07, 04-design-notes §4.14).
- **El foco se lleva a la vista** (QA-G3-11, AC-APP-05-07, AC-RUT-13-13, -13-15): un `ScrollView` de UI Toolkit no se desplaza solo hasta el hijo que recibe el foco por Tab. `FocusScroller` escucha `FocusInEvent` (burbujea desde el hijo) en `#routines-list`, `#editor-scroll` y `.rm-dialog__content`; si el elemento con foco no se ve entero con 16 dp de aire, anima `scrollOffset` con `CriticalSpring` ω 22 (en el acto con "reducir movimiento"). Es lo que hace Flutter al recorrer con Tab (`Scrollable.ensureVisible` en el pedido de foco por defecto, `<flutter-sdk>/packages/flutter/lib/src/widgets/focus_traversal.dart:204-213`). `NAV.TabScrollsTheFocusedIntoView`: editor de 60 filas (`RoutineSeeds.Sixty`) y diálogo de paso de serie a 640 × 360.
- `ScrollPolicy.Apply` en `#routines-list`, `#editor-scroll` y el contenido de los diálogos (AC-APP-05-11).
- AC-APP-05-10 (barra de título de Windows) ya quedó en `WindowsTitleBar` (00-deuda-it2 §4), con DD-01 confirmada por el PO (se actualiza en ≤ 2 s, o en el acto al volver el foco); CAP-27 lo muestra (§5.5).

---

## 5. Arnés de capturas

### 5.1 Cambios al arnés

| Pieza | Cambio |
|---|---|
| `SnapshotCli` | `-snapshotSet it2\|it3` (por defecto `it3`); la salida por defecto sale del conjunto: `docs/migration/iterations/03-rutinas/snapshots` |
| `SnapshotCatalog` | `For(set)`: `it2` = el catálogo actual (sin cambios), `it3` = `RoutinesCatalog.All` |
| `SnapshotScenario` | `Kind = Sequence`: una sola armada con varias tomas (DEMO) |
| `ScenarioScript` | `Type(name, text)`, `Shot(id, stem)` (aplica la regla de toma de una foto fija y registra la toma), `Reopen()` (dispone app y shell, cierra la base y la vuelve a armar sobre el mismo archivo), `RightClick(name)`, `PressBack(source)` |
| `SnapshotRunner` | secuencias; `INDEX.md` y `manifest.json` por conjunto; ni el *manifest* ni el `INDEX.md` llevan rutas de la PC (rutas relativas al repo; `SnapshotEvidenceTests` lo controla, QA-G3-20) |
| Regla de toma | la de la It. 2 más las de 04-design-notes §7.1: ningún `GrowMotion`, `CollapseMotion`, resaltado, cruce de color, resorte (llave, desplazamiento, aviso) ni transición de capa en curso; aviso **entero** a la vista cuando la toma lo pide (no a mitad de su entrada); el indicador de carga cuenta como el *spinner* de la It. 2; la vista previa del diálogo, ya rearmada después de `Type` |
| `capture-snapshots.ps1` | `-Set it3` por defecto; `ROWMATE_DATA_DIR` temporal propio y borrado en `finally`; nunca toma CAP-27 |

Deuda de la It. 2 que entra acá (REPORT §7.2): las sondas de aspecto del Designer pasan al catálogo `it3` (`WithProbe(…, aspect: true)`) y la toma falla si una transición queda congelada más de 2 s de reloj de prueba.

### 5.2 Bases y servicios de las tomas

`UiHostOptions.Routines` (nuevo) recibe una `HostRoutineFactory`:

| Fábrica | Qué arma |
|---|---|
| `HostRoutines.Seeded(RoutineSeed seed)` | `TempDatabase` + semilla + `SqliteRoutineRepository` con `InlineDbExecutor` (SQLite real, determinista) |
| `HostRoutines.Rota()` | F-ROTA: el texto con el nombre de la base |
| `HostRoutines.Scripted(Action<ScriptedRoutineRepository>)` | base lenta (`LoadDelay` y `SaveDelay`, por `FakeScheduler`; `Never` = no termina) o que falla al cargar, guardar o borrar |
| `HostRoutines.SameFile(TempDatabase db)` | reabrir (DEMO-10, AC-RUT-08-09) |
| `HostRoutines.Serial(seed)` | ejecutor real, solo para rendimiento |

Por defecto en el conjunto `it3`: perfil indicado, español (`es-AR`), **modo simulador** con la capa completa y el simulador sin ruido, `UiHostOptions.CreationClock = SequenceClock` para las fechas de creación (aparte del `FakeScheduler`, que da la hora de todo lo demás y los ids de serie: QA-G3-19), `FakeClipboard`, teléfono (`IsDesktop = false`) en TV y L640, escritorio en W1280.

### 5.3 Semillas (`RoutineSeeds`, spec §0.4)

`Empty` (F-VACIA), `Lista` (F-LISTA: las 4, de la más vieja a la más nueva, fechas 2026-10-20 a -23), `Borde` (F-BORDE: `Casos borde` con los 11 pasos de AC-RUT-12-06), `RaraWatts` (50 000 W), `RaraHuge` (×2 000 000 000), `RaraZero` (×0), `RaraSprint` (tipo `sprint`), `ListaPlus(int n)` (F-LISTA + `Rutina 01`…), `PiramideContiguous` (CAP-13: `Pirámide 500` con una serie ×2 [trabajo 60 s] justo después de la ×4), `SeriesTimesTen` (CAP-20b: serie ×10 con trabajo SPM 18), `Performance` (F-LISTA + 50 rutinas de 20 pasos), `Sixty` (F-LISTA + `Sesenta pasos`, 60 pasos sueltos: `NAV.TabScrollsTheFocusedIntoView`, R3-09), `RaraLooseProgressions` (paso suelto con progresión SPM 5 000: QA-G3-13 c), `RaraNegativeRepeat` (serie ×−3: DT-13), `RaraInt32` (`target_watts_min` = 3 000 000 000: DT-10), `Cap27` (`Escalera SPM` de F-LISTA, sola: la rutina **sintética** de la ventana del build normal, §5.5). Las F-RARA se escriben con SQL (la app no las deja guardar).

### 5.4 Catálogo `it3`

Archivo `CAP-nn[a|b]-<tema>.png`, `DEMO-nn-<tema>.png`, `VID-nn-<tema>.mp4`. "Comprobaciones" = `Checks` del escenario (fallan la toma). Las de aspecto son las sondas de 04-design-notes §7.2 (DN3-23), que WP-B suma en B3 con `WithProbe(…, aspect: true)` y esos mismos valores; la tabla las nombra cuando cambian la geometría.

| ID | Perfil · base | Cómo se llega (script) | Comprobaciones |
|---|---|---|---|
| CAP-01 | TV · `Empty` | `Tap(tab-routines)` | `#routines-empty` visible con `routinesEmpty` y `routinesEmptyHint`; `#routines-add` entero dentro de la ventana y **sin intersección** con `#sim-open`; `#sim-open.yMax + 16 = #routines-add.yMin` ± 1 y el mismo `xMax` ± 1 (AC-BLE-16-02); `#sim-ribbon`; pestaña 2 elegida; barra sin acciones |
| CAP-02 | TV · `Scripted(LoadDelay = Never)` | `Tap(tab-routines)`, `Advance(300 ms)` (gracia de 120 + fundido de 160) | solo `#routines-loading`, con opacidad 1 y el indicador de 32; ningún texto en `#routines-body`; `+` visible |
| CAP-03 | TV · `Rota()` | `Tap(tab-routines)` | `No se pudieron cargar las rutinas`, `#routines-retry`; sin indicador; `+` visible; el archivo F-ROTA sin cambios (hash) |
| CAP-04 | TV · `Lista` | `Tap(tab-routines)` | nombres en orden `Escalera SPM`, `Pirámide 500`, `Solo calentamiento`, `Intervalos 4×`; descripción solo en la tarjeta 1; resúmenes de AC-RUT-10-10; después del *layout*, `SegmentCount` 8/14/1/7 y tipos en orden, `Overflow` falso y `SegmentWidth` 24 ± 0,5 en `Escalera SPM` y 16,4 ± 0,5 en `Pirámide 500` (DN3-07); `⋮` en las 4; barra vertical oculta |
| CAP-05 | TV · `Lista` · `en` | ídem | `Routines`; `1 interval · 1 rest`, `2 intervals · 1 rest`, `1 step`, `4 intervals · 3 rests` |
| CAP-06 | L640 · `ListaPlus(26)` | `Tap(tab-routines)`; `ScrollToEnd(routines-list)` | última tarjeta entera, con `yMax` ≤ `yMin` de `#sim-open` − 16 ± 1 (que está sobre `#routines-add`: AC-RUT-10-09, AC-BLE-16-02); `Audit()` sin desbordes; llave sin intersección con `+` |
| CAP-07 | TV · `Lista` | `Tap(routine-menu-0)` | `#routine-menu-edit` y `#routine-menu-delete` visibles; el menú debajo del `⋮` con su `xMax` igual al del botón ± 1; `#popup-layer` antes que `#simulator-layer` en el árbol |
| CAP-08 | TV · `Lista` | `Tap(routine-menu-0)`, `Tap(routine-menu-delete)` | título, `¿Eliminar "Escalera SPM"?`, `#dialog-confirm` con `.rm-button--danger-fill`; diálogo de 331 de ancho (`min(400; 411 − 80)`); `#sim-open` tocable (*hit-test*) con el velo |
| CAP-09 | TV · `Empty` | `Tap(tab-routines)`, `Tap(routines-add)` | `Nueva rutina`, `#editor-save`, campos vacíos, `Pasos (0)`, `editorEmptySteps` con `\n`; `#tab-bar` oculta; `WhyNotTappable(editor-back) == null`; `#sim-open` a 80 ± 1 del borde inferior (16 sobre el alto conservado de la barra: AC-BLE-16-02) |
| CAP-10 | TV · `Lista` | `Tap(routine-card-0)` | `Editar rutina`; `Pasos (4)`; la cápsula de `#series-repeat-0` = `×3` y la caja del chip ≥ 48 × 48; textos de `step-row-0…3` (`StepSubtitleView.Text`, con `␣␣·␣␣`); ícono de tendencia en `step-row-1`; `step-row-3` después de `#series-footer-0`; `.rm-step-row--in-series` a 16 ± 1 del borde izquierdo del bloque; dos `.rm-series__stack-layer` visibles (×3) |
| CAP-11 | W1280 · `Lista` | ídem | ídem; comparable con `docs/image-04.png` del legacy |
| CAP-12a/b | TV · `Borde` | `Tap(tab-routines)`, `Tap(routine-card-0)`; b: `ScrollTo(step-row-10)` | `Pasos (11)`; los 11 títulos y subtítulos de AC-RUT-12-06; `×1` y `×2`; `×1` sin hojas visibles y `×2` con una; ningún texto cortado (`Audit`) |
| CAP-13 | TV · `PiramideContiguous` | abrir `Pirámide 500` | dos `.rm-series` distintos, cada uno con su chip (`×4`, `×2`), su tacho y su pie; **16 ± 1 dp** entre el pie del primero y el encabezado del segundo (8 + 8, AC-RUT-14-11, QA-G3-04) |
| CAP-14 | TV · `Empty` | `+`, `Tap(editor-add-step)` | `Nuevo paso`; `step-type-work` y `step-mode-time` elegidos; mosaicos en 2 × 2; `#step-preview` dentro de `.rm-dialog__pinned` (alto máximo 866 ≥ 560) = `Trabajo` `0s`; campos vacíos; sin `editorProgressions` |
| CAP-15a/b | TV · `Lista` | `Tap(routine-card-0)`, `Tap(step-edit-1)`; b: `ScrollToEnd` del contenido | a: `Editar paso`, `5`/`0`, `18`, vista previa con tendencia; b: los tres campos de progresión, `2`, `Ej: 5`, `Ej: -2 (más rápido)`, botones dentro de la ventana |
| CAP-16 | TV · `Empty` | `+`, add-step, `Tap(step-mode-distance)`, `Type(step-meters, "500")` | `#step-meters` = `500`; sin `#step-min`; vista previa `500m` |
| CAP-17 | TV · `Lista` | `Tap(routine-card-0)`, `Tap(series-repeat-0)` | `#repeat-amount` = `3` con foco; teléfono: cursor al final; diálogo de 331 de ancho |
| CAP-18 | TV · `Empty` | `+`, `Type(editor-name, "␣␣␣")`, `Tap(editor-save)` | `#toast` = `El nombre no puede estar vacío`, entero, de 16 a 16 y a 16 ± 1 del borde inferior; sin intersección con `#sim-open` (`#toast.yMin ≥ #sim-open.yMax + 8`, el aviso queda debajo de la llave: DN3-13); el editor sigue |
| CAP-19 | TV · `Empty` | `+`, `Type(editor-name, "Prueba")`, `Tap(editor-save)` | `Agregá al menos un paso` |
| CAP-20a | TV · `Empty` | `+`, add-step, `Type(step-wmin, "10000")`, `Tap(dialog-confirm)` | `#step-wmin` con `.rm-field--error`; `#step-wmin-error` = `Fuera de rango: hasta ±9999`; diálogo abierto |
| CAP-20b | TV · `SeriesTimesTen` | abrir, `Tap(step-edit-0)`, `Type(step-prog-spm, "200")`, confirmar | `#step-prog-spm-error` = `Fuera de rango en la repetición 6: hasta ±999` |
| CAP-21 | TV · `Lista` | chip `×3`, `Type(repeat-amount, "1000")`, `Tap(dialog-confirm)` | `#repeat-error` = `Máximo 999`; diálogo abierto |
| CAP-22 | L640 · `Lista` | abrir `Escalera SPM` | barra superior y campos enteros; `Audit` sin desbordes; `#tab-bar` oculta |
| CAP-23 | L640 · `Lista` | abrir, `Tap(step-edit-1)` | título y `#dialog-cancel`/`#dialog-confirm` dentro de la ventana; `.rm-dialog--compact`; `#step-preview` como primer hijo del contenido desplazable (alto máximo 312 < 560); mosaicos en una fila de 4; ninguna etiqueta cortada |
| CAP-24a/b | TV · `Lista` · `en` | a: abrir; b: `step-edit-1` y `ScrollToEnd` | textos de spec CAP-24 |
| CAP-25a/b | TV · `Lista` · letra 1,3 | a: lista; b: editor | `Audit` sin desbordes; nada fuera de su tarjeta, fila o bloque |
| CAP-26 | W1280 · `Lista` · escritorio | abrir, foco en `editor-name` (todo seleccionado), `FakeClipboard = "texto"`, `RightClick(editor-name)` | menú `.rm-menu--dense` con `Cortar`, `Copiar`, `Pegar` y **sin** `Seleccionar todo` |
| CAP-27 | — | **no es del arnés**: §5.5 (solo a pedido) | — |
| CAP-28 | TV · `Scripted(SaveFails)` | `+`, nombre `Prueba`, `Tap(editor-add-series)`, `Tap(editor-save)` | `No se pudo guardar la rutina`, con las comprobaciones de aviso de CAP-18; editor abierto con `Prueba` y `Pasos (2)` |
| DEMO-01…12 | W1280 · `Empty` · `Sequence` | spec §7.4, un `Shot` por paso; DEMO-10 con `Reopen()` | lo de spec §7.4 (`Pasos (3)`, `Pasos (4)`, `1 serie · 1 descanso`, 8 y 10 tramos, `×4`, `18 spm`, `SPM +2/rep`, el vacío al final) |
| VID-01 | TV · `Empty` · 10 s | 0,5 `editor-add-step` · 1,5 `dialog-confirm` · 3,5 `editor-add-series` · 6 `step-delete-0` · 8 `series-delete-0` | sin saltos de alto entre cuadros (la caja del contenido nunca cambia más que lo que da el resorte) |
| VID-02 | TV · `Lista` · 8 s | 0,5 `routine-card-0` · 2,5 `editor-back` · 4 `routine-menu-0` · 5 `routine-menu-delete` · 6,5 `dialog-confirm` | la tarjeta colapsa y las de abajo suben sin salto |
| VID-03 | TV · `Empty` · 8 s | `+` antes de grabar; 0,5 / 1,5 / 2,5 `editor-save` | un solo `#toast` en todo el video; se va a los 6,5 s ± 1 cuadro |

### 5.5 CAP-27: ventana del build normal

La toma pide el **build normal** con una rutina **de su base**, y el build normal solo usa la carpeta real (§2.4). Es la única excepción de AC-HIS-20-04 (spec §0.4, §7.1, §12.1) y QA la aceptó con condiciones (DT-12, QA-G3-06). La hace `tools/unity/capture-cap27.ps1`, un script **aparte**:

0. **Solo a pedido.** Sin `-ReplaceUserDatabase` no toca nada: dice lo que haría y sale con 4. No lo llaman `capture-snapshots.ps1`, `run-unity-tests.ps1`, `build.ps1` ni ningún *workflow* de `.github/` (lo controla `ITT.Cap27RunsOnlyOnRequest`). Si Windows no está ya en modo Oscuro (`AppsUseLightTheme` = 0), sale con 3 (NO VERIFICABLE) sin tocar nada: nunca cambia el tema del usuario.
1. **Base sintética.** Escribe en `<scratch>` la base sembrada (`SnapshotCli -snapshotSeedDb <carpeta>/semilla/cap27.db -seed Cap27`, en batchmode, con `ROWMATE_DATA_DIR` en otra carpeta temporal, y espera a que Unity termine): solo `Escalera SPM` de F-LISTA, nunca datos del usuario (el repo es público). Lleva un nombre **neutro**, porque `TempDatabase.Guard` no deja escribir `rower_app.db` fuera de `<temp>/RowMate.Tests/`; el paso 4 la copia como `rower_app.db` (QA-V3-01). Sale con 7 si Unity falla o si el archivo no empieza con la firma de SQLite. `SnapshotCli.SeedDatabase` y `ITT.Cap27SeedStepRunsForReal` corren el mismo `RoutineSeeds.WriteFile`.
2. **Se niega a correr** (sale con 5, sin tocar nada) si hay un proceso `RowMate` o un **Editor** de Unity abierto (no puede saber si tienen la base abierta) o si `rower_app.db`, `-wal` o `-shm` no se pueden abrir con `FileShare.None` (archivo en uso). El Editor se reconoce por la ruta del ejecutable (`…\Editor\Unity.exe`, de cualquier proyecto: una copia de este repo usa la misma carpeta de datos) o por `Temp/UnityLockfile` tomado. El `unity.exe serve` de la Hub, que está abierto siempre que la Hub lo está, no es un Editor y no cuenta; un proceso `Unity` cuya ruta no se puede leer sí cuenta (QA-V3-01). Lo comprueba antes de sembrar y otra vez justo antes del resguardo, porque la semilla abre Unity y tarda.
3. **Resguardo verificado antes de tocar.** Copia `rower_app.db`, `-wal` y `-shm` de la carpeta real (o anota cuáles no existen) a una carpeta de resguardo **calculada en el momento** (`[IO.Path]::GetTempPath()` + `RowMate.Cap27.<fecha-hora>`, nunca una ruta escrita en el script), con un `resguardo.json` con tamaño, SHA-256 y `LastWriteTimeUtc` de cada uno. También anota el *hash* y la fecha de `rower_app_dev.db`, que no se toca. Vuelve a calcular el SHA-256 de cada copia: si alguna no coincide, sale con 5 **antes** de reemplazar nada.
4. Reemplaza `rower_app.db` por la base sembrada y borra `-wal` y `-shm`. Abre el build normal, espera, toca la pestaña Rutinas, captura la ventana entera con su barra de título (`capture-window.ps1`, como en la It. 2) y cierra el build. El toque va a `0.625,0.98` del área cliente: el centro de la 3.ª de las 4 pestañas y, en alto, dentro de la barra de 64 dp en ventanas de hasta 3200 dp (con 0,96 salía de la barra pasados los 1600 dp). `NBT.Cap27ClickSelectsTheRoutinesTab` lee el punto del script y lo prueba con la UI real del build normal en seis tamaños de ventana. `capture-window.ps1` solo hace clic si en ese punto está la ventana del build, y solo captura si está adelante en 9 puntos de la ventana. Si otra la tapa (otra app, un aviso del sistema), sale con 3 sin clic ni captura, porque en la foto saldría la pantalla del usuario. La llamada está **una sola vez**, en `Invoke-WindowCapture`, sin `-Arguments`. `capture-window.ps1` pasa `-ArgumentList` solo si hay argumentos, porque `Start-Process` de Windows PowerShell 5.1 no acepta `''`. Deja de esperar la ventana si el proceso termina y sale con 4 ante un error no previsto, para no confundirlo con el 1 de "no apareció la ventana" (QA-V3-11). `ITT.WindowCaptureStartsTheExeForReal` corre el arranque de verdad con esa llamada.
5. **Restauración en `finally`**, pase lo que pase: cierra solo el build que abrió (un `RowMate` con esa ruta que arrancó después que el script), borra lo que el build haya dejado (`db`, `-wal`, `-shm`, con reintentos mientras se cierra), vuelve a copiar cada archivo resguardado con su `LastWriteTimeUtc` original, o deja la carpeta sin base si no había ninguna. Después **verifica**: los mismos archivos existen, con el mismo SHA-256 y la misma fecha, y `rower_app_dev.db` sigue igual. Si algo no coincide, **no borra el resguardo**: sale con 6 e imprime en la consola cómo recuperar (qué archivo copiar adónde, con las rutas reales: es la consola del usuario, no un documento). Si todo coincide, borra el resguardo.

Ni el script ni la captura escriben rutas de la PC en el repo: CAP-27 no entra al `manifest.json` ni al `INDEX.md` del arnés, y el resumen que copian los informes usa `<scratch>` (QA-G3-20). La carpeta `LocalLow/iguisoft/RowMate` y el `Player.log` del motor no son bases del usuario: quedan como estén. QA corre la comparación de AC-HIS-20-04 antes y después de todo, también de esta toma. `ITT.Cap27ProtectsTheUserDatabase` revisa el texto del script: el *switch* obligatorio, las dos negativas, la verificación del resguardo antes de la copia que reemplaza, el `finally`, la verificación final y los códigos de salida.

**Ensayo (`-Rehearsal`, QA-V3-01).** Corre de verdad los pasos 1, 2, 3 y 5 y el reemplazo del 4 sobre una carpeta temporal con archivos de prueba: `rower_app.db` y `-wal` de mentira con una fecha vieja con fracción de segundo, y `rower_app_dev.db`. La carpeta del build normal no se nombra en esa rama. No hace falta `-ReplaceUserDatabase`, no abre ningún build, no toca el mouse ni la pantalla y no pide el modo Oscuro. En lugar de la captura comprueba que la base sintética quedó como `rower_app.db` (el mismo SHA-256 que la semilla, sin `-wal` ni `-shm`). Después corre `Invoke-WindowCapture`, la misma llamada de la toma real, con un ejecutable de prueba sin ventana en lugar del build (`new-windowless-exe.ps1`, en la carpeta de trabajo), y espera 1 con `el build no abrio ninguna ventana` y ninguna captura (QA-V3-11; si no, 8). Sin ventana no hay clic ni captura. Al final deja un `-wal` y un `-shm`, como el build; la restauración los tiene que sacar o devolver. Los procesos que frenarían la toma real se informan y nada más; este proyecto abierto en un Editor sí frena el ensayo (5), porque la semilla necesita Unity. Sirve para que QA pruebe la herramienta de punta a punta antes de la toma real, que sigue siendo solo a pedido del usuario. `ITT.Cap27RehearsalNeverTouchesTheUserFolder` controla que el ensayo nunca nombre la carpeta real ni abra el build, y que corra la llamada única con el ejecutable de prueba.

---

## 6. Tests

### 6.1 Suites, dobles y comandos

| Suite | Qué | Comando |
|---|---|---|
| dotnet (Core, App, Infrastructure con SQLite real, Simulator) | §6.2 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it3/<carpeta>` (y `-Matrix -UnityResults …`) |
| Unity EditMode (con y sin simulador) | las mismas fuentes, con el SQLite de gilzoide; `Tests/UnityEditMode` | `tools/unity/run-unity-tests.ps1 [-Simulator]` |
| Unity PlayMode (con y sin simulador) | la UI montada con base de prueba | ídem |
| Arnés | §5 | `tools/unity/capture-snapshots.ps1 -Set it3` |
| Builds | normal Mono (`BuildOutputInspector` sin rastros nuevos), simulador Mono, IL2CPP (3 = NO VERIFICABLE sin módulo); humo de base con `-rowmate-db-smoke` en un build de desarrollo | `tools/build/build.ps1 …` |

Dobles (Tests/Support, nunca en un build): `InlineDbExecutor`, `TempDatabase` (con su guardia), `RoutineSeeds`, `ScriptedRoutineRepository` (demoras de carga y de guardado por `FakeScheduler`, fallas a pedido al cargar, guardar o borrar, cuenta de llamadas), `InMemoryRoutineRepository`, `SequenceClock` (solo la lee `RoutinesStore`), `FakeClipboard`, `FakeSoftKeyboard`, `RecordingLogger` (seguro entre hilos).

**Humo de la base en un *player*** (R3-01): con `-rowmate-db-smoke <carpeta>` (solo `UNITY_EDITOR || DEVELOPMENT_BUILD`), `AppRoot` corre `DatabaseSmoke` (crea v8, guarda y relee `Escalera SPM` con sus 4 pasos, borra), escribe `[DB] humo: ok` o `[DB] humo: falla (<problema> <código de SQLite>)` y sale. **Sin la carpeta ni ninguna ruta** en esa línea (QA-G3-20): los informes la copian tal cual. En Mono prueba que el binario nativo carga en un build de Windows; con el módulo IL2CPP, el mapeo por reflexión y `link.xml`.

### 6.2 Archivos de test

dotnet + Unity EditMode (WP-A, `Tests/EditMode/`):

| Archivo | Abrev. | Cubre |
|---|---|---|
| `Infrastructure/SqliteRoutineRepositoryTests.cs` | RRT | AC-RUT-08-01 a -08 (todo sobre una carpeta con `ñá`) |
| `Infrastructure/DatabaseSchemaTests.cs` | DSC | AC-HIS-17-02 (SQL de `sqlite_master` al carácter, `table_info`, `foreign_key_list`, índice, `user_version`, `journal_mode`); `DSC.WriteConnectionIsWalFullAndNoForeignKeys` (QA-G3-08); `DSC.SqliteVersionIsTheExpectedOne` (3.53.4 en Unity, 3.49.1 en dotnet; reemplaza a SQU) |
| `Infrastructure/DatabaseOpenTests.cs` | DOT | AC-HIS-17-04, -05 (bytes iguales con F-ROTA, v9 en el encabezado, v9 solo en el WAL, v0 con tablas, v1-7, archivo vacío, **archivo de 1 a 99 bytes con y sin la firma**: QA-G3-13 e); `DOT.ProblemsNeverCarryTheFolder` (QA-G3-20) |
| `Infrastructure/DatabaseFilesTests.cs` | DFT | AC-HIS-20-01 |
| `Infrastructure/SerialExecutorTests.cs` | SET | hilos (§2.6), AC-HIS-17-06 (regla); `SET.DisposeUnsubscribesTheUnloadHook`, `SET.RunAfterDisposeFailsTheTask` (QA-G3-09) |
| `Infrastructure/LegacyIsolationTests.cs` | LIT | AC-HIS-17-03 (QA-G3-14): ningún `.cs` de producción de `Assets/RowMate` (fuera de `Tests/`) nombra `com.example`, `.dart_tool`, `sqflite`, `shared_preferences` ni `FlutterSharedPreferences`, y `rower_app` aparece solo en `DatabaseFiles.cs` |
| `Support/TempDatabaseTests.cs` | TDT | AC-HIS-20-04 (la guardia, con mensajes sin la carpeta); `TDT.WorksUnderAFolderWithAccents` (QA-G3-13 g) |
| `Support/RecordingLoggerTests.cs` | RLG | `RLG.ConcurrentLogsAreAllKept` (QA-G3-09) |
| `Text/DartIntParserTests.cs` | DIP | AC-RUT-13-05 |
| `Text/DartTextTests.cs` | DTT | recorte de Dart (AC-RUT-11-06, -05) |
| `Routines/StepLimitsTests.cs` | SLT | AC-RUT-13-11, -12, AC-RUT-14-06 (forma cerrada) |
| `Routines/RoutineTemplateTests.cs` | RTT | AC-RUT-11-03, AC-RUT-12-01, -03, -04, AC-RUT-14-01, -07 a -09, -11 |
| `Routines/EditorLayoutTests.cs` | ELT | AC-RUT-14-03, -10, -11 |
| `Routines/SeriesIdGeneratorTests.cs` | SIG | AC-RUT-08-06 |
| `Routines/RoutineStructureTests.cs` | RSB | AC-RUT-10-11 |
| `Routines/StructureBarLayoutTests.cs` | SBL | AC-RUT-10-11 (DN3-07: capacidad, ancho, desborde y pista vacía) |
| `Design/BottomObstaclesTests.cs` | BOT | AC-APP-03-05, AC-BLE-16-02, AC-RUT-10-09 (DN3-13, DN3-14: posiciones de 04-design-notes §4.12-§4.13) |
| `Design/MotionSpecsTests.cs` | (N) | las constantes de 04-design-notes §5.11 (DN3-16) |
| `Routines/StepSubtitleTests.cs` | SST | AC-RUT-12-06, AC-RUT-04-06 |
| `Routines/StepDialogFormTests.cs` | SFT | AC-RUT-12-03, AC-RUT-13-01, -04, -06 a -12, -16; filas de QA-G3-13 (a, c, d) |
| `Routines/RepeatCountTests.cs` | RCT | AC-RUT-14-05, -06; `RCT.HugeNegativeIsUnchanged` (QA-G3-13 b) |
| `Routines/RoutinesStoreTests.cs` | RST | AC-RUT-09-01, -02, -04, -06; AC-RUT-08-05 (lugar); `RST.LateResultsAfterDisposeChangeNothing`, `RST.ExpectedFailuresAreWarnings` (QA-G3-09) |
| `Routines/RoutineEditorStoreTests.cs` | RET | AC-RUT-09-03, -05, AC-RUT-11-05 a -07; `RET.SaveAfterSavedIsIgnored` (QA-G3-01), `RET.LateSaveAfterDisposeTouchesNothing` (QA-G3-09), `RET.LooseStepProgressionsDoNotBlockTheSave`, `RET.ZeroAndNegativeRepeatsDoNotBlockTheSave` |
| `Routines/RoutineCardModelTests.cs`, `StepRowModelTests.cs` | RCM, SRM | AC-RUT-10-03, -10, AC-RUT-12-02 |
| `Composition/AppComposerTests.cs` | ACT (C) | AC-RUT-09-01, AC-APP-01-09 |
| `Localization/CatalogParityTests.cs`, `L10nKeysTests.cs` | (C) | 190 claves |
| `Traceability/It03Ids.cs`, `TraceabilityTests.cs` | (N/C) | los **102** IDs con su verificación (con AC-BLE-16-02); `UnityOnlyIds`, `WorkPackageBIds` |
| `Project/AsmdefLintTests.cs`, `PureCodeRulesTests.cs` | (C) | §1.2; `ConfigureAwait(false)` también en Infrastructure; `SqliteOnlyInInfrastructure` (QA-G3-07); `SqlLiteralsUseSingleQuotes` (QA-G3-08) |
| `Tests/Simulator/SimulatorPanelLayoutTests.cs` | SPL (N) | la llave con obstáculos (§4.8): `SPL.FabBottomAvoidsTheFab`, `SPL.PanelBottomNeverMoves`; AC-BLE-16-03 sin cambios |

Unity (WP-B):

| Archivo | Abrev. | Cubre |
|---|---|---|
| `PlayMode/RoutinesScreenTests.cs` | RLT | F-RUT-10, AC-RUT-09-01, -02, -04, -06, AC-RUT-08-08, AC-HIS-17-05, AC-APP-01-09, AC-APP-02-08; `RLT.UserTextIsShownAsWritten`, `RLT.LongWordWithoutSpacesWraps` (QA-G3-10, -13 f) |
| `PlayMode/RoutineEditorTests.cs` | RED | F-RUT-11, -12, -14 en pantalla, AC-RUT-09-03, -05; `RED.TapDuringTheExitCreatesNothing`, `RED.EnterDuringTheExitDoesNothing` (QA-G3-01), `RED.BackDuringASlowSaveClosesAndTheSaveLands` (QA-G3-09) |
| `PlayMode/StepDialogTests.cs` | SDT | F-RUT-13 en pantalla |
| `PlayMode/RepeatDialogTests.cs` | RPT | AC-RUT-14-04 a -06 |
| `PlayMode/NavigationLayersTests.cs` | NAV | F-APP-03, AC-RUT-10-06, -07, AC-RUT-11-08, AC-APP-05-07, -08; `NAV.MenuItemTwiceOpensOneDialog`, `NAV.DialogButtonTwiceActsOnce`, `NAV.BackSkipsWhatIsClosing` (QA-G3-01), `NAV.EscapeClosesOneLevelAtATime` (QA-G3-10), `NAV.TabScrollsTheFocusedIntoView` (QA-G3-11) |
| `PlayMode/TextEditingTests.cs` | TXT | AC-RUT-11-02, -09, AC-APP-09-07; `TXT.TabFromTheDescriptionMovesFocus`, `TXT.EscapeInAFieldKeepsTextAndFocus`, `TXT.EnterInASingleLineFieldBlurs`, `TXT.EnterInADialogFieldDoesNotConfirm` (QA-G3-10), `TXT.LongPressMenuInAppMenuMode` (R3-13) |
| `PlayMode/KeyboardAvoiderTests.cs` | KAV | AC-RUT-11-10 (simulado) |
| `PlayMode/RoutinesPersistenceTests.cs` | RPS | AC-RUT-08-06, -09, AC-HIS-17-04, AC-HIS-20-02 (`RPS.VariantsDoNotMix`) |
| `PlayMode/RoutinesPerformanceTests.cs` | PRF | AC-HIS-17-06 (`Stopwatch` del código propio en el hilo principal: QA-G3-21) |
| `PlayMode/BootSceneTests.cs` | BOOT (C) | arranque con la base en `ROWMATE_DATA_DIR` (AC-HIS-20-04) |
| `PlayModeSimulator/RoutinesSimulatorLayerTests.cs` | RSL | AC-APP-03-07, AC-BLE-16-01, AC-RUT-10-04, AC-HIS-20-03, AC-APP-03-05 (llave) |
| `PlayModeSimulator/SimulatorOverlayTests.cs` | (C) | AC-BLE-16-02 con el texto nuevo (§4.8): la llave sobre el `+` en Rutinas, en su lugar en las otras pestañas y con el editor, sin saltos y tocable mientras se mueve, y el panel abierto siempre en el mismo lugar |
| `UnityEditMode/DataLocationTests.cs` | DLT | AC-HIS-17-01 (Windows: `LocalLow/iguisoft/RowMate`; Android: el camino de código por `getDatabasePath`) |
| `UnityEditMode/RuntimePanelFactoryTests.cs` | RPF | QA-G3-12: la copia de `PanelTextSettings` con los respaldos en orden (íconos, Noto Emoji), distinta del asset, y la misma en los tres anfitriones |
| `EditMode/Project/ItThreeToolingTests.cs` | ITT (dotnet, archivo de WP-B) | scripts con `ROWMATE_DATA_DIR` y `finally`; `capture-cap27.ps1` con sus condiciones (`ITT.Cap27RunsOnlyOnRequest`, `ITT.Cap27ProtectsTheUserDatabase`), el paso 1 de verdad (`ITT.Cap27SeedStepRunsForReal`), el ensayo (`ITT.Cap27RehearsalNeverTouchesTheUserFolder`) y `capture-window.ps1` (`ITT.WindowCaptureOnlyClicksAndCapturesItsOwnWindow`) (QA-V3-01); el arranque de `capture-window.ps1` de verdad, con y sin `-Arguments` (`ITT.WindowCaptureStartsTheExeForReal`, QA-V3-11); solo `AppRoot` elige el archivo; agujas nuevas del inspector; ningún script ni *manifest* con rutas de la PC (QA-G3-20) |

### 6.3 Matriz AC → test / captura

Marcas: **d** dotnet (y Unity EditMode) · **dB** dotnet en archivo de WP-B · **s** dotnet con simulador · **u** Unity EditMode solo · **p** PlayMode · **ps** PlayMode con simulador · **cap** captura · **eq** equipo (QA) · **qa** revisión de QA.

**F-RUT-08 (+ F-HIS-18)**

| AC | Verif. | Tests / capturas |
|---|---|---|
| 08-01 | d | `RRT.InsertKeepsTheFourteenAttributesAndText` (`Pirámide ñ ¿¡`, `Sube 2 spm⏎2.ª vuelta`, 0 ≠ null), `RRT.InsertNumbersStepsFromZero` |
| 08-02 | d | `RRT.UpdateReplacesStepsAndKeepsCreatedAt`, `RRT.UpdateWithoutChangesIsIdentical`, `RRT.UpdateLeavesTheOthers` |
| 08-03 | d | `RRT.DeleteRemovesTheRoutineAndItsSteps`, `RRT.NewRoutineNeverSeesDeletedSteps` |
| 08-04 | d | `RRT.GetByIdReturnsStepsInOrder`, `RRT.GetByIdOfMissingOrDeletedIsNull` |
| 08-05 | d | `RRT.ListIsNewestFirst`, `RST.CreatedAtIsUtcAcrossATimeZoneChange` (UTC−3 → UTC−5), `RST.EditKeepsDateAndPlace` |
| 08-06 | d p | `SIG.SameMillisecondGivesDistinctIds` (reloj fijo), `RRT.SeriesSurviveTheRoundTrip`; `RPS.SeriesBlocksSurviveReopen` (dos toques en `Serie` sin avanzar el `FakeScheduler`: mismo milisegundo) |
| 08-07 | d eq | `RRT.InsertIsAllOrNothing`, `RRT.UpdateIsAllOrNothing` (*trigger*); `DSC.WriteConnectionIsWalFullAndNoForeignKeys` (también en Unity EditMode, QA-G3-08); [Device-Win] cerrar el proceso 1 s después de `Guardar` |
| 08-08 | d p | `RRT.UnknownTypeFailsTheLoadWithoutWriting`, `RRT.Int32OverflowFailsTheLoadWithoutWriting` (`RaraInt32`, DT-10), `RST.UnknownTypeGivesLoadFailed`; `RLT.UnknownTypeShowsTheLoadError` |
| 08-09 | p cap eq | `RPS.ReopenShowsTheSame`; DEMO-10; [Device-Win] |

**F-RUT-09**

| AC | Verif. | Tests / capturas |
|---|---|---|
| 09-01 | d p | `RST.LoadsOnceAtStart`, `ACT.StartLoadsRoutines`; `RLT.SecondVisitDoesNotShowTheIndicator` (base de 2 s) |
| 09-02 | d p | `RST.CreateGoesFirst`, `RST.UpdateKeepsPosition`, `RST.DeleteKeepsOrder`; `RLT.CreateEditDeleteInPlace` |
| 09-03 | d p | `RET.SaveWhileSavingIsIgnored`, `RET.SaveAfterSavedIsIgnored`; `RED.DoubleTapSaveCreatesOne`, `RED.TapAndEnterCreateOne`, `RED.EditSavesOnce`, `RED.TapDuringTheExitCreatesNothing` (segundo toque 50 ms después, con el guardado terminado y el editor saliendo), `RED.EnterDuringTheExitDoesNothing` |
| 09-04 | d p cap | `RST.LoadFailureThenRetry`; `RLT.LoadErrorAndRetry`, `RLT.DeviceTabStillWorksWithLoadError`; CAP-03 |
| 09-05 | d p cap | `RET.SaveFailureKeepsEverything`, `RST.ExpectedFailuresAreWarnings`; `RED.SaveErrorKeepsTheEditorOpen`, `RED.BackDuringASlowSaveClosesAndTheSaveLands`; CAP-28 |
| 09-06 | d p | `RST.DeleteFailureKeepsTheRoutine`, `RST.ExpectedFailuresAreWarnings`; `RLT.DeleteErrorKeepsTheCard`, `RLT.DeleteRemovesWhenDone` |

**F-RUT-10**

| AC | Verif. | Tests / capturas |
|---|---|---|
| 10-01 | p cap | `RLT.TabTitleWithoutActionsAndFab`; CAP-01, CAP-04 |
| 10-02 | p cap | `RLT.LoadingShowsOnlyTheIndicator` (nada a los 100 ms; el indicador de 32 entero a los 300 ms), `RLT.FastLoadNeverShowsTheIndicator` (carga de 20 ms), `RLT.EmptyStateTextsAndColors`; CAP-01, CAP-02 |
| 10-03 | d p cap | `RCM.DescriptionOnlyWhenNotEmpty`; `RLT.CardContent`, `RLT.LongNameAndDescriptionWrap`, `RLT.LongWordWithoutSpacesWraps`, `RLT.UserTextIsShownAsWritten` (`<b>A</b>` y `Serie\n2` en la tarjeta, el diálogo de eliminar y los campos); CAP-04, CAP-06 |
| 10-04 | p ps cap | `RLT.FabOpensANewEditor`; `RSL.WrenchNeverCoversTheFab`, `RSL.FabTappableEverywhere`, `SimulatorOverlayTests.WrenchSitsAboveTheFabInRoutines`; CAP-01 |
| 10-05 | p | `RLT.TapCardOpensItsEditor` |
| 10-06 | p cap | `NAV.CardMenuEditAndDelete`, `NAV.MenuClosesOnOutsideEscapeAndBack`, `NAV.MenuButtonTooltip`, `NAV.MenuItemTwiceOpensOneDialog`, `RLT.MenuButtonDoesNotOpenTheEditor`; CAP-07 |
| 10-07 | p cap | `NAV.DeleteDialogOnlyConfirmDeletes` (cancelar, velo, Escape, "Atrás"), `NAV.DialogButtonTwiceActsOnce` (un solo borrado), `RLT.UserTextIsShownAsWritten`; CAP-08, DEMO-11 |
| 10-08 | qa p | `RLT.NoOtherActions` (sin búsqueda, orden, deslizar, deshacer) |
| 10-09 | d p ps cap | `BOT.PositionsOfTheDesignNotes`; `RLT.ThirtyRoutinesScrollAboveTheFab`, `RLT.ScrollPositionSurvivesEditorAndTabs`; `RSL.LastCardEndsAboveTheWrench`; CAP-06 |
| 10-10 | d p cap | `RCM.SummariesEsEn` (las 5 filas); `RLT.SummariesOnScreen`; CAP-04, CAP-05 |
| 10-11 | d p cap | `RSB.FixtureSegments` (8, 10, 14, 1, 7 y tipos), `RSB.ImpossibleRepeatsNeverExpand`, `SBL.WidthCapacityOverflowAndEmptyTrack`; `RLT.StructureBarStaysInOneLine`, `RLT.ImpossibleRoutinesLoadAndRespond` (×2 000 000 000 con desborde; ×0 con la pista vacía); CAP-04, DEMO-07, DEMO-09 |

**F-RUT-11**

| AC | Verif. | Tests / capturas |
|---|---|---|
| 11-01 | p cap | `RED.AppBarNewAndEdit` (título, `navBack`, `Guardar`); CAP-09, CAP-10, CAP-24 |
| 11-02 | p cap | `RED.FieldsAndPrefill`, `TXT.SingleLineDropsPastedNewlines`, `TXT.DescriptionTwoLinesEnterAndScroll`, `TXT.EnterInASingleLineFieldBlurs`; CAP-09 |
| 11-03 | d p | `RTT.CountIsTheTemplate`; `RED.StepsCountUpdatesAtOnce` |
| 11-04 | p cap | `RED.EmptyStepsText`; CAP-09 |
| 11-05 | d p cap | `RET.ValidationOrder`, `RET.OutOfRangeStepBlocksTheSave` (`RaraWatts`, `RaraHuge`), `RET.LooseStepProgressionsDoNotBlockTheSave` (`RaraLooseProgressions`), `RET.ZeroAndNegativeRepeatsDoNotBlockTheSave` (`RaraZero`, `RaraNegativeRepeat`; DT-13); `RED.ValidationToasts`; CAP-18, CAP-19 |
| 11-06 | d p | `RET.SaveTrimsRenumbersAndKeepsId`, `DTT.TrimLikeDart`; `RED.SaveClosesAndTheListShowsIt` |
| 11-07 | d p | `RET.NoUniquenessNoMaxLength`; `RED.SameNameTwiceAndFiveHundredChars` |
| 11-08 | p eq | `NAV.BackDiscardsWithoutAsking`, `NAV.EscapeDoesNotCloseTheEditor`, `RED.BackDuringASlowSaveClosesAndTheSaveLands`; [Device-And] |
| 11-09 | p eq cap | `TXT.ContextMenuItemsByState`, `TXT.CutCopyPasteWithTheTestClipboard`, `TXT.TabSelectsAllClickPlacesTheCaret`, `TXT.DescriptionDoesNotSelectAll`, `TXT.NoMenuWithoutActions`; [Device-Win] (atajos, acentos, AltGr); CAP-26 |
| 11-10 | p eq | `KAV.FocusedFieldStaysAboveTheKeyboard` (también en un diálogo, a 16 dp), `TXT.MobileInputIsHidden`, `TXT.LongPressMenuInAppMenuMode` (alternativa de R3-13); [Device-And] NO VERIFICABLE sin módulo (menú del sistema con `hideMobileInput`, `TouchScreenKeyboard.area`) |

**F-RUT-12**

| AC | Verif. | Tests / capturas |
|---|---|---|
| 12-01 | d p | `RTT.AppendLooseGoesLastEvenAfterASeries`; `RED.AddStepAtTheEnd`, `RED.CancelAddsNothing` |
| 12-02 | d p cap | `SRM.TitleColorAndIcon` (título e ícono en `step.<tipo>.hud`; borde e insignia en el color base: DV-12 ampliada); `RED.StepRowContract` (borde 3, insignia 32 `@20 %`, `min-height` 56, lápiz y cruz de 20 en 48); CAP-10, CAP-12 |
| 12-03 | d p | `RTT.ReplaceKeepsSeriesAndCount`, `SFT.LooseStepKeepsHiddenProgressions`, `SFT.LooseStepHiddenProgressionsAreNotChecked`; `RED.EditInPlace` |
| 12-04 | d p | `RTT.RemoveAt`; `RED.DeleteStepAtOnce` |
| 12-05 | qa p | `RED.NoReorderOrMoveAffordances` |
| 12-06 | d p cap | `SST.BorderCasesTable` (las 11 filas); `RED.BorderSubtitlesOnScreen`; CAP-12 |

**F-RUT-13**

| AC | Verif. | Tests / capturas |
|---|---|---|
| 13-01 | d p cap | `SFT.NewFormDefaults`; `SDT.TitlesAndDefaults`; CAP-14 |
| 13-02 | p cap | `SDT.FieldsInOrder`, `SDT.ProgressionsOnlyInASeries`, `SDT.HintsWhenEmpty` (visibles sin foco, etiqueta arriba: DV-19); CAP-14, CAP-15, CAP-24 |
| 13-03 | d p | `StepTypeTests.SelectorOrder` (It. 1); `SDT.FourTypesOneSelected` |
| 13-04 | d p | `SFT.OnlyTheChosenModeCounts`; `SDT.ModeSwitchKeepsTheText` |
| 13-05 | d | `DIP.LegacyTable` (cada fila de la spec), `DIP.AnyLengthNeverOverflows`, `SFT.HugeNumbersAreOutOfRange`, `SFT.NonPositiveSplitIsNoSplitEvenPastTheCap` |
| 13-06 | d p | `SFT.ByTimeTable`; `SDT.EmptyTimeConfirmsZero` |
| 13-07 | d p | `SFT.ByDistanceTable`; `SDT.EmptyDistanceIsNoGoal` |
| 13-08 | d | `SFT.SplitTable`, `SFT.NonPositiveSplitIsNoSplitEvenPastTheCap` (`-200` y vacío; `-99999999999999999999` y `0`; `0` y `-6000`: sin split y sin error; QA-G3-13 a) |
| 13-09 | d | `SFT.NoSenseValidation` |
| 13-10 | d p cap | `SFT.PrefillTable` (las 8 filas, con −90 → −30 y sin meta → 0 s); `SDT.PrefillOnScreen`; CAP-15 |
| 13-11 | d p cap | `SLT.FieldCaps`, `SFT.FieldErrorsAndMaxTexts`, `SFT.HiddenModeFieldsAreNotChecked` (QA-G3-13 d); `SDT.RangeErrorKeepsTheDialogAndClearsOnChange`, `SDT.FocusGoesToTheFirstError` (con desplazamiento: DN3-10); CAP-20a |
| 13-12 | d p cap | `SLT.FirstRepetitionClosedForm` (1018 → rep 6; 9 990 + 10 → rep 2; +100 → acepta), `SLT.TwoBillionRepeatsWithoutLoops`, `SFT.RepetitionErrors`, `SFT.LooseStepProgressionsAreNotChecked`; `SDT.RepetitionErrorUnderTheProgression`; CAP-20b |
| 13-13 | p eq | `SDT.TabStaysInsideTheDialog`, `SDT.NumericFieldsUseSignedKeyboard`, `NAV.TabScrollsTheFocusedIntoView`; [Device-And] |
| 13-14 | p | `SDT.CancelOutsideEscapeAndBackChangeNothing`, `NAV.DialogButtonTwiceActsOnce` |
| 13-15 | p cap | `SDT.LowWindowScrollsAndKeepsTheButtons`, `NAV.TabScrollsTheFocusedIntoView` (640 × 360); CAP-23 |
| 13-16 | d p cap | `SFT.PreviewIsTheConfirmedRow`; `SDT.PreviewMatchesTheEditorRow`; CAP-14, CAP-15 |

**F-RUT-14**

| AC | Verif. | Tests / capturas |
|---|---|---|
| 14-01 | d p cap | `RTT.DefaultSeries` (dos toques, dos ids); `RED.SeriesButton`; DEMO-04 |
| 14-02 | p cap | `RED.SeriesBlockContract` (8 arriba y abajo; encabezado y pie de 48; riel continuo en encabezado, cuerpo y pie; sangría visible 16; hojas según N); CAP-10, CAP-11, CAP-12 |
| 14-03 | d p cap | `ELT.HeaderRepeatDefaultsToOne`, `ELT.ZeroRepeatShowsZero`; `RED.ChipTextAndTouchArea` (caja ≥ 48 × 48; número de la cápsula en `accent.onTonal`; la cápsula crece y no corta `×2000000000`); CAP-10, CAP-12 |
| 14-04 | p cap | `RPT.PrefillAndFocus` (escritorio: todo seleccionado; teléfono: cursor al final); CAP-17 |
| 14-05 | d p | `RCT.Table`, `RCT.HugeNegativeIsUnchanged` (`-99999999999999999999` → sin cambio); `RPT.Rules` |
| 14-06 | d p cap | `RCT.MaxAndProgressedTargets` (×491 sí, ×492 y ×999 no, bajar siempre); `RPT.OutOfRangeKeepsTheDialog`; CAP-21 |
| 14-07 | d p | `RTT.InsertIntoSeriesAfterItsLast` (con dos contiguas); `RED.AddStepToSeries` |
| 14-08 | d p | `RTT.ReplaceInSeries`; `RED.EditSeriesStep` |
| 14-09 | d p | `RTT.RemoveSeries`; `RED.DeleteSeriesAtOnce` |
| 14-10 | d p | `ELT.EmptiedSeriesDisappears`, `ELT.OneStepSeries`; `RED.SeriesEmptiesAndDisappears` |
| 14-11 | d p cap | `ELT.ContiguousSeriesAreTwoBlocks`, `RTT.RepeatChangeTouchesOnlyItsSeries`; `RED.ContiguousSeries` (16 ± 1 dp entre el pie y el encabezado); CAP-13 |
| 14-12 | qa p | `RED.NoReorderOrMoveAffordances` |

**F-HIS-17, F-HIS-20**

| AC | Verif. | Tests / capturas |
|---|---|---|
| HIS-17-01 | u qa eq | `DLT.WindowsUsesLocalLow`, `DLT.AndroidUsesTheDatabasesFolder`; [Device-Win]; [Device-And] |
| HIS-17-02 | d qa | `DSC.SchemaIsV8` (SQL al carácter, columnas, obligatorios, *defaults*, FK, índice, `user_version`), `DSC.WriteConnectionIsWalFullAndNoForeignKeys`, `DSC.SqliteVersionIsTheExpectedOne` (los tres también en Unity EditMode); QA con una herramienta de SQLite |
| HIS-17-03 | d qa eq | `LIT.NoCodeReachesTheLegacyData` (estático, QA-G3-14); [Device-Win]: base y preferencias del legacy con el mismo contenido y fecha |
| HIS-17-04 | d p | `DOT.ExistingV8IsUsedAsIs` (bytes iguales); `RPS.OpensAnExistingBase` |
| HIS-17-05 | d p | `DOT.NotADatabaseIsUntouched`, `DOT.ShortFilesAreUnreadableAndUntouched` (1 a 99 bytes, con y sin la firma), `DOT.NewerVersionIsUntouched`, `DOT.NewerVersionOnlyInTheWalIsUntouched`, `DOT.UnrecognizedIsUntouched` (v1-7 y v0 con tablas: DT-09), `DOT.ProblemsNeverCarryTheFolder`; `RLT.UnreadableBaseShowsTheError` |
| HIS-17-06 | d p eq | `SET.WorkRunsOffTheCallingThread`; `PRF.FiftyFourRoutinesUnderOneSecond`, `PRF.MainThreadWorkStaysUnderTheFrameBudget` (QA-G3-21); [Device-Win] (la fluidez a la vista) |
| HIS-20-01 | d dB qa eq | `DFT.NameByVariant`; `ITT.OnlyAppRootChoosesTheDatabaseFile`; [Device-Win] |
| HIS-20-02 | p eq | `RPS.VariantsDoNotMix` (el mismo nombre que §2.5); [Device-Win] |
| HIS-20-03 | ps cap | `RSL.SameScreensOnlyTheRibbon`; CAP-04 |
| HIS-20-04 | d dB p qa | `TDT.RefusesUserDataPaths`; `ITT.ScriptsIsolateTheDataFolder`, `ITT.Cap27RunsOnlyOnRequest`, `ITT.Cap27ProtectsTheUserDatabase` (la excepción de CAP-27 con sus condiciones), `ITT.Cap27SeedStepRunsForReal`, `ITT.Cap27RehearsalNeverTouchesTheUserFolder`, `ITT.WindowCaptureOnlyClicksAndCapturesItsOwnWindow`, `ITT.WindowCaptureStartsTheExeForReal`; `BOOT.UsesTheTestDataFolder`; QA antes/después, también de CAP-27 |

**F-APP-03 y "también toca"**

| AC | Verif. | Tests / capturas |
|---|---|---|
| APP-03-01 | p cap | `NAV.EditorCoversTheTabBar` (oculta, no tocable, fuera de Tab; al volver, barra y lista donde estaban), `RED.TapDuringTheExitCreatesNothing`, `RED.EnterDuringTheExitDoesNothing`, `NAV.BackSkipsWhatIsClosing`; CAP-09 |
| APP-03-02 | p eq | `NAV.BackClosesOnlyTheTop`, `NAV.EscapeClosesOnlyMenusAndDialogs`, `NAV.EscapeClosesOneLevelAtATime` (menú de texto sobre el diálogo de paso), `TXT.EscapeInAFieldKeepsTextAndFocus`; [Device-And] |
| APP-03-03 | p cap | `NAV.DialogsAreModal`, `NAV.ScrimCancels`, `NAV.TabIsTrappedInTheDialog`, `NAV.DialogButtonTwiceActsOnce`; CAP-08, CAP-14, CAP-17 |
| APP-03-04 | p cap | `NAV.MenuAboveEverythingButTheSimulator`, `NAV.MenuItemTwiceOpensOneDialog`; CAP-07 |
| APP-03-05 | d p ps cap | `BOT.PositionsOfTheDesignNotes`; `NAV.ToastFourSecondsAndReplaces`; `RSL.ToastAndWrenchDoNotOverlap` (editor y Rutinas, TV y W1280); CAP-18, CAP-19, CAP-28, VID-03 |
| APP-03-06 | p | `NAV.TooltipsOnHoverAndLongPress` (aparece al apoyar, 0,1 s, 1,5 s; solo los tres; la ayuda no tapa toques); lector de pantalla: NO VERIFICABLE, pasa a la It. 14 (DT-05, decidido) |
| APP-03-07 | ps cap | `RSL.SimulatorLayerAboveEverything`, `RSL.BackArrowUnderTheRibbonWorks`, `RSL.PanelWorksWithADialogOpen`; CAP-08, CAP-09, DEMO-02 |
| APP-03-08 | p cap | `NAV.TransitionsFollowTheBible`, `NAV.ReducedMotionFades`; VID-02 |
| APP-01-09 | d p | `ACT.StartLoadsRoutines`; `RLT.ListIsReadyBeforeTheFirstVisit` |
| APP-05-10 | u eq cap | `WindowsTitleBarTests` (It. 2, deuda; DD-01: ≤ 2 s o en el acto al volver el foco); [Device-Win]; CAP-27 (a pedido, §5.5) |
| APP-05-11 | p eq cap | `RLT.NoScrollbarOnPhone`, `RED.NoScrollbarOnPhone`, `SDT.NoScrollbarOnPhone`; [Device-And]; CAP-04, CAP-10 |

**§5 (iteraciones anteriores)**

| AC | Verif. | Tests / capturas |
|---|---|---|
| BLE-11-02 | p ps cap | `DeviceScreenTests.OutgoingConnectedViewIsFrozen`, `DeviceSimulatorTests.DisconnectKeepsTheNameWhileTheConnectedViewLeaves`, `.SimulatedDropKeepsTheName…`: **PASS** (00-deuda-it2 §1); VID-03 de la It. 2 regenerado |
| BLE-16-01 | ps cap | `RSL.SimulatorLayerAboveEverything`; CAP-08, CAP-09 |
| BLE-16-02 | d s ps cap | **cambia por DV-13** (texto nuevo de spec §5): `SPL.FabBottomAvoidsTheFab`, `SPL.PanelBottomNeverMoves`, `BOT.PositionsOfTheDesignNotes`; `SimulatorOverlayTests.WrenchSitsAboveTheFabInRoutines` (cargando, vacío, lista y error), `.WrenchStaysOnOtherTabsAndWithTheEditor`, `.WrenchMovesWithoutJumpsAndStaysTappable`, `.PanelOpensInTheSamePlaceInRoutines` (tapa el `+`; la llave no se ve), `.ReducedMotionMovesTheWrenchAtOnce`; CAP-01, CAP-06, CAP-09 |
| RUT-01-04 | p cap | `RED.StepRowIcons`; CAP-12 |
| RUT-01-05 | d p | `RRT.UnknownTypeFailsTheLoadWithoutWriting`; `RLT.UnknownTypeShowsTheLoadError` |
| RUT-04-06 | d p cap | `SST.EditorSeparatorHasTwoSpaces`; `RED.BorderSubtitlesOnScreen`; CAP-12 |
| APP-09-07 | p eq | `TXT.ContextMenuInTheAppLanguage`; [Device-Win]; [Device-And] NO VERIFICABLE (con la variante de P-06 si el sistema no da su menú: R3-13) |
| APP-02-08 | p cap | `RLT.RoutinesIsNotAPlaceholder`, `ShellTests` (Workout e Historial siguen provisorias); CAP-01 |
| APP-05-05 | p cap | `RLT.TextScale130`, `RED.TextScale130`, `SDT.TextScale130`; CAP-25 |
| APP-05-07 | p eq | `NAV.TabOrderAndActivation`, `NAV.TabScrollsTheFocusedIntoView`, `TXT.TabFromTheDescriptionMovesFocus`, `TXT.EnterInASingleLineFieldBlurs`, `TXT.EnterInADialogFieldDoesNotConfirm`; [Device-Win] |
| APP-05-08 | p | `NAV.ReducedMotionFades`, `RED.RowMotionReduced`, `RLT.StructureBarStaticWithReducedMotion` |

### 6.4 Conteos esperados (aproximados)

dotnet: +≈ 225 tests (repositorio y base ≈ 55, reglas del editor ≈ 120, *stores* ≈ 35, geometría y obstáculos ≈ 10, resto ≈ 5). PlayMode: +≈ 90 sin simulador y +≈ 17 con simulador. EditMode de Unity: +2 archivos (`DataLocationTests`, `RuntimePanelFactoryTests`; SQU pasa a `Tests/EditMode` como DSC). Arnés: 42 IDs (27 CAP sin CAP-27, 12 DEMO, 3 VID) = 47 archivos, porque CAP-12, -15, -20, -24 y -25 tienen dos tomas; CAP-27 sale de `capture-cap27.ps1`, a pedido. Matriz: `It03Ids` con los **102** IDs (91 nuevos y 11 de iteraciones anteriores que se vuelven a verificar, con AC-BLE-16-02), además de los 286 de la It. 2.

### 6.5 Lo que queda sin test automatizado

Teclado y menú del sistema en Android, "Atrás" real, almacenamiento privado (AC-RUT-11-10, AC-RUT-13-13, AC-HIS-17-01, AC-APP-03-02, AC-APP-09-07): [Device-And], NO VERIFICABLE sin módulo y teléfono. Cierre forzado (AC-RUT-08-07), legacy intacto en el equipo (AC-HIS-17-03; la parte estática la cubre `LIT`), atajos de teclado, acentos y AltGr (AC-RUT-11-09), barra de título en modo claro y en vivo (AC-APP-05-10), la fluidez a la vista (AC-HIS-17-06): [Device-Win]. IL2CPP: NO VERIFICABLE sin el módulo (el humo de §6.1 queda listo). Lector de pantalla: DT-05, decidido (It. 14).

**Rendimiento sin depender de la máquina** (QA-G3-21, R3-09): `PRF.MainThreadWorkStaysUnderTheFrameBudget` no mide cuadros, que en batchmode dependen del equipo. Mide con `Stopwatch` **el tiempo de nuestro código en el hilo principal** en cada cuadro mientras la base carga o guarda en el `SerialExecutor` real (`HostRoutines.Serial(Performance)`): continuación del *store*, *diff* y armado de la lista o del editor, y `Shell.Tick`. Descarta 3 repeticiones de calentamiento (JIT) y el umbral declarado es **16 ms por cuadro**, el presupuesto a 60 fps. Si se pasa, la lista se arma de a 8 tarjetas por cuadro (R3-09). El máximo medido va al registro (`[PRF] … ms`) para el informe; la sensación de fluidez se ve en [Device-Win]. `PRF.FiftyFourRoutinesUnderOneSecond` sigue midiendo el tiempo hasta ver la lista, con 1 s de tope.

---

## 7. Paquetes de trabajo

### 7.0 Orden y reglas

1. **WP-D0** (Designer, antes de WP-A y WP-B, chico).
2. **WP-A** (sin Unity): todo lo de dotnet. Termina con `run-tests.ps1` en verde, la matriz con los IDs de Unity en `PENDIENTE UNITY` y la compilación de `RowMateDatabase.cs` contra el `SQLite.cs` de gilzoide en `<scratch>` (§2.7).
3. **WP-B** (Unity), en tres entregas: **B1** capas (con `ExitLock`), navegación, lista, llave y avisos, base en la app; **B2** editor, diálogos, texto y teclado, `RuntimePanelFactory` y Noto Emoji; **B3** arnés, capturas, herramientas (`capture-cap27.ps1`), builds y documentos. El contrato de §7.5 y las comprobaciones de §5.4 ya incorporan 04-design-notes (Anexo B): B1 y B2 arrancan con todo decidido. Una sola instancia de Unity por vez.
   Antes de B2 corre la sonda **S-B3a** en el proyecto real y deja la evidencia en el informe. Mide trece cosas, y §4.4 y §4.10 ya dicen qué hacer con cada resultado:
   1. el subtítulo con ícono por fuente de respaldo, con la copia de `PanelTextSettings` de `RuntimePanelFactory`, y `Remo 🚣 ñ` con y sin Noto Emoji (DN3-21);
   2. `selectAllOnFocus` con foco por puntero;
   3. el clic derecho en *TrickleDown*;
   4. quitar `\n`, `\r` y `\t` al pegar;
   5. `hideMobileInput` (en el Editor, la API; el efecto, en [Device-And]);
   6. Tab en la descripción multilínea: ¿escribe `\t` o mueve el foco?;
   7. Escape con foco en un campo: ¿revierte el valor o suelta el foco?;
   8. Escape manejado una sola vez entre `AppRoot.Update` y `NavigationCancelEvent`;
   9. Enter en un campo de una línea;
   10. si el cursor parpadea, con qué reloj, y las transiciones del tema por defecto en batchmode;
   11. `enableRichText = false` y `parseEscapeSequences = false` en un `Label` y en el `TextElement` del `TextField`, con `<b>A</b>` y `Serie\n2`;
   12. `white-space`: si `normal` junta dos espacios seguidos o un `\n` (por eso el texto del usuario y el subtítulo van con `pre-wrap`) y si una palabra de 200 caracteres sin espacios se parte;
   13. que `FocusInEvent` burbujee desde un hijo del `ScrollView` (QA-G3-10, -11, -13).
4. **WP-D** (Designer): aspecto sobre las clases de §7.5, iterando con el arnés.

Cada archivo tiene un solo dueño. Si alguien necesita cambiar el de otro, lo pide.

### 7.1 WP-D0 — glifos, tokens, contraste y fuente de emojis (Designer)

| Archivo | Qué |
|---|---|
| `UI/Icons/RowMateIcons-Outline.ttf` | + `arrow_back` (U+E5C4) y `more_vert` (U+E5D4), receta de ASSET_SLOTS §4.1 (`uvx --from fonttools pyftsubset …`: `uvx` ya está, no hace falta instalar Python; QA-G3-18), mismo commit de Material Symbols |
| `Core/Design/Icons.cs`, `Tests/EditMode/Design/IconsTests.cs` | `ArrowBack`, `MoreVert`; 42 → 44 glifos de contorno |
| `Core/Design/Tokens.cs`, `Presentation/Styles/Tokens.uss` (G), `Presentation/Styles/Typography.uss` (G), `Tests/EditMode/Design/{TokenCatalogTests,TokensContractTests,TokensUssSyncTests,TypographyUssSyncTests}.cs` | `overlay.scrim` y los cinco de 04-design-notes §2.1 (DN3-01): **127 → 133 tokens**; `.rm-type-title-xs`, `.rm-type-header-s`, `.rm-type-button-s` |
| `Tests/EditMode/Design/ContrastTests.cs` | `ItThreeDesignerPairs` (R1-R27 de 04-design-notes §8.1) |
| `UI/Fonts/NotoEmoji/NotoEmoji-Regular.ttf`, `OFL.txt` | Noto Emoji monocroma estática (P-09, DN3-21), con su licencia |
| `docs/migration/design/ASSET_SLOTS.md` §4.3 | la tabla de íconos y el slot `ui.font.emoji` |

### 7.2 WP-A — lógica, base y tests de dotnet (≈ 80 archivos)

Core (`DartText`, `DartIntParser`, `Core/Routines/*` con `StructureBarLayout`, `Core/Design/BottomObstacles`, `Ports/{IRoutineRepository,IClipboard,ISoftKeyboard}`, `MotionSpecs` con las constantes de 04-design-notes §5.11, `UiTimings`, `L10nKeys`); catálogos `strings.es.json` y `strings.en.json` (16 claves, 190); App (`App/Routines/*`, composición); Infrastructure entera (asmdef, `link.xml`, `Threading`, `Database` con `WritePragmas` y `QueryRawForTestsAsync`, `Routines`, `Diagnostics`); `Simulator/Panel/SimulatorPanelLayout.cs`; `Tests/Support/*` nuevos y `RecordingLogger.cs`; `Tests/EditMode/**` de §6.2 (salvo las excepciones de §1.1) y `Tests/Simulator/SimulatorPanelLayoutTests.cs`; `tools/dotnet/RowMate.Infrastructure.csproj`, `run-tests.ps1`.

### 7.3 WP-B — UI, composición, arnés, herramientas y tests de Unity (≈ 75 archivos)

Platform (`Storage`, `Input/{SystemClipboard,SoftKeyboard}`, servicios); Presentation (`Shell/*` nuevos y cambiados con `ExitLock`, `Controls/*` con `FocusScroller`, `Surface/RuntimePanelFactory.cs`, `Routines/*`, `Motion/*`, `UiContext`, `Base.uss`); `SimulatorPresentation/SimulatorLayerView.cs`; Bootstrap (`DataLocation`, `AppRoot`, `ShellHost*`, `UiAssets`); `Editor/Build/BuildOutputInspector.cs`; `Editor/Bootstrap/{UiAssetsBuilder,FontAssetBuilder,ProjectBootstrapper}.cs`, `Core/Design/FontAssetPaths.cs` y `UI/Fonts/Generated/NotoEmoji-Regular-SDF.asset` (B2); `Tests/UiSupport/*`; `Tests/PlayMode*/*` y `Tests/UnityEditMode/*` de §6.2; `Tests/EditMode/Project/{ItThreeToolingTests,ItTwoDebtTests}.cs`; `Snapshots/*`; `tools/unity/*` (con `capture-cap27.ps1`), `tools/build/build.ps1` (humo); capturas de `03-rutinas/snapshots/`. Al cerrar: ARCHITECTURE (§2, §6, §7.2, §8, §9.1, "Cambios de la It. 3"), ADR 007 y 008 (sección "Cambios de la It. 3") y la corrección de 02-tech-design de la It. 2 sobre `PanelSettings.textSettings` (como nota en ARCHITECTURE, sin editar el documento cerrado).

### 7.4 WP-D — pulido visual (Designer)

`Skin.uss`, `Motion.uss`, `SimulatorSkin.uss`; sondas de aspecto en `RoutinesCatalog` (las agrega WP-B en B3 con los valores de 04-design-notes §7.2); `04-design-notes.md`; ART_BIBLE, ANIMATION_BIBLE y LEGACY_VISUAL_AUDIT (04-design-notes §12, "Cambios de la It. 3"). La fuente Noto Emoji ya entra en WP-D0 y su `FontAsset` en B2: WP-D solo da el visto bueno de cómo se ve (04-design-notes §4.15).

### 7.5 Contrato USS (WP-B en `Base.uss` o por código; WP-D no lo cambia)

Siguen las cinco reglas de la It. 2 (02-tech-design §3.12): sin colores literales, nadie define tamaño ni fuente en un elemento con `.rm-type-*`, Skin y Motion no tocan el contrato, las clases de estado las pone el código y lo que va en línea por código manda.

| Elemento | Contrato | Fuente |
|---|---|---|
Al día con 04-design-notes (Anexo B; QA-G3-02). Lo que cambió respecto de la primera versión va marcado **[DN3-nn]**.

| Elemento | Contrato | Fuente |
|---|---|---|
| `#page-stack`, `.rm-page` | la página ocupa todo el panel, fondo `--rm-bg-app`, recibe eventos; lo de atrás en `display: none` al terminar la entrada (por código); la que sale, inerte entera (`ExitLock`) | AC-APP-03-01; QA-G3-01 |
| `.rm-page-stack__dim`, `.rm-dialog__scrim` | `--rm-overlay-scrim`; opacidad **por código** (0 → 0,08 / 0 → 0,6) | ANIMATION_BIBLE §2.2 |
| `.rm-dialog` | centrado; título, contenido desplazable y acciones abajo a la derecha (secundaria de texto, principal rellena), fuera del desplazamiento; ancho `min(máx, ventana − 80)` con mínimo 280 y máximo **400** (eliminar, repeticiones) o **560** (paso), y alto máximo `ventana − 48 − área segura`, por código **[DN3-10]** | AC-APP-03-03, AC-RUT-13-15 |
| `.rm-dialog--compact`, `.rm-dialog__pinned`, `.rm-dialog__content--scrolls` | clases que pone el código: ventana < 480 dp de alto; vista previa fija si el alto máximo ≥ 560; contenido que desborda **[DN3-10]** | AC-RUT-13-15, -16 |
| `.rm-button--danger-fill` | fondo `--rm-state-danger-fill` (`#D32F2F`), texto `--rm-text-primary` | AC-RUT-10-07 |
| `.rm-menu`, `.rm-menu__item--danger`, `.rm-menu--dense` | ítem de peligro: texto `--rm-state-danger-text` y su capa de estado con `--rm-overlay-scrim`; `--dense` en el menú de texto; anillo de foco interior **[DN3-11]** | AC-RUT-10-06, AC-RUT-11-09 |
| `#popup-layer`, `#toast-layer` | orden de árbol de §4.1; posición del menú, la ayuda y el aviso por código (§4.6, §4.7) | AC-APP-03-04, -05; DN3-12, DN3-13 |
| `.rm-tooltip` | ella y todo su árbol con `pickingMode = Ignore`; ancho máximo 240 | AC-APP-03-06; QA-G3-01 |
| `.rm-fab` (`#routines-add`) | círculo 56, `--rm-accent-primary`, ícono `add` 24 `--rm-text-on-accent`; posición por código (right 16, bottom = barra + 16) | AC-RUT-10-01, -04 |
| `.rm-empty` de Rutinas | ícono 56 opacidad **0,12**; título `--rm-text-secondary`; ayuda 13 (`.rm-type-body-s`) `--rm-text-tertiary`; 12 y 8 de separación; centrado entre las dos barras | AC-RUT-10-02 (DV-11) |
| `#routines-loading` | solo el indicador: `Spinner` 32, trazo 3, `--rm-accent-primary`; gracia de 120 y fundido de 160 por código; centrado entre las dos barras **[DN3-15]** | AC-RUT-10-02 |
| `#routines-error`, `#routines-error-icon` | ícono `error` 48 `--rm-state-danger-text` → 12 → texto → 16 → `#routines-retry` **[DN3-15]** | AC-RUT-09-04 |
| `.rm-routine-card` | fondo `--rm-surface-card` (`#1A2E45`), **sin** borde superior (no es `.rm-card`); `.rm-routine-card__icon` caja 36 `--rm-accent-tonal-bg` con el glifo 22 `--rm-accent-primary`; `.rm-highlight` (capa absoluta, `pickingMode Ignore`, opacidad por código); nombre y descripción `white-space: pre-wrap`, con `enableRichText` y `parseEscapeSequences` en `false` por código **[DN3-06, DN3-22]** | AC-RUT-10-03 |
| `.rm-structure-bar` | un renglón de 4; tramos alineados a la izquierda en el color base de su tipo, ancho, desborde y pista vacía por código (`StructureBarLayout`) **[DN3-07]** | AC-RUT-10-11 |
| `.rm-step-row`, `--<tipo>` | fondo `--rm-surface-card`; borde izquierdo 3 dp del color **base** del tipo; margen vertical 3; `min-height` 56; `--in-series`: `margin-left` **13** (con el riel de 3, la sangría visible es 16) **[DN3-05]** | AC-RUT-12-02, AC-RUT-14-02 |
| `.rm-step-row__badge-bg` | color base del tipo `@20 %` (por código) | AC-RUT-12-02 |
| `.rm-step-row__title` e ícono de la insignia | `step.<tipo>.hud` en los cuatro tipos (trabajo `#FF5D7F`, enfriamiento `#37A2CB`); resuelve DT-19 **[DN3-03]** | AC-RUT-12-02 (DV-12 ampliada) |
| `.rm-step-row__subtitle` | ajusta, nunca "…", `white-space: pre-wrap`; glifo `trending_up` en `metric.distance` con el texto enriquecido que arma el código | AC-RUT-12-06 (DV-10) |
| `#step-edit-*`, `#step-delete-*`, `#series-delete-*` | `.rm-icon-button` (48) con ícono 20 por parámetro; lápiz `--rm-text-secondary`; cruz y tacho `--rm-state-danger-text` **[DN3-17]** | AC-RUT-12-02, AC-RUT-14-02 (DV-12) |
| `.rm-series` | 8 dp arriba y abajo (16 entre dos series contiguas) | AC-RUT-14-02, -11 |
| `.rm-series__header`, `.rm-series__steps`, `.rm-series__footer` | fondos `brand.seed @12 %` / `@6 %` / `@6 %` (por código); borde izquierdo 3 `--rm-brand-seed` **continuo en los tres**; radios superiores del encabezado e inferiores del pie 12; encabezado y pie `min-height` 48; título `editorSeries` y botón del pie en `--rm-accent-primary` **[DN3-05]** | AC-RUT-14-02 |
| `.rm-series__stack` > `.rm-series__stack-layer` × 2 | absoluta bajo el pie (dentro de los 8 de aire), `pickingMode Ignore`; visibles según N (×2 una, ×3 o más dos); colores por código **[DN3-05]** | AC-RUT-14-02 |
| `.rm-chip--repeat` > `.rm-chip__pill` | caja táctil transparente ≥ 48 × 48, sin el borde ni el `min-height` de `.rm-chip`; la cápsula (alto 28, radio 14, `min-width` 40) lleva el fondo `accent.primary @20 %` por código, la capa de estado, el número y el anillo **[DN3-04]** | AC-RUT-14-03 |
| `.rm-field` y `--focused`, `--filled`, `--error`, `--multiline`, `--floating` | borde de `.rm-field__box`: reposo `--rm-border-input`, foco 2 `--rm-accent-primary`, error 2 `--rm-state-danger`; `.rm-field__error` `--rm-state-danger-text`; `.rm-field__label` adentro de la caja, con escala 0,8 por código al flotar; sin `.rm-focus-ring`; `min-height` (nunca `height` fijo); `transition-duration: 0s` en lo que anima el tema por defecto **[DN3-09]** | AC-RUT-11-02, AC-RUT-13-11 |
| `.rm-field__input` (descripción) | alto de 2 líneas | AC-RUT-11-02 |
| `.rm-segmented`, `.rm-segmented--tiles` | tiempo/distancia con la variante por defecto; tipo en mosaicos (ícono arriba, nombre abajo), 2 × 2 o 4 en fila según el contenido ≥ 400; fondo y borde por opción por código **[DN3-08]** | AC-RUT-13-03 (DV-09) |
| `#step-preview` | `.rm-step-row--preview` (sin acciones) | AC-RUT-13-16 |
| `.rm-toast` | ancho y posición por código (§4.7) **[DN3-13]** | AC-APP-03-05 |

**Clase de texto y color por elemento** (la tabla de 04-design-notes §2.2, que reemplaza a la de la primera versión; DN3-02):

| Elemento | Clase | Color |
|---|---|---|
| `.rm-routine-card__name` | `.rm-type-title-s` (16/600) | `text.primary` |
| `.rm-routine-card__description` | `.rm-type-body-xs` (12) | `text.secondary` |
| `.rm-routine-card__summary` | `.rm-type-body-s` (13) | `text.tertiary` |
| `.rm-empty__title` / `.rm-empty__hint` (Rutinas) | `.rm-type-body` / `.rm-type-body-s` | `text.secondary` / `text.tertiary` |
| `#routines-error-text` | `.rm-type-body` | `text.secondary` |
| `#editor-steps-count` | `.rm-type-title-xs` (14/600) | `text.primary` |
| `.rm-editor__empty` | `.rm-type-body-m` | `text.tertiary` |
| `.rm-step-row__title` / `__subtitle` | `.rm-type-title-xs` / `.rm-type-body-xs` | `step.<tipo>.hud` / `text.secondary` (glifo en `metric.distance`) |
| `.rm-series__title` | `.rm-type-header-s` (13/700) | `accent.primary` |
| `.rm-chip__pill` (número) | `.rm-type-num-stat` (Barlow 16/700) | `accent.onTonal` |
| `#series-add-step-<k>` (etiqueta) | `.rm-type-button-s` (12/600) | `accent.primary` |
| `.rm-field__input` / `.rm-field__label` / `.rm-field__error` | `.rm-type-body` / `.rm-type-body` con escala 0,8 al flotar / `.rm-type-body-xs` | `text.primary` / `text.secondary` (foco `accent.primary`, error `state.dangerText`) / `state.dangerText` |
| Ayuda (`placeholder`) | la del campo | `text.tertiary` |
| Rótulos `editorOptionalTargets`, `editorProgressions` | `.rm-type-body-xs` | `text.tertiary` |
| `.rm-segmented__option` (tiempo / distancia) | `.rm-type-button` | `text.secondary`; elegida `text.primary` |
| Mosaico de tipo (nombre) | `.rm-type-button-s` | `text.secondary`; elegido `text.primary` |
| `.rm-dialog__title` | `.rm-type-title` | `text.primary` |
| Contenido del diálogo de eliminar | `.rm-type-body` | `text.secondary` |
| `.rm-toast__text`, `.rm-menu__item` | `.rm-type-body-m` | `text.primary`; ítem de peligro `state.dangerText` |
| `.rm-tooltip` | `.rm-type-body-xs` | `text.primary` |

**Elementos que crea WP-B para WP-D:** `.rm-field__box`, `.rm-field__label`, `.rm-segmented`, `.rm-segmented--tiles`, `.rm-segmented__option` (`--selected` por código), `.rm-menu`, `.rm-menu--dense`, `.rm-popup-catcher`, `.rm-tooltip`, `.rm-toast`, `.rm-dialog` (con `--compact`, `__pinned`, `__content--scrolls`), `.rm-page-stack__dim`, `.rm-fab` (capa de estado y anillo de foco como `RmButton`), `.rm-routine-card` (capa de estado, `.rm-highlight`, anillo), `.rm-routine-card__icon` (la caja), `.rm-chip__pill`, `.rm-series__steps`, `.rm-series__stack` y sus capas, `#routines-error-icon`, `.rm-divider`.

**Lo que es de WP-D:** radios, rellenos y separaciones internas fuera de la tabla; *hover*, presionado y foco (`Motion.uss` de 04-design-notes §5.9, siempre bajo `:hover`); aspecto de campos (fondo `bg.sunken`, borde, etiqueta), segmentos, mosaicos, cápsula, riel, hojas, menú, ayuda, aviso y diálogo; la llave en `SimulatorSkin.uss` si hace falta. El tamaño del indicador de carga, la forma de la mini-barra con valores imposibles y la ubicación de la llave ya no son de WP-D: quedaron decididos (DN3-15, DN3-07, DN3-14) y son contrato.

---

## 8. Riesgos y desvíos

### 8.1 Desvíos propuestos (con lo que decidió el gate)

| ID | Qué | Por qué | Decide |
|---|---|---|---|
| DT-01 | En el editor, campos y fila de `Pasos` se desplazan con la lista (un solo `ScrollView`); en el legacy quedan fijos | A 640 × 360 con letra 1,3× o con el teclado abierto, la parte fija no entra y la lista queda sin lugar; así nada desborda (AC-APP-05-05, CAP-22) | **Decidido**: el PO lo aceptó como **DV-18** (spec §11.1) |
| DT-02 | Las ayudas de progresión (`Ej: 2`…) se ven con el campo vacío aunque no tenga foco (lo que piden AC-RUT-13-02 y CAP-15b). En el legacy solo se ven con foco (`input_decorator.dart:2346`: `isEmpty && !_hasInlineLabel`) | Se sigue la spec | **Decidido**: el PO lo aceptó como **DV-19** (spec §11.1) |
| DT-03 | DV-13: la llave se aparta 16 dp por encima del `+` solo mientras se ve Rutinas; en el resto, donde dice AC-BLE-16-02. El panel sigue anclado como en AC-BLE-16-03 | DV-13 choca con AC-BLE-16-02 ("a 16 dp sobre la barra"): cualquier ubicación fija cambia esa AC en todas las pestañas | **Decidido**: el Designer eligió barra + 88 (04-design-notes §4.13) y el PO reescribió AC-BLE-16-02 en spec §5 (QA-G3-05); AC-BLE-16-03 no cambia |
| DT-04 | Escape cierra menús y diálogos, nunca páginas; "Atrás" de Android además cierra la página | P-08; se alinean ARCHITECTURE §8 y ADR 007 | — (P-08, PO) |
| DT-05 | El texto de la ayuda se guarda como nombre accesible, pero UI Toolkit 6000.3 no expone los elementos de un panel de runtime a los lectores de pantalla del sistema | No hay puente en el motor; hacerlo (Android/iOS con `AccessibilityHierarchy`) es trabajo propio. AC-APP-03-06 queda PASS en lo visible y NO VERIFICABLE en el lector | **Decidido** (PO): pasa a la It. 14 (spec §11, §13) |
| DT-06 | Atajos de teclado y portapapeles: los de UI Toolkit; el menú propio usa `IClipboard` (el mismo portapapeles del sistema). Se verifican en [Device-Win]; en PlayMode, solo lo que se puede simular | Ninguna prueba toca el portapapeles real del usuario | **Aceptado** (QA, gate §9) |
| DT-07 | `synchronous = FULL` (ARCHITECTURE §9.1 decía `NORMAL`) | H-05 "aunque se corte la luz": con WAL y `NORMAL` un *commit* puede perderse ante un corte; con `FULL` no. Costo: un `fsync` por guardado | — |
| DT-08 | `created_at` con milisegundos fijos (`.fffZ`) y orden `created_at DESC, id DESC` | El formato de Dart omite los microsegundos en 0 y deja largos distintos: el orden del texto dejaría de ser el del tiempo | — |
| DT-09 | Una base con `user_version` 1 a 7, o 0 con tablas, se trata como F-ROTA (error, no se toca) | La spec nombra solo "> 8"; la app nunca crea esas versiones y no hay importación (Q-11) | **Confirmado** (PO): AC-HIS-17-05 |
| DT-10 | Un entero de la base que no entra en 32 bits (solo a mano) hace fallar la carga, como un tipo desconocido | El modelo es `int?` (It. 1); Dart lee 64 bits. Ninguna base de la app lo tiene | **Confirmado** (PO): AC-RUT-08-08 |
| DT-11 | Hexadecimal positivo más allá de 64 bits (`0xFFFFFFFFFFFFFFFF`): Dart da −1 (lo lee como u64); acá es "fuera de rango" | Se usa el valor matemático; siempre está por encima de cualquier tope | — |
| DT-12 | CAP-27 se toma con la base real del build normal reemplazada y restaurada al terminar | El build normal no acepta otra carpeta (solo desarrollo, QA-V2-09); así la toma muestra una rutina sin cambiar nada del usuario | **Aceptado con condiciones** (QA-G3-06), que §5.5 cumple una por una; el PO agregó la excepción en AC-HIS-20-04, §0.4, §7.1 y §12.1 |
| DT-13 | Al guardar, una serie ×0 (o con repeticiones negativas, F-RARA) no cuenta como "fuera de rango"; ×1000 o más sí | ×0 se muestra (paridad, AC-RUT-14-03) y no aporta pasos; no rompe nada. Lo que traba la expansión son las repeticiones grandes | **Confirmado** (PO): AC-RUT-11-05 |
| DT-14 | Las operaciones de serie van por `groupId`, como el legacy | Igual a "por bloque" con ids únicos; con un id repetido (solo a mano) cambian juntas, como en el legacy | — |
| DT-15 | Menú `⋮`, menú de texto y ayudas propios (no `GenericDropdownMenu` ni `tooltip`) | S3-b: `GenericDropdownMenu` se cuelga de la raíz del panel (encima del simulador); `tooltip` es solo del Editor; no hay menú contextual en runtime | — |
| DT-16 | dotnet corre SQLite 3.49.1 y Unity 3.53.4 | Mismo SQL (básico); las mismas pruebas corren también en Unity EditMode | — |
| DT-17 | Con un campo de texto con foco, la app pide 60 fps | ARCHITECTURE §13.1 baja a 30 fps en reposo; escribir a 30 fps se nota | — |
| DT-18 | Un segundo `Push` mientras entra otra página se ignora | Evita dos editores con un doble toque en `+`; sin cambio visible en uso normal | — |
| DT-19 | Título del enfriamiento: DV-12 dice `#35A0C9`, pero `step.cooldown.hud` pasó a `#37A2CB` con QA-V2-12 | Se usa el token | **Resuelto**: el Designer pasó título e ícono de los cuatro tipos a `step.<tipo>.hud` (DN3-03) y el PO amplió DV-12 (QA-G3-03) |
| DT-20 | ART_BIBLE §3.10 pide tarjetas *skeleton* al cargar; AC-RUT-10-02 pide el indicador del legacy | Manda la spec | **Decidido** (Designer): indicador de 32 con gracia de 120; los *skeleton* salen de la biblia en WP-D (04-design-notes §1, §12) |
| DT-21 | Mientras un diálogo o un menú salen (150 / 90 ms), su velo y su atrapador **se tragan** los toques sin cancelar nada. En Flutter, en cambio, el velo de una ruta que sale deja pasar el toque a lo de atrás (`<flutter-sdk>/packages/flutter/lib/src/widgets/routes.dart:2278-2281`). La página que sale sí deja pasar los toques, como Flutter | Un segundo toque rápido en `Confirmar` caería sobre el editor de atrás, en el lugar de una cruz o un tacho, y borraría un paso sin querer. Un segundo toque en `Eliminar` del menú caería sobre el velo del diálogo que entra y lo cancelaría. AC-APP-03-03 ya dice "mientras hay uno abierto, lo de atrás no responde". Diferencia de 150 ms, sin efecto visible | PO (informativo) |

Corrección para el registro: 02-tech-design de la It. 2 (§3, hallazgos de S-0) dice que `PanelSettings` no tiene `textSettings` en 6.3. Es un **campo** público y existe (S3-b). Se anota en ARCHITECTURE al cerrar (el documento de la It. 2 no se edita).

### 8.2 Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R3-01 | IL2CPP + SQLite nativo: el mapeo de filas va por reflexión y el *stripping* podría sacar los setters | Atributos del paquete con `Preserve`, `link.xml`, `DatabaseSmoke` en un *player* de desarrollo (§6.1). Sin el módulo IL2CPP: NO VERIFICABLE (`build.ps1` sale con 3); Mono sí se prueba |
| R3-02 | Android: ruta privada por JNI, `.so` arm64, teclado y "Atrás" | Código aislado en `AppDataLocation` y `SoftKeyboard`; [Device-And], NO VERIFICABLE sin módulo y teléfono |
| R3-03 | Hilos: una continuación en el hilo de la base, un hilo vivo después de una recarga de dominio del Editor, una continuación que llega después de `Dispose` o un registro que se pisa entre hilos | `RunContinuationsAsynchronously`, prohibido `ConfigureAwait(false)` (test), `SerialExecutor` *background* con cierre en `OnDestroy` y en `DomainUnload` (y baja de la suscripción en `Dispose`), `_disposed` en *stores*, páginas y capas, `RecordingLogger` con `lock`, `SerialExecutorTests` (QA-G3-09) |
| R3-04 | Ícono en el subtítulo por fuente de respaldo (línea de base, tamaño, color) | S-B3a antes de B2; respaldo de tres piezas; los tests miran `Text` igual en los dos caminos |
| R3-05 | `selectAllOnFocus` también podría seleccionar todo con un clic | S-B3a; si pasa, `RmTextField` deja el cursor donde se hizo clic al soltar el puntero |
| R3-06 | Kerning de glifos nuevos (`ñ`, `¿`, `¡`, `×`, `±`) en `FontAsset` dinámicos (00-deuda-it2 §3) | El Designer decide si genera la tabla para un juego fijo dentro de `AllowWrites` (QA-G3-18, del Designer); las capturas son deterministas igual (mismo orden) |
| R3-07 | Emojis en nombres: la fuente no los tiene | Se guardan tal cual (AC-RUT-08-01); respaldo Noto Emoji monocromo (P-09, DN3-21), protegido como los otros `FontAsset`; sin él, `□` y el texto que lo rodea no se corre |
| R3-08 | La It. 4a expande la rutina al iniciar: una base tocada a mano con ×2 000 000 000 agotaría la memoria | `StepLimitCheck.TemplateIsWithinLimits` queda en Core para que la 4a lo use antes de `Flatten` (anotado para su diseño) |
| R3-09 | Armar 54 tarjetas o un editor de 60 filas en un cuadro | Medido en `PRF` con `Stopwatch` sobre nuestro código en el hilo principal, umbral de 16 ms (§6.5, QA-G3-21); si se pasa, la lista se arma de a 8 tarjetas por cuadro |
| R3-10 | Pruebas que tocan el portapapeles o la carpeta real del usuario | `FakeClipboard`; `TempDatabase` con guardia; `ROWMATE_DATA_DIR`; CAP-27 solo a pedido y con resguardo verificado (§5.5); QA compara antes y después |
| R3-11 | La restauración de NuGet necesita red | `ci-core` tiene red; en la PC se restaura una vez |
| R3-12 | La trampa de foco depende de `FocusController.IgnoreEvent` | API confirmada (S3-b); `NAV.TabIsTrappedInTheDialog` |
| R3-13 | Android (QA-G3-16): con `hideMobileInput = true`, el sistema probablemente no muestra su menú de seleccionar, copiar y pegar sobre un campo de UI Toolkit (AC-RUT-11-10, P-06), y no está probado que `TouchScreenKeyboard.area` sea confiable | Alternativa lista (§4.10): `TextContextMenu` en modo táctil abre el menú de la PC al mantener presionado, con los textos del catálogo (la variante de P-06 que el PO ya escribió, spec §14); el alto del teclado por JNI si `.area` falla. Se decide y se verifica en [Device-And] (NO VERIFICABLE sin módulo y teléfono) |
| R3-14 | Los dos SQLite compilan con opciones distintas (WAL en `NORMAL` y sin comillas dobles en Unity; `FULL` y comillas dobles en dotnet): dotnet en verde no garantiza Unity | Pragmas explícitos y leídos de vuelta, literales con comillas simples, y DSC, DOT y RRT corren también en Unity EditMode (§2.1, QA-G3-08) |
| R3-15 | El `TextField` de runtime puede escribir `\t` con Tab, revertir con Escape o animar con el tema por defecto, distinto de Flutter (QA-G3-10) | Los trece puntos de S-B3a (§7.0), con la respuesta ya decidida para cada resultado (§4.10) |

### 8.3 Pedidos al Designer (respondidos)

Los cinco pedidos de la primera versión (DV-13 y la posición del aviso, la mini-barra con valores imposibles, el título del enfriamiento y las clases de texto, los campos, el indicador de carga y ART_BIBLE §3.10) están respondidos en 04-design-notes §1. Lo que esas respuestas cambian en este diseño, y los otros pedidos del Designer (DN3-01 a DN3-23), está en el Anexo B. Queda del Designer: QA-G3-18 (el kerning de R3-06, y `uvx` en lugar de Python, que §7.1 ya usa).

### 8.4 Para otros roles

| Para | Qué |
|---|---|
| PO | DT-01 (DV-18), -02 (DV-19), -03, -05, -09, -10, -13 y -19: **decididos** en el gate (spec §16). Queda informativo DT-21 (lo que sale se traga los toques en diálogos y menús); la variante de P-06 para Android se decide con el teléfono (R3-13); los pendientes de spec §15 siguen siendo suyos |
| QA | DT-06 y DT-12 aceptados (DT-12 con las condiciones de §5.5); comparación de AC-HIS-20-04 antes y después de **todas** las corridas (incluido CAP-27); esquema con una herramienta de SQLite; los resultados de S-B3a (§7.0) en el informe de B2 |
| Designer | QA-G3-18 (kerning de R3-06); la fuente Noto Emoji pasa a WP-D0 (§7.1) para que su `FontAsset` entre en B2 |
| Coordinador | La restauración de NuGet agrega `sqlite-net-pcl` y `SQLitePCLRaw.*` al caché local la primera vez. CAP-27 se corre solo si el usuario lo pide, con la app y Unity cerrados (§5.5) |

---

## Anexo A. Sondas (evidencia)

### A.1 S3-a — el mismo código de datos contra las dos librerías (dotnet)

Un `Probe.cs` compartido, en `<scratch>/it3/arch-s3/`, compilado en dos proyectos `net8.0`: uno con `sqlite-net-pcl` 1.9.172 (paquetes restaurados en el scratch) y otro con el `SQLite.cs` de `com.gilzoide.sqlite-net` 1.3.3 copiado del caché de paquetes, más su `gilzoide-sqlite-net.dll` de Windows x64 y un *stub* de `UnityEngine.Scripting.PreserveAttribute`. Los dos compilaron sin advertencias y dieron **22 de 22** (agrupadas en 7 casos):

| # | Comprobación | sqlite-net-pcl (SQLite 3.39.2; 3.49.1 con `bundle_green` 2.1.11) | gilzoide (SQLite 3.53.4) |
|---|---|---|---|
| 1 | Creación en rollback, DDL + `user_version = 8` en una transacción, después `journal_mode = WAL` → `wal`; `origin TEXT NOT NULL DEFAULT 'app'`; FK `routine_id → routines.id` `CASCADE`; 4 tablas + `sqlite_sequence`; 1 índice | ok | ok |
| 2 | Texto `Pirámide ñ ¿¡ 📈` y `Sube 2 spm\n2.ª vuelta` al carácter; `long?` distingue 0 de NULL; ×2 000 000 000 leído; orden `created_at DESC, id DESC` | ok | ok |
| 3 | Encabezado: `SQLite format 3\0`, offset 60 = 8, bytes 18/19 = 2 (WAL) | ok | ok |
| 4 | Sonda de solo lectura y apertura en escritura **solo leyendo**: mismos bytes, sin archivos nuevos | ok | ok |
| 5 | F-ROTA (texto): `SQLiteException` `NonDBFile` (26) "file is not a database"; mismos bytes; sin `-wal` ni `-shm` | ok | ok |
| 6 | Base "más nueva" copiada con su `-wal` sin *checkpoint*: el **encabezado dice 8** y la sonda de solo lectura lee **9** desde el WAL; principal y `-wal` con los mismos bytes después | ok | ok |
| 7 | *Trigger* `RAISE(ABORT)` en el segundo paso: `SQLiteException` `Constraint` "falla inyectada"; rutinas y pasos como antes | ok | ok |

Conclusiones que usa el diseño: el código de acceso compila igual en las dos librerías (§1.2); hace falta la sonda de solo lectura además del encabezado (§2.3, caso 6); la falla a la mitad se prueba con un *trigger* (§2.7); conviene fijar `bundle_green` 2.1.11 (§2.7).

### A.2 S3-b — UI Toolkit en un panel de runtime (Unity 6000.3.27f1, batchmode)

Proyecto descartable en el scratch (solo módulos de UI), `-executeMethod` que arma un `UIDocument` con una copia de `PanelSettings` sobre una RT, un `TextField` de una línea y uno multilínea. Resultados (`[S3B]` del registro):

| Pregunta | Resultado | Uso en el diseño |
|---|---|---|
| Tipo de panel | `RuntimePanel`, `contextType = Player` | — |
| ¿Menú contextual de UI Toolkit? | `contextualMenuManager = null` | menú del clic derecho propio (§4.10) |
| Valores por defecto de selección | `selectAllOnFocus = True`, `selectAllOnMouseUp = True`, también en multilínea; `hideMobileInput = False`; `keyboardType = Default` | tabla de §4.10 |
| `Focus()` por código | selecciona todo (cursor 0, selección 12) | Tab selecciona todo en una línea |
| `SelectRange(0, 4)` | cursor 0, selección 4 | acciones del menú de texto |
| `PanelSettings.textSettings` | existe (campo, tipo `PanelTextSettings`, `null` por defecto); `TextSettings` tiene `fallbackFontAssets`, `defaultSpriteAsset`, `emojiFallbackTextAssets` | subtítulo con ícono por respaldo de fuente (§4.4) |
| `GenericDropdownMenu.DropDown` | se cuelga de `PanelRootElement` (la raíz del panel) | no se usa: quedaría encima del simulador (DT-15) |
| Clic derecho (`PointerDownEvent` botón 1) con todo seleccionado | la selección pasa a 0…0 | el menú de texto frena el evento en *TrickleDown* |
| API de foco | `FocusController.IgnoreEvent(EventBase)`, `NavigationMoveEvent.Direction.Next/Previous` existen | `FocusTrap` (§4.5) |

El repo no se abrió con Unity para esta sonda; el `Library` del proyecto descartable se borró al terminar. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar.

### A.3 S-B3a — texto y teclado en el proyecto real (WP-B, antes de cerrar B2)

Una prueba PlayMode **temporal** (`SB3aProbe`, borrada después de correrla) sobre un `UiTestHost` del proyecto (el panel de `RuntimePanelFactory` con los respaldos del `UiAssets.asset`, TV, densidad 2,625), con `tools/unity/run-unity-tests.ps1 -Platform PlayMode -Filter SB3aProbe`, dos corridas. Las teclas se mandan al `TextElement` interno del campo, que es la hoja con foco: `focusController.focusedElement` devuelve el `TextField` (la raíz del compuesto) y un evento dirigido a él no llega al editor de texto. Resultados (`[S-B3a]` del registro; los recortes PNG quedaron en `<scratch>/it3/probe-b3a/`):

| # | Pregunta | Medido | Qué hace el diseño |
|---|---|---|---|
| 1 | Subtítulo con el glifo por respaldo | La copia tiene `RowMateIcons-Outline-SDF` (con U+E8E5) y `NotoEmoji-Medium-SDF`. El glifo se dibuja en la línea, sin corrimiento de la línea de base a la vista; avance 14,48 dp (`IconView` 14: 14,1). El renglón con el glifo mide 18,7 dp (14,9 sin él). **Color**: con la base en `text.secondary` el glifo sale `#3994B4`, porque TextCore usa el alfa más chico entre el elemento y la etiqueta; con la base opaca y el texto con su `<color>` de `text.secondary`, el glifo sale `#4CC9F0` y el texto `#B6BABF`, igual que por USS | Camino principal, con la base opaca (`text.primary`, en línea) y `<color>` en el texto (`StepSubtitleView`). No hace falta el respaldo de tres piezas ni `<voffset>` |
| 1b | `Remo 🚣 ñ` con y sin Noto Emoji | Con Noto Emoji: el glifo monocromo de Noto. Sin Noto Emoji (otro panel de la misma fábrica sin el segundo respaldo, medido primero en el proceso): **Windows dibuja el emoji de color del sistema** (`enableEmojiSupport`), no `□`. El texto no se corre en ningún caso. TextCore guarda en Inter lo que encontró en un respaldo, así que en un mismo proceso manda la primera búsqueda. El `PanelTextSettings` de la copia tiene `missingCharacterUnicode` = 0 (sin carácter de reemplazo configurado) | Todos los paneles salen de la fábrica con Noto Emoji (app, `UiTestHost`, arnés), así que la primera búsqueda siempre la resuelve Noto |
| 2 | `selectAllOnFocus` con foco por puntero | Un clic deja el cursor donde se hizo clic (8…8), aun con `selectAllOnFocus = true`. `Focus()` por código selecciona todo (0…12) | R3-05 no pasa; `selectAllOnMouseUp = false` queda igual |
| 3 | Clic derecho en *TrickleDown* | Con 0…12 seleccionado, la selección queda igual con y sin el freno (S3-b había visto 0…0 en el proyecto descartable) | Se deja el freno: no cambia nada y cubre lo de S3-b |
| 4 | Pegar con `\n`, `\r` y `\t` | El `Paste()` de UI Toolkit (`UnityEngine.TextEditingUtilities`, IMGUI) llama a `ReplaceNewlinesWithSpaces`: `"a\nb\r\nc\td"` → `"a b c\td"`. El filtro del campo recibiría espacios y no podría quitar los saltos. `HandleKeyEvent` lleva Ctrl+V al mismo `Paste()` | `RmTextField` pega por su cuenta con `IClipboard` (Ctrl+V, Mayús+Insert y el comando `Paste`, frenados en *TrickleDown*): `"a\nb\r\nc\td"` → `"abcd"` en una línea (AC-RUT-11-02) |
| 5 | `hideMobileInput` | La API existe; por defecto `false`, queda en `true` | Se asigna; el efecto, en [Device-And] |
| 6 | Tab en la multilínea | `KeyDown(Tab, '\t')` **escribe** `\t`; `NavigationMoveEvent(Next)` **no saca** el foco del campo | `RmTextField` frena el Tab y mueve el foco él mismo (una vez); también frena el `NavigationMove` Next/Previous y, si llega solo, mueve por el mismo camino. Medido: `"x"` queda igual y el foco pasa al campo siguiente |
| 7 | Escape con foco | El `TextField` **revierte** lo escrito (`"abcd"` → `"abc"`) y no suelta el foco | `RmTextField` frena Escape y `NavigationCancel`: el texto queda y el foco también |
| 8 | Escape una sola vez | Con el diálogo de eliminar abierto, un `NavigationCancelEvent` al elemento con foco no cierra nada; `HandleBack(Escape)` cierra el diálogo y nada más | Un solo camino (`AppRoot.Update` → `HandleBack`), como §4.2 |
| 9 | Enter en una línea | El `TextField` no suelta el foco y no cambia el valor | `RmTextField` hace `Blur()` y el `NavigationSubmit` no sale del campo (medido: 0 al contenedor) |
| 10 | Cursor y tema por defecto | El cursor se dibuja y **no parpadea**: 0 píxeles distintos en 1,5 s de reloj real; ningún campo `blink` en `UnityEngine.UIElements`. Transiciones resueltas en el campo y en `#unity-text-input`: `all 0s` | Las tomas son deterministas sin ocultar el cursor |
| 11 | `enableRichText` y `parseEscapeSequences` | `Label` por defecto: `enableRichText = true`, `parseEscapeSequences = false`; `TextElement` del campo: los dos en `false`. `<b>A</b>` sin texto enriquecido mide 73,1 dp (10,3 con él); `Serie\n2` (barra y `n`) en una línea con `parseEscapeSequences = false` y en dos con `true` | Se apagan los dos en todo `Label` con texto del usuario y en el campo (DN3-22) |
| 12 | `white-space` | Ni `normal` ni `pre-wrap` juntan dos espacios (21,7 / 25,9 dp); `\n` salta en los cuatro modos; una palabra de 200 `W` en 200 dp **se parte** con `normal` y `pre-wrap` (194,7 × 288) y no con `pre` ni `nowrap` | `DisplayText.BreakLongRuns = false`: no hace falta insertar U+200B (QA-G3-13 f) |
| 13 | `FocusInEvent` desde un hijo del `ScrollView` | Burbujea: 1 en el `ScrollView` y 1 en su contenido | `FocusScroller` escucha en el `ScrollView` (QA-G3-11) |

`git -C ../rowmate-legacy status --porcelain` vacío antes y después; los `FontAsset` generados no cambiaron con las corridas.

---

## Anexo B. Pedidos de 04-design-notes (DN3-01 a DN3-23)

04-design-notes se escribió después de la primera versión de este diseño. QA-G3-02 pide aceptar o rechazar cada pedido y dejar el contrato al día. **Se aceptan los 23.** Ninguno cambia datos, reglas ni textos del catálogo, y los que tocan un AC ya están en la spec corregida (spec §16, QA-G3-02 y -03). Dos se aceptan con un cambio de dueño (DN3-16, DN3-21) y dos como P2, sin AC que los pida (DN3-19, DN3-20). "Antes de" dice qué entrega no puede empezar sin el pedido resuelto. Con este documento, **todos quedan resueltos antes de WP-A**.

| ID | Decisión | Qué cambia en este diseño | Paquete | Antes de |
|---|---|---|---|---|
| DN3-01 | Aceptado | Tokens 127 → **133** (`overlay.scrim`, `border.input`, `text.selection`, `titleXs`, `headerS`, `buttonS`) y `Typography.uss` con tres clases: §1.1, §7.1, §7.5 | WP-D0 | WP-A |
| DN3-02 | Aceptado | La tabla de clases y colores de 04-design-notes §2.2 reemplaza a la de §7.5 (descripción `text.secondary`, resumen `text.tertiary`, título de fila y `Pasos (N)` `title-xs`, `Serie` `header-s`, pie `button-s`, etiqueta `body` con escala, ayuda `body-xs`) | WP-B | B1 (lista) · B2 (editor, diálogos) |
| DN3-03 | Aceptado (resuelve DT-19; DV-12 ampliada por el PO, QA-G3-03) | `.rm-step-row__title` e ícono de la insignia en `step.<tipo>.hud`; borde, insignia y mini-barra en el color base: §4.4, §7.5; `SRM.TitleColorAndIcon` | WP-A (SRM) · WP-B | B2 |
| DN3-04 | Aceptado (AC-RUT-14-03 corregido) | `.rm-chip--repeat` como caja de 48 con `.rm-chip__pill` adentro; número en `accent.onTonal`: §4.4, §7.5; `RED.ChipTextAndTouchArea`; CAP-10 | WP-B | B2 |
| DN3-05 | Aceptado (AC-RUT-14-02 lo describe) | Riel continuo en `.rm-series__steps`, `--in-series` con `margin-left` **13**, hojas `.rm-series__stack`: §4.4, §7.5; `RED.SeriesBlockContract`; CAP-10, -12, -13 | WP-B | B2 |
| DN3-06 | Aceptado | Caja de 36 del ícono de la tarjeta, `⋮` con `margin-top` −14, `.rm-highlight` (no `.rm-button__state`): §4.3, §7.5 | WP-B | B1 |
| DN3-07 | Aceptado (AC-RUT-10-11 lo describe) | `StructureBarLayout` puro (capacidad `⌊(W + 2) / 4⌋`, tope 24, mínimo 2, izquierda, 6 que se apagan, pista vacía) y `SegmentWidth`/`Overflow`, calculado en `GeometryChangedEvent`: §1.1, §3.6, §4.3; `SBL`; CAP-04 | WP-A (regla) · WP-B (vista) | WP-A · B1 |
| DN3-08 | Aceptado (DV-09) | `.rm-segmented--tiles`: 2 × 2 o 4 en fila según el contenido ≥ 400: §4.5, §7.5; CAP-14, CAP-23 | WP-B | B2 |
| DN3-09 | Aceptado (DT-02 → DV-19) | `RmTextField` con etiqueta adentro, `--floating`, sin anillo, `min-height`, error de par con las dos cajas: §4.10, §7.5 | WP-B | B2 |
| DN3-10 | Aceptado (AC-RUT-13-11 lo describe) | Anchos 400 / 560 con mínimo 280, `--compact`, `__pinned`, `__content--scrolls`, foco y desplazamiento al primer error: §4.5, §7.5; `SDT.FocusGoesToTheFirstError`; CAP-08, -14, -17, -23 | WP-B | B2 |
| DN3-11 | Aceptado | Posición del menú, `--dense`, capa del ítem de peligro con `--rm-overlay-scrim`, anillo interior: §4.6, §7.5; CAP-07, CAP-26 | WP-B | B1 (⋮) · B2 (texto) |
| DN3-12 | Aceptado, con `pickingMode = Ignore` en todo su árbol (QA-G3-01) | Ayuda 8 dp debajo del ancla, arriba si no entra, ancho máximo 240: §4.6, §7.5 | WP-B | B1 |
| DN3-13 | Aceptado | Obstáculo que **se cruza** (agrandado 8 dp), ancho por ventana, empujón y cruce de texto, resorte si cambia un obstáculo: `BottomObstacles` (Core) y §4.7; `BOT`; CAP-18, -19, -28 | WP-A (regla) · WP-B | B1 |
| DN3-14 | Aceptado (AC-BLE-16-02 reescrito, QA-G3-05) | Llave en barra + 88 en Rutinas (x fija), el `+` como obstáculo solo con Rutinas elegida y sin página encima, panel siempre en barra + 16, rellenos inferiores por obstáculos: §1.1, §4.3, §4.8; `SPL`, `SimulatorOverlayTests`; CAP-01, -06, -09 | WP-A (regla) · WP-B | B1 |
| DN3-15 | Aceptado (AC-RUT-10-02 lo describe) | `Spinner` 32 / 3, gracia de 120 y fundido de 160, estados centrados entre las barras, `#routines-error-icon`: §4.3, §7.5; `RLT.LoadingShowsOnlyTheIndicator` y CAP-02 esperan la gracia | WP-B | B1 |
| DN3-16 | Aceptado **con dueños corregidos** (QA-G3-02) | Las constantes de §5.11 van a `MotionSpecs` (WP-A); `GrowMotion` y `CollapseMotion` (alto **y** márgenes) y "escalonar solo con la pestaña a la vista" son de WP-B (`Presentation/Motion/*`, `RoutineListView`): §1.1, §4.3, §4.9 | WP-A · WP-B | WP-A · B1 |
| DN3-17 | Aceptado | Tamaño del ícono por parámetro en `IconButton` y `RmButton` (20 en filas y encabezado, 16 en el pie; por defecto 24 y 18): §1.1, §7.5 | WP-B | B1 |
| DN3-18 | Aceptado y ampliado (QA-G3-11) | Desplazar hasta lo nuevo (ω 12, 16 dp); teclado a 16 dp con el diálogo recentrado (ω 22); además, el foco por Tab (`FocusScroller`): §4.4, §4.10, §4.11 | WP-B | B2 |
| DN3-19 | Aceptado como **P2** | *Spinner* de 16 en `Guardar` si el guardado tarda más de 300 ms: §4.4. Ningún AC lo pide; si no entra en B2, queda anotado en el REPORT | WP-B | — |
| DN3-20 | Aceptado como **P2** | Alto viejo → nuevo en `GrowMotion` y mini-barra que interpola: §4.9. Ningún AC lo pide | WP-B | — |
| DN3-21 | Aceptado, **con dueños** (QA-G3-12, -15) | Fábrica única `RuntimePanelFactory` (AppRoot, UiTestHost y arnés) con los respaldos en el orden de 04-design-notes §4.15; Noto Emoji: `.ttf` y licencia en WP-D0, `FontAsset`, `FontAssetPaths.All` (10) y bootstrapper en B2 (protegido por `GeneratedAssetGuard`); S-B3a con `Remo 🚣 ñ`: §1.1, §4.4, §7 | WP-D0 · WP-B | B2 |
| DN3-22 | Aceptado y ampliado (QA-G3-10) | `enableRichText = false` **y** `parseEscapeSequences = false` en el texto del usuario (tarjeta, diálogo de eliminar y campos), `white-space: pre-wrap`; pruebas con `<b>A</b>` y `Serie\n2`: §4.3, §4.10, §7.5; `RLT.UserTextIsShownAsWritten` | WP-B | B1 (tarjeta, diálogo) · B2 (campos) |
| DN3-23 | Aceptado | Las sondas de 04-design-notes §7.2 con `WithProbe(…, aspect: true)` y las reglas de toma de §7.1: §5.1, §5.4 | WP-B | B3 |

---

## Cambios por gate QA

Correcciones por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md), APROBADO CON OBSERVACIONES: 0 bloqueantes, 7 mayores, 14 menores), 2026-10-27. Se resolvieron los 16 issues del Architect (4 mayores y 12 menores), sin disputas. Se alineó el diseño con la spec corregida (QA-G3-03, -04, -05 y -17 del PO) y con 04-design-notes (Anexo B).

**Verificación hecha.** Legacy (solo lectura): `routine_editor_screen.dart:40-73` (`_save`: `if (mounted) Navigator.pop` después del `await`), `:108-133` (`_addSeries`, `g{ms}_{contador}`), `:150-188` (repeticiones), `:528-562` (`_build`, `_buildSplitSeconds`: `total > 0`). SDK de Flutter: `widgets/routes.dart:1153-1156`, `:1215-1223` (la ruta que sale ignora toques y suelta el foco), `:2273-2281` (su velo también); `widgets/editable_text.dart:3736-3753`, `:3914-3947`, `:5642-5648`; `widgets/app.dart:1271-1276`; `widgets/focus_traversal.dart:204-213` (Tab lleva a la vista con `Scrollable.ensureVisible`). gilzoide: `sqlite3_defines.h:5`, `:7`, `:10` y `SQLite.cs:40-55`, `:411`, `:1149`, `:4802-4944`. `sqlite-net-pcl` 1.9.172 por reflexión: los 13 métodos de bajo nivel, públicos y estáticos. UI Toolkit 6000.3.27f1: documentación XML de `WhiteSpace` (`Normal`: "trailing sequences of whitespace are collapsed", y no aclara qué pasa con los espacios internos ni con `\n`, por eso S-B3a lo mide; `PreWrap`: conserva los espacios y ajusta) y de `TextElement.parseEscapeSequences`. Repo: `Tests/Support/RecordingLogger.cs`, el `HostLogger` de `Tests/UiSupport/UiTestHost.cs`, `Bootstrap/AppRoot.cs` (Escape en `Update`, copia del panel en `Awake`), `Bootstrap/UiAssets.cs`, `Tests/UiSupport/UiAssetsLocator.cs`, `Core/Design/FontAssetPaths.cs` (`All` = 9), `Editor/Bootstrap/GeneratedAssetGuard.cs`, `Tests/EditMode/Project/ItTwoDebtTests.cs` (cuenta 9), `Infrastructure/AssemblyInfo.cs` (`InternalsVisibleTo`), `Tests/EditMode/RowMate.Tests.EditMode.asmdef`, `Tests/EditMode/Project/PureCodeRulesTests.cs`, `SimulatorPresentation/SimulatorRibbon.cs` (`Ignore` en la cinta y en su `Label`), `Simulator/Panel/SimulatorPanelLayout.cs`, `tools/dotnet/run-tests.ps1` (`-Matrix` lee todos los `It0*Ids.cs`), `tools/unity/capture-window.ps1`. Ningún `.cs` de producción nombra hoy `rower_app`, `com.example`, `.dart_tool`, `sqflite` ni `shared_preferences`. No se leyó `lib/core/strava/strava_secrets.dart`; `git -C ../rowmate-legacy status --porcelain` vacío antes y después. Es un cambio de documento: no se tocó código, no se corrió dotnet y no se abrió Unity.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-G3-01 | mayor | `RoutineEditorStore` queda cerrado después de `Saved` (`IsClosed`: todo `SaveAsync` posterior da `Ignored` y ninguna acción cambia nada). `ExitLock` vuelve inerte **todo el árbol** de la página, el diálogo o el menú en el cuadro 0 de su salida (`pickingMode = Ignore` y `focusable = false` en cada elemento, porque `pickingMode` no se hereda) y saca el foco, que vuelve a quien lo tenía. `BackNavigation` saltea lo que sale. La ayuda va entera con `Ignore`. `Close` del diálogo y `Pop` de la página son idempotentes. Pruebas: `RET.SaveAfterSavedIsIgnored`, `RED.TapDuringTheExitCreatesNothing`, `RED.EnterDuringTheExitDoesNothing`, `NAV.MenuItemTwiceOpensOneDialog`, `NAV.DialogButtonTwiceActsOnce`, `NAV.BackSkipsWhatIsClosing`. Precisión sobre el arreglo propuesto: el velo del diálogo y el atrapador del menú se **tragan** los toques durante la salida (DT-21, informativo para el PO). Si los dejaran pasar, como Flutter, un doble toque en `Confirmar` podría borrar un paso del editor de atrás | §0, §1.1, §3.3, §4.1, §4.2, §4.3, §4.5, §4.6, §6.2, §6.3, §7.5, §8.1 |
| QA-G3-02 | mayor | Anexo B: los 23 DN3 aceptados (DN3-16 y -21 con dueños corregidos; DN3-19 y -20 como P2). §7.5 reescrito: contrato y tabla de clases y colores. §5.4 al día: CAP-01 (llave sobre el `+`), CAP-02 (gracia), CAP-04 (`SegmentWidth`, `Overflow`), CAP-06, -07, -08, -09, -10, -12, -13, -14, -17, -18, -23, -26 y -28. También al día §4.3 (mini-barra con `⌊(W + 2) / 4⌋` en `GeometryChangedEvent`, indicador de 32 con gracia), §4.4, §4.5 (anchos 400 / 560), §4.6, §4.7 (obstáculos que se cruzan), §4.8 y §4.9. Todo queda resuelto antes de B1 y B2 | Anexo B, §1.1, §3.6, §4.3-§4.9, §5.4, §7.5 |
| QA-G3-06 | mayor | DT-12 con las cinco condiciones: `capture-cap27.ps1` aparte, solo con `-ReplaceUserDatabase`, fuera de `capture-snapshots`, de los otros scripts y del CI; se niega a correr con `RowMate` o `Unity` abiertos o con archivos en uso; resguardo de `db`/`-wal`/`-shm` en una carpeta calculada en el momento, con SHA-256 verificado **antes** de reemplazar; restauración en `finally` con *hash* y fecha, y si falla conserva el resguardo e imprime cómo recuperar; base sintética (`Escalera SPM`). `ITT.Cap27RunsOnlyOnRequest`, `ITT.Cap27ProtectsTheUserDatabase`. El PO ya agregó la excepción en AC-HIS-20-04, §0.4, §7.1 y §12.1 | §0, §1.1, §5.5, §6.3, §8.1 |
| QA-G3-07 | mayor | `internal Task<IReadOnlyList<object?[]>> QueryRawForTestsAsync(string sql, params object?[] args)` en `RowMateDatabase`, que solo devuelve `long`, `double`, `string`, `byte[]` o `null` y usa la API de bajo nivel con `var` (verificada en las dos librerías). DSC, DOT y RRT la usan. `PureCodeRulesTests.SqliteOnlyInInfrastructure`. SQU pasa a `Tests/EditMode` como `DSC.SqliteVersionIsTheExpectedOne`, porque `RowMate.Tests.UnityEditMode` no ve los `internal` de Infrastructure. WP-A compila `RowMateDatabase.cs` también contra el `SQLite.cs` de gilzoide en `<scratch>` | §1.1, §1.2, §2.7, §3.1, §6.2, §7.0, §7.2 |
| QA-G3-08 | menor | `DatabaseSchema.WritePragmas` se ejecuta siempre (gilzoide: WAL en `NORMAL` por defecto y `SQLITE_DQS 0`). `DSC.WriteConnectionIsWalFullAndNoForeignKeys` (`wal`, `2`, `0`) corre en dotnet y en Unity EditMode. Literales SQL solo con comillas simples (`PureCodeRulesTests.SqlLiteralsUseSingleQuotes`). R3-14 | §0, §2.1, §6.2, §6.3, §8.2 |
| QA-G3-09 | menor | `RecordingLogger` con `lock` y copias al leer (`RLG.ConcurrentLogsAreAllKept`). `_disposed` en *stores*, páginas, diálogos y capas: lo que vuelve tarde no toca nada (`RST.LateResultsAfterDisposeChangeNothing`, `RET.LateSaveAfterDisposeTouchesNothing`). "Atrás" durante un guardado lento cierra el editor y el guardado termina igual, como el legacy (`RED.BackDuringASlowSaveClosesAndTheSaveLands`). `SerialExecutor.Dispose` se desuscribe de `DomainUnload` (`SET.DisposeUnsubscribesTheUnloadHook`). Las fallas de cargar, guardar y borrar se registran como `Warning` (`RST.ExpectedFailuresAreWarnings`) | §1.1, §2.6, §3.2, §3.3, §6.2, §6.3, §8.2 |
| QA-G3-10 | menor | S-B3a suma los seis casos (Tab en la descripción, Escape con foco, Escape una sola vez, Enter, cursor y transiciones del tema, `parseEscapeSequences`), más `white-space` y la palabra larga. La respuesta del diseño ya está decidida para cada resultado (tabla de §4.10). El único camino de Escape es `AppRoot.Update` → `HandleBack`. `parseEscapeSequences = false` junto con `enableRichText = false`, y `white-space: pre-wrap` en el texto del usuario. Pruebas con `<b>A</b>` y `Serie\n2`, `TXT.TabFromTheDescriptionMovesFocus`, `TXT.EscapeInAFieldKeepsTextAndFocus`, `TXT.EnterInASingleLineFieldBlurs`, `TXT.EnterInADialogFieldDoesNotConfirm` y `NAV.EscapeClosesOneLevelAtATime`. R3-15 | §0, §4.2, §4.3, §4.4, §4.10, §6.2, §6.3, §7.0, §7.5, §8.2 |
| QA-G3-11 | menor | `FocusScroller` en `#routines-list`, `#editor-scroll` y `.rm-dialog__content`: en `FocusInEvent`, si lo enfocado no se ve entero con 16 dp de aire, desplaza con ω 22 (en el acto con "reducir movimiento"). Lo usan también lo nuevo, el primer error y el teclado. `NAV.TabScrollsTheFocusedIntoView` (60 filas con `RoutineSeeds.Sixty`; diálogo a 640 × 360) | §1.1, §4.4, §4.5, §4.10, §4.11, §5.3, §6.3 |
| QA-G3-12 | menor | `RuntimePanelFactory`, la única fábrica de la copia del panel con la copia de `PanelTextSettings` y los respaldos (íconos y Noto Emoji). La llaman `AppRoot.Awake` (antes del `OnEnable` del documento), `UiTestHost` y el arnés, con el mismo `UiAssets.asset`. Noto Emoji entra en `FontAssetPaths.All` (10) y por eso en `GeneratedAssetGuard`. `RPF.TheThreeHostsShareTheFallbacks` | §1.1, §4.4, §6.2, §7.1, §7.3 |
| QA-G3-13 | menor | (a) Split ≤ 0 = sin split, por encima del tope (`SFT.NonPositiveSplitIsNoSplitEvenPastTheCap`). (b) Negativo enorme en repeticiones = sin cambio (`RCT.HugeNegativeIsUnchanged`). (c) Progresiones ocultas de un paso suelto: no se controlan ni en `Confirm()` ni al guardar (`SFT.LooseStepHiddenProgressionsAreNotChecked`, `RET.LooseStepProgressionsDoNotBlockTheSave`). (d) Campos del modo no elegido, sin controlar (`SFT.HiddenModeFieldsAreNotChecked`). (e) Archivo de 1 a 99 bytes = `Unreadable`, con o sin la firma (`DOT.ShortFilesAreUnreadableAndUntouched`). (f) Palabra de 200 caracteres que se parte (`RLT.LongWordWithoutSpacesWraps`, con U+200B solo al dibujar si hiciera falta). (g) Toda carpeta temporal de prueba lleva `ñá` (`TDT.WorksUnderAFolderWithAccents`). El PO confirmó (a) y (b) | §1.1, §2.3, §2.4, §3.4, §3.5, §4.3, §6.2, §6.3 |
| QA-G3-14 | menor | AC-HIS-17-03 con un test estático de WP-A: `LIT.NoCodeReachesTheLegacyData` (ningún `.cs` de producción nombra `com.example`, `.dart_tool`, `sqflite`, `shared_preferences` ni `FlutterSharedPreferences`, y `rower_app` aparece solo en `DatabaseFiles.cs`). Es de WP-A y no de WP-B (QA lo proponía como dB), porque no depende de nada de Unity. Nombre único: `RPS.VariantsDoNotMix` | §2.5, §6.2, §6.3, §6.5 |
| QA-G3-15 | menor | Excepciones explícitas a `Tests/EditMode/**` [A]: los tests de diseño son de [D0] y `ItThreeToolingTests` e `ItTwoDebtTests` son de [B]. Nuevas filas: `Typography.uss` (G, [D0]); Noto Emoji `.ttf` + licencia [D0]; `FontAssetPaths.cs`, el bootstrapper y el `FontAsset` generado [B] | §1.1, §7.1-§7.3 |
| QA-G3-16 | menor | R3-13 con la alternativa: el menú propio al mantener presionado (`TextContextMenu` en modo táctil, textos del catálogo), que es la variante de P-06 que el PO ya escribió; el alto del teclado por JNI si `TouchScreenKeyboard.area` falla. Se decide en [Device-And]. `TXT.LongPressMenuInAppMenuMode` | §0, §1.1, §4.10, §6.3, §8.2 |
| QA-G3-19 | menor | Relojes separados: `CreationClock` lo lee solo `RoutinesStore` (una lectura por alta = "un minuto más por cada rutina creada"); los ids de serie usan el `Scheduler`. `UiHostOptions.CreationClock` va aparte del `FakeScheduler`. `SIG` con reloj fijo y `RPS.SeriesBlocksSurviveReopen` con dos series en el mismo milisegundo | §1.1, §3.2, §3.3, §3.6, §3.7, §5.2, §6.3 |
| QA-G3-20 | menor | Ninguna ruta en el registro ni en los mensajes. `[DB]`, `DatabaseOpenException` y `TempDatabase` llevan el nombre del archivo; el `Message` de `SQLiteException` no se registra, porque trae la ruta (`SQLite.cs:411`). `[DB] humo: …` sin carpeta. La carpeta de resguardo de CAP-27 se calcula en el momento. Ni el `manifest.json` ni el `INDEX.md` llevan rutas. `DOT.ProblemsNeverCarryTheFolder`; ITT revisa los scripts | §1.1, §2.3, §2.4, §5.1, §5.5, §6.1, §6.2 |
| QA-G3-21 | menor | `PRF.MainThreadWorkStaysUnderTheFrameBudget` reemplaza a `PRF.NoLongFramesWhileLoadingOrSaving`: mide con `Stopwatch` nuestro código en el hilo principal por cuadro, con un umbral declarado de 16 ms, 3 repeticiones de calentamiento y el máximo en el registro. La fluidez a la vista queda en [Device-Win] | §6.2, §6.3, §6.5, §8.2 |

**Alineación con la spec corregida (issues del PO).** QA-G3-03: §7.5 y `SRM` con `step.<tipo>.hud` y `accent.onTonal` (DV-12 ampliada), y DT-19 resuelto. QA-G3-04: CAP-13 se comprueba con 16 ± 1 (§5.4, §6.3). QA-G3-05: AC-BLE-16-02, con su texto nuevo, entra en `It03Ids` (**102** IDs), en §4.8, en la matriz (§6.3), en `SPL` y en `SimulatorOverlayTests`, con el panel que se abre siempre en barra + 16. QA-G3-17: DT-01 → DV-18 y DT-02 → DV-19; DT-05, -09, -10, -13 y DD-01 confirmados (§8.1). También entran los textos nuevos de AC-RUT-09-03, AC-RUT-10-02, -03, -11, AC-RUT-11-05, AC-RUT-13-05, -08, -11, AC-RUT-14-02, -03, -05, -11, AC-APP-03-01 a -04, AC-APP-05-07 y P-06 (spec §16).

**Disputas: ninguna.** Hay tres precisiones sobre lo que proponía QA, todas en la dirección del issue:

1. **QA-G3-01.** `pickingMode = Ignore` en la raíz no alcanza, porque UI Toolkit no lo hereda: `ExitLock` recorre el árbol. Además, el velo y el atrapador del menú se tragan los toques mientras salen (DT-21).
2. **QA-G3-07.** La prueba de la versión de SQLite en Unity pasa a `Tests/EditMode`, porque `RowMate.Tests.UnityEditMode` no tiene `InternalsVisibleTo` de Infrastructure.
3. **QA-G3-14.** El test estático es de WP-A, no de WP-B.

**Pendientes que no son del Architect:** QA-G3-18, del Designer (kerning de R3-06); la variante de P-06 en Android, que se decide en [Device-And]; DT-21, informativo para el PO.

---

## Cambios por verificación (ronda 1)

Correcciones por la verificación de QA de la It. 3 ([05-qa-verification](05-qa-verification.md) §7: 0 bloqueantes, 1 mayor, 9 menores), 2026-10-28.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-V3-01 | mayor | **(a)** La semilla se escribe como `semilla/cap27.db` y el paso 4 la copia como `rower_app.db`. La guardia de `TempDatabase` no se aflojó. El código de la semilla pasó a `RoutineSeeds.WriteFile` (guardia, borra un `-wal`/`-shm` viejo, siembra y cierra), que llaman `SnapshotCli.SeedDatabase` y la prueba nueva. El script siembra con `ROWMATE_DATA_DIR` temporal y comprueba la firma de SQLite (7 si falla). **(b)** Un proceso `Unity` cuenta solo si su ruta termina en `\Editor\Unity.exe` o si no se puede leer; también cuenta `Temp/UnityLockfile` tomado. Las negativas se comprueban antes de sembrar y otra vez antes del resguardo. **(c)** El clic va a `0.625,0.98`: el centro de la 3.ª de 4 pestañas y, en alto, dentro de la barra hasta 3200 dp (con 0,96 salía de la barra pasados los 1600 dp). `capture-window.ps1` solo hace clic si en ese punto está la ventana del build, y solo captura si está adelante en 9 puntos; si no, sale con 3 sin clic ni captura. Después del clic el puntero vuelve a su lugar. **Arreglos de la misma revisión**, sin issue propio: el `finally` cierra solo el build que abrió el script, y no cualquier `RowMate` con esa ruta. Reintenta el borrado mientras el build se cierra. Si no puede volver a copiar un archivo, lo cuenta como diferencia (6, con el resguardo) en vez de cortar con una excepción. Lee la fecha con `RoundtripKind`: antes pasaba por la hora local y en el cambio de horario se podía correr una hora (daba 6 sin motivo). **Ensayo** `-Rehearsal` (§5.5). **Pruebas nuevas:** `ITT.Cap27SeedStepRunsForReal` corre el paso 1 de verdad, con los nombres que lee del script, en dotnet y en Unity. También `ITT.Cap27RehearsalNeverTouchesTheUserFolder`, `ITT.WindowCaptureOnlyClicksAndCapturesItsOwnWindow` y `NBT.Cap27ClickSelectsTheRoutinesTab`, que prueba el punto del script contra la UI real en 6 tamaños. `ITT.Cap27RunsOnlyOnRequest` y `ITT.Cap27ProtectsTheUserDatabase` quedan al día | `tools/unity/capture-cap27.ps1`, `tools/unity/capture-window.ps1`, `Snapshots/SnapshotCli.cs`, `Tests/Support/RoutineSeeds.cs`, `Tests/EditMode/Project/ItThreeToolingTests.cs`, `Tests/PlayMode/NormalBuildTests.cs`, `tools/unity/README.md`; §1.1, §5.5, §6.2, §6.3 |

**QA-V3-01: verificación hecha.** Repo: `Tests/Support/TempDatabase.cs:68-90` (la guardia rechaza `rower_app.db` y `rower_app_dev.db` fuera de `<temp>/RowMate.Tests/`), `Snapshots/SnapshotCli.cs`, `Tests/Support/RoutineSeeds.cs` (`Cap27`, `Apply`), `Infrastructure/Database/RowMateDatabase.cs` (`Dispose` cierra la conexión, y SQLite pasa el WAL al archivo), `Presentation/Styles/Base.uss:110-153` (barra de 64 dp abajo, pestañas con `flex-grow: 1` y `flex-basis: 0`), `Tests/PlayMode/ShellTests.cs` (las 4 pestañas del build normal: Dispositivo, Entrenamiento, Rutinas e Historial; Perfil solo con Strava), `ProjectSettings/ProjectSettings.asset` (ventana de 1280 × 720, redimensionable) y `Test-ProjectLocked` de `tools/build/build.ps1` y `run-unity-tests.ps1`. En la PC, el `unity.exe` de la Hub tiene `ProcessName` `unity` y su ruta queda en la carpeta de la Hub, no en `…\Editor\`. No se leyó `lib/core/strava/strava_secrets.dart`; `git -C ../rowmate-legacy status --porcelain` vacío antes y después.

**QA-V3-01: corridas.** Todas en serie, con una sola instancia de Unity por vez y carpetas propias en `<scratch>/it3/fix-architect-r2*`.

| Qué | Resultado |
|---|---|
| `run-tests.ps1 -ArtifactsPath <scratch>/it3/fix-architect-r2` | compilación con 0 advertencias y 0 errores; Simulator 151/151; EditMode **1447**, pasan 1430, fallan 0, 17 ignorados (+3) |
| Unity EditMode / con simulador | **1478** / 1462 / 0 / 16 (+3) · **1629** / 1613 / 0 / 16 (+3), con el define restaurado |
| Unity PlayMode / con simulador | **188/188** (+1) · **235/235** (+1) |
| Matriz `run-tests.ps1 -UnityResults` con esos cuatro XML | **PASA 377**, FALLA 0, IGNORADO 0, PENDIENTE UNITY 0, PENDIENTE WP-B 0, SIN TEST 0 (igual que QA) |
| Arnés `capture-snapshots.ps1 -Set it3 -Out <scratch>/…` | 47 tomas, 0 con problemas, 0 sondas que fallan. Los 47 PNG y MP4 y el `INDEX.md` son iguales byte a byte a los del repo; `manifest.json` solo cambia en `durationMs`. `SnapshotCli` no cambió ninguna toma |
| La reproducción de QA: `SnapshotCli.SeedDatabase -snapshotSeedDb <scratch>/…/semilla/<nombre>` | `rower_app.db` → 1, con el mensaje de la guardia (la guardia sigue igual). `cap27.db` → 0: un archivo de 28 672 bytes con la firma `SQLite format 3`, solo con `Escalera SPM` y sin `-wal` ni `-shm` |
| `capture-cap27.ps1` sin switch | 4, sin tocar nada |
| `capture-cap27.ps1 -Rehearsal` | 0, en 9 s. Semilla con Unity, resguardo, reemplazo (el mismo SHA-256 que la semilla), restauración y verificación: fechas exactas, el `-shm` que dejó el "build" se fue y `rower_app_dev.db` quedó igual. El `unity.exe serve` de la Hub, abierto, **no** aparece. Solo se informa el `RowMate.exe` de la It. 2 que seguía abierto (la toma real se negaría por él). No quedó ninguna carpeta `RowMate.Cap27.*` |
| Carpeta real del build normal (solo la lista de archivos, sin abrir ninguno) | Sin `rower_app.db`, `-wal`, `-shm` ni `rower_app_dev.db`, antes y después. Solo cambió el `TestResults.xml` que el Unity Test Framework escribe ahí al final de cada corrida de PlayMode, como ya pasaba antes de esta corrección |

**QA-V3-01: lo que queda.** CAP-27 sigue sin tomarse, porque es solo a pedido del usuario (§5.5; 05-qa-verification §8). Además, hoy la toma real se negaría (5) por el `RowMate.exe` de la It. 2 que está abierto. Con los avisos del Firewall pendientes (QA-V3-08), `capture-window.ps1` saldría con 3 si la tapan. Cuando el usuario la pida: cerrar RowMate y el Editor, contestar los avisos y correr `tools/unity/capture-cap27.ps1 -ReplaceUserDatabase`, si se quiere después de un `-Rehearsal`. Queda una observación fuera del issue: `GetWindowRect` incluye los bordes invisibles de Windows 10 y 11, así que en la foto entran franjas de unos 8 px de lo que hay detrás de la ventana, a los costados y abajo. Una ventana de 1280 × 720 sale de 1296 × 759, como midió QA en la It. 2. Antes de versionar CAP-27 conviene mirarlas o recortarlas con `DWMWA_EXTENDED_FRAME_BOUNDS`.

---

## Cambios por verificación (ronda 2)

Correcciones por la ronda 2 de la verificación de QA de la It. 3 ([05-qa-verification](05-qa-verification.md) §R2.7: 0 bloqueantes, 1 mayor y 1 menor nuevo), 2026-10-28. Al Architect le tocó QA-V3-11. Cambian solo herramientas, una prueba y documentos. No cambió ningún archivo de producción ni ninguna captura.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-V3-11 | mayor | **(a) El arranque.** `capture-window.ps1` pasa `-ArgumentList` solo si `-Arguments` no está vacío (con *splatting*), porque `Start-Process` de Windows PowerShell 5.1 rechaza `''`. Es el defecto que vio QA. Venía de la It. 2, donde CAP-22 siempre se tomó con `-Arguments`. **(b) La espera: un segundo defecto, que encontró la prueba nueva.** Con solo el arreglo de una línea, el ejecutable de prueba seguía fallando. En 5.1, `MainWindowHandle` de un proceso que terminó da `$null` (la excepción del *getter* se traga), y `$null -eq [IntPtr]::Zero` es falso. El bucle lo tomaba como una ventana y el script se caía en `SetForegroundWindow` (`Cannot convert … to type "System.IntPtr"`, también con 1). Ahora `Get-MainWindow` da cero si el proceso no tiene ventana o ya terminó. La espera se corta cuando el proceso termina: sale con 1 enseguida y muestra el código de salida. Después de los `WaitSeconds` vuelve a mirar, por si el build se cerró en ese rato (1). **(c) Códigos.** Con un `trap`, un error no previsto sale con **4**, con su mensaje y su línea, y no con el 1 de "no apareció la ventana". El `finally` corre antes y cierra el build. **(d) Una sola llamada.** `capture-cap27.ps1` llama a `capture-window.ps1` solo desde `Invoke-WindowCapture`, sin `-Arguments`. El ensayo `-Rehearsal` la corre con un ejecutable de prueba sin ventana en lugar del build y espera 1, `el build no abrio ninguna ventana` y ninguna captura. Si no, sale con 8. **(e) El ejecutable de prueba.** Lo arma `new-windowless-exe.ps1` (nuevo) con `Add-Type -OutputType WindowsApplication`: subsistema gráfico, que el script comprueba en el encabezado PE, y `Main` vacío. No abre una consola, termina enseguida y no muestra nada. Siempre se crea en una carpeta temporal. **Pruebas.** `ITT.WindowCaptureStartsTheExeForReal` (nueva) corre el arranque de verdad con Windows PowerShell 5.1, en dotnet y en Unity sobre Windows. Lo hace sin `-Arguments`, con la llamada de `capture-cap27.ps1` (la prueba controla que sea esa), y con `-Arguments`, como CAP-22. Espera 1, los dos mensajes, menos de 20 s y ninguna captura, y revisa el texto de los arreglos. En otros sistemas corre solo la parte estática. `ITT.Cap27RehearsalNeverTouchesTheUserFolder` queda al día: hay una sola llamada, el ensayo la corre con el ejecutable de prueba y nunca usa `$exePath`. El control ASCII y de PowerShell 5.1 de `ITT.WindowCaptureOnlyClicksAndCapturesItsOwnWindow` suma `new-windowless-exe.ps1` | `tools/unity/capture-window.ps1`, `tools/unity/capture-cap27.ps1`, `tools/unity/new-windowless-exe.ps1`, `tools/unity/README.md`, `Tests/EditMode/Project/ItThreeToolingTests.cs`; §1.1, §5.5, §6.2, §6.3 |

**Precisión sobre lo que proponía QA.** El arreglo de una línea hacía falta, pero no alcanzaba: la prueba que pedía QA (que espere `el build no abrio ninguna ventana`) falla con ese arreglo solo, por (b). Las dos opciones que proponía QA, la prueba y el paso del ensayo, están hechas. En lugar del `hostname.exe` de la reproducción, la prueba usa un ejecutable propio. Uno de consola, abierto con `Start-Process`, abre su propia ventana de consola (o una pestaña de Windows Terminal) mientras corre: se vería en la pantalla del usuario y la espera podría tomarla por la ventana del build. Un ejecutable de subsistema gráfico con `Main` vacío no abre ninguna. Desvíos: ninguno.

**QA-V3-11: verificación hecha.** En la PC hay Windows PowerShell 5.1 y no hay pwsh 7. Las sondas y las copias de los scripts quedaron en `<scratch>/it3/fix-architect-r3/repro`.

- **El defecto de QA.** `capture-window.ps1` de antes, con el ejecutable de prueba y los parámetros de `capture-cap27.ps1`, da `Cannot validate argument on parameter 'ArgumentList'. The argument is null or empty.` y sale con 1 en 0,5 s. No arranca ningún proceso ni escribe ningún archivo.
- **Solo con el arreglo de una línea** (en una copia): da `Cannot convert argument "hWnd" … to type "System.IntPtr"` y sale con 1, con `-ClickRelative` y sin él. Una sonda lo explica: después de `Refresh()`, un proceso que terminó tiene `MainWindowHandle` vacío (`$null`) y `ExitCode` 0.
- **Con el arreglo.** Sin `-Arguments` y con `-Arguments '-screen-width 1280 -screen-height 720'` sale con 1 en 0,7 a 0,8 s, con `[ventana] el build no abrio ninguna ventana` y `[ventana] el proceso termino sin ventana (codigo 0)`. No deja PNG ni procesos.
- **Los otros dos caminos**, con un ejecutable sin ventana que queda vivo 30 s:
  - con `-WaitSeconds 0` sale con 1 a los 23 s (los 20 s del plazo más el cierre), sin la línea de "termino", y el `finally` lo cierra;
  - con un `throw` dentro del `try` (en una copia) sale con 4 (`error inesperado (linea 94): …`), y el `finally` también lo cierra.
- **PowerShell 5.1.** En una sonda, el `finally` corre antes del `trap` y el `exit 4` sale igual. Otra sonda: si se captura solo la salida estándar de un `powershell` hijo con `$ErrorActionPreference = 'Stop'`, el script no se corta aunque el hijo escriba en la salida de error, ni en la consola ni con la salida redirigida. Por eso `Invoke-WindowCapture` no usa `2>&1`. Un valor con guion pasado por `-File` (`-Arguments '-screen-width 1280'`) llega entero.
- **Carpetas con acentos.** `Add-Type -OutputType WindowsApplication` y `Start-Process` funcionan en una carpeta con `ñá` y espacios. El `.exe` pesa 3584 bytes y tiene subsistema 2.
- **Sin la toma real.** No corrí `-ReplaceUserDatabase`: CAP-27 es solo a pedido del usuario, y hoy se negaría (5) por el `RowMate.exe` de la It. 2 que sigue abierto. Tampoco corrí el camino con ventana (clic y captura), porque toca el mouse y la pantalla del usuario. Para un proceso vivo con ventana, `Get-MainWindow` da lo mismo que el código de antes (`Refresh()` y `MainWindowHandle`), con el que QA detectó la ventana del build en la ronda 2. Solo cambia cuando el proceso terminó. Ahora, además, después de los `WaitSeconds` vuelve a leer la ventana actual, en lugar de la que había guardado.
- **Legacy.** No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` dio vacío antes y después.

**QA-V3-11: corridas.** Todas en serie, con una sola instancia de Unity por vez y carpetas propias en `<scratch>/it3/fix-architect-r3*`.

| Qué | Resultado |
|---|---|
| `run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it3/fix-architect-r3` | compilación con 0 advertencias y 0 errores; Simulator 151/151; EditMode **1448**, pasan 1431, fallan 0, 17 ignorados (+1). `ITT.WindowCaptureStartsTheExeForReal` corre en 1,9 s |
| Unity EditMode / con simulador | **1479** / 1463 / 0 / 16 (+1) · **1630** / 1614 / 0 / 16 (+1), 0 inconclusos, con el define restaurado. La prueba nueva también corre de verdad en Unity (Mono): `Passed`, 1,96 s |
| Unity PlayMode / con simulador | **188/188** · **235/235**. `NBT.Cap27ClickSelectsTheRoutinesTab`, que lee el punto de `capture-cap27.ps1`, en verde en las dos |
| Matriz `run-tests.ps1 -UnityResults` con esos cuatro XML | **PASA 377**, FALLA 0, IGNORADO 0, PENDIENTE UNITY 0, PENDIENTE WP-B 0, SIN TEST 0; dotnet 1599 / 1582 / 0 / 17 |
| `capture-cap27.ps1 -Rehearsal` | 0 en 13 s. Hace la semilla, el resguardo y el reemplazo; da `[sin-ventana] listo` y `el build no abrio ninguna ventana` por `Invoke-WindowCapture`; restaura y verifica. Solo informa el `RowMate.exe` de la It. 2. No quedan carpetas `RowMate.Cap27.*` ni procesos de prueba |
| Arnés `capture-snapshots.ps1 -Set it3 -Out <scratch>/…` | 47 tomas, 0 con problemas, 0 sondas que fallan. Los 47 archivos y el `INDEX.md` son iguales byte a byte a los del repo, y `manifest.json` solo cambia en `durationMs`. Ninguna captura cambia, porque el arreglo no toca la UI |
| Limpieza | `<temp>/RowMate.Tests/` vacía. La carpeta de datos del build normal sigue sin bases: solo cambió el `TestResults.xml` de las PlayMode, como siempre. `ProjectSettings.asset` igual a `HEAD` y sin `ROWMATE_SIMULATOR`, sin `Library/RowMate/` ni `Assets/Resources`, y los *assets* generados sin cambios. Los servidores de compilación de dotnet, cerrados con `dotnet build-server shutdown` |

**QA-V3-11: lo que queda.**

- CAP-27 sigue sin tomarse, porque es solo a pedido del usuario (§5.5). Para tomarla hay que cerrar el `RowMate.exe` de la It. 2 y el Editor, contestar los avisos del Firewall (QA-V3-08) y correr `tools/unity/capture-cap27.ps1 -ReplaceUserDatabase`, si se quiere después de un `-Rehearsal`.
- **QA-V3-12 (menor) no entró en esta ronda, y con la foto funcionando ya puede pasar.** La captura incluye las franjas de unos 8 px de los bordes invisibles y por defecto se escribe en `docs/…/snapshots/`, que va a un repositorio público. Hasta que se corrija, conviene tomar CAP-27 con `-Out` en una carpeta temporal y mirarla antes de pasarla al repo.
